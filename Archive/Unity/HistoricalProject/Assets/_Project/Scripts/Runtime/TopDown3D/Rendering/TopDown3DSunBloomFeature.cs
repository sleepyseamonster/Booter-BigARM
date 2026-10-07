using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace BooterBigArm.TopDown3D
{
    public sealed class TopDown3DSunBloomFeature : ScriptableRendererFeature
    {
        public const RenderPassEvent CanonicalInjectionPoint = RenderPassEvent.BeforeRenderingPostProcessing;
        public const int DefaultDownsample = 2;
        public const int DefaultPyramidLevels = 6;
        public const float DefaultIntensity = 0.38f;
        public const float DefaultThreshold = 0.82f;
        public const float DefaultSoftKnee = 0.48f;
        public const float DefaultScatter = 0.7f;
        public const float DefaultClamp = 12f;
        public const float DefaultTightWeight = 0.9f;
        public const float DefaultMediumWeight = 0.72f;
        public const float DefaultBroadWeight = 0.48f;
        public const float DefaultSunAureoleIntensity = 0.62f;
        public const float DefaultSunGlowRadius = 0.16f;
        public const float DefaultSunEdgeReach = 0.35f;
        public static readonly Color DefaultTint = new Color(1f, 0.84f, 0.68f, 1f);

        [SerializeField] private Shader bloomShader;
        [SerializeField] private bool bloomEnabled = true;
        [SerializeField] private bool previewInSceneView = true;
        [SerializeField, Range(1, 4)] private int downsample = DefaultDownsample;
        [SerializeField, Range(3, 7)] private int pyramidLevels = DefaultPyramidLevels;
        [SerializeField, Range(0f, 2f)] private float intensity = DefaultIntensity;
        [SerializeField, Range(0f, 4f)] private float threshold = DefaultThreshold;
        [SerializeField, Range(0f, 1f)] private float softKnee = DefaultSoftKnee;
        [SerializeField, Range(0f, 1f)] private float scatter = DefaultScatter;
        [SerializeField, Range(1f, 32f)] private float clamp = DefaultClamp;
        [SerializeField, Range(0f, 2f)] private float tightWeight = DefaultTightWeight;
        [SerializeField, Range(0f, 2f)] private float mediumWeight = DefaultMediumWeight;
        [SerializeField, Range(0f, 2f)] private float broadWeight = DefaultBroadWeight;
        [SerializeField, Range(0f, 2f)] private float sunAureoleIntensity = DefaultSunAureoleIntensity;
        [SerializeField, Range(0.02f, 0.5f)] private float sunAureoleRadius = DefaultSunGlowRadius;
        [SerializeField] private Color tint = new Color(1f, 0.84f, 0.68f, 1f);

        private Material material;
        private SunBloomPass pass;
        private Light fallbackDirectionalSun;

        public Shader BloomShader => bloomShader;
        public bool BloomEnabled => bloomEnabled;
        public bool PreviewInSceneView => previewInSceneView;
        public int Downsample => downsample;
        public int PyramidLevels => pyramidLevels;
        public float Intensity => intensity;
        public float Threshold => threshold;
        public float SoftKnee => softKnee;
        public float Scatter => scatter;
        public float Clamp => clamp;
        public float TightWeight => tightWeight;
        public float MediumWeight => mediumWeight;
        public float BroadWeight => broadWeight;
        public float SunAureoleIntensity => sunAureoleIntensity;
        public float SunAureoleRadius => sunAureoleRadius;
        public Color Tint => tint;

        public override void Create()
        {
            if (material != null && material.shader != bloomShader)
            {
                CoreUtils.Destroy(material);
                material = null;
            }

            if (material == null && bloomShader != null)
            {
                material = CoreUtils.CreateEngineMaterial(bloomShader);
            }

            pass = new SunBloomPass { renderPassEvent = CanonicalInjectionPoint };
        }

        public void Configure(Shader shader)
        {
            bloomShader = shader;
            bloomEnabled = true;
            previewInSceneView = true;
            downsample = DefaultDownsample;
            pyramidLevels = DefaultPyramidLevels;
            intensity = DefaultIntensity;
            threshold = DefaultThreshold;
            softKnee = DefaultSoftKnee;
            scatter = DefaultScatter;
            clamp = DefaultClamp;
            tightWeight = DefaultTightWeight;
            mediumWeight = DefaultMediumWeight;
            broadWeight = DefaultBroadWeight;
            sunAureoleIntensity = DefaultSunAureoleIntensity;
            sunAureoleRadius = DefaultSunGlowRadius;
            tint = DefaultTint;
            Create();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var cameraData = renderingData.cameraData;
            var cameraType = cameraData.cameraType;
            if (!bloomEnabled
                || material == null
                || intensity <= 0f
                || cameraData.renderType != CameraRenderType.Base
                || (cameraType != CameraType.Game && cameraType != CameraType.SceneView)
                || (cameraType == CameraType.SceneView && !previewInSceneView))
            {
                return;
            }

            var hasSun = TryResolveSun(
                cameraData.camera,
                out var sunPosition,
                out var sunBrightness);
            pass.Setup(
                material,
                Mathf.Clamp(downsample, 1, 4),
                Mathf.Clamp(pyramidLevels, 3, 7),
                Mathf.Clamp(intensity, 0f, 2f),
                Mathf.Max(0f, threshold),
                Mathf.Clamp01(softKnee),
                Mathf.Clamp01(scatter),
                Mathf.Clamp(clamp, 1f, 32f),
                Mathf.Clamp(tightWeight, 0f, 2f),
                Mathf.Clamp(mediumWeight, 0f, 2f),
                Mathf.Clamp(broadWeight, 0f, 2f),
                Mathf.Clamp(sunAureoleIntensity, 0f, 2f),
                Mathf.Clamp(sunAureoleRadius, 0.02f, 0.5f),
                tint,
                hasSun ? sunPosition : new Vector2(-10f, -10f),
                cameraData.camera.aspect,
                hasSun ? sunBrightness : 0f);
            renderer.EnqueuePass(pass);
        }

        private bool TryResolveSun(Camera camera, out Vector2 viewportPosition, out float brightness)
        {
            var sun = RenderSettings.sun;
            Vector3 directionToSun;
            if (IsUsableDirectionalLight(sun))
            {
                directionToSun = -sun.transform.forward;
                brightness = EvaluateSunBrightness(sun);
            }
            else if (PerpetualTwilightSun.Active != null)
            {
                directionToSun = PerpetualTwilightSun.Active.DirectionToSun;
                brightness = Mathf.Lerp(0.7f, 1.2f, PerpetualTwilightSun.Active.Brightness01);
            }
            else
            {
                if (!IsUsableDirectionalLight(fallbackDirectionalSun))
                {
                    fallbackDirectionalSun = FindBrightestDirectionalLight();
                }

                if (fallbackDirectionalSun == null)
                {
                    viewportPosition = default;
                    brightness = 0f;
                    return false;
                }

                directionToSun = -fallbackDirectionalSun.transform.forward;
                brightness = EvaluateSunBrightness(fallbackDirectionalSun);
            }

            var sourcePoint = camera.transform.position
                + directionToSun.normalized * (camera.farClipPlane * 0.9f);
            var projected = camera.WorldToViewportPoint(sourcePoint);
            viewportPosition = new Vector2(projected.x, projected.y);
            return projected.z > 0f;
        }

        private static bool IsUsableDirectionalLight(Light candidate)
        {
            return candidate != null
                && candidate.isActiveAndEnabled
                && candidate.type == LightType.Directional;
        }

        private static float EvaluateSunBrightness(Light source)
        {
            var peakColor = Mathf.Max(source.color.r, Mathf.Max(source.color.g, source.color.b));
            return Mathf.Clamp(source.intensity * peakColor, 0.35f, 2.5f);
        }

        private static Light FindBrightestDirectionalLight()
        {
            var lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude);
            Light brightest = null;
            var brightestIntensity = float.MinValue;
            foreach (var candidate in lights)
            {
                if (!IsUsableDirectionalLight(candidate) || candidate.intensity <= brightestIntensity)
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
            CoreUtils.Destroy(material);
            material = null;
            pass = null;
        }

        public static float EvaluateHighlightWeight(float brightness, float highlightThreshold, float knee)
        {
            brightness = Mathf.Max(0f, brightness);
            highlightThreshold = Mathf.Max(0f, highlightThreshold);
            var kneeWidth = Mathf.Max(0.0001f, highlightThreshold * Mathf.Clamp01(knee));
            var soft = Mathf.Clamp01((brightness - highlightThreshold + kneeWidth) / (2f * kneeWidth));
            soft = soft * soft * kneeWidth;
            var contribution = Mathf.Max(brightness - highlightThreshold, soft);
            return contribution / Mathf.Max(0.0001f, brightness);
        }

        public static float EvaluateScaleWeight(
            float normalizedLevel,
            float tight,
            float medium,
            float broad)
        {
            normalizedLevel = Mathf.Clamp01(normalizedLevel);
            if (normalizedLevel <= 0.5f)
            {
                return Mathf.Lerp(tight, medium, normalizedLevel * 2f);
            }

            return Mathf.Lerp(medium, broad, (normalizedLevel - 0.5f) * 2f);
        }

        private sealed class SunBloomPass : ScriptableRenderPass
        {
            private static readonly int ParamsId = Shader.PropertyToID("_SunBloomParams");
            private static readonly int OpticsId = Shader.PropertyToID("_SunBloomOptics");
            private static readonly int TintId = Shader.PropertyToID("_SunBloomTint");
            private static readonly int TextureId = Shader.PropertyToID("_SunBloomTexture");
            private static readonly int TexelSizeId = Shader.PropertyToID("_SunBloomTexelSize");
            private static readonly int DirectionId = Shader.PropertyToID("_SunBloomDirection");
            private static readonly int LowTextureId = Shader.PropertyToID("_SunBloomLowTexture");
            private static readonly int LowTexelSizeId = Shader.PropertyToID("_SunBloomLowTexelSize");
            private static readonly int LevelWeightId = Shader.PropertyToID("_SunBloomLevelWeight");
            private static readonly int SourceId = Shader.PropertyToID("_SunBloomSource");
            private static readonly int SunOpticsId = Shader.PropertyToID("_SunBloomSunOptics");

            private Material material;
            private int downsample;
            private int pyramidLevels;
            private float intensity;
            private float threshold;
            private float softKnee;
            private float scatter;
            private float clamp;
            private float tightWeight;
            private float mediumWeight;
            private float broadWeight;
            private float sunAureoleIntensity;
            private float sunAureoleRadius;
            private Color tint;
            private Vector2 sunPosition;
            private float cameraAspect;
            private float sunBrightness;
            private readonly TextureHandle[] pyramid = new TextureHandle[7];
            private readonly int[] levelWidths = new int[7];
            private readonly int[] levelHeights = new int[7];

            public SunBloomPass()
            {
                profilingSampler = new ProfilingSampler("TopDown3D Sun Bloom");
                requiresIntermediateTexture = true;
            }

            public void Setup(Material value, int scale, int levels, float strength, float cutoff, float knee,
                float radius, float maximum, float tight, float medium, float broad,
                float aureoleIntensity, float aureoleRadius, Color color, Vector2 physicalSunPosition,
                float aspect, float physicalSunBrightness)
            {
                material = value;
                downsample = scale;
                pyramidLevels = levels;
                intensity = strength;
                threshold = cutoff;
                softKnee = knee;
                scatter = radius;
                clamp = maximum;
                tightWeight = tight;
                mediumWeight = medium;
                broadWeight = broad;
                sunAureoleIntensity = aureoleIntensity;
                sunAureoleRadius = aureoleRadius;
                tint = color;
                sunPosition = physicalSunPosition;
                cameraAspect = aspect;
                sunBrightness = physicalSunBrightness;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var resources = frameData.Get<UniversalResourceData>();
                var cameraData = frameData.Get<UniversalCameraData>();
                if (material == null || resources.isActiveTargetBackBuffer || !resources.activeColorTexture.IsValid())
                {
                    return;
                }

                var source = resources.activeColorTexture;
                var cameraDescriptor = cameraData.cameraTargetDescriptor;
                var width = Mathf.Max(1, cameraDescriptor.width / downsample);
                var height = Mathf.Max(1, cameraDescriptor.height / downsample);
                var levels = Mathf.Clamp(pyramidLevels, 3, 7);
                var bloomDescriptor = source.GetDescriptor(renderGraph);
                bloomDescriptor.clearBuffer = false;
                bloomDescriptor.filterMode = FilterMode.Bilinear;
                bloomDescriptor.wrapMode = TextureWrapMode.Clamp;
                bloomDescriptor.msaaSamples = MSAASamples.None;
                for (var level = 0; level < levels; level++)
                {
                    levelWidths[level] = Mathf.Max(1, width >> level);
                    levelHeights[level] = Mathf.Max(1, height >> level);
                    bloomDescriptor.width = levelWidths[level];
                    bloomDescriptor.height = levelHeights[level];
                    bloomDescriptor.name = $"_TopDown3DSunBloomDown{level}";
                    pyramid[level] = renderGraph.CreateTexture(bloomDescriptor);
                }

                material.SetVector(ParamsId, new Vector4(threshold, softKnee, intensity, clamp));
                material.SetVector(OpticsId, new Vector4(scatter, 0f, 0f, 0f));
                material.SetVector(TexelSizeId, new Vector4(1f / width, 1f / height, width, height));
                material.SetColor(TintId, tint);
                material.SetVector(
                    SourceId,
                    new Vector4(sunPosition.x, sunPosition.y, cameraAspect, sunBrightness));
                material.SetVector(
                    SunOpticsId,
                    new Vector4(DefaultSunEdgeReach, sunAureoleRadius, sunAureoleIntensity, 0f));

                RecordBlit(renderGraph, "TopDown3D Sun Bloom Extract", source, pyramid[0], 0,
                    Vector2.zero, levelWidths[0], levelHeights[0]);
                for (var level = 1; level < levels; level++)
                {
                    RecordBlit(renderGraph, $"TopDown3D Sun Bloom Downsample {level}",
                        pyramid[level - 1], pyramid[level], 1, Vector2.zero,
                        levelWidths[level - 1], levelHeights[level - 1]);
                }

                var accumulated = pyramid[levels - 1];
                for (var level = levels - 2; level >= 0; level--)
                {
                    bloomDescriptor.width = levelWidths[level];
                    bloomDescriptor.height = levelHeights[level];
                    bloomDescriptor.name = $"_TopDown3DSunBloomUp{level}";
                    var combined = renderGraph.CreateTexture(bloomDescriptor);
                    var normalizedLevel = levels <= 2 ? 0f : level / (float)(levels - 2);
                    var levelWeight = TopDown3DSunBloomFeature.EvaluateScaleWeight(
                        normalizedLevel,
                        tightWeight,
                        mediumWeight,
                        broadWeight);
                    RecordUpsample(renderGraph, $"TopDown3D Sun Bloom Upsample {level}",
                        pyramid[level], accumulated, combined, levelWeight,
                        levelWidths[level + 1], levelHeights[level + 1]);
                    accumulated = combined;
                }

                var destinationDescriptor = source.GetDescriptor(renderGraph);
                destinationDescriptor.name = "_TopDown3DSunBloomCameraColor";
                destinationDescriptor.clearBuffer = false;
                var destination = renderGraph.CreateTexture(destinationDescriptor);
                RecordComposite(renderGraph, source, accumulated, destination);
                resources.cameraColor = destination;
            }

            private void RecordBlit(RenderGraph graph, string name, TextureHandle source,
                TextureHandle destination, int shaderPass, Vector2 direction, int sourceWidth, int sourceHeight)
            {
                using var builder = graph.AddRasterRenderPass<BlitPassData>(name, out var data, profilingSampler);
                data.material = material;
                data.source = source;
                data.shaderPass = shaderPass;
                data.direction = direction;
                data.texelSize = new Vector4(
                    1f / Mathf.Max(1, sourceWidth),
                    1f / Mathf.Max(1, sourceHeight),
                    sourceWidth,
                    sourceHeight);
                builder.UseTexture(source, AccessFlags.Read);
                builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc(static (BlitPassData value, RasterGraphContext context) =>
                {
                    context.cmd.SetGlobalVector(DirectionId, value.direction);
                    context.cmd.SetGlobalVector(TexelSizeId, value.texelSize);
                    Blitter.BlitTexture(context.cmd, value.source, new Vector4(1f, 1f, 0f, 0f),
                        value.material, value.shaderPass);
                });
            }

            private void RecordUpsample(RenderGraph graph, string name, TextureHandle high,
                TextureHandle low, TextureHandle destination, float levelWeight,
                int lowWidth, int lowHeight)
            {
                using var builder = graph.AddRasterRenderPass<UpsamplePassData>(
                    name, out var data, profilingSampler);
                data.material = material;
                data.high = high;
                data.low = low;
                data.levelWeight = levelWeight;
                data.lowTexelSize = new Vector4(
                    1f / Mathf.Max(1, lowWidth),
                    1f / Mathf.Max(1, lowHeight),
                    lowWidth,
                    lowHeight);
                builder.UseTexture(high, AccessFlags.Read);
                builder.UseTexture(low, AccessFlags.Read);
                builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc(static (UpsamplePassData value, RasterGraphContext context) =>
                {
                    context.cmd.SetGlobalTexture(LowTextureId, value.low);
                    context.cmd.SetGlobalFloat(LevelWeightId, value.levelWeight);
                    context.cmd.SetGlobalVector(LowTexelSizeId, value.lowTexelSize);
                    Blitter.BlitTexture(context.cmd, value.high, new Vector4(1f, 1f, 0f, 0f),
                        value.material, 2);
                });
            }

            private void RecordComposite(RenderGraph graph, TextureHandle source,
                TextureHandle bloom, TextureHandle destination)
            {
                using var builder = graph.AddRasterRenderPass<CompositePassData>(
                    "TopDown3D Sun Bloom Composite", out var data, profilingSampler);
                data.material = material;
                data.source = source;
                data.bloom = bloom;
                builder.UseTexture(source, AccessFlags.Read);
                builder.UseTexture(bloom, AccessFlags.Read);
                builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc(static (CompositePassData value, RasterGraphContext context) =>
                {
                    context.cmd.SetGlobalTexture(TextureId, value.bloom);
                    Blitter.BlitTexture(context.cmd, value.source, new Vector4(1f, 1f, 0f, 0f),
                        value.material, 3);
                });
            }

            private sealed class BlitPassData
            {
                public Material material;
                public TextureHandle source;
                public int shaderPass;
                public Vector2 direction;
                public Vector4 texelSize;
            }

            private sealed class UpsamplePassData
            {
                public Material material;
                public TextureHandle high;
                public TextureHandle low;
                public float levelWeight;
                public Vector4 lowTexelSize;
            }

            private sealed class CompositePassData
            {
                public Material material;
                public TextureHandle source;
                public TextureHandle bloom;
            }
        }
    }
}
