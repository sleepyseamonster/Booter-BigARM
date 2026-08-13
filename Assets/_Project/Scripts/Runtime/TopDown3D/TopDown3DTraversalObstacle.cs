using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public enum TopDown3DTraversalMove
    {
        None,
        Spin,
        Vault
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class TopDown3DTraversalObstacle : MonoBehaviour
    {
    }

    public static class TopDown3DTraversalPlanner
    {
        public static TopDown3DTraversalMove SelectMove(
            float obstacleHeight,
            float maximumVaultHeight,
            float lateralOffset,
            float lateralHalfExtent,
            float sideThresholdRatio)
        {
            if (obstacleHeight <= 0f || lateralHalfExtent <= 0f)
            {
                return TopDown3DTraversalMove.None;
            }

            var sideThreshold = lateralHalfExtent * Mathf.Clamp01(sideThresholdRatio);
            if (Mathf.Abs(lateralOffset) >= sideThreshold)
            {
                return TopDown3DTraversalMove.Spin;
            }

            return obstacleHeight <= Mathf.Max(0f, maximumVaultHeight)
                ? TopDown3DTraversalMove.Vault
                : TopDown3DTraversalMove.None;
        }

        public static float ProjectedExtent(Bounds bounds, Vector3 axis)
        {
            if (axis.sqrMagnitude <= 0.000001f)
            {
                return 0f;
            }

            axis.Normalize();
            var extents = bounds.extents;
            return Mathf.Abs(axis.x) * extents.x
                + Mathf.Abs(axis.y) * extents.y
                + Mathf.Abs(axis.z) * extents.z;
        }

        public static Vector3 CalculateVaultPoint(
            Vector3 start,
            Vector3 end,
            float arcHeight,
            float normalizedTime)
        {
            var time = Mathf.Clamp01(normalizedTime);
            if (time <= 0f)
            {
                return start;
            }

            if (time >= 1f)
            {
                return end;
            }

            return Vector3.Lerp(start, end, time)
                + Vector3.up * (Mathf.Sin(time * Mathf.PI) * Mathf.Max(0f, arcHeight));
        }

        public static Vector3 CalculateQuadraticPoint(
            Vector3 start,
            Vector3 control,
            Vector3 end,
            float normalizedTime)
        {
            var time = Mathf.Clamp01(normalizedTime);
            var inverse = 1f - time;
            return inverse * inverse * start
                + 2f * inverse * time * control
                + time * time * end;
        }
    }
}
