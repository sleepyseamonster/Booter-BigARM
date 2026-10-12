using System;
using System.Linq;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class FixedTerrainPopulationStateTests
    {
        [Test]
        public void RemovalSurvivesCacheReleaseRecreationSerializationAndTerrainExpansion()
        {
            var cell = new Vector2Int(-1, 0); var state = Create(); var candidates = Candidates(cell);
            Assert.That(state.RegisterBaseline(cell, candidates), Is.True);
            Assert.That(state.TryRemove(cell, "fixture.clutter", 2), Is.True);
            Assert.That(state.TryRemove(cell, "fixture.clutter", 2), Is.False);
            var beforeRelease = state.Revision; state.ReleaseBaselineCache(cell);
            Assert.That(state.Revision, Is.EqualTo(beforeRelease));
            Assert.That(state.IsRemoved(cell, "fixture.clutter", 2), Is.True);
            Assert.That(state.RegisterBaseline(cell, candidates.Reverse()), Is.True);
            var snapshot = JsonUtility.FromJson<FixedTerrainPopulationSnapshot>(JsonUtility.ToJson(state.CaptureSnapshot()));
            var recreated = Create(); Assert.That(recreated.ApplySnapshot(snapshot), Is.True);
            Assert.That(recreated.RegisterBaseline(cell, candidates), Is.True);
            Assert.That(recreated.IsRemoved(cell, "fixture.clutter", 2), Is.True);
            Assert.That(recreated.RegisterBaseline(new Vector2Int(1, 0), Candidates(new Vector2Int(1, 0))), Is.True);
            Assert.That(recreated.StableId(cell, "fixture.clutter", 2), Is.EqualTo(state.StableId(cell, "fixture.clutter", 2)));
            Assert.That(recreated.RegisterBaseline(cell, candidates.Take(2)), Is.False, "New border thinning cannot erase accepted saved features.");
        }

        [TestCase("world")][TestCase("seed")][TestCase("generation")][TestCase("catalog")][TestCase("recipe")][TestCase("terrain")]
        [TestCase("constraints")][TestCase("duplicate-cell")][TestCase("duplicate-removal")][TestCase("ordinal")][TestCase("unknown-cell")]
        public void InvalidMetadataOrDeltaRejectsAtomically(string problem)
        {
            var state = Create(); var cell = new Vector2Int(-1, 0); state.RegisterBaseline(cell, Candidates(cell));
            state.TryRemove(cell, "fixture.clutter", 2);
            var before = JsonUtility.ToJson(state.CaptureSnapshot()); var snapshot = state.CaptureSnapshot();
            if (problem == "world") snapshot.worldId = "other-world";
            if (problem == "seed") snapshot.seed++;
            if (problem == "generation") snapshot.generationVersion++;
            if (problem == "catalog") snapshot.catalogRevision = "changed";
            if (problem == "recipe") snapshot.recipes[0].version++;
            if (problem == "terrain") snapshot.cells[0].terrainRevision = "changed";
            if (problem == "constraints") snapshot.cells[0].constraintRevision = "changed";
            if (problem == "duplicate-cell") snapshot.cells.Add(snapshot.cells[0]);
            if (problem == "duplicate-removal") snapshot.removed.Add(snapshot.removed[0]);
            if (problem == "ordinal") snapshot.removed[0].ordinal = 64;
            if (problem == "unknown-cell") snapshot.removed[0].x = 9;
            Assert.That(state.ApplySnapshot(snapshot), Is.False);
            Assert.That(JsonUtility.ToJson(state.CaptureSnapshot()), Is.EqualTo(before));
        }

        [Test]
        public void SnapshotsAreDetachedSortedAndUnacceptedCandidatesCannotMutate()
        {
            var state = Create(); var cell = new Vector2Int(-1, 0);
            Assert.That(state.TryRemove(cell, "fixture.clutter", 0), Is.False);
            state.RegisterBaseline(cell, Candidates(cell).Take(2));
            Assert.That(state.TryRemove(cell, "fixture.clutter", 2), Is.False);
            Assert.That(state.TryRemove(cell, "fixture.clutter", 0), Is.True);
            var snapshot = state.CaptureSnapshot(); snapshot.recipes[0].version = 999; snapshot.cells[0].terrainRevision = "changed";
            snapshot.removed.Clear();
            Assert.That(state.IsRemoved(cell, "fixture.clutter", 0), Is.True);
            Assert.That(state.CaptureSnapshot().recipes[0].version, Is.EqualTo(1));
            Assert.That(state.RegisterBaseline(cell, Candidates(cell).Take(2).Reverse()), Is.True);
        }
        [Test]
        public void FreshRestoreRejectsUnacceptedRemovalAndForgedAcceptanceDigest()
        {
            var original = Create(); var cell = new Vector2Int(-1, 0);
            original.RegisterBaseline(cell, Candidates(cell).Take(2)); original.TryRemove(cell, "fixture.clutter", 0);
            var snapshot = original.CaptureSnapshot(); snapshot.removed[0].ordinal = 2;
            Assert.That(Create().ApplySnapshot(snapshot), Is.False, "Recipe-valid but unaccepted candidates cannot become saved mutations.");
            snapshot = original.CaptureSnapshot(); snapshot.cells[0].acceptedDigest = new string('0', 64);
            Assert.That(Create().ApplySnapshot(snapshot), Is.False);
        }
        public static FixedTerrainPopulationState Create() => new(GreaterWastelandInventorySnapshot.WorldIdentity, 17, 1, "fixture.catalog.v1",
            new[] { new FixedTerrainPopulationRecipeVersion { recipeId = "fixture.clutter", version = 1, candidateCount = 64 } },
            cell => cell.x >= -1 && cell.x <= 1 && cell.y == 0 ? "terrain.cell.v1" : null,
            cell => cell.x >= -1 && cell.x <= 1 && cell.y == 0 ? "constraints.cell.v1" : null);
        public static FixedTerrainPopulationCandidateAddress[] Candidates(Vector2Int cell) => Enumerable.Range(0, 4)
            .Select(i => new FixedTerrainPopulationCandidateAddress { x = cell.x, z = cell.y, recipeId = "fixture.clutter", ordinal = i }).ToArray();
    }
}
