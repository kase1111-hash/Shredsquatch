using UnityEngine;
using System.Collections.Generic;

namespace Shredsquatch.Core
{
    /// <summary>
    /// Monitors game performance. FPS tracking and debug overlay only.
    /// Dynamic quality scaling deferred until profiling data exists from real gameplay.
    /// </summary>
    public class PerformanceMonitor : MonoBehaviour
    {
        public static PerformanceMonitor Instance { get; private set; }

        [Header("Target Performance")]
        [SerializeField] private int _targetFPS = 60;
        [SerializeField] private int _minAcceptableFPS = 45;
        [SerializeField] private float _measureInterval = 2f;

        [Header("Debug Display")]
        [SerializeField] private bool _showDebugOverlay = false;

        // FPS tracking
        private float _fpsAccumulator;
        private int _fpsFrameCount;
        private float _currentFPS;
        private float _averageFPS;
        private float _minFPS = float.MaxValue;
        private float _maxFPS;
        private Queue<float> _fpsHistory = new Queue<float>();
        private const int FPSHistorySize = 30;

        // Quality level
        private int _currentQualityLevel;

        // Frame time tracking for stutters
        private float _lastFrameTime;
        private int _stutterCount;
        private const float StutterThreshold = 0.05f; // 50ms = 20fps

        // Run tracking
        private float _runStartTime;
        private float _totalDistance;

        // Events
        public event System.Action<int> OnQualityChanged;
        public event System.Action<float> OnFPSUpdate;
        public event System.Action OnPerformanceWarning;

        // Properties
        public float CurrentFPS => _currentFPS;
        public float AverageFPS => _averageFPS;
        public int CurrentQualityLevel => _currentQualityLevel;
        public int StutterCount => _stutterCount;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _currentQualityLevel = QualitySettings.GetQualityLevel();
            Application.targetFrameRate = _targetFPS;
        }

        private void Start()
        {
            _runStartTime = Time.time;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRunStarted += OnRunStarted;
                GameManager.Instance.OnDistanceChanged += OnDistanceChanged;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRunStarted -= OnRunStarted;
                GameManager.Instance.OnDistanceChanged -= OnDistanceChanged;
            }
        }

        private void Update()
        {
            TrackFrameRate();
            TrackStutters();
        }

        private void OnRunStarted()
        {
            ResetStats();
            _runStartTime = Time.time;
        }

        private void OnDistanceChanged(float distance)
        {
            _totalDistance = distance;
        }

        private void TrackFrameRate()
        {
            _fpsAccumulator += Time.unscaledDeltaTime;
            _fpsFrameCount++;

            if (_fpsAccumulator >= _measureInterval)
            {
                _currentFPS = _fpsFrameCount / _fpsAccumulator;

                // Update history
                _fpsHistory.Enqueue(_currentFPS);
                if (_fpsHistory.Count > FPSHistorySize)
                {
                    _fpsHistory.Dequeue();
                }

                // Calculate average
                float sum = 0f;
                foreach (float fps in _fpsHistory)
                {
                    sum += fps;
                }
                _averageFPS = sum / _fpsHistory.Count;

                // Track min/max
                if (_currentFPS < _minFPS) _minFPS = _currentFPS;
                if (_currentFPS > _maxFPS) _maxFPS = _currentFPS;

                OnFPSUpdate?.Invoke(_currentFPS);

                // Reset accumulator
                _fpsAccumulator = 0f;
                _fpsFrameCount = 0;
            }
        }

        private void TrackStutters()
        {
            float frameTime = Time.unscaledDeltaTime;

            if (frameTime > StutterThreshold && _lastFrameTime <= StutterThreshold)
            {
                _stutterCount++;

                if (_stutterCount % 10 == 0)
                {
                    Debug.LogWarning($"[Performance] {_stutterCount} stutters detected this run");
                }
            }

            _lastFrameTime = frameTime;
        }

        /// <summary>
        /// Force quality reduction (called by WebGLCompatibility on low memory).
        /// </summary>
        public void ForceQualityReduction()
        {
            if (_currentQualityLevel > 0)
            {
                _currentQualityLevel = 0;
                QualitySettings.SetQualityLevel(0, true);
                OnQualityChanged?.Invoke(0);
                OnPerformanceWarning?.Invoke();
                Debug.LogWarning("[Performance] Forced quality to Low due to memory pressure");
            }
        }

        private void ResetStats()
        {
            _minFPS = float.MaxValue;
            _maxFPS = 0f;
            _stutterCount = 0;
            _fpsHistory.Clear();
        }

        /// <summary>
        /// Get a performance report string for debugging/display.
        /// </summary>
        public string GetPerformanceReport()
        {
            float runTime = Time.time - _runStartTime;
            return $"FPS: {_currentFPS:F1} (Avg: {_averageFPS:F1}, Min: {_minFPS:F1}, Max: {_maxFPS:F1})\n" +
                   $"Quality: {QualitySettings.names[_currentQualityLevel]}\n" +
                   $"Stutters: {_stutterCount}\n" +
                   $"Memory: {System.GC.GetTotalMemory(false) / (1024f * 1024f):F1}MB\n" +
                   $"Run Time: {runTime:F0}s, Distance: {_totalDistance:F2}km";
        }

        private GUIStyle _debugStyle;

        private void OnGUI()
        {
            if (!_showDebugOverlay) return;

            if (_debugStyle == null)
            {
                _debugStyle = new GUIStyle(GUI.skin.label);
                _debugStyle.fontSize = 14;
            }
            _debugStyle.normal.textColor = _currentFPS >= _minAcceptableFPS ? Color.green : Color.red;

            GUI.Label(new Rect(10, 10, 300, 150), GetPerformanceReport(), _debugStyle);
        }

        /// <summary>
        /// Toggle debug overlay visibility.
        /// </summary>
        public void ToggleDebugOverlay()
        {
            _showDebugOverlay = !_showDebugOverlay;
        }
    }
}
