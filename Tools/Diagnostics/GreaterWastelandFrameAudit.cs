using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;

// Isolated audit helper kept outside Assets so production never imports it.
// See README.md for compilation, capture prerequisites and measurement limits.
public static class GreaterWastelandFrameAudit
{
    const string RunningKey = "GreaterWasteland.FrameAudit.Running";
    [Serializable] public class Phase { public string name; public int samples, enabledTerrains, drawnTerrains; public float averageMs, p95Ms, p99Ms, fps; public double cpuMs, gpuMs; public string[] markerNames; public double[] markerMeans; }
    [Serializable] public class Report { public string scene, gpu, error, mode; public int terrainCount, width, height; public float cameraFar; public bool restored; public Phase[] phases; }
    static Terrain[] tiles; static bool[] enabled, draw; static UniversalRenderPipelineAsset source, clone;
    static List<Phase> phases = new List<Phase>(); static List<float> times = new List<float>();
    static List<ProfilerRecorder> recorders = new List<ProfilerRecorder>(); static List<string> names = new List<string>();
    static double[] sums; static double cpu, gpu; static int timings, phase, lastFrame; static double started;
    static string output; static Report report; static FrameTiming[] ft = new FrameTiming[1];
    static readonly string[] labels = { "baseline", "far_draw_heightmap_off", "far_terrain_component_off", "baseline_repeat", "half_render_scale", "shadows_off" };
    static RenderTexture renderTarget; static RenderPipelineAsset previousPipeline; static bool previousBackground;
    static void ValidateContext()
    {
        if (!Application.dataPath.Replace('\\', '/').Contains("/BooterBigArmValidation/PerformanceAudit"))
            throw new InvalidOperationException("Use a task-owned isolated PerformanceAudit candidate.");
        if (SessionState.GetBool(RunningKey, false))
            throw new InvalidOperationException("A frame audit is already running.");
        if (!EditorApplication.isPlaying || EditorApplication.isPaused || UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != "Assets/_Project/Scenes/Production/GreaterWasteland.unity")
            throw new InvalidOperationException("Requires running, unpaused Greater Wasteland.");
        if (Camera.main == null || !((QualitySettings.renderPipeline ?? GraphicsSettings.defaultRenderPipeline) is UniversalRenderPipelineAsset))
            throw new InvalidOperationException("Missing pipeline or camera.");
    }
    public static string StartOffscreen()
    {
        ValidateContext();
        renderTarget = new RenderTexture(1158, 495, 24);
        try
        {
            renderTarget.Create();
            // Complete the first shader/render warmup before starting the measurement clock.
            RenderPipeline.SubmitRenderRequest(Camera.main, new UniversalRenderPipeline.SingleCameraRequest { destination = renderTarget });
            return Start();
        }
        catch
        {
            renderTarget.Release();
            UnityEngine.Object.DestroyImmediate(renderTarget);
            renderTarget = null;
            throw;
        }
    }
    public static string Start()
    {
        ValidateContext();
        phases.Clear(); times.Clear(); recorders.Clear(); names.Clear();
        previousBackground = Application.runInBackground; Application.runInBackground = true; EditorApplication.QueuePlayerLoopUpdate(); tiles = Terrain.activeTerrains; enabled = tiles.Select(t=>t.enabled).ToArray(); draw = tiles.Select(t=>t.drawHeightmap).ToArray();
        previousPipeline = QualitySettings.renderPipeline;
        source = (previousPipeline ?? GraphicsSettings.defaultRenderPipeline) as UniversalRenderPipelineAsset;
        if (source == null || Camera.main == null) throw new InvalidOperationException("Missing pipeline or camera.");
        output = Path.GetFullPath("Logs/greater-wasteland-frame-audit.json");
        report = new Report {mode=renderTarget!=null?"offscreen_camera_render":"editor_game_loop",scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,gpu=SystemInfo.graphicsDeviceName,terrainCount=tiles.Length,width=renderTarget!=null?renderTarget.width:Screen.width,height=renderTarget!=null?renderTarget.height:Screen.height,cameraFar=Camera.main.farClipPlane};
        var handles = new List<ProfilerRecorderHandle>(); ProfilerRecorderHandle.GetAvailable(handles);
        foreach (var h in handles)
        {
            var d = ProfilerRecorderHandle.GetDescription(h);
            if (d.Name == "Main Thread" || d.Name == "Render Thread" || d.Name == "GC Allocated In Frame" || d.Name == "Draw Calls Count" || d.Name == "Triangles Count" || d.Name == "Batches Count" || d.Name == "SetPass Calls Count" || d.Name == "BehaviourUpdate" || d.Name == "Physics.Simulate" || d.Name == "Camera.Render" || d.Name.IndexOf("Terrain", StringComparison.OrdinalIgnoreCase)>=0 || d.Name == "CullResults.Cull")
            {
                try { recorders.Add(ProfilerRecorder.StartNew(d.Category,d.Name,1)); names.Add(d.Name); } catch { }
            }
        }
        sums = new double[names.Count]; phase=0; BeginPhase();
        SessionState.SetBool(RunningKey, true);
        EditorApplication.update += Tick; EditorApplication.playModeStateChanged += ModeChanged;
        AssemblyReloadEvents.beforeAssemblyReload += Aborted;
        return "Started six stationary phases: 5 s settling + 15 s sampling each. Result: " + output;
    }
    static void Restore()
    {
        for(int i=0;i<tiles.Length;i++) if(tiles[i]!=null) {tiles[i].enabled=enabled[i];tiles[i].drawHeightmap=draw[i];}
        QualitySettings.renderPipeline=previousPipeline;
        if(clone!=null) {UnityEngine.Object.DestroyImmediate(clone);clone=null;}
    }
    static void BeginPhase()
    {
        Restore();
        if(phase==1 || phase==2)
        {
            var c=Camera.main; var p=c.transform.position; float radius=Mathf.Max(1000,c.farClipPlane+512);
            for(int i=0;i<tiles.Length;i++)
            {
                var o=tiles[i].transform.position; var s=tiles[i].terrainData.size;
                var b=new Bounds(o+s*.5f,s); var q=b.ClosestPoint(p);
                if(new Vector2(q.x-p.x,q.z-p.z).sqrMagnitude>radius*radius)
                { if(phase==1) tiles[i].drawHeightmap=false; else tiles[i].enabled=false; }
            }
        }
        if(phase==4 || phase==5)
        { clone=UnityEngine.Object.Instantiate(source);clone.hideFlags=HideFlags.DontSave; if(phase==4) clone.renderScale=.5f; else clone.shadowDistance=0; QualitySettings.renderPipeline=clone; }
        times.Clear(); Array.Clear(sums,0,sums.Length); cpu=0;gpu=0;timings=0;lastFrame=-1;started=EditorApplication.timeSinceStartup;
        File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented));
    }
    static void Tick()
    {
        try
        {
            if(!EditorApplication.isPlaying) {Finish("Play Mode ended early");return;}
            if(renderTarget!=null && lastFrame!=Time.frameCount) RenderPipeline.SubmitRenderRequest(Camera.main,new UniversalRenderPipeline.SingleCameraRequest {destination=renderTarget});
            var elapsed=EditorApplication.timeSinceStartup-started;
            if(elapsed>=20)
            {
                var sorted=times.OrderBy(t=>t).ToArray(); var avg=times.Count>0 ? times.Average():0;
                phases.Add(new Phase {name=labels[phase],samples=times.Count,enabledTerrains=tiles.Count(t=>t.enabled),drawnTerrains=tiles.Count(t=>t.enabled&&t.drawHeightmap),averageMs=avg,fps=avg>0?1000/avg:0,p95Ms=sorted.Length>0?sorted[Math.Min(sorted.Length-1,(int)Math.Ceiling(sorted.Length*.95)-1)]:0,p99Ms=sorted.Length>0?sorted[Math.Min(sorted.Length-1,(int)Math.Ceiling(sorted.Length*.99)-1)]:0,cpuMs=timings>0?cpu/timings:0,gpuMs=timings>0?gpu/timings:0,markerNames=names.ToArray(),markerMeans=sums.Select(s=>times.Count>0?s/times.Count:0).ToArray()});
                report.phases=phases.ToArray();phase++;if(phase==labels.Length){Finish(null);return;}BeginPhase();return;
            }
            if(elapsed<5 || lastFrame==Time.frameCount) return;
            lastFrame=Time.frameCount;times.Add(Time.unscaledDeltaTime*1000);
            for(int i=0;i<recorders.Count;i++) sums[i]+=recorders[i].LastValue;
            FrameTimingManager.CaptureFrameTimings();
            if(FrameTimingManager.GetLatestTimings(1,ft)>0){cpu+=ft[0].cpuFrameTime;gpu+=ft[0].gpuFrameTime;timings++;}
        }
        catch(Exception e){Finish(e.ToString());}
    }
    static void ModeChanged(PlayModeStateChange s) {if(s==PlayModeStateChange.ExitingPlayMode) Finish("Interrupted by Play Mode exit");}
    static void Aborted(){Finish("Interrupted by assembly reload");}
    public static void Cancel() { if (SessionState.GetBool(RunningKey, false)) Finish("Cancelled by audit controller"); }
    static void Finish(string error)
    {
        if (!SessionState.GetBool(RunningKey, false)) return;
        SessionState.SetBool(RunningKey, false);
        EditorApplication.update-=Tick;EditorApplication.playModeStateChanged-=ModeChanged;AssemblyReloadEvents.beforeAssemblyReload-=Aborted;
        try{Restore();Application.runInBackground=previousBackground;report.restored=true;}catch(Exception e){error=(error??"")+e;}
        foreach(var r in recorders) r.Dispose();
        if(renderTarget!=null){renderTarget.Release();UnityEngine.Object.DestroyImmediate(renderTarget);renderTarget=null;}
        if (error == null && phases.Any(p => p.samples == 0)) error = "At least one phase has no samples; do not treat it as a valid frame measurement.";
        report.error=error; report.phases=phases.ToArray();File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented));
        // Remain in Play Mode so caller can read results before explicitly returning to Edit Mode.
    }
}
