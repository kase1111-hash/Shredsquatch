using UnityEngine;
using UnityEngine.AI;
using Shredsquatch.Core;

namespace Shredsquatch.Sasquatch
{
    public class SasquatchAI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _player;
        [SerializeField] private NavMeshAgent _agent;
        [SerializeField] private Animator _animator;
        [SerializeField] private AudioSource _roarAudio;

        // Cached component references
        private Player.SnowboardPhysics _playerPhysics;

        [Header("Chase Settings")]
        [SerializeField] private float _baseSpeed = 25f;          // m/s (90 km/h)
        [SerializeField] private float _targetDistance = 400f;
        [SerializeField] private float _catchDistance = 3f;

        [Header("Visual")]
        [SerializeField] private GameObject _eyeGlowLeft;
        [SerializeField] private GameObject _eyeGlowRight;
        [SerializeField] private ParticleSystem _snowTrail;

        // State
        private bool _isActive;
        private bool _hasSpawned;
        private float _currentDistance;
        private float _currentSpeed;
        private bool _isRepelled;
        private float _repelTimer;

        // Rubber-band state
        private float _rubberBandMultiplier = 1f;
        private float _smoothedPlayerSpeed;

        // Properties
        public bool IsActive => _isActive;
        public float DistanceToPlayer => _currentDistance;
        public float CurrentSpeed => _currentSpeed * 3.6f; // km/h

        // Events
        public event System.Action OnSpawn;
        public event System.Action OnCatchPlayer;
        public event System.Action<float> OnDistanceChanged;

        private void Start()
        {
            // Hide until spawn
            gameObject.SetActive(false);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnDistanceChanged += CheckSpawnCondition;
                GameManager.Instance.OnRunStarted += Reset;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnDistanceChanged -= CheckSpawnCondition;
                GameManager.Instance.OnRunStarted -= Reset;
            }
        }

        private void Update()
        {
            if (!_isActive) return;

            UpdateDistance();
            UpdateRubberBand();
            UpdateMovement();
            CheckCatch();
            UpdateVisuals();
            UpdateRepellent();
        }

        private void CheckSpawnCondition(float distanceKm)
        {
            if (_hasSpawned) return;

            if (distanceKm >= Constants.Sasquatch.SpawnDistance)
            {
                Spawn();
            }
        }

        private void Spawn()
        {
            if (_player == null) return;

            _hasSpawned = true;
            _isActive = true;
            gameObject.SetActive(true);

            // Spawn behind player
            Vector3 spawnPos = _player.position - _player.forward * 800f;
            spawnPos.y = _player.position.y;
            transform.position = spawnPos;
            transform.LookAt(_player);

            // Dramatic entrance
            PlayRoar();

            if (_animator != null)
            {
                _animator.SetTrigger("Spawn");
            }

            OnSpawn?.Invoke();

            Debug.Log("Sasquatch has spawned!");
        }

        private void UpdateDistance()
        {
            if (_player == null) return;

            _currentDistance = Vector3.Distance(transform.position, _player.position);
            OnDistanceChanged?.Invoke(_currentDistance);
        }

        private void UpdateRubberBand()
        {
            if (_player == null) return;

            // Cache player physics component if needed
            if (_playerPhysics == null)
            {
                _playerPhysics = _player.GetComponent<Player.SnowboardPhysics>();
            }

            // Get player speed from SnowboardPhysics (smoothed to avoid jitter)
            float playerSpeed = _playerPhysics != null ? _playerPhysics.CurrentSpeed : 0f;
            _smoothedPlayerSpeed = Mathf.Lerp(_smoothedPlayerSpeed, playerSpeed, Time.deltaTime * 2f);

            // Rubber-band based on distance
            if (_currentDistance > Constants.Sasquatch.FarThreshold)
            {
                // Player is far ahead - speed up
                _rubberBandMultiplier = Constants.Sasquatch.BurstSpeedMod;
            }
            else if (_currentDistance < Constants.Sasquatch.CloseThreshold)
            {
                // Player is close - slow down slightly
                _rubberBandMultiplier = Constants.Sasquatch.TiredSpeedMod;
            }
            else
            {
                // In target range - match player roughly
                _rubberBandMultiplier = 1f;
            }

            // If player crashed, speed up more
            var playerController = _player.GetComponent<Player.PlayerController>();
            if (playerController != null && playerController.IsInRagdoll())
            {
                _rubberBandMultiplier = Constants.Sasquatch.BurstSpeedMod * 1.2f;
            }
        }

        private void UpdateMovement()
        {
            if (_player == null) return;

            // Calculate speed
            float targetSpeed = _baseSpeed * _rubberBandMultiplier;

            if (_isRepelled)
            {
                targetSpeed *= Constants.Powerup.RepellentSlowdown;
            }

            _currentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed, Time.deltaTime * 2f);

            // Move toward player
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.speed = _currentSpeed;
                _agent.SetDestination(_player.position);
            }
            else
            {
                // Fallback: direct movement
                Vector3 direction = (_player.position - transform.position).normalized;
                transform.position += direction * _currentSpeed * Time.deltaTime;
                transform.LookAt(_player);
            }

            // Animation
            if (_animator != null)
            {
                _animator.SetFloat("Speed", _currentSpeed / _baseSpeed);
            }
        }

        private void CheckCatch()
        {
            if (_currentDistance <= _catchDistance)
            {
                CatchPlayer();
            }
        }

        private void CatchPlayer()
        {
            _isActive = false;

            // Play catch animation/sound
            if (_animator != null)
            {
                _animator.SetTrigger("Catch");
            }

            PlayRoar();

            OnCatchPlayer?.Invoke();

            // End the game
            GameManager.Instance?.EndRun();
        }

        private void UpdateVisuals()
        {
            // Eye glow intensity based on distance
            float glowIntensity = Mathf.InverseLerp(800f, 100f, _currentDistance);

            if (_eyeGlowLeft != null)
            {
                var renderer = _eyeGlowLeft.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.SetFloat("_EmissionIntensity", glowIntensity * 2f);
                }
            }

            if (_eyeGlowRight != null)
            {
                var renderer = _eyeGlowRight.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.SetFloat("_EmissionIntensity", glowIntensity * 2f);
                }
            }

            // Snow trail when moving fast
            if (_snowTrail != null)
            {
                var emission = _snowTrail.emission;
                emission.enabled = _currentSpeed > _baseSpeed * 0.5f;
            }
        }

        private void UpdateRepellent()
        {
            if (_isRepelled)
            {
                _repelTimer -= Time.deltaTime;
                if (_repelTimer <= 0)
                {
                    _isRepelled = false;
                }
            }
        }

        public void ApplyRepellent()
        {
            _isRepelled = true;
            _repelTimer = Constants.Powerup.RepellentDuration;

            // Play confused/slowed animation
            if (_animator != null)
            {
                _animator.SetTrigger("Repelled");
            }
        }

        public void PlayRoar()
        {
            if (_roarAudio != null)
            {
                _roarAudio.Play();
            }
        }

        private void Reset()
        {
            _hasSpawned = false;
            _isActive = false;
            _isRepelled = false;
            _currentSpeed = 0f;
            _rubberBandMultiplier = 1f;
            gameObject.SetActive(false);
        }

        public void SetPlayerReference(Transform player)
        {
            _player = player;
            _playerPhysics = null; // Clear cached reference so it gets re-fetched
        }

        // Called when destroying trees
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Tree"))
            {
                // Smash through trees
                Rigidbody treeRb = other.GetComponent<Rigidbody>();
                if (treeRb != null)
                {
                    treeRb.isKinematic = false;
                    treeRb.AddForce(transform.forward * 1000f + Vector3.up * 500f);
                }
                else
                {
                    // Just destroy it
                    Destroy(other.gameObject, 0.5f);
                }
            }
        }
    }
}
