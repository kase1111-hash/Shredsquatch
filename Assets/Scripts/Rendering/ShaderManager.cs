using UnityEngine;
using System.Collections.Generic;

namespace Shredsquatch.Rendering
{
    /// <summary>
    /// Manages custom shaders and materials at runtime.
    /// Handles dynamic shader property updates for weather, time of day, etc.
    /// </summary>
    public class ShaderManager : MonoBehaviour
    {
        public static ShaderManager Instance { get; private set; }

        [Header("Snow Settings")]
        [SerializeField] private Material _snowMaterial;
        [SerializeField] private float _sparkleIntensity = 1f;
        [SerializeField] private float _subsurfaceStrength = 0.3f;

        [Header("Sasquatch Settings")]
        [SerializeField] private Material _sasquatchMaterial;
        [SerializeField] private float _eyeGlowIntensity = 0f;
        [SerializeField] private Color _eyeGlowColor = new Color(1f, 0.2f, 0.1f);

        [Header("Weather Effects")]
        [Range(0f, 1f)]
        [SerializeField] private float _frostAmount = 0f;
        [SerializeField] private bool _isBlizzard = false;

        [Header("Trail Materials")]
        [SerializeField] private Material _trailFireMaterial;
        [SerializeField] private Material _trailRainbowMaterial;
        [SerializeField] private Material _trailLightningMaterial;

        [Header("Aurora")]
        [SerializeField] private Material _auroraMaterial;
        [SerializeField] private bool _auroraEnabled = false;

        // Shader property IDs (cached for performance)
        private static readonly int SparkleIntensityID = Shader.PropertyToID("_SparkleIntensity");
        private static readonly int SubsurfaceStrengthID = Shader.PropertyToID("_SubsurfaceStrength");
        private static readonly int EyeGlowIntensityID = Shader.PropertyToID("_EyeGlowIntensity");
        private static readonly int EyeGlowColorID = Shader.PropertyToID("_EyeGlowColor");
        private static readonly int FrostAmountID = Shader.PropertyToID("_FrostAmount");
        private static readonly int OverallAlphaID = Shader.PropertyToID("_OverallAlpha");

        private List<Material> _allManagedMaterials = new List<Material>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            CacheMaterials();
        }

        private void CacheMaterials()
        {
            if (_snowMaterial != null) _allManagedMaterials.Add(_snowMaterial);
            if (_sasquatchMaterial != null) _allManagedMaterials.Add(_sasquatchMaterial);
            if (_trailFireMaterial != null) _allManagedMaterials.Add(_trailFireMaterial);
            if (_trailRainbowMaterial != null) _allManagedMaterials.Add(_trailRainbowMaterial);
            if (_trailLightningMaterial != null) _allManagedMaterials.Add(_trailLightningMaterial);
            if (_auroraMaterial != null) _allManagedMaterials.Add(_auroraMaterial);
        }

        private void Update()
        {
            UpdateSnowShader();
            UpdateSasquatchShader();
            UpdateAuroraShader();
        }

        private void UpdateSnowShader()
        {
            if (_snowMaterial == null) return;

            _snowMaterial.SetFloat(SparkleIntensityID, _sparkleIntensity);
            _snowMaterial.SetFloat(SubsurfaceStrengthID, _subsurfaceStrength);
        }

        private void UpdateSasquatchShader()
        {
            if (_sasquatchMaterial == null) return;

            _sasquatchMaterial.SetFloat(EyeGlowIntensityID, _eyeGlowIntensity);
            _sasquatchMaterial.SetColor(EyeGlowColorID, _eyeGlowColor);
            _sasquatchMaterial.SetFloat(FrostAmountID, _frostAmount);
        }

        private void UpdateAuroraShader()
        {
            if (_auroraMaterial == null) return;

            float targetAlpha = _auroraEnabled ? 0.7f : 0f;
            float currentAlpha = _auroraMaterial.GetFloat(OverallAlphaID);
            float newAlpha = Mathf.Lerp(currentAlpha, targetAlpha, Time.deltaTime * 2f);
            _auroraMaterial.SetFloat(OverallAlphaID, newAlpha);
        }

        #region Public API

        /// <summary>
        /// Set the Sasquatch eye glow intensity (0 = off, 1+ = glowing).
        /// Call this when Sasquatch is hunting/close to player.
        /// </summary>
        public void SetSasquatchEyeGlow(float intensity)
        {
            _eyeGlowIntensity = intensity;
        }

        /// <summary>
        /// Set the frost amount on Sasquatch fur (for blizzard conditions).
        /// </summary>
        public void SetFrostAmount(float amount)
        {
            _frostAmount = Mathf.Clamp01(amount);
        }

        /// <summary>
        /// Enable/disable blizzard visual effects.
        /// </summary>
        public void SetBlizzardActive(bool active)
        {
            _isBlizzard = active;

            // Reduce sparkle during blizzard
            _sparkleIntensity = active ? 0.3f : 1f;

            // Add frost during blizzard
            if (active)
            {
                SetFrostAmount(0.5f);
            }
        }

        /// <summary>
        /// Enable/disable aurora borealis (for night mode).
        /// </summary>
        public void SetAuroraEnabled(bool enabled)
        {
            _auroraEnabled = enabled;
        }

        /// <summary>
        /// Get the material for a specific trail type.
        /// </summary>
        public Material GetTrailMaterial(TrailType type)
        {
            switch (type)
            {
                case TrailType.Fire:
                    return _trailFireMaterial;
                case TrailType.Rainbow:
                    return _trailRainbowMaterial;
                case TrailType.Lightning:
                    return _trailLightningMaterial;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Pulse Sasquatch eyes (call this when Sasquatch spots player).
        /// </summary>
        public void PulseSasquatchEyes(float duration = 0.5f, float intensity = 3f)
        {
            StartCoroutine(EyePulseCoroutine(duration, intensity));
        }

        private System.Collections.IEnumerator EyePulseCoroutine(float duration, float maxIntensity)
        {
            float elapsed = 0f;
            float startIntensity = _eyeGlowIntensity;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Pulse up then down
                float pulse = Mathf.Sin(t * Mathf.PI) * maxIntensity;
                _eyeGlowIntensity = startIntensity + pulse;

                yield return null;
            }

            _eyeGlowIntensity = startIntensity;
        }

        #endregion

        #region Time of Day

        /// <summary>
        /// Update shaders for time of day (affects lighting, aurora visibility).
        /// </summary>
        public void SetTimeOfDay(float normalizedTime)
        {
            // normalizedTime: 0 = midnight, 0.25 = 6am, 0.5 = noon, 0.75 = 6pm

            // Aurora visible at night (before 6am, after 8pm)
            bool isNight = normalizedTime < 0.25f || normalizedTime > 0.83f;
            SetAuroraEnabled(isNight);

            // Adjust snow sparkle based on sun angle
            // More sparkle around noon when sun is high
            float sunAngle = Mathf.Abs(normalizedTime - 0.5f) * 2f; // 0 at noon, 1 at midnight
            _sparkleIntensity = Mathf.Lerp(1.2f, 0.3f, sunAngle);

            // Sasquatch eyes glow more at night
            if (isNight)
            {
                _eyeGlowIntensity = Mathf.Max(_eyeGlowIntensity, 0.5f);
            }
        }

        #endregion
    }

    public enum TrailType
    {
        Default,
        Fire,
        Rainbow,
        Lightning
    }
}
