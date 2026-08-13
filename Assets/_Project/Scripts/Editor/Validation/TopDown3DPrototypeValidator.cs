using System;
using System.Collections.Generic;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Editor
{
    public static class TopDown3DPrototypeValidator
    {
        [MenuItem("Booter & BigARM/Top Down 3D/Validate Perspective Prototype")]
        public static void ValidateFromMenu()
        {
            var errors = CollectErrors();
            if (errors.Count > 0)
            {
                Debug.LogError(FormatErrors(errors));
                return;
            }

            Debug.Log("Perspective top-down 3D prototype validation passed.");
        }

        public static void ValidateFromCli()
        {
            var errors = CollectErrors();
            if (errors.Count > 0)
            {
                throw new BuildFailedException(FormatErrors(errors));
            }
        }

        public static List<string> CollectErrors()
        {
            var errors = ConversionBaselineValidator.CollectErrors();
            ValidateAssetExists(TopDown3DPrototypeBuilder.ScenePath, errors);
            ValidateAssetExists(TopDown3DPrototypeBuilder.WorldSettingsPath, errors);
            ValidateWorldCoverage(errors);
            ValidateCameraInput(errors);
            ValidateBuildSettings(errors);
            ValidateScene(errors);
            return errors;
        }

        private static void ValidateWorldCoverage(ICollection<string> errors)
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(
                TopDown3DPrototypeBuilder.WorldSettingsPath);
            if (settings == null)
            {
                return;
            }

            if (settings.StreamingRadius < 4)
            {
                errors.Add(
                    "TopDown3D world streaming must retain a four-chunk radius so supported camera angles and terrain relief cannot expose empty space.");
            }

            if (settings.ImmediateLoadRadius < 2)
            {
                errors.Add(
                    "TopDown3D initial loading must build a two-chunk radius before budgeted outer-ring streaming begins.");
            }
        }

        private static void ValidateCameraInput(ICollection<string> errors)
        {
            var input = AssetDatabase.LoadAssetAtPath<InputActionAsset>(TopDown3DPrototypeBuilder.InputActionsPath);
            var look = input?.FindActionMap("Gameplay", false)?.FindAction("Look", false);
            if (look == null)
            {
                errors.Add("The perspective camera requires Gameplay/Look in the shared input asset.");
                return;
            }

            if (!look.bindings.Any(binding =>
                    binding.path == "<Gamepad>/rightStick" && binding.groups.Contains("Gamepad")))
            {
                errors.Add("Gameplay/Look must bind the gamepad right stick for perspective camera orbit.");
            }
        }

        private static void ValidateAssetExists(string path, ICollection<string> errors)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) == null)
            {
                errors.Add($"Missing perspective prototype asset: {path}");
            }
        }

        private static void ValidateBuildSettings(ICollection<string> errors)
        {
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (string.Equals(scene.path, TopDown3DPrototypeBuilder.ScenePath, StringComparison.Ordinal))
                {
                    errors.Add("TopDown3DPrototype must remain absent from Build Settings before cutover.");
                }
            }
        }

        private static void ValidateScene(ICollection<string> errors)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TopDown3DPrototypeBuilder.ScenePath) == null)
            {
                return;
            }

            var loadedScene = SceneManager.GetSceneByPath(TopDown3DPrototypeBuilder.ScenePath);
            var openedForValidation = !loadedScene.IsValid() || !loadedScene.isLoaded;
            if (openedForValidation && !Application.isBatchMode && HasDirtyLoadedScene())
            {
                errors.Add("Cannot inspect TopDown3DPrototype while another loaded scene has unsaved changes.");
                return;
            }

            var scene = openedForValidation
                ? EditorSceneManager.OpenScene(
                    TopDown3DPrototypeBuilder.ScenePath,
                    Application.isBatchMode ? OpenSceneMode.Single : OpenSceneMode.Additive)
                : loadedScene;
            try
            {
                var roots = scene.GetRootGameObjects();
                ValidateSingle<TopDown3DInputRouter>(roots, errors);
                ValidateSingle<TopDown3DPlayerMotor>(roots, errors);
                ValidateSingle<TopDown3DCameraRig>(roots, errors);
                ValidateSingle<TopDown3DProceduralWorld>(roots, errors);
                ValidateSingle<TopDown3DBigArmFollower>(roots, errors);

                var cameras = FindComponents<Camera>(roots);
                if (cameras.Length != 1 || cameras[0].orthographic)
                {
                    errors.Add("TopDown3DPrototype must contain exactly one perspective Camera.");
                }
                else
                {
                    ValidateCameraRenderer(cameras[0], errors);
                }

                var bigArm = FindComponents<TopDown3DBigArmFollower>(roots).SingleOrDefault();
                var box = bigArm != null ? bigArm.GetComponent<BoxCollider>() : null;
                if (box == null || box.size.x > 1.75f || box.size.z > 2f)
                {
                    errors.Add("BigARM must use the compact foundation footprint, not the original oversized spike volume.");
                }

                for (var i = 0; i < roots.Length; i++)
                {
                    var transforms = roots[i].GetComponentsInChildren<Transform>(true);
                    for (var j = 0; j < transforms.Length; j++)
                    {
                        if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transforms[j].gameObject) > 0)
                        {
                            errors.Add($"Missing script on '{transforms[j].name}' in TopDown3DPrototype.");
                        }
                    }
                }
            }
            finally
            {
                if (openedForValidation && !Application.isBatchMode && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static void ValidateCameraRenderer(Camera camera, ICollection<string> errors)
        {
            var additional = camera.GetComponent<UniversalAdditionalCameraData>();
            if (additional == null)
            {
                errors.Add("Perspective camera is missing UniversalAdditionalCameraData.");
                return;
            }

            var serializedCamera = new SerializedObject(additional);
            var rendererIndex = serializedCamera.FindProperty("m_RendererIndex");
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(
                ConversionBaselineValidator.PipelineAssetPath);
            var renderer = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(
                ConversionBaselineValidator.ConversionRendererPath);
            if (rendererIndex == null || pipeline == null || renderer == null)
            {
                errors.Add("Perspective camera renderer relationship could not be inspected.");
                return;
            }

            var serializedPipeline = new SerializedObject(pipeline);
            var rendererList = serializedPipeline.FindProperty("m_RendererDataList");
            var index = rendererIndex.intValue;
            if (rendererList == null
                || !rendererList.isArray
                || index <= 0
                || index >= rendererList.arraySize
                || rendererList.GetArrayElementAtIndex(index).objectReferenceValue != renderer)
            {
                errors.Add("Perspective camera renderer index does not resolve to the protected 3D renderer asset.");
            }
        }

        private static void ValidateSingle<T>(GameObject[] roots, ICollection<string> errors) where T : Component
        {
            var count = FindComponents<T>(roots).Length;
            if (count != 1)
            {
                errors.Add($"TopDown3DPrototype must contain exactly one {typeof(T).Name}; found {count}.");
            }
        }

        private static T[] FindComponents<T>(GameObject[] roots) where T : Component
        {
            return roots.SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
        }

        private static bool HasDirtyLoadedScene()
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.IsValid() && scene.isLoaded && scene.isDirty)
                {
                    return true;
                }
            }

            return false;
        }

        private static string FormatErrors(IReadOnlyList<string> errors)
        {
            return "Perspective top-down 3D prototype validation failed:\n- " + string.Join("\n- ", errors);
        }
    }
}
