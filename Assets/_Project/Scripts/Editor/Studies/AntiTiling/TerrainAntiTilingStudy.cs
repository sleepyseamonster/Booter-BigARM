using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Editor
{
    /// <summary>Background-only native four-tile study. No production scene opens,
    /// Play Mode, source TerrainData edits, simulation, or global asset changes.</summary>
    public static class TerrainAntiTilingStudy
    {
        public const string ScenePath = "Assets/_Project/Scenes/Reference/TerrainAntiTilingStudy.unity";
        public const string Folder = "Assets/_Project/Art/Environment/Ground/Studies/AntiTiling20261017";
        public const string ShaderPath = "Assets/_Project/Shaders/TopDown3D/TerrainAntiTilingStudy.shader";
        const string Ground = "Assets/_Project/Art/Environment/Ground/SandDirt/";
        const int Width = 1024, Height = 768;

        [Serializable] public sealed class Tile { public string name, path; public Vector3 position; }
        [Serializable] public sealed class FileSeal { public string path, sha256; }
        [Serializable] public sealed class Input { public string source, candidate; public Tile[] tiles; public FileSeal[] files; }
        [Serializable] public sealed class Check { public string name; public double value, limit; public bool passed; }
        [Serializable] public sealed class Report
        {
            public int schemaVersion = 2;
            public string unity, gpu, api, scene;
            public string assemblySha256;
            public FileSeal[] testedFiles;
            public string[] inputs, captures;
            public Check[] checks;
            public int terrainCount;
            public string limitation = "Static EditMode D3D11 study, single surface per draw; no gameplay, Player GPU timing, production terrain-layer integration, triplanar or visual acceptance proof.";
        }
        static readonly List<Check> Checks = new List<Check>();

        static void Guard()
        {
            if (!Application.isBatchMode || !File.Exists(".anti-tiling-study") ||
                EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Requires a marked isolated batchmode EditMode candidate.");
        }

        public static void BuildFromCli()
        {
            Guard();
            if (File.Exists(ScenePath) || Directory.Exists(Folder))
                throw new InvalidOperationException("Preserve existing study; use a fresh candidate/output version.");
            var input = JsonUtility.FromJson<Input>(File.ReadAllText("anti-tiling-inputs.json"));
            if (input.tiles == null || input.tiles.Length != 4) throw new InvalidOperationException("Four source tiles required.");
            Directory.CreateDirectory(Folder);
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            AssetDatabase.Refresh();
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (!shader || !shader.isSupported) throw new InvalidOperationException("Study shader unavailable.");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var material = CreateMaterial(shader, "RockAntiTiled", "BrokenWorldMixedRockyAlbedo.png", true, true);
            CreateMaterial(shader, "RockTiled", "BrokenWorldMixedRockyAlbedo.png", false, true);
            CreateMaterial(shader, "SandAntiTiled", "BrokenWorldSandDirtAlbedo.png", true, false, .2f);
            CreateMaterial(shader, "SandTiled", "BrokenWorldSandDirtAlbedo.png", false, false, .2f);
            CreateMaterial(shader, "GravelAntiTiled", "BrokenWorldGravelAlbedo.png", true, false);
            CreateMaterial(shader, "GravelTiled", "BrokenWorldGravelAlbedo.png", false, false);
            var terrains = new List<Terrain>();
            foreach (Tile tile in input.tiles)
            {
                var data = AssetDatabase.LoadAssetAtPath<TerrainData>(tile.path);
                if (!data) throw new InvalidOperationException("Missing native terrain: " + tile.path);
                var go = Terrain.CreateTerrainGameObject(data);
                go.name = tile.name;
                go.transform.position = tile.position;
                var terrain = go.GetComponent<Terrain>();
                terrain.materialTemplate = material;
                terrain.drawInstanced = true;
                terrain.drawTreesAndFoliage = false;
                terrain.heightmapPixelError = 1;
                terrain.basemapDistance = 100000; // no unsupported terrain basemap substitution
                terrain.shadowCastingMode = ShadowCastingMode.Off;
                terrain.allowAutoConnect = false;
                terrain.groupingID = 20261017;
                terrains.Add(terrain);
            }
            Connect(terrains.ToArray());
            var camera = new GameObject("StudyCamera").AddComponent<Camera>();
            camera.gameObject.tag = "MainCamera";
            camera.backgroundColor = new Color(.12f,.14f,.16f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.nearClipPlane = .1f; camera.farClipPlane = 3000;
            camera.allowHDR = false; camera.allowMSAA = false;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            var sun = new GameObject("StudySun").AddComponent<Light>();
            sun.type = LightType.Directional; sun.intensity = 1.8f;
            sun.transform.rotation = Quaternion.Euler(48,-35,0);
            sun.shadows = LightShadows.None;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.4f,.4f,.4f);
            RenderSettings.fog = false;
            PlaceCamera(camera, terrains.ToArray(), "near");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("ANTI_TILING_STUDY_BUILT: " + ScenePath);
        }

        static Material CreateMaterial(Shader shader, string name, string albedo, bool enabled, bool rock, float rotation = Mathf.PI)
        {
            var m = new Material(shader) { name = name, enableInstancing = true };
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Ground + albedo));
            if (rock)
            {
                m.SetTexture("_NormalMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Ground + "BrokenWorldMixedRockyNormal.png"));
                m.SetTexture("_HeightMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Ground + "BrokenWorldMixedRockyHeight.png"));
            }
            m.SetFloat("_Rotation", rotation);
            m.SetFloat("_MetersPerTile", 3);
            m.SetFloat("_CellMeters", 5);
            m.SetFloat("_Seed", 17);
            m.SetFloat("_Sharpness", 3);
            m.SetFloat("_ScaleVariation", .15f);
            SetEnabled(m, enabled);
            AssetDatabase.CreateAsset(m, Folder + "/" + name + ".mat");
            return m;
        }

        static void SetEnabled(Material material, bool enabled)
        {
            material.SetFloat("_AntiTiling", enabled ? 1 : 0);
            if (enabled) material.EnableKeyword("_ANTI_TILING"); else material.DisableKeyword("_ANTI_TILING");
        }

        static void Connect(Terrain[] terrains)
        {
            foreach (var t in terrains)
            {
                Vector3 p=t.transform.position, s=t.terrainData.size;
                Func<float,float,Terrain> at=(x,z)=>terrains.FirstOrDefault(n =>
                    Mathf.Abs(n.transform.position.x-x)<.01f && Mathf.Abs(n.transform.position.z-z)<.01f);
                t.SetNeighbors(at(p.x-s.x,p.z), at(p.x,p.z+s.z), at(p.x+s.x,p.z), at(p.x,p.z-s.z));
            }
        }

        static void PlaceCamera(Camera c, Terrain[] terrains, string view)
        {
            float x = terrains.Min(t=>t.transform.position.x)+256;
            float z = terrains.Min(t=>t.transform.position.z)+256;
            var t0 = terrains.First(t=>t.transform.position.x <= x && t.transform.position.z <= z);
            float y = t0.SampleHeight(new Vector3(x,0,z))+t0.transform.position.y;
            Vector3 center = new Vector3(x,y,z);
            c.orthographic = view != "oblique";
            c.orthographicSize = view == "overview" ? 265 : 22;
            c.fieldOfView = 55;
            c.transform.position = center + (view == "oblique" ? new Vector3(22,19,-25) : new Vector3(0,750,0));
            c.transform.LookAt(center, view == "oblique" ? Vector3.up : Vector3.forward);
        }

        public static void ValidateAndCaptureFromCli() => Capture("Logs/AntiTilingStudy");

        public static void AuditFromCli() => Capture("Logs/AntiTilingAudit20261017");

        static string HashFile(string path)
        {
            using(var sha=SHA256.Create())
            using(var stream=File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();
        }

        static FileSeal[] SealInputs()
        {
            var input=JsonUtility.FromJson<Input>(File.ReadAllText("anti-tiling-inputs.json"));
            var paths=input.files.Where(f=>f.path.StartsWith("Assets/",StringComparison.Ordinal)).Select(f=>f.path).ToList();
            paths.Add("Assets/_Project/Scripts/Editor/Studies/AntiTiling/AntiTilingContractProbe.shader");
            paths.Add("Assets/_Project/Scripts/Editor/Studies/AntiTiling/AntiTilingContractProbe.shader.meta");
            paths.Add(ScenePath); paths.Add(ScenePath+".meta");
            foreach(string path in Directory.GetFiles(Folder,"*.mat"))
            { paths.Add(path.Replace('\\','/')); paths.Add(path.Replace('\\','/')+".meta"); }
            return paths.Distinct().OrderBy(p=>p,StringComparer.Ordinal)
                .Select(p=>new FileSeal{path=p,sha256=HashFile(p)}).ToArray();
        }

        static void Capture(string output)
        {
            Guard(); Checks.Clear();
            FileSeal[] tested=SealInputs();
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                throw new InvalidOperationException("Rendering requires graphics; omit -nographics.");
            if (Directory.Exists(output)) throw new InvalidOperationException("Capture destination already exists.");
            Directory.CreateDirectory(output);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var terrains = UnityEngine.Object.FindObjectsByType<Terrain>();
            Require("four native terrains", terrains.Length, 4, terrains.Length==4);
            ValidateBorders(terrains);
            var camera = Camera.main;
            var pipeline = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            var serialized = new SerializedObject(pipeline);
            var list = serialized.FindProperty("m_RendererDataList");
            list.arraySize=1; list.GetArrayElementAtIndex(0).objectReferenceValue=renderer;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var oldGraphics = GraphicsSettings.defaultRenderPipeline;
            var oldQuality = QualitySettings.renderPipeline;
            var originalMaterials = terrains.Select(t=>t.materialTemplate).ToArray();
            byte[] sceneBefore = File.ReadAllBytes(ScenePath);
            var captures = new List<string>();
            try
            {
                GraphicsSettings.defaultRenderPipeline = pipeline;
                QualitySettings.renderPipeline = pipeline;
                foreach (string surface in new[]{"Rock","Sand","Gravel"})
                foreach (string view in new[]{"overview","near","oblique"})
                foreach (bool anti in new[]{false,true})
                {
                    var material = AssetDatabase.LoadAssetAtPath<Material>(Folder+"/"+surface+(anti?"AntiTiled":"Tiled")+".mat");
                    foreach (var t in terrains) t.materialTemplate=material;
                    PlaceCamera(camera,terrains,view);
                    string path=output+"/"+surface+"-"+view+(anti?"-anti":"-tiled")+".png";
                    var pixels=Render(camera,Width,Height);
                    File.WriteAllBytes(path,pixels.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(pixels);
                    captures.Add(path);
                }
                var rock = new Material(originalMaterials[0]);
                try
                {
                    ValidateNativeGpu(camera,terrains,rock);
                    ValidateGpu(camera,terrains,rock,output);
                    ValidateAnalyticNormals(camera,terrains);
                }
                finally { UnityEngine.Object.DestroyImmediate(rock); }
                foreach(string name in new[]{"BooterBigArm/Studies/Terrain Anti Tiling","Hidden/BooterBigArm/AntiTilingContractProbe"})
                    foreach(var message in ShaderUtil.GetShaderMessages(Shader.Find(name)))
                        if(message.severity.ToString()=="Error") throw new InvalidOperationException(message.message);
                Require("shader compiler errors",0,0,true);
            }
            finally
            {
                for(int i=0;i<terrains.Length;i++) terrains[i].materialTemplate=originalMaterials[i];
                GraphicsSettings.defaultRenderPipeline=oldGraphics;
                QualitySettings.renderPipeline=oldQuality;
                UnityEngine.Object.DestroyImmediate(pipeline);
                UnityEngine.Object.DestroyImmediate(renderer);
            }
            Require("saved scene bytes unchanged",0,0,sceneBefore.SequenceEqual(File.ReadAllBytes(ScenePath)));
            Require("all tested file bytes unchanged during capture",0,0,tested.All(f=>HashFile(f.path)==f.sha256));
            var report = new Report { unity=Application.unityVersion, gpu=SystemInfo.graphicsDeviceName,
                api=SystemInfo.graphicsDeviceType.ToString(), scene=ScenePath, terrainCount=terrains.Length,
                inputs=terrains.Select(t=>AssetDatabase.GetAssetPath(t.terrainData)).ToArray(),
                captures=captures.ToArray(), checks=Checks.ToArray(), testedFiles=tested,
                assemblySha256=HashFile(typeof(TerrainAntiTilingStudy).Assembly.Location) };
            File.WriteAllText(output+"/report.json",JsonUtility.ToJson(report,true));
            Debug.Log("ANTI_TILING_STUDY_OK: "+Checks.Count+" checks; "+captures.Count+" native terrain captures.");
        }

        static void ValidateBorders(Terrain[] terrains)
        {
            double max=0; int joins=0, samples=0;
            foreach(var a in terrains) foreach(var b in terrains)
            {
                Vector3 ap=a.transform.position,bp=b.transform.position;
                bool east=Mathf.Abs(bp.x-ap.x-a.terrainData.size.x)<.01 && Mathf.Abs(bp.z-ap.z)<.01;
                bool north=Mathf.Abs(bp.z-ap.z-a.terrainData.size.z)<.01 && Mathf.Abs(bp.x-ap.x)<.01;
                if(!east && !north) continue;
                joins++;
                int steps=Math.Max(a.terrainData.heightmapResolution,b.terrainData.heightmapResolution);
                for(int i=0;i<steps;i++)
                {
                    float f=(float)i/(steps-1);
                    float ah=a.terrainData.GetInterpolatedHeight(east?1:f,east?f:1)+ap.y;
                    float bh=b.terrainData.GetInterpolatedHeight(east?0:f,east?f:0)+bp.y;
                    max=Math.Max(max,Math.Abs(ah-bh)); samples++;
                }
            }
            Require("native shared edges",joins,4,joins==4);
            Require("native height border error metres ("+samples+" samples)",max,.0003,max<=.0003);
        }

        static Texture2D Render(Camera camera,int width,int height)
        {
            var target=RenderTexture.GetTemporary(width,height,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            var oldTarget=camera.targetTexture; var oldActive=RenderTexture.active;
            try
            {
                camera.targetTexture=target;
                camera.Render();
                RenderTexture.active=target;
                var result=new Texture2D(width,height,TextureFormat.RGBA32,false,false);
                result.ReadPixels(new Rect(0,0,width,height),0,0); result.Apply();
                return result;
            }
            finally { camera.targetTexture=oldTarget; RenderTexture.active=oldActive; RenderTexture.ReleaseTemporary(target); }
        }

        static GameObject Quad(string name,float x,float z,float width,float depth,Material material)
        {
            var go=new GameObject(name);
            go.transform.position=new Vector3(x,100,z);
            var mesh=new Mesh { name=name+"Mesh" };
            mesh.vertices=new[]{Vector3.zero,new Vector3(0,0,depth),new Vector3(width,0,depth),new Vector3(width,0,0)};
            mesh.normals=Enumerable.Repeat(Vector3.up,4).ToArray();
            mesh.uv=new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right};
            mesh.triangles=new[]{0,1,2,0,2,3}; mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh=mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial=material;
            return go;
        }

        static void ValidateNativeGpu(Camera camera,Terrain[] terrains,Material material)
        {
            var previous=terrains.Select(t=>t.materialTemplate).ToArray();
            var instanced=terrains.Select(t=>t.drawInstanced).ToArray();
            material.enableInstancing=true;
            PlaceCamera(camera,terrains,"near");
            try
            {
                foreach(var t in terrains) t.materialTemplate=material;
                for(int mode=1;mode<=3;mode++)
                {
                    material.SetFloat("_DebugView",mode);
                    foreach(var t in terrains) t.drawInstanced=true;
                    var a=Render(camera,256,256);
                    foreach(var t in terrains) t.drawInstanced=false;
                    var b=Render(camera,256,256);
                    try
                    {
                        int difference=MaxDifference(a,b);
                        Require("native instanced vs non-instanced GPU mode "+mode,difference,2,difference<=2);
                        Require("native rendered field is nonconstant mode "+mode,RepeatDifference(a,17),.1,RepeatDifference(a,17)>.1);
                    }
                    finally { UnityEngine.Object.DestroyImmediate(a); UnityEngine.Object.DestroyImmediate(b); }
                }
            }
            finally
            {
                for(int i=0;i<terrains.Length;i++)
                { terrains[i].drawInstanced=instanced[i]; terrains[i].materialTemplate=previous[i]; }
                material.SetFloat("_DebugView",0);
            }
        }

        static void ValidateAnalyticNormals(Camera camera,Terrain[] terrains)
        {
            var enabled=terrains.Select(t=>t.enabled).ToArray();
            foreach(var t in terrains) t.enabled=false;
            var shader=Shader.Find("Hidden/BooterBigArm/AntiTilingContractProbe");
            if(!shader) throw new InvalidOperationException("Contract probe shader missing.");
            var material=new Material(shader);
            var quad=Quad("AnalyticNormal",-16,-16,32,32,material);
            try
            {
                camera.orthographic=true; camera.orthographicSize=16; camera.aspect=1;
                camera.transform.position=new Vector3(0,200,0);
                camera.transform.rotation=Quaternion.Euler(90,0,0);
                var expected=new[]{new Vector3(.6f,0,.8f),new Vector3(0,-.6f,.8f),
                    new Vector3(0,.6f,.8f),new Vector3(-.6f,0,.8f)};
                var angles=new[]{0f,Mathf.PI/2,-Mathf.PI/2,Mathf.PI};
                for(int i=0;i<angles.Length;i++)
                {
                    material.SetFloat("_Angle",angles[i]);
                    AssertNormalPixels(camera,material,expected[i],"inverse normal rotation "+angles[i]);
                }
                material.SetFloat("_Mode",1);
                material.SetVector("_SourceNormal",new Vector4(0,0,1,0));
                foreach(Vector3 n in new[]{Vector3.right,Vector3.left,Vector3.up})
                {
                    material.SetVector("_GeometricNormal",n);
                    AssertNormalPixels(camera,material,n,"neutral normal finite on face "+n);
                }
                material.SetVector("_SourceNormal",new Vector4(.6f,0,.8f,0));
                material.SetVector("_GeometricNormal",Vector3.right);
                AssertNormalPixels(camera,material,new Vector3(.8f,0,.6f),"non-neutral normal finite on vertical face");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(quad.GetComponent<MeshFilter>().sharedMesh);
                UnityEngine.Object.DestroyImmediate(quad); UnityEngine.Object.DestroyImmediate(material);
                for(int i=0;i<terrains.Length;i++) terrains[i].enabled=enabled[i];
            }
        }

        static void AssertNormalPixels(Camera camera,Material material,Vector3 normal,string name)
        {
            var image=Render(camera,32,32);
            try
            {
                Vector3 encoded=normal*.5f+Vector3.one*.5f;
                if(QualitySettings.activeColorSpace==ColorSpace.Linear)
                    encoded=new Vector3(Mathf.LinearToGammaSpace(encoded.x),Mathf.LinearToGammaSpace(encoded.y),Mathf.LinearToGammaSpace(encoded.z));
                int r=Mathf.RoundToInt(encoded.x*255),g=Mathf.RoundToInt(encoded.y*255),b=Mathf.RoundToInt(encoded.z*255);
                int error=0;
                foreach(var pixel in image.GetPixels32())
                {
                    error=Math.Max(error,Math.Abs(pixel.r-r)); error=Math.Max(error,Math.Abs(pixel.g-g)); error=Math.Max(error,Math.Abs(pixel.b-b));
                }
                Require(name,error,2,error<=2);
            }
            finally { UnityEngine.Object.DestroyImmediate(image); }
        }

        static void ValidateGpu(Camera camera,Terrain[] terrains,Material material,string output)
        {
            foreach(var t in terrains) t.enabled=false;
            var objects=new List<GameObject>();
            const int size=256;
            try
            {
                camera.orthographic=true; camera.orthographicSize=16;
                camera.aspect=1;
                // Terrain instancing interprets vertices as heightmap patch indices.
                // These ordinary mesh probes must use the non-instanced vertex path.
                material.enableInstancing=false;
                SetEnabled(material,true);
                // Cross world zero and use the same negative coordinates as the camp.
                foreach(Vector2 center in new[]{Vector2.zero,new Vector2(-1792,-1280)})
                {
                    camera.transform.position=new Vector3(center.x,200,center.y);
                    camera.transform.rotation=Quaternion.Euler(90,0,0);
                    var whole=Quad("Continuous",center.x-16,center.y-16,32,32,material); objects.Add(whole);
                    var split=new List<GameObject>();
                    for(int z=0;z<2;z++) for(int x=0;x<2;x++)
                    {
                        var q=Quad("Split_"+x+"_"+z,center.x-16+16*x,center.y-16+16*z,16,16,material);
                        q.SetActive(false); split.Add(q); objects.Add(q);
                    }
                    for(int mode=1;mode<=3;mode++)
                    {
                        material.SetFloat("_DebugView",mode);
                        var a=Render(camera,size,size);
                        var repeat=Render(camera,size,size);
                        Require("deterministic GPU mode "+mode+" at "+center,MaxDifference(a,repeat),0,MaxDifference(a,repeat)==0);
                        whole.SetActive(false); foreach(var q in split) q.SetActive(true);
                        var b=Render(camera,size,size);
                        int error=MaxDifference(a,b);
                        Require("four transforms vs continuous GPU mode "+mode+" at "+center,error,2,error<=2);
                        if(mode==1 && center==Vector2.zero)
                        {
                            SetEnabled(material,false);
                            var tiled=Render(camera,size,size);
                            double tiledRepeat=RepeatDifference(tiled,24);
                            double antiRepeat=RepeatDifference(b,24);
                            Require("tiled texture repeats at three metres",tiledRepeat,1,tiledRepeat<=1);
                            Require("anti tiling breaks three metre repeat",antiRepeat,3,antiRepeat>3);
                            UnityEngine.Object.DestroyImmediate(tiled);
                            SetEnabled(material,true);
                            material.SetFloat("_Seed",29);
                            var changed=Render(camera,size,size);
                            Require("seed changes pattern",MaxDifference(b,changed),5,MaxDifference(b,changed)>5);
                            UnityEngine.Object.DestroyImmediate(changed); material.SetFloat("_Seed",17);
                            ValidateMapAlignment(camera,material);
                        }
                        if(mode==2 && center==Vector2.zero)
                        {
                            material.SetFloat("_NormalStrength",0);
                            var flat=Render(camera,size,size);
                            var flatPixels=flat.GetPixels32();
                            var first=flatPixels[0];
                            bool neutral=flatPixels.All(p=>Math.Abs(p.r-first.r)<=1 && Math.Abs(p.g-first.g)<=1 && p.b>=254)
                                && Math.Abs(first.r-first.g)<=1 && first.r>=126 && first.r<=190;
                            Require("zero strength gives neutral normal under all rotations",neutral?0:1,0,neutral);
                            material.SetFloat("_NormalStrength",1);
                            UnityEngine.Object.DestroyImmediate(flat);
                        }
                        File.WriteAllBytes(output+"/Probe-"+(center==Vector2.zero?"origin":"camp")+"-"+mode+".png",b.EncodeToPNG());
                        whole.SetActive(true); foreach(var q in split) q.SetActive(false);
                        UnityEngine.Object.DestroyImmediate(a); UnityEngine.Object.DestroyImmediate(b); UnityEngine.Object.DestroyImmediate(repeat);
                    }
                    foreach(var go in objects) { UnityEngine.Object.DestroyImmediate(go.GetComponent<MeshFilter>().sharedMesh); UnityEngine.Object.DestroyImmediate(go); }
                    objects.Clear();
                }
            }
            finally
            {
                foreach(var go in objects) if(go) { UnityEngine.Object.DestroyImmediate(go.GetComponent<MeshFilter>().sharedMesh); UnityEngine.Object.DestroyImmediate(go); }
                foreach(var t in terrains) t.enabled=true;
            }
        }

        static int MaxDifference(Texture2D a,Texture2D b)
        {
            var aa=a.GetPixels32(); var bb=b.GetPixels32(); int max=0;
            for(int i=0;i<aa.Length;i++)
            {
                max=Math.Max(max,Math.Abs(aa[i].r-bb[i].r));
                max=Math.Max(max,Math.Abs(aa[i].g-bb[i].g));
                max=Math.Max(max,Math.Abs(aa[i].b-bb[i].b));
            }
            return max;
        }

        static void ValidateMapAlignment(Camera camera,Material material)
        {
            Texture albedo=material.GetTexture("_BaseMap"),height=material.GetTexture("_HeightMap");
            float mode=material.GetFloat("_DebugView");
            var pattern=new Texture2D(16,16,TextureFormat.RGBA32,true,true);
            pattern.wrapMode=TextureWrapMode.Repeat;
            var pixels=new Color[256];
            for(int y=0;y<16;y++) for(int x=0;x<16;x++)
            { float value=((x*7+y*11)%17)/16f; pixels[y*16+x]=new Color(value,value,value,1); }
            pattern.SetPixels(pixels); pattern.Apply(true);
            Texture2D a=null,b=null;
            try
            {
                material.SetTexture("_BaseMap",pattern); material.SetTexture("_HeightMap",pattern);
                material.SetFloat("_DebugView",1); a=Render(camera,256,256);
                material.SetFloat("_DebugView",3); b=Render(camera,256,256);
                int difference=MaxDifference(a,b);
                Require("synthetic color and height share sampling coordinates",difference,0,difference==0);
                Require("synthetic alignment field is nonconstant",RepeatDifference(a,17),1,RepeatDifference(a,17)>1);
            }
            finally
            {
                material.SetTexture("_BaseMap",albedo); material.SetTexture("_HeightMap",height);
                material.SetFloat("_DebugView",mode);
                if(a) UnityEngine.Object.DestroyImmediate(a); if(b) UnityEngine.Object.DestroyImmediate(b);
                UnityEngine.Object.DestroyImmediate(pattern);
            }
        }

        static double RepeatDifference(Texture2D image,int offset)
        {
            var p=image.GetPixels32(); double sum=0; int count=0;
            for(int y=2;y<image.height-2;y++) for(int x=2;x<image.width-offset-2;x++)
            {
                var a=p[y*image.width+x]; var b=p[y*image.width+x+offset];
                sum+=Math.Abs(a.r-b.r)+Math.Abs(a.g-b.g)+Math.Abs(a.b-b.b); count+=3;
            }
            return sum/count;
        }

        static void Require(string name,double value,double limit,bool pass)
        {
            Checks.Add(new Check{name=name,value=value,limit=limit,passed=pass});
            if(!pass) throw new InvalidOperationException(name+": "+value+" (limit "+limit+")");
        }
    }
}
