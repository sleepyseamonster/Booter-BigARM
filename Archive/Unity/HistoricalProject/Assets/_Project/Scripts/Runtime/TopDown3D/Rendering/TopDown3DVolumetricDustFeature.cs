using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace BooterBigArm.TopDown3D
{
    public sealed class TopDown3DVolumetricDustFeature : ScriptableRendererFeature
    {
        public const RenderPassEvent CanonicalInjectionPoint = RenderPassEvent.BeforeRenderingTransparents;

        [SerializeField] private Shader volumetricShader;
        [SerializeField, Range(1, 4)] private int downsample = 2;
        [SerializeField, Range(4, 32)] private int raymarchSteps = 16;
        [SerializeField, Range(1, 16)] private int shadowSamples = 8;
        [SerializeField, Range(8f, 192f)] private float depthEdgeSharpness = 96f;

        private Material material;
        private VolumetricDustPass pass;

        public Shader VolumetricShader => volumetricShader;
        public int Downsample => downsample;
        public int RaymarchSteps => raymarchSteps;
        public int ShadowSamples => shadowSamples;
        public float DepthEdgeSharpness => depthEdgeSharpness;

        public override void Create()
        {
            if (material != null && material.shader != volumetricShader)
            {
                CoreUtils.Destroy(material);
                material = null;
            }

            if (material == null && volumetricShader != null)
            {
                material = CoreUtils.CreateEngineMaterial(volumetricShader);
            }

            pass = new VolumetricDustPass
            {
                renderPassEvent = CanonicalInjectionPoint,
            };
        }

        public void Configure(Shader shader)
        {
            volumetricShader = shader;
            Create();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var cameraData = renderingData.cameraData;
            var camera = cameraData.camera;
            if (material == null
                || camera == null
                || cameraData.cameraType != CameraType.Game
                || cameraData.renderType != CameraRenderType.Base
                || TopDown3DDustAtmosphere.Active == null
                || !TopDown3DDustAtmosphere.Active.TryGetVolumetricRenderState(camera, out var state))
            {
                return;
            }

            pass.Setup(
                material,
                state,
                Mathf.Clamp(downsample, 1, 4),
                Mathf.Clamp(raymarchSteps, 4, 32),
                Mathf.Clamp(shadowSamples, 1, 16),
                Mathf.Clamp(depthEdgeSharpness, 8f, 192f));
            renderer.EnqueuePass(pass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(material);
            material = null;
            pass = null;
        }

        private sealed class VolumetricDustPass : ScriptableRenderPass
        {
            private const string ScatterPassName = "TopDown3D Volumetric Dust Raymarch";
            private const string CompositePassName = "TopDown3D Volumetric Dust Composite";

            private static readonly int DensityMapId = Shader.PropertyToID("_DustDensityMap");
            private static readonly int DensityMapParamsId = Shader.PropertyToID("_DustDensityMapParams");
            private static readonly int DustTintId = Shader.PropertyToID("_DustTint");
            private static readonly int DustOpticsId = Shader.PropertyToID("_DustOptics");
            private static readonly int DustMaximumForwardPhaseId =
                Shader.PropertyToID("_DustMaximumForwardPhase");
            private static readonly int DustMarchId = Shader.PropertyToID("_DustMarch");
            private static readonly int DustScatteringTextureId = Shader.PropertyToID("_DustScatteringTexture");
            private static readonly int DustScatteringTexelSizeId = Shader.PropertyToID("_DustScatteringTexelSize");

            private Material material;
            private TopDown3DDustAtmosphere.VolumetricRenderState state;
            private int downsample;
            private int raymarchSteps;
            private int shadowSamples;
            private float depthEdgeSharpness;

            public VolumetricDustPass()
            {
                profilingSampler = new ProfilingSampler("TopDown3D Volumetric Dust");
                ConfigureInput(ScriptableRenderPassInput.Depth);
                requiresIntermediateTexture = true;
            }

            public void Setup(
                Material passMaterial,
                TopDown3DDustAtmosphere.VolumetricRenderState renderState,
                int renderDownsample,
                int steps,
                int sampledShadows,
                float edgeSharpness)
            {
                material = passMaterial;
                state = renderState;
                downsample = renderDownsample;
                raymarchSteps = steps;
                shadowSamples = sampledShadows;
                depthEdgeSharpness = edgeSharpness;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var resourceData = frameData.Get<UniversalResourceData>();
                var cameraData = frameData.Get<UniversalCameraData>();
                if (material == null
                    || state.DensityMap == null
                    || resourceData.isActiveTargetBackBuffer
                    || !resourceData.cameraDepthTexture.IsValid()
                    || !resourceData.activeColorTexture.IsValid())
                {
                    return;
                }

                var cameraDescriptor = cameraData.cameraTargetDescriptor;
                var scatteringWidth = Mathf.Max(1, cameraDescriptor.width / downsample);
                var scatteringHeight = Mathf.Max(1, cameraDescriptor.height / downsample);
                var scatteringDescriptor = new TextureDesc(scatteringWidth, scatteringHeight)
                {
                    name = "_TopDown3DVolumetricDustScattering",
                    colorFormat = GraphicsFormat.R16G16B16A16_SFloat,
                    clearBuffer = true,
                    clearColor = new Color(0f, 0f, 0f, 1f),
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    msaaSamples = MSAASamples.None,
                };
                var scatteringTexture = renderGraph.CreateTexture(scatteringDescriptor);

                ApplyMaterialState(scatteringWidth, scatteringHeight);
                RecordScatterPass(renderGraph, resourceData.cameraDepthTexture, scatteringTexture);

                var sourceColor = resourceData.activeColorTexture;
                var destinationDescriptor = sourceColor.GetDescriptor(renderGraph);
                destinationDescriptor.name = "_TopDown3DVolumetricDustCameraColor";
                destinationDescriptor.clearBuffer = false;
                var destinationColor = renderGraph.CreateTexture(destinationDescriptor);
                RecordCompositePass(
                    renderGraph,
                    sourceColor,
                    resourceData.cameraDepthTexture,
                    scatteringTexture,
                    destinationColor);
                resourceData.cameraColor = destinationColor;
            }

            private void ApplyMaterialState(int scatteringWidth, int scatteringHeight)
            {
                var shadowStride = Mathf.Max(1, Mathf.CeilToInt(raymarchSteps / (float)shadowSamples));
                material.SetTexture(DensityMapId, state.DensityMap);
                material.SetVector(DensityMapParamsId, state.DensityMapParams);
                material.SetColor(DustTintId, state.Tint);
                material.SetVector(
                    DustOpticsId,
                    new Vector4(
                        state.ExtinctionAtIntensityOne,
                        state.ScatteringAlbedo,
                        state.Anisotropy,
                        state.AmbientScattering));
                material.SetFloat(DustMaximumForwardPhaseId, state.MaximumForwardPhase);
                material.SetVector(
                    DustMarchId,
                    new Vector4(state.MaximumMarchDistance, raymarchSteps, shadowStride, depthEdgeSharpness));
                material.SetVector(
                    DustScatteringTexelSizeId,
                    new Vector4(
                        1f / scatteringWidth,
                        1f / scatteringHeight,
                        scatteringWidth,
                        scatteringHeight));
            }

            private void RecordScatterPass(
                RenderGraph renderGraph,
                TextureHandle depthTexture,
                TextureHandle scatteringTexture)
            {
                using var builder = renderGraph.AddRasterRenderPass<ScatterPassData>(
                    ScatterPassName,
                    out var passData,
                    profilingSampler);
                passData.material = material;
                passData.depthTexture = depthTexture;
                builder.UseTexture(depthTexture, AccessFlags.Read);
                builder.SetRenderAttachment(scatteringTexture, 0, AccessFlags.Write);
                builder.SetRenderFunc(static (ScatterPassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(
                        context.cmd,
                        data.depthTexture,
                        new Vector4(1f, 1f, 0f, 0f),
                        data.material,
                        0);
                });
            }

            private void RecordCompositePass(
                RenderGraph renderGraph,
                TextureHandle sourceColor,
                TextureHandle depthTexture,
                TextureHandle scatteringTexture,
                TextureHandle destinationColor)
            {
                using var builder = renderGraph.AddRasterRenderPass<CompositePassData>(
                    CompositePassName,
                    out var passData,
                    profilingSampler);
                passData.material = material;
                passData.sourceColor = sourceColor;
                passData.depthTexture = depthTexture;
                passData.scatteringTexture = scatteringTexture;
                builder.UseTexture(sourceColor, AccessFlags.Read);
                builder.UseTexture(depthTexture, AccessFlags.Read);
                builder.UseTexture(scatteringTexture, AccessFlags.Read);
                builder.SetRenderAttachment(destinationColor, 0, AccessFlags.Write);
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc(static (CompositePassData data, RasterGraphContext context) =>
                {
                    context.cmd.SetGlobalTexture(DustScatteringTextureId, data.scatteringTexture);
                    Blitter.BlitTexture(
                        context.cmd,
                        data.sourceColor,
                        new Vector4(1f, 1f, 0f, 0f),
                        data.material,
                        1);
                });
            }

            private sealed class ScatterPassData
            {
                public Material material;
                public TextureHandle depthTexture;
            }

            private sealed class CompositePassData
            {
                public Material material;
                public TextureHandle sourceColor;
                public TextureHandle depthTexture;
                public TextureHandle scatteringTexture;
            }
        }
    }
}
