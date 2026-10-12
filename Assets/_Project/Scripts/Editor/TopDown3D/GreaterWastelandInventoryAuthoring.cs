using System;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Editor
{
    public static class GreaterWastelandInventoryAuthoring
    {
        public const string CachePath = "Assets/_Project/Prefabs/TopDown3D/Resources/GreaterWastelandResourceCache.prefab";
        [MenuItem("Booter & BigARM/Inventory/Build Starter Resource Cache")]
        public static void BuildCache()
        {
            var scene = SceneManager.GetSceneByPath(GreaterWastelandIronstoneAuthoring.ScenePath);
            if (!scene.IsValid() || !scene.isLoaded || scene.isDirty || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Load the saved production scene in clean Edit Mode.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(CachePath) != null)
            { ValidateCache(); return; }
            var terrains = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Terrain>(true));
            var index = new FixedTerrainSurfaceIndex(terrains, "greater-wasteland.authored.v1");
            var actor = TopDown3DGameHudCanvas.FindInScene<TopDown3DPlayerMotor>(scene);
            var center = actor.transform.position;
            // Read the starting-camp authoring source without coupling runtime code to that feature.
            var camp = AssetDatabase.LoadMainAssetAtPath("Assets/_Project/Settings/Camping/Resources/TopDown3DWaypointCampSettings.asset");
            if (camp != null)
            {
                var property = new SerializedObject(camp).FindProperty("position");
                if (property != null) center = property.vector3Value;
            }
            Physics.SyncTransforms();
            FixedTerrainSurfaceIndex.Sample sample = default;
            var found = false;
            foreach (var offset in new[] { new Vector2(8, 0), new Vector2(10, 0), new Vector2(8, -4), new Vector2(-8, 0), new Vector2(0, -8) })
            {
                if (!index.TrySample(new Vector2(center.x, center.z) + offset, out sample) || sample.SlopeDegrees > 25f) continue;
                var overlaps = Physics.OverlapBox(sample.Position + Vector3.up * 0.65f,
                    new Vector3(1.5f, 0.6f, 1.5f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
                if (overlaps.Any(collider => !(collider is TerrainCollider))) continue;
                found = true; break;
            }
            if (!found) throw new InvalidOperationException("No safe supported cache location; production untouched.");
            var previousActive = SceneManager.GetActiveScene();
            var staging = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var owner = GameObject.CreatePrimitive(PrimitiveType.Cube);
            owner.name = "Starting-area Resource Cache";
            try
            {
                owner.transform.position = sample.Position + Vector3.up * 0.6f;
                owner.transform.localScale = new Vector3(1.4f, 1.2f, 1f);
                owner.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                    "Assets/_Project/Materials/TopDown3D/Resources/IronstoneNodeDepleted.mat");
                var cache = owner.AddComponent<TopDown3DResourceCache>();
                cache.Configure(AssetDatabase.LoadAssetAtPath<TopDown3DItemCatalog>(
                    "Assets/_Project/Settings/Items/TopDown3DItemCatalog.asset"), 24, "greater-wasteland:cache:starter:v1");
                PrefabUtility.SaveAsPrefabAsset(owner, CachePath);
            }
            finally
            {
                EditorSceneManager.CloseScene(staging, true);
                SceneManager.SetActiveScene(previousActive);
            }
            ValidateCache();
        }

        public static void ValidateCache()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CachePath);
            var cache = prefab != null ? prefab.GetComponent<TopDown3DResourceCache>() : null;
            if (cache == null || cache.ItemCatalog == null || cache.State.Capacity != 24
                || string.IsNullOrWhiteSpace(cache.StableId) || prefab.GetComponent<Collider>() == null)
                throw new InvalidOperationException("Starter resource cache is incomplete.");
            Debug.Log("INVENTORY_CACHE_VALIDATED: finite 24-slot resource-only local cache; production scene untouched.");
        }
    }
}
