using UnityEngine;
using System.Collections;
using Shredsquatch.Core;

namespace Shredsquatch.Powerups
{
    public class PowerupManager : MonoBehaviour
    {
        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem _goldenBoardEffect;
        [SerializeField] private ParticleSystem _nitroEffect;
        [SerializeField] private ParticleSystem _repellentEffect;
        [SerializeField] private Renderer _boardRenderer;
        [SerializeField] private Material _goldenBoardMaterial;

        [Header("Audio")]
        [SerializeField] private AudioSource _powerupActiveAudio;

        // Active powerup states
        private bool _goldenBoardActive;
        private bool _nitroActive;
        private bool _repellentActive;

        private float _goldenBoardTimer;
        private float _nitroTimer;
        private float _repellentTimer;

        private Material _originalBoardMaterial;
        private float _trickMultiplier = 1f;

        // Properties
        public bool GoldenBoardActive => _goldenBoardActive;
        public bool NitroActive => _nitroActive;
        public bool RepellentActive => _repellentActive;
        public float TrickMultiplier => _trickMultiplier;

        // Events
        public event System.Action<PowerupType> OnPowerupActivated;
        public event System.Action<PowerupType> OnPowerupExpired;

        private void Start()
        {
            if (_boardRenderer != null)
            {
                _originalBoardMaterial = _boardRenderer.sharedMaterial;
            }
        }

        private void Update()
        {
            UpdateGoldenBoard();
            UpdateNitro();
            UpdateRepellent();
        }

        public void ActivateGoldenBoard()
        {
            _goldenBoardActive = true;
            _goldenBoardTimer = Constants.Powerup.GoldenBoardDuration;
            _trickMultiplier = 2f;

            // Visual
            if (_goldenBoardEffect != null)
            {
                _goldenBoardEffect.Play();
            }

            if (_boardRenderer != null && _goldenBoardMaterial != null)
            {
                _boardRenderer.material = _goldenBoardMaterial;
            }

            OnPowerupActivated?.Invoke(PowerupType.GoldenBoard);
        }

        public void ActivateNitro()
        {
            _nitroActive = true;
            _nitroTimer = Constants.Powerup.NitroDuration;

            if (_nitroEffect != null)
            {
                _nitroEffect.Play();
            }

            OnPowerupActivated?.Invoke(PowerupType.NitroTuck);
        }

        public void ActivateRepellent()
        {
            // Repellent doesn't stack
            if (_repellentActive) return;

            _repellentActive = true;
            _repellentTimer = Constants.Powerup.RepellentDuration;

            if (_repellentEffect != null)
            {
                _repellentEffect.Play();
            }

            OnPowerupActivated?.Invoke(PowerupType.Repellent);
        }

        private void UpdateGoldenBoard()
        {
            if (!_goldenBoardActive) return;

            _goldenBoardTimer -= Time.deltaTime;

            if (_goldenBoardTimer <= 0)
            {
                _goldenBoardActive = false;
                _trickMultiplier = 1f;

                if (_goldenBoardEffect != null)
                {
                    _goldenBoardEffect.Stop();
                }

                if (_boardRenderer != null && _originalBoardMaterial != null)
                {
                    _boardRenderer.material = _originalBoardMaterial;
                }

                OnPowerupExpired?.Invoke(PowerupType.GoldenBoard);
            }
        }

        private void UpdateNitro()
        {
            if (!_nitroActive) return;

            _nitroTimer -= Time.deltaTime;

            if (_nitroTimer <= 0)
            {
                _nitroActive = false;

                if (_nitroEffect != null)
                {
                    _nitroEffect.Stop();
                }

                OnPowerupExpired?.Invoke(PowerupType.NitroTuck);
            }
        }

        private void UpdateRepellent()
        {
            if (!_repellentActive) return;

            _repellentTimer -= Time.deltaTime;

            if (_repellentTimer <= 0)
            {
                _repellentActive = false;

                if (_repellentEffect != null)
                {
                    _repellentEffect.Stop();
                }

                OnPowerupExpired?.Invoke(PowerupType.Repellent);
            }
        }

        public float GetRemainingTime(PowerupType type)
        {
            return type switch
            {
                PowerupType.GoldenBoard => _goldenBoardTimer,
                PowerupType.NitroTuck => _nitroTimer,
                PowerupType.Repellent => _repellentTimer,
                _ => 0f
            };
        }

        public void ClearAllPowerups()
        {
            _goldenBoardActive = false;
            _nitroActive = false;
            _repellentActive = false;
            _trickMultiplier = 1f;

            if (_goldenBoardEffect != null) _goldenBoardEffect.Stop();
            if (_nitroEffect != null) _nitroEffect.Stop();
            if (_repellentEffect != null) _repellentEffect.Stop();

            if (_boardRenderer != null && _originalBoardMaterial != null)
            {
                _boardRenderer.material = _originalBoardMaterial;
            }
        }
    }
}
