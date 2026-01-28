using UnityEngine;
using Shredsquatch.Core;
using Shredsquatch.Player;

namespace Shredsquatch.Tricks
{
    public enum RailType
    {
        FallenPine,     // 150 pts/sec
        FenceRail,      // 200 pts/sec
        LogPile,        // 250 pts/sec
        MetalBarrier,   // 300 pts/sec
        CabinRidge,     // 350 pts/sec
        PipeRail,       // 400 pts/sec
        ChairliftCable  // 200-400 pts/sec
    }

    public class RailGrindController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SnowboardPhysics _physics;
        [SerializeField] private PlayerInput _input;
        [SerializeField] private TrickController _trickController;

        [Header("Grind Settings")]
        [SerializeField] private float _balanceDecayRate = 0.5f;
        [SerializeField] private float _balanceSensitivity = 2f;
        [SerializeField] private float _maxWobble = 1f;
        [SerializeField] private float _ollieForce = 8f;

        // State
        private bool _isGrinding;
        private RailType _currentRailType;
        private float _grindDuration;
        private float _grindDistance;        // Meters traveled on rail
        private float _balance;              // -1 to 1
        private float _wobbleTimer;
        private int _grindScore;
        private float _entryRotation;        // For spin-on bonus
        private bool _isHoldingGrab;
        private Transform _currentRail;
        private float _railProgress;         // 0 to 1 along rail

        // Properties
        public bool IsGrinding => _isGrinding;
        public float Balance => _balance;
        public float GrindDuration => _grindDuration;

        // Events
        public event System.Action<int> OnGrindComplete;         // points
        public event System.Action<float> OnGrindDistanceComplete; // meters
        public event System.Action<string> OnGrindStart;         // rail name
        public event System.Action OnGrindFail;

        private void Update()
        {
            if (!_isGrinding) return;

            if (GameManager.Instance?.CurrentState != GameState.Playing)
                return;

            UpdateGrind();
            UpdateBalance();
            CheckGrindEnd();
        }

        public void StartGrind(Transform rail, RailType type, float entryRotation = 0f)
        {
            _isGrinding = true;
            _currentRail = rail;
            _currentRailType = type;
            _grindDuration = 0f;
            _grindDistance = 0f;
            _grindScore = 0;
            _balance = 0f;
            _wobbleTimer = 0f;
            _entryRotation = entryRotation;
            _railProgress = 0f;

            // Apply spin entry bonus
            if (Mathf.Abs(entryRotation) >= 180f)
            {
                _grindScore += Constants.Score.RailSpinEntry;
            }

            OnGrindStart?.Invoke(GetRailName(type));
        }

        private void UpdateGrind()
        {
            _grindDuration += Time.deltaTime;

            // Track distance traveled (speed in m/s * time)
            _grindDistance += _physics.CurrentSpeed * Time.deltaTime;

            // Points per second based on rail type
            int pointsPerSec = GetRailPointsPerSecond(_currentRailType);

            // Bonus for long grinds
            if (_grindDuration > 5f)
            {
                pointsPerSec *= 2; // +400/sec for 5+ seconds
            }
            else if (_grindDuration > 2f)
            {
                pointsPerSec = Mathf.RoundToInt(pointsPerSec * 1.33f); // +200/sec for 2-5 seconds
            }

            _grindScore += Mathf.RoundToInt(pointsPerSec * Time.deltaTime);

            // Check for grab while grinding
            if (_input.GrabInput > 0)
            {
                if (!_isHoldingGrab)
                {
                    _isHoldingGrab = true;
                    _grindScore += Constants.Score.RailGrabBonus;
                }
            }
            else
            {
                _isHoldingGrab = false;
            }

            // Speed while grinding
            UpdateGrindSpeed();

            // Move along rail
            UpdateRailPosition();
        }

        private void UpdateBalance()
        {
            // Player input affects balance
            float balanceInput = _input.SteerInput;

            if (Mathf.Abs(balanceInput) > 0.1f)
            {
                // Counteract wobble with input
                _balance -= balanceInput * _balanceSensitivity * Time.deltaTime;
                _wobbleTimer = 0f;
            }
            else
            {
                // No input = gradual wobble increase
                _wobbleTimer += Time.deltaTime;

                if (_wobbleTimer > Constants.Rail.BalanceDecayTime)
                {
                    // Fall off
                    EndGrind(false);
                    return;
                }

                // Wobble back and forth
                float wobbleSpeed = 1f + _wobbleTimer;
                _balance += Mathf.Sin(Time.time * wobbleSpeed) * _balanceDecayRate * Time.deltaTime;
            }

            _balance = Mathf.Clamp(_balance, -_maxWobble, _maxWobble);

            // Fall off if too unbalanced
            if (Mathf.Abs(_balance) >= _maxWobble)
            {
                EndGrind(false);
            }
        }

        private void UpdateGrindSpeed()
        {
            // Accelerate while grinding (up to max)
            float currentSpeedKmh = _physics.CurrentSpeedKmh;
            float accelerationKmh = Constants.Rail.AccelerationRate * Time.deltaTime;

            // Metal barriers accelerate faster
            if (_currentRailType == RailType.MetalBarrier)
            {
                accelerationKmh *= 2f;
            }

            float targetSpeedKmh = Mathf.Min(
                currentSpeedKmh + accelerationKmh,
                Constants.Rail.MaxGrindSpeed
            );

            // Apply the speed boost (convert km/h delta to m/s)
            float speedDeltaKmh = targetSpeedKmh - currentSpeedKmh;
            if (speedDeltaKmh > 0)
            {
                _physics.ApplyBoost(speedDeltaKmh / 3.6f);
            }
        }

        private void UpdateRailPosition()
        {
            if (_currentRail == null) return;

            // Move player along rail
            // This is simplified - real implementation would follow rail spline
            _railProgress += _physics.CurrentSpeed * Time.deltaTime / 50f; // Normalize by rail length

            if (_railProgress >= 1f)
            {
                // Reached end of rail
                EndGrind(true);
            }
        }

        private void CheckGrindEnd()
        {
            // Jump off rail
            if (_input.JumpPressed)
            {
                // Check if ollie at end for bonus
                bool perfectDismount = _railProgress >= 0.9f;

                if (perfectDismount)
                {
                    _grindScore += Constants.Score.RailOllieBonus;
                }

                EndGrind(true, true);
            }
        }

        public void EndGrind(bool success, bool jumped = false)
        {
            if (!_isGrinding) return;

            _isGrinding = false;

            if (success)
            {
                // Log pile launch bonus
                if (_currentRailType == RailType.LogPile && jumped)
                {
                    _physics.ApplyJumpForce(_ollieForce * 1.5f); // Extra height
                }
                else if (jumped)
                {
                    _physics.ApplyJumpForce(_ollieForce);
                }

                OnGrindComplete?.Invoke(_grindScore);
                OnGrindDistanceComplete?.Invoke(_grindDistance);

                // Add to game score
                GameManager.Instance?.AddTrickScore(_grindScore, 1);
            }
            else
            {
                OnGrindFail?.Invoke();
            }

            _currentRail = null;
            _grindScore = 0;
        }

        private int GetRailPointsPerSecond(RailType type)
        {
            return type switch
            {
                RailType.FallenPine => 150,
                RailType.FenceRail => 200,
                RailType.LogPile => 250,
                RailType.MetalBarrier => 300,
                RailType.CabinRidge => 350,
                RailType.PipeRail => 400,
                RailType.ChairliftCable => 300, // Variable 200-400
                _ => 150
            };
        }

        private string GetRailName(RailType type)
        {
            return type switch
            {
                RailType.FallenPine => "Fallen Pine",
                RailType.FenceRail => "Fence Rail",
                RailType.LogPile => "Log Pile",
                RailType.MetalBarrier => "Metal Barrier",
                RailType.CabinRidge => "Cabin Ridge",
                RailType.PipeRail => "Pipe Rail",
                RailType.ChairliftCable => "Chairlift Cable",
                _ => "Rail"
            };
        }

        // Called by rail colliders
        private void OnTriggerEnter(Collider other)
        {
            if (_isGrinding) return;
            if (!other.CompareTag("Rail")) return;

            // Determine rail type
            RailType type = RailType.FenceRail; // Default

            if (other.name.Contains("Pine")) type = RailType.FallenPine;
            else if (other.name.Contains("Fence")) type = RailType.FenceRail;
            else if (other.name.Contains("Log")) type = RailType.LogPile;
            else if (other.name.Contains("Metal") || other.name.Contains("Barrier")) type = RailType.MetalBarrier;
            else if (other.name.Contains("Cabin")) type = RailType.CabinRidge;
            else if (other.name.Contains("Pipe")) type = RailType.PipeRail;
            else if (other.name.Contains("Chair") || other.name.Contains("Cable")) type = RailType.ChairliftCable;

            // Check entry angle/distance
            float distFromCenter = Vector3.Distance(
                transform.position,
                other.ClosestPoint(transform.position)
            );

            if (distFromCenter <= Constants.Rail.EntryTolerance)
            {
                StartGrind(other.transform, type);
            }
        }
    }
}
