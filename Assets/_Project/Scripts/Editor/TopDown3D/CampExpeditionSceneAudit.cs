using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BooterBigArm.Editor
{
    public static class CampExpeditionSceneAudit
    {
        [Serializable] private sealed class Seed {public int seed,nodes,queries;public Vector3 origin;public string failure;public bool arrival,complete;public double milliseconds;}
        [Serializable] private sealed class Report {public string proof="Non-Play serialized configuration and canonical geometry queries only; no gameplay simulation, scene save, account write or actor movement.";public string unity,sceneSha256;public string[] terrainBounds,materials;public int terrains,eventSystems,missingScripts;public Seed[] seeds;}
        public static void AuditFromCli()
        {
            if(!Application.isBatchMode||Application.isPlaying||!File.Exists(".camp-expedition-validation"))throw new InvalidOperationException("Marked, non-Play validation candidate required.");
            // Scene opening unloads assets held only on a managed local stack. Load after the scene.
            var scene=EditorSceneManager.OpenScene(CampExpeditionDefinition.ScenePath,OpenSceneMode.Single);
            var configuration=Resources.Load<CampExpeditionConfiguration>("CampExpeditionConfiguration");
            if(configuration==null||!configuration.enabledForCampLoop||configuration.definition==null||configuration.menuPrefab==null||!configuration.definition.itemCatalog.TryValidate(out _))throw new InvalidOperationException("Private expedition configuration is incomplete.");
            var roots=scene.GetRootGameObjects();var all=roots.SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
            T Find<T>()where T:Component=>all.Select(t=>t.GetComponent<T>()).FirstOrDefault(t=>t!=null);
            var player=Find<TopDown3DPlayerMotor>();var legger=Find<TopDown3DBigArmFollower>();
            if(player==null||legger==null||Find<TopDown3DInputRouter>()==null||Find<TopDown3DPlayerInventory>()==null||Find<TopDown3DSurvivalVitals>()==null||Find<TopDown3DAnimationPlaygroundSession>()==null||Find<TopDown3DBigArmCargo>()==null)throw new InvalidOperationException("Saved scene actor/input bindings are incomplete.");
            var terrains=all.Select(t=>t.GetComponent<Terrain>()).Where(t=>t!=null&&t.isActiveAndEnabled).ToArray();
            Physics.SyncTransforms();var playerBefore=player.transform.position;var leggerBefore=legger.transform.position;var results=new List<Seed>();
            foreach(int seed in new[]{427,1021,4408,6004,10101,20210,30155,41999})
            {
                var watch=Stopwatch.StartNew();var row=new Seed{seed=seed};row.arrival=RootBeaconArrival.TryChoose(terrains,seed,player,legger,out var site);
                if(row.arrival)
                {row.origin=site.Beacon;var provider=new CampExpeditionTerrain(terrains,player.transform,legger.transform);row.complete=CampExpeditionPopulation.TryPrepare(configuration.definition,"audit-"+seed,seed,site.Beacon,provider.Support,provider.Route,out var nodes,out row.failure);row.nodes=nodes?.Length??0;row.queries=provider.Queries;}
                watch.Stop();row.milliseconds=watch.Elapsed.TotalMilliseconds;results.Add(row);
            }
            if(player.transform.position!=playerBefore||legger.transform.position!=leggerBefore)throw new InvalidOperationException("Read-only geometry audit moved an actor.");
            var report=new Report{unity=Application.unityVersion,sceneSha256=CampExpeditionFile.Hash(File.ReadAllText(CampExpeditionDefinition.ScenePath)),terrains=terrains.Length,eventSystems=all.Count(t=>t.GetComponent<UnityEngine.EventSystems.EventSystem>()!=null),missingScripts=all.Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)),seeds=results.ToArray()};
            report.terrainBounds=terrains.Select(t=>t.name+" / "+t.transform.position+" / "+t.terrainData.size).ToArray();report.materials=configuration.definition.items.Where(i=>i.visibleInTree&&i.inputs.Length==0).Select(i=>i.id).ToArray();
            File.WriteAllText("scene-geometry-audit.json",JsonUtility.ToJson(report,true));
            if(results.All(r=>!r.complete))throw new InvalidOperationException("No audited arrival supplied complete supported material routes; do not enable this layout.");
            UnityEngine.Debug.Log("Camp expedition configuration/geometry audit complete. Accepted "+results.Count(r=>r.complete)+" / "+results.Count+" fixed seeds; no Play Mode.");
        }
    }
}
