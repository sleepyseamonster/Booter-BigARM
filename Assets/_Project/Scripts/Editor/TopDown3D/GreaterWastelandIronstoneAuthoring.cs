using System;
using System.Collections.Generic;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Editor
{
    /// <summary>Additive, bounded native-terrain resource authoring; never writes TerrainData.</summary>
    public static class GreaterWastelandIronstoneAuthoring
    {
        public const string ScenePath = "Assets/_Project/Scenes/Production/GreaterWasteland.unity";
        public const string PrefabPath = "Assets/_Project/Prefabs/TopDown3D/IronstoneNode.prefab";
        public const string AreaPrefabPath = "Assets/_Project/Prefabs/TopDown3D/Resources/GreaterWastelandIronstone.prefab";
        public const string RootName = "Greater Wasteland Ironstone";
        public const int NodeCount = 12;
        public const string IdPrefix = "greater-wasteland:ironstone:v1:authored:";
        private const string ResourcePath = "Assets/_Project/Settings/World/Resource_IronstoneNode.asset";
        private const string CatalogPath = "Assets/_Project/Settings/World/TopDown3DNaturalObjectCatalog.asset";
        private static readonly Vector2[] Offsets =
        {
            new Vector2(8, 5), new Vector2(-8, 7), new Vector2(14, -7), new Vector2(-15, -8),
            new Vector2(22, 12), new Vector2(-23, 14), new Vector2(32, -18), new Vector2(-35, -20),
            new Vector2(39, 25), new Vector2(-42, 29), new Vector2(48, -32), new Vector2(-51, -35)
        };

        [MenuItem("Booter & BigARM/Greater Wasteland Ironstone/Install Bounded Harvest Area")]
        public static void InstallFromMenu() => BuildForLoadedScene();

        public static string BuildForLoadedScene()
        {
            var scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Load Greater Wasteland in Edit Mode before ironstone authoring.");
            if (scene.isDirty)
                throw new InvalidOperationException("Ground placements must be based on the saved production scene.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(AreaPrefabPath) != null)
            {
                ValidatePrefabForScene(scene);
                return "Ironstone already installed and validated; no changes.";
            }

            var resource = AssetDatabase.LoadAssetAtPath<TopDown3DResourceDefinition>(ResourcePath);
            var catalog = AssetDatabase.LoadAssetAtPath<TopDown3DNaturalObjectCatalog>(CatalogPath);
            if (resource == null || !resource.TryValidate(catalog, out _))
                throw new InvalidOperationException("Ironstone resource assets are invalid.");
            var family = catalog.GetRequiredMeshFamily(resource.MeshShape, resource.MeshVariant);
            var roots = scene.GetRootGameObjects();
            var player = roots.SelectMany(root => root.GetComponentsInChildren<TopDown3DPlayerMotor>(true)).Single();
            var cargo = roots.SelectMany(root => root.GetComponentsInChildren<TopDown3DBigArmCargo>(true)).Single();
            var index = new FixedTerrainSurfaceIndex(
                roots.SelectMany(root => root.GetComponentsInChildren<Terrain>(true)), "greater-wasteland-authored-v1");
            Physics.SyncTransforms();
            var positions = new List<Vector3>();
            for (var i = 0; i < Offsets.Length; i++)
            {
                if (!TryPlacement(index, player.transform.position, cargo.transform.position, Offsets[i], positions, out var position))
                    throw new InvalidOperationException($"No safe native surface for ironstone candidate {i + 1}; scene untouched.");
                positions.Add(position);
            }

            EnsurePrefab(resource, family);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs/TopDown3D/Resources"))
                AssetDatabase.CreateFolder("Assets/_Project/Prefabs/TopDown3D", "Resources");
            var previousActive = SceneManager.GetActiveScene();
            var staging = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var rootObject = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(rootObject, staging);
            var state = rootObject.AddComponent<TopDown3DResourceWorldState>();
            state.Configure(1);
            try
            {
                for (var i = 0; i < positions.Count; i++)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, staging);
                    instance.name = $"Ironstone {i + 1:00}";
                    instance.transform.SetParent(rootObject.transform, true);
                    instance.transform.position = positions[i] - Vector3.up * resource.SinkDepth;
                    instance.transform.rotation = Quaternion.Euler(0f, i * 137.5f, 0f);
                    var authored = instance.GetComponent<TopDown3DAuthoredIronstoneNode>();
                    authored.Configure(IdPrefix + (i + 1).ToString("00"), resource, state,
                        instance.GetComponent<Collider>(), instance.GetComponentsInChildren<Renderer>());
                    PrefabUtility.RecordPrefabInstancePropertyModifications(authored);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
                }
                Physics.SyncTransforms();
                ValidateRoot(rootObject, index);
                if (PrefabUtility.SaveAsPrefabAsset(rootObject, AreaPrefabPath) == null)
                    throw new InvalidOperationException("Could not save ironstone area prefab.");
                return $"Installed runtime area prefab with {NodeCount} authored ironstone nodes; Booter center {player.transform.position}. Production scene untouched.";
            }
            finally
            {
                EditorSceneManager.CloseScene(staging, true);
                SceneManager.SetActiveScene(previousActive);
            }
        }

        private static bool TryPlacement(FixedTerrainSurfaceIndex index, Vector3 player, Vector3 companion,
            Vector2 offset, List<Vector3> accepted, out Vector3 result)
        {
            // Authored candidates can move at most six metres to find supported, walkable ground.
            for (var ring = 0; ring <= 3; ring++)
            for (var direction = 0; direction < (ring == 0 ? 1 : 8); direction++)
            {
                var angle = direction * Mathf.PI / 4f;
                var delta = offset + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * ring * 2f;
                var xz = new Vector2(player.x, player.z) + delta;
                if (Mathf.Abs(delta.x) < 5.6f || delta.magnitude < 6f
                    || !index.TrySample(xz, out var sample) || sample.SlopeDegrees > 32f
                    || Vector2.Distance(xz, new Vector2(companion.x, companion.z)) < 5f
                    || accepted.Any(position => Vector2.Distance(xz, new Vector2(position.x, position.z)) < 7f))
                    continue;
                var supported = true;
                foreach (var corner in new[] { new Vector2(-1.6f, -1.6f), new Vector2(-1.6f, 1.6f),
                    new Vector2(1.6f, -1.6f), new Vector2(1.6f, 1.6f) })
                    supported &= index.TrySample(xz + corner, out var edge)
                        && edge.SlopeDegrees <= 32f && Mathf.Abs(edge.Position.y - sample.Position.y) < 0.7f;
                if (!supported) continue;
                var overlaps = Physics.OverlapBox(sample.Position + Vector3.up * 0.7f,
                    new Vector3(1.7f, 0.65f, 1.7f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
                if (overlaps.Any(collider => !(collider is TerrainCollider))) continue;
                result = sample.Position;
                return true;
            }
            result = default;
            return false;
        }

        private static void EnsurePrefab(TopDown3DResourceDefinition resource, TopDown3DNaturalMeshFamily family)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null) return;
            var nodeObject = new GameObject("Ironstone Node");
            nodeObject.SetActive(false);
            try
            {
                var obstacle = nodeObject.AddComponent<BoxCollider>();
                obstacle.center = family.ColliderCenter;
                obstacle.size = family.ColliderSize;
                var renderers = TopDown3DNaturalObjectDecorator.CreateMemberLods(nodeObject, family, resource.ActiveMaterial);
                nodeObject.AddComponent<TopDown3DIronstoneNode>();
                var authored = nodeObject.AddComponent<TopDown3DAuthoredIronstoneNode>();
                var serialized = new SerializedObject(authored);
                serialized.FindProperty("definition").objectReferenceValue = resource;
                serialized.FindProperty("obstacle").objectReferenceValue = obstacle;
                var rendererProperty = serialized.FindProperty("renderers");
                rendererProperty.arraySize = renderers.Length;
                for (var i = 0; i < renderers.Length; i++) rendererProperty.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
                serialized.ApplyModifiedPropertiesWithoutUndo();
                // The prefab requires its scene-specific identity/state overrides before runtime use.
                nodeObject.SetActive(true);
                PrefabUtility.SaveAsPrefabAsset(nodeObject, PrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(nodeObject); }
        }

        public static void ValidateLoadedScene() => ValidatePrefabForScene(SceneManager.GetSceneByPath(ScenePath));

        public static void ValidatePrefabForScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) throw new InvalidOperationException("Greater Wasteland is not loaded.");
            var components = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Component>(true)).ToArray();
            if (components.OfType<TopDown3DProceduralWorld>().Any() || components.OfType<TopDown3DGameStateSaveService>().Any())
                throw new InvalidOperationException("Generated terrain/save services must remain absent.");
            var index = new FixedTerrainSurfaceIndex(components.OfType<Terrain>(), "greater-wasteland-authored-v1");
            var root = PrefabUtility.LoadPrefabContents(AreaPrefabPath);
            try { ValidateRoot(root, index); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            var action = components.OfType<TopDown3DPlayerActionController>().Single();
            var player = components.OfType<TopDown3DPlayerInventory>().Single();
            var resource = AssetDatabase.LoadAssetAtPath<TopDown3DResourceDefinition>(ResourcePath);
            if (!player.ItemCatalog.TryGetDefinition(resource.YieldedItem.ItemId, out var item) || item != resource.YieldedItem
                || action.GetComponent<TopDown3DInteractionController>() == null)
                throw new InvalidOperationException("Ironstone player inventory or interaction wiring is missing.");
            Debug.Log($"GREATER_WASTELAND_IRONSTONE_VALIDATED: {NodeCount} nodes, {index.Count} native terrains, one resource state.");
        }

        private static void ValidateRoot(GameObject root, FixedTerrainSurfaceIndex index)
        {
            var states = root.GetComponentsInChildren<TopDown3DResourceWorldState>(true);
            var nodes = root.GetComponentsInChildren<TopDown3DAuthoredIronstoneNode>(true);
            if (states.Length != 1 || nodes.Length != NodeCount || nodes.Select(node => node.StableId).Distinct().Count() != NodeCount)
                throw new InvalidOperationException("Ironstone requires one state owner and twelve unique nodes.");
            foreach (var node in nodes)
            {
                if (!node.StableId.StartsWith(IdPrefix, StringComparison.Ordinal) || node.WorldState != states[0]
                    || node.Definition == null || node.Obstacle == null || !node.Obstacle.enabled || node.Obstacle.isTrigger
                    || node.GetComponent<LODGroup>() == null
                    || !index.TrySample(new Vector2(node.transform.position.x, node.transform.position.z), out var sample)
                    || Mathf.Abs(node.transform.position.y - sample.Position.y + node.Definition.SinkDepth) > 0.01f)
                    throw new InvalidOperationException("Ironstone identity, collision, LOD or native grounding is invalid: " + node.name);
            }
        }
    }
}
