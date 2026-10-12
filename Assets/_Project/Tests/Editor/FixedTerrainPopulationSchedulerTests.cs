using System;
using System.Linq;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Tests
{
    public sealed class FixedTerrainPopulationSchedulerTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void SchedulerReusesOnlyItsOwnCommittedReceiptAndPublicQueueStillRegisters(bool separatePresenterState)
        {
            var scene = EditorSceneManager.NewPreviewScene(); FixedTerrainPopulationPresentation presenter = null; FixedTerrainPopulationScheduler scheduler = null;
            try
            {
                int terrainReads = 0, constraintReads = 0;
                var state = new FixedTerrainPopulationState(GreaterWastelandInventorySnapshot.WorldIdentity, 17, 1, "fixture.catalog.v1",
                    new[] { new FixedTerrainPopulationRecipeVersion { recipeId = "fixture.clutter", version = 1, candidateCount = 64 } },
                    cell => { terrainReads++; return "terrain.cell.v1"; },
                    cell => { constraintReads++; return "constraints.cell.v1"; });
                var presenterState = separatePresenterState ? FixedTerrainPopulationStateTests.Create() : state;
                var template = GameObject.CreatePrimitive(PrimitiveType.Cube); SceneManager.MoveGameObjectToScene(template, scene); template.SetActive(false);
                var parent = new GameObject("Single receipt fixture"); SceneManager.MoveGameObjectToScene(parent, scene);
                presenter = new FixedTerrainPopulationPresentation(template, parent.transform, presenterState, "fixture.clutter", 256, 128);
                var planner = Planner(); scheduler = new FixedTerrainPopulationScheduler(planner, presenter, state, new[] { Vector2Int.zero });
                scheduler.SetDemand(new[] { new FixedTerrainPopulationDemand(Vector2Int.zero, true, true) }, 0);
                Finish(scheduler, presenter, 0);
                Assert.That(terrainReads, Is.EqualTo(1)); Assert.That(constraintReads, Is.EqualTo(1));
                Assert.That(presenterState.CellCount, Is.EqualTo(1), "A separate presenter owner needs its own receipt.");
                var direct = planner.Begin(Vector2Int.right);
                for (int i = 0; !direct.Complete && i < 10000; i++) direct.Advance(97, 100);
                Assert.That(direct.Complete, Is.True);
                Assert.That(presenter.Queue(Vector2Int.right, direct, true), Is.True);
                Assert.That(presenterState.CellCount, Is.EqualTo(2));
                Assert.That(terrainReads, Is.EqualTo(separatePresenterState ? 1 : 2));
                Assert.That(constraintReads, Is.EqualTo(separatePresenterState ? 1 : 2));
            }
            finally { scheduler?.Dispose(); presenter?.Dispose(); EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void CollisionDemandPrecedesVisualWorkAndCancellationCannotAdoptStaleCell()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            FixedTerrainPopulationPresentation presenter = null; FixedTerrainPopulationScheduler scheduler = null;
            try
            {
                var template = GameObject.CreatePrimitive(PrimitiveType.Cube); SceneManager.MoveGameObjectToScene(template, scene); template.SetActive(false);
                var parent = new GameObject("Actor demand fixture"); SceneManager.MoveGameObjectToScene(parent, scene);
                var state = FixedTerrainPopulationStateTests.Create(); var planner = Planner();
                presenter = new FixedTerrainPopulationPresentation(template, parent.transform, state, "fixture.clutter", 128, 128);
                scheduler = new FixedTerrainPopulationScheduler(planner, presenter, state, new[] { Vector2Int.zero, Vector2Int.right });
                scheduler.SetDemand(new[] { new FixedTerrainPopulationDemand(Vector2Int.zero, true, false, 1000), new FixedTerrainPopulationDemand(Vector2Int.right, false, true) }, 0);
                int frames = 0;
                while (!presenter.IsActive(Vector2Int.right))
                {
                    Assert.That(scheduler.Advance(13, 100, frames * .01), Is.LessThanOrEqualTo(13));
                    if (++frames > 10000) Assert.Fail("Actor cell did not become ready.");
                }
                Assert.That(presenter.IsActive(Vector2Int.zero), Is.False, "Offscreen actor work precedes high visual priority.");
                Assert.That(parent.GetComponentsInChildren<MeshRenderer>().All(r => !r.enabled), Is.True);
                Assert.That(parent.GetComponentsInChildren<Collider>().All(c => c.enabled), Is.True);
                scheduler.SetDemand(new[] { new FixedTerrainPopulationDemand(Vector2Int.zero, true, false) }, frames * .01);
                scheduler.Advance(3, 100, frames * .01);
                scheduler.SetDemand(Array.Empty<FixedTerrainPopulationDemand>(), frames * .01 + .01);
                Assert.That(scheduler.CancelledBuilds, Is.GreaterThan(0));
                scheduler.Advance(1000, 100, frames * .01 + 3);
                Assert.That(presenter.ActiveCount, Is.Zero); Assert.That(presenter.PendingCells, Is.Zero);
                Assert.That(state.CellCount, Is.EqualTo(1), "Cancelled visual cell cannot publish a receipt.");
            }
            finally { scheduler?.Dispose(); presenter?.Dispose(); EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void LiveRemovalsAndSnapshotRestoreInvalidatePresentationWithoutRegrowth()
        {
            var scene = EditorSceneManager.NewPreviewScene(); FixedTerrainPopulationPresentation presenter = null; FixedTerrainPopulationScheduler scheduler = null;
            try
            {
                var template = GameObject.CreatePrimitive(PrimitiveType.Cube); SceneManager.MoveGameObjectToScene(template, scene); template.SetActive(false);
                var parent = new GameObject("Mutation fixture"); SceneManager.MoveGameObjectToScene(parent, scene);
                var state = FixedTerrainPopulationStateTests.Create(); var planner = Planner();
                presenter = new FixedTerrainPopulationPresentation(template, parent.transform, state, "fixture.clutter", 128, 128);
                scheduler = new FixedTerrainPopulationScheduler(planner, presenter, state, new[] { Vector2Int.zero });
                scheduler.SetDemand(new[] { new FixedTerrainPopulationDemand(Vector2Int.zero, true, true) }, 0);
                Finish(scheduler, presenter, 0); var baseline = state.CaptureSnapshot(); int count = presenter.ActiveCount;
                var address = baseline.cells[0].acceptedCandidates[0]; var id = state.StableId(Vector2Int.zero, address.recipeId, address.ordinal);
                Assert.That(state.TryRemove(Vector2Int.zero, address.recipeId, address.ordinal), Is.True);
                Assert.That(presenter.ActiveCount, Is.EqualTo(count - 1));
                Assert.That(parent.GetComponentsInChildren<Collider>().Any(c => c.gameObject.name == id), Is.False);
                var saved = JsonUtility.FromJson<FixedTerrainPopulationSnapshot>(JsonUtility.ToJson(state.CaptureSnapshot()));
                scheduler.SetDemand(Array.Empty<FixedTerrainPopulationDemand>(), 1); scheduler.Advance(500, 100, 4);
                var another = baseline.cells[0].acceptedCandidates[1];
                Assert.That(state.TryRemove(Vector2Int.zero, another.recipeId, another.ordinal), Is.True, "Frozen receipts support mutation without presentation/cache residency.");
                Assert.That(state.ApplySnapshot(saved), Is.True); // restore one-removal snapshot
                scheduler.SetDemand(new[] { new FixedTerrainPopulationDemand(Vector2Int.zero, true, true) }, 5); Finish(scheduler, presenter, 5);
                Assert.That(presenter.ActiveCount, Is.EqualTo(count - 1));
                Assert.That(parent.GetComponentsInChildren<Collider>().Any(c => c.gameObject.name == id), Is.False);
                Assert.That(state.ApplySnapshot(baseline), Is.True);
                Assert.That(presenter.ActiveCount, Is.Zero, "Old objects are invalidated before adopting restored world state.");
                Finish(scheduler, presenter, 5); Assert.That(presenter.ActiveCount, Is.EqualTo(count));
            }
            finally { scheduler?.Dispose(); presenter?.Dispose(); EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void InvalidDemandIsAtomicAndUnsupportedMetadataStopsRetrying()
        {
            var scene = EditorSceneManager.NewPreviewScene(); FixedTerrainPopulationPresentation presenter = null; FixedTerrainPopulationScheduler scheduler = null;
            try
            {
                var template = GameObject.CreatePrimitive(PrimitiveType.Cube); SceneManager.MoveGameObjectToScene(template, scene); template.SetActive(false);
                var parent = new GameObject("Invalid demand fixture"); SceneManager.MoveGameObjectToScene(parent, scene);
                var state = FixedTerrainPopulationStateTests.Create();
                presenter = new FixedTerrainPopulationPresentation(template, parent.transform, state, "fixture.clutter", 128, 128);
                scheduler = new FixedTerrainPopulationScheduler(Planner(), presenter, state, new[] { Vector2Int.zero, new Vector2Int(9, 9) });
                scheduler.SetDemand(new[] { new FixedTerrainPopulationDemand(Vector2Int.zero, true, true) }, 0);
                Assert.Throws<ArgumentException>(() => scheduler.SetDemand(new[] { new FixedTerrainPopulationDemand(Vector2Int.left * 10, true, true) }, 1));
                Finish(scheduler, presenter, 0); Assert.That(presenter.IsActive(Vector2Int.zero), Is.True);
                scheduler.SetDemand(new[] { new FixedTerrainPopulationDemand(new Vector2Int(9, 9), true, true) }, 1);
                for (int i = 0; i < 100; i++) scheduler.Advance(1000, 100, 1 + i * .1);
                Assert.That(scheduler.FaultedCells, Is.EqualTo(1)); Assert.That(scheduler.PendingBuilds, Is.Zero);
                Assert.That(scheduler.IsCollisionReady(new Vector2Int(9, 9)), Is.False);
                Assert.That(state.CellCount, Is.EqualTo(1));
            }
            finally { scheduler?.Dispose(); presenter?.Dispose(); EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void CollisionReadinessRequiresFullPublicationCurrentDemandAndEnabledObstacles()
        {
            var scene = EditorSceneManager.NewPreviewScene(); FixedTerrainPopulationPresentation presenter = null; FixedTerrainPopulationScheduler scheduler = null;
            try
            {
                var template = GameObject.CreatePrimitive(PrimitiveType.Cube); SceneManager.MoveGameObjectToScene(template, scene); template.SetActive(false);
                var dormant = new GameObject("Authored inactive collider"); dormant.transform.SetParent(template.transform, false); dormant.AddComponent<BoxCollider>(); dormant.SetActive(false);
                var parent = new GameObject("Readiness fixture"); SceneManager.MoveGameObjectToScene(parent, scene);
                var state = FixedTerrainPopulationStateTests.Create();
                presenter = new FixedTerrainPopulationPresentation(template, parent.transform, state, "fixture.clutter", 128, 128);
                scheduler = new FixedTerrainPopulationScheduler(Planner(), presenter, state, new[] { Vector2Int.zero });
                scheduler.SetDemand(new[] { new FixedTerrainPopulationDemand(Vector2Int.zero, false, true) }, 0);
                Assert.That(scheduler.IsCollisionReady(Vector2Int.zero), Is.False);
                for (int i = 0; i < 10000 && !presenter.IsPending(Vector2Int.zero); i++) scheduler.Advance(1, 100, 0);
                Assert.That(presenter.IsPending(Vector2Int.zero), Is.True);
                for (int i = 0; i < 10000 && !parent.GetComponentsInChildren<Collider>().Any(c => c.enabled); i++)
                { scheduler.Advance(1, 100, 0); Assert.That(scheduler.IsCollisionReady(Vector2Int.zero), Is.False); }
                Assert.That(parent.GetComponentsInChildren<Collider>().Any(c => c.enabled), Is.True);
                Finish(scheduler, presenter, 0);
                Assert.That(scheduler.IsCollisionReady(Vector2Int.zero), Is.True, "Originally inactive children are not required obstacles.");
                Assert.That(parent.GetComponentsInChildren<MeshRenderer>().All(r => !r.enabled), Is.True, "Offscreen coverage can be ready.");
                var obstacle = parent.GetComponentsInChildren<Collider>().First(); obstacle.enabled = false;
                Assert.That(scheduler.IsCollisionReady(Vector2Int.zero), Is.False); obstacle.enabled = true;
                parent.SetActive(false); Assert.That(scheduler.IsCollisionReady(Vector2Int.zero), Is.False); parent.SetActive(true);
                Assert.That(scheduler.IsCollisionReady(Vector2Int.zero), Is.True);
                scheduler.SetDemand(new[] { new FixedTerrainPopulationDemand(Vector2Int.zero, true, false) }, 1);
                Assert.That(scheduler.IsCollisionReady(Vector2Int.zero), Is.False);
                scheduler.SetDemand(new[] { new FixedTerrainPopulationDemand(Vector2Int.zero, false, true) }, 2);
                Assert.That(scheduler.IsCollisionReady(Vector2Int.zero), Is.True);
                var snapshot = state.CaptureSnapshot(); Assert.That(state.ApplySnapshot(snapshot), Is.True);
                Assert.That(scheduler.IsCollisionReady(Vector2Int.zero), Is.False); Finish(scheduler, presenter, 2);
                Assert.That(scheduler.IsCollisionReady(Vector2Int.zero), Is.True);
                scheduler.SetDemand(Array.Empty<FixedTerrainPopulationDemand>(), 3);
                Assert.That(presenter.IsActive(Vector2Int.zero), Is.True, "Retention preserves outgoing collision.");
                Assert.That(scheduler.IsCollisionReady(Vector2Int.zero), Is.False, "Retained unrequested cells do not release entry.");
            }
            finally { scheduler?.Dispose(); presenter?.Dispose(); EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void EmptyCellsNegativeSeamsAndOutOfAreaCoverageHaveExplicitReadiness()
        {
            var scene = EditorSceneManager.NewPreviewScene(); FixedTerrainPopulationPresentation presenter = null; FixedTerrainPopulationScheduler scheduler = null;
            try
            {
                var template = GameObject.CreatePrimitive(PrimitiveType.Cube); SceneManager.MoveGameObjectToScene(template, scene); template.SetActive(false);
                var parent = new GameObject("Empty seam fixture"); SceneManager.MoveGameObjectToScene(parent, scene);
                var state = FixedTerrainPopulationStateTests.Create();
                presenter = new FixedTerrainPopulationPresentation(template, parent.transform, state, "fixture.clutter", 128, 128);
                scheduler = new FixedTerrainPopulationScheduler(Planner(0), presenter, state, new[] { Vector2Int.left, Vector2Int.zero });
                scheduler.SetDemand(new[] { new FixedTerrainPopulationDemand(Vector2Int.zero, false, true) }, 0);
                for (int i = 0; i < 1000 && scheduler.UnreadyCells > 0; i++) scheduler.Advance(97, 100, 0);
                Assert.That(scheduler.IsCollisionReady(Vector2Int.zero), Is.True, "An accepted empty cell has complete coverage.");
                var seam = new Bounds(new Vector3(0, 0, 128), new Vector3(2, 0, 2));
                Assert.That(scheduler.IsCollisionCoverageReady(seam), Is.False);
                scheduler.SetDemand(new[] { new FixedTerrainPopulationDemand(Vector2Int.zero, false, true), new FixedTerrainPopulationDemand(Vector2Int.left, false, true) }, 1);
                for (int i = 0; i < 1000 && scheduler.UnreadyCells > 0; i++) scheduler.Advance(97, 100, 1);
                Assert.That(scheduler.IsCollisionCoverageReady(seam), Is.True);
                Assert.That(scheduler.IsCollisionCoverageReady(new Bounds(new Vector3(-127.5f, 0, 128), new Vector3(255, 0, 2))), Is.True, "Exact maximum seam requires both owners.");
                Assert.That(scheduler.IsCollisionCoverageReady(new Bounds(new Vector3(-128, 0, 128), new Vector3(256, 0, 2))), Is.False, "Exact lower seam also touches the out-of-area owner.");
                Assert.That(scheduler.IsCollisionCoverageReady(new Bounds(new Vector3(-257, 0, 128), Vector3.zero)), Is.False);
                Assert.That(scheduler.IsCollisionCoverageReady(new Bounds(Vector3.zero, Vector3.one * float.MaxValue)), Is.False);
                Assert.That(scheduler.IsCollisionCoverageReady(new Bounds(new Vector3(float.NaN, 0, 0), Vector3.one)), Is.False);
                Assert.That(scheduler.IsCollisionCoverageReady(new Bounds(Vector3.zero, new Vector3(-1, 0, 1))), Is.False);
                Assert.That(presenter.ActiveCount, Is.Zero); Assert.That(state.CellCount, Is.EqualTo(2));
                scheduler.Dispose(); Assert.Throws<ObjectDisposedException>(() => scheduler.IsCollisionCoverageReady(seam));
            }
            finally { scheduler?.Dispose(); presenter?.Dispose(); EditorSceneManager.ClosePreviewScene(scene); }
        }

        private static FixedTerrainPopulationPlanner Planner(double density = .5)
        {
            bool Flat(Vector2 p, out FixedTerrainPopulationPlanner.Surface surface)
            { surface = new FixedTerrainPopulationPlanner.Surface(new Vector3(p.x, 0, p.y), Vector3.up); return true; }
            return new FixedTerrainPopulationPlanner(new FixedTerrainPopulationIdentity(GreaterWastelandInventorySnapshot.WorldIdentity, 17, 1, "fixture.clutter", 1), Flat, null, 12, 2, 38, 1, density);
        }
        private static void Finish(FixedTerrainPopulationScheduler scheduler, FixedTerrainPopulationPresentation presenter, double time)
        {
            int i = 0;
            do { scheduler.Advance(97, 100, time); if (++i > 10000) Assert.Fail("Scheduler did not finish."); }
            while (scheduler.PendingBuilds > 0 || presenter.PendingCells > 0 || presenter.ActiveCount == 0);
        }
    }
}
