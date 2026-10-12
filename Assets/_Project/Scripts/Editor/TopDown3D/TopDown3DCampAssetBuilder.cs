using System;
using System.IO;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BooterBigArm.Editor
{
    /// <summary>Explicit, bounded asset authoring. Never rebuilds or saves Greater Wasteland.</summary>
    public static class TopDown3DCampAssetBuilder
    {
        public const string SettingsPath = "Assets/_Project/Settings/Camping/Resources/TopDown3DCampSettings.asset";
        public const string StudyPath = "Assets/_Project/Scenes/Reference/CampSleepStudy.unity";
        public static void BuildAssets()
        {
            const string wornSource = "Assets/_Project/Art/Characters/Booter/Cloak/Cloak_Worn.fbx";
            var cloakImporter = (ModelImporter)AssetImporter.GetAtPath(wornSource);
            if (cloakImporter == null) throw new InvalidOperationException("Worn cloak source missing.");
            if (!cloakImporter.isReadable) { cloakImporter.isReadable = true; cloakImporter.SaveAndReimport(); }
            var cloth = Material("CampCanvas", new Color(.42f, .32f, .21f), 0);
            var cloak = Material("CampCloak", new Color(.27f, .29f, .27f), 0);
            var iron = Material("CampIron", new Color(.19f, .13f, .1f), .65f);
            var preview = Material("CampFootprint", new Color(.65f, .8f, .56f), 0);
            var settings = LoadOrCreate<TopDown3DCampSettings>(SettingsPath);
            settings.deployedPrefab = Prefab("Assets/_Project/Art/Environment/Camp/PopupShade_Deployed.fbx", "PopupShade", cloth, iron, true);
            settings.packedPrefab = Prefab("Assets/_Project/Art/Environment/Camp/PopupShade_Packed.fbx", "PackedShade", cloth, iron, false);
            settings.wornCloakPrefab = Prefab("Assets/_Project/Art/Characters/Booter/Cloak/Cloak_Worn.fbx", "WornCloak", cloak, iron, false);
            settings.blanketPrefab = Prefab("Assets/_Project/Art/Characters/Booter/Cloak/Cloak_Blanket.fbx", "SleepingCloak", cloak, iron, false);
            settings.previewMaterial = preview;
            settings.sleepClip = Clip("Booter_CampSleep", 2, .03f, -.88f, -.25f);
            settings.restClip = Clip("Booter_CampRest", 2, .18f, -.7f, -.8f);
            settings.setupClip = Clip("Booter_CampSetup", 2, .35f, -.4f, -.15f);
            settings.wakeClip = Clip("Booter_CampWake", .45f, .1f, -.8f, -.1f);
            EditorUtility.SetDirty(settings);
            var icon = Icon();
            var item = LoadOrCreate<TopDown3DItemDefinition>("Assets/_Project/Settings/Items/Item_PopupCamp.asset");
            item.Configure(TopDown3DCampSettings.ItemId, "Pop-up camp", "Reusable vertical twilight shade. Deploy locally to rest or sleep.",
                "Camp", icon, 1, 2.5f, TopDown3DCarryPreference.Either, TopDown3DPackingPreference.QuickAccess);
            EditorUtility.SetDirty(item);
            var catalog = AssetDatabase.LoadAssetAtPath<TopDown3DItemCatalog>("Assets/_Project/Settings/Items/TopDown3DItemCatalog.asset");
            catalog.Configure(catalog.Definitions.Where(value => value != null && value.ItemId != item.ItemId).Concat(new[] { item }));
            EditorUtility.SetDirty(catalog);
            Input(); AddMenuAssignment();
            AssetDatabase.SaveAssetIfDirty(settings); AssetDatabase.SaveAssetIfDirty(item); AssetDatabase.SaveAssetIfDirty(catalog);
            if (!settings.HasAssets || !item.TryValidate(out var error)) throw new InvalidOperationException("Camp asset validation failed");
            Debug.Log("CAMP ASSETS BUILT: four mesh prefabs, four humanoid clips, item icon, camp input map, and settings. Production scene untouched.");
        }

        private static Material Material(string name, Color color, float metallic)
        {
            var path = "Assets/_Project/Materials/TopDown3D/Camp/" + name + ".mat";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var asset = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (asset == null) { asset = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(asset, path); }
            asset.SetColor("_BaseColor", color); asset.SetFloat("_Metallic", metallic); asset.SetFloat("_Smoothness", .12f);
            asset.SetFloat("_Cull", 0); EditorUtility.SetDirty(asset); AssetDatabase.SaveAssetIfDirty(asset); return asset;
        }
        private static GameObject Prefab(string modelPath, string name, Material cloth, Material iron, bool deployed)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null) throw new InvalidOperationException("Missing camp mesh: " + modelPath);
            var instance = new GameObject(name);
            var visual = UnityEngine.Object.Instantiate(model, instance.transform, false); visual.name = "Imported mesh";
            try
            {
                foreach (var renderer in instance.GetComponentsInChildren<MeshRenderer>())
                {
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(value => value != null && value.name.Contains("Iron") ? iron : cloth).ToArray();
                    renderer.shadowCastingMode = ShadowCastingMode.TwoSided;
                }
                if (name == "WornCloak")
                {
                    var backpack = AssetDatabase.LoadAssetAtPath<GameObject>(
                        "Assets/_Project/Prefabs/TopDown3D/Environment/Camp/FramedBackpack.prefab");
                    if (backpack == null) throw new InvalidOperationException("Booter's framed backpack prefab is missing.");
                    PrefabUtility.InstantiatePrefab(backpack, instance.transform);
                }
                if (deployed)
                {
                    instance.AddComponent<TopDown3DCampView>();
                    var screen = instance.AddComponent<BoxCollider>(); screen.center = new Vector3(0, .68f, 0); screen.size = new Vector3(2.2f, 1.35f, .12f);
                    var trigger = new GameObject("Camp interaction"); trigger.transform.SetParent(instance.transform, false);
                    trigger.transform.localPosition = new Vector3(0, .8f, .75f);
                    var box = trigger.AddComponent<BoxCollider>(); box.isTrigger = true; box.size = new Vector3(2.2f, 1.6f, 1.3f);
                }
                var path = "Assets/_Project/Prefabs/TopDown3D/Environment/Camp/" + name + ".prefab";
                Directory.CreateDirectory(Path.GetDirectoryName(path)); return PrefabUtility.SaveAsPrefabAsset(instance, path);
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
        }
        private static AnimationClip Clip(string name, float duration, float spine, float arms, float knees)
        {
            var path = "Assets/_Project/Art/Characters/Booter/Cloak/" + name + ".anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null) { clip = new AnimationClip { name = name, frameRate = 30 }; AssetDatabase.CreateAsset(clip, path); }
            var profile = AssetDatabase.LoadAssetAtPath<TopDown3DLocomotionClipProfile>("Assets/_Project/Settings/Player/TopDown3DLocomotionClipProfile.asset");
            // Preserve the avatar's authored body translation from the canonical idle pose.
            if (profile == null) throw new InvalidOperationException("Canonical locomotion profile missing.");
            var baseline = profile.GetRequiredEntry(TopDown3DLocomotionClipRole.Idle).Clip;
            foreach (var binding in AnimationUtility.GetCurveBindings(baseline))
            {
                if (!binding.propertyName.StartsWith("RootT.") && !binding.propertyName.StartsWith("RootQ.")) continue;
                var value = AnimationUtility.GetEditorCurve(baseline, binding).Evaluate(0);
                AnimationUtility.SetEditorCurve(clip, binding, AnimationCurve.Constant(0, duration, value));
            }
            foreach (var muscle in HumanTrait.MuscleName)
            {
                var value = 0f;
                if (muscle == "Spine Front-Back" || muscle == "Chest Front-Back") value = spine;
                if (muscle.EndsWith("Arm Down-Up")) value = arms;
                if (muscle.EndsWith("Forearm Stretch")) value = -.35f;
                if (muscle.EndsWith("Lower Leg Stretch")) value = knees;
                if (muscle.EndsWith("Upper Leg Front-Back") && name.Contains("Rest")) value = .65f;
                var curve = new AnimationCurve(new Keyframe(0, value), new Keyframe(duration * .5f, value + (muscle == "Chest Front-Back" ? .012f : 0)), new Keyframe(duration, value));
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Animator), muscle), curve);
            }
            var config = AnimationUtility.GetAnimationClipSettings(clip); config.loopTime = name.Contains("Sleep") || name.Contains("Rest");
            AnimationUtility.SetAnimationClipSettings(clip, config); EditorUtility.SetDirty(clip); AssetDatabase.SaveAssetIfDirty(clip); return clip;
        }
        private static Sprite Icon()
        {
            const string path = "Assets/_Project/UI/Icons/Camp/PopupCamp.png";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            for (var y = 0; y < 128; y++) for (var x = 0; x < 128; x++)
            {
                var panel = x >= 23 && x <= 31 && y >= 25 && y < 102;
                var top = y >= 95 && y < 103 && x >= 23 && x < 107;
                var canvas = x >= 32 && x <= 102 && y >= 56 && y < 95;
                var sleeper = y >= 28 && y <= 40 && x >= 43 && x <= 105;
                var head = (x - 98) * (x - 98) + (y - 43) * (y - 43) < 55;
                texture.SetPixel(x, y, panel || top || sleeper || head ? new Color(.93f, .84f, .66f) : canvas ? new Color(.6f, .46f, .27f) : Color.clear);
            }
            texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.textureType = TextureImporterType.Sprite;
            importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        private static void Input()
        {
            const string path = "Assets/_Project/Settings/Input/InputSystem_Actions.inputactions";
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
            if (asset.FindActionMap("Camp", false) != null) return;
            var map = new InputActionMap("Camp"); asset.AddActionMap(map);
            var move = map.AddAction("Move", InputActionType.Value, "<Gamepad>/leftStick"); move.expectedControlType = "Vector2";
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            map.AddAction("Look", InputActionType.Value, "<Gamepad>/rightStick").expectedControlType = "Vector2";
            Add(map, "Confirm", "<Gamepad>/buttonSouth", "<Keyboard>/enter");
            Add(map, "Cancel", "<Gamepad>/buttonEast", "<Keyboard>/escape");
            Add(map, "RotateLeft", "<Gamepad>/dpad/left", "<Keyboard>/z");
            Add(map, "RotateRight", "<Gamepad>/dpad/right", "<Keyboard>/c");
            Add(map, "FaceSun", "<Gamepad>/dpad/up", "<Keyboard>/r");
            // Use scheme groups for native prompt names and saved rebinding overrides.
            for (var i = 0; i < map.bindings.Count; i++)
                map.ChangeBinding(i).WithGroups(map.bindings[i].path.StartsWith("<Gamepad>") ? "Gamepad" : "Keyboard&Mouse");
            File.WriteAllText(path, asset.ToJson()); AssetDatabase.ImportAsset(path);
        }
        private static void Add(InputActionMap map, string name, string pad, string keyboard)
        { var action = map.AddAction(name, InputActionType.Button, pad); action.AddBinding(keyboard); }
        private static void AddMenuAssignment()
        {
            const string path = "Assets/_Project/Prefabs/UI/Resources/SystemMenu.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var source = root.GetComponentsInChildren<Button>(true).First(value => value.name == "Assign4");
                if (root.GetComponentsInChildren<Button>(true).Any(value => value.name == "Assign5")) return;
                var button = UnityEngine.Object.Instantiate(source.gameObject, source.transform.parent); button.name = "Assign5";
                foreach (var text in button.GetComponentsInChildren<Text>(true)) { text.name = text.name.Replace("Assign4", "Assign5"); text.text = "Camp"; }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); return asset;
        }
        public static void BuildStudy()
        {
            if (File.Exists(StudyPath)) throw new InvalidOperationException("Camp study already exists; preserve the authored scene.");
            if (!AssetDatabase.CopyAsset("Assets/_Project/Scenes/TopDown3D/GameplaySetup.unity", StudyPath)) throw new InvalidOperationException("Missing gameplay template");
            var scene = EditorSceneManager.OpenScene(StudyPath, OpenSceneMode.Additive);
            try
            {
                var inventory = scene.GetRootGameObjects().SelectMany(value => value.GetComponentsInChildren<TopDown3DPlayerInventory>(true)).Single();
                var serialized = new SerializedObject(inventory); var items = serialized.FindProperty("startingItems");
                var index = items.arraySize; items.InsertArrayElementAtIndex(index);
                items.GetArrayElementAtIndex(index).FindPropertyRelative("itemId").stringValue = TopDown3DCampSettings.ItemId;
                items.GetArrayElementAtIndex(index).FindPropertyRelative("quantity").intValue = 1; serialized.ApplyModifiedPropertiesWithoutUndo();
                var ground = GameObject.CreatePrimitive(PrimitiveType.Cube); ground.name = "Camp study authored ground";
                SceneManager.MoveGameObjectToScene(ground, scene); ground.transform.localScale = new Vector3(80, .5f, 80);
                ground.transform.position = new Vector3(0, -.25f, 0); ground.AddComponent<TopDown3DGroundSurface>();
                var player = inventory.transform; player.position = new Vector3(0, 1.3f, 1);
                EditorSceneManager.SaveScene(scene);
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
