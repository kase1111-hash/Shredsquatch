using UnityEngine;
using System.Text;

namespace Shredsquatch.Core
{
    /// <summary>
    /// Runtime debug overlay showing game state, player info, and common issues.
    /// Toggle with F1 key. Shows what's missing or broken.
    /// </summary>
    public class RuntimeDebugger : MonoBehaviour
    {
        [SerializeField] private bool _showOnStart = true;
        [SerializeField] private KeyCode _toggleKey = KeyCode.F1;
        
        private bool _isVisible;
        private GUIStyle _boxStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _errorStyle;
        private GUIStyle _warnStyle;
        private GUIStyle _okStyle;
        
        private StringBuilder _sb = new StringBuilder();
        private float _updateInterval = 0.5f;
        private float _lastUpdate;
        private string _cachedInfo;

        private void Start()
        {
            _isVisible = _showOnStart;
        }

        private void Update()
        {
            if (Input.GetKeyDown(_toggleKey))
            {
                _isVisible = !_isVisible;
            }
            
            if (_isVisible && Time.unscaledTime - _lastUpdate > _updateInterval)
            {
                _lastUpdate = Time.unscaledTime;
                UpdateInfo();
            }
        }

        private void InitStyles()
        {
            if (_boxStyle != null) return;
            
            _boxStyle = new GUIStyle(GUI.skin.box);
            _boxStyle.normal.background = MakeTexture(2, 2, new Color(0, 0, 0, 0.8f));
            
            _labelStyle = new GUIStyle(GUI.skin.label);
            _labelStyle.fontSize = 12;
            _labelStyle.normal.textColor = Color.white;
            
            _errorStyle = new GUIStyle(_labelStyle);
            _errorStyle.normal.textColor = new Color(1f, 0.4f, 0.4f);
            
            _warnStyle = new GUIStyle(_labelStyle);
            _warnStyle.normal.textColor = new Color(1f, 0.9f, 0.4f);
            
            _okStyle = new GUIStyle(_labelStyle);
            _okStyle.normal.textColor = new Color(0.4f, 1f, 0.4f);
        }

        private Texture2D MakeTexture(int width, int height, Color color)
        {
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            var texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private void UpdateInfo()
        {
            _sb.Clear();
            _sb.AppendLine("=== SHREDSQUATCH DEBUG (F1 to toggle) ===\n");
            
            // Core Systems
            _sb.AppendLine("-- Core Systems --");
            AppendStatus("GameManager", GameManager.Instance != null);
            AppendStatus("ErrorRecovery", ErrorRecoveryManager.Instance != null);
            
            // Game State
            if (GameManager.Instance != null)
            {
                _sb.AppendLine($"\n-- Game State --");
                _sb.AppendLine($"State: {GameManager.Instance.CurrentState}");
                _sb.AppendLine($"Mode: {GameManager.Instance.CurrentMode}");
                _sb.AppendLine($"Distance: {GameManager.CurrentRun.Distance:F2} km");
                _sb.AppendLine($"Score: {GameManager.CurrentRun.TrickScore}");
                _sb.AppendLine($"TimeScale: {Time.timeScale}");
            }
            
            // Player
            _sb.AppendLine($"\n-- Player --");
            var player = GameObject.FindGameObjectWithTag("Player");
            AppendStatus("Player Found", player != null);
            
            if (player != null)
            {
                AppendStatus("CharacterController", player.GetComponent<CharacterController>() != null);
                AppendStatus("PlayerInput", player.GetComponent<Player.PlayerInput>() != null);
                AppendStatus("SnowboardPhysics", player.GetComponent<Player.SnowboardPhysics>() != null);
                AppendStatus("PlayerController", player.GetComponent<Player.PlayerController>() != null);
                
                var physics = player.GetComponent<Player.SnowboardPhysics>();
                if (physics != null)
                {
                    _sb.AppendLine($"Speed: {physics.CurrentSpeed:F1} km/h");
                    _sb.AppendLine($"Grounded: {physics.IsGrounded}");
                }
                
                _sb.AppendLine($"Position: {player.transform.position}");
            }
            
            // Scene
            _sb.AppendLine($"\n-- Scene --");
            var grounds = FindObjectsOfType<Collider>();
            int groundCount = 0;
            foreach (var c in grounds)
            {
                if (c.gameObject.layer == LayerMask.NameToLayer("Ground") || 
                    c.gameObject.name.ToLower().Contains("ground") ||
                    c.gameObject.name.ToLower().Contains("terrain"))
                {
                    groundCount++;
                }
            }
            AppendStatus($"Ground Colliders ({groundCount})", groundCount > 0);
            
            // Camera
            AppendStatus("Main Camera", Camera.main != null);
            
            // Input
            _sb.AppendLine($"\n-- Input --");
            _sb.AppendLine($"Horizontal: {Input.GetAxis("Horizontal"):F2}");
            _sb.AppendLine($"Vertical: {Input.GetAxis("Vertical"):F2}");
            _sb.AppendLine($"Jump: {Input.GetButton("Jump")}");
            
            // FPS
            _sb.AppendLine($"\n-- Performance --");
            _sb.AppendLine($"FPS: {(1f / Time.unscaledDeltaTime):F0}");
            
            // Quick fixes
            _sb.AppendLine($"\n-- Quick Actions --");
            _sb.AppendLine("Press F2: Start game");
            _sb.AppendLine("Press F3: Reset player position");
            _sb.AppendLine("Press F4: Toggle pause");
            
            _cachedInfo = _sb.ToString();
        }

        private void AppendStatus(string name, bool isOk)
        {
            string status = isOk ? "[OK]" : "[MISSING]";
            _sb.AppendLine($"{status} {name}");
        }

        private void OnGUI()
        {
            if (!_isVisible) return;
            
            InitStyles();
            
            float width = 300;
            float height = 500;
            var rect = new Rect(10, 10, width, height);
            
            GUI.Box(rect, "", _boxStyle);
            
            GUILayout.BeginArea(new Rect(rect.x + 10, rect.y + 10, rect.width - 20, rect.height - 20));
            GUILayout.Label(_cachedInfo ?? "Loading...", _labelStyle);
            GUILayout.EndArea();
            
            // Handle quick actions
            HandleQuickActions();
        }

        private void HandleQuickActions()
        {
            if (Input.GetKeyDown(KeyCode.F2))
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.StartRun(GameMode.Standard);
                    Debug.Log("[Debug] Started game");
                }
            }
            
            if (Input.GetKeyDown(KeyCode.F3))
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    player.transform.position = new Vector3(0, 5, 0);
                    Debug.Log("[Debug] Reset player position");
                }
            }
            
            if (Input.GetKeyDown(KeyCode.F4))
            {
                if (GameManager.Instance != null)
                {
                    if (GameManager.Instance.CurrentState == GameState.Playing)
                        GameManager.Instance.PauseGame();
                    else if (GameManager.Instance.CurrentState == GameState.Paused)
                        GameManager.Instance.ResumeGame();
                }
            }
        }
    }
}
