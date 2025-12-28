using UnityEngine;
using UnityEngine.UI;
using Shredsquatch.Core;

namespace Shredsquatch.UI
{
    public class PauseMenuController : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _quitButton;

        [Header("Panels")]
        [SerializeField] private GameObject _pausePanel;
        [SerializeField] private GameObject _settingsPanel;

        private void Start()
        {
            if (_resumeButton != null)
                _resumeButton.onClick.AddListener(OnResumeClicked);

            if (_restartButton != null)
                _restartButton.onClick.AddListener(OnRestartClicked);

            if (_settingsButton != null)
                _settingsButton.onClick.AddListener(OnSettingsClicked);

            if (_quitButton != null)
                _quitButton.onClick.AddListener(OnQuitClicked);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged += HandleStateChanged;
            }

            gameObject.SetActive(false);
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
            gameObject.SetActive(state == GameState.Paused);

            if (state == GameState.Paused)
            {
                ShowPausePanel();
            }
        }

        private void OnResumeClicked()
        {
            GameManager.Instance?.ResumeGame();
        }

        private void OnRestartClicked()
        {
            GameManager.Instance?.StartRun(GameManager.Instance.CurrentMode);
        }

        private void OnSettingsClicked()
        {
            ShowSettingsPanel();
        }

        private void OnQuitClicked()
        {
            GameManager.Instance?.ReturnToMenu();
        }

        private void ShowPausePanel()
        {
            if (_pausePanel != null) _pausePanel.SetActive(true);
            if (_settingsPanel != null) _settingsPanel.SetActive(false);
        }

        private void ShowSettingsPanel()
        {
            if (_pausePanel != null) _pausePanel.SetActive(false);
            if (_settingsPanel != null) _settingsPanel.SetActive(true);
        }

        public void BackToPause()
        {
            ShowPausePanel();
        }
    }
}
