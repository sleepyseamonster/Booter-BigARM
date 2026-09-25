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
            var cameraData = renderingData.cameraData;
            var cameraType = cameraData.cameraType;
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

        private sealed class TiltShiftPass : ScriptableRenderPass
        {
            private const string HorizontalPassName = "TopDown3D Tilt Shift Horizontal";
            private const string CompositePassName = "TopDown3D Tilt Shift Composite";

            private static readonly int TiltShiftBlurTextureId =
                Shader.PropertyToID("_TiltShiftBlurTexture");
            private static readonly int TiltShiftBlurTexelSizeId =
                Shader.PropertyToID("_TiltShiftBlurTexelSize");
            private static readonly int TiltShiftParamsId = Shader.PropertyToID("_TiltShiftParams");

            private Material material;
            private int downsample;
            private float focusCenter;
            private float sharpBandWidth;
            private float featherWidth;
            private float blurRadius;

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

                material.SetVector(
                    TiltShiftParamsId,
                    new Vector4(focusCenter, sharpBandWidth, featherWidth, blurRadius));
                material.SetVector(
                    TiltShiftBlurTexelSizeId,
                    new Vector4(1f / blurWidth, 1f / blurHeight, blurWidth, blurHeight));

                RecordHorizontalPass(renderGraph, sourceColor, horizontalBlur);

                var destinationDescriptor = sourceColor.GetDescriptor(renderGraph);
                destinationDescriptor.name = "_TopDown3DTiltShiftCameraColor";
                destinationDescriptor.clearBuffer = false;
                var destinationColor = renderGraph.CreateTexture(destinationDescriptor);
                RecordCompositePass(renderGraph, sourceColor, horizontalBlur, destinationColor);
                resourceData.cameraColor = destinationColor;
            }

            private void RecordHorizontalPass(
                RenderGraph renderGraph,
                TextureHandle sourceColor,
                TextureHandle horizontalBlur)
            {
                using var builder = renderGraph.AddRasterRenderPass<HorizontalPassData>(
                    HorizontalPassName,
                    out var passData,
                    profilingSampler);
                passData.material = material;
                passData.sourceColor = sourceColor;
                builder.UseTexture(sourceColor, AccessFlags.Read);
                builder.SetRenderAttachment(horizontalBlur, 0, AccessFlags.Write);
                builder.SetRenderFunc(static (HorizontalPassData data, RasterGraphContext context) =>
                {
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
                TextureHandle destinationColor)
            {
                using var builder = renderGraph.AddRasterRenderPass<CompositePassData>(
                    CompositePassName,
                    out var passData,
                    profilingSampler);
                passData.material = material;
                passData.sourceColor = sourceColor;
                passData.horizontalBlur = horizontalBlur;
                builder.UseTexture(sourceColor, AccessFlags.Read);
                builder.UseTexture(horizontalBlur, AccessFlags.Read);
                builder.SetRenderAttachment(destinationColor, 0, AccessFlags.Write);
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc(static (CompositePassData data, RasterGraphContext context) =>
                {
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
            }

            private sealed class CompositePassData
            {
                public Material material;
                public TextureHandle sourceColor;
                public TextureHandle horizontalBlur;
            }
        }
    }
}
