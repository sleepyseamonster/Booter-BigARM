using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BooterBigArm.TopDown3D
{
    internal static class TopDown3DRockFormationMeshBuilder
    {
        internal const int FusionAlgorithmVersion = 2;

        internal static bool TryBuild(
            TopDown3DRockFormationPlan formation,
            TopDown3DNaturalObjectCatalog catalog,
            Matrix4x4 worldToRoot,
            out TopDown3DRockFormationMeshData result,
            out string error)
        {
            result = null;
            error = null;
            if (formation == null || formation.Members.Count == 0)
            {
                error = "A rock formation mesh requires at least one member.";
                return false;
            }

            if (formation.Members.Count == 1)
            {
                result = BuildSingleMember(catalog, formation.Members[0], worldToRoot);
                return true;
            }

            IReadOnlyList<TopDown3DIndexedMeshData> solids;
            try
            {
                solids = PrepareUnionSolids(formation, catalog, worldToRoot);
            }
            catch (Exception exception)
            {
                error = $"Could not prepare formation {formation.StableId}: {exception.Message}";
                return false;
            }

            return TryBuildUnion(solids, out result, out error);
        }

        internal static IReadOnlyList<TopDown3DIndexedMeshData> PrepareUnionSolids(
            TopDown3DRockFormationPlan formation,
            TopDown3DNaturalObjectCatalog catalog,
            Matrix4x4 worldToRoot)
        {
            if (formation == null || formation.Members.Count < 2)
            {
                throw new ArgumentException("Union input requires a multi-member formation.", nameof(formation));
            }

            var solids = new TopDown3DIndexedMeshData[formation.Members.Count];
            for (var memberIndex = 0; memberIndex < formation.Members.Count; memberIndex++)
            {
                var member = formation.Members[memberIndex];
                var source = TopDown3DRockMeshTopology.Get(catalog, member.Shape, member.Variant);
                var matrix = worldToRoot * Matrix4x4.TRS(
                    member.Position,
                    member.Rotation,
                    member.Scale);
                var vertices = new Vector3[source.Vertices.Length];
                for (var vertex = 0; vertex < source.Vertices.Length; vertex++)
                {
                    vertices[vertex] = matrix.MultiplyPoint3x4(source.Vertices[vertex]);
                }

                var triangles = (int[])source.Triangles.Clone();
                if (matrix.determinant < 0f)
                {
                    ReverseWinding(triangles);
                }

                var solid = new TopDown3DIndexedMeshData(vertices, triangles);
                var report = TopDown3DRockMeshTopology.Validate(solid);
                if (!report.IsValid || report.SignedVolume <= 0.0)
                {
                    throw new InvalidOperationException(
                        $"Member {member.StableId} transformed into invalid topology: {report.Error}");
                }

                solids[memberIndex] = solid;
            }

            return solids;
        }

        internal static bool TryBuildUnion(
            IReadOnlyList<TopDown3DIndexedMeshData> solids,
            out TopDown3DRockFormationMeshData result,
            out string error)
        {
            result = null;
            if (!TopDown3DManifoldNative.TryUnionMany(
                    solids,
                    out var union,
                    out _,
                    out error))
            {
                return false;
            }

            try
            {
                var normals = BuildAreaWeightedNormals(union);
                var bounds = CalculateBounds(union.Vertices);
                result = new TopDown3DRockFormationMeshData(
                    union.Vertices,
                    normals,
                    union.Triangles,
                    bounds);
                return true;
            }
            catch (Exception exception)
            {
                error = $"Could not finalize fused formation mesh: {exception.Message}";
                return false;
            }
        }

        internal static Mesh CreateUnityMesh(TopDown3DRockFormationMeshData data, string name)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var mesh = new Mesh { name = name };
            if (data.Vertices.Length > ushort.MaxValue)
            {
                mesh.indexFormat = IndexFormat.UInt32;
            }

            mesh.vertices = data.Vertices;
            mesh.normals = data.Normals;
            mesh.triangles = data.Triangles;
            mesh.bounds = data.Bounds;
            return mesh;
        }

        internal static ulong ComputeDeterministicHash(TopDown3DRockFormationMeshData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            const ulong offset = 14695981039346656037UL;
            var hash = offset;
            for (var i = 0; i < data.Vertices.Length; i++)
            {
                hash = Add(hash, BitConverter.SingleToInt32Bits(data.Vertices[i].x));
                hash = Add(hash, BitConverter.SingleToInt32Bits(data.Vertices[i].y));
                hash = Add(hash, BitConverter.SingleToInt32Bits(data.Vertices[i].z));
            }

            for (var i = 0; i < data.Triangles.Length; i++)
            {
                hash = Add(hash, data.Triangles[i]);
            }

            return hash;
        }

        private static TopDown3DRockFormationMeshData BuildSingleMember(
            TopDown3DNaturalObjectCatalog catalog,
            TopDown3DRockFormationMember member,
            Matrix4x4 worldToRoot)
        {
            var source = catalog.GetRequiredLod0Data(member.Shape, member.Variant);
            var matrix = worldToRoot * Matrix4x4.TRS(
                member.Position,
                member.Rotation,
                member.Scale);
            var normalMatrix = matrix.inverse.transpose;
            var vertices = new Vector3[source.Vertices.Length];
            var normals = new Vector3[source.Normals.Length];
            for (var i = 0; i < source.Vertices.Length; i++)
            {
                vertices[i] = matrix.MultiplyPoint3x4(source.Vertices[i]);
                normals[i] = normalMatrix.MultiplyVector(source.Normals[i]).normalized;
            }

            return new TopDown3DRockFormationMeshData(
                vertices,
                normals,
                (int[])source.Triangles.Clone(),
                CalculateBounds(vertices));
        }

        private static Vector3[] BuildAreaWeightedNormals(TopDown3DIndexedMeshData mesh)
        {
            var normals = new Vector3[mesh.Vertices.Length];
            var normalX = new double[mesh.Vertices.Length];
            var normalY = new double[mesh.Vertices.Length];
            var normalZ = new double[mesh.Vertices.Length];
            for (var triangle = 0; triangle < mesh.Triangles.Length; triangle += 3)
            {
                var a = mesh.Triangles[triangle];
                var b = mesh.Triangles[triangle + 1];
                var c = mesh.Triangles[triangle + 2];
                var firstX = (double)mesh.Vertices[b].x - mesh.Vertices[a].x;
                var firstY = (double)mesh.Vertices[b].y - mesh.Vertices[a].y;
                var firstZ = (double)mesh.Vertices[b].z - mesh.Vertices[a].z;
                var secondX = (double)mesh.Vertices[c].x - mesh.Vertices[a].x;
                var secondY = (double)mesh.Vertices[c].y - mesh.Vertices[a].y;
                var secondZ = (double)mesh.Vertices[c].z - mesh.Vertices[a].z;
                var weightedX = firstY * secondZ - firstZ * secondY;
                var weightedY = firstZ * secondX - firstX * secondZ;
                var weightedZ = firstX * secondY - firstY * secondX;
                normalX[a] += weightedX;
                normalY[a] += weightedY;
                normalZ[a] += weightedZ;
                normalX[b] += weightedX;
                normalY[b] += weightedY;
                normalZ[b] += weightedZ;
                normalX[c] += weightedX;
                normalY[c] += weightedY;
                normalZ[c] += weightedZ;
            }

            for (var i = 0; i < normals.Length; i++)
            {
                var magnitudeSquared = normalX[i] * normalX[i]
                    + normalY[i] * normalY[i]
                    + normalZ[i] * normalZ[i];
                if (double.IsNaN(magnitudeSquared)
                    || double.IsInfinity(magnitudeSquared)
                    || magnitudeSquared <= 0.0)
                {
                    throw new InvalidOperationException($"Fused vertex {i} has no usable surface normal.");
                }

                var inverseMagnitude = 1.0 / Math.Sqrt(magnitudeSquared);
                normals[i] = new Vector3(
                    (float)(normalX[i] * inverseMagnitude),
                    (float)(normalY[i] * inverseMagnitude),
                    (float)(normalZ[i] * inverseMagnitude));
            }

            return normals;
        }

        private static Bounds CalculateBounds(IReadOnlyList<Vector3> vertices)
        {
            if (vertices == null || vertices.Count == 0)
            {
                throw new ArgumentException("Cannot calculate bounds for an empty vertex set.", nameof(vertices));
            }

            var minimum = vertices[0];
            var maximum = vertices[0];
            for (var i = 1; i < vertices.Count; i++)
            {
                minimum = Vector3.Min(minimum, vertices[i]);
                maximum = Vector3.Max(maximum, vertices[i]);
            }

            var bounds = new Bounds();
            bounds.SetMinMax(minimum, maximum);
            return bounds;
        }

        private static void ReverseWinding(IList<int> triangles)
        {
            for (var triangle = 0; triangle < triangles.Count; triangle += 3)
            {
                var temporary = triangles[triangle + 1];
                triangles[triangle + 1] = triangles[triangle + 2];
                triangles[triangle + 2] = temporary;
            }
        }

        private static ulong Add(ulong hash, int value)
        {
            unchecked
            {
                const ulong prime = 1099511628211UL;
                hash ^= (uint)value;
                return hash * prime;
            }
        }
    }
}
