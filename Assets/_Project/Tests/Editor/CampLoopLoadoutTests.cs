using System;
using System.Linq;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class CampLoopLoadoutTests
    {
        private TopDown3DItemCatalog catalog;
        private const string DustTool = "tool.micro_dust_harvester";
        private const string Jacket = "equipment.jacket";
        [SetUp] public void Setup()
        {
            var profile = AssetDatabase.LoadAssetAtPath<TopDown3DCampLoopLoadoutProfile>("Assets/_Project/Settings/Items/CampLoopLoadout/Resources/CampLoopLoadoutProfile.asset");
            Assert.That(profile, Is.Not.Null);
            catalog = UnityEngine.Object.Instantiate(profile.sourceCatalog);
            catalog.ApplyLoadoutProfile(profile);
        }
        [TearDown] public void Cleanup() => UnityEngine.Object.DestroyImmediate(catalog);
        private TopDown3DInventoryState Kit() => new(catalog, new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.Booter, 10, 0, true));
        private TopDown3DInventoryState Cargo(float mass = 92) => new(catalog, new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.BigArm, 12, mass));
        [Test] public void EveryEquipmentFitsOnlyItsNamedSlot()
        {
            var state = Kit();
            foreach (var item in catalog.Definitions.Where(d => d.IsEquipment))
            {
                Assert.That(state.TryAdd(new TopDown3DItemAmount(item.ItemId, 1)).Succeeded, Is.True);
                var index = (int)item.EquipmentSlot - 1;
                Assert.That(state.Slots[index].ItemId, Is.EqualTo(item.ItemId));
                Assert.That(Guid.TryParseExact(state.Slots[index].InstanceId, "N", out _), Is.True);
                for (var wrong = 0; wrong < 10; wrong++)
                    if (wrong != index) Assert.That(state.Policy.AllowsSlot(wrong, item), Is.False);
            }
            Assert.That(state.OccupiedSlotCount, Is.EqualTo(6));
            Assert.That(state.CreateDraft().OccupiedSlotCount, Is.EqualTo(6));
            Assert.That(state.TryAdd(new TopDown3DItemAmount(Jacket, 1)).Succeeded, Is.False);
        }
        [TestCase("resource.ironstone_ore")][TestCase("resource.airborne_dust")]
        public void ResourcesNeverEnterEquipmentOrSupplies(string id)
        { Assert.That(Kit().TryAdd(new TopDown3DItemAmount(id, 1)).Succeeded, Is.False); }
        [Test] public void PopupCampUsesSupplyPocketAndWrongSlotMovePreservesIdentity()
        {
            var state = Kit(); state.TryAdd(new TopDown3DItemAmount(DustTool, 1));
            state.TryAdd(new TopDown3DItemAmount("tool.popup_camp", 1));
            var identity = state.Slots[0].InstanceId;
            Assert.That(state.Slots[6].ItemId, Is.EqualTo("tool.popup_camp"));
            Assert.That(state.TryMoveOrSwap(0, 7).Succeeded, Is.False);
            Assert.That(state.Slots[0].InstanceId, Is.EqualTo(identity));
            Assert.That(state.TryMoveOrSwap(6, 9).Succeeded, Is.True);
        }
        [Test] public void EquipmentIdentitySurvivesTransferPackingAndSnapshot()
        {
            var kit = Kit(); var cargo = Cargo(); kit.TryAdd(new TopDown3DItemAmount(Jacket, 1));
            var identity = kit.Slots[5].InstanceId;
            Assert.That(TopDown3DInventoryTransferService.TransferSlot(kit, cargo, 5, () => true).Succeeded, Is.True);
            var packing = AssetDatabase.LoadAssetAtPath<TopDown3DPackingSettings>("Assets/_Project/Settings/Items/TopDown3DPackingSettings.asset");
            Assert.That(TopDown3DCargoAutoPacker.TryPack(cargo, packing, out var packed), Is.True);
            Assert.That(cargo.ApplySnapshot(packed).Succeeded, Is.True);
            Assert.That(cargo.Slots.Single(s => !s.IsEmpty).InstanceId, Is.EqualTo(identity));
            var index = Enumerable.Range(0, 12).Single(i => !cargo.Slots[i].IsEmpty);
            Assert.That(TopDown3DInventoryTransferService.EquipSlot(cargo, kit, index, () => true).Succeeded, Is.True);
            Assert.That(kit.Slots[5].InstanceId, Is.EqualTo(identity));
        }
        [Test] public void EquipSwapIsCoherentBeforeEitherObserverRuns()
        {
            var kit = Kit(); var cargo = Cargo(); kit.TryAdd(new TopDown3DItemAmount(Jacket, 1)); cargo.TryAdd(new TopDown3DItemAmount(Jacket, 1));
            var old = kit.Slots[5].InstanceId; var next = cargo.Slots[0].InstanceId; var observations = 0;
            void Observe() { observations++; Assert.That(kit.Slots[5].InstanceId, Is.EqualTo(next)); Assert.That(cargo.Slots.Single(s => !s.IsEmpty).InstanceId, Is.EqualTo(old)); }
            kit.Changed += Observe; cargo.Changed += Observe;
            Assert.That(TopDown3DInventoryTransferService.EquipSlot(cargo, kit, 0, () => true).Succeeded, Is.True);
            Assert.That(observations, Is.EqualTo(2));
        }
        [Test] public void AccessLossAndExcessMassPreserveBothEquipmentOwners()
        {
            var kit = Kit(); var cargo = Cargo(.9f); kit.TryAdd(new TopDown3DItemAmount(Jacket, 1)); cargo.TryAdd(new TopDown3DItemAmount(DustTool, 1));
            var before = JsonUtility.ToJson(kit.CaptureSnapshot()); var cargoBefore = JsonUtility.ToJson(cargo.CaptureSnapshot());
            Assert.That(TopDown3DInventoryTransferService.TransferSlot(kit, cargo, 5, () => true).Succeeded, Is.False);
            Assert.That(TopDown3DInventoryTransferService.EquipSlot(cargo, kit, 0, () => false).Succeeded, Is.False);
            Assert.That(JsonUtility.ToJson(kit.CaptureSnapshot()), Is.EqualTo(before)); Assert.That(JsonUtility.ToJson(cargo.CaptureSnapshot()), Is.EqualTo(cargoBefore));
        }
        [Test] public void AttachedAlgaeCannotBeTransferredOrRemoved()
        {
            var kit = Kit(); kit.TryAdd(new TopDown3DItemAmount("equipment.algae_canister", 1));
            Assert.That(kit.TryRemove("equipment.algae_canister", 1).Succeeded, Is.False);
            Assert.That(TopDown3DInventoryTransferService.TransferSlot(kit, Cargo(), 1, () => true).Succeeded, Is.False);
        }
        [Test] public void DeployedReservationRejectsReplacementButAcceptsSameInstance()
        {
            var kit = Kit(); var identity = Guid.NewGuid().ToString("N");
            kit.EquipmentAdmission = (id, instance) => id != DustTool || instance == identity;
            Assert.That(kit.TryAdd(new TopDown3DItemAmount(DustTool, 1)).Succeeded, Is.False);
            Assert.That(kit.TryAdd(new TopDown3DItemAmount(DustTool, 1, identity)).Succeeded, Is.True);
        }
        [Test] public void DuplicateInstanceSnapshotIsRejectedWithoutMutation()
        {
            var cargo = Cargo(); var id = Guid.NewGuid().ToString("N");
            var records = Enumerable.Range(0, 12).Select(i => TopDown3DInventorySlotSnapshot.Create(i < 2 ? Jacket : null, i < 2 ? 1 : 0, i < 2 ? id : null)).ToArray();
            Assert.That(cargo.ApplySnapshot(TopDown3DInventorySnapshot.Create(2, records)).Succeeded, Is.False);
            Assert.That(cargo.OccupiedSlotCount, Is.Zero);
        }
        [Test] public void LegacyProductionKeepsOriginalCapacityAndVersionOne()
        {
            var production = AssetDatabase.LoadAssetAtPath<TopDown3DItemCatalog>("Assets/_Project/Settings/Items/TopDown3DItemCatalog.asset");
            var state = new TopDown3DInventoryState(production, 16);
            Assert.That(state.TryAdd(new TopDown3DItemAmount(DustTool, 4)).Succeeded, Is.True);
            Assert.That(state.Slots[0].Quantity, Is.EqualTo(4)); Assert.That(state.Slots[0].InstanceId, Is.Null);
            Assert.That(state.CaptureSnapshot().Version, Is.EqualTo(1));
            Assert.That(state.ApplySnapshot(TopDown3DInventorySnapshot.Create(2, state.Slots)).Succeeded, Is.False);
        }

        [Test] public void WorldAndInventoryResetInstallBeforeNotificationsAndBlockReentry()
        {
            var owner = new GameObject("Reset state fixture");
            try
            {
                var algae = owner.AddComponent<TopDown3DAlgaeCanisterState>();
                var settings = AssetDatabase.LoadAssetAtPath<TopDown3DAlgaeCanisterSettings>("Assets/_Project/Settings/Survival/Resources/TopDown3DAlgaeCanisterSettings.asset");
                algae.Configure(settings); var initial = algae.CaptureSnapshot();
                algae.ApplySnapshot(new TopDown3DAlgaeCanisterSnapshot { remaining = 20, capacity = settings.capacity });
                var kit = Kit(); var cargo = Cargo(); cargo.TryAdd(new TopDown3DItemAmount(Jacket, 1));
                var a = new TopDown3DInventoryTransaction.Mutation(kit); var b = new TopDown3DInventoryTransaction.Mutation(cargo);
                a.Draft.TryAdd(new TopDown3DItemAmount(Jacket, 1, cargo.Slots[0].InstanceId)); b.Draft.RemoveFromSlot(0, 1);
                var observations = 0;
                kit.Changed += () => { observations++; Assert.That(algae.Remaining, Is.EqualTo(initial.remaining)); Assert.That(cargo.OccupiedSlotCount, Is.Zero);
                    Assert.That(kit.TryRemove(Jacket, 1).Succeeded, Is.False); Assert.That(algae.ApplySnapshot(initial), Is.False); };
                Assert.That(TopDown3DInventoryTransaction.Commit(new[] { a, b }, null,
                    new[] { algae.PrepareTrialRestore(initial) }).Succeeded, Is.True);
                Assert.That(observations, Is.EqualTo(1));
            }
            finally { UnityEngine.Object.DestroyImmediate(owner); }
        }
        [Test] public void StaleWorldResetRejectsWithoutChangingInventoryOrAlgae()
        {
            var owner = new GameObject("Stale reset fixture");
            try
            {
                var algae = owner.AddComponent<TopDown3DAlgaeCanisterState>();
                var settings = AssetDatabase.LoadAssetAtPath<TopDown3DAlgaeCanisterSettings>("Assets/_Project/Settings/Survival/Resources/TopDown3DAlgaeCanisterSettings.asset");
                algae.Configure(settings); var initial = algae.CaptureSnapshot();
                algae.ApplySnapshot(new TopDown3DAlgaeCanisterSnapshot { remaining = 20, capacity = settings.capacity });
                var prepared = algae.PrepareTrialRestore(initial); algae.Advance(1);
                var before = algae.Remaining; var kit = Kit(); var mutation = new TopDown3DInventoryTransaction.Mutation(kit);
                mutation.Draft.TryAdd(new TopDown3DItemAmount(Jacket, 1));
                Assert.That(TopDown3DInventoryTransaction.Commit(new[] { mutation }, null, new[] { prepared }).Succeeded, Is.False);
                Assert.That(kit.OccupiedSlotCount, Is.Zero); Assert.That(algae.Remaining, Is.EqualTo(before));
            }
            finally { UnityEngine.Object.DestroyImmediate(owner); }
        }
        [Test] public void LoadoutNavigationConnectsEquipmentSuppliesAndFooter()
        {
            var owner = new GameObject("Loadout UI fixture", typeof(RectTransform));
            try
            {
                var canvas = owner.AddComponent<BooterBigArm.TopDown3D.TopDown3DInventoryCanvas>();
                canvas.InitializeDual(10, 12, _ => { }, _ => { }, _ => { }, _ => { }, () => { }); canvas.ConfigureLoadout(true);
                canvas.ConfigureTrialControls(() => { }, true, false, () => { }, true, "STORE", () => { }, true);
                Assert.That(canvas.SlotViews[0].Button.navigation.selectOnRight, Is.EqualTo(canvas.SlotViews[1].Button));
                Assert.That(canvas.SlotViews[4].Button.navigation.selectOnDown, Is.EqualTo(canvas.SlotViews[6].Button));
                Assert.That(canvas.SlotViews[9].Button.navigation.selectOnUp, Is.EqualTo(canvas.SlotViews[5].Button));
                Assert.That(canvas.SlotViews[6].Button.navigation.selectOnDown.name, Is.EqualTo("Reset Inventory Trial"));
                canvas.ConfigureLoadout(false);
                Assert.That(canvas.SlotViews[0].Button.navigation.selectOnDown, Is.EqualTo(canvas.SlotViews[4].Button));
            }
            finally { UnityEngine.Object.DestroyImmediate(owner); }
        }
        [Test] public void SavedCampLoopUsesTypedStateBeforeAndAfterServiceInstallation()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(GreaterWastelandSceneScope.CampLoopScene);
            try
            {
                var kit = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<TopDown3DPlayerInventory>(true)).Single();
                Assert.That(kit.UsesLoadout, Is.True, "Profile must match the private catalog and session-only marker.");
                Assert.That(kit.State.Policy.IsLoadout, Is.True); Assert.That(kit.Capacity, Is.EqualTo(10));
                Assert.That(kit.State.CreateDraft().OccupiedSlotCount, Is.EqualTo(7));
                var lab = TopDown3DAnimationPlaygroundSession.Find(scene);
                kit.GetComponent<TopDown3DPlayerMotor>().Configure(TopDown3DGameHudCanvas.FindInScene<TopDown3DInputRouter>(scene),
                    TopDown3DGameHudCanvas.FindInScene<Camera>(scene).transform);
                Assert.That(lab.CheckDependencies(), Is.True);
                Assert.That(TopDown3DInventoryUiSceneInstaller.TryInstallForScene(scene,
                    System.IO.Path.Combine(System.IO.Path.GetTempPath(), "CampLoadoutFixture-" + Guid.NewGuid().ToString("N"))), Is.Not.Null);
                Assert.That(lab.Advance(AnimationPlaygroundReadiness.ServicesBound), Is.True);
                Assert.That(lab.Advance(AnimationPlaygroundReadiness.VisualReady), Is.True);
                var economy = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<TopDown3DCampLoopEconomySession>(true)).Single();
                Assert.That(economy.Initialize(), Is.True, economy.FailureReason);
                Assert.That(kit.State.CreateDraft().OccupiedSlotCount, Is.EqualTo(7));
                var trial = kit.GetComponent<TopDown3DCampLoopInventoryTrial>();
                Assert.That(trial, Is.Not.Null); Assert.That(trial.Ready, Is.True, trial.FailureReason);
                var ui = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<TopDown3DInventoryUiController>(true)).Single();
                ui.Open();
                var cargo = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<TopDown3DBigArmCargo>(true)).Single();
                cargo.State.TryAdd(new TopDown3DItemAmount("resource.ironstone_ore", 3));
                var devices = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<TopDown3DPlacedHarvesterState>(true)).Single();
                var canisterIdentity = kit.State.Slots[0].InstanceId;
                Assert.That(devices.TryPlace(kit.transform.position, kit.State, out var placement), Is.True);
                Assert.That(devices.CaptureSnapshot().Version, Is.EqualTo(2));
                var placed = devices.CaptureSnapshot().Records.Single();
                Assert.That(placed.EquipmentInstanceId, Is.EqualTo(canisterIdentity));
                cargo.State.TryAdd(new TopDown3DItemAmount(DustTool, 1, Guid.NewGuid().ToString("N"), TopDown3DCanisterContents.Create()));
                var spareIndex = Enumerable.Range(0, cargo.State.Capacity).Single(i => cargo.State.Slots[i].ItemId == DustTool);
                Assert.That(TopDown3DInventoryTransferService.EquipSlot(cargo.State, kit.State, spareIndex, () => true).Succeeded, Is.False);
                var collected = TopDown3DPlacedHarvesterRecord.Create(placement, placed.PlacementSequence, placed.Position, 2, 0, 20, canisterIdentity);
                var capture = devices.CaptureSnapshot();
                Assert.That(devices.ApplySnapshot(TopDown3DPlacedHarvesterSnapshot.Create(0, 20, capture.NextSequence, new[] { collected }, capture.AuthoredWorldId)), Is.True);
                Assert.That(devices.TryPickup(placement, kit.State, cargo.State, out var dust, () => true), Is.True);
                Assert.That(dust, Is.EqualTo(2)); Assert.That(kit.State.Slots[0].IsEmpty, Is.True);
                var returnedIndex = Enumerable.Range(0, cargo.State.Capacity).Single(i => cargo.State.Slots[i].InstanceId == canisterIdentity);
                Assert.That(cargo.State.Slots[returnedIndex].CanisterContents.dust, Is.EqualTo(2));
                Assert.That(kit.State.Slots.Any(slot => slot.ItemId == "resource.airborne_dust"), Is.False);
                Assert.That(devices.TryPlaceCanister(kit.transform.position, cargo.State, returnedIndex, () => true, out _), Is.True);
                var sequenceBeforeReset = devices.CaptureSnapshot().NextSequence;
                var initialIdentity = kit.State.Slots[5].InstanceId;
                Assert.That(TopDown3DInventoryTransferService.TransferSlot(kit.State, cargo.State, 5, () => true).Succeeded, Is.True);
                Assert.That(trial.RequestReset(), Does.Contain("Confirm reset"));
                Assert.That(trial.RequestReset(), Does.Contain("restored"));
                Assert.That(kit.State.Slots[5].InstanceId, Is.EqualTo(initialIdentity)); Assert.That(cargo.State.OccupiedSlotCount, Is.Zero);
                Assert.That(devices.DeployedCount, Is.Zero); Assert.That(kit.State.Slots[0].InstanceId, Is.EqualTo(canisterIdentity));
                Assert.That(devices.CaptureSnapshot().NextSequence, Is.GreaterThanOrEqualTo(sequenceBeforeReset));
                Assert.That(trial.RequestReset(), Does.Contain("Confirm reset")); Assert.That(trial.CancelConfirmation(), Is.True);
                trial.ExternalResetBlockReason = () => "Refinery owns equipment; use the coordinated economy reset.";
                Assert.That(trial.RequestReset(), Does.Contain("Refinery owns equipment")); Assert.That(trial.ConfirmationPending, Is.False);
            }
            finally { UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene); }
        }
        [Test] public void ReconstructionReconnectsBothSlotRegionsWithoutDuplicates()
        {
            var owner = new GameObject("Reconstruction fixture", typeof(RectTransform));
            try
            {
                var canvas = owner.AddComponent<TopDown3DInventoryCanvas>();
                canvas.InitializeDual(10, 12, _ => { }, _ => { }, _ => { }, _ => { }, () => { }); canvas.ConfigureLoadout(true);
                var originals = canvas.SlotViews.ToArray();
                var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                ((System.Collections.Generic.List<TopDown3DInventorySlotView>)typeof(TopDown3DInventoryCanvas).GetField("slotViews", flags).GetValue(canvas)).Clear();
                ((System.Collections.Generic.List<TopDown3DInventorySlotView>)typeof(TopDown3DInventoryCanvas).GetField("cargoSlotViews", flags).GetValue(canvas)).Clear();
                canvas.InitializeDual(10, 12, _ => { }, _ => { }, _ => { }, _ => { }, () => { }); canvas.ConfigureLoadout(true);
                Assert.That(canvas.SlotViews, Is.EqualTo(originals)); Assert.That(owner.GetComponentsInChildren<TopDown3DInventorySlotView>(true).Length, Is.EqualTo(22));
            }
            finally { UnityEngine.Object.DestroyImmediate(owner); }
        }
    }
}
