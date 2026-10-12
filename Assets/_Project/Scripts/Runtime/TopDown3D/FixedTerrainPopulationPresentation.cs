using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Bounded static-prefab presentation. Logical receipts/removals outlive every pooled Unity object.</summary>
    public sealed class FixedTerrainPopulationPresentation : IDisposable
    {
        private sealed class Instance
        {
            public GameObject Object;
            public Collider[] Colliders;
            public bool[] CollisionEnabled;
            public bool[] CollisionRequired;
            public string StableId;
            public int Ordinal;
            public Renderer[] Renderers;
            public bool[] RenderingEnabled;
            public LODGroup[] Lods;
            public bool[] LodEnabled;
        }
        private sealed class Request
        {
            public Vector2Int Cell;
            public FixedTerrainPopulationPlacement[] Plan;
            public bool CollisionRelevant;
            public bool VisualRelevant;
            public int Next;
            public int Activation;
            public readonly List<Instance> Prepared = new();
        }
        private readonly GameObject prefab;
        private readonly Transform parent;
        private readonly FixedTerrainPopulationState state;
        private readonly string recipeId;
        private readonly int capacity, poolCapacity, pendingCapacity;
        private readonly Stack<Instance> pool = new();
        private readonly Dictionary<Vector2Int, List<Instance>> active = new();
        private readonly Dictionary<Vector2Int, Request> requests = new();
        private readonly Queue<Instance> retiring = new();
        private readonly Stopwatch clock = new();
        private int instances;
        private bool disposed;
        public int InstanceCount => instances;
        public ulong CollisionRevision { get; private set; }
        public int PendingCells => requests.Count;
        public int ActiveCount { get { int count = 0; foreach (var pair in active) count += pair.Value.Count; return count; } }
        public int AvailablePreparationSlots
        {
            get
            {
                int reserved = 0;
                foreach (var pair in requests)
                    for (int i = pair.Value.Next; i < pair.Value.Plan.Length; i++)
                        if (!state.IsRemoved(pair.Key, recipeId, pair.Value.Plan[i].Ordinal)) reserved++;
                return capacity - instances + pool.Count - reserved;
            }
        }

        public FixedTerrainPopulationPresentation(GameObject prefab, Transform parent, FixedTerrainPopulationState state,
            string recipeId, int maximumInstances = 1024, int maximumPooledInstances = 128, int maximumPendingCells = 16)
        {
            if (prefab == null || parent == null || state == null || string.IsNullOrWhiteSpace(recipeId)) throw new ArgumentException("Presentation owners are required.");
            if (maximumInstances < 1 || maximumPooledInstances < 0 || maximumPooledInstances > maximumInstances) throw new ArgumentException("Invalid presentation capacities.");
            if (maximumPendingCells < 1 || maximumPendingCells > 1024) throw new ArgumentException("Invalid request capacity.");
            // This first backend owns static art only. Gameplay scripts and rigidbodies need their own authoritative integration.
            if (prefab.GetComponentsInChildren<Rigidbody>(true).Length != 0)
                throw new ArgumentException("Use a static art prefab without gameplay callbacks or rigidbodies.");
            foreach (var script in prefab.GetComponentsInChildren<MonoBehaviour>(true))
                if (!(script is SculptedRockFusionParticipant participant) || participant.LiveFusion)
                    throw new ArgumentException("Only inert sculpted-rock authoring metadata is supported on this static backend.");
            if ((parent.lossyScale - Vector3.one).sqrMagnitude > .000001f)
                throw new ArgumentException("The presentation parent must have unit world scale so placement footprints remain valid.");
            this.prefab = prefab; this.parent = parent; this.state = state; this.recipeId = recipeId;
            capacity = maximumInstances; poolCapacity = maximumPooledInstances; pendingCapacity = maximumPendingCells;
            state.CandidateRemoved += RemoveCandidate;
            state.SnapshotApplied += ResetPresentation;
        }

        public bool Queue(Vector2Int cell, FixedTerrainPopulationPlanner.CellBuild build, bool collisionRelevant, bool visualRelevant = true)
        {
            if (disposed || build == null || build.Cell != cell || build.RecipeId != recipeId || !build.Complete || build.Cancelled
                || requests.ContainsKey(cell) || requests.Count >= pendingCapacity) return false;
            if (!build.MatchesState(state)) return false;
            int needed = 0;
            foreach (var placement in build.Placements) if (!state.IsRemoved(cell, recipeId, placement.Ordinal)) needed++;
            // Reserve every pending acquisition. Replacements need room for both versions until the successor is ready.
            // A caller can retry after releasing lower-priority cells; rejection never removes current coverage.
            if (needed > AvailablePreparationSlots) return false;
            if (!build.TryCommitBaseline(state)) return false;
            requests.Add(cell, new Request { Cell = cell, Plan = build.Placements.ToArray(), CollisionRelevant = collisionRelevant, VisualRelevant = visualRelevant });
            return true;
        }

        public bool IsActive(Vector2Int cell) => active.ContainsKey(cell);
        public bool IsPending(Vector2Int cell) => requests.ContainsKey(cell);
        public bool IsCollisionPublished(Vector2Int cell)
        {
            if (disposed || parent == null || !parent.gameObject.activeInHierarchy || requests.ContainsKey(cell)
                || !active.TryGetValue(cell, out var current)) return false;
            foreach (var item in current)
            {
                if (item.Object == null || !item.Object.activeInHierarchy) return false;
                for (int i = 0; i < item.Colliders.Length; i++)
                    if (item.CollisionRequired[i] && (item.Colliders[i] == null || !item.Colliders[i].enabled
                        || !item.Colliders[i].gameObject.activeInHierarchy)) return false;
            }
            return true;
        }
        private void RemoveCandidate(Vector2Int cell, string removedRecipe, int ordinal)
        {
            if (removedRecipe != recipeId || disposed) return;
            if (active.TryGetValue(cell, out var current)) RemoveFrom(current, ordinal);
            if (requests.TryGetValue(cell, out var request))
            {
                int index = request.Prepared.FindIndex(v => v.Ordinal == ordinal);
                if (index >= 0 && index < request.Activation) request.Activation--;
                RemoveFrom(request.Prepared, ordinal);
            }
        }
        private void RemoveFrom(List<Instance> items, int ordinal)
        {
            int index = items.FindIndex(v => v.Ordinal == ordinal);
            if (index < 0) return;
            var item = items[index]; items.RemoveAt(index); item.Object.SetActive(false); retiring.Enqueue(item); CollisionRevision++;
        }
        private void ResetPresentation()
        {
            foreach (var cell in requests.Keys.Concat(active.Keys).Distinct().ToArray()) Release(cell);
        }
        public void Release(Vector2Int cell)
        {
            if (requests.ContainsKey(cell) || active.ContainsKey(cell)) CollisionRevision++;
            if (requests.TryGetValue(cell, out var request))
            {
                foreach (var item in request.Prepared) { item.Object.SetActive(false); retiring.Enqueue(item); }
                requests.Remove(cell);
            }
            if (active.TryGetValue(cell, out var current))
            {
                foreach (var item in current) { item.Object.SetActive(false); retiring.Enqueue(item); }
                active.Remove(cell);
            }
            state.ReleaseBaselineCache(cell);
        }
        public void SetCollisionRelevant(Vector2Int cell, bool relevant)
        {
            if (requests.TryGetValue(cell, out var request))
            {
                request.CollisionRelevant = relevant;
                foreach (var item in request.Prepared) if (item.Object.activeSelf) Collision(item, relevant);
            }
            if (active.TryGetValue(cell, out var current)) foreach (var item in current) Collision(item, relevant);
        }
        public void SetVisualRelevant(Vector2Int cell, bool relevant)
        {
            if (requests.TryGetValue(cell, out var request))
            {
                request.VisualRelevant = relevant;
                foreach (var item in request.Prepared) if (item.Object.activeSelf) Visual(item, relevant);
            }
            if (active.TryGetValue(cell, out var current)) foreach (var item in current) Visual(item, relevant);
        }

        /// <summary>One prefab acquisition/configuration or one retirement per work unit. Capacity exhaustion retains old coverage.</summary>
        public int Advance(int maximumOperations, double milliseconds)
        {
            if (disposed) throw new ObjectDisposedException(nameof(FixedTerrainPopulationPresentation));
            if (maximumOperations < 1 || double.IsNaN(milliseconds) || double.IsInfinity(milliseconds) || milliseconds <= 0)
                throw new ArgumentException("A finite positive presentation budget is required.");
            int count = 0; clock.Restart();
            while (count < maximumOperations && clock.Elapsed.TotalMilliseconds < milliseconds)
            {
                if (retiring.Count > 0) { Retire(retiring.Dequeue()); count++; continue; }
                Request request = null;
                foreach (var pair in requests)
                {
                    if (request == null || pair.Value.CollisionRelevant && !request.CollisionRelevant) request = pair.Value;
                    if (request.CollisionRelevant) break;
                }
                if (request == null) break;
                if (request.Next >= request.Plan.Length)
                {
                    active.TryGetValue(request.Cell, out var previous);
                    // One successor is enabled and its matching outgoing collider retired in the same work unit.
                    if (request.Activation < request.Prepared.Count)
                    {
                        var item = request.Prepared[request.Activation];
                        if (state.IsRemoved(request.Cell, recipeId, item.Ordinal))
                        { request.Prepared.RemoveAt(request.Activation); item.Object.SetActive(false); retiring.Enqueue(item); count++; continue; }
                        Collision(item, request.CollisionRelevant); Visual(item, request.VisualRelevant); item.Object.SetActive(true); request.Activation++; CollisionRevision++;
                        var old = previous?.Find(v => v.StableId == item.StableId);
                        if (old != null) { previous.Remove(old); old.Object.SetActive(false); retiring.Enqueue(old); }
                        count++; continue;
                    }
                    if (previous != null && previous.Count > 0)
                    { var old = previous[previous.Count - 1]; previous.RemoveAt(previous.Count - 1); old.Object.SetActive(false); retiring.Enqueue(old); count++; continue; }
                    active[request.Cell] = request.Prepared; requests.Remove(request.Cell); count++; continue;
                }
                var placement = request.Plan[request.Next];
                if (placement.Cell != request.Cell) { Release(request.Cell); count++; continue; }
                if (state.IsRemoved(request.Cell, recipeId, placement.Ordinal)) { request.Next++; count++; continue; }
                if (pool.Count == 0 && instances >= capacity) break;
                var instance = Acquire();
                instance.StableId = placement.StableId; instance.Ordinal = placement.Ordinal; instance.Object.name = placement.StableId;
                instance.Object.transform.SetPositionAndRotation(placement.Position, Quaternion.Euler(0, placement.Yaw, 0));
                instance.Object.transform.localScale = Vector3.Scale(prefab.transform.localScale, Vector3.one * placement.Scale);
                Collision(instance, false); request.Prepared.Add(instance); request.Next++; count++;
            }
            return count;
        }

        private Instance Acquire()
        {
            if (pool.Count > 0) return pool.Pop();
            var owner = UnityEngine.Object.Instantiate(prefab, parent, false); owner.SetActive(false);
            foreach (var participant in owner.GetComponentsInChildren<SculptedRockFusionParticipant>(true)) participant.enabled = false;
            var colliders = owner.GetComponentsInChildren<Collider>(true);
            var original = new bool[colliders.Length]; for (int i = 0; i < colliders.Length; i++) original[i] = colliders[i].enabled;
            var required = new bool[colliders.Length];
            for (int i = 0; i < colliders.Length; i++) required[i] = original[i] && ActiveBelowRoot(colliders[i].transform, owner.transform);
            var renderers = owner.GetComponentsInChildren<Renderer>(true);
            var rendering = new bool[renderers.Length]; for (int i = 0; i < renderers.Length; i++) rendering[i] = renderers[i].enabled;
            var lods = owner.GetComponentsInChildren<LODGroup>(true);
            var lodEnabled = new bool[lods.Length]; for (int i = 0; i < lods.Length; i++) lodEnabled[i] = lods[i].enabled;
            instances++; return new Instance { Object = owner, Colliders = colliders, CollisionEnabled = original, CollisionRequired = required,
                Renderers = renderers, RenderingEnabled = rendering, Lods = lods, LodEnabled = lodEnabled };
        }
        private static bool ActiveBelowRoot(Transform child, Transform root)
        {
            for (var node = child; node != root; node = node.parent)
                if (node == null || !node.gameObject.activeSelf) return false;
            return true;
        }
        private void Collision(Instance item, bool enabled)
        {
            for (int i = 0; i < item.Colliders.Length; i++)
            {
                bool wanted = enabled && item.CollisionEnabled[i];
                if (item.Colliders[i].enabled == wanted) continue;
                item.Colliders[i].enabled = wanted; CollisionRevision++;
            }
        }
        private static void Visual(Instance item, bool enabled)
        {
            for (int i = 0; i < item.Lods.Length; i++) item.Lods[i].enabled = enabled && item.LodEnabled[i];
            for (int i = 0; i < item.Renderers.Length; i++) item.Renderers[i].enabled = enabled && item.RenderingEnabled[i];
        }
        private void Retire(Instance item)
        {
            if (pool.Count < poolCapacity) { item.Object.SetActive(false); Collision(item, false); pool.Push(item); }
            else { Destroy(item.Object); instances--; }
        }
        public void Dispose()
        {
            if (disposed) return;
            state.CandidateRemoved -= RemoveCandidate;
            state.SnapshotApplied -= ResetPresentation;
            foreach (var pair in requests) foreach (var item in pair.Value.Prepared) Destroy(item.Object);
            foreach (var pair in active) foreach (var item in pair.Value) Destroy(item.Object);
            while (pool.Count > 0) Destroy(pool.Pop().Object);
            while (retiring.Count > 0) Destroy(retiring.Dequeue().Object);
            requests.Clear(); active.Clear(); instances = 0; disposed = true;
        }
        private static void Destroy(GameObject item)
        { if (item == null) return; if (Application.isPlaying) UnityEngine.Object.Destroy(item); else UnityEngine.Object.DestroyImmediate(item); }
    }
}
