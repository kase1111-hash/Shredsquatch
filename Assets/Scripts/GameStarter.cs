using UnityEngine;
using Shredsquatch.Core;
using Shredsquatch.Terrain;
using Shredsquatch.Sasquatch;

namespace Shredsquatch
{
    /// <summary>
    /// Auto-starts the game for testing and development.
    /// Attach to a GameObject in the scene.
    /// </summary>
    public class GameStarter : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool _autoStart = true;
        [SerializeField] private float _startDelay = 0.5f;
        [SerializeField] private GameMode _defaultMode = GameMode.Standard;

        [Header("References")]
        [SerializeField] private Transform _player;
        [SerializeField] private TerrainGenerator _terrainGenerator;
        [SerializeField] private SasquatchAI _sasquatch;
        [SerializeField] private ErrorRecoveryManager _errorRecovery;

        private void Awake()
        {
            // Ensure ErrorRecoveryManager exists (create if not assigned)
            if (_errorRecovery == null && ErrorRecoveryManager.Instance == null)
            {
                var errorRecoveryGO = new GameObject("ErrorRecoveryManager");
                _errorRecovery = errorRecoveryGO.AddComponent<ErrorRecoveryManager>();
                DontDestroyOnLoad(errorRecoveryGO);
            }
        }

        private void Start()
        {
            // Wire up references
            if (GameManager.Instance != null)
            {
                if (_player != null)
                {
                    GameManager.Instance.SetPlayerReference(_player);
                }
            }

            if (_terrainGenerator != null && _player != null)
            {
                _terrainGenerator.SetPlayerReference(_player);
            }

            if (_sasquatch != null && _player != null)
            {
                _sasquatch.SetPlayerReference(_player);
            }

            if (_autoStart)
            {
                Invoke(nameof(StartGame), _startDelay);
            }
        }

        public void StartGame()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartRun(_defaultMode);
                Debug.Log("Game started!");
            }
            else
            {
                Debug.LogError("GameManager not found!");
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
