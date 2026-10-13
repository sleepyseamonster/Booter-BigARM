using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public static class TopDown3DCompassNavigation
    {
        public const float ArrivalRadius = 2f;
        public const float DepartureRadius = 3f;

        public static bool TryHeading(Vector3 facing, out float heading)
        {
            heading = 0;
            if (!Finite(facing.x) || !Finite(facing.z) || new Vector2(facing.x, facing.z).sqrMagnitude < .000001f) return false;
            heading = Normalize(Mathf.Atan2(facing.x, facing.z) * Mathf.Rad2Deg);
            return true;
        }

        public static float Normalize(float degrees) => Mathf.Repeat(degrees, 360f);
        public static float RelativeBearing(Vector3 player, Vector3 camp, float heading)
            => Normalize(Mathf.Atan2(camp.x - player.x, camp.z - player.z) * Mathf.Rad2Deg - heading);
        public static Vector2 LocalCoordinates(Vector3 player, Vector3 camp) => new(player.x - camp.x, player.z - camp.z);
        public static bool IsArrived(float distance, bool previouslyArrived)
            => distance <= (previouslyArrived ? DepartureRadius : ArrivalRadius);
        public static Vector2 OnRing(float clockwiseDegrees, float radius)
            => new(Mathf.Sin(clockwiseDegrees * Mathf.Deg2Rad) * radius, Mathf.Cos(clockwiseDegrees * Mathf.Deg2Rad) * radius);
        public static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        public static bool ValidHorizontalPosition(Vector3 value) => Finite(value.x) && Finite(value.z);
    }

    // One envelope supplies the compass, safe-area inset, canister spacing, and dust-caption clearance.
    public static class TopDown3DCompassLayout
    {
        public static float Scale(float padSize) => padSize / TopDown3DActionDpadHud.ReferenceSize;
        public static float RingRadius(float padSize) => 116f * Scale(padSize);
        public static float OuterRadius(float padSize) => 130f * Scale(padSize); // includes sun rays
        public static float Overhang(float padSize) => OuterRadius(padSize) - padSize * .5f;
        public static float DustBottom(float padSize) => -Overhang(padSize) - 28f;
        public static float DetailsLeft(float padSize) => padSize * .5f + OuterRadius(padSize) + 22f;
        public static Vector2 ExtraInset(float padSize) => new(Overhang(padSize), -DustBottom(padSize));
    }
}
