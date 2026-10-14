using System;
using System.Linq;
using System.Reflection;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace BooterBigArm.Tests
{
    public sealed class CampEconomySessionTests
    {
        private TopDown3DCampLoopEconomySession session;
        private TopDown3DAnimationPlaygroundSession lab;
        private string preferences;
        private static void Field(object owner, string name, object value) => owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(owner, value);
        private static void Call(object owner, string name, params object[] args) => owner.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(owner, args);
        [SetUp] public void Setup()
        {
            var scene = EditorSceneManager.OpenScene(GreaterWastelandSceneScope.CampLoopScene);
            session = CampWorkstationSceneAuthoring.All<TopDown3DCampLoopEconomySession>(scene).Single();
            session.Kit.GetComponent<TopDown3DPlayerMotor>().Configure(CampWorkstationSceneAuthoring.All<TopDown3DInputRouter>(scene).Single(),
                CampWorkstationSceneAuthoring.All<Camera>(scene).Single().transform);
            lab = TopDown3DAnimationPlaygroundSession.Find(scene); Assert.That(lab.CheckDependencies(), Is.True);
            preferences = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "CampEconomyFixture-" + Guid.NewGuid().ToString("N"));
            Assert.That(TopDown3DInventoryUiSceneInstaller.TryInstallForScene(scene, preferences), Is.Not.Null);
            Assert.That(lab.Advance(AnimationPlaygroundReadiness.ServicesBound), Is.True);
            Assert.That(lab.Advance(AnimationPlaygroundReadiness.VisualReady), Is.True);
            Assert.That(session.Initialize(), Is.True, session.FailureReason);
        }
        [TearDown] public void Cleanup()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            if (System.IO.Directory.Exists(preferences)) System.IO.Directory.Delete(preferences, true);
        }
        private int Ore => session.Cache.State.Slots.Where(s => s.ItemId == "resource.ironstone_ore").Sum(s => s.Quantity);
        private void AtBench()
        {
            var p = CampWorkstationSceneAuthoring.Supported(session.gameObject.scene, session.Cache.transform.position + new Vector3(-.8f, 0, -1.2f));
            session.Kit.transform.position = p + Vector3.up * .95f; Physics.SyncTransforms();
            Assert.That(session.Workbench.CanAccess(session.Kit.transform), Is.True, "One supported approach must reach both linked chest and workbench.");
        }
        [Test] public void AuthoredLoopConsumesRecipeOnceThenRefinesAndResetsWithoutDuplicatingIdentity()
        {
            Assert.That(Ore, Is.EqualTo(4)); var id = session.Kit.State.Slots[0].InstanceId;
            Assert.That(session.Initialize(), Is.True); Assert.That(Ore, Is.EqualTo(4)); AtBench();
            Assert.That(session.Workbench.Craft(session.Kit).Succeeded, Is.True); Assert.That(Ore, Is.Zero);
            Assert.That(session.Workbench.Craft(session.Kit).Succeeded, Is.False);
            var battery = Enumerable.Range(0, session.Kit.State.Capacity).Single(i => session.Kit.State.Slots[i].ItemId == TopDown3DDustRefineryState.EmptyBatteryId);
            Assert.That(session.Refinery.InsertBattery(session.Kit.State, battery, () => true).Succeeded, Is.True);
            Assert.That(session.Devices.TryPlaceCanister(session.Kit.transform.position, session.Kit.State, 0, () => true, out var placement), Is.True);
            Field(session.Devices, "simulationSeconds", 133d);
            Assert.That(session.Devices.TryPickup(placement, session.Kit.State, session.Cargo.State, out var dust, () => true), Is.True);
            Assert.That(dust, Is.EqualTo(13));
            var canister = Enumerable.Range(0, session.Cargo.State.Capacity).Single(i => session.Cargo.State.Slots[i].InstanceId == id);
            Assert.That(session.Refinery.InsertCanister(session.Cargo.State, canister, () => true).Succeeded, Is.True);
            Assert.That(session.Refinery.Refine(() => true).Succeeded, Is.True);
            Assert.That(session.Refinery.CaptureState().dust, Is.EqualTo(3));
            Assert.That(session.Refinery.RetrieveCanister(session.Kit.State, () => true).Succeeded, Is.False);
            Assert.That(session.ValidateOwnership(), Is.True);
            var trial = session.Kit.GetComponent<TopDown3DCampLoopInventoryTrial>(); session.Input.EnterInventoryMode();
            Assert.That(trial.RequestReset(), Does.Contain("Retrieve refinery items"));
            var epoch = session.Epoch; Assert.That(session.ResetEconomy(epoch).Succeeded, Is.True);
            Assert.That(session.Epoch, Is.EqualTo(epoch + 1)); Assert.That(Ore, Is.EqualTo(4));
            Assert.That(session.Kit.State.Slots[0].InstanceId, Is.EqualTo(id));
            Assert.That(session.Devices.DeployedCount, Is.Zero); Assert.That(session.Refinery.CaptureState().HasCanister, Is.False);
            Assert.That(session.ResetEconomy(epoch).Succeeded, Is.False);
            Assert.That(session.ValidateOwnership(), Is.True);
            Assert.That(trial.RequestReset(), Does.Contain("Confirm reset")); Assert.That(trial.RequestReset(), Does.Contain("restored"));
            Assert.That(Ore, Is.EqualTo(4)); Assert.That(session.Kit.State.Slots[0].InstanceId, Is.EqualTo(id));
        }
        [Test] public void StationMenuSuspendsResumesAndLostAccessClosesWithoutChangingContents()
        {
            AtBench(); var ui = session.Ui;
            Call(ui, "OpenTarget", session.Workbench); Assert.That(ui.IsOpen, Is.True); Assert.That(session.Action.IsBusy, Is.True);
            var menu = CampWorkstationSceneAuthoring.All<TopDown3DMenuController>(session.gameObject.scene).Single();
            menu.Toggle(); Assert.That(ui.IsSuspended, Is.True); Assert.That(session.Input.Mode, Is.EqualTo(TopDown3DInputMode.SystemMenu));
            menu.Close(); Assert.That(ui.IsSuspended, Is.False); Assert.That(ui.IsOpen, Is.True); Assert.That(session.Input.Mode, Is.EqualTo(TopDown3DInputMode.Inventory));
            // Commands remain guarded until the opening controls are released.
            Call(ui, "Command", "Craft"); Assert.That(Ore, Is.EqualTo(4));
            Call(ui, "Update");
            menu.Toggle(); session.Kit.transform.position += Vector3.right * 30; Physics.SyncTransforms(); menu.Close();
            Assert.That(ui.IsOpen, Is.False); Assert.That(session.Action.IsBusy, Is.False); Assert.That(Ore, Is.EqualTo(4));
        }
        [Test] public void ReenableUsesSameUiAndRetainsUsedEconomyRatherThanGrantingAgain()
        {
            var original = session.Ui; AtBench(); Assert.That(session.Workbench.Craft(session.Kit).Succeeded, Is.True);
            session.enabled = false; Call(session, "OnDisable"); Assert.That(session.Ready, Is.False); session.enabled = true;
            Assert.That(session.Initialize(), Is.True, session.FailureReason); Assert.That(session.Ui, Is.SameAs(original)); Assert.That(Ore, Is.Zero);
            Assert.That(session.gameObject.GetComponentsInChildren<TopDown3DCampWorkstationUiController>(true).Length, Is.EqualTo(1));
            Assert.That(session.Kit.State.Slots.Count(s => s.ItemId == TopDown3DDustRefineryState.EmptyBatteryId), Is.EqualTo(1));
        }
        [Test] public void StartupCapabilityPreparationRejectsOneInvalidOwnerWithoutMutatingAnother()
        {
            var a = new TopDown3DInventoryState(session.Kit.ItemCatalog, new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.BigArm, 2));
            var b = new TopDown3DInventoryState(session.Kit.ItemCatalog, new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.BigArm, 2));
            a.TryAdd(new TopDown3DItemAmount(TopDown3DHarvesterSettings.CanisterItemId, 1));
            var before = JsonUtility.ToJson(a.CaptureSnapshot()); var prepared = a.PrepareCanisterContentsAtStartup();
            b.RetainForRecovery(); Assert.That(b.PrepareCanisterContentsAtStartup(), Is.Null);
            Assert.That(JsonUtility.ToJson(a.CaptureSnapshot()), Is.EqualTo(before)); Assert.That(a.Policy.SupportsCanisterContents, Is.False);
            Assert.That(prepared, Is.Not.Null);
        }
        [Test] public void HeldGamepadSubmitCannotCraftUntilReleased()
        {
            AtBench(); var pad = InputSystem.AddDevice<Gamepad>();
            try
            {
                InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.South)); InputSystem.Update();
                Call(session.Ui, "OpenTarget", session.Workbench); Call(session.Ui, "Update"); Call(session.Ui, "Command", "Craft");
                Assert.That(Ore, Is.EqualTo(4));
                InputSystem.QueueStateEvent(pad, new GamepadState()); InputSystem.Update(); Call(session.Ui, "Update");
                Call(session.Ui, "Command", "Craft"); Assert.That(Ore, Is.Zero);
                Assert.That(session.Kit.State.Slots.Count(s => s.ItemId == TopDown3DDustRefineryState.EmptyBatteryId), Is.EqualTo(1));
            }
            finally { InputSystem.RemoveDevice(pad); }
        }
        [Test] public void RetainedMissingStarterBlocksRegrantAndLeavesUsedItemsIntact()
        {
            AtBench(); Assert.That(session.Workbench.Craft(session.Kit).Succeeded, Is.True);
            var mutation = new TopDown3DInventoryTransaction.Mutation(session.Kit.State);
            Assert.That(mutation.Draft.RemoveFromSlot(0, 1).Succeeded, Is.True);
            Assert.That(TopDown3DInventoryTransaction.Commit(new[] { mutation }).Succeeded, Is.True);
            session.enabled = false; Call(session, "OnDisable"); session.enabled = true;
            Assert.That(session.Initialize(), Is.False); Assert.That(session.Ready, Is.False); Assert.That(Ore, Is.Zero);
            Assert.That(session.Kit.State.Slots.Count(s => s.ItemId == TopDown3DDustRefineryState.EmptyBatteryId), Is.EqualTo(1));
        }
        [Test] public void InvalidRetainedDeviceSnapshotBlocksEconomyWithoutDeletingSnapshot()
        {
            var snapshot = session.Devices.CaptureSnapshot();
            var invalid = TopDown3DPlacedHarvesterSnapshot.Create(0, snapshot.SimulationSeconds, snapshot.NextSequence,
                Array.Empty<TopDown3DPlacedHarvesterRecord>(), "wrong-world");
            Field(session.Devices, "reloadSnapshot", invalid);
            Assert.That(session.Devices.RestoreRetainedState(), Is.False); Assert.That(session.Devices.HasRetainedRecovery, Is.True);
            session.enabled = false; Call(session, "OnDisable"); session.enabled = true;
            Assert.That(session.Initialize(), Is.False); Assert.That(Ore, Is.EqualTo(4));
            Assert.That(typeof(TopDown3DPlacedHarvesterState).GetField("reloadSnapshot", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session.Devices), Is.SameAs(invalid));
        }
    }
}
