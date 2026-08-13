using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public static class TopDown3DNaturalMeshLibrary
    {
        public const int VariantsPerShape = 12;
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
            var profile = GetShapeProfile(shape);
            var rings = new Vector3[profile.Radii.Length, profile.Segments];
            var xStretch = Mathf.Lerp(0.78f, 1.22f, Hash01((int)shape, variant, 503, 31));
            var zStretch = Mathf.Lerp(0.8f, 1.2f, Hash01((int)shape, variant, 719, 47));
            var fractureAngle = Hash01((int)shape, variant, 887, 59) * Mathf.PI * 2f;
            var fractureNormal = new Vector2(Mathf.Cos(fractureAngle), Mathf.Sin(fractureAngle));
            var fractureLimit = Mathf.Lerp(0.5f, 0.72f, Hash01((int)shape, variant, 997, 71));
            var fractureDepth = Mathf.Lerp(0.1f, 0.24f, Hash01((int)shape, variant, 1069, 83));
            for (var ring = 0; ring < profile.Radii.Length; ring++)
            {
                var center = GetRingCenter(shape, variant, ring, profile.Radii[ring]);
                for (var segment = 0; segment < profile.Segments; segment++)
                {
                    var angleJitter = Mathf.Lerp(
                        -0.28f,
                        0.28f,
                        Hash01((int)shape, variant, segment, 1217));
                    var angle = ((segment + RingOffset(shape, variant, ring) + angleJitter)
                        / profile.Segments) * Mathf.PI * 2f;
                    var silhouetteNoise = Mathf.Lerp(
                        0.8f,
                        1.2f,
                        Hash01((int)shape, variant, segment, 1327));
                    var ringNoise = Mathf.Lerp(
                        0.92f,
                        1.08f,
                        Hash01((int)shape, variant, ring, segment));
                    var heightNoise = ring == 0
                        ? 0f
                        : Mathf.Lerp(-0.035f, 0.035f, Hash01(variant, segment, ring, 941));
                    var radius = profile.Radii[ring] * silhouetteNoise * ringNoise;
                    var point = new Vector3(
                        center.x + Mathf.Cos(angle) * radius * xStretch,
                        profile.Height * (profile.Heights[ring] + heightNoise),
                        center.y + Mathf.Sin(angle) * radius * zStretch);
                    rings[ring, segment] = ApplyFracture(
                        point,
                        fractureNormal,
                        fractureLimit * profile.Radii[1],
                        fractureDepth * profile.Radii[1]);
                }
            }

            var topRing = profile.Radii.Length - 1;
            var topCenter = GetRingCenter(shape, variant, topRing, profile.Radii[topRing]);
            var top = new Vector3(
                topCenter.x + Mathf.Lerp(-0.08f, 0.08f, Hash01(variant, (int)shape, 701, 17)),
                profile.Height * profile.TopHeight,
                topCenter.y + Mathf.Lerp(-0.08f, 0.08f, Hash01(variant, (int)shape, 307, 29)));
            top = ApplyFracture(
                top,
                fractureNormal,
                fractureLimit * profile.Radii[1],
                fractureDepth * profile.Radii[1]);
            var bottom = new Vector3(0f, 0f, 0f);
            var vertices = new List<Vector3>(profile.Segments * 24);
            var normals = new List<Vector3>(profile.Segments * 24);
            var triangles = new List<int>(profile.Segments * 24);

            for (var segment = 0; segment < profile.Segments; segment++)
            {
                var next = (segment + 1) % profile.Segments;
                AddTriangle(vertices, normals, triangles, bottom, rings[0, segment], rings[0, next]);
                for (var ring = 0; ring < topRing; ring++)
                {
                    AddQuad(
                        vertices,
                        normals,
                        triangles,
                        rings[ring, segment],
                        rings[ring, next],
                        rings[ring + 1, next],
                        rings[ring + 1, segment]);
                }

                AddTriangle(vertices, normals, triangles, rings[topRing, segment], top, rings[topRing, next]);
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

        private static Vector2 GetRingCenter(
            TopDown3DNaturalObjectShape shape,
            int variant,
            int ring,
            float radius)
        {
            var drift = radius * (ring + 1) * 0.055f;
            return new Vector2(
                Mathf.Lerp(-drift, drift, Hash01((int)shape, variant, ring, 1433)),
                Mathf.Lerp(-drift, drift, Hash01((int)shape, variant, ring, 1543)));
        }

        private static Vector3 ApplyFracture(
            Vector3 point,
            Vector2 fractureNormal,
            float fractureLimit,
            float fractureDepth)
        {
            var projected = point.x * fractureNormal.x + point.z * fractureNormal.y;
            if (projected <= fractureLimit)
            {
                return point;
            }

            var clippedProjection = fractureLimit - fractureDepth;
            var correction = projected - clippedProjection;
            point.x -= fractureNormal.x * correction;
            point.z -= fractureNormal.y * correction;
            return point;
        }

        private static ShapeProfile GetShapeProfile(TopDown3DNaturalObjectShape shape)
        {
            switch (shape)
            {
                case TopDown3DNaturalObjectShape.Pebble:
                    return new ShapeProfile(
                        8,
                        0.46f,
                        0.94f,
                        new[] { 0.68f, 0.86f, 0.72f, 0.42f },
                        new[] { 0.06f, 0.3f, 0.62f, 0.84f });
                case TopDown3DNaturalObjectShape.Shard:
                    return new ShapeProfile(
                        6,
                        1.05f,
                        0.95f,
                        new[] { 0.54f, 0.74f, 0.5f, 0.2f },
                        new[] { 0.04f, 0.28f, 0.64f, 0.86f });
                case TopDown3DNaturalObjectShape.Slab:
                    return new ShapeProfile(
                        7,
                        0.34f,
                        0.94f,
                        new[] { 0.82f, 1f, 0.94f, 0.76f },
                        new[] { 0.08f, 0.3f, 0.62f, 0.84f });
                case TopDown3DNaturalObjectShape.Nodule:
                    return new ShapeProfile(
                        9,
                        0.72f,
                        0.95f,
                        new[] { 0.6f, 0.88f, 0.82f, 0.52f },
                        new[] { 0.05f, 0.32f, 0.66f, 0.86f });
                default:
                    return new ShapeProfile(
                        9,
                        1f,
                        0.96f,
                        new[] { 0.66f, 0.98f, 0.84f, 0.48f },
                        new[] { 0.04f, 0.3f, 0.65f, 0.86f });
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

        private readonly struct ShapeProfile
        {
            public ShapeProfile(
                int segments,
                float height,
                float topHeight,
                float[] radii,
                float[] heights)
            {
                Segments = segments;
                Height = height;
                TopHeight = topHeight;
                Radii = radii;
                Heights = heights;
            }

            public int Segments { get; }
            public float Height { get; }
            public float TopHeight { get; }
            public float[] Radii { get; }
            public float[] Heights { get; }
        }
    }
}
