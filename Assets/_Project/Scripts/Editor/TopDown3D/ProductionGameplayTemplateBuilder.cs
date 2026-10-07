using System;
using System.Collections.Generic;
using System.IO;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Editor
{
    /// <summary>Explicit authoring utility: captures current gameplay wiring without terrain or generation.</summary>
    public static class ProductionGameplayTemplateBuilder
    {
        public const string TemplateScenePath = "Assets/_Project/Scenes/TopDown3D/GameplaySetup.unity";
        private const string SourceScenePath = "Assets/_Project/Scenes/Production/GreaterWasteland.unity";
        private static readonly string[] PlayRoots =
        {
            "Top Down 3D Input", "Booter Perspective 3D Controller", "Legger", "Main Camera",
            "Directional Key Light", "Dust Atmosphere", "EventSystem", "Inventory Canvas", "Game HUD Canvas"
        };

        [MenuItem("Booter & BigARM/Authoring/Create Production Gameplay Template")]
        public static void BuildFromMenu() => BuildFromCli();

        public static void BuildFromCli()
        {
            RequireCleanEditMode();
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SourceScenePath) == null)
                throw new FileNotFoundException("The current production source scene is missing.", SourceScenePath);
            if (File.Exists(TemplateScenePath) || AssetDatabase.LoadMainAssetAtPath(TemplateScenePath) != null)
                throw new InvalidOperationException("The gameplay template already exists; refusing to overwrite authored work.");
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                // Copying the scene retains cross-root references and scene lighting settings.
                // Only the new asset is opened, edited, and saved; the source scene is untouched.
                if (!AssetDatabase.CopyAsset(SourceScenePath, TemplateScenePath))
                    throw new IOException("Could not copy the production gameplay source scene.");
                var scene = EditorSceneManager.OpenScene(TemplateScenePath, OpenSceneMode.Single);
                var expected = new HashSet<string>(PlayRoots, StringComparer.Ordinal);
                foreach (var root in scene.GetRootGameObjects())
                    if (!expected.Contains(root.name))
                        UnityEngine.Object.DestroyImmediate(root);
                ValidateScene(scene);
                if (!EditorSceneManager.SaveScene(scene, TemplateScenePath))
                    throw new IOException("Could not save the production gameplay template.");
                Debug.Log("PRODUCTION_GAMEPLAY_TEMPLATE_CREATED: " + TemplateScenePath);
            }
            finally
            {
                RestoreCleanSceneSetup(setup);
            }
        }

        [MenuItem("Booter & BigARM/Validation/Validate Production Gameplay Template")]
        public static void ValidateFromMenu() => ValidateFromCli();

        public static void ValidateFromCli()
        {
            RequireCleanEditMode();
            var existing = SceneManager.GetSceneByPath(TemplateScenePath);
            var scene = existing.IsValid() && existing.isLoaded
                ? existing : EditorSceneManager.OpenScene(TemplateScenePath, OpenSceneMode.Additive);
            try
            {
                ValidateScene(scene);
                Debug.Log("PRODUCTION_GAMEPLAY_TEMPLATE_VALIDATED: nine configured roots; no terrain, generator or save service.");
            }
            finally
            {
                if (!existing.IsValid() || !existing.isLoaded)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        public static void ValidateScene(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            if (roots.Length != PlayRoots.Length)
                throw new InvalidDataException("Gameplay template must contain exactly nine play roots.");
            var byName = new Dictionary<string, GameObject>(StringComparer.Ordinal);
            foreach (var root in roots)
            {
                if (!byName.TryAdd(root.name, root))
                    throw new InvalidDataException("Duplicate gameplay template root: " + root.name);
                if (root.GetComponentsInChildren<Terrain>(true).Length != 0
                    || root.GetComponentsInChildren<TerrainCollider>(true).Length != 0
                    || root.GetComponentsInChildren<TopDown3DProceduralWorld>(true).Length != 0
                    || root.GetComponentsInChildren<TopDown3DGameStateSaveService>(true).Length != 0)
                    throw new InvalidDataException("Gameplay template must not contain terrain, generation or save integration.");
                foreach (var component in root.GetComponentsInChildren<Component>(true))
                {
                    if (component == null)
                        throw new InvalidDataException("Gameplay template has a missing script.");
                    ValidateSceneReferences(component, scene);
                }
            }
            foreach (var name in PlayRoots)
                if (!byName.ContainsKey(name))
                    throw new InvalidDataException("Missing gameplay template root: " + name);

            var player = byName["Booter Perspective 3D Controller"];
            var cameraObject = byName["Main Camera"];
            var input = Require<TopDown3DInputRouter>(byName["Top Down 3D Input"]);
            if (input.InputActions == null)
                throw new InvalidDataException("Gameplay template input actions are missing.");
            var motor = Require<TopDown3DPlayerMotor>(player);
            var follower = Require<TopDown3DBigArmFollower>(byName["Legger"]);
            var rig = Require<TopDown3DCameraRig>(cameraObject);
            var camera = Require<Camera>(cameraObject);
            Require<AudioListener>(cameraObject);
            Require<CapsuleCollider>(player);
            Require<Rigidbody>(player);
            Require<PerpetualTwilightSun>(byName["Directional Key Light"]);
            Require<Light>(byName["Directional Key Light"]);
            Require<TopDown3DGameHudCanvas>(byName["Game HUD Canvas"]);
            var dust = Require<TopDown3DDustAtmosphere>(byName["Dust Atmosphere"]);
            var events = Require<EventSystem>(byName["EventSystem"]);
            var inventory = Require<TopDown3DPlayerInventory>(player);
            var action = Require<TopDown3DPlayerActionController>(player);
            var canvas = Require<TopDown3DInventoryCanvas>(byName["Inventory Canvas"]);
            var ui = FindUnique<TopDown3DInventoryUiController>(roots);
            ExpectReference(motor, "input", input);
            ExpectReference(motor, "cameraBasis", rig.transform);
            ExpectReference(follower, "input", input);
            ExpectReference(follower, "followTarget", player.transform);
            ExpectReference(follower, "cameraBasis", rig.transform);
            ExpectReference(rig, "input", input);
            ExpectReference(rig, "target", player.transform);
            ExpectReference(dust, "subject", player.transform);
            ExpectReference(dust, "outputCamera", camera);
            ExpectReference(ui, "input", input);
            ExpectReference(ui, "inventory", inventory);
            ExpectReference(ui, "actionController", action);
            ExpectReference(ui, "canvas", canvas);
            ExpectReference(ui, "eventSystem", events);
        }

        private static void RestoreCleanSceneSetup(SceneSetup[] setup)
        {
            // A fresh headless Editor can report an unnamed empty scene, which
            // RestoreSceneManagerSetup cannot load by path. Clean unnamed scenes
            // contain no saved authoring state; restore their empty Editor state.
            var saved = new List<SceneSetup>();
            var emptyLoaded = 0;
            var emptyWasActive = false;
            var activeSavedPath = string.Empty;
            foreach (var entry in setup)
            {
                if (!string.IsNullOrEmpty(entry.path))
                {
                    saved.Add(entry);
                    if (entry.isActive) activeSavedPath = entry.path;
                }
                else if (entry.isLoaded)
                {
                    emptyLoaded++;
                    emptyWasActive |= entry.isActive;
                }
            }
            if (saved.Count == 0)
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                return;
            }
            if (string.IsNullOrEmpty(activeSavedPath))
                for (var i = 0; i < saved.Count; i++)
                    if (saved[i].isLoaded)
                    {
                        var entry = saved[i];
                        entry.isActive = true;
                        saved[i] = entry;
                        break;
                    }
            EditorSceneManager.RestoreSceneManagerSetup(saved.ToArray());
            var empty = default(Scene);
            for (var i = 0; i < emptyLoaded; i++)
                empty = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            if (emptyWasActive && empty.IsValid()) SceneManager.SetActiveScene(empty);
            else if (!string.IsNullOrEmpty(activeSavedPath))
                SceneManager.SetActiveScene(SceneManager.GetSceneByPath(activeSavedPath));
        }

        private static void RequireCleanEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Gameplay template authoring and validation require Edit mode.");
            for (var i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save or discard scene changes before explicit gameplay template work.");
        }

        private static T Require<T>(GameObject owner) where T : Component
        {
            var component = owner.GetComponent<T>();
            if (component == null)
                throw new InvalidDataException($"Missing {typeof(T).Name} on {owner.name}.");
            return component;
        }

        private static T FindUnique<T>(GameObject[] roots) where T : Component
        {
            T found = null;
            foreach (var root in roots)
                foreach (var component in root.GetComponentsInChildren<T>(true))
                {
                    if (found != null) throw new InvalidDataException("Duplicate " + typeof(T).Name);
                    found = component;
                }
            if (found == null) throw new InvalidDataException("Missing " + typeof(T).Name);
            return found;
        }

        private static void ExpectReference(Component owner, string propertyName, UnityEngine.Object expected)
        {
            var property = new SerializedObject(owner).FindProperty(propertyName);
            if (property == null || property.objectReferenceValue != expected)
                throw new InvalidDataException($"Incorrect {owner.GetType().Name}.{propertyName} gameplay reference.");
        }

        private static void ValidateSceneReferences(Component component, Scene scene)
        {
            var iterator = new SerializedObject(component).GetIterator();
            while (iterator.Next(true))
            {
                if (iterator.propertyType != SerializedPropertyType.ObjectReference) continue;
                var value = iterator.objectReferenceValue;
                if (value is Component referenced && referenced.gameObject.scene != scene
                    || value is GameObject gameObject && gameObject.scene != scene)
                {
                    if (value != null && !EditorUtility.IsPersistent(value))
                        throw new InvalidDataException("Gameplay template has a cross-scene reference: " + iterator.propertyPath);
                }
            }
        }
    }
}
