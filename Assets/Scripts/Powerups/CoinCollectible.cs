using UnityEngine;
using Shredsquatch.Core;
using Shredsquatch.Tricks;

namespace Shredsquatch.Powerups
{
    public class CoinCollectible : PowerupBase
    {
        [Header("Magnet Settings")]
        [SerializeField] private float _magnetSpeed = 15f;

        private Transform _magnetTarget;
        private bool _beingMagnetized;

        protected override void Update()
        {
            if (_collected) return;

            if (_beingMagnetized && _magnetTarget != null)
            {
                // Move toward player
                Vector3 direction = (_magnetTarget.position - transform.position).normalized;
                transform.position += direction * _magnetSpeed * Time.deltaTime;

                // Check if close enough to collect
                if (Vector3.Distance(transform.position, _magnetTarget.position) < 1f)
                {
                    Collect(_magnetTarget.gameObject);
                }
            }
            else
            {
                base.Update();
                CheckForMagnet();
            }
        }

        private void CheckForMagnet()
        {
            // Check if player is in combo and within magnet range
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            var trickController = player.GetComponent<TrickController>();
            if (trickController == null) return;

            // Only magnetize during active combo
            if (trickController.ComboCount > 0)
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance <= Constants.Powerup.ComboMagnetRadius)
                {
                    _beingMagnetized = true;
                    _magnetTarget = player.transform;
                }
            }
        }

        protected override void ApplyEffect(GameObject player)
        {
            GameManager.Instance?.CollectCoin();
        }
    }
}
