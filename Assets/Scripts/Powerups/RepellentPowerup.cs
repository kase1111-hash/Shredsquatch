using UnityEngine;

namespace Shredsquatch.Powerups
{
    public class RepellentPowerup : PowerupBase
    {
        protected override void ApplyEffect(GameObject player)
        {
            // Delegate entirely to PowerupManager so that the repellent timer for
            // the visual effect and the Sasquatch slowdown stay in sync.
            // Previously this also called sasquatch.ApplyRepellent() directly, causing
            // the Sasquatch and PowerupManager timers to desync on re-pickup.
            var manager = player.GetComponent<PowerupManager>();
            if (manager != null)
            {
                manager.ActivateRepellent();
            }
        }
    }
}
