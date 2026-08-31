using System;
using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    internal static class TopDown3DRockMeshTopology
    {
        // Manifold can emit extremely narrow but still representable intersection slivers.
        // Measure in double precision and reject only a true zero-area triangle.
        private const double MinimumVolume = 1e-12;
        private static readonly Dictionary<int, TopDown3DIndexedMeshData> Cache =
            new Dictionary<int, TopDown3DIndexedMeshData>();

        internal static TopDown3DIndexedMeshData Get(
            TopDown3DNaturalObjectCatalog catalog,
            TopDown3DNaturalObjectShape shape,
            int variant)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            variant = TopDown3DNaturalObjectCatalog.NormalizeMeshVariant(variant);
            var source = catalog.GetRequiredLod0Data(shape, variant);
            var key = source.Mesh.GetInstanceID();
            if (Cache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var normalized = Normalize(source.Vertices, source.Triangles);
            var report = Validate(normalized);
            if (!report.IsValid)
            {
                throw new InvalidOperationException(
                    $"Natural rock topology {shape}:{variant} is invalid: {report.Error}");
            }

            Cache.Add(key, normalized);
            return normalized;
        }

        internal static TopDown3DIndexedMeshData Normalize(
            IReadOnlyList<Vector3> sourceVertices,
            IReadOnlyList<int> sourceTriangles)
        {
            if (sourceVertices == null)
            {
                throw new ArgumentNullException(nameof(sourceVertices));
            }

            if (sourceTriangles == null)
            {
                throw new ArgumentNullException(nameof(sourceTriangles));
            }

            if (sourceTriangles.Count % 3 != 0)
            {
                throw new ArgumentException("Triangle index count must be divisible by three.", nameof(sourceTriangles));
            }

            var vertices = new List<Vector3>(sourceVertices.Count);
            var indexByPosition = new Dictionary<Vector3, int>();
            var triangles = new int[sourceTriangles.Count];
            for (var i = 0; i < sourceTriangles.Count; i++)
            {
                var sourceIndex = sourceTriangles[i];
                if (sourceIndex < 0 || sourceIndex >= sourceVertices.Count)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(sourceTriangles),
                        $"Triangle index {sourceIndex} is outside the source vertex array.");
                }

                var position = sourceVertices[sourceIndex];
                if (!indexByPosition.TryGetValue(position, out var normalizedIndex))
                {
                    normalizedIndex = vertices.Count;
                    vertices.Add(position);
                    indexByPosition.Add(position, normalizedIndex);
                }

                triangles[i] = normalizedIndex;
            }

            var result = new TopDown3DIndexedMeshData(vertices.ToArray(), triangles);
            var report = Validate(result);
            if (report.IsValid && report.SignedVolume < 0.0)
            {
                ReverseWinding(triangles);
                result = new TopDown3DIndexedMeshData(vertices.ToArray(), triangles);
            }

            return result;
        }

        internal static TopDown3DMeshTopologyReport Validate(TopDown3DIndexedMeshData mesh)
        {
            if (mesh == null)
            {
                return Invalid("Mesh data is null.");
            }

            if (mesh.Vertices.Length < 4)
            {
                return Invalid("A closed solid needs at least four vertices.");
            }

            if (mesh.Triangles.Length == 0 || mesh.Triangles.Length % 3 != 0)
            {
                return Invalid("Triangle index count is empty or not divisible by three.");
            }

            for (var i = 0; i < mesh.Vertices.Length; i++)
            {
                if (!IsFinite(mesh.Vertices[i]))
                {
                    return Invalid($"Vertex {i} is not finite.");
                }
            }

            var edgeUses = new Dictionary<Edge, EdgeUse>();
            var volume = 0.0;
            for (var triangle = 0; triangle < mesh.Triangles.Length; triangle += 3)
            {
                var aIndex = mesh.Triangles[triangle];
                var bIndex = mesh.Triangles[triangle + 1];
                var cIndex = mesh.Triangles[triangle + 2];
                if (!IsValidIndex(aIndex, mesh.Vertices.Length)
                    || !IsValidIndex(bIndex, mesh.Vertices.Length)
                    || !IsValidIndex(cIndex, mesh.Vertices.Length))
                {
                    return Invalid($"Triangle {triangle / 3} has an out-of-range vertex index.");
                }

                if (aIndex == bIndex || bIndex == cIndex || cIndex == aIndex)
                {
                    return Invalid($"Triangle {triangle / 3} repeats a vertex index.");
                }

                var a = mesh.Vertices[aIndex];
                var b = mesh.Vertices[bIndex];
                var c = mesh.Vertices[cIndex];
                var crossSquared = GetTriangleCrossSquared(a, b, c);
                if (double.IsNaN(crossSquared)
                    || double.IsInfinity(crossSquared)
                    || crossSquared <= 0.0)
                {
                    return Invalid(
                        $"Triangle {triangle / 3} is degenerate " +
                        $"(cross squared {crossSquared:R}).");
                }

                volume += GetSignedTetrahedronVolume(a, b, c);
                AddEdge(edgeUses, aIndex, bIndex);
                AddEdge(edgeUses, bIndex, cIndex);
                AddEdge(edgeUses, cIndex, aIndex);
            }

            foreach (var pair in edgeUses)
            {
                if (pair.Value.Count != 2 || pair.Value.DirectionBalance != 0)
                {
                    return Invalid(
                        $"Edge {pair.Key.Min}-{pair.Key.Max} is not used exactly once in each direction.");
                }
            }

            if (Math.Abs(volume) <= MinimumVolume)
            {
                return Invalid("Signed volume is zero or too small.");
            }

            return new TopDown3DMeshTopologyReport(
                true,
                null,
                mesh.Vertices.Length,
                mesh.Triangles.Length / 3,
                volume);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache()
        {
            Cache.Clear();
        }

        private static void AddEdge(IDictionary<Edge, EdgeUse> uses, int from, int to)
        {
            var edge = new Edge(from, to);
            uses.TryGetValue(edge, out var use);
            use.Count++;
            use.DirectionBalance += from < to ? 1 : -1;
            uses[edge] = use;
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

        private static bool IsValidIndex(int index, int vertexCount)
        {
            return index >= 0 && index < vertexCount;
        }

        private static double GetTriangleCrossSquared(Vector3 a, Vector3 b, Vector3 c)
        {
            var firstX = (double)b.x - a.x;
            var firstY = (double)b.y - a.y;
            var firstZ = (double)b.z - a.z;
            var secondX = (double)c.x - a.x;
            var secondY = (double)c.y - a.y;
            var secondZ = (double)c.z - a.z;
            var crossX = firstY * secondZ - firstZ * secondY;
            var crossY = firstZ * secondX - firstX * secondZ;
            var crossZ = firstX * secondY - firstY * secondX;
            return crossX * crossX + crossY * crossY + crossZ * crossZ;
        }

        private static double GetSignedTetrahedronVolume(Vector3 a, Vector3 b, Vector3 c)
        {
            var crossX = (double)b.y * c.z - (double)b.z * c.y;
            var crossY = (double)b.z * c.x - (double)b.x * c.z;
            var crossZ = (double)b.x * c.y - (double)b.y * c.x;
            return ((double)a.x * crossX + (double)a.y * crossY + (double)a.z * crossZ) / 6.0;
        }

        private static bool IsFinite(Vector3 value)
        {
            return !float.IsNaN(value.x) && !float.IsInfinity(value.x)
                && !float.IsNaN(value.y) && !float.IsInfinity(value.y)
                && !float.IsNaN(value.z) && !float.IsInfinity(value.z);
        }

        private static TopDown3DMeshTopologyReport Invalid(string error)
        {
            return new TopDown3DMeshTopologyReport(false, error, 0, 0, 0.0);
        }

        private readonly struct Edge : IEquatable<Edge>
        {
            public Edge(int a, int b)
            {
                Min = Math.Min(a, b);
                Max = Math.Max(a, b);
            }

            public int Min { get; }
            public int Max { get; }

            public bool Equals(Edge other)
            {
                return Min == other.Min && Max == other.Max;
            }

            public override bool Equals(object obj)
            {
                return obj is Edge other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return Min * 397 ^ Max;
                }
            }
        }

        private struct EdgeUse
        {
            public int Count;
            public int DirectionBalance;
        }
    }
}
