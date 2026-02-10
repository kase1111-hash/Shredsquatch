using UnityEngine;
using Shredsquatch.Core;

namespace Shredsquatch
{
    /// <summary>
    /// Auto-starts the game run. SceneInitializer handles all wiring;
    /// this script only triggers the state transition to Playing.
    /// </summary>
    public class GameStarter : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool _autoStart = true;
        [SerializeField] private float _startDelay = 0.5f;
        [SerializeField] private GameMode _defaultMode = GameMode.Standard;

        private void Start()
        {
            if (_autoStart)
            {
                Invoke(nameof(StartGame), _startDelay);
            }
        }

        private void Update()
        {
            // R to restart during Game Over (works without UI Canvas)
            if (GameManager.Instance != null
                && GameManager.Instance.CurrentState == GameState.GameOver
                && UnityEngine.Input.GetKeyDown(KeyCode.R))
            {
                RestartGame();
            }
        }

        public void StartGame()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartRun(_defaultMode);
                Debug.Log("[GameStarter] Game started");
            }
            else
            {
                Debug.LogError("[GameStarter] GameManager not found!");
            }
        }

        public void RestartGame()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ReturnToMenu();
                Invoke(nameof(StartGame), 0.1f);
            }
        }
    }
}
