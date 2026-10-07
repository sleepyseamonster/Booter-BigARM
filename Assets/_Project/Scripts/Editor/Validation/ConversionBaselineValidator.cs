using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace BooterBigArm.Editor
{
    /// <summary>Validates production routing without depending on imported historical implementations.</summary>
    public static class ConversionBaselineValidator
    {
        public const string ProductionScenePath = "Assets/_Project/Scenes/Production/GreaterWasteland.unity";
        public const string PipelineAssetPath = "Assets/_Project/Settings/Rendering/URP/UniversalRP.asset";
        public const string ConversionRendererPath = "Assets/_Project/Settings/Rendering/URP/Renderer3D.asset";

        [MenuItem("Booter & BigARM/Validation/Validate Conversion Baseline")]
        public static void ValidateFromMenu()
        {
            var errors = CollectErrors();
            if (errors.Count == 0)
                Debug.Log("Production baseline validation passed.");
            else
                Debug.LogError(FormatErrors(errors));
        }

        public static void ValidateFromCli()
        {
            var errors = CollectErrors();
            if (errors.Count > 0)
                throw new BuildFailedException(FormatErrors(errors));
            Debug.Log("Production baseline validation passed.");
        }

        public static List<string> CollectErrors()
        {
            var errors = new List<string>();
            ValidateAssetExists(ProductionScenePath, errors);
            ValidateAssetExists(PipelineAssetPath, errors);
            ValidateAssetExists(ConversionRendererPath, errors);
            ValidateBuildSettings(errors);
            ValidateRendererTopology(errors);
            return errors;
        }

        private static void ValidateAssetExists(string path, ICollection<string> errors)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) == null)
                errors.Add($"Missing production asset: {path}");
        }

        private static void ValidateBuildSettings(ICollection<string> errors)
        {
            var enabledScenes = new List<string>();
            foreach (var scene in EditorBuildSettings.scenes)
                if (scene.enabled)
                    enabledScenes.Add(scene.path);
            if (enabledScenes.Count != 1)
            {
                errors.Add($"Expected exactly one enabled production scene, found {enabledScenes.Count}.");
                return;
            }
            if (!string.Equals(enabledScenes[0], ProductionScenePath, StringComparison.Ordinal))
                errors.Add($"Enabled scene must be '{ProductionScenePath}', found '{enabledScenes[0]}'.");
        }

        private static void ValidateRendererTopology(ICollection<string> errors)
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelineAssetPath);
            var renderer = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(ConversionRendererPath);
            if (pipeline == null || renderer == null)
                return;
            var serialized = new SerializedObject(pipeline);
            var defaultIndex = serialized.FindProperty("m_DefaultRendererIndex");
            var rendererList = serialized.FindProperty("m_RendererDataList");
            if (defaultIndex == null || rendererList == null || !rendererList.isArray)
            {
                errors.Add("Could not inspect the URP renderer list; serialized field names may have changed.");
                return;
            }
            var rendererIndex = -1;
            for (var i = 0; i < rendererList.arraySize; i++)
                if (rendererList.GetArrayElementAtIndex(i).objectReferenceValue == renderer)
                    rendererIndex = i;
            if (rendererIndex < 0)
                errors.Add("The production 3D renderer must be present in the URP renderer list.");
            else if (defaultIndex.intValue != rendererIndex)
                errors.Add($"The production 3D renderer must be the URP default at index {rendererIndex}; found {defaultIndex.intValue}.");
        }

        private static string FormatErrors(IReadOnlyList<string> errors)
        {
            return "Production baseline validation failed:\n- " + string.Join("\n- ", errors);
        }
    }
}
