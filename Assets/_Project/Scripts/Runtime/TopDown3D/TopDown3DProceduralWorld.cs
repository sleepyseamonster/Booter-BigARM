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
        private static readonly ProfilerMarker PlanNaturalObjectsMarker =
            new ProfilerMarker("TopDown3D.World.PlanNaturalObjects");

        [SerializeField] private TopDown3DWorldSettings settings;
        [SerializeField] private Transform streamingTarget;
        [SerializeField] private Material groundMaterial;
        [SerializeField] private Material propMaterial;

        private readonly Dictionary<Vector2Int, TopDown3DGeneratedChunk> loadedChunks =
            new Dictionary<Vector2Int, TopDown3DGeneratedChunk>();
        private readonly HashSet<Vector2Int> requiredChunks = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> requiredDecoratedChunks = new HashSet<Vector2Int>();
        private readonly List<Vector2Int> pendingChunks = new List<Vector2Int>();
        private readonly Queue<Vector2Int> pendingDecorations = new Queue<Vector2Int>();
        private readonly HashSet<Vector2Int> queuedDecorations = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> decoratedChunks = new HashSet<Vector2Int>();
        private readonly List<Vector2Int> unloadBuffer = new List<Vector2Int>();
        private int pendingChunkCursor;
        private Vector2Int currentCenterChunk = new Vector2Int(int.MinValue, int.MinValue);
        private Vector2 spawnExclusionCenter;
        private TopDown3DWorldGenerator worldGenerator;
        private TopDown3DFarLandscape farLandscape;
        private TopDown3DResourceWorldState resourceWorldState;

        public int LoadedChunkCount => loadedChunks.Count;
        public int PendingChunkCount => PendingTerrainChunkCount;
        public int PendingTerrainChunkCount => Mathf.Max(0, pendingChunks.Count - pendingChunkCursor);
        public int PendingDecorationCount => queuedDecorations.Count;
        public int DecoratedChunkCount => decoratedChunks.Count;
        public int TerrainRendererCount => loadedChunks.Count;
        public int TerrainColliderCount => loadedChunks.Count;
        public int DecorationRendererCount => SumDecorationRenderers();
        public int DecorationColliderCount => SumDecorationColliders();
        public int DecorationMeshCount => SumDecorationMeshes();
        public Vector2Int CurrentCenterChunk => currentCenterChunk;
        public int WorldSeed => settings != null ? settings.WorldSeed : 0;

        private int EffectiveStreamingRadius => settings == null
            ? 0
            : Mathf.Clamp(
                TopDown3DPlaytestPerformanceProfile.StreamingRadiusOverride > 0
                    ? TopDown3DPlaytestPerformanceProfile.StreamingRadiusOverride
                    : settings.StreamingRadius,
                1,
                settings.StreamingRadius);

        private int EffectiveDecorationStreamingRadius => settings == null
            ? 0
            : Mathf.Clamp(
                TopDown3DPlaytestPerformanceProfile.DecorationStreamingRadiusOverride >= 0
                    ? TopDown3DPlaytestPerformanceProfile.DecorationStreamingRadiusOverride
                    : settings.DecorationStreamingRadius,
                0,
                EffectiveStreamingRadius);

        private int EffectiveChunksBuiltPerFrame => settings == null
            ? 0
            : Mathf.Max(
                1,
                TopDown3DPlaytestPerformanceProfile.ChunksBuiltPerFrameOverride > 0
                    ? TopDown3DPlaytestPerformanceProfile.ChunksBuiltPerFrameOverride
                    : settings.ChunksBuiltPerFrame);

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
            worldGenerator = new TopDown3DWorldGenerator(settings);
            if (settings != null && settings.ResourceCatalog != null)
            {
                resourceWorldState = GetComponent<TopDown3DResourceWorldState>();
                if (resourceWorldState == null)
                {
                    resourceWorldState = gameObject.AddComponent<TopDown3DResourceWorldState>();
                }

                resourceWorldState.Configure(settings.ResourceGenerationVersion);
            }
            farLandscape = new TopDown3DFarLandscape(
                transform,
                settings,
                worldGenerator,
                groundMaterial);
            EnsureSafeStreamingTarget();
            farLandscape.Refresh(streamingTarget != null ? streamingTarget.position : Vector3.zero, true);
            RefreshChunks(true);
        }

        private void Update()
        {
            RefreshChunks(false);
            if (streamingTarget != null)
            {
                farLandscape?.Refresh(streamingTarget.position, false);
            }
            ProcessPendingChunks(EffectiveChunksBuiltPerFrame);
        }

        private void RefreshChunks(bool force)
        {
            using (RefreshChunksMarker.Auto())
            {
                if (settings == null || streamingTarget == null)
                {
                    return;
                }

                worldGenerator ??= new TopDown3DWorldGenerator(settings);
                var center = worldGenerator.WorldToChunk(settings, streamingTarget.position);
                if (!force && center == currentCenterChunk)
                {
                    return;
                }

                currentCenterChunk = center;
                requiredChunks.Clear();
                requiredDecoratedChunks.Clear();
                pendingChunks.Clear();
                pendingDecorations.Clear();
                queuedDecorations.Clear();
                pendingChunkCursor = 0;
                var streamingRadius = EffectiveStreamingRadius;
                var decorationStreamingRadius = EffectiveDecorationStreamingRadius;
                for (var z = -streamingRadius; z <= streamingRadius; z++)
                {
                    for (var x = -streamingRadius; x <= streamingRadius; x++)
                    {
                        var coordinate = new Vector2Int(center.x + x, center.y + z);
                        requiredChunks.Add(coordinate);
                        if (ChebyshevDistance(coordinate, center) <= decorationStreamingRadius)
                        {
                            requiredDecoratedChunks.Add(coordinate);
                        }

                        if (!loadedChunks.ContainsKey(coordinate))
                        {
                            pendingChunks.Add(coordinate);
                        }
                        else if (requiredDecoratedChunks.Contains(coordinate)
                            && !decoratedChunks.Contains(coordinate))
                        {
                            EnqueueDecoration(coordinate);
                        }
                    }
                }

                pendingChunks.Sort(ComparePendingChunks);
                if (force)
                {
                    var immediateRadius = Mathf.Min(settings.ImmediateLoadRadius, streamingRadius);
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

                    if (EffectiveDecorationStreamingRadius > 0)
                    {
                        DecorateChunkImmediately(center);
                    }
                }

                unloadBuffer.Clear();
                var unloadRadius = streamingRadius + settings.UnloadPadding;
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

                unloadBuffer.Clear();
                foreach (var coordinate in decoratedChunks)
                {
                    if (!requiredDecoratedChunks.Contains(coordinate))
                    {
                        unloadBuffer.Add(coordinate);
                    }
                }

                for (var i = 0; i < unloadBuffer.Count; i++)
                {
                    var coordinate = unloadBuffer[i];
                    if (loadedChunks.TryGetValue(coordinate, out var chunk) && chunk != null)
                    {
                        chunk.ClearDecoration();
                    }

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
            worldGenerator ??= new TopDown3DWorldGenerator(settings);
            if (!worldGenerator.TryFindWalkablePosition(
                    desired,
                    settings.SafeSpawnSearchRadius,
                    settings.SafeSpawnSearchStep,
                    settings.MaximumSafeSpawnSlope,
                    out var groundPosition))
            {
                groundPosition = new Vector3(
                    desired.x,
                    worldGenerator.SampleHeight(desired.x, desired.y),
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

                worldGenerator ??= new TopDown3DWorldGenerator(settings);
                var mesh = TopDown3DChunkMeshBuilder.BuildMesh(settings, worldGenerator, coordinate);
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
                if (requiredDecoratedChunks.Contains(coordinate))
                {
                    EnqueueDecoration(coordinate);
                }
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
                    || !requiredDecoratedChunks.Contains(coordinate)
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
                TopDown3DNaturalObjectChunkPlan plan;
                using (PlanNaturalObjectsMarker.Auto())
                {
                    plan = TopDown3DNaturalObjectPlanner.BuildChunkPlan(
                        settings,
                        worldGenerator,
                        settings.NaturalObjectCatalog,
                        chunk.Coordinate,
                        spawnExclusionCenter);
                }

                TopDown3DNaturalObjectDecorator.Decorate(
                    chunk,
                    settings,
                    propMaterial,
                    plan);
                TopDown3DResourceNodeDecorator.Decorate(
                    chunk,
                    settings,
                    plan,
                    resourceWorldState);
                TopDown3DDustDepositionDecorator.Decorate(
                    chunk,
                    settings,
                    worldGenerator,
                    groundMaterial,
                    spawnExclusionCenter);
                chunk.RefreshDecorationCounts();
            }
        }

        private int SumDecorationRenderers()
        {
            var count = 0;
            foreach (var chunk in loadedChunks.Values)
            {
                if (chunk != null)
                {
                    count += chunk.DecorationRendererCount;
                }
            }

            return count;
        }

        private int SumDecorationColliders()
        {
            var count = 0;
            foreach (var chunk in loadedChunks.Values)
            {
                if (chunk != null)
                {
                    count += chunk.DecorationColliderCount;
                }
            }

            return count;
        }

        private int SumDecorationMeshes()
        {
            var count = 0;
            foreach (var chunk in loadedChunks.Values)
            {
                if (chunk != null)
                {
                    count += chunk.DecorationMeshCount;
                }
            }

            return count;
        }

        private void OnDestroy()
        {
            farLandscape?.Dispose();
            farLandscape = null;
        }
    }
}
