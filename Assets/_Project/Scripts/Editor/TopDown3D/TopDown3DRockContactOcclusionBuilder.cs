using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace BooterBigArm.Editor
{
    internal static class TopDown3DRockContactOcclusionBuilder
    {
        private const string RendererPath =
            "Assets/_Project/Settings/Rendering/URP/IsometricRenderer.asset";

        [MenuItem("Tools/Booter & BigARM/Rendering/Configure Rock Contact Occlusion")]
        public static void ConfigureRockContactOcclusion()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null)
            {
                throw new InvalidOperationException(
                    $"The production URP renderer is missing at '{RendererPath}'.");
            }

            var matches = renderer.rendererFeatures
                .OfType<ScreenSpaceAmbientOcclusion>()
                .ToArray();
            if (matches.Length > 1)
            {
                throw new InvalidOperationException(
                    "The production renderer contains duplicate ambient-occlusion features.");
            }

            var feature = matches.Length == 1
                ? matches[0]
                : AddFeature(renderer);
            ConfigureFeature(feature);
            feature.SetActive(true);
            feature.Create();
            renderer.SetDirty();
            EditorUtility.SetDirty(feature);
            EditorUtility.SetDirty(renderer);
            AssetDatabase.SaveAssets();
            Debug.Log(
                "[Rock Workbench] Configured restrained SSAO for rock contact seams and nearby surfaces.",
                renderer);
        }

        private static ScreenSpaceAmbientOcclusion AddFeature(UniversalRendererData renderer)
        {
            var feature = ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();
            feature.name = "Rock Contact Occlusion";
            feature.hideFlags = HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(feature, renderer);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId);

            var serializedRenderer = new SerializedObject(renderer);
            var serializedFeatures = serializedRenderer.FindProperty("m_RendererFeatures");
            var serializedFeatureMap = serializedRenderer.FindProperty("m_RendererFeatureMap");
            if (serializedFeatures == null || serializedFeatureMap == null || localId == 0)
            {
                UnityEngine.Object.DestroyImmediate(feature, true);
                throw new InvalidOperationException(
                    "Unity could not serialize the ambient-occlusion renderer feature safely.");
            }

            var featureIndex = serializedFeatures.arraySize;
            serializedFeatures.InsertArrayElementAtIndex(featureIndex);
            serializedFeatures.GetArrayElementAtIndex(featureIndex).objectReferenceValue = feature;
            serializedFeatureMap.InsertArrayElementAtIndex(featureIndex);
            serializedFeatureMap.GetArrayElementAtIndex(featureIndex).longValue = localId;
            serializedRenderer.ApplyModifiedPropertiesWithoutUndo();
            return feature;
        }

        private static void ConfigureFeature(ScreenSpaceAmbientOcclusion feature)
        {
            var serialized = new SerializedObject(feature);
            var settings = serialized.FindProperty("m_Settings");
            if (settings == null)
            {
                throw new InvalidOperationException(
                    "Unity's ambient-occlusion settings could not be found.");
            }

            SetInteger(settings, "AOMethod", 1); // Interleaved Gradient: stable and inexpensive.
            SetBoolean(settings, "Downsample", true);
            SetBoolean(settings, "AfterOpaque", false);
            SetInteger(settings, "Source", 1); // Depth Normals.
            SetInteger(settings, "NormalSamples", 1);
            SetFloat(settings, "Intensity", 1.35f);
            SetFloat(settings, "DirectLightingStrength", 0.28f);
            SetFloat(settings, "Radius", 0.12f);
            SetInteger(settings, "Samples", 1); // Medium, eight samples.
            SetInteger(settings, "BlurQuality", 0); // High-quality bilateral blur.
            SetFloat(settings, "Falloff", 75f);
            SetInteger(settings, "SampleCount", -1);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBoolean(SerializedProperty settings, string name, bool value)
        {
            Require(settings, name).boolValue = value;
        }

        private static void SetInteger(SerializedProperty settings, string name, int value)
        {
            Require(settings, name).intValue = value;
        }

        private static void SetFloat(SerializedProperty settings, string name, float value)
        {
            Require(settings, name).floatValue = value;
        }

        private static SerializedProperty Require(SerializedProperty settings, string name)
        {
            var property = settings.FindPropertyRelative(name);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"Unity's ambient-occlusion setting '{name}' could not be found.");
            }
            return property;
        }
    }
}
