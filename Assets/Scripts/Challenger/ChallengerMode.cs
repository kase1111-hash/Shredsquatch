using UnityEngine;
using System;
using Shredsquatch.Core;
using Shredsquatch.Player;

namespace Shredsquatch.Challenger
{
    /// <summary>
    /// Main controller for the alternate challenger mode.
    /// Spawns challenger character on creature mount with trick survival gameplay.
    /// [PLACEHOLDER] - Character assets pending external permission.
    /// </summary>
    public class ChallengerMode : MonoBehaviour
    {
        public static ChallengerMode Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private ChallengerConfig _config;

        [Header("Scene References")]
        [SerializeField] private Transform _spawnPoint;

        // Runtime instances
        private GameObject _characterInstance;
        private CreatureMount _mountInstance;
        private TrickSurvival _survivalSystem;
        private bool _modeActive;

        // Events
        public event Action OnModeActivated;
        public event Action OnModeDeactivated;
        public event Action<float> OnSurvivalEnded; // time survived

        public bool IsActive => _modeActive;
        public ChallengerConfig Config => _config;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Check if challenger mode is available.
        /// </summary>
        public bool IsAvailable()
        {
            if (_config == null) return false;
            return _config.CanActivate();
        }

        /// <summary>
        /// Attempt to activate challenger mode.
        /// </summary>
        public bool TryActivate()
        {
            if (!IsAvailable())
            {
                Debug.Log("[ChallengerMode] Mode not available - missing config or assets");
                return false;
            }

            if (_modeActive)
            {
                Debug.Log("[ChallengerMode] Already active");
                return false;
            }

            ActivateMode();
            return true;
        }

        private void ActivateMode()
        {
            _modeActive = true;

            // Disable normal player if present
            var normalPlayer = FindObjectOfType<PlayerController>();
            if (normalPlayer != null)
            {
                normalPlayer.gameObject.SetActive(false);
            }

            // Disable Sasquatch - this mode has no chase
            var sasquatch = FindObjectOfType<Sasquatch.SasquatchAI>();
            if (sasquatch != null)
            {
                sasquatch.gameObject.SetActive(false);
            }

            // Spawn challenger character
            SpawnChallenger();

            // Setup survival system
            SetupSurvivalSystem();

            // Update game state
            if (GameManager.Instance != null)
            {
                // Use existing mode enum or set custom state
                GameManager.Instance.StartRun(GameMode.Standard); // Could add GameMode.Challenger
            }

            OnModeActivated?.Invoke();
            Debug.Log($"[ChallengerMode] Activated - {_config.CharacterName} on {_config.MountName}");
        }

        private void SpawnChallenger()
        {
            Vector3 spawnPos = _spawnPoint != null ? _spawnPoint.position : new Vector3(0, 5, 0);
            Quaternion spawnRot = _spawnPoint != null ? _spawnPoint.rotation : Quaternion.identity;

            // Spawn character
            if (_config.CharacterPrefab != null)
            {
                _characterInstance = Instantiate(_config.CharacterPrefab, spawnPos, spawnRot);
                _characterInstance.name = _config.CharacterName;

                // Apply color tint if specified
                if (_config.CharacterTint != Color.white)
                {
                    ApplyTint(_characterInstance, _config.CharacterTint);
                }
            }

            // Spawn and attach mount
            if (_config.MountPrefab != null && _characterInstance != null)
            {
                var mountObj = Instantiate(_config.MountPrefab, spawnPos, spawnRot);
                mountObj.transform.SetParent(_characterInstance.transform);
                mountObj.transform.localPosition = Vector3.zero;

                _mountInstance = mountObj.GetComponent<CreatureMount>();
                if (_mountInstance != null)
                {
                    _mountInstance.SetMountType(_config.MountStyle);
                }
            }
        }

        private void SetupSurvivalSystem()
        {
            if (_characterInstance == null) return;

            // Add survival system to character
            _survivalSystem = _characterInstance.AddComponent<TrickSurvival>();

            // Wire up references via reflection (config is serialized)
            var configField = typeof(TrickSurvival).GetField("_config",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (configField != null) configField.SetValue(_survivalSystem, _config);

            // Find trick controller on character
            var trickController = _characterInstance.GetComponent<Tricks.TrickController>();
            if (trickController != null)
            {
                var tcField = typeof(TrickSurvival).GetField("_trickController",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (tcField != null) tcField.SetValue(_survivalSystem, trickController);
            }

            // Subscribe to game over
            _survivalSystem.OnEnergyDepleted += OnChallengerPoofed;

            // Activate
            _survivalSystem.Activate();
        }

        private void OnChallengerPoofed()
        {
            float timeSurvived = _survivalSystem != null ? _survivalSystem.TimeSurvived : 0f;

            OnSurvivalEnded?.Invoke(timeSurvived);

            // Trigger game over with special message
            if (GameManager.Instance != null)
            {
                GameManager.Instance.EndRun(false);
            }

            Debug.Log($"[ChallengerMode] Run ended - survived {timeSurvived:F1} seconds");
        }

        /// <summary>
        /// Deactivate challenger mode and return to normal.
        /// </summary>
        public void Deactivate()
        {
            if (!_modeActive) return;

            _modeActive = false;

            // Cleanup instances
            if (_characterInstance != null)
            {
                Destroy(_characterInstance);
                _characterInstance = null;
            }

            _mountInstance = null;
            _survivalSystem = null;

            // Re-enable normal player
            var normalPlayer = FindObjectOfType<PlayerController>(true);
            if (normalPlayer != null)
            {
                normalPlayer.gameObject.SetActive(true);
            }

            OnModeDeactivated?.Invoke();
            Debug.Log("[ChallengerMode] Deactivated");
        }

        private void ApplyTint(GameObject obj, Color tint)
        {
            var renderers = obj.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                foreach (var mat in renderer.materials)
                {
                    if (mat.HasProperty("_BaseColor"))
                    {
                        Color current = mat.GetColor("_BaseColor");
                        mat.SetColor("_BaseColor", current * tint);
                    }
                    else if (mat.HasProperty("_Color"))
                    {
                        Color current = mat.GetColor("_Color");
                        mat.SetColor("_Color", current * tint);
                    }
                }
            }
        }

        /// <summary>
        /// Get the creature mount if active.
        /// </summary>
        public CreatureMount GetMount()
        {
            return _mountInstance;
        }

        /// <summary>
        /// Get the survival system if active.
        /// </summary>
        public TrickSurvival GetSurvivalSystem()
        {
            return _survivalSystem;
        }

        /// <summary>
        /// Get attribution text for credits screen.
        /// </summary>
        public string GetAttributionText()
        {
            if (_config == null) return null;
            return _config.GetAttributionText();
        }
    }
}
