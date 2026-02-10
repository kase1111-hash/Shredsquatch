using UnityEngine;
using Shredsquatch.Core;

namespace Shredsquatch.Player
{
    public class FirstPersonCamera : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _playerBody;
        [SerializeField] private Transform _cameraHolder;
        [SerializeField] private PlayerInput _input;

        [Header("Look Settings")]
        [SerializeField] private float _mouseSensitivity = 2f;
        [SerializeField] private float _controllerSensitivity = 100f;
        [SerializeField] private float _maxLookUp = 60f;
        [SerializeField] private float _maxLookDown = 80f;

        [Header("Smoothing")]
        [SerializeField] private float _smoothTime = 0.05f;
        [SerializeField] private bool _enableSmoothing = true;

        [Header("Camera Effects")]
        [SerializeField] private float _baseFOV = 75f;
        [SerializeField] private float _maxFOV = 100f;
        [SerializeField] private float _fovSpeedInfluence = 0.2f;
        [SerializeField] private float _tiltAmount = 5f;
        [SerializeField] private float _tiltSpeed = 3f;

        [Header("Shake")]
        [SerializeField] private float _shakeDecay = 5f;

        private Camera _camera;
        private float _xRotation;
        private float _yRotation;
        private Vector2 _smoothedInput;
        private Vector2 _inputVelocity;
        private float _currentTilt;
        private float _shakeIntensity;
        private float _targetFOV;

        private void Awake()
        {
            _camera = GetComponentInChildren<Camera>();

            // Wire references if not Inspector-assigned (procedural player)
            if (_playerBody == null) _playerBody = transform.parent;
            if (_cameraHolder == null) _cameraHolder = transform;
            if (_input == null) _input = GetComponentInParent<PlayerInput>();

            if (_camera != null)
            {
                _targetFOV = _baseFOV;
                _camera.fieldOfView = _baseFOV;
            }
        }

        private void Start()
        {
            // Lock cursor for first-person control
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged += HandleStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
            }
        }

        private void Update()
        {
            if (GameManager.Instance?.CurrentState != GameState.Playing)
                return;

            HandleLookInput();
            UpdateCameraEffects();
            UpdateShake();
        }

        private void LateUpdate()
        {
            ApplyCameraTransform();
        }

        private void HandleLookInput()
        {
            if (_input == null) return;

            Vector2 lookInput = _input.LookInput;

            // Determine sensitivity based on input type
            float sensitivity = Mathf.Abs(lookInput.x) > 0.5f || Mathf.Abs(lookInput.y) > 0.5f
                ? _controllerSensitivity * Time.deltaTime
                : _mouseSensitivity;

            Vector2 targetInput = lookInput * sensitivity;

            // Apply smoothing
            if (_enableSmoothing)
            {
                _smoothedInput = Vector2.SmoothDamp(
                    _smoothedInput,
                    targetInput,
                    ref _inputVelocity,
                    _smoothTime
                );
            }
            else
            {
                _smoothedInput = targetInput;
            }

            // Apply rotation
            _yRotation += _smoothedInput.x;
            _xRotation -= _smoothedInput.y;
            _xRotation = Mathf.Clamp(_xRotation, -_maxLookUp, _maxLookDown);
        }

        private void UpdateCameraEffects()
        {
            if (_camera == null) return;

            // Speed-based FOV
            var physics = _playerBody?.GetComponent<SnowboardPhysics>();
            if (physics != null)
            {
                float speedRatio = physics.CurrentSpeedKmh / 120f;
                _targetFOV = Mathf.Lerp(_baseFOV, _maxFOV, speedRatio * _fovSpeedInfluence);

                // Lean-based camera tilt
                float targetTilt = -physics.LeanAngle * (_tiltAmount / 45f);
                _currentTilt = Mathf.Lerp(_currentTilt, targetTilt, Time.deltaTime * _tiltSpeed);
            }

            _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, _targetFOV, Time.deltaTime * 5f);
        }

        private void UpdateShake()
        {
            if (_shakeIntensity > 0)
            {
                _shakeIntensity -= _shakeDecay * Time.deltaTime;
                _shakeIntensity = Mathf.Max(0, _shakeIntensity);
            }
        }

        private void ApplyCameraTransform()
        {
            if (_cameraHolder == null) return;

            // Base rotation
            Vector3 rotation = new Vector3(_xRotation, _yRotation, _currentTilt);

            // Add shake
            if (_shakeIntensity > 0)
            {
                rotation.x += Random.Range(-_shakeIntensity, _shakeIntensity);
                rotation.y += Random.Range(-_shakeIntensity, _shakeIntensity);
            }

            _cameraHolder.rotation = Quaternion.Euler(rotation);
        }

        public void AddShake(float intensity)
        {
            _shakeIntensity = Mathf.Max(_shakeIntensity, intensity);
        }

        public void SetSensitivity(float mouse, float controller)
        {
            _mouseSensitivity = mouse;
            _controllerSensitivity = controller;
        }

        private void HandleStateChanged(GameState state)
        {
            switch (state)
            {
                case GameState.Playing:
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    break;
                case GameState.Paused:
                case GameState.MainMenu:
                case GameState.GameOver:
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    break;
            }
        }

        public void ResetRotation()
        {
            _xRotation = 0f;
            _yRotation = 0f;
            _currentTilt = 0f;
        }
    }
}
