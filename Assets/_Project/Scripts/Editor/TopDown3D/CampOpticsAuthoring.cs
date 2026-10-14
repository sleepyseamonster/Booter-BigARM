using System;
using System.IO;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BooterBigArm.Editor
{
    public static class CampOpticsAuthoring
    {
        public const string Settings = "Assets/_Project/Settings/Studies/CampOptics20261014";
        public const string Textures = "Assets/_Project/VFX/Textures/CampOptics";
        public static void CreateFromCli()
        {
            Directory.CreateDirectory(Settings);
            Directory.CreateDirectory(Textures);
            var fine = Derive("LensDustFine");
            var soft = Derive("LensDustSoft");
            foreach (var tier in new[] { "Low", "Standard", "High", "Band", "Flare", "Off" })
            {
                var path = Settings + "/" + tier + "Optics.asset";
                if (File.Exists(path)) throw new InvalidOperationException("Refusing to replace " + path);
                var optics = ScriptableObject.CreateInstance<TopDown3DCampOpticsProfile>();
                optics.name = tier;
                optics.postExposure = 1f;
                optics.opticsEnabled = tier != "Off";
                optics.highQualityBloom = tier != "Low";
                optics.dirtTexture = tier == "High" ? soft : fine;
                optics.dirtIntensity = tier == "Low" ? .15f : .3f;
                optics.depthMode = tier == "Standard" ? DepthOfFieldMode.Gaussian
                    : tier == "High" ? DepthOfFieldMode.Bokeh : DepthOfFieldMode.Off;
                optics.followPlayerFocus = tier == "Standard" || tier == "High";
                AssetDatabase.CreateAsset(optics, path);
                var clear = AssetDatabase.LoadAssetAtPath<TopDown3DAtmosphereStudyProfile>(CampAtmosphereAuthoring.Settings + "/Clear.asset");
                var atmosphere = UnityEngine.Object.Instantiate(clear);
                atmosphere.name = tier + "Twilight";
                atmosphere.nativeOptics = optics;
                atmosphere.bloomEnabled = false;
                atmosphere.flareEnabled = tier == "Flare";
                atmosphere.flareIntensity = .05f;
                atmosphere.streakEnabled = false;
                atmosphere.tiltShiftEnabled = tier == "Band";
                atmosphere.useReferenceResolutionTiltShift = tier == "Band";
                atmosphere.tiltShiftBlurRadiusPixels = 2f;
                atmosphere.tiltShiftSharpBandWidth = .3f;
                atmosphere.tiltShiftFeatherWidth = .3f;
                AssetDatabase.CreateAsset(atmosphere, Settings + "/" + tier + "Twilight.asset");
            }
            AssetDatabase.SaveAssets();
            Debug.Log("CAMP_OPTICS_AUTHORING_OK: six sibling profiles; active scene and Clear baseline untouched");
        }

        public static void TuneExposureFromCli()
        {
            foreach (var tier in new[] { "Low", "Standard", "High", "Band", "Flare", "Off" })
            {
                var optics = AssetDatabase.LoadAssetAtPath<TopDown3DCampOpticsProfile>(Settings + "/" + tier + "Optics.asset");
                if (optics == null) throw new InvalidOperationException("Missing " + tier);
                optics.postExposure = 1f; EditorUtility.SetDirty(optics);
                var clear = AssetDatabase.LoadAssetAtPath<TopDown3DAtmosphereStudyProfile>(CampAtmosphereAuthoring.Settings + "/Clear.asset");
                optics.sunAngularDiameterDegrees = clear.skyMaterial.GetFloat("_SunAngularRadius") * 2f;
            }
            if (AssetDatabase.LoadAssetAtPath<TopDown3DCampOpticsProfile>(Settings + "/AcesOptics.asset") == null)
            {
                var optics = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<TopDown3DCampOpticsProfile>(Settings + "/StandardOptics.asset"));
                optics.name = "Aces"; optics.tonemapping = TonemappingMode.ACES;
                AssetDatabase.CreateAsset(optics, Settings + "/AcesOptics.asset");
                var atmosphere = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<TopDown3DAtmosphereStudyProfile>(Settings + "/StandardTwilight.asset"));
                atmosphere.nativeOptics = optics;
                AssetDatabase.CreateAsset(atmosphere, Settings + "/AcesTwilight.asset");
            }
            else
            {
                var aces = AssetDatabase.LoadAssetAtPath<TopDown3DCampOpticsProfile>(Settings + "/AcesOptics.asset");
                var standard = AssetDatabase.LoadAssetAtPath<TopDown3DCampOpticsProfile>(Settings + "/StandardOptics.asset");
                EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(standard), aces);
                aces.name = "Aces"; aces.tonemapping = TonemappingMode.ACES; EditorUtility.SetDirty(aces);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("CAMP_OPTICS_EXPOSURE_COMPARISON_READY: +1 EV fixed post exposure, Neutral versus ACES");
        }

        public static void ValidateFromCli()
        {
            foreach (var tier in new[] { "Low", "Standard", "High", "Band", "Flare", "Off", "Aces", "Combined" })
            {
                var atmosphere = AssetDatabase.LoadAssetAtPath<TopDown3DAtmosphereStudyProfile>(Settings + "/" + tier + "Twilight.asset");
                if (atmosphere == null || !atmosphere.IsValid || atmosphere.nativeOptics == null || !atmosphere.nativeOptics.IsValid)
                    throw new InvalidOperationException("Invalid optics selection " + tier);
                if (atmosphere.bloomEnabled || atmosphere.streakEnabled) throw new InvalidOperationException("Duplicate bloom/streak producer " + tier);
                if (atmosphere.tiltShiftEnabled && atmosphere.nativeOptics.depthMode != DepthOfFieldMode.Off)
                    throw new InvalidOperationException("Stacked world blur " + tier);
                if (atmosphere.flareEnabled && atmosphere.nativeOptics.depthMode != DepthOfFieldMode.Off
                    && !atmosphere.nativeOptics.gaussianBeforeLens)
                    throw new InvalidOperationException("Custom flare would enter native DOF " + tier);
                var texture = atmosphere.nativeOptics.dirtTexture;
                var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
                if (importer == null || importer.sRGBTexture || texture.width != 1024 || texture.height != 1024)
                    throw new InvalidOperationException("Invalid lens data texture " + tier);
            }
            var clear = AssetDatabase.LoadAssetAtPath<TopDown3DAtmosphereStudyProfile>(CampAtmosphereAuthoring.Settings + "/Clear.asset");
            if (clear.nativeOptics != null || clear.tiltShiftEnabled) throw new InvalidOperationException("Clear baseline changed");
            foreach (var name in new[] { "BooterBigArm/TopDown3D/Broken World Tilt Shift", "BooterBigArm/TopDown3D/Broken World Round Lens Flare" })
            {
                var shader = Shader.Find(name);
                if (shader == null) throw new InvalidOperationException("Missing " + name);
                foreach (var message in ShaderUtil.GetShaderMessages(shader))
                    if (message.severity.ToString() == "Error") throw new InvalidOperationException(message.message);
            }
            Debug.Log("CAMP_OPTICS_VALIDATION_OK: eight candidate selections, linear masks, blur exclusivity, baseline and shader diagnostics");
        }

        public static void ValidateCameraPrerequisites(Camera camera, UniversalRendererData renderer)
        {
            var data = camera != null ? camera.GetComponent<UniversalAdditionalCameraData>() : null;
            if (data == null || data.renderType != CameraRenderType.Base || !data.renderPostProcessing)
                throw new InvalidOperationException("Camp optics requires a base camera with URP postprocessing enabled.");
            if (renderer == null || !renderer.rendererFeatures.Exists(feature => feature is TopDown3DSunBloomFeature && feature.isActive))
                throw new InvalidOperationException("Selected camp renderer is missing the active Sun Bloom optics bridge.");
            if (!renderer.rendererFeatures.Exists(feature => feature is TopDown3DRoundLensFlareFeature && feature.isActive))
                throw new InvalidOperationException("Selected camp renderer is missing the active Round Lens Flare feature.");
            var shader = renderer.postProcessData != null ? renderer.postProcessData.shaders.gaussianDepthOfFieldPS : null;
            if (shader == null || !shader.isSupported)
                throw new InvalidOperationException("Selected camp renderer requires the supported stock Gaussian DOF shader.");
        }

        public static void ValidateCampFromCli()
        {
            ValidateFromCli();
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Reference/CampLoopTesting.unity");
            var camera = TopDown3DSceneRenderingOverride.AtmosphereCamera(scene);
            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (camera == null || pipeline == null) throw new InvalidOperationException("Missing camp camera or active URP pipeline.");
            var cameraSerialized = new SerializedObject(camera.GetUniversalAdditionalCameraData());
            int index = cameraSerialized.FindProperty("m_RendererIndex").intValue;
            var pipelineSerialized = new SerializedObject(pipeline);
            if (index < 0) index = pipelineSerialized.FindProperty("m_DefaultRendererIndex").intValue;
            var renderers = pipelineSerialized.FindProperty("m_RendererDataList");
            if (index >= renderers.arraySize) throw new InvalidOperationException("Camp renderer index is outside the selected pipeline.");
            ValidateCameraPrerequisites(camera, renderers.GetArrayElementAtIndex(index).objectReferenceValue as UniversalRendererData);
            if (!TopDown3DSceneRenderingOverride.TryGetAtmosphereProfile(camera, out var study)
                || study.nativeOptics == null || !study.nativeOptics.gaussianBeforeLens || !study.flareEnabled)
                throw new InvalidOperationException("Camp must select the combined world-blur/lens profile.");
            Debug.Log("CAMP_COMBINED_OPTICS_VALIDATION_OK: saved binding, renderer, postprocessing and stock shader prerequisites");
        }

        public static void ValidateRenderingFromCli()
        {
            ValidateCampFromCli();
            CampOpticsLensFixture.RunFromCli();
            CampOpticsCapture.CaptureFromCli();
        }

        private static Texture2D Derive(string name)
        {
            var sourcePath = "SourceArt/Rendering/CampOptics/" + name + "-master.png";
            var destination = Textures + "/" + name + ".png";
            if (File.Exists(destination)) throw new InvalidOperationException("Refusing to replace " + destination);
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            var derived = new Texture2D(1024, 1024, TextureFormat.RGBA32, false, true);
            try
            {
                if (!source.LoadImage(File.ReadAllBytes(sourcePath))) throw new InvalidDataException(sourcePath);
                var pixels = new Color[1024 * 1024];
                for (int y = 0; y < 1024; y++)
                    for (int x = 0; x < 1024; x++)
                    {
                        // Generated pixels are artist scattering data, not calibrated display radiance.
                        var c = source.GetPixelBilinear((x + .5f) / 1024f, (y + .5f) / 1024f);
                        var v = Mathf.Clamp01(c.r * .2126f + c.g * .7152f + c.b * .0722f);
                        pixels[y * 1024 + x] = new Color(v, v, v, 1f);
                    }
                derived.SetPixels(pixels); derived.Apply();
                File.WriteAllBytes(destination, derived.EncodeToPNG());
                Debug.Log($"CAMP_OPTICS_DERIVE {name}: {source.width}x{source.height} -> 1024x1024; numeric grayscale data");
            }
            finally { UnityEngine.Object.DestroyImmediate(source); UnityEngine.Object.DestroyImmediate(derived); }
            AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(destination);
            importer.sRGBTexture = false;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 1024;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(destination);
        }
    }
}
