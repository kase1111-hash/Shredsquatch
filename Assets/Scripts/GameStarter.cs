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

        private bool _hasStartedOnce;

        private void Update()
        {
            // R to restart during Game Over, or from the menu state that error recovery
            // returns to (the scene has no menu UI yet)
            if (GameManager.Instance != null
                && (GameManager.Instance.CurrentState == GameState.GameOver
                    || (GameManager.Instance.CurrentState == GameState.MainMenu && _hasStartedOnce))
                && UnityEngine.Input.GetKeyDown(KeyCode.R))
            {
                RestartGame();
            }
        }

        public void StartGame()
        {
            if (GameManager.Instance != null)
            {
                _hasStartedOnce = true;
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

        // The scene has no menu/game-over canvas yet, so draw a minimal overlay
        // so the player can tell the run ended (or is paused) and how to continue.
        private void OnGUI()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            if (gm.CurrentState == GameState.GameOver)
            {
                var run = gm.CurrentRun;
                DrawOverlay("SQUATCHED!",
                    $"Distance {run.Distance:F2} km   Trick score {run.TrickScore:N0}   Max speed {run.MaxSpeed:F0} km/h",
                    "Press R to ride again");
            }
            else if (gm.CurrentState == GameState.Paused)
            {
                DrawOverlay("PAUSED", string.Empty, "Press Esc to resume");
            }
            else if (gm.CurrentState == GameState.MainMenu && _hasStartedOnce)
            {
                DrawOverlay("RUN RESET", string.Empty, "Press R to ride again");
            }
        }

        private static void DrawOverlay(string title, string stats, string prompt)
        {
            float w = Screen.width;
            float h = Screen.height;

            Color previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(0f, 0f, w, h), Texture2D.whiteTexture);
            GUI.color = previous;

            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(h * 0.08f),
                fontStyle = FontStyle.Bold
            };
            style.normal.textColor = Color.white;
            GUI.Label(new Rect(0f, h * 0.30f, w, h * 0.15f), title, style);

            style.fontSize = Mathf.RoundToInt(h * 0.035f);
            style.fontStyle = FontStyle.Normal;
            GUI.Label(new Rect(0f, h * 0.47f, w, h * 0.08f), stats, style);
            GUI.Label(new Rect(0f, h * 0.56f, w, h * 0.08f), prompt, style);
        }
    }
}
