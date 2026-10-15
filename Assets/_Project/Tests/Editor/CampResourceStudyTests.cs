using System.Linq;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class CampResourceStudyTests
    {
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
        public void FiveSourcesHaveDistinctIdentityAndSingleFiniteHarvest(int index)
        {
            var ledger=new CampResourceLedger();
            if(index==4){Assert.That(ledger.Consume(4),Is.False);ledger.Damage(30);}
            Assert.That(ledger.Consume(index),Is.True);Assert.That(ledger.Consume(index),Is.False);
            Assert.That(Enumerable.Range(0,5).Select(CampResourceLedger.SourceId).Distinct().Count(),Is.EqualTo(5));
        }
        [Test] public void DeathDoesNotHarvestOrReviveAndRejectsInvalidDamage()
        {
            var l=new CampResourceLedger();Assert.That(l.Damage(float.NaN),Is.False);Assert.That(l.Damage(-1),Is.False);
            l.Damage(30);Assert.That(l.Dead,Is.True);Assert.That(l.Used(4),Is.False);Assert.That(l.Damage(5),Is.False);
            Assert.That(l.CanHarvest(4),Is.True);l.Consume(4);Assert.That(l.CanHarvest(4),Is.False);
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
        public void ReloadAndAnimationInvalidationPreserveStockAndDeath(int index)
        {
            var l=new CampResourceLedger();if(index==4)l.Damage(30);l.Consume(index);l.Invalidate();
            var restored=JsonUtility.FromJson<CampResourceLedger>(JsonUtility.ToJson(l));
            Assert.That(restored.Valid,Is.True);Assert.That(restored.Used(index),Is.True);Assert.That(restored.CanHarvest(index),Is.False);
            Assert.That(restored.Epoch,Is.EqualTo(l.Epoch));Assert.That(restored.Health,Is.EqualTo(l.Health));
        }
        [Test] public void InvalidRetainedStateFailsClosed()
        {
            Assert.That(JsonUtility.FromJson<CampResourceLedger>("{\"version\":2}").Valid,Is.False);
            Assert.That(JsonUtility.FromJson<CampResourceLedger>("{\"version\":1,\"consumed\":16,\"beetleHealth\":30}").Valid,Is.False);
        }
        [Test] public void StrikeEmitsOnceAndCancellationCannotQueueDamage()
        {
            var s=new CampResourceStrike();s.Begin();int hits=0;
            for(int i=0;i<60;i++)if(s.Advance(.02f,true))hits++;
            Assert.That(hits,Is.EqualTo(1));Assert.That(s.Active,Is.False);
            s.Begin();s.Advance(.2f,true);Assert.That(s.Advance(.2f,false),Is.False);Assert.That(s.Advance(.2f,true),Is.False);
        }
        [Test] public void SkippingTheEntireHitWindowCannotApplyALateStrike()
        {var s=new CampResourceStrike();s.Begin();Assert.That(s.Advance(.7f,true),Is.False);}
        [TestCase(0,0,1,true)] [TestCase(0,0,4,false)] [TestCase(0,0,-1,false)] [TestCase(0,3,1,false)]
        public void HitRequiresRangeFacingAndVerticalEligibility(float x,float y,float z,bool expected)
        {Assert.That(CampHuskBeetle.CanHit(Vector3.zero,Vector3.forward,new Vector3(x,y,z),2,.2f),Is.EqualTo(expected));}
        private sealed class Target : ITopDown3DInteractable, ITopDown3DTargetEpoch
        {
            public CampResourceLedger State;public int Index;public GameObject Owner;
            public Object UnityObject=>Owner;public string StableId=>CampResourceLedger.SourceId(Index);public uint Epoch=>State.Epoch;
            public string Prompt=>"Gather";public Vector3 InteractionPoint=>Vector3.zero;public float InteractionRange=>2;
            public bool IsAvailable=>State.CanHarvest(Index);public float ActionDuration=>1;
            public TopDown3DItemAmount Reward=>new(CampResourceLedger.Items[Index],CampResourceLedger.Quantities[Index]);
            public bool TryConsume(out int remaining){remaining=0;return State.Consume(Index);}
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
        public void GatherUsesCargoOnceAndAnimationResetCannotReplenish(int index)
        {
            var cat=AssetDatabase.LoadAssetAtPath<TopDown3DItemCatalog>("Assets/_Project/Settings/Studies/CampLoopSalvage/LabItemCatalog.asset");
            var kit=new TopDown3DInventoryState(cat,new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.Booter,16));
            var cargo=new TopDown3DInventoryState(cat,new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.BigArm,12,92));
            var state=new CampResourceLedger();if(index==4)state.Damage(30);var g=new GameObject("Source fixture");
            try
            {
                var target=new Target{State=state,Index=index,Owner=g};var gather=new TopDown3DGatherActionState(new TopDown3DItemDestinationService(kit,cargo,()=>true));
                Assert.That(gather.TryBegin(target,Vector3.zero),Is.EqualTo(TopDown3DGatherActionResult.Started));
                Assert.That(gather.Advance(1,Vector3.zero),Is.EqualTo(TopDown3DGatherActionResult.Completed));
                state.Invalidate();Assert.That(gather.TryBegin(target,Vector3.zero),Is.EqualTo(TopDown3DGatherActionResult.Rejected));
                Assert.That(kit.OccupiedSlotCount,Is.Zero);Assert.That(cargo.Slots.Sum(s=>s.Quantity),Is.EqualTo(target.Reward.Quantity));
            }
            finally{Object.DestroyImmediate(g);}
        }
        [Test] public void CargoLeavingAndStaleEpochCannotConsume()
        {
            var cat=AssetDatabase.LoadAssetAtPath<TopDown3DItemCatalog>("Assets/_Project/Settings/Studies/CampLoopSalvage/LabItemCatalog.asset");
            var kit=new TopDown3DInventoryState(cat,new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.Booter,16));
            var cargo=new TopDown3DInventoryState(cat,new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.BigArm,12,92));
            var state=new CampResourceLedger();var g=new GameObject("Source fixture");bool access=true;
            try
            {
                var t=new Target{State=state,Index=1,Owner=g};var gather=new TopDown3DGatherActionState(new TopDown3DItemDestinationService(kit,cargo,()=>access));
                gather.TryBegin(t,Vector3.zero);access=false;Assert.That(gather.Advance(1,Vector3.zero),Is.EqualTo(TopDown3DGatherActionResult.DestinationUnavailable));
                Assert.That(state.Used(1),Is.False);access=true;gather.TryBegin(t,Vector3.zero);state.Invalidate();
                Assert.That(gather.Advance(1,Vector3.zero),Is.EqualTo(TopDown3DGatherActionResult.Cancelled));Assert.That(cargo.OccupiedSlotCount,Is.Zero);
            }
            finally{Object.DestroyImmediate(g);}
        }
        [Test] public void SceneHasFiveOwnedSourcesAndPreservesOriginalResourceAndNpcOwners()
        {
            var scene=EditorSceneManager.OpenScene(TopDown3DAnimationPlaygroundSession.ScenePath);
            try{CampResourceStudyAuthoring.Validate(scene);}
            finally{EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);}
        }
        [Test] public void PreparedTrialResetRestoresInventoryAndSourcesTogetherAndRejectsChangedState()
        {
            var scene=EditorSceneManager.OpenScene(TopDown3DAnimationPlaygroundSession.ScenePath);
            var study=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<CampResourceStudy>()).Single();
            var field=typeof(CampResourceStudy).GetField("bound",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            field.SetValue(study,true);
            try
            {
                study.Ledger.Consume(0);study.Ledger.Damage(30);study.Ledger.Consume(4);
                var catalog=AssetDatabase.LoadAssetAtPath<TopDown3DItemCatalog>("Assets/_Project/Settings/Studies/CampLoopSalvage/LabItemCatalog.asset");
                var cargo=new TopDown3DInventoryState(catalog,new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.BigArm,12,92));
                var baseline=cargo.CaptureSnapshot();cargo.TryAdd(new TopDown3DItemAmount(CampResourceLedger.Items[0],2));
                var draft=new TopDown3DInventoryTransaction.Mutation(cargo);draft.Draft.ApplySnapshot(baseline);
                var prepared=study.PrepareReset();Assert.That(prepared,Is.Not.Null);
                var failed=TopDown3DInventoryTransaction.Commit(new[]{draft},()=>false,prepared.World);
                Assert.That(failed.Succeeded,Is.False);Assert.That(study.Ledger.Dead,Is.True);Assert.That(study.Ledger.Used(0),Is.True);Assert.That(cargo.OccupiedSlotCount,Is.EqualTo(1));
                var success=TopDown3DInventoryTransaction.Commit(new[]{draft},()=>true,study.PrepareReset().World);
                Assert.That(success.Succeeded,Is.True);Assert.That(study.Ledger.Health,Is.EqualTo(30));Assert.That(study.Ledger.Used(0),Is.False);Assert.That(cargo.OccupiedSlotCount,Is.Zero);
                var stale=study.PrepareReset();study.Ledger.Damage(1);
                Assert.That(TopDown3DInventoryTransaction.Commit(new[]{new TopDown3DInventoryTransaction.Mutation(cargo)},()=>true,stale.World).Succeeded,Is.False);
            }
            finally{field.SetValue(study,false);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);}
        }
        [Test] public void EverySourceHasAnUnobstructedAuthoredWorkFace()
        {
            var scene=EditorSceneManager.OpenScene(TopDown3DAnimationPlaygroundSession.ScenePath);
            try
            {
                Physics.SyncTransforms();
                foreach(var point in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<CampResourceSource>()))
                {
                    var from=point.InteractionPoint-point.transform.forward*1.4f+Vector3.up*.2f;
                    var delta=point.InteractionPoint-from;
                    if(Physics.Raycast(from,delta.normalized,out var hit,delta.magnitude+.03f,~0,QueryTriggerInteraction.Collide))
                        Assert.That(point.OwnsCollider(hit.collider),Is.True,point.name+" work face is occluded by "+hit.collider.name);
                }
            }
            finally{EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);}
        }
        [Test] public void InstalledStudyBindsExistingEconomyAndConfirmedResetRestoresItsBeetle()
        {
            var scene=EditorSceneManager.OpenScene(TopDown3DAnimationPlaygroundSession.ScenePath);
            try
            {
                var economy=CampWorkstationSceneAuthoring.All<TopDown3DCampLoopEconomySession>(scene).Single();
                var player=economy.Kit.GetComponent<TopDown3DPlayerMotor>();var input=CampWorkstationSceneAuthoring.All<TopDown3DInputRouter>(scene).Single();
                player.Configure(input,CampWorkstationSceneAuthoring.All<Camera>(scene).Single().transform);
                var lab=TopDown3DAnimationPlaygroundSession.Find(scene);Assert.That(lab.CheckDependencies(),Is.True);
                TopDown3DInventoryUiSceneInstaller.TryInstallForScene(scene,"Logs/ResourceStudyTestPreferences");
                lab.Advance(AnimationPlaygroundReadiness.ServicesBound);lab.Advance(AnimationPlaygroundReadiness.VisualReady);
                Assert.That(economy.Initialize(),Is.True,economy.FailureReason);
                var study=CampWorkstationSceneAuthoring.All<CampResourceStudy>(scene).Single();
                typeof(CampResourceStudy).GetMethod("Update",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(study,null);
                Assert.That(study.Ready,Is.True,study.Failure);
                var trial=economy.Kit.GetComponent<TopDown3DCampLoopInventoryTrial>();Assert.That(trial.Ready,Is.True,trial.FailureReason);
                study.Ledger.Damage(30);study.Ledger.Consume(4);
                economy.Cargo.State.TryAdd(new TopDown3DItemAmount(CampResourceLedger.Items[4],3));
                input.EnterInventoryMode();
                Assert.That(trial.RequestReset(),Does.Contain("husk beetle"));
                Assert.That(trial.RequestReset(),Does.Contain("Inventory trial restored"));
                Assert.That(study.Ledger.Health,Is.EqualTo(30));Assert.That(study.Ledger.Used(4),Is.False);
                Assert.That(economy.Cargo.State.Slots.Any(s=>s.ItemId==CampResourceLedger.Items[4]),Is.False);
            }
            finally{EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);}
        }
    }
}
