using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace BooterBigArm.TopDown3D
{
    public sealed class TopDown3DTiltShiftFeature : ScriptableRendererFeature
    {
        public const RenderPassEvent CanonicalInjectionPoint = RenderPassEvent.BeforeRenderingPostProcessing;
        public const int DefaultDownsample = 2;
        public const float DefaultFocusCenter = 0.5f;
        public const float DefaultSharpBandWidth = 0.3f;
        public const float DefaultFeatherWidth = 0.28f;
        public const float DefaultBlurRadius = 7f;

        [SerializeField] private Shader tiltShiftShader;
        [SerializeField] private bool previewInSceneView = true;
        [SerializeField, Range(1, 4)] private int downsample = DefaultDownsample;
        [SerializeField, Range(0f, 1f)] private float focusCenter = DefaultFocusCenter;
        [SerializeField, Range(0.05f, 0.8f)] private float sharpBandWidth = DefaultSharpBandWidth;
        [FormerlySerializedAs("transitionWidth")]
        [SerializeField, Range(0.01f, 0.5f)]
        [Tooltip("Screen-height distance used to fade smoothly from the sharp band into full blur. Larger values create a broader, softer gradient.")]
        private float featherWidth = DefaultFeatherWidth;
        [SerializeField, Range(0.5f, 12f)] private float blurRadius = DefaultBlurRadius;

        private Material material;
        private TiltShiftPass pass;

        public Shader TiltShiftShader => tiltShiftShader;
        public bool PreviewInSceneView => previewInSceneView;
        public int Downsample => downsample;
        public float FocusCenter => focusCenter;
        public float SharpBandWidth => sharpBandWidth;
        public float FeatherWidth => featherWidth;
        public float BlurRadius => blurRadius;

        public override void Create()
        {
            if (material != null && material.shader != tiltShiftShader)
            {
                CoreUtils.Destroy(material);
                material = null;
            }

            if (material == null && tiltShiftShader != null)
            {
                material = CoreUtils.CreateEngineMaterial(tiltShiftShader);
            }

            pass = new TiltShiftPass
            {
                renderPassEvent = CanonicalInjectionPoint,
            };
        }

        public void Configure(Shader shader)
        {
            tiltShiftShader = shader;
            previewInSceneView = true;
            downsample = DefaultDownsample;
            focusCenter = DefaultFocusCenter;
            sharpBandWidth = DefaultSharpBandWidth;
            featherWidth = DefaultFeatherWidth;
            blurRadius = DefaultBlurRadius;
            Create();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!TopDown3DSceneRenderingOverride.AuthoringEffectsEnabled(renderingData.cameraData.camera)) return;
            var cameraData = renderingData.cameraData;
            var cameraType = cameraData.cameraType;
            var scene = TopDown3DSceneRenderingOverride.RenderingScene(cameraData.camera);
            if (TopDown3DSceneRenderingOverride.TryGetAtmosphereProfile(cameraData.camera, out var selectedProfile)
                && SuppressArtisticBlur(selectedProfile)) return;
#if UNITY_EDITOR
            // Editor Scene cameras are utility objects; their rendered authoring scene is active.
            if (cameraType == CameraType.SceneView) scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
#endif
            if (TopDown3DSceneRenderingOverride.TiltShiftDisabledIn(scene)) return;
            if (material == null
                || cameraData.renderType != CameraRenderType.Base
                || (cameraType != CameraType.Game && cameraType != CameraType.SceneView)
                || (cameraType == CameraType.SceneView && !previewInSceneView))
            {
                return;
            }

            pass.Setup(
                material,
                Mathf.Clamp(downsample, 1, 4),
                Mathf.Clamp01(focusCenter),
                Mathf.Clamp(sharpBandWidth, 0.05f, 0.8f),
                Mathf.Clamp(featherWidth, 0.01f, 0.5f),
                Mathf.Clamp(blurRadius, 0.5f, 12f));
            if (TopDown3DSceneRenderingOverride.TryGetAtmosphereProfile(cameraData.camera, out var profile)
                && profile.useReferenceResolutionTiltShift)
            {
                if (profile.tiltShiftBlurRadiusPixels <= 0f) return;
                pass.SetupReferenceResolution(profile);
            }
            renderer.EnqueuePass(pass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(material);
            material = null;
            pass = null;
        }

        public static float EvaluateBlurWeight(
            float screenY,
            float bandCenter,
            float bandWidth,
            float featherWidth)
        {
            var halfBand = Mathf.Max(0f, bandWidth) * 0.5f;
            var distanceOutsideBand = Mathf.Max(0f, Mathf.Abs(screenY - bandCenter) - halfBand);
            return Mathf.SmoothStep(
                0f,
                1f,
                distanceOutsideBand / Mathf.Max(0.0001f, featherWidth));
        }

        // Radius is the separation of adjacent kernel taps at the reference height.
        // The outermost tap is four steps away. Downsampling changes filtering quality,
        // not this UV radius; at 1080 reference height both axes have the same pixel reach.
        public static Vector2 CalculateReferenceBlurStep(float tapStepPixels, float referenceHeight, float aspect)
        {
            var step = Mathf.Clamp(tapStepPixels, 0f, 24f) / Mathf.Clamp(referenceHeight, 360f, 4320f);
            return new Vector2(step / Mathf.Max(0.0001f, aspect), step);
        }

        public static bool SuppressArtisticBlur(TopDown3DAtmosphereStudyProfile profile)
            => profile != null && profile.nativeOptics != null
                && (!profile.nativeOptics.IsValid || !profile.nativeOptics.opticsEnabled
                    || profile.nativeOptics.depthMode != DepthOfFieldMode.Off);

        public static float EvaluateReferenceBlurWeight(Vector2 uv, float aspect, float bandAngleDegrees,
            float bandCenter, float bandWidth, float featherWidth)
        {
            var angle = Mathf.Clamp(bandAngleDegrees, -90f, 90f) * Mathf.Deg2Rad;
            var relative = new Vector2((uv.x - .5f) * Mathf.Max(.0001f, aspect), uv.y - .5f);
            var bandCoordinate = -Mathf.Sin(angle) * relative.x + Mathf.Cos(angle) * relative.y + .5f;
            return EvaluateBlurWeight(bandCoordinate, Mathf.Clamp01(bandCenter),
                Mathf.Clamp(bandWidth, .05f, .8f), Mathf.Clamp(featherWidth, .01f, .5f));
        }

        private sealed class TiltShiftPass : ScriptableRenderPass
        {
            private const string HorizontalPassName = "TopDown3D Tilt Shift Horizontal";
            private const string CompositePassName = "TopDown3D Tilt Shift Composite";

            private static readonly int TiltShiftBlurTextureId =
                Shader.PropertyToID("_TiltShiftBlurTexture");
            private static readonly int TiltShiftBlurTexelSizeId =
                Shader.PropertyToID("_TiltShiftBlurTexelSize");
            private static readonly int TiltShiftParamsId = Shader.PropertyToID("_TiltShiftParams");
            private static readonly int TiltShiftModeParamsId = Shader.PropertyToID("_TiltShiftModeParams");
            private static readonly int TiltShiftReferenceBlurStepId = Shader.PropertyToID("_TiltShiftReferenceBlurStep");

            private Material material;
            private int downsample;
            private float focusCenter;
            private float sharpBandWidth;
            private float featherWidth;
            private float blurRadius;
            private bool referenceResolution;
            private float referenceHeight;
            private float bandAngleDegrees;

            public TiltShiftPass()
            {
                profilingSampler = new ProfilingSampler("TopDown3D Tilt Shift");
                requiresIntermediateTexture = true;
            }

            public void Setup(
                Material passMaterial,
                int renderDownsample,
                float bandCenter,
                float bandWidth,
                float featherWidth,
                float maximumBlurRadius)
            {
                material = passMaterial;
                downsample = renderDownsample;
                focusCenter = bandCenter;
                sharpBandWidth = bandWidth;
                this.featherWidth = featherWidth;
                blurRadius = maximumBlurRadius;
                referenceResolution = false;
                bandAngleDegrees = 0f;
            }

            public void SetupReferenceResolution(TopDown3DAtmosphereStudyProfile profile)
            {
                referenceResolution = true;
                downsample = Mathf.Clamp(profile.tiltShiftDownsample, 1, 4);
                referenceHeight = Mathf.Clamp(profile.tiltShiftReferenceHeight, 360f, 4320f);
                blurRadius = Mathf.Clamp(profile.tiltShiftBlurRadiusPixels, 0f, 24f);
                bandAngleDegrees = Mathf.Clamp(profile.tiltShiftBandAngleDegrees, -90f, 90f);
                focusCenter = Mathf.Clamp01(profile.tiltShiftFocusCenter);
                sharpBandWidth = Mathf.Clamp(profile.tiltShiftSharpBandWidth, .05f, .8f);
                featherWidth = Mathf.Clamp(profile.tiltShiftFeatherWidth, .01f, .5f);
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var resourceData = frameData.Get<UniversalResourceData>();
                var cameraData = frameData.Get<UniversalCameraData>();
                if (material == null
                    || resourceData.isActiveTargetBackBuffer
                    || !resourceData.activeColorTexture.IsValid())
                {
                    return;
                }

                var sourceColor = resourceData.activeColorTexture;
                var cameraDescriptor = cameraData.cameraTargetDescriptor;
                var blurWidth = Mathf.Max(1, cameraDescriptor.width / downsample);
                var blurHeight = Mathf.Max(1, cameraDescriptor.height / downsample);
                var blurDescriptor = sourceColor.GetDescriptor(renderGraph);
                blurDescriptor.name = "_TopDown3DTiltShiftHorizontal";
                blurDescriptor.width = blurWidth;
                blurDescriptor.height = blurHeight;
                blurDescriptor.clearBuffer = false;
                blurDescriptor.filterMode = FilterMode.Bilinear;
                blurDescriptor.wrapMode = TextureWrapMode.Clamp;
                blurDescriptor.msaaSamples = MSAASamples.None;
                var horizontalBlur = renderGraph.CreateTexture(blurDescriptor);

                var parameters = new ShaderParameters
                {
                    band = new Vector4(focusCenter, sharpBandWidth, featherWidth, blurRadius),
                    blurTexels = new Vector4(1f / blurWidth, 1f / blurHeight, blurWidth, blurHeight),
                };
                var aspect = cameraDescriptor.width / (float)Mathf.Max(1, cameraDescriptor.height);
                var angle = bandAngleDegrees * Mathf.Deg2Rad;
                parameters.mode = new Vector4(referenceResolution ? 1f : 0f, aspect, Mathf.Cos(angle), Mathf.Sin(angle));
                var referenceStep = CalculateReferenceBlurStep(blurRadius, referenceHeight, aspect);
                parameters.referenceStep = new Vector4(referenceStep.x, referenceStep.y, 0f, 0f);

                RecordHorizontalPass(renderGraph, sourceColor, horizontalBlur, parameters);

                var destinationDescriptor = sourceColor.GetDescriptor(renderGraph);
                destinationDescriptor.name = "_TopDown3DTiltShiftCameraColor";
                destinationDescriptor.clearBuffer = false;
                var destinationColor = renderGraph.CreateTexture(destinationDescriptor);
                RecordCompositePass(renderGraph, sourceColor, horizontalBlur, destinationColor, parameters);
                resourceData.cameraColor = destinationColor;
            }

            private void RecordHorizontalPass(
                RenderGraph renderGraph,
                TextureHandle sourceColor,
                TextureHandle horizontalBlur,
                ShaderParameters parameters)
            {
                using var builder = renderGraph.AddRasterRenderPass<HorizontalPassData>(
                    HorizontalPassName,
                    out var passData,
                    profilingSampler);
                passData.material = material;
                passData.sourceColor = sourceColor;
                passData.parameters = parameters;
                builder.UseTexture(sourceColor, AccessFlags.Read);
                builder.SetRenderAttachment(horizontalBlur, 0, AccessFlags.Write);
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc(static (HorizontalPassData data, RasterGraphContext context) =>
                {
                    ApplyShaderParameters(context, data.parameters);
                    Blitter.BlitTexture(
                        context.cmd,
                        data.sourceColor,
                        new Vector4(1f, 1f, 0f, 0f),
                        data.material,
                        0);
                });
            }

            private void RecordCompositePass(
                RenderGraph renderGraph,
                TextureHandle sourceColor,
                TextureHandle horizontalBlur,
                TextureHandle destinationColor,
                ShaderParameters parameters)
            {
                using var builder = renderGraph.AddRasterRenderPass<CompositePassData>(
                    CompositePassName,
                    out var passData,
                    profilingSampler);
                passData.material = material;
                passData.sourceColor = sourceColor;
                passData.horizontalBlur = horizontalBlur;
                passData.parameters = parameters;
                builder.UseTexture(sourceColor, AccessFlags.Read);
                builder.UseTexture(horizontalBlur, AccessFlags.Read);
                builder.SetRenderAttachment(destinationColor, 0, AccessFlags.Write);
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc(static (CompositePassData data, RasterGraphContext context) =>
                {
                    ApplyShaderParameters(context, data.parameters);
                    context.cmd.SetGlobalTexture(TiltShiftBlurTextureId, data.horizontalBlur);
                    Blitter.BlitTexture(
                        context.cmd,
                        data.sourceColor,
                        new Vector4(1f, 1f, 0f, 0f),
                        data.material,
                        1);
                });
            }

            private sealed class HorizontalPassData
            {
                public Material material;
                public TextureHandle sourceColor;
                public ShaderParameters parameters;
            }

            private sealed class CompositePassData
            {
                public Material material;
                public TextureHandle sourceColor;
                public TextureHandle horizontalBlur;
                public ShaderParameters parameters;
            }

            private struct ShaderParameters
            {
                public Vector4 band, blurTexels, mode, referenceStep;
            }

            private static void ApplyShaderParameters(RasterGraphContext context, ShaderParameters parameters)
            {
                // Capture values in each graph pass rather than mutating one material while
                // recording multiple cameras. Command-buffer values follow their own draw.
                context.cmd.SetGlobalVector(TiltShiftParamsId, parameters.band);
                context.cmd.SetGlobalVector(TiltShiftBlurTexelSizeId, parameters.blurTexels);
                context.cmd.SetGlobalVector(TiltShiftModeParamsId, parameters.mode);
                context.cmd.SetGlobalVector(TiltShiftReferenceBlurStepId, parameters.referenceStep);
            }
        }
    }
}
