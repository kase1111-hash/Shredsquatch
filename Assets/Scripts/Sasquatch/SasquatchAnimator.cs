using UnityEngine;
using Shredsquatch.Procedural;

namespace Shredsquatch.Sasquatch
{
    /// <summary>
    /// Procedurally animates the Sasquatch. Replaces the placeholder capsule with a furry body
    /// (legs, long arms, head with glowing eyes) and drives it with SasquatchGait: a loping bound
    /// matched to how fast it is really covering ground, a roar when it appears, a dazed swat
    /// while repelled and a lunge when it catches the rider.
    /// </summary>
    public class SasquatchAnimator : MonoBehaviour
    {
        [Header("Look")]
        [SerializeField] private Color _furColor = new Color(0.3f, 0.2f, 0.1f);
        [SerializeField] private Color _eyeColor = new Color(1f, 0.15f, 0.05f);
        [SerializeField] private float _eyeGlow = 3f;

        private const float MaxMeasuredSpeed = 40f;   // m/s; ignores teleports
        private const float TorsoWidth = 1.0f;
        private const float TorsoDepth = 0.75f;
        private static readonly Vector3 HeadSize = new Vector3(0.62f, 0.6f, 0.66f);
        private const float EyeSize = 0.09f;
        private const float UpperArmThickness = 0.3f;
        private const float ForearmThickness = 0.26f;
        private const float HandSize = 0.34f;
        private const float ThighThickness = 0.4f;
        private const float ShinThickness = 0.32f;
        private static readonly Vector3 FootSize = new Vector3(0.32f, 0.16f, 0.56f);

        private SasquatchAI _ai;
        private readonly SasquatchGait _gait = new SasquatchGait();

        private Material _furMaterial;
        private bool _ownsFurMaterial;
        private Material _eyeMaterial;

        private Vector3 _lastPosition;
        private bool _hasLastPosition;
        private float _speed;

        private Transform _model;
        private Transform _torso, _head, _eyeLeft, _eyeRight;
        private Transform _upperArmLeft, _forearmLeft, _handLeft;
        private Transform _upperArmRight, _forearmRight, _handRight;
        private Transform _thighLeft, _shinLeft, _footLeft;
        private Transform _thighRight, _shinRight, _footRight;

        private void Awake()
        {
            _ai = GetComponent<SasquatchAI>();
            BuildModel();
            ApplyPose(_gait.Pose);

            // Subscribed for the object's lifetime: the AI keeps it inactive until it spawns
            if (_ai != null)
            {
                _ai.OnSpawn += HandleSpawn;
                _ai.OnCatchPlayer += HandleCatch;
            }
        }

        private void OnDestroy()
        {
            if (_ai != null)
            {
                _ai.OnSpawn -= HandleSpawn;
                _ai.OnCatchPlayer -= HandleCatch;
            }

            if (_ownsFurMaterial && _furMaterial != null) Destroy(_furMaterial);
            if (_eyeMaterial != null) Destroy(_eyeMaterial);
        }

        private void OnEnable()
        {
            _hasLastPosition = false;
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            MeasureSpeed(dt);

            bool repelled = _ai != null && _ai.IsRepelled;
            ApplyPose(_gait.Step(_speed, repelled, dt));
        }

        private void HandleSpawn()
        {
            // It has just been placed far up the slope; don't read that as speed
            _hasLastPosition = false;
            _speed = 0f;
            _gait.Reset();
            _gait.Roar();
        }

        private void HandleCatch()
        {
            _gait.Lunge();
        }

        /// <summary>
        /// Gait follows how fast it actually covers ground, whether moved by NavMesh or directly.
        /// </summary>
        private void MeasureSpeed(float dt)
        {
            Vector3 position = transform.position;

            if (_hasLastPosition && dt > 0f)
            {
                Vector3 moved = position - _lastPosition;
                moved.y = 0f;
                float measured = moved.magnitude / dt;
                if (measured <= MaxMeasuredSpeed)
                {
                    _speed = ProceduralAnimation.Damp(_speed, measured, 6f, dt);
                }
            }

            _lastPosition = position;
            _hasLastPosition = true;
        }

        private void BuildModel()
        {
            // The placeholder mesh (the prefab's "Body", or the procedural template's own capsule)
            // gives way to the animated body but lends it its fur
            Renderer placeholder = null;
            Transform body = transform.Find("Body");
            if (body != null) placeholder = body.GetComponent<Renderer>();
            if (placeholder == null) placeholder = GetComponent<Renderer>();

            if (placeholder != null)
            {
                _furMaterial = placeholder.sharedMaterial;
                placeholder.enabled = false;
            }

            if (_furMaterial == null)
            {
                _furMaterial = ProceduralRig.CreateMaterial(null, _furColor);
                _ownsFurMaterial = true;
            }

            _eyeMaterial = ProceduralRig.CreateMaterial(_furMaterial, _eyeColor);
            ProceduralRig.SetEmission(_eyeMaterial, _eyeColor * _eyeGlow);

            // Cancel the root's scale so the model is built in metres and rotating limbs never shear
            _model = new GameObject("SasquatchModel").transform;
            _model.gameObject.layer = gameObject.layer;
            _model.SetParent(transform, false);
            Vector3 scale = transform.lossyScale;
            _model.localScale = new Vector3(1f / SafeScale(scale.x), 1f / SafeScale(scale.y), 1f / SafeScale(scale.z));

            _torso = ProceduralRig.CreatePart("Torso", PrimitiveType.Capsule, _model, _furMaterial);
            _head = ProceduralRig.CreatePart("Head", PrimitiveType.Sphere, _model, _furMaterial);
            _head.localScale = HeadSize;
            _eyeLeft = ProceduralRig.CreatePart("Eye_L", PrimitiveType.Sphere, _model, _eyeMaterial);
            _eyeRight = ProceduralRig.CreatePart("Eye_R", PrimitiveType.Sphere, _model, _eyeMaterial);
            _eyeLeft.localScale = _eyeRight.localScale = Vector3.one * EyeSize;

            _upperArmLeft = ProceduralRig.CreatePart("UpperArm_L", PrimitiveType.Capsule, _model, _furMaterial);
            _forearmLeft = ProceduralRig.CreatePart("Forearm_L", PrimitiveType.Capsule, _model, _furMaterial);
            _handLeft = ProceduralRig.CreatePart("Hand_L", PrimitiveType.Sphere, _model, _furMaterial);
            _upperArmRight = ProceduralRig.CreatePart("UpperArm_R", PrimitiveType.Capsule, _model, _furMaterial);
            _forearmRight = ProceduralRig.CreatePart("Forearm_R", PrimitiveType.Capsule, _model, _furMaterial);
            _handRight = ProceduralRig.CreatePart("Hand_R", PrimitiveType.Sphere, _model, _furMaterial);
            _handLeft.localScale = _handRight.localScale = Vector3.one * HandSize;

            _thighLeft = ProceduralRig.CreatePart("Thigh_L", PrimitiveType.Capsule, _model, _furMaterial);
            _shinLeft = ProceduralRig.CreatePart("Shin_L", PrimitiveType.Capsule, _model, _furMaterial);
            _footLeft = ProceduralRig.CreatePart("Foot_L", PrimitiveType.Cube, _model, _furMaterial);
            _thighRight = ProceduralRig.CreatePart("Thigh_R", PrimitiveType.Capsule, _model, _furMaterial);
            _shinRight = ProceduralRig.CreatePart("Shin_R", PrimitiveType.Capsule, _model, _furMaterial);
            _footRight = ProceduralRig.CreatePart("Foot_R", PrimitiveType.Cube, _model, _furMaterial);
            _footLeft.localScale = _footRight.localScale = FootSize;
        }

        private static float SafeScale(float value)
        {
            return Mathf.Abs(value) > 0.0001f ? value : 1f;
        }

        private void ApplyPose(SasquatchPose pose)
        {
            ProceduralRig.PlaceCapsule(_torso, pose.Pelvis, pose.Chest, TorsoWidth, TorsoDepth);

            _head.localPosition = pose.Head;
            _head.localRotation = pose.HeadRotation;
            _eyeLeft.localPosition = pose.Head + pose.HeadRotation * new Vector3(-0.13f, 0.06f, 0.29f);
            _eyeRight.localPosition = pose.Head + pose.HeadRotation * new Vector3(0.13f, 0.06f, 0.29f);

            ProceduralRig.PlaceCapsule(_upperArmLeft, pose.ShoulderLeft, pose.ElbowLeft, UpperArmThickness);
            ProceduralRig.PlaceCapsule(_forearmLeft, pose.ElbowLeft, pose.HandLeft, ForearmThickness);
            _handLeft.localPosition = pose.HandLeft;
            ProceduralRig.PlaceCapsule(_upperArmRight, pose.ShoulderRight, pose.ElbowRight, UpperArmThickness);
            ProceduralRig.PlaceCapsule(_forearmRight, pose.ElbowRight, pose.HandRight, ForearmThickness);
            _handRight.localPosition = pose.HandRight;

            ProceduralRig.PlaceCapsule(_thighLeft, pose.HipLeft, pose.KneeLeft, ThighThickness);
            ProceduralRig.PlaceCapsule(_shinLeft, pose.KneeLeft, pose.FootLeft, ShinThickness);
            _footLeft.localPosition = pose.FootLeft + new Vector3(0f, -0.05f, 0.12f);
            ProceduralRig.PlaceCapsule(_thighRight, pose.HipRight, pose.KneeRight, ThighThickness);
            ProceduralRig.PlaceCapsule(_shinRight, pose.KneeRight, pose.FootRight, ShinThickness);
            _footRight.localPosition = pose.FootRight + new Vector3(0f, -0.05f, 0.12f);
        }
    }
}
