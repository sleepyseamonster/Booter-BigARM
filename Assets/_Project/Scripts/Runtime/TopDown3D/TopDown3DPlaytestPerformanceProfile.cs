using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BooterBigArm.TopDown3D
{
    /// <summary>
    /// Installs telemetry plus a reversible rendering posture for Editor and Development Player playtests.
    /// An explicit command-line flag keeps full production content active for controlled profiling.
    /// It deliberately leaves release-player graphics unchanged.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class TopDown3DPlaytestPerformanceProfile : MonoBehaviour
    {
        internal const string FullContentProfileArgument = "-topDown3DFullContentProfile";
        public const string FullContentEditorPreferenceKey = "BooterBigArm.TopDown3D.FullContentPlayMode";

        private const string FastTerrainKeyword = "TOPDOWN3D_PLAYTEST_FAST_TERRAIN";
        private const float ReportIntervalSeconds = 5f;
        private const int TargetFrameRate = 60;
        private const float RenderScale = 0.5f;
        private const float ShadowDistance = 20f;
        private const int ShadowResolution = 1024;
        private const int ShadowCascades = 1;
        private const int TextureMipmapLimit = 2;
        private const int StreamingRadius = 2;
        private const int DecorationStreamingRadius = 0;
        private const int ChunksBuiltPerFrame = 1;

        private readonly float[] frameTimes = new float[600];
        private readonly float[] sortedFrameTimes = new float[600];
        private readonly FrameTiming[] latestTiming = new FrameTiming[1];
        private RenderPipelineAsset previousQualityPipeline;
        private UniversalRenderPipelineAsset playtestPipeline;
        private int previousVSyncCount;
        private int previousTargetFrameRate;
        private int previousTextureMipmapLimit;
        private int frameTimeCount;
        private int frameTimeCursor;
        private float nextReportTime;
        private TopDown3DProceduralWorld world;
        private bool stressOverridesApplied;
        private bool fullContentTelemetryMode;

        internal static int StreamingRadiusOverride { get; private set; }
        internal static int DecorationStreamingRadiusOverride { get; private set; } = -1;
        internal static int ChunksBuiltPerFrameOverride { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (FindAnyObjectByType<TopDown3DPlaytestPerformanceProfile>() != null)
            {
                return;
            }

            var profileObject = new GameObject("TopDown3D Playtest Performance Profile");
            DontDestroyOnLoad(profileObject);
            profileObject.AddComponent<TopDown3DPlaytestPerformanceProfile>();
#endif
        }

        private void Awake()
        {
            fullContentTelemetryMode = ShouldUseFullContentProfile(
                Environment.GetCommandLineArgs(),
                IsEditorFullContentProfileEnabled());
            if (fullContentTelemetryMode)
            {
                Debug.Log(
                    "[TopDown3D Performance] Full-content telemetry mode enabled: production rendering, "
                    + "streaming, decoration, and generation settings remain unchanged.",
                    this);
                return;
            }

            StreamingRadiusOverride = StreamingRadius;
            DecorationStreamingRadiusOverride = DecorationStreamingRadius;
            ChunksBuiltPerFrameOverride = ChunksBuiltPerFrame;
            previousQualityPipeline = QualitySettings.renderPipeline;
            previousVSyncCount = QualitySettings.vSyncCount;
            previousTargetFrameRate = Application.targetFrameRate;
            previousTextureMipmapLimit = QualitySettings.globalTextureMipmapLimit;

            var sourcePipeline = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
            if (sourcePipeline == null)
            {
                sourcePipeline = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            }

            if (sourcePipeline != null)
            {
                playtestPipeline = Instantiate(sourcePipeline);
                playtestPipeline.name = "TopDown3D Playtest URP (Runtime)";
                playtestPipeline.hideFlags = HideFlags.DontSave;
                playtestPipeline.renderScale = RenderScale;
                playtestPipeline.shadowDistance = ShadowDistance;
                playtestPipeline.mainLightShadowmapResolution = ShadowResolution;
                playtestPipeline.shadowCascadeCount = ShadowCascades;
                QualitySettings.renderPipeline = playtestPipeline;
            }

            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TargetFrameRate;
            QualitySettings.globalTextureMipmapLimit = TextureMipmapLimit;
            Shader.EnableKeyword(FastTerrainKeyword);
            stressOverridesApplied = true;
            Debug.Log(
                "[TopDown3D Performance] Stress-test profile enabled: 50% render scale, "
                + "1024px single-cascade shadows, 60 FPS cap, two-level texture mip reduction, "
                + "fast terrain path, a two-chunk terrain ring, no runtime decoration, "
                + "and one generation stage per frame.",
                this);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStreamingRadiusOverride()
        {
            StreamingRadiusOverride = 0;
            DecorationStreamingRadiusOverride = -1;
            ChunksBuiltPerFrameOverride = 0;
        }

        private void Update()
        {
            frameTimes[frameTimeCursor] = Time.unscaledDeltaTime * 1000f;
            frameTimeCursor = (frameTimeCursor + 1) % frameTimes.Length;
            frameTimeCount = Mathf.Min(frameTimeCount + 1, frameTimes.Length);
            FrameTimingManager.CaptureFrameTimings();

            if (Time.unscaledTime < nextReportTime)
            {
                return;
            }

            nextReportTime = Time.unscaledTime + ReportIntervalSeconds;
            ReportTelemetry();
        }

        private void OnDestroy()
        {
            StreamingRadiusOverride = 0;
            DecorationStreamingRadiusOverride = -1;
            ChunksBuiltPerFrameOverride = 0;

            if (!stressOverridesApplied)
            {
                return;
            }

            Shader.DisableKeyword(FastTerrainKeyword);
            QualitySettings.vSyncCount = previousVSyncCount;
            Application.targetFrameRate = previousTargetFrameRate;
            QualitySettings.globalTextureMipmapLimit = previousTextureMipmapLimit;
            if (QualitySettings.renderPipeline == playtestPipeline)
            {
                QualitySettings.renderPipeline = previousQualityPipeline;
            }

            if (playtestPipeline != null)
            {
                Destroy(playtestPipeline);
                playtestPipeline = null;
            }
        }

        private void ReportTelemetry()
        {
            if (world == null)
            {
                world = FindAnyObjectByType<TopDown3DProceduralWorld>();
            }

            Array.Copy(frameTimes, sortedFrameTimes, frameTimeCount);
            Array.Sort(sortedFrameTimes, 0, frameTimeCount);
            var averageMilliseconds = 0f;
            for (var i = 0; i < frameTimeCount; i++)
            {
                averageMilliseconds += frameTimes[i];
            }

            averageMilliseconds = frameTimeCount > 0 ? averageMilliseconds / frameTimeCount : 0f;
            var p95Milliseconds = Percentile(0.95f);
            var p99Milliseconds = Percentile(0.99f);
            var timingCount = FrameTimingManager.GetLatestTimings(1, latestTiming);
            var timing = timingCount > 0 ? latestTiming[0] : default;
            var activePipeline = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
            if (activePipeline == null)
            {
                activePipeline = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            }

            var activeRenderScale = activePipeline != null ? activePipeline.renderScale : 1f;
            var profileMode = fullContentTelemetryMode ? "full-content" : "stress";
            var worldSummary = world == null
                ? "world=not-found"
                : $"chunks={world.LoadedChunkCount} decorated={world.DecoratedChunkCount} "
                    + $"pendingTerrain={world.PendingTerrainChunkCount} pendingDecoration={world.PendingDecorationCount} "
                    + $"renderers={world.TerrainRendererCount + world.DecorationRendererCount} "
                    + $"colliders={world.TerrainColliderCount + world.DecorationColliderCount} "
                    + $"decorationMeshes={world.DecorationMeshCount}";
            Debug.Log(
                $"[TopDown3D Performance] mode={profileMode} "
                + $"fps={(averageMilliseconds > 0f ? 1000f / averageMilliseconds : 0f):F1} "
                + $"frameMs avg={averageMilliseconds:F2} p95={p95Milliseconds:F2} p99={p99Milliseconds:F2} "
                + $"cpu={timing.cpuFrameTime:F2} gpu={timing.gpuFrameTime:F2} "
                + $"resolution={Screen.width}x{Screen.height} scale={activeRenderScale:F2} "
                + $"textureMipLimit={QualitySettings.globalTextureMipmapLimit} "
                + $"managedMB={GC.GetTotalMemory(false) / (1024f * 1024f):F0} {worldSummary}",
                this);
        }

        internal static bool IsFullContentProfileRequested(string[] arguments)
        {
            if (arguments == null)
            {
                return false;
            }

            for (var i = 0; i < arguments.Length; i++)
            {
                if (string.Equals(
                        arguments[i],
                        FullContentProfileArgument,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool ShouldUseFullContentProfile(string[] arguments, bool editorOptIn)
        {
            return editorOptIn || IsFullContentProfileRequested(arguments);
        }

        private static bool IsEditorFullContentProfileEnabled()
        {
#if UNITY_EDITOR
            return UnityEditor.EditorPrefs.GetBool(FullContentEditorPreferenceKey, false);
#else
            return false;
#endif
        }

        private float Percentile(float percentile)
        {
            if (frameTimeCount == 0)
            {
                return 0f;
            }

            var index = Mathf.Clamp(
                Mathf.CeilToInt(frameTimeCount * percentile) - 1,
                0,
                frameTimeCount - 1);
            return sortedFrameTimes[index];
        }
    }
}
