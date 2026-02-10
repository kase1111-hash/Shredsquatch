using UnityEngine;
using UnityEngine.InputSystem;
using Shredsquatch.Player;
using Shredsquatch.Sasquatch;

namespace Shredsquatch.Core
{
    /// <summary>
    /// Centralized system for game feedback effects including screen shake and haptic/rumble feedback.
    /// </summary>
    public class GameFeedback : MonoBehaviour
    {
        public static GameFeedback Instance { get; private set; }

        [Header("Screen Shake Settings")]
        [SerializeField] private float _crashShakeIntensity = 3f;
        [SerializeField] private float _nearMissShakeIntensity = 1f;
        [SerializeField] private float _sasquatchCloseShakeIntensity = 0.5f;
        [SerializeField] private float _landingShakeIntensity = 0.8f;

        [Header("Haptic Settings")]
        [SerializeField] private bool _hapticsEnabled = true;
        [SerializeField] private float _crashRumbleDuration = 0.5f;
        [SerializeField] private float _nearMissRumbleDuration = 0.15f;
        [SerializeField] private float _trickLandRumbleDuration = 0.1f;

        [Header("References")]
        [SerializeField] private FirstPersonCamera _camera;

        // Haptic state
        private Gamepad _activeGamepad;
        private float _rumbleTimer;
        private float _lowFreqTarget;
        private float _highFreqTarget;

        // Tracked references for proper unsubscription
        private CrashHandler _crashHandler;
        private SasquatchAI _sasquatch;

        // Near-miss tracking
        private float _lastNearMissTime;
        private const float NearMissCooldown = 0.5f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Subscribe to GameManager events (always available via AutoSetup)
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRunStarted += OnRunStarted;
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            StopAllRumble();
        }

        /// <summary>
        /// Wire to the player's camera for screen shake. Called by SceneInitializer after spawn.
        /// </summary>
        public void SetCamera(FirstPersonCamera cam)
        {
            _camera = cam;
        }

        /// <summary>
        /// Wire to the player's CrashHandler for crash/recovery feedback. Called by SceneInitializer.
        /// </summary>
        public void SetCrashHandler(CrashHandler handler)
        {
            // Unsub from previous if any
            if (_crashHandler != null)
            {
                _crashHandler.OnRagdollStart -= OnPlayerCrash;
                _crashHandler.OnRecoveryComplete -= OnPlayerRecovery;
            }

            _crashHandler = handler;

            if (_crashHandler != null)
            {
                _crashHandler.OnRagdollStart += OnPlayerCrash;
                _crashHandler.OnRecoveryComplete += OnPlayerRecovery;
            }
        }

        /// <summary>
        /// Wire to the Sasquatch for proximity/catch feedback. Called by SceneInitializer.
        /// </summary>
        public void SetSasquatch(SasquatchAI sasquatch)
        {
            // Unsub from previous if any
            if (_sasquatch != null)
            {
                _sasquatch.OnDistanceChanged -= OnSasquatchDistanceChanged;
                _sasquatch.OnCatchPlayer -= OnPlayerCaught;
            }

            _sasquatch = sasquatch;

            if (_sasquatch != null)
            {
                _sasquatch.OnDistanceChanged += OnSasquatchDistanceChanged;
                _sasquatch.OnCatchPlayer += OnPlayerCaught;
            }
        }

        private void Update()
        {
            UpdateGamepad();
            UpdateRumble();
        }

        private void UpdateGamepad()
        {
            _activeGamepad = Gamepad.current;
        }

        private void UpdateRumble()
        {
            if (_rumbleTimer > 0)
            {
                _rumbleTimer -= Time.unscaledDeltaTime;

                if (_rumbleTimer <= 0)
                {
                    StopRumble();
                }
            }
        }

        private void UnsubscribeFromEvents()
        {
            SetCrashHandler(null);
            SetSasquatch(null);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRunStarted -= OnRunStarted;
            }
        }

        private void OnRunStarted()
        {
            StopAllRumble();
        }

        private void OnPlayerCrash()
        {
            // Strong screen shake
            TriggerScreenShake(_crashShakeIntensity);

            // Strong rumble - both motors
            TriggerRumble(0.8f, 1f, _crashRumbleDuration);
        }

        private void OnPlayerRecovery()
        {
            // Light feedback on getting back up
            TriggerRumble(0.2f, 0f, 0.1f);
        }

        private void OnSasquatchDistanceChanged(float distance)
        {
            // Continuous low rumble when Sasquatch is close
            if (distance < 150f)
            {
                // Intensity increases as Sasquatch gets closer
                float intensity = Mathf.InverseLerp(150f, 30f, distance);

                // Subtle continuous shake
                if (_camera != null)
                {
                    _camera.AddShake(_sasquatchCloseShakeIntensity * intensity);
                }

                // Low frequency rumble that intensifies
                if (_hapticsEnabled && _activeGamepad != null)
                {
                    float lowFreq = Mathf.Lerp(0.1f, 0.5f, intensity);
                    _activeGamepad.SetMotorSpeeds(lowFreq, 0f);
                }
            }
            else if (distance < 300f)
            {
                // Occasional pulse when in danger zone
                // Handled by proximity pulsing in HUD
            }
        }

        private void OnPlayerCaught()
        {
            // Maximum feedback on death
            TriggerScreenShake(_crashShakeIntensity * 2f);
            TriggerRumble(1f, 1f, 1f);
        }

        /// <summary>
        /// Call when player barely misses an obstacle (grazing).
        /// </summary>
        public void TriggerNearMiss()
        {
            // Cooldown to prevent spam
            if (Time.time - _lastNearMissTime < NearMissCooldown)
                return;

            _lastNearMissTime = Time.time;

            TriggerScreenShake(_nearMissShakeIntensity);
            TriggerRumble(0.3f, 0.5f, _nearMissRumbleDuration);
        }

        /// <summary>
        /// Call when player lands a trick successfully.
        /// </summary>
        public void TriggerTrickLand(int points)
        {
            // Scale feedback with points
            float intensity = Mathf.Clamp01(points / 5000f);

            TriggerScreenShake(_landingShakeIntensity * (0.5f + intensity * 0.5f));
            TriggerRumble(0.2f + intensity * 0.3f, 0.1f + intensity * 0.2f, _trickLandRumbleDuration);
        }

        /// <summary>
        /// Call when player hits a speed boost or powerup.
        /// </summary>
        public void TriggerBoost()
        {
            TriggerRumble(0.4f, 0.6f, 0.2f);
        }

        /// <summary>
        /// Call when player lands from a jump (non-trick).
        /// </summary>
        public void TriggerLanding(float impactStrength)
        {
            float shake = _landingShakeIntensity * impactStrength;
            TriggerScreenShake(shake);
            TriggerRumble(impactStrength * 0.3f, 0f, 0.1f);
        }

        /// <summary>
        /// Triggers screen shake effect.
        /// </summary>
        public void TriggerScreenShake(float intensity)
        {
            if (_camera != null)
            {
                _camera.AddShake(intensity);
            }
        }

        /// <summary>
        /// Triggers controller rumble/haptic feedback.
        /// </summary>
        /// <param name="lowFrequency">Left motor (0-1), heavy/slow rumble</param>
        /// <param name="highFrequency">Right motor (0-1), light/fast rumble</param>
        /// <param name="duration">Duration in seconds</param>
        public void TriggerRumble(float lowFrequency, float highFrequency, float duration)
        {
            if (!_hapticsEnabled) return;
            if (_activeGamepad == null) return;

            _lowFreqTarget = lowFrequency;
            _highFreqTarget = highFrequency;
            _rumbleTimer = duration;

            _activeGamepad.SetMotorSpeeds(lowFrequency, highFrequency);
        }

        private void StopRumble()
        {
            if (_activeGamepad != null)
            {
                _activeGamepad.SetMotorSpeeds(0f, 0f);
            }
            _lowFreqTarget = 0f;
            _highFreqTarget = 0f;
        }

        private void StopAllRumble()
        {
            _rumbleTimer = 0f;
            StopRumble();
        }

        public void SetHapticsEnabled(bool enabled)
        {
            _hapticsEnabled = enabled;
            if (!enabled)
            {
                StopAllRumble();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                StopAllRumble();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                StopAllRumble();
            }
        }
    }
}
