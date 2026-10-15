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

            pass?.ReleaseHistory();
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
            if (!TopDown3DSceneRenderingOverride.AuthoringEffectsEnabled(cameraData.camera))
            { pass?.ForgetHistory(cameraData.camera); return; }
            bool hasStudy = TopDown3DSceneRenderingOverride.TryGetAtmosphereProfile(cameraData.camera, out var study);
            bool isModular = TopDown3DCameraOpticsFeature.IsModularCamera(cameraData.camera);
            var modular = TopDown3DSceneRenderingOverride.CameraOpticsFor(cameraData.camera);
            if (isModular && (modular == null || !modular.flareEnabled || !modular.nativeOptics.opticsEnabled))
            { pass?.ForgetHistory(cameraData.camera); return; }
            var conservative = isModular || (hasStudy && study.nativeOptics != null);
            var optics = isModular ? modular.nativeOptics : conservative ? study.nativeOptics : null;
            var effectiveDepth = isModular ? modular.EffectiveDepthMode : optics != null ? optics.depthMode : DepthOfFieldMode.Off;
            if (isModular && ((effectiveDepth != DepthOfFieldMode.Off && !optics.gaussianBeforeLens)
                || (effectiveDepth == DepthOfFieldMode.Gaussian && optics.gaussianBeforeLens && !TopDown3DCampGaussianPass.ShaderSupported)))
            { pass?.ForgetHistory(cameraData.camera); return; }
            if (!isModular && hasStudy && (!study.flareEnabled || (conservative
                && (!optics.IsValid || !optics.opticsEnabled
                    || (optics.depthMode != DepthOfFieldMode.Off && !optics.gaussianBeforeLens)
                    || (optics.gaussianBeforeLens && !TopDown3DCampGaussianPass.ShaderSupported)
                    || !float.IsFinite(optics.sunAngularDiameterDegrees)
                    || !float.IsFinite(optics.flareVisibilityAttackSeconds)
                    || !float.IsFinite(optics.flareVisibilityReleaseSeconds)))))
            { pass?.ForgetHistory(cameraData.camera); return; }
            var cameraType = cameraData.cameraType;
            if (!flareEnabled
                || material == null
                || intensity <= 0f
                || cameraData.renderType != CameraRenderType.Base
                || (cameraType != CameraType.Game && cameraType != CameraType.SceneView)
                || (cameraType == CameraType.SceneView && !previewInSceneView))
            {
                pass?.ForgetHistory(cameraData.camera);
                return;
            }

            var directionToSun = ResolveDirectionToSun(out var sunBrightness);
            if (!float.IsFinite(directionToSun.x) || !float.IsFinite(directionToSun.y)
                || !float.IsFinite(directionToSun.z) || directionToSun.sqrMagnitude <= 0.0001f)
            {
                pass?.ForgetHistory(cameraData.camera);
                return;
            }

            var camera = cameraData.camera;
            float angularDiameter = isModular
                ? TopDown3DSceneRenderingOverride.SunAngularDiameterFor(camera, optics.sunAngularDiameterDegrees)
                : conservative ? optics.sunAngularDiameterDegrees : 0f;
            if (conservative && !float.IsFinite(angularDiameter))
            { pass?.ForgetHistory(camera); return; }
            var sourcePoint = camera.transform.position
                + directionToSun.normalized * (camera.farClipPlane * 0.9f);
            var viewportPoint = camera.WorldToViewportPoint(sourcePoint);
            if (!float.IsFinite(viewportPoint.x) || !float.IsFinite(viewportPoint.y) || viewportPoint.z <= 0f
                || (conservative && EvaluateConservativeOffscreenVisibility(new Vector2(viewportPoint.x, viewportPoint.y)) <= 0f))
            {
                pass?.ForgetHistory(camera);
                return;
            }

            pass.Setup(
                material,
                camera,
                cameraData.cameraTargetDescriptor,
                new Vector2(viewportPoint.x, viewportPoint.y),
                camera.aspect,
                Mathf.Clamp(isModular ? modular.flareIntensity : hasStudy ? study.flareIntensity : intensity, 0f, 2f),
                conservative ? Mathf.Max(.001f, CalculateSolarDiscViewportRadius(angularDiameter, camera.fieldOfView) * 2f)
                    : Mathf.Clamp(radius, 0.02f, 0.75f),
                conservative ? 1f : Mathf.Clamp(anisotropy, 0.4f, 2.5f),
                Mathf.Clamp(ghostReach, 0f, 3f),
                Mathf.Clamp01(edgeReach),
                Mathf.Clamp(haloThickness, 0.03f, 0.5f),
                Mathf.Clamp(hdrEnergy, 0.5f, 6f),
                sunBrightness,
                conservative ? CalculateSolarDiscViewportRadius(angularDiameter, camera.fieldOfView)
                    : Mathf.Clamp(occlusionRadius, 0.005f, 0.25f),
                conservative ? 1f : Mathf.Clamp01(coreOcclusion),
                conservative ? 1f : Mathf.Clamp01(aureoleOcclusion),
                conservative ? 1f : Mathf.Clamp01(ghostOcclusion),
                Mathf.Clamp(occlusionStability, 0f, 0.95f),
                isModular ? TopDown3DSceneRenderingOverride.SunTintFor(camera, tint)
                    : conservative ? EvaluateStudyTint(study, sunBrightness) : tint,
                primaryFlareSprite,
                ghostRingSprite,
                apertureGhostSprite,
                conservative,
                isModular ? VisibilityProfileKey(modular, angularDiameter) : conservative ? VisibilityProfileKey(study) : 0,
                conservative ? optics.flareVisibilityAttackSeconds : .06f,
                conservative ? optics.flareVisibilityReleaseSeconds : .12f);
            renderer.EnqueuePass(pass);
        }

        public static Color EvaluateStudyTint(TopDown3DAtmosphereStudyProfile study, float fallbackBrightness)
        {
            float phase = PerpetualTwilightSun.Active != null ? PerpetualTwilightSun.Active.Brightness01
                : Mathf.InverseLerp(.68f, 1.18f, fallbackBrightness);
            return Color.Lerp(study.deepSunColor, study.brightSunColor, phase);
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

        public static bool IsCameraCut(Vector3 previousPosition, Vector3 position, Quaternion previousRotation, Quaternion rotation)
            => Vector3.Distance(previousPosition, position) > 2f || Quaternion.Angle(previousRotation, rotation) > 20f;

        public static float EvaluateConservativeOffscreenVisibility(Vector2 source)
            => float.IsFinite(source.x) && float.IsFinite(source.y)
                && source.x >= 0f && source.x <= 1f && source.y >= 0f && source.y <= 1f ? 1f : 0f;

        public static float EvaluateBackgroundDepthVisibility(float rawDepth, bool reversedZ, bool depthAvailable)
            => depthAvailable && float.IsFinite(rawDepth)
                && rawDepth >= 0f && rawDepth <= 1f
                && (reversedZ ? rawDepth == 0f : rawDepth == 1f) ? 1f : 0f;

        public static float CalculateSolarDiscViewportRadius(float angularDiameterDegrees, float verticalFieldOfView)
            => .5f * Mathf.Tan(Mathf.Clamp(angularDiameterDegrees, .05f, 5f) * Mathf.Deg2Rad * .5f)
                / Mathf.Tan(Mathf.Clamp(verticalFieldOfView, 1f, 179f) * Mathf.Deg2Rad * .5f);

        public static float EvaluateTimedVisibility(float previous, float current, float deltaSeconds,
            float attackSeconds, float releaseSeconds)
        {
            // Fully blocked/unknown light must not linger through an opaque occluder.
            // Timed release applies to changes between partially visible states.
            if (current <= 0f) return 0f;
            var duration = current > previous ? attackSeconds : releaseSeconds;
            var retention = Mathf.Exp(-Mathf.Max(0f, deltaSeconds) / Mathf.Max(.001f, duration));
            return Mathf.Lerp(Mathf.Clamp01(current), Mathf.Clamp01(previous), retention);
        }

        private static int VisibilityProfileKey(TopDown3DAtmosphereStudyProfile study)
        {
            unchecked
            {
                var optics = study.nativeOptics;
                var key = study.GetInstanceID();
                key = key * 397 ^ optics.GetInstanceID();
                key = key * 397 ^ study.flareIntensity.GetHashCode();
                key = key * 397 ^ optics.sunAngularDiameterDegrees.GetHashCode();
                key = key * 397 ^ optics.flareVisibilityAttackSeconds.GetHashCode();
                return key * 397 ^ optics.flareVisibilityReleaseSeconds.GetHashCode();
            }
        }

        private static int VisibilityProfileKey(TopDown3DCameraOpticsProfile profile, float angularDiameter)
        {
            unchecked
            {
                var optics = profile.nativeOptics;
                var key = profile.GetInstanceID();
                key = key * 397 ^ optics.GetInstanceID();
                key = key * 397 ^ profile.flareIntensity.GetHashCode();
                key = key * 397 ^ angularDiameter.GetHashCode();
                key = key * 397 ^ optics.flareVisibilityAttackSeconds.GetHashCode();
                return key * 397 ^ optics.flareVisibilityReleaseSeconds.GetHashCode();
            }
        }

        public static bool ShouldResetVisibilityHistory(bool valid, int previousProfileKey, int profileKey,
            Vector2Int previousResolution, Vector2Int resolution,
            Vector3 previousPosition, Vector3 position, Quaternion previousRotation, Quaternion rotation)
            => !valid || previousProfileKey != profileKey || previousResolution != resolution
                || IsCameraCut(previousPosition, position, previousRotation, rotation);

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
            private static readonly int VisibilityModeId = Shader.PropertyToID("_RoundFlareVisibilityMode");

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
            private Vector3 cameraPosition;
            private Quaternion cameraRotation;
            private bool conservative;
            private int profileKey;
            private float attackSeconds, releaseSeconds, deltaSeconds;
            private Vector2Int resolution;
            private readonly List<int> deadCameras = new();

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
                Texture2D optionalApertureSprite,
                bool conservativeVisibility,
                int visibilityProfileKey,
                float visibilityAttackSeconds,
                float visibilityReleaseSeconds)
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
                conservative = conservativeVisibility;
                profileKey = visibilityProfileKey;
                attackSeconds = Mathf.Clamp(visibilityAttackSeconds, .001f, 1f);
                releaseSeconds = Mathf.Clamp(visibilityReleaseSeconds, .001f, 1f);
                resolution = new Vector2Int(cameraDescriptor.width, cameraDescriptor.height);

                PrepareHistory(camera, cameraDescriptor, viewportSource);
            }

            public void ForgetHistory(Camera camera)
            {
                if (camera == null) return;
                var id = camera.GetInstanceID();
                if (!histories.TryGetValue(id, out var history)) return;
                history.Release();
                histories.Remove(id);
                if (activeHistory == history) activeHistory = null;
            }

            private void PrepareHistory(
                Camera camera,
                RenderTextureDescriptor cameraDescriptor,
                Vector2 viewportSource)
            {
                deadCameras.Clear();
                foreach (var entry in histories)
                    if (entry.Value.camera == null) { entry.Value.Release(); deadCameras.Add(entry.Key); }
                foreach (var id in deadCameras) histories.Remove(id);
                if (camera == null)
                {
                    activeHistory = null;
                    return;
                }

                var cameraId = camera.GetInstanceID();
                if (!histories.TryGetValue(cameraId, out activeHistory))
                {
                    activeHistory = new CameraHistory { camera = camera };
                    histories.Add(cameraId, activeHistory);
                }

                cameraDescriptor.width = 1;
                cameraDescriptor.height = 1;
                cameraDescriptor.msaaSamples = 1;
                cameraDescriptor.depthBufferBits = 0;
                cameraDescriptor.graphicsFormat = GraphicsFormat.R16_SFloat;
                cameraDescriptor.depthStencilFormat = GraphicsFormat.None;
                activeHistory.EnsureAllocated(cameraDescriptor, cameraId);
                if (activeHistory.conservative != conservative) activeHistory.valid = false;

                var frameGap = Time.frameCount - activeHistory.lastFrame;
                deltaSeconds = Mathf.Max(0f, Time.realtimeSinceStartup - activeHistory.lastTime);
                if (frameGap > 1
                    || Vector2.Distance(activeHistory.lastSource, viewportSource) > 0.12f
                    || (activeHistory.valid && IsCameraCut(activeHistory.lastPosition, camera.transform.position,
                        activeHistory.lastRotation, camera.transform.rotation)))
                {
                    activeHistory.valid = false;
                }
                if (conservative && (ShouldResetVisibilityHistory(activeHistory.valid,
                    activeHistory.profileKey, profileKey, activeHistory.resolution, resolution,
                    activeHistory.lastPosition, camera.transform.position, activeHistory.lastRotation, camera.transform.rotation)
                    || deltaSeconds > .5f)) activeHistory.valid = false;
                cameraPosition = camera.transform.position; cameraRotation = camera.transform.rotation;
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
                var depth = conservative ? resourceData.cameraDepthTexture : resourceData.activeDepthTexture;
                var parameters = new ShaderParameters
                {
                    source = new Vector4(source.x, source.y, aspect, edgeReach),
                    parameters = new Vector4(intensity, radius, anisotropy, ghostReach),
                    optics = new Vector4(
                        haloThickness,
                        !conservative && primarySprite != null ? 1f : 0f,
                        !conservative && ringSprite != null ? 1f : 0f,
                        !conservative && apertureSprite != null ? 1f : 0f),
                    tint = tint,
                    energy = new Vector4(hdrEnergy, sunBrightness, 0f, 0f),
                    occlusion = new Vector4(occlusionRadius, coreOcclusion, aureoleOcclusion, ghostOcclusion),
                    visibilityMode = new Vector4(conservative ? 1f : 0f, depth.IsValid() ? 1f : 0f, 0f, 0f),
                };
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
                    depth,
                    previousOcclusion,
                    currentOcclusion,
                    activeHistory.valid && (!conservative || depth.IsValid()), parameters);

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
                passData.parameters = parameters;
                builder.UseTexture(sourceColor, AccessFlags.Read);
                builder.UseTexture(currentOcclusion, AccessFlags.Read);
                builder.SetRenderAttachment(destinationColor, 0, AccessFlags.Write);
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                {
                    ApplyShaderParameters(context, data.parameters);
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
                activeHistory.lastPosition = cameraPosition; activeHistory.lastRotation = cameraRotation;
                activeHistory.profileKey = profileKey;
                activeHistory.resolution = resolution;
                activeHistory.lastTime = Time.realtimeSinceStartup;
                activeHistory.conservative = conservative;
                activeHistory.Swap();
            }

            private void RecordOcclusionHistory(
                RenderGraph renderGraph,
                TextureHandle depth,
                TextureHandle previous,
                TextureHandle current,
                bool hasHistory,
                ShaderParameters parameters)
            {
                using var builder = renderGraph.AddRasterRenderPass<OcclusionPassData>(
                    "TopDown3D Sun Occlusion History",
                    out var passData,
                    profilingSampler);
                passData.material = material;
                passData.previous = previous;
                passData.stability = occlusionStability;
                passData.hasHistory = hasHistory;
                passData.parameters = parameters;
                passData.depth = depth;
                passData.attackRetention = Mathf.Exp(-deltaSeconds / attackSeconds);
                passData.releaseRetention = Mathf.Exp(-deltaSeconds / releaseSeconds);
                builder.UseTexture(previous, AccessFlags.Read);
                if (depth.IsValid())
                {
                    builder.UseTexture(depth, AccessFlags.Read);
                }

                builder.SetRenderAttachment(current, 0, AccessFlags.Write);
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc(static (OcclusionPassData data, RasterGraphContext context) =>
                {
                    ApplyShaderParameters(context, data.parameters);
                    if (data.parameters.visibilityMode.x > .5f && data.depth.IsValid())
                        context.cmd.SetGlobalTexture(Shader.PropertyToID("_CameraDepthTexture"), data.depth);
                    context.cmd.SetGlobalTexture(OcclusionHistoryId, data.previous);
                    context.cmd.SetGlobalVector(
                        TemporalId,
                        new Vector4(data.stability, data.hasHistory ? 1f : 0f, data.attackRetention, data.releaseRetention));
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
                public ShaderParameters parameters;
            }

            private sealed class OcclusionPassData
            {
                public Material material;
                public TextureHandle previous;
                public float stability;
                public bool hasHistory;
                public ShaderParameters parameters;
                public TextureHandle depth;
                public float attackRetention, releaseRetention;
            }

            private struct ShaderParameters
            {
                public Vector4 source, parameters, optics, energy, occlusion, visibilityMode;
                public Color tint;
            }

            private static void ApplyShaderParameters(RasterGraphContext context, ShaderParameters parameters)
            {
                context.cmd.SetGlobalVector(SourceId, parameters.source);
                context.cmd.SetGlobalVector(ParamsId, parameters.parameters);
                context.cmd.SetGlobalVector(OpticsId, parameters.optics);
                context.cmd.SetGlobalColor(TintId, parameters.tint);
                context.cmd.SetGlobalVector(EnergyId, parameters.energy);
                context.cmd.SetGlobalVector(OcclusionId, parameters.occlusion);
                context.cmd.SetGlobalVector(VisibilityModeId, parameters.visibilityMode);
            }

            private sealed class CameraHistory
            {
                public RTHandle read;
                public RTHandle write;
                public bool valid;
                public int lastFrame = -1;
                public Vector2 lastSource;
                public Vector3 lastPosition;
                public Quaternion lastRotation;
                public Camera camera;
                public int profileKey;
                public Vector2Int resolution;
                public float lastTime;
                public bool conservative;

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
