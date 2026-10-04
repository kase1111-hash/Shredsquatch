using UnityEngine;
using System.Collections.Generic;
using Shredsquatch.Core;
using Shredsquatch.Tricks;

namespace Shredsquatch.Player
{
    public class JumpController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SnowboardPhysics _physics;
        [SerializeField] private PlayerInput _input;
        private RailGrindController _railGrind;

        [Header("Jump Settings")]
        [SerializeField] private float _baseJumpForce = 8f;
        [SerializeField] private float _chargeRate = 1f;
        [Tooltip("Ramp zones marked AutoLaunch throw the rider at their lip without a button press.")]
        [SerializeField] private bool _autoLaunchOffLips = true;

        // State
        private float _chargeTime;
        private bool _isCharging;
        private float _airTime;
        private bool _wasGrounded;
        private RampType _currentRamp = RampType.None;
        private float _rampMemoryUntil = -1f;
        private float _lastGroundedTime = -999f;
        private float _lastLaunchTime = -999f;
        private RampType _lastLaunchRamp = RampType.None;

        // Ramp triggers currently overlapped, reference-counted so overlapping zones don't cancel each other
        private readonly List<Collider> _rampColliders = new List<Collider>();
        private readonly List<RampType> _rampTypes = new List<RampType>();
        private readonly List<RampZone> _rampZones = new List<RampZone>();   // null entries for legacy tag triggers

        // Feet within this height of a lip count as being carried up the deck to it
        private const float LipCarryHeight = 0.35f;

        // Properties
        public float AirTime => _airTime;
        public bool IsAirborne => !_physics.IsGrounded;
        public RampType CurrentRamp => _currentRamp;

        // Events
        public event System.Action<float> OnJump;           // airtime potential
        public event System.Action<float, bool> OnLand;    // airtime, wasClean
        public event System.Action OnChargeStart;

        private void Awake()
        {
            if (_physics == null) _physics = GetComponent<SnowboardPhysics>();
            if (_input == null) _input = GetComponent<PlayerInput>();
            _railGrind = GetComponent<RailGrindController>();
        }

        private void Start()
        {
            if (_physics != null)
            {
                // Lip launches run inside the physics step, after the ground check and before the move
                _physics.BeforeMove += CheckAutoLaunch;
                _physics.OnMotionReset += HandleMotionReset;
            }
        }

        private void OnDestroy()
        {
            if (_physics != null)
            {
                _physics.BeforeMove -= CheckAutoLaunch;
                _physics.OnMotionReset -= HandleMotionReset;
            }
        }

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

            if (_physics.IsGrounded) _lastGroundedTime = Time.time;
            RefreshCurrentRamp();

            HandleJumpInput();
            TrackAirTime();
            CheckLanding();
        }

        private void HandleJumpInput()
        {
            // Start charging
            if (_physics.IsGrounded && _input.JumpPressed)
            {
                _isCharging = true;
                _chargeTime = 0f;
                OnChargeStart?.Invoke();
            }

            // Continue charging (also through a lip, where an auto-launch uses the charge)
            if (_isCharging && _input.JumpHeld)
            {
                _chargeTime = Mathf.Min(_chargeTime + Time.deltaTime * _chargeRate, Constants.Jump.ChargeTimeMax);
            }

            // Release jump
            if (_isCharging && _input.JumpReleased)
            {
                if (CanJumpNow())
                {
                    ExecuteJump();
                }
                else
                {
                    // Released in the air: the charge is dropped
                    _isCharging = false;
                }
            }
        }

        /// <summary>
        /// Grounded, or just left the ground (coyote time) without having launched or still rising.
        /// </summary>
        private bool CanJumpNow()
        {
            if (_physics.IsGrounded) return true;

            float now = Time.time;
            return !_physics.IsRising
                && now - _lastGroundedTime <= Constants.Launch.CoyoteTime
                && now - _lastLaunchTime > Constants.Launch.CoyoteTime;
        }

        private void ExecuteJump()
        {
            RampType ramp = _currentRamp;

            // Ollie off a box: counts as a small bump so flips are allowed. The slide's exit grace counts
            // too: a Flat/Down box has no exit ramp, so the lock drops the moment the board leaves the deck,
            // while the grind still pays the ollie bonus for a release in that window.
            bool onBox = _physics.CurrentGrindSurface != null || (_railGrind != null && _railGrind.IsBoxGrinding);
            if (ramp == RampType.None && onBox)
            {
                ramp = RampType.SmallBump;
            }

            Launch(ramp, _chargeTime / Constants.Jump.ChargeTimeMax);
        }

        /// <summary>
        /// The single launch path for button jumps and lip auto-launches.
        /// </summary>
        private void Launch(RampType ramp, float charge01)
        {
            float gravity = Gravity;
            float jumpHeight = LaunchHeight(ramp, charge01);

            // Speed boost goes first so the flight carries it
            float speedBoost = GetRampSpeedBoost(ramp);
            if (speedBoost > 0f)
            {
                _physics.ApplyBoost(speedBoost / 3.6f); // Convert km/h to m/s
            }

            _physics.ApplyJumpForce(Mathf.Sqrt(2f * gravity * jumpHeight)); // sqrt(2gh)

            _airTime = 0f;
            _isCharging = false;
            _chargeTime = 0f;
            _lastLaunchTime = Time.time;
            _lastLaunchRamp = ramp;

            // TrickController reads CurrentRamp inside OnJump
            _currentRamp = ramp;
            OnJump?.Invoke(EstimateAirTime(jumpHeight, gravity));

            // Keep the zones still overlapped: Unity never re-sends OnTriggerEnter for a trigger the rider
            // didn't leave, so dropping them would leave a chute wall or kicker dead after landing in it.
            // Cooldown, IsRising and the lip-carry check stop the same lip firing twice.
            _currentRamp = RampType.None;
            _rampMemoryUntil = -1f;
        }

        private float Gravity => _physics.Gravity > 0f ? _physics.Gravity : 20f;

        /// <summary>Base height from flat ground plus the ramp bonus, then the charge bonus (0 to 50%).</summary>
        private float LaunchHeight(RampType ramp, float charge01)
        {
            return (Constants.Jump.BaseHeight + GetRampBonus(ramp))
                * (1f + Mathf.Clamp01(charge01) * Constants.Jump.ChargeBonus);
        }

        /// <summary>
        /// BeforeMove handler. Runs inside SnowboardPhysics.Update after the ground check, so the
        /// grounded state is fresh, and before gravity and the move.
        /// </summary>
        private void CheckAutoLaunch()
        {
            if (!_autoLaunchOffLips || _rampZones.Count == 0) return;
            if (_physics.MovementLocked) return;

            float now = Time.time;
            if (_physics.IsGrounded) _lastGroundedTime = now;

            // Normal case: riding the deck over the lip
            bool ridingDeck = !_physics.IsRising
                && now - _lastGroundedTime <= Constants.Launch.GroundedGrace
                && now - _lastLaunchTime >= Constants.Launch.Cooldown;

            float charge01 = _isCharging ? _chargeTime / Constants.Jump.ChargeTimeMax : 0f;
            float speed = _physics.CurrentSpeed;
            Vector3 forward = transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 1e-6f) return;
            forward.Normalize();

            for (int i = 0; i < _rampZones.Count; i++)
            {
                RampZone zone = _rampZones[i];
                if (zone == null || !zone.AutoLaunch || speed < zone.MinSpeed) continue;

                Vector3 zoneForward = zone.transform.forward;
                zoneForward.y = 0f;
                if (zoneForward.sqrMagnitude < 1e-6f || Vector3.Angle(forward, zoneForward) > zone.MaxEntryAngle) continue;

                // Lip not reached this frame
                Vector3 local = zone.transform.InverseTransformPoint(transform.position);
                if (zone.LipLocalZ - local.z > speed * Time.deltaTime) continue;

                // A hop released just before the ramp leaves the rider "rising" while the deck carries them
                // up to the lip (or lands them on it inside the cooldown); skipping the pop then would drop
                // them off a 3-4.5 m lip with no air. Feet at lip height, still on the deck (grounded or being
                // carried up it, never falling past the lip mid-flight) and leaving slower than this lip's
                // launch still pops. After a ramp launch only half its speed counts, so a lip can't refire
                // straight after its own launch.
                if (!ridingDeck)
                {
                    bool onDeck = _physics.IsGrounded || _physics.IsRising;
                    float lipLaunchSpeed = Mathf.Sqrt(2f * Gravity * LaunchHeight(zone.Ramp, charge01));
                    float limit = _lastLaunchRamp == RampType.None ? lipLaunchSpeed : 0.5f * lipLaunchSpeed;
                    bool carriedToLip = onDeck && local.y <= LipCarryHeight && _physics.Velocity.y < limit;
                    if (!carriedToLip) continue;
                }

                // Holding jump through the lip adds the charge
                Launch(zone.Ramp, charge01);
                return; // One launch per frame
            }
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

        private static float EstimateAirTime(float height, float gravity)
        {
            // Time = sqrt(2h/g), up + down
            return Mathf.Sqrt(2f * height / gravity) * 2f;
        }

        public void SetCurrentRamp(RampType ramp)
        {
            _currentRamp = ramp;
            _rampMemoryUntil = Time.time + Constants.Launch.RampMemory;
        }

        private void OnTriggerEnter(Collider other)
        {
            // Park and terrain zones carry a RampZone; older ramps are recognised by tag
            RampZone zone = other.GetComponent<RampZone>();
            RampType type = zone != null ? zone.Ramp : RampTypeFromTag(other);
            if (type == RampType.None || _rampColliders.Contains(other)) return;

            _rampColliders.Add(other);
            _rampTypes.Add(type);
            _rampZones.Add(zone);
            RefreshCurrentRamp();
        }

        private void OnTriggerExit(Collider other)
        {
            int index = _rampColliders.IndexOf(other);
            if (index >= 0) RemoveRampAt(index);
            RefreshCurrentRamp();
        }

        private static RampType RampTypeFromTag(Collider other)
        {
            if (other.CompareTag("SmallBump")) return RampType.SmallBump;
            if (other.CompareTag("MediumRamp")) return RampType.MediumRamp;
            if (other.CompareTag("LargeKicker")) return RampType.LargeKicker;
            if (other.CompareTag("HalfpipeLip")) return RampType.HalfpipeLip;
            if (other.CompareTag("CabinAFrame")) return RampType.CabinAFrame;
            if (other.CompareTag("CliffJump")) return RampType.CliffJump;
            if (other.CompareTag("LogRamp")) return RampType.LogRamp;
            return RampType.None;
        }

        private void RemoveRampAt(int index)
        {
            _rampColliders.RemoveAt(index);
            _rampTypes.RemoveAt(index);
            _rampZones.RemoveAt(index);
        }

        /// <summary>
        /// The best ramp among the triggers still overlapped; after leaving the last one the type
        /// is remembered briefly so a jump released just past the lip still gets it.
        /// </summary>
        private void RefreshCurrentRamp()
        {
            RampType best = RampType.None;

            for (int i = _rampColliders.Count - 1; i >= 0; i--)
            {
                Collider c = _rampColliders[i];

                // Chunk unloads destroy triggers without OnTriggerExit
                if (c == null || !c.enabled || !c.gameObject.activeInHierarchy)
                {
                    RemoveRampAt(i);
                    continue;
                }

                if (GetRampBonus(_rampTypes[i]) > GetRampBonus(best)) best = _rampTypes[i];
            }

            if (best != RampType.None)
            {
                _currentRamp = best;
                _rampMemoryUntil = Time.time + Constants.Launch.RampMemory;
            }
            else if (Time.time > _rampMemoryUntil)
            {
                _currentRamp = RampType.None;
            }
        }

        private void ClearRampState()
        {
            _rampColliders.Clear();
            _rampTypes.Clear();
            _rampZones.Clear();
            _currentRamp = RampType.None;
            _rampMemoryUntil = -1f;
        }

        /// <summary>
        /// Teleport, run start or recovery: forget ramps, charge and airtime from before.
        /// </summary>
        private void HandleMotionReset()
        {
            ClearRampState();
            _isCharging = false;
            _chargeTime = 0f;
            _airTime = 0f;
            _lastGroundedTime = -999f;
            _lastLaunchRamp = RampType.None;
        }
    }
}
