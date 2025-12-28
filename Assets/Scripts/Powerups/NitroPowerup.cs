using UnityEngine;
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
                physics.ApplyBoost(Core.Constants.Powerup.NitroBoost / 3.6f); // Convert to m/s
            }

            var manager = player.GetComponent<PowerupManager>();
            if (manager != null)
            {
                manager.ActivateNitro();
            }
        }
    }
}
