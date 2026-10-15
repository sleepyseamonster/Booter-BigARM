using System;
using System.IO;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace BooterBigArm.Editor
{
    public static class CampResourceStudyAuthoring
    {
        public const string Folder = "Assets/_Project/Settings/Studies/CampLoopResources";
        public const string Prefabs = "Assets/_Project/Prefabs/TopDown3D/Studies/CampLoopResources";
        public const string RootPrefab = Prefabs + "/CampLoopResourceStudy.prefab";
        public const string RootName = "Camp Loop Resource Study";
        private const string CatalogPath = "Assets/_Project/Settings/Studies/CampLoopSalvage/LabItemCatalog.asset";
        private static readonly string[] Labels = { "Ironstone Outcrop", "Ancient Salt Crust", "Bristle Hollow", "Dead Winch", "Husk Beetle Site" };
        private static T[] All<T>(Scene s) where T : Component => s.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();
        private static GameObject Group(Transform p, string name)
        { var g = new GameObject(name); g.transform.SetParent(p, false); return g; }
        private static Material Mat(string name, Color color, float metal = 0)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")); m.SetColor("_BaseColor", color);
            m.SetFloat("_Metallic", metal); m.SetFloat("_Smoothness", .18f);
            AssetDatabase.CreateAsset(m, Folder + "/" + name + ".mat"); return m;
        }
        private static GameObject Shape(Transform parent, string name, PrimitiveType type, Vector3 p, Vector3 size, Material m, bool solid = false)
        {
            var g = GameObject.CreatePrimitive(type); g.name = name; g.transform.SetParent(parent, false);
            g.transform.localPosition = p; g.transform.localScale = size; g.GetComponent<Renderer>().sharedMaterial = m;
            if (!solid) Object.DestroyImmediate(g.GetComponent<Collider>()); return g;
        }
        private static bool Ground(Scene s, Vector3 p, out Vector3 point) => CampLoopSalvageAuthoring.Ground(s, p, out point);
        private static void FitSurface(Scene s, Transform site)
        {
            if (Ground(s,site.position+Vector3.right,out var right) && Ground(s,site.position-Vector3.right,out var left)
                && Ground(s,site.position+Vector3.forward,out var front) && Ground(s,site.position-Vector3.forward,out var back))
                site.rotation=Quaternion.FromToRotation(Vector3.up,Vector3.Cross(front-back,right-left).normalized);
        }
        private static void SaltPatch(Scene s, Transform parent, string name, Material material, float elevation)
        {
            var vertices=new System.Collections.Generic.List<Vector3>();var triangles=new System.Collections.Generic.List<int>();
            for(int z=0;z<4;z++)for(int x=0;x<5;x++)
            {
                if((z==0 || z==3) && (x==0 || x==4))continue;
                int start=vertices.Count;
                foreach(var corner in new[]{new Vector2(.02f,.02f),new Vector2(.38f,.02f),new Vector2(.02f,.38f),new Vector2(.38f,.38f)})
                {
                    float jitter=Mathf.Sin(x*13+z*37+corner.x*11+corner.y*29)*.035f;
                    var world=parent.TransformPoint(new Vector3(-1+x*.4f+corner.x+jitter,0,-.8f+z*.4f+corner.y-jitter));
                    if(!Ground(s,world,out var grounded))throw new InvalidOperationException("Salt footprint is unsupported.");
                    vertices.Add(parent.InverseTransformPoint(grounded+Vector3.up*elevation));
                }
                triangles.AddRange(new[]{start,start+2,start+1,start+1,start+2,start+3});
            }
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/"+name+".asset");bool fresh=mesh==null;
            if(fresh)mesh=new Mesh{name=name};else mesh.Clear();
            mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            if(fresh)AssetDatabase.CreateAsset(mesh,Folder+"/"+name+".asset");else EditorUtility.SetDirty(mesh);
            var g=parent.Find(name)!=null?parent.Find(name).gameObject:Group(parent,name);
            (g.GetComponent<MeshFilter>()??g.AddComponent<MeshFilter>()).sharedMesh=mesh;
            (g.GetComponent<MeshRenderer>()??g.AddComponent<MeshRenderer>()).sharedMaterial=material;
        }
        private static bool Open(Scene s, Vector3 p, float radius)
        {
            if (!Ground(s, p, out var ground)) return false;
            for (int i = 0; i < 12; i++)
            {
                var at = p + Quaternion.Euler(0, i * 30, 0) * Vector3.forward * radius;
                if (!Ground(s, at, out var edge) || Mathf.Abs(edge.y - ground.y) > radius * .35f) return false;
            }
            var bounds = new Bounds(ground + Vector3.up * 1.2f, new Vector3(radius * 2, 2.4f, radius * 2));
            if (All<Collider>(s).Any(c => !(c is TerrainCollider) && c.enabled && c.gameObject.activeInHierarchy && !c.isTrigger && c.bounds.Intersects(bounds))) return false;
            foreach (var actor in All<NpcActor>(s)) if (NpcMath.Distance(actor.transform.position, ground) < radius + 14) return false;
            return true;
        }
        private static Vector3[] Survey(Scene s)
        {
            var camp = TopDown3DWaypointCampSettings.Load();
            var offsets = new[] { new Vector3(34,0,-4), new Vector3(36,0,5), new Vector3(46,0,14), new Vector3(49,0,23), new Vector3(57,0,-14) };
            for (int n = 0; n < 64; n++)
            {
                var yaw = Quaternion.Euler(0, n * 360f / 64, 0); var points = new Vector3[5]; bool legal = true;
                for (int i = 0; i < 5 && legal; i++)
                {
                    var proposed = camp.position + yaw * offsets[i];
                    legal = Open(s, proposed, i == 4 ? 8 : 3.8f) && Ground(s, proposed, out points[i]);
                    if (legal && camp.returnRoute.Any(p => NpcMath.Distance(p, points[i]) < (i == 4 ? 12 : 6))) legal = false;
                }
                if (!legal) continue;
                // The route begins outside the protected camp reserve. Native support and a 3m corridor are surveyed.
                var prior = camp.position + yaw * new Vector3(22, 0, -4);
                foreach (var point in points)
                {
                    var delta = Vector3.ProjectOnPlane(point - prior, Vector3.up); int steps = Mathf.CeilToInt(delta.magnitude);
                    for (int j = 0; j <= steps && legal; j++)
                    {
                        var p = Vector3.Lerp(prior, point, (float)j / Mathf.Max(1, steps));
                        // Proposed sources do not yet have colliders in the scene.
                        if (!Open(s, p, 1.5f)) legal = false;
                    }
                    prior = point;
                }
                if (legal) return points;
            }
            throw new InvalidOperationException("No supported five-site circuit with clear approaches and protected reservations. Scene preserved.");
        }
        private static Sprite Icon(int key)
        {
            var t = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
            {
                bool draw = key == 1 ? Mathf.Abs(x-32)+Mathf.Abs(y-32)<21
                    : key == 2 ? y>12 && y<52 && ((x+y/4)%10<3) && x>12 && x<52
                    : key == 3 ? Mathf.Abs(Mathf.Sqrt((x-32)*(x-32)+(y-32)*(y-32))-18)<4
                    : x>12 && x<52 && y>12 && y<52 && (x+y)%9<4;
                t.SetPixel(x,y,draw ? new Color(.83f,.7f,.46f) : Color.clear);
            }
            t.Apply(); string path = Folder + "/Resource" + key + ".png"; File.WriteAllBytes(path,t.EncodeToPNG()); Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path); var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite; importer.spritePixelsPerUnit=64; importer.alphaIsTransparency=true; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        public static void BuildFromCli()
        {
            if (!Application.isBatchMode || !File.Exists(".camp-resources-validation")) throw new InvalidOperationException("Author only in the isolated background candidate.");
            var s=EditorSceneManager.OpenScene(TopDown3DAnimationPlaygroundSession.ScenePath);
            if (All<CampResourceStudy>(s).Length!=0) { Validate(s); return; }
            if (Directory.Exists(Folder) || Directory.Exists(Prefabs)) throw new InvalidOperationException("Uninstalled resource assets exist; preserve and reconcile them.");
            Physics.SyncTransforms(); var points=Survey(s);
            Directory.CreateDirectory(Folder); Directory.CreateDirectory(Prefabs); AssetDatabase.Refresh();
            var catalog=AssetDatabase.LoadAssetAtPath<TopDown3DItemCatalog>(CatalogPath);
            var definitions=catalog.Definitions.ToList();
            var names=new[]{"Ironstone Ore","Dry Mineral Salts","Coarse Sensory Bristles","Salvaged Metal Cable","Scraped Chitin Fibers"};
            var masses=new[]{1f,.15f,.08f,.8f,.1f};
            for(int i=1;i<5;i++)
            {
                var item=ScriptableObject.CreateInstance<TopDown3DItemDefinition>();
                item.Configure(CampResourceLedger.Items[i],names[i],"Raw material from the Camp Loop resource study. Prototype tuning.","Resource",Icon(i),20,masses[i]);
                AssetDatabase.CreateAsset(item,Folder+"/Resource"+i+".asset");definitions.Add(item);
            }
            catalog.Configure(definitions);EditorUtility.SetDirty(catalog);
            var rock=Mat("RustStone",new Color(.29f,.13f,.08f));var ore=Mat("OreFace",new Color(.12f,.09f,.07f),.35f);
            var salt=Mat("PaleSalts",new Color(.83f,.75f,.56f));var baseMat=Mat("SpentGround",new Color(.33f,.25f,.16f));
            var bristle=Mat("Bristles",new Color(.45f,.30f,.17f));var metal=Mat("DeadMetal",new Color(.19f,.17f,.14f),.65f);
            var cable=Mat("Cable",new Color(.52f,.43f,.31f),.6f);var shell=Mat("BeetleShell",new Color(.40f,.18f,.08f),.15f);
            var root=new GameObject(RootName);SceneManager.MoveGameObjectToScene(root,s);
            var services=Group(root.transform,"Study Services");var study=services.AddComponent<CampResourceStudy>();
            var anchors=Group(root.transform,"Layout Anchors");var sites=Group(root.transform,"Resource Sites");
            var sources=new CampResourceSource[5];CampHuskBeetle beetle=null;
            for(int i=0;i<5;i++)
            {
                var site=Group(sites.transform,"RS0"+(i+1)+" "+Labels[i]);site.transform.position=points[i];
                if(i!=4)FitSurface(s,site.transform);
                var visuals=Group(site.transform,"Visuals");var available=Group(visuals.transform,"Available Material");
                var spent=Group(visuals.transform,"Worked Material");spent.SetActive(false);
                var point=Group(site.transform,"Interaction Anchor");point.transform.localPosition=new Vector3(0,.5f,-.65f);
                var collision=Group(site.transform,"Interaction Volume");var volume=collision.AddComponent<BoxCollider>();
                volume.size=new Vector3(1.6f,.8f,1.5f);volume.center=Vector3.up*.4f;volume.isTrigger=true;
                if(i==0)
                {
                    Shape(visuals.transform,"Rock Body",PrimitiveType.Sphere,new Vector3(0,.48f,0),new Vector3(2.1f,1.25f,1.65f),rock,true);
                    Shape(available.transform,"Dark fractured ore face",PrimitiveType.Cube,new Vector3(0,.65f,-.72f),new Vector3(1.15f,.55f,.22f),ore);
                    Shape(spent.transform,"Worked face",PrimitiveType.Cube,new Vector3(0,.65f,-.73f),new Vector3(1.15f,.55f,.12f),baseMat);
                    point.transform.localPosition=new Vector3(0,.65f,-.95f);
                }
                else if(i==1)
                {
                    SaltPatch(s,visuals.transform,"DryDepositBase",baseMat,.012f);
                    SaltPatch(s,available.transform,"CrackedSaltCrust",salt,.035f);
                    point.transform.localPosition=new Vector3(0,.12f,-.55f);
                }
                else if(i==2)
                {
                    for(int k=0;k<3;k++)Shape(visuals.transform,"Shelter stone",PrimitiveType.Sphere,new Vector3(k-1,.22f,.75f),new Vector3(1,.65f,.7f),rock,true);
                    for(int k=0;k<13;k++)
                    {var b=Shape(available.transform,"Rigid shed bristle",PrimitiveType.Cylinder,new Vector3((k%5-2)*.15f,.18f,k/5*.16f),new Vector3(.045f,.23f,.045f),bristle);b.transform.localRotation=Quaternion.Euler(25,0,(k%3-1)*20);}
                    point.transform.localPosition=new Vector3(0,.25f,-.45f);
                }
                else if(i==3)
                {
                    Shape(visuals.transform,"Broken frame",PrimitiveType.Cube,new Vector3(0,.12f,0),new Vector3(1.7f,.24f,1.2f),metal,true);
                    var drum=Shape(visuals.transform,"Stripped drum",PrimitiveType.Cylinder,new Vector3(0,.62f,0),new Vector3(.85f,.6f,.85f),metal);drum.transform.localRotation=Quaternion.Euler(0,0,90);
                    for(int k=0;k<8;k++){var coil=Shape(available.transform,"Recoverable cable turn",PrimitiveType.Cylinder,new Vector3(-.5f+k*.14f,.62f,0),new Vector3(1.05f,.025f,1.05f),cable);coil.transform.localRotation=Quaternion.Euler(0,0,90);}
                    Shape(visuals.transform,"Retained cable end",PrimitiveType.Cube,new Vector3(.6f,.4f,-.45f),new Vector3(.05f,.05f,.55f),cable);
                    point.transform.localPosition=new Vector3(0,.6f,-.8f);
                }
                else
                {
                    var actor=Group(site.transform,"Husk Beetle 01");actor.transform.position=points[i]+Vector3.up*NpcGroundMotor.Skin;
                    visuals.transform.SetParent(actor.transform,false);collision.transform.SetParent(actor.transform,false);point.transform.SetParent(actor.transform,false);
                    var rb=actor.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;
                    var capsule=actor.AddComponent<CapsuleCollider>();capsule.radius=.4f;capsule.height=.8f;capsule.center=Vector3.up*.4f;
                    actor.AddComponent<NpcGroundMotor>();beetle=actor.AddComponent<CampHuskBeetle>();
                    Shape(visuals.transform,"Low segmented husk",PrimitiveType.Sphere,new Vector3(0,.46f,0),new Vector3(.9f,.5f,1.3f),shell);
                    Shape(visuals.transform,"Head",PrimitiveType.Sphere,new Vector3(0,.35f,.65f),new Vector3(.55f,.3f,.4f),ore);
                    var limbs=new Transform[6];
                    for(int k=0;k<6;k++)limbs[k]=Shape(visuals.transform,"Leg "+k,PrimitiveType.Capsule,new Vector3(k<3?-.5f:.5f,.22f,(k%3-1)*.36f),new Vector3(.1f,.25f,.1f),ore).transform;
                    Shape(available.transform,"Exposed fiber layer",PrimitiveType.Cube,new Vector3(0,.64f,-.1f),new Vector3(.55f,.08f,.65f),salt);
                    beetle.Configure(study,visuals.transform,limbs);point.transform.localPosition=new Vector3(0,.55f,0);
                }
                sources[i]=site.AddComponent<CampResourceSource>();sources[i].Configure(study,i,volume,point.transform,available,spent);
                if(i!=4){volume.transform.position=point.transform.position;volume.center=Vector3.zero;volume.size=new Vector3(1.1f,.7f,.35f);}
                var marker=Group(anchors.transform,Labels[i]+" Anchor");marker.transform.position=points[i];
                // Save independent visual/source prefabs; cross-scene study references are installed on the root below.
                sources[i].Configure(null,i,volume,point.transform,available,spent);
                var prefab=PrefabUtility.SaveAsPrefabAsset(site,Prefabs+"/Resource"+i+".prefab");
                sources[i].Configure(study,i,volume,point.transform,available,spent);
            }
            var bindings=All<TopDown3DAnimationPlaygroundSession>(s).Single().Bindings;
            var motion=bindings.Find("punch.punch.jab");
            if(motion==null || !motion.CanPreview)throw new InvalidOperationException("The existing imported unrestricted jab clip is required.");
            study.Configure(sources,beetle,motion.clip);
            PrefabUtility.SaveAsPrefabAsset(root,RootPrefab);
            AssetDatabase.SaveAssets();Validate(s);EditorSceneManager.SaveScene(s);
            File.WriteAllText("Logs/resource-survey.json",JsonUtility.ToJson(new SurveyReceipt{positions=points},true));
            Debug.Log("CAMP_RESOURCES_AUTHORED: five sources, one study, surveyed supported circuit; no native terrain edits.");
        }
        [Serializable] private sealed class SurveyReceipt { public Vector3[] positions; }
        private static void EditOwnedRoot(Scene scene)
        {
            var root=scene.GetRootGameObjects().Single(g=>g.GetComponentInChildren<CampResourceStudy>(true)!=null);
            if(PrefabUtility.IsPartOfPrefabInstance(root))PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        }
        public static void RefineBlockoutFromCli()
        {
            if(!Application.isBatchMode || !File.Exists(".camp-resources-validation"))throw new InvalidOperationException("Isolated authoring only.");
            var s=EditorSceneManager.OpenScene(TopDown3DAnimationPlaygroundSession.ScenePath);
            EditOwnedRoot(s);
            var beetle=All<CampHuskBeetle>(s).Single();var visual=beetle.transform.Find("Visuals");
            visual.Find("Available Material").gameObject.SetActive(false);
            var shell=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/BeetleShell.mat");
            if(visual.Find("Husk plate 0")==null)for(int i=0;i<3;i++)
                Shape(visual,"Husk plate "+i,PrimitiveType.Sphere,new Vector3(0,.63f,(i-1)*.34f),new Vector3(.9f,.16f,.45f),shell);
            for(int i=0;i<6;i++)visual.Find("Leg "+i).localRotation=Quaternion.Euler(0,0,i<3?-45:45);
            var salt=All<CampResourceSource>(s).Single(p=>p.Index==1).transform.Find("Visuals");
            SaltPatch(s,salt,"DryDepositBase",AssetDatabase.LoadAssetAtPath<Material>(Folder+"/SpentGround.mat"),.012f);
            SaltPatch(s,salt.Find("Available Material"),"CrackedSaltCrust",AssetDatabase.LoadAssetAtPath<Material>(Folder+"/PaleSalts.mat"),.035f);
            foreach(var source in All<CampResourceSource>(s))PrefabUtility.SaveAsPrefabAsset(source.gameObject,Prefabs+"/Resource"+source.Index+".prefab");
            var root=s.GetRootGameObjects().Single(g=>g.GetComponentInChildren<CampResourceStudy>(true)!=null);PrefabUtility.SaveAsPrefabAsset(root,RootPrefab);
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(s);SealFromCli();
        }
        public static void SealFromCli()
        {
            if(!Application.isBatchMode || !File.Exists(".camp-resources-validation"))throw new InvalidOperationException("Isolated authoring only.");
            var s=EditorSceneManager.OpenScene(TopDown3DAnimationPlaygroundSession.ScenePath);
            EditOwnedRoot(s);
            foreach(var source in All<CampResourceSource>(s))
            {
                if(source.Index!=4)
                {
                    var box=source.GetComponentsInChildren<BoxCollider>().Single(c=>c.isTrigger);
                    box.transform.position=source.InteractionPoint;box.center=Vector3.zero;box.size=new Vector3(1.1f,.7f,.35f);
                }
                PrefabUtility.SaveAsPrefabAsset(source.gameObject,Prefabs+"/Resource"+source.Index+".prefab");
            }
            var root=s.GetRootGameObjects().Single(g=>g.GetComponentInChildren<CampResourceStudy>(true)!=null);PrefabUtility.SaveAsPrefabAsset(root,RootPrefab);
            Object.DestroyImmediate(root);var installed=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RootPrefab),s);installed.name=RootName;
            Physics.SyncTransforms();AssetDatabase.SaveAssets();Validate(s);EditorSceneManager.SaveScene(s);
        }
        public static void FitGroundFromCli()
        {
            if(!Application.isBatchMode || !File.Exists(".camp-resources-validation"))throw new InvalidOperationException("Isolated authoring only.");
            var s=EditorSceneManager.OpenScene(TopDown3DAnimationPlaygroundSession.ScenePath);
            EditOwnedRoot(s);
            foreach(var source in All<CampResourceSource>(s))if(source.Index!=4)FitSurface(s,source.transform);
            var salt=All<CampResourceSource>(s).Single(p=>p.Index==1);var visuals=salt.transform.Find("Visuals");var available=visuals.Find("Available Material");
            foreach(Transform child in visuals.Cast<Transform>().ToArray())if(child.name=="Dry deposit base")Object.DestroyImmediate(child.gameObject);
            foreach(Transform child in available.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
            SaltPatch(s,visuals,"DryDepositBase",AssetDatabase.LoadAssetAtPath<Material>(Folder+"/SpentGround.mat"),.012f);
            SaltPatch(s,available,"CrackedSaltCrust",AssetDatabase.LoadAssetAtPath<Material>(Folder+"/PaleSalts.mat"),.035f);
            // Persist the authored fixture and its individual reusable site templates together.
            foreach(var source in All<CampResourceSource>(s))PrefabUtility.SaveAsPrefabAsset(source.gameObject,Prefabs+"/Resource"+source.Index+".prefab");
            var root=s.GetRootGameObjects().Single(g=>g.GetComponentInChildren<CampResourceStudy>(true)!=null);PrefabUtility.SaveAsPrefabAsset(root,RootPrefab);
            AssetDatabase.SaveAssets();Validate(s);EditorSceneManager.SaveScene(s);
        }
        public static void ValidateFromCli() => Validate(EditorSceneManager.OpenScene(TopDown3DAnimationPlaygroundSession.ScenePath));
        public static void Validate(Scene s)
        {
            CampLoopTestingSceneBuilder.Validate(s);
            var study=All<CampResourceStudy>(s);var sources=All<CampResourceSource>(s);
            if(study.Length!=1 || sources.Length!=5 || sources.Select(p=>p.StableId).Distinct().Count()!=5 || All<CampHuskBeetle>(s).Length!=1
                || All<TopDown3DSalvageWreckState>(s).Length!=1 || All<TopDown3DIronstoneNode>(s).Length!=12)
                throw new InvalidOperationException("Resource study identity or protected content mismatch.");
            foreach(var p in sources)if(!Ground(s,p.transform.position,out _))throw new InvalidOperationException("Unsupported resource site.");
            if(!All<CampHuskBeetle>(s).Single().CanRestore())throw new InvalidOperationException("Beetle home lacks support/clearance.");
            Debug.Log("CAMP_RESOURCES_VALIDATED: 5 source IDs, 12 original nodes, 1 original wreck, legal beetle anchor.");
        }
        [MenuItem("Booter & BigARM/Camp Loop/Install Five Resource Sources")]
        private static void InstallFromMenu() => Debug.Log(InstallInOpenAuthorScene());
        public static string InstallInOpenAuthorScene()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)throw new InvalidOperationException("Require compiled Edit Mode.");
            var s=SceneManager.GetActiveScene();if(s.path!=TopDown3DAnimationPlaygroundSession.ScenePath)throw new InvalidOperationException("Camp Loop must already be open.");
            if(All<CampResourceStudy>(s).Length>0){Validate(s);return "Resource study already installed; no duplicate created.";}
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(RootPrefab);if(prefab==null)throw new InvalidOperationException("Missing verified study prefab.");
            var root=(GameObject)PrefabUtility.InstantiatePrefab(prefab,s);root.name=RootName;Undo.RegisterCreatedObjectUndo(root,"Install Camp Loop resource study");
            try { Physics.SyncTransforms();Validate(s);return "Five resource sources installed additively; scene remains unsaved."; }
            catch { Undo.DestroyObjectImmediate(root);throw; }
        }
        public static void CaptureFromCli()
        {
            if(!Application.isBatchMode || !File.Exists(".camp-resources-validation"))throw new InvalidOperationException("Capture in the background candidate only.");
            var s=EditorSceneManager.OpenScene(TopDown3DAnimationPlaygroundSession.ScenePath);Validate(s);
            Directory.CreateDirectory("Logs/ResourceCaptures");
            foreach(var source in All<CampResourceSource>(s))
            {
                var go=new GameObject("Temporary resource proof camera");var camera=go.AddComponent<Camera>();
                var target=RenderTexture.GetTemporary(960,720,24);var previous=RenderTexture.active;
                try
                {
                    var center=source.transform.position+Vector3.up*.4f;
                    camera.transform.position=center+new Vector3(3.5f,4.5f,-5.5f);camera.transform.LookAt(center);
                    camera.nearClipPlane=.1f;camera.farClipPlane=100;camera.fieldOfView=42;
                    camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.11f,.07f,.05f);camera.targetTexture=target;
                    camera.Render();RenderTexture.active=target;
                    var image=new Texture2D(960,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,960,720),0,0);image.Apply();
                    var pixels=image.GetPixels32();if(!pixels.Any(p=>Math.Abs(p.r-pixels[0].r)+Math.Abs(p.g-pixels[0].g)+Math.Abs(p.b-pixels[0].b)>60))throw new InvalidOperationException("Blank resource capture.");
                    File.WriteAllBytes("Logs/ResourceCaptures/Resource"+source.Index+".png",image.EncodeToPNG());Object.DestroyImmediate(image);
                }
                finally{camera.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);Object.DestroyImmediate(go);}
            }
        }
    }
}
