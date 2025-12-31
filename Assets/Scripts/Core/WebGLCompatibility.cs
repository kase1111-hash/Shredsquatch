using UnityEngine;
using System.Runtime.InteropServices;

namespace Shredsquatch.Core
{
    /// <summary>
    /// Handles WebGL browser detection, compatibility checks, and platform-specific optimizations.
    /// </summary>
    public class WebGLCompatibility : MonoBehaviour
    {
        public static WebGLCompatibility Instance { get; private set; }

        [Header("Compatibility Settings")]
        [SerializeField] private bool _autoDetectBrowser = true;
        [SerializeField] private bool _showWarnings = true;

        // Browser detection
        public BrowserType DetectedBrowser { get; private set; } = BrowserType.Unknown;
        public bool IsWebGL { get; private set; }
        public bool IsMobile { get; private set; }
        public bool HasWebGL2 { get; private set; } = true;

        // Events
        public event System.Action<BrowserType> OnBrowserDetected;
        public event System.Action<string> OnCompatibilityWarning;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern string GetBrowserInfo();

        [DllImport("__Internal")]
        private static extern bool CheckWebGL2Support();

        [DllImport("__Internal")]
        private static extern bool IsMobileDevice();

        [DllImport("__Internal")]
        private static extern void SetFullscreen(bool fullscreen);
#endif

        public enum BrowserType
        {
            Unknown,
            Chrome,
            Firefox,
            Safari,
            Edge,
            Opera,
            Other
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

#if UNITY_WEBGL && !UNITY_EDITOR
            IsWebGL = true;
#else
            IsWebGL = false;
#endif

            if (_autoDetectBrowser)
            {
                DetectBrowser();
            }

            ApplyPlatformOptimizations();
        }

        private void Start()
        {
            CheckCompatibility();
        }

        private void DetectBrowser()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                string browserInfo = GetBrowserInfo();
                DetectedBrowser = ParseBrowserString(browserInfo);
                IsMobile = IsMobileDevice();
                HasWebGL2 = CheckWebGL2Support();
            }
            catch
            {
                // JavaScript interop failed, use fallback
                DetectedBrowser = BrowserType.Unknown;
            }
#else
            // In editor, simulate Chrome for testing
            DetectedBrowser = BrowserType.Chrome;
#endif

            OnBrowserDetected?.Invoke(DetectedBrowser);
            Debug.Log($"[WebGL] Browser detected: {DetectedBrowser}, Mobile: {IsMobile}, WebGL2: {HasWebGL2}");
        }

        private BrowserType ParseBrowserString(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return BrowserType.Unknown;

            userAgent = userAgent.ToLower();

            // Order matters - Edge contains Chrome, Chrome contains Safari
            if (userAgent.Contains("edg/") || userAgent.Contains("edge/"))
                return BrowserType.Edge;
            if (userAgent.Contains("opr/") || userAgent.Contains("opera"))
                return BrowserType.Opera;
            if (userAgent.Contains("firefox"))
                return BrowserType.Firefox;
            if (userAgent.Contains("chrome"))
                return BrowserType.Chrome;
            if (userAgent.Contains("safari"))
                return BrowserType.Safari;

            return BrowserType.Other;
        }

        private void CheckCompatibility()
        {
            if (!IsWebGL) return;

            // Safari-specific warnings
            if (DetectedBrowser == BrowserType.Safari)
            {
                WarnUser("Safari may have reduced performance. Chrome or Firefox recommended for best experience.");
            }

            // WebGL2 check
            if (!HasWebGL2)
            {
                WarnUser("WebGL 2.0 not supported. Some visual effects may be disabled.");
                ApplyWebGL1Fallback();
            }

            // Mobile warning
            if (IsMobile)
            {
                WarnUser("Mobile browser detected. For best experience, use a desktop browser.");
                ApplyMobileOptimizations();
            }

            // Memory check (WebGL has limited memory)
            CheckMemoryConstraints();
        }

        private void ApplyPlatformOptimizations()
        {
            if (!IsWebGL) return;

            // WebGL-specific settings
            Application.targetFrameRate = 60;

            // Reduce GC pressure
            Application.lowMemory += OnLowMemory;

            // Browser-specific optimizations
            switch (DetectedBrowser)
            {
                case BrowserType.Safari:
                    // Safari has issues with certain WebGL features
                    ApplySafariOptimizations();
                    break;

                case BrowserType.Firefox:
                    // Firefox handles memory differently
                    ApplyFirefoxOptimizations();
                    break;

                case BrowserType.Chrome:
                case BrowserType.Edge:
                    // Chromium-based browsers - optimal settings
                    ApplyChromiumOptimizations();
                    break;
            }
        }

        private void ApplySafariOptimizations()
        {
            // Safari has stricter memory limits and some WebGL quirks
            QualitySettings.shadowDistance = 50f;
            QualitySettings.shadowCascades = 1;

            // Reduce particle counts
            Debug.Log("[WebGL] Applied Safari optimizations");
        }

        private void ApplyFirefoxOptimizations()
        {
            // Firefox handles async operations differently
            QualitySettings.asyncUploadTimeSlice = 4;
            Debug.Log("[WebGL] Applied Firefox optimizations");
        }

        private void ApplyChromiumOptimizations()
        {
            // Chrome/Edge can handle more
            Debug.Log("[WebGL] Applied Chromium optimizations");
        }

        private void ApplyWebGL1Fallback()
        {
            // Disable features that require WebGL2
            QualitySettings.SetQualityLevel(0); // Low quality
            Debug.Log("[WebGL] Applied WebGL 1.0 fallback settings");
        }

        private void ApplyMobileOptimizations()
        {
            // Aggressive quality reduction for mobile browsers
            QualitySettings.SetQualityLevel(0); // Low quality
            QualitySettings.shadowDistance = 30f;
            QualitySettings.antiAliasing = 0;

            // Mobile-friendly touch areas would be configured here
            Debug.Log("[WebGL] Applied mobile browser optimizations");
        }

        private void CheckMemoryConstraints()
        {
            // WebGL typically has ~2GB limit, but varies by browser
            // Log current estimated usage
#if UNITY_WEBGL && !UNITY_EDITOR
            Debug.Log($"[WebGL] Total memory: {SystemInfo.systemMemorySize}MB");
#endif
        }

        private void OnLowMemory()
        {
            Debug.LogWarning("[WebGL] Low memory warning - forcing garbage collection");

            // Force cleanup
            Resources.UnloadUnusedAssets();
            System.GC.Collect();

            // Notify performance monitor to reduce quality
            if (PerformanceMonitor.Instance != null)
            {
                PerformanceMonitor.Instance.ForceQualityReduction();
            }
        }

        private void WarnUser(string message)
        {
            if (!_showWarnings) return;

            Debug.LogWarning($"[WebGL Compatibility] {message}");
            OnCompatibilityWarning?.Invoke(message);
        }

        /// <summary>
        /// Request fullscreen mode (WebGL only).
        /// </summary>
        public void RequestFullscreen()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            SetFullscreen(true);
#else
            Screen.fullScreen = true;
#endif
        }

        /// <summary>
        /// Exit fullscreen mode.
        /// </summary>
        public void ExitFullscreen()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            SetFullscreen(false);
#else
            Screen.fullScreen = false;
#endif
        }

        /// <summary>
        /// Get browser-specific performance recommendations.
        /// </summary>
        public string GetPerformanceTips()
        {
            return DetectedBrowser switch
            {
                BrowserType.Safari => "Tip: Close other Safari tabs for better performance.",
                BrowserType.Firefox => "Tip: Enable hardware acceleration in Firefox settings.",
                BrowserType.Chrome => "Tip: Close unused Chrome extensions for smoother gameplay.",
                BrowserType.Edge => "Tip: Use Edge's Gaming Mode for optimal performance.",
                _ => "Tip: Use Chrome or Firefox for the best experience."
            };
        }
    }
}
