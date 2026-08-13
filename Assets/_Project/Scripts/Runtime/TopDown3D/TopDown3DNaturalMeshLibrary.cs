using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public static class TopDown3DNaturalMeshLibrary
    {
        private const int VariantsPerShape = 6;
        private static readonly Dictionary<int, MeshEntry> Meshes = new Dictionary<int, MeshEntry>();

        public static Mesh GetMesh(TopDown3DNaturalObjectShape shape, int variant)
        {
            return GetData(shape, variant).Mesh;
        }

        internal static MeshData GetData(TopDown3DNaturalObjectShape shape, int variant)
        {
            variant = Mathf.Abs(variant) % VariantsPerShape;
            var key = (int)shape * VariantsPerShape + variant;
            if (!Meshes.TryGetValue(key, out var entry) || entry.Mesh == null)
            {
                entry = BuildMesh(shape, variant);
                Meshes[key] = entry;
            }

            return entry.Data;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache()
        {
            foreach (var entry in Meshes.Values)
            {
                if (entry.Mesh != null)
                {
                    Object.Destroy(entry.Mesh);
                }
            }

            Meshes.Clear();
        }

        private static MeshEntry BuildMesh(TopDown3DNaturalObjectShape shape, int variant)
        {
            GetShape(shape, out var segments, out var baseRadius, out var middleRadius, out var upperRadius, out var height);
            var rings = new Vector3[3, segments];
            for (var ring = 0; ring < 3; ring++)
            {
                var radius = ring == 0 ? baseRadius : ring == 1 ? middleRadius : upperRadius;
                var y = ring == 0 ? 0.04f : ring == 1 ? height * 0.42f : height * 0.78f;
                for (var segment = 0; segment < segments; segment++)
                {
                    var angle = ((segment + RingOffset(shape, variant, ring)) / segments) * Mathf.PI * 2f;
                    var radialNoise = Mathf.Lerp(0.78f, 1.16f, Hash01((int)shape, variant, ring, segment));
                    var heightNoise = ring == 0
                        ? 0f
                        : Mathf.Lerp(-0.06f, 0.06f, Hash01(variant, segment, ring, 941));
                    rings[ring, segment] = new Vector3(
                        Mathf.Cos(angle) * radius * radialNoise,
                        y + height * heightNoise,
                        Mathf.Sin(angle) * radius * radialNoise);
                }
            }

            var top = new Vector3(
                Mathf.Lerp(-0.16f, 0.16f, Hash01(variant, (int)shape, 701, 17)),
                height,
                Mathf.Lerp(-0.16f, 0.16f, Hash01(variant, (int)shape, 307, 29)));
            var bottom = new Vector3(0f, 0f, 0f);
            var vertices = new List<Vector3>(segments * 24);
            var normals = new List<Vector3>(segments * 24);
            var triangles = new List<int>(segments * 24);

            for (var segment = 0; segment < segments; segment++)
            {
                var next = (segment + 1) % segments;
                AddTriangle(vertices, normals, triangles, bottom, rings[0, segment], rings[0, next]);
                AddQuad(vertices, normals, triangles, rings[0, segment], rings[0, next], rings[1, next], rings[1, segment]);
                AddQuad(vertices, normals, triangles, rings[1, segment], rings[1, next], rings[2, next], rings[2, segment]);
                AddTriangle(vertices, normals, triangles, rings[2, segment], top, rings[2, next]);
            }

            var mesh = new Mesh
            {
                name = $"Natural {shape} {variant}",
                hideFlags = HideFlags.HideAndDontSave
            };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0, true);
            mesh.RecalculateBounds();
            return new MeshEntry(
                mesh,
                new MeshData(mesh, vertices.ToArray(), normals.ToArray(), triangles.ToArray()));
        }

        private static void AddQuad(
            ICollection<Vector3> vertices,
            ICollection<Vector3> normals,
            ICollection<int> triangles,
            Vector3 a,
            Vector3 b,
            Vector3 c,
            Vector3 d)
        {
            AddTriangle(vertices, normals, triangles, a, c, b);
            AddTriangle(vertices, normals, triangles, a, d, c);
        }

        private static void AddTriangle(
            ICollection<Vector3> vertices,
            ICollection<Vector3> normals,
            ICollection<int> triangles,
            Vector3 a,
            Vector3 b,
            Vector3 c)
        {
            var normal = Vector3.Cross(b - a, c - a).normalized;
            var index = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);
            triangles.Add(index);
            triangles.Add(index + 1);
            triangles.Add(index + 2);
        }

        private static void GetShape(
            TopDown3DNaturalObjectShape shape,
            out int segments,
            out float baseRadius,
            out float middleRadius,
            out float upperRadius,
            out float height)
        {
            switch (shape)
            {
                case TopDown3DNaturalObjectShape.Pebble:
                    segments = 7;
                    baseRadius = 0.72f;
                    middleRadius = 0.84f;
                    upperRadius = 0.56f;
                    height = 0.46f;
                    break;
                case TopDown3DNaturalObjectShape.Shard:
                    segments = 6;
                    baseRadius = 0.58f;
                    middleRadius = 0.74f;
                    upperRadius = 0.34f;
                    height = 1.05f;
                    break;
                case TopDown3DNaturalObjectShape.Slab:
                    segments = 7;
                    baseRadius = 0.86f;
                    middleRadius = 1f;
                    upperRadius = 0.82f;
                    height = 0.34f;
                    break;
                case TopDown3DNaturalObjectShape.Nodule:
                    segments = 8;
                    baseRadius = 0.62f;
                    middleRadius = 0.88f;
                    upperRadius = 0.72f;
                    height = 0.72f;
                    break;
                default:
                    segments = 8;
                    baseRadius = 0.68f;
                    middleRadius = 0.96f;
                    upperRadius = 0.63f;
                    height = 1f;
                    break;
            }
        }

        private static float RingOffset(TopDown3DNaturalObjectShape shape, int variant, int ring)
        {
            return Hash01((int)shape, variant, ring, 193) * 0.42f;
        }

        private static float Hash01(int a, int b, int c, int d)
        {
            unchecked
            {
                var hash = (uint)a * 0x9E3779B9u;
                hash ^= (uint)b * 0x85EBCA6Bu;
                hash ^= (uint)c * 0xC2B2AE35u;
                hash ^= (uint)d * 0x27D4EB2Fu;
                hash ^= hash >> 16;
                hash *= 0x7FEB352Du;
                hash ^= hash >> 15;
                return (hash & 0x00FFFFFFu) / 16777215f;
            }
        }

        internal readonly struct MeshData
        {
            public MeshData(Mesh mesh, Vector3[] vertices, Vector3[] normals, int[] triangles)
            {
                Mesh = mesh;
                Vertices = vertices;
                Normals = normals;
                Triangles = triangles;
            }

            public Mesh Mesh { get; }
            public Vector3[] Vertices { get; }
            public Vector3[] Normals { get; }
            public int[] Triangles { get; }
        }

        private readonly struct MeshEntry
        {
            public MeshEntry(Mesh mesh, MeshData data)
            {
                Mesh = mesh;
                Data = data;
            }

            public Mesh Mesh { get; }
            public MeshData Data { get; }
        }
    }
}
