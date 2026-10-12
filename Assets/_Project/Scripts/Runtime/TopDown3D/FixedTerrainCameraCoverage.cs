using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Conservative bounds of a convex camera frustum intersected with resident terrain's vertical range.</summary>
    public static class FixedTerrainCameraCoverage
    {
        // Near ring, far ring, then four connecting edges. Camera corner order is consistent on both planes.
        private static readonly int[] Edges = { 0,1, 1,2, 2,3, 3,0, 4,5, 5,6, 6,7, 7,4, 0,4, 1,5, 2,6, 3,7 };

        public static void FillFrustum(Camera camera, float distance, Camera.MonoOrStereoscopicEye eye, Vector3[] result)
        {
            if (camera == null || result == null || result.Length != 8) throw new ArgumentException("One camera and eight corner slots are required.");
            float far = Mathf.Max(distance, camera.nearClipPlane);
            for (int i = 0; i < 4; i++)
            {
                float x = i >= 2 ? 1 : 0, y = i == 1 || i == 2 ? 1 : 0;
                result[i] = camera.ViewportToWorldPoint(new Vector3(x, y, camera.nearClipPlane), eye);
                result[i + 4] = camera.ViewportToWorldPoint(new Vector3(x, y, far), eye);
            }
        }

        public static bool TryHeightBounds(Vector3[] frustum, float minimumY, float maximumY, out Bounds bounds)
        {
            bounds = default;
            if (frustum == null || frustum.Length != 8 || !float.IsFinite(minimumY) || !float.IsFinite(maximumY) || minimumY > maximumY) return false;
            foreach (var point in frustum) if (!Finite(point)) return false;
            bool found = false;
            foreach (var point in frustum)
                if (point.y >= minimumY && point.y <= maximumY) Include(point, ref bounds, ref found);
            for (int i = 0; i < Edges.Length; i += 2)
            {
                var a = frustum[Edges[i]]; var b = frustum[Edges[i + 1]];
                Intersect(a, b, minimumY, ref bounds, ref found);
                Intersect(a, b, maximumY, ref bounds, ref found);
            }
            // Float interpolation at large world coordinates may round outward/inward by a few millimetres.
            if (found) bounds.Expand(new Vector3(1, 0, 1));
            return found;
        }
        /// <summary>Conservative XZ projection of a caster's height interval onto visible receiver heights.
        /// Unknown, upward or horizon lighting fails open; no whole-landscape height range is needed.</summary>
        public static bool MayCastDirectionalShadow(Bounds caster, Bounds receivers, Vector3 travelDirection)
        {
            if (!Finite(caster.min) || !Finite(caster.max) || !Finite(receivers.min) || !Finite(receivers.max)
                || !Finite(travelDirection) || travelDirection.y >= -.001f
                || caster.size.x < 0 || caster.size.y < 0 || caster.size.z < 0
                || receivers.size.x < 0 || receivers.size.y < 0 || receivers.size.z < 0) return true;
            if (caster.max.y < receivers.min.y) return false;
            float minimum = Mathf.Max(0, (caster.min.y - receivers.max.y) / -travelDirection.y);
            float maximum = Mathf.Max(0, (caster.max.y - receivers.min.y) / -travelDirection.y);
            var projected = new Bounds(caster.center + travelDirection * minimum, caster.size);
            projected.Encapsulate(caster.min + travelDirection * maximum);
            projected.Encapsulate(caster.max + travelDirection * maximum);
            if (!Finite(projected.min) || !Finite(projected.max)) return true;
            return FixedTerrainRenderWindow.IntersectsXZ(projected, receivers);
        }
        private static void Intersect(Vector3 a, Vector3 b, float height, ref Bounds bounds, ref bool found)
        {
            float delta = b.y - a.y;
            if (delta == 0) return; // Coplanar endpoints were included above.
            float t = (height - a.y) / delta;
            if (t < 0 || t > 1) return;
            var point = Vector3.LerpUnclamped(a, b, t); point.y = height;
            Include(point, ref bounds, ref found);
        }
        private static void Include(Vector3 point, ref Bounds bounds, ref bool found)
        { if (found) bounds.Encapsulate(point); else { bounds = new Bounds(point, Vector3.zero); found = true; } }
        private static bool Finite(Vector3 point) => float.IsFinite(point.x) && float.IsFinite(point.y) && float.IsFinite(point.z);
    }
}
