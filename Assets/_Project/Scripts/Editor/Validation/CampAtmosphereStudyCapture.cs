using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Debug = UnityEngine.Debug;

namespace BooterBigArm.Editor
{
    /// <summary>Static image and offscreen-render cost comparison. No simulation or gameplay replay.</summary>
    public static class CampAtmosphereStudyCapture
    {
        [Serializable] private sealed class Timing
        {
            public string mode;
            public double averageCpuRenderMs;
            public double maximumCpuRenderMs;
            public double averageBloomGpuMs;
            public bool bloomGpuSamplesAvailable;
            public int measuredFrames;
        }
        [Serializable] private sealed class Report
        {
            public string proof = "Static offscreen Editor camera render; excludes gameplay CPU, presentation and readback";
            public string gpu, graphics, editor;
            public bool supportsGpuRecorder;
            public int width = 1536, height = 864;
            public string[] captures;
            public Timing[] timings;
        }
        private static Camera camera;
        private static PerpetualTwilightSun sun;
        private static TopDown3DSceneRenderingOverride binding;
        private static TopDown3DAtmosphereStudyProfile selected;
        private static Material baselineSky, previewMaterial;
        private static RenderTexture target;
        private static byte[] sceneBefore;
        private static readonly List<string> captures = new List<string>();
        private static readonly List<Timing> timings = new List<Timing>();
        private static readonly List<double> cpu = new List<double>(), gpu = new List<double>();
        private static Recorder bloomRecorder;
        private static int mode, frame;
        private const string Output = "Logs/CampAtmosphere";

        public static void CaptureFromCli()
        {
            CampAtmosphereCapture.RequireCandidate();
            try
            {
                captures.Clear(); timings.Clear(); sceneBefore = File.ReadAllBytes(GreaterWastelandSceneScope.CampLoopScene);
                var scene = EditorSceneManager.OpenScene(GreaterWastelandSceneScope.CampLoopScene, OpenSceneMode.Single);
                camera = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Camera>(true)).Single();
                sun = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PerpetualTwilightSun>(true)).Single();
                Physics.SyncTransforms();
                binding = camera.GetComponent<TopDown3DSceneRenderingOverride>();
                if (binding == null) binding = camera.gameObject.AddComponent<TopDown3DSceneRenderingOverride>();
                if (!scene.GetRootGameObjects().Any(r => r.name.StartsWith("Camp Atmosphere Terrain Context")))
                    PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CampAtmosphereAuthoring.ContextPrefab), scene);
                camera.enabled = false; camera.cameraType = CameraType.Game; camera.scene = scene;
                camera.clearFlags = CameraClearFlags.Skybox;
                if (camera.GetComponent<Skybox>() == null) camera.gameObject.AddComponent<Skybox>();
                baselineSky = new Material(Resources.Load<Material>("TopDown3D/MartianPanoramaSky"));
                camera.GetComponent<Skybox>().material = baselineSky;
                target = new RenderTexture(1536, 864, 24); camera.targetTexture = target;
                Directory.CreateDirectory(Output);
                var rig = camera.GetComponent<TopDown3DCameraRig>();
                foreach (var variant in new[] { "Moderate", "Clear", "Deep" })
                {
                    var asset = AssetDatabase.LoadAssetAtPath<TopDown3DAtmosphereStudyProfile>(CampAtmosphereAuthoring.Settings + "/" + variant + ".asset");
                    selected = Object.Instantiate(asset); selected.name = variant;
                    binding.enabled = true; binding.ConfigureAtmosphere(selected);
                    previewMaterial = new Material(selected.skyMaterial);
                    sun.ApplyStudyPreview(.12f, previewMaterial);
                    // Diagnostic sky camera can see the sun; gameplay lens remains unchanged.
                    camera.cullingMask = 0; camera.fieldOfView = 75;
                    camera.transform.rotation = Quaternion.LookRotation(sun.DirectionToSun, Vector3.up);
                    Capture(variant + "-diagnostic-sun");
                    if (variant == "Clear")
                    {
                        camera.transform.rotation = Quaternion.Euler(0, 290, 0);
                        Capture("Clear-sky-seam");
                        camera.transform.rotation = Quaternion.LookRotation(Vector3.up, Vector3.forward);
                        Capture("Clear-sky-zenith");
                        camera.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
                        Capture("Clear-sky-nadir");
                    }
                    camera.cullingMask = ~0; camera.fieldOfView = 48;
                    foreach (var pitch in new[] { 26f, 50f, 65f })
                    {
                        rig.SetViewAngles(40, pitch); Capture(variant + "-camp-pitch" + pitch);
                    }
                    if (variant == "Clear")
                    {
                        var followTarget = new SerializedObject(rig).FindProperty("target").objectReferenceValue as Transform;
                        var savedPosition = followTarget.position;
                        var terrains = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Terrain>(true)).ToArray();
                        var native = terrains.First(t => Contains(t, savedPosition));
                        float groundOffset = savedPosition.y - native.SampleHeight(savedPosition) - native.transform.position.y;
                        foreach (var offset in new[] { new Vector3(12, 0, 0), new Vector3(-12, 0, 0), new Vector3(0, 0, -150) })
                        {
                            var position = savedPosition + offset; native = terrains.First(t => Contains(t, position));
                            position.y = native.SampleHeight(position) + native.transform.position.y + groundOffset;
                            followTarget.position = position; Physics.SyncTransforms(); rig.SetViewAngles(180, 26);
                            Capture("Clear-context-offset" + offset.x + "_" + offset.z);
                        }
                        followTarget.position = savedPosition; Physics.SyncTransforms();
                        foreach (var phase in new[] { .25f, .75f })
                        {
                            sun.ApplyStudyPreview(phase, previewMaterial); rig.SetViewAngles(180, 26);
                            Capture("Clear-south-phase" + phase);
                        }
                        foreach (var preset in new[] { "Low", "Medium", "High", "Maximum" })
                        {
                            camera.GetComponent<BadwaterCameraRange>().SetPreset(preset);
                            Capture("Clear-distance-" + preset);
                        }
                    }
                    sun.EndStudyPreview(); Object.DestroyImmediate(previewMaterial); previewMaterial = null;
                    Object.DestroyImmediate(selected); selected = null;
                }
                selected = Object.Instantiate(AssetDatabase.LoadAssetAtPath<TopDown3DAtmosphereStudyProfile>(CampAtmosphereAuthoring.Settings + "/Clear.asset"));
                previewMaterial = new Material(selected.skyMaterial);
                camera.GetComponent<BadwaterCameraRange>().SetPreset("Maximum"); rig.SetViewAngles(40, 26);
                mode = 0; frame = 0; cpu.Clear(); gpu.Clear(); SetMode();
                bloomRecorder = Recorder.Get("TopDown3D Sun Bloom"); bloomRecorder.enabled = true;
                EditorApplication.update += Measure;
            }
            catch (Exception exception) { Debug.LogException(exception); Cleanup(); EditorApplication.Exit(1); }
        }
        private static bool Contains(Terrain terrain, Vector3 position)
            => position.x >= terrain.transform.position.x && position.x < terrain.transform.position.x + terrain.terrainData.size.x
                && position.z >= terrain.transform.position.z && position.z < terrain.transform.position.z + terrain.terrainData.size.z;
        private static void Capture(string name)
        {
            camera.Render(); camera.Render();
            var path = Output + "/" + name + ".png"; CampAtmosphereCapture.SavePng(target, path); captures.Add(path);
        }
        private static void SetMode()
        {
            sun.EndStudyPreview();
            binding.enabled = mode != 0;
            if (mode == 0)
            {
                sun.SetCycle01(.12f); camera.GetComponent<Skybox>().material = baselineSky;
                baselineSky.SetVector("_SunDirection", sun.DirectionToSun);
                baselineSky.SetFloat("_Exposure", Mathf.Lerp(.65f, 1.05f, sun.Brightness01));
            }
            else { binding.ConfigureAtmosphere(selected); sun.ApplyStudyPreview(.12f, previewMaterial); }
        }
        private static void Measure()
        {
            try
            {
                var watch = Stopwatch.StartNew(); camera.Render(); watch.Stop();
                if (frame++ >= 20)
                {
                    cpu.Add(watch.Elapsed.TotalMilliseconds);
                    var property = typeof(Recorder).GetProperty("gpuElapsedNanoseconds");
                    double value = property != null ? Convert.ToDouble(property.GetValue(bloomRecorder)) / 1000000 : 0;
                    if (value > 0 && bloomRecorder.isValid) gpu.Add(value);
                }
                if (frame < 80) return;
                timings.Add(new Timing { mode = mode == 0 ? "Shared baseline optics" : "Clear study optics",
                    averageCpuRenderMs = cpu.Average(), maximumCpuRenderMs = cpu.Max(), measuredFrames = cpu.Count,
                    averageBloomGpuMs = gpu.Count > 0 ? gpu.Average() : 0, bloomGpuSamplesAvailable = gpu.Count > 0 });
                if (mode++ == 0) { frame = 0; cpu.Clear(); gpu.Clear(); SetMode(); return; }
                if (!sceneBefore.SequenceEqual(File.ReadAllBytes(GreaterWastelandSceneScope.CampLoopScene)))
                    throw new IOException("Static capture unexpectedly changed saved scene.");
                File.WriteAllText(Output + "/study-capture-report.json", JsonUtility.ToJson(new Report {
                    gpu = SystemInfo.graphicsDeviceName, graphics = SystemInfo.graphicsDeviceType.ToString(), editor = Application.unityVersion,
                    supportsGpuRecorder = SystemInfo.supportsGpuRecorder, captures = captures.ToArray(), timings = timings.ToArray() }, true));
                Debug.Log("CAMP_ATMOSPHERE_CAPTURE_PASS: static views and matched offscreen Editor render costs.");
                Cleanup(); EditorApplication.Exit(0);
            }
            catch (Exception exception) { Debug.LogException(exception); Cleanup(); EditorApplication.Exit(1); }
        }
        private static void Cleanup()
        {
            EditorApplication.update -= Measure;
            if (bloomRecorder != null) bloomRecorder.enabled = false;
            if (sun != null) sun.EndStudyPreview();
            if (camera != null) camera.targetTexture = null;
            if (target != null) { target.Release(); Object.DestroyImmediate(target); }
            if (previewMaterial != null) Object.DestroyImmediate(previewMaterial);
            if (selected != null) Object.DestroyImmediate(selected);
            if (baselineSky != null) Object.DestroyImmediate(baselineSky);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }
}
