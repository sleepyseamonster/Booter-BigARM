using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Light))]
    [DefaultExecutionOrder(-500)]
    public sealed class PerpetualTwilightSun : MonoBehaviour
    {
        public readonly struct SunState
        {
            public SunState(float elevationDegrees, float azimuthDegrees, float brightness01)
            {
                ElevationDegrees = elevationDegrees;
                AzimuthDegrees = azimuthDegrees;
                Brightness01 = brightness01;
            }

            public float ElevationDegrees { get; }
            public float AzimuthDegrees { get; }
            public float Brightness01 { get; }
        }

        private const float MinimumCycleDurationSeconds = 1f;
        private const string PanoramaMaterialResourcePath = "TopDown3D/MartianPanoramaSky";
        public const float DefaultCycleDurationSeconds = 1200f;

        [Header("Cycle")]
        [SerializeField] private bool cycleEnabled = true;
        [SerializeField, Min(MinimumCycleDurationSeconds)] private float cycleDurationSeconds =
            DefaultCycleDurationSeconds;
        [SerializeField, Range(0f, 1f)] private float cycle01 = 0.12f;

        [Header("Sun Path")]
        [SerializeField, Range(1f, 45f)] private float centerElevationDegrees = 19f;
        [SerializeField, Range(0f, 20f)] private float elevationAmplitudeDegrees = 7f;
        [SerializeField, Range(-180f, 180f)] private float centerAzimuthDegrees = -32f;
        [SerializeField, Range(0f, 45f)] private float azimuthAmplitudeDegrees = 14f;

        [Header("Direct Light")]
        [SerializeField] private Color deepTwilightSunColor = new Color(1f, 0.38f, 0.16f);
        [SerializeField] private Color brightTwilightSunColor = new Color(1f, 0.79f, 0.52f);
        [SerializeField, Min(0f)] private float deepTwilightIntensity = 0.68f;
        [SerializeField, Min(0f)] private float brightTwilightIntensity = 1.18f;
        [SerializeField, Range(0f, 1f)] private float shadowStrength = 0.96f;
        [SerializeField, Range(0f, 2f)] private float shadowBias = 0.2f;
        [SerializeField, Range(0f, 3f)] private float shadowNormalBias = 0.35f;

        [Header("World Fill")]
        [SerializeField] private Color deepTwilightAmbient = new Color(0.16f, 0.075f, 0.10f);
        [SerializeField] private Color brightTwilightAmbient = new Color(0.38f, 0.23f, 0.17f);
        [SerializeField] private Color deepTwilightSkyFill = new Color(0.12f, 0.13f, 0.19f);
        [SerializeField] private Color brightTwilightSkyFill = new Color(0.27f, 0.29f, 0.34f);
        [SerializeField] private Color deepTwilightGroundFill = new Color(0.045f, 0.025f, 0.035f);
        [SerializeField] private Color brightTwilightGroundFill = new Color(0.12f, 0.07f, 0.055f);
        [Header("Sky")]
        [SerializeField] private Color deepTwilightSkyTint = new Color(0.36f, 0.10f, 0.12f);
        [SerializeField] private Color brightTwilightSkyTint = new Color(0.86f, 0.34f, 0.16f);
        [SerializeField] private Color skyGroundColor = new Color(0.11f, 0.045f, 0.035f);

        [SerializeField, HideInInspector] private Light sun;

        private UniversalAdditionalLightData additionalSunData;
        private Material runtimeSkybox;
        private Material previousSkybox;
        private float brightness01;
        private TopDown3DAtmosphereStudyProfile studyProfile;
        private bool studyStateCaptured;
        private AmbientMode previousAmbientMode;
        private Color previousAmbientSky, previousAmbientEquator, previousAmbientGround, previousSubtractive;
        private Light previousSun;
        private Color previousLightColor;
        private float previousLightIntensity;
        private Quaternion previousRotation;
        private LightType previousLightType;
        private LightShadows previousShadows;
        private float previousShadowStrength, previousShadowBias, previousShadowNormalBias, previousShadowNearPlane, previousBounceIntensity;
        private bool previousPipelineSettings;
        private SoftShadowQuality previousSoftShadowQuality;
        private DefaultReflectionMode previousReflectionMode;
        private Texture previousReflection;
        private float previousReflectionIntensity;
        private Camera studyCamera;
        private Material studyTemplate;
        private int cameraSkyboxReleaseFrame = -1;
        private Skybox studyCameraSkybox;
        private Material previousCameraSkybox;
        private bool previousCameraSkyboxEnabled, createdCameraSkybox;
        private bool staticStudyPreview;

        public static PerpetualTwilightSun Active { get; private set; }

        // RenderSettings belongs to the active scene. Restore inactive scenes only when Unity
        // activates them; never change the user's scene merely to access its environment.
        private sealed class EnvironmentSnapshot
        {
            public Light owner, previousSun;
            public AmbientMode mode;
            public Color sky, equator, ground, subtractive;
            public DefaultReflectionMode reflectionMode;
            public Texture reflection;
            public float reflectionIntensity;
            public void Restore()
            {
                if (RenderSettings.sun != owner) return;
                RenderSettings.sun = previousSun;
                RenderSettings.ambientMode = mode;
                RenderSettings.ambientSkyColor = sky;
                RenderSettings.ambientEquatorColor = equator;
                RenderSettings.ambientGroundColor = ground;
                RenderSettings.subtractiveShadowColor = subtractive;
                RenderSettings.defaultReflectionMode = reflectionMode;
                RenderSettings.customReflectionTexture = reflection;
                RenderSettings.reflectionIntensity = reflectionIntensity;
            }
        }
        private static readonly Dictionary<int, EnvironmentSnapshot> pendingRestores = new Dictionary<int, EnvironmentSnapshot>();
        private static void RegisterRestoreCallbacks()
        {
            SceneManager.activeSceneChanged -= RestoreActivatedScene;
            SceneManager.activeSceneChanged += RestoreActivatedScene;
            SceneManager.sceneUnloaded -= ForgetUnloadedScene;
            SceneManager.sceneUnloaded += ForgetUnloadedScene;
        }
        private static void RestoreActivatedScene(Scene previous, Scene current)
        {
            if (!pendingRestores.TryGetValue(current.handle, out var snapshot)) return;
            pendingRestores.Remove(current.handle);
            snapshot.Restore();
        }
        private static void ForgetUnloadedScene(Scene scene) => pendingRestores.Remove(scene.handle);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            Active = null;
            pendingRestores.Clear();
            RegisterRestoreCallbacks();
        }

        public float Cycle01 => cycle01;
        public float CycleDurationSeconds => cycleDurationSeconds;
        public float Brightness01 => brightness01;
        public bool CycleEnabled => cycleEnabled;
        public Vector3 DirectionToSun => sun != null ? -sun.transform.forward : Vector3.up;
        public Vector3 LightTravelDirection => sun != null ? sun.transform.forward : Vector3.down;

        private void Awake()
        {
            ResolveSun();
        }

        private void OnEnable()
        {
            ResolveSun();
            ApplyCurrentState();
        }

        private void Update()
        {
            // Presentation ownership does not change the existing scaled simulation clock.
            if (cycleEnabled)
            {
                cycle01 = Mathf.Repeat(
                    cycle01 + (Time.deltaTime / Mathf.Max(MinimumCycleDurationSeconds, cycleDurationSeconds)),
                    1f);
            }

            ApplyCurrentState();
        }

        private void OnDisable()
        {
            if (Active == this)
            {
                Active = null;
            }
            ReleaseStudyState();
            studyProfile = null;
        }

        private void OnDestroy()
        {
            ReleaseStudyState();
            if (runtimeSkybox == null)
            {
                return;
            }

            if (SceneManager.GetActiveScene() == gameObject.scene && RenderSettings.skybox == runtimeSkybox)
            {
                RenderSettings.skybox = previousSkybox;
            }

            DisposeOwnedObject(runtimeSkybox);
            runtimeSkybox = null;
        }

        private void OnValidate()
        {
            cycleDurationSeconds = Mathf.Max(MinimumCycleDurationSeconds, cycleDurationSeconds);
            elevationAmplitudeDegrees = Mathf.Min(
                elevationAmplitudeDegrees,
                Mathf.Max(0f, centerElevationDegrees - 1f));
            ResolveSun();

            if (!Application.isPlaying)
            {
                ApplyCurrentState();
            }
        }

        public void Configure(Light directionalSun)
        {
            sun = directionalSun;
            ApplyCurrentState();
        }

        public void SetCycle01(float normalizedCycle)
        {
            cycle01 = Mathf.Repeat(normalizedCycle, 1f);
            ApplyCurrentState();
        }

        public void SetCycleEnabled(bool enabled)
        {
            cycleEnabled = enabled;
        }

        public static SunState EvaluateCycle(
            float normalizedCycle,
            float centerElevation,
            float elevationAmplitude,
            float centerAzimuth,
            float azimuthAmplitude)
        {
            var phaseRadians = Mathf.Repeat(normalizedCycle, 1f) * Mathf.PI * 2f;
            var phaseHeight = Mathf.Sin(phaseRadians);
            var phaseSide = Mathf.Cos(phaseRadians);
            return new SunState(
                centerElevation + (phaseHeight * elevationAmplitude),
                centerAzimuth + (phaseSide * azimuthAmplitude),
                0.5f + (phaseHeight * 0.5f));
        }

        private void ResolveSun()
        {
            if (sun == null)
            {
                sun = GetComponent<Light>();
            }

            if (sun != null && additionalSunData == null)
            {
                additionalSunData = sun.GetUniversalAdditionalLightData();
            }
        }

        private void ApplyCurrentState()
        {
            if (!ReconcilePresentation()) return;
            if (sun == null)
            {
                return;
            }

            var state = EvaluateCycle(
                cycle01,
                centerElevationDegrees,
                elevationAmplitudeDegrees,
                centerAzimuthDegrees,
                azimuthAmplitudeDegrees);
            brightness01 = state.Brightness01;
            var profile = studyProfile;

            transform.rotation = Quaternion.Euler(state.ElevationDegrees, state.AzimuthDegrees, 0f);
            sun.type = LightType.Directional;
            sun.color = Color.Lerp(profile != null ? profile.deepSunColor : deepTwilightSunColor,
                profile != null ? profile.brightSunColor : brightTwilightSunColor, brightness01);
            sun.intensity = Mathf.Lerp(deepTwilightIntensity, brightTwilightIntensity, brightness01);
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = shadowStrength;
            sun.shadowBias = shadowBias;
            sun.shadowNormalBias = shadowNormalBias;
            sun.shadowNearPlane = 0.1f;
            sun.bounceIntensity = 0.2f;
            if (additionalSunData != null)
            {
                additionalSunData.usePipelineSettings = false;
                additionalSunData.softShadowQuality = SoftShadowQuality.High;
            }

            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Color.Lerp(
                profile != null ? profile.deepSkyFill : deepTwilightSkyFill,
                profile != null ? profile.brightSkyFill : brightTwilightSkyFill,
                brightness01);
            RenderSettings.ambientEquatorColor = Color.Lerp(
                profile != null ? profile.deepEquatorFill : deepTwilightAmbient,
                profile != null ? profile.brightEquatorFill : brightTwilightAmbient,
                brightness01);
            RenderSettings.ambientGroundColor = Color.Lerp(
                profile != null ? profile.deepGroundFill : deepTwilightGroundFill,
                profile != null ? profile.brightGroundFill : brightTwilightGroundFill,
                brightness01);
            RenderSettings.subtractiveShadowColor = new Color(0.055f, 0.025f, 0.05f);
            if (profile != null && profile.sunlessReflection != null)
            {
                RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
                RenderSettings.customReflectionTexture = profile.sunlessReflection;
                RenderSettings.reflectionIntensity = Mathf.Lerp(profile.deepSkyExposure, profile.brightSkyExposure, brightness01);
            }
            if (runtimeSkybox != null)
            {
                if (runtimeSkybox.HasProperty("_SunDirection"))
                {
                    // The painted sun follows the authoritative light, including the twilight cycle.
                    runtimeSkybox.SetVector("_SunDirection", DirectionToSun);
                    ApplyStudySunSize(runtimeSkybox, profile);
                    if (runtimeSkybox.HasProperty("_TwilightBrightness")) runtimeSkybox.SetFloat("_TwilightBrightness", brightness01);
                    runtimeSkybox.SetFloat("_Exposure", Mathf.Lerp(profile != null ? profile.deepSkyExposure : .65f,
                        profile != null ? profile.brightSkyExposure : 1.05f, brightness01));
                }
                else
                {
                    runtimeSkybox.SetColor(
                        "_SkyTint",
                        Color.Lerp(deepTwilightSkyTint, brightTwilightSkyTint, brightness01));
                    runtimeSkybox.SetFloat("_Exposure", Mathf.Lerp(0.55f, 0.82f, brightness01));
                }
            }
        }

        private void EnsureRuntimeSkybox()
        {
            if ((!Application.isPlaying && !staticStudyPreview) || runtimeSkybox != null)
            {
                return;
            }

            // A study must never fall back to a global sky. Its camera was validated before capture/allocation.
            if (studyProfile != null && (studyCamera == null || !studyProfile.IsValid)) return;
            var panoramaTemplate = studyProfile != null ? studyProfile.skyMaterial : Resources.Load<Material>(PanoramaMaterialResourcePath);
            if (panoramaTemplate != null
                && panoramaTemplate.shader != null
                && panoramaTemplate.shader.isSupported
                && panoramaTemplate.mainTexture != null)
            {
                previousSkybox = RenderSettings.skybox;
                runtimeSkybox = new Material(panoramaTemplate)
                {
                    name = "Runtime Martian Panorama Sky",
                    hideFlags = HideFlags.DontSave,
                };
                if (studyProfile != null)
                {
                    var camera = studyCamera;
                    studyCameraSkybox = camera.GetComponent<Skybox>();
                    createdCameraSkybox = studyCameraSkybox == null;
                    if (createdCameraSkybox) studyCameraSkybox = camera.gameObject.AddComponent<Skybox>();
                    previousCameraSkybox = studyCameraSkybox.material;
                    previousCameraSkyboxEnabled = studyCameraSkybox.enabled;
                    studyCameraSkybox.material = runtimeSkybox; studyCameraSkybox.enabled = true;
                }
                else RenderSettings.skybox = runtimeSkybox;
                return;
            }

            if (studyProfile != null) return;

            var shader = Shader.Find("Skybox/Procedural");
            if (shader == null)
            {
                return;
            }

            previousSkybox = RenderSettings.skybox;
            runtimeSkybox = new Material(shader)
            {
                name = "Runtime Perpetual Twilight Sky",
                hideFlags = HideFlags.DontSave,
            };
            runtimeSkybox.SetFloat("_SunDisk", 2f);
            runtimeSkybox.SetFloat("_SunSize", 0.055f);
            runtimeSkybox.SetFloat("_SunSizeConvergence", 5f);
            runtimeSkybox.SetFloat("_AtmosphereThickness", 1.35f);
            runtimeSkybox.SetColor("_GroundColor", skyGroundColor);
            RenderSettings.skybox = runtimeSkybox;
        }

        // Static candidate captures use the same cycle and color path without advancing simulation.
        public void ApplyStudyPreview(float phase, Material sky)
        {
            TopDown3DSceneRenderingOverride.TryGetAtmosphereProfile(gameObject.scene, out var profile);
            if (profile == null || sky == null || !sky.HasProperty("_SunDirection") || !sky.HasProperty("_Exposure")
                || TopDown3DSceneRenderingOverride.AtmosphereCamera(gameObject.scene) == null
                || SceneManager.GetActiveScene() != gameObject.scene)
                throw new System.InvalidOperationException("An active scene with a unique valid camera study profile is required.");
            staticStudyPreview = true;
            SetCycle01(phase);
            sky.SetVector("_SunDirection", DirectionToSun);
            ApplyStudySunSize(sky, profile);
            if (sky.HasProperty("_TwilightBrightness")) sky.SetFloat("_TwilightBrightness", brightness01);
            sky.SetFloat("_Exposure", Mathf.Lerp(profile.deepSkyExposure, profile.brightSkyExposure, brightness01));
        }

        public void EndStudyPreview()
        {
            staticStudyPreview = false;
            ReleaseStudyState();
            studyProfile = null;
            if (Active == this) Active = null;
        }

        private static void ApplyStudySunSize(Material material, TopDown3DAtmosphereStudyProfile profile)
        {
            if (profile == null || !material.HasProperty("_SunAngularRadius")) return;
            float radius = profile.nativeOptics != null && profile.nativeOptics.IsValid
                ? Mathf.Clamp(profile.nativeOptics.sunAngularDiameterDegrees, .05f, 5f) * .5f
                : profile.skyMaterial.GetFloat("_SunAngularRadius");
            material.SetFloat("_SunAngularRadius", radius);
        }

        private void CaptureStudyState()
        {
            if (studyProfile == null || studyStateCaptured) return;
            previousSkybox = RenderSettings.skybox; previousSun = RenderSettings.sun;
            previousAmbientMode = RenderSettings.ambientMode;
            previousAmbientSky = RenderSettings.ambientSkyColor; previousAmbientEquator = RenderSettings.ambientEquatorColor;
            previousAmbientGround = RenderSettings.ambientGroundColor; previousSubtractive = RenderSettings.subtractiveShadowColor;
            previousLightColor = sun.color; previousLightIntensity = sun.intensity;
            previousRotation = transform.rotation; previousLightType = sun.type; previousShadows = sun.shadows;
            previousShadowStrength = sun.shadowStrength; previousShadowBias = sun.shadowBias;
            previousShadowNormalBias = sun.shadowNormalBias; previousShadowNearPlane = sun.shadowNearPlane;
            previousBounceIntensity = sun.bounceIntensity;
            previousReflectionMode = RenderSettings.defaultReflectionMode;
            previousReflection = RenderSettings.customReflectionTexture;
            previousReflectionIntensity = RenderSettings.reflectionIntensity;
            if (additionalSunData != null)
            {
                previousPipelineSettings = additionalSunData.usePipelineSettings;
                previousSoftShadowQuality = additionalSunData.softShadowQuality;
            }
            studyStateCaptured = true;
        }

        private void ReleaseStudyState()
        {
            if (!studyStateCaptured) return;
            if (sun != null)
            {
                transform.rotation = previousRotation; sun.type = previousLightType;
                sun.color = previousLightColor; sun.intensity = previousLightIntensity; sun.shadows = previousShadows;
                sun.shadowStrength = previousShadowStrength; sun.shadowBias = previousShadowBias;
                sun.shadowNormalBias = previousShadowNormalBias; sun.shadowNearPlane = previousShadowNearPlane;
                sun.bounceIntensity = previousBounceIntensity;
                if (additionalSunData != null)
                {
                    additionalSunData.usePipelineSettings = previousPipelineSettings;
                    additionalSunData.softShadowQuality = previousSoftShadowQuality;
                }
            }
            var snapshot = new EnvironmentSnapshot { owner = sun, previousSun = previousSun,
                mode = previousAmbientMode, sky = previousAmbientSky, equator = previousAmbientEquator,
                ground = previousAmbientGround, subtractive = previousSubtractive,
                reflectionMode = previousReflectionMode, reflection = previousReflection, reflectionIntensity = previousReflectionIntensity };
            if (SceneManager.GetActiveScene() == gameObject.scene) snapshot.Restore();
            else if (gameObject.scene.IsValid() && gameObject.scene.isLoaded)
            {
                RegisterRestoreCallbacks();
                pendingRestores[gameObject.scene.handle] = snapshot;
            }
            if (studyCameraSkybox != null && studyCameraSkybox.material == runtimeSkybox)
            {
                studyCameraSkybox.material = previousCameraSkybox; studyCameraSkybox.enabled = previousCameraSkyboxEnabled;
                if (createdCameraSkybox)
                {
                    // Destroy is deferred in Play Mode. Do not acquire this doomed component
                    // again if a binding is changed more than once during the same frame.
                    if (Application.isPlaying) cameraSkyboxReleaseFrame = Time.frameCount;
                    DisposeOwnedObject(studyCameraSkybox);
                }
            }
            studyCameraSkybox = null;
            if (runtimeSkybox != null) DisposeOwnedObject(runtimeSkybox);
            runtimeSkybox = null; studyStateCaptured = false;
            studyCamera = null; studyTemplate = null; createdCameraSkybox = false;
        }

        private bool ReconcilePresentation()
        {
            if (SceneManager.GetActiveScene() != gameObject.scene)
            {
                if (Active == this) Active = null;
                ReleaseStudyState();
                studyProfile = null;
                return false;
            }
            RestoreActivatedScene(default, gameObject.scene);
            TopDown3DSceneRenderingOverride.TryGetAtmosphereProfile(gameObject.scene, out var requested);
            var requestedCamera = requested != null ? TopDown3DSceneRenderingOverride.AtmosphereCamera(gameObject.scene) : null;
            bool invalidBinding = TopDown3DSceneRenderingOverride.AtmosphereBindingCount(gameObject.scene) > 0
                && (requested == null || requestedCamera == null || requested.skyMaterial.shader == null
                    || !requested.skyMaterial.shader.isSupported || requested.skyMaterial.mainTexture == null);
            if (studyProfile != requested || studyCamera != requestedCamera
                || (requested != null && studyTemplate != requested.skyMaterial) || invalidBinding)
            {
                if (studyStateCaptured) ReleaseStudyState();
                else if (runtimeSkybox != null)
                {
                    if (RenderSettings.skybox == runtimeSkybox) RenderSettings.skybox = previousSkybox;
                    DisposeOwnedObject(runtimeSkybox); runtimeSkybox = null;
                }
                studyProfile = requested;
                studyCamera = requestedCamera;
                studyTemplate = requested != null ? requested.skyMaterial : null;
            }
            if (invalidBinding || sun == null || (requested != null && Application.isPlaying
                && cameraSkyboxReleaseFrame == Time.frameCount))
            {
                if (Active == this) Active = null;
                return false;
            }
            Active = this;
            CaptureStudyState();
            EnsureRuntimeSkybox();
            return true;
        }

        private static void DisposeOwnedObject(Object owned)
        {
            if (Application.isPlaying) Destroy(owned);
            else DestroyImmediate(owned);
        }
    }

    internal static class PerpetualTwilightSunBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterForSceneLoads()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (Object.FindAnyObjectByType<TopDown3DProceduralWorld>() == null
                || Object.FindAnyObjectByType<PerpetualTwilightSun>() != null)
            {
                return;
            }

            var lights = Object.FindObjectsByType<Light>();
            Light directionalSun = null;
            foreach (var candidate in lights)
            {
                if (candidate.type == LightType.Directional)
                {
                    directionalSun = candidate;
                    break;
                }
            }

            if (directionalSun == null)
            {
                var sunObject = new GameObject("Perpetual Twilight Sun");
                directionalSun = sunObject.AddComponent<Light>();
                directionalSun.type = LightType.Directional;
            }

            directionalSun.gameObject
                .AddComponent<PerpetualTwilightSun>()
                .Configure(directionalSun);
        }
    }
}
