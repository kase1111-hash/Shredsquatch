using UnityEngine;
using System.Collections.Generic;

namespace Shredsquatch.Challenger
{
    /// <summary>
    /// Handles hidden input sequences for secret unlocks.
    /// Attach to persistent game object.
    /// </summary>
    public class SecretInput : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _inputTimeout = 2f;
        [SerializeField] private bool _enableSecrets = true;

        // Input buffer
        private List<KeyCode> _inputBuffer = new List<KeyCode>();
        private float _lastInputTime;

        // Known sequences (can be configured externally)
        private Dictionary<string, System.Action> _sequences = new Dictionary<string, System.Action>();

        // Sequence definitions using base64-ish obscurity
        // Actual keys are set at runtime to avoid easy discovery
        private string _challengerSequence;

        private void Start()
        {
            // Register sequences
            RegisterDefaultSequences();
        }

        private void RegisterDefaultSequences()
        {
            // Challenger mode: specific button combo
            // Encoded to avoid casual discovery in decompiled code
            _challengerSequence = DecodeSequence("VVVET0RXTg=="); // Placeholder

            _sequences["challenger"] = TryActivateChallenger;
        }

        private void Update()
        {
            if (!_enableSecrets) return;

            // Clear buffer on timeout
            if (Time.time - _lastInputTime > _inputTimeout && _inputBuffer.Count > 0)
            {
                _inputBuffer.Clear();
            }

            // Check for any key press
            if (Input.anyKeyDown)
            {
                CheckInput();
            }
        }

        private void CheckInput()
        {
            // Find which key was pressed
            foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(key))
                {
                    AddInput(key);
                    break;
                }
            }
        }

        private void AddInput(KeyCode key)
        {
            _inputBuffer.Add(key);
            _lastInputTime = Time.time;

            // Limit buffer size
            if (_inputBuffer.Count > 20)
            {
                _inputBuffer.RemoveAt(0);
            }

            // Check against known sequences
            CheckSequences();
        }

        private void CheckSequences()
        {
            string current = BufferToString();

            foreach (var kvp in _sequences)
            {
                string expected = GetSequenceKeys(kvp.Key);
                if (string.IsNullOrEmpty(expected)) continue;

                if (current.EndsWith(expected))
                {
                    // Match found
                    _inputBuffer.Clear();
                    kvp.Value?.Invoke();
                    return;
                }
            }
        }

        private string BufferToString()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var key in _inputBuffer)
            {
                sb.Append(KeyToChar(key));
            }
            return sb.ToString();
        }

        private char KeyToChar(KeyCode key)
        {
            // Map common keys to chars
            switch (key)
            {
                case KeyCode.UpArrow: return 'U';
                case KeyCode.DownArrow: return 'D';
                case KeyCode.LeftArrow: return 'L';
                case KeyCode.RightArrow: return 'R';
                case KeyCode.Space: return 'S';
                case KeyCode.Return: return 'E';
                case KeyCode.A: return 'A';
                case KeyCode.B: return 'B';
                default:
                    if (key >= KeyCode.A && key <= KeyCode.Z)
                        return (char)('A' + (key - KeyCode.A));
                    return '?';
            }
        }

        private string GetSequenceKeys(string sequenceName)
        {
            // Return the key sequence for a given name
            switch (sequenceName)
            {
                case "challenger":
                    // Up Up Down Down Left Right Left Right B A
                    // Classic konami-style but modified
                    return "UUDDLRLRBA";
                default:
                    return null;
            }
        }

        private string DecodeSequence(string encoded)
        {
            // Simple decode - not actual security, just obscurity
            try
            {
                byte[] data = System.Convert.FromBase64String(encoded);
                return System.Text.Encoding.UTF8.GetString(data);
            }
            catch
            {
                return null;
            }
        }

        private void TryActivateChallenger()
        {
            var challengerMode = ChallengerMode.Instance;
            if (challengerMode == null)
            {
                challengerMode = FindObjectOfType<ChallengerMode>();
            }

            if (challengerMode != null && challengerMode.IsAvailable())
            {
                if (challengerMode.TryActivate())
                {
                    // Play unlock sound or effect
                    Debug.Log("[Secret] Challenger mode unlocked!");
                }
            }
            else
            {
                // Mode not available - silently fail
                Debug.Log("[Secret] Mode not available");
            }
        }

        /// <summary>
        /// Register a custom secret sequence.
        /// </summary>
        public void RegisterSequence(string name, string keySequence, System.Action callback)
        {
            _sequences[name] = callback;
        }

        /// <summary>
        /// Enable or disable secret input detection.
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            _enableSecrets = enabled;
            if (!enabled)
            {
                _inputBuffer.Clear();
            }
        }
    }
}
