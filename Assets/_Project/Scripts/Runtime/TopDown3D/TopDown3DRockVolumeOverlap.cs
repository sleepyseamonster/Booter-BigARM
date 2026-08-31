using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    internal static class TopDown3DRockVolumeOverlap
    {
        private const float MinimumOverlapExtent = 1e-5f;
        private const float MinimumAbsoluteClearance = 0.0025f;
        private const float RelativeClearance = 0.005f;
        private const double StrictInsideAngle = Math.PI * 3.0;

        internal static WorldSolid CreateWorldSolid(
            TopDown3DNaturalObjectCatalog catalog,
            TopDown3DRockFormationMember member)
        {
            var source = catalog.GetRequiredLod0Data(member.Shape, member.Variant);
            var matrix = Matrix4x4.TRS(member.Position, member.Rotation, member.Scale);
            var vertices = new Vector3[source.Vertices.Length];
            for (var i = 0; i < vertices.Length; i++)
            {
                vertices[i] = matrix.MultiplyPoint3x4(source.Vertices[i]);
            }
            var bounds = new Bounds(vertices[0], Vector3.zero);
            for (var i = 1; i < vertices.Length; i++)
            {
                bounds.Encapsulate(vertices[i]);
            }

            return new WorldSolid(vertices, source.Triangles, bounds);
        }

        internal static bool HasPositiveVolumeOverlap(WorldSolid first, WorldSolid second)
        {
            if (first == null || second == null)
            {
                return false;
            }

            var minimum = Vector3.Max(first.Bounds.min, second.Bounds.min);
            var maximum = Vector3.Min(first.Bounds.max, second.Bounds.max);
            var size = maximum - minimum;
            if (size.x <= MinimumOverlapExtent
                || size.y <= MinimumOverlapExtent
                || size.z <= MinimumOverlapExtent)
            {
                return false;
            }

            var clearance = Mathf.Max(
                MinimumAbsoluteClearance,
                Mathf.Min(GetMinimumDimension(first.Bounds), GetMinimumDimension(second.Bounds))
                    * RelativeClearance);
            var clearanceSquared = clearance * clearance;
            if (IsSharedInteriorPoint(
                    (minimum + maximum) * 0.5f,
                    first,
                    second,
                    clearanceSquared))
            {
                return true;
            }

            return false;
        }

        private static bool IsSharedInteriorPoint(
            Vector3 point,
            WorldSolid first,
            WorldSolid second,
            float minimumClearanceSquared)
        {
            return IsStrictlyInside(point, first, minimumClearanceSquared)
                && IsStrictlyInside(point, second, minimumClearanceSquared);
        }

        private static bool IsStrictlyInside(
            Vector3 point,
            WorldSolid solid,
            float minimumClearanceSquared)
        {
            if (!solid.Bounds.Contains(point))
            {
                return false;
            }

            var angle = 0.0;
            for (var triangle = 0; triangle < solid.Triangles.Length; triangle += 3)
            {
                var first = solid.Vertices[solid.Triangles[triangle]] - point;
                var second = solid.Vertices[solid.Triangles[triangle + 1]] - point;
                var third = solid.Vertices[solid.Triangles[triangle + 2]] - point;

                var firstX = (double)first.x;
                var firstY = (double)first.y;
                var firstZ = (double)first.z;
                var secondX = (double)second.x;
                var secondY = (double)second.y;
                var secondZ = (double)second.z;
                var thirdX = (double)third.x;
                var thirdY = (double)third.y;
                var thirdZ = (double)third.z;

                var firstLength = Math.Sqrt(
                    firstX * firstX + firstY * firstY + firstZ * firstZ);
                var secondLength = Math.Sqrt(
                    secondX * secondX + secondY * secondY + secondZ * secondZ);
                var thirdLength = Math.Sqrt(
                    thirdX * thirdX + thirdY * thirdY + thirdZ * thirdZ);
                var numerator = firstX * (secondY * thirdZ - secondZ * thirdY)
                    + firstY * (secondZ * thirdX - secondX * thirdZ)
                    + firstZ * (secondX * thirdY - secondY * thirdX);
                var denominator = firstLength * secondLength * thirdLength
                    + (firstX * secondX + firstY * secondY + firstZ * secondZ) * thirdLength
                    + (secondX * thirdX + secondY * thirdY + secondZ * thirdZ) * firstLength
                    + (thirdX * firstX + thirdY * firstY + thirdZ * firstZ) * secondLength;
                angle += 2.0 * Math.Atan2(numerator, denominator);
            }

            if (double.IsNaN(angle)
                || double.IsInfinity(angle)
                || Math.Abs(angle) < StrictInsideAngle)
            {
                return false;
            }

            for (var triangle = 0; triangle < solid.Triangles.Length; triangle += 3)
            {
                var first = solid.Vertices[solid.Triangles[triangle]];
                var second = solid.Vertices[solid.Triangles[triangle + 1]];
                var third = solid.Vertices[solid.Triangles[triangle + 2]];
                if (PointTriangleDistanceSquared(point, first, second, third)
                    < minimumClearanceSquared)
                {
                    return false;
                }
            }

            return true;
        }

        private static float GetMinimumDimension(Bounds bounds)
        {
            return Mathf.Min(bounds.size.x, Mathf.Min(bounds.size.y, bounds.size.z));
        }

        private static float PointTriangleDistanceSquared(
            Vector3 point,
            Vector3 first,
            Vector3 second,
            Vector3 third)
        {
            var firstToSecond = second - first;
            var firstToThird = third - first;
            var firstToPoint = point - first;
            var firstProjection = Vector3.Dot(firstToSecond, firstToPoint);
            var secondProjection = Vector3.Dot(firstToThird, firstToPoint);
            if (firstProjection <= 0f && secondProjection <= 0f)
            {
                return firstToPoint.sqrMagnitude;
            }

            var secondToPoint = point - second;
            var thirdProjection = Vector3.Dot(firstToSecond, secondToPoint);
            var fourthProjection = Vector3.Dot(firstToThird, secondToPoint);
            if (thirdProjection >= 0f && fourthProjection <= thirdProjection)
            {
                return secondToPoint.sqrMagnitude;
            }

            var firstEdgeRegion = firstProjection * fourthProjection
                - thirdProjection * secondProjection;
            if (firstEdgeRegion <= 0f && firstProjection >= 0f && thirdProjection <= 0f)
            {
                var factor = firstProjection / (firstProjection - thirdProjection);
                return (point - (first + factor * firstToSecond)).sqrMagnitude;
            }

            var thirdToPoint = point - third;
            var fifthProjection = Vector3.Dot(firstToSecond, thirdToPoint);
            var sixthProjection = Vector3.Dot(firstToThird, thirdToPoint);
            if (sixthProjection >= 0f && fifthProjection <= sixthProjection)
            {
                return thirdToPoint.sqrMagnitude;
            }

            var secondEdgeRegion = fifthProjection * secondProjection
                - firstProjection * sixthProjection;
            if (secondEdgeRegion <= 0f && secondProjection >= 0f && sixthProjection <= 0f)
            {
                var factor = secondProjection / (secondProjection - sixthProjection);
                return (point - (first + factor * firstToThird)).sqrMagnitude;
            }

            var thirdEdgeRegion = thirdProjection * sixthProjection
                - fifthProjection * fourthProjection;
            if (thirdEdgeRegion <= 0f
                && fourthProjection - thirdProjection >= 0f
                && fifthProjection - sixthProjection >= 0f)
            {
                var factor = (fourthProjection - thirdProjection)
                    / ((fourthProjection - thirdProjection) + (fifthProjection - sixthProjection));
                return (point - (second + factor * (third - second))).sqrMagnitude;
            }

            var denominator = 1f / (firstEdgeRegion + secondEdgeRegion + thirdEdgeRegion);
            var secondWeight = secondEdgeRegion * denominator;
            var thirdWeight = thirdEdgeRegion * denominator;
            var closest = first + firstToSecond * secondWeight + firstToThird * thirdWeight;
            return (point - closest).sqrMagnitude;
        }

        internal sealed class WorldSolid
        {
            internal WorldSolid(
                Vector3[] vertices,
                int[] triangles,
                Bounds bounds)
            {
                Vertices = vertices;
                Triangles = triangles;
                Bounds = bounds;
            }

            internal Vector3[] Vertices { get; }
            internal int[] Triangles { get; }
            internal Bounds Bounds { get; }
        }
    }
}
