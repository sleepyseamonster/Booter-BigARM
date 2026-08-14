using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    public sealed class TopDown3DProceduralWorld : MonoBehaviour
    {
        private const double PendingChunkBudgetMilliseconds = 2.0;

        private static readonly ProfilerMarker RefreshChunksMarker =
            new ProfilerMarker("TopDown3D.World.RefreshChunks");
        private static readonly ProfilerMarker ProcessPendingChunksMarker =
            new ProfilerMarker("TopDown3D.World.ProcessPendingChunks");
        private static readonly ProfilerMarker BuildChunkMarker =
            new ProfilerMarker("TopDown3D.World.BuildChunk");
        private static readonly ProfilerMarker DecorateChunkMarker =
            new ProfilerMarker("TopDown3D.World.DecorateChunk");

        [SerializeField] private TopDown3DWorldSettings settings;
        [SerializeField] private Transform streamingTarget;
        [SerializeField] private Material groundMaterial;
        [SerializeField] private Material propMaterial;

        private readonly Dictionary<Vector2Int, TopDown3DGeneratedChunk> loadedChunks =
            new Dictionary<Vector2Int, TopDown3DGeneratedChunk>();
        private readonly HashSet<Vector2Int> requiredChunks = new HashSet<Vector2Int>();
        private readonly List<Vector2Int> pendingChunks = new List<Vector2Int>();
        private readonly Queue<Vector2Int> pendingDecorations = new Queue<Vector2Int>();
        private readonly HashSet<Vector2Int> queuedDecorations = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> decoratedChunks = new HashSet<Vector2Int>();
        private readonly List<Vector2Int> unloadBuffer = new List<Vector2Int>();
        private int pendingChunkCursor;
        private Vector2Int currentCenterChunk = new Vector2Int(int.MinValue, int.MinValue);
        private Vector2 spawnExclusionCenter;

        public int LoadedChunkCount => loadedChunks.Count;
        public int PendingChunkCount => PendingTerrainChunkCount;
        public int PendingTerrainChunkCount => Mathf.Max(0, pendingChunks.Count - pendingChunkCursor);
        public int PendingDecorationCount => queuedDecorations.Count;
        public int DecoratedChunkCount => decoratedChunks.Count;
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
            EnsureSafeStreamingTarget();
            RefreshChunks(true);
        }

        private void Update()
        {
            RefreshChunks(false);
            ProcessPendingChunks(settings != null ? settings.ChunksBuiltPerFrame : 0);
        }

        private void RefreshChunks(bool force)
        {
            using (RefreshChunksMarker.Auto())
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
                requiredChunks.Clear();
                pendingChunks.Clear();
                pendingDecorations.Clear();
                queuedDecorations.Clear();
                pendingChunkCursor = 0;
                for (var z = -settings.StreamingRadius; z <= settings.StreamingRadius; z++)
                {
                    for (var x = -settings.StreamingRadius; x <= settings.StreamingRadius; x++)
                    {
                        var coordinate = new Vector2Int(center.x + x, center.y + z);
                        requiredChunks.Add(coordinate);
                        if (!loadedChunks.ContainsKey(coordinate))
                        {
                            pendingChunks.Add(coordinate);
                        }
                        else if (!decoratedChunks.Contains(coordinate))
                        {
                            EnqueueDecoration(coordinate);
                        }
                    }
                }

                pendingChunks.Sort(ComparePendingChunks);
                if (force)
                {
                    var immediateRadius = Mathf.Min(settings.ImmediateLoadRadius, settings.StreamingRadius);
                    for (var i = pendingChunks.Count - 1; i >= 0; i--)
                    {
                        var coordinate = pendingChunks[i];
                        if (ChebyshevDistance(coordinate, center) > immediateRadius)
                        {
                            continue;
                        }

                        EnsureChunkTerrain(coordinate);
                        pendingChunks.RemoveAt(i);
                    }

                    DecorateChunkImmediately(center);
                }

                unloadBuffer.Clear();
                var unloadRadius = settings.StreamingRadius + settings.UnloadPadding;
                foreach (var pair in loadedChunks)
                {
                    if (ChebyshevDistance(pair.Key, center) > unloadRadius)
                    {
                        unloadBuffer.Add(pair.Key);
                    }
                }

                for (var i = 0; i < unloadBuffer.Count; i++)
                {
                    var coordinate = unloadBuffer[i];
                    if (loadedChunks.TryGetValue(coordinate, out var chunk) && chunk != null)
                    {
                        Destroy(chunk.gameObject);
                    }

                    loadedChunks.Remove(coordinate);
                    decoratedChunks.Remove(coordinate);
                    queuedDecorations.Remove(coordinate);
                }
            }
        }

        private void ProcessPendingChunks(int budget)
        {
            using (ProcessPendingChunksMarker.Auto())
            {
                var count = Mathf.Max(0, budget);
                var startedAt = Time.realtimeSinceStartupAsDouble;
                for (var i = 0; i < count; i++)
                {
                    if (!TryProcessDecoration())
                    {
                        if (pendingChunkCursor >= pendingChunks.Count)
                        {
                            break;
                        }

                        var coordinate = pendingChunks[pendingChunkCursor++];
                        if (requiredChunks.Contains(coordinate))
                        {
                            EnsureChunkTerrain(coordinate);
                        }
                    }

                    var elapsedMilliseconds =
                        (Time.realtimeSinceStartupAsDouble - startedAt) * 1000.0;
                    if (elapsedMilliseconds >= PendingChunkBudgetMilliseconds)
                    {
                        break;
                    }
                }

                if (pendingChunkCursor >= pendingChunks.Count)
                {
                    pendingChunks.Clear();
                    pendingChunkCursor = 0;
                }
            }
        }

        private int ComparePendingChunks(Vector2Int left, Vector2Int right)
        {
            var distanceComparison = ChebyshevDistance(left, currentCenterChunk)
                .CompareTo(ChebyshevDistance(right, currentCenterChunk));
            if (distanceComparison != 0)
            {
                return distanceComparison;
            }

            var zComparison = left.y.CompareTo(right.y);
            return zComparison != 0 ? zComparison : left.x.CompareTo(right.x);
        }

        private static int ChebyshevDistance(Vector2Int left, Vector2Int right)
        {
            return Mathf.Max(Mathf.Abs(left.x - right.x), Mathf.Abs(left.y - right.y));
        }

        private void EnsureSafeStreamingTarget()
        {
            if (settings == null || streamingTarget == null)
            {
                return;
            }

            var desired = new Vector2(streamingTarget.position.x, streamingTarget.position.z);
            if (!TopDown3DHeightSampler.TryFindWalkablePosition(
                    settings,
                    desired,
                    settings.SafeSpawnSearchRadius,
                    settings.SafeSpawnSearchStep,
                    settings.MaximumSafeSpawnSlope,
                    out var groundPosition))
            {
                groundPosition = new Vector3(
                    desired.x,
                    TopDown3DHeightSampler.SampleHeight(settings, desired.x, desired.y),
                    desired.y);
                Debug.LogWarning("No walkable safe-spawn candidate was found; using the requested terrain position.", this);
            }

            var collider = streamingTarget.GetComponent<Collider>();
            var verticalClearance = collider != null ? collider.bounds.extents.y + 0.06f : 1.05f;
            var spawnPosition = groundPosition + Vector3.up * verticalClearance;
            var motor = streamingTarget.GetComponent<TopDown3DPlayerMotor>();
            if (motor != null)
            {
                motor.Teleport(spawnPosition);
            }
            else
            {
                streamingTarget.position = spawnPosition;
            }

            spawnExclusionCenter = new Vector2(spawnPosition.x, spawnPosition.z);
        }

        private void EnsureChunkTerrain(Vector2Int coordinate)
        {
            using (BuildChunkMarker.Auto())
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
                EnqueueDecoration(coordinate);
            }
        }

        private void EnqueueDecoration(Vector2Int coordinate)
        {
            if (decoratedChunks.Contains(coordinate) || !queuedDecorations.Add(coordinate))
            {
                return;
            }

            pendingDecorations.Enqueue(coordinate);
        }

        private bool TryProcessDecoration()
        {
            while (pendingDecorations.Count > 0)
            {
                var coordinate = pendingDecorations.Dequeue();
                if (!queuedDecorations.Remove(coordinate)
                    || decoratedChunks.Contains(coordinate)
                    || !requiredChunks.Contains(coordinate)
                    || !loadedChunks.TryGetValue(coordinate, out var chunk)
                    || chunk == null)
                {
                    continue;
                }

                DecorateChunk(chunk);
                decoratedChunks.Add(coordinate);
                return true;
            }

            return false;
        }

        private void DecorateChunkImmediately(Vector2Int coordinate)
        {
            queuedDecorations.Remove(coordinate);
            if (decoratedChunks.Contains(coordinate)
                || !loadedChunks.TryGetValue(coordinate, out var chunk)
                || chunk == null)
            {
                return;
            }

            DecorateChunk(chunk);
            decoratedChunks.Add(coordinate);
        }

        private void DecorateChunk(TopDown3DGeneratedChunk chunk)
        {
            using (DecorateChunkMarker.Auto())
            {
                TopDown3DEscarpmentSurfaceDecorator.Decorate(chunk, settings, propMaterial);
                TopDown3DNaturalObjectDecorator.Decorate(chunk, settings, propMaterial, spawnExclusionCenter);
                TopDown3DDustDepositionDecorator.Decorate(
                    chunk,
                    settings,
                    groundMaterial,
                    spawnExclusionCenter);
            }
        }
    }
}
