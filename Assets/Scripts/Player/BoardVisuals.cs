using UnityEngine;

namespace Shredsquatch.Player
{
    /// <summary>
    /// Board trails, snow spray and board skins. The board's pose (edge lean, tweaks) is
    /// animated by RiderAnimator.
    /// </summary>
    public class BoardVisuals : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _boardMesh;
        [SerializeField] private TrailRenderer _leftTrail;
        [SerializeField] private TrailRenderer _rightTrail;
        [SerializeField] private ParticleSystem _snowSpray;
        [SerializeField] private ParticleSystem _powderBurst;

        [Header("Trail Settings")]
        [SerializeField] private float _trailMinSpeed = 20f;
        [SerializeField] private Gradient _normalTrailColor;
        [SerializeField] private Gradient _fireTrailColor;
        [SerializeField] private Gradient _rainbowTrailColor;
        [SerializeField] private Gradient _lightningTrailColor;

        private SnowboardPhysics _physics;
        private TrailType _currentTrailType = TrailType.Normal;

        public enum TrailType
        {
            Normal,
            Fire,
            Rainbow,
            Lightning
        }

        private void Awake()
        {
            _physics = GetComponentInParent<SnowboardPhysics>();
        }

        private void Update()
        {
            UpdateTrails();
            UpdateSnowSpray();
        }

        private void UpdateTrails()
        {
            if (_physics == null) return;

            bool shouldEmit = _physics.IsGrounded && _physics.CurrentSpeedKmh > _trailMinSpeed;

            if (_leftTrail != null)
            {
                _leftTrail.emitting = shouldEmit;
            }

            if (_rightTrail != null)
            {
                _rightTrail.emitting = shouldEmit;
            }
        }

        private void UpdateSnowSpray()
        {
            if (_physics == null || _snowSpray == null) return;

            var emission = _snowSpray.emission;

            if (_physics.IsGrounded && _physics.CurrentSpeedKmh > 10f)
            {
                emission.enabled = true;

                // Increase particles with speed
                float speedRatio = _physics.CurrentSpeedKmh / 120f;
                emission.rateOverTime = Mathf.Lerp(10f, 100f, speedRatio);

                // Increase with lean angle
                float leanRatio = Mathf.Abs(_physics.LeanAngle) / 45f;
                emission.rateOverTime = emission.rateOverTime.constant * (1f + leanRatio);
            }
            else
            {
                emission.enabled = false;
            }
        }

        public void TriggerPowderBurst()
        {
            if (_powderBurst != null)
            {
                _powderBurst.Play();
            }
        }

        public void SetTrailType(TrailType type)
        {
            _currentTrailType = type;

            Gradient gradient = type switch
            {
                TrailType.Fire => _fireTrailColor,
                TrailType.Rainbow => _rainbowTrailColor,
                TrailType.Lightning => _lightningTrailColor,
                _ => _normalTrailColor
            };

            if (_leftTrail != null)
            {
                _leftTrail.colorGradient = gradient;
            }

            if (_rightTrail != null)
            {
                _rightTrail.colorGradient = gradient;
            }
        }

        public void SetGoldenBoard(bool enabled)
        {
            // Would swap materials here
            if (_boardMesh != null)
            {
                var renderer = _boardMesh.GetComponent<Renderer>();
                if (renderer != null)
                {
                    // Toggle golden material
                    // renderer.material = enabled ? _goldenMaterial : _normalMaterial;
                }
            }
        }
    }
}
