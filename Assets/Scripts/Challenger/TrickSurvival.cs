using UnityEngine;
using System;
using Shredsquatch.Tricks;
using Shredsquatch.Core;

namespace Shredsquatch.Challenger
{
    /// <summary>
    /// Alternate gameplay mode: survive by performing tricks.
    /// Energy constantly drains, tricks refill it. Poof when empty.
    /// No Sasquatch chase - purely skill-based survival.
    /// </summary>
    public class TrickSurvival : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private ChallengerConfig _config;

        [Header("References")]
        [SerializeField] private TrickController _trickController;

        [Header("UI References")]
        [SerializeField] private UnityEngine.UI.Slider _energyBar;
        [SerializeField] private TMPro.TextMeshProUGUI _energyText;
        [SerializeField] private UnityEngine.UI.Image _energyFill;
        [SerializeField] private Animator _uiAnimator;

        [Header("Effects")]
        [SerializeField] private GameObject _poofEffectPrefab;
        [SerializeField] private AudioSource _audioSource;

        // State
        private float _currentEnergy;
        private float _maxEnergy;
        private bool _isActive;
        private bool _inGracePeriod;
        private float _gracePeriodTimer;
        private float _timeSurvived;

        // Visual feedback
        private float _lastEnergyGain;
        private Color _normalColor = new Color(0.3f, 0.8f, 1f);
        private Color _lowColor = new Color(1f, 0.5f, 0.2f);
        private Color _criticalColor = new Color(1f, 0.2f, 0.2f);

        // Events
        public event Action OnEnergyDepleted;
        public event Action<float> OnEnergyChanged;
        public event Action<float, float> OnTrickEnergyGain; // gained, total

        public float CurrentEnergy => _currentEnergy;
        public float MaxEnergy => _maxEnergy;
        public float EnergyPercent => _maxEnergy > 0 ? _currentEnergy / _maxEnergy : 0;
        public float TimeSurvived => _timeSurvived;
        public bool IsActive => _isActive;

        private void Awake()
        {
            if (_config == null)
            {
                Debug.LogWarning("[TrickSurvival] No config assigned, using defaults");
                _maxEnergy = 100f;
            }
            else
            {
                _maxEnergy = _config.StartingEnergy;
            }
        }

        private void OnEnable()
        {
            if (_trickController != null)
            {
                _trickController.OnTrickCompleted += OnTrickCompleted;
                _trickController.OnComboUpdated += OnComboUpdated;
            }
        }

        private void OnDisable()
        {
            if (_trickController != null)
            {
                _trickController.OnTrickCompleted -= OnTrickCompleted;
                _trickController.OnComboUpdated -= OnComboUpdated;
            }
        }

        private void Update()
        {
            if (!_isActive) return;

            // Grace period countdown
            if (_inGracePeriod)
            {
                _gracePeriodTimer -= Time.deltaTime;
                if (_gracePeriodTimer <= 0)
                {
                    _inGracePeriod = false;
                }
                UpdateUI();
                return;
            }

            // Drain energy
            float drainRate = _config != null ? _config.EnergyDrainRate : 5f;
            _currentEnergy -= drainRate * Time.deltaTime;

            // Track survival time
            _timeSurvived += Time.deltaTime;

            // Check for depletion
            if (_currentEnergy <= 0)
            {
                _currentEnergy = 0;
                TriggerPoof();
            }

            UpdateUI();
            OnEnergyChanged?.Invoke(_currentEnergy);
        }

        /// <summary>
        /// Start the trick survival mode.
        /// </summary>
        public void Activate()
        {
            _currentEnergy = _maxEnergy;
            _isActive = true;
            _timeSurvived = 0f;

            // Grace period at start
            float gracePeriod = _config != null ? _config.GracePeriod : 10f;
            _inGracePeriod = gracePeriod > 0;
            _gracePeriodTimer = gracePeriod;

            if (_energyBar != null)
            {
                _energyBar.gameObject.SetActive(true);
            }

            Debug.Log("[TrickSurvival] Mode activated");
        }

        /// <summary>
        /// Stop the trick survival mode.
        /// </summary>
        public void Deactivate()
        {
            _isActive = false;

            if (_energyBar != null)
            {
                _energyBar.gameObject.SetActive(false);
            }
        }

        private void OnTrickCompleted(string trickName, int points)
        {
            if (!_isActive || _inGracePeriod) return;

            int minScore = _config != null ? _config.MinTrickScore : 100;
            if (points < minScore) return;

            float energyPerPoint = _config != null ? _config.EnergyPerTrickPoint : 0.1f;
            float energyGained = points * energyPerPoint;

            AddEnergy(energyGained);
            OnTrickEnergyGain?.Invoke(energyGained, _currentEnergy);

            // Visual feedback
            _lastEnergyGain = energyGained;
            if (_uiAnimator != null)
            {
                _uiAnimator.SetTrigger("EnergyGain");
            }
        }

        private void OnComboUpdated(int comboCount)
        {
            // Bonus energy for combos
            if (!_isActive || comboCount < 2) return;

            float comboBonus = comboCount * 2f;
            AddEnergy(comboBonus);
        }

        /// <summary>
        /// Add energy (from tricks, combos, pickups).
        /// </summary>
        public void AddEnergy(float amount)
        {
            _currentEnergy = Mathf.Min(_currentEnergy + amount, _maxEnergy);
            OnEnergyChanged?.Invoke(_currentEnergy);
        }

        private void UpdateUI()
        {
            if (_energyBar != null)
            {
                _energyBar.value = EnergyPercent;
            }

            if (_energyText != null)
            {
                if (_inGracePeriod)
                {
                    _energyText.text = $"GET READY: {_gracePeriodTimer:F0}";
                }
                else
                {
                    _energyText.text = $"{_currentEnergy:F0}";
                }
            }

            if (_energyFill != null)
            {
                // Color based on energy level
                if (EnergyPercent > 0.5f)
                    _energyFill.color = _normalColor;
                else if (EnergyPercent > 0.25f)
                    _energyFill.color = _lowColor;
                else
                    _energyFill.color = _criticalColor;
            }

            // Pulsing when critical
            if (_uiAnimator != null)
            {
                _uiAnimator.SetBool("Critical", EnergyPercent < 0.25f && !_inGracePeriod);
            }
        }

        private void TriggerPoof()
        {
            _isActive = false;

            // Spawn poof effect
            GameObject poofPrefab = _poofEffectPrefab;
            if (_config != null && _config.PoofEffectPrefab != null)
            {
                poofPrefab = _config.PoofEffectPrefab;
            }

            if (poofPrefab != null)
            {
                Instantiate(poofPrefab, transform.position, Quaternion.identity);
            }

            // Play sound
            if (_audioSource != null && _config != null && _config.PoofSound != null)
            {
                _audioSource.PlayOneShot(_config.PoofSound);
            }

            // Notify game over
            OnEnergyDepleted?.Invoke();

            Debug.Log($"[TrickSurvival] Poof! Survived {_timeSurvived:F1} seconds");
        }

        /// <summary>
        /// Get current status for UI/debug.
        /// </summary>
        public string GetStatusText()
        {
            if (_inGracePeriod)
                return $"Grace Period: {_gracePeriodTimer:F1}s";

            return $"Energy: {_currentEnergy:F0}/{_maxEnergy:F0} | Survived: {_timeSurvived:F1}s";
        }
    }
}
