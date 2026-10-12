using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public readonly struct FixedTerrainPopulationDemand
    {
        public readonly Vector2Int Cell;
        public readonly bool Visual, Collision;
        public readonly float Priority;
        public FixedTerrainPopulationDemand(Vector2Int cell, bool visual, bool collision, float priority = 0)
        {
            if (!float.IsFinite(priority)) throw new ArgumentException("Demand priority must be finite.");
            Cell = cell; Visual = visual; Collision = collision; Priority = priority;
        }
    }

    /// <summary>Main-thread bounded-area scheduler. Demand controls residency and priority, never placement identity.</summary>
    public sealed class FixedTerrainPopulationScheduler : IDisposable
    {
        private sealed class Entry
        {
            public Vector2Int Cell;
            public bool Wanted, Visual, Collision, Faulted;
            public float Priority;
            public double LastWanted, Since, RetryAfter;
            public FixedTerrainPopulationPlanner.CellBuild Build;
        }
        private readonly Dictionary<Vector2Int, Entry> entries = new();
        private readonly Dictionary<Vector2Int, FixedTerrainPopulationDemand> incoming = new();
        private readonly FixedTerrainPopulationPlanner planner;
        private readonly FixedTerrainPopulationPresentation presentation;
        private readonly FixedTerrainPopulationState state;
        private readonly double retention;
        private readonly Stopwatch clock = new();
        private double now;
        private bool disposed, presentationTurn;
        public int CancelledBuilds { get; private set; }
        public int CompletedBuilds { get; private set; }
        public int CapacityDeferrals { get; private set; }
        public int PendingBuilds { get { int count = 0; foreach (var e in entries.Values) if (e.Build != null) count++; return count; } }
        public int FaultedCells { get { int count = 0; foreach (var e in entries.Values) if (e.Faulted) count++; return count; } }
        public int UnreadyCells { get { int count = 0; foreach (var e in entries.Values) if (e.Wanted && !e.Faulted && (!presentation.IsActive(e.Cell) || presentation.IsPending(e.Cell))) count++; return count; } }

        /// <summary>All candidate presentations are published for current collision demand, including empty accepted cells.
        /// This is an activation barrier; the locomotion owner still owns physics synchronization and route/obstacle queries.</summary>
        public bool IsCollisionReady(Vector2Int cell)
        {
            ThrowIfDisposed();
            return entries.TryGetValue(cell, out var entry) && entry.Wanted && entry.Collision && !entry.Faulted
                && presentation.IsCollisionPublished(cell);
        }

        /// <summary>Every owner cell touched by a horizontal preparation region must be collision-ready.
        /// The caller includes actor lookahead and the population footprint halo in this region; visibility is irrelevant.</summary>
        public bool IsCollisionCoverageReady(Bounds region)
        {
            ThrowIfDisposed();
            var size = region.size; var min = region.min; var max = region.max;
            if (!float.IsFinite(size.x) || !float.IsFinite(size.z) || size.x < 0 || size.z < 0
                || !float.IsFinite(min.x) || !float.IsFinite(min.z) || !float.IsFinite(max.x) || !float.IsFinite(max.z)) return false;
            double x0 = Math.Floor((double)min.x / FixedTerrainPopulationIdentity.CellSize);
            double z0 = Math.Floor((double)min.z / FixedTerrainPopulationIdentity.CellSize);
            double x1 = Math.Floor((double)max.x / FixedTerrainPopulationIdentity.CellSize);
            double z1 = Math.Floor((double)max.z / FixedTerrainPopulationIdentity.CellSize);
            // Regions are closed: an exact lower seam also touches the preceding owner cell.
            if ((double)min.x / FixedTerrainPopulationIdentity.CellSize == x0) x0--;
            if ((double)min.z / FixedTerrainPopulationIdentity.CellSize == z0) z0--;
            double width = x1 - x0 + 1, height = z1 - z0 + 1;
            if (x0 < int.MinValue || z0 < int.MinValue || x1 > int.MaxValue || z1 > int.MaxValue
                || width < 1 || height < 1 || width * height > entries.Count) return false;
            // At most the explicitly bounded area is inspected, even for malicious/accidental huge coordinates.
            for (int z = 0; z < (int)height; z++) for (int x = 0; x < (int)width; x++)
                if (!IsCollisionReady(new Vector2Int((int)(x0 + x), (int)(z0 + z)))) return false;
            return true;
        }

        public FixedTerrainPopulationScheduler(FixedTerrainPopulationPlanner planner, FixedTerrainPopulationPresentation presentation,
            FixedTerrainPopulationState state, IEnumerable<Vector2Int> allowedCells, double retentionSeconds = 2)
        {
            this.planner = planner ?? throw new ArgumentNullException(nameof(planner));
            this.presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            if (!double.IsFinite(retentionSeconds) || retentionSeconds < 0 || allowedCells == null) throw new ArgumentException("Invalid bounded scheduler area/retention.");
            retention = retentionSeconds;
            foreach (var cell in allowedCells)
            {
                if (entries.Count >= 64 || !entries.TryAdd(cell, new Entry { Cell = cell, LastWanted = double.NegativeInfinity }))
                    throw new ArgumentException("Use one explicit area of at most 64 unique identity cells.");
            }
            if (entries.Count == 0) throw new ArgumentException("A scheduler area is required.");
            state.SnapshotApplied += ResetJobs;
        }

        /// <summary>Atomic replacement of demand. Include all actors, even outside camera coverage.</summary>
        public void SetDemand(IEnumerable<FixedTerrainPopulationDemand> demands, double time)
        {
            ThrowIfDisposed();
            if (demands == null || !double.IsFinite(time) || time < now) throw new ArgumentException("Use finite monotonic demand time.");
            incoming.Clear(); int records = 0;
            foreach (var demand in demands)
            {
                if (++records > 256 || !entries.ContainsKey(demand.Cell) || !float.IsFinite(demand.Priority))
                    throw new ArgumentException("Demand must fit the explicit area and bounded input capacity.");
                if (!demand.Visual && !demand.Collision) continue;
                if (incoming.TryGetValue(demand.Cell, out var prior))
                    incoming[demand.Cell] = new FixedTerrainPopulationDemand(demand.Cell, prior.Visual || demand.Visual,
                        prior.Collision || demand.Collision, Mathf.Max(prior.Priority, demand.Priority));
                else incoming.Add(demand.Cell, demand);
            }
            now = time;
            foreach (var entry in entries.Values)
            {
                bool wanted = incoming.TryGetValue(entry.Cell, out var demand);
                if (wanted)
                {
                    if (!entry.Wanted) entry.Since = now;
                    if (entry.Visual != demand.Visual) presentation.SetVisualRelevant(entry.Cell, demand.Visual);
                    if (entry.Collision != demand.Collision) presentation.SetCollisionRelevant(entry.Cell, demand.Collision);
                    entry.Visual = demand.Visual; entry.Collision = demand.Collision; entry.Priority = demand.Priority; entry.LastWanted = now;
                }
                if (!wanted && entry.Build != null) { entry.Build.Cancel(); entry.Build = null; CancelledBuilds++; }
                entry.Wanted = wanted;
            }
        }

        /// <summary>Shared budget for planning, integration and retirement. A single prefab acquisition is indivisible.</summary>
        public int Advance(int maximumOperations, double milliseconds, double time)
        {
            ThrowIfDisposed();
            if (maximumOperations < 1 || !double.IsFinite(milliseconds) || milliseconds <= 0 || !double.IsFinite(time) || time < now)
                throw new ArgumentException("Use finite positive work limits and monotonic time.");
            now = time; clock.Restart(); int count = 0;
            while (count < maximumOperations && clock.Elapsed.TotalMilliseconds < milliseconds)
            {
                Entry expired = null;
                foreach (var entry in entries.Values)
                    if (!entry.Wanted && now - entry.LastWanted >= retention && (presentation.IsActive(entry.Cell) || presentation.IsPending(entry.Cell)))
                    { expired = entry; break; }
                if (expired != null) { presentation.Release(expired.Cell); count++; continue; }
                Entry selected = SelectPlanning();
                presentationTurn = !presentationTurn;
                double remaining = milliseconds - clock.Elapsed.TotalMilliseconds;
                if (remaining <= 0) break;
                if (presentationTurn || selected == null)
                {
                    int work = presentation.Advance(1, remaining);
                    if (work > 0) { count += work; continue; }
                    if (selected == null) break;
                }
                if (selected.Build == null) { selected.Build = planner.Begin(selected.Cell); count++; continue; }
                if (!selected.Build.Complete)
                {
                    remaining = milliseconds - clock.Elapsed.TotalMilliseconds;
                    if (remaining <= 0) break;
                    count += selected.Build.Advance(1, remaining); continue;
                }
                if (!selected.Build.TryCommitBaseline(state)) { selected.Faulted = true; selected.Build.Cancel(); selected.Build = null; count++; continue; }
                if (presentation.Queue(selected.Cell, selected.Build, selected.Collision, selected.Visual))
                { selected.Build.Cancel(); selected.Build = null; CompletedBuilds++; }
                else { selected.RetryAfter = now + .2; CapacityDeferrals++; }
                count++;
            }
            return count;
        }

        private Entry SelectPlanning()
        {
            Entry best = null;
            foreach (var entry in entries.Values)
            {
                if (!entry.Wanted || entry.Faulted || entry.RetryAfter > now || presentation.IsActive(entry.Cell) || presentation.IsPending(entry.Cell)) continue;
                if (best == null || entry.Collision && !best.Collision || entry.Collision == best.Collision && Better(entry, best)) best = entry;
            }
            return best;
        }
        private bool Better(Entry a, Entry b)
        {
            double ap = a.Priority + (now - a.Since), bp = b.Priority + (now - b.Since);
            return ap > bp || ap == bp && (a.Cell.x < b.Cell.x || a.Cell.x == b.Cell.x && a.Cell.y < b.Cell.y);
        }
        private void ResetJobs()
        {
            foreach (var entry in entries.Values)
            {
                if (entry.Build != null) { entry.Build.Cancel(); entry.Build = null; CancelledBuilds++; }
                entry.Faulted = false; entry.RetryAfter = now;
            }
        }
        private void ThrowIfDisposed() { if (disposed) throw new ObjectDisposedException(nameof(FixedTerrainPopulationScheduler)); }
        public void Dispose()
        {
            if (disposed) return;
            state.SnapshotApplied -= ResetJobs; ResetJobs();
            foreach (var entry in entries.Values) presentation.Release(entry.Cell);
            disposed = true;
        }
    }
}
