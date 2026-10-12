using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [Serializable] public sealed class FixedTerrainPopulationRecipeVersion
    {
        public string recipeId;
        public int version, candidateCount;
    }
    [Serializable] public sealed class FixedTerrainPopulationCellRecord
    {
        public int x, z;
        public string terrainRevision, constraintRevision, acceptedDigest;
        public List<FixedTerrainPopulationCandidateAddress> acceptedCandidates = new();
    }
    [Serializable] public sealed class FixedTerrainPopulationCandidateAddress
    {
        public int x, z, ordinal;
        public string recipeId;
    }
    [Serializable] public sealed class FixedTerrainPopulationSnapshot
    {
        public int version = 1, seed, generationVersion, cellSize = FixedTerrainPopulationIdentity.CellSize;
        public string worldId, catalogRevision;
        public List<FixedTerrainPopulationRecipeVersion> recipes = new();
        public List<FixedTerrainPopulationCellRecord> cells = new();
        public List<FixedTerrainPopulationCandidateAddress> removed = new();
    }

    /// <summary>Logical population receipts and sparse mutations. Owns no Unity objects or presentation lifetime.</summary>
    public sealed class FixedTerrainPopulationState
    {
        private readonly FixedTerrainPopulationSnapshot header;
        private readonly Dictionary<string, FixedTerrainPopulationRecipeVersion> recipes;
        private readonly Dictionary<string, FixedTerrainPopulationIdentity> identities;
        private readonly Func<Vector2Int, string> terrainRevision, constraintRevision;
        private Dictionary<Vector2Int, FixedTerrainPopulationCellRecord> cells = new();
        private Dictionary<string, FixedTerrainPopulationCandidateAddress> removed = new(StringComparer.Ordinal);
        private Dictionary<Vector2Int, HashSet<string>> removalsByCell = new();
        private readonly Dictionary<Vector2Int, HashSet<string>> accepted = new();
        public long Revision { get; private set; }
        public string WorldId => header.worldId;
        public int CellCount => cells.Count;
        public int RemovedCount => removed.Count;
        // Main-thread integration notifications; logical state is committed before listeners observe it.
        internal event Action<Vector2Int, string, int> CandidateRemoved;
        internal event Action SnapshotApplied;
        public bool MatchesRecipe(FixedTerrainPopulationIdentity identity, int candidateCount) => identity != null
            && identity.WorldId == header.worldId && identity.Seed == header.seed && identity.GenerationVersion == header.generationVersion
            && recipes.TryGetValue(identity.RecipeId, out var recipe) && recipe.version == identity.RecipeVersion && recipe.candidateCount == candidateCount;

        public FixedTerrainPopulationState(string worldId, int seed, int generationVersion, string catalogRevision,
            IEnumerable<FixedTerrainPopulationRecipeVersion> recipeVersions,
            Func<Vector2Int, string> terrainRevision, Func<Vector2Int, string> constraintRevision)
        {
            if (string.IsNullOrWhiteSpace(worldId) || string.IsNullOrWhiteSpace(catalogRevision) || generationVersion < 1)
                throw new ArgumentException("Explicit population world, catalog and generation metadata are required.");
            this.terrainRevision = terrainRevision ?? throw new ArgumentNullException(nameof(terrainRevision));
            this.constraintRevision = constraintRevision ?? throw new ArgumentNullException(nameof(constraintRevision));
            recipes = new Dictionary<string, FixedTerrainPopulationRecipeVersion>(StringComparer.Ordinal);
            identities = new Dictionary<string, FixedTerrainPopulationIdentity>(StringComparer.Ordinal);
            if (recipeVersions == null) throw new ArgumentNullException(nameof(recipeVersions));
            foreach (var value in recipeVersions)
            {
                if (value == null || string.IsNullOrWhiteSpace(value.recipeId) || value.version < 1 || value.candidateCount < 1
                    || recipes.ContainsKey(value.recipeId)) throw new ArgumentException("Invalid or duplicate population recipe.");
                var copy = new FixedTerrainPopulationRecipeVersion { recipeId = value.recipeId, version = value.version, candidateCount = value.candidateCount };
                recipes.Add(copy.recipeId, copy);
                identities.Add(copy.recipeId, new FixedTerrainPopulationIdentity(worldId, seed, generationVersion, copy.recipeId, copy.version));
            }
            if (recipes.Count == 0) throw new ArgumentException("A population recipe is required.");
            header = new FixedTerrainPopulationSnapshot { worldId = worldId, seed = seed, generationVersion = generationVersion,
                catalogRevision = catalogRevision, recipes = recipes.Values.OrderBy(v => v.recipeId, StringComparer.Ordinal).ToList() };
        }

        public string StableId(Vector2Int cell, string recipeId, int ordinal)
        {
            ValidateCandidateAddress(recipeId, ordinal);
            return identities[recipeId].CandidateId(cell, ordinal);
        }
        private void ValidateCandidateAddress(string recipeId, int ordinal)
        {
            if (recipeId == null || !recipes.TryGetValue(recipeId, out var recipe) || ordinal < 0 || ordinal >= recipe.candidateCount)
                throw new ArgumentException("Unknown population recipe/candidate address.");
        }

        // Called with regenerated accepted candidates, before adopting any renderer/collider.
        // A receipt freezes acceptance without storing mesh geometry or every placement transform.
        public bool RegisterBaseline(Vector2Int cell, IEnumerable<FixedTerrainPopulationCandidateAddress> candidates)
        {
            if (candidates == null) return false;
            var terrain = terrainRevision(cell); var constraints = constraintRevision(cell);
            if (string.IsNullOrWhiteSpace(terrain) || string.IsNullOrWhiteSpace(constraints)) return false;
            var keys = new HashSet<string>(StringComparer.Ordinal);
            var addresses = new List<FixedTerrainPopulationCandidateAddress>();
            foreach (var candidate in candidates)
            {
                if (!ValidAddress(candidate) || candidate.x != cell.x || candidate.z != cell.y) return false;
                if (!keys.Add(StableId(cell, candidate.recipeId, candidate.ordinal))) return false;
                addresses.Add(Copy(candidate));
            }
            return CommitBaseline(cell, terrain, constraints, keys, addresses);
        }

        // Only a complete immutable planner result can supply precomputed identities.
        // Public address registration and untrusted snapshot restoration still hash independently.
        internal bool RegisterPlannedBaseline(FixedTerrainPopulationPlanner.CellBuild build)
        {
            if (build == null || !build.Complete || build.Cancelled || !build.MatchesState(this)) return false;
            var cell = build.Cell;
            var terrain = terrainRevision(cell); var constraints = constraintRevision(cell);
            if (string.IsNullOrWhiteSpace(terrain) || string.IsNullOrWhiteSpace(constraints)) return false;
            var keys = new HashSet<string>(StringComparer.Ordinal);
            var addresses = new List<FixedTerrainPopulationCandidateAddress>(build.Placements.Count);
            foreach (var placement in build.Placements)
            {
                var address = placement.Address(build.RecipeId);
                if (placement.Cell != cell || !ValidAddress(address) || string.IsNullOrEmpty(placement.StableId)
                    || !keys.Add(placement.StableId)) return false;
                addresses.Add(address);
            }
            return CommitBaseline(cell, terrain, constraints, keys, addresses);
        }

        private bool CommitBaseline(Vector2Int cell, string terrain, string constraints,
            HashSet<string> keys, List<FixedTerrainPopulationCandidateAddress> addresses)
        {
            var record = new FixedTerrainPopulationCellRecord { x = cell.x, z = cell.y,
                terrainRevision = terrain, constraintRevision = constraints, acceptedDigest = Digest(keys),
                acceptedCandidates = addresses.OrderBy(v => v.recipeId, StringComparer.Ordinal).ThenBy(v => v.ordinal).ToList() };
            if (cells.TryGetValue(cell, out var old) && !SameCell(old, record)) return false;
            if (removalsByCell.TryGetValue(cell, out var cellRemovals) && !cellRemovals.IsSubsetOf(keys)) return false;
            if (!cells.ContainsKey(cell)) { cells.Add(cell, record); Revision++; }
            accepted[cell] = keys;
            return true;
        }

        public void ReleaseBaselineCache(Vector2Int cell) => accepted.Remove(cell);
        public bool TryRemove(Vector2Int cell, string recipeId, int ordinal)
        {
            var reference = new FixedTerrainPopulationCandidateAddress { x = cell.x, z = cell.y, recipeId = recipeId, ordinal = ordinal };
            if (!ValidAddress(reference)) return false;
            if (!accepted.TryGetValue(cell, out var keys))
            {
                if (!cells.TryGetValue(cell, out var receipt)) return false;
                keys = new HashSet<string>(receipt.acceptedCandidates.Select(v => StableId(cell, v.recipeId, v.ordinal)), StringComparer.Ordinal);
                accepted[cell] = keys;
            }
            var id = StableId(cell, recipeId, ordinal);
            if (!keys.Contains(id) || removed.ContainsKey(id)) return false;
            removed.Add(id, reference);
            if (!removalsByCell.TryGetValue(cell, out var cellRemovals)) removalsByCell.Add(cell, cellRemovals = new HashSet<string>(StringComparer.Ordinal));
            cellRemovals.Add(id); Revision++; CandidateRemoved?.Invoke(cell, recipeId, ordinal); return true;
        }
        public bool IsRemoved(Vector2Int cell, string recipeId, int ordinal)
        {
            // Sparse deltas usually leave a cell untouched. Validate even on that fast path,
            // but avoid SHA/string allocation unless this cell actually has removals.
            ValidateCandidateAddress(recipeId, ordinal);
            return removalsByCell.TryGetValue(cell, out var keys) && keys.Contains(identities[recipeId].CandidateId(cell, ordinal));
        }

        public FixedTerrainPopulationSnapshot CaptureSnapshot()
        {
            return new FixedTerrainPopulationSnapshot { worldId = header.worldId, seed = header.seed,
                generationVersion = header.generationVersion, catalogRevision = header.catalogRevision,
                recipes = header.recipes.Select(v => new FixedTerrainPopulationRecipeVersion { recipeId = v.recipeId, version = v.version, candidateCount = v.candidateCount }).ToList(),
                cells = cells.OrderBy(p => p.Key.x).ThenBy(p => p.Key.y).Select(p => Copy(p.Value)).ToList(),
                removed = removed.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => Copy(p.Value)).ToList() };
        }
        public bool CanApplySnapshot(FixedTerrainPopulationSnapshot snapshot) => TryBuild(snapshot, out _, out _);
        public bool ApplySnapshot(FixedTerrainPopulationSnapshot snapshot)
        {
            if (!TryBuild(snapshot, out var nextCells, out var nextRemoved)) return false;
            var nextIndex = new Dictionary<Vector2Int, HashSet<string>>();
            foreach (var pair in nextRemoved)
            {
                var cell = new Vector2Int(pair.Value.x, pair.Value.z);
                if (!nextIndex.TryGetValue(cell, out var keys)) nextIndex.Add(cell, keys = new HashSet<string>(StringComparer.Ordinal));
                keys.Add(pair.Key);
            }
            cells = nextCells; removed = nextRemoved; removalsByCell = nextIndex; accepted.Clear(); Revision++;
            SnapshotApplied?.Invoke(); return true;
        }

        private bool TryBuild(FixedTerrainPopulationSnapshot snapshot,
            out Dictionary<Vector2Int, FixedTerrainPopulationCellRecord> nextCells,
            out Dictionary<string, FixedTerrainPopulationCandidateAddress> nextRemoved)
        {
            nextCells = null; nextRemoved = null;
            if (snapshot == null || snapshot.version != 1 || snapshot.cellSize != FixedTerrainPopulationIdentity.CellSize
                || snapshot.worldId != header.worldId || snapshot.seed != header.seed || snapshot.generationVersion != header.generationVersion
                || snapshot.catalogRevision != header.catalogRevision || snapshot.recipes == null || snapshot.recipes.Count != recipes.Count
                || snapshot.cells == null || snapshot.removed == null) return false;
            var seenRecipes = new HashSet<string>(StringComparer.Ordinal);
            foreach (var value in snapshot.recipes)
                if (value == null || value.recipeId == null || !seenRecipes.Add(value.recipeId) || !recipes.TryGetValue(value.recipeId, out var expected)
                    || value.version != expected.version || value.candidateCount != expected.candidateCount) return false;
            nextCells = new Dictionary<Vector2Int, FixedTerrainPopulationCellRecord>();
            var membership = new Dictionary<Vector2Int, HashSet<string>>();
            foreach (var value in snapshot.cells)
            {
                if (value == null || !ValidDigest(value.acceptedDigest)) return false;
                var cell = new Vector2Int(value.x, value.z);
                if (value.acceptedCandidates == null) return false;
                var cellKeys = new HashSet<string>(StringComparer.Ordinal);
                foreach (var candidate in value.acceptedCandidates)
                    if (!ValidAddress(candidate) || candidate.x != cell.x || candidate.z != cell.y
                        || !cellKeys.Add(StableId(cell, candidate.recipeId, candidate.ordinal))) return false;
                if (value.acceptedDigest != Digest(cellKeys)) return false;
                if (string.IsNullOrWhiteSpace(value.terrainRevision) || string.IsNullOrWhiteSpace(value.constraintRevision)
                    || value.terrainRevision != terrainRevision(cell) || value.constraintRevision != constraintRevision(cell)
                    || (cells.TryGetValue(cell, out var current) && !SameCell(current, value)) || !nextCells.TryAdd(cell, Copy(value))) return false;
                membership.Add(cell, cellKeys);
            }
            nextRemoved = new Dictionary<string, FixedTerrainPopulationCandidateAddress>(StringComparer.Ordinal);
            foreach (var value in snapshot.removed)
            {
                if (!ValidAddress(value)) return false;
                var cell = new Vector2Int(value.x, value.z);
                if (!nextCells.ContainsKey(cell)) return false;
                var id = StableId(cell, value.recipeId, value.ordinal);
                if (!membership[cell].Contains(id) || !nextRemoved.TryAdd(id, Copy(value))) return false;
            }
            return true;
        }
        private bool ValidAddress(FixedTerrainPopulationCandidateAddress value) => value != null && value.recipeId != null
            && recipes.TryGetValue(value.recipeId, out var recipe) && value.ordinal >= 0 && value.ordinal < recipe.candidateCount;
        private static bool SameCell(FixedTerrainPopulationCellRecord a, FixedTerrainPopulationCellRecord b) => a.terrainRevision == b.terrainRevision
            && a.constraintRevision == b.constraintRevision && a.acceptedDigest == b.acceptedDigest;
        private static FixedTerrainPopulationCellRecord Copy(FixedTerrainPopulationCellRecord v) => new() { x = v.x, z = v.z,
            terrainRevision = v.terrainRevision, constraintRevision = v.constraintRevision, acceptedDigest = v.acceptedDigest,
            acceptedCandidates = v.acceptedCandidates.Select(Copy).ToList() };
        private static FixedTerrainPopulationCandidateAddress Copy(FixedTerrainPopulationCandidateAddress v) => new() { x = v.x, z = v.z, recipeId = v.recipeId, ordinal = v.ordinal };
        private static string Digest(HashSet<string> keys)
        {
            using var sha = SHA256.Create();
            return FixedTerrainPopulationIdentity.EncodeHex(sha.ComputeHash(
                Encoding.UTF8.GetBytes(string.Join("\n", keys.OrderBy(v => v, StringComparer.Ordinal)))));
        }
        private static bool ValidDigest(string value) => value != null && value.Length == 64 && value.All(c => c >= '0' && c <= '9' || c >= 'a' && c <= 'f');
    }
}
