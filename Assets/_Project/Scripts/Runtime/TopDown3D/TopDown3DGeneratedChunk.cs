using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    public sealed class TopDown3DGeneratedChunk : MonoBehaviour
    {
        private readonly List<Mesh> generatedMeshes = new List<Mesh>();

        public Vector2Int Coordinate { get; private set; }

        public void Initialize(Vector2Int coordinate, Mesh mesh)
        {
            Coordinate = coordinate;
            RegisterGeneratedMesh(mesh);
        }

        public void RegisterGeneratedMesh(Mesh mesh)
        {
            if (mesh != null && !generatedMeshes.Contains(mesh))
            {
                generatedMeshes.Add(mesh);
            }
        }

        private void OnDestroy()
        {
            for (var i = 0; i < generatedMeshes.Count; i++)
            {
                if (generatedMeshes[i] != null)
                {
                    Destroy(generatedMeshes[i]);
                }
            }

            generatedMeshes.Clear();
        }
    }
}
