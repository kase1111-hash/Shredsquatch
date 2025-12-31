using UnityEngine;
using Shredsquatch.Core;
using Shredsquatch.Player;

namespace Shredsquatch.Powerups
{
    public class NitroPowerup : PowerupBase
    {
        protected override void ApplyEffect(GameObject player)
        {
            var physics = player.GetComponent<SnowboardPhysics>();
            if (physics != null)
            {
                physics.ApplyBoost(Constants.Powerup.NitroBoost / 3.6f); // Convert to m/s
            }

            var manager = player.GetComponent<PowerupManager>();
            if (manager != null)
            {
                manager.ActivateNitro();
            }

            // Trigger boost feedback
            if (GameFeedback.Instance != null)
            {
                GameFeedback.Instance.TriggerBoost();
            }
        }
    }
}
