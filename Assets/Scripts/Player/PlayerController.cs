using UnityEngine;
using Shredsquatch.Core;
using Shredsquatch.Tricks;

namespace Shredsquatch.Player
{
    [RequireComponent(typeof(PlayerInput))]
    [RequireComponent(typeof(SnowboardPhysics))]
    [RequireComponent(typeof(JumpController))]
    [RequireComponent(typeof(CrashHandler))]
    public class PlayerController : MonoBehaviour, IRecoverable
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

        private Vector3 _lastSafePosition;
        private Quaternion _lastSafeRotation;

        // Where a run begins. Defaults to the scene placement; SceneInitializer moves it
        // onto the generated terrain surface.
        private Vector3 _spawnPosition;
        private Quaternion _spawnRotation = Quaternion.identity;

        public Vector3 SpawnPosition => _spawnPosition;
        public Quaternion SpawnRotation => _spawnRotation;

        private void Awake()
        {
            // Get components if not assigned
            if (_input == null) _input = GetComponent<PlayerInput>();
            if (_physics == null) _physics = GetComponent<SnowboardPhysics>();
            if (_jumpController == null) _jumpController = GetComponent<JumpController>();
            if (_crashHandler == null) _crashHandler = GetComponent<CrashHandler>();
            if (_trickController == null) _trickController = GetComponent<TrickController>();
            if (_railController == null) _railController = GetComponent<RailGrindController>();

            _spawnPosition = transform.position;
            _spawnRotation = transform.rotation;
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

            // Register with error recovery system
            if (ErrorRecoveryManager.Instance != null)
            {
                ErrorRecoveryManager.Instance.RegisterRecoverable(this);
            }

            // Subscribe to events
            if (_physics != null) _physics.OnCrash += OnCrash;
            // Note: TrickController subscribes directly to JumpController.OnLand

            // Store initial safe position
            _lastSafePosition = transform.position;
            _lastSafeRotation = transform.rotation;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged -= HandleGameStateChanged;
                GameManager.Instance.OnRunStarted -= OnRunStarted;
            }

            if (ErrorRecoveryManager.Instance != null)
            {
                ErrorRecoveryManager.Instance.UnregisterRecoverable(this);
            }

            if (_physics != null) _physics.OnCrash -= OnCrash;
        }

        private void Update()
        {
            // Pause input must be read while paused too, otherwise the game can never resume
            HandlePause();

            if (!_isActive) return;

            UpdateEffects();
            UpdateSafePosition();
        }

        private void UpdateSafePosition()
        {
            // Store last known good position when grounded and not in ragdoll
            if (_physics != null && _physics.IsGrounded &&
                _crashHandler != null && !_crashHandler.IsInRagdoll)
            {
                _lastSafePosition = transform.position;
                _lastSafeRotation = transform.rotation;
            }
        }

        private void HandlePause()
        {
            if (_input == null || GameManager.Instance == null) return;

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

        private void ResetPlayer()
        {
            // Back to the spawn point (never the world origin, which may be under the terrain)
            TeleportTo(_spawnPosition, _spawnRotation);
        }

        /// <summary>
        /// Define where runs start. Also becomes the current "last safe" position.
        /// </summary>
        public void SetSpawnPoint(Vector3 position, Quaternion rotation)
        {
            _spawnPosition = position;
            _spawnRotation = rotation;
            _lastSafePosition = position;
            _lastSafeRotation = rotation;
        }

        /// <summary>
        /// Move the rider instantly. The CharacterController is toggled so the
        /// teleport is not swallowed by its internal position, and motion is cleared.
        /// </summary>
        public void TeleportTo(Vector3 position, Quaternion rotation)
        {
            var controller = GetComponent<CharacterController>();
            bool controllerWasEnabled = controller != null && controller.enabled;
            if (controllerWasEnabled) controller.enabled = false;

            transform.SetPositionAndRotation(position, rotation);

            if (controllerWasEnabled) controller.enabled = true;

            if (_physics != null)
            {
                _physics.ResetMotion();
            }
        }

        // Public getters for other systems
        public float GetCurrentSpeed() => _physics?.CurrentSpeed ?? 0f;
        public float GetCurrentSpeedKmh() => _physics?.CurrentSpeedKmh ?? 0f;
        public bool IsGrounded() => _physics?.IsGrounded ?? true;
        public bool IsInRagdoll() => _crashHandler?.IsInRagdoll ?? false;
        public bool IsInvincible() => _crashHandler?.IsInvincible ?? false;
        public float GetAirTime() => _jumpController?.AirTime ?? 0f;

        /// <summary>
        /// IRecoverable implementation - reset player to a safe state.
        /// </summary>
        public void AttemptRecovery()
        {
            // Reset to last safe position
            TeleportTo(_lastSafePosition, _lastSafeRotation);

            // Reset physics state
            if (_physics != null)
            {
                _physics.SetSpeedAfterRecovery();
            }

            // End any active tricks/combos
            if (_trickController != null)
            {
                SafeExecution.Try(() => _trickController.EndCombo(false), "RecoveryEndCombo");
            }

            // Deactivate until game restarts
            _isActive = false;

            Debug.Log("[PlayerController] Recovery complete - player reset to safe state");
        }
    }
}
