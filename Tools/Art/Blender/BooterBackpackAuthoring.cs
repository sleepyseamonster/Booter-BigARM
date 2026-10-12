using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using BooterBigArm.TopDown3D;

// Invoked with Unity CLI run_script; outside Assets to avoid disturbing an active Play session.
public static class BooterBackpackAuthoring
{
    const string Model = "Assets/_Project/Art/Characters/Booter/Backpack/BooterFramedBackpack.fbx";
    const string Cloak = "Assets/_Project/Prefabs/TopDown3D/Environment/Camp/WornCloak.prefab";
    const string Pack = "Assets/_Project/Prefabs/TopDown3D/Environment/Camp/FramedBackpack.prefab";
    public static string Build()
    {
        AssetDatabase.ImportAsset(Model, ImportAssetOptions.ForceSynchronousImport);
        var importer = (ModelImporter)AssetImporter.GetAtPath(Model);
        importer.isReadable = true; importer.importAnimation = false;
        importer.importCameras = false; importer.importLights = false; importer.SaveAndReimport();
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(Model);
        if (!model) throw new Exception("Pack import failed");
        var root = new GameObject("FramedBackpack");
        try
        {
            var visual = UnityEngine.Object.Instantiate(model, root.transform, false);
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>())
                r.sharedMaterials = r.sharedMaterials.Select(m => Material(m.name)).ToArray();
            PrefabUtility.SaveAsPrefabAsset(root, Pack);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
        var contents = PrefabUtility.LoadPrefabContents(Cloak);
        try
        {
            if (!contents.transform.Find("FramedBackpack"))
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Pack), contents.transform);
                instance.name = "FramedBackpack";
            }
            PrefabUtility.SaveAsPrefabAsset(contents, Cloak);
        }
        finally { PrefabUtility.UnloadPrefabContents(contents); }
        return "Pack prefab created and attached to canonical worn cloak. No production scene saved.";
    }
    static Material Material(string name)
    {
        var path = "Assets/_Project/Materials/TopDown3D/Camp/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m,path); }
        var frame = name.Contains("Frame"); var strap = name.Contains("Webbing");
        m.SetColor("_BaseColor", frame ? new Color(.24f,.23f,.20f) : strap ? new Color(.095f,.075f,.052f) : new Color(.32f,.225f,.13f));
        m.SetFloat("_Metallic", frame ? .7f : 0); m.SetFloat("_Smoothness", frame ? .28f : .1f);
        EditorUtility.SetDirty(m); AssetDatabase.SaveAssetIfDirty(m); return m;
    }
    public static string Capture()
    {
        if (EditorApplication.isPlaying) throw new Exception("Capture requires Edit Mode; never stop the user's session automatically.");
        var source=UnityEngine.Object.FindObjectsByType<TopDown3DPlayerAnimationDriver>().Single();
        var scene=EditorSceneManager.NewPreviewScene();
        var stage=new GameObject("Backpack inspection stage");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(stage,scene);
        var pose=UnityEngine.Object.Instantiate(source.gameObject);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(pose,scene);
        pose.transform.position=Vector3.zero; pose.transform.rotation=Quaternion.identity;
        var driver=pose.GetComponent<TopDown3DPlayerAnimationDriver>();
        var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
        try
        {
            typeof(TopDown3DPlayerAnimationDriver).GetMethod("Awake",flags).Invoke(driver,null);
            if (!driver.ConfigureCamp(TopDown3DCampSettings.Load())) throw new Exception("Camp binding failed");
            var graph=(UnityEngine.Playables.PlayableGraph)typeof(TopDown3DPlayerAnimationDriver).GetField("playableGraph",flags).GetValue(driver);
            graph.Evaluate(0);
            typeof(TopDown3DPlayerAnimationDriver).GetMethod("LateUpdate",flags).Invoke(driver,null);
            var animator=driver.HumanoidAnimator;
            // Bake the actual posed Booter and cloak into an isolated render stage.
            foreach(var r in animator.GetComponentsInChildren<Renderer>().Where(r=>r.enabled && r.gameObject.activeInHierarchy))
            {
                var skin=r as SkinnedMeshRenderer; if (!skin) continue;
                var mesh=new Mesh(); skin.BakeMesh(mesh);
                var o=new GameObject(r.name); o.transform.SetParent(stage.transform,false);
                mesh.vertices=mesh.vertices.Select(v=>animator.transform.InverseTransformPoint(r.transform.TransformPoint(v))*1.4f).ToArray();
                mesh.RecalculateBounds(); o.AddComponent<MeshFilter>().sharedMesh=mesh;
                o.AddComponent<MeshRenderer>().sharedMaterials=r.sharedMaterials;
            }
            pose.SetActive(false);
            var sun=new GameObject("Review sun"); sun.transform.SetParent(stage.transform,false);
            var light=sun.AddComponent<Light>(); light.type=LightType.Directional; light.intensity=1.8f;
            sun.transform.rotation=Quaternion.Euler(40,-35,0);
            var cameraObject=new GameObject("Review camera"); cameraObject.transform.SetParent(stage.transform,false);
            var camera=cameraObject.AddComponent<Camera>(); camera.scene=scene;
            var cameraData=cameraObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing=false;
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.18f,.18f,.17f);
            var bounds=stage.GetComponentsInChildren<Renderer>().First().bounds;
            foreach(var r in stage.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(r.bounds);
            camera.orthographic=true; camera.orthographicSize=bounds.extents.y*1.22f;
            var target=bounds.center;
            Directory.CreateDirectory("Docs/Evidence/Art/BooterBackpack");
            foreach(var view in new[]{"rear","side","front","elevated"})
            {
                var pos=view=="rear"?new Vector3(3,2.3f,-5):view=="side"?new Vector3(5,1.9f,0):view=="front"?new Vector3(-3,2.2f,5):new Vector3(3,5,-4);
                camera.transform.position=target+pos; camera.transform.LookAt(target);
                var rt=RenderTexture.GetTemporary(1000,1000,24); var previous=RenderTexture.active;
                try
                {
                    camera.targetTexture=rt; camera.Render(); RenderTexture.active=rt;
                    var image=new Texture2D(1000,1000,TextureFormat.RGB24,false);
                    image.ReadPixels(new Rect(0,0,1000,1000),0,0); image.Apply();
                    File.WriteAllBytes("Docs/Evidence/Art/BooterBackpack/"+view+".png",image.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(image);
                }
                finally {camera.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);}
            }
        }
        finally
        {
            foreach(var f in stage.GetComponentsInChildren<MeshFilter>()) UnityEngine.Object.DestroyImmediate(f.sharedMesh);
            typeof(TopDown3DPlayerAnimationDriver).GetMethod("OnDestroy",flags).Invoke(driver,null);
            UnityEngine.Object.DestroyImmediate(pose);
            UnityEngine.Object.DestroyImmediate(stage); EditorSceneManager.ClosePreviewScene(scene);
        }
        return "Four views captured from actual Unity rig. No scene changes or gameplay replay.";
    }
}
