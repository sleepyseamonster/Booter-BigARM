using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class CampExpeditionSourceTests
    {
        private readonly List<GameObject> objects = new();
        private CampExpeditionDefinition definition;
        private CampExpeditionSourceLibrary library;
        private float time;
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly Vector3 Origin = new(32000,0,32000);
        [SetUp] public void Setup()
        {
            time=Time.timeScale;
            definition=AssetDatabase.LoadAssetAtPath<CampExpeditionDefinition>("Assets/_Project/Settings/UI/CampExpedition/ExpeditionDefinition.asset");
            library=CampExpeditionSourceLibrary.Load(definition);
        }
        [TearDown] public void Cleanup()
        {
            for(int i=objects.Count-1;i>=0;i--)if(objects[i]!=null)Object.DestroyImmediate(objects[i]);
            objects.Clear();Time.timeScale=time;
        }
        private GameObject Make(string name){var value=new GameObject(name);objects.Add(value);return value;}
        private CampExpeditionSourceView View(string id)
        {var value=Object.Instantiate(library.Find(id));objects.Add(value.gameObject);value.transform.position=Origin;return value;}
        private static void Set(object target,string field,object value)=>target.GetType().GetField(field,Flags).SetValue(target,value);
        private static object Call(object target,string method,params object[] args)=>target.GetType().GetMethod(method,Flags).Invoke(target,args);
        private CampExpeditionSession Session(CampExpeditionNodeRecord node,out CampExpeditionLedger ledger,out MemoryFile file,bool tool=true,int cargoSlots=12)
        {
            var configuration=Resources.Load<CampExpeditionConfiguration>("CampExpeditionConfiguration");
            var booter=new TopDown3DInventoryState(definition.itemCatalog,new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.Booter,CampExpeditionLayout.Capacity,expeditionLayout:true));
            if(tool)Assert.That(booter.TryAdd(new TopDown3DItemAmount(CampExpeditionDefinition.ToolZeroId,1)).Succeeded,Is.True);
            var legger=new TopDown3DInventoryState(definition.itemCatalog,new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.BigArm,cargoSlots));
            var storage=new TopDown3DInventoryState(definition.itemCatalog,new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.ResourceCache,24,campStorage:true));
            var snapshot=new CampExpeditionSnapshot{sequence=1,profileId=Guid.NewGuid().ToString("N"),contentFingerprint=configuration.ContentFingerprint,
                phase=CampExpeditionPhase.Exploring,booter=booter.CaptureSnapshot(),legger=legger.CaptureSnapshot(),storage=storage.CaptureSnapshot(),
                run=new CampExpeditionRunRecord{id="source-proof",terrainId=configuration.terrainIdentity,nodes=new[]{node}}};
            file=new MemoryFile{saved=snapshot.Clone()};ledger=new CampExpeditionLedger(file,snapshot,booter,legger,storage);
            var session=Make("Source test session").AddComponent<CampExpeditionSession>();
            Set(session,"<Configuration>k__BackingField",configuration);Set(session,"ledger",ledger);Set(session,"snapshot",ledger.State);Set(session,"frozen",false);
            Call(session,"RefreshCache");ledger.Changed+=()=>Call(session,"Committed");return session;
        }
        private CampExpeditionSource Bind(CampExpeditionSourceView view,CampExpeditionSession session,CampExpeditionNodeRecord node)
        {
            var source=view.Bind(session,node);
            ((List<CampExpeditionSource>)typeof(CampExpeditionSession).GetField("sourceViews",Flags).GetValue(session)).Add(source);
            return source;
        }
        private static CampExpeditionNodeRecord Node(string id,bool tool=false,int stock=1)=>new(){key="retained/source/001",itemId=id,position=Origin,requiresTool=tool,remaining=stock};
        [Test] public void AllNineRewardsHaveDistinctQualifiedSourcesAndConservativeCollision()
        {
            Assert.That(library.entries.Length,Is.EqualTo(9));Assert.That(library.entries.Select(e=>e.prefab).Distinct().Count(),Is.EqualTo(9));
            foreach(var entry in library.entries)
            {
                var view=View(entry.itemId);Assert.That(view.Valid,Is.True,entry.itemId);view.Present(Node(entry.itemId,true));Physics.SyncTransforms();
                var envelope=new Bounds(Origin+Vector3.up*.4f,new Vector3(1.1f,.8f,.9f));
                foreach(var shape in view.GetComponentsInChildren<Collider>())
                {Assert.That(view.Owns(shape),Is.True);Assert.That(envelope.Contains(shape.bounds.min)&&envelope.Contains(shape.bounds.max),Is.True,entry.itemId+"/"+shape.name);}
                Assert.That(view.GetComponentsInChildren<MeshRenderer>().Length,Is.GreaterThan(5));
                foreach(var renderer in view.GetComponentsInChildren<MeshRenderer>())Assert.That(renderer.sharedMaterial.shader.name,Is.EqualTo("Universal Render Pipeline/Lit"));
            }
        }
        [Test] public void AvailableAndExhaustedViewsRestoreWithoutChangingTheNodeOrCreatingBlockers()
        {
            foreach(var entry in library.entries)
            {
                var node=Node(entry.itemId,true);var before=JsonUtility.ToJson(node);var view=View(entry.itemId);view.Present(node);
                int available=view.GetComponentsInChildren<MeshRenderer>().Length;
                Assert.That(JsonUtility.ToJson(node),Is.EqualTo(before));node.remaining=0;view.Present(node);
                Assert.That(view.GetComponentsInChildren<MeshRenderer>().Length,Is.LessThan(available));
                Assert.That(view.GetComponentsInChildren<Collider>().All(c=>!c.enabled),Is.True);
                var restored=View(entry.itemId);restored.Present(JsonUtility.FromJson<CampExpeditionNodeRecord>(JsonUtility.ToJson(node)));
                Assert.That(restored.GetComponentsInChildren<MeshRenderer>().Length,Is.EqualTo(view.GetComponentsInChildren<MeshRenderer>().Length));
                view.gameObject.SetActive(false);view.gameObject.SetActive(true);Assert.That(node.remaining,Is.Zero);
            }
        }
        [Test] public void HarvestUpdatesTheRealPresenterOnlyAfterDurableCommitAndCannotRepeat()
        {
            var node=Node("author.bio.stone_bone");var session=Session(node,out var ledger,out var file);var view=View(node.itemId);var source=Bind(view,session,node);
            int before=view.GetComponentsInChildren<MeshRenderer>().Length;file.fail=true;
            var pending=ledger.Prepare("source.harvest",(next,drafts)=>{next.run.nodes[0].remaining--;drafts[1].TryAdd(new TopDown3DItemAmount(node.itemId,1));});
            Assert.That(ledger.Commit(pending,out _),Is.False);Assert.That(session.Node(node.key).remaining,Is.EqualTo(1));
            Assert.That(ledger.Legger.OccupiedSlotCount,Is.Zero);Assert.That(view.GetComponentsInChildren<MeshRenderer>().Length,Is.EqualTo(before));
            file.fail=false;Assert.That(source.TryCommitHarvest(ledger.Legger,out _),Is.True);
            Assert.That(view.GetComponentsInChildren<MeshRenderer>().Length,Is.LessThan(before));Assert.That(session.Node(node.key).remaining,Is.Zero);
            Assert.That(file.saved.run.nodes[0].remaining,Is.Zero);Assert.That(ledger.Legger.Slots.Sum(s=>s.Quantity),Is.EqualTo(1));
            Assert.That(source.TryCommitHarvest(ledger.Legger,out _),Is.False);Assert.That(ledger.Legger.Slots.Sum(s=>s.Quantity),Is.EqualTo(1));
        }
        [Test] public void InteractionUsesOwnedVolumesAndStillRejectsForeignObstruction()
        {
            var node=Node("proposal.bio.shell");var session=Session(node,out _,out _);var view=View(node.itemId);var source=Bind(view,session,node);
            var actor=Make("Reach actor");actor.transform.position=Origin+new Vector3(0,0,-1.4f);var motor=actor.AddComponent<TopDown3DPlayerMotor>();motor.enabled=false;
            var interaction=actor.AddComponent<TopDown3DInteractionController>();interaction.Configure(motor);Physics.SyncTransforms();
            Assert.That(interaction.CanReachTarget(source),Is.True);
            var wall=Make("Foreign obstruction");wall.transform.position=Origin+new Vector3(0,.45f,-.8f);wall.AddComponent<BoxCollider>().size=new Vector3(1,1,.1f);Physics.SyncTransforms();
            Assert.That(interaction.CanReachTarget(source),Is.False);
        }
        [Test] public void ToolGateAndCancelledGatherPreserveAvailableMaterial()
        {
            var node=Node("resource.ironstone_ore",true);var session=Session(node,out var ledger,out _,false);var source=Bind(View(node.itemId),session,node);
            Assert.That(source.IsAvailable,Is.False);Assert.That(source.TryCommitHarvest(ledger.Legger,out _),Is.False);
            Assert.That(ledger.Booter.TryAdd(new TopDown3DItemAmount(CampExpeditionDefinition.ToolZeroId,1)).Succeeded,Is.True);
            Assert.That(source.IsAvailable,Is.True);
            var gather=new TopDown3DGatherActionState(ledger.Legger);var actor=Origin+Vector3.back;
            Assert.That(gather.TryBegin(source,actor),Is.EqualTo(TopDown3DGatherActionResult.Started));gather.Cancel();
            Assert.That(session.Node(node.key).remaining,Is.EqualTo(1));Assert.That(ledger.Legger.OccupiedSlotCount,Is.Zero);
        }
        [Test] public void FullCargoRejectsHarvestWithoutRemovingGeometry()
        {
            var node=Node("proposal.bio.gripping_pad_scales");var session=Session(node,out var ledger,out _,true,1);var source=Bind(View(node.itemId),session,node);
            Assert.That(ledger.Legger.TryAdd(new TopDown3DItemAmount("resource.ironstone_ore",20)).Succeeded,Is.True);
            Assert.That(source.TryCommitHarvest(ledger.Legger,out _),Is.False);Assert.That(source.IsAvailable,Is.True);Assert.That(session.Node(node.key).remaining,Is.EqualTo(1));
        }
        [Test] public void ClearingFieldRemovesEveryViewWithoutChangingPersistentRunStock()
        {
            var node=Node("proposal.bio.shell");var session=Session(node,out var ledger,out _);var view=View(node.itemId);Bind(view,session,node);
            var population=Make("Disposable population");view.transform.SetParent(population.transform);Set(session,"population",population);
            Call(session,"ClearField");Assert.That(population==null||!population.activeSelf,Is.True);Assert.That(ledger.State.run.nodes[0].remaining,Is.EqualTo(1));
            Assert.That(((List<CampExpeditionSource>)typeof(CampExpeditionSession).GetField("sourceViews",Flags).GetValue(session)).Count,Is.Zero);
        }
        [Test] public void PresentationDoesNotChangeEconomicFingerprint()
        {
            var configuration=Resources.Load<CampExpeditionConfiguration>("CampExpeditionConfiguration");var before=configuration.ContentFingerprint;
            foreach(var entry in library.entries){var view=View(entry.itemId);view.Present(Node(entry.itemId,true,0));}
            Assert.That(configuration.ContentFingerprint,Is.EqualTo(before));Assert.That(CampExpeditionPopulation.Version,Is.EqualTo(1));
        }
        [Test] public void ProductionSourceReconstructionKeepsExhaustedSitesWithoutMovingSavedActors()
        {
            var node=Node("proposal.bio.shell",true,0);var session=Session(node,out var ledger,out _);
            var player=Make("Saved actor").AddComponent<TopDown3DPlayerMotor>();player.enabled=false;
            var legger=Make("Saved companion").AddComponent<TopDown3DBigArmFollower>();legger.enabled=false;
            var snapshot=ledger.State;snapshot.run.playerPosition=Origin+new Vector3(2,2,0);snapshot.run.leggerPosition=Origin+new Vector3(5,2,0);
            Set(session,"snapshot",snapshot);player.transform.position=snapshot.run.playerPosition;legger.transform.position=snapshot.run.leggerPosition;
            var before=JsonUtility.ToJson(snapshot);Set(session,"population",Make("Source reconstruction"));Call(session,"RestoreSources",library);
            var views=(List<CampExpeditionSource>)typeof(CampExpeditionSession).GetField("sourceViews",Flags).GetValue(session);
            Assert.That(views.Count,Is.EqualTo(1));Assert.That(views[0].StableId,Is.EqualTo(node.key));Assert.That(views[0].transform.position,Is.EqualTo(node.position));
            Assert.That(views[0].GetComponentsInChildren<Collider>().All(c=>!c.enabled),Is.True);
            Assert.That(player.transform.position,Is.EqualTo(snapshot.run.playerPosition));Assert.That(legger.transform.position,Is.EqualTo(snapshot.run.leggerPosition));
            Call(session,"ClearField");Set(session,"population",Make("Restored source reconstruction"));Call(session,"RestoreSources",library);
            Assert.That(views.Count,Is.EqualTo(1));Assert.That(JsonUtility.ToJson(snapshot),Is.EqualTo(before));
        }
        private sealed class MemoryFile:ICampExpeditionFile
        {
            public CampExpeditionSnapshot saved;public bool fail;
            public CampExpeditionSnapshot Read()=>saved.Clone();
            public void Write(CampExpeditionSnapshot candidate){if(fail)throw new IOException("Simulated source-save failure");saved=candidate.Clone();}
        }
    }
}
