using UnityEngine;

namespace Shredsquatch.Core
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
        [SerializeField] private Terrain.TerrainGenerator _terrainGenerator;
        [SerializeField] private Sasquatch.SasquatchAI _sasquatch;

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
