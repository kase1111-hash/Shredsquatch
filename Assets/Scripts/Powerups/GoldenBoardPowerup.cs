using UnityEngine;
using Shredsquatch.Core;

namespace Shredsquatch.Powerups
{
    public class GoldenBoardPowerup : PowerupBase
    {
        protected override void ApplyEffect(GameObject player)
        {
            var manager = player.GetComponent<PowerupManager>();
            if (manager != null)
            {
                manager.ActivateGoldenBoard();
            }
        }
    }
}
