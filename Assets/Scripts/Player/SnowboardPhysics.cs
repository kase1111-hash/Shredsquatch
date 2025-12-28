using UnityEngine;
using Shredsquatch.Core;

namespace Shredsquatch.Player
{
    public class SnowboardPhysics : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CharacterController _controller;
        [SerializeField] private PlayerInput _input;
        [SerializeField] private Transform _boardTransform;

        [Header("Movement Settings")]
        [SerializeField] private float _gravity = 20f;
        [SerializeField] private float _slopeAcceleration = 15f;
        [SerializeField] private float _maxSpeed = 120f;
        [SerializeField] private float _turnSpeed = 100f;
        [SerializeField] private float _tuckSpeedBonus = 1.5f;
        [SerializeField] private float _brakeStrength = 0.5f;

        [Header("Ground Detection")]
        [SerializeField] private float _groundCheckDistance = 0.5f;
        [SerializeField] private LayerMask _groundMask;

        // State
        private Vector3 _velocity;
        private float _currentSpeed;
        private float _currentLeanAngle;
        private bool _isGrounded;
        private bool _isInPowder;
        private Vector3 _groundNormal = Vector3.up;
        private float _carveBoostAccumulator;

        // Public properties
        public float CurrentSpeed => _currentSpeed;
        public float CurrentSpeedKmh => _currentSpeed * 3.6f;
        public bool IsGrounded => _isGrounded;
        public bool IsInPowder => _isInPowder;
        public float LeanAngle => _currentLeanAngle;
        public Vector3 Velocity => _velocity;

        // Events
        public event System.Action OnCrash;
        public event System.Action OnEdgeCatch;

        private void Update()
        {
            if (GameManager.Instance?.CurrentState != GameState.Playing)
                return;

            CheckGround();
            HandleMovement();
            HandleCarving();
            ApplyGravity();
            MovePlayer();

            // Update max speed tracking
            GameManager.Instance?.UpdateMaxSpeed(CurrentSpeedKmh);
        }

        private void CheckGround()
        {
            RaycastHit hit;
            _isGrounded = Physics.Raycast(
                transform.position + Vector3.up * 0.1f,
                Vector3.down,
                out hit,
                _groundCheckDistance + 0.1f,
                _groundMask
            );

            if (_isGrounded)
            {
                _groundNormal = hit.normal;

                // Check for powder (could be a tag or layer)
                _isInPowder = hit.collider.CompareTag("Powder");
            }
            else
            {
                _groundNormal = Vector3.up;
            }
        }

        private void HandleMovement()
        {
            if (!_isGrounded) return;

            // Calculate slope influence
            float slopeAngle = Vector3.Angle(_groundNormal, Vector3.up);
            Vector3 slopeDirection = Vector3.Cross(Vector3.Cross(Vector3.up, _groundNormal), _groundNormal).normalized;

            // Base acceleration from slope
            float slopeAccel = slopeAngle * _slopeAcceleration * 0.01f;

            // Apply tuck bonus
            if (_input.IsTucking)
            {
                slopeAccel *= _tuckSpeedBonus;
            }

            // Apply brake
            if (_input.IsBraking)
            {
                slopeAccel -= _brakeStrength * _currentSpeed;
            }

            // Apply powder drag
            if (_isInPowder)
            {
                slopeAccel *= Constants.Speed.PowderDrag;
            }

            // Update speed
            _currentSpeed += slopeAccel * Time.deltaTime;
            _currentSpeed = Mathf.Clamp(_currentSpeed, 0, _maxSpeed);

            // Determine target speed range based on state
            float targetMaxSpeed = _input.IsTucking ? Constants.Speed.TuckMax : Constants.Speed.Cruise;
            if (_currentSpeed > targetMaxSpeed && !_input.IsTucking)
            {
                _currentSpeed = Mathf.Lerp(_currentSpeed, targetMaxSpeed, Time.deltaTime * 0.5f);
            }
        }

        private void HandleCarving()
        {
            if (!_isGrounded) return;

            float steerInput = _input.SteerInput;

            // Update lean angle
            float targetLean = steerInput * Constants.Carving.CarveMaxAngle;
            _currentLeanAngle = Mathf.Lerp(_currentLeanAngle, targetLean, Time.deltaTime * 5f);

            // Apply turn
            float turnAmount = steerInput * _turnSpeed * Time.deltaTime;
            transform.Rotate(0, turnAmount, 0);

            // Rotate board visual for lean
            if (_boardTransform != null)
            {
                _boardTransform.localRotation = Quaternion.Euler(0, 0, -_currentLeanAngle);
            }

            // Check for edge catch
            float absLean = Mathf.Abs(_currentLeanAngle);
            if (absLean > Constants.Carving.EdgeCatchAngle)
            {
                TriggerEdgeCatch();
                return;
            }

            // Apply carve boost
            if (absLean >= Constants.Carving.CarveMinAngle && absLean < Constants.Carving.CarveMaxAngle)
            {
                _carveBoostAccumulator += Time.deltaTime;
                if (_carveBoostAccumulator >= 1f)
                {
                    _currentSpeed += Constants.Speed.CarveBoost;
                    _carveBoostAccumulator = 0f;
                }
            }
            else
            {
                _carveBoostAccumulator = 0f;
            }
        }

        private void ApplyGravity()
        {
            if (_isGrounded)
            {
                // Project velocity onto ground plane
                _velocity = Vector3.ProjectOnPlane(transform.forward * _currentSpeed, _groundNormal);
            }
            else
            {
                // Apply gravity when airborne
                _velocity.y -= _gravity * Time.deltaTime;
            }
        }

        private void MovePlayer()
        {
            if (_controller != null)
            {
                _controller.Move(_velocity * Time.deltaTime);
            }
            else
            {
                transform.position += _velocity * Time.deltaTime;
            }
        }

        public void ApplyJumpForce(float force)
        {
            _isGrounded = false;
            _velocity.y = force;
        }

        public void ApplyBoost(float speedBoost)
        {
            _currentSpeed = Mathf.Min(_currentSpeed + speedBoost, _maxSpeed * 1.5f);
        }

        public void TriggerCrash(Vector3 impactPoint, float impactSpeed)
        {
            _currentSpeed = 0;
            _velocity = Vector3.zero;
            OnCrash?.Invoke();
        }

        private void TriggerEdgeCatch()
        {
            _currentSpeed *= 0.5f;
            _currentLeanAngle = 0;
            OnEdgeCatch?.Invoke();
        }

        public void SetSpeedAfterRecovery()
        {
            _currentSpeed = Constants.Speed.WipeoutRecovery;
        }
    }
}
