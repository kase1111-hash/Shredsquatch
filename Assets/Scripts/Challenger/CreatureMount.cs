using UnityEngine;

namespace Shredsquatch.Challenger
{
    /// <summary>
    /// Creature mount that replaces snowboard in challenger mode.
    /// Handles waddle/slide animations and creature-specific physics.
    /// </summary>
    public class CreatureMount : MonoBehaviour
    {
        [Header("Mount Settings")]
        [SerializeField] private MountType _mountType = MountType.Waddle;
        [SerializeField] private float _waddleSpeed = 8f;
        [SerializeField] private float _waddleAmount = 0.3f;
        [SerializeField] private float _bobAmount = 0.1f;

        [Header("Physics Modifiers")]
        [SerializeField] private float _turnSpeedMultiplier = 0.8f;
        [SerializeField] private float _maxSpeedMultiplier = 0.9f;
        [SerializeField] private float _jumpMultiplier = 1.1f;

        [Header("Visuals")]
        [SerializeField] private Transform _bodyTransform;
        [SerializeField] private Transform _feetTransform;
        [SerializeField] private ParticleSystem _trailParticles;
        [SerializeField] private Animator _animator;

        [Header("Audio")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip[] _waddleSounds;
        [SerializeField] private AudioClip _slideSound;

        // State
        private float _waddlePhase;
        private float _currentSpeed;
        private bool _isSliding;
        private bool _isAirborne;

        // Animation hashes
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int SlidingHash = Animator.StringToHash("Sliding");
        private static readonly int AirborneHash = Animator.StringToHash("Airborne");
        private static readonly int TrickHash = Animator.StringToHash("DoTrick");

        public float TurnSpeedMultiplier => _turnSpeedMultiplier;
        public float MaxSpeedMultiplier => _maxSpeedMultiplier;
        public float JumpMultiplier => _jumpMultiplier;

        private void Update()
        {
            if (_mountType == MountType.Waddle && !_isSliding && !_isAirborne)
            {
                UpdateWaddle();
            }

            UpdateAnimator();
        }

        private void UpdateWaddle()
        {
            if (_currentSpeed < 0.1f) return;

            _waddlePhase += Time.deltaTime * _waddleSpeed * (_currentSpeed / 30f);

            if (_bodyTransform != null)
            {
                // Side-to-side waddle
                float sideWaddle = Mathf.Sin(_waddlePhase) * _waddleAmount;
                // Up-down bob
                float verticalBob = Mathf.Abs(Mathf.Sin(_waddlePhase * 2f)) * _bobAmount;

                _bodyTransform.localPosition = new Vector3(sideWaddle, verticalBob, 0);
                _bodyTransform.localRotation = Quaternion.Euler(0, 0, -sideWaddle * 15f);
            }

            // Play waddle sound at footstep moments
            if (_waddleSounds != null && _waddleSounds.Length > 0)
            {
                float phase = _waddlePhase % (Mathf.PI * 2f);
                if (phase < 0.1f || (phase > Mathf.PI && phase < Mathf.PI + 0.1f))
                {
                    PlayWaddleSound();
                }
            }
        }

        private void UpdateAnimator()
        {
            if (_animator == null) return;

            _animator.SetFloat(SpeedHash, _currentSpeed);
            _animator.SetBool(SlidingHash, _isSliding);
            _animator.SetBool(AirborneHash, _isAirborne);
        }

        /// <summary>
        /// Update mount with current player state.
        /// </summary>
        public void UpdateState(float speed, bool isGrounded, bool isTucking)
        {
            _currentSpeed = speed;
            _isAirborne = !isGrounded;
            _isSliding = isTucking && isGrounded && _mountType == MountType.Waddle;

            // Faster when sliding
            if (_isSliding && _trailParticles != null)
            {
                if (!_trailParticles.isPlaying)
                    _trailParticles.Play();
            }
            else if (_trailParticles != null && _trailParticles.isPlaying)
            {
                _trailParticles.Stop();
            }
        }

        /// <summary>
        /// Trigger trick animation on mount.
        /// </summary>
        public void TriggerTrick()
        {
            if (_animator != null)
            {
                _animator.SetTrigger(TrickHash);
            }
        }

        /// <summary>
        /// Set mount type and adjust behavior.
        /// </summary>
        public void SetMountType(MountType type)
        {
            _mountType = type;

            // Adjust physics based on mount
            switch (type)
            {
                case MountType.Waddle:
                    _turnSpeedMultiplier = 0.8f;
                    _maxSpeedMultiplier = 0.9f;
                    _jumpMultiplier = 1.1f;
                    break;
                case MountType.Slide:
                    _turnSpeedMultiplier = 0.6f;
                    _maxSpeedMultiplier = 1.1f;
                    _jumpMultiplier = 0.9f;
                    break;
                case MountType.Hop:
                    _turnSpeedMultiplier = 1.0f;
                    _maxSpeedMultiplier = 0.8f;
                    _jumpMultiplier = 1.3f;
                    break;
                case MountType.Glide:
                    _turnSpeedMultiplier = 0.7f;
                    _maxSpeedMultiplier = 1.0f;
                    _jumpMultiplier = 1.0f;
                    break;
            }
        }

        private void PlayWaddleSound()
        {
            if (_audioSource == null || _waddleSounds == null || _waddleSounds.Length == 0)
                return;

            if (_audioSource.isPlaying) return;

            int index = Random.Range(0, _waddleSounds.Length);
            _audioSource.PlayOneShot(_waddleSounds[index], 0.3f);
        }

        /// <summary>
        /// Play celebratory animation (for trick completion).
        /// </summary>
        public void Celebrate()
        {
            if (_animator != null)
            {
                _animator.SetTrigger("Celebrate");
            }
        }
    }
}
