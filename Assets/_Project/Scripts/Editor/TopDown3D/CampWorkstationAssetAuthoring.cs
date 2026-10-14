using System;
using System.IO;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BooterBigArm.Editor
{
    /// <summary>Authors owned workstation assets only. Scene/catalog integration is a separate operation.</summary>
    public static class CampWorkstationAssetAuthoring
    {
        public const string Settings = "Assets/_Project/Settings/Items/CampLoop";
        public const string Prefabs = "Assets/_Project/Prefabs/TopDown3D/Camping";
        public const string Materials = "Assets/_Project/Materials/TopDown3D/Camping";
        public const string Icons = "Assets/_Project/UI/Items/CampWorkstations";
        public const string RecipePath = Settings + "/EmptyArcBatteryRecipe.asset";
        public const string EmptyPath = Settings + "/EmptyArcBattery.asset";
        public const string ChargedPath = Settings + "/ChargedArcBattery.asset";

        public static void BuildAssetsFromCli()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Use an isolated background candidate.");
            foreach (var folder in new[] { Settings, Prefabs, Materials, Icons }) Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
            var empty = CreateBattery(EmptyPath, TopDown3DDustRefineryState.EmptyBatteryId, "Empty Arc Battery", false);
            var charged = CreateBattery(ChargedPath, TopDown3DDustRefineryState.ChargedBatteryId, "Charged Arc Battery", true);
            var recipe = AssetDatabase.LoadAssetAtPath<TopDown3DCraftingRecipe>(RecipePath);
            if (recipe == null)
            {
                recipe = ScriptableObject.CreateInstance<TopDown3DCraftingRecipe>();
                recipe.Configure("camp.empty-arc-battery.v1", new[] { new TopDown3DCraftingRecipe.Ingredient("resource.ironstone_ore", 4) }, empty.ItemId);
                AssetDatabase.CreateAsset(recipe, RecipePath);
            }
            var metal = Material("CampStationMetal", new Color(.25f, .20f, .15f), .65f);
            var dark = Material("CampStationDark", new Color(.1f, .1f, .09f), .3f);
            var dust = Material("CampStationDust", new Color(.68f, .34f, .12f), .1f);
            BuildWorkbench(recipe, metal, dark); BuildRefinery(metal, dark, dust);
            AssetDatabase.SaveAssets();
            if (!empty.TryValidate(out var error) || !charged.TryValidate(out error)) throw new InvalidOperationException(error);
            Debug.Log("CAMP_WORKSTATION_ASSETS_PASS: battery definitions, recipe, two owned station prefabs.");
        }
        private static TopDown3DItemDefinition CreateBattery(string path, string id, string label, bool charged)
        {
            var existing = AssetDatabase.LoadAssetAtPath<TopDown3DItemDefinition>(path); if (existing != null) return existing;
            var iconPath = Icons + (charged ? "/ChargedArcBattery.png" : "/EmptyArcBattery.png");
            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            var pixels = new Color32[4096];
            for (var y = 6; y < 57; y++) for (var x = 16; x < 48; x++)
            {
                var edge = x < 20 || x > 43 || y < 10 || y > 52;
                pixels[y * 64 + x] = edge ? new Color32(194, 173, 138, 255)
                    : charged ? new Color32(37, 193, 183, 255) : new Color32(61, 67, 69, 255);
            }
            for (var y = 57; y < 62; y++) for (var x = 25; x < 39; x++) pixels[y * 64 + x] = new Color32(194, 173, 138, 255);
            texture.SetPixels32(pixels); texture.Apply(); File.WriteAllBytes(iconPath, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(iconPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(iconPath); importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single; importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.SaveAndReimport();
            var item = ScriptableObject.CreateInstance<TopDown3DItemDefinition>();
            item.Configure(id, label, charged ? "Arc Energy held in a refined battery." : "An empty battery ready for the camp dust refinery.",
                "Supply", AssetDatabase.LoadAssetAtPath<Sprite>(iconPath), 1, .8f);
            item.ConfigureLoadout(TopDown3DEquipmentSlot.None, true);
            AssetDatabase.CreateAsset(item, path); return item;
        }
        private static Material Material(string name, Color color, float metallic)
        {
            var path = Materials + "/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path); if (existing != null) return existing;
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")); material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", metallic); material.SetFloat("_Smoothness", .3f); AssetDatabase.CreateAsset(material, path); return material;
        }
        private static GameObject Part(Transform parent, string name, PrimitiveType primitive, Vector3 position, Vector3 scale, Material material, bool collision = false)
        {
            var part = GameObject.CreatePrimitive(primitive); part.name = name; part.transform.SetParent(parent, false);
            part.transform.localPosition = position; part.transform.localScale = scale; part.GetComponent<Renderer>().sharedMaterial = material;
            if (!collision) Object.DestroyImmediate(part.GetComponent<Collider>()); return part;
        }
        private static void BuildWorkbench(TopDown3DCraftingRecipe recipe, Material metal, Material dark)
        {
            var path = Prefabs + "/CampWorkbench.prefab"; if (File.Exists(path)) return;
            var root = new GameObject("Camp Workbench");
            root.AddComponent<TopDown3DCampWorkbench>().Configure(recipe, null);
            Part(root.transform, "Work Surface", PrimitiveType.Cube, new Vector3(0, .85f, 0), new Vector3(1.7f, .16f, .85f), metal, true);
            foreach (var x in new[] { -.68f, .68f }) foreach (var z in new[] { -.3f, .3f })
                Part(root.transform, "Bench Leg", PrimitiveType.Cube, new Vector3(x, .4f, z), new Vector3(.12f, .8f, .12f), dark, true);
            Part(root.transform, "Bench Tool", PrimitiveType.Cube, new Vector3(.4f, .98f, 0), new Vector3(.32f, .15f, .3f), dark);
            PrefabUtility.SaveAsPrefabAsset(root, path); Object.DestroyImmediate(root);
        }
        private static void BuildRefinery(Material metal, Material dark, Material dust)
        {
            var path = Prefabs + "/DustRefinery.prefab"; if (File.Exists(path)) return;
            var root = new GameObject("Dust Refinery"); var refinery = root.AddComponent<TopDown3DDustRefinery>();
            Part(root.transform, "Refinery Body", PrimitiveType.Cube, new Vector3(0, .5f, 0), new Vector3(1.1f, 1, .8f), metal, true);
            Part(root.transform, "Canister Socket", PrimitiveType.Cylinder, new Vector3(-.27f, 1.04f, 0), new Vector3(.32f, .04f, .32f), dark);
            Part(root.transform, "Battery Socket", PrimitiveType.Cube, new Vector3(.29f, 1.05f, 0), new Vector3(.26f, .06f, .26f), dark);
            var canister = new GameObject("Docked Canister"); canister.transform.SetParent(root.transform, false);
            canister.transform.localPosition = new Vector3(-.27f, 1.08f, 0);
            Part(canister.transform, "Canister Base", PrimitiveType.Cylinder, new Vector3(0, .035f, 0), new Vector3(.27f, .035f, .27f), dark);
            Part(canister.transform, "Canister Cap", PrimitiveType.Cylinder, new Vector3(0, .38f, 0), new Vector3(.27f, .035f, .27f), metal);
            foreach (var x in new[] { -.12f, .12f })
                Part(canister.transform, "Canister Brace", PrimitiveType.Cube, new Vector3(x, .2f, 0), new Vector3(.025f, .33f, .025f), metal);
            var fill = Part(canister.transform, "Dust Fill", PrimitiveType.Cylinder, new Vector3(0, .22f, 0), new Vector3(.21f, .14f, .21f), dust);
            var battery = Part(root.transform, "Docked Battery", PrimitiveType.Cylinder, new Vector3(.29f, 1.22f, 0), new Vector3(.19f, .14f, .19f), metal);
            refinery.ConfigureVisuals(canister, battery, fill.transform, battery.GetComponent<Renderer>());
            PrefabUtility.SaveAsPrefabAsset(root, path); Object.DestroyImmediate(root);
        }
    }
}
