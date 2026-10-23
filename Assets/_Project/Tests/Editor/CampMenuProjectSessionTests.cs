using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.LowLevel;

namespace BooterBigArm.Tests
{
    // Inactive session plus synthetic menu input: no startup, field restoration, Play Mode or profile IO.
    public sealed class CampMenuProjectSessionTests
    {
        private const BindingFlags Fields=BindingFlags.Instance|BindingFlags.NonPublic;
        private CampExpeditionGamepadTests fixture;
        private CampExpeditionSession session;
        private CampExpeditionMenuView view;
        private CampExpeditionLedger ledger;
        private MemoryFile file;
        private T Get<T>(string name)=>(T)typeof(CampExpeditionGamepadTests).GetField(name,Fields).GetValue(fixture);
        private void Set(string name,object value)=>typeof(CampExpeditionSession).GetField(name,Fields).SetValue(session,value);
        private object Read(string name)=>typeof(CampExpeditionSession).GetField(name,Fields).GetValue(session);
        private void Call(string name,params object[] args)=>typeof(CampExpeditionSession).GetMethod(name,Fields).Invoke(session,args);
        [SetUp] public void Setup()
        {
            fixture=new CampExpeditionGamepadTests();fixture.Setup();session=Get<CampExpeditionSession>("session");view=Get<CampExpeditionMenuView>("view");
            var configuration=Resources.Load<CampExpeditionConfiguration>("CampExpeditionConfiguration");var catalog=configuration.definition.itemCatalog;
            var booter=new TopDown3DInventoryState(catalog,new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.Booter,CampExpeditionLayout.Capacity,expeditionLayout:true));var cargo=new TopDown3DInventoryState(catalog,new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.BigArm,12));var storage=new TopDown3DInventoryState(catalog,new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.ResourceCache,24,campStorage:true));
            Assert.That(cargo.TryAdd(new TopDown3DItemAmount("resource.ironstone_ore",2)).Succeeded,Is.True);
            var snapshot=new CampExpeditionSnapshot{sequence=1,profileId="project-menu-fixture",contentFingerprint="test",storageCapacity=24,phase=CampExpeditionPhase.Town,booter=booter.CaptureSnapshot(),legger=cargo.CaptureSnapshot(),storage=storage.CaptureSnapshot()};file=new MemoryFile{saved=snapshot.Clone()};ledger=new CampExpeditionLedger(file,snapshot,booter,cargo,storage);
            typeof(CampExpeditionSession).GetProperty("Configuration").SetValue(session,configuration);Set("ledger",ledger);Set("snapshot",snapshot);Set("vitals",session.gameObject.AddComponent<TopDown3DSurvivalVitals>());Set("kit",session.gameObject.AddComponent<TopDown3DPlayerInventory>());ledger.Changed+=()=>Set("snapshot",ledger.State);Call("Render");
        }
        [TearDown] public void Cleanup(){fixture?.Cleanup();}
        [Test] public void SessionProjectCommandsPreserveQuantitiesOrderPossessionsAndUnknownSavedPlans()
        {
            string root=CampExpeditionDefinition.ToolOneId,other=CampExpeditionDefinition.ToolZeroId;var inventory=JsonUtility.ToJson(ledger.Legger.CaptureSnapshot());
            Call("Command","project:add:"+root);Call("Command","project:set:7:"+root);int writes=file.writes;Call("Command","project:add:"+root);Assert.That(file.writes,Is.EqualTo(writes));Assert.That(ledger.State.projects.Single().remaining,Is.EqualTo(7));
            Call("Command","project:add:"+other);Call("Command","project:set:8:"+root);CollectionAssert.AreEqual(new[]{root,other},ledger.State.projects.Select(plan=>plan.rootId));Assert.That(JsonUtility.ToJson(ledger.Legger.CaptureSnapshot()),Is.EqualTo(inventory));
            var unknown=ledger.PrepareProject("retired.saved.recipe",3);ledger.Commit(unknown,out _);Call("Command","project:set:4:retired.saved.recipe");Assert.That(ledger.State.projects.Single(plan=>plan.rootId=="retired.saved.recipe").remaining,Is.EqualTo(4));Call("Command","project:remove:retired.saved.recipe");Assert.That(ledger.State.projects.Any(plan=>plan.rootId=="retired.saved.recipe"),Is.False);
        }
        [Test] public void InvalidCommandsAndUnknownNewRootsCannotWriteOrGrantAnything()
        {
            foreach(var command in new[]{"project:set:0:"+CampExpeditionDefinition.ToolOneId,"project:set:100:"+CampExpeditionDefinition.ToolOneId,"project:set:bad:"+CampExpeditionDefinition.ToolOneId,"project:add:unknown.new.recipe"})Call("Command",command);
            Assert.That(file.writes,Is.Zero);Assert.That(ledger.State.projects,Is.Empty);Assert.That(ledger.Legger.Slots[0].Quantity,Is.EqualTo(2));
        }
        [Test] public void NativeSouthAtTheWorkshopPlanningActionCommitsOneT1PlanThroughTheSession()
        {
            Action<string> route=command=>Call("Command",command);view.Command+=route;
            try
            {
                view.ShowPage("workstations");var track=view.GetComponentsInChildren<CampMenuControl>().Single(control=>control.key=="workshop:track");Get<EventSystem>("events").SetSelectedGameObject(track.gameObject);
                var send=typeof(CampExpeditionGamepadTests).GetMethod("Send",Fields,null,new[]{typeof(GamepadState)},null);send.Invoke(fixture,new object[]{new GamepadState().WithButton(GamepadButton.South)});
                Assert.That(ledger.State.projects.Single().rootId,Is.EqualTo(CampExpeditionDefinition.ToolOneId));Assert.That(ledger.State.projects.Single().remaining,Is.EqualTo(1));Assert.That(file.writes,Is.EqualTo(1));Assert.That(track.Button.interactable,Is.False);Assert.That(ledger.Legger.Slots[0].Quantity,Is.EqualTo(2));Assert.That(ledger.State.commons,Is.EqualTo(50));
            }
            finally{view.Command-=route;}
        }
        [TestCase(CampExpeditionPhase.CargoReview,true)]
        [TestCase(CampExpeditionPhase.CargoReview,false)]
        [TestCase(CampExpeditionPhase.Exploring,true)]
        [TestCase(CampExpeditionPhase.Exploring,false)]
        public void RetryingAPlanningSaveKeepsTheCurrentPageAndCommitsOnce(CampExpeditionPhase phase,bool project)
        {
            var phaseChange=ledger.Prepare("test.phase",(next,_)=>{next.phase=phase;next.inGameSeconds=123;if(phase==CampExpeditionPhase.Exploring)next.run=new CampExpeditionRunRecord{id="preserved-run",seed=42};});Assert.That(ledger.Commit(phaseChange,out _),Is.True);
            view.ShowPage(project?"projects":"tracked");int writes=file.writes;var possessions=JsonUtility.ToJson(ledger.Legger.CaptureSnapshot());file.fail=true;
            if(project)Call("Command","project:add:"+CampExpeditionDefinition.ToolOneId);else Call("ToggleTrackedItem","author.bio.stone_bone");
            Assert.That(Read("saveRecovery"),Is.EqualTo(true));Assert.That(Read("pending"),Is.Not.Null);Assert.That(ledger.State.projects.Length+ledger.State.tracked.Length,Is.Zero);Assert.That(Time.timeScale,Is.Zero);
            file.fail=false;Set("waitingRelease",false);Call("Command","quote-confirm");Assert.That(view.Page,Is.EqualTo(project?"projects":"tracked"));Assert.That(Read("pending"),Is.Null);Assert.That(Read("menuOpen"),Is.EqualTo(true));Assert.That(ledger.State.phase,Is.EqualTo(phase));Assert.That(ledger.State.inGameSeconds,Is.EqualTo(123));Assert.That(JsonUtility.ToJson(ledger.Legger.CaptureSnapshot()),Is.EqualTo(possessions));
            Assert.That(ledger.State.projects.Length+ledger.State.tracked.Length,Is.EqualTo(1));Assert.That(file.writes,Is.EqualTo(writes+2));if(phase==CampExpeditionPhase.Exploring){Assert.That(ledger.State.run.id,Is.EqualTo("preserved-run"));Assert.That(Time.timeScale,Is.EqualTo(1));}
        }
        private sealed class MemoryFile:ICampExpeditionFile
        {
            internal CampExpeditionSnapshot saved;internal bool fail;internal int writes;
            public CampExpeditionSnapshot Read()=>saved?.Clone();
            public void Write(CampExpeditionSnapshot value){writes++;if(fail)throw new IOException("Injected project save failure");saved=value.Clone();}
        }
    }
}
