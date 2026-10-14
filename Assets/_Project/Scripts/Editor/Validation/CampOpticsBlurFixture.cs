using System;
using System.Collections.Generic;
using System.IO;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Editor
{
    // Actual URP camera rendering, including renderer features and the private camp stack.
    // Static readbacks establish filtering units/shape, never a GPU performance measurement.
    public static class CampOpticsBlurFixture
    {
        [Serializable] private sealed class Measurement
        {
            public string variant;
            public int downsample;
            public float energy, centerX, centerY, rmsX, rmsY, symmetryRatio;
        }
        [Serializable] private sealed class Report
        {
            public string proof = "Actual URP camera impulse readback; no gameplay or frame-budget claim";
            public string gpu, api;
            public int width = 512, height = 512;
            public float referenceHeight = 1080f, tapStepPixels = 7f;
            public bool stackRestored, targetsRestored, symmetryPassed, blurActive;
            public bool downsampleConsistencyPassed;
            public bool volumeManagerInitializedAtEntry;
            public Measurement[] measurements;
        }

        public static void RunFromCli()
        {
            const int size = 512;
            const string output = "Logs/CampOpticsBlurFixture";
            Directory.CreateDirectory(output);
            var baseline = AssetDatabase.LoadAssetAtPath<TopDown3DAtmosphereStudyProfile>(
                CampAtmosphereAuthoring.Settings + "/Clear.asset");
            if (baseline == null || !baseline.IsValid)
                throw new InvalidOperationException("Blur fixture requires the valid retained Clear atmosphere profile.");
            var unlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlit == null || !unlit.isSupported)
                throw new InvalidOperationException("URP unlit shader unavailable in rendering fixture.");

            var previousActive = RenderTexture.active;
            var initializedAtEntry = VolumeManager.instance.isInitialized;
            var originalStack = VolumeManager.instance.stack;
            var scene = EditorSceneManager.NewPreviewScene();
            var temporary = new List<UnityEngine.Object>();
            Camera camera = null;
            RenderTexture target = null;
            bool stackRestored = true, targetsRestored = false;
            var measurements = new List<Measurement>();
            try
            {
                var cameraObject = new GameObject("Blur impulse fixture camera") { hideFlags = HideFlags.HideAndDontSave };
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                camera.scene = scene;
                camera.orthographic = true;
                camera.orthographicSize = 1f;
                camera.aspect = 1f;
                camera.nearClipPlane = .1f;
                camera.farClipPlane = 10f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.allowHDR = true;
                camera.allowMSAA = false;
                var data = camera.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = true;
                data.antialiasing = AntialiasingMode.None;
                data.dithering = false;
                data.volumeLayerMask = 0;

                var optics = ScriptableObject.CreateInstance<TopDown3DCampOpticsProfile>();
                temporary.Add(optics);
                optics.bloomEnabled = false;
                optics.depthMode = DepthOfFieldMode.Off;
                optics.tonemapping = TonemappingMode.None;
                optics.dirtIntensity = 0f;
                var profile = ScriptableObject.CreateInstance<TopDown3DAtmosphereStudyProfile>();
                temporary.Add(profile);
                profile.skyMaterial = baseline.skyMaterial;
                profile.nativeOptics = optics;
                profile.bloomEnabled = false;
                profile.flareEnabled = false;
                profile.streakEnabled = false;
                profile.useReferenceResolutionTiltShift = true;
                profile.tiltShiftReferenceHeight = 1080f;
                profile.tiltShiftBlurRadiusPixels = 7f;
                profile.tiltShiftFocusCenter = .5f;
                profile.tiltShiftSharpBandWidth = .1f;
                profile.tiltShiftFeatherWidth = .01f;
                var binding = cameraObject.AddComponent<TopDown3DSceneRenderingOverride>();
                binding.ConfigureAtmosphere(profile);

                var impulse = GameObject.CreatePrimitive(PrimitiveType.Quad);
                impulse.name = "Four source pixel square impulse";
                impulse.hideFlags = HideFlags.HideAndDontSave;
                SceneManager.MoveGameObjectToScene(impulse, scene);
                impulse.transform.position = new Vector3(0f, .6f, 5f); // viewport row .8, full blur mask
                impulse.transform.localScale = Vector3.one * (8f / size);
                var impulseMaterial = new Material(unlit) { hideFlags = HideFlags.HideAndDontSave };
                temporary.Add(impulseMaterial);
                impulseMaterial.SetColor("_BaseColor", Color.white);
                impulse.GetComponent<Renderer>().sharedMaterial = impulseMaterial;

                target = new RenderTexture(size, size, 24, RenderTextureFormat.ARGBHalf)
                { hideFlags = HideFlags.HideAndDontSave, antiAliasing = 1 };
                temporary.Add(target);
                target.Create();
                camera.targetTexture = target;
                foreach (var downsample in new[] { 0, 2, 4 })
                {
                    profile.tiltShiftEnabled = downsample != 0;
                    profile.tiltShiftDownsample = Mathf.Max(1, downsample);
                    for (var warmup = 0; warmup < 3; warmup++)
                    {
                        var stack = VolumeManager.instance.stack;
                        var initializedBeforeRender = VolumeManager.instance.isInitialized;
                        try { camera.Render(); }
                        finally { TopDown3DCampOpticsStack.Restore(camera); }
                        stackRestored &= !initializedBeforeRender || stack == VolumeManager.instance.stack;
                    }
                    var pixels = new Texture2D(size, size, TextureFormat.RGBAFloat, false, true);
                    temporary.Add(pixels);
                    RenderTexture.active = target;
                    pixels.ReadPixels(new Rect(0, 0, size, size), 0, 0);
                    pixels.Apply();
                    var values = pixels.GetPixels();
                    var name = downsample == 0 ? "Off" : "Band-downsample" + downsample;
                    measurements.Add(Measure(values, size, name, downsample));
                    var png = new Texture2D(size, size, TextureFormat.RGB24, false, true);
                    temporary.Add(png);
                    png.SetPixels(values); png.Apply();
                    File.WriteAllBytes(Path.Combine(output, name + ".png"), png.EncodeToPNG());
                }
            }
            finally
            {
                if (camera != null)
                {
                    TopDown3DCampOpticsStack.Release(camera);
                    camera.targetTexture = null;
                }
                RenderTexture.active = previousActive;
                targetsRestored = RenderTexture.active == previousActive;
                EditorSceneManager.ClosePreviewScene(scene);
                if (target != null) target.Release();
                for (var i = temporary.Count - 1; i >= 0; i--)
                    if (temporary[i] != null) UnityEngine.Object.DestroyImmediate(temporary[i]);
            }
            stackRestored &= !initializedAtEntry || originalStack == VolumeManager.instance.stack;
            bool symmetry = measurements.Count == 3, active = measurements.Count == 3;
            foreach (var measure in measurements)
            {
                if (measure.downsample == 0) continue;
                symmetry &= measure.symmetryRatio >= .75f && measure.symmetryRatio <= 1.33334f;
                active &= measure.rmsX > measurements[0].rmsX * 1.5f
                    && measure.rmsY > measurements[0].rmsY * 1.5f;
            }
            bool downsampleConsistent = measurements.Count == 3
                && measurements[2].rmsX / Mathf.Max(.00001f, measurements[1].rmsX) >= .75f
                && measurements[2].rmsX / Mathf.Max(.00001f, measurements[1].rmsX) <= 1.33334f
                && measurements[2].rmsY / Mathf.Max(.00001f, measurements[1].rmsY) >= .75f
                && measurements[2].rmsY / Mathf.Max(.00001f, measurements[1].rmsY) <= 1.33334f;
            var report = new Report
            {
                gpu = SystemInfo.graphicsDeviceName, api = SystemInfo.graphicsDeviceType.ToString(),
                stackRestored = stackRestored, targetsRestored = targetsRestored,
                symmetryPassed = symmetry, blurActive = active, measurements = measurements.ToArray(),
                volumeManagerInitializedAtEntry = initializedAtEntry,
                downsampleConsistencyPassed = downsampleConsistent,
            };
            File.WriteAllText(Path.Combine(output, "report.json"), JsonUtility.ToJson(report, true));
            if (!symmetry || !active || !downsampleConsistent || !stackRestored || !targetsRestored)
                throw new InvalidOperationException("Blur camera fixture gate failed; inspect Logs/CampOpticsBlurFixture/report.json.");
            Debug.Log("CAMP_OPTICS_BLUR_FIXTURE_OK: corrected axis symmetry, active blur and restored state.");
        }

        private static Measurement Measure(Color[] pixels, int size, string variant, int downsample)
        {
            double energy = 0, xSum = 0, ySum = 0;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                double weight = Mathf.Max(0f, pixels[y * size + x].r);
                energy += weight; xSum += weight * x; ySum += weight * y;
            }
            if (energy < .0001) throw new InvalidOperationException("Empty camera impulse: actual renderer did not draw fixture.");
            double centerX = xSum / energy, centerY = ySum / energy, xx = 0, yy = 0;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                double weight = Mathf.Max(0f, pixels[y * size + x].r);
                xx += weight * (x - centerX) * (x - centerX);
                yy += weight * (y - centerY) * (y - centerY);
            }
            var rmsX = (float)Math.Sqrt(xx / energy);
            var rmsY = (float)Math.Sqrt(yy / energy);
            return new Measurement { variant = variant, downsample = downsample, energy = (float)energy,
                centerX = (float)centerX, centerY = (float)centerY, rmsX = rmsX, rmsY = rmsY,
                symmetryRatio = rmsX / Mathf.Max(.00001f, rmsY) };
        }
    }
}
