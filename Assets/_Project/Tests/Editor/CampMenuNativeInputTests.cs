using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock.LowLevel;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace BooterBigArm.Tests
{
    // Menu-only EditMode checks: no session startup, world, profile or Play Mode.
    public sealed class CampMenuNativeInputTests
    {
        private CampExpeditionGamepadTests fixture;
        private Gamepad native;
        private CampExpeditionLedger departureLedger;
        private MemoryFile departureFile;
        private const BindingFlags Fields=BindingFlags.Instance|BindingFlags.NonPublic;
        private T Get<T>(string name)=>(T)typeof(CampExpeditionGamepadTests).GetField(name,Fields).GetValue(fixture);

        [SetUp] public void Setup(){fixture=new CampExpeditionGamepadTests();fixture.Setup();}
        [TearDown] public void Cleanup()
        {
            fixture?.Cleanup();
            if(native!=null&&native.added)InputSystem.RemoveDevice(native);
        }

        [TestCase("DualSenseGamepadHID")]
        [TestCase("XInputControllerWindows")]
        public void NativeReportsSwitchTabsAndSubmitPlay(string layout)
        {
            native=(Gamepad)InputSystem.AddDevice(layout);
            Get<InputActionAsset>("actions").devices=new InputDevice[]{native,Get<Keyboard>("keyboard")};
            var view=Get<CampExpeditionMenuView>("view");
            Send(layout);Send(layout,shoulders:2);
            Assert.That(view.Page,Is.EqualTo("storage"),"Right bumper must reach Outpost.");
            Send(layout);Send(layout,shoulders:2);
            Assert.That(view.Page,Is.EqualTo("workstations"),"Crafting must start on Workshops.");
            Send(layout);Send(layout,trigger:255);
            Assert.That(view.Page,Is.EqualTo("tree"),"Right trigger must reach Crafting tree.");
            view.ShowPage("play");Send(layout);Send(layout,south:true);
            Assert.That(Get<List<string>>("commands").Count(c=>c=="play"),Is.EqualTo(1));
        }

        [TestCase("DualSenseGamepadHID")]
        [TestCase("XInputControllerWindows")]
        public void CargoReviewPlayIsTheOnlyPageActionAndSouthAttemptsDeparture(string layout)
        {
            PrepareCargoReview();
            var session=Get<CampExpeditionSession>("session");
            var view=Get<CampExpeditionMenuView>("view");
            var play=view.GetComponentsInChildren<CampMenuControl>().Single(c=>c.key=="play");
            Assert.That(play.Button.IsInteractable(),Is.True,"Cargo review must not silently disable the Play page.");
            CollectionAssert.AreEquivalent(new[]{"play"},view.GetComponentsInChildren<CampMenuControl>()
                .Where(c=>!c.tab&&!c.prompt&&c.Button.IsInteractable()).Select(c=>c.key));
            var before=JsonUtility.ToJson(departureLedger.State);
            view.Command+=key=>typeof(CampExpeditionSession).GetMethod("Command",Fields).Invoke(session,new object[]{key});
            native=(Gamepad)InputSystem.AddDevice(layout);
            Get<InputActionAsset>("actions").devices=new InputDevice[]{native};
            Send(layout);Send(layout,south:true);
            // Deliberately omit world actors. Reaching the arrival failure proves that
            // native South entered BeginRun; it never starts gameplay or writes a profile.
            Assert.That(typeof(CampExpeditionSession).GetField("message",Fields).GetValue(session),
                Is.EqualTo("No supported arrival was found; town state was retained."));
            Assert.That(JsonUtility.ToJson(departureLedger.State),Is.EqualTo(before));
            Assert.That(departureFile.writes,Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DepartureFromReviewKeepsCargoEquipmentAndFundsAndRequiresDurableSave(bool failSave)
        {
            PrepareCargoReview();var before=departureLedger.State;
            departureFile.fail=failSave;
            var operation=CampExpeditionSession.PrepareDeparture(departureLedger,new CampExpeditionRunRecord{id="next-run",seed=427});
            Assert.That(departureLedger.Commit(operation,out _),Is.EqualTo(!failSave));
            var after=departureLedger.State;
            Assert.That(after.phase,Is.EqualTo(failSave?CampExpeditionPhase.CargoReview:CampExpeditionPhase.Exploring));
            Assert.That(after.commons,Is.EqualTo(before.commons));
            Assert.That(JsonUtility.ToJson(after.booter),Is.EqualTo(JsonUtility.ToJson(before.booter)));
            Assert.That(JsonUtility.ToJson(after.legger),Is.EqualTo(JsonUtility.ToJson(before.legger)));
            Assert.That(JsonUtility.ToJson(after.storage),Is.EqualTo(JsonUtility.ToJson(before.storage)));
            Assert.That(after.sequence,Is.EqualTo(before.sequence+(failSave?0:1)));
            Assert.That(after.run?.id,Is.EqualTo(failSave?null:"next-run"));
        }

        [TestCase("review")][TestCase("inventory")][TestCase("storage")][TestCase("tree")]
        [TestCase("workstations")][TestCase("tracked")][TestCase("projects")]
        public void CargoReviewBackChangesOnlyThePageAndRetainsAllRecoveredPossessions(string page)
        {
            PrepareCargoReview();var session=Get<CampExpeditionSession>("session");var view=Get<CampExpeditionMenuView>("view");view.ShowPage(page);
            string before=JsonUtility.ToJson(departureLedger.State),saved=JsonUtility.ToJson(departureFile.saved);int writes=departureFile.writes;
            System.Action<string> route=key=>typeof(CampExpeditionSession).GetMethod("Command",Fields).Invoke(session,new object[]{key});view.Command+=route;
            try{view.Back();Assert.That(view.Page,Is.EqualTo("play"));Assert.That(JsonUtility.ToJson(departureLedger.State),Is.EqualTo(before));Assert.That(departureLedger.State.phase,Is.EqualTo(CampExpeditionPhase.CargoReview));Assert.That(JsonUtility.ToJson(departureFile.saved),Is.EqualTo(saved));Assert.That(departureFile.writes,Is.EqualTo(writes));}
            finally{view.Command-=route;}
        }

        private void PrepareCargoReview()
        {
            var configuration=Resources.Load<CampExpeditionConfiguration>("CampExpeditionConfiguration");
            var catalog=configuration.definition.itemCatalog;
            var booter=new TopDown3DInventoryState(catalog,new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.Booter,CampExpeditionLayout.Capacity,expeditionLayout:true));
            var cargo=new TopDown3DInventoryState(catalog,new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.BigArm,12));
            var storage=new TopDown3DInventoryState(catalog,new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.ResourceCache,24,campStorage:true));
            Assert.That(booter.TryAdd(new TopDown3DItemAmount(CampExpeditionDefinition.ToolZeroId,1)).Succeeded,Is.True);
            Assert.That(cargo.TryAdd(new TopDown3DItemAmount("author.bio.stone_bone",1)).Succeeded,Is.True);
            var snapshot=new CampExpeditionSnapshot{phase=CampExpeditionPhase.CargoReview,sequence=1,commons=50,profileId="menu-review-fixture",contentFingerprint="test",storageCapacity=24,booter=booter.CaptureSnapshot(),legger=cargo.CaptureSnapshot(),storage=storage.CaptureSnapshot()};
            departureFile=new MemoryFile{saved=snapshot.Clone()};
            departureLedger=new CampExpeditionLedger(departureFile,snapshot,booter,cargo,storage);
            var session=Get<CampExpeditionSession>("session");
            typeof(CampExpeditionSession).GetProperty("Configuration").SetValue(session,configuration);
            typeof(CampExpeditionSession).GetField("ledger",Fields).SetValue(session,departureLedger);
            typeof(CampExpeditionSession).GetField("snapshot",Fields).SetValue(session,snapshot);
            typeof(CampExpeditionSession).GetField("vitals",Fields).SetValue(session,session.gameObject.AddComponent<TopDown3DSurvivalVitals>());
            typeof(CampExpeditionSession).GetMethod("Render",Fields).Invoke(session,null);
        }

        private sealed class MemoryFile:ICampExpeditionFile
        {
            public CampExpeditionSnapshot saved;public bool fail;public int writes;
            public CampExpeditionSnapshot Read()=>saved?.Clone();
            public void Write(CampExpeditionSnapshot value)
            {writes++;if(fail)throw new System.IO.IOException("Injected departure save failure");saved=value.Clone();}
        }

        [Test] public void ReenteringInventoryEnablesActionsAddedWhileTheMenuWasOpen()
        {
            var router=Get<TopDown3DInputRouter>("router");
            var actions=Get<InputActionAsset>("actions");
            Assert.That(router.Mode,Is.EqualTo(TopDown3DInputMode.Inventory));
            // Preparation restores individual prior states; a newly introduced action
            // therefore starts disabled even if the router was already in Inventory.
            router.EditExpeditionMenuActions(map=>map.AddAction("MenuReentryFixture",InputActionType.Button,"<Gamepad>/rightTrigger"));
            var introduced=actions.FindAction("UI/MenuReentryFixture",true);
            Assert.That(introduced.enabled,Is.False);
            router.EnterInventoryMode();
            Assert.That(introduced.enabled,Is.True,"Opening a menu must enable its complete current UI map even when the mode did not change.");
            Assert.That(actions.FindActionMap("Gameplay").enabled,Is.False);
        }

        private void Send(string layout,byte shoulders=0,byte trigger=0,bool south=false)
        {
            if(layout=="DualSenseGamepadHID")
                InputSystem.QueueStateEvent(native,new DualSenseHIDInputReport
                {leftStickX=128,leftStickY=128,rightStickX=128,rightStickY=128,
                    buttons0=(byte)(8|(south?32:0)),buttons1=shoulders,rightTrigger=trigger});
            else InputSystem.QueueStateEvent(native,new WindowsReport
                {buttons=(ushort)((shoulders<<8)|(south?1<<12:0)),rightTrigger=trigger});
            InputSystem.Update();
        }

        [StructLayout(LayoutKind.Explicit,Size=12)]
        private struct WindowsReport:IInputStateTypeInfo
        {
            public FourCC format=>new FourCC('X','I','N','P');
            [FieldOffset(0)] public ushort buttons;
            [FieldOffset(2)] public byte leftTrigger;
            [FieldOffset(3)] public byte rightTrigger;
            [FieldOffset(4)] public short leftX;
            [FieldOffset(6)] public short leftY;
            [FieldOffset(8)] public short rightX;
            [FieldOffset(10)] public short rightY;
        }
    }
}
