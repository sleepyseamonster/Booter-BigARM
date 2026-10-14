using System;
using System.IO;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BooterBigArm.Editor
{
    public static class CampOpticsBuildHarness
    {
        public static void BuildFromCli()
        {
            // Guard against accidentally authoring/building in the production checkout.
            if (!Application.dataPath.Replace('\\', '/').Contains("BooterBigArmValidation/CampOptics"))
                throw new InvalidOperationException("Rendering harness requires the isolated CampOptics candidate");
            CampOpticsAuthoring.TuneExposureFromCli();
            CampOpticsCapture.CaptureFromCli();
            CampOpticsBlurFixture.RunFromCli();
            CampOpticsLensFixture.RunFromCli();
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Reference/CampLoopTesting.unity", OpenSceneMode.Single);
            var camera = TopDown3DSceneRenderingOverride.AtmosphereCamera(scene);
            if (camera == null) throw new InvalidOperationException("Missing camp camera");
            camera.GetComponent<TopDown3DCameraRig>().SetViewAngles(40f, 50f);
            var player = UnityEngine.Object.FindFirstObjectByType<TopDown3DPlayerMotor>();
            float frozenFocus = player != null ? Mathf.Max(.1f, TopDown3DCampOpticsStack.EyeDepth(camera, player.transform.position + Vector3.up * 1.15f)) : 20f;
            foreach (var root in scene.GetRootGameObjects())
            {
                var components = root.GetComponentsInChildren<MonoBehaviour>(true);
                for (int i = components.Length - 1; i >= 0; i--)
                    if (!(components[i] is TopDown3DSceneRenderingOverride) && !(components[i] is PerpetualTwilightSun)
                        && !(components[i] is UniversalAdditionalCameraData) && !(components[i] is UniversalAdditionalLightData)
                        && !(components[i] is Volume))
                        UnityEngine.Object.DestroyImmediate(components[i]);
                    else if (components[i] is PerpetualTwilightSun) components[i].enabled = false;
            }
            foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)) canvas.enabled = false;
            foreach (var animator in UnityEngine.Object.FindObjectsByType<Animator>(FindObjectsSortMode.None)) animator.enabled = false;
            foreach (var other in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) other.enabled = other == camera;
            var owner = new GameObject("Rendering-only optics fixture");
            var fixture = owner.AddComponent<TopDown3DCampOpticsPerformanceFixture>();
            fixture.outputCamera = camera;
            fixture.binding = camera.GetComponent<TopDown3DSceneRenderingOverride>();
            var names = Array.IndexOf(Environment.GetCommandLineArgs(), "-campCombinedQualification") >= 0
                ? new[] { "Off", "Combined" } : new[] { "Off", "Low", "Standard", "High", "Band", "Flare", "Combined" };
            fixture.profiles = new TopDown3DAtmosphereStudyProfile[names.Length];
            var focus = frozenFocus;
            for (int i = 0; i < names.Length; i++)
            {
                var profile = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<TopDown3DAtmosphereStudyProfile>(CampOpticsAuthoring.Settings + "/" + names[i] + "Twilight.asset"));
                var optics = UnityEngine.Object.Instantiate(profile.nativeOptics);
                optics.followPlayerFocus = false; optics.focusDistance = focus;
                optics.collectDiagnostics = true;
                optics.farStart = focus + optics.readableFarPadding; optics.farEnd = optics.farStart + optics.farBlurTransition;
                Directory.CreateDirectory("Assets/_Project/Tests/Fixtures/CampOptics");
                AssetDatabase.CreateAsset(optics, "Assets/_Project/Tests/Fixtures/CampOptics/" + names[i] + "FixtureOptics.asset");
                profile.nativeOptics = optics;
                AssetDatabase.CreateAsset(profile, "Assets/_Project/Tests/Fixtures/CampOptics/" + names[i] + "FixtureTwilight.asset");
                fixture.profiles[i] = profile;
            }
            fixture.emitters = new GameObject("Fixture emissive highlights");
            var hud = new GameObject("Optics fixture sharp overlay", typeof(Canvas));
            hud.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var marker = new GameObject("Checkerboard", typeof(RectTransform), typeof(RawImage));
            marker.transform.SetParent(hud.transform, false);
            var rect = marker.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(16f, 16f); rect.sizeDelta = new Vector2(64f, 64f);
            var checker = new Texture2D(64, 64, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++) checker.SetPixel(x, y, ((x / 8 + y / 8) % 2) == 0 ? Color.black : Color.white);
            checker.Apply();
            Directory.CreateDirectory("Assets/_Project/Tests/Fixtures/CampOptics");
            AssetDatabase.CreateAsset(checker, "Assets/_Project/Tests/Fixtures/CampOptics/Checker.asset");
            marker.GetComponent<RawImage>().texture = checker;
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", new Color(.2f, .1f, .03f));
            material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", new Color(12f, 5f, .5f));
            Directory.CreateDirectory("Assets/_Project/Tests/Fixtures/CampOptics");
            AssetDatabase.CreateAsset(material, "Assets/_Project/Tests/Fixtures/CampOptics/Emitters.mat");
            var center = camera.transform.position + camera.transform.forward * 20f;
            for (int i = 0; i < 24; i++)
            {
                var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.SetParent(fixture.emitters.transform);
                sphere.transform.position = center + camera.transform.right * ((i % 8 - 3.5f) * .8f) + camera.transform.up * ((i / 8 - 1) * .8f);
                sphere.transform.localScale = Vector3.one * .12f;
                sphere.GetComponent<Renderer>().sharedMaterial = material;
                UnityEngine.Object.DestroyImmediate(sphere.GetComponent<Collider>());
            }
            const string path = "Assets/_Project/Tests/Fixtures/CampOptics/Harness.unity";
            var clear = AssetDatabase.LoadAssetAtPath<TopDown3DAtmosphereStudyProfile>(CampAtmosphereAuthoring.Settings + "/Clear.asset");
            var sky = new Material(clear.skyMaterial);
            AssetDatabase.CreateAsset(sky, "Assets/_Project/Tests/Fixtures/CampOptics/FrozenSky.mat");
            UnityEngine.Object.FindFirstObjectByType<PerpetualTwilightSun>(FindObjectsInactive.Include).ApplyStudyPreview(.12f, sky);
            var cameraSky = camera.GetComponent<Skybox>();
            if (cameraSky == null) cameraSky = camera.gameObject.AddComponent<Skybox>();
            cameraSky.enabled = true; cameraSky.material = sky;
            RenderSettings.skybox = sky;
            RenderSettings.customReflectionTexture = clear.sunlessReflection;
            EditorUtility.SetDirty(sky); AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene, path);
            var output = "Builds/CampOpticsHarness/CampOpticsHarness.exe";
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var oldTiming = PlayerSettings.enableFrameTimingStats;
            try
            {
                PlayerSettings.enableFrameTimingStats = true;
                var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { path }, locationPathName = output,
                    target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
                if (result.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Harness build " + result.summary.result);
                Debug.Log("CAMP_OPTICS_HARNESS_BUILD_OK " + output);
            }
            finally { PlayerSettings.enableFrameTimingStats = oldTiming; }
        }
    }
}
