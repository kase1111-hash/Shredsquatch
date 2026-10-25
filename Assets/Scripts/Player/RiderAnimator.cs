using System.Collections.Generic;
using UnityEngine;
using Shredsquatch.Core;
using Shredsquatch.Procedural;
using Shredsquatch.Tricks;

namespace Shredsquatch.Player
{
    /// <summary>
    /// Procedurally animates the rider. Builds a simple body (legs, boots, arms and gloves, plus a
    /// shadow-only torso and head so they never block the first-person view) around the board and
    /// camera, then poses it every frame from the physics, jump, trick, rail and crash state via
    /// RiderPoseSolver. Owns the board's pose: edge lean, ollie, grab tweaks, skids and slides.
    /// </summary>
    /// <remarks>
    /// Hierarchy built at runtime:
    /// Player > RiderRig (hips: flips, wipeouts, balance) > CameraMount > camera holder,
    /// plus the board and body parts under RiderRig.
    /// </remarks>
    public class RiderAnimator : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Board model. Found by name (\"Snowboard\") or created when empty.")]
        [SerializeField] private Transform _board;
        [Tooltip("Transform the FirstPersonCamera rotates. Found automatically when empty.")]
        [SerializeField] private Transform _cameraHolder;

        [Header("Outfit")]
        [SerializeField] private Color _jacketColor = new Color(0.95f, 0.4f, 0.1f);
        [SerializeField] private Color _pantsColor = new Color(0.12f, 0.15f, 0.25f);
        [SerializeField] private Color _gloveColor = new Color(0.08f, 0.08f, 0.1f);
        [SerializeField] private Color _bootColor = new Color(0.2f, 0.16f, 0.12f);
        [SerializeField] private Color _helmetColor = new Color(0.92f, 0.92f, 0.96f);

        private const float TorsoThickness = 0.34f;
        private const float HeadSize = 0.25f;
        private const float UpperArmThickness = 0.11f;
        private const float ForearmThickness = 0.09f;
        private const float GloveSize = 0.11f;
        private const float ThighThickness = 0.16f;
        private const float ShinThickness = 0.13f;
        private static readonly Vector3 BootSize = new Vector3(0.12f, 0.11f, 0.28f);

        // Gameplay state
        private SnowboardPhysics _physics;
        private PlayerInput _input;
        private JumpController _jump;
        private TrickController _tricks;
        private RailGrindController _rails;
        private CrashHandler _crash;

        private RiderPoseSolver _solver;
        private readonly List<Material> _materials = new List<Material>();

        // Rig
        private Transform _rig;
        private Transform _cameraMount;
        private Transform _torso;
        private Transform _head;
        private Transform _upperArmLeft, _forearmLeft, _gloveLeft;
        private Transform _upperArmRight, _forearmRight, _gloveRight;
        private Transform _thighLeft, _shinLeft, _bootLeft;
        private Transform _thighRight, _shinRight, _bootRight;

        private void Awake()
        {
            _physics = GetComponent<SnowboardPhysics>();
            _input = GetComponent<PlayerInput>();
            _jump = GetComponent<JumpController>();
            _tricks = GetComponent<TrickController>();
            _rails = GetComponent<RailGrindController>();
            _crash = GetComponent<CrashHandler>();

            BuildRig();
            ApplyPose(_solver.Pose);
        }

        private void Start()
        {
            if (_jump != null)
            {
                _jump.OnJump += HandleJump;
                _jump.OnLand += HandleLand;
            }

            if (_physics != null)
            {
                _physics.OnHeadingCorrected += HandleHeadingCorrected;
                _physics.OnEdgeCatch += HandleEdgeCatch;
            }
        }

        private void OnDestroy()
        {
            if (_jump != null)
            {
                _jump.OnJump -= HandleJump;
                _jump.OnLand -= HandleLand;
            }

            if (_physics != null)
            {
                _physics.OnHeadingCorrected -= HandleHeadingCorrected;
                _physics.OnEdgeCatch -= HandleEdgeCatch;
            }

            foreach (var material in _materials)
            {
                if (material != null) Destroy(material);
            }
            _materials.Clear();
        }

        private void LateUpdate()
        {
            ApplyPose(_solver.Step(GatherInput(), Time.deltaTime));
        }

        /// <summary>
        /// Stand the rider up and cancel any animation in progress (run start).
        /// </summary>
        public void ResetPose()
        {
            if (_solver == null) return;
            _solver.Reset();
            ApplyPose(_solver.Pose);
        }

        private void HandleJump(float estimatedAirtime) => _solver.Jump();
        private void HandleLand(float airTime, bool wasClean) => _solver.Land(airTime);
        private void HandleHeadingCorrected(float deltaDegrees) => _solver.CorrectHeading(deltaDegrees);
        private void HandleEdgeCatch() => _solver.EdgeCatch();

        private RiderPoseInput GatherInput()
        {
            var state = new RiderPoseInput { Grounded = true };

            if (_crash != null)
            {
                state.Crashed = _crash.IsInRagdoll;
                state.Recovering = _crash.IsRecovering;
            }

            // Outside a run (menus, the start countdown, game over) the gameplay systems stop
            // updating, so just stand there rather than freeze mid-move
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            {
                return state;
            }

            if (_physics != null)
            {
                state.Grounded = _physics.IsGrounded;
                state.SpeedKmh = _physics.CurrentSpeedKmh;
                state.Lean = _physics.LeanAngle;
            }

            if (_input != null)
            {
                state.Steer = _input.SteerInput;
                state.Tucking = _input.IsTucking;
                state.Braking = _input.IsBraking;

                // Spins only turn the rider after a launch, not when rolling off an edge
                if (_tricks != null && _tricks.IsAirborne) state.Spin = _input.SpinInput;
            }

            if (_jump != null)
            {
                state.Charging = _jump.IsCharging;
                state.Charge = _jump.ChargeProgress;
            }

            if (_tricks != null)
            {
                state.Grab = _tricks.HeldGrab;
                state.Flipping = _tricks.IsFlipping;
                state.FlipAngle = _tricks.FlipRotation;
            }

            // Boxes are ridden on their deck; rails are triggers the rider passes through at snow
            // level, so a rail grind only shows while the board is down (not if it rolls off a crest)
            if (_rails != null && _rails.IsGrinding && (_rails.IsBoxGrinding || state.Grounded))
            {
                state.Grinding = true;
                state.OnBox = _rails.IsBoxGrinding;
                state.Balance = _rails.Balance;
                state.Grab = _input != null ? (GrabType)_input.GrabInput : GrabType.None;
            }

            return state;
        }

        private void BuildRig()
        {
            if (_cameraHolder == null)
            {
                var firstPersonCamera = GetComponentInChildren<FirstPersonCamera>(true);
                if (firstPersonCamera != null)
                {
                    _cameraHolder = firstPersonCamera.transform;
                }
                else
                {
                    var anyCamera = GetComponentInChildren<Camera>(true);
                    if (anyCamera != null) _cameraHolder = anyCamera.transform;
                }
            }

            if (_board == null) _board = transform.Find("Snowboard");
            if (_board == null) _board = CreateBoard();

            // Fit the body to wherever the scene placed the eyes and the board
            float eyeHeight = _cameraHolder != null
                ? transform.InverseTransformPoint(_cameraHolder.position).y
                : 1.6f;
            eyeHeight = Mathf.Max(eyeHeight, RiderPoseSolver.HipHeight + RiderPoseSolver.NeckHeight);
            float boardHeight = transform.InverseTransformPoint(_board.position).y;

            // Board footprint, so a tipped board can be rested on a box deck by its edge
            Vector3 boardHalfSize = 0.5f * _board.localScale;
            var boardMesh = _board.GetComponent<MeshFilter>();
            if (boardMesh != null && boardMesh.sharedMesh != null)
            {
                boardHalfSize = Vector3.Scale(boardMesh.sharedMesh.bounds.extents, _board.localScale);
            }

            _solver = new RiderPoseSolver(eyeHeight, boardHeight, boardHalfSize.z, boardHalfSize.x);

            _rig = new GameObject("RiderRig").transform;
            _rig.gameObject.layer = gameObject.layer;
            _rig.SetParent(transform, false);

            _cameraMount = new GameObject("CameraMount").transform;
            _cameraMount.gameObject.layer = gameObject.layer;
            _cameraMount.SetParent(_rig, false);

            if (_cameraHolder != null)
            {
                _cameraHolder.SetParent(_cameraMount, false);
                _cameraHolder.localPosition = Vector3.zero;
            }

            _board.SetParent(_rig, false);

            // Match the board's shader so the body renders with whatever pipeline the scene uses
            Material template = null;
            var boardRenderer = _board.GetComponent<Renderer>();
            if (boardRenderer != null) template = boardRenderer.sharedMaterial;

            Material jacket = TrackMaterial(ProceduralRig.CreateMaterial(template, _jacketColor));
            Material pants = TrackMaterial(ProceduralRig.CreateMaterial(template, _pantsColor));
            Material gloves = TrackMaterial(ProceduralRig.CreateMaterial(template, _gloveColor));
            Material boots = TrackMaterial(ProceduralRig.CreateMaterial(template, _bootColor));
            Material helmet = TrackMaterial(ProceduralRig.CreateMaterial(template, _helmetColor));

            // The camera sits inside the head and just above the chest: those only cast shadows
            _torso = ProceduralRig.CreatePart("Torso", PrimitiveType.Capsule, _rig, jacket, false);
            _head = ProceduralRig.CreatePart("Head", PrimitiveType.Sphere, _rig, helmet, false);
            _head.localScale = Vector3.one * HeadSize;

            _upperArmLeft = ProceduralRig.CreatePart("UpperArm_L", PrimitiveType.Capsule, _rig, jacket);
            _forearmLeft = ProceduralRig.CreatePart("Forearm_L", PrimitiveType.Capsule, _rig, jacket);
            _gloveLeft = ProceduralRig.CreatePart("Glove_L", PrimitiveType.Sphere, _rig, gloves);
            _upperArmRight = ProceduralRig.CreatePart("UpperArm_R", PrimitiveType.Capsule, _rig, jacket);
            _forearmRight = ProceduralRig.CreatePart("Forearm_R", PrimitiveType.Capsule, _rig, jacket);
            _gloveRight = ProceduralRig.CreatePart("Glove_R", PrimitiveType.Sphere, _rig, gloves);
            _gloveLeft.localScale = _gloveRight.localScale = Vector3.one * GloveSize;

            _thighLeft = ProceduralRig.CreatePart("Thigh_L", PrimitiveType.Capsule, _rig, pants);
            _shinLeft = ProceduralRig.CreatePart("Shin_L", PrimitiveType.Capsule, _rig, pants);
            _bootLeft = ProceduralRig.CreatePart("Boot_L", PrimitiveType.Cube, _rig, boots);
            _thighRight = ProceduralRig.CreatePart("Thigh_R", PrimitiveType.Capsule, _rig, pants);
            _shinRight = ProceduralRig.CreatePart("Shin_R", PrimitiveType.Capsule, _rig, pants);
            _bootRight = ProceduralRig.CreatePart("Boot_R", PrimitiveType.Cube, _rig, boots);
            _bootLeft.localScale = _bootRight.localScale = BootSize;
        }

        private Transform CreateBoard()
        {
            Material material = TrackMaterial(ProceduralRig.CreateMaterial(null, new Color(0.1f, 0.1f, 0.15f)));
            Transform board = ProceduralRig.CreatePart("Snowboard", PrimitiveType.Cube, transform, material);
            board.localPosition = new Vector3(0f, 0.05f, 0f);
            board.localScale = new Vector3(0.3f, 0.02f, 1.5f);
            return board;
        }

        private Material TrackMaterial(Material material)
        {
            _materials.Add(material);
            return material;
        }

        private void ApplyPose(RiderPose pose)
        {
            _rig.localPosition = pose.RigPosition;
            _rig.localRotation = pose.RigRotation;

            _cameraMount.localPosition = pose.CameraPosition;
            _cameraMount.localRotation = pose.CameraRotation;

            _board.localPosition = pose.BoardPosition;
            _board.localRotation = pose.BoardRotation;

            ProceduralRig.PlaceCapsule(_torso, pose.Pelvis, pose.Neck, TorsoThickness);
            _head.localPosition = pose.Head;

            ProceduralRig.PlaceCapsule(_upperArmLeft, pose.ShoulderLeft, pose.ElbowLeft, UpperArmThickness);
            ProceduralRig.PlaceCapsule(_forearmLeft, pose.ElbowLeft, pose.HandLeft, ForearmThickness);
            _gloveLeft.localPosition = pose.HandLeft;
            ProceduralRig.PlaceCapsule(_upperArmRight, pose.ShoulderRight, pose.ElbowRight, UpperArmThickness);
            ProceduralRig.PlaceCapsule(_forearmRight, pose.ElbowRight, pose.HandRight, ForearmThickness);
            _gloveRight.localPosition = pose.HandRight;

            ProceduralRig.PlaceCapsule(_thighLeft, pose.HipLeft, pose.KneeLeft, ThighThickness);
            ProceduralRig.PlaceCapsule(_shinLeft, pose.KneeLeft, pose.AnkleLeft, ShinThickness);
            PlaceBoot(_bootLeft, pose.AnkleLeft, pose.BoardRotation);
            ProceduralRig.PlaceCapsule(_thighRight, pose.HipRight, pose.KneeRight, ThighThickness);
            ProceduralRig.PlaceCapsule(_shinRight, pose.KneeRight, pose.AnkleRight, ShinThickness);
            PlaceBoot(_bootRight, pose.AnkleRight, pose.BoardRotation);
        }

        private static void PlaceBoot(Transform boot, Vector3 ankle, Quaternion boardRotation)
        {
            // Strapped flat to the board, sole on its top surface, toes a little ahead of the ankle
            boot.localRotation = boardRotation;
            boot.localPosition = ankle + boardRotation * new Vector3(0f, -0.04f, 0.03f);
        }
    }
}
