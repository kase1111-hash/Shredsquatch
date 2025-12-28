using UnityEngine;
using Shredsquatch.Core;

namespace Shredsquatch.Powerups
{
    public enum PowerupType
    {
        GoldenBoard,    // x2 trick points for 10s
        NitroTuck,      // +50 km/h boost for 5s
        Repellent,      // Sasquatch slows 50% for 15s
        Coin            // 50 trick points
    }

    public abstract class PowerupBase : MonoBehaviour
    {
        [Header("Base Settings")]
        [SerializeField] protected PowerupType _type;
        [SerializeField] protected float _rotateSpeed = 90f;
        [SerializeField] protected float _bobSpeed = 2f;
        [SerializeField] protected float _bobHeight = 0.3f;

        [Header("Visual")]
        [SerializeField] protected ParticleSystem _idleParticles;
        [SerializeField] protected ParticleSystem _collectParticles;
        [SerializeField] protected AudioSource _collectSound;
        [SerializeField] protected GameObject _visual;

        protected Vector3 _startPosition;
        protected bool _collected;

        public PowerupType Type => _type;

        protected virtual void Start()
        {
            _startPosition = transform.position;
        }

        protected virtual void Update()
        {
            if (_collected) return;

            // Rotate
            transform.Rotate(Vector3.up, _rotateSpeed * Time.deltaTime);

            // Bob up and down
            float yOffset = Mathf.Sin(Time.time * _bobSpeed) * _bobHeight;
            transform.position = _startPosition + Vector3.up * yOffset;
        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            if (_collected) return;

            if (other.CompareTag("Player"))
            {
                Collect(other.gameObject);
            }
        }

        protected virtual void Collect(GameObject player)
        {
            _collected = true;

            // Hide visual
            if (_visual != null)
            {
                _visual.SetActive(false);
            }

            // Stop idle particles
            if (_idleParticles != null)
            {
                _idleParticles.Stop();
            }

            // Play collect effect
            if (_collectParticles != null)
            {
                _collectParticles.Play();
            }

            if (_collectSound != null)
            {
                _collectSound.Play();
            }

            // Apply effect
            ApplyEffect(player);

            // Destroy after particles finish
            float destroyDelay = _collectParticles != null ? _collectParticles.main.duration : 0.5f;
            Destroy(gameObject, destroyDelay);
        }

        protected abstract void ApplyEffect(GameObject player);
    }
}
