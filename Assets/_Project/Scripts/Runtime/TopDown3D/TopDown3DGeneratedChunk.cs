using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    public sealed class TopDown3DGeneratedChunk : MonoBehaviour
    {
        private Mesh generatedMesh;

        public Vector2Int Coordinate { get; private set; }

        public void Initialize(Vector2Int coordinate, Mesh mesh)
        {
            Coordinate = coordinate;
            generatedMesh = mesh;
        }

        private void OnDestroy()
        {
            if (generatedMesh != null)
            {
                Destroy(generatedMesh);
            }
        }
    }
}
