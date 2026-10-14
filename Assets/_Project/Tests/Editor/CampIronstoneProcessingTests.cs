using System;
using System.Linq;
using System.Reflection;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace BooterBigArm.Tests
{
    public sealed class CampIronstoneProcessingTests
    {
        private TopDown3DCampLoopEconomySession session;
        private TopDown3DIronstoneProcessor processor;
        private string preferences;
        private TopDown3DInventoryState Source => session.Cache.State;
        private TopDown3DInventoryState Tray => processor.Output.State;
        private static int Count(TopDown3DInventoryState state, string id) => state.Slots.Where(s => s.ItemId == id).Sum(s => s.Quantity);
        private static string Snapshot(TopDown3DInventoryState state) => JsonUtility.ToJson(state.CaptureSnapshot());
        private static void Field(object owner, string name, object value) => owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(owner, value);
        private static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(owner, args);
        [SetUp] public void Setup()
        {
            var scene = EditorSceneManager.OpenScene(GreaterWastelandSceneScope.CampLoopScene);
            session = CampWorkstationSceneAuthoring.All<TopDown3DCampLoopEconomySession>(scene).Single();
            processor = session.Site.Processor;
            var motor = session.Kit.GetComponent<TopDown3DPlayerMotor>();
            motor.Configure(CampWorkstationSceneAuthoring.All<TopDown3DInputRouter>(scene).Single(), CampWorkstationSceneAuthoring.All<Camera>(scene).Single().transform);
            var lab = TopDown3DAnimationPlaygroundSession.Find(scene); Assert.That(lab.CheckDependencies(), Is.True);
            preferences = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "CampIronstone-" + Guid.NewGuid().ToString("N"));
            Assert.That(TopDown3DInventoryUiSceneInstaller.TryInstallForScene(scene, preferences), Is.Not.Null);
            Assert.That(lab.Advance(AnimationPlaygroundReadiness.ServicesBound), Is.True);
            Assert.That(lab.Advance(AnimationPlaygroundReadiness.VisualReady), Is.True);
            Assert.That(session.Initialize(), Is.True, session.FailureReason);
        }
        [TearDown] public void Cleanup()
        { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene); if (System.IO.Directory.Exists(preferences)) System.IO.Directory.Delete(preferences, true); }
        [Test] public void FreshCampHasFourOwnersThreeStationsAndOnlyOldFourOreSeed()
        {
            Assert.That(session.Site.Registry.Stores.Count, Is.EqualTo(4)); Assert.That(session.Site.Registry.Stations.Count, Is.EqualTo(3));
            Assert.That(session.Site.WorldId, Is.EqualTo("greater-wasteland.authored.v1")); Assert.That(session.Site.SiteId, Is.EqualTo("camp.starting"));
            Assert.That(Count(Source, TopDown3DProcessRecipe.Ore), Is.EqualTo(4)); Assert.That(Tray.Capacity, Is.EqualTo(8));
            Assert.That(Tray.OccupiedSlotCount, Is.Zero); Assert.That(session.AcceptsTransfers(3), Is.False);
            Assert.That(Tray.TryAdd(new TopDown3DItemAmount(TopDown3DDustRefineryState.EmptyBatteryId,1)).Succeeded, Is.False);
        }
        [Test] public void CompleteBatchPublishesAllOwnersAndCannotBeCommittedTwice()
        {
            Assert.That(TopDown3DProcessingService.Prepare(processor,Source,()=>true,out var quote).Succeeded, Is.True);
            var observed = false;
            Source.Changed += () => { observed = true; Assert.That(processor.Revision, Is.EqualTo(1)); Assert.That(Count(Tray,TopDown3DProcessRecipe.Products[3]),Is.EqualTo(2));
                Assert.That(Source.TryAdd(new TopDown3DItemAmount(TopDown3DProcessRecipe.Ore,1)).Succeeded, Is.False); };
            Assert.That(quote.Commit().Succeeded, Is.True); Assert.That(observed,Is.True);
            Assert.That(Count(Source,TopDown3DProcessRecipe.Ore),Is.Zero);
            Assert.That(Tray.CalculateMass(),Is.EqualTo(5).Within(.0001));
            Assert.That(quote.Commit().Succeeded,Is.False); Assert.That(processor.Revision,Is.EqualTo(1));
        }
        [Test] public void FullTrayRejectsEveryResultWithoutConsumingOre()
        {
            foreach(var id in TopDown3DProcessRecipe.Products) Assert.That(Tray.TryAdd(new TopDown3DItemAmount(id,198)).Succeeded,Is.True);
            var before=Snapshot(Tray); var source=Snapshot(Source);
            Assert.That(TopDown3DProcessingService.Prepare(processor,Source,()=>true,out var quote).Succeeded,Is.False);
            Assert.That(quote,Is.Null); Assert.That(Snapshot(Tray),Is.EqualTo(before)); Assert.That(Snapshot(Source),Is.EqualTo(source)); Assert.That(processor.Revision,Is.Zero);
        }
        [Test] public void MissingIngredientRejectsWithoutOutputs()
        {
            Source.TryRemove(TopDown3DProcessRecipe.Ore,1); var before=Snapshot(Source);
            Assert.That(TopDown3DProcessingService.Prepare(processor,Source,()=>true,out _).Succeeded,Is.False);
            Assert.That(Snapshot(Source),Is.EqualTo(before)); Assert.That(Tray.OccupiedSlotCount,Is.Zero);
        }
        [TestCase(true)] [TestCase(false)] public void LostAccessOrStaleSourceRejectsPreparedBatch(bool loseAccess)
        {
            var access=true; Assert.That(TopDown3DProcessingService.Prepare(processor,Source,()=>access,out var quote).Succeeded,Is.True);
            if(loseAccess) access=false; else Source.TryAdd(new TopDown3DItemAmount(TopDown3DProcessRecipe.Ore,1));
            var before=Snapshot(Source); Assert.That(quote.Commit().Succeeded,Is.False);
            Assert.That(Snapshot(Source),Is.EqualTo(before)); Assert.That(Tray.OccupiedSlotCount,Is.Zero); Assert.That(processor.Revision,Is.Zero);
        }
        [Test] public void BalancedRecipeEditWithoutVersionBumpRejectsOldQuote()
        {
            var original=processor.Recipe; var recipe=UnityEngine.Object.Instantiate(original); processor.Configure(recipe);
            try
            {
                Assert.That(TopDown3DProcessingService.Prepare(processor,Source,()=>true,out var quote).Succeeded,Is.True);
                recipe.Configure(original.Inputs.ToArray(), new[] { new TopDown3DCraftingRecipe.Ingredient(TopDown3DProcessRecipe.Products[0],1),
                    new TopDown3DCraftingRecipe.Ingredient(TopDown3DProcessRecipe.Products[1],1),new TopDown3DCraftingRecipe.Ingredient(TopDown3DProcessRecipe.Products[2],1),
                    new TopDown3DCraftingRecipe.Ingredient(TopDown3DProcessRecipe.Products[3],5) });
                Assert.That(recipe.TryFingerprint(Source.ItemCatalog,out _,out _),Is.True);
                Assert.That(quote.Commit().Succeeded,Is.False); Assert.That(Count(Source,TopDown3DProcessRecipe.Ore),Is.EqualTo(4)); Assert.That(Tray.OccupiedSlotCount,Is.Zero);
            }
            finally { processor.Configure(original); UnityEngine.Object.DestroyImmediate(recipe); }
        }
        [Test] public void ExactStackRejectsPartlyFittingDestinationAndLegacyTransferStillMovesMaximum()
        {
            Tray.TryAdd(new TopDown3DItemAmount(TopDown3DProcessRecipe.Products[0],2));
            var destination=new TopDown3DInventoryState(Source.ItemCatalog,new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.ResourceCache,1));
            destination.TryAdd(new TopDown3DItemAmount(TopDown3DProcessRecipe.Products[0],98)); var before=Snapshot(Tray);
            Assert.That(TopDown3DInventoryTransferService.TransferExactSlot(Tray,destination,0,Tray.Revision,destination.Revision,()=>true).Succeeded,Is.False);
            Assert.That(Snapshot(Tray),Is.EqualTo(before)); Assert.That(Count(destination,TopDown3DProcessRecipe.Products[0]),Is.EqualTo(98));
            Assert.That(TopDown3DInventoryTransferService.TransferSlot(Tray,destination,0,()=>true).Quantity,Is.EqualTo(1));
        }
        [Test] public void BothResetsIncludeTrayAndKeepEconomyScopeSeparateFromFullTrial()
        {
            TopDown3DProcessingService.Prepare(processor,Source,()=>true,out var quote); Assert.That(quote.Commit().Succeeded,Is.True);
            session.Cargo.State.TryAdd(new TopDown3DItemAmount(TopDown3DProcessRecipe.Ore,3));
            session.Cargo.State.TryAdd(new TopDown3DItemAmount(TopDown3DProcessRecipe.Products[0],2));
            session.Input.EnterInventoryMode(); Assert.That(session.ResetEconomy(session.Epoch).Succeeded,Is.True);
            Assert.That(Tray.OccupiedSlotCount,Is.Zero); Assert.That(Count(session.Cargo.State,TopDown3DProcessRecipe.Products[0]),Is.Zero);
            Assert.That(Count(session.Cargo.State,TopDown3DProcessRecipe.Ore),Is.EqualTo(3)); Assert.That(Count(Source,TopDown3DProcessRecipe.Ore),Is.EqualTo(4));
            Tray.TryAdd(new TopDown3DItemAmount(TopDown3DProcessRecipe.Products[2],1));
            var trial=session.Kit.GetComponent<TopDown3DCampLoopInventoryTrial>();
            Assert.That(trial.RequestReset(),Does.Contain("Confirm reset")); Assert.That(trial.RequestReset(),Does.Contain("restored"));
            Assert.That(Tray.OccupiedSlotCount,Is.Zero); Assert.That(Count(session.Cargo.State,TopDown3DProcessRecipe.Ore),Is.Zero);
        }
        [Test] public void ResetConfirmationRejectsChangedTray()
        {
            session.Input.EnterInventoryMode(); var trial=session.Kit.GetComponent<TopDown3DCampLoopInventoryTrial>();
            Assert.That(trial.RequestReset(),Does.Contain("Confirm reset")); Tray.TryAdd(new TopDown3DItemAmount(TopDown3DProcessRecipe.Products[2],1));
            Assert.That(trial.RequestReset(),Does.Contain("changed")); Assert.That(Tray.OccupiedSlotCount,Is.EqualTo(1));
        }
        [Test] public void DisableInvalidatesRegistryAndOldCommandsWithoutReseeding()
        {
            var registry=session.Site.Registry; var epoch=session.Epoch;
            TopDown3DProcessingService.Prepare(processor,Source,()=>registry.Active && session.Epoch==epoch,out var quote);
            session.enabled=false; Call(session,"OnDisable"); session.enabled=true; Assert.That(session.Initialize(),Is.True,session.FailureReason);
            Assert.That(session.Site.Registry.SessionToken,Is.Not.EqualTo(registry.SessionToken)); Assert.That(quote.Commit().Succeeded,Is.False);
            Assert.That(Count(Source,TopDown3DProcessRecipe.Ore),Is.EqualTo(4)); Assert.That(Tray.OccupiedSlotCount,Is.Zero);
        }
        [Test] public void CorruptRetentionIsPreservedAndProcessingCannotActivate()
        {
            Field(processor.Output,"retainedJson","{\"version\":99,\"present\":true}"); processor.Output.OnAfterDeserialize();
            Assert.That(processor.Output.Bind(session,processor,session.Site.WorldId,session.Site.SiteId),Is.False);
            Assert.That(processor.Output.Ready,Is.False); Assert.That(processor.Output.GetType().GetField("retainedJson",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(processor.Output),
                Is.EqualTo("{\"version\":99,\"present\":true}")); Assert.That(Count(Source,TopDown3DProcessRecipe.Ore),Is.EqualTo(4));
        }
        [Test] public void RegistryRejectsAliasAndCrossSceneOwner()
        {
            var registry=new TopDown3DCampRegistry(session.gameObject.scene,session.Site.WorldId,session.Site.SiteId);
            registry.Add(new TopDown3DCampStoreBinding("one","ONE",session.Cache,Source,true,()=>true));
            Assert.Throws<ArgumentException>(()=>registry.Add(new TopDown3DCampStoreBinding("two","TWO",session.Cache,Source,true,()=>true)));
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive); var owner=new GameObject("Wrong scene").AddComponent<TopDown3DStationResourceStore>();
            Assert.Throws<ArgumentException>(()=>registry.Add(new TopDown3DCampStoreBinding("other","OTHER",owner,Tray,false,()=>true)));
            EditorSceneManager.CloseScene(scene,true);
        }
        private void AtSorter()
        {
            session.Kit.transform.position = CampWorkstationSceneAuthoring.Supported(session.gameObject.scene,
                (processor.transform.position+session.Cache.transform.position)*.5f)+Vector3.up*.95f;
            Physics.SyncTransforms();
            Assert.That(processor.CanAccess(session.Kit.transform),Is.True,"Supported common loading approach must reach sorter.");
            Assert.That(session.Cache.CanAccess(session.Kit.transform),Is.True,"Supported common loading approach must reach chest.");
        }
        [Test] public void PhysicalUiRequiresFreshBatchConfirmationAndTakeMovesExactStack()
        {
            AtSorter(); var ui=session.Ui; Call(ui,"OpenTarget",processor); Assert.That(ui.IsOpen,Is.True); Call(ui,"Update");
            Call(ui,"Command","Sort"); Assert.That(Tray.OccupiedSlotCount,Is.Zero);
            Call(ui,"Command","Sort"); Assert.That(Tray.OccupiedSlotCount,Is.Zero,"Held second submit is blocked.");
            Call(ui,"Update"); Call(ui,"Command","Sort"); Assert.That(Tray.OccupiedSlotCount,Is.EqualTo(4));
            Call(ui,"Update"); Call(ui,"Command","Source"); // chest -> output tray
            session.Cargo.transform.position=session.Kit.transform.position+Vector3.left*1.4f; Physics.SyncTransforms();
            Call(ui,"Command","Destination"); Call(ui,"Select",0); Call(ui,"Command","Take");
            Assert.That(Count(session.Cargo.State,TopDown3DProcessRecipe.Products[0]),Is.EqualTo(2)); Assert.That(Count(Tray,TopDown3DProcessRecipe.Products[0]),Is.Zero);
        }
        [Test] public void MenuSuspendsManagementAndDiscardsPreparedProcessQuote()
        {
            AtSorter(); var ui=session.Ui; Call(ui,"OpenTarget",processor); Call(ui,"Update"); Call(ui,"Command","Sort");
            var menu=CampWorkstationSceneAuthoring.All<TopDown3DMenuController>(session.gameObject.scene).Single();
            menu.Toggle(); Assert.That(ui.IsSuspended,Is.True); menu.Close(); Assert.That(ui.IsOpen,Is.True);
            Call(ui,"Update"); Call(ui,"Command","Sort"); Assert.That(Tray.OccupiedSlotCount,Is.Zero,"Resume prepares a new quote instead of committing the old one.");
        }
        [Test] public void CampOverviewCannotDepositIntoOutputTray()
        {
            AtSorter(); var ui=session.Ui; Call(ui,"OpenTarget",processor); Call(ui,"Update"); Call(ui,"Command","OverviewItems");
            for(var i=0;i<8;i++) { Call(ui,"Command","Destination"); var destination=(int)ui.GetType().GetField("destination",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(ui);
                Assert.That(destination,Is.Not.EqualTo(3)); }
            Assert.That(Tray.OccupiedSlotCount,Is.Zero);
        }
        [Test] public void OutOfReachStationNavigationCannotAcquireRemoteTarget()
        {
            AtSorter(); var ui=session.Ui; Call(ui,"OpenTarget",processor); Call(ui,"Update"); Call(ui,"Command","OverviewStations");
            session.Workbench.transform.position+=Vector3.right*30;
            Call(ui,"Command","Station0"); var target=ui.GetType().GetField("target",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(ui);
            Assert.That(target,Is.SameAs(processor)); Assert.That(Count(Source,TopDown3DProcessRecipe.Ore),Is.EqualTo(4));
        }
        [TestCase("focus")] [TestCase("damage")] [TestCase("device")] public void InterruptionClosesModalAndPreservesPreparedStock(string interruption)
        {
            AtSorter(); var ui=session.Ui; Call(ui,"OpenTarget",processor); Call(ui,"Update"); Call(ui,"Command","Sort");
            if(interruption=="focus") Call(ui,"OnApplicationFocus",false);
            else if(interruption=="damage") Call(ui,"Interrupt");
            else Call(ui,"DeviceChanged",null,UnityEngine.InputSystem.InputDeviceChange.Disconnected);
            Assert.That(ui.IsOpen,Is.False); Assert.That(session.Input.Mode,Is.EqualTo(TopDown3DInputMode.Gameplay));
            Assert.That(Count(Source,TopDown3DProcessRecipe.Ore),Is.EqualTo(4)); Assert.That(Tray.OccupiedSlotCount,Is.Zero);
        }
        [Test] public void SerializedTrayRoundTripPreservesProductsAndRejectsOldStateQuote()
        {
            TopDown3DProcessingService.Prepare(processor,Source,()=>true,out var batch); Assert.That(batch.Commit().Succeeded,Is.True);
            Source.TryAdd(new TopDown3DItemAmount(TopDown3DProcessRecipe.Ore,4));
            TopDown3DProcessingService.Prepare(processor,Source,()=>true,out var oldQuote);
            var saved=processor.Output.CaptureRetained(); var contents=Snapshot(Tray); processor.Output.OnAfterDeserialize();
            Assert.That(processor.Output.Bind(session,processor,session.Site.WorldId,session.Site.SiteId),Is.True);
            Assert.That(Snapshot(Tray),Is.EqualTo(contents)); Assert.That(processor.Revision,Is.EqualTo(1)); Assert.That(oldQuote.Commit().Succeeded,Is.False);
            Assert.That(processor.Output.CaptureRetained(),Is.EqualTo(saved));
        }
        [Test] public void OneBlockedResetParticipantLeavesEveryOwnerIntact()
        {
            Tray.TryAdd(new TopDown3DItemAmount(TopDown3DProcessRecipe.Products[2],2)); var tray=Snapshot(Tray); var source=Snapshot(Source);
            var trial=session.Kit.GetComponent<TopDown3DCampLoopInventoryTrial>();
            trial.RegisterResetParticipant("fixture.blocked",()=>"Fixture owner unavailable"); session.Input.EnterInventoryMode();
            Assert.That(trial.RequestReset(),Does.Contain("Fixture owner unavailable")); Assert.That(Snapshot(Tray),Is.EqualTo(tray)); Assert.That(Snapshot(Source),Is.EqualTo(source));
        }
        [Test] public void ProcessorRevisionExhaustionRejectsWithoutConsumption()
        {
            processor.RestoreRevision(long.MaxValue); Assert.That(TopDown3DProcessingService.Prepare(processor,Source,()=>true,out _).Succeeded,Is.False);
            Assert.That(Count(Source,TopDown3DProcessRecipe.Ore),Is.EqualTo(4)); Assert.That(Tray.OccupiedSlotCount,Is.Zero);
        }
        [Test] public void MalformedRetainedBaselineBlocksFreshGrantBeforeInitialization()
        {
            Source.TryRemove(TopDown3DProcessRecipe.Ore,4);
            var trial=session.Kit.GetComponent<TopDown3DCampLoopInventoryTrial>();
            Field(trial,"baselineJson","{bad baseline"); Field(trial,"captured",false);
            session.enabled=false; Call(session,"OnDisable"); session.enabled=true;
            Field(session,"initialized",false); var before=Snapshot(Source);
            LogAssert.Expect(LogType.Warning,"Retained trial baseline cannot be decoded; its bytes were preserved.");
            Assert.That(session.Initialize(),Is.False); Assert.That(Snapshot(Source),Is.EqualTo(before)); Assert.That(Count(Source,TopDown3DProcessRecipe.Ore),Is.Zero);
        }
        [Test] public void MountedCompanionCannotReceiveCampCall()
        {
            var riding=CampWorkstationSceneAuthoring.All<TopDown3DLeggerRidingController>(session.gameObject.scene).Single();
            var follower=session.Cargo.GetComponent<TopDown3DBigArmFollower>();
            Assert.That(follower.BindRiding(riding),Is.True); riding.State.BeginMount();
            Assert.That(TopDown3DCampCompanionStatus.CanCall(session),Is.False); Assert.That(TopDown3DCampCompanionStatus.Describe(session),Does.Contain("Mounted"));
            riding.State.Clear(); follower.UnbindRiding(riding);
        }
        [Test] public void HiddenStationCommandCannotExecuteFromOverview()
        {
            var p=CampWorkstationSceneAuthoring.Supported(session.gameObject.scene,session.Cache.transform.position+new Vector3(-.8f,0,-1.2f));
            session.Kit.transform.position=p+Vector3.up*.95f; Physics.SyncTransforms();
            var ui=session.Ui; Call(ui,"OpenTarget",session.Workbench); Call(ui,"Update"); Call(ui,"Command","OverviewItems");
            Call(ui,"Command","Craft"); Assert.That(Count(Source,TopDown3DProcessRecipe.Ore),Is.EqualTo(4));
        }
        [Test] public void DisabledOutputOwnerCannotProcessAndLegacyWorkbenchRemainsAvailable()
        {
            processor.Output.enabled=false;
            Assert.That(TopDown3DProcessingService.Prepare(processor,Source,()=>true,out _).Succeeded,Is.False);
            Assert.That(session.Ready,Is.True); Assert.That(Count(Source,TopDown3DProcessRecipe.Ore),Is.EqualTo(4));
            var p=CampWorkstationSceneAuthoring.Supported(session.gameObject.scene,session.Cache.transform.position+new Vector3(-.8f,0,-1.2f));
            session.Kit.transform.position=p+Vector3.up*.95f; Physics.SyncTransforms();
            Assert.That(session.Workbench.Craft(session.Kit).Succeeded,Is.True);
        }
        [TestCase("unknown")] [TestCase("duplicate")] [TestCase("mass")] [TestCase("overflow")]
        public void InvalidRecipeDefinitionsRejectBeforeAnyStockDraftIsInstalled(string invalid)
        {
            var original=processor.Recipe; var recipe=UnityEngine.Object.Instantiate(original); processor.Configure(recipe);
            try
            {
                var inputs=original.Inputs.ToArray(); var outputs=original.Outputs.ToArray();
                if(invalid=="unknown")outputs[0].itemId="resource.unknown";
                else if(invalid=="duplicate")outputs[1].itemId=outputs[0].itemId;
                else if(invalid=="mass")outputs[0].quantity=3;
                else inputs[0].quantity=int.MaxValue;
                recipe.Configure(inputs,outputs);var source=Snapshot(Source);
                Assert.That(recipe.TryFingerprint(Source.ItemCatalog,out _,out _),Is.False);
                Assert.That(TopDown3DProcessingService.Prepare(processor,Source,()=>true,out _).Succeeded,Is.False);
                Assert.That(Snapshot(Source),Is.EqualTo(source)); Assert.That(Tray.OccupiedSlotCount,Is.Zero);
            }
            finally{processor.Configure(original);UnityEngine.Object.DestroyImmediate(recipe);}
        }
        [Test] public void RemovedProcessorComponentClosesViewAndRetainsItsTrayWithoutThrowing()
        {
            AtSorter(); var output=processor.Output; var ui=session.Ui;
            output.State.TryAdd(new TopDown3DItemAmount(TopDown3DProcessRecipe.Products[0],2)); var before=Snapshot(output.State);
            Call(ui,"OpenTarget",processor); Assert.That(ui.IsOpen,Is.True);
            UnityEngine.Object.DestroyImmediate(processor); Call(ui,"Update");
            Assert.That(ui.IsOpen,Is.False); Assert.That(Snapshot(output.State),Is.EqualTo(before));
            session.Input.EnterInventoryMode(); Assert.That(session.ResetEconomy(session.Epoch).Succeeded,Is.False);
            Assert.That(Snapshot(output.State),Is.EqualTo(before));
        }
    }
}
