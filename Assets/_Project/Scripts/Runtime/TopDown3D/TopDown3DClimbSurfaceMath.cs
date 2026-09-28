using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public enum TopDown3DClimbMode
    {
        None = -1,
        Ground = 0,
        Incline = 1,
        Scramble = 2,
        Wall = 3,
        Overhang = 4,
    }

    public static class TopDown3DClimbSurfaceMath
    {
        public const float InclineStartAngle = 35f;
        public const float ScrambleStartAngle = 55f;
        public const float WallStartAngle = 75f;
        public const float OverhangStartAngle = 105f;
        public const float MaximumOverhangAngle = 160f;
        public const float TransitionHysteresis = 3f;

        public static bool CanStartClimb(
            bool sprintHeld,
            float movementMagnitude,
            Vector3 measuredNormal,
            float maximumWalkableSlope)
        {
            if (!sprintHeld || movementMagnitude <= 0.3f)
            {
                return false;
            }

            var angle = Vector3.Angle(measuredNormal, Vector3.up);
            return measuredNormal.sqrMagnitude > 0.000001f
                && angle > maximumWalkableSlope
                && angle <= MaximumOverhangAngle;
        }

        public static TopDown3DClimbMode SelectMode(
            Vector3 measuredNormal,
            TopDown3DClimbMode previousMode)
        {
            if (measuredNormal.sqrMagnitude < 0.000001f)
            {
                return TopDown3DClimbMode.None;
            }

            var angle = Vector3.Angle(measuredNormal, Vector3.up);
            if (angle > MaximumOverhangAngle)
            {
                return TopDown3DClimbMode.None;
            }

            var previousBand = Mathf.Max(0, (int)previousMode);
            var mode = TopDown3DClimbMode.Ground;
            for (var boundary = 0; boundary < 4; boundary++)
            {
                var startAngle = boundary == 0 ? InclineStartAngle
                    : boundary == 1 ? ScrambleStartAngle
                    : boundary == 2 ? WallStartAngle
                    : OverhangStartAngle;
                var hysteresis = previousMode == TopDown3DClimbMode.None
                    ? 0f
                    : boundary >= previousBand ? TransitionHysteresis : -TransitionHysteresis;
                if (angle < startAngle + hysteresis)
                {
                    break;
                }

                mode = (TopDown3DClimbMode)(boundary + 1);
            }

            return mode;
        }

        public static Vector3 SurfaceUp(Vector3 measuredNormal, Vector3 previousSurfaceUp)
        {
            if (measuredNormal.sqrMagnitude < 0.000001f)
            {
                return Vector3.zero;
            }

            var normal = measuredNormal.normalized;
            var up = Vector3.ProjectOnPlane(Vector3.up, normal);
            if (up.sqrMagnitude < 0.000001f)
            {
                up = Vector3.ProjectOnPlane(previousSurfaceUp, normal);
            }

            return up.sqrMagnitude > 0.000001f ? up.normalized : Vector3.zero;
        }
    }
}
