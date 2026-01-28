using UnityEngine;
using System.Collections.Generic;
using Shredsquatch.Core;
using Shredsquatch.Player;

namespace Shredsquatch.Tricks
{
    public class TrickController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInput _input;
        [SerializeField] private JumpController _jumpController;

        [Header("Settings")]
        [SerializeField] private float _spinSpeed = 360f;
        [SerializeField] private float _flipSpeed = 360f;

        // Current trick state
        private ActiveTrick _currentTrick = new ActiveTrick();
        private bool _isAirborne;
        private float _airStartTime;
        private bool _launchedFromRamp; // Cache ramp state at jump start for flip eligibility

        // Combo tracking
        private List<TrickType> _comboTricks = new List<TrickType>();
        private int _comboCount;
        private float _lastLandTime;
        private Dictionary<TrickType, int> _trickRepeatCount = new Dictionary<TrickType, int>();

        // Events
        public event System.Action<string, int> OnTrickCompleted;   // name, points
        public event System.Action<int> OnComboUpdated;             // combo count
        public event System.Action<int> OnComboEnded;               // total points

        // Properties
        public int ComboCount => _comboCount;
        public bool IsPerformingTrick => _currentTrick.Type != TrickType.None || _currentTrick.Grab != GrabType.None;

        private void Start()
        {
            if (_jumpController != null)
            {
                _jumpController.OnJump += OnJumpStarted;
                _jumpController.OnLand += OnLandingInternal;
            }
        }

        private void OnDestroy()
        {
            if (_jumpController != null)
            {
                _jumpController.OnJump -= OnJumpStarted;
                _jumpController.OnLand -= OnLandingInternal;
            }
        }

        private void Update()
        {
            if (GameManager.Instance?.CurrentState != GameState.Playing)
                return;

            if (_isAirborne)
            {
                ProcessSpinInput();
                ProcessGrabInput();
                ProcessFlipInput();
                UpdateTrickProgress();
            }
        }

        private void OnJumpStarted(float estimatedAirtime)
        {
            _isAirborne = true;
            _airStartTime = Time.time;
            _currentTrick.Reset();
            _currentTrick.StartTime = Time.time;
            // Cache ramp state at jump start since JumpController resets it immediately
            _launchedFromRamp = _jumpController != null &&
                                _jumpController.CurrentRamp != JumpController.RampType.None;
        }

        private void ProcessSpinInput()
        {
            float spinInput = _input.SpinInput;

            if (Mathf.Abs(spinInput) > 0.1f)
            {
                float spinAmount = spinInput * _spinSpeed * Time.deltaTime;
                _currentTrick.Rotation += spinAmount;

                // Rotate player visually
                transform.Rotate(0, spinAmount, 0);
            }
        }

        private void ProcessGrabInput()
        {
            int grabInput = _input.GrabInput;

            if (grabInput > 0 && _currentTrick.Grab == GrabType.None)
            {
                // Start grab
                _currentTrick.Grab = (GrabType)grabInput;
                _currentTrick.GrabStartTime = Time.time - _airStartTime;
            }
            else if (grabInput > 0 && _currentTrick.Grab == (GrabType)grabInput)
            {
                // Continue holding grab
                _currentTrick.GrabHoldDuration = Time.time - _airStartTime - _currentTrick.GrabStartTime;
            }
            else if (grabInput == 0 && _currentTrick.Grab != GrabType.None)
            {
                // Released grab (but we track it was held)
            }
        }

        private void ProcessFlipInput()
        {
            if (_currentTrick.IsFlipping) return;

            // Can only flip if launched from a ramp (use cached value since ramp is reset after jump)
            if (!_launchedFromRamp) return;

            if (_input.FlipForward || _input.FlipBackward)
            {
                _currentTrick.IsFlipping = true;
            }
        }

        private void UpdateTrickProgress()
        {
            if (_currentTrick.IsFlipping)
            {
                float flipDirection = _input.FlipForward ? 1f : -1f;
                _currentTrick.FlipRotation += flipDirection * _flipSpeed * Time.deltaTime;

                // Visual rotation would happen here via animator or direct transform
            }
        }

        private void OnLandingInternal(float airTime, bool wasClean)
        {
            if (!_isAirborne) return;

            _isAirborne = false;

            // Calculate and award points
            int totalPoints = CalculateTrickScore(airTime, wasClean);

            if (totalPoints > 0)
            {
                // Check combo chain
                if (Time.time - _lastLandTime <= Constants.Combo.ChainWindow && _comboCount > 0)
                {
                    _comboCount++;
                }
                else
                {
                    // New combo
                    _comboCount = 1;
                    _trickRepeatCount.Clear();
                }

                // Apply combo multiplier
                float multiplier = GetComboMultiplier(_comboCount);
                int finalPoints = Mathf.RoundToInt(totalPoints * multiplier);

                // Report to game
                string trickName = BuildTrickName();
                OnTrickCompleted?.Invoke(trickName, finalPoints);
                OnComboUpdated?.Invoke(_comboCount);

                GameManager.Instance?.AddTrickScore(finalPoints, _comboCount);

                // Trigger feedback (screen shake + haptics)
                if (GameFeedback.Instance != null)
                {
                    GameFeedback.Instance.TriggerTrickLand(finalPoints);
                }

                _lastLandTime = Time.time;
            }

            _currentTrick.Reset();
        }

        private int CalculateTrickScore(float airTime, bool wasClean)
        {
            int total = 0;

            // Spin points
            TrickType spinTrick = GetSpinTrickFromRotation(_currentTrick.Rotation);
            if (spinTrick != TrickType.None)
            {
                var def = TrickDatabase.GetDefinition(spinTrick);
                if (def != null && airTime >= def.MinAirtime)
                {
                    int spinPoints = ApplyRepetitionPenalty(spinTrick, def.BasePoints);
                    total += spinPoints;
                }
            }

            // Grab points (timing-based)
            if (_currentTrick.Grab != GrabType.None)
            {
                int grabPoints = CalculateGrabPoints(_currentTrick.GrabStartTime, _currentTrick.GrabHoldDuration);
                total += grabPoints;
            }

            // Flip points
            if (_currentTrick.IsFlipping)
            {
                TrickType flipTrick = GetFlipTrickFromRotation(_currentTrick.FlipRotation);
                if (flipTrick != TrickType.None)
                {
                    var def = TrickDatabase.GetDefinition(flipTrick);
                    if (def != null)
                    {
                        total += def.BasePoints;
                    }
                }
            }

            // Style bonuses
            total = ApplyStyleBonuses(total, spinTrick, _currentTrick.Grab, _currentTrick.IsFlipping);

            // Clean land bonus
            if (wasClean && total > 0)
            {
                total += Constants.Score.CleanLandBonus;
            }

            // Basic jump if nothing else
            if (total == 0 && airTime >= 0.3f)
            {
                total = Constants.Score.BasicJump;
            }

            return total;
        }

        private int CalculateGrabPoints(float grabStartTime, float holdDuration)
        {
            // Base points from timing
            int basePoints;
            if (grabStartTime >= 1.0f) basePoints = 600;      // Expert (late grab)
            else if (grabStartTime >= 0.6f) basePoints = 450; // Skilled
            else if (grabStartTime >= 0.3f) basePoints = 300; // Standard
            else basePoints = 150;                            // Panic grab

            // Hold bonus: +50 per 0.5s, max 2s
            float clampedHold = Mathf.Min(holdDuration, 2f);
            int holdBonus = Mathf.FloorToInt(clampedHold / 0.5f) * 50;

            return basePoints + holdBonus;
        }

        private TrickType GetSpinTrickFromRotation(float rotation)
        {
            float absRotation = Mathf.Abs(rotation);

            if (absRotation >= 1080 - 45) return TrickType.Spin1080;
            if (absRotation >= 900 - 45) return TrickType.Spin900;
            if (absRotation >= 720 - 45) return TrickType.Spin720;
            if (absRotation >= 540 - 45) return TrickType.Spin540;
            if (absRotation >= 360 - 45) return TrickType.Spin360;
            if (absRotation >= 180 - 45) return TrickType.Spin180;

            return TrickType.None;
        }

        private TrickType GetFlipTrickFromRotation(float rotation)
        {
            float absRotation = Mathf.Abs(rotation);

            if (absRotation >= 720 - 45) return rotation > 0 ? TrickType.DoubleFront : TrickType.DoubleBack;
            if (absRotation >= 360 - 45) return rotation > 0 ? TrickType.Frontflip : TrickType.Backflip;

            return TrickType.None;
        }

        private int ApplyRepetitionPenalty(TrickType trick, int basePoints)
        {
            if (!_trickRepeatCount.ContainsKey(trick))
            {
                _trickRepeatCount[trick] = 0;
            }

            _trickRepeatCount[trick]++;
            int repeatCount = _trickRepeatCount[trick];

            float multiplier = repeatCount switch
            {
                1 => 1.0f,
                2 => 1.0f,
                3 => Constants.Combo.RepeatPenalty50,
                4 => Constants.Combo.RepeatPenalty25,
                _ => Constants.Combo.RepeatPenaltyMin
            };

            return Mathf.RoundToInt(basePoints * multiplier);
        }

        private int ApplyStyleBonuses(int total, TrickType spin, GrabType grab, bool flipped)
        {
            float multiplier = 1f;

            bool hasSpin = spin != TrickType.None;
            bool hasGrab = grab != GrabType.None;

            // Flip + Spin + Grab = x3
            if (flipped && hasSpin && hasGrab)
            {
                multiplier = 3f;
            }
            // Flip + Spin = x2
            else if (flipped && hasSpin)
            {
                multiplier = 2f;
            }
            // Spin + Grab = x1.5
            else if (hasSpin && hasGrab)
            {
                multiplier = 1.5f;
            }

            return Mathf.RoundToInt(total * multiplier);
        }

        private float GetComboMultiplier(int comboLength)
        {
            return comboLength switch
            {
                1 => 1.0f,
                2 => 1.5f,
                3 => 2.0f,
                4 => 2.5f,
                _ => Constants.Combo.MaxMultiplier
            };
        }

        private string BuildTrickName()
        {
            List<string> parts = new List<string>();

            // Spin
            TrickType spin = GetSpinTrickFromRotation(_currentTrick.Rotation);
            if (spin != TrickType.None)
            {
                var def = TrickDatabase.GetDefinition(spin);
                if (def != null) parts.Add(def.Name);
            }

            // Flip
            if (_currentTrick.IsFlipping)
            {
                TrickType flip = GetFlipTrickFromRotation(_currentTrick.FlipRotation);
                if (flip != TrickType.None)
                {
                    var def = TrickDatabase.GetDefinition(flip);
                    if (def != null) parts.Add(def.Name);
                }
            }

            // Grab
            if (_currentTrick.Grab != GrabType.None)
            {
                parts.Add(TrickDatabase.GetGrabName(_currentTrick.Grab));
            }

            return parts.Count > 0 ? string.Join(" ", parts) : "Air";
        }

        public void EndCombo(bool wasClean)
        {
            if (_comboCount > 0)
            {
                OnComboEnded?.Invoke(_comboCount);
            }
            _comboCount = 0;
            _comboTricks.Clear();
            _trickRepeatCount.Clear();
        }
    }
}
