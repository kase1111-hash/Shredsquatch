using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace Shredsquatch.Core
{
    /// <summary>
    /// Monitors game performance and dynamically adjusts quality settings to maintain target FPS.
    /// Optimized for long runs with memory management and quality scaling.
    /// </summary>
    public class PerformanceMonitor : MonoBehaviour
    {
        public static PerformanceMonitor Instance { get; private set; }

        [Header("Target Performance")]
        [SerializeField] private int _targetFPS = 60;
        [SerializeField] private int _minAcceptableFPS = 45;
        [SerializeField] private float _measureInterval = 2f;

        [Header("Auto Quality Settings")]
        [SerializeField] private bool _autoAdjustQuality = true;
        [SerializeField] private float _qualityAdjustCooldown = 10f;
        [SerializeField] private int _framesBeforeAdjust = 120; // ~2 seconds at 60fps

        [Header("Memory Management")]
        [SerializeField] private float _memoryCheckInterval = 30f;
        [SerializeField] private float _gcTriggerThreshold = 0.8f; // 80% of budget

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

        // Quality adjustment
        private int _currentQualityLevel;
        private float _lastQualityAdjustTime;
        private int _lowFPSFrameCount;
        private int _highFPSFrameCount;

        // Memory tracking
        private float _lastMemoryCheckTime;
        private float _lastGCTime;
        private long _peakMemoryUsage;

        // Frame time tracking for stutters
        private float _lastFrameTime;
        private int _stutterCount;
        private const float StutterThreshold = 0.05f; // 50ms = 20fps

        // Long run optimizations
        private float _runStartTime;
        private float _totalDistance;
        private bool _hasReducedForLongRun;

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
            CheckMemory();

            if (_autoAdjustQuality)
            {
                EvaluateQualityAdjustment();
            }
        }

        private void OnRunStarted()
        {
            ResetStats();
            _runStartTime = Time.time;
            _hasReducedForLongRun = false;
        }

        private void OnDistanceChanged(float distance)
        {
            _totalDistance = distance;

            // Long run optimizations kick in after 10km
            if (distance > 10f && !_hasReducedForLongRun)
            {
                ApplyLongRunOptimizations();
            }
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

        private void CheckMemory()
        {
            if (Time.time - _lastMemoryCheckTime < _memoryCheckInterval)
                return;

            _lastMemoryCheckTime = Time.time;

            // Track memory usage
            long currentMemory = System.GC.GetTotalMemory(false);
            if (currentMemory > _peakMemoryUsage)
            {
                _peakMemoryUsage = currentMemory;
            }

            // Estimate memory pressure (Unity doesn't expose exact budgets)
            float memoryMB = currentMemory / (1024f * 1024f);

            // Trigger cleanup if we're using too much
            // WebGL typically has ~2GB limit, trigger cleanup at lower threshold
            float threshold = Application.platform == RuntimePlatform.WebGLPlayer ? 800f : 2000f;

            if (memoryMB > threshold * _gcTriggerThreshold)
            {
                TriggerMemoryCleanup();
            }

            // Log memory stats periodically
            Debug.Log($"[Performance] Memory: {memoryMB:F1}MB, Peak: {_peakMemoryUsage / (1024f * 1024f):F1}MB");
        }

        private void EvaluateQualityAdjustment()
        {
            if (Time.time - _lastQualityAdjustTime < _qualityAdjustCooldown)
                return;

            // Track consecutive low/high FPS frames
            if (_currentFPS < _minAcceptableFPS)
            {
                _lowFPSFrameCount++;
                _highFPSFrameCount = 0;
            }
            else if (_currentFPS >= _targetFPS)
            {
                _highFPSFrameCount++;
                _lowFPSFrameCount = 0;
            }
            else
            {
                // Reset both if in acceptable range
                _lowFPSFrameCount = Mathf.Max(0, _lowFPSFrameCount - 1);
                _highFPSFrameCount = Mathf.Max(0, _highFPSFrameCount - 1);
            }

            // Reduce quality if sustained low FPS
            if (_lowFPSFrameCount >= _framesBeforeAdjust)
            {
                ReduceQuality();
                _lowFPSFrameCount = 0;
            }
            // Increase quality if sustained high FPS (and we're not at max)
            else if (_highFPSFrameCount >= _framesBeforeAdjust * 2) // Slower to increase
            {
                IncreaseQuality();
                _highFPSFrameCount = 0;
            }
        }

        private void ReduceQuality()
        {
            int newLevel = _currentQualityLevel - 1;
            if (newLevel < 0) return;

            SetQualityLevel(newLevel);
            Debug.Log($"[Performance] Reduced quality to {QualitySettings.names[newLevel]} due to low FPS ({_currentFPS:F1})");
            OnPerformanceWarning?.Invoke();
        }

        private void IncreaseQuality()
        {
            int maxLevel = QualitySettings.names.Length - 1;
            int newLevel = _currentQualityLevel + 1;
            if (newLevel > maxLevel) return;

            SetQualityLevel(newLevel);
            Debug.Log($"[Performance] Increased quality to {QualitySettings.names[newLevel]} due to good FPS ({_currentFPS:F1})");
        }

        private void SetQualityLevel(int level)
        {
            _currentQualityLevel = level;
            QualitySettings.SetQualityLevel(level, true);
            _lastQualityAdjustTime = Time.time;
            OnQualityChanged?.Invoke(level);
        }

        /// <summary>
        /// Force quality reduction (called by WebGL on low memory).
        /// </summary>
        public void ForceQualityReduction()
        {
            if (_currentQualityLevel > 0)
            {
                SetQualityLevel(0);
                Debug.LogWarning("[Performance] Forced quality to Low due to memory pressure");
            }
        }

        private void ApplyLongRunOptimizations()
        {
            _hasReducedForLongRun = true;

            // Reduce shadow distance for long runs (less terrain to shadow)
            QualitySettings.shadowDistance = Mathf.Min(QualitySettings.shadowDistance, 50f);

            // More aggressive LOD bias
            QualitySettings.lodBias = Mathf.Min(QualitySettings.lodBias, 0.7f);

            // Trigger cleanup
            TriggerMemoryCleanup();

            Debug.Log("[Performance] Applied long run optimizations (10km+)");
        }

        private void TriggerMemoryCleanup()
        {
            if (Time.time - _lastGCTime < 30f) return; // Don't GC too often

            _lastGCTime = Time.time;

            // Unload unused assets
            Resources.UnloadUnusedAssets();

            // Force garbage collection (do this sparingly - causes frame spike)
            System.GC.Collect();

            Debug.Log("[Performance] Memory cleanup performed");
        }

        private void ResetStats()
        {
            _minFPS = float.MaxValue;
            _maxFPS = 0f;
            _stutterCount = 0;
            _fpsHistory.Clear();
            _lowFPSFrameCount = 0;
            _highFPSFrameCount = 0;
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

        private void OnGUI()
        {
            if (!_showDebugOverlay) return;

            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 14;
            style.normal.textColor = _currentFPS >= _minAcceptableFPS ? Color.green : Color.red;

            GUI.Label(new Rect(10, 10, 300, 150), GetPerformanceReport(), style);
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
