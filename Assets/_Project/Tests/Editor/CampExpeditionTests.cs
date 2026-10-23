using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class CampExpeditionTests
    {
        private CampExpeditionDefinition definition;
        private readonly List<Object> temporary=new();
        private string directory;
        [SetUp] public void Setup()
        {
            definition=AssetDatabase.LoadAssetAtPath<CampExpeditionDefinition>("Assets/_Project/Settings/UI/CampExpedition/ExpeditionDefinition.asset");
            Assert.That(definition,Is.Not.Null);directory=Path.Combine(Path.GetTempPath(),"CampExpeditionTests",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        }
        [TearDown] public void Cleanup(){foreach(var item in temporary)Object.DestroyImmediate(item);temporary.Clear();if(Directory.Exists(directory))Directory.Delete(directory,true);}
        private TopDown3DInventoryState Booter()=>new(definition.itemCatalog,new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.Booter,CampExpeditionLayout.Capacity,expeditionLayout:true));
        private TopDown3DInventoryState Legger()=>new(definition.itemCatalog,new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.BigArm,12));
        private TopDown3DInventoryState Storage(int capacity)=>new(definition.itemCatalog,new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.ResourceCache,capacity,campStorage:true));
        private CampExpeditionLedger Ledger(MemoryFile file,int capacity=24,bool starters=true)
        {
            var a=Booter();var b=Legger();var c=Storage(capacity);
            if(starters)Assert.That(a.TryAdd(new[]{new TopDown3DItemAmount(CampExpeditionDefinition.HarnessId,1),new TopDown3DItemAmount(CampExpeditionDefinition.ToolZeroId,1),new TopDown3DItemAmount(CampExpeditionDefinition.PhagiaId,1)}).Succeeded,Is.True);
            var snapshot=new CampExpeditionSnapshot{sequence=1,profileId=Guid.NewGuid().ToString("N"),contentFingerprint="test",storageCapacity=capacity,booter=a.CaptureSnapshot(),legger=b.CaptureSnapshot(),storage=c.CaptureSnapshot()};
            file.saved=snapshot.Clone();return new CampExpeditionLedger(file,snapshot,a,b,c);
        }
        [Test] public void StoreAllCompletesReviewInTheSameDurableTransactionAndCanRetry()
        {
            var file=new MemoryFile();var ledger=Ledger(file);
            Assert.That(ledger.Commit(ledger.Prepare("return",(next,_)=>next.phase=CampExpeditionPhase.CargoReview),out _),Is.True);
            var before=JsonUtility.ToJson(ledger.State);
            var operation=ledger.PrepareStorage(ledger.AllCarriedManifest(),1,true);
            Assert.That(operation.candidate.phase,Is.EqualTo(CampExpeditionPhase.Town));
            file.failBefore=true;Assert.That(ledger.Commit(operation,out _),Is.False);
            Assert.That(JsonUtility.ToJson(ledger.State),Is.EqualTo(before));
            file.failBefore=false;Assert.That(ledger.Commit(operation,out _),Is.True);
            Assert.That(ledger.State.phase,Is.EqualTo(CampExpeditionPhase.Town));
            Assert.That(ledger.Booter.OccupiedSlotCount+ledger.Legger.OccupiedSlotCount,Is.Zero);
            Assert.That(ledger.Storage.OccupiedSlotCount,Is.EqualTo(3));
            var sequence=ledger.State.sequence;Assert.That(ledger.Commit(operation,out _),Is.True);
            Assert.That(ledger.State.sequence,Is.EqualTo(sequence));
            Assert.That(file.saved.phase,Is.EqualTo(CampExpeditionPhase.Town));
        }

        [Test] public void StarterLayoutHasNineBodyAddressesTwoMountsAndNoBonusEquipment()
        {
            var ledger=Ledger(new MemoryFile());Assert.That(ledger.Booter.OccupiedSlotCount,Is.EqualTo(3));
            Assert.That(ledger.Booter.Slots[1].ItemId,Is.EqualTo(CampExpeditionDefinition.HarnessId));
            Assert.That(ledger.Booter.Slots[8].ItemId,Is.EqualTo(CampExpeditionDefinition.ToolZeroId));
            Assert.That(ledger.Booter.Slots[9].ItemId,Is.EqualTo(CampExpeditionDefinition.PhagiaId));Assert.That(ledger.Booter.Slots[10].IsEmpty,Is.True);
            Assert.That(Enumerable.Range(0,9).Select(CampExpeditionLayout.Address).Distinct().Count(),Is.EqualTo(9));
            Assert.That(TopDown3DLoadoutLayout.Capacity,Is.EqualTo(10));Assert.That((int)TopDown3DEquipmentSlot.Jacket,Is.EqualTo(6));
        }
        [Test] public void NoFrameHasNoMountsAndCannotReleaseOccupiedFrame()
        {
            var a=Booter();Assert.That(a.TryAdd(new TopDown3DItemAmount(CampExpeditionDefinition.PhagiaId,1)).Succeeded,Is.True);
            Assert.That(a.Slots[9].IsEmpty&&a.Slots[10].IsEmpty,Is.True);Assert.That(a.Slots[11].ItemId,Is.EqualTo(CampExpeditionDefinition.PhagiaId));
            var ledger=Ledger(new MemoryFile());Assert.That(ledger.Booter.CanRelease(1),Is.False);
            Assert.That(ledger.Booter.TryMoveOrSwap(1,12).Succeeded,Is.False);
        }
        [Test] public void SaveFailureLeavesExactOwnershipBalanceAndCapacityUnchanged()
        {
            var file=new MemoryFile();var ledger=Ledger(file,1,false);FillTwoKinds(ledger);
            var before=JsonUtility.ToJson(ledger.State);var proposal=ledger.PrepareStorage(ledger.AllCarriedManifest());
            Assert.That(proposal.AdditionalSlots,Is.EqualTo(1));file.failBefore=true;
            Assert.That(ledger.Commit(proposal,out _),Is.False);Assert.That(JsonUtility.ToJson(ledger.State),Is.EqualTo(before));
            file.failBefore=false;Assert.That(ledger.Commit(proposal,out _),Is.True);Assert.That(ledger.State.commons,Is.EqualTo(49));
        }
        [Test] public void OverflowSlotsArePermanentAndRepeatConfirmationCannotChargeTwice()
        {
            var ledger=Ledger(new MemoryFile(),1,false);FillTwoKinds(ledger);var proposal=ledger.PrepareStorage(ledger.AllCarriedManifest());
            Assert.That(ledger.Commit(proposal,out _),Is.True);var sequence=ledger.State.sequence;
            Assert.That(ledger.Commit(proposal,out _),Is.True);Assert.That(ledger.State.sequence,Is.EqualTo(sequence));
            Assert.That(ledger.Storage.Capacity,Is.EqualTo(2));Assert.That(ledger.State.commons,Is.EqualTo(49));Assert.That(ledger.Legger.OccupiedSlotCount,Is.Zero);
        }
        [Test] public void ConfirmedOverflowMayCreateDebtWithoutLosingGoods()
        {
            var ledger=Ledger(new MemoryFile(),1,false);FillTwoKinds(ledger);var proposal=ledger.PrepareStorage(ledger.AllCarriedManifest(),75);
            Assert.That(ledger.Commit(proposal,out _),Is.True);Assert.That(ledger.State.commons,Is.EqualTo(-25));Assert.That(ledger.Storage.OccupiedSlotCount,Is.EqualTo(2));
        }
        [Test] public void StaleCargoQuoteCannotWriteOrMoveAnything()
        {
            var file=new MemoryFile();var ledger=Ledger(file,1,false);FillTwoKinds(ledger);var proposal=ledger.PrepareStorage(ledger.AllCarriedManifest());
            ledger.Legger.TryAdd(new TopDown3DItemAmount("resource.ironstone_ore",1));var before=JsonUtility.ToJson(ledger.State);
            Assert.That(ledger.Commit(proposal,out var reason),Is.False);StringAssert.Contains("changed",reason);Assert.That(JsonUtility.ToJson(ledger.State),Is.EqualTo(before));Assert.That(file.writes,Is.Zero);
        }
        [Test] public void AcknowledgementFailureReconcilesTheSameReceiptBeforePublishing()
        {
            var file=new MemoryFile{failAfter=true};var ledger=Ledger(file,1,false);FillTwoKinds(ledger);var proposal=ledger.PrepareStorage(ledger.AllCarriedManifest());
            Assert.That(ledger.Commit(proposal,out _),Is.True);Assert.That(ledger.State.HasReceipt(proposal.Id),Is.True);Assert.That(ledger.State.commons,Is.EqualTo(49));Assert.That(file.writes,Is.EqualTo(1));
        }
        [Test] public void ObserversSeeAllStoresAndAccountInstalledTogether()
        {
            var ledger=Ledger(new MemoryFile(),1,false);FillTwoKinds(ledger);bool observed=false;
            ledger.Legger.Changed+=()=>{observed=true;Assert.That(ledger.Storage.OccupiedSlotCount,Is.EqualTo(2));Assert.That(ledger.State.commons,Is.EqualTo(49));};
            Assert.That(ledger.Commit(ledger.PrepareStorage(ledger.AllCarriedManifest()),out _),Is.True);Assert.That(observed,Is.True);
        }
        [Test] public void EntireSendAllManifestIncludesEquipmentAndDetachesMountBeforeFrame()
        {
            var ledger=Ledger(new MemoryFile());var ids=ledger.Booter.Slots.Where(s=>!s.IsEmpty).Select(s=>s.InstanceId).ToArray();
            var proposal=ledger.PrepareStorage(ledger.AllCarriedManifest());Assert.That(ledger.Commit(proposal,out var reason),Is.True,reason);
            Assert.That(ledger.Booter.OccupiedSlotCount,Is.Zero);CollectionAssert.AreEquivalent(ids,ledger.Storage.Slots.Where(s=>!s.IsEmpty).Select(s=>s.InstanceId));
        }
        [Test] public void SelectedTransferLeavesOtherCarrierAndItemsUntouched()
        {
            var ledger=Ledger(new MemoryFile());FillTwoKinds(ledger);string booter=JsonUtility.ToJson(ledger.Booter.CaptureSnapshot());
            var proposal=ledger.PrepareStorage(new[]{(1,0)});Assert.That(ledger.Commit(proposal,out _),Is.True);
            Assert.That(JsonUtility.ToJson(ledger.Booter.CaptureSnapshot()),Is.EqualTo(booter));Assert.That(ledger.Legger.OccupiedSlotCount,Is.EqualTo(1));
        }
        [Test] public void DuplicateManifestIsRejectedBeforePreparation()
        {var ledger=Ledger(new MemoryFile());Assert.Throws<InvalidOperationException>(()=>ledger.PrepareStorage(new[]{(0,8),(0,8)}));}
        [Test] public void DeathSealsExactInstancesWithoutChangingProtectedFundsOrStorage()
        {
            var ledger=Ledger(new MemoryFile());FillTwoKinds(ledger);
            var begin=ledger.Prepare("run",(next,_)=>{next.phase=CampExpeditionPhase.Exploring;next.run=new CampExpeditionRunRecord{id="run-one"};next.vitals=TopDown3DSurvivalSnapshot.Create(0,21,32,43,54);});ledger.Commit(begin,out _);
            var original=JsonUtility.ToJson(ledger.Booter.CaptureSnapshot());Assert.That(ledger.Commit(ledger.PrepareDeath(),out _),Is.True);
            Assert.That(ledger.State.commons,Is.EqualTo(50));Assert.That(ledger.Booter.OccupiedSlotCount+ledger.Legger.OccupiedSlotCount,Is.Zero);
            var bundle=ledger.State.losses.Single();Assert.That(bundle.state,Is.EqualTo(CampRecoveryState.Sealed));Assert.That(JsonUtility.ToJson(bundle.booter),Is.EqualTo(original));
            Assert.That(bundle.cargoFootprint,Is.GreaterThan(5));Assert.That(ledger.State.phase,Is.EqualTo(CampExpeditionPhase.Town));Assert.That(CampOfflineRecovery.Enabled,Is.False);
            Assert.That(ledger.State.vitals.Health,Is.EqualTo(100));Assert.That(ledger.State.vitals.Hunger,Is.EqualTo(21));Assert.That(ledger.State.vitals.Thirst,Is.EqualTo(32));
        }
        [Test] public void RecoveryDelayUsesOnlyPersistedGameDaysAndRequestDoesNotMutateLoss()
        {
            var bundle=new CampRecoveryBundle{state=CampRecoveryState.Sealed};var request=CampOfflineRecovery.PrepareRequest(bundle,7,250,3);
            Assert.That(bundle.state,Is.EqualTo(CampRecoveryState.Sealed));Assert.That(request.dueGameDay,Is.EqualTo(10));Assert.That(CampOfflineRecovery.IsDue(request,7),Is.False);Assert.That(CampOfflineRecovery.IsDue(request,10),Is.True);
        }
        [Test] public void StarterToolDoesNotCompleteT1AndPreparedPartsReplaceOnlyTheirOwnBranch()
        {
            var ledger=Ledger(new MemoryFile());var initial=CampExpeditionProject.Calculate(definition,ledger.Booter,ledger.Legger,ledger.Storage);Assert.That(initial.Ready,Is.False);
            var part=definition.Find(definition.rootId).inputs.First();Assert.That(ledger.Storage.TryAdd(new TopDown3DItemAmount(part.itemId,part.quantity)).Succeeded,Is.True);
            var after=CampExpeditionProject.Calculate(definition,ledger.Booter,ledger.Legger,ledger.Storage);
            Assert.That(after.missing.Values.Sum(),Is.LessThan(initial.missing.Values.Sum()));Assert.That(after.Ready,Is.False);
        }
        [Test] public void VersionOneMigrationPreservesOriginalBytesAndEveryExistingStore()
        {
            var ledger=Ledger(new MemoryFile());var legacy=ledger.State;legacy.version=1;
            var payload=JsonUtility.ToJson(legacy).Replace(",\"discovered\":[]", "").Replace(",\"projects\":[]", "");
            var path=Path.Combine(directory,"legacy.json");
            var original=JsonUtility.ToJson(new TestEnvelope{payload=payload,sha256=CampExpeditionFile.Hash(payload)});
            File.WriteAllText(path,original);var file=new CampExpeditionFile(path,"test");var migrated=file.Read();
            Assert.That(migrated.version,Is.EqualTo(CampExpeditionSnapshot.CurrentVersion));Assert.That(migrated.projects,Is.Empty);Assert.That(migrated.discovered,Is.Empty);
            Assert.That(JsonUtility.ToJson(migrated.booter),Is.EqualTo(JsonUtility.ToJson(legacy.booter)));Assert.That(migrated.profileId,Is.EqualTo(legacy.profileId));
            Assert.That(migrated.commons,Is.EqualTo(legacy.commons));Assert.That(File.ReadAllText(path),Is.EqualTo(original));
            migrated.sequence++;file.Write(migrated);Assert.That(File.ReadAllText(path+".backup"),Is.EqualTo(original));
        }
        [Test] public void UnknownFutureMenuVersionIsPreservedAndNeverReseeded()
        {
            var state=Ledger(new MemoryFile()).State;state.version=99;var payload=JsonUtility.ToJson(state);var path=Path.Combine(directory,"future.json");
            var original=JsonUtility.ToJson(new TestEnvelope{payload=payload,sha256=CampExpeditionFile.Hash(payload)});File.WriteAllText(path,original);
            Assert.Throws<IOException>(()=>new CampExpeditionFile(path,"test").Read());Assert.That(File.ReadAllText(path),Is.EqualTo(original));
        }
        [Test] public void ProjectAndDiscoveryRecordsRoundTripWithoutChangingWorldIdentity()
        {
            var memory=new MemoryFile();var ledger=Ledger(memory);var identity=ledger.State.contentFingerprint;
            var file=new CampExpeditionFile(Path.Combine(directory,"menu.json"),identity);file.Write(ledger.State);
            var operation=ledger.Prepare("menu.plan",(next,_)=>{next.projects=new[]{new CampProjectRecord{rootId=definition.rootId,remaining=3}};next.discovered=new[]{CampExpeditionDefinition.ToolZeroId};});
            Assert.That(ledger.Commit(operation,out _),Is.True);file.Write(ledger.State);var read=file.Read();Assert.That(read.projects.Single().remaining,Is.EqualTo(3));
            Assert.That(read.discovered,Does.Contain(CampExpeditionDefinition.ToolZeroId));Assert.That(read.discovered.Length,Is.EqualTo(3));Assert.That(read.contentFingerprint,Is.EqualTo(identity));
        }
        [Test] public void InvalidProjectQuantityNeverOverwritesTheDurableProfile()
        {
            var state=Ledger(new MemoryFile()).State;var path=Path.Combine(directory,"invalid-plan.json");var file=new CampExpeditionFile(path,"test");file.Write(state);var before=File.ReadAllText(path);
            state.sequence++;state.projects=new[]{new CampProjectRecord{rootId=definition.rootId,remaining=100}};
            Assert.Throws<IOException>(()=>file.Write(state));Assert.That(File.ReadAllText(path),Is.EqualTo(before));
        }
        [Serializable] private sealed class TestEnvelope {public int version=1;public string payload,sha256;}
        [Test] public void FileRoundTripPreservesInstancesSequenceAndRejectsOlderCheckpoint()
        {
            var memory=new MemoryFile();var ledger=Ledger(memory);var path=Path.Combine(directory,"profile.json");var file=new CampExpeditionFile(path,"test");file.Write(ledger.State);
            var copy=file.Read();Assert.That(JsonUtility.ToJson(copy),Is.EqualTo(JsonUtility.ToJson(ledger.State)));
            var later=copy.Clone();later.sequence++;file.Write(later);Assert.Throws<InvalidOperationException>(()=>file.Write(copy));Assert.That(File.Exists(path+".backup"),Is.True);
        }
        [Test] public void UnknownContentNeverBecomesANewProfile()
        {
            var memory=new MemoryFile();var ledger=Ledger(memory);var path=Path.Combine(directory,"profile.json");new CampExpeditionFile(path,"test").Write(ledger.State);
            Assert.Throws<IOException>(()=>new CampExpeditionFile(path,"changed").Read());Assert.That(File.Exists(path),Is.True);
        }
        [Test] public void DuplicateEquipmentAcrossStoresIsRejectedWithoutWriting()
        {
            var file=new MemoryFile();var ledger=Ledger(file);var tool=ledger.Booter.Slots[8];
            Assert.Throws<InvalidOperationException>(()=>ledger.Prepare("invalid",(_,drafts)=>drafts[1].TryAdd(new TopDown3DItemAmount(tool.ItemId,1,tool.InstanceId))));Assert.That(file.writes,Is.Zero);
        }
        [Test] public void SelectionRejectsAChangedOwnerRevisionOrDifferentEquipmentCopy()
        {
            var ledger=Ledger(new MemoryFile());var tool=ledger.Booter.Slots[8];var revision=ledger.Booter.Revision;
            Assert.That(ledger.SelectionMatches(0,8,tool.ItemId,tool.Quantity,tool.InstanceId,revision),Is.True);
            Assert.That(ledger.SelectionMatches(0,8,tool.ItemId,tool.Quantity,"different-instance",revision),Is.False);
            Assert.That(ledger.SelectionMatches(0,8,tool.ItemId,tool.Quantity,tool.InstanceId,revision+1),Is.False);
        }
        [Test] public void TrackedQuantitiesRequestAdditionalOutputsAndNeverReserveOwnedItems()
        {
            var ledger=Ledger(new MemoryFile());var before=JsonUtility.ToJson(ledger.Booter.CaptureSnapshot());
            Assert.That(ledger.Commit(ledger.PrepareProject(definition.rootId,2),out _),Is.True);
            var initial=CampExpeditionProject.Calculate(definition,ledger.Booter,ledger.Legger,ledger.Storage,ledger.State.projects);
            Assert.That(ledger.Storage.TryAdd(new TopDown3DItemAmount(definition.rootId,1)).Succeeded,Is.True);
            var after=CampExpeditionProject.Calculate(definition,ledger.Booter,ledger.Legger,ledger.Storage,ledger.State.projects);
            CollectionAssert.AreEquivalent(initial.missing,after.missing);Assert.That(JsonUtility.ToJson(ledger.Booter.CaptureSnapshot()),Is.EqualTo(before));
            Assert.That(ledger.Commit(ledger.PrepareProject(definition.rootId,0),out _),Is.True);Assert.That(ledger.State.projects,Is.Empty);
        }
        [Test] public void CombinedProjectsShareOneEligibleMaterialPoolAndRetainUnknownRecipes()
        {
            var ledger=Ledger(new MemoryFile());var fake=ScriptableObject.CreateInstance<CampExpeditionDefinition>();temporary.Add(fake);
            fake.items=new[]{new CampExpeditionItem{id="a",inputs=new[]{new CampExpeditionIngredient{itemId="resource.ironstone_ore",quantity=2}}},
                new CampExpeditionItem{id="b",inputs=new[]{new CampExpeditionIngredient{itemId="resource.ironstone_ore",quantity=2}}},new CampExpeditionItem{id="resource.ironstone_ore"}};
            Assert.That(ledger.Legger.TryAdd(new TopDown3DItemAmount("resource.ironstone_ore",3)).Succeeded,Is.True);
            var projects=new[]{new CampProjectRecord{rootId="a"},new CampProjectRecord{rootId="b"}};
            var demand=CampExpeditionProject.Calculate(fake,ledger.Booter,ledger.Legger,ledger.Storage,projects);
            Assert.That(demand.total["resource.ironstone_ore"],Is.EqualTo(4));Assert.That(demand.missing["resource.ironstone_ore"],Is.EqualTo(1));Assert.That(demand.Ready,Is.False);
            demand=CampExpeditionProject.Calculate(fake,ledger.Booter,ledger.Legger,ledger.Storage,projects,false,false);
            Assert.That(demand.missing["resource.ironstone_ore"],Is.EqualTo(4));Assert.That(demand.carried["resource.ironstone_ore"],Is.EqualTo(3));
            demand=CampExpeditionProject.Calculate(fake,ledger.Booter,ledger.Legger,ledger.Storage,new[]{new CampProjectRecord{rootId="unqualified"}});
            Assert.That(demand.Ready,Is.False);Assert.That(demand.unqualified,Does.Contain("unqualified"));
        }
        [Test] public void CraftOutputAndRemainingProjectCountAreOneReplaySafeDurableOperation()
        {
            var file=new MemoryFile();var ledger=Ledger(file);Assert.That(ledger.Commit(ledger.PrepareProject(definition.rootId,3),out _),Is.True);
            var craft=ledger.Prepare("test.output",(next,drafts)=>{Assert.That(drafts[1].TryAdd(new TopDown3DItemAmount(definition.rootId,1)).Succeeded,Is.True);CampExpeditionLedger.CompleteProjectOutputs(next,definition.rootId,1);});
            file.failBefore=true;Assert.That(ledger.Commit(craft,out _),Is.False);Assert.That(ledger.State.projects.Single().remaining,Is.EqualTo(3));Assert.That(ledger.Legger.OccupiedSlotCount,Is.Zero);
            file.failBefore=false;Assert.That(ledger.Commit(craft,out _),Is.True);Assert.That(ledger.Commit(craft,out _),Is.True);
            Assert.That(ledger.State.projects.Single().remaining,Is.EqualTo(2));Assert.That(ledger.Legger.OccupiedSlotCount,Is.EqualTo(1));
        }
        [Test] public void PopulationIsDeterministicCoversAllMaterialsAndUsesStableRunKeys()
        {
            bool Support(Vector3 point,out Vector3 result){result=new Vector3(point.x,0,point.z);return true;}
            Assert.That(CampExpeditionPopulation.TryPrepare(definition,"run-one",427,Vector3.zero,Support,(_,_)=>true,out var a),Is.True);
            Assert.That(CampExpeditionPopulation.TryPrepare(definition,"run-one",427,Vector3.zero,Support,(_,_)=>true,out var b),Is.True);
            CollectionAssert.AreEqual(a.Select(n=>n.key+":"+n.position),b.Select(n=>n.key+":"+n.position));Assert.That(a.Select(n=>n.key).Distinct().Count(),Is.EqualTo(a.Length));
            var required=definition.items.Where(i=>i.visibleInTree&&i.inputs.Length==0).Select(i=>i.id);foreach(var id in required)Assert.That(a.Any(n=>n.itemId==id),Is.True);
            Assert.That(a.Where(n=>n.itemId=="resource.ironstone_ore").All(n=>n.requiresTool),Is.True);
            Assert.That(a.Last().position.magnitude,Is.GreaterThan(a.Take(a.Length-1).Max(n=>n.position.magnitude)));
        }
        [Test] public void UnsupportedPopulationDoesNotCommitFallbackNodes()
        {
            bool Missing(Vector3 point,out Vector3 result){result=default;return false;}
            Assert.That(CampExpeditionPopulation.TryPrepare(definition,"run",0,Vector3.zero,Missing,(_,_)=>true,out var nodes),Is.False);Assert.That(nodes,Is.Null);
        }
        [Test] public void HarvestDepletionAndInventoryCommitTogetherAndRestoreWithoutReroll()
        {
            var memory=new MemoryFile();var ledger=Ledger(memory);var begin=ledger.Prepare("begin",(next,_)=>{next.phase=CampExpeditionPhase.Exploring;next.run=new CampExpeditionRunRecord{id="retained-run",seed=123,origin=new Vector3(5,6,7),nodes=new[]{new CampExpeditionNodeRecord{key="fixed-node",itemId="resource.ironstone_ore",remaining=2}}};next.inGameSeconds=543;});ledger.Commit(begin,out _);
            var harvest=ledger.Prepare("harvest",(next,drafts)=>{next.run.nodes[0].remaining--;drafts[1].TryAdd(new TopDown3DItemAmount("resource.ironstone_ore",1));});memory.failBefore=true;
            Assert.That(ledger.Commit(harvest,out _),Is.False);Assert.That(ledger.State.run.nodes[0].remaining,Is.EqualTo(2));Assert.That(ledger.Legger.OccupiedSlotCount,Is.Zero);
            memory.failBefore=false;ledger.Commit(harvest,out _);var saved=memory.Read();
            var restored=new CampExpeditionLedger(memory,saved,Booter(),Legger(),Storage(saved.storageCapacity));
            Assert.That(restored.State.run.seed,Is.EqualTo(123));Assert.That(restored.State.run.origin,Is.EqualTo(new Vector3(5,6,7)));Assert.That(restored.State.run.nodes[0].remaining,Is.EqualTo(1));Assert.That(restored.Legger.Slots[0].Quantity,Is.EqualTo(1));Assert.That(restored.State.inGameSeconds,Is.EqualTo(543));
        }
        [Test] public void ContinueChangesNoCargoAndAnotherRunDoesNotReseedFundsOrStarters()
        {
            var file=new MemoryFile();var ledger=Ledger(file);FillTwoKinds(ledger);string booter=JsonUtility.ToJson(ledger.Booter.CaptureSnapshot()),cargo=JsonUtility.ToJson(ledger.Legger.CaptureSnapshot());
            ledger.Commit(ledger.Prepare("review",(next,_)=>next.phase=CampExpeditionPhase.CargoReview),out _);
            ledger.Commit(ledger.Prepare("continue",(next,_)=>next.phase=CampExpeditionPhase.Town),out _);
            ledger.Commit(ledger.Prepare("begin",(next,_)=>{next.phase=CampExpeditionPhase.Exploring;next.run=new CampExpeditionRunRecord{id="next-run"};}),out _);
            var restored=new CampExpeditionLedger(file,file.Read(),Booter(),Legger(),Storage(24));Assert.That(restored.State.commons,Is.EqualTo(50));Assert.That(JsonUtility.ToJson(restored.Booter.CaptureSnapshot()),Is.EqualTo(booter));Assert.That(JsonUtility.ToJson(restored.Legger.CaptureSnapshot()),Is.EqualTo(cargo));
        }
        [Test] public void PendingInitialCheckpointCanBeFinishedExplicitlyWithoutAnotherStarterGrant()
        {
            var memory=new MemoryFile();var ledger=Ledger(memory);var path=Path.Combine(directory,"profile.json");var file=new CampExpeditionFile(path,"test");file.Write(ledger.State);
            File.Move(path,path+".pending");Assert.Throws<IOException>(()=>file.Read());var finished=file.FinishPending();Assert.That(finished.sequence,Is.EqualTo(1));Assert.That(JsonUtility.ToJson(finished.booter),Is.EqualTo(JsonUtility.ToJson(ledger.Booter.CaptureSnapshot())));Assert.That(Directory.GetFiles(directory,"*.recovered").Length,Is.EqualTo(1));
        }
        [Test] public void BackupRecoveryRetainsLaterCheckpointBytes()
        {
            var memory=new MemoryFile();var ledger=Ledger(memory);var path=Path.Combine(directory,"profile.json");var file=new CampExpeditionFile(path,"test");file.Write(ledger.State);var later=ledger.State;later.sequence=2;later.commons=12;file.Write(later);
            var restored=file.RestoreBackup();Assert.That(restored.sequence,Is.EqualTo(1));Assert.That(restored.commons,Is.EqualTo(50));Assert.That(Directory.GetFiles(directory,"*.retained.*").Length,Is.EqualTo(1));Assert.That(File.Exists(path+".backup"),Is.True);
        }
        [Test] public void DeathBundlePreservesInstanceBoundPhagiaContents()
        {
            var memory=new MemoryFile();var ledger=Ledger(memory);var instance=ledger.Booter.Slots[9].InstanceId;
            ledger.Commit(ledger.Prepare("begin",(next,_)=>{next.phase=CampExpeditionPhase.Exploring;next.run=new CampExpeditionRunRecord{id="lost-run"};next.equipmentStates=new[]{new CampEquipmentStateRecord{instanceId=instance,phagia=new TopDown3DAlgaeCanisterSnapshot{capacity=100,remaining=12.5}}};}),out _);
            ledger.Commit(ledger.PrepareDeath(),out _);Assert.That(ledger.State.equipmentStates,Is.Empty);var contents=ledger.State.losses.Single().equipmentStates.Single();Assert.That(contents.instanceId,Is.EqualTo(instance));Assert.That(contents.phagia.remaining,Is.EqualTo(12.5));
        }
        private static void FillTwoKinds(CampExpeditionLedger ledger)
        {
            Assert.That(ledger.Legger.TryAdd(new[]{new TopDown3DItemAmount("resource.ironstone_ore",1),new TopDown3DItemAmount("proposal.bio.shell",1)}).Succeeded,Is.True);
        }
        [TestCase(0f)] [TestCase(180f)]
        public void OwnedBeaconRecoversSelectionAndMovingCountdownReturnsCargo(float facing)
        {
            var scene=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var origin=new Vector3(30000,10,30000);
            GameObject Make(string name){var go=new GameObject(name);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);return go;}
            var actor=Make("Return actor");actor.transform.position=origin+Vector3.back;
            var body=actor.AddComponent<Rigidbody>();body.isKinematic=true;
            var motor=actor.AddComponent<TopDown3DPlayerMotor>();
            typeof(TopDown3DPlayerMotor).GetField("facingDirection",flags).SetValue(motor,Quaternion.Euler(0,facing,0)*Vector3.forward);
            var input=actor.AddComponent<TopDown3DInputRouter>();
            actor.AddComponent<TopDown3DPlayerActionController>();
            var interaction=actor.AddComponent<TopDown3DInteractionController>();interaction.Configure(motor);
            typeof(TopDown3DPlayerMotor).GetProperty("IsGrounded").SetValue(motor,true);
            typeof(TopDown3DInputRouter).GetProperty("Mode").SetValue(input,TopDown3DInputMode.Gameplay);
            var companion=Make("Return companion");companion.transform.position=origin+Vector3.right*3;
            companion.AddComponent<Rigidbody>().isKinematic=true;companion.AddComponent<BoxCollider>();companion.AddComponent<TopDown3DBigArmFollower>();
            var owner=Make("Return owner");var session=owner.AddComponent<CampExpeditionSession>();
            var target=Make("Owned beacon");target.transform.position=origin;
            var shape=target.AddComponent<SphereCollider>();shape.isTrigger=true;shape.radius=.65f;shape.center=Vector3.up*.8f;
            var beacon=target.AddComponent<CampRootBeaconSession>();
            try
            {
                var memory=new MemoryFile();var ledger=Ledger(memory);FillTwoKinds(ledger);
                var record=new CampExpeditionRunRecord{id="moving-return",seed=42,origin=origin};
                Assert.That(ledger.Commit(CampExpeditionSession.PrepareDeparture(ledger,record),out _),Is.True);
                void Set(string key,object value)=>typeof(CampExpeditionSession).GetField(key,flags).SetValue(session,value);
                Set("ledger",ledger);Set("snapshot",ledger.State);Set("beacon",beacon);Set("boundRunId",record.id);
                var registry=(Dictionary<int,CampExpeditionSession>)typeof(CampExpeditionSession).GetField("sessions",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).GetValue(null);
                registry.Remove(scene.handle);
                Assert.That(CampExpeditionSession.Find(scene),Is.SameAs(session));
                Assert.That(beacon.Run,Is.Null);
                Assert.That(session.TryRestoreBeaconState(beacon),Is.True);
                Assert.That(beacon.Run.RunId,Is.EqualTo(record.id));
                // A retained managed run is not proof that its actor/input bindings survived.
                typeof(CampRootBeaconSession).GetProperty("Input").SetValue(beacon,null);
                Assert.DoesNotThrow(()=>typeof(CampRootBeaconSession).GetMethod("OnEnable",flags).Invoke(beacon,null));
                Assert.That(beacon.IsAvailable,Is.False);
                beacon.AdvanceDeparture(0);
                Assert.That(beacon.Input,Is.SameAs(input));
                Assert.That(beacon.Run.RunId,Is.EqualTo(record.id));
                Physics.SyncTransforms();interaction.RefreshTarget();
                Assert.That(interaction.SelectedTarget,Is.SameAs(beacon));
                var wall=Make("Real beacon obstruction");wall.transform.position=Vector3.Lerp(actor.transform.position+Vector3.up*.8f,beacon.InteractionPoint,.5f);
                wall.AddComponent<BoxCollider>().size=Vector3.one*.3f;Physics.SyncTransforms();interaction.RefreshTarget();
                Assert.That(interaction.SelectedTarget,Is.Null);
                Assert.That(beacon.DepartureBlocker,Does.Contain("obstructed"));
                Object.DestroyImmediate(wall);Physics.SyncTransforms();interaction.RefreshTarget();
                var duplicate=Make("Unowned duplicate beacon").AddComponent<CampRootBeaconSession>();duplicate.RestoreExpeditionState(record);
                Assert.That(duplicate.IsAvailable,Is.False,"The same run ID cannot authorize a second native beacon.");
                Object.DestroyImmediate(duplicate.gameObject);
                typeof(TopDown3DPlayerActionController).GetField("pickupHarvesterId",flags).SetValue(actor.GetComponent<TopDown3DPlayerActionController>(),string.Empty);
                Assert.That(actor.GetComponent<TopDown3DPlayerActionController>().IsBusy,Is.False,"An empty serialized pickup ID cannot create a phantom busy action.");
                Assert.That(beacon.TryBeginDeparture(),Is.True);
                Assert.That(motor.IsActionConstrained,Is.False,"The timer must not own locomotion.");
                var ride=companion.AddComponent<TopDown3DLeggerRidingController>();
                ride.State.BeginMount();ride.State.CommitMount(ride.State.Epoch);Assert.That(input.BindRidingInput(ride),Is.True);
                typeof(TopDown3DInputRouter).GetField("inputUpdateMode",flags).SetValue(input,TopDown3DInputMode.Gameplay);
                typeof(TopDown3DInputRouter).GetField("pickupRequested",flags).SetValue(input,true);
                Assert.That((bool)typeof(TopDown3DInputRouter).GetMethod("ResolveRidingCommands",flags).Invoke(input,new object[]{false}),Is.True);
                Assert.That(beacon.Run.Phase,Is.EqualTo(RootBeaconPhase.Exploring),"C must cancel even while riding owns the input.");
                Assert.That((bool)typeof(TopDown3DInputRouter).GetField("pickupRequested",flags).GetValue(input),Is.False);
                Assert.That(ride.State.Phase,Is.EqualTo(LeggerRidePhase.Mounted));
                ride.State.Clear();input.UnbindRidingInput(ride);Object.DestroyImmediate(ride);
                Assert.That(beacon.TryBeginDeparture(),Is.True);
                // Substitute only the event consumer, so this EditMode fixture exercises the
                // real beacon guards/countdown and the same durable return operation as the host.
                typeof(CampRootBeaconSession).GetField("ExtractionCompleted",flags).SetValue(beacon,null);
                int returns=0;beacon.ExtractionCompleted+=run=>{Assert.That(ledger.Commit(CampExpeditionSession.PrepareExtraction(ledger,run),out _),Is.True);returns++;};
                body.position=origin+Vector3.forward*20;actor.transform.position=body.position;
                beacon.AdvanceDeparture(1);Assert.That(beacon.Run.Phase,Is.EqualTo(RootBeaconPhase.Departing));
                Assert.That(motor.IsActionConstrained,Is.False);
                body.position+=Vector3.right*10;actor.transform.position=body.position;
                beacon.AdvanceDeparture(2);beacon.AdvanceDeparture(5);
                Assert.That(returns,Is.EqualTo(1));Assert.That(ledger.State.phase,Is.EqualTo(CampExpeditionPhase.CargoReview));
                Assert.That(ledger.Legger.OccupiedSlotCount,Is.EqualTo(2));
                Assert.That(memory.saved.phase,Is.EqualTo(CampExpeditionPhase.CargoReview));
            }
            finally{UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);}
        }
        [Test] public void DestroyingReplacedOwnerCannotRemoveRegistryOrResetSimulation()
        {
            var scene=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);
            var oldObject=new GameObject("Replaced expedition owner");var currentObject=new GameObject("Current expedition owner");
            var before=Time.timeScale;
            try
            {
                var old=oldObject.AddComponent<CampExpeditionSession>();var current=currentObject.AddComponent<CampExpeditionSession>();
                var registry=(Dictionary<int,CampExpeditionSession>)typeof(CampExpeditionSession).GetField("sessions",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).GetValue(null);
                registry[scene.handle]=current;Time.timeScale=.37f;
                Object.DestroyImmediate(oldObject);
                Assert.That(CampExpeditionSession.Find(scene),Is.SameAs(current));
                Assert.That(Time.timeScale,Is.EqualTo(.37f));
            }
            finally{Object.DestroyImmediate(currentObject);if(oldObject!=null)Object.DestroyImmediate(oldObject);Time.timeScale=before;}
        }
        [Test] public void OrphanReconciliationRemovesNativeCollidersWithoutTouchingLoadedOwner()
        {
            var scene=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);
            var oldOwner=new GameObject("Old runtime owner"){hideFlags=HideFlags.DontSave};oldOwner.AddComponent<CampExpeditionSession>();
            var population=new GameObject("Expedition population · orphan-fixture"){hideFlags=HideFlags.DontSave};
            var target=new GameObject("Old beacon");target.transform.SetParent(population.transform);target.AddComponent<SphereCollider>();target.AddComponent<CampRootBeaconSession>();
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);
            var current=new GameObject("Healthy loaded owner");current.AddComponent<CampExpeditionSession>();var before=Time.timeScale;
            try
            {
                Assert.That(population!=null,Is.True,"Reproduce the old DontSave population before testing its cleanup.");
                Assert.That(population.scene.IsValid(),Is.False);
                Time.timeScale=.43f;Assert.That(CampExpeditionSession.ReconcileOrphans(),Is.GreaterThanOrEqualTo(2));
                Assert.That(population==null,Is.True);Assert.That(oldOwner==null,Is.True);
                Assert.That(current!=null,Is.True);Assert.That(Time.timeScale,Is.EqualTo(.43f));
            }
            finally{if(population!=null)Object.DestroyImmediate(population);if(oldOwner!=null)Object.DestroyImmediate(oldOwner);Object.DestroyImmediate(current);Time.timeScale=before;}
        }
        [Test] public void NewExpeditionOwnerUsesSceneLifetimeAndLeavesNoNativeOwnerAfterUnload()
        {
            var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            try
            {
                CampExpeditionSession.Install(scene);
                var owner=TopDown3DGameHudCanvas.FindInScene<CampExpeditionSession>(scene);
                Assert.That(owner,Is.Not.Null);Assert.That(owner.gameObject.hideFlags,Is.EqualTo(HideFlags.None));
                UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
                Assert.That(owner==null,Is.True,"Normal scene lifetime must destroy the native owner.");
            }
            finally{if(scene.IsValid())UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
        }
        [Test] public void RestoredExpeditionPoseMeasuresSupportWithoutMovingAirborneCheckpoints()
        {
            var actor=new GameObject("Restored physical support");var ground=new GameObject("Restore support ground");
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            try
            {
                ground.transform.position=new Vector3(25000,0,25000);ground.AddComponent<BoxCollider>().size=new Vector3(10,.1f,10);ground.AddComponent<TopDown3DGroundSurface>();
                var body=actor.AddComponent<Rigidbody>();body.useGravity=false;var capsule=actor.AddComponent<CapsuleCollider>();capsule.height=2;
                var motor=actor.AddComponent<TopDown3DPlayerMotor>();
                typeof(TopDown3DPlayerMotor).GetField("body",flags).SetValue(motor,body);typeof(TopDown3DPlayerMotor).GetField("capsule",flags).SetValue(motor,capsule);
                var supported=new Vector3(25000,1.05f,25000);motor.RestoreExpeditionPose(supported,Quaternion.identity);
                Assert.That(motor.IsGrounded,Is.True);Assert.That(motor.Position,Is.EqualTo(supported));
                var airborne=supported+Vector3.up*10;motor.RestoreExpeditionPose(airborne,Quaternion.identity);
                Assert.That(motor.IsGrounded,Is.False);Assert.That(motor.Position,Is.EqualTo(airborne));
            }
            finally{Object.DestroyImmediate(actor);Object.DestroyImmediate(ground);}
        }
        [Test] public void ReloadCheckpointKeepsUnsavedCargoAndExtractsOnceWithoutAnotherGrant()
        {
            var memory=new MemoryFile();var ledger=Ledger(memory);
            var origin=new Vector3(-17,6,20);
            Assert.That(ledger.Commit(ledger.Prepare("begin",(next,_)=>{next.phase=CampExpeditionPhase.Exploring;next.run=new CampExpeditionRunRecord{id="reload-run",seed=42,origin=origin,nodes=new[]{new CampExpeditionNodeRecord{key="retained-node",remaining=2}}};}),out _),Is.True);
            FillTwoKinds(ledger); // Live changes since the last durable checkpoint must survive.
            var owner=new GameObject("Session reload fixture");owner.SetActive(false);
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            try
            {
                var session=owner.AddComponent<CampExpeditionSession>();
                typeof(CampExpeditionSession).GetField("ledger",flags).SetValue(session,ledger);
                var writes=memory.writes;
                typeof(CampExpeditionSession).GetMethod("OnDisable",flags).Invoke(session,null);
                var json=(string)typeof(CampExpeditionSession).GetField("reloadCheckpoint",flags).GetValue(session);
                Assert.That(memory.writes,Is.EqualTo(writes),"Reload handoff must not write to the player file.");
                var checkpoint=CampExpeditionSnapshot.FromJson(json);
                var restored=new CampExpeditionLedger(memory,checkpoint,Booter(),Legger(),Storage(checkpoint.storageCapacity));
                Assert.That(restored.State.run.origin,Is.EqualTo(origin));
                Assert.That(restored.State.run.seed,Is.EqualTo(42));
                Assert.That(restored.State.run.nodes[0].remaining,Is.EqualTo(2));
                Assert.That(restored.Legger.OccupiedSlotCount,Is.EqualTo(2));
                var beacon=JsonUtility.FromJson<RootBeaconRun>(JsonUtility.ToJson(new RootBeaconRun("camp","reload-run",42,origin)));
                Assert.Throws<InvalidOperationException>(()=>CampExpeditionSession.PrepareExtraction(restored,beacon));
                beacon.TryDepart(origin);beacon.Advance(3,origin,true);
                var operation=CampExpeditionSession.PrepareExtraction(restored,beacon);
                memory.failBefore=true;Assert.That(restored.Commit(operation,out _),Is.False);
                Assert.That(restored.State.phase,Is.EqualTo(CampExpeditionPhase.Exploring));
                memory.failBefore=false;Assert.That(restored.Commit(operation,out _),Is.True);
                Assert.That(restored.Commit(operation,out _),Is.True);
                Assert.That(memory.writes,Is.EqualTo(writes+1));
                Assert.That(restored.State.phase,Is.EqualTo(CampExpeditionPhase.CargoReview));
                Assert.That(restored.State.run,Is.Null);Assert.That(restored.Legger.OccupiedSlotCount,Is.EqualTo(2));
                Assert.That(restored.State.commons,Is.EqualTo(50));
                Assert.Throws<InvalidOperationException>(()=>CampExpeditionSession.PrepareExtraction(restored,beacon));
            }
            finally{Object.DestroyImmediate(owner);}
        }
        private sealed class MemoryFile:ICampExpeditionFile
        {
            internal CampExpeditionSnapshot saved;internal bool failBefore,failAfter;internal int writes;
            public CampExpeditionSnapshot Read()=>saved?.Clone();
            public void Write(CampExpeditionSnapshot next){if(failBefore)throw new IOException("injected before replacement");saved=next.Clone();writes++;if(failAfter)throw new IOException("injected after replacement");}
        }
    }
}
