using UnityEngine;

namespace Shredsquatch.Procedural
{
    /// <summary>
    /// Frame-rate independent smoothing, easing and two-bone limb IK shared by the
    /// procedural character animators. Pure math with no scene access, so it can be unit tested.
    /// </summary>
    public static class ProceduralAnimation
    {
        /// <summary>
        /// Exponentially approach a target. Rate is roughly the inverse time constant:
        /// after 1/rate seconds about 63% of the gap is closed, whatever the frame rate.
        /// </summary>
        public static float Damp(float current, float target, float rate, float deltaTime)
        {
            return Mathf.Lerp(current, target, 1f - Mathf.Exp(-rate * deltaTime));
        }

        public static Vector3 Damp(Vector3 current, Vector3 target, float rate, float deltaTime)
        {
            return Vector3.Lerp(current, target, 1f - Mathf.Exp(-rate * deltaTime));
        }

        public static float EaseOutCubic(float t)
        {
            float u = 1f - Mathf.Clamp01(t);
            return 1f - u * u * u;
        }

        public static float EaseInOutCubic(float t)
        {
            t = Mathf.Clamp01(t);
            return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;
        }

        /// <summary>
        /// Ease out with a small overshoot past 1 before settling (a body slamming into the snow).
        /// </summary>
        public static float EaseOutBack(float t)
        {
            const float Overshoot = 1.70158f;
            float u = Mathf.Clamp01(t) - 1f;
            return 1f + (Overshoot + 1f) * u * u * u + Overshoot * u * u;
        }

        /// <summary>
        /// Rises from 0 to 1 over fadeIn seconds, holds, then falls back to 0 between
        /// fadeOutStart and duration. Used to weight one-shot actions such as a roar.
        /// </summary>
        public static float Envelope(float time, float fadeIn, float fadeOutStart, float duration)
        {
            if (time <= 0f || time >= duration) return 0f;
            float rise = fadeIn > 0f ? Mathf.Clamp01(time / fadeIn) : 1f;
            float fall = 1f - Mathf.Clamp01((time - fadeOutStart) / Mathf.Max(duration - fadeOutStart, 0.0001f));
            return Mathf.SmoothStep(0f, 1f, Mathf.Min(rise, fall));
        }

        /// <summary>
        /// Two-segment limb IK (thigh/shin, upper arm/forearm). Places the middle joint so both
        /// segments keep their length, bending toward bendHint. A target out of reach is pulled
        /// back along the limb so the limb points straight at it.
        /// </summary>
        /// <param name="root">Hip or shoulder.</param>
        /// <param name="target">Where the limb should end (ankle or hand).</param>
        /// <param name="end">The reachable end point actually used.</param>
        /// <returns>The middle joint (knee or elbow).</returns>
        public static Vector3 SolveTwoBone(Vector3 root, Vector3 target, float upperLength, float lowerLength,
            Vector3 bendHint, out Vector3 end)
        {
            Vector3 toTarget = target - root;
            float distance = toTarget.magnitude;
            Vector3 direction = distance > 0.00001f ? toTarget / distance : Vector3.down;

            float maxReach = (upperLength + lowerLength) * 0.999f;
            float minReach = Mathf.Abs(upperLength - lowerLength) + 0.001f;
            distance = Mathf.Clamp(distance, minReach, maxReach);
            end = root + direction * distance;

            // Law of cosines: angle at the root between the limb axis and the upper segment
            float cosRoot = (upperLength * upperLength + distance * distance - lowerLength * lowerLength)
                            / (2f * upperLength * distance);
            cosRoot = Mathf.Clamp(cosRoot, -1f, 1f);
            float sinRoot = Mathf.Sqrt(1f - cosRoot * cosRoot);

            // Bend within the plane of the limb axis and the hint
            Vector3 bend = bendHint - direction * Vector3.Dot(bendHint, direction);
            if (bend.sqrMagnitude < 0.000001f)
            {
                bend = Vector3.Cross(direction, Vector3.right);
                if (bend.sqrMagnitude < 0.000001f) bend = Vector3.Cross(direction, Vector3.forward);
            }
            bend.Normalize();

            return root + direction * (upperLength * cosRoot) + bend * (upperLength * sinRoot);
        }
    }

    /// <summary>
    /// Damped spring for impacts such as landing compression. Value springs back to the
    /// target; give it a kick by adding to Velocity.
    /// </summary>
    public struct DampedSpring
    {
        public float Value;
        public float Velocity;

        // Small fixed sub-steps keep the integration stable when the frame rate drops
        private const float MaxSubStep = 1f / 120f;

        public void Step(float target, float stiffness, float damping, float deltaTime)
        {
            int steps = Mathf.Max(1, Mathf.CeilToInt(deltaTime / MaxSubStep));
            float h = deltaTime / steps;

            for (int i = 0; i < steps; i++)
            {
                float acceleration = stiffness * (target - Value) - damping * Velocity;
                Velocity += acceleration * h;
                Value += Velocity * h;
            }
        }

        public void Reset()
        {
            Value = 0f;
            Velocity = 0f;
        }
    }
}
