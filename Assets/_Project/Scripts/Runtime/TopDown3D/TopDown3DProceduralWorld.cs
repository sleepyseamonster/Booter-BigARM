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
        private static readonly ProfilerMarker DecorateNaturalObjectsMarker =
            new ProfilerMarker("TopDown3D.World.DecorateNaturalObjects");
        private static readonly ProfilerMarker DecorateResourceNodesMarker =
            new ProfilerMarker("TopDown3D.World.DecorateResourceNodes");

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
        private readonly Queue<Vector2Int> pendingFormationRefreshes = new Queue<Vector2Int>();
        private readonly HashSet<Vector2Int> queuedFormationRefreshes = new HashSet<Vector2Int>();
        private IEnumerator<int> activeFormationRefresh;
        private Vector2Int activeFormationRefreshCoordinate;
        private long activeFormationRefreshToken;
        private bool activeFormationRefreshInvalidated;
        private TopDown3DNaturalObjectPlanner.Work activeDecorationPlan;
        private TopDown3DNaturalObjectChunkPlan activeChunkPlan;
        private IEnumerator<int> activeNaturalObjects;
        private IEnumerator<int> activeResources;
        private IEnumerator<int> activeCliffFaces;
        private Vector2Int activeDecorationCoordinate;
        private long activeDecorationToken;
        private AbsoluteWorldPosition activeDecorationOrigin;
        private int activeDecorationStage;
        private readonly Dictionary<Vector2Int, (IReadOnlyList<TopDown3DRockFormationPlan> plans,
            AbsoluteWorldPosition origin)> formationTreatments =
            new Dictionary<Vector2Int, (IReadOnlyList<TopDown3DRockFormationPlan>, AbsoluteWorldPosition)>();
        private readonly Queue<Vector2Int> pendingTerrainColliders = new Queue<Vector2Int>();
        private readonly HashSet<Vector2Int> queuedTerrainColliders = new HashSet<Vector2Int>();
        private readonly List<Vector2Int> unloadBuffer = new List<Vector2Int>();
        private readonly Dictionary<Vector2Int, TerrainRequest> terrainRequests =
            new Dictionary<Vector2Int, TerrainRequest>();
        private readonly Dictionary<Vector2Int, double> terrainRetryDue =
            new Dictionary<Vector2Int, double>();
        private readonly Dictionary<Vector2Int, int> terrainRetryCounts =
            new Dictionary<Vector2Int, int>();
        private readonly Dictionary<Vector2Int, double> missingTerrainIntegrationSince =
            new Dictionary<Vector2Int, double>();
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
            + terrainRequests.Count + terrainRetryDue.Count;
        public int PendingTerrainColliderCount => queuedTerrainColliders.Count;
        public int PendingDecorationCount => queuedDecorations.Count
            + (activeDecorationStage != 0 ? 1 : 0) + queuedFormationRefreshes.Count
            + (activeFormationRefresh != null ? 1 : 0);
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
                groundMaterial,
                IsNearTerrainLoaded);
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

                        if (!loadedChunks.ContainsKey(coordinate)
                            && !terrainRequests.ContainsKey(coordinate)
                            && !terrainRetryDue.ContainsKey(coordinate))
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
                unloadBuffer.Clear();
                foreach (var pair in terrainRetryCounts)
                    if (!requiredChunks.Contains(pair.Key)) unloadBuffer.Add(pair.Key);
                for (var i = 0; i < unloadBuffer.Count; i++)
                {
                    terrainRetryDue.Remove(unloadBuffer[i]);
                    terrainRetryCounts.Remove(unloadBuffer[i]);
                }
                if (force)
                {
                    var immediateRadius = Mathf.Min(settings.ImmediateLoadRadius, streamingRadius);
                    var immediate = new List<Vector2Int>();
                    for (var i = pendingChunks.Count - 1; i >= 0; i--)
                    {
                        var coordinate = pendingChunks[i];
                        if (ChebyshevDistance(coordinate, center) > immediateRadius) continue;
                        immediate.Add(coordinate);
                        pendingChunks.RemoveAt(i);
                    }
                    immediate.Sort(ComparePendingChunks);
                    foreach (var coordinate in immediate) RequestChunkTerrain(coordinate);

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
                        chunk.gameObject.SetActive(false);
                        Destroy(chunk.gameObject);
                    }

                    loadedChunks.Remove(coordinate);
                    farLandscape?.NotifyNearCoverageChanged(coordinate.x, coordinate.y);
                    if (activeDecorationStage != 0 && activeDecorationCoordinate == coordinate)
                        CancelActiveDecoration(false);
                    if (formationTreatments.Remove(coordinate)) QueueFormationTerrainNear(coordinate);
                    terrainRequests.Remove(coordinate);
                    terrainRetryDue.Remove(coordinate);
                    terrainRetryCounts.Remove(coordinate);
                    missingTerrainIntegrationSince.Remove(coordinate);
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
                    if (activeDecorationStage != 0 && activeDecorationCoordinate == coordinate)
                        CancelActiveDecoration(false);
                    if (formationTreatments.Remove(coordinate)) QueueFormationTerrainNear(coordinate);
                    else QueueFormationTerrainRefresh(coordinate);
                    queuedDecorations.Remove(coordinate);
                }

                RefreshTerrainCollisionStreaming();
                if (activeDecorationStage != 0
                    && !requiredDecoratedChunks.Contains(activeDecorationCoordinate))
                    CancelActiveDecoration(false);
            }
        }

        private void ProcessPendingChunks(int budget)
        {
            using (ProcessPendingChunksMarker.Auto())
            {
                // Planning yields after surface samples and decoration after rock members.
                // Allow many small steps, but stop as soon as the frame's time is spent.
                var count = Mathf.Max(0, budget * 128);
                var startedAt = Time.realtimeSinceStartupAsDouble;
                // Keep nearby collision and terrain ahead of decoration, then advance
                // one decoration step before distant terrain and ground refreshes.
                if (!TryCreatePendingTerrainCollider() && !TryIntegrateRequestedTerrainWithin(1))
                    TryProcessDecoration();
                for (var i = 0; i < count; i++)
                {
                    if (!TryCreatePendingTerrainCollider()
                        && !TryIntegrateRequestedTerrain()
                        && !TryRequestDueTerrainRetry()
                        && !TryRefreshPendingFormationTerrain()
                        && !TryProcessDecoration())
                    {
                        if (pendingChunkCursor >= pendingChunks.Count)
                        {
                            break;
                        }

                        // Bound cold terrain builds so decoration queries can make progress.
                        if (terrainRequests.Count >= 32) break;

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

            var cameraRig = Camera.main != null
                ? Camera.main.GetComponent<TopDown3DCameraRig>()
                : null;
            if (cameraRig != null)
            {
                cameraRig.SnapToTarget();
            }

            var companion = FindFirstObjectByType<TopDown3DBigArmFollower>();
            if (companion != null)
            {
                var companionPosition = companion.transform.position;
                companion.PrepareInitialGroundHeight(
                    worldGenerator.SampleHeight(companionPosition.x, companionPosition.z));
            }
        }

        private void RequestChunkTerrain(Vector2Int coordinate)
        {
            if (loadedChunks.ContainsKey(coordinate)
                || terrainRequests.ContainsKey(coordinate)
                || worldCreatorRuntime == null
                || settings == null
                || (terrainRetryDue.TryGetValue(coordinate, out var retryDue)
                    && Time.realtimeSinceStartupAsDouble < retryDue))
            {
                return;
            }

            terrainRetryDue.Remove(coordinate);
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
            return TryIntegrateRequestedTerrainWithin(int.MaxValue);
        }

        private bool TryIntegrateRequestedTerrainWithin(int maximumDistance)
        {
            var found = false;
            var coordinate = default(Vector2Int);
            var request = default(TerrainRequest);
            foreach (var pair in terrainRequests)
            {
                if (ChebyshevDistance(pair.Key, currentCenterChunk) > maximumDistance
                    || !pair.Value.Task.IsCompleted
                    || (found && ComparePendingChunks(pair.Key, coordinate) >= 0)) continue;

                var candidateOutcome = pair.Value.Task.GetAwaiter().GetResult();
                if (candidateOutcome.State == WorldRepresentationRequestState.QueuedForIntegration
                    && requiredChunks.Contains(pair.Key)
                    && !worldCreatorRuntime.TryGetIntegrated(pair.Value.Key, out _))
                {
                    var now = Time.realtimeSinceStartupAsDouble;
                    if (!missingTerrainIntegrationSince.TryGetValue(pair.Key, out var since))
                    {
                        missingTerrainIntegrationSince.Add(pair.Key, now);
                        since = now;
                    }
                    // A waiting nearest tile must not delay another completed tile.
                    // An emptied queue or expired wait still reaches the retry path below.
                    if (worldCreatorRuntime.CaptureMetrics().Queued > 0 && now - since < 5d)
                        continue;
                }

                found = true;
                coordinate = pair.Key;
                request = pair.Value;
            }

            if (!found)
            {
                return false;
            }

            var outcome = request.Task.GetAwaiter().GetResult();
            if (outcome.State == WorldRepresentationRequestState.Failed
                || outcome.State == WorldRepresentationRequestState.Cancelled)
            {
                Debug.LogError($"World Creator near representation {coordinate} {outcome.State}: {outcome.Error}", this);
                terrainRequests.Remove(coordinate);
                ScheduleTerrainRetry(coordinate);
                return true;
            }

            if (!requiredChunks.Contains(coordinate))
            {
                terrainRequests.Remove(coordinate);
                terrainRetryDue.Remove(coordinate);
                terrainRetryCounts.Remove(coordinate);
                missingTerrainIntegrationSince.Remove(coordinate);
                return true;
            }

            if (!worldCreatorRuntime.TryGetIntegrated(request.Key, out var result))
            {
                var now = Time.realtimeSinceStartupAsDouble;
                if (!missingTerrainIntegrationSince.TryGetValue(coordinate, out var since))
                {
                    missingTerrainIntegrationSince.Add(coordinate, now);
                    since = now;
                }
                if (outcome.State == WorldRepresentationRequestState.QueuedForIntegration
                    && worldCreatorRuntime.CaptureMetrics().Queued > 0
                    && now - since < 5d)
                    return false;
                terrainRequests.Remove(coordinate);
                missingTerrainIntegrationSince.Remove(coordinate);
                ScheduleTerrainRetry(coordinate);
                return true;
            }

            terrainRequests.Remove(coordinate);
            terrainRetryDue.Remove(coordinate);
            terrainRetryCounts.Remove(coordinate);
            missingTerrainIntegrationSince.Remove(coordinate);
            IntegrateChunkTerrain(coordinate, result);
            return true;
        }

        private void ScheduleTerrainRetry(Vector2Int coordinate)
        {
            if (!requiredChunks.Contains(coordinate)) return;
            var attempts = terrainRetryCounts.TryGetValue(coordinate, out var previous)
                ? Mathf.Min(7, previous + 1) : 1;
            terrainRetryCounts[coordinate] = attempts;
            terrainRetryDue[coordinate] = Time.realtimeSinceStartupAsDouble
                + Mathf.Min(30f, 0.5f * (1 << (attempts - 1)));
        }

        private bool TryRequestDueTerrainRetry()
        {
            var now = Time.realtimeSinceStartupAsDouble;
            var found = false;
            var nearest = default(Vector2Int);
            foreach (var pair in terrainRetryDue)
            {
                if (pair.Value > now || !requiredChunks.Contains(pair.Key)
                    || loadedChunks.ContainsKey(pair.Key) || terrainRequests.ContainsKey(pair.Key)
                    || (found && ComparePendingChunks(pair.Key, nearest) >= 0)) continue;
                nearest = pair.Key;
                found = true;
            }
            if (!found) return false;
            RequestChunkTerrain(nearest);
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
                farLandscape?.NotifyNearCoverageChanged(coordinate.x, coordinate.y);
                QueueFormationTerrainRefresh(coordinate);
                QueueTerrainCollider(coordinate);
                if (requiredDecoratedChunks.Contains(coordinate))
                {
                    EnqueueDecoration(coordinate);
                }

            }
        }

        private bool IsNearTerrainLoaded(long a, long b)
        {
            return a >= int.MinValue && a <= int.MaxValue
                && b >= int.MinValue && b <= int.MaxValue
                && loadedChunks.ContainsKey(new Vector2Int((int)a, (int)b));
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
            if (decoratedChunks.Contains(coordinate)
                || (activeDecorationStage != 0 && activeDecorationCoordinate == coordinate)
                || !queuedDecorations.Add(coordinate))
            {
                return;
            }

            pendingDecorations.Enqueue(coordinate);
        }

        private bool TryProcessDecoration()
        {
            if (activeDecorationStage == 0)
            {
                if (pendingDecorations.Count > 1)
                {
                    var nearestFirst = new List<Vector2Int>(pendingDecorations);
                    nearestFirst.Sort(ComparePendingChunks);
                    pendingDecorations.Clear();
                    foreach (var coordinate in nearestFirst) pendingDecorations.Enqueue(coordinate);
                }
                while (pendingDecorations.Count > 0)
                {
                    var coordinate = pendingDecorations.Dequeue();
                    if (!queuedDecorations.Remove(coordinate)
                        || decoratedChunks.Contains(coordinate)
                        || !requiredDecoratedChunks.Contains(coordinate)
                        || !loadedChunks.TryGetValue(coordinate, out var chunk)
                        || chunk == null) continue;
                    activeDecorationCoordinate = coordinate;
                    activeDecorationToken = chunk.GenerationToken;
                    activeDecorationOrigin = worldCreatorRuntime.CurrentFrame.OriginPosition;
                    activeDecorationPlan = new TopDown3DNaturalObjectPlanner.Work(settings,
                        worldGenerator, settings.NaturalObjectCatalog, coordinate, spawnExclusionCenter);
                    activeDecorationStage = 1;
                    break;
                }
            }
            if (activeDecorationStage == 0) return false;
            if (!requiredDecoratedChunks.Contains(activeDecorationCoordinate)
                || !loadedChunks.TryGetValue(activeDecorationCoordinate, out var activeChunk)
                || activeChunk == null || activeChunk.GenerationToken != activeDecorationToken
                || !activeDecorationOrigin.Equals(worldCreatorRuntime.CurrentFrame.OriginPosition))
            {
                CancelActiveDecoration(requiredDecoratedChunks.Contains(activeDecorationCoordinate));
                return true;
            }

            using (DecorateChunkMarker.Auto())
            {
                if (activeDecorationStage == 1)
                {
                    using (PlanNaturalObjectsMarker.Auto()) activeDecorationPlan.Step();
                    if (activeDecorationPlan.IsWaitingForReservation) return false;
                    if (activeDecorationPlan.IsComplete)
                    {
                        activeChunkPlan = activeDecorationPlan.Result;
                        activeDecorationPlan = null;
                        activeNaturalObjects = TopDown3DNaturalObjectDecorator.DecorateSteps(
                            activeChunk, settings, propMaterial, activeChunkPlan).GetEnumerator();
                        activeDecorationStage = 2;
                    }
                }
                else if (activeDecorationStage == 2)
                {
                    bool hasMore;
                    using (DecorateNaturalObjectsMarker.Auto()) hasMore = activeNaturalObjects.MoveNext();
                    if (!hasMore)
                    {
                        activeNaturalObjects.Dispose();
                        activeNaturalObjects = null;
                        if (activeChunkPlan.PhysicalFormations.Count > 0)
                        {
                            formationTreatments[activeDecorationCoordinate] =
                                (activeChunkPlan.PhysicalFormations, activeDecorationOrigin);
                            QueueFormationTerrainNear(activeDecorationCoordinate);
                        }
                        activeResources = TopDown3DResourceNodeDecorator.DecorateSteps(
                            activeChunk, settings, activeChunkPlan, resourceWorldState).GetEnumerator();
                        activeDecorationStage = 3;
                    }
                }
                else if (activeDecorationStage == 3)
                {
                    bool hasMore;
                    using (DecorateResourceNodesMarker.Auto()) hasMore = activeResources.MoveNext();
                    if (!hasMore)
                    {
                        activeResources.Dispose();
                        activeResources = null;
                        activeCliffFaces = TopDown3DCliffFaceDecorator.DecorateSteps(
                            activeChunk, settings, worldCreatorRuntime, spawnExclusionCenter,
                            activeChunkPlan.PhysicalFormations).GetEnumerator();
                        activeDecorationStage = 4;
                    }
                }
                else if (activeDecorationStage == 4)
                {
                    if (!activeCliffFaces.MoveNext())
                    {
                        activeCliffFaces.Dispose();
                        activeCliffFaces = null;
                        activeDecorationStage = 5;
                    }
                }
                else
                {
                    TopDown3DDustDepositionDecorator.Decorate(activeChunk, settings,
                        worldGenerator, groundMaterial, spawnExclusionCenter);
                    activeChunk.RefreshDecorationCounts();
                    decoratedChunks.Add(activeDecorationCoordinate);
                    activeDecorationStage = 0;
                    activeChunkPlan = null;
                }
            }
            return true;
        }

        private void CancelActiveDecoration(bool retry)
        {
            if (activeDecorationStage == 0) return;
            var coordinate = activeDecorationCoordinate;
            var createdObjects = activeDecorationStage >= 2;
            activeNaturalObjects?.Dispose();
            activeResources?.Dispose();
            activeCliffFaces?.Dispose();
            activeDecorationPlan?.Dispose();
            activeNaturalObjects = null;
            activeResources = null;
            activeCliffFaces = null;
            activeDecorationPlan = null;
            activeChunkPlan = null;
            activeDecorationStage = 0;
            if (createdObjects && loadedChunks.TryGetValue(coordinate, out var chunk)
                && chunk != null && chunk.GenerationToken == activeDecorationToken)
            {
                chunk.ClearDecoration();
                QueueFormationTerrainRefresh(coordinate);
            }
            if (formationTreatments.Remove(coordinate)) QueueFormationTerrainNear(coordinate);
            if (retry && requiredDecoratedChunks.Contains(coordinate)) EnqueueDecoration(coordinate);
        }

        private void QueueFormationTerrainNear(Vector2Int center)
        {
            foreach (var pair in loadedChunks)
                if (ChebyshevDistance(pair.Key, center) <= 3)
                    QueueFormationTerrainRefresh(pair.Key);
        }

        private void QueueFormationTerrainRefresh(Vector2Int coordinate)
        {
            if (activeFormationRefresh != null && activeFormationRefreshCoordinate == coordinate)
                activeFormationRefreshInvalidated = true;
            if (queuedFormationRefreshes.Add(coordinate)) pendingFormationRefreshes.Enqueue(coordinate);
        }

        private bool TryRefreshPendingFormationTerrain()
        {
            if (activeFormationRefresh != null)
            {
                if (activeFormationRefreshInvalidated
                    || !loadedChunks.TryGetValue(activeFormationRefreshCoordinate, out var current)
                    || current == null || current.GenerationToken != activeFormationRefreshToken)
                {
                    activeFormationRefresh.Dispose();
                    activeFormationRefresh = null;
                    activeFormationRefreshInvalidated = false;
                    return true;
                }
                if (!activeFormationRefresh.MoveNext())
                {
                    activeFormationRefresh.Dispose();
                    activeFormationRefresh = null;
                }
                return true;
            }
            while (pendingFormationRefreshes.Count > 0)
            {
                var coordinate = pendingFormationRefreshes.Dequeue();
                if (!queuedFormationRefreshes.Remove(coordinate)
                    || !loadedChunks.TryGetValue(coordinate, out var chunk)
                    || chunk == null) continue;
                activeFormationRefreshCoordinate = coordinate;
                activeFormationRefreshToken = chunk.GenerationToken;
                activeFormationRefresh = RefreshFormationTerrainForChunkSteps(coordinate).GetEnumerator();
                return true;
            }
            return false;
        }

        private IEnumerable<int> RefreshFormationTerrainForChunkSteps(Vector2Int coordinate)
        {
            if (!loadedChunks.TryGetValue(coordinate, out var chunk) || chunk == null) yield break;
            var nearby = new List<TopDown3DFormationTerrainShader.Influence>();
            var currentOrigin = worldCreatorRuntime.CurrentFrame.OriginPosition;
            foreach (var pair in formationTreatments)
            {
                if (ChebyshevDistance(pair.Key, coordinate) > 3) continue;
                var shift = new Vector2(
                    checked((float)(pair.Value.origin.HorizontalA - currentOrigin.HorizontalA)),
                    checked((float)(pair.Value.origin.HorizontalB - currentOrigin.HorizontalB)));
                foreach (var formation in pair.Value.plans)
                    nearby.Add(new TopDown3DFormationTerrainShader.Influence(formation, shift));
            }
            foreach (var step in TopDown3DFormationTerrainShader.ApplySteps(chunk, settings, nearby))
                yield return step;
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
            // A plan stores local-frame positions and must be restarted after a rebase.
            if (activeDecorationStage != 0) CancelActiveDecoration(true);
            if (activeFormationRefresh != null)
            {
                activeFormationRefresh.Dispose();
                activeFormationRefresh = null;
                activeFormationRefreshInvalidated = false;
                QueueFormationTerrainRefresh(activeFormationRefreshCoordinate);
            }
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
                    TopDown3DFormationTerrainShader.RepositionMask(pair.Value, settings);
                }
            }
        }

        private void OnDestroy()
        {
            CancelActiveDecoration(false);
            activeFormationRefresh?.Dispose();
            activeFormationRefresh = null;
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
