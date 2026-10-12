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
                Assert.That(state.CellCount, Is.EqualTo(1));
            }
            finally { scheduler?.Dispose(); presenter?.Dispose(); EditorSceneManager.ClosePreviewScene(scene); }
        }

        private static FixedTerrainPopulationPlanner Planner()
        {
            bool Flat(Vector2 p, out FixedTerrainPopulationPlanner.Surface surface)
            { surface = new FixedTerrainPopulationPlanner.Surface(new Vector3(p.x, 0, p.y), Vector3.up); return true; }
            return new FixedTerrainPopulationPlanner(new FixedTerrainPopulationIdentity(GreaterWastelandInventorySnapshot.WorldIdentity, 17, 1, "fixture.clutter", 1), Flat, null, 12, 2, 38, 1, .5);
        }
        private static void Finish(FixedTerrainPopulationScheduler scheduler, FixedTerrainPopulationPresentation presenter, double time)
        {
            int i = 0;
            do { scheduler.Advance(97, 100, time); if (++i > 10000) Assert.Fail("Scheduler did not finish."); }
            while (scheduler.PendingBuilds > 0 || presenter.PendingCells > 0 || presenter.ActiveCount == 0);
        }
    }
}
