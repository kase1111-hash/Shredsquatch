using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Shredsquatch.Core;

namespace Shredsquatch.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Main Menu Buttons")]
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _avalancheModeButton;
        [SerializeField] private Button _stormModeButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _quitButton;

        [Header("Stats Display")]
        [SerializeField] private TextMeshProUGUI _bestDistanceText;
        [SerializeField] private TextMeshProUGUI _bestTrickScoreText;

        [Header("Unlock Indicators")]
        [SerializeField] private GameObject _nightModeUnlockBadge;
        [SerializeField] private GameObject _avalancheUnlockBadge;
        [SerializeField] private GameObject _stormUnlockBadge;

        [Header("Panels")]
        [SerializeField] private GameObject _mainPanel;
        [SerializeField] private GameObject _settingsPanel;
        [SerializeField] private GameObject _modeSelectPanel;

        private void Start()
        {
            // Button listeners
            if (_playButton != null)
                _playButton.onClick.AddListener(OnPlayClicked);

            if (_avalancheModeButton != null)
                _avalancheModeButton.onClick.AddListener(OnAvalancheClicked);

            if (_stormModeButton != null)
                _stormModeButton.onClick.AddListener(OnStormClicked);

            if (_settingsButton != null)
                _settingsButton.onClick.AddListener(OnSettingsClicked);

            if (_quitButton != null)
                _quitButton.onClick.AddListener(OnQuitClicked);

            // Subscribe to state changes
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged += HandleStateChanged;
            }

            UpdateDisplay();
            ShowMainPanel();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
            }
        }

        private void HandleStateChanged(GameState state)
        {
            gameObject.SetActive(state == GameState.MainMenu);

            if (state == GameState.MainMenu)
            {
                UpdateDisplay();
            }
        }

        private void UpdateDisplay()
        {
            if (GameManager.Instance == null) return;

            var progress = GameManager.Instance.Progress;

            // Stats
            if (_bestDistanceText != null)
                _bestDistanceText.text = $"Best: {progress.BestDistance:F2} km";

            if (_bestTrickScoreText != null)
                _bestTrickScoreText.text = $"Best Tricks: {progress.BestTrickScore:N0}";

            // Unlock badges
            if (_nightModeUnlockBadge != null)
                _nightModeUnlockBadge.SetActive(progress.NightModeUnlocked);

            if (_avalancheUnlockBadge != null)
                _avalancheUnlockBadge.SetActive(progress.AvalancheModeUnlocked);

            if (_stormUnlockBadge != null)
                _stormUnlockBadge.SetActive(progress.StormModeUnlocked);

            // Enable/disable mode buttons
            if (_avalancheModeButton != null)
                _avalancheModeButton.interactable = progress.AvalancheModeUnlocked;

            if (_stormModeButton != null)
                _stormModeButton.interactable = progress.StormModeUnlocked;
        }

        private void OnPlayClicked()
        {
            GameManager.Instance?.StartRun(GameMode.Standard);
        }

        private void OnAvalancheClicked()
        {
            if (GameManager.Instance?.Progress.AvalancheModeUnlocked == true)
            {
                GameManager.Instance.StartRun(GameMode.Avalanche);
            }
        }

        private void OnStormClicked()
        {
            if (GameManager.Instance?.Progress.StormModeUnlocked == true)
            {
                GameManager.Instance.StartRun(GameMode.Storm);
            }
        }

        private void OnSettingsClicked()
        {
            ShowSettingsPanel();
        }

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void ShowMainPanel()
        {
            if (_mainPanel != null) _mainPanel.SetActive(true);
            if (_settingsPanel != null) _settingsPanel.SetActive(false);
            if (_modeSelectPanel != null) _modeSelectPanel.SetActive(false);
        }

        private void ShowSettingsPanel()
        {
            if (_mainPanel != null) _mainPanel.SetActive(false);
            if (_settingsPanel != null) _settingsPanel.SetActive(true);
        }

        public void BackToMain()
        {
            ShowMainPanel();
        }
    }
}
