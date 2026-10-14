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
using Object = UnityEngine.Object;

namespace BooterBigArm.Editor
{
    public static class CampOpticsLensFixture
    {
        private const int Size = 512;
        private const string Output = "Logs/CampOpticsLensFixture";

        [Serializable] private sealed class Difference
        {
            public string comparison;
            public float meanAbsoluteRgb, maxAbsoluteRgb;
            public int changedPixels;
        }

        [Serializable] private sealed class Report
        {
            public string proof = "Actual URP camera readbacks; combined Gaussian/flare/native bloom/dirt, lens invariance and opaque solar visibility";
            public string limitations = "No GPU timing, continuous-motion/translucent occlusion, native data-driven flare, or gameplay acceptance proof";
            public string gpu, api, failure;
            public int width = Size, height = Size;
            public string dirtAsset = CampOpticsAuthoring.Textures + "/LensDustFine.png";
            public bool linearDirtImport, initializedAtEntry, stackRestored = true;
            public bool sunRestored, targetsRestored, activeSceneRestored;
            public bool dirtRespondsToBloom, dirtAbsentWithoutBloom, clearSunProducesFlare, blockedSunHasNoFlare;
            public bool combinedDofActive, combinedDirtActive, combinedFlareActive, lensUnaffectedByDof, combinedBlockedSun;
            public bool combinedPartialSun, combinedOffscreenSun;
            public Difference[] comparisons;
        }

        public static void RunFromCli()
        {
            Directory.CreateDirectory(Output);
            var report = new Report { gpu = SystemInfo.graphicsDeviceName, api = SystemInfo.graphicsDeviceType.ToString(),
                initializedAtEntry = VolumeManager.instance.isInitialized };
            var previousTarget = RenderTexture.active;
            var previousSun = RenderSettings.sun;
            var previousScene = SceneManager.GetActiveScene();
            var previousStack = VolumeManager.instance.stack;
            var temporary = new List<Object>();
            var differences = new List<Difference>();
            Scene scene = default;
            Camera camera = null;
            RenderTexture target = null;
            TopDown3DAtmosphereStudyProfile profile = null;
            Exception failure = null;
            try
            {
                if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                    throw new InvalidOperationException("Lens fixture requires a graphics-enabled runner.");
                var baseline = AssetDatabase.LoadAssetAtPath<TopDown3DAtmosphereStudyProfile>(CampAtmosphereAuthoring.Settings + "/Clear.asset");
                var dirt = AssetDatabase.LoadAssetAtPath<Texture2D>(report.dirtAsset);
                var importer = AssetImporter.GetAtPath(report.dirtAsset) as TextureImporter;
                report.linearDirtImport = importer != null && !importer.sRGBTexture;
                if (baseline == null || !baseline.IsValid || dirt == null || !report.linearDirtImport)
                    throw new InvalidOperationException("Requires valid Clear sky and imported linear LensDustFine runtime asset.");
                var unlit = Shader.Find("Universal Render Pipeline/Unlit");
                if (unlit == null || !unlit.isSupported) throw new InvalidOperationException("URP unlit fixture shader unavailable.");
                scene = EditorSceneManager.NewPreviewScene();
                var owner = NewObject("Lens optical fixture camera", scene);
                camera = owner.AddComponent<Camera>();
                camera.enabled = false;
                camera.scene = scene;
                camera.aspect = 1f;
                camera.fieldOfView = 60f;
                camera.nearClipPlane = .1f;
                camera.farClipPlane = 50f;
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
                optics.postExposure = 0f;
                optics.tonemapping = TonemappingMode.Neutral;
                optics.depthMode = DepthOfFieldMode.Off;
                optics.followPlayerFocus = false;
                optics.bloomIntensity = .45f;
                optics.bloomThreshold = .8f;
                optics.bloomScatter = .85f;
                optics.highQualityBloom = true;
                optics.dirtTexture = dirt;
                profile = ScriptableObject.CreateInstance<TopDown3DAtmosphereStudyProfile>();
                temporary.Add(profile);
                profile.skyMaterial = baseline.skyMaterial;
                profile.nativeOptics = optics;
                profile.tiltShiftEnabled = false;
                profile.streakEnabled = false;
                profile.bloomEnabled = false;
                profile.flareEnabled = false;
                profile.flareIntensity = .05f;
                owner.AddComponent<TopDown3DSceneRenderingOverride>().ConfigureAtmosphere(profile);
                target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGBHalf)
                    { hideFlags = HideFlags.HideAndDontSave, antiAliasing = 1 };
                target.Create();
                temporary.Add(target);
                camera.targetTexture = target;
                var readback = new Texture2D(Size, Size, TextureFormat.RGBAFloat, false, true);
                temporary.Add(readback);
                var png = new Texture2D(Size, Size, TextureFormat.RGB24, false, true);
                temporary.Add(png);
                var emitterMaterial = new Material(unlit) { hideFlags = HideFlags.HideAndDontSave };
                temporary.Add(emitterMaterial);
                emitterMaterial.SetColor("_BaseColor", new Color(12f, 9f, 6f, 1f));
                var emitters = new List<GameObject>();
                foreach (var uv in new[] { new Vector2(.16f,.24f), new Vector2(.84f,.24f),
                    new Vector2(.16f,.76f), new Vector2(.84f,.76f) })
                {
                    var emitter = Primitive(PrimitiveType.Quad, "HDR peripheral emitter", scene, emitterMaterial);
                    emitter.transform.position = camera.ViewportToWorldPoint(new Vector3(uv.x, uv.y, 5f));
                    emitter.transform.localScale = Vector3.one * .24f;
                    emitters.Add(emitter);
                }

                Color[] Capture(string name, int renderCount = 3)
                {
                    for (int i = 0; i < renderCount; i++)
                    {
                        var stack = VolumeManager.instance.stack;
                        var wasInitialized = VolumeManager.instance.isInitialized;
                        try { camera.Render(); }
                        finally { TopDown3DCampOpticsStack.Restore(camera); }
                        report.stackRestored &= !wasInitialized || VolumeManager.instance.stack == stack;
                    }
                    RenderTexture.active = target;
                    readback.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
                    readback.Apply();
                    var pixels = readback.GetPixels();
                    png.SetPixels(pixels); png.Apply();
                    File.WriteAllBytes(Path.Combine(Output, name + ".png"), png.EncodeToPNG());
                    return pixels;
                }

                optics.bloomEnabled = true; optics.dirtIntensity = 0f;
                var bloomClean = Capture("Bloom-on-dirt-zero");
                optics.dirtIntensity = 3f;
                var bloomDirty = Capture("Bloom-on-dirt-three");
                var litDifference = Compare("Bloom enabled: dirt 0 versus 3", bloomClean, bloomDirty);
                differences.Add(litDifference);
                report.dirtRespondsToBloom = litDifference.meanAbsoluteRgb > .00001f && litDifference.changedPixels > 32;
                optics.bloomEnabled = false; optics.dirtIntensity = 0f;
                var noBloomClean = Capture("Bloom-off-dirt-zero");
                optics.dirtIntensity = 3f;
                var noBloomDirty = Capture("Bloom-off-dirt-three");
                var darkDifference = Compare("Bloom disabled: dirt 0 versus 3", noBloomClean, noBloomDirty);
                differences.Add(darkDifference);
                report.dirtAbsentWithoutBloom = darkDifference.maxAbsoluteRgb <= .0001f;

                foreach (var emitter in emitters) emitter.SetActive(false);
                optics.dirtIntensity = 0f;
                var light = NewObject("Authoritative fixture directional sun", scene).AddComponent<Light>();
                light.type = LightType.Directional;
                light.color = Color.yellow;
                light.intensity = 1f;
                light.transform.rotation = Quaternion.LookRotation(-camera.transform.forward, camera.transform.up);
                RenderSettings.sun = light;
                profile.flareEnabled = false;
                var clearOff = Capture("Sun-clear-flare-off");
                profile.flareEnabled = true;
                var clearOn = Capture("Sun-clear-flare-on");
                var clearDifference = Compare("Clear directional sun: flare off versus on", clearOff, clearOn);
                differences.Add(clearDifference);
                report.clearSunProducesFlare = clearDifference.meanAbsoluteRgb > .00001f && clearDifference.changedPixels > 32;
                var black = new Material(unlit) { hideFlags = HideFlags.HideAndDontSave };
                temporary.Add(black); black.SetColor("_BaseColor", Color.black);
                var blocker = Primitive(PrimitiveType.Cube, "Opaque sun blocker", scene, black);
                blocker.transform.position = camera.transform.position + camera.transform.forward * 4f;
                blocker.transform.localScale = Vector3.one * 1.5f;
                // No intervening disabled render: this tests immediate removal of a visible
                // source's existing history on the first fully blocked frame.
                var blockedOn = Capture("Sun-blocked-first-frame-flare-on", 1);
                profile.flareEnabled = false;
                var blockedOff = Capture("Sun-blocked-flare-off", 1);
                var blockedDifference = Compare("Fully blocked sun: first enabled frame versus disabled", blockedOn, blockedOff);
                differences.Add(blockedDifference);
                report.blockedSunHasNoFlare = blockedDifference.maxAbsoluteRgb <= .0001f;

                blocker.SetActive(false);
                // Prove the lens is downstream of world DOF on a black scene, then
                // exercise all producers together at the selected bloom/dirt strengths.
                optics.depthMode = DepthOfFieldMode.Gaussian; optics.gaussianBeforeLens = true;
                optics.farStart = 2f; optics.farEnd = 4f; optics.gaussianRadius = .6f;
                optics.bloomEnabled = true; optics.bloomIntensity = .18f; optics.bloomThreshold = 1f;
                optics.bloomScatter = .65f; optics.dirtIntensity = .3f;
                profile.flareEnabled = true;
                var lensWithDof = Capture("Combined-lens-world-DOF-on");
                optics.depthMode = DepthOfFieldMode.Off;
                var lensWithoutDof = Capture("Combined-lens-world-DOF-off");
                var invariant = Compare("Lens artifacts: world DOF on versus off, black scene", lensWithDof, lensWithoutDof);
                differences.Add(invariant); report.lensUnaffectedByDof = invariant.maxAbsoluteRgb <= .0001f;
                foreach (var emitter in emitters) emitter.SetActive(true);
                optics.depthMode = DepthOfFieldMode.Gaussian;
                var combined = Capture("Combined-all-effects");
                optics.depthMode = DepthOfFieldMode.Off;
                var combinedNoDof = Capture("Combined-without-world-DOF");
                var dofDifference = Compare("Combined: Gaussian versus no world DOF", combined, combinedNoDof);
                differences.Add(dofDifference); report.combinedDofActive = dofDifference.maxAbsoluteRgb > .0001f;
                optics.depthMode = DepthOfFieldMode.Gaussian; optics.dirtIntensity = 0f;
                var combinedNoDirt = Capture("Combined-without-lens-dust");
                var dirtDifference = Compare("Combined: dirt 0.3 versus zero", combined, combinedNoDirt);
                differences.Add(dirtDifference); report.combinedDirtActive = dirtDifference.maxAbsoluteRgb > .0001f;
                optics.dirtIntensity = .3f; profile.flareEnabled = false;
                var combinedNoFlare = Capture("Combined-without-lens-flare");
                var flareDifference = Compare("Combined: lens flare on versus off", combined, combinedNoFlare);
                differences.Add(flareDifference); report.combinedFlareActive = flareDifference.maxAbsoluteRgb > .0001f;
                profile.flareEnabled = true;
                Capture("Combined-visible-history");
                blocker.SetActive(true);
                var combinedBlocked = Capture("Combined-blocked-first-frame", 1);
                profile.flareEnabled = false;
                var combinedBlockedOff = Capture("Combined-blocked-flare-off");
                var combinedBlockedDifference = Compare("Combined: opaque blocking first frame versus flare off", combinedBlocked, combinedBlockedOff);
                differences.Add(combinedBlockedDifference); report.combinedBlockedSun = combinedBlockedDifference.maxAbsoluteRgb <= .0001f;
                blocker.transform.position += camera.transform.right * .75f;
                var partialOff = Capture("Combined-partial-flare-off");
                profile.flareEnabled = true;
                var partialOn = Capture("Combined-partial-flare-on");
                var partialDifference = Compare("Combined: half-disc opaque occlusion", partialOn, partialOff);
                differences.Add(partialDifference);
                report.combinedPartialSun = partialDifference.maxAbsoluteRgb > .0001f
                    && partialDifference.meanAbsoluteRgb < flareDifference.meanAbsoluteRgb;
                light.transform.rotation = Quaternion.LookRotation(camera.transform.forward, camera.transform.up);
                var behindOn = Capture("Combined-behind-camera-flare-on", 1);
                profile.flareEnabled = false;
                var behindOff = Capture("Combined-behind-camera-flare-off");
                var behindDifference = Compare("Combined: behind-camera source versus flare off", behindOn, behindOff);
                differences.Add(behindDifference); report.combinedOffscreenSun = behindDifference.maxAbsoluteRgb <= .0001f;
            }
            catch (Exception exception) { failure = exception; report.failure = exception.ToString(); }
            finally
            {
                if (camera != null)
                {
                    // A failed mid-sequence run must also discard this camera's custom flare
                    // history through the feature's normal disabled-camera path.
                    if (profile != null && camera.targetTexture != null && VolumeManager.instance.isInitialized)
                    {
                        profile.flareEnabled = false;
                        var cleanupStack = VolumeManager.instance.stack;
                        try { camera.Render(); }
                        catch (Exception exception)
                        {
                            if (failure == null) { failure = exception; report.failure = exception.ToString(); }
                        }
                        finally { TopDown3DCampOpticsStack.Restore(camera); }
                        report.stackRestored &= VolumeManager.instance.stack == cleanupStack;
                    }
                    TopDown3DCampOpticsStack.Release(camera);
                    camera.targetTexture = null;
                }
                RenderSettings.sun = previousSun;
                RenderTexture.active = previousTarget;
                report.sunRestored = RenderSettings.sun == previousSun;
                report.targetsRestored = RenderTexture.active == previousTarget;
                if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
                if (target != null) target.Release();
                for (int i = temporary.Count - 1; i >= 0; i--)
                    if (temporary[i] != null) Object.DestroyImmediate(temporary[i]);
                report.activeSceneRestored = SceneManager.GetActiveScene() == previousScene;
                report.stackRestored &= !report.initializedAtEntry || VolumeManager.instance.stack == previousStack;
                report.comparisons = differences.ToArray();
                File.WriteAllText(Path.Combine(Output, "report.json"), JsonUtility.ToJson(report, true));
            }
            if (failure != null) throw new InvalidOperationException("Lens fixture could not complete; inspect its report.", failure);
            if (!report.dirtRespondsToBloom || !report.dirtAbsentWithoutBloom || !report.clearSunProducesFlare
                || !report.combinedDofActive || !report.combinedDirtActive || !report.combinedFlareActive
                || !report.lensUnaffectedByDof || !report.combinedBlockedSun
                || !report.combinedPartialSun || !report.combinedOffscreenSun
                || !report.blockedSunHasNoFlare || !report.stackRestored || !report.sunRestored
                || !report.targetsRestored || !report.activeSceneRestored)
                throw new InvalidOperationException("Lens camera fixture gate failed; inspect Logs/CampOpticsLensFixture/report.json.");
            Debug.Log("CAMP_OPTICS_LENS_FIXTURE_OK: combined world DOF/flare/bloom/dust, partial and immediate opaque occlusion, restored state.");
        }

        private static GameObject NewObject(string name, Scene scene)
        {
            var result = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave };
            SceneManager.MoveGameObjectToScene(result, scene);
            return result;
        }

        private static GameObject Primitive(PrimitiveType type, string name, Scene scene, Material material)
        {
            var result = GameObject.CreatePrimitive(type);
            result.name = name;
            result.hideFlags = HideFlags.HideAndDontSave;
            SceneManager.MoveGameObjectToScene(result, scene);
            result.GetComponent<Renderer>().sharedMaterial = material;
            return result;
        }

        private static Difference Compare(string name, Color[] a, Color[] b)
        {
            double sum = 0;
            float maximum = 0f;
            int changed = 0;
            for (int i = 0; i < a.Length; i++)
            {
                float r = Mathf.Abs(a[i].r - b[i].r), g = Mathf.Abs(a[i].g - b[i].g), blue = Mathf.Abs(a[i].b - b[i].b);
                if (!float.IsFinite(r) || !float.IsFinite(g) || !float.IsFinite(blue))
                    throw new InvalidOperationException("Non-finite optical readback.");
                var largest = Mathf.Max(r, Mathf.Max(g, blue));
                sum += r + g + blue;
                maximum = Mathf.Max(maximum, largest);
                if (largest > .0001f) changed++;
            }
            return new Difference { comparison = name, meanAbsoluteRgb = (float)(sum / (a.Length * 3)),
                maxAbsoluteRgb = maximum, changedPixels = changed };
        }
    }
}
