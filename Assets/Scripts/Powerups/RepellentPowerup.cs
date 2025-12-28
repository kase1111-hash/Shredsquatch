using UnityEngine;
using Shredsquatch.Sasquatch;

namespace Shredsquatch.Powerups
{
    public class RepellentPowerup : PowerupBase
    {
        protected override void ApplyEffect(GameObject player)
        {
            // Find Sasquatch and apply repellent
            var sasquatch = FindObjectOfType<SasquatchAI>();
            if (sasquatch != null)
            {
                sasquatch.ApplyRepellent();
            }

            var manager = player.GetComponent<PowerupManager>();
            if (manager != null)
            {
                manager.ActivateRepellent();
            }
        }
    }
}
