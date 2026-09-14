using UnityEngine;
using System.Collections;
using Shredsquatch.Core;

namespace Shredsquatch.Player
{
    public class CrashHandler : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SnowboardPhysics _physics;
        [SerializeField] private Animator _animator;
        [SerializeField] private Collider _playerCollider;

        [Header("Ragdoll")]
        [SerializeField] private Rigidbody[] _ragdollBodies;
        [SerializeField] private Collider[] _ragdollColliders;

        [Header("Effects")]
        [SerializeField] private ParticleSystem _powderSprayPrefab;

        // State
        private bool _isInRagdoll;
        private bool _isRecovering;
        private bool _isInvincible;
        private float _tumbleDistance;
        private float _lastGrazeTime = -10f;

        private const float GrazeCooldown = 0.75f;

        // Properties
        public bool IsInRagdoll => _isInRagdoll;
        public bool IsRecovering => _isRecovering;
        public bool IsInvincible => _isInvincible;

        // Events
        public event System.Action OnRagdollStart;
        public event System.Action OnRagdollEnd;
        public event System.Action OnRecoveryComplete;

        private void Awake()
        {
            if (_physics == null) _physics = GetComponent<SnowboardPhysics>();
        }

        private void Start()
        {
            // Disable ragdoll at start
            SetRagdollActive(false);

            if (_physics != null)
            {
                _physics.OnCrash += HandleCrash;
                _physics.OnEdgeCatch += HandleEdgeCatch;
            }
        }

        private void OnDestroy()
        {
            if (_physics != null)
            {
                _physics.OnCrash -= HandleCrash;
                _physics.OnEdgeCatch -= HandleEdgeCatch;
            }
        }

        public void HandleCollision(Collision collision, float currentSpeed)
        {
            if (collision == null || collision.contactCount == 0) return;
            HandleObstacleHit(collision.gameObject, collision.GetContact(0).point, currentSpeed);
        }

        /// <summary>
        /// Shared obstacle-impact logic for rigidbody collisions and CharacterController hits.
        /// </summary>
        public void HandleObstacleHit(GameObject obstacle, Vector3 contactPoint, float currentSpeed)
        {
            if (_isInvincible || _isInRagdoll || _isRecovering) return;
            if (obstacle == null || !IsObstacle(obstacle)) return;

            float speedKmh = currentSpeed * 3.6f;

            if (speedKmh > Constants.Speed.CrashThreshold)
            {
                // Full ragdoll, then stop the board (OnCrash also ends the trick combo)
                StartCoroutine(RagdollSequence(speedKmh, contactPoint));
                _physics.TriggerCrash(contactPoint, currentSpeed);
            }
            else
            {
                // Powder spray, minor slow (rate-limited: controller hits repeat every frame)
                if (Time.time - _lastGrazeTime < GrazeCooldown) return;
                _lastGrazeTime = Time.time;

                TriggerPowderSpray(contactPoint);
                _physics.ApplyBoost(-20f / 3.6f); // -20 km/h
            }
        }

        private static bool IsObstacle(GameObject obj)
        {
            if (obj.CompareTag("Tree") || obj.CompareTag("Rock")) return true;

            // Colliders are sometimes on a child of the tagged prefab root
            Transform parent = obj.transform.parent;
            return parent != null && (parent.CompareTag("Tree") || parent.CompareTag("Rock"));
        }

        public void HandleBadLanding(float angleOff)
        {
            if (_isInvincible || _isInRagdoll || _isRecovering) return;

            if (angleOff > Constants.Jump.LandingAngleMax)
            {
                // Perpendicular landing = crash
                StartCoroutine(RagdollSequence(_physics.CurrentSpeedKmh, transform.position));
            }
            else if (angleOff > Constants.Jump.LandingAngleClean)
            {
                // Stumble
                _physics.ApplyBoost(-30f / 3.6f); // -30 km/h
                TriggerStumble();
            }
        }

        private void HandleCrash()
        {
            if (!_isInRagdoll && !_isInvincible)
            {
                StartCoroutine(RagdollSequence(_physics.CurrentSpeedKmh, transform.position));
            }
        }

        private void HandleEdgeCatch()
        {
            if (!_isInRagdoll && !_isInvincible)
            {
                // Half ragdoll time for edge catch
                StartCoroutine(RagdollSequence(_physics.CurrentSpeedKmh * 0.5f, transform.position, true));
            }
        }

        private IEnumerator RagdollSequence(float speedKmh, Vector3 impactPoint, bool isEdgeCatch = false)
        {
            _isInRagdoll = true;
            if (_physics != null) _physics.MovementLocked = true;
            OnRagdollStart?.Invoke();

            // Disable normal controls
            if (_animator != null)
            {
                _animator.enabled = false;
            }

            // Enable ragdoll physics
            SetRagdollActive(true);

            // Apply impact force
            Vector3 impactDirection = (transform.position - impactPoint).normalized + Vector3.up * 0.5f;
            float impactForce = speedKmh * 10f;

            if (_ragdollBodies != null)
            {
                foreach (var rb in _ragdollBodies)
                {
                    if (rb != null)
                    {
                        rb.AddForce(impactDirection * impactForce, ForceMode.Impulse);
                    }
                }
            }

            // Calculate tumble distance
            float speedRatio = Mathf.InverseLerp(50f, 100f, speedKmh);
            _tumbleDistance = Mathf.Lerp(
                Constants.Crash.TumbleDistanceBase,
                Constants.Crash.TumbleDistanceMax,
                speedRatio
            );

            // Ragdoll duration
            float ragdollTime = Mathf.Lerp(
                Constants.Crash.RagdollMinDuration,
                Constants.Crash.RagdollMaxDuration,
                speedRatio
            );

            if (isEdgeCatch)
            {
                ragdollTime *= 0.5f;
            }

            yield return new WaitForSeconds(ragdollTime);

            // End ragdoll
            SetRagdollActive(false);
            _isInRagdoll = false;
            OnRagdollEnd?.Invoke();

            // Start recovery
            yield return StartCoroutine(RecoverySequence());
        }

        private IEnumerator RecoverySequence()
        {
            _isRecovering = true;

            // Re-enable animator
            if (_animator != null)
            {
                _animator.enabled = true;
                _animator.SetTrigger("StandUp");
            }

            yield return new WaitForSeconds(Constants.Crash.RecoveryTime);

            _isRecovering = false;
            _physics.MovementLocked = false;
            _physics.SetSpeedAfterRecovery();

            // Grant invincibility
            _isInvincible = true;
            OnRecoveryComplete?.Invoke();

            yield return new WaitForSeconds(Constants.Crash.InvincibilityTime);

            _isInvincible = false;
        }

        private void SetRagdollActive(bool active)
        {
            if (_ragdollBodies == null) return;

            foreach (var rb in _ragdollBodies)
            {
                if (rb != null)
                {
                    rb.isKinematic = !active;
                }
            }

            if (_ragdollColliders != null)
            {
                foreach (var col in _ragdollColliders)
                {
                    if (col != null)
                    {
                        col.enabled = active;
                    }
                }
            }

            // Toggle main collider opposite
            if (_playerCollider != null)
            {
                _playerCollider.enabled = !active;
            }
        }

        private void TriggerPowderSpray(Vector3 position)
        {
            // Spawn powder particle effect
            if (_powderSprayPrefab != null)
            {
                ParticleSystem spray = Instantiate(_powderSprayPrefab, position, Quaternion.identity);
                spray.Play();
                Destroy(spray.gameObject, spray.main.duration + spray.main.startLifetime.constantMax);
            }

            // Trigger near-miss feedback (screen shake + haptics)
            if (GameFeedback.Instance != null)
            {
                GameFeedback.Instance.TriggerNearMiss();
            }
        }

        private void TriggerStumble()
        {
            if (_animator != null)
            {
                _animator.SetTrigger("Stumble");
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            HandleCollision(collision, _physics.CurrentSpeed);
        }

        // The rider moves with a CharacterController, which reports contacts here
        // rather than through OnCollisionEnter.
        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (_physics == null || hit.gameObject == null) return;
            HandleObstacleHit(hit.gameObject, hit.point, _physics.CurrentSpeed);
        }
    }
}
