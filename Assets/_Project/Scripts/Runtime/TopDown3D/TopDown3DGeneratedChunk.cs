using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    public sealed class TopDown3DGeneratedChunk : MonoBehaviour
    {
        private static long nextGenerationToken;
        private readonly List<Mesh> generatedMeshes = new List<Mesh>();
        private readonly List<Mesh> decorationMeshes = new List<Mesh>();
        private readonly List<Renderer> rendererCountBuffer = new List<Renderer>();
        private readonly List<Collider> colliderCountBuffer = new List<Collider>();
        private Transform decorationRoot;

        public Vector2Int Coordinate { get; private set; }
        public long GenerationToken { get; private set; }
        public Transform DecorationRoot => EnsureDecorationRoot();
        public int DecorationRendererCount { get; private set; }
        public int DecorationColliderCount { get; private set; }
        public int DecorationMeshCount => decorationMeshes.Count;

        public void Initialize(Vector2Int coordinate, Mesh mesh)
        {
            Coordinate = coordinate;
            GenerationToken = Interlocked.Increment(ref nextGenerationToken);
            RegisterGeneratedMesh(mesh);
        }

        public void RegisterGeneratedMesh(Mesh mesh)
        {
            if (mesh != null && !generatedMeshes.Contains(mesh))
            {
                generatedMeshes.Add(mesh);
            }
        }

        public void RegisterDecorationMesh(Mesh mesh)
        {
            if (mesh == null || decorationMeshes.Contains(mesh))
            {
                return;
            }

            decorationMeshes.Add(mesh);
            RegisterGeneratedMesh(mesh);
        }

        public void RefreshDecorationCounts()
        {
            DecorationRendererCount = 0;
            DecorationColliderCount = 0;
            if (decorationRoot == null)
            {
                return;
            }

            decorationRoot.GetComponentsInChildren(true, rendererCountBuffer);
            decorationRoot.GetComponentsInChildren(true, colliderCountBuffer);
            DecorationRendererCount = rendererCountBuffer.Count;
            DecorationColliderCount = colliderCountBuffer.Count;
            rendererCountBuffer.Clear();
            colliderCountBuffer.Clear();
        }

        public void ClearDecoration()
        {
            DecorationRendererCount = 0;
            DecorationColliderCount = 0;
            if (decorationRoot != null)
            {
                DestroyOwnedObject(decorationRoot.gameObject);
                decorationRoot = null;
            }

            for (var i = 0; i < decorationMeshes.Count; i++)
            {
                var mesh = decorationMeshes[i];
                generatedMeshes.Remove(mesh);
                DestroyOwnedObject(mesh);
            }

            decorationMeshes.Clear();
        }

        private Transform EnsureDecorationRoot()
        {
            if (decorationRoot != null)
            {
                return decorationRoot;
            }

            var rootObject = new GameObject("Streamed Decoration");
            decorationRoot = rootObject.transform;
            decorationRoot.SetParent(transform, false);
            return decorationRoot;
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
            decorationMeshes.Clear();
        }

        private static void DestroyOwnedObject(Object ownedObject)
        {
            if (ownedObject == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(ownedObject);
            }
            else
            {
                DestroyImmediate(ownedObject);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetGenerationTokens()
        {
            Interlocked.Exchange(ref nextGenerationToken, 0L);
        }
    }
}
