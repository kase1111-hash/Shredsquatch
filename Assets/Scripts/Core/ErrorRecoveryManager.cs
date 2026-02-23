using UnityEngine;
using System;
using System.Collections.Generic;

namespace Shredsquatch.Core
{
    /// <summary>
    /// Global error handling and recovery system.
    /// Catches unhandled exceptions and attempts to recover gracefully.
    /// </summary>
    public class ErrorRecoveryManager : MonoBehaviour
    {
        public static ErrorRecoveryManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private int _maxErrorsBeforeReset = 5;
        [SerializeField] private float _errorWindowSeconds = 10f;
        [SerializeField] private bool _logToConsole = true;

        // Error tracking
        private Queue<float> _recentErrorTimes = new Queue<float>();
        private int _totalErrors;
        private bool _isRecovering;

        // Registered recoverable systems
        private List<IRecoverable> _recoverableSystems = new List<IRecoverable>();

        // Events
        public event Action<Exception> OnErrorCaught;
        public event Action OnRecoveryStarted;
        public event Action OnRecoveryComplete;
        public event Action OnFatalError;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Subscribe to Unity's error logging
            Application.logMessageReceived += HandleLogMessage;
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= HandleLogMessage;
        }

        private void HandleLogMessage(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error)
            {
                RecordError(condition, stackTrace, type == LogType.Exception);
            }
        }

        /// <summary>
        /// Register a system that can be recovered on error.
        /// </summary>
        public void RegisterRecoverable(IRecoverable recoverable)
        {
            if (!_recoverableSystems.Contains(recoverable))
            {
                _recoverableSystems.Add(recoverable);
            }
        }

        /// <summary>
        /// Unregister a recoverable system.
        /// </summary>
        public void UnregisterRecoverable(IRecoverable recoverable)
        {
            _recoverableSystems.Remove(recoverable);
        }

        /// <summary>
        /// Wrap an action in error handling. Returns true if action succeeded.
        /// </summary>
        public bool TryExecute(Action action, string context = null)
        {
            try
            {
                action?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                HandleException(ex, context);
                return false;
            }
        }

        /// <summary>
        /// Wrap a function in error handling. Returns default(T) on failure.
        /// </summary>
        public T TryExecute<T>(Func<T> func, T fallback = default, string context = null)
        {
            try
            {
                return func != null ? func() : fallback;
            }
            catch (Exception ex)
            {
                HandleException(ex, context);
                return fallback;
            }
        }

        /// <summary>
        /// Handle an exception with logging and recovery attempt.
        /// </summary>
        public void HandleException(Exception ex, string context = null)
        {
            string message = string.IsNullOrEmpty(context)
                ? ex.Message
                : $"[{context}] {ex.Message}";

            if (_logToConsole)
            {
                Debug.LogWarning($"[ErrorRecovery] Caught exception: {message}\n{ex.StackTrace}");
            }

            RecordError(message, ex.StackTrace, true);
            OnErrorCaught?.Invoke(ex);
        }

        private void RecordError(string message, string stackTrace, bool isException)
        {
            float currentTime = Time.unscaledTime;
            _totalErrors++;

            // Track recent errors
            _recentErrorTimes.Enqueue(currentTime);

            // Remove old errors outside the window
            while (_recentErrorTimes.Count > 0 &&
                   currentTime - _recentErrorTimes.Peek() > _errorWindowSeconds)
            {
                _recentErrorTimes.Dequeue();
            }

            // Check if we've hit the error threshold
            if (_recentErrorTimes.Count >= _maxErrorsBeforeReset && !_isRecovering)
            {
                AttemptRecovery();
            }
        }

        /// <summary>
        /// Attempt to recover all registered systems.
        /// </summary>
        public void AttemptRecovery()
        {
            if (_isRecovering) return;

            _isRecovering = true;
            OnRecoveryStarted?.Invoke();

            if (_logToConsole)
            {
                Debug.LogWarning($"[ErrorRecovery] Too many errors ({_recentErrorTimes.Count} in {_errorWindowSeconds}s). Attempting recovery...");
            }

            bool allRecovered = true;

            // Try to recover each system
            foreach (var system in _recoverableSystems)
            {
                try
                {
                    if (system != null)
                    {
                        system.AttemptRecovery();
                        if (_logToConsole)
                        {
                            Debug.Log($"[ErrorRecovery] Recovered: {system.GetType().Name}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    allRecovered = false;
                    if (_logToConsole)
                    {
                        Debug.LogError($"[ErrorRecovery] Failed to recover {system?.GetType().Name}: {ex.Message}");
                    }
                }
            }

            // Clear error history after recovery attempt
            _recentErrorTimes.Clear();
            _isRecovering = false;

            if (allRecovered)
            {
                OnRecoveryComplete?.Invoke();
                if (_logToConsole)
                {
                    Debug.Log("[ErrorRecovery] Recovery complete.");
                }
            }
            else
            {
                OnFatalError?.Invoke();
                if (_logToConsole)
                {
                    Debug.LogError("[ErrorRecovery] Some systems failed to recover. Game may be unstable.");
                }
            }
        }

        /// <summary>
        /// Force a full game state reset (return to menu).
        /// </summary>
        // TODO: Add user-visible feedback when error recovery triggers — currently resets silently
        public void ForceReset()
        {
            if (_logToConsole)
            {
                Debug.LogWarning("[ErrorRecovery] Forcing full game reset...");
            }

            // Reset time scale in case it was frozen
            Time.timeScale = 1f;

            // Return to main menu
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ReturnToMenu();
            }

            // Clear error history
            _recentErrorTimes.Clear();
            _totalErrors = 0;
        }

        /// <summary>
        /// Get error statistics.
        /// </summary>
        public (int total, int recent) GetErrorStats()
        {
            return (_totalErrors, _recentErrorTimes.Count);
        }
    }

    /// <summary>
    /// Interface for systems that can recover from errors.
    /// </summary>
    public interface IRecoverable
    {
        /// <summary>
        /// Attempt to recover this system to a safe state.
        /// </summary>
        void AttemptRecovery();
    }
}
