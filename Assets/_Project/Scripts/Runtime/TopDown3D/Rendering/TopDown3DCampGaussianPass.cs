using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace BooterBigArm.TopDown3D
{
    // Runs the pinned URP Gaussian shader before camera artifacts, rather than inside
    // the indivisible native postprocess block. Native bloom/dirt/grading follow it.
    internal sealed class TopDown3DCampGaussianPass : ScriptableRenderPass
    {
        public const RenderPassEvent InjectionPoint = RenderPassEvent.BeforeRenderingPostProcessing - 1;
        private readonly Material material;
        private readonly Material cocMaterial;
        private readonly GraphicsFormat cocFormat;
        public bool IsSupported => material != null && material.shader.isSupported;
        public static bool ShaderSupported
        {
            get { var shader = Shader.Find("Hidden/Universal Render Pipeline/GaussianDepthOfField"); return shader != null && shader.isSupported; }
        }

        public TopDown3DCampGaussianPass()
        {
            renderPassEvent = InjectionPoint;
            ConfigureInput(ScriptableRenderPassInput.Depth);
            var shader = Shader.Find("Hidden/Universal Render Pipeline/GaussianDepthOfField");
            if (shader != null)
            {
                material = CoreUtils.CreateEngineMaterial(shader);
                cocMaterial = CoreUtils.CreateEngineMaterial(shader);
            }
            cocFormat = Supports(GraphicsFormat.R16_UNorm) ? GraphicsFormat.R16_UNorm
                : Supports(GraphicsFormat.R16_SFloat) ? GraphicsFormat.R16_SFloat : GraphicsFormat.R8_UNorm;
        }

        private static bool Supports(GraphicsFormat format)
            => SystemInfo.IsFormatSupported(format, GraphicsFormatUsage.Render)
                && SystemInfo.IsFormatSupported(format, GraphicsFormatUsage.Linear);

        public void Dispose() { CoreUtils.Destroy(material); CoreUtils.Destroy(cocMaterial); }

        private sealed class Data
        {
            public Material material, cocMaterial;
            public TextureHandle source, depth, fullCoc, halfCoc, ping, pong, destination;
            public Vector4 coc, sourceSize;
            public bool highQuality, alpha;
            public RenderTargetIdentifier[] targets = new RenderTargetIdentifier[2];
        }

        public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
        {
            var resources = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            if (!IsSupported || resources.isActiveTargetBackBuffer || !resources.cameraDepthTexture.IsValid()
                || !TopDown3DSceneRenderingOverride.TryGetAtmosphereProfile(cameraData.camera, out var study)
                || study.nativeOptics == null || !study.nativeOptics.IsValid || !study.nativeOptics.opticsEnabled
                || !study.nativeOptics.gaussianBeforeLens || study.nativeOptics.depthMode != DepthOfFieldMode.Gaussian)
                return;
            var dof = VolumeManager.instance.stack.GetComponent<DepthOfField>();
            var source = resources.activeColorTexture;
            if (!source.IsValid()) return;
            var descriptor = source.GetDescriptor(graph);
            int halfWidth = Mathf.Max(1, descriptor.width / 2), halfHeight = Mathf.Max(1, descriptor.height / 2);
            TextureHandle Create(string name, int width, int height, GraphicsFormat format)
            {
                var desc = descriptor;
                desc.name = name; desc.width = width; desc.height = height;
                desc.colorFormat = format; desc.depthBufferBits = DepthBits.None;
                desc.msaaSamples = MSAASamples.None; desc.clearBuffer = false;
                desc.filterMode = FilterMode.Bilinear;
                return graph.CreateTexture(desc);
            }
            var full = Create("Camp Gaussian full CoC", descriptor.width, descriptor.height, cocFormat);
            var half = Create("Camp Gaussian half CoC", halfWidth, halfHeight, cocFormat);
            var ping = Create("Camp Gaussian ping", halfWidth, halfHeight, descriptor.colorFormat);
            var pong = Create("Camp Gaussian pong", halfWidth, halfHeight, descriptor.colorFormat);
            var destination = Create("Camp Gaussian world color", descriptor.width, descriptor.height, descriptor.colorFormat);
            using var builder = graph.AddUnsafePass<Data>("Camp World DOF Before Lens", out var data);
            data.material = material; data.cocMaterial = cocMaterial; data.source = source; data.depth = resources.cameraDepthTexture;
            data.fullCoc = full; data.halfCoc = half; data.ping = ping; data.pong = pong; data.destination = destination;
            data.coc = new Vector4(dof.gaussianStart.value, dof.gaussianEnd.value,
                Mathf.Min(2f, dof.gaussianMaxRadius.value * (halfWidth / 1080f)), 0f);
            data.sourceSize = new Vector4(descriptor.width, descriptor.height, 1f / descriptor.width, 1f / descriptor.height);
            data.highQuality = dof.highQualitySampling.value; data.alpha = cameraData.isAlphaOutputEnabled;
            builder.UseTexture(source, AccessFlags.Read); builder.UseTexture(data.depth, AccessFlags.Read);
            builder.UseTexture(full, AccessFlags.ReadWrite); builder.UseTexture(half, AccessFlags.ReadWrite);
            builder.UseTexture(ping, AccessFlags.ReadWrite); builder.UseTexture(pong, AccessFlags.ReadWrite);
            builder.UseTexture(destination, AccessFlags.Write);
            builder.SetRenderFunc(static (Data pass, UnsafeGraphContext context) =>
            {
                var cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);
                var mat = pass.material;
                RTHandle sourceHandle = pass.source;
                var size = pass.sourceSize;
                if (sourceHandle.rt != null)
                {
                    float width = sourceHandle.rt.width, height = sourceHandle.rt.height;
                    if (sourceHandle.rt.useDynamicScale)
                    { width *= ScalableBufferManager.widthScaleFactor; height *= ScalableBufferManager.heightScaleFactor; }
                    size = new Vector4(width, height, 1f / width, 1f / height);
                }
                mat.SetVector("_CoCParams", pass.coc);
                mat.SetVector("_SourceSize", size);
                mat.SetVector("_DownSampleScaleFactor", new Vector4(.5f, .5f, 2f, 2f));
                CoreUtils.SetKeyword(mat, "_HIGH_QUALITY_SAMPLING", pass.highQuality);
                CoreUtils.SetKeyword(mat, "_ENABLE_ALPHA_OUTPUT", pass.alpha);
                pass.cocMaterial.SetVector("_CoCParams", pass.coc);
                pass.cocMaterial.SetVector("_SourceSize", size);
                pass.cocMaterial.SetTexture("_CameraDepthTexture", pass.depth);
                Blitter.BlitCameraTexture(cmd, pass.source, pass.fullCoc, pass.cocMaterial, 0);
                mat.SetTexture("_FullCoCTexture", pass.fullCoc);
                pass.targets[0] = pass.halfCoc; pass.targets[1] = pass.ping;
                CoreUtils.SetRenderTarget(cmd, pass.targets, pass.halfCoc);
                var scale = sourceHandle.useScaling ? sourceHandle.rtHandleProperties.rtHandleScale : Vector4.one;
                Blitter.BlitTexture(cmd, pass.source, new Vector2(scale.x, scale.y), mat, 1);
                mat.SetTexture("_HalfCoCTexture", pass.halfCoc);
                Blitter.BlitCameraTexture(cmd, pass.ping, pass.pong, mat, 2);
                Blitter.BlitCameraTexture(cmd, pass.pong, pass.ping, mat, 3);
                mat.SetTexture("_ColorTexture", pass.ping);
                Blitter.BlitCameraTexture(cmd, pass.source, pass.destination, mat, 4);
            });
            resources.cameraColor = destination;
        }
    }
}
