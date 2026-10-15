using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace BooterBigArm.TopDown3D
{
    // Selected explicitly by migrated camera bindings. Legacy cameras retain their existing setup path.
    public sealed class TopDown3DCameraOpticsFeature : ScriptableRendererFeature
    {
        private TopDown3DCampGaussianPass gaussian;
        private DepthRequirement depth;
        private bool reportedGaussianFallback;

        private sealed class DepthRequirement : ScriptableRenderPass
        {
            public DepthRequirement()
            {
                renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
                ConfigureInput(ScriptableRenderPassInput.Depth);
            }
            public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData) { }
        }

        public static bool IsModularCamera(Camera camera)
        {
            var binding = TopDown3DSceneRenderingOverride.BindingForCamera(camera);
            return binding != null && binding.ModularOpticsConfigured;
        }

        public override void Create()
        {
            gaussian?.Dispose();
            gaussian = new TopDown3DCampGaussianPass();
            depth = new DepthRequirement();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var camera = renderingData.cameraData.camera;
            if (!IsModularCamera(camera)) return;
            if (!TopDown3DCampOpticsStack.TryApply(camera)) return;
            var profile = TopDown3DSceneRenderingOverride.CameraOpticsFor(camera);
            if (profile == null || profile.EffectiveDepthMode == DepthOfFieldMode.Off) return;
            if (profile.nativeOptics.gaussianBeforeLens && profile.EffectiveDepthMode == DepthOfFieldMode.Gaussian)
            {
                if (gaussian.IsSupported) renderer.EnqueuePass(gaussian);
                else
                {
                    VolumeManager.instance.stack.GetComponent<DepthOfField>().mode.Override(DepthOfFieldMode.Gaussian);
                    renderer.EnqueuePass(depth);
                    if (!reportedGaussianFallback)
                        Debug.LogWarning("Camera optics: early Gaussian unavailable; using native DOF with lens flare suppressed.");
                    reportedGaussianFallback = true;
                }
            }
            else renderer.EnqueuePass(depth);
        }

        protected override void Dispose(bool disposing)
        {
            gaussian?.Dispose();
            gaussian = null;
            depth = null;
        }
    }
}
