using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace BooterBigArm.TopDown3D
{
    public sealed class TopDown3DAnamorphicStreakFeature : ScriptableRendererFeature
    {
        public const RenderPassEvent CanonicalInjectionPoint = RenderPassEvent.BeforeRenderingPostProcessing;
        public const int DefaultDownsample = 4;
        public const float DefaultIntensity = 0.55f;
        public const float DefaultLength = 0.62f;
        public const float DefaultThreshold = 0.88f;
        public const float DefaultOrientation = 0f;
        public const float DefaultChromaticSeparation = 0.035f;
        public static readonly Color DefaultTint = new Color(1f, 0.82f, 0.68f, 1f);

        [SerializeField] private Shader streakShader;
        [SerializeField] private bool streakEnabled = true;
        [SerializeField] private bool previewInSceneView = true;
        [SerializeField, Range(1, 4)] private int downsample = DefaultDownsample;
        [SerializeField, Range(0f, 2f)] private float intensity = DefaultIntensity;
        [SerializeField, Range(0f, 1f)] private float length = DefaultLength;
        [SerializeField, Range(0f, 4f)] private float threshold = DefaultThreshold;
        [SerializeField, Range(-180f, 180f)] private float orientation = DefaultOrientation;
        [SerializeField, Range(0f, 0.15f)] private float chromaticSeparation = DefaultChromaticSeparation;
        [SerializeField] private Color tint = new Color(1f, 0.82f, 0.68f, 1f);

        private Material material;
        private AnamorphicStreakPass pass;

        public Shader StreakShader => streakShader;
        public bool StreakEnabled => streakEnabled;
        public bool PreviewInSceneView => previewInSceneView;
        public int Downsample => downsample;
        public float Intensity => intensity;
        public float Length => length;
        public float Threshold => threshold;
        public float Orientation => orientation;
        public float ChromaticSeparation => chromaticSeparation;
        public Color Tint => tint;

        public override void Create()
        {
            if (material != null && material.shader != streakShader)
            {
                CoreUtils.Destroy(material);
                material = null;
            }

            if (material == null && streakShader != null)
            {
                material = CoreUtils.CreateEngineMaterial(streakShader);
            }

            pass = new AnamorphicStreakPass
            {
                renderPassEvent = CanonicalInjectionPoint,
            };
        }

        public void Configure(Shader shader)
        {
            streakShader = shader;
            streakEnabled = true;
            previewInSceneView = true;
            downsample = DefaultDownsample;
            intensity = DefaultIntensity;
            length = DefaultLength;
            threshold = DefaultThreshold;
            orientation = DefaultOrientation;
            chromaticSeparation = DefaultChromaticSeparation;
            tint = DefaultTint;
            Create();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var cameraData = renderingData.cameraData;
            var cameraType = cameraData.cameraType;
            if (!streakEnabled
                || material == null
                || intensity <= 0f
                || cameraData.renderType != CameraRenderType.Base
                || (cameraType != CameraType.Game && cameraType != CameraType.SceneView)
                || (cameraType == CameraType.SceneView && !previewInSceneView))
            {
                return;
            }

            var radians = orientation * Mathf.Deg2Rad;
            pass.Setup(
                material,
                Mathf.Clamp(downsample, 1, 4),
                Mathf.Clamp(intensity, 0f, 2f),
                Mathf.Clamp01(length),
                Mathf.Max(0f, threshold),
                new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)),
                Mathf.Clamp(chromaticSeparation, 0f, 0.15f),
                tint);
            renderer.EnqueuePass(pass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(material);
            material = null;
            pass = null;
        }

        public static float EvaluateHighlightWeight(float brightness, float highlightThreshold)
        {
            var lower = Mathf.Max(0f, highlightThreshold);
            var t = Mathf.InverseLerp(lower, lower + 0.12f, Mathf.Max(0f, brightness));
            return Mathf.SmoothStep(0f, 1f, t);
        }

        private sealed class AnamorphicStreakPass : ScriptableRenderPass
        {
            private const string ExtractPassName = "TopDown3D Anamorphic Streak Extract";
            private const string CompositePassName = "TopDown3D Anamorphic Streak Composite";

            private static readonly int StreakTextureId = Shader.PropertyToID("_AnamorphicStreakTexture");
            private static readonly int StreakTexelSizeId = Shader.PropertyToID("_AnamorphicStreakTexelSize");
            private static readonly int StreakParamsId = Shader.PropertyToID("_AnamorphicStreakParams");
            private static readonly int StreakDirectionId = Shader.PropertyToID("_AnamorphicStreakDirection");
            private static readonly int StreakTintId = Shader.PropertyToID("_AnamorphicStreakTint");

            private Material material;
            private int downsample;
            private float intensity;
            private float length;
            private float threshold;
            private Vector2 direction;
            private float chromaticSeparation;
            private Color tint;

            public AnamorphicStreakPass()
            {
                profilingSampler = new ProfilingSampler("TopDown3D Anamorphic Streak");
                requiresIntermediateTexture = true;
            }

            public void Setup(
                Material passMaterial,
                int renderDownsample,
                float streakIntensity,
                float streakLength,
                float highlightThreshold,
                Vector2 streakDirection,
                float colorSeparation,
                Color streakTint)
            {
                material = passMaterial;
                downsample = renderDownsample;
                intensity = streakIntensity;
                length = streakLength;
                threshold = highlightThreshold;
                direction = streakDirection;
                chromaticSeparation = colorSeparation;
                tint = streakTint;
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
                var streakWidth = Mathf.Max(1, cameraDescriptor.width / downsample);
                var streakHeight = Mathf.Max(1, cameraDescriptor.height / downsample);
                var streakDescriptor = sourceColor.GetDescriptor(renderGraph);
                streakDescriptor.name = "_TopDown3DAnamorphicStreak";
                streakDescriptor.width = streakWidth;
                streakDescriptor.height = streakHeight;
                streakDescriptor.clearBuffer = false;
                streakDescriptor.filterMode = FilterMode.Bilinear;
                streakDescriptor.wrapMode = TextureWrapMode.Clamp;
                streakDescriptor.msaaSamples = MSAASamples.None;
                var streakTexture = renderGraph.CreateTexture(streakDescriptor);

                material.SetVector(
                    StreakParamsId,
                    new Vector4(threshold, intensity, length, chromaticSeparation));
                material.SetVector(
                    StreakDirectionId,
                    new Vector4(direction.x, direction.y, 0f, 0f));
                material.SetVector(
                    StreakTexelSizeId,
                    new Vector4(1f / streakWidth, 1f / streakHeight, streakWidth, streakHeight));
                material.SetColor(StreakTintId, tint);

                RecordExtractPass(renderGraph, sourceColor, streakTexture);

                var destinationDescriptor = sourceColor.GetDescriptor(renderGraph);
                destinationDescriptor.name = "_TopDown3DAnamorphicCameraColor";
                destinationDescriptor.clearBuffer = false;
                var destinationColor = renderGraph.CreateTexture(destinationDescriptor);
                RecordCompositePass(renderGraph, sourceColor, streakTexture, destinationColor);
                resourceData.cameraColor = destinationColor;
            }

            private void RecordExtractPass(
                RenderGraph renderGraph,
                TextureHandle sourceColor,
                TextureHandle streakTexture)
            {
                using var builder = renderGraph.AddRasterRenderPass<ExtractPassData>(
                    ExtractPassName,
                    out var passData,
                    profilingSampler);
                passData.material = material;
                passData.sourceColor = sourceColor;
                builder.UseTexture(sourceColor, AccessFlags.Read);
                builder.SetRenderAttachment(streakTexture, 0, AccessFlags.Write);
                builder.SetRenderFunc(static (ExtractPassData data, RasterGraphContext context) =>
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
                TextureHandle streakTexture,
                TextureHandle destinationColor)
            {
                using var builder = renderGraph.AddRasterRenderPass<CompositePassData>(
                    CompositePassName,
                    out var passData,
                    profilingSampler);
                passData.material = material;
                passData.sourceColor = sourceColor;
                passData.streakTexture = streakTexture;
                builder.UseTexture(sourceColor, AccessFlags.Read);
                builder.UseTexture(streakTexture, AccessFlags.Read);
                builder.SetRenderAttachment(destinationColor, 0, AccessFlags.Write);
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc(static (CompositePassData data, RasterGraphContext context) =>
                {
                    context.cmd.SetGlobalTexture(StreakTextureId, data.streakTexture);
                    Blitter.BlitTexture(
                        context.cmd,
                        data.sourceColor,
                        new Vector4(1f, 1f, 0f, 0f),
                        data.material,
                        1);
                });
            }

            private sealed class ExtractPassData
            {
                public Material material;
                public TextureHandle sourceColor;
            }

            private sealed class CompositePassData
            {
                public Material material;
                public TextureHandle sourceColor;
                public TextureHandle streakTexture;
            }
        }
    }
}
