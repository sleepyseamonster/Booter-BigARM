using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Profiling;

namespace BooterBigArm.TopDown3D
{
    // Rendering-only standalone fixture. No player movement or gameplay input is exercised.
    public sealed class TopDown3DCampOpticsPerformanceFixture : MonoBehaviour
    {
        public Camera outputCamera;
        public TopDown3DSceneRenderingOverride binding;
        public TopDown3DAtmosphereStudyProfile[] profiles;
        public GameObject emitters;
        [Serializable] private sealed class Result
        {
            public string name;
            public int frames, validGpuFrames;
            public double cpuP50, cpuP95, gpuP50, gpuP95, gpuP99;
            public long graphicsDriverAllocatedBytes;
            public long maximumBridgeAllocationBytes;
            public int bridgeSamples;
            public double submissionCpuP95;
        }
        [Serializable] private sealed class Report
        {
            public string proof = "Standalone static camp rendering fixture; excludes gameplay acceptance";
            public string gpu, api;
            public int width, height, warmup = 120, measured = 600, vsync;
            public bool offscreen;
            public string timingBoundary = "FrameTiming GPU values require valid samples; manual submission CPU excludes GPU execution and presentation";
            public Result[] results;
        }
        private readonly FrameTiming[] timings = new FrameTiming[1];
        private readonly List<double> cpu = new List<double>(600), gpu = new List<double>(600);
        private readonly List<double> submission = new List<double>(600);
        private readonly List<Result> results = new List<Result>(18);
        private int index, frame;
        private Quaternion baseRotation;
        private Quaternion caseRotation;
        private long maximumBridgeAllocation;
        private int bridgeSamples;
        private RenderTexture offscreenTarget;

        private void Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            Application.runInBackground = true;
            Physics.simulationMode = SimulationMode.Script;
            if (Application.isBatchMode)
            {
                offscreenTarget = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGBHalf);
                offscreenTarget.Create(); outputCamera.targetTexture = offscreenTarget; outputCamera.enabled = false;
            }
            baseRotation = outputCamera.transform.rotation;
            Directory.CreateDirectory(GetOutput());
            BeginCase();
        }
        private void BeginCase()
        {
            frame = 0; cpu.Clear(); gpu.Clear(); submission.Clear();
            maximumBridgeAllocation = 0; bridgeSamples = 0;
            binding.ConfigureAtmosphere(profiles[index % profiles.Length]);
            int group = index / profiles.Length;
            emitters.SetActive(group == 1);
            caseRotation = group == 2 && RenderSettings.sun != null ? Quaternion.LookRotation(-RenderSettings.sun.transform.forward, Vector3.up) : baseRotation;
            outputCamera.transform.rotation = caseRotation;
        }
        private void Update()
        {
            if (index >= profiles.Length * 3) return;
            // Same deterministic view motion in every case.
            outputCamera.transform.rotation = caseRotation * Quaternion.Euler(0f, Mathf.Sin(frame * .02f) * 1.5f, 0f);
            double submissionMs = 0;
            if (offscreenTarget != null)
            {
                long started = System.Diagnostics.Stopwatch.GetTimestamp();
                try { outputCamera.Render(); } finally { TopDown3DCampOpticsStack.Restore(outputCamera); }
                submissionMs = (System.Diagnostics.Stopwatch.GetTimestamp() - started) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            }
            if (frame == 60)
            {
                if (offscreenTarget != null) SaveOffscreen();
                else ScreenCapture.CaptureScreenshot(Path.Combine(GetOutput(), "UI-case" + index + ".png"));
            }
            FrameTimingManager.CaptureFrameTimings();
            if (frame >= 120)
            {
                if (offscreenTarget != null) submission.Add(submissionMs);
                long allocated = TopDown3DCampOpticsStack.LastApplyAllocatedBytes(outputCamera);
                if (allocated >= 0) { bridgeSamples++; maximumBridgeAllocation = Math.Max(maximumBridgeAllocation, allocated); }
                if (FrameTimingManager.GetLatestTimings(1, timings) > 0)
                {
                    if (timings[0].cpuFrameTime > 0) cpu.Add(timings[0].cpuFrameTime);
                    if (timings[0].gpuFrameTime > 0) gpu.Add(timings[0].gpuFrameTime);
                }
            }
            frame++;
            if (frame < 720) return;
            if (bridgeSamples < 580)
            {
                File.WriteAllText(Path.Combine(GetOutput(), "fixture-error.txt"), "Native processing bridge missing from measured frames: " + bridgeSamples);
                Application.Quit(2); index = profiles.Length * 3; return;
            }
            cpu.Sort(); gpu.Sort(); submission.Sort();
            results.Add(new Result { name = profiles[index % profiles.Length].name + (index / profiles.Length == 2 ? "-sun-view" : index / profiles.Length == 1 ? "-emitters" : "-quiet"),
                frames = 600, validGpuFrames = gpu.Count, cpuP50 = Percentile(cpu, .50), cpuP95 = Percentile(cpu, .95),
                gpuP50 = Percentile(gpu, .50), gpuP95 = Percentile(gpu, .95), gpuP99 = Percentile(gpu, .99),
                graphicsDriverAllocatedBytes = Profiler.GetAllocatedMemoryForGraphicsDriver() });
            results[results.Count - 1].maximumBridgeAllocationBytes = maximumBridgeAllocation;
            results[results.Count - 1].bridgeSamples = bridgeSamples;
            results[results.Count - 1].submissionCpuP95 = Percentile(submission, .95);
            index++;
            if (index < profiles.Length * 3) { BeginCase(); return; }
            var directory = GetOutput(); Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "player-frame-times.json"), JsonUtility.ToJson(new Report
            { gpu = SystemInfo.graphicsDeviceName, api = SystemInfo.graphicsDeviceType.ToString(), width = Screen.width,
                height = Screen.height, vsync = QualitySettings.vSyncCount, offscreen = offscreenTarget != null, results = results.ToArray() }, true));
            Debug.Log("CAMP_OPTICS_PLAYER_FIXTURE_DONE " + directory);
            Application.Quit(0);
        }
        private void SaveOffscreen()
        {
            var prior = RenderTexture.active;
            var image = new Texture2D(offscreenTarget.width, offscreenTarget.height, TextureFormat.RGB24, false, true);
            try
            {
                RenderTexture.active = offscreenTarget;
                image.ReadPixels(new Rect(0, 0, image.width, image.height), 0, 0); image.Apply();
                var pixels = image.GetPixels();
                for (int i = 0; i < pixels.Length; i++) pixels[i] = pixels[i].gamma;
                image.SetPixels(pixels); image.Apply();
                File.WriteAllBytes(Path.Combine(GetOutput(), "Offscreen-case" + index + ".png"), image.EncodeToPNG());
            }
            finally { RenderTexture.active = prior; Destroy(image); }
        }
        private void OnDestroy()
        {
            TopDown3DCampOpticsStack.Release(outputCamera);
            if (offscreenTarget != null) { outputCamera.targetTexture = null; offscreenTarget.Release(); Destroy(offscreenTarget); }
        }
        private static double Percentile(List<double> values, double percentile)
            => values.Count == 0 ? 0 : values[Math.Min(values.Count - 1, (int)Math.Ceiling(percentile * values.Count) - 1)];
        private static string GetOutput()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++) if (args[i] == "-campOpticsOutput") return args[i + 1];
            return Path.Combine(Application.persistentDataPath, "CampOpticsFixture");
        }
    }
}
