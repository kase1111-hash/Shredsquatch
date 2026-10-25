using UnityEngine;

namespace Shredsquatch.Tricks
{
    /// <summary>
    /// A park box the rider slides along on top of. Lives on the box root, which is pitched and
    /// yawed but never rolled; the deck runs along the root's local +Z between DeckStartZ and DeckEndZ.
    /// </summary>
    public class GrindSurface : MonoBehaviour
    {
        public RailType Type = RailType.FunBox;
        public float DeckStartZ = 0f;
        public float DeckEndZ = 1f;
        public float DeckHalfWidth = 0.8f;

        public Vector3 AxisFlat
        {
            get
            {
                Vector3 f = transform.forward;
                f.y = 0f;
                return f.sqrMagnitude > 1e-6f ? f.normalized : Vector3.forward;
            }
        }

        public Vector3 RightFlat
        {
            get
            {
                Vector3 r = transform.right;
                r.y = 0f;
                return r.sqrMagnitude > 1e-6f ? r.normalized : Vector3.right;
            }
        }

        public float AxisYaw
        {
            get
            {
                Vector3 f = AxisFlat;
                return Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
            }
        }

        /// <summary>Signed horizontal distance of a world point from the box axis (+ = right).</summary>
        public float LateralOffset(Vector3 world)
        {
            return Vector3.Dot(world - transform.position, RightFlat);
        }

        public bool IsOnDeck(Vector3 world)
        {
            Vector3 local = transform.InverseTransformPoint(world);
            return local.z >= DeckStartZ - 0.25f
                && local.z <= DeckEndZ + 0.25f
                && Mathf.Abs(local.x) <= DeckHalfWidth + 0.3f;
        }

        /// <summary>0 at the start of the deck, 1 at its end.</summary>
        public float GetProgress(Vector3 world)
        {
            float length = DeckEndZ - DeckStartZ;
            if (length <= 0f) return 1f;
            return Mathf.Clamp01((transform.InverseTransformPoint(world).z - DeckStartZ) / length);
        }
    }
}
