using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace BooterBigArm.Editor
{
    /// <summary>Basic modeled source kit. Authoring/capture is restricted to an offline candidate.</summary>
    public static class CampExpeditionSourceAuthoring
    {
        public const string Art = "Assets/_Project/Art/Environment/ExpeditionSources";
        public const string Prefabs = "Assets/_Project/Prefabs/TopDown3D/Studies/CampExpeditionSources";
        public const string Settings = "Assets/_Project/Settings/Items/CampExpeditionSources";
        public const string LibraryPath = Settings + "/Resources/CampExpeditionSourceLibrary.asset";
        private static Mesh rock, shell, bone, scale, ring, blade;
        private static Material rust, ore, boneMat, boneDark, shellMat, shellEdge, pad, iron, dirt;
        private static readonly string[] Ids = { "author.bio.stone_bone", "proposal.bio.gripping_pad_scales", "proposal.bio.shell", "resource.ironstone_ore",
            "component.mining.bone_shaft", "component.mining.iron_digging_head", "component.mining.iron_head_collar", "component.mining.iron_retaining_pin", "component.mining.shell_butt_cap" };
        private static readonly string[] Names = { "Stone-bone Remains", "Shed Gripping Pads", "Abandoned Shell Molt", "Ironstone Outcrop",
            "Abandoned Shaft Fitting", "Discarded Digging Head", "Damaged Head Connection", "Exposed Retaining Pin", "Split Tool Butt" };
        private static readonly string[] FileNames = { "StoneBoneRemains", "ShedGrippingPads", "AbandonedShellMolt", "IronstoneOutcrop", "AbandonedShaftFitting", "DiscardedDiggingHead", "DamagedHeadConnection", "ExposedRetainingPin", "SplitToolButt" };
        private static readonly string[] Verbs = { "Recover", "Collect", "Lift", "Collect", "Recover", "Recover", "Remove", "Withdraw", "Remove" };
        private static void Guard()
        {
            if (!Application.isBatchMode || Directory.Exists(".git") || !File.Exists(".camp-source-validation") || Application.isPlaying)
                throw new InvalidOperationException("Use only the marked offline expedition-source candidate in Edit Mode.");
        }
        private static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int split = path.LastIndexOf('/'); Folder(path.Substring(0, split)); AssetDatabase.CreateFolder(path.Substring(0, split), path.Substring(split + 1));
        }
        private static Material Mat(string name, Color color, float metallic = 0)
        {
            var value = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            value.SetColor("_BaseColor", color); value.SetFloat("_Smoothness", .24f); value.SetFloat("_Metallic", metallic);
            AssetDatabase.CreateAsset(value, Art + "/" + name.Replace(" ", "") + ".mat"); return value;
        }
        private static Mesh SaveMesh(string name, List<Vector3> vertices, List<int> triangles)
        {
            var mesh = new Mesh { name = name }; mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, Art + "/" + name.Replace(" ", "") + ".asset"); return mesh;
        }
        private static Mesh Radial(string name, int sides, float[] radii, float[] heights, bool boneShape = false)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            for (int r = 0; r < radii.Length; r++) for (int s = 0; s < sides; s++)
            {
                float a = s * Mathf.PI * 2 / sides;
                float radius = radii[r] * (1 + .06f * Mathf.Sin(s * 4.1f + r * 1.7f));
                v.Add(boneShape ? new Vector3(heights[r], Mathf.Cos(a) * radius, Mathf.Sin(a) * radius * .8f)
                    : new Vector3(Mathf.Cos(a) * radius, heights[r], Mathf.Sin(a) * radius * .78f));
            }
            for (int r = 0; r < radii.Length - 1; r++) for (int s = 0; s < sides; s++)
            {
                int a = r * sides + s, b = r * sides + (s + 1) % sides;
                if (boneShape) t.AddRange(new[] { a, b, a + sides, b, b + sides, a + sides });
                else t.AddRange(new[] { a, a + sides, b, b, a + sides, b + sides });
            }
            for (int s = 1; s < sides - 1; s++)
            {
                if (boneShape) { t.AddRange(new[] { 0, s + 1, s }); int end = (radii.Length - 1) * sides; t.AddRange(new[] { end, end + s, end + s + 1 }); }
                else { t.AddRange(new[] { 0, s, s + 1 }); int end = (radii.Length - 1) * sides; t.AddRange(new[] { end, end + s + 1, end + s }); }
            }
            return SaveMesh(name, v, t);
        }
        private static Mesh ShellMesh()
        {
            const int width = 12, length = 6; var v = new List<Vector3>(); var t = new List<int>(); int layerSize = (width + 1) * (length + 1);
            for (int layer = 0; layer < 2; layer++) for (int z = 0; z <= length; z++) for (int x = 0; x <= width; x++)
            {
                float u = x / (float)width, w = z / (float)length, angle = Mathf.Lerp(-1.25f, 1.25f, u);
                float r = .31f + (layer == 0 ? .018f : -.018f);
                v.Add(new Vector3(Mathf.Sin(angle) * r * (1 - .22f * w), Mathf.Cos(angle) * r + .018f * Mathf.Sin(w * 9), (w - .5f) * .64f));
            }
            for (int l = 0; l < 2; l++) for (int z = 0; z < length; z++) for (int x = 0; x < width; x++)
            {
                int a = l * layerSize + z * (width + 1) + x, b = a + width + 1;
                if (l == 0) t.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
                else t.AddRange(new[] { a, a + 1, b, a + 1, b + 1, b });
            }
            for (int z = 0; z < length; z++) { Rim(z * (width + 1), (z + 1) * (width + 1)); Rim((z + 1) * (width + 1) + width, z * (width + 1) + width); }
            for (int x = 0; x < width; x++) { Rim(x + 1, x); Rim(length * (width + 1) + x, length * (width + 1) + x + 1); }
            return SaveMesh("Curved Hollow Shell", v, t);
            void Rim(int a, int b) => t.AddRange(new[] { a, a + layerSize, b, b, a + layerSize, b + layerSize });
        }
        private static Mesh RingMesh()
        {
            var v = new List<Vector3>(); var t = new List<int>();
            for (int a = 0; a < 20; a++) for (int b = 0; b < 6; b++)
            { float u = a * Mathf.PI * .1f, w = b * Mathf.PI / 3, r = .105f + Mathf.Cos(w) * .025f; v.Add(new Vector3(Mathf.Cos(u) * r, Mathf.Sin(w) * .025f, Mathf.Sin(u) * r)); }
            for (int a = 0; a < 20; a++) for (int b = 0; b < 6; b++)
            { int i = a * 6 + b, j = (a + 1) % 20 * 6 + b, k = a * 6 + (b + 1) % 6, l = (a + 1) % 20 * 6 + (b + 1) % 6; t.AddRange(new[] { i, k, j, k, l, j }); }
            return SaveMesh("Forged Open Collar", v, t);
        }
        private static Mesh BladeMesh()
        {
            var v = new List<Vector3>(); var t = new List<int>();
            var outline = new[] { new Vector2(-.32f,.1f), new Vector2(.08f,.13f), new Vector2(.34f,.02f), new Vector2(.29f,-.16f), new Vector2(.17f,-.25f), new Vector2(.12f,-.08f), new Vector2(-.32f,-.05f) };
            foreach(float z in new[]{-.065f,.065f})foreach(var p in outline)v.Add(new Vector3(p.x,p.y,z));
            for(int i=1;i<outline.Length-1;i++){t.AddRange(new[]{0,i+1,i});t.AddRange(new[]{7,7+i,8+i});}
            for(int i=0;i<7;i++){int j=(i+1)%7;t.AddRange(new[]{i,j,7+i,j,7+j,7+i});}
            return SaveMesh("Downturned Adze And Poll",v,t);
        }
        private static GameObject Group(string name, Transform parent)
        { var value = new GameObject(name); value.transform.SetParent(parent, false); return value; }
        private static GameObject Model(Transform parent, string name, Mesh mesh, Material material, Vector3 position, Vector3 size, Vector3 rotation = default)
        {
            var value = Group(name, parent); value.transform.localPosition = position; value.transform.localScale = size; value.transform.localEulerAngles = rotation;
            value.AddComponent<MeshFilter>().sharedMesh = mesh; value.AddComponent<MeshRenderer>().sharedMaterial = material; return value;
        }
        private static void Primitive(Transform parent, string name, PrimitiveType type, Material material, Vector3 p, Vector3 s, Vector3 r = default)
        {
            var value = GameObject.CreatePrimitive(type); value.name = name; value.transform.SetParent(parent, false); value.transform.localPosition = p; value.transform.localScale = s; value.transform.localEulerAngles = r;
            Object.DestroyImmediate(value.GetComponent<Collider>()); value.GetComponent<Renderer>().sharedMaterial = material;
        }
        private static void GroundScraps(Transform parent, int type)
        {
            for (int i = 0; i < 4; i++) Model(parent, "Unusable fragments", type == 2 ? shell : rock, type == 0 ? boneDark : dirt,
                new Vector3((i % 2 == 0 ? -.35f : .32f), .015f, (i < 2 ? -.28f : .26f)), new Vector3(.18f, .09f, .15f), new Vector3(0, i * 73, 0));
        }
        private static CampExpeditionSourceView Build(int type)
        {
            var root = new GameObject(Names[type]);
            try
            {
                var baseParts = Group("Spent site - persistent nonblocking remains", root.transform);
                GroundScraps(baseParts.transform, type);
                var reward = Group("Recoverable material", root.transform);
                var debris = Group("Shallow compacted debris - tool access", root.transform);
                for (int i = 0; i < 3; i++) Model(debris.transform, "Crust over lodged material", rock, dirt, new Vector3((i - 1) * .19f, .095f, -.18f), new Vector3(.28f,.14f,.24f));
                if (type == 0)
                {
                    Model(reward.transform, "Sound shaft-length stone bone", bone, boneMat, new Vector3(0,.19f,0), new Vector3(1,1,1), new Vector3(0,-12,8));
                    Model(baseParts.transform, "Broken faunal hard part", bone, boneDark, new Vector3(-.12f,.07f,.28f), new Vector3(.44f,.45f,.45f), new Vector3(0,10,0));
                }
                else if (type == 1)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        var patch = Model(reward.transform, "Shed contact scale", scale, pad, new Vector3((i - 1) * .23f,.07f + i * .025f,0), new Vector3(.36f,.12f,.65f), new Vector3(0, (i - 1) * 18, 0));
                        for (int ridge = 0; ridge < 5; ridge++) Primitive(patch.transform, "Parallel contact ridge", PrimitiveType.Cube, shellEdge, new Vector3(0,.2f,(ridge-2)*.105f), new Vector3(.68f,.07f,.045f));
                    }
                }
                else if (type == 2)
                {
                    Model(baseParts.transform,"Empty split molt",shell,shellEdge,new Vector3(-.16f,-.03f,.1f),new Vector3(.78f,.65f,.83f),new Vector3(0,14,32));
                    Model(reward.transform,"Thick intact shed plate",shell,shellMat,new Vector3(.14f,.05f,-.03f),new Vector3(.9f,.95f,.85f),new Vector3(0,-12,-12));
                    Model(reward.transform,"Overlapping shell section",shell,shellEdge,new Vector3(-.14f,.04f,-.13f),new Vector3(.55f,.6f,.6f),new Vector3(0,30,7));
                }
                else if (type == 3)
                {
                    Model(baseParts.transform,"Exposed mineral face",rock,rust,new Vector3(.04f,0,.14f),new Vector3(.88f,.7f,.68f));
                    for(int i=0;i<3;i++)Model(reward.transform,"Iron-bearing fragment",rock,ore,new Vector3((i-1)*.21f,.025f,-.23f),new Vector3(.28f,.27f,.24f),new Vector3(0,i*47,0));
                    for(int i=0;i<3;i++)Primitive(baseParts.transform,"Dark iron-rich band",PrimitiveType.Cube,ore,new Vector3(.03f,.14f+i*.12f,.22f),new Vector3(.48f-i*.08f,.025f,.24f),new Vector3(0,-14,4));
                }
                else
                {
                    // Contemporary damaged manual-tool fittings: no ancient-machine reward substitution.
                    Primitive(baseParts.transform,"Iron repair cradle",PrimitiveType.Cube,rust,new Vector3(0,.045f,0),new Vector3(.8f,.07f,.5f));
                    for(int side=-1;side<=1;side+=2)Primitive(baseParts.transform,"Broken support",PrimitiveType.Cube,iron,new Vector3(side*.31f,.1f,.13f),new Vector3(.045f,.15f,.18f),new Vector3(0,0,side*12));
                    if(type==4)
                    {
                        Model(reward.transform,"Shaped stone-bone shaft",bone,boneMat,new Vector3(0,.19f,0),new Vector3(1,.65f,.65f));
                        for(int i=0;i<4;i++)Model(reward.transform,"Captured grip scales",scale,pad,new Vector3(-.23f+i*.1f,.245f,0),new Vector3(.13f,.09f,.25f));
                    }
                    if(type==5)
                    {Model(reward.transform,"Forged digging head and hammer poll",blade,iron,new Vector3(0,.36f,0),Vector3.one);Model(reward.transform,"Visible head eye",ring,rust,new Vector3(.04f,.39f,0),Vector3.one,new Vector3(90,0,0));}
                    if(type==6)
                    {Model(reward.transform,"Iron reinforcing collar",ring,iron,new Vector3(0,.25f,0),new Vector3(1.45f,1.6f,1.45f),new Vector3(65,0,0));Model(baseParts.transform,"Broken head seat",bone,boneDark,new Vector3(-.21f,.16f,0),new Vector3(.34f,.6f,.6f));}
                    if(type==7)
                    {
                        Primitive(reward.transform,"Exposed fitted retaining pin",PrimitiveType.Cylinder,iron,new Vector3(0,.21f,-.06f),new Vector3(.05f,.18f,.05f),new Vector3(0,0,90));
                        Primitive(reward.transform,"Pin head",PrimitiveType.Sphere,rust,new Vector3(-.34f,.21f,-.06f),new Vector3(.075f,.075f,.075f));
                        Model(baseParts.transform,"Empty split shaft connection",bone,boneDark,new Vector3(.1f,.17f,.06f),new Vector3(.4f,.8f,.8f),new Vector3(0,90,0));
                    }
                    if(type==8)
                    {Model(reward.transform,"Rounded shell butt cap",shell,shellMat,new Vector3(0,.15f,0),new Vector3(.55f,.65f,.45f),new Vector3(0,0,-12));Model(baseParts.transform,"Split rear handhold",bone,boneDark,new Vector3(-.22f,.16f,0),new Vector3(.42f,.65f,.65f));}
                }
                var anchor = Group("Work anchor", root.transform); anchor.transform.localPosition = new Vector3(0,.38f,-.12f);
                var target = root.AddComponent<BoxCollider>(); target.isTrigger = true; target.center = new Vector3(0,.4f,0); target.size = new Vector3(1.08f,.78f,.88f);
                var body = root.AddComponent<BoxCollider>(); body.center = new Vector3(0,type==3?.27f:.15f,0); body.size = new Vector3(.96f,type==3?.54f:.3f,.76f);
                var view = root.AddComponent<CampExpeditionSourceView>();
                view.Configure(Names[type],Verbs[type],type==3?"Loosen":type<4?"Free":"Remove",anchor.transform,reward,debris,target,body);
                var prefab = PrefabUtility.SaveAsPrefabAsset(root,Prefabs+"/"+FileNames[type]+".prefab");return prefab.GetComponent<CampExpeditionSourceView>();
            }
            finally { Object.DestroyImmediate(root); }
        }
        public static void BuildFromCli()
        {
            Guard();if(AssetDatabase.LoadAssetAtPath<CampExpeditionSourceLibrary>(LibraryPath)!=null)throw new InvalidOperationException("Preserve existing authored sources; no overwrite builder.");
            Folder(Art);Folder(Prefabs);Folder(Settings+"/Resources");
            rust=Mat("Oxidized Iron",new Color(.29f,.16f,.105f),.3f);ore=Mat("Ironstone Fracture",new Color(.13f,.12f,.14f),.2f);
            boneMat=Mat("Pale Stone Bone",new Color(.67f,.62f,.45f));boneDark=Mat("Weathered Hard Parts",new Color(.32f,.29f,.22f));
            shellMat=Mat("Ochre Shell",new Color(.49f,.265f,.14f));shellEdge=Mat("Shell Inner Layer",new Color(.67f,.43f,.24f));
            pad=Mat("Violet Gripping Pads",new Color(.22f,.16f,.23f));iron=Mat("Worked Iron",new Color(.31f,.34f,.35f),.65f);dirt=Mat("Compacted Dust",new Color(.28f,.205f,.145f));
            rock=Radial("Angular Ironstone",9,new[]{.5f,.48f,.31f,.13f},new[]{0f,.16f,.5f,.76f});
            bone=Radial("Long Faunal Bone",12,new[]{.085f,.115f,.055f,.048f,.063f,.12f,.08f},new[]{-.44f,-.36f,-.26f,0f,.26f,.36f,.44f},true);
            scale=Radial("Flattened Contact Scale",10,new[]{.34f,.5f,.34f},new[]{0f,.13f,.2f});shell=ShellMesh();ring=RingMesh();blade=BladeMesh();
            var library=ScriptableObject.CreateInstance<CampExpeditionSourceLibrary>();var entries=new List<CampExpeditionSourceLibrary.Entry>();
            for(int i=0;i<Ids.Length;i++)entries.Add(new CampExpeditionSourceLibrary.Entry{itemId=Ids[i],prefab=Build(i)});
            library.entries=entries.ToArray();AssetDatabase.CreateAsset(library,LibraryPath);AssetDatabase.SaveAssets();
            library.Validate(AssetDatabase.LoadAssetAtPath<CampExpeditionDefinition>("Assets/_Project/Settings/UI/CampExpedition/ExpeditionDefinition.asset"));
            Debug.Log("EXPEDITION_SOURCE_KIT_CREATED: nine modeled sources; economic content unchanged.");
        }
        public static void NormalizeAndCaptureFromCli()
        {
            Guard();
            foreach(var path in AssetDatabase.FindAssets("",new[]{Art}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>!AssetDatabase.IsValidFolder(p)).ToArray())
            {
                var renamed=path.Replace(" ","");if(renamed!=path){var error=AssetDatabase.MoveAsset(path,renamed);if(!string.IsNullOrEmpty(error))throw new InvalidOperationException(error);}
            }
            for(int i=0;i<FileNames.Length;i++)
            {
                var old=Prefabs+"/"+i+".prefab";var path=Prefabs+"/"+FileNames[i]+".prefab";
                if(AssetDatabase.LoadAssetAtPath<GameObject>(old)!=null){var error=AssetDatabase.MoveAsset(old,path);if(!string.IsNullOrEmpty(error))throw new InvalidOperationException(error);}
                if(i==4)
                {
                    var root=PrefabUtility.LoadPrefabContents(path);
                    try{root.name=Names[i];var view=root.GetComponent<CampExpeditionSourceView>();var data=new SerializedObject(view);data.FindProperty("sourceName").stringValue=Names[i];data.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,path);}
                    finally{PrefabUtility.UnloadPrefabContents(root);}
                }
            }
            AssetDatabase.SaveAssets();CampExpeditionSourceValidation.ValidateAndCaptureFromCli();
        }
    }
}
