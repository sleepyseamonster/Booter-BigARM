using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace BooterBigArm.Editor
{
    public static class ConversionBaselineValidator
    {
        public const string PrototypeScenePath = "Assets/_Project/Scenes/PrototypeScene.unity";
        public const string SampleScenePath = "Assets/_Project/Scenes/SampleScene.unity";
        public const string ConversionScenePath = "Assets/_Project/Scenes/Isometric/IsometricConversionLab.unity";
        public const string PipelineAssetPath = "Assets/_Project/Settings/Rendering/URP/UniversalRP.asset";
        public const string LegacyRendererPath = "Assets/_Project/Settings/Rendering/URP/Renderer2D.asset";
        public const string ConversionRendererPath = "Assets/_Project/Settings/Rendering/URP/IsometricRenderer.asset";

        private static readonly string[] ExpectedEnabledScenes =
        {
            PrototypeScenePath,
            SampleScenePath
        };

        [MenuItem("Booter & BigARM/Validation/Validate Conversion Baseline")]
        public static void ValidateFromMenu()
        {
            var errors = CollectErrors();
            if (errors.Count == 0)
            {
                Debug.Log("Conversion baseline validation passed. Legacy scenes, renderer default, hierarchy guards, and Build Settings are preserved.");
                return;
            }

            Debug.LogError(FormatErrors(errors));
        }

        public static void ValidateFromCli()
        {
            var errors = CollectErrors();
            if (errors.Count > 0)
            {
                throw new BuildFailedException(FormatErrors(errors));
            }

            Debug.Log("Conversion baseline validation passed.");
        }

        public static List<string> CollectErrors()
        {
            var errors = new List<string>();
            ValidateAssetExists(PrototypeScenePath, errors);
            ValidateAssetExists(SampleScenePath, errors);
            ValidateAssetExists(PipelineAssetPath, errors);
            ValidateAssetExists(LegacyRendererPath, errors);
            ValidateBuildSettings(errors);
            ValidateRendererTopology(errors);
            ValidateProtectedHierarchyState(errors);
            return errors;
        }

        private static void ValidateAssetExists(string path, ICollection<string> errors)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) == null)
            {
                errors.Add($"Missing protected asset: {path}");
            }
        }

        private static void ValidateBuildSettings(ICollection<string> errors)
        {
            var enabledScenes = new List<string>();
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                {
                    enabledScenes.Add(scene.path);
                }

                if (string.Equals(scene.path, ConversionScenePath, StringComparison.Ordinal) && scene.enabled)
                {
                    errors.Add("The conversion lab must not be enabled in Build Settings before cutover.");
                }
            }

            if (enabledScenes.Count != ExpectedEnabledScenes.Length)
            {
                errors.Add($"Expected exactly {ExpectedEnabledScenes.Length} enabled legacy scenes, found {enabledScenes.Count}.");
                return;
            }

            for (var i = 0; i < ExpectedEnabledScenes.Length; i++)
            {
                if (!string.Equals(enabledScenes[i], ExpectedEnabledScenes[i], StringComparison.Ordinal))
                {
                    errors.Add($"Enabled scene {i} must be '{ExpectedEnabledScenes[i]}', found '{enabledScenes[i]}'.");
                }
            }
        }

        private static void ValidateRendererTopology(ICollection<string> errors)
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelineAssetPath);
            var legacyRenderer = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(LegacyRendererPath);
            if (pipeline == null || legacyRenderer == null)
            {
                return;
            }

            var serializedPipeline = new SerializedObject(pipeline);
            var defaultIndex = serializedPipeline.FindProperty("m_DefaultRendererIndex");
            var rendererList = serializedPipeline.FindProperty("m_RendererDataList");
            if (defaultIndex == null || rendererList == null || !rendererList.isArray)
            {
                errors.Add("Could not inspect the URP renderer list; serialized field names may have changed.");
                return;
            }

            if (defaultIndex.intValue != 0)
            {
                errors.Add($"URP default renderer index must remain 0, found {defaultIndex.intValue}.");
            }

            if (rendererList.arraySize == 0 || rendererList.GetArrayElementAtIndex(0).objectReferenceValue != legacyRenderer)
            {
                errors.Add("Renderer2D.asset must remain renderer index 0.");
            }

            var conversionRenderer = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(ConversionRendererPath);
            if (conversionRenderer == null)
            {
                return;
            }

            var conversionIndex = FindRendererIndex(rendererList, conversionRenderer);
            if (conversionIndex <= 0)
            {
                errors.Add("The conversion renderer must be present at a non-default index greater than 0.");
            }
        }

        private static int FindRendererIndex(SerializedProperty rendererList, UnityEngine.Object renderer)
        {
            for (var i = 0; i < rendererList.arraySize; i++)
            {
                if (rendererList.GetArrayElementAtIndex(i).objectReferenceValue == renderer)
                {
                    return i;
                }
            }

            return -1;
        }

        private static void ValidateProtectedHierarchyState(ICollection<string> errors)
        {
            var fullPath = Path.GetFullPath(PrototypeScenePath);
            if (!File.Exists(fullPath))
            {
                return;
            }

            var sceneYaml = File.ReadAllText(fullPath);
            ValidateInactiveGameObject(sceneYaml, "Sand Patch Grid", errors);
            ValidateInactiveGameObject(sceneYaml, "Ground Grid", errors);
        }

        private static void ValidateInactiveGameObject(string sceneYaml, string objectName, ICollection<string> errors)
        {
            var nameToken = $"  m_Name: {objectName}";
            var nameIndex = sceneYaml.IndexOf(nameToken, StringComparison.Ordinal);
            if (nameIndex < 0)
            {
                errors.Add($"Protected legacy GameObject '{objectName}' was not found in PrototypeScene.");
                return;
            }

            var blockEnd = sceneYaml.IndexOf("--- !u!", nameIndex, StringComparison.Ordinal);
            if (blockEnd < 0)
            {
                blockEnd = sceneYaml.Length;
            }

            var block = sceneYaml.Substring(nameIndex, blockEnd - nameIndex);
            if (block.IndexOf("  m_IsActive: 0", StringComparison.Ordinal) < 0)
            {
                errors.Add($"Protected legacy GameObject '{objectName}' must remain inactive.");
            }
        }

        private static string FormatErrors(IReadOnlyList<string> errors)
        {
            return "Conversion baseline validation failed:\n- " + string.Join("\n- ", errors);
        }
    }
}
