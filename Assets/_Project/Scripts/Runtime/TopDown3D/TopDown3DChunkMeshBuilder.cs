using Unity.Profiling;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public readonly struct TopDown3DChunkMeshData
    {
        public TopDown3DChunkMeshData(Vector3[] vertices, int[] triangles, Vector2[] uvs, Vector3[] normals)
        {
            Vertices = vertices;
            Triangles = triangles;
            Uvs = uvs;
            Normals = normals;
        }

        public Vector3[] Vertices { get; }
        public int[] Triangles { get; }
        public Vector2[] Uvs { get; }
        public Vector3[] Normals { get; }
    }

    public static class TopDown3DChunkMeshBuilder
    {
        private static readonly ProfilerMarker BuildDataMarker =
            new ProfilerMarker("TopDown3D.World.BuildTerrainData");
        private static readonly ProfilerMarker BuildMeshMarker =
            new ProfilerMarker("TopDown3D.World.ApplyTerrainMesh");

        public static TopDown3DChunkMeshData BuildData(TopDown3DWorldSettings settings, Vector2Int chunkCoordinate)
        {
            using (BuildDataMarker.Auto())
            {
                var quads = settings.QuadsPerAxis;
                var verticesPerAxis = quads + 1;
                var vertices = new Vector3[verticesPerAxis * verticesPerAxis];
                var uvs = new Vector2[vertices.Length];
                var normals = new Vector3[vertices.Length];
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
                        vertices[index] = new Vector3(
                            localX,
                            TopDown3DHeightSampler.SampleHeight(settings, worldX, worldZ),
                            localZ);
                        uvs[index] = new Vector2(worldX / settings.ChunkSize, worldZ / settings.ChunkSize);
                        normals[index] = TopDown3DHeightSampler.SampleNormal(settings, worldX, worldZ, step);
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

                return new TopDown3DChunkMeshData(vertices, triangles, uvs, normals);
            }
        }

        public static Mesh BuildMesh(TopDown3DWorldSettings settings, Vector2Int chunkCoordinate)
        {
            using (BuildMeshMarker.Auto())
            {
                var data = BuildData(settings, chunkCoordinate);
                var mesh = new Mesh
                {
                    name = $"TopDown3D Chunk {chunkCoordinate.x},{chunkCoordinate.y}"
                };
                mesh.SetVertices(data.Vertices);
                mesh.SetTriangles(data.Triangles, 0, true);
                mesh.SetUVs(0, data.Uvs);
                mesh.SetNormals(data.Normals);
                mesh.RecalculateBounds();
                return mesh;
            }
        }
    }
}
