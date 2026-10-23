using System;
using System.Collections.Generic;
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
    public static class CampFirstToolAuthoring
    {
        public const string Settings = "Assets/_Project/Settings/Items/CampFirstToolLoop";
        public const string Prefabs = "Assets/_Project/Prefabs/TopDown3D/Studies/CampFirstToolLoop";
        public const string Materials = "Assets/_Project/Materials/Studies/CampFirstToolLoop";
        public const string Icons = "Assets/_Project/UI/Items/Studies/CampFirstToolLoop";
        public const string Art = "Assets/_Project/Art/Environment/Resources/CampFirstToolLoop";
        public const string RootName = "First Tool Loop";
        public const string RootPrefab = Prefabs + "/FirstToolLoop.prefab";
        public static T[] All<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray();
        private static readonly string[] Ids = { "proposal.bio.jaw", "author.bio.stone_bone", "proposal.bio.shell", "proposal.bio.insert",
            "component.mining.bone_shaft", "component.mining.shell_head_collar", "component.mining.shell_butt_cap", "proposal.t1.iron_bloom",
            "proposal.natural.iron_stock", "component.mining.iron_retaining_pin", CampFirstToolScenario.ToolId };
        private static readonly string[] Names = { "Dense jaw plates", "Stone bone", "Shell husks / shed plates", "Jaw cutting insert", "Bone multi-tool shaft",
            "Shell digging-head collar", "Shell tool butt cap", "Unconsolidated iron bloom", "Consolidated iron bar stock", "Iron tool retaining pin", "Bone salvage multi-tool" };
        private static readonly string[] Images = { "dense-jaw-plates-painterly-v1", "stone-bone-painterly-v1", "shell-husks-shed-plates-painterly-v1",
            "jaw-cutting-insert-painterly-v1", "bone-multi-tool-shaft-painterly-v1", "shell-digging-head-collar-painterly-v1", "shell-tool-butt-cap-painterly-v1",
            "unconsolidated-iron-bloom-painterly-v1", "consolidated-iron-bar-stock-painterly-v1", "iron-tool-retaining-pin-painterly-v1", "jawbone-salvage-multi-tool-v2" };
        private static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var split = path.LastIndexOf('/'); Folder(path.Substring(0, split)); AssetDatabase.CreateFolder(path.Substring(0, split), path.Substring(split + 1));
        }
        private static Material Material(string name, Color color, float metallic = 0)
        {
            var value = new Material(Shader.Find("Universal Render Pipeline/Lit")); value.name = name;
            value.SetColor("_BaseColor", color); value.SetFloat("_Metallic", metallic); value.SetFloat("_Smoothness", .24f);
            AssetDatabase.CreateAsset(value, Materials + "/" + name + ".mat"); return value;
        }
        private static GameObject Group(string name, Transform parent)
        { var g = new GameObject(name); if (parent != null) g.transform.SetParent(parent, false); return g; }
        private static GameObject Shape(Transform parent, string name, PrimitiveType type, Vector3 pos, Vector3 size, Material material, bool solid = false)
        {
            var g = GameObject.CreatePrimitive(type); g.name = name; g.transform.SetParent(parent, false); g.transform.localPosition = pos;
            g.transform.localScale = size; g.GetComponent<Renderer>().sharedMaterial = material;
            if (!solid) Object.DestroyImmediate(g.GetComponent<Collider>()); return g;
        }
        private static Vector3 Ground(Scene scene, Vector3 point)
        {
            foreach (var terrain in All<Terrain>(scene))
            {
                var p = terrain.transform.position; var size = terrain.terrainData.size;
                if (point.x < p.x || point.x >= p.x + size.x || point.z < p.z || point.z >= p.z + size.z) continue;
                point.y = terrain.SampleHeight(point) + p.y; return point;
            }
            throw new InvalidOperationException("Site left native terrain: " + point);
        }
        private static Vector3 Survey(Scene scene, Vector3 point)
        {
            for (var ring = 0; ring < 9; ring++) for (var n = 0; n < 12; n++)
            {
                var p = Ground(scene, point + Quaternion.Euler(0, n * 30, 0) * Vector3.right * ring * 1.5f);
                var overlaps = Physics.OverlapBox(p + Vector3.up * .8f, new Vector3(2.5f, .7f, 2.5f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
                if (overlaps.All(c => c is TerrainCollider)) return p;
            }
            throw new InvalidOperationException("No clear source footprint near " + point);
        }
        private static Mesh RockMesh()
        {
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            for (int ring = 0; ring < 3; ring++) for (int i = 0; i < 9; i++)
            {
                float angle = i * Mathf.PI * 2 / 9, radius = ring == 0 ? 1.2f : ring == 1 ? 1.4f : .8f;
                radius *= 1 + Mathf.Sin(i * 7.3f + ring) * .12f;
                vertices.Add(new Vector3(Mathf.Cos(angle) * radius, ring * .55f + Mathf.Sin(i * 5) * .07f, Mathf.Sin(angle) * radius));
            }
            vertices.Add(new Vector3(.1f, 1.4f, -.15f));
            for (int ring = 0; ring < 2; ring++) for (int i = 0; i < 9; i++)
            { int a = ring * 9 + i, b = ring * 9 + (i + 1) % 9; triangles.AddRange(new[] { a, a + 9, b, b, a + 9, b + 9 }); }
            for (int i = 0; i < 9; i++) triangles.AddRange(new[] { 18 + i, 27, 18 + (i + 1) % 9 });
            var mesh = new Mesh { name = "Layered Ironstone Formation" }; mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, Art + "/IronstoneFormation.asset"); return mesh;
        }
        private static GameObject BuildTool(Material bone, Material shell, Material iron)
        {
            var g = Group("Bone Salvage Multi-tool", null);
            Shape(g.transform, "Long stone-bone shaft", PrimitiveType.Capsule, Vector3.zero, new Vector3(.065f, .52f, .065f), bone);
            Shape(g.transform, "Broad jaw wedge", PrimitiveType.Cube, new Vector3(.12f, .49f, 0), new Vector3(.32f, .12f, .19f), bone).transform.localRotation = Quaternion.Euler(0, 0, -12);
            Shape(g.transform, "Blunt bony poll", PrimitiveType.Cube, new Vector3(-.17f, .49f, 0), new Vector3(.18f, .16f, .16f), bone);
            Shape(g.transform, "Shell head collar", PrimitiveType.Cylinder, new Vector3(0, .4f, 0), new Vector3(.12f, .045f, .12f), shell);
            Shape(g.transform, "Shell butt cap", PrimitiveType.Sphere, new Vector3(0, -.5f, 0), new Vector3(.095f, .075f, .095f), shell);
            Shape(g.transform, "Iron retaining pin", PrimitiveType.Cylinder, new Vector3(0, .43f, 0), new Vector3(.025f, .08f, .025f), iron).transform.localRotation = Quaternion.Euler(90, 0, 0);
            var prefab = PrefabUtility.SaveAsPrefabAsset(g, Prefabs + "/BoneSalvageMultiTool.prefab"); Object.DestroyImmediate(g); return prefab;
        }
        public static string Build(string repository)
        {
            if (!Application.isBatchMode || Directory.Exists(".git") || !File.Exists(".camp-first-tool-validation") || Application.isPlaying)
                throw new InvalidOperationException("Build only in the marked offline first-tool candidate.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RootPrefab) != null) throw new InvalidOperationException("Preserve the existing authored trial.");
            var scene = EditorSceneManager.OpenScene(TopDown3DAnimationPlaygroundSession.ScenePath);
            foreach (var folder in new[] { Settings, Prefabs, Materials, Icons, Art }) Folder(folder);
            var original = AssetDatabase.LoadAssetAtPath<TopDown3DCampLoopLoadoutProfile>("Assets/_Project/Settings/Items/CampLoopLoadout/Resources/CampLoopLoadoutProfile.asset");
            var profile = Object.Instantiate(original); profile.name = "FirstToolLoadout";
            profile.startingEquipment = original.startingEquipment.Where(i => i.ItemId != "equipment.digging_tool").ToArray();
            var definitions = original.definitions.ToList();
            for (int i = 0; i < Ids.Length; i++)
            {
                var path = Icons + "/" + i + ".png";
                File.Copy(Path.Combine(repository, "SourceArt/ItemArt/BrowserAssets", Images[i] + ".png"), path, false);
                AssetDatabase.ImportAsset(path); var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite; importer.spritePixelsPerUnit = 100; importer.maxTextureSize = 256; importer.alphaIsTransparency = true; importer.SaveAndReimport();
                var item = ScriptableObject.CreateInstance<TopDown3DItemDefinition>();
                item.Configure(Ids[i], Names[i], "First-tool study: one-unit prototype recipe; live critter harvesting is a temporary gameplay shortcut.",
                    i == 10 ? "Equipment" : "Resource", AssetDatabase.LoadAssetAtPath<Sprite>(path), i == 10 ? 1 : 20, i == 10 ? 1.5f : .2f);
                if (i == 10) item.ConfigureLoadout(TopDown3DEquipmentSlot.DiggingTool);
                AssetDatabase.CreateAsset(item, Settings + "/Item" + i + ".asset"); definitions.Add(item);
            }
            profile.definitions = definitions.ToArray(); AssetDatabase.CreateAsset(profile, Settings + "/FirstToolLoadout.asset");
            string ore = "resource.ironstone_ore";
            var maps = new[] { new[] { Ids[0] }, new[] { Ids[1] }, new[] { Ids[2] }, new[] { Ids[2] }, new[] { ore }, new[] { Ids[7], ore }, new[] { Ids[8] }, new[] { Ids[3], Ids[4], Ids[5], Ids[6], Ids[9] } };
            var outputs = new[] { 3, 4, 5, 6, 7, 8, 9, 10 }; var recipes = new TopDown3DCraftingRecipe[8];
            for (var i = 0; i < recipes.Length; i++)
            {
                var recipe = ScriptableObject.CreateInstance<TopDown3DCraftingRecipe>();
                recipe.Configure("camp.first-tool.v1." + i, maps[i].Select(id => new TopDown3DCraftingRecipe.Ingredient(id, 1)).ToArray(), Ids[outputs[i]]);
                AssetDatabase.CreateAsset(recipe, Settings + "/Recipe" + i + ".asset"); recipes[i] = recipe;
            }
            var shell = Material("Dusty Husk", new Color(.42f, .31f, .22f)); var bone = Material("Stone Bone", new Color(.67f, .64f, .50f));
            var dark = Material("Dark Joints", new Color(.14f, .12f, .10f)); var iron = Material("Retaining Iron", new Color(.3f, .27f, .24f), .6f);
            var oreMat = Material("Ironstone", new Color(.32f, .19f, .13f)); var signal = Material("Available Material", new Color(.63f, .47f, .28f));
            var spent = Material("Worked Material", new Color(.21f, .20f, .17f)); var rock = RockMesh(); var tool = BuildTool(bone, shell, iron);
            var root = Group(RootName, null); SceneManager.MoveGameObjectToScene(root, scene); var scenario = root.AddComponent<CampFirstToolScenario>();
            var camp = TopDown3DWaypointCampSettings.Load().position;
            var offsets = new[] { new Vector3(14,0,18), new Vector3(27,0,25), new Vector3(39,0,14), new Vector3(32,0,-19), new Vector3(48,0,27), new Vector3(49,0,-25) };
            var sourceItems = new[] { Ids[0], Ids[1], Ids[2], ore, Ids[2], ore }; var labels = new[] { "Jaw plates", "Stone bone", "Shell pieces", "Ironstone", "Spare shell", "Spare Ironstone" };
            var sources = new CampFirstToolSource[6]; Physics.SyncTransforms();
            for (var i = 0; i < 6; i++)
            {
                bool living = i != 3 && i != 5; var site = Group(living ? "Living Husk Critter · " + labels[i] : "Rock Formation · " + labels[i], root.transform);
                site.transform.position = Survey(scene, camp + offsets[i]); var visuals = Group("Persistent living body or formation", site.transform);
                if (living)
                {
                    AddLivingCollision(site);
                    for (int plate = 0; plate < 4; plate++) Shape(visuals.transform, "Overlapping shell plate", PrimitiveType.Sphere, new Vector3(0, .55f, (plate - 1.5f) * .28f), new Vector3(1.05f - plate * .06f, .55f, .55f), shell);
                    Shape(visuals.transform, "Head", PrimitiveType.Sphere, new Vector3(0, .34f, .78f), new Vector3(.5f, .35f, .42f), bone);
                    for (int leg = 0; leg < 6; leg++)
                        Shape(visuals.transform, "Jointed leg", PrimitiveType.Capsule, new Vector3(leg < 3 ? -.52f : .52f, .24f, (leg % 3 - 1) * .4f), new Vector3(.1f, .25f, .11f), dark).transform.localRotation = Quaternion.Euler(0, 0, leg < 3 ? 35 : -35);
                    for (int eye = 0; eye < 2; eye++) Shape(visuals.transform, "Eye", PrimitiveType.Sphere, new Vector3(eye == 0 ? -.17f : .17f, .46f, .93f), Vector3.one * .085f, dark);
                }
                else
                {
                    var formation = Group("Layered iron-bearing formation", visuals.transform);
                    formation.AddComponent<MeshFilter>().sharedMesh = rock; formation.AddComponent<MeshRenderer>().sharedMaterial = oreMat;
                    var solid = formation.AddComponent<BoxCollider>(); solid.center = Vector3.up * .6f; solid.size = new Vector3(1.9f, 1.15f, 1.8f);
                }
                var point = Group("Gather work face", site.transform); point.transform.localPosition = new Vector3(0, .45f, living ? -1.1f : -1.65f);
                var workFace = point.AddComponent<BoxCollider>(); workFace.isTrigger = true; workFace.size = new Vector3(1.1f, .7f, .35f);
                var available = Shape(site.transform, "Available stock marker", PrimitiveType.Sphere, new Vector3(.45f, .12f, -1.1f), new Vector3(.22f,.12f,.22f), signal);
                var depleted = Shape(site.transform, "Harvested stock marker", PrimitiveType.Sphere, available.transform.localPosition, available.transform.localScale, spent); depleted.SetActive(false);
                sources[i] = site.AddComponent<CampFirstToolSource>(); sources[i].Configure(scenario, i, "camp.first-tool.v1.source." + i, sourceItems[i], labels[i], i < 2 ? 1 : 2,
                    living, point.transform, workFace, available, depleted, living ? visuals.transform : null);
                Physics.SyncTransforms();
            }
            scenario.Configure(profile, recipes, sources); scenario.ConfigureTool(tool);
            var bench = All<TopDown3DCampWorkbench>(scene).Single(); var preview = (GameObject)PrefabUtility.InstantiatePrefab(tool, scene);
            preview.transform.SetParent(root.transform); preview.name = "Multi-tool assembly reference";
            preview.transform.SetPositionAndRotation(bench.transform.position + new Vector3(0, 1.02f, 0), Quaternion.Euler(0, 0, 90));
            PrefabUtility.SaveAsPrefabAssetAndConnect(root, RootPrefab, InteractionMode.AutomatedAction);
            AssetDatabase.SaveAssets(); if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Candidate scene save failed.");
            Validate(scene); return "Created six finite sources, live critters, rock formations, eleven definitions, eight recipes, tool model and explicit no-grant scenario.";
        }
        public static void Validate(Scene scene)
        {
            CampLoopTestingSceneBuilder.Validate(scene);
            var scenario = All<CampFirstToolScenario>(scene).Single();
            if (scenario.Loadout == null || scenario.Loadout.startingEquipment.Any(i => i.ItemId == "equipment.digging_tool")
                || scenario.Sources.Count != 6 || scenario.Recipes.Count != 8 || scenario.Sources.Select(s => s.StableId).Distinct().Count() != 6
                || scenario.Sources.Any(s => !s.gameObject.activeInHierarchy || string.IsNullOrWhiteSpace(s.Reward.ItemId) || s.Reward.Quantity <= 0))
                throw new InvalidOperationException("Incomplete first-tool scenario.");
            foreach (var recipe in scenario.Recipes) if (!recipe.TryValidate(All<TopDown3DPlayerInventory>(scene).Single().ItemCatalog, out var error)) throw new InvalidOperationException(error);
        }
        private static void AddLivingCollision(GameObject site)
        {
            var body = site.AddComponent<CapsuleCollider>(); body.direction = 2;
            body.center = new Vector3(0, .45f, .1f); body.radius = .43f; body.height = 1.65f;
        }
        public static void CompleteCandidateCollision()
        {
            if (!Application.isBatchMode || Directory.Exists(".git") || !File.Exists(".camp-first-tool-validation") || Application.isPlaying)
                throw new InvalidOperationException("Marked offline candidate required.");
            var scene = SceneManager.GetActiveScene();
            var scenario = All<CampFirstToolScenario>(scene).Single();
            foreach (var source in scenario.Sources.Where(s => s.IsLiving))
                if (source.GetComponent<CapsuleCollider>() == null) AddLivingCollision(source.gameObject);
            PrefabUtility.ApplyPrefabInstance(scenario.gameObject, InteractionMode.AutomatedAction);
            AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene); Validate(scene);
        }
        [MenuItem("Booter & BigARM/Camp Loop/Install First Tool Loop")]
        public static void InstallInOpenAuthorScene()
        {
            if (Application.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("A stable Edit Mode scene is required.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != TopDown3DAnimationPlaygroundSession.ScenePath || SceneManager.sceneCount != 1)
                throw new InvalidOperationException("Keep the existing Camp Loop scene open; this command never opens or saves scenes.");
            if (All<CampFirstToolScenario>(scene).Length != 0) { Validate(scene); return; }
            CampLoopTestingSceneBuilder.Validate(scene);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RootPrefab);
            if (prefab == null) throw new InvalidOperationException("Import the verified first-tool prefab first.");
            Undo.IncrementCurrentGroup(); var group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Install First Tool Loop");
            GameObject root = null;
            try
            {
                root = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                root.name = RootName;
                Undo.RegisterCreatedObjectUndo(root, "Install First Tool Loop");
                Validate(scene); EditorSceneManager.MarkSceneDirty(scene); Undo.CollapseUndoOperations(group);
            }
            catch { if (root != null) Undo.DestroyObjectImmediate(root); throw; }
        }
    }
}
