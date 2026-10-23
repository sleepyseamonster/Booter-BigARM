using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace BooterBigArm.Editor
{
    public static class CampExpeditionSourceValidation
    {
        [Serializable] private sealed class Report
        {
            public string proof="Static EditMode geometry, native placement and offscreen renders; no gameplay/feel acceptance";
            public string fingerprintBefore,fingerprintAfter;
            public int nativeSeeds,nativeNodes,sourceFamilies=9;
            public float nativeCameraDistance,nativeCameraPitch,nativeCameraFieldOfView;
            public bool sceneBytesPreserved;
            public Row[] sources;
        }
        [Serializable] private sealed class Row {public string id,name;public int renderers,triangles;public Vector3 visualSize;}
        public const string Output="Logs/ExpeditionSources";
        private static void Require(bool test,string message){if(!test)throw new InvalidOperationException(message);}
        private static void Guard()=>Require(Application.isBatchMode&&!Directory.Exists(".git")&&File.Exists(".camp-source-validation")&&!Application.isPlaying,"Marked offline source candidate only.");
        public static void ValidateAndCaptureFromCli()
        {
            Guard();Directory.CreateDirectory(Output);
            var sceneBefore=File.ReadAllBytes(CampExpeditionDefinition.ScenePath);
            var scene=EditorSceneManager.OpenScene(CampExpeditionDefinition.ScenePath,OpenSceneMode.Single);
            var definition=AssetDatabase.LoadAssetAtPath<CampExpeditionDefinition>("Assets/_Project/Settings/UI/CampExpedition/ExpeditionDefinition.asset");
            var configuration=Resources.Load<CampExpeditionConfiguration>("CampExpeditionConfiguration");
            var library=CampExpeditionSourceLibrary.Load(definition);var report=new Report{fingerprintBefore=configuration.ContentFingerprint};
            var roots=scene.GetRootGameObjects();var terrains=roots.SelectMany(r=>r.GetComponentsInChildren<Terrain>(false)).ToArray();
            var player=roots.SelectMany(r=>r.GetComponentsInChildren<TopDown3DPlayerMotor>(false)).Single();
            var legger=roots.SelectMany(r=>r.GetComponentsInChildren<TopDown3DBigArmFollower>(false)).Single();
            foreach(var seed in new[]{0,1,2,3,42,427,713,20261022})
            {
                Physics.SyncTransforms();Require(RootBeaconArrival.TryChoose(terrains,seed,player,legger,out var site),"Missing native arrival for "+seed);
                var support=new CampExpeditionTerrain(terrains,player.transform,legger.transform);
                Require(CampExpeditionPopulation.TryPrepare(definition,"source-validation-"+seed,seed,site.Beacon,support.Support,support.Route,out var nodes,out var error),"Native route failed: "+error);
                Require(nodes.Length==13,"Population changed.");
                var views=new List<CampExpeditionSourceView>();
                try
                {
                    foreach(var node in nodes)
                    {
                        var view=Object.Instantiate(library.Find(node.itemId));SceneManager.MoveGameObjectToScene(view.gameObject,scene);
                        view.transform.position=node.position;view.Present(node);views.Add(view);
                    }
                    Physics.SyncTransforms();
                    if(report.nativeSeeds==0)
                    {
                        var nativeCamera=roots.SelectMany(r=>r.GetComponentsInChildren<Camera>(true)).Single(c=>c.GetComponent<TopDown3DCameraRig>()!=null);
                        Require(nativeCamera!=null,"Missing authored native camera.");var rig=nativeCamera.GetComponent<TopDown3DCameraRig>();
                        report.nativeCameraDistance=rig.Distance;report.nativeCameraPitch=rig.PitchDegrees;report.nativeCameraFieldOfView=nativeCamera.fieldOfView;
                        var rotation=Quaternion.Euler(rig.PitchDegrees,rig.YawDegrees,0);
                        foreach(int index in new[]{0,3,6,9})
                        {
                            nativeCamera.transform.SetPositionAndRotation(nodes[index].position+Vector3.up*.5f-rotation*Vector3.forward*rig.Distance,rotation);
                            Capture(nativeCamera,Output+"/native-"+nodes[index].itemId+".png",1536,864);
                        }
                    }
                    for(int i=0;i<nodes.Length;i++)
                    {
                        var envelope=new Bounds(nodes[i].position+Vector3.up*.4f,new Vector3(1.1f,.8f,.9f));
                        foreach(var shape in views[i].GetComponentsInChildren<Collider>())Require(envelope.Contains(shape.bounds.min)&&envelope.Contains(shape.bounds.max),"New obstruction outside retained source bounds.");
                        nodes[i].remaining=0;views[i].Present(nodes[i]);Require(views[i].GetComponentsInChildren<Collider>().All(c=>!c.enabled),"Spent site blocks restored traversal.");
                    }
                    report.nativeSeeds++;report.nativeNodes+=nodes.Length;
                }
                finally{foreach(var view in views)Object.DestroyImmediate(view.gameObject);}
            }
            // A temporary rendering scene only. Never saves or starts the PlayerLoop.
            scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            definition=AssetDatabase.LoadAssetAtPath<CampExpeditionDefinition>("Assets/_Project/Settings/UI/CampExpedition/ExpeditionDefinition.asset");
            configuration=Resources.Load<CampExpeditionConfiguration>("CampExpeditionConfiguration");library=CampExpeditionSourceLibrary.Load(definition);
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.43f,.4f,.37f);RenderSettings.fog=false;
            var light=new GameObject("Static study sun").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.4f;light.color=new Color(1,.86f,.7f);light.transform.rotation=Quaternion.Euler(48,-30,0);
            var fill=new GameObject("Static cool fill").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.5f;fill.color=new Color(.65f,.73f,1);fill.transform.rotation=Quaternion.Euler(32,150,0);
            var camera=new GameObject("Source review camera").AddComponent<Camera>();camera.backgroundColor=new Color(.075f,.065f,.06f);camera.clearFlags=CameraClearFlags.SolidColor;camera.nearClipPlane=.05f;camera.farClipPlane=100;camera.fieldOfView=45;
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Preview ground";floor.transform.position=new Vector3(0,-.055f,0);floor.transform.localScale=new Vector3(12,.1f,12);Object.DestroyImmediate(floor.GetComponent<Collider>());
            floor.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(CampExpeditionSourceAuthoring.Art+"/CompactedDust.mat");
            var rows=new List<Row>();var grid=new List<CampExpeditionSourceView>();
            foreach(var entry in library.entries)
            {
                var view=Object.Instantiate(entry.prefab);var record=new CampExpeditionNodeRecord{itemId=entry.itemId,remaining=1};view.Present(record);
                var renderers=view.GetComponentsInChildren<MeshRenderer>();var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
                rows.Add(new Row{id=entry.itemId,name=view.SourceName,renderers=renderers.Length,triangles=view.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.triangles.Length/3),visualSize=bounds.size});
                camera.orthographic=false;camera.transform.position=new Vector3(.95f,1.65f,-2.05f);camera.transform.LookAt(new Vector3(0,.2f,0));
                Capture(camera,Output+"/"+entry.itemId+"-available.png",1024,768);
                record.requiresTool=true;view.Present(record);Capture(camera,Output+"/"+entry.itemId+"-lodged.png",1024,768);
                record.remaining=0;view.Present(record);Capture(camera,Output+"/"+entry.itemId+"-spent.png",1024,768);Object.DestroyImmediate(view.gameObject);
            }
            for(int i=0;i<library.entries.Length;i++)
            {
                var view=Object.Instantiate(library.entries[i].prefab);view.transform.position=new Vector3((i%3-1)*1.8f,0,(1-i/3)*1.65f);
                view.Present(new CampExpeditionNodeRecord{remaining=1});grid.Add(view);
            }
            camera.orthographic=true;camera.orthographicSize=3.7f;camera.transform.position=new Vector3(3.6f,6.5f,-7.5f);camera.transform.LookAt(new Vector3(0,.1f,0));
            Capture(camera,Output+"/source-kit.png",1536,1152);
            foreach(var view in grid)Object.DestroyImmediate(view.gameObject);
            report.sources=rows.ToArray();report.fingerprintAfter=configuration.ContentFingerprint;
            report.sceneBytesPreserved=sceneBefore.SequenceEqual(File.ReadAllBytes(CampExpeditionDefinition.ScenePath));
            Require(report.sceneBytesPreserved&&report.fingerprintBefore==report.fingerprintAfter,"Protected source or fingerprint changed.");
            File.WriteAllText(Output+"/verification.json",JsonUtility.ToJson(report,true));
            Debug.Log("EXPEDITION_SOURCES_VERIFIED: "+report.nativeSeeds+" native seeds / "+report.nativeNodes+" nodes; 32 offscreen views; protected fingerprint and scene unchanged.");
        }
        private static void Capture(Camera camera,string path,int width,int height)
        {
            var before=RenderTexture.active;var target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);var pixels=new Texture2D(width,height,TextureFormat.RGB24,false);
            try{camera.targetTexture=target;camera.Render();TopDown3DCampOpticsStack.Restore(camera);RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();File.WriteAllBytes(path,pixels.EncodeToPNG());}
            finally{camera.targetTexture=null;RenderTexture.active=before;Object.DestroyImmediate(pixels);Object.DestroyImmediate(target);}
        }
    }
}
