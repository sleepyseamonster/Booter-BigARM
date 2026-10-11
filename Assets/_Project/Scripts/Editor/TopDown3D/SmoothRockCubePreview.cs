using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace BooterBigArm.Editor
{
    /// <summary>Automatic Edit Mode display only. No panel, controls, source reparenting or scene save.</summary>
    [InitializeOnLoad]
    public static class SmoothRockCubePreview
    {
        internal const string MaterialPath="Assets/_Project/Materials/TopDown3D/Rocks/SmoothCube_Volcanic.mat";
        private const string VolcanicFolder="Assets/_Project/Art/Environment/Rocks/SmoothCubeVolcanic";
        public const string PrefabPath="Assets/_Project/Prefabs/TopDown3D/Environment/SmoothRockCube.prefab";
        private sealed class Node
        { internal SmoothRockCube Source; internal MeshRenderer Renderer; internal SmoothRockBox Box; }
        private sealed class Group
        {
            internal string Key, Signature;
            internal Node[] Nodes;
            internal double Due;
            internal bool Attempted;
            internal GameObject Display;
            internal Mesh Mesh;
        }
        private sealed class Completed
        { internal SmoothRockField Field; internal TopDown3DRockWorkbenchBuildResult Result; internal double Milliseconds; }
        private sealed class Job
        { internal string Key, Signature; internal CancellationTokenSource Cancellation; internal Task<Completed> Task; }
        private static readonly Dictionary<string,Group> Groups=new Dictionary<string,Group>();
        private static readonly HashSet<MeshRenderer> Registered=new HashSet<MeshRenderer>();
        private static readonly Dictionary<int,string> Invalid=new Dictionary<int,string>();
        private static Job worker;
        private static double nextScan;
        private static Ray lastSceneRay;
        internal static bool Suspended;
        internal static int PreviewCount => Groups.Values.Count(g=>g.Display!=null);
        internal static Mesh PreviewMesh => Groups.Values.Select(g=>g.Mesh).FirstOrDefault(m=>m!=null);
        internal static bool IsIdle => worker==null && Groups.Values.All(g=>g.Attempted || DistinctCount(g.Nodes)==1);
        internal static double LastBuildMilliseconds,LastApplyMilliseconds;

        static SmoothRockCubePreview()
        {
            EditorApplication.update+=()=>Tick();
            AssemblyReloadEvents.beforeAssemblyReload+=Clear;
            EditorApplication.quitting+=Clear;
            EditorApplication.playModeStateChanged+=_=>Clear();
            Undo.undoRedoPerformed+=Clear;
            EditorSceneManager.sceneClosing+=(scene,removing)=>Clear();
            EditorSceneManager.sceneSaving+=(scene,path)=>Clear();
            Selection.selectionChanged+=RedirectSelection;
            SceneView.duringSceneGui+=SceneGui;
        }

        internal static void Tick(bool force=false)
        {
            if(Suspended || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            { if(Groups.Count>0 || Registered.Count>0) Clear(); return; }
            var now=EditorApplication.timeSinceStartup;
            if(!force && now<nextScan) return;
            nextScan=now+.08;
            var nodes=new List<Node>();
            foreach(var source in Resources.FindObjectsOfTypeAll<SmoothRockCube>().OrderBy(s=>s.GetInstanceID()))
            {
                if(source==null || EditorUtility.IsPersistent(source) || !source.gameObject.scene.IsValid() || !source.gameObject.scene.isLoaded
                    || EditorSceneManager.IsPreviewScene(source.gameObject.scene) || PrefabStageUtility.GetPrefabStage(source.gameObject)!=null
                    || (source.gameObject.hideFlags & HideFlags.DontSaveInEditor)!=0) continue;
                var renderer=source.GetComponent<MeshRenderer>();
                if(renderer==null) continue;
                // This service exclusively owns forceRenderingOff on its marked cube renderers.
                // New duplicates may inherit the temporary culling flag; enabled remains author-owned.
                if(Registered.Add(renderer)) renderer.forceRenderingOff=false;
                if(!source.isActiveAndEnabled || !renderer.enabled) continue;
                try
                {
                    if(renderer.sharedMaterial==null || renderer.sharedMaterial.shader.name!="BooterBigArm/TopDown3D/Broken World Rock Workbench PBR"
                        || source.GetComponent<MeshFilter>().sharedMesh==null)
                        throw new InvalidOperationException("Rock cube needs its existing rock material and box mesh.");
                    var box=SmoothRockBox.Capture(source.transform);
                    ApplyMaterial(renderer);
                    nodes.Add(new Node {Source=source,Renderer=renderer,Box=box});
                    Invalid.Remove(source.GetInstanceID());
                }
                catch(Exception exception)
                {
                    renderer.forceRenderingOff=false;
                    int id=source.GetInstanceID();
                    if(!Invalid.TryGetValue(id,out var message) || message!=exception.Message)
                    { Invalid[id]=exception.Message; UnityEngine.Debug.LogWarning(exception.Message,source); }
                }
            }
            var pending=new HashSet<Node>(nodes);
            var found=new HashSet<string>();
            while(pending.Count>0)
            {
                var first=pending.First(); pending.Remove(first);
                var connected=new List<Node> {first};
                for(int i=0;i<connected.Count;i++) foreach(var other in pending.ToArray())
                    if(connected[i].Source.gameObject.scene==other.Source.gameObject.scene && SmoothRockBox.Touches(connected[i].Box,other.Box))
                    { connected.Add(other); pending.Remove(other); }
                var members=connected.OrderBy(n=>n.Source.GetInstanceID()).ToArray();
                var key=string.Join("_",members.Select(n=>n.Source.GetInstanceID()));
                var signature=string.Join("|",members.Select(n=>n.Source.transform.localToWorldMatrix.ToString("R")+":"+n.Renderer.sharedMaterial.GetInstanceID()));
                found.Add(key);
                if(!Groups.TryGetValue(key,out var group))
                { group=new Group {Key=key,Signature=signature,Nodes=members,Due=now+.15}; Groups.Add(key,group); }
                else if(group.Signature!=signature)
                {
                    Retire(group);
                    if(worker?.Key==key) worker.Cancellation.Cancel();
                    group.Signature=signature; group.Nodes=members; group.Due=now+.15; group.Attempted=false;
                }
                if(DistinctCount(members)==1)
                {
                    group.Attempted=true;
                    for(int i=0;i<members.Length;i++) members[i].Renderer.forceRenderingOff=i>0;
                }
            }
            foreach(var key in Groups.Keys.Where(k=>!found.Contains(k)).ToArray())
            { if(worker?.Key==key) worker.Cancellation.Cancel(); Retire(Groups[key]); Groups.Remove(key); }
            if(worker!=null && worker.Task.IsCompleted) CompleteWorker();
            if(worker==null && GUIUtility.hotControl==0)
            {
                var group=Groups.Values.FirstOrDefault(g=>!g.Attempted && now>=g.Due);
                if(group!=null) StartWorker(group);
            }
            Registered.RemoveWhere(r=>r==null);
        }

        private static int DistinctCount(Node[] nodes)
        {
            int count=0;
            for(int i=0;i<nodes.Length;i++) if(!nodes.Take(i).Any(n=>n.Box.SameGeometry(nodes[i].Box))) count++;
            return count;
        }

        private static void StartWorker(Group group)
        {
            group.Attempted=true;
            if(group.Nodes.Any(n=>n.Renderer.sharedMaterial!=group.Nodes[0].Renderer.sharedMaterial))
            { UnityEngine.Debug.LogWarning("Touching rock cubes must share the same rock material; sources remain visible.",group.Nodes[0].Source); return; }
            var boxes=group.Nodes.Select(n=>n.Box).ToArray();
            var cancellation=new CancellationTokenSource(); var token=cancellation.Token;
            worker=new Job {Key=group.Key,Signature=group.Signature,Cancellation=cancellation,Task=Task.Run(()=>
            {
                var clock=Stopwatch.StartNew();
                var field=SmoothRockCubeGeometry.BuildField(boxes,token);
                if(!TopDown3DRockWorkbenchMesher.TryBuildSampledField(field.Bounds,field.Cells,field.Values,field.Spacing,token,out var result,out var error,SmoothRockCubeGeometry.MaximumFineSamples))
                    throw new InvalidOperationException(error);
                if(result.ConnectedComponents!=1) throw new InvalidOperationException("The cube junction is unresolved; sources remain visible.");
                return new Completed {Field=field,Result=result,Milliseconds=clock.Elapsed.TotalMilliseconds};
            },token)};
        }

        private static void CompleteWorker()
        {
            var job=worker; worker=null;
            try
            {
                if(job.Cancellation.IsCancellationRequested || job.Task.IsCanceled)
                { if(job.Task.IsFaulted) _=job.Task.Exception; return; }
                if(!Groups.TryGetValue(job.Key,out var group) || group.Signature!=job.Signature) return;
                if(job.Task.IsFaulted) throw job.Task.Exception.GetBaseException();
                var completed=job.Task.Result;
                var apply=Stopwatch.StartNew();
                group.Mesh=completed.Result.CreateMesh("Smooth cube rock exterior");
                group.Mesh.hideFlags=HideFlags.HideAndDontSave;
                group.Display=new GameObject("Smooth cube rock exterior",typeof(MeshFilter),typeof(MeshRenderer));
                group.Display.hideFlags=HideFlags.HideInHierarchy|HideFlags.DontSaveInEditor|HideFlags.DontSaveInBuild;
                SceneManager.MoveGameObjectToScene(group.Display,group.Nodes[0].Source.gameObject.scene);
                group.Display.transform.position=completed.Field.Origin;
                group.Display.GetComponent<MeshFilter>().sharedMesh=group.Mesh;
                var renderer=group.Display.GetComponent<MeshRenderer>();
                renderer.sharedMaterial=group.Nodes[0].Renderer.sharedMaterial;
                ApplyMaterial(renderer);
                foreach(var node in group.Nodes) if(node.Renderer!=null) node.Renderer.forceRenderingOff=true;
                LastBuildMilliseconds=completed.Milliseconds; LastApplyMilliseconds=apply.Elapsed.TotalMilliseconds;
                SceneView.RepaintAll();
            }
            catch(Exception exception)
            {
                if(Groups.TryGetValue(job.Key,out var group))
                { Retire(group); UnityEngine.Debug.LogWarning("Cube blending: "+exception.Message,group.Nodes.FirstOrDefault()?.Source); }
            }
            finally { job.Cancellation.Dispose(); }
        }

        private static void Retire(Group group)
        {
            foreach(var node in group.Nodes) if(node.Renderer!=null) node.Renderer.forceRenderingOff=false;
            if(group.Display!=null) Object.DestroyImmediate(group.Display);
            if(group.Mesh!=null) Object.DestroyImmediate(group.Mesh);
            group.Display=null; group.Mesh=null;
        }

        public static void Clear()
        {
            if(worker!=null) worker.Cancellation.Cancel();
            foreach(var group in Groups.Values) Retire(group);
            Groups.Clear();
            foreach(var renderer in Registered) if(renderer!=null) renderer.forceRenderingOff=false;
            Registered.Clear(); Invalid.Clear(); nextScan=0;
        }

        private static void SceneGui(SceneView view)
        {
            lastSceneRay=HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            var selected=Selection.activeGameObject?.GetComponent<SmoothRockCube>();
            if(selected==null || !selected.isActiveAndEnabled) return;
            var previous=Handles.matrix; var color=Handles.color;
            Handles.matrix=selected.transform.localToWorldMatrix; Handles.color=new Color(1f,.75f,.2f,1f);
            Handles.DrawWireCube(Vector3.zero,Vector3.one);
            Handles.matrix=previous; Handles.color=color;
        }

        private static void RedirectSelection()
        {
            var group=Groups.Values.FirstOrDefault(g=>g.Display==Selection.activeGameObject && g.Display!=null);
            if(group!=null) Selection.activeGameObject=PickSource(group,lastSceneRay)?.gameObject;
        }

        private static SmoothRockCube PickSource(Group group,Ray ray)
        {
            // Intersect editable boxes. For a hit in a newly filled junction, use nearest box to ray.
            return group.Nodes.Where(n=>n.Source!=null).OrderBy(n=>
            {
                if(n.Box.Raycast(ray,out var distance)) return distance;
                var along=Mathf.Max(0f,Vector3.Dot(n.Box.Center-ray.origin,ray.direction));
                return 10000f+n.Box.Distance(ray.GetPoint(along));
            }).ThenBy(n=>n.Box.Center.x).ThenBy(n=>n.Box.Center.y).ThenBy(n=>n.Box.Center.z).FirstOrDefault()?.Source;
        }

        internal static void ApplyMaterial(MeshRenderer renderer)
        {
            var properties=new MaterialPropertyBlock();
            properties.SetVector("_RockOriginWS",Vector4.zero);
            properties.SetVector("_RockSize",Vector3.one);
            properties.SetFloat("_RockSeed01",.5f);
            properties.SetFloat("_RockMetersPerTile",1.25f);
            properties.SetFloat("_SurfacePatchStrength",0f);
            properties.SetFloat("_CrackAmount",0f);
            properties.SetFloat("_SideGritAmount",0f);
            properties.SetFloat("_UndersideShaleAmount",0f);
            properties.SetFloat("_SideShalePatchAmount",0f);
            properties.SetFloat("_TopShalePatchAmount",0f);
            properties.SetFloat("_WornSmoothnessBoost",0f);
            properties.SetFloat("_GeologicalSeamAmount",0f);
            properties.SetFloat("_FormationFractureAmount",0f);
            properties.SetColor("_BaseColor",Color.white);
            properties.SetColor("_CrackColor",new Color(.018f,.015f,.013f,1f));
            properties.SetColor("_MineralColor",new Color(.58f,.51f,.41f,1f));
            properties.SetColor("_DustColor",new Color(.32f,.3f,.28f,1f));
            properties.SetFloat("_SmoothnessMin",0f);
            properties.SetFloat("_SmoothnessMax",1f);
            renderer.SetPropertyBlock(properties);
        }

        /// <summary>Delivery automation only; imports the existing volcanic maps, never alters their source.</summary>
        public static void ConfigureVolcanicMaterial()
        {
            foreach(var kind in new[]{"Albedo","Normal","Surface"})
            {
                var path=VolcanicFolder+"/VolcanicBlackBasalt_"+kind+".png";
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureShape=TextureImporterShape.Texture2D;
                importer.textureType=kind=="Normal"?TextureImporterType.NormalMap:TextureImporterType.Default;
                importer.sRGBTexture=kind=="Albedo";
                importer.wrapMode=TextureWrapMode.Repeat; importer.filterMode=FilterMode.Trilinear;
                importer.mipmapEnabled=true; importer.anisoLevel=4; importer.maxTextureSize=1024;
                importer.textureCompression=TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
            }
            var template=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/TopDown3D/RockWorkbench_NeutralPBR.mat");
            var material=AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if(material==null) { material=new Material(template); AssetDatabase.CreateAsset(material,MaterialPath); }
            material.name="SmoothCube_Volcanic";
            var albedo=AssetDatabase.LoadAssetAtPath<Texture2D>(VolcanicFolder+"/VolcanicBlackBasalt_Albedo.png");
            var normal=AssetDatabase.LoadAssetAtPath<Texture2D>(VolcanicFolder+"/VolcanicBlackBasalt_Normal.png");
            var surface=AssetDatabase.LoadAssetAtPath<Texture2D>(VolcanicFolder+"/VolcanicBlackBasalt_Surface.png");
            if(albedo==null || normal==null || surface==null)
                throw new InvalidOperationException("Volcanic maps must import as 2D textures before assigning the cube material.");
            foreach(var name in new[]{"_BaseMap","_TopBaseMap","_BottomBaseMap","_GritBaseMap"}) material.SetTexture(name,albedo);
            foreach(var name in new[]{"_NormalMap","_TopNormalMap","_BottomNormalMap","_GritNormalMap"}) material.SetTexture(name,normal);
            foreach(var name in new[]{"_SurfaceMap","_TopSurfaceMap","_BottomSurfaceMap","_GritSurfaceMap"}) material.SetTexture(name,surface);
            material.SetTexture("_CrackMap",Texture2D.blackTexture);
            material.SetColor("_BaseColor",Color.white);
            foreach(var name in new[]{"_CrackAmount","_SurfacePatchStrength","_SideGritAmount","_UndersideShaleAmount","_SideShalePatchAmount","_TopShalePatchAmount","_DustAmount","_MacroStrength","_HeightColorStrength","_WornSmoothnessBoost","_MineralShine","_SmoothnessMin"}) material.SetFloat(name,0f);
            material.SetFloat("_RockMetersPerTile",1.25f); material.SetFloat("_SmoothnessMax",1f);
            material.SetFloat("_OcclusionStrength",.6f);
            foreach(var name in new[]{"_NormalStrength","_BottomNormalStrength","_GritNormalStrength"}) material.SetFloat(name,.65f);
            EditorUtility.SetDirty(material); AssetDatabase.SaveAssetIfDirty(material);
            var root=PrefabUtility.LoadPrefabContents(PrefabPath);
            try { root.GetComponent<MeshRenderer>().sharedMaterial=material; PrefabUtility.SaveAsPrefabAsset(root,PrefabPath); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        internal static GameObject CreateCube(Vector3 position)
        {
            var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name="Rock cube";
            Object.DestroyImmediate(cube.GetComponent<BoxCollider>());
            cube.transform.position=position;
            cube.AddComponent<SmoothRockCube>();
            cube.GetComponent<MeshRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            ApplyMaterial(cube.GetComponent<MeshRenderer>());
            return cube;
        }

        public static void CreateStarterPrefab()
        {
            if(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)!=null) return;
            bool previous=Suspended; Suspended=true;
            GameObject cube=null;
            try
            {
                cube=CreateCube(Vector3.zero);
                PrefabUtility.SaveAsPrefabAsset(cube,PrefabPath);
            }
            finally { if(cube!=null) Object.DestroyImmediate(cube); Suspended=previous; }
        }

        public static GameObject PlaceInActiveStudy()
        {
            var scene=SceneManager.GetActiveScene();
            if(scene.path!=NineAreaRockStudy.ScenePath) throw new InvalidOperationException("Keep the nine-area study active before placing its cube.");
            var area=scene.GetRootGameObjects().FirstOrDefault(r=>r.name=="Area 05");
            if(area==null) throw new InvalidOperationException("Area 05 is missing.");
            var existing=area.GetComponentInChildren<SmoothRockCube>(true);
            if(existing!=null) return existing.gameObject;
            CreateStarterPrefab();
            Undo.IncrementCurrentGroup();
            int undoGroup=Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Add rock cube to center study");
            var cube=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath),scene);
            Undo.RegisterCreatedObjectUndo(cube,"Add rock cube to center study");
            Undo.SetTransformParent(cube.transform,area.transform,"Place rock cube");
            var target=area.transform.TransformPoint(new Vector3(-3,0,0));
            var terrain=area.GetComponentInChildren<Terrain>();
            if(terrain!=null) target.y=terrain.SampleHeight(target)+terrain.transform.position.y;
            cube.transform.position=target+Vector3.up*.5f;
            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(undoGroup);
            Tick(true);
            return cube;
        }
    }
}
