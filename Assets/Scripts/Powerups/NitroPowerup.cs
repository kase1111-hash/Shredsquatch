using UnityEngine;
using Shredsquatch.Core;
using Shredsquatch.Player;

namespace Shredsquatch.Powerups
{
    public class NitroPowerup : PowerupBase
    {
        protected override void ApplyEffect(GameObject player)
        {
            // ActivateNitro already applies the speed boost and triggers haptic feedback,
            // so we only call the manager to avoid doubling the boost and feedback.
            var manager = player.GetComponent<PowerupManager>();
            if (manager != null)
            {
                manager.ActivateNitro();
            }
        }
    }
}
