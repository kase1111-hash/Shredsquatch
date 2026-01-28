using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Shredsquatch.Core;
using Shredsquatch.Player;
using Shredsquatch.Tricks;
using Shredsquatch.Sasquatch;

namespace Shredsquatch.UI
{
    public class HUDController : MonoBehaviour
    {
        [Header("Distance/Speed Panel")]
        [SerializeField] private TextMeshProUGUI _distanceText;
        [SerializeField] private TextMeshProUGUI _speedText;
        [SerializeField] private Image _speedGauge;

        [Header("Trick Panel")]
        [SerializeField] private TextMeshProUGUI _trickNameText;
        [SerializeField] private TextMeshProUGUI _trickScoreText;
        [SerializeField] private TextMeshProUGUI _comboText;
        [SerializeField] private TextMeshProUGUI _totalTrickScoreText;
        [SerializeField] private Animator _trickAnimator;

        [Header("Sasquatch Proximity")]
        [SerializeField] private Slider _proximityBar;
        [SerializeField] private Image _proximityFill;
        [SerializeField] private Animator _proximityAnimator;

        [Header("Proximity Colors")]
        [SerializeField] private Color _safeColor = Color.green;      // >600m
        [SerializeField] private Color _warningColor = Color.yellow;  // 300-600m
        [SerializeField] private Color _dangerColor = Color.red;      // <300m

        [Header("References")]
        [SerializeField] private PlayerController _player;
        [SerializeField] private TrickController _trickController;
        [SerializeField] private SasquatchAI _sasquatch;

        private float _trickDisplayTimer;
        private bool _isPulsing;

        private void Start()
        {
            // Subscribe to events
            if (_trickController != null)
            {
                _trickController.OnTrickCompleted += ShowTrick;
                _trickController.OnComboUpdated += UpdateCombo;
            }

            if (_sasquatch != null)
            {
                _sasquatch.OnDistanceChanged += UpdateProximity;
                _sasquatch.OnSpawn += OnSasquatchSpawn;
            }

            // Initial state
            HideTrickDisplay();
            HideProximityBar();
        }

        private void OnDestroy()
        {
            if (_trickController != null)
            {
                _trickController.OnTrickCompleted -= ShowTrick;
                _trickController.OnComboUpdated -= UpdateCombo;
            }

            if (_sasquatch != null)
            {
                _sasquatch.OnDistanceChanged -= UpdateProximity;
                _sasquatch.OnSpawn -= OnSasquatchSpawn;
            }
        }

        private void Update()
        {
            if (GameManager.Instance?.CurrentState != GameState.Playing)
                return;

            UpdateDistanceSpeed();
            UpdateTrickDisplay();
            UpdateTotalScore();
        }

        private void UpdateDistanceSpeed()
        {
            if (GameManager.Instance == null) return;

            // Distance
            float distance = GameManager.Instance.CurrentRun.Distance;
            if (_distanceText != null)
            {
                _distanceText.text = $"{distance:F2} km";
            }

            // Speed
            float speed = _player != null ? _player.GetCurrentSpeedKmh() : 0f;
            if (_speedText != null)
            {
                _speedText.text = $"{speed:F0} km/h";
            }

            // Speed gauge (0-120 km/h range)
            if (_speedGauge != null)
            {
                _speedGauge.fillAmount = Mathf.Clamp01(speed / 120f);
            }
        }

        private void ShowTrick(string trickName, int points)
        {
            if (_trickNameText != null)
            {
                _trickNameText.text = trickName;
                _trickNameText.gameObject.SetActive(true);
            }

            if (_trickScoreText != null)
            {
                _trickScoreText.text = $"+{points:N0}";
                _trickScoreText.gameObject.SetActive(true);
            }

            if (_trickAnimator != null)
            {
                _trickAnimator.SetTrigger("ShowTrick");
            }

            _trickDisplayTimer = 2f;
        }

        private void UpdateCombo(int comboCount)
        {
            if (_comboText != null)
            {
                if (comboCount > 1)
                {
                    _comboText.text = $"x{comboCount} COMBO!";
                    _comboText.gameObject.SetActive(true);
                }
                else
                {
                    _comboText.gameObject.SetActive(false);
                }
            }
        }

        private void UpdateTrickDisplay()
        {
            if (_trickDisplayTimer > 0)
            {
                _trickDisplayTimer -= Time.deltaTime;

                if (_trickDisplayTimer <= 0)
                {
                    HideTrickDisplay();
                }
            }
        }

        private void HideTrickDisplay()
        {
            if (_trickNameText != null)
                _trickNameText.gameObject.SetActive(false);

            if (_trickScoreText != null)
                _trickScoreText.gameObject.SetActive(false);

            if (_comboText != null)
                _comboText.gameObject.SetActive(false);
        }

        private void UpdateTotalScore()
        {
            if (_totalTrickScoreText != null && GameManager.Instance != null)
            {
                int score = GameManager.Instance.CurrentRun.TrickScore;
                _totalTrickScoreText.text = $"Tricks: {score:N0}";
            }
        }

        private void OnSasquatchSpawn()
        {
            ShowProximityBar();

            if (_proximityAnimator != null)
            {
                _proximityAnimator.SetTrigger("Spawn");
            }
        }

        private void UpdateProximity(float distance)
        {
            if (_proximityBar == null) return;

            // Map distance to bar (800m = safe, 0m = caught)
            float normalizedDistance = Mathf.Clamp01(distance / 800f);
            _proximityBar.value = 1f - normalizedDistance; // Inverted: fuller = closer

            // Color based on distance
            Color targetColor;
            if (distance > 600f)
            {
                targetColor = _safeColor;
                StopPulsing();
            }
            else if (distance > 300f)
            {
                targetColor = _warningColor;
                StopPulsing();
            }
            else
            {
                targetColor = _dangerColor;

                // Pulsing when very close
                if (distance < 150f)
                {
                    StartPulsing();
                }
            }

            if (_proximityFill != null)
            {
                _proximityFill.color = targetColor;
            }
        }

        private void ShowProximityBar()
        {
            if (_proximityBar != null)
            {
                _proximityBar.gameObject.SetActive(true);
            }
        }

        private void HideProximityBar()
        {
            if (_proximityBar != null)
            {
                _proximityBar.gameObject.SetActive(false);
            }
        }

        private void StartPulsing()
        {
            if (_isPulsing) return;
            _isPulsing = true;

            if (_proximityAnimator != null)
            {
                _proximityAnimator.SetBool("Pulsing", true);
            }
        }

        private void StopPulsing()
        {
            if (!_isPulsing) return;
            _isPulsing = false;

            if (_proximityAnimator != null)
            {
                _proximityAnimator.SetBool("Pulsing", false);
            }
        }

        #region Runtime Wiring

        /// <summary>
        /// Set the player reference at runtime.
        /// </summary>
        public void SetPlayer(PlayerController player)
        {
            _player = player;
        }

        /// <summary>
        /// Set the trick controller reference at runtime.
        /// </summary>
        public void SetTrickController(TrickController trickController)
        {
            // Unsubscribe from old
            if (_trickController != null)
            {
                _trickController.OnTrickCompleted -= ShowTrick;
                _trickController.OnComboUpdated -= UpdateCombo;
            }

            _trickController = trickController;

            // Subscribe to new
            if (_trickController != null)
            {
                _trickController.OnTrickCompleted += ShowTrick;
                _trickController.OnComboUpdated += UpdateCombo;
            }
        }

        /// <summary>
        /// Set the Sasquatch reference at runtime.
        /// </summary>
        public void SetSasquatch(SasquatchAI sasquatch)
        {
            // Unsubscribe from old
            if (_sasquatch != null)
            {
                _sasquatch.OnDistanceChanged -= UpdateProximity;
                _sasquatch.OnSpawn -= OnSasquatchSpawn;
            }

            _sasquatch = sasquatch;

            // Subscribe to new
            if (_sasquatch != null)
            {
                _sasquatch.OnDistanceChanged += UpdateProximity;
                _sasquatch.OnSpawn += OnSasquatchSpawn;
                // Only show proximity bar if Sasquatch has already spawned (is active)
                // Otherwise wait for OnSpawn event to be triggered
                if (_sasquatch.IsActive)
                {
                    ShowProximityBar();
                }
            }
        }

        #endregion
    }
}
