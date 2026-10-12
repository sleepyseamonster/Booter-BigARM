using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using BooterBigArm.TopDown3D;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Inert outside Assets. Copy only into the runtime assembly of the isolated audit candidate.
public sealed class GreaterWastelandPlayerAudit : MonoBehaviour
{
    [Serializable] public sealed class Marker { public string name, unit; public double meanRaw; }
    [Serializable] public struct TransitionFrame
    { public double elapsed, updateMs, requestMs; public Vector3 camera; public int terrainRenderers; }
    [Serializable] public sealed class Report
    {
        public string mode, error, scene, gpu, graphicsApi, unity, persistentPath;
        public string previewPath, previewSha256;
        public bool nonuniformPixels;
        public bool completed, rendered, allTerrainRendering, savesDisabled;
        public int width, height, residentTerrains, enabledTerrains, enabledColliders, samples, renderedFrames, quality;
        public int manualRequests;
        public double[] frameTimesMs;
        public double startupSeconds, sampleSeconds, meanMs, medianMs, p95Ms, p99Ms, maximumMs, meanCpuMs, meanGpuMs;
        public int validCpuTimings, validGpuTimings;
        public float renderScale, shadowDistance, cameraFar;
        public Vector3 cameraStart, cameraEnd;
        public Marker[] markers;
        public bool cameraSweep;
        public bool explicitTerrainPreparation;
        public TransitionFrame[] transitionFrames;
    }
    private readonly List<double> times = new();
    private readonly List<ProfilerRecorder> recorders = new();
    private readonly List<Marker> markers = new();
    private readonly FrameTiming[] timing = new FrameTiming[1];
    private double[] sums;
    private Report report;
    private Terrain[] terrain;
    private Camera view;
    private RenderTexture target;
    private string output;
    private double begin, samplingStart;
    private int lastRenderedFrame = -1, renderEvents;
    private bool finished;
    private bool manualRender;
    private GreaterWastelandTerrainRendering terrainRendering;
    private bool discardBoundaryFrame;
    private readonly List<TransitionFrame> transitionFrames = new(10000);
    private TransitionFrame lastSubmission;
    private FixedTerrainSurfaceIndex sweepSurface;
    private Vector3 sweepOrigin;
    private float sweepClearance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (Application.isEditor || Array.IndexOf(Environment.GetCommandLineArgs(), "-greaterWastelandPlayerAudit") < 0) return;
        var owner = new GameObject("Isolated Greater Wasteland Player Audit");
        owner.AddComponent<GreaterWastelandPlayerAudit>();
    }
    private void Awake()
    {
        try
        {
            output = Argument("-auditOutput");
            string full = Path.GetFullPath(output ?? "");
            if (!Application.isBatchMode || !full.StartsWith("D:\\BooterBigArmValidation\\PerformancePlayer", StringComparison.OrdinalIgnoreCase)
                || Path.GetExtension(full) != ".json" || File.Exists(full)) throw new InvalidOperationException("Use a fresh task-owned PerformancePlayer JSON output in batch mode.");
            output = full;
            if (Application.companyName != "BooterBigArmDiagnostics" || !Application.productName.StartsWith("GreaterWastelandAudit", StringComparison.Ordinal))
                throw new InvalidOperationException("Diagnostic-only application identity is required to isolate production saves.");
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) throw new InvalidOperationException("A real graphics device is required; no nographics throughput may be accepted as FPS.");
            if (SceneManager.GetActiveScene().path != GreaterWastelandTerrainRendering.ScenePath || Camera.main == null)
                throw new InvalidOperationException("The saved production scene and real main camera are required.");
            report = new Report { mode = "standalone_player_automatic_offscreen_camera_loop", scene = SceneManager.GetActiveScene().path,
                gpu = SystemInfo.graphicsDeviceName, graphicsApi = SystemInfo.graphicsDeviceType.ToString(), unity = Application.unityVersion,
                width = 1920, height = 1080, quality = QualitySettings.GetQualityLevel(), persistentPath = Application.persistentDataPath,
                allTerrainRendering = Array.IndexOf(Environment.GetCommandLineArgs(), GreaterWastelandTerrainRendering.AllTerrainArgument) >= 0 };
            manualRender = Array.IndexOf(Environment.GetCommandLineArgs(), "-auditManualRender") >= 0;
            report.cameraSweep = Array.IndexOf(Environment.GetCommandLineArgs(), "-auditCameraSweep") >= 0;
            if (report.cameraSweep && !manualRender) throw new InvalidOperationException("Camera sweep requires explicit manual URP diagnostics.");
            if (manualRender) report.mode = "standalone_player_manual_urp_offscreen_diagnostic";
            // Prevent checkpoint restore/autosaves from changing comparison poses or touching any user's save.
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var save in root.GetComponentsInChildren<GreaterWastelandInventorySaveService>(true)) save.enabled = false;
            report.savesDisabled = true;
            terrain = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Terrain>(true)).ToArray();
            report.residentTerrains = terrain.Length;
            if (report.cameraSweep) sweepSurface = new FixedTerrainSurfaceIndex(terrain, "audit.frozen.native");
            view = Camera.main;
            terrainRendering = FindTerrainRendering(view.gameObject.scene);
            var pipeline = (QualitySettings.renderPipeline ?? GraphicsSettings.defaultRenderPipeline) as UniversalRenderPipelineAsset;
            if (pipeline == null) throw new InvalidOperationException("Expected pinned URP pipeline.");
            report.renderScale = pipeline.renderScale; report.shadowDistance = pipeline.shadowDistance; report.cameraFar = view.farClipPlane;
            target = new RenderTexture(1920, 1080, 24, RenderTextureFormat.DefaultHDR) { antiAliasing = pipeline.msaaSampleCount };
            if (!target.Create()) throw new InvalidOperationException("1080p render target creation failed.");
            view.targetTexture = target;
            // Uncap both conditions identically; production rendering quality remains intact.
            QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1; Application.runInBackground = true;
            RenderPipelineManager.endCameraRendering += Rendered;
            var available = new List<ProfilerRecorderHandle>(); ProfilerRecorderHandle.GetAvailable(available);
            foreach (var handle in available)
            {
                var d = ProfilerRecorderHandle.GetDescription(handle);
                if (d.Name != "Main Thread" && d.Name != "Render Thread" && d.Name != "GC Allocated In Frame"
                    && d.Name != "Draw Calls Count" && d.Name != "Triangles Count" && d.Name != "SetPass Calls Count"
                    && d.Name != "BehaviourUpdate" && d.Name != "Physics.Simulate" && d.Name.IndexOf("Terrain", StringComparison.OrdinalIgnoreCase) < 0) continue;
                try { recorders.Add(ProfilerRecorder.StartNew(d.Category, d.Name, 1)); markers.Add(new Marker { name = d.Name, unit = d.UnitType.ToString() }); }
                catch (ArgumentException) { }
            }
            sums = new double[recorders.Count]; begin = Time.realtimeSinceStartupAsDouble;
        }
        catch (Exception e) { Finish(e.ToString()); }
    }
    private void Rendered(ScriptableRenderContext context, Camera camera)
    { if (camera == view && !manualRender) { lastRenderedFrame = Time.frameCount; renderEvents++; } }
    private void Update()
    {
        if (finished) return;
        var completedSubmission = lastSubmission;
        if (manualRender)
        {
            try
            {
                if (report.cameraSweep && samplingStart > 0 && !discardBoundaryFrame) MoveSweepCamera();
                double requestStart = Time.realtimeSinceStartupAsDouble;
                if (!report.allTerrainRendering)
                {
                    if (terrainRendering == null) terrainRendering = FindTerrainRendering(view.gameObject.scene);
                    if (terrainRendering == null || terrainRendering.ResidentTerrainCount != terrain.Length)
                        throw new InvalidOperationException("The transient terrain renderer must be resolved before explicit culling.");
                    terrainRendering.PrepareCamera(view); report.explicitTerrainPreparation = true;
                }
                RenderPipeline.SubmitRenderRequest(view, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                // SingleCameraRequest may bypass normal camera callbacks. Successful submission records diagnostic requests, not automatic rendered frames.
                lastRenderedFrame = Time.frameCount - 1; renderEvents++;
                lastSubmission = new TransitionFrame { elapsed = samplingStart > 0 ? requestStart - samplingStart : 0,
                    camera = view.transform.position, terrainRenderers = terrainRendering?.EnabledTerrainCount ?? terrain.Length,
                    requestMs = (Time.realtimeSinceStartupAsDouble - requestStart) * 1000 };
            }
            catch (Exception e) { Finish("Manual URP request failed: " + e); return; }
        }
        double now = Time.realtimeSinceStartupAsDouble;
        if (now - begin > 120) { Finish("Audit deadline exceeded."); return; }
        if (samplingStart == 0)
        {
            if (now - begin < 10) return;
            if (renderEvents == 0) { Finish("Automatic Player camera rendering did not occur; no valid rendered-frame evidence."); return; }
            samplingStart = now; discardBoundaryFrame = true; renderEvents = 0;
            return;
        }
        // Delta time and recorder values belong to the completed previous frame. Discard the warmup-boundary frame before starting the sample clock.
        if (discardBoundaryFrame)
        {
            discardBoundaryFrame = false; samplingStart = now; report.startupSeconds = now;
            lastSubmission.elapsed = 0;
            report.cameraStart = view.transform.position; report.cameraFar = view.farClipPlane; renderEvents = 0; return;
        }
        if (lastRenderedFrame != Time.frameCount - 1) { Finish("A sampled Player frame lacked the real camera render callback."); return; }
        times.Add(Time.unscaledDeltaTime * 1000d);
        if (report.cameraSweep) { completedSubmission.updateMs = times[times.Count - 1]; transitionFrames.Add(completedSubmission); }
        for (int i = 0; i < recorders.Count; i++) sums[i] += recorders[i].LastValue;
        FrameTimingManager.CaptureFrameTimings();
        if (FrameTimingManager.GetLatestTimings(1, timing) > 0)
        {
            if (timing[0].cpuFrameTime > 0) { report.meanCpuMs += timing[0].cpuFrameTime; report.validCpuTimings++; }
            if (timing[0].gpuFrameTime > 0) { report.meanGpuMs += timing[0].gpuFrameTime; report.validGpuTimings++; }
        }
        if (now - samplingStart >= 30) { report.sampleSeconds = now - samplingStart; Finish(null); }
    }
    // Unity's general object lookup excludes DontSave objects. The installed renderer is a transient scene root.
    public static GreaterWastelandTerrainRendering FindTerrainRendering(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            var controller = root.GetComponent<GreaterWastelandTerrainRendering>();
            if (controller != null && controller.isActiveAndEnabled) return controller;
        }
        return null;
    }
    private void MoveSweepCamera()
    {
        if (transitionFrames.Count == 0)
        {
            sweepOrigin = report.cameraStart;
            if (!sweepSurface.TrySample(new Vector2(sweepOrigin.x, sweepOrigin.z), out var ground)) throw new InvalidOperationException("Sweep origin lacks native collision.");
            sweepClearance = sweepOrigin.y - ground.Position.y;
            if (sweepClearance <= 0) throw new InvalidOperationException("Sweep camera must begin above native ground.");
            var rig = view.GetComponent<TopDown3DCameraRig>(); if (rig != null) rig.enabled = false;
        }
        float leg = Mathf.Clamp01((float)((Time.realtimeSinceStartupAsDouble - samplingStart) / 30)) * 4;
        Vector2 offset = leg < 1 ? new Vector2(leg * 1024, 0) : leg < 2 ? new Vector2(1024, (leg - 1) * 1024)
            : leg < 3 ? new Vector2((3 - leg) * 1024, 1024) : new Vector2(0, (4 - leg) * 1024);
        var position = new Vector2(sweepOrigin.x + offset.x, sweepOrigin.z + offset.y);
        if (!sweepSurface.TrySample(position, out var sample)) throw new InvalidOperationException("Sweep route leaves valid native collision.");
        view.transform.position = new Vector3(position.x, sample.Position.y + sweepClearance, position.y);
    }
    private void Finish(string error)
    {
        if (finished) return; finished = true;
        RenderPipelineManager.endCameraRendering -= Rendered;
        if (report == null) report = new Report();
        report.error = error; report.completed = error == null; report.rendered = renderEvents > 0;
        report.renderedFrames = manualRender ? 0 : renderEvents; report.manualRequests = manualRender ? renderEvents : 0;
        report.samples = times.Count;
        report.frameTimesMs = times.ToArray();
        if (report.cameraSweep) report.transitionFrames = transitionFrames.ToArray();
        if (times.Count > 0)
        {
            times.Sort(); report.meanMs = times.Average(); report.medianMs = Percentile(.5); report.p95Ms = Percentile(.95);
            report.p99Ms = Percentile(.99); report.maximumMs = times[times.Count - 1];
            for (int i = 0; i < markers.Count; i++) markers[i].meanRaw = sums[i] / times.Count;
        }
        report.markers = markers.ToArray();
        if (error == null && manualRender && target != null)
        {
            var prior = RenderTexture.active;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            try
            {
                RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0, false);
                var bytes = image.GetRawTextureData<byte>().ToArray();
                for (int i = 3; i < bytes.Length; i += 3 * 509)
                    if (bytes[i] != bytes[0] || bytes[i + 1] != bytes[1] || bytes[i + 2] != bytes[2]) { report.nonuniformPixels = true; break; }
                if (!report.nonuniformPixels) { report.error = "Render target remained uniform; reject manual rendering evidence."; report.completed = false; }
                report.previewPath = Path.ChangeExtension(output, ".png"); var png = image.EncodeToPNG(); File.WriteAllBytes(report.previewPath, png);
                using var hash = SHA256.Create(); report.previewSha256 = string.Concat(hash.ComputeHash(png).Select(v => v.ToString("x2")));
            }
            catch (Exception e) { report.error = "Diagnostic pixel verification failed: " + e; report.completed = false; }
            finally { RenderTexture.active = prior; Destroy(image); }
        }
        if (report.validCpuTimings > 0) report.meanCpuMs /= report.validCpuTimings;
        if (report.validGpuTimings > 0) report.meanGpuMs /= report.validGpuTimings;
        if (terrain != null)
        {
            report.enabledTerrains = terrain.Count(t => t != null && t.enabled);
            report.enabledColliders = terrain.Count(t => t != null && t.GetComponent<TerrainCollider>()?.enabled == true);
        }
        if (view != null) { report.cameraEnd = view.transform.position; report.cameraFar = view.farClipPlane; view.targetTexture = null; }
        foreach (var recorder in recorders) recorder.Dispose();
        if (target != null) { target.Release(); Destroy(target); }
        if (!string.IsNullOrWhiteSpace(output) && Path.GetFullPath(output).StartsWith("D:\\BooterBigArmValidation\\PerformancePlayer", StringComparison.OrdinalIgnoreCase))
        { Directory.CreateDirectory(Path.GetDirectoryName(output)); File.WriteAllText(output, JsonUtility.ToJson(report, true)); }
        Debug.Log("[Player audit] " + (report.error ?? "Completed rendered-frame capture."));
        Application.Quit(report.completed ? 0 : 1);
    }
    private double Percentile(double fraction) => times[Math.Min(times.Count - 1, (int)Math.Ceiling(times.Count * fraction) - 1)];
    private static string Argument(string name)
    { var args = Environment.GetCommandLineArgs(); for (int i = 0; i + 1 < args.Length; i++) if (args[i] == name) return args[i + 1]; return null; }
}
