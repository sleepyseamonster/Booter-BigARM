using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace BooterBigArm.TopDown3D
{
    public sealed class TopDown3DRoundLensFlareFeature : ScriptableRendererFeature
    {
        public const RenderPassEvent CanonicalInjectionPoint = RenderPassEvent.BeforeRenderingPostProcessing;
        public const float DefaultIntensity = 0.42f;
        public const float DefaultRadius = 0.24f;
        public const float DefaultAnisotropy = 1.12f;
        public const float DefaultGhostReach = 1.65f;
        public const float DefaultEdgeReach = 0.35f;
        public const float DefaultHaloThickness = 0.18f;
        public const float DefaultHdrEnergy = 2.2f;
        public const float DefaultOcclusionRadius = 0.075f;
        public const float DefaultCoreOcclusion = 1f;
        public const float DefaultAureoleOcclusion = 0.08f;
        public const float DefaultGhostOcclusion = 0.42f;
        public const float DefaultOcclusionStability = 0.82f;
        public static readonly Color DefaultTint = new Color(1f, 0.62f, 0.32f, 1f);

        [SerializeField] private Shader flareShader;
        [SerializeField] private bool flareEnabled = true;
        [SerializeField] private bool previewInSceneView = true;
        [SerializeField, Range(0f, 2f)] private float intensity = DefaultIntensity;
        [SerializeField, Range(0.02f, 0.75f)] private float radius = DefaultRadius;
        [SerializeField, Range(0.4f, 2.5f)] private float anisotropy = DefaultAnisotropy;
        [SerializeField, Range(0f, 3f)] private float ghostReach = DefaultGhostReach;
        [SerializeField, Range(0f, 1f)] private float edgeReach = DefaultEdgeReach;
        [SerializeField, Range(0.03f, 0.5f)] private float haloThickness = DefaultHaloThickness;
        [SerializeField, Range(0.5f, 6f)] private float hdrEnergy = DefaultHdrEnergy;
        [SerializeField, Range(0.005f, 0.25f)] private float occlusionRadius = DefaultOcclusionRadius;
        [SerializeField, Range(0f, 1f)] private float coreOcclusion = DefaultCoreOcclusion;
        [SerializeField, Range(0f, 1f)] private float aureoleOcclusion = DefaultAureoleOcclusion;
        [SerializeField, Range(0f, 1f)] private float ghostOcclusion = DefaultGhostOcclusion;
        [SerializeField, Range(0f, 0.95f)] private float occlusionStability = DefaultOcclusionStability;
        [SerializeField] private Color tint = new Color(1f, 0.62f, 0.32f, 1f);
        [SerializeField] private Texture2D primaryFlareSprite;
        [SerializeField] private Texture2D ghostRingSprite;
        [SerializeField] private Texture2D apertureGhostSprite;

        private Material material;
        private RoundLensFlarePass pass;
        private Light fallbackDirectionalSun;

        public Shader FlareShader => flareShader;
        public bool FlareEnabled => flareEnabled;
        public bool PreviewInSceneView => previewInSceneView;
        public float Intensity => intensity;
        public float Radius => radius;
        public float Anisotropy => anisotropy;
        public float GhostReach => ghostReach;
        public float EdgeReach => edgeReach;
        public float HaloThickness => haloThickness;
        public float HdrEnergy => hdrEnergy;
        public float OcclusionRadius => occlusionRadius;
        public float CoreOcclusion => coreOcclusion;
        public float AureoleOcclusion => aureoleOcclusion;
        public float GhostOcclusion => ghostOcclusion;
        public float OcclusionStability => occlusionStability;
        public Color Tint => tint;
        public Texture2D PrimaryFlareSprite => primaryFlareSprite;
        public Texture2D GhostRingSprite => ghostRingSprite;
        public Texture2D ApertureGhostSprite => apertureGhostSprite;

        public override void Create()
        {
            if (material != null && material.shader != flareShader)
            {
                CoreUtils.Destroy(material);
                material = null;
            }

            if (material == null && flareShader != null)
            {
                material = CoreUtils.CreateEngineMaterial(flareShader);
            }

            pass = new RoundLensFlarePass
            {
                renderPassEvent = CanonicalInjectionPoint,
            };
        }

        public void Configure(
            Shader shader,
            Texture2D primarySprite,
            Texture2D ringSprite,
            Texture2D apertureSprite)
        {
            flareShader = shader;
            flareEnabled = true;
            previewInSceneView = true;
            intensity = DefaultIntensity;
            radius = DefaultRadius;
            anisotropy = DefaultAnisotropy;
            ghostReach = DefaultGhostReach;
            edgeReach = DefaultEdgeReach;
            haloThickness = DefaultHaloThickness;
            hdrEnergy = DefaultHdrEnergy;
            occlusionRadius = DefaultOcclusionRadius;
            coreOcclusion = DefaultCoreOcclusion;
            aureoleOcclusion = DefaultAureoleOcclusion;
            ghostOcclusion = DefaultGhostOcclusion;
            occlusionStability = DefaultOcclusionStability;
            tint = DefaultTint;
            primaryFlareSprite = primarySprite;
            ghostRingSprite = ringSprite;
            apertureGhostSprite = apertureSprite;
            Create();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var cameraData = renderingData.cameraData;
            var cameraType = cameraData.cameraType;
            if (!flareEnabled
                || material == null
                || intensity <= 0f
                || cameraData.renderType != CameraRenderType.Base
                || (cameraType != CameraType.Game && cameraType != CameraType.SceneView)
                || (cameraType == CameraType.SceneView && !previewInSceneView))
            {
                return;
            }

            var directionToSun = ResolveDirectionToSun(out var sunBrightness);
            if (directionToSun.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            var camera = cameraData.camera;
            var sourcePoint = camera.transform.position
                + directionToSun.normalized * (camera.farClipPlane * 0.9f);
            var viewportPoint = camera.WorldToViewportPoint(sourcePoint);
            if (viewportPoint.z <= 0f)
            {
                return;
            }

            pass.Setup(
                material,
                camera,
                cameraData.cameraTargetDescriptor,
                new Vector2(viewportPoint.x, viewportPoint.y),
                camera.aspect,
                Mathf.Clamp(intensity, 0f, 2f),
                Mathf.Clamp(radius, 0.02f, 0.75f),
                Mathf.Clamp(anisotropy, 0.4f, 2.5f),
                Mathf.Clamp(ghostReach, 0f, 3f),
                Mathf.Clamp01(edgeReach),
                Mathf.Clamp(haloThickness, 0.03f, 0.5f),
                Mathf.Clamp(hdrEnergy, 0.5f, 6f),
                sunBrightness,
                Mathf.Clamp(occlusionRadius, 0.005f, 0.25f),
                Mathf.Clamp01(coreOcclusion),
                Mathf.Clamp01(aureoleOcclusion),
                Mathf.Clamp01(ghostOcclusion),
                Mathf.Clamp(occlusionStability, 0f, 0.95f),
                tint,
                primaryFlareSprite,
                ghostRingSprite,
                apertureGhostSprite);
            renderer.EnqueuePass(pass);
        }

        private Vector3 ResolveDirectionToSun(out float sunBrightness)
        {
            var configuredSun = RenderSettings.sun;
            if (IsUsableDirectionalLight(configuredSun))
            {
                sunBrightness = EvaluateSunBrightness(configuredSun);
                return -configuredSun.transform.forward;
            }

            if (PerpetualTwilightSun.Active != null)
            {
                sunBrightness = Mathf.Lerp(0.7f, 1.2f, PerpetualTwilightSun.Active.Brightness01);
                return PerpetualTwilightSun.Active.DirectionToSun;
            }

            if (!IsUsableDirectionalLight(fallbackDirectionalSun))
            {
                fallbackDirectionalSun = FindBrightestDirectionalLight();
            }

            if (fallbackDirectionalSun != null)
            {
                sunBrightness = EvaluateSunBrightness(fallbackDirectionalSun);
                return -fallbackDirectionalSun.transform.forward;
            }

            sunBrightness = 0f;
            return Vector3.zero;
        }

        private static float EvaluateSunBrightness(Light source)
        {
            var peakColor = Mathf.Max(source.color.r, Mathf.Max(source.color.g, source.color.b));
            return Mathf.Clamp(source.intensity * peakColor, 0.35f, 2.5f);
        }

        private static bool IsUsableDirectionalLight(Light candidate)
        {
            return candidate != null
                && candidate.isActiveAndEnabled
                && candidate.type == LightType.Directional;
        }

        private static Light FindBrightestDirectionalLight()
        {
            var lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude);
            Light brightest = null;
            var brightestIntensity = float.MinValue;
            foreach (var candidate in lights)
            {
                if (!IsUsableDirectionalLight(candidate)
                    || candidate.intensity <= brightestIntensity)
                {
                    continue;
                }

                brightest = candidate;
                brightestIntensity = candidate.intensity;
            }

            return brightest;
        }

        protected override void Dispose(bool disposing)
        {
            pass?.ReleaseHistory();
            CoreUtils.Destroy(material);
            material = null;
            pass = null;
        }

        public static float EvaluateOffscreenVisibility(Vector2 viewportPosition, float allowedEdgeReach)
        {
            var outsideX = Mathf.Max(0f, Mathf.Max(-viewportPosition.x, viewportPosition.x - 1f));
            var outsideY = Mathf.Max(0f, Mathf.Max(-viewportPosition.y, viewportPosition.y - 1f));
            var outsideDistance = Mathf.Sqrt(outsideX * outsideX + outsideY * outsideY);
            var reach = Mathf.Max(0.0001f, allowedEdgeReach);
            return 1f - Mathf.SmoothStep(0f, 1f, outsideDistance / reach);
        }

        public static float EvaluateOcclusionResponse(float sampledVisibility, float response)
        {
            return Mathf.Lerp(1f, Mathf.Clamp01(sampledVisibility), Mathf.Clamp01(response));
        }

        public static float EvaluateTemporalOcclusion(float previous, float current, float stability)
        {
            return Mathf.Lerp(
                Mathf.Clamp01(previous),
                Mathf.Clamp01(current),
                1f - Mathf.Clamp(stability, 0f, 0.95f));
        }

        private sealed class RoundLensFlarePass : ScriptableRenderPass
        {
            private const string PassName = "TopDown3D Round Lens Flare";

            private static readonly int SourceId = Shader.PropertyToID("_RoundFlareSource");
            private static readonly int ParamsId = Shader.PropertyToID("_RoundFlareParams");
            private static readonly int OpticsId = Shader.PropertyToID("_RoundFlareOptics");
            private static readonly int TintId = Shader.PropertyToID("_RoundFlareTint");
            private static readonly int EnergyId = Shader.PropertyToID("_RoundFlareEnergy");
            private static readonly int OcclusionId = Shader.PropertyToID("_RoundFlareOcclusion");
            private static readonly int PrimarySpriteId = Shader.PropertyToID("_RoundFlarePrimarySprite");
            private static readonly int GhostRingSpriteId = Shader.PropertyToID("_RoundFlareGhostRingSprite");
            private static readonly int ApertureGhostSpriteId = Shader.PropertyToID("_RoundFlareApertureGhostSprite");
            private static readonly int OcclusionHistoryId = Shader.PropertyToID("_RoundFlareOcclusionHistory");
            private static readonly int TemporalId = Shader.PropertyToID("_RoundFlareTemporal");

            private Material material;
            private Vector2 source;
            private float aspect;
            private float intensity;
            private float radius;
            private float anisotropy;
            private float ghostReach;
            private float edgeReach;
            private float haloThickness;
            private float hdrEnergy;
            private float sunBrightness;
            private float occlusionRadius;
            private float coreOcclusion;
            private float aureoleOcclusion;
            private float ghostOcclusion;
            private float occlusionStability;
            private Color tint;
            private Texture2D primarySprite;
            private Texture2D ringSprite;
            private Texture2D apertureSprite;
            private CameraHistory activeHistory;
            private readonly Dictionary<int, CameraHistory> histories = new();

            public RoundLensFlarePass()
            {
                profilingSampler = new ProfilingSampler("TopDown3D Round Lens Flare");
                requiresIntermediateTexture = true;
                ConfigureInput(ScriptableRenderPassInput.Depth);
            }

            public void Setup(
                Material passMaterial,
                Camera camera,
                RenderTextureDescriptor cameraDescriptor,
                Vector2 viewportSource,
                float cameraAspect,
                float flareIntensity,
                float flareRadius,
                float flareAnisotropy,
                float flareGhostReach,
                float flareEdgeReach,
                float flareHaloThickness,
                float flareHdrEnergy,
                float directionalSunBrightness,
                float flareOcclusionRadius,
                float flareCoreOcclusion,
                float flareAureoleOcclusion,
                float flareGhostOcclusion,
                float flareOcclusionStability,
                Color flareTint,
                Texture2D optionalPrimarySprite,
                Texture2D optionalRingSprite,
                Texture2D optionalApertureSprite)
            {
                material = passMaterial;
                source = viewportSource;
                aspect = cameraAspect;
                intensity = flareIntensity;
                radius = flareRadius;
                anisotropy = flareAnisotropy;
                ghostReach = flareGhostReach;
                edgeReach = flareEdgeReach;
                haloThickness = flareHaloThickness;
                hdrEnergy = flareHdrEnergy;
                sunBrightness = directionalSunBrightness;
                occlusionRadius = flareOcclusionRadius;
                coreOcclusion = flareCoreOcclusion;
                aureoleOcclusion = flareAureoleOcclusion;
                ghostOcclusion = flareGhostOcclusion;
                occlusionStability = flareOcclusionStability;
                tint = flareTint;
                primarySprite = optionalPrimarySprite;
                ringSprite = optionalRingSprite;
                apertureSprite = optionalApertureSprite;

                PrepareHistory(camera, cameraDescriptor, viewportSource);
            }

            private void PrepareHistory(
                Camera camera,
                RenderTextureDescriptor cameraDescriptor,
                Vector2 viewportSource)
            {
                if (camera == null)
                {
                    activeHistory = null;
                    return;
                }

                var cameraId = camera.GetInstanceID();
                if (!histories.TryGetValue(cameraId, out activeHistory))
                {
                    activeHistory = new CameraHistory();
                    histories.Add(cameraId, activeHistory);
                }

                cameraDescriptor.width = 1;
                cameraDescriptor.height = 1;
                cameraDescriptor.msaaSamples = 1;
                cameraDescriptor.depthBufferBits = 0;
                cameraDescriptor.graphicsFormat = GraphicsFormat.R16_SFloat;
                cameraDescriptor.depthStencilFormat = GraphicsFormat.None;
                activeHistory.EnsureAllocated(cameraDescriptor, cameraId);

                var frameGap = Time.frameCount - activeHistory.lastFrame;
                if (frameGap > 1
                    || Vector2.Distance(activeHistory.lastSource, viewportSource) > 0.12f)
                {
                    activeHistory.valid = false;
                }
            }

            public void ReleaseHistory()
            {
                foreach (var history in histories.Values)
                {
                    history.Release();
                }

                histories.Clear();
                activeHistory = null;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var resourceData = frameData.Get<UniversalResourceData>();
                if (material == null
                    || activeHistory == null
                    || resourceData.isActiveTargetBackBuffer
                    || !resourceData.activeColorTexture.IsValid())
                {
                    return;
                }

                var sourceColor = resourceData.activeColorTexture;
                material.SetVector(SourceId, new Vector4(source.x, source.y, aspect, edgeReach));
                material.SetVector(ParamsId, new Vector4(intensity, radius, anisotropy, ghostReach));
                material.SetVector(
                    OpticsId,
                    new Vector4(
                        haloThickness,
                        primarySprite != null ? 1f : 0f,
                        ringSprite != null ? 1f : 0f,
                        apertureSprite != null ? 1f : 0f));
                material.SetColor(TintId, tint);
                material.SetVector(EnergyId, new Vector4(hdrEnergy, sunBrightness, 0f, 0f));
                material.SetVector(
                    OcclusionId,
                    new Vector4(occlusionRadius, coreOcclusion, aureoleOcclusion, ghostOcclusion));
                material.SetTexture(
                    PrimarySpriteId,
                    primarySprite != null ? primarySprite : Texture2D.whiteTexture);
                material.SetTexture(
                    GhostRingSpriteId,
                    ringSprite != null ? ringSprite : Texture2D.whiteTexture);
                material.SetTexture(
                    ApertureGhostSpriteId,
                    apertureSprite != null ? apertureSprite : Texture2D.whiteTexture);

                var previousOcclusion = renderGraph.ImportTexture(activeHistory.read);
                var currentOcclusion = renderGraph.ImportTexture(activeHistory.write);
                RecordOcclusionHistory(
                    renderGraph,
                    resourceData.activeDepthTexture,
                    previousOcclusion,
                    currentOcclusion,
                    activeHistory.valid);

                var destinationDescriptor = sourceColor.GetDescriptor(renderGraph);
                destinationDescriptor.name = "_TopDown3DRoundLensFlareCameraColor";
                destinationDescriptor.clearBuffer = false;
                var destinationColor = renderGraph.CreateTexture(destinationDescriptor);

                using var builder = renderGraph.AddRasterRenderPass<PassData>(
                    PassName,
                    out var passData,
                    profilingSampler);
                passData.material = material;
                passData.sourceColor = sourceColor;
                passData.occlusionHistory = currentOcclusion;
                builder.UseTexture(sourceColor, AccessFlags.Read);
                builder.UseTexture(currentOcclusion, AccessFlags.Read);
                builder.SetRenderAttachment(destinationColor, 0, AccessFlags.Write);
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                {
                    context.cmd.SetGlobalTexture(OcclusionHistoryId, data.occlusionHistory);
                    Blitter.BlitTexture(
                        context.cmd,
                        data.sourceColor,
                        new Vector4(1f, 1f, 0f, 0f),
                        data.material,
                        1);
                });
                resourceData.cameraColor = destinationColor;

                activeHistory.valid = true;
                activeHistory.lastFrame = Time.frameCount;
                activeHistory.lastSource = source;
                activeHistory.Swap();
            }

            private void RecordOcclusionHistory(
                RenderGraph renderGraph,
                TextureHandle depth,
                TextureHandle previous,
                TextureHandle current,
                bool hasHistory)
            {
                using var builder = renderGraph.AddRasterRenderPass<OcclusionPassData>(
                    "TopDown3D Sun Occlusion History",
                    out var passData,
                    profilingSampler);
                passData.material = material;
                passData.previous = previous;
                passData.stability = occlusionStability;
                passData.hasHistory = hasHistory;
                builder.UseTexture(previous, AccessFlags.Read);
                if (depth.IsValid())
                {
                    builder.UseTexture(depth, AccessFlags.Read);
                }

                builder.SetRenderAttachment(current, 0, AccessFlags.Write);
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc(static (OcclusionPassData data, RasterGraphContext context) =>
                {
                    context.cmd.SetGlobalTexture(OcclusionHistoryId, data.previous);
                    context.cmd.SetGlobalVector(
                        TemporalId,
                        new Vector4(data.stability, data.hasHistory ? 1f : 0f, 0f, 0f));
                    Blitter.BlitTexture(
                        context.cmd,
                        data.previous,
                        new Vector4(1f, 1f, 0f, 0f),
                        data.material,
                        0);
                });
            }

            private sealed class PassData
            {
                public Material material;
                public TextureHandle sourceColor;
                public TextureHandle occlusionHistory;
            }

            private sealed class OcclusionPassData
            {
                public Material material;
                public TextureHandle previous;
                public float stability;
                public bool hasHistory;
            }

            private sealed class CameraHistory
            {
                public RTHandle read;
                public RTHandle write;
                public bool valid;
                public int lastFrame = -1;
                public Vector2 lastSource;

                public void EnsureAllocated(RenderTextureDescriptor descriptor, int cameraId)
                {
                    var changed = RenderingUtils.ReAllocateHandleIfNeeded(
                        ref read,
                        descriptor,
                        FilterMode.Bilinear,
                        TextureWrapMode.Clamp,
                        name: $"_TopDown3DFlareOcclusionHistoryA_{cameraId}");
                    changed |= RenderingUtils.ReAllocateHandleIfNeeded(
                        ref write,
                        descriptor,
                        FilterMode.Bilinear,
                        TextureWrapMode.Clamp,
                        name: $"_TopDown3DFlareOcclusionHistoryB_{cameraId}");
                    if (changed)
                    {
                        valid = false;
                    }
                }

                public void Swap()
                {
                    (read, write) = (write, read);
                }

                public void Release()
                {
                    read?.Release();
                    write?.Release();
                    read = null;
                    write = null;
                    valid = false;
                }
            }
        }
    }
}
