using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Shredsquatch.Core;

namespace Shredsquatch.UI
{
    public class GameOverScreen : MonoBehaviour
    {
        [Header("Stats Display")]
        [SerializeField] private TextMeshProUGUI _distanceText;
        [SerializeField] private TextMeshProUGUI _trickCountText;
        [SerializeField] private TextMeshProUGUI _trickScoreText;
        [SerializeField] private TextMeshProUGUI _maxSpeedText;
        [SerializeField] private TextMeshProUGUI _maxComboText;
        [SerializeField] private TextMeshProUGUI _totalScoreText;

        [Header("Personal Best")]
        [SerializeField] private GameObject _newPBBadge;
        [SerializeField] private TextMeshProUGUI _previousBestText;

        [Header("Unlocks")]
        [SerializeField] private GameObject _unlockPanel;
        [SerializeField] private TextMeshProUGUI _unlockText;

        [Header("Buttons")]
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _menuButton;

        [Header("Animation")]
        [SerializeField] private Animator _animator;

        private void Start()
        {
            gameObject.SetActive(false);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameOver += Show;
            }

            if (_retryButton != null)
            {
                _retryButton.onClick.AddListener(OnRetryClicked);
            }

            if (_menuButton != null)
            {
                _menuButton.onClick.AddListener(OnMenuClicked);
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameOver -= Show;
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);

            if (GameManager.Instance == null) return;

            var stats = GameManager.Instance.CurrentRun;
            var progress = GameManager.Instance.Progress;

            // Display stats
            if (_distanceText != null)
                _distanceText.text = $"Distance: {stats.Distance:F2} km";

            if (_trickCountText != null)
                _trickCountText.text = $"Tricks: {stats.TrickCount}";

            if (_trickScoreText != null)
                _trickScoreText.text = $"Trick Score: {stats.TrickScore:N0}";

            if (_maxSpeedText != null)
                _maxSpeedText.text = $"Max Speed: {stats.MaxSpeed:F0} km/h";

            if (_maxComboText != null)
                _maxComboText.text = $"Max Combo: {stats.MaxCombo}x";

            if (_totalScoreText != null)
                _totalScoreText.text = $"Total: {stats.CalculateTotalScore():N0}";

            // Check for new personal best
            bool isNewPB = stats.Distance > progress.BestDistance;
            if (_newPBBadge != null)
            {
                _newPBBadge.SetActive(isNewPB);
            }

            if (_previousBestText != null && !isNewPB)
            {
                _previousBestText.text = $"Best: {progress.BestDistance:F2} km";
                _previousBestText.gameObject.SetActive(true);
            }
            else if (_previousBestText != null)
            {
                _previousBestText.gameObject.SetActive(false);
            }

            // Check for unlocks
            CheckUnlocks(stats.Distance, progress);

            // Play animation
            if (_animator != null)
            {
                _animator.SetTrigger("Show");
            }
        }

        private void CheckUnlocks(float distance, PlayerProgress progress)
        {
            string unlockMessage = "";

            // Check each unlock threshold
            if (distance >= 5f && !WasUnlocked(progress, 5f))
            {
                unlockMessage = "Unlocked: Classic Yeti & Abominable Skins!";
            }
            else if (distance >= 10f && !WasUnlocked(progress, 10f))
            {
                unlockMessage = "Unlocked: Night Mode!";
            }
            else if (distance >= 15f && !WasUnlocked(progress, 15f))
            {
                unlockMessage = "Unlocked: Fire Trail & Avalanche Mode!";
            }
            else if (distance >= 20f && !WasUnlocked(progress, 20f))
            {
                unlockMessage = "Unlocked: Rainbow Trail & Storm Mode!";
            }
            else if (distance >= 25f && !WasUnlocked(progress, 25f))
            {
                unlockMessage = "Unlocked: Lightning Trail!";
            }
            else if (distance >= 30f && !WasUnlocked(progress, 30f))
            {
                unlockMessage = "Unlocked: GOLDEN SASQUATCH & All Trails!";
            }

            if (_unlockPanel != null)
            {
                _unlockPanel.SetActive(!string.IsNullOrEmpty(unlockMessage));
            }

            if (_unlockText != null && !string.IsNullOrEmpty(unlockMessage))
            {
                _unlockText.text = unlockMessage;
            }
        }

        private bool WasUnlocked(PlayerProgress progress, float threshold)
        {
            // Check if this was already unlocked in a previous run
            return progress.BestDistance >= threshold;
        }

        private void OnRetryClicked()
        {
            gameObject.SetActive(false);
            GameManager.Instance?.StartRun(GameManager.Instance.CurrentMode);
        }

        private void OnMenuClicked()
        {
            gameObject.SetActive(false);
            GameManager.Instance?.ReturnToMenu();
        }

        public void Hide()
        {
            if (_animator != null)
            {
                _animator.SetTrigger("Hide");
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
