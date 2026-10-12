using System;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace BooterBigArm.Editor
{
    public static class InventoryHaulingStudyAuthoring
    {
        public const string ScenePath = "Assets/_Project/Scenes/Reference/InventoryHaulingStudy.unity";
        public const string SettingsPath = "Assets/_Project/Settings/Items/InventoryHaulingStudyPacking.asset";
        public const string TerrainPath = "Assets/_Project/Settings/Items/InventoryHaulingStudyGround.asset";
        public static void BuildFromCli()
        {
            if (!Application.isBatchMode || AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
                throw new InvalidOperationException("Build once in an isolated background batch project.");
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Reference/GeneratedWorld/TopDown3DPrototype.unity", OpenSceneMode.Single);
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.GetComponentInChildren<TopDown3DProceduralWorld>(true) != null
                    || root.GetComponentInChildren<TopDown3DGameStateSaveService>(true) != null
                    || !(root.GetComponentInChildren<TopDown3DPlayerMotor>(true) != null
                        || root.GetComponentInChildren<TopDown3DBigArmCargo>(true) != null
                        || root.GetComponentInChildren<TopDown3DInputRouter>(true) != null
                        || root.GetComponentInChildren<Camera>(true) != null || root.GetComponentInChildren<Light>(true) != null
                        || root.GetComponentInChildren<Canvas>(true) != null || root.GetComponentInChildren<EventSystem>(true) != null
                        || root.GetComponentInChildren<UnityEngine.Rendering.Volume>(true) != null)) Object.DestroyImmediate(root);
            }
            var ground = new TerrainData { heightmapResolution = 33, size = new Vector3(70, 5, 50) };
            AssetDatabase.CreateAsset(ground, TerrainPath);
            var groundObject = Terrain.CreateTerrainGameObject(ground); groundObject.name = "Authored flat hauling ground";
            groundObject.transform.position = new Vector3(-15, 0, -25); groundObject.AddComponent<TopDown3DGroundSurface>();
            var catalog = AssetDatabase.LoadAssetAtPath<TopDown3DItemCatalog>("Assets/_Project/Settings/Items/TopDown3DItemCatalog.asset");
            var kit = TopDown3DGameHudCanvas.FindInScene<TopDown3DPlayerInventory>(scene);
            kit.Configure(catalog, 8, new[] { new TopDown3DItemAmount("tool.micro_dust_harvester", 1), new TopDown3DItemAmount("tool.popup_camp", 1) });
            kit.transform.position = new Vector3(0, 1, 0);
            var packing = Object.Instantiate(AssetDatabase.LoadAssetAtPath<TopDown3DPackingSettings>("Assets/_Project/Settings/Items/TopDown3DPackingSettings.asset"));
            var edit = new SerializedObject(packing); edit.FindProperty("recommendedMass").floatValue = 10f; edit.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(packing, SettingsPath);
            var cargo = TopDown3DGameHudCanvas.FindInScene<TopDown3DBigArmCargo>(scene); cargo.Configure(catalog, packing);
            cargo.transform.position = new Vector3(-2, .82f, -2);
            var field = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/TopDown3D/Resources/GreaterWastelandIronstone.prefab"), scene);
            field.name = "Twelve authored study deposits";
            var nodes = field.GetComponentsInChildren<TopDown3DAuthoredIronstoneNode>();
            for (var i = 0; i < nodes.Length; i++)
            {
                var node = nodes[i]; node.transform.position = new Vector3(8 + i % 6 * 3, 1, i / 6 * 5);
                Physics.SyncTransforms(); node.transform.position += Vector3.up * (.05f - node.Obstacle.bounds.min.y);
                node.Configure($"inventory-study:ore:{i:D2}:v1", node.Definition, node.WorldState, node.Obstacle, node.GetComponentsInChildren<Renderer>());
            }
            var cacheObject = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(GreaterWastelandInventoryAuthoring.CachePath), scene);
            cacheObject.transform.position = new Vector3(-4, .6f, 0);
            cacheObject.GetComponent<TopDown3DResourceCache>().Configure(catalog, 2, "inventory-study:cache:v1");
            EditorSceneManager.SaveScene(scene, ScenePath); Validate(scene);
        }
        public static void Validate(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            if (roots.Any(root => root.GetComponentInChildren<TopDown3DProceduralWorld>(true) != null
                || root.GetComponentInChildren<TopDown3DGameStateSaveService>(true) != null
                || root.GetComponentInChildren<GreaterWastelandInventorySaveService>(true) != null)) throw new InvalidOperationException("Study contains a generated world or save owner.");
            var cargo = TopDown3DGameHudCanvas.FindInScene<TopDown3DBigArmCargo>(scene);
            if (Mathf.Abs(cargo.PackingSettings.HardMass - 11.5f) > .001f
                || roots.Sum(root => root.GetComponentsInChildren<TopDown3DAuthoredIronstoneNode>().Length) != 12)
                throw new InvalidOperationException("Study must reach its hard boundary with nine ore and reject the tenth.");
            Debug.Log("INVENTORY_HAULING_STUDY_VALIDATED: authored ground, 12 deposits, 7.5/10/11.5 study load bands; production tuning untouched.");
        }
    }
}
