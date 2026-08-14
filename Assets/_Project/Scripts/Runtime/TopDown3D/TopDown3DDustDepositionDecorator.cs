using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BooterBigArm.TopDown3D
{
    public readonly struct TopDown3DDustMeshData
    {
        public TopDown3DDustMeshData(Vector3[] vertices, Color32[] colors, int[] triangles)
        {
            Vertices = vertices;
            Colors = colors;
            Triangles = triangles;
        }

        public Vector3[] Vertices { get; }
        public Color32[] Colors { get; }
        public int[] Triangles { get; }
    }

    public static class TopDown3DDustDepositionDecorator
    {
        private const float MinimumVisibleWeight = 0.025f;

        public static void Decorate(
            TopDown3DGeneratedChunk chunk,
            TopDown3DWorldSettings settings,
            Material fallbackMaterial,
            Vector2 spawnExclusionCenter)
        {
            if (chunk == null
                || settings == null
                || (settings.DepositedDustMaterial == null && fallbackMaterial == null))
            {
                return;
            }

            var plan = TopDown3DDustDepositionPlanner.BuildPlan(
                settings,
                settings.NaturalObjectCatalog,
                chunk.Coordinate,
                spawnExclusionCenter);
            if (!plan.HasVisibleDeposits)
            {
                return;
            }

            var meshData = BuildMeshData(settings, chunk.Coordinate, plan);
            if (meshData.Triangles.Length == 0)
            {
                return;
            }

            var mesh = new Mesh
            {
                name = $"Chunk {chunk.Coordinate.x},{chunk.Coordinate.y} Deposited Dust"
            };
            mesh.SetVertices(meshData.Vertices);
            mesh.SetColors(meshData.Colors);
            mesh.SetTriangles(meshData.Triangles, 0, true);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            chunk.RegisterGeneratedMesh(mesh);

            var dustObject = new GameObject("Wind Deposited Dust");
            dustObject.transform.SetParent(chunk.transform, false);
            dustObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = dustObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = settings.DepositedDustMaterial != null
                ? settings.DepositedDustMaterial
                : fallbackMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
        }

        public static TopDown3DDustMeshData BuildMeshData(
            TopDown3DWorldSettings settings,
            Vector2Int chunkCoordinate,
            TopDown3DDustDepositionPlan plan)
        {
            if (settings == null || plan == null || !plan.HasVisibleDeposits)
            {
                return new TopDown3DDustMeshData(
                    new Vector3[0],
                    new Color32[0],
                    new int[0]);
            }

            var verticesPerAxis = plan.VerticesPerAxis;
            var grid = new DustVertex[verticesPerAxis * verticesPerAxis];
            for (var z = 0; z < verticesPerAxis; z++)
            {
                for (var x = 0; x < verticesPerAxis; x++)
                {
                    var index = z * verticesPerAxis + x;
                    var sample = plan.GetSample(x, z);
                    var worldX = chunkCoordinate.x * settings.ChunkSize + x * plan.Step;
                    var worldZ = chunkCoordinate.y * settings.ChunkSize + z * plan.Step;
                    grid[index] = new DustVertex(
                        new Vector3(
                            x * plan.Step,
                            TopDown3DHeightSampler.SampleHeight(settings, worldX, worldZ)
                                + settings.DustSurfaceOffset
                                + sample.Height,
                            z * plan.Step),
                        sample.Weight);
                }
            }

            var vertices = new List<Vector3>(plan.QuadsPerAxis * plan.QuadsPerAxis * 6);
            var colors = new List<Color32>(vertices.Capacity);
            var triangles = new List<int>(vertices.Capacity);
            var clippedPolygon = new List<DustVertex>(4);
            for (var z = 0; z < plan.QuadsPerAxis; z++)
            {
                for (var x = 0; x < plan.QuadsPerAxis; x++)
                {
                    var bottomLeft = z * verticesPerAxis + x;
                    var topLeft = bottomLeft + verticesPerAxis;
                    AddClippedTriangle(
                        grid[bottomLeft],
                        grid[topLeft],
                        grid[bottomLeft + 1],
                        clippedPolygon,
                        vertices,
                        colors,
                        triangles);
                    AddClippedTriangle(
                        grid[bottomLeft + 1],
                        grid[topLeft],
                        grid[topLeft + 1],
                        clippedPolygon,
                        vertices,
                        colors,
                        triangles);
                }
            }

            return new TopDown3DDustMeshData(
                vertices.ToArray(),
                colors.ToArray(),
                triangles.ToArray());
        }

        private static void AddClippedTriangle(
            DustVertex a,
            DustVertex b,
            DustVertex c,
            List<DustVertex> clippedPolygon,
            List<Vector3> vertices,
            List<Color32> colors,
            List<int> triangles)
        {
            clippedPolygon.Clear();
            var previous = c;
            var previousInside = previous.Weight >= MinimumVisibleWeight;
            for (var corner = 0; corner < 3; corner++)
            {
                var current = corner == 0 ? a : corner == 1 ? b : c;
                var currentInside = current.Weight >= MinimumVisibleWeight;
                if (currentInside != previousInside)
                {
                    clippedPolygon.Add(Intersect(previous, current));
                }

                if (currentInside)
                {
                    clippedPolygon.Add(current);
                }

                previous = current;
                previousInside = currentInside;
            }

            if (clippedPolygon.Count < 3)
            {
                return;
            }

            var anchor = clippedPolygon[0];
            for (var index = 1; index < clippedPolygon.Count - 1; index++)
            {
                AddTriangle(
                    anchor,
                    clippedPolygon[index],
                    clippedPolygon[index + 1],
                    vertices,
                    colors,
                    triangles);
            }
        }

        private static DustVertex Intersect(DustVertex start, DustVertex end)
        {
            var range = end.Weight - start.Weight;
            var time = Mathf.Abs(range) <= 0.000001f
                ? 0f
                : Mathf.Clamp01((MinimumVisibleWeight - start.Weight) / range);
            return new DustVertex(
                Vector3.Lerp(start.Position, end.Position, time),
                MinimumVisibleWeight);
        }

        private static void AddTriangle(
            DustVertex a,
            DustVertex b,
            DustVertex c,
            List<Vector3> vertices,
            List<Color32> colors,
            List<int> triangles)
        {
            var index = vertices.Count;
            AddVertex(a, vertices, colors);
            AddVertex(b, vertices, colors);
            AddVertex(c, vertices, colors);
            triangles.Add(index);
            triangles.Add(index + 1);
            triangles.Add(index + 2);
        }

        private static void AddVertex(
            DustVertex vertex,
            List<Vector3> vertices,
            List<Color32> colors)
        {
            vertices.Add(vertex.Position);
            colors.Add(new Color32(
                255,
                255,
                255,
                (byte)Mathf.RoundToInt(vertex.Weight * 255f)));
        }

        private readonly struct DustVertex
        {
            public DustVertex(Vector3 position, float weight)
            {
                Position = position;
                Weight = weight;
            }

            public Vector3 Position { get; }
            public float Weight { get; }
        }
    }
}
