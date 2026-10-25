using UnityEngine;
using Shredsquatch.Procedural;

namespace Shredsquatch.Sasquatch
{
    /// <summary>
    /// A Sasquatch pose in model space: metres, origin on the ground under the hips, +Z forward.
    /// </summary>
    public struct SasquatchPose
    {
        public Vector3 Pelvis;
        public Vector3 Chest;
        public Quaternion BodyRotation;
        public Vector3 Head;
        public Quaternion HeadRotation;

        public Vector3 HipLeft, KneeLeft, FootLeft;
        public Vector3 HipRight, KneeRight, FootRight;
        public Vector3 ShoulderLeft, ElbowLeft, HandLeft;
        public Vector3 ShoulderRight, ElbowRight, HandRight;
    }

    /// <summary>
    /// Procedural gait for the Sasquatch: a loping, arm-swinging bound that quickens, lengthens
    /// and leans forward with speed, plus one-shot roar and lunge actions and a dazed stagger
    /// while repelled. Pure math with no scene access, so it can be unit tested;
    /// SasquatchAnimator applies the result to the model.
    /// </summary>
    public class SasquatchGait
    {
        // Proportions in metres for a ~3.2 m tall Sasquatch
        public const float HipHeight = 1.35f;
        public const float HipHalfWidth = 0.3f;
        public const float ThighLength = 0.75f;
        public const float ShinLength = 0.72f;
        public const float AnkleHeight = 0.12f;
        public const float SpineLength = 1.05f;
        public const float ShoulderHalfWidth = 0.55f;
        public const float UpperArmLength = 0.78f;
        public const float ForearmLength = 0.78f;

        // Ground speed (m/s) at which it is in a full bound
        public const float FullBoundSpeed = 14f;

        private const float MaxDeltaTime = 0.05f;
        private const float RoarDuration = 1.6f;
        private const float LungeDuration = 1.1f;
        private const float RoarAfterLunge = 0.8f;
        private const float TwoPi = Mathf.PI * 2f;

        private float _phase;           // stride cycle, radians
        private float _run;             // 0 standing .. 1 full bound
        private float _repel;
        private float _roarTime = -1f;
        private float _lungeTime = -1f;
        private float _time;

        public SasquatchPose Pose { get; private set; }

        public SasquatchGait()
        {
            Pose = BuildPose(0f, 0f);
        }

        /// <summary>Rear up and roar (spawning, and after catching the rider).</summary>
        public void Roar()
        {
            _roarTime = 0f;
        }

        /// <summary>Lunge at the rider with both arms, then roar.</summary>
        public void Lunge()
        {
            _lungeTime = 0f;
        }

        /// <summary>Back to standing still with no action playing.</summary>
        public void Reset()
        {
            _phase = 0f;
            _run = 0f;
            _repel = 0f;
            _roarTime = -1f;
            _lungeTime = -1f;
            Pose = BuildPose(0f, 0f);
        }

        /// <param name="speed">Ground speed in m/s.</param>
        public SasquatchPose Step(float speed, bool repelled, float deltaTime)
        {
            float dt = Mathf.Clamp(deltaTime, 0f, MaxDeltaTime);
            _time += dt;

            _run = ProceduralAnimation.Damp(_run, Mathf.Clamp01(speed / FullBoundSpeed), 4f, dt);
            _repel = ProceduralAnimation.Damp(_repel, repelled ? 1f : 0f, 5f, dt);

            // Strides come faster as it speeds up; standing still the cycle stops where it is
            float strideRate = speed > 0.3f ? Mathf.Lerp(0.9f, 2.4f, _run) : 0f;
            _phase = Mathf.Repeat(_phase + strideRate * TwoPi * dt, TwoPi);

            float roar = 0f;
            if (_roarTime >= 0f)
            {
                _roarTime += dt;
                roar = ProceduralAnimation.Envelope(_roarTime, 0.2f, 1.25f, RoarDuration);
                if (_roarTime >= RoarDuration) _roarTime = -1f;
            }

            float lunge = 0f;
            if (_lungeTime >= 0f)
            {
                float before = _lungeTime;
                _lungeTime += dt;
                lunge = ProceduralAnimation.Envelope(_lungeTime, 0.12f, 0.7f, LungeDuration);
                if (before < RoarAfterLunge && _lungeTime >= RoarAfterLunge) Roar();
                if (_lungeTime >= LungeDuration) _lungeTime = -1f;
            }

            Pose = BuildPose(roar, lunge);
            return Pose;
        }

        private SasquatchPose BuildPose(float roar, float lunge)
        {
            var pose = new SasquatchPose();

            float stride = 0.7f * _run * (1f - 0.5f * _repel);
            float lift = Mathf.Lerp(0.05f, 0.45f, _run);
            float bob = Mathf.Lerp(0.02f, 0.2f, _run);
            float cos = Mathf.Cos(_phase);
            float sin = Mathf.Sin(_phase);

            // Lowest at mid-stance, highest as it pushes off; crouches into the bound at speed
            float breathe = 0.02f * Mathf.Sin(_time * 1.7f) * (1f - _run);
            pose.Pelvis = new Vector3(
                0.06f * _run * sin,
                HipHeight - 0.12f * _run - bob * (1f - Mathf.Abs(cos)) + breathe,
                0f);

            // Hunched forward into the chase, rears back to roar, dives in to lunge
            float lean = Mathf.Lerp(8f, 26f, _run) + 6f * _repel;
            lean = Mathf.Lerp(lean, -14f, roar);
            lean = Mathf.Lerp(lean, 38f, lunge);
            float sway = 4f * _run * sin;
            pose.BodyRotation = Quaternion.Euler(lean, 0f, sway);

            pose.Chest = pose.Pelvis + pose.BodyRotation * new Vector3(0f, SpineLength, 0f);
            pose.Head = pose.Pelvis + pose.BodyRotation * new Vector3(0f, SpineLength + 0.35f, 0.2f);

            // Head keeps looking ahead, throws back to roar, shakes off the repellent
            float headPitch = -0.6f * lean - 28f * roar;
            float headYaw = 25f * _repel * Mathf.Sin(_time * 9f);
            pose.HeadRotation = pose.BodyRotation * Quaternion.Euler(headPitch, headYaw, 0f);

            // Legs: feet trace a stride loop, planted while moving back, lifted swinging forward
            pose.HipLeft = pose.Pelvis + pose.BodyRotation * new Vector3(-HipHalfWidth, -0.05f, 0f);
            pose.HipRight = pose.Pelvis + pose.BodyRotation * new Vector3(HipHalfWidth, -0.05f, 0f);
            Vector3 footLeft = FootTarget(cos, sin, -1f, stride, lift);
            Vector3 footRight = FootTarget(-cos, -sin, 1f, stride, lift);
            pose.KneeLeft = ProceduralAnimation.SolveTwoBone(pose.HipLeft, footLeft, ThighLength, ShinLength,
                Vector3.forward, out pose.FootLeft);
            pose.KneeRight = ProceduralAnimation.SolveTwoBone(pose.HipRight, footRight, ThighLength, ShinLength,
                Vector3.forward, out pose.FootRight);

            // Long arms swing against the legs, swat at the head when repelled, go up to roar,
            // reach out to lunge
            pose.ShoulderLeft = pose.Pelvis + pose.BodyRotation * new Vector3(-ShoulderHalfWidth, SpineLength - 0.08f, 0f);
            pose.ShoulderRight = pose.Pelvis + pose.BodyRotation * new Vector3(ShoulderHalfWidth, SpineLength - 0.08f, 0f);
            Vector3 handLeft = ArmTarget(-1f, -cos, stride, roar, lunge);
            Vector3 handRight = ArmTarget(1f, cos, stride, roar, lunge);
            pose.ElbowLeft = ProceduralAnimation.SolveTwoBone(pose.ShoulderLeft,
                pose.ShoulderLeft + pose.BodyRotation * handLeft, UpperArmLength, ForearmLength,
                pose.BodyRotation * new Vector3(-0.6f, -0.2f, -1f), out pose.HandLeft);
            pose.ElbowRight = ProceduralAnimation.SolveTwoBone(pose.ShoulderRight,
                pose.ShoulderRight + pose.BodyRotation * handRight, UpperArmLength, ForearmLength,
                pose.BodyRotation * new Vector3(0.6f, -0.2f, -1f), out pose.HandRight);

            return pose;
        }

        private static Vector3 FootTarget(float cos, float sin, float side, float stride, float lift)
        {
            // Phase 0..pi: on the ground sweeping back. Phase pi..2pi: lifted, swinging forward.
            float height = sin < 0f ? -sin * lift : 0f;
            return new Vector3(side * HipHalfWidth * 0.9f, AnkleHeight + height, stride * cos);
        }

        /// <summary>Hand target relative to its shoulder, in body space.</summary>
        private Vector3 ArmTarget(float side, float swing, float stride, float roar, float lunge)
        {
            float reach = 1.4f * stride * swing;
            Vector3 hand = new Vector3(side * 0.18f, -1.2f + 0.3f * Mathf.Max(0f, swing) * _run, 0.2f + reach);

            Vector3 swat = new Vector3(-side * 0.15f + 0.08f * Mathf.Sin(_time * 13f + side),
                0.45f, 0.5f + 0.1f * Mathf.Sin(_time * 11f));
            hand = Vector3.Lerp(hand, swat, _repel);

            hand = Vector3.Lerp(hand, new Vector3(side * 0.55f, 0.65f, 0.35f), roar);
            hand = Vector3.Lerp(hand, new Vector3(-side * 0.15f, -0.1f, 1.3f), lunge);
            return hand;
        }
    }
}
