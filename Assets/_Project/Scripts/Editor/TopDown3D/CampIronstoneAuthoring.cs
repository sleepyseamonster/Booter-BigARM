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
    public static class CampIronstoneAuthoring
    {
        public const string Settings = "Assets/_Project/Settings/Items/CampLoopIronstone";
        public const string Icons = "Assets/_Project/UI/Items/CampIronstone";
        public const string Prefab = "Assets/_Project/Prefabs/TopDown3D/Camping/IronstoneProcessor.prefab";
        public const string Recipe = Settings + "/DrySortingRecipe.asset";
        public static void BuildAssetsFromCli()
        {
            RequireBackground(); Directory.CreateDirectory(Settings); Directory.CreateDirectory(Icons); AssetDatabase.Refresh();
            var labels = new[] { "Coarse Aggregate", "Iron-rich Concentrate", "Mineral Sand", "Mineral Dust" };
            var masses = new[] { .75f, 2f, 1f, .25f };
            var colors = new[] { new Color(.48f,.32f,.23f), new Color(.48f,.17f,.10f), new Color(.76f,.55f,.30f), new Color(.60f,.45f,.35f) };
            for (var i = 0; i < labels.Length; i++)
            {
                var path = Settings + "/" + i + "-" + labels[i].Replace(" ", "") + ".asset";
                if (File.Exists(path)) continue;
                var icon = Icons + "/Material" + i + ".png";
                var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false); var pixels = new Color[4096];
                for (var y=10; y<54; y++) for (var x=10; x<54; x++)
                    if ((x-32)*(x-32)+(y-32)*(y-32) < 480) pixels[y*64+x] = ((x+y+i)%7<2 ? colors[i]*.65f : colors[i]);
                texture.SetPixels(pixels); texture.Apply(); File.WriteAllBytes(icon, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(icon, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(icon); importer.textureType = TextureImporterType.Sprite;
                importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.SaveAndReimport();
                var item = ScriptableObject.CreateInstance<TopDown3DItemDefinition>();
                item.Configure(TopDown3DProcessRecipe.Products[i], labels[i], "Dry Ironstone sorting product. Camp Loop trial material.",
                    "Resource", AssetDatabase.LoadAssetAtPath<Sprite>(icon), 99, masses[i]);
                AssetDatabase.CreateAsset(item, path);
            }
            if (!File.Exists(Recipe))
            {
                var recipe = ScriptableObject.CreateInstance<TopDown3DProcessRecipe>();
                recipe.Configure(new[] { new TopDown3DCraftingRecipe.Ingredient(TopDown3DProcessRecipe.Ore, 4) },
                    new[] { new TopDown3DCraftingRecipe.Ingredient(TopDown3DProcessRecipe.Products[0],2), new TopDown3DCraftingRecipe.Ingredient(TopDown3DProcessRecipe.Products[1],1),
                        new TopDown3DCraftingRecipe.Ingredient(TopDown3DProcessRecipe.Products[2],1), new TopDown3DCraftingRecipe.Ingredient(TopDown3DProcessRecipe.Products[3],2) });
                AssetDatabase.CreateAsset(recipe, Recipe);
            }
            var profile = AssetDatabase.LoadAssetAtPath<TopDown3DCampLoopLoadoutProfile>(CampWorkstationSceneAuthoring.ProfilePath);
            var products = AssetDatabase.FindAssets("t:TopDown3DItemDefinition", new[] { Settings }).Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<TopDown3DItemDefinition>).ToArray();
            if (products.Length != 4 || products.Any(p => !p.TryValidate(out _))) throw new InvalidOperationException("Ironstone material definitions invalid.");
            var combined = profile.definitions.Concat(products).Distinct().ToArray();
            if (!profile.definitions.SequenceEqual(combined)) { profile.definitions = combined; EditorUtility.SetDirty(profile); }
            if (!File.Exists(Prefab))
            {
                var root = new GameObject("Ironstone Processor"); root.AddComponent<TopDown3DIronstoneProcessor>().Configure(AssetDatabase.LoadAssetAtPath<TopDown3DProcessRecipe>(Recipe));
                var metal = AssetDatabase.LoadAssetAtPath<Material>(CampWorkstationAssetAuthoring.Materials + "/CampStationMetal.mat");
                var dark = AssetDatabase.LoadAssetAtPath<Material>(CampWorkstationAssetAuthoring.Materials + "/CampStationDark.mat");
                Part(root.transform, "Crusher Housing", new Vector3(0,.55f,0), new Vector3(1.15f,1.1f,.8f), metal, true);
                Part(root.transform, "Dry Feed Hopper", new Vector3(0,1.2f,0), new Vector3(.8f,.2f,.65f), dark, false);
                Part(root.transform, "Eight-slot Output Tray", new Vector3(0,.35f,-.65f), new Vector3(1.2f,.16f,.5f), dark, true);
                Part(root.transform, "Manual Handle", new Vector3(.7f,.75f,0), new Vector3(.22f,.12f,.12f), metal, false);
                PrefabUtility.SaveAsPrefabAsset(root, Prefab); Object.DestroyImmediate(root);
            }
            AssetDatabase.SaveAssets(); Debug.Log("CAMP_IRONSTONE_ASSETS_PASS");
        }
        private static void Part(Transform root, string name, Vector3 position, Vector3 size, Material material, bool collision)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube); part.name = name; part.transform.SetParent(root, false);
            part.transform.localPosition = position; part.transform.localScale = size; part.GetComponent<Renderer>().sharedMaterial = material;
            if (!collision) Object.DestroyImmediate(part.GetComponent<Collider>());
        }
        private static void RequireBackground()
        { if (!Application.isBatchMode || Application.isPlaying) throw new InvalidOperationException("Use an isolated background Edit Mode candidate."); }
        public static void InstallFromCli()
        {
            RequireBackground();
            var scene = EditorSceneManager.OpenScene(GreaterWastelandSceneScope.CampLoopScene, OpenSceneMode.Single);
            var sites = CampWorkstationSceneAuthoring.All<TopDown3DCampSite>(scene);
            if (sites.Length > 0) { Validate(scene); Debug.Log("CAMP_IRONSTONE_ALREADY_INSTALLED"); return; }
            var economy = CampWorkstationSceneAuthoring.All<TopDown3DCampLoopEconomySession>(scene).Single();
            var root = new GameObject("Camp Management"); SceneManager.MoveGameObjectToScene(root, scene);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab), scene);
            instance.transform.SetParent(root.transform, false);
            instance.transform.position = CampWorkstationSceneAuthoring.Supported(scene, economy.Cache.transform.position + new Vector3(3.6f,0,-1.8f));
            var site = root.AddComponent<TopDown3DCampSite>();
            site.Configure(TopDown3DWaypointCampSettings.Load(), instance.GetComponent<TopDown3DIronstoneProcessor>()); economy.ConfigureSite(site);
            Validate(scene); if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Ironstone scene save failed.");
            Debug.Log("CAMP_IRONSTONE_INSTALLED");
        }
        public static void ValidateFromCli() { RequireBackground(); Validate(EditorSceneManager.OpenScene(GreaterWastelandSceneScope.CampLoopScene)); }
        public static void SealFromCli()
        {
            RequireBackground();
            var before = File.ReadAllBytes(GreaterWastelandSceneScope.CampLoopScene);
            ValidateFromCli(); InstallFromCli();
            if (!before.SequenceEqual(File.ReadAllBytes(GreaterWastelandSceneScope.CampLoopScene)))
                throw new InvalidOperationException("Repeated installation changed the saved scene.");
            CampIronstoneVisualProof.CaptureFromCli();
            Debug.Log("CAMP_IRONSTONE_SEAL_PASS: structural validation, idempotence and actual UI views.");
        }
        public static void Validate(Scene scene)
        {
            CampWorkstationSceneAuthoring.Validate(scene);
            var site = CampWorkstationSceneAuthoring.All<TopDown3DCampSite>(scene).Single();
            var processor = CampWorkstationSceneAuthoring.All<TopDown3DIronstoneProcessor>(scene).Single();
            var economy = CampWorkstationSceneAuthoring.All<TopDown3DCampLoopEconomySession>(scene).Single();
            var profile = AssetDatabase.LoadAssetAtPath<TopDown3DCampLoopLoadoutProfile>(CampWorkstationSceneAuthoring.ProfilePath);
            profile.sourceCatalog.ApplyLoadoutProfile(profile);
            if (economy.Site != site || site.Processor != processor || site.WorldId != TopDown3DWaypointCampSettings.Load().worldId
                || site.SiteId != TopDown3DWaypointCampSettings.Load().siteId || processor.Output == null
                || !processor.Recipe.TryFingerprint(profile.sourceCatalog, out _, out var reason)) throw new InvalidOperationException("Ironstone camp binding/recipe invalid.");
            if (Vector3.Distance(processor.transform.position, CampWorkstationSceneAuthoring.Supported(scene, processor.transform.position)) > .005f)
                throw new InvalidOperationException("Unsupported processor.");
            var approach = CampWorkstationSceneAuthoring.Supported(scene, (processor.transform.position + economy.Cache.transform.position) * .5f);
            Physics.SyncTransforms();
            if (Physics.OverlapCapsule(approach + Vector3.up * .42f, approach + Vector3.up * 1.5f, .38f, ~0, QueryTriggerInteraction.Ignore).Length != 0)
                throw new InvalidOperationException("Processor/chest approach lacks standing capsule clearance.");
            var loadPoint = CampWorkstationSceneAuthoring.Supported(scene, approach + Vector3.back * 2f);
            if (Physics.OverlapBox(loadPoint + Vector3.up * .8f, new Vector3(.65f,.7f,.9f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore).Any(c => c is not TerrainCollider))
                throw new InvalidOperationException("Processor loading approach lacks Legger clearance.");
            Debug.Log("CAMP_IRONSTONE_VALIDATION_PASS: one site, processor/tray, four resource products; legacy loop valid.");
        }
    }
}
