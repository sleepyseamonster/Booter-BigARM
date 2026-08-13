using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    public sealed class TopDown3DProceduralWorld : MonoBehaviour
    {
        [SerializeField] private TopDown3DWorldSettings settings;
        [SerializeField] private Transform streamingTarget;
        [SerializeField] private Material groundMaterial;
        [SerializeField] private Material propMaterial;

        private readonly Dictionary<Vector2Int, TopDown3DGeneratedChunk> loadedChunks =
            new Dictionary<Vector2Int, TopDown3DGeneratedChunk>();
        private Vector2Int currentCenterChunk = new Vector2Int(int.MinValue, int.MinValue);

        public int LoadedChunkCount => loadedChunks.Count;
        public Vector2Int CurrentCenterChunk => currentCenterChunk;
        public int WorldSeed => settings != null ? settings.WorldSeed : 0;

        public void Configure(
            TopDown3DWorldSettings worldSettings,
            Transform target,
            Material terrainMaterial,
            Material obstacleMaterial)
        {
            settings = worldSettings;
            streamingTarget = target;
            groundMaterial = terrainMaterial;
            propMaterial = obstacleMaterial;
        }

        private void Start()
        {
            RefreshChunks(true);
        }

        private void Update()
        {
            RefreshChunks(false);
        }

        private void RefreshChunks(bool force)
        {
            if (settings == null || streamingTarget == null)
            {
                return;
            }

            var center = TopDown3DHeightSampler.WorldToChunk(settings, streamingTarget.position);
            if (!force && center == currentCenterChunk)
            {
                return;
            }

            currentCenterChunk = center;
            var required = new HashSet<Vector2Int>();
            for (var z = -settings.StreamingRadius; z <= settings.StreamingRadius; z++)
            {
                for (var x = -settings.StreamingRadius; x <= settings.StreamingRadius; x++)
                {
                    var coordinate = new Vector2Int(center.x + x, center.y + z);
                    required.Add(coordinate);
                    EnsureChunk(coordinate);
                }
            }

            var unload = new List<Vector2Int>();
            foreach (var pair in loadedChunks)
            {
                if (!required.Contains(pair.Key))
                {
                    unload.Add(pair.Key);
                }
            }

            for (var i = 0; i < unload.Count; i++)
            {
                var coordinate = unload[i];
                if (loadedChunks.TryGetValue(coordinate, out var chunk) && chunk != null)
                {
                    Destroy(chunk.gameObject);
                }

                loadedChunks.Remove(coordinate);
            }
        }

        private void EnsureChunk(Vector2Int coordinate)
        {
            if (loadedChunks.ContainsKey(coordinate))
            {
                return;
            }

            var chunkObject = new GameObject($"Chunk {coordinate.x},{coordinate.y}");
            chunkObject.transform.SetParent(transform, false);
            chunkObject.transform.localPosition = new Vector3(
                coordinate.x * settings.ChunkSize,
                0f,
                coordinate.y * settings.ChunkSize);

            var mesh = TopDown3DChunkMeshBuilder.BuildMesh(settings, coordinate);
            var filter = chunkObject.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = chunkObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = groundMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            var collider = chunkObject.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            chunkObject.AddComponent<TopDown3DGroundSurface>();
            var chunk = chunkObject.AddComponent<TopDown3DGeneratedChunk>();
            chunk.Initialize(coordinate, mesh);
            loadedChunks.Add(coordinate, chunk);
            CreateProps(chunkObject.transform, coordinate);
        }

        private void CreateProps(Transform chunkRoot, Vector2Int coordinate)
        {
            if (settings.PropsPerChunk <= 0)
            {
                return;
            }

            var random = new System.Random(TopDown3DHeightSampler.StableChunkSeed(settings.WorldSeed, coordinate));
            for (var i = 0; i < settings.PropsPerChunk; i++)
            {
                var localX = 1.25f + (float)random.NextDouble() * (settings.ChunkSize - 2.5f);
                var localZ = 1.25f + (float)random.NextDouble() * (settings.ChunkSize - 2.5f);
                var worldX = coordinate.x * settings.ChunkSize + localX;
                var worldZ = coordinate.y * settings.ChunkSize + localZ;
                if (new Vector2(worldX, worldZ).magnitude < settings.ClearSpawnRadius)
                {
                    continue;
                }

                var width = Mathf.Lerp(0.7f, 1.7f, (float)random.NextDouble());
                var height = Mathf.Lerp(0.7f, 2.1f, (float)random.NextDouble());
                var depth = Mathf.Lerp(0.7f, 1.7f, (float)random.NextDouble());
                var prop = GameObject.CreatePrimitive(PrimitiveType.Cube);
                prop.name = $"Generated Rock {i + 1}";
                prop.transform.SetParent(chunkRoot, false);
                prop.transform.localScale = new Vector3(width, height, depth);
                prop.transform.localRotation = Quaternion.Euler(0f, (float)random.NextDouble() * 180f, 0f);
                prop.transform.localPosition = new Vector3(
                    localX,
                    TopDown3DHeightSampler.SampleHeight(settings, worldX, worldZ) + height * 0.5f,
                    localZ);
                var renderer = prop.GetComponent<Renderer>();
                renderer.sharedMaterial = propMaterial;
            }
        }
    }
}
