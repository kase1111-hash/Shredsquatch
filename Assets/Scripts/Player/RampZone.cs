using UnityEngine;
using Shredsquatch.Core;

namespace Shredsquatch.Player
{
    /// <summary>
    /// Marks a trigger volume as a ramp zone. JumpController reads it in OnTriggerEnter before
    /// falling back to the legacy ramp tags. The zone's +Z is the launch direction and
    /// LipLocalZ is the lip's local Z (0 = the zone origin sits on the lip).
    /// </summary>
    public class RampZone : MonoBehaviour
    {
        public JumpController.RampType Ramp = JumpController.RampType.SmallBump;
        public bool AutoLaunch = true;
        public float LipLocalZ = 0f;
        public float MaxEntryAngle = Constants.Launch.KickerMaxEntryAngle;
        public float MinSpeed = Constants.Launch.DefaultMinSpeed;
    }
}
