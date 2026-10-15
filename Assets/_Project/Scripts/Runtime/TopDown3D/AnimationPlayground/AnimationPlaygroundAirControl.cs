using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public static class AnimationPlaygroundAirControl
    {
        // Neutral retains launch momentum. Countersteering is the brake; small
        // stick input retains its magnitude. Flight cannot accelerate past the
        // greater of the launch speed and the standing-jump steering allowance.
        public static Vector3 Advance(Vector3 current, Vector3 input, float launchSpeed,
            float maximumSpeed, float acceleration, float seconds)
        {
            current.y = input.y = 0;
            if (input.sqrMagnitude < .000001f) return current;
            var allowance = Mathf.Min(maximumSpeed, Mathf.Max(1.4f, launchSpeed));
            return Vector3.MoveTowards(current, Vector3.ClampMagnitude(input, 1) * allowance,
                Mathf.Max(0, acceleration * seconds));
        }
    }
}
