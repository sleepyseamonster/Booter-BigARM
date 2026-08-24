using System;
using System.Collections.Generic;

namespace BooterBigArm.TopDown3D.WorldCreator
{
    /// <summary>
    /// Deterministically materializes bounded plan windows on demand while preserving an
    /// effectively unbounded absolute query space. Cache cells index disposable data only;
    /// they never author geography, identity, thematic coordinates, or persisted deltas.
    /// </summary>
    public sealed class UnboundedHybridWorldQueryService : IWorldQueryService
    {
        private readonly object gate = new object();
        private readonly WorldIdentity world;
        private readonly IWorldCoordinateModel coordinateModel;
        private readonly IWorldCoordinateContextProvider contextProvider;
        private readonly CanyonSystemPlanner canyonPlanner;
        private readonly int maximumCanyonPlans;
        private readonly int maximumTerrainWindows;
        private readonly Dictionary<CanyonSystemCellIndex, CanyonCacheEntry> canyonPlans =
            new Dictionary<CanyonSystemCellIndex, CanyonCacheEntry>();
        private readonly LinkedList<CanyonSystemCellIndex> canyonRecency =
            new LinkedList<CanyonSystemCellIndex>();
        private readonly Dictionary<CanyonSystemCellIndex, TerrainCacheEntry> terrainWindows =
            new Dictionary<CanyonSystemCellIndex, TerrainCacheEntry>();
        private readonly LinkedList<CanyonSystemCellIndex> terrainRecency =
            new LinkedList<CanyonSystemCellIndex>();
        private long canyonPlanBuilds;
        private long terrainWindowBuilds;
        private long canyonCacheHits;
        private long terrainCacheHits;
        private long canyonEvictions;
        private long terrainEvictions;

        public UnboundedHybridWorldQueryService(
            WorldIdentity world,
            IWorldCoordinateModel coordinateModel,
            IWorldCoordinateContextProvider contextProvider,
            CanyonPlannerProfile canyonProfile,
            int maximumCanyonPlans = 96,
            int maximumTerrainWindows = 24)
        {
            this.world = world;
            this.coordinateModel = coordinateModel ?? throw new ArgumentNullException(nameof(coordinateModel));
            this.contextProvider = contextProvider ?? throw new ArgumentNullException(nameof(contextProvider));
            if (maximumCanyonPlans < 9) throw new ArgumentOutOfRangeException(nameof(maximumCanyonPlans));
            if (maximumTerrainWindows < 1) throw new ArgumentOutOfRangeException(nameof(maximumTerrainWindows));
            this.maximumCanyonPlans = maximumCanyonPlans;
            this.maximumTerrainWindows = maximumTerrainWindows;
            canyonPlanner = new CanyonSystemPlanner(contextProvider, canyonProfile);
        }

        public bool TrySampleSurface(AbsoluteWorldPosition position, out WorldSurfaceSample sample, out string error)
        {
            if (!TryGetWindow(position, out var query, out error))
            {
                sample = default;
                return false;
            }

            return query.TrySampleSurface(position, out sample, out error);
        }

        public bool TrySampleVolume(AbsoluteWorldPosition position, out WorldVolumeSample sample, out string error)
        {
            if (!TryGetWindow(position, out var query, out error))
            {
                sample = default;
                return false;
            }

            return query.TrySampleVolume(position, out sample, out error);
        }

        public bool TrySampleAffordance(
            AbsoluteWorldPosition position,
            WorldAgentProfile agent,
            out WorldAffordanceSample sample,
            out string error)
        {
            if (!TryGetWindow(position, out var query, out error))
            {
                sample = default;
                return false;
            }

            return query.TrySampleAffordance(position, agent, out sample, out error);
        }

        public WorldQueryCacheSnapshot CaptureCacheSnapshot()
        {
            lock (gate)
            {
                return new WorldQueryCacheSnapshot(
                    canyonPlans.Count,
                    terrainWindows.Count,
                    maximumCanyonPlans,
                    maximumTerrainWindows,
                    canyonPlanBuilds,
                    terrainWindowBuilds,
                    canyonCacheHits,
                    terrainCacheHits,
                    canyonEvictions,
                    terrainEvictions);
            }
        }

        public void ClearCaches()
        {
            lock (gate)
            {
                canyonPlans.Clear();
                canyonRecency.Clear();
                terrainWindows.Clear();
                terrainRecency.Clear();
            }
        }

        private bool TryGetWindow(
            AbsoluteWorldPosition position,
            out HybridTerrainWindowQueryService query,
            out string error)
        {
            var cell = CanyonSystemCellIndex.FromAbsolute(position, canyonPlanner.Profile.CellSpan);
            lock (gate)
            {
                if (terrainWindows.TryGetValue(cell, out var cached))
                {
                    terrainRecency.Remove(cached.Node);
                    terrainRecency.AddLast(cached.Node);
                    terrainCacheHits++;
                    query = cached.Query;
                    error = null;
                    return true;
                }

                try
                {
                    var plans = new List<CanyonSystemPlan>(9);
                    for (var offsetA = -1; offsetA <= 1; offsetA++)
                    {
                        for (var offsetB = -1; offsetB <= 1; offsetB++)
                        {
                            CanyonSystemCellIndex participant;
                            try
                            {
                                participant = cell.Offset(offsetA, offsetB);
                            }
                            catch (OverflowException)
                            {
                                query = null;
                                error = "The requested absolute position exceeds the supported technical planning index range.";
                                return false;
                            }

                            if (!TryGetCanyonPlan(participant, out var plan, out error))
                            {
                                query = null;
                                return false;
                            }

                            plans.Add(plan);
                        }
                    }

                    var terrain = HybridTerrainCompiler.Compile(world, plans);
                    query = new HybridTerrainWindowQueryService(terrain, coordinateModel, contextProvider);
                    var node = terrainRecency.AddLast(cell);
                    terrainWindows.Add(cell, new TerrainCacheEntry(query, node));
                    terrainWindowBuilds++;
                    EnforceTerrainBudget();
                    error = null;
                    return true;
                }
                catch (Exception exception)
                {
                    query = null;
                    error = "Unable to compile the requested hybrid terrain window: " + exception.Message;
                    return false;
                }
            }
        }

        private bool TryGetCanyonPlan(
            CanyonSystemCellIndex cell,
            out CanyonSystemPlan plan,
            out string error)
        {
            if (canyonPlans.TryGetValue(cell, out var cached))
            {
                canyonRecency.Remove(cached.Node);
                canyonRecency.AddLast(cached.Node);
                canyonCacheHits++;
                plan = cached.Plan;
                error = null;
                return true;
            }

            if (!canyonPlanner.TryBuild(world, coordinateModel, cell, out plan, out error)) return false;
            var node = canyonRecency.AddLast(cell);
            canyonPlans.Add(cell, new CanyonCacheEntry(plan, node));
            canyonPlanBuilds++;
            EnforceCanyonBudget();
            return true;
        }

        private void EnforceCanyonBudget()
        {
            while (canyonPlans.Count > maximumCanyonPlans)
            {
                var oldest = canyonRecency.First;
                if (oldest == null) return;
                canyonPlans.Remove(oldest.Value);
                canyonRecency.RemoveFirst();
                canyonEvictions++;
            }
        }

        private void EnforceTerrainBudget()
        {
            while (terrainWindows.Count > maximumTerrainWindows)
            {
                var oldest = terrainRecency.First;
                if (oldest == null) return;
                terrainWindows.Remove(oldest.Value);
                terrainRecency.RemoveFirst();
                terrainEvictions++;
            }
        }

        private sealed class CanyonCacheEntry
        {
            public CanyonCacheEntry(CanyonSystemPlan plan, LinkedListNode<CanyonSystemCellIndex> node)
            {
                Plan = plan;
                Node = node;
            }

            public CanyonSystemPlan Plan { get; }
            public LinkedListNode<CanyonSystemCellIndex> Node { get; }
        }

        private sealed class TerrainCacheEntry
        {
            public TerrainCacheEntry(HybridTerrainWindowQueryService query, LinkedListNode<CanyonSystemCellIndex> node)
            {
                Query = query;
                Node = node;
            }

            public HybridTerrainWindowQueryService Query { get; }
            public LinkedListNode<CanyonSystemCellIndex> Node { get; }
        }
    }

    public readonly struct WorldQueryCacheSnapshot
    {
        public WorldQueryCacheSnapshot(
            int canyonPlanCount,
            int terrainWindowCount,
            int maximumCanyonPlans,
            int maximumTerrainWindows,
            long canyonPlanBuilds,
            long terrainWindowBuilds,
            long canyonCacheHits,
            long terrainCacheHits,
            long canyonEvictions,
            long terrainEvictions)
        {
            CanyonPlanCount = canyonPlanCount;
            TerrainWindowCount = terrainWindowCount;
            MaximumCanyonPlans = maximumCanyonPlans;
            MaximumTerrainWindows = maximumTerrainWindows;
            CanyonPlanBuilds = canyonPlanBuilds;
            TerrainWindowBuilds = terrainWindowBuilds;
            CanyonCacheHits = canyonCacheHits;
            TerrainCacheHits = terrainCacheHits;
            CanyonEvictions = canyonEvictions;
            TerrainEvictions = terrainEvictions;
        }

        public int CanyonPlanCount { get; }
        public int TerrainWindowCount { get; }
        public int MaximumCanyonPlans { get; }
        public int MaximumTerrainWindows { get; }
        public long CanyonPlanBuilds { get; }
        public long TerrainWindowBuilds { get; }
        public long CanyonCacheHits { get; }
        public long TerrainCacheHits { get; }
        public long CanyonEvictions { get; }
        public long TerrainEvictions { get; }
    }
}
