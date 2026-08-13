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

        [Header("Cycle")]
        [SerializeField] private bool cycleEnabled = true;
        [SerializeField, Min(MinimumCycleDurationSeconds)] private float cycleDurationSeconds = 1200f;
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
        [Header("Sky")]
        [SerializeField] private Color deepTwilightSkyTint = new Color(0.36f, 0.10f, 0.12f);
        [SerializeField] private Color brightTwilightSkyTint = new Color(0.86f, 0.34f, 0.16f);
        [SerializeField] private Color skyGroundColor = new Color(0.11f, 0.045f, 0.035f);

        [SerializeField, HideInInspector] private Light sun;

        private UniversalAdditionalLightData additionalSunData;
        private Material runtimeSkybox;
        private Material previousSkybox;
        private float brightness01;

        public static PerpetualTwilightSun Active { get; private set; }

        public float Cycle01 => cycle01;
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
            Active = this;
            ResolveSun();
            EnsureRuntimeSkybox();
            ApplyCurrentState();
        }

        private void Update()
        {
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
        }

        private void OnDestroy()
        {
            if (runtimeSkybox == null)
            {
                return;
            }

            if (RenderSettings.skybox == runtimeSkybox)
            {
                RenderSettings.skybox = previousSkybox;
            }

            Destroy(runtimeSkybox);
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

            transform.rotation = Quaternion.Euler(state.ElevationDegrees, state.AzimuthDegrees, 0f);
            sun.type = LightType.Directional;
            sun.color = Color.Lerp(deepTwilightSunColor, brightTwilightSunColor, brightness01);
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
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Color.Lerp(deepTwilightAmbient, brightTwilightAmbient, brightness01);
            RenderSettings.subtractiveShadowColor = new Color(0.055f, 0.025f, 0.05f);
            if (runtimeSkybox != null)
            {
                runtimeSkybox.SetColor(
                    "_SkyTint",
                    Color.Lerp(deepTwilightSkyTint, brightTwilightSkyTint, brightness01));
                runtimeSkybox.SetFloat("_Exposure", Mathf.Lerp(0.55f, 0.82f, brightness01));
            }
        }

        private void EnsureRuntimeSkybox()
        {
            if (!Application.isPlaying || runtimeSkybox != null)
            {
                return;
            }

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
