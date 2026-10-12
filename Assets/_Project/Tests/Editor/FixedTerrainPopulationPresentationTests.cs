using System.Linq;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Tests
{
    public sealed class FixedTerrainPopulationPresentationTests
    {
        [Test]
        public void CapacityAdmissionReservesPendingWorkAndKeepsCurrentCoverage()
        {
            var scene = EditorSceneManager.NewPreviewScene(); FixedTerrainPopulationPresentation presenter = null;
            try
            {
                var template = GameObject.CreatePrimitive(PrimitiveType.Cube); SceneManager.MoveGameObjectToScene(template, scene); template.SetActive(false);
                var parent = new GameObject("Capacity fixture"); SceneManager.MoveGameObjectToScene(parent, scene);
                var first = Build(Vector2Int.zero); var second = Build(Vector2Int.right);
                int capacity = System.Math.Max(first.Placements.Count, second.Placements.Count);
                Assert.That(capacity, Is.LessThan(first.Placements.Count * 2));
                presenter = new FixedTerrainPopulationPresentation(template, parent.transform, FixedTerrainPopulationStateTests.Create(), "fixture.clutter", capacity, capacity);
                Assert.That(presenter.Queue(first.Cell, first, true), Is.True);
                Assert.That(presenter.Queue(second.Cell, second, true), Is.False, "Pending preparation reserves capacity before instantiation.");
                Finish(presenter);
                var ids = parent.GetComponentsInChildren<MeshRenderer>().Select(r => r.name).OrderBy(v => v).ToArray();
                Assert.That(presenter.Queue(first.Cell, Build(first.Cell), true), Is.False, "Replacement headroom is required.");
                Assert.That(parent.GetComponentsInChildren<MeshRenderer>().Select(r => r.name).OrderBy(v => v), Is.EqualTo(ids));
                Assert.That(presenter.PendingCells, Is.Zero);
                Assert.That(presenter.InstanceCount, Is.LessThanOrEqualTo(capacity));
                presenter.Release(first.Cell); Finish(presenter);
                Assert.That(presenter.Queue(second.Cell, second, true), Is.True); Finish(presenter);
                Assert.That(presenter.ActiveCount, Is.EqualTo(second.Placements.Count));
            }
            finally { presenter?.Dispose(); EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void RelevanceChangesReachPartiallyPublishedSuccessorsAndPendingQueueIsBounded()
        {
            var scene = EditorSceneManager.NewPreviewScene(); FixedTerrainPopulationPresentation presenter = null;
            try
            {
                var template = GameObject.CreatePrimitive(PrimitiveType.Cube); SceneManager.MoveGameObjectToScene(template, scene); template.SetActive(false);
                var parent = new GameObject("Partial publication fixture"); SceneManager.MoveGameObjectToScene(parent, scene);
                var build = Build(Vector2Int.zero);
                presenter = new FixedTerrainPopulationPresentation(template, parent.transform, FixedTerrainPopulationStateTests.Create(), "fixture.clutter", 128, 128, 1);
                Assert.That(presenter.Queue(build.Cell, build, true), Is.True);
                Assert.That(presenter.Queue(Vector2Int.right, Build(Vector2Int.right), true), Is.False);
                presenter.Advance(build.Placements.Count + 1, 100);
                Assert.That(parent.GetComponentsInChildren<MeshRenderer>().Length, Is.EqualTo(1));
                presenter.SetVisualRelevant(build.Cell, false); presenter.SetCollisionRelevant(build.Cell, false);
                Assert.That(parent.GetComponentsInChildren<MeshRenderer>().All(r => !r.enabled), Is.True);
                Assert.That(parent.GetComponentsInChildren<Collider>().All(c => !c.enabled), Is.True);
                Finish(presenter);
                Assert.That(parent.GetComponentsInChildren<MeshRenderer>().All(r => !r.enabled), Is.True);
                presenter.SetCollisionRelevant(build.Cell, true);
                Assert.That(parent.GetComponentsInChildren<Collider>().All(c => c.enabled), Is.True);
            }
            finally { presenter?.Dispose(); EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void NativeSectorIntegratesAcceptedBasaltLodsStateAndRecreationWithoutTerrainChanges()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var data = new TerrainData { heightmapResolution = 33, size = new Vector3(256, 100, 256) };
            FixedTerrainPopulationPresentation presenter = null;
            try
            {
                var heights = new float[33, 33];
                for (int z = 0; z < 33; z++) for (int x = 0; x < 33; x++) heights[z, x] = .5f;
                data.SetHeights(0, 0, heights); var before = data.GetHeights(0, 0, 33, 33);
                var tiles = new System.Collections.Generic.List<Terrain>();
                for (int z = -2; z < 2; z++) for (int x = -2; x < 2; x++)
                {
                    var tile = Terrain.CreateTerrainGameObject(data).GetComponent<Terrain>(); SceneManager.MoveGameObjectToScene(tile.gameObject, scene);
                    tile.transform.position = new Vector3(x * 256, -25, z * 256); tile.enabled = false; tiles.Add(tile);
                }
                Physics.SyncTransforms(); var surface = new FixedTerrainSurfaceIndex(tiles, "fixture.native.v1");
                var identity = new FixedTerrainPopulationIdentity(GreaterWastelandInventorySnapshot.WorldIdentity, 17, 1, "fixture.basalt", 1);
                var state = new FixedTerrainPopulationState(identity.WorldId, 17, 1, "fixture.basalt.catalog.v1",
                    new[] { new FixedTerrainPopulationRecipeVersion { recipeId = identity.RecipeId, version = 1, candidateCount = 64 } },
                    cell => cell.x >= -2 && cell.x < 2 && cell.y >= -2 && cell.y < 2 ? "fixture.native.v1" : null,
                    cell => "fixture.spawn-mask.v1");
                var planner = new FixedTerrainPopulationPlanner(identity, FixedTerrainPopulationPlanner.NativeSurface(surface),
                    new[] { new FixedTerrainPopulationExclusion(Vector2.zero, Vector2.zero, 64) }, 12, 4, 38, 1, .05);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/TopDown3D/Environment/SculptedBasalt01.prefab");
                Assert.That(prefab, Is.Not.Null);
                var parent = new GameObject("Bounded native sector basalt fixture"); SceneManager.MoveGameObjectToScene(parent, scene);
                presenter = new FixedTerrainPopulationPresentation(prefab, parent.transform, state, identity.RecipeId, 256, 128);
                var builds = new System.Collections.Generic.List<FixedTerrainPopulationPlanner.CellBuild>();
                for (int z = -2; z < 2; z++) for (int x = -2; x < 2; x++)
                {
                    var build = planner.Begin(new Vector2Int(x, z)); while (!build.Complete) build.Advance(13, 100);
                    Assert.That(presenter.Queue(build.Cell, build, false), Is.True); builds.Add(build);
                }
                Finish(presenter); int count = presenter.ActiveCount;
                Assert.That(count, Is.GreaterThan(10)); Assert.That(count, Is.LessThan(100));
                Assert.That(parent.GetComponentsInChildren<LODGroup>().Length, Is.EqualTo(count));
                var knownMeshes = prefab.GetComponentsInChildren<MeshFilter>(true).Select(f => f.sharedMesh).ToArray();
                Assert.That(parent.GetComponentsInChildren<MeshFilter>(true).All(f => knownMeshes.Contains(f.sharedMesh)), Is.True);
                var first = builds.First(b => b.Placements.Count > 0); var removed = first.Placements[0];
                Assert.That(state.TryRemove(first.Cell, identity.RecipeId, removed.Ordinal), Is.True);
                var saved = JsonUtility.FromJson<FixedTerrainPopulationSnapshot>(JsonUtility.ToJson(state.CaptureSnapshot()));
                foreach (var build in builds) presenter.Release(build.Cell); Finish(presenter);
                Assert.That(state.ApplySnapshot(saved), Is.True);
                foreach (var build in builds) Assert.That(presenter.Queue(build.Cell, build, false), Is.True);
                Finish(presenter);
                Assert.That(presenter.ActiveCount, Is.EqualTo(count - 1));
                Assert.That(parent.GetComponentsInChildren<LODGroup>().Any(g => g.gameObject.name == removed.StableId), Is.False);
                Assert.That(tiles.All(t => !t.enabled && t.GetComponent<TerrainCollider>().enabled), Is.True);
                Assert.That(data.GetHeights(0, 0, 33, 33), Is.EqualTo(before));
            }
            finally { presenter?.Dispose(); EditorSceneManager.ClosePreviewScene(scene); Object.DestroyImmediate(data); }
        }
        [Test]
        public void BoundedPoolRestoresCellWithoutRegrowingRemovalAndCollisionIsIndependent()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            FixedTerrainPopulationPresentation presenter = null;
            try
            {
                var template = GameObject.CreatePrimitive(PrimitiveType.Cube); SceneManager.MoveGameObjectToScene(template, scene); template.SetActive(false);
                var parent = new GameObject("Presentation fixture"); SceneManager.MoveGameObjectToScene(parent, scene);
                var state = FixedTerrainPopulationStateTests.Create(); var cell = Vector2Int.zero;
                var build = Build(cell);
                presenter = new FixedTerrainPopulationPresentation(template, parent.transform, state, "fixture.clutter", 128, 128);
                Assert.That(presenter.Queue(cell, build, false), Is.True);
                Assert.That(presenter.Advance(1, 100), Is.EqualTo(1));
                Assert.That(parent.GetComponentsInChildren<MeshRenderer>().Length, Is.Zero, "Prepared objects remain hidden until the cell is ready.");
                Finish(presenter);
                Assert.That(presenter.ActiveCount, Is.EqualTo(build.Placements.Count));
                Assert.That(parent.GetComponentsInChildren<Collider>().All(c => !c.enabled), Is.True);
                presenter.SetCollisionRelevant(cell, true);
                Assert.That(parent.GetComponentsInChildren<Collider>().All(c => c.enabled), Is.True);
                presenter.SetVisualRelevant(cell, false);
                Assert.That(parent.GetComponentsInChildren<MeshRenderer>().All(r => !r.enabled), Is.True);
                Assert.That(parent.GetComponentsInChildren<Collider>().All(c => c.enabled), Is.True, "Offscreen actor collision survives visual culling.");
                presenter.SetVisualRelevant(cell, true);
                var removed = build.Placements[0]; Assert.That(state.TryRemove(cell, "fixture.clutter", removed.Ordinal), Is.True);
                presenter.Release(cell); Assert.That(presenter.ActiveCount, Is.Zero); Finish(presenter);
                int pooledCount = presenter.InstanceCount;
                Assert.That(presenter.Queue(cell, Build(cell), true), Is.True); Finish(presenter);
                Assert.That(presenter.ActiveCount, Is.EqualTo(build.Placements.Count - 1));
                Assert.That(parent.GetComponentsInChildren<MeshRenderer>().Any(r => r.gameObject.name == removed.StableId), Is.False);
                Assert.That(presenter.InstanceCount, Is.EqualTo(pooledCount), "The released cell reuses shared-art instances.");
                presenter.Dispose(); Assert.That(parent.transform.childCount, Is.Zero);
            }
            finally { presenter?.Dispose(); EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void ReplacementRetainsOldCoverageAndRejectsWrongOwnership()
        {
            var scene = EditorSceneManager.NewPreviewScene(); FixedTerrainPopulationPresentation presenter = null;
            try
            {
                var template = GameObject.CreatePrimitive(PrimitiveType.Cube); SceneManager.MoveGameObjectToScene(template, scene); template.SetActive(false);
                var parent = new GameObject("Replacement fixture"); SceneManager.MoveGameObjectToScene(parent, scene);
                var state = FixedTerrainPopulationStateTests.Create(); var cell = Vector2Int.zero;
                presenter = new FixedTerrainPopulationPresentation(template, parent.transform, state, "fixture.clutter", 128, 128);
                var initial = Build(cell); presenter.Queue(cell, initial, true); Finish(presenter);
                var ids = parent.GetComponentsInChildren<MeshRenderer>().Select(r => r.gameObject.name).OrderBy(v => v).ToArray();
                Assert.That(presenter.Queue(Vector2Int.right, Build(cell), true), Is.False);
                Assert.That(presenter.Queue(cell, Build(cell, 99), true), Is.False, "An incompatible seed must reject before capacity queries or receipt adoption.");
                var replacement = Build(cell); Assert.That(presenter.Queue(cell, replacement, true), Is.True);
                presenter.Advance(1, 100);
                Assert.That(parent.GetComponentsInChildren<MeshRenderer>().Select(r => r.gameObject.name).OrderBy(v => v), Is.EqualTo(ids));
                // A logical mutation after preparation must not be published by the pending replacement.
                Assert.That(state.TryRemove(cell, "fixture.clutter", replacement.Placements[0].Ordinal), Is.True);
                Finish(presenter);
                Assert.That(presenter.ActiveCount, Is.EqualTo(initial.Placements.Count - 1));
                Assert.That(parent.GetComponentsInChildren<Collider>().Count(c => c.enabled), Is.EqualTo(presenter.ActiveCount));
            }
            finally { presenter?.Dispose(); EditorSceneManager.ClosePreviewScene(scene); }
        }
        private static FixedTerrainPopulationPlanner.CellBuild Build(Vector2Int cell, int seed = 17)
        {
            bool Surface(Vector2 p, out FixedTerrainPopulationPlanner.Surface sample)
            { sample = new FixedTerrainPopulationPlanner.Surface(new Vector3(p.x, 0, p.y), Vector3.up); return true; }
            var planner = new FixedTerrainPopulationPlanner(new FixedTerrainPopulationIdentity(GreaterWastelandInventorySnapshot.WorldIdentity, seed, 1, "fixture.clutter", 1),
                Surface, null, 12, 2, 38, 1, .5);
            var job = planner.Begin(cell); while (!job.Complete) job.Advance(97, 100); return job;
        }
        private static void Finish(FixedTerrainPopulationPresentation presenter)
        {
            int count = 0;
            // Retirements can remain after the cell is released, so keep advancing until one empty step.
            while (presenter.Advance(7, 100) != 0) if (++count > 10000) Assert.Fail("Unbounded presentation work.");
            Assert.That(presenter.PendingCells, Is.Zero);
        }
    }
}
