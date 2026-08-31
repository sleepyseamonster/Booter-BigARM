using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BooterBigArm.TopDown3D.WorldCreator;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    public sealed class TopDown3DProceduralWorld : MonoBehaviour
    {
        private const double PendingChunkBudgetMilliseconds = 2.0;
        // Terrain outside this ring is visual-only.  Keeping collision local prevents
        // PhysX from cooking every streamed terrain mesh when the player crosses a chunk.
        private const int TerrainCollisionStreamingRadius = 1;

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
        private readonly Queue<Vector2Int> pendingTerrainColliders = new Queue<Vector2Int>();
        private readonly HashSet<Vector2Int> queuedTerrainColliders = new HashSet<Vector2Int>();
        private readonly List<Vector2Int> unloadBuffer = new List<Vector2Int>();
        private readonly Dictionary<Vector2Int, TerrainRequest> terrainRequests =
            new Dictionary<Vector2Int, TerrainRequest>();
        private int pendingChunkCursor;
        private Vector2Int currentCenterChunk = new Vector2Int(int.MinValue, int.MinValue);
        private Vector2 spawnExclusionCenter;
        private WorldCreatorProductionRuntime worldCreatorRuntime;
        private TopDown3DWorldGenerator worldGenerator;
        private TopDown3DFarLandscape farLandscape;
        private TopDown3DResourceWorldState resourceWorldState;

        internal WorldCreatorProductionRuntime ProductionRuntime => worldCreatorRuntime;
        internal TopDown3DResourceWorldState ResourceWorldState => resourceWorldState;

        internal bool TryLocalToAbsolute(Vector3 localPosition, out AbsoluteWorldPosition absolute)
        {
            if (worldCreatorRuntime == null)
            {
                absolute = default;
                return false;
            }
            absolute = worldCreatorRuntime.ToAbsolute(
                localPosition.x,
                localPosition.y,
                localPosition.z);
            return true;
        }

        internal bool TryAbsoluteToLocal(AbsoluteWorldPosition absolute, out Vector3 localPosition)
        {
            localPosition = default;
            if (worldCreatorRuntime == null
                || !worldCreatorRuntime.TryToLocal(absolute, out var local))
                return false;
            localPosition = new Vector3(local.X, local.Y, local.Z);
            return true;
        }

        internal bool TryPrepareLoadFrame(AbsoluteWorldPosition absolute)
        {
            if (worldCreatorRuntime == null) return false;
            if (worldCreatorRuntime.TryToLocal(absolute, out _)) return true;

            var currentOrigin = worldCreatorRuntime.CurrentFrame.OriginPosition;
            var nextOrigin = new AbsoluteWorldPosition(
                absolute.HorizontalA,
                currentOrigin.Vertical,
                absolute.HorizontalB);
            var shift = new Vector3(
                checked((float)(nextOrigin.HorizontalA - currentOrigin.HorizontalA)),
                checked((float)(nextOrigin.Vertical - currentOrigin.Vertical)),
                checked((float)(nextOrigin.HorizontalB - currentOrigin.HorizontalB)));
            if (!worldCreatorRuntime.TryRebase(nextOrigin)) return false;

            var worldRoot = transform.root.gameObject;
            var roots = gameObject.scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                if (roots[i] != worldRoot) roots[i].transform.position -= shift;
            }
            RepositionLoadedChunks();
            farLandscape?.RepositionForCurrentFrame();
            return true;
        }

        private TopDown3DPlayerMotor suspendedMotor;
        private Rigidbody suspendedBody;
        private bool suspendedMotorWasEnabled;
        private bool suspendedBodyWasKinematic;
        private bool waitingForInitialTerrain;

        public int LoadedChunkCount => loadedChunks.Count;
        public int PendingChunkCount => PendingTerrainChunkCount;
        public int PendingTerrainChunkCount => Mathf.Max(0, pendingChunks.Count - pendingChunkCursor)
            + terrainRequests.Count;
        public int PendingTerrainColliderCount => queuedTerrainColliders.Count;
        public int PendingDecorationCount => queuedDecorations.Count;
        public int DecoratedChunkCount => decoratedChunks.Count;
        public int TerrainRendererCount => loadedChunks.Count;
        public int TerrainColliderCount => CountEnabledTerrainColliders();
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
            worldCreatorRuntime = WorldCreatorProductionRuntime.Create(settings != null ? settings.WorldSeed : 0);
            worldGenerator = new TopDown3DWorldGenerator(settings, worldCreatorRuntime);
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
                worldCreatorRuntime,
                groundMaterial);
            EnsureSafeStreamingTarget();
            SuspendStreamingTargetUntilInitialTerrain();
            farLandscape.Refresh(streamingTarget != null ? streamingTarget.position : Vector3.zero, true);
            RefreshChunks(true);
        }

        private void Update()
        {
            TryRebaseLocalOrigin();
            worldCreatorRuntime?.DrainIntegrationQueue(4, TimeSpan.FromMilliseconds(1d));
            RefreshChunks(false);
            if (streamingTarget != null)
            {
                farLandscape?.Refresh(streamingTarget.position, false);
            }
            ProcessPendingChunks(EffectiveChunksBuiltPerFrame);
            farLandscape?.ProcessReady(2, 2);
        }

        private void RefreshChunks(bool force)
        {
            using (RefreshChunksMarker.Auto())
            {
                if (settings == null || streamingTarget == null)
                {
                    return;
                }

                worldGenerator ??= new TopDown3DWorldGenerator(settings, worldCreatorRuntime);
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

                        RequestChunkTerrain(coordinate);
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
                    terrainRequests.Remove(coordinate);
                    decoratedChunks.Remove(coordinate);
                    queuedDecorations.Remove(coordinate);
                    queuedTerrainColliders.Remove(coordinate);
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

                RefreshTerrainCollisionStreaming();
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
                    if (!TryCreatePendingTerrainCollider()
                        && !TryIntegrateRequestedTerrain()
                        && !TryProcessDecoration())
                    {
                        if (pendingChunkCursor >= pendingChunks.Count)
                        {
                            break;
                        }

                        var coordinate = pendingChunks[pendingChunkCursor++];
                        if (requiredChunks.Contains(coordinate))
                        {
                            RequestChunkTerrain(coordinate);
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
            worldGenerator ??= new TopDown3DWorldGenerator(settings, worldCreatorRuntime);
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

        private void RequestChunkTerrain(Vector2Int coordinate)
        {
            if (loadedChunks.ContainsKey(coordinate)
                || terrainRequests.ContainsKey(coordinate)
                || worldCreatorRuntime == null
                || settings == null)
            {
                return;
            }

            var key = worldCreatorRuntime.CreateRepresentationKey(
                WorldRepresentationTier.Near,
                coordinate.x,
                coordinate.y,
                settings.ChunkSize);
            terrainRequests.Add(
                coordinate,
                new TerrainRequest(key, worldCreatorRuntime.RequestAsync(key)));
        }

        private bool TryIntegrateRequestedTerrain()
        {
            var found = false;
            var coordinate = default(Vector2Int);
            var request = default(TerrainRequest);
            foreach (var pair in terrainRequests)
            {
                if (pair.Value.Task.IsCompleted
                    && (!found || ComparePendingChunks(pair.Key, coordinate) < 0))
                {
                    found = true;
                    coordinate = pair.Key;
                    request = pair.Value;
                    break;
                }
            }

            if (!found)
            {
                return false;
            }

            var outcome = request.Task.GetAwaiter().GetResult();
            if (outcome.State == WorldRepresentationRequestState.Failed)
            {
                Debug.LogError($"World Creator near representation failed: {outcome.Error}", this);
                terrainRequests.Remove(coordinate);
                return true;
            }

            if (!requiredChunks.Contains(coordinate))
            {
                terrainRequests.Remove(coordinate);
                return true;
            }

            if (!worldCreatorRuntime.TryGetIntegrated(request.Key, out var result))
            {
                return false;
            }

            terrainRequests.Remove(coordinate);
            IntegrateChunkTerrain(coordinate, result);
            return true;
        }

        private void IntegrateChunkTerrain(
            Vector2Int coordinate,
            WorldRepresentationBuildResult representation)
        {
            using (BuildChunkMarker.Auto())
            {
                if (loadedChunks.ContainsKey(coordinate)
                    || !worldCreatorRuntime.TryToLocal(representation.Key.Minimum, out var localOrigin))
                {
                    return;
                }

                var chunkObject = new GameObject($"Chunk {coordinate.x},{coordinate.y}");
                chunkObject.transform.SetParent(transform, false);
                chunkObject.transform.localPosition = new Vector3(
                    localOrigin.X,
                    localOrigin.Y,
                    localOrigin.Z);

                var mesh = TopDown3DChunkMeshBuilder.BuildMesh(
                    representation,
                    $"World Creator Near {coordinate.x},{coordinate.y}");
                var filter = chunkObject.AddComponent<MeshFilter>();
                filter.sharedMesh = mesh;
                var renderer = chunkObject.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = groundMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
                chunkObject.AddComponent<TopDown3DGroundSurface>();
                var chunk = chunkObject.AddComponent<TopDown3DGeneratedChunk>();
                chunk.Initialize(coordinate, mesh);
                loadedChunks.Add(coordinate, chunk);
                QueueTerrainCollider(coordinate);
                if (requiredDecoratedChunks.Contains(coordinate))
                {
                    EnqueueDecoration(coordinate);
                }

            }
        }

        private void RefreshTerrainCollisionStreaming()
        {
            foreach (var pair in loadedChunks)
            {
                if (pair.Value == null)
                {
                    continue;
                }

                var collider = pair.Value.GetComponent<MeshCollider>();
                if (ShouldHaveTerrainCollider(pair.Key))
                {
                    if (collider == null)
                    {
                        QueueTerrainCollider(pair.Key);
                    }
                    else
                    {
                        collider.enabled = true;
                    }
                }
                else if (collider != null)
                {
                    collider.enabled = false;
                }
            }
        }

        private bool ShouldHaveTerrainCollider(Vector2Int coordinate)
        {
            return ChebyshevDistance(coordinate, currentCenterChunk) <= TerrainCollisionStreamingRadius;
        }

        private void QueueTerrainCollider(Vector2Int coordinate)
        {
            if (ShouldHaveTerrainCollider(coordinate)
                && queuedTerrainColliders.Add(coordinate))
            {
                pendingTerrainColliders.Enqueue(coordinate);
            }
        }

        private bool TryCreatePendingTerrainCollider()
        {
            while (pendingTerrainColliders.Count > 0)
            {
                var coordinate = pendingTerrainColliders.Dequeue();
                if (!queuedTerrainColliders.Remove(coordinate)
                    || !ShouldHaveTerrainCollider(coordinate)
                    || !loadedChunks.TryGetValue(coordinate, out var chunk)
                    || chunk == null)
                {
                    continue;
                }

                var collider = chunk.GetComponent<MeshCollider>();
                if (collider == null)
                {
                    var filter = chunk.GetComponent<MeshFilter>();
                    if (filter == null || filter.sharedMesh == null)
                    {
                        continue;
                    }

                    collider = chunk.gameObject.AddComponent<MeshCollider>();
                    collider.sharedMesh = filter.sharedMesh;
                }
                else
                {
                    collider.enabled = true;
                }

                if (waitingForInitialTerrain && coordinate == currentCenterChunk)
                {
                    ResumeStreamingTargetAfterInitialTerrain();
                }

                return true;
            }

            return false;
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

        private int CountEnabledTerrainColliders()
        {
            var count = 0;
            foreach (var chunk in loadedChunks.Values)
            {
                var collider = chunk != null ? chunk.GetComponent<MeshCollider>() : null;
                if (collider != null && collider.enabled)
                {
                    count++;
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

        private void SuspendStreamingTargetUntilInitialTerrain()
        {
            if (streamingTarget == null)
            {
                return;
            }

            suspendedMotor = streamingTarget.GetComponent<TopDown3DPlayerMotor>();
            suspendedBody = streamingTarget.GetComponent<Rigidbody>();
            if (suspendedMotor != null)
            {
                suspendedMotorWasEnabled = suspendedMotor.enabled;
                suspendedMotor.enabled = false;
            }

            if (suspendedBody != null)
            {
                suspendedBodyWasKinematic = suspendedBody.isKinematic;
                suspendedBody.isKinematic = true;
            }

            waitingForInitialTerrain = true;
        }

        private void ResumeStreamingTargetAfterInitialTerrain()
        {
            if (!waitingForInitialTerrain)
            {
                return;
            }

            waitingForInitialTerrain = false;
            if (suspendedBody != null)
            {
                suspendedBody.isKinematic = suspendedBodyWasKinematic;
            }

            if (suspendedMotor != null)
            {
                suspendedMotor.enabled = suspendedMotorWasEnabled;
            }
        }

        private void TryRebaseLocalOrigin()
        {
            if (worldCreatorRuntime == null || streamingTarget == null || settings == null)
            {
                return;
            }

            var position = streamingTarget.position;
            var threshold = worldCreatorRuntime.Profile.RebaseThreshold;
            if (Mathf.Max(Mathf.Abs(position.x), Mathf.Abs(position.z)) < threshold)
            {
                return;
            }

            var chunkSize = Mathf.Max(1f, settings.ChunkSize);
            var shift = new Vector3(
                Mathf.Round(position.x / chunkSize) * chunkSize,
                0f,
                Mathf.Round(position.z / chunkSize) * chunkSize);
            var currentOrigin = worldCreatorRuntime.CurrentFrame.OriginPosition;
            var nextOrigin = new AbsoluteWorldPosition(
                currentOrigin.HorizontalA + shift.x,
                currentOrigin.Vertical,
                currentOrigin.HorizontalB + shift.z);
            if (!worldCreatorRuntime.TryRebase(nextOrigin))
            {
                return;
            }

            var worldRoot = transform.root.gameObject;
            var roots = gameObject.scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                if (roots[i] != worldRoot)
                {
                    roots[i].transform.position -= shift;
                }
            }

            RepositionLoadedChunks();
            farLandscape?.RepositionForCurrentFrame();
        }

        private void RepositionLoadedChunks()
        {
            foreach (var pair in loadedChunks)
            {
                var key = worldCreatorRuntime.CreateRepresentationKey(
                    WorldRepresentationTier.Near,
                    pair.Key.x,
                    pair.Key.y,
                    settings.ChunkSize);
                if (pair.Value != null
                    && worldCreatorRuntime.TryToLocal(key.Minimum, out var local))
                {
                    pair.Value.transform.localPosition = new Vector3(local.X, local.Y, local.Z);
                }
            }
        }

        private void OnDestroy()
        {
            ResumeStreamingTargetAfterInitialTerrain();
            farLandscape?.Dispose();
            farLandscape = null;
            worldCreatorRuntime?.Dispose();
            worldCreatorRuntime = null;
        }

        private readonly struct TerrainRequest
        {
            public TerrainRequest(
                WorldRepresentationKey key,
                Task<WorldRepresentationRequestOutcome> task)
            {
                Key = key;
                Task = task;
            }

            public WorldRepresentationKey Key { get; }
            public Task<WorldRepresentationRequestOutcome> Task { get; }
        }
    }
}
