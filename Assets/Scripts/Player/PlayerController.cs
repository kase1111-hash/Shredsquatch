using UnityEngine;
using Shredsquatch.Core;
using Shredsquatch.Tricks;

namespace Shredsquatch.Player
{
    [RequireComponent(typeof(PlayerInput))]
    [RequireComponent(typeof(SnowboardPhysics))]
    [RequireComponent(typeof(JumpController))]
    [RequireComponent(typeof(CrashHandler))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private PlayerInput _input;
        [SerializeField] private SnowboardPhysics _physics;
        [SerializeField] private JumpController _jumpController;
        [SerializeField] private CrashHandler _crashHandler;
        [SerializeField] private TrickController _trickController;
        [SerializeField] private RailGrindController _railController;

        [Header("Visual")]
        [SerializeField] private GameObject _boardModel;
        [SerializeField] private ParticleSystem _snowSpray;
        [SerializeField] private ParticleSystem _boardTrail;

        [Header("Audio")]
        [SerializeField] private AudioSource _carveAudio;
        [SerializeField] private AudioSource _windAudio;

        // State
        private bool _isActive;

        private void Awake()
        {
            // Get components if not assigned
            if (_input == null) _input = GetComponent<PlayerInput>();
            if (_physics == null) _physics = GetComponent<SnowboardPhysics>();
            if (_jumpController == null) _jumpController = GetComponent<JumpController>();
            if (_crashHandler == null) _crashHandler = GetComponent<CrashHandler>();
            if (_trickController == null) _trickController = GetComponent<TrickController>();
            if (_railController == null) _railController = GetComponent<RailGrindController>();
        }

        private void Start()
        {
            // Register with GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetPlayerReference(transform);
                GameManager.Instance.OnStateChanged += HandleGameStateChanged;
                GameManager.Instance.OnRunStarted += OnRunStarted;
            }

            // Subscribe to events
            _physics.OnCrash += OnCrash;
            _jumpController.OnLand += OnLand;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged -= HandleGameStateChanged;
                GameManager.Instance.OnRunStarted -= OnRunStarted;
            }

            _physics.OnCrash -= OnCrash;
            _jumpController.OnLand -= OnLand;
        }

        private void Update()
        {
            if (!_isActive) return;

            HandlePause();
            UpdateEffects();
        }

        private void HandlePause()
        {
            if (_input.PausePressed)
            {
                if (GameManager.Instance.CurrentState == GameState.Playing)
                {
                    GameManager.Instance.PauseGame();
                }
                else if (GameManager.Instance.CurrentState == GameState.Paused)
                {
                    GameManager.Instance.ResumeGame();
                }
            }
        }

        private void UpdateEffects()
        {
            // Snow spray based on speed and ground contact
            if (_snowSpray != null)
            {
                var emission = _snowSpray.emission;
                if (_physics.IsGrounded && _physics.CurrentSpeedKmh > 20f)
                {
                    emission.enabled = true;
                    emission.rateOverTime = Mathf.Lerp(10f, 100f, _physics.CurrentSpeedKmh / 120f);
                }
                else
                {
                    emission.enabled = false;
                }
            }

            // Board trail
            if (_boardTrail != null)
            {
                var emission = _boardTrail.emission;
                emission.enabled = _physics.IsGrounded;
            }

            // Audio
            UpdateAudio();
        }

        private void UpdateAudio()
        {
            // Carve sound based on lean angle
            if (_carveAudio != null)
            {
                float leanIntensity = Mathf.Abs(_physics.LeanAngle) / Constants.Carving.CarveMaxAngle;
                _carveAudio.volume = _physics.IsGrounded ? leanIntensity * 0.5f : 0f;
                _carveAudio.pitch = 0.8f + leanIntensity * 0.4f;
            }

            // Wind sound based on speed
            if (_windAudio != null)
            {
                float speedRatio = _physics.CurrentSpeedKmh / 120f;
                _windAudio.volume = Mathf.Lerp(0.1f, 1f, speedRatio);
                _windAudio.pitch = 0.9f + speedRatio * 0.3f;
            }
        }

        private void HandleGameStateChanged(GameState newState)
        {
            _isActive = newState == GameState.Playing;
        }

        private void OnRunStarted()
        {
            _isActive = true;
            ResetPlayer();
        }

        private void OnCrash()
        {
            // Notify trick system to end combo
            if (_trickController != null)
            {
                _trickController.EndCombo(false);
            }
        }

        private void OnLand(float airTime, bool wasClean)
        {
            if (_trickController != null)
            {
                _trickController.OnLanding(wasClean);
            }
        }

        private void ResetPlayer()
        {
            // Reset position/rotation as needed
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
        }

        // Public getters for other systems
        public float GetCurrentSpeed() => _physics.CurrentSpeed;
        public float GetCurrentSpeedKmh() => _physics.CurrentSpeedKmh;
        public bool IsGrounded() => _physics.IsGrounded;
        public bool IsInRagdoll() => _crashHandler.IsInRagdoll;
        public bool IsInvincible() => _crashHandler.IsInvincible;
        public float GetAirTime() => _jumpController.AirTime;
    }
}
