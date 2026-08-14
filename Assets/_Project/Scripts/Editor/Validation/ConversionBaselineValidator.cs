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
        public const string PrototypeScenePath = "Assets/_Project/Legacy2D/Scenes/PrototypeScene.unity";
        public const string SampleScenePath = "Assets/_Project/Legacy2D/Scenes/SampleScene.unity";
        public const string ProductionScenePath = "Assets/_Project/Scenes/TopDown3D/TopDown3DPrototype.unity";
        public const string ConversionScenePath = "Assets/_Project/Scenes/Isometric/IsometricConversionLab.unity";
        public const string PipelineAssetPath = "Assets/_Project/Settings/Rendering/URP/UniversalRP.asset";
        public const string LegacyRendererPath = "Assets/_Project/Legacy2D/Settings/Rendering/URP/Renderer2D.asset";
        public const string ConversionRendererPath = "Assets/_Project/Settings/Rendering/URP/IsometricRenderer.asset";

        private static readonly string[] ExpectedEnabledScenes =
        {
            ProductionScenePath
        };

        [MenuItem("Booter & BigARM/Validation/Validate Conversion Baseline")]
        public static void ValidateFromMenu()
        {
            var errors = CollectErrors();
            if (errors.Count == 0)
            {
                Debug.Log("Production baseline validation passed. TopDown3D is primary and the isolated legacy scenes remain protected.");
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

            Debug.Log("Production and legacy-boundary validation passed.");
        }

        public static List<string> CollectErrors()
        {
            var errors = new List<string>();
            ValidateAssetExists(PrototypeScenePath, errors);
            ValidateAssetExists(SampleScenePath, errors);
            ValidateAssetExists(ProductionScenePath, errors);
            ValidateAssetExists(PipelineAssetPath, errors);
            ValidateAssetExists(LegacyRendererPath, errors);
            ValidateAssetExists(ConversionRendererPath, errors);
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
            var foundPrototypeScene = false;
            var foundSampleScene = false;
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                {
                    enabledScenes.Add(scene.path);
                }

                if (string.Equals(scene.path, ConversionScenePath, StringComparison.Ordinal) && scene.enabled)
                {
                    errors.Add("The isometric conversion lab must remain disabled in production Build Settings.");
                }

                if (string.Equals(scene.path, PrototypeScenePath, StringComparison.Ordinal))
                {
                    foundPrototypeScene = true;
                    if (scene.enabled)
                    {
                        errors.Add($"Legacy scene '{scene.path}' must remain disabled in production Build Settings.");
                    }
                }

                if (string.Equals(scene.path, SampleScenePath, StringComparison.Ordinal))
                {
                    foundSampleScene = true;
                    if (scene.enabled)
                    {
                        errors.Add($"Legacy scene '{scene.path}' must remain disabled in production Build Settings.");
                    }
                }
            }

            if (!foundPrototypeScene || !foundSampleScene)
            {
                errors.Add("Both preserved legacy scenes must remain registered and disabled in Build Settings.");
            }

            if (enabledScenes.Count != ExpectedEnabledScenes.Length)
            {
                errors.Add($"Expected exactly {ExpectedEnabledScenes.Length} enabled production scene, found {enabledScenes.Count}.");
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

            var conversionRenderer = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(ConversionRendererPath);
            if (conversionRenderer == null)
            {
                return;
            }

            if (rendererList.arraySize == 0 || rendererList.GetArrayElementAtIndex(0).objectReferenceValue != legacyRenderer)
            {
                errors.Add("Renderer2D.asset must remain renderer index 0.");
            }

            var conversionIndex = FindRendererIndex(rendererList, conversionRenderer);
            if (conversionIndex <= 0)
            {
                errors.Add("The production 3D renderer must be present at an index greater than 0.");
            }

            if (defaultIndex.intValue != conversionIndex)
            {
                errors.Add($"The production 3D renderer must be the URP default at index {conversionIndex}; found {defaultIndex.intValue}.");
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
