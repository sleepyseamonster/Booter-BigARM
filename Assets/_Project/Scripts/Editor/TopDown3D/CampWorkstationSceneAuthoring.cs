using System;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Editor
{
    public static class CampWorkstationSceneAuthoring
    {
        public const string RootName = "Camp Workstations";
        public const string ProfilePath = "Assets/_Project/Settings/Items/CampLoopLoadout/Resources/CampLoopLoadoutProfile.asset";
        public static T[] All<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();
        public static void InstallFromCli()
        {
            if (!Application.isBatchMode || Application.isPlaying) throw new InvalidOperationException("Use background Edit Mode authoring.");
            var profile = AssetDatabase.LoadAssetAtPath<TopDown3DCampLoopLoadoutProfile>(ProfilePath);
            var batteries = new[] { AssetDatabase.LoadAssetAtPath<TopDown3DItemDefinition>(CampWorkstationAssetAuthoring.EmptyPath),
                AssetDatabase.LoadAssetAtPath<TopDown3DItemDefinition>(CampWorkstationAssetAuthoring.ChargedPath) };
            if (batteries.Any(b => b == null)) throw new InvalidOperationException("Owned battery definitions missing.");
            if (profile == null) throw new InvalidOperationException("Preserve existing private loadout profile.");
            profile.definitions = profile.definitions.Concat(batteries).Distinct().ToArray(); EditorUtility.SetDirty(profile);
            var scene = EditorSceneManager.OpenScene(GreaterWastelandSceneScope.CampLoopScene, OpenSceneMode.Single);
            if (All<TopDown3DCampLoopEconomySession>(scene).Length != 0) { Validate(scene); return; }
            CampLoopTestingSceneBuilder.Validate(scene);
            var kit = All<TopDown3DPlayerInventory>(scene).Single(); var cargo = All<TopDown3DBigArmCargo>(scene).Single();
            var cache = All<TopDown3DResourceCache>(scene).Single();
            if (profile.sourceCatalog != new SerializedObject(cache).FindProperty("itemCatalog").objectReferenceValue)
                throw new InvalidOperationException("Preserve existing private loadout catalog.");
            var root = new GameObject(RootName); SceneManager.MoveGameObjectToScene(root, scene);
            var benchObject = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CampWorkstationAssetAuthoring.Prefabs + "/CampWorkbench.prefab"), scene);
            var refineryObject = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CampWorkstationAssetAuthoring.Prefabs + "/DustRefinery.prefab"), scene);
            benchObject.transform.SetParent(root.transform, false); refineryObject.transform.SetParent(root.transform, false);
            benchObject.transform.position = Supported(scene, cache.transform.position + new Vector3(-1.7f, 0, .6f));
            refineryObject.transform.position = Supported(scene, cache.transform.position + new Vector3(1.6f, 0, .6f));
            var bench = benchObject.GetComponent<TopDown3DCampWorkbench>(); var refinery = refineryObject.GetComponent<TopDown3DDustRefinery>();
            bench.Configure(AssetDatabase.LoadAssetAtPath<TopDown3DCraftingRecipe>(CampWorkstationAssetAuthoring.RecipePath), cache);
            var registry = new GameObject("Camp Canister Registry"); registry.transform.SetParent(root.transform, false);
            var devices = registry.AddComponent<TopDown3DPlacedHarvesterState>();
            devices.ConfigureAuthored(TopDown3DPlacedHarvesterState.AuthoredWorldIdentity, TopDown3DHarvesterSettings.Load()); devices.ConfigureCanisterContents(true);
            SetFlag(kit, "campCanisterContents"); SetFlag(cargo, "campCanisterContents"); SetFlag(cache, "campStorage");
            root.AddComponent<TopDown3DCampLoopEconomySession>().Configure(kit, cargo, cache, bench, refinery);
            Validate(scene); AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Camp workstation scene save failed.");
            Debug.Log("CAMP_WORKSTATIONS_INSTALLED");
        }
        private static void SetFlag(UnityEngine.Object owner, string name)
        {
            var data = new SerializedObject(owner); data.FindProperty(name).boolValue = true; data.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(owner);
        }
        public static Vector3 Supported(Scene scene, Vector3 point)
        {
            var terrain = All<Terrain>(scene).Single(t => point.x >= t.transform.position.x && point.z >= t.transform.position.z
                && point.x < t.transform.position.x + t.terrainData.size.x && point.z < t.transform.position.z + t.terrainData.size.z);
            point.y = terrain.SampleHeight(point) + terrain.transform.position.y; return point;
        }
        public static void ValidateFromCli() => Validate(EditorSceneManager.OpenScene(GreaterWastelandSceneScope.CampLoopScene));
        public static void Validate(Scene scene)
        {
            CampLoopTestingSceneBuilder.Validate(scene);
            var economy = All<TopDown3DCampLoopEconomySession>(scene).Single(); var bench = All<TopDown3DCampWorkbench>(scene).Single();
            var refinery = All<TopDown3DDustRefinery>(scene).Single(); var cache = All<TopDown3DResourceCache>(scene).Single();
            var profile = AssetDatabase.LoadAssetAtPath<TopDown3DCampLoopLoadoutProfile>(ProfilePath);
            profile.sourceCatalog.ApplyLoadoutProfile(profile);
            if (!TopDown3DAnimationPlaygroundSession.IsSessionOnly(scene) || economy.Cache != cache || economy.Workbench != bench || economy.Refinery != refinery
                || bench.Chest != cache || cache.State.Capacity != 24 || !bench.Recipe.TryValidate(cache.ItemCatalog, out _)
                || refinery.DustPerBattery != 10 || refinery.StableId == bench.StableId
                || All<TopDown3DPlacedHarvesterState>(scene).Length != 1 || !All<TopDown3DPlacedHarvesterState>(scene)[0].RetainsCanisterContents
                || new SerializedObject(economy.Kit).FindProperty("campCanisterContents").boolValue != true
                || new SerializedObject(economy.Cargo).FindProperty("campCanisterContents").boolValue != true
                || new SerializedObject(cache).FindProperty("campStorage").boolValue != true)
                throw new InvalidOperationException($"Camp economy authored ownership/configuration invalid: bindings {economy.Cache == cache}/{economy.Workbench == bench}/{economy.Refinery == refinery}, chest {bench.Chest == cache}, capacity {cache.State.Capacity}, recipe {bench.Recipe.TryValidate(cache.ItemCatalog, out var recipeError)}:{recipeError}, refinery cost {refinery.DustPerBattery}, devices {All<TopDown3DPlacedHarvesterState>(scene).Length}:{All<TopDown3DPlacedHarvesterState>(scene)[0].RetainsCanisterContents}.");
            foreach (var station in new[] { bench.transform, refinery.transform })
                if (Vector3.Distance(station.position, Supported(scene, station.position)) > .005f
                    || Vector3.Distance(station.position, cache.transform.position) > 2.2f) throw new InvalidOperationException("Unsupported or separated camp station.");
            Debug.Log("CAMP_WORKSTATIONS_VALIDATION_PASS: two supported stations, one linked 24-slot chest, private battery profile, fixed canister owner.");
        }
    }
}
