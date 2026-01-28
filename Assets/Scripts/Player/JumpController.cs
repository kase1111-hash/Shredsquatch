using UnityEngine;
using Shredsquatch.Core;

namespace Shredsquatch.Player
{
    public class JumpController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SnowboardPhysics _physics;
        [SerializeField] private PlayerInput _input;

        [Header("Jump Settings")]
        [SerializeField] private float _baseJumpForce = 8f;
        [SerializeField] private float _chargeRate = 1f;

        // State
        private float _chargeTime;
        private bool _isCharging;
        private float _airTime;
        private bool _wasGrounded;
        private RampType _currentRamp = RampType.None;

        // Properties
        public float AirTime => _airTime;
        public bool IsAirborne => !_physics.IsGrounded;
        public RampType CurrentRamp => _currentRamp;

        // Events
        public event System.Action<float> OnJump;           // airtime potential
        public event System.Action<float, bool> OnLand;    // airtime, wasClean
        public event System.Action OnChargeStart;

        public enum RampType
        {
            None,
            SmallBump,      // +1m
            MediumRamp,     // +2m
            LargeKicker,    // +4m
            HalfpipeLip,    // +3m
            CabinAFrame,    // +3m
            CliffJump,      // +5-8m
            LogRamp         // +2m
        }

        private void Update()
        {
            if (GameManager.Instance?.CurrentState != GameState.Playing)
                return;

            HandleJumpInput();
            TrackAirTime();
            CheckLanding();
        }

        private void HandleJumpInput()
        {
            if (_physics.IsGrounded)
            {
                // Start charging
                if (_input.JumpPressed)
                {
                    _isCharging = true;
                    _chargeTime = 0f;
                    OnChargeStart?.Invoke();
                }

                // Continue charging
                if (_isCharging && _input.JumpHeld)
                {
                    _chargeTime += Time.deltaTime * _chargeRate;
                    _chargeTime = Mathf.Min(_chargeTime, Constants.Jump.ChargeTimeMax);
                }

                // Release jump
                if (_isCharging && _input.JumpReleased)
                {
                    ExecuteJump();
                    _isCharging = false;
                }
            }
        }

        private void ExecuteJump()
        {
            // Calculate charge bonus (0 to 50%)
            float chargePercent = _chargeTime / Constants.Jump.ChargeTimeMax;
            float chargeBonus = chargePercent * Constants.Jump.ChargeBonus;

            // Base height from flat ground
            float jumpHeight = Constants.Jump.BaseHeight;

            // Add ramp bonus
            float rampBonus = GetRampBonus(_currentRamp);
            jumpHeight += rampBonus;

            // Apply charge bonus
            jumpHeight *= (1f + chargeBonus);

            // Convert height to force (simplified physics)
            float jumpForce = Mathf.Sqrt(2f * 20f * jumpHeight); // sqrt(2gh)

            // Check for flip input
            bool attemptingFlip = _input.FlipForward || _input.FlipBackward;
            if (attemptingFlip && _currentRamp == RampType.None)
            {
                // Can't flip without a ramp
                attemptingFlip = false;
            }

            _physics.ApplyJumpForce(jumpForce);
            _airTime = 0f;

            // Apply speed boost from certain ramps
            float speedBoost = GetRampSpeedBoost(_currentRamp);
            if (speedBoost > 0)
            {
                _physics.ApplyBoost(speedBoost / 3.6f); // Convert km/h to m/s
            }

            OnJump?.Invoke(EstimateAirTime(jumpHeight));

            // Reset ramp
            _currentRamp = RampType.None;
        }

        private void TrackAirTime()
        {
            if (!_physics.IsGrounded)
            {
                _airTime += Time.deltaTime;
            }
        }

        private void CheckLanding()
        {
            if (_wasGrounded == false && _physics.IsGrounded)
            {
                // Just landed
                bool cleanLand = EvaluateLanding();
                OnLand?.Invoke(_airTime, cleanLand);
                _airTime = 0f;
            }

            _wasGrounded = _physics.IsGrounded;
        }

        private bool EvaluateLanding()
        {
            // Check landing angle vs slope
            // Simplified: assume clean if player is mostly upright
            float playerAngle = Vector3.Angle(transform.up, Vector3.up);
            return playerAngle <= Constants.Jump.LandingAngleClean;
        }

        private float GetRampBonus(RampType ramp)
        {
            return ramp switch
            {
                RampType.SmallBump => 1f,
                RampType.MediumRamp => 2f,
                RampType.LargeKicker => 4f,
                RampType.HalfpipeLip => 3f,
                RampType.CabinAFrame => 3f,
                // Use deterministic value (6.5m average) for leaderboard consistency
                // Cliff jumps provide variable height based on terrain, not randomness
                RampType.CliffJump => 6.5f,
                RampType.LogRamp => 2f,
                _ => 0f
            };
        }

        private float GetRampSpeedBoost(RampType ramp)
        {
            return ramp switch
            {
                RampType.MediumRamp => 10f,
                RampType.LargeKicker => 20f,
                RampType.CabinAFrame => 15f,
                RampType.CliffJump => 25f,
                RampType.LogRamp => 5f,
                _ => 0f
            };
        }

        private float EstimateAirTime(float height)
        {
            // Time = sqrt(2h/g)
            return Mathf.Sqrt(2f * height / 20f) * 2f; // Up + down
        }

        public void SetCurrentRamp(RampType ramp)
        {
            _currentRamp = ramp;
        }

        private void OnTriggerEnter(Collider other)
        {
            // Detect ramp types
            if (other.CompareTag("SmallBump")) SetCurrentRamp(RampType.SmallBump);
            else if (other.CompareTag("MediumRamp")) SetCurrentRamp(RampType.MediumRamp);
            else if (other.CompareTag("LargeKicker")) SetCurrentRamp(RampType.LargeKicker);
            else if (other.CompareTag("HalfpipeLip")) SetCurrentRamp(RampType.HalfpipeLip);
            else if (other.CompareTag("CabinAFrame")) SetCurrentRamp(RampType.CabinAFrame);
            else if (other.CompareTag("CliffJump")) SetCurrentRamp(RampType.CliffJump);
            else if (other.CompareTag("LogRamp")) SetCurrentRamp(RampType.LogRamp);
        }

        private void OnTriggerExit(Collider other)
        {
            // Only clear if we're leaving the current ramp type
            if (_currentRamp != RampType.None && DoesColliderMatchRamp(other, _currentRamp))
            {
                _currentRamp = RampType.None;
            }
        }

        private bool DoesColliderMatchRamp(Collider other, RampType ramp)
        {
            return ramp switch
            {
                RampType.SmallBump => other.CompareTag("SmallBump"),
                RampType.MediumRamp => other.CompareTag("MediumRamp"),
                RampType.LargeKicker => other.CompareTag("LargeKicker"),
                RampType.HalfpipeLip => other.CompareTag("HalfpipeLip"),
                RampType.CabinAFrame => other.CompareTag("CabinAFrame"),
                RampType.CliffJump => other.CompareTag("CliffJump"),
                RampType.LogRamp => other.CompareTag("LogRamp"),
                _ => false
            };
        }
    }
}
