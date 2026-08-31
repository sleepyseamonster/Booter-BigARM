using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    internal sealed class TopDown3DIndexedMeshData
    {
        public TopDown3DIndexedMeshData(Vector3[] vertices, int[] triangles)
        {
            Vertices = vertices ?? throw new ArgumentNullException(nameof(vertices));
            Triangles = triangles ?? throw new ArgumentNullException(nameof(triangles));
        }

        public Vector3[] Vertices { get; }
        public int[] Triangles { get; }
    }

    internal sealed class TopDown3DRockFormationMeshData
    {
        public TopDown3DRockFormationMeshData(
            Vector3[] vertices,
            Vector3[] normals,
            int[] triangles,
            Bounds bounds)
        {
            Vertices = vertices ?? throw new ArgumentNullException(nameof(vertices));
            Normals = normals ?? throw new ArgumentNullException(nameof(normals));
            Triangles = triangles ?? throw new ArgumentNullException(nameof(triangles));
            Bounds = bounds;
        }

        public Vector3[] Vertices { get; }
        public Vector3[] Normals { get; }
        public int[] Triangles { get; }
        public Bounds Bounds { get; }
    }

    internal readonly struct TopDown3DMeshTopologyReport
    {
        public TopDown3DMeshTopologyReport(
            bool isValid,
            string error,
            int vertexCount,
            int triangleCount,
            double signedVolume)
        {
            IsValid = isValid;
            Error = error;
            VertexCount = vertexCount;
            TriangleCount = triangleCount;
            SignedVolume = signedVolume;
        }

        public bool IsValid { get; }
        public string Error { get; }
        public int VertexCount { get; }
        public int TriangleCount { get; }
        public double SignedVolume { get; }
    }
}
