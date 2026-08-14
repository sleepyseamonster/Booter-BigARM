using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BooterBigArm.TopDown3D
{
    public readonly struct TopDown3DEscarpmentSurfaceData
    {
        public TopDown3DEscarpmentSurfaceData(Vector3[] vertices, Vector3[] normals, int[] triangles)
        {
            Vertices = vertices;
            Normals = normals;
            Triangles = triangles;
        }

        public Vector3[] Vertices { get; }
        public Vector3[] Normals { get; }
        public int[] Triangles { get; }
    }

    /// <summary>
    /// Builds a dense, presentation-only rocky skin over the terrain's escarpment transition.
    /// The streamed terrain mesh remains the sole collision and shadow authority.
    /// </summary>
    public static class TopDown3DEscarpmentSurfaceDecorator
    {
        private const int MinimumAngularSegments = 96;
        private const int AngularDensityMultiplier = 3;
        private const int RadialBands = 6;
        private const float RadialCoverage = 0.85f;
        private const float SurfaceOffset = 0.008f;

        public static TopDown3DEscarpmentSurfaceData BuildData(
            TopDown3DWorldSettings settings,
            Vector2Int chunkCoordinate)
        {
            if (settings == null)
            {
                return new TopDown3DEscarpmentSurfaceData(
                    new Vector3[0],
                    new Vector3[0],
                    new int[0]);
            }

            var features = new List<TopDown3DEscarpmentFeature>();
            var chunkOrigin = new Vector2(
                chunkCoordinate.x * settings.ChunkSize,
                chunkCoordinate.y * settings.ChunkSize);
            TopDown3DEscarpmentSampler.CollectFeatures(
                settings,
                new Rect(chunkOrigin.x, chunkOrigin.y, settings.ChunkSize, settings.ChunkSize),
                features);
            var segmentCount = Mathf.Max(
                MinimumAngularSegments,
                settings.EscarpmentFaceSegments * AngularDensityMultiplier);
            var maximumOwnedSegments = features.Count * segmentCount;
            var maximumVertexCount = maximumOwnedSegments * (RadialBands + 1) * 2;
            var maximumTriangleIndexCount = maximumOwnedSegments * RadialBands * 6;
            var vertices = new List<Vector3>(maximumVertexCount);
            var normals = new List<Vector3>(maximumVertexCount);
            var triangles = new List<int>(maximumTriangleIndexCount);

            for (var index = 0; index < features.Count; index++)
            {
                AppendFeature(
                    settings,
                    chunkCoordinate,
                    chunkOrigin,
                    features[index],
                    vertices,
                    normals,
                    triangles);
            }

            return new TopDown3DEscarpmentSurfaceData(
                vertices.ToArray(),
                normals.ToArray(),
                triangles.ToArray());
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
            if (data.Vertices.Length == 0)
            {
                return;
            }

            var mesh = new Mesh
            {
                name = $"Smooth Escarpment Surface {chunk.Coordinate.x},{chunk.Coordinate.y}"
            };
            mesh.SetVertices(data.Vertices);
            mesh.SetNormals(data.Normals);
            mesh.SetTriangles(data.Triangles, 0, true);
            mesh.RecalculateBounds();
            chunk.RegisterGeneratedMesh(mesh);

            var faceObject = new GameObject("Smooth Rocky Escarpment Surface");
            faceObject.layer = chunk.gameObject.layer;
            faceObject.transform.SetParent(chunk.transform, false);
            var filter = faceObject.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = faceObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = rockMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            if (Application.isPlaying)
            {
                mesh.UploadMeshData(true);
            }
        }

        private static void AppendFeature(
            TopDown3DWorldSettings settings,
            Vector2Int chunkCoordinate,
            Vector2 chunkOrigin,
            TopDown3DEscarpmentFeature feature,
            ICollection<Vector3> vertices,
            ICollection<Vector3> normals,
            ICollection<int> triangles)
        {
            var segmentCount = Mathf.Max(
                MinimumAngularSegments,
                settings.EscarpmentFaceSegments * AngularDensityMultiplier);
            for (var segment = 0; segment < segmentCount; segment++)
            {
                var angleA = segment / (float)segmentCount * Mathf.PI * 2f;
                var angleB = (segment + 1) / (float)segmentCount * Mathf.PI * 2f;
                var midpoint = feature.SampleBoundary((angleA + angleB) * 0.5f, 0f);
                if (WorldToChunk(settings, midpoint) != chunkCoordinate)
                {
                    continue;
                }

                AppendSegment(
                    settings,
                    chunkOrigin,
                    feature,
                    angleA,
                    angleB,
                    vertices,
                    normals,
                    triangles);
            }
        }

        private static void AppendSegment(
            TopDown3DWorldSettings settings,
            Vector2 chunkOrigin,
            TopDown3DEscarpmentFeature feature,
            float angleA,
            float angleB,
            ICollection<Vector3> vertices,
            ICollection<Vector3> normals,
            ICollection<int> triangles)
        {
            var firstVertex = vertices.Count;
            for (var band = 0; band <= RadialBands; band++)
            {
                var radialT = band / (float)RadialBands;
                var radialOffset = Mathf.Lerp(
                    feature.EdgeWidth * RadialCoverage,
                    -feature.EdgeWidth * RadialCoverage,
                    radialT);
                AppendSurfaceVertex(settings, chunkOrigin, feature, angleA, radialOffset, vertices, normals);
                AppendSurfaceVertex(settings, chunkOrigin, feature, angleB, radialOffset, vertices, normals);
            }

            for (var band = 0; band < RadialBands; band++)
            {
                var outerA = firstVertex + band * 2;
                var outerB = outerA + 1;
                var innerA = outerA + 2;
                var innerB = outerA + 3;
                triangles.Add(outerA);
                triangles.Add(innerA);
                triangles.Add(innerB);
                triangles.Add(outerA);
                triangles.Add(innerB);
                triangles.Add(outerB);
            }
        }

        private static void AppendSurfaceVertex(
            TopDown3DWorldSettings settings,
            Vector2 chunkOrigin,
            TopDown3DEscarpmentFeature feature,
            float angle,
            float radialOffset,
            ICollection<Vector3> vertices,
            ICollection<Vector3> normals)
        {
            var worldPoint = feature.SampleBoundary(angle, radialOffset);
            var normalSampleDistance = Mathf.Max(0.12f, feature.EdgeWidth / RadialBands);
            var normal = TopDown3DHeightSampler.SampleNormal(
                settings,
                worldPoint.x,
                worldPoint.y,
                normalSampleDistance);
            vertices.Add(new Vector3(
                worldPoint.x - chunkOrigin.x,
                TopDown3DHeightSampler.SampleHeight(settings, worldPoint.x, worldPoint.y)
                    + SurfaceOffset,
                worldPoint.y - chunkOrigin.y));
            normals.Add(normal);
        }

        private static Vector2Int WorldToChunk(TopDown3DWorldSettings settings, Vector2 worldPoint)
        {
            return new Vector2Int(
                Mathf.FloorToInt(worldPoint.x / settings.ChunkSize),
                Mathf.FloorToInt(worldPoint.y / settings.ChunkSize));
        }
    }
}
