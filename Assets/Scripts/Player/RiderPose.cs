using UnityEngine;
using Shredsquatch.Procedural;
using Shredsquatch.Tricks;

namespace Shredsquatch.Player
{
    /// <summary>
    /// What the rider is doing this frame, gathered from the gameplay components by RiderAnimator.
    /// </summary>
    public struct RiderPoseInput
    {
        public bool Grounded;
        public float SpeedKmh;
        public float Lean;          // SnowboardPhysics.LeanAngle in degrees, + = carving right
        public float Steer;         // -1..1
        public bool Tucking;
        public bool Braking;
        public bool Charging;       // holding jump on the ground
        public float Charge;        // 0..1
        public float Spin;          // -1..1 spin input while airborne
        public GrabType Grab;       // grab being held, in the air or on a rail
        public bool Flipping;
        public float FlipAngle;     // degrees, + = frontflip
        public bool Grinding;
        public float Balance;       // -1..1 rail balance
        public bool Crashed;        // wiped out, tumbling or lying in the snow
        public bool Recovering;     // getting back up
    }

    /// <summary>
    /// A full-body rider pose. RigPosition/RigRotation place the body relative to the player root
    /// (the feet); every other point is in rig space: origin at the hips, +Z forward, +X right.
    /// </summary>
    public struct RiderPose
    {
        public Vector3 RigPosition;
        public Quaternion RigRotation;

        public Vector3 CameraPosition;
        public Quaternion CameraRotation;   // look offset; FirstPersonCamera's mouse look goes on top

        public Vector3 BoardPosition;
        public Quaternion BoardRotation;

        public Vector3 Pelvis;
        public Vector3 Neck;
        public Vector3 Head;

        public Vector3 HipLeft, KneeLeft, AnkleLeft;
        public Vector3 HipRight, KneeRight, AnkleRight;
        public Vector3 ShoulderLeft, ElbowLeft, HandLeft;
        public Vector3 ShoulderRight, ElbowRight, HandRight;

        public bool IsGrabbing;
        public Vector3 GrabPoint;           // where the held grab touches the board
    }

    /// <summary>
    /// Turns gameplay state into a full-body rider pose over time: soft-knee stance, carving,
    /// tuck, braking skid, jump charge and ollie pop, landing compression, grabs, spins, flips,
    /// boardslides, wipeouts and getting back up. Pure math with no scene access, so it can be
    /// unit tested; RiderAnimator applies the result to the rig's transforms.
    /// </summary>
    public class RiderPoseSolver
    {
        // Body proportions in metres for a ~1.8 m rider. Heights are above the player root.
        public const float HipHeight = 0.9f;
        public const float HipHalfWidth = 0.1f;
        public const float ThighLength = 0.42f;
        public const float ShinLength = 0.42f;
        public const float AnkleHeight = 0.09f;         // above the board's centre
        public const float StanceHalfLength = 0.22f;    // feet either side of the board's centre
        public const float ShoulderHalfWidth = 0.19f;
        public const float ShoulderHeight = 0.48f;      // above the hips
        public const float NeckHeight = 0.56f;
        public const float UpperArmLength = 0.32f;
        public const float ForearmLength = 0.34f;       // elbow to the middle of the glove

        // Deepest knee bend: how far folded legs pull hips and board together
        public const float MaxFold = 0.48f;

        // The camera follows only part of the upper body's hunch so the view stays up and ahead.
        // The torso and head are shadow-only, so the camera never sees itself sitting behind them.
        private const float CameraHunchFollow = 0.3f;

        private const float MaxDeltaTime = 0.05f;
        private const float MinLandingAirtime = 0.12f;      // shorter "landings" are bumps
        private const float HeadingSmoothThreshold = 30f;   // smaller corrections are steering clamps
        private const float TumblePitch = 290f;             // head over heels, ending on the back
        private const float CrashDrop = 0.48f;
        private const float StandUpTime = 1.1f;
        private const float GrindLift = 0.42f;              // board up on top of the rail
        private const float BodyShiftPerDegree = 0.004f;
        private const float LandingNodPerMetre = 18f;
        private const float MaxChatter = 0.8f;              // degrees of board roll at top speed
        private const float MinHeadClearance = 0.2f;        // eyes above the snow under the feet
        private const float HandRate = 14f;

        private readonly float _boardHeight;
        private readonly float _eyeOffset;      // eyes above the hips

        // Smoothed pose channels
        private float _legBend;
        private float _groundBlend;
        private float _hunch;
        private float _twist;
        private float _bodyRoll;
        private float _balanceRoll;
        private float _grindLift;
        private float _nod;
        private float _chatter;
        private Vector3 _boardEuler;           // pitch (+ = nose down), yaw, roll (+ = right edge up)
        private Vector3 _boardOffset;
        private Vector3 _handLeft;              // upper-body space
        private Vector3 _handRight;
        private Vector3 _handLeftTarget;
        private Vector3 _handRightTarget;
        private bool _grabbing;
        private GrabStyle _grabStyle;

        // Impulses
        private DampedSpring _compression;
        private float _ollie;
        private float _yawOffset;
        private float _flipAngle;
        private float _time;
        private float _lastLean;
        private bool _edgeCatchPending;

        // Wipeout
        private bool _wasCrashed;
        private bool _wasRecovering;
        private float _crashTime;
        private float _recoverTime;
        private float _crashSide;               // 0 = forward tumble, +/-1 = slammed onto that side
        private float _crashPitch;
        private float _crashRoll;
        private float _crashDrop;
        private float _recoverPitch;
        private float _recoverRoll;
        private float _recoverDrop;

        public RiderPose Pose { get; private set; }

        public RiderPoseSolver(float eyeHeight = 1.6f, float boardHeight = 0.05f)
        {
            _eyeOffset = eyeHeight - HipHeight;
            _boardHeight = boardHeight;
            Reset();
        }

        /// <summary>
        /// Snap to the standing pose and drop any impulse in progress (new run, respawn).
        /// </summary>
        public void Reset()
        {
            _legBend = 0.05f;
            _groundBlend = 1f;
            _hunch = 6f;
            _twist = 0f;
            _bodyRoll = 0f;
            _balanceRoll = 0f;
            _grindLift = 0f;
            _nod = 0f;
            _chatter = 0f;
            _boardEuler = Vector3.zero;
            _boardOffset = Vector3.zero;
            _handLeft = _handLeftTarget = new Vector3(-0.32f, 0.06f, 0.22f);
            _handRight = _handRightTarget = new Vector3(0.32f, 0.06f, 0.22f);
            _grabbing = false;

            _compression.Reset();
            _ollie = 0f;
            _yawOffset = 0f;
            _flipAngle = 0f;
            _lastLean = 0f;
            _edgeCatchPending = false;

            _wasCrashed = false;
            _wasRecovering = false;
            _crashPitch = 0f;
            _crashRoll = 0f;
            _crashDrop = 0f;

            Pose = BuildPose(0f);
        }

        /// <summary>The rider popped off the ground: snap the tail and lift the nose (ollie).</summary>
        public void Jump()
        {
            _ollie = 1f;
        }

        /// <summary>Absorb a landing in the knees; more airtime compresses further.</summary>
        public void Land(float airTime)
        {
            if (airTime < MinLandingAirtime) return;
            _compression.Velocity += Mathf.Clamp(1.2f + airTime * 2.6f, 1.2f, 6.5f);
        }

        /// <summary>
        /// SnowboardPhysics snapped the heading, e.g. turning a landed 180 into riding switch.
        /// Large snaps swing the body round over a moment instead of teleporting the view.
        /// </summary>
        public void CorrectHeading(float deltaDegrees)
        {
            if (Mathf.Abs(deltaDegrees) < HeadingSmoothThreshold) return;
            _yawOffset = Mathf.DeltaAngle(0f, _yawOffset - deltaDegrees);
        }

        /// <summary>The board's edge caught: the wipeout that follows slams the rider sideways.</summary>
        public void EdgeCatch()
        {
            _edgeCatchPending = true;
        }

        public RiderPose Step(in RiderPoseInput input, float deltaTime)
        {
            float dt = Mathf.Clamp(deltaTime, 0f, MaxDeltaTime);
            _time += dt;

            UpdateWipeout(input, dt);
            UpdateChannels(input, dt);

            _lastLean = input.Lean;
            _edgeCatchPending = false;

            Pose = BuildPose(dt);
            return Pose;
        }

        private void UpdateWipeout(in RiderPoseInput input, float dt)
        {
            if (input.Crashed && !_wasCrashed)
            {
                _crashTime = 0f;
                _crashSide = _edgeCatchPending ? (_lastLean >= 0f ? 1f : -1f) : 0f;
            }

            if (input.Recovering && !_wasRecovering)
            {
                _recoverTime = 0f;
                _recoverPitch = Mathf.DeltaAngle(0f, _crashPitch);
                _recoverRoll = Mathf.DeltaAngle(0f, _crashRoll);
                _recoverDrop = _crashDrop;
            }

            _wasCrashed = input.Crashed;
            _wasRecovering = input.Recovering;

            if (input.Crashed)
            {
                _crashTime += dt;

                if (_crashSide == 0f)
                {
                    // Head over heels, ending flat on the back looking at the sky
                    _crashPitch = TumblePitch * ProceduralAnimation.EaseOutCubic(_crashTime / 0.85f);
                    _crashRoll = 18f * Mathf.Sin(_crashTime * 8f) * Mathf.Exp(-3f * _crashTime);
                }
                else
                {
                    // Slammed down onto the side whose edge caught
                    _crashRoll = -_crashSide * 78f * ProceduralAnimation.EaseOutBack(_crashTime / 0.45f);
                    _crashPitch = 12f * ProceduralAnimation.EaseOutCubic(_crashTime / 0.45f);
                }

                _crashDrop = CrashDrop * ProceduralAnimation.EaseOutCubic(_crashTime / 0.55f);
            }
            else if (input.Recovering)
            {
                _recoverTime += dt;
                float remaining = 1f - ProceduralAnimation.EaseInOutCubic(_recoverTime / StandUpTime);
                _crashPitch = _recoverPitch * remaining;
                _crashRoll = _recoverRoll * remaining;
                _crashDrop = _recoverDrop * remaining;
            }
            else
            {
                _crashPitch = ProceduralAnimation.Damp(Mathf.DeltaAngle(0f, _crashPitch), 0f, 8f, dt);
                _crashRoll = ProceduralAnimation.Damp(Mathf.DeltaAngle(0f, _crashRoll), 0f, 8f, dt);
                _crashDrop = ProceduralAnimation.Damp(_crashDrop, 0f, 8f, dt);
            }
        }

        private void UpdateChannels(in RiderPoseInput input, float dt)
        {
            bool airborne = !input.Grounded && !input.Grinding;
            float speedRatio = Mathf.Clamp01(input.SpeedKmh / 120f);

            float legBend;
            float hunch;
            float twist = 0f;
            float bodyRoll = 0f;
            float nod = 0f;
            Vector3 boardEuler = Vector3.zero;
            Vector3 boardOffset = Vector3.zero;
            Vector3 handLeft;
            Vector3 handRight;

            if (input.Crashed)
            {
                legBend = 0.45f;
                hunch = 10f;

                if (_crashTime < 0.9f)
                {
                    // Arms flailing through the tumble
                    float t = _crashTime;
                    handLeft = new Vector3(-0.45f, 0.5f, 0.1f)
                        + new Vector3(0.25f * Mathf.Sin(t * 11f), 0.25f * Mathf.Cos(t * 9f), 0.15f * Mathf.Sin(t * 7f));
                    handRight = new Vector3(0.45f, 0.5f, 0.1f)
                        + new Vector3(0.25f * Mathf.Sin(t * 10f + 1.3f), 0.25f * Mathf.Cos(t * 8f + 0.7f), 0.15f * Mathf.Sin(t * 6f));
                }
                else
                {
                    // Sprawled in the snow
                    handLeft = new Vector3(-0.62f, 0.25f, 0.05f);
                    handRight = new Vector3(0.62f, 0.25f, 0.05f);
                }
            }
            else if (input.Recovering)
            {
                // Push up off the snow, then stand
                legBend = 0.55f;
                hunch = 20f;
                handLeft = new Vector3(-0.3f, -0.1f, 0.3f);
                handRight = new Vector3(0.3f, -0.1f, 0.3f);
            }
            else if (input.Grinding)
            {
                // Boardslide: hips turned with the board across the rail, shoulders still facing
                // down the hill, arms out in front against the wobble
                legBend = 0.3f;
                hunch = 6f;
                boardEuler = new Vector3(0f, 80f, -input.Balance * 18f);
                handLeft = new Vector3(-0.4f, 0.48f + 0.14f * input.Balance, 0.5f);
                handRight = new Vector3(0.4f, 0.48f - 0.14f * input.Balance, 0.5f);
            }
            else if (airborne)
            {
                // Soft knees, arms out for balance (they come into view at the bottom corners)
                legBend = 0.3f;
                hunch = 8f;
                handLeft = new Vector3(-0.42f, 0.45f, 0.45f);
                handRight = new Vector3(0.42f, 0.45f, 0.45f);

                float spin = Mathf.Clamp(input.Spin, -1f, 1f);
                if (Mathf.Abs(spin) > 0.1f)
                {
                    // Lead with the shoulders: front arm wraps across the chest, back arm swings behind
                    float side = Mathf.Sign(spin);
                    float amount = Mathf.Abs(spin);
                    Vector3 lead = new Vector3(0.14f * side, 0.36f, 0.4f);
                    Vector3 trail = new Vector3(0.5f * side, 0.42f, -0.12f);
                    twist = 22f * spin;
                    legBend = 0.4f;

                    if (side > 0f)
                    {
                        handLeft = Vector3.Lerp(handLeft, lead, amount);
                        handRight = Vector3.Lerp(handRight, trail, amount);
                    }
                    else
                    {
                        handRight = Vector3.Lerp(handRight, lead, amount);
                        handLeft = Vector3.Lerp(handLeft, trail, amount);
                    }
                }

                if (input.Flipping)
                {
                    // Tucked into a ball, hands on the shins
                    legBend = 0.85f;
                    hunch = 30f;
                    handLeft = new Vector3(-0.16f, 0f, 0.32f);
                    handRight = new Vector3(0.16f, 0f, 0.32f);
                }
            }
            else
            {
                // Riding: soft knees, lower and more forward as speed builds, on edge into carves
                legBend = 0.05f + 0.12f * speedRatio;
                hunch = 6f + 6f * speedRatio;
                bodyRoll = input.Lean * 0.25f;
                boardEuler.z = -input.Lean * 0.8f;

                float steer = Mathf.Clamp(input.Steer, -1f, 1f);
                handLeft = new Vector3(-0.32f, 0.06f + 0.1f * steer, 0.22f);    // outside arm rises
                handRight = new Vector3(0.32f, 0.06f - 0.1f * steer, 0.22f);

                if (input.Charging)
                {
                    // Sinking into the knees, deeper the longer jump is held
                    float charge = Mathf.Clamp01(input.Charge);
                    legBend = Mathf.Lerp(0.3f, 0.8f, charge);
                    hunch = 12f + 10f * charge;
                    handLeft = new Vector3(-0.24f, 0.02f, 0.34f);
                    handRight = new Vector3(0.24f, 0.02f, 0.34f);
                }
                else if (input.Tucking)
                {
                    legBend = 0.5f;
                    hunch = 28f;
                    handLeft = new Vector3(-0.12f, 0.08f, 0.4f);
                    handRight = new Vector3(0.12f, 0.08f, 0.4f);
                }
                else if (input.Braking)
                {
                    // Skid: board turned across the fall line, leading edge lifted, weight back
                    legBend = 0.32f;
                    hunch = -8f;
                    boardEuler.y = 32f;
                    boardEuler.z -= 12f;
                    handLeft = new Vector3(-0.36f, 0.3f, 0.36f);
                    handRight = new Vector3(0.36f, 0.3f, 0.36f);
                }
            }

            _grabbing = (airborne || input.Grinding) && !input.Crashed && input.Grab != GrabType.None;
            if (_grabbing)
            {
                _grabStyle = GetGrabStyle(input.Grab);

                // In the air the knees come up to the chest; on a rail the board stays down, so squat
                legBend = Mathf.Max(legBend, airborne ? 0.95f : 0.6f);
                hunch = Mathf.Max(hunch, _grabStyle.Hunch);
                boardEuler += _grabStyle.BoardTweak;
                if (airborne) boardOffset += _grabStyle.BoardOffset;
                // On a rail, keep half an eye on the rail ahead
                nod = airborne ? _grabStyle.Nod : _grabStyle.Nod * 0.5f;

                if (_grabStyle.RightHand) handLeft = Mirror(_grabStyle.FreeHand);
                else handRight = _grabStyle.FreeHand;
            }

            _legBend = ProceduralAnimation.Damp(_legBend, legBend, 12f, dt);
            float groundTarget = input.Grounded || input.Grinding ? 1f : 0f;
            _groundBlend = ProceduralAnimation.Damp(_groundBlend, groundTarget, groundTarget > _groundBlend ? 30f : 8f, dt);
            _hunch = ProceduralAnimation.Damp(_hunch, hunch, 9f, dt);
            _twist = ProceduralAnimation.Damp(_twist, twist, 8f, dt);
            _bodyRoll = ProceduralAnimation.Damp(_bodyRoll, bodyRoll, 8f, dt);
            _nod = ProceduralAnimation.Damp(_nod, nod, 10f, dt);
            _boardEuler = ProceduralAnimation.Damp(_boardEuler, boardEuler, 12f, dt);
            _boardOffset = ProceduralAnimation.Damp(_boardOffset, boardOffset, 12f, dt);
            _grindLift = ProceduralAnimation.Damp(_grindLift, input.Grinding ? GrindLift : 0f, 12f, dt);
            _balanceRoll = ProceduralAnimation.Damp(_balanceRoll, input.Grinding ? -input.Balance * 9f : 0f, 10f, dt);
            bool riding = input.Grounded && !input.Grinding && !input.Crashed && !input.Recovering;
            _chatter = ProceduralAnimation.Damp(_chatter, riding ? speedRatio * speedRatio * MaxChatter : 0f, 10f, dt);
            _handLeftTarget = handLeft;
            _handRightTarget = handRight;

            _compression.Step(0f, 180f, 18f, dt);
            _ollie = ProceduralAnimation.Damp(_ollie, 0f, 7f, dt);
            _yawOffset = ProceduralAnimation.Damp(_yawOffset, 0f, 9f, dt);

            if (input.Flipping && !input.Crashed)
            {
                // Follow the trick's real rotation so the view flips exactly as scored
                _flipAngle = input.FlipAngle;
            }
            else
            {
                // Landed (or bailed): settle the nearest way back to upright
                _flipAngle = ProceduralAnimation.Damp(Mathf.DeltaAngle(0f, _flipAngle), 0f, 10f, dt);
            }
        }

        private RiderPose BuildPose(float dt)
        {
            var pose = new RiderPose();

            // Bent legs lower the hips on the ground and lift the board in the air; landing
            // compression always comes out of the hips so the board stays on the snow
            float legFold = _legBend * MaxFold;
            float compression = Mathf.Clamp(_compression.Value, -0.05f, MaxFold + 0.04f - legFold);
            float hipDrop = legFold * _groundBlend + compression;
            float fold = legFold + compression;

            pose.RigPosition = new Vector3(0f, HipHeight - hipDrop + _grindLift - _crashDrop, 0f);
            pose.RigRotation = Quaternion.Euler(0f, _yawOffset, 0f)
                               * Quaternion.Euler(_flipAngle + _crashPitch, 0f, _crashRoll + _balanceRoll);

            // Board, with a little edge chatter at speed while it's on the snow
            float chatter = _chatter * (Mathf.Sin(_time * 41f) * 0.6f + Mathf.Sin(_time * 23f + 1.7f) * 0.4f);
            pose.BoardPosition = new Vector3(0f, _boardHeight - HipHeight + fold, 0f) + _boardOffset;
            pose.BoardRotation = Quaternion.Euler(_boardEuler.x - 24f * _ollie, _boardEuler.y, _boardEuler.z + chatter);

            // Upper body pivots at the hips and leans into carves
            Vector3 pelvis = new Vector3(_bodyRoll * BodyShiftPerDegree, 0f, 0f);
            Quaternion upper = Quaternion.Euler(_hunch, _twist, -_bodyRoll);
            pose.Pelvis = pelvis;
            pose.Neck = pelvis + upper * new Vector3(0f, NeckHeight, 0f);
            pose.Head = pelvis + upper * new Vector3(0f, _eyeOffset - 0.02f, 0.02f);

            Quaternion cameraLean = Quaternion.Euler(_hunch * CameraHunchFollow, 0f, -_bodyRoll * CameraHunchFollow);
            pose.CameraPosition = pelvis + cameraLean * new Vector3(0f, _eyeOffset, 0.04f);
            pose.CameraRotation = Quaternion.Euler(_nod + compression * LandingNodPerMetre, 0f, 0f);

            // On the ground, keep the head out of the snow while tumbling or rolling a botched
            // flip upright (upside down, the eyes are well below the hips)
            float headHeight = pose.RigPosition.y + (pose.RigRotation * pose.CameraPosition).y;
            if (headHeight < MinHeadClearance)
            {
                pose.RigPosition.y += (MinHeadClearance - headHeight) * _groundBlend;
            }

            // Legs: hips turn with the board, feet stay planted on it (regular stance, left foot forward)
            Quaternion hips = Quaternion.Euler(0f, _boardEuler.y * 0.9f, 0f);
            pose.HipLeft = hips * new Vector3(-HipHalfWidth, -0.03f, 0f);
            pose.HipRight = hips * new Vector3(HipHalfWidth, -0.03f, 0f);
            Vector3 ankleLeft = pose.BoardPosition + pose.BoardRotation * new Vector3(0f, AnkleHeight, StanceHalfLength);
            Vector3 ankleRight = pose.BoardPosition + pose.BoardRotation * new Vector3(0f, AnkleHeight, -StanceHalfLength);
            pose.KneeLeft = ProceduralAnimation.SolveTwoBone(pose.HipLeft, ankleLeft, ThighLength, ShinLength,
                hips * new Vector3(-0.3f, 0f, 1f), out pose.AnkleLeft);
            pose.KneeRight = ProceduralAnimation.SolveTwoBone(pose.HipRight, ankleRight, ThighLength, ShinLength,
                hips * new Vector3(0.3f, 0f, 1f), out pose.AnkleRight);

            // Arms: a grabbing hand reaches for its spot on the board
            Vector3 handLeftTarget = _handLeftTarget;
            Vector3 handRightTarget = _handRightTarget;
            pose.IsGrabbing = _grabbing;
            if (_grabbing)
            {
                pose.GrabPoint = pose.BoardPosition + pose.BoardRotation * _grabStyle.GrabPoint;
                Vector3 grabLocal = Quaternion.Inverse(upper) * (pose.GrabPoint - pelvis);
                if (_grabStyle.RightHand) handRightTarget = grabLocal;
                else handLeftTarget = grabLocal;
            }

            _handLeft = ProceduralAnimation.Damp(_handLeft, handLeftTarget, HandRate, dt);
            _handRight = ProceduralAnimation.Damp(_handRight, handRightTarget, HandRate, dt);

            pose.ShoulderLeft = pelvis + upper * new Vector3(-ShoulderHalfWidth, ShoulderHeight, 0f);
            pose.ShoulderRight = pelvis + upper * new Vector3(ShoulderHalfWidth, ShoulderHeight, 0f);
            pose.ElbowLeft = ProceduralAnimation.SolveTwoBone(pose.ShoulderLeft, pelvis + upper * _handLeft,
                UpperArmLength, ForearmLength, upper * new Vector3(-0.9f, -0.7f, -0.5f), out pose.HandLeft);
            pose.ElbowRight = ProceduralAnimation.SolveTwoBone(pose.ShoulderRight, pelvis + upper * _handRight,
                UpperArmLength, ForearmLength, upper * new Vector3(0.9f, -0.7f, -0.5f), out pose.HandRight);

            return pose;
        }

        private static Vector3 Mirror(Vector3 local)
        {
            return new Vector3(-local.x, local.y, local.z);
        }

        private struct GrabStyle
        {
            public bool RightHand;      // the left hand is the front hand in a regular stance
            public Vector3 GrabPoint;   // board space, metres
            public Vector3 BoardTweak;  // pitch, yaw, roll in degrees
            public Vector3 BoardOffset; // rig space, metres
            public Vector3 FreeHand;    // upper-body space target for the free hand, right side (mirrored for the left)
            public float Hunch;
            public float Nod;           // look down at the grab
        }

        private static GrabStyle GetGrabStyle(GrabType grab)
        {
            switch (grab)
            {
                case GrabType.Nose:
                    // Front hand on the nose, poked up into view
                    return new GrabStyle
                    {
                        RightHand = false,
                        GrabPoint = new Vector3(0f, 0.03f, 0.68f),
                        BoardTweak = new Vector3(-26f, 0f, 0f),
                        BoardOffset = new Vector3(0f, 0.04f, 0.12f),
                        FreeHand = new Vector3(0.45f, 0.62f, 0.3f),
                        Hunch = 34f,
                        Nod = 26f
                    };
                case GrabType.Indy:
                    // Rear hand on the toe edge between the bindings
                    return new GrabStyle
                    {
                        RightHand = true,
                        GrabPoint = new Vector3(0.15f, 0f, 0.12f),
                        BoardTweak = new Vector3(0f, -8f, 18f),
                        BoardOffset = new Vector3(0.03f, 0.09f, 0.16f),
                        FreeHand = new Vector3(0.45f, 0.62f, 0.3f),
                        Hunch = 38f,
                        Nod = 50f
                    };
                case GrabType.Melon:
                    // Front hand on the heel edge between the bindings
                    return new GrabStyle
                    {
                        RightHand = false,
                        GrabPoint = new Vector3(-0.15f, 0f, 0.12f),
                        BoardTweak = new Vector3(0f, 8f, -18f),
                        BoardOffset = new Vector3(-0.03f, 0.09f, 0.16f),
                        FreeHand = new Vector3(0.45f, 0.62f, 0.3f),
                        Hunch = 38f,
                        Nod = 50f
                    };
                case GrabType.Stalefish:
                    // Rear hand behind the back leg onto the heel edge, tail kicked up and round
                    return new GrabStyle
                    {
                        RightHand = true,
                        GrabPoint = new Vector3(-0.13f, 0f, -0.28f),
                        BoardTweak = new Vector3(25f, -30f, -10f),
                        BoardOffset = new Vector3(0.05f, 0.22f, -0.02f),
                        FreeHand = new Vector3(0.45f, 0.62f, 0.3f),
                        Hunch = 15f,
                        Nod = 15f
                    };
                default:
                    return default;
            }
        }
    }
}
