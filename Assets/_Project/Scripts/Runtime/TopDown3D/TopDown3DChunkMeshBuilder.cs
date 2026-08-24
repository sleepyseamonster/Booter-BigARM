using BooterBigArm.TopDown3D.WorldCreator;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

namespace BooterBigArm.TopDown3D
{
    public readonly struct TopDown3DChunkMeshData
    {
        public TopDown3DChunkMeshData(
            Vector3[] vertices,
            int[] triangles,
            Vector2[] uvs,
            Vector3[] normals,
            Color[] colors)
        {
            Vertices = vertices;
            Triangles = triangles;
            Uvs = uvs;
            Normals = normals;
            Colors = colors;
        }

        public Vector3[] Vertices { get; }
        public int[] Triangles { get; }
        public Vector2[] Uvs { get; }
        public Vector3[] Normals { get; }
        public Color[] Colors { get; }
    }

    public static class TopDown3DChunkMeshBuilder
    {
        private static readonly ProfilerMarker BuildDataMarker =
            new ProfilerMarker("TopDown3D.World.BuildTerrainData");
        private static readonly ProfilerMarker BuildMeshMarker =
            new ProfilerMarker("TopDown3D.World.ApplyTerrainMesh");

        public static TopDown3DChunkMeshData BuildData(
            TopDown3DWorldSettings settings,
            TopDown3DWorldGenerator generator,
            Vector2Int chunkCoordinate)
        {
            using (BuildDataMarker.Auto())
            {
                var quads = settings.QuadsPerAxis;
                var verticesPerAxis = quads + 1;
                var vertices = new Vector3[verticesPerAxis * verticesPerAxis];
                var uvs = new Vector2[vertices.Length];
                var normals = new Vector3[vertices.Length];
                var colors = new Color[vertices.Length];
                var triangles = new int[quads * quads * 6];
                var step = settings.ChunkSize / quads;
                var originX = chunkCoordinate.x * settings.ChunkSize;
                var originZ = chunkCoordinate.y * settings.ChunkSize;

                for (var z = 0; z < verticesPerAxis; z++)
                {
                    for (var x = 0; x < verticesPerAxis; x++)
                    {
                        var index = z * verticesPerAxis + x;
                        var localX = x * step;
                        var localZ = z * step;
                        var worldX = originX + localX;
                        var worldZ = originZ + localZ;
                        var surface = generator.Sample(worldX, worldZ);
                        vertices[index] = new Vector3(localX, surface.Height, localZ);
                        uvs[index] = new Vector2(worldX / settings.ChunkSize, worldZ / settings.ChunkSize);
                        normals[index] = surface.Normal;
                        colors[index] = surface.ToVertexColor();
                    }
                }

                var triangleIndex = 0;
                for (var z = 0; z < quads; z++)
                {
                    for (var x = 0; x < quads; x++)
                    {
                        var bottomLeft = z * verticesPerAxis + x;
                        var topLeft = bottomLeft + verticesPerAxis;
                        triangles[triangleIndex++] = bottomLeft;
                        triangles[triangleIndex++] = topLeft;
                        triangles[triangleIndex++] = bottomLeft + 1;
                        triangles[triangleIndex++] = bottomLeft + 1;
                        triangles[triangleIndex++] = topLeft;
                        triangles[triangleIndex++] = topLeft + 1;
                    }
                }

                return new TopDown3DChunkMeshData(vertices, triangles, uvs, normals, colors);
            }
        }

        public static Mesh BuildMesh(
            TopDown3DWorldSettings settings,
            TopDown3DWorldGenerator generator,
            Vector2Int chunkCoordinate)
        {
            using (BuildMeshMarker.Auto())
            {
                var data = BuildData(settings, generator, chunkCoordinate);
                var mesh = new Mesh
                {
                    name = $"TopDown3D Chunk {chunkCoordinate.x},{chunkCoordinate.y}"
                };
                mesh.SetVertices(data.Vertices);
                mesh.SetTriangles(data.Triangles, 0, true);
                mesh.SetUVs(0, data.Uvs);
                mesh.SetNormals(data.Normals);
                mesh.SetColors(data.Colors);
                mesh.RecalculateBounds();
                return mesh;
            }
        }

        public static Mesh BuildMesh(WorldRepresentationBuildResult representation, string meshName)
        {
            using (BuildMeshMarker.Auto())
            {
                var vertices = new Vector3[representation.VertexCount];
                var normals = new Vector3[representation.VertexCount];
                var colors = new Color[representation.VertexCount];
                var uvs = new Vector2[representation.VertexCount];
                for (var index = 0; index < representation.VertexCount; index++)
                {
                    var absolute = representation.GetAbsolutePosition(index);
                    vertices[index] = new Vector3(
                        checked((float)(absolute.HorizontalA - representation.Key.Minimum.HorizontalA)),
                        checked((float)absolute.Vertical),
                        checked((float)(absolute.HorizontalB - representation.Key.Minimum.HorizontalB)));
                    representation.GetNormal(index, out var normalA, out var normalVertical, out var normalB);
                    normals[index] = new Vector3(normalA, normalVertical, normalB);
                    colors[index] = SemanticColor(representation.GetSemantic(index));
                    uvs[index] = new Vector2(
                        checked((float)(absolute.HorizontalA / representation.Key.TileSpan)),
                        checked((float)(absolute.HorizontalB / representation.Key.TileSpan)));
                }

                var triangles = new int[representation.IndexCount];
                for (var index = 0; index < triangles.Length; index++)
                {
                    triangles[index] = representation.GetIndex(index);
                }

                var mesh = new Mesh { name = meshName };
                mesh.indexFormat = vertices.Length > ushort.MaxValue
                    ? IndexFormat.UInt32
                    : IndexFormat.UInt16;
                mesh.SetVertices(vertices);
                mesh.SetTriangles(triangles, 0, true);
                mesh.SetUVs(0, uvs);
                mesh.SetNormals(normals);
                mesh.SetColors(colors);
                mesh.RecalculateBounds();
                return mesh;
            }
        }

        private static Color SemanticColor(WorldSurfaceSemantic semantic)
        {
            var canyonFloor = (semantic & WorldSurfaceSemantic.CanyonFloor) != 0;
            var canyonShelf = (semantic & WorldSurfaceSemantic.CanyonShelf) != 0;
            var canyonWall = (semantic & WorldSurfaceSemantic.CanyonWall) != 0;
            var disturbed = (semantic & (WorldSurfaceSemantic.Disturbed | WorldSurfaceSemantic.Buried)) != 0;
            var weathered = (semantic & WorldSurfaceSemantic.Weathered) != 0;
            return new Color(
                canyonFloor || disturbed ? 0.46f : 0.04f,
                canyonShelf ? 0.68f : 0.18f,
                canyonWall ? 0.9f : 0.24f,
                weathered ? 0.88f : 0.52f);
        }
    }
}
