using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Inert outside Assets. Measures a reference Player; never injects movement or accepts interactive FPS.
public sealed class PopulationSectorPlayerAudit : MonoBehaviour
{
    private const string ScenePath = "Assets/_Project/Scenes/Reference/Performance/GreaterWastelandPopulationActors20261012.unity";
    [Serializable] public struct Sample
    {
        public int phase, operations, active, pending, unready, synchronizations;
        public double elapsed, updateMs, populationMs, renderMs;
        public bool booterWaiting, bigArmWaiting;
    }
    [Serializable] public sealed class Report
    {
        public string mode = "manual_urp_population_reference_diagnostic", error, gpu, scene, previewPath;
        public bool completed, nonuniformPixels;
        public int nativeTerrains, enabledColliders, quality;
        public float cameraFar, renderScale, shadowDistance;
        public double readySeconds, startupMaximumPopulationMs;
        public Vector3 booterStart, booterEnd, bigArmStart, bigArmEnd;
        public Sample[] samples;
    }
    private readonly List<Sample> samples = new(32768);
    private Report report;
    private FixedTerrainPopulationSectorStudy study;
    private TopDown3DPlayerMotor booter;
    private TopDown3DBigArmFollower bigArm;
    private Camera view;
    private RenderTexture target;
    private string output;
    private double begin, readyAt, sampleAt;
    private Sample previous;
    private bool discard, finished, outputApproved;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (!Application.isEditor && Array.IndexOf(Environment.GetCommandLineArgs(), "-populationSectorAudit") >= 0)
            new GameObject("Isolated population audit").AddComponent<PopulationSectorPlayerAudit>();
    }
    private void Awake()
    {
        try
        {
            var args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, "-auditOutput");
            output = index >= 0 && index + 1 < args.Length ? Path.GetFullPath(args[index + 1]) : null;
            if (!Application.isBatchMode || Application.companyName != "BooterBigArmDiagnostics"
                || Application.productName != "PopulationSectorAudit20261012" || output == null || File.Exists(output)
                || !output.StartsWith("D:\\BooterBigArmValidation\\PerformancePlayer", StringComparison.OrdinalIgnoreCase)
                || Path.GetExtension(output) != ".json" || SceneManager.GetActiveScene().path != ScenePath
                || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) throw new InvalidOperationException("Fresh isolated reference audit with real graphics required.");
            outputApproved = true;
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            study = roots.SelectMany(r => r.GetComponentsInChildren<FixedTerrainPopulationSectorStudy>()).Single();
            booter = roots.SelectMany(r => r.GetComponentsInChildren<TopDown3DPlayerMotor>()).Single();
            bigArm = roots.SelectMany(r => r.GetComponentsInChildren<TopDown3DBigArmFollower>()).Single();
            if (roots.SelectMany(r => r.GetComponentsInChildren<GreaterWastelandInventorySaveService>(true)).Any()) throw new InvalidOperationException("No save service in reference audit.");
            view = Camera.main; if (view == null) throw new InvalidOperationException("Real reference camera missing.");
            var pipeline = (QualitySettings.renderPipeline ?? GraphicsSettings.defaultRenderPipeline) as UniversalRenderPipelineAsset;
            if (pipeline == null) throw new InvalidOperationException("Pinned URP required.");
            target = new RenderTexture(1920, 1080, 24, RenderTextureFormat.DefaultHDR) { antiAliasing = pipeline.msaaSampleCount };
            if (!target.Create()) throw new InvalidOperationException("Render target creation failed."); view.targetTexture = target;
            report = new Report { gpu = SystemInfo.graphicsDeviceName, scene = ScenePath, quality = QualitySettings.GetQualityLevel(),
                cameraFar = view.farClipPlane, renderScale = pipeline.renderScale, shadowDistance = pipeline.shadowDistance,
                nativeTerrains = roots.SelectMany(r => r.GetComponentsInChildren<Terrain>(true)).Count(), booterStart = booter.transform.position, bigArmStart = bigArm.transform.position };
            QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1; Application.runInBackground = true;
            study.enabled = false; study.Initialize(); // Own exactly one normal-cadence update, not two competing loops.
            begin = Time.realtimeSinceStartupAsDouble;
        }
        catch (Exception e) { Finish(e.ToString()); }
    }
    private void Update()
    {
        if (finished) return;
        try
        {
            double now = Time.realtimeSinceStartupAsDouble;
            if (now - begin > 120) { Finish("Reference audit deadline exceeded."); return; }
            double elapsed = sampleAt > 0 ? now - sampleAt : 0;
            int phase = elapsed < 10 ? 0 : elapsed < 20 ? 1 : 2;
            view.enabled = phase != 1;
            double workStart = Time.realtimeSinceStartupAsDouble; int operations = study.AdvanceRuntime(now);
            double populationMs = (Time.realtimeSinceStartupAsDouble - workStart) * 1000;
            double renderStart = Time.realtimeSinceStartupAsDouble;
            RenderPipeline.SubmitRenderRequest(view, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            var current = new Sample { phase = phase, elapsed = elapsed, operations = operations, populationMs = populationMs,
                renderMs = (Time.realtimeSinceStartupAsDouble - renderStart) * 1000, active = study.ActiveCount,
                pending = study.PendingBuilds + study.PendingPresentations, unready = study.UnreadyCells,
                synchronizations = study.PhysicsSynchronizations, booterWaiting = booter.WaitingForPopulation,
                bigArmWaiting = bigArm.State == TopDown3DBigArmFollower.FollowState.WaitingForPopulation };
            if (study.FaultedCells > 0) { Finish("Population metadata fault."); return; }
            if (sampleAt == 0)
            {
                report.startupMaximumPopulationMs = Math.Max(report.startupMaximumPopulationMs, populationMs);
                if (readyAt == 0 && study.UnreadyCells == 0
                    && booter.PopulationPreparation(booter.PopulationPreparationBounds()) && bigArm.PopulationPreparation(bigArm.PopulationPreparationBounds()))
                { readyAt = Time.realtimeSinceStartupAsDouble; report.readySeconds = readyAt - begin; }
                if (readyAt > 0 && Time.realtimeSinceStartupAsDouble - readyAt >= 2) { sampleAt = Time.realtimeSinceStartupAsDouble; discard = true; }
            }
            else if (discard) { discard = false; sampleAt = Time.realtimeSinceStartupAsDouble; }
            else
            {
                previous.updateMs = Time.unscaledDeltaTime * 1000d; samples.Add(previous);
                if (elapsed >= 30) { Finish(null); return; }
            }
            previous = current;
        }
        catch (Exception e) { Finish(e.ToString()); }
    }
    private void Finish(string error)
    {
        if (finished) return; finished = true;
        report ??= new Report(); report.error = error; report.completed = error == null; report.samples = samples.ToArray();
        if (booter != null) report.booterEnd = booter.transform.position;
        if (bigArm != null) report.bigArmEnd = bigArm.transform.position;
        report.enabledColliders = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<TerrainCollider>(true)).Count(c => c.enabled);
        if (error == null && target != null)
        {
            var prior = RenderTexture.active; var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            try
            {
                RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0, false);
                var bytes = image.GetRawTextureData<byte>();
                for (int i = 3; i < bytes.Length; i += 3 * 509)
                    if (bytes[i] != bytes[0] || bytes[i + 1] != bytes[1] || bytes[i + 2] != bytes[2]) { report.nonuniformPixels = true; break; }
                report.previewPath = Path.ChangeExtension(output, ".png"); File.WriteAllBytes(report.previewPath, image.EncodeToPNG());
                if (!report.nonuniformPixels) { report.completed = false; report.error = "Uniform target; rendering evidence rejected."; }
            }
            finally { RenderTexture.active = prior; Destroy(image); }
        }
        if (view != null) view.targetTexture = null;
        if (target != null) { target.Release(); Destroy(target); }
        if (outputApproved) File.WriteAllText(output, JsonUtility.ToJson(report, true));
        Debug.Log("POPULATION_SECTOR_AUDIT: " + (report.error ?? "Completed manual capture.")); Application.Quit(report.completed ? 0 : 1);
    }
}
