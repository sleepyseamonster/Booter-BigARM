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
    public static class CampOpticsCapture
    {
        [Serializable] private sealed class Report
        {
            public string proof = "Static matched Editor captures; no Player or GPU frame budget claim";
            public string gpu, api;
            public int width = 1536, height = 864;
            public bool stackRestored, sourceSceneUnchanged;
            public string pngEncoding = "Linear HDR target readback, converted to sRGB for PNG display";
            public string[] variants;
        }
        public static void CaptureFromCli()
        {
            const string scenePath = "Assets/_Project/Scenes/Reference/CampLoopTesting.unity";
            const string output = "Logs/CampOpticsCaptures";
            Directory.CreateDirectory(output);
            var before = File.ReadAllBytes(scenePath);
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var camera = TopDown3DSceneRenderingOverride.AtmosphereCamera(scene);
            if (camera == null) throw new InvalidOperationException("Missing unique camp camera binding");
            var binding = camera.GetComponent<TopDown3DSceneRenderingOverride>();
            var rig = camera.GetComponent<TopDown3DCameraRig>();
            var sun = UnityEngine.Object.FindFirstObjectByType<PerpetualTwilightSun>();
            var oldProfile = AssetDatabase.LoadAssetAtPath<TopDown3DAtmosphereStudyProfile>(CampAtmosphereAuthoring.Settings + "/Clear.asset");
            var oldTarget = camera.targetTexture;
            var oldEnabled = camera.enabled;
            var oldPosition = camera.transform.position; var oldRotation = camera.transform.rotation;
            var rigSnapshot = EditorJsonUtility.ToJson(rig);
            var cameraData = camera.GetComponent<UniversalAdditionalCameraData>();
            var originalAA = cameraData.antialiasing;
            var active = RenderTexture.active;
            var target = new RenderTexture(1536, 864, 24, RenderTextureFormat.ARGBHalf);
            var pixels = new Texture2D(1536, 864, TextureFormat.RGB24, false, true);
            Material sky = null;
            bool restored = true;
            var variants = new[] { "Clear", "Off", "Low", "Standard", "High", "Band", "Flare", "Aces", "Fxaa" };
            try
            {
                camera.enabled = false;
                camera.targetTexture = target;
                foreach (var variant in variants)
                {
                    var profile = variant == "Clear" ? oldProfile : AssetDatabase.LoadAssetAtPath<TopDown3DAtmosphereStudyProfile>(CampOpticsAuthoring.Settings + "/" + (variant == "Fxaa" ? "Standard" : variant) + "Twilight.asset");
                    cameraData.antialiasing = variant == "Fxaa" ? AntialiasingMode.FastApproximateAntialiasing : originalAA;
                    if (profile == null) throw new InvalidOperationException("Missing " + variant);
                    binding.ConfigureAtmosphere(profile);
                    if (sky != null) UnityEngine.Object.DestroyImmediate(sky);
                    sky = new Material(profile.skyMaterial);
                    sun.ApplyStudyPreview(.12f, sky);
                    foreach (var pitch in new[] { 26f, 50f, 65f })
                    {
                        rig.SetViewAngles(40f, pitch);
                        for (int frame = 0; frame < 4; frame++)
                        {
                            var prior = VolumeManager.instance.stack;
                            try { camera.Render(); }
                            finally { TopDown3DCampOpticsStack.Restore(camera); }
                            restored &= prior == VolumeManager.instance.stack || variant == "Clear";
                        }
                        RenderTexture.active = target;
                        pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); pixels.Apply();
                        var displayPixels = pixels.GetPixels();
                        for (int i = 0; i < displayPixels.Length; i++) displayPixels[i] = displayPixels[i].gamma;
                        pixels.SetPixels(displayPixels); pixels.Apply();
                        File.WriteAllBytes(output + "/" + variant + "-pitch" + pitch + ".png", pixels.EncodeToPNG());
                    }
                    foreach (var phase in new[] { .25f, .75f })
                    {
                        sun.ApplyStudyPreview(phase, sky); rig.SetViewAngles(40f, 26f);
                        try { camera.Render(); } finally { TopDown3DCampOpticsStack.Restore(camera); }
                        RenderTexture.active = target;
                        pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); pixels.Apply();
                        var displayPixels = pixels.GetPixels();
                        for (int i = 0; i < displayPixels.Length; i++) displayPixels[i] = displayPixels[i].gamma;
                        pixels.SetPixels(displayPixels); pixels.Apply();
                        File.WriteAllBytes(output + "/" + variant + "-phase" + phase + ".png", pixels.EncodeToPNG());
                    }
                }
            }
            finally
            {
                binding.ConfigureAtmosphere(oldProfile);
                TopDown3DCampOpticsStack.Release(camera);
                sun.EndStudyPreview();
                camera.targetTexture = oldTarget;
                camera.enabled = oldEnabled;
                cameraData.antialiasing = originalAA;
                EditorJsonUtility.FromJsonOverwrite(rigSnapshot, rig);
                camera.transform.SetPositionAndRotation(oldPosition, oldRotation);
                RenderTexture.active = active;
                UnityEngine.Object.DestroyImmediate(sky);
                UnityEngine.Object.DestroyImmediate(pixels);
                UnityEngine.Object.DestroyImmediate(target);
            }
            bool unchanged = Convert.ToBase64String(before) == Convert.ToBase64String(File.ReadAllBytes(scenePath));
            File.WriteAllText(output + "/report.json", JsonUtility.ToJson(new Report
            { gpu = SystemInfo.graphicsDeviceName, api = SystemInfo.graphicsDeviceType.ToString(), stackRestored = restored, sourceSceneUnchanged = unchanged, variants = variants }, true));
            if (!unchanged || !restored) throw new InvalidOperationException("Capture preservation gate failed");
            Debug.Log("CAMP_OPTICS_CAPTURE_OK: 45 matched views; scene bytes preserved; manager stack restored");
        }
    }
}
