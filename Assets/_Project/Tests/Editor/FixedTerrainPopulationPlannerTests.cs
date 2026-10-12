using System;
using System.Collections.Generic;
using System.Linq;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Tests
{
    public sealed class FixedTerrainPopulationPlannerTests
    {
        [TestCase(12f, -6, -6)]
        [TestCase(32f, -1, 0)]
        [TestCase(32.01f, 0, -1)]
        [TestCase(64f, 3, 4)]
        [TestCase(128f, -4, 3)]
        [TestCase(12f, 4095, -4095)]
        [TestCase(12f, 4096, -4096)]
        public void ReducedNeighbourSearchMatchesOriginalWiderSearch(float spacing, int x, int z)
        {
            var identity = Identity(); var cell = new Vector2Int(x, z);
            var planner = new FixedTerrainPopulationPlanner(identity, Flat, null, spacing, .3f, 38, 1, 1);
            var job = Finish(planner.Begin(cell), 1);
            var expected = new List<int>();
            var candidates = new Dictionary<(long, long), (Vector2 point, double priority)>();
            (Vector2 point, double priority) Candidate(long gx, long gz)
            {
                if (candidates.TryGetValue((gx, gz), out var cached)) return cached;
                long cx = (long)Math.Floor(gx / 8d), cz = (long)Math.Floor(gz / 8d);
                int ordinal = (int)(gz - cz * 8) * 8 + (int)(gx - cx * 8);
                var address = new Vector2Int((int)cx, (int)cz);
                var value = (identity.CandidatePosition(address, ordinal), identity.Sample01(address, ordinal, 3));
                candidates.Add((gx, gz), value); return value;
            }
            int originalHalo = Mathf.CeilToInt(spacing / 32) + 1;
            for (int ordinal = 0; ordinal < 64; ordinal++)
            {
                long gx = (long)x * 8 + ordinal % 8, gz = (long)z * 8 + ordinal / 8;
                var current = Candidate(gx, gz); bool accepted = true;
                for (long nz = gz - originalHalo; nz <= gz + originalHalo; nz++)
                    for (long nx = gx - originalHalo; nx <= gx + originalHalo; nx++)
                    {
                        if (nx == gx && nz == gz) continue;
                        var other = Candidate(nx, nz);
                        if ((other.point - current.point).sqrMagnitude < spacing * spacing
                            && (other.priority < current.priority || other.priority == current.priority && (nx < gx || nx == gx && nz < gz))) accepted = false;
                    }
                if (accepted) expected.Add(ordinal);
            }
            Assert.That(job.Placements.Select(p => p.Ordinal), Is.EqualTo(expected));
            foreach (var placement in job.Placements)
            {
                Assert.That(placement.StableId, Is.EqualTo(identity.CandidateId(cell, placement.Ordinal)));
                Assert.That(new Vector2(placement.Position.x, placement.Position.z), Is.EqualTo(identity.CandidatePosition(cell, placement.Ordinal)));
                Assert.That(placement.Yaw, Is.EqualTo((float)identity.Sample01(cell, placement.Ordinal, 4) * 360));
                Assert.That(placement.Scale, Is.EqualTo(.85f + (float)identity.Sample01(cell, placement.Ordinal, 6) * .3f));
            }
            if (spacing == 12 && Math.Abs(x) <= 4095 && Math.Abs(z) <= 4095)
            {
                Assert.That(job.CachedCandidates, Is.LessThanOrEqualTo(100));
                Assert.That(job.Operations, Is.LessThan(2000), "Redundant neighbour work must actually decrease.");
            }
        }

        [Test]
        public void OrderWorkBudgetsProtectedRoutesAndCrossCellSpacingAgree()
        {
            var masks = new[] { new FixedTerrainPopulationExclusion(Vector2.zero, Vector2.zero, 64),
                new FixedTerrainPopulationExclusion(new Vector2(-512, 100), new Vector2(512, 100), 8) };
            var planner = Planner(Flat, masks);
            var cells = new List<Vector2Int>();
            for (int z = -2; z < 2; z++) for (int x = -2; x < 2; x++) cells.Add(new Vector2Int(x, z));
            var forward = Generate(planner, cells, 97); var reverse = Generate(planner, cells.AsEnumerable().Reverse(), 1);
            Assert.That(Signature(reverse), Is.EqualTo(Signature(forward)));
            Assert.That(forward.Count, Is.GreaterThan(100));
            Assert.That(forward.Select(p => p.StableId).Distinct().Count(), Is.EqualTo(forward.Count));
            foreach (var p in forward)
            {
                var point = new Vector2(p.Position.x, p.Position.z);
                Assert.That(FixedTerrainPopulationIdentity.CellAt(point), Is.EqualTo(p.Cell));
                foreach (var mask in masks) Assert.That(mask.Intersects(point, 2), Is.False);
            }
            for (int i = 0; i < forward.Count; i++) for (int j = i + 1; j < forward.Count; j++)
                Assert.That((forward[i].Position - forward[j].Position).sqrMagnitude, Is.GreaterThanOrEqualTo(144 - .001f));
        }

        [Test]
        public void FootprintHolesSlopeReliefCancellationAndIdentityMismatchFailClosed()
        {
            var identity = Identity(); var cell = Vector2Int.zero; var target = identity.CandidatePosition(cell, 0);
            bool RingHole(Vector2 point, out FixedTerrainPopulationPlanner.Surface sample)
            { Flat(point, out sample); return !(point.x > target.x + .2f && Mathf.Abs(point.y - target.y) < .1f && point.x < target.x + .4f); }
            var hole = new FixedTerrainPopulationPlanner(identity, RingHole, null, 1, .3f, 38, 1, 1);
            var job = Finish(hole.Begin(cell), 3);
            Assert.That(job.Placements.Any(p => p.Ordinal == 0), Is.False, "A solid center cannot bridge a footprint hole.");
            bool Steep(Vector2 point, out FixedTerrainPopulationPlanner.Surface sample)
            { sample = new FixedTerrainPopulationPlanner.Surface(new Vector3(point.x, 0, point.y), new Vector3(1, .1f, 0)); return true; }
            Assert.That(Finish(Planner(Steep).Begin(cell), 97).Placements.Count, Is.Zero);
            bool Uneven(Vector2 point, out FixedTerrainPopulationPlanner.Surface sample)
            { sample = new FixedTerrainPopulationPlanner.Surface(new Vector3(point.x, point.x, point.y), Vector3.up); return true; }
            Assert.That(Finish(Planner(Uneven).Begin(cell), 97).Placements.Count, Is.Zero);
            var cancelled = hole.Begin(cell); Assert.That(cancelled.Advance(1, 100), Is.EqualTo(1)); cancelled.Cancel();
            Assert.That(cancelled.Advance(100, 100), Is.Zero); Assert.That(cancelled.Placements, Is.Empty);
            var incompatible = FixedTerrainPopulationStateTests.Create();
            Assert.That(job.TryCommitBaseline(incompatible), Is.True);
            var other = new FixedTerrainPopulationPlanner(new FixedTerrainPopulationIdentity(identity.WorldId, 99, 1, identity.RecipeId, 1), Flat, null, 12, 2, 38, 1, 1);
            Assert.That(Finish(other.Begin(cell), 97).TryCommitBaseline(incompatible), Is.False);
        }

        [Test]
        public void BoundedNativeSectorGroundsOnResidentCollisionWithRenderingDisabled()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var data = new TerrainData { heightmapResolution = 33, size = new Vector3(256, 100, 256) };
            try
            {
                var heights = new float[33, 33];
                for (int z = 0; z < 33; z++) for (int x = 0; x < 33; x++) heights[z, x] = .5f;
                data.SetHeights(0, 0, heights); var before = data.GetHeights(0, 0, 33, 33);
                var terrains = new List<Terrain>();
                for (int z = -2; z < 2; z++) for (int x = -2; x < 2; x++)
                {
                    var t = Terrain.CreateTerrainGameObject(data).GetComponent<Terrain>();
                    SceneManager.MoveGameObjectToScene(t.gameObject, scene); t.transform.position = new Vector3(x * 256, -25, z * 256);
                    t.enabled = false; terrains.Add(t);
                }
                Physics.SyncTransforms(); var index = new FixedTerrainSurfaceIndex(terrains, "fixture.native.v1");
                var planner = Planner(FixedTerrainPopulationPlanner.NativeSurface(index));
                var result = Generate(planner, terrains.Select(t => FixedTerrainPopulationIdentity.CellAt(new Vector2(t.transform.position.x, t.transform.position.z))), 13);
                Assert.That(result.Count, Is.GreaterThan(100));
                foreach (var p in result) Assert.That(p.Position.y, Is.EqualTo(25).Within(.01));
                Assert.That(terrains.All(t => !t.enabled && t.GetComponent<TerrainCollider>().enabled), Is.True);
                Assert.That(data.GetHeights(0, 0, 33, 33), Is.EqualTo(before));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); UnityEngine.Object.DestroyImmediate(data); }
        }

        private static FixedTerrainPopulationIdentity Identity() => new(GreaterWastelandInventorySnapshot.WorldIdentity, 17, 1, "fixture.clutter", 1);
        private static FixedTerrainPopulationPlanner Planner(FixedTerrainPopulationPlanner.SurfaceQuery query, IEnumerable<FixedTerrainPopulationExclusion> exclusions = null)
            => new(Identity(), query, exclusions, 12, 2, 38, 1, 1);
        private static bool Flat(Vector2 p, out FixedTerrainPopulationPlanner.Surface sample)
        { sample = new FixedTerrainPopulationPlanner.Surface(new Vector3(p.x, 0, p.y), Vector3.up); return true; }
        private static FixedTerrainPopulationPlanner.CellBuild Finish(FixedTerrainPopulationPlanner.CellBuild job, int operations)
        {
            int iterations = 0;
            while (!job.Complete) { Assert.That(job.Advance(operations, 100), Is.LessThanOrEqualTo(operations)); if (++iterations > 100000) Assert.Fail("Unbounded planner work."); }
            Assert.That(job.CachedCandidates, Is.LessThanOrEqualTo(400)); return job;
        }
        private static List<FixedTerrainPopulationPlacement> Generate(FixedTerrainPopulationPlanner planner, IEnumerable<Vector2Int> cells, int operations)
        {
            var result = new List<FixedTerrainPopulationPlacement>(); foreach (var cell in cells) result.AddRange(Finish(planner.Begin(cell), operations).Placements); return result;
        }
        private static string Signature(IEnumerable<FixedTerrainPopulationPlacement> values) => string.Join("\n", values.OrderBy(v => v.StableId, StringComparer.Ordinal)
            .Select(v => v.StableId + "|" + v.Position.ToString("R") + "|" + v.Yaw.ToString("R", System.Globalization.CultureInfo.InvariantCulture)));
    }
}
