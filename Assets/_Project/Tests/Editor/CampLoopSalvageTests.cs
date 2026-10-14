using System.Linq;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class CampLoopSalvageTests
    {
        private TopDown3DItemCatalog catalog;
        private GameObject owner;
        private TopDown3DSalvageWreckState wreck;
        private TopDown3DInventoryState kit, cargo, cache;
        private TopDown3DInventoryState[] Inventories => new[] { kit, cargo, cache };
        [SetUp] public void Setup()
        {
            catalog = AssetDatabase.LoadAssetAtPath<TopDown3DItemCatalog>(CampLoopSalvageAuthoring.CatalogPath);
            Assert.That(catalog, Is.Not.Null);
            owner = new GameObject("Salvage contract fixture"); wreck = owner.AddComponent<TopDown3DSalvageWreckState>();
            kit = new(catalog, new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.Booter, 16));
            cargo = new(catalog, new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.BigArm, 12, 92));
            cache = new(catalog, new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.ResourceCache, 24));
        }
        [TearDown] public void Cleanup() { if (owner != null) Object.DestroyImmediate(owner); }
        private sealed class Target : ITopDown3DInteractable, ITopDown3DTargetEpoch
        {
            public TopDown3DSalvageWreckState Wreck;
            public int Index;
            public Object UnityObject => Wreck;
            public string StableId => Wreck.StableId + ":fixture:" + Index;
            public uint Epoch => Wreck.Epoch;
            public string Prompt => "Extract";
            public Vector3 InteractionPoint => Vector3.zero;
            public float InteractionRange => 2;
            public bool IsAvailable => !Wreck.IsRemoved(Index);
            public float ActionDuration => 1;
            public TopDown3DItemAmount Reward => new(TopDown3DSalvageWreckState.ItemIds[Index], TopDown3DSalvageWreckState.Quantities[Index]);
            public bool TryConsume(out int remaining) { remaining = 0; return Wreck.TryConsume(Index); }
        }
        private TopDown3DGatherActionState Gather(System.Func<bool> accessible = null, System.Func<ITopDown3DInteractable, bool> reachable = null)
            => new(new TopDown3DItemDestinationService(kit, cargo, accessible ?? (() => true)), reachable);
        [Test] public void ThreeIndependentPartsAwardExactlyOnceAndRespectCargoOnlyPolicy()
        {
            foreach (var i in new[] { 2, 0, 1 })
            {
                var gather = Gather(); var target = new Target { Wreck = wreck, Index = i };
                Assert.That(gather.TryBegin(target, Vector3.zero), Is.EqualTo(TopDown3DGatherActionResult.Started));
                Assert.That(gather.Advance(1, Vector3.zero), Is.EqualTo(TopDown3DGatherActionResult.Completed));
                Assert.That(gather.TryBegin(target, Vector3.zero), Is.EqualTo(TopDown3DGatherActionResult.Rejected));
                Assert.That(kit.TryAdd(target.Reward).Succeeded, Is.False);
            }
            Assert.That(cargo.CalculateMass(), Is.EqualTo(10.5f)); Assert.That(kit.OccupiedSlotCount, Is.Zero);
            Assert.That(wreck.ValidateRewards(Inventories), Is.True);
        }
        [Test] public void CargoLeavingAtCompletionDoesNotConsumeOrFallback()
        {
            var access = true; var gather = Gather(() => access);
            Assert.That(gather.TryBegin(new Target { Wreck = wreck }, Vector3.zero), Is.EqualTo(TopDown3DGatherActionResult.Started));
            access = false;
            Assert.That(gather.Advance(2, Vector3.zero), Is.EqualTo(TopDown3DGatherActionResult.DestinationUnavailable));
            Assert.That(wreck.RemovedMask, Is.Zero); Assert.That(cargo.OccupiedSlotCount, Is.Zero); Assert.That(kit.OccupiedSlotCount, Is.Zero);
        }
        [Test] public void EpochChangeCancelsEvenWhenRestoredTargetIsAvailable()
        {
            var gather = Gather(); gather.TryBegin(new Target { Wreck = wreck }, Vector3.zero);
            wreck.Invalidate(); Assert.That(gather.Advance(2, Vector3.zero), Is.EqualTo(TopDown3DGatherActionResult.Cancelled));
            Assert.That(cargo.OccupiedSlotCount, Is.Zero); Assert.That(wreck.RemovedMask, Is.Zero);
        }
        [Test] public void BlockedReachAndExplicitCancelLeavePartIntact()
        {
            var reach = true; var gather = Gather(reachable: _ => reach);
            gather.TryBegin(new Target { Wreck = wreck }, Vector3.zero); reach = false;
            Assert.That(gather.Advance(2, Vector3.zero), Is.EqualTo(TopDown3DGatherActionResult.Cancelled));
            reach = true; gather.TryBegin(new Target { Wreck = wreck }, Vector3.zero); gather.Cancel();
            Assert.That(wreck.RemovedMask, Is.Zero); Assert.That(cargo.OccupiedSlotCount, Is.Zero);
        }
        [Test] public void HeavyCargoRejectsBeforeExtraction()
        {
            cargo.TryAdd(new TopDown3DItemAmount("resource.ironstone_ore", 72));
            Assert.That(Gather().TryBegin(new Target { Wreck = wreck, Index = 2 }, Vector3.zero), Is.EqualTo(TopDown3DGatherActionResult.InventoryFull));
            Assert.That(wreck.RemovedMask, Is.Zero);
        }
        [Test] public void ResetClearsTransferredRewardsAndPreservesOtherItems()
        {
            for (var i = 0; i < 3; i++) { var gather = Gather(); gather.TryBegin(new Target { Wreck = wreck, Index = i }, Vector3.zero); gather.Advance(2, Vector3.zero); }
            TopDown3DInventoryTransferService.Transfer(cargo, cache, TopDown3DSalvageWreckState.ItemIds[0], 1);
            cargo.TryAdd(new TopDown3DItemAmount("resource.ironstone_ore", 3));
            cache.TryAdd(new TopDown3DItemAmount("resource.airborne_dust", 5));
            kit.TryAdd(new TopDown3DItemAmount("tool.popup_camp", 1));
            Assert.That(wreck.TryResetRewards(Inventories, out _), Is.True);
            Assert.That(wreck.TryResetRewards(Inventories, out _), Is.True);
            Assert.That(cargo.CalculateMass(), Is.EqualTo(3.75f)); Assert.That(cache.Slots.Sum(s => s.Quantity), Is.EqualTo(5));
            Assert.That(kit.OccupiedSlotCount, Is.EqualTo(1)); Assert.That(wreck.ValidateRewards(Inventories), Is.True);
        }
        [Test] public void MissingResetStorageNeverReplenishesWreck()
        {
            wreck.TryConsume(0);
            Assert.That(wreck.TryResetRewards(new[] { kit, cargo }, out _), Is.False); Assert.That(wreck.IsRemoved(0), Is.True);
            Assert.That(wreck.TryResetRewards(new[] { kit, cargo, cargo }, out _), Is.False); Assert.That(wreck.IsRemoved(0), Is.True);
        }
        [Test] public void SerializedDepletionAndInventoryReconstructTogether()
        {
            var gather = Gather(); gather.TryBegin(new Target { Wreck = wreck, Index = 1 }, Vector3.zero); gather.Advance(2, Vector3.zero);
            var text = JsonUtility.ToJson(wreck); var saved = cargo.CaptureSnapshot();
            var restoredObject = new GameObject("Reloaded state");
            try
            {
                var restored = restoredObject.AddComponent<TopDown3DSalvageWreckState>(); JsonUtility.FromJsonOverwrite(text, restored);
                var retained = new TopDown3DInventoryState(catalog, new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.BigArm, 12, 92));
                retained.ApplySnapshot(saved);
                Assert.That(restored.ValidateRewards(new[] { kit, retained, cache }), Is.True);
                Assert.That(restored.TryConsume(1), Is.False);
                JsonUtility.FromJsonOverwrite("{\"version\":99}", restored); Assert.That(restored.IsValid, Is.False);
            }
            finally { Object.DestroyImmediate(restoredObject); }
        }
        [Test] public void SceneHasPrivateBindingsAndCheckpointIsSuppressed()
        {
            Object.DestroyImmediate(owner); owner = null;
            var scene = EditorSceneManager.OpenScene(GreaterWastelandSceneScope.CampLoopScene);
            CampLoopSalvageAuthoring.Validate(scene);
            Assert.That(TopDown3DAnimationPlaygroundSession.IsSessionOnly(scene), Is.True);
            var lab = TopDown3DAnimationPlaygroundSession.Find(scene);
            Assert.That(lab.CheckDependencies(), Is.True);
            Assert.That(lab.Advance(AnimationPlaygroundReadiness.ServicesBound), Is.True);
            Assert.That(lab.Advance(AnimationPlaygroundReadiness.VisualReady), Is.True);
            var roots = scene.GetRootGameObjects();
            var session = roots.SelectMany(r => r.GetComponentsInChildren<CampLoopSalvageSession>()).Single();
            session.TryBindSession();
            Assert.That(session.Ready, Is.True, session.FailureReason);
            var sceneKit = roots.SelectMany(r => r.GetComponentsInChildren<TopDown3DPlayerInventory>()).Single();
            var sceneCargo = roots.SelectMany(r => r.GetComponentsInChildren<TopDown3DBigArmCargo>()).Single();
            foreach (var point in roots.SelectMany(r => r.GetComponentsInChildren<TopDown3DSalvagePoint>()))
            {
                var actor = point.InteractionPoint + Vector3.back;
                Physics.SyncTransforms();
                var ray = new Ray(actor, Vector3.forward);
                Assert.That(Physics.Raycast(ray, out var hit, 1.2f, ~0, QueryTriggerInteraction.Collide), Is.True);
                Assert.That(point.OwnsCollider(hit.collider), Is.True, "Exposed approach must reach its own socket first.");
                var gather = new TopDown3DGatherActionState(new TopDown3DItemDestinationService(sceneKit.State, sceneCargo.State, () => true));
                Assert.That(gather.TryBegin(point, actor), Is.EqualTo(TopDown3DGatherActionResult.Started));
                Assert.That(gather.Advance(4, actor), Is.EqualTo(TopDown3DGatherActionResult.Completed));
                point.RefreshPresentation(); Assert.That(point.IsAvailable, Is.False);
            }
            Assert.That(lab.ResetRegistry.TryReset(), Is.True, lab.ResetRegistry.LastFailure);
            Assert.That(sceneCargo.State.Slots.Any(s => TopDown3DSalvageWreckState.ItemIds.Contains(s.ItemId)), Is.False);
            var saveObject = new GameObject("Checkpoint policy fixture");
            try { var save = saveObject.AddComponent<GreaterWastelandInventorySaveService>(); Assert.That(save.IsSessionOnly, Is.True); Assert.That(save.Save(), Is.False); }
            finally { Object.DestroyImmediate(saveObject); EditorSceneManager.NewScene(NewSceneSetup.EmptyScene); }
        }
    }
}
