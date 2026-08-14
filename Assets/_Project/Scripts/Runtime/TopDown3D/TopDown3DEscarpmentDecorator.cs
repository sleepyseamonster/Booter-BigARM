using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BooterBigArm.TopDown3D
{
    public readonly struct TopDown3DEscarpmentWallData
    {
        public TopDown3DEscarpmentWallData(Vector3 center, Vector3 size, Quaternion rotation)
        {
            Center = center;
            Size = size;
            Rotation = rotation;
        }

        public Vector3 Center { get; }
        public Vector3 Size { get; }
        public Quaternion Rotation { get; }
    }

    public readonly struct TopDown3DEscarpmentMeshData
    {
        public TopDown3DEscarpmentMeshData(
            Vector3[] vertices,
            Vector3[] normals,
            int[] triangles,
            TopDown3DEscarpmentWallData[] walls)
        {
            Vertices = vertices;
            Normals = normals;
            Triangles = triangles;
            Walls = walls;
        }

        public Vector3[] Vertices { get; }
        public Vector3[] Normals { get; }
        public int[] Triangles { get; }
        public TopDown3DEscarpmentWallData[] Walls { get; }
    }

    public static class TopDown3DEscarpmentDecorator
    {
        private const float SurfaceOffset = 0.025f;
        private const float ColliderVerticalOverlap = 0.035f;
        private const float MaximumTraversalColliderHeight = 0.78f;

        public static TopDown3DEscarpmentMeshData BuildData(
            TopDown3DWorldSettings settings,
            Vector2Int chunkCoordinate)
        {
            if (settings == null)
            {
                return new TopDown3DEscarpmentMeshData(
                    new Vector3[0],
                    new Vector3[0],
                    new int[0],
                    new TopDown3DEscarpmentWallData[0]);
            }

            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var triangles = new List<int>();
            var walls = new List<TopDown3DEscarpmentWallData>();
            var features = new List<TopDown3DEscarpmentFeature>();
            var chunkOrigin = new Vector2(
                chunkCoordinate.x * settings.ChunkSize,
                chunkCoordinate.y * settings.ChunkSize);
            TopDown3DEscarpmentSampler.CollectFeatures(
                settings,
                new Rect(chunkOrigin.x, chunkOrigin.y, settings.ChunkSize, settings.ChunkSize),
                features);

            for (var featureIndex = 0; featureIndex < features.Count; featureIndex++)
            {
                AppendFeature(
                    settings,
                    chunkCoordinate,
                    chunkOrigin,
                    features[featureIndex],
                    vertices,
                    normals,
                    triangles,
                    walls);
            }

            return new TopDown3DEscarpmentMeshData(
                vertices.ToArray(),
                normals.ToArray(),
                triangles.ToArray(),
                walls.ToArray());
        }

        public static void Decorate(
            TopDown3DGeneratedChunk chunk,
            TopDown3DWorldSettings settings,
            Material rockMaterial)
        {
            if (chunk == null || settings == null)
            {
                return;
            }

            var data = BuildData(settings, chunk.Coordinate);
            if (data.Vertices.Length > 0)
            {
                var mesh = new Mesh
                {
                    name = $"Escarpment Faces {chunk.Coordinate.x},{chunk.Coordinate.y}"
                };
                mesh.SetVertices(data.Vertices);
                mesh.SetNormals(data.Normals);
                mesh.SetTriangles(data.Triangles, 0, true);
                mesh.RecalculateBounds();
                chunk.RegisterGeneratedMesh(mesh);

                var faceObject = new GameObject("Rocky Escarpment Faces");
                faceObject.layer = chunk.gameObject.layer;
                faceObject.transform.SetParent(chunk.transform, false);
                var filter = faceObject.AddComponent<MeshFilter>();
                filter.sharedMesh = mesh;
                var renderer = faceObject.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = rockMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }

            // The streamed terrain MeshCollider already contains this exact elevation.
            // A second overlapping wall collider creates seams that snag a moving capsule,
            // so the rock-face strip remains presentation-only.
        }

        private static void AppendFeature(
            TopDown3DWorldSettings settings,
            Vector2Int chunkCoordinate,
            Vector2 chunkOrigin,
            TopDown3DEscarpmentFeature feature,
            ICollection<Vector3> vertices,
            ICollection<Vector3> normals,
            ICollection<int> triangles,
            ICollection<TopDown3DEscarpmentWallData> walls)
        {
            var segmentCount = Mathf.Max(16, settings.EscarpmentFaceSegments);
            var included = new bool[segmentCount];
            var geometry = new SegmentGeometry[segmentCount];
            for (var segment = 0; segment < segmentCount; segment++)
            {
                var angleA = segment / (float)segmentCount * Mathf.PI * 2f;
                var angleB = (segment + 1) / (float)segmentCount * Mathf.PI * 2f;
                var midpoint = feature.SampleBoundary((angleA + angleB) * 0.5f, 0f);
                var owner = new Vector2Int(
                    Mathf.FloorToInt(midpoint.x / settings.ChunkSize),
                    Mathf.FloorToInt(midpoint.y / settings.ChunkSize));
                if (owner != chunkCoordinate)
                {
                    continue;
                }

                included[segment] = true;
                geometry[segment] = BuildSegment(
                    settings,
                    chunkOrigin,
                    feature,
                    angleA,
                    angleB,
                    vertices,
                    normals,
                    triangles);
            }

            AppendWallRuns(
                settings,
                included,
                geometry,
                walls);
        }

        private static SegmentGeometry BuildSegment(
            TopDown3DWorldSettings settings,
            Vector2 chunkOrigin,
            TopDown3DEscarpmentFeature feature,
            float angleA,
            float angleB,
            ICollection<Vector3> vertices,
            ICollection<Vector3> normals,
            ICollection<int> triangles)
        {
            var halfWidth = feature.EdgeWidth * 0.58f;
            var outerA = SampleSurfacePoint(settings, chunkOrigin, feature, angleA, halfWidth);
            var outerB = SampleSurfacePoint(settings, chunkOrigin, feature, angleB, halfWidth);
            var middleA = SampleSurfacePoint(settings, chunkOrigin, feature, angleA, 0f);
            var middleB = SampleSurfacePoint(settings, chunkOrigin, feature, angleB, 0f);
            var innerA = SampleSurfacePoint(settings, chunkOrigin, feature, angleA, -halfWidth);
            var innerB = SampleSurfacePoint(settings, chunkOrigin, feature, angleB, -halfWidth);

            AddQuad(vertices, normals, triangles, outerA, middleA, middleB, outerB);
            AddQuad(vertices, normals, triangles, middleA, innerA, innerB, middleB);

            var worldBoundaryA = feature.SampleBoundary(angleA, 0f);
            var worldBoundaryB = feature.SampleBoundary(angleB, 0f);
            var minimumY = Mathf.Min(outerA.y, outerB.y) - ColliderVerticalOverlap;
            var maximumY = Mathf.Max(innerA.y, innerB.y) + ColliderVerticalOverlap;
            return new SegmentGeometry(
                new Vector3(worldBoundaryA.x - chunkOrigin.x, 0f, worldBoundaryA.y - chunkOrigin.y),
                new Vector3(worldBoundaryB.x - chunkOrigin.x, 0f, worldBoundaryB.y - chunkOrigin.y),
                minimumY,
                maximumY,
                feature.EdgeWidth);
        }

        private static Vector3 SampleSurfacePoint(
            TopDown3DWorldSettings settings,
            Vector2 chunkOrigin,
            TopDown3DEscarpmentFeature feature,
            float angle,
            float radialOffset)
        {
            var point = feature.SampleBoundary(angle, radialOffset);
            return new Vector3(
                point.x - chunkOrigin.x,
                TopDown3DHeightSampler.SampleHeight(settings, point.x, point.y) + SurfaceOffset,
                point.y - chunkOrigin.y);
        }

        private static void AppendWallRuns(
            TopDown3DWorldSettings settings,
            IReadOnlyList<bool> included,
            IReadOnlyList<SegmentGeometry> geometry,
            ICollection<TopDown3DEscarpmentWallData> walls)
        {
            var maximumRun = Mathf.Max(1, settings.EscarpmentColliderSegmentsPerRun);
            var index = 0;
            while (index < included.Count)
            {
                if (!included[index])
                {
                    index++;
                    continue;
                }

                var runStart = index;
                var runEnd = index;
                while (runEnd + 1 < included.Count
                    && included[runEnd + 1]
                    && runEnd - runStart + 1 < maximumRun)
                {
                    runEnd++;
                }

                AppendWall(geometry, runStart, runEnd, walls);
                index = runEnd + 1;
            }
        }

        private static void AppendWall(
            IReadOnlyList<SegmentGeometry> geometry,
            int first,
            int last,
            ICollection<TopDown3DEscarpmentWallData> walls)
        {
            var start = geometry[first].Start;
            var end = geometry[last].End;
            var direction = end - start;
            direction.y = 0f;
            var length = direction.magnitude;
            if (length <= 0.05f)
            {
                return;
            }

            var minimumY = float.PositiveInfinity;
            var maximumY = float.NegativeInfinity;
            var maximumDepth = 0f;
            for (var index = first; index <= last; index++)
            {
                minimumY = Mathf.Min(minimumY, geometry[index].MinimumY);
                maximumY = Mathf.Max(maximumY, geometry[index].MaximumY);
                maximumDepth = Mathf.Max(maximumDepth, geometry[index].Depth);
            }

            var height = Mathf.Max(0.08f, maximumY - minimumY);
            if (height > MaximumTraversalColliderHeight && first < last)
            {
                var middle = (first + last) / 2;
                AppendWall(geometry, first, middle, walls);
                AppendWall(geometry, middle + 1, last, walls);
                return;
            }

            height = Mathf.Min(height, MaximumTraversalColliderHeight);
            var center = (start + end) * 0.5f;
            center.y = maximumY - height * 0.5f;
            var rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            walls.Add(new TopDown3DEscarpmentWallData(
                center,
                new Vector3(maximumDepth * 1.25f, height, length + 0.16f),
                rotation));
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
            AddTriangle(vertices, normals, triangles, a, b, c);
            AddTriangle(vertices, normals, triangles, a, c, d);
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

        private readonly struct SegmentGeometry
        {
            public SegmentGeometry(Vector3 start, Vector3 end, float minimumY, float maximumY, float depth)
            {
                Start = start;
                End = end;
                MinimumY = minimumY;
                MaximumY = maximumY;
                Depth = depth;
            }

            public Vector3 Start { get; }
            public Vector3 End { get; }
            public float MinimumY { get; }
            public float MaximumY { get; }
            public float Depth { get; }
        }
    }
}
