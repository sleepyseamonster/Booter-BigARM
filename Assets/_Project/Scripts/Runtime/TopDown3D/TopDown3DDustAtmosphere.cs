using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Volume))]
    [DefaultExecutionOrder(-350)]
    public sealed class TopDown3DDustAtmosphere : MonoBehaviour
    {
        public const float DefaultMinimumRegionalIntensity = 1.08f;
        public const float DefaultMaximumRegionalIntensity = 1.58f;
        public const float DefaultFogDensityAtIntensityOne = 0.0295f;

        public readonly struct DustSample
        {
            public DustSample(float intensity, float zoneInfluence, Color tint)
            {
                Intensity = intensity;
                ZoneInfluence = zoneInfluence;
                Tint = tint;
            }

            public float Intensity { get; }
            public float ZoneInfluence { get; }
            public Color Tint { get; }
        }

        [Header("Observer")]
        [SerializeField] private Transform subject;
        [SerializeField] private Camera outputCamera;
        [SerializeField] private int worldSeed = 24681357;

        [Header("Regional Dust")]
        [SerializeField, Range(0.25f, 3f)] private float minimumRegionalIntensity =
            DefaultMinimumRegionalIntensity;
        [SerializeField, Range(0.25f, 3f)] private float maximumRegionalIntensity =
            DefaultMaximumRegionalIntensity;
        [SerializeField, Min(0.0001f)] private float regionalFrequency = 0.0065f;
        [SerializeField, Min(0.01f)] private float responseSeconds = 1.8f;

        [Header("Distance Haze")]
        [SerializeField, Min(0.0001f)] private float fogDensityAtIntensityOne =
            DefaultFogDensityAtIntensityOne;
        [SerializeField] private Color deepTwilightDust = new Color(0.32f, 0.14f, 0.09f);
        [SerializeField] private Color brightTwilightDust = new Color(0.62f, 0.34f, 0.17f);
        [SerializeField, Range(0f, 1f)] private float zoneTintStrength = 0.42f;

        [Header("Close Haze")]
        [SerializeField, Range(0f, 80f)] private float moteEmissionAtIntensityOne = 30f;
        [SerializeField, Range(0f, 20f)] private float veilEmissionAtIntensityOne = 6.5f;
        [SerializeField] private Vector3 prevailingWind = new Vector3(0.14f, 0.015f, 0.05f);

        private Volume volume;
        private VolumeProfile runtimeProfile;
        private ColorAdjustments colorAdjustments;
        private Bloom bloom;
        private Vignette vignette;
        private ParticleSystem motes;
        private ParticleSystem veils;
        private Material particleMaterial;
        private Texture2D particleTexture;
        private float currentIntensity;
        private float intensityVelocity;
        private Color currentTint;
        private bool runtimeInitialized;
        private bool previousFogEnabled;
        private FogMode previousFogMode;
        private float previousFogDensity;
        private Color previousFogColor;
        private bool previousPostProcessing;

        public static TopDown3DDustAtmosphere Active { get; private set; }

        public float CurrentDustIntensity => currentIntensity;
        public float DustExposure01 => Mathf.InverseLerp(0.85f, 1.9f, currentIntensity);
        public float ApproximateVisibilityDistance => EvaluateHalfVisibilityDistance(CurrentFogDensity);
        public float CurrentFogDensity => fogDensityAtIntensityOne * Mathf.Max(0.25f, currentIntensity);

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (Active != null && Active != this)
            {
                enabled = false;
                return;
            }

            Active = this;
            InitializeRuntime();
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            ResolveReferences();
            if (!runtimeInitialized)
            {
                InitializeRuntime();
            }

            var position = subject != null ? subject.position : transform.position;
            var target = SampleAtPosition(position);
            currentIntensity = Mathf.SmoothDamp(
                currentIntensity,
                target.Intensity,
                ref intensityVelocity,
                responseSeconds);
            var colorResponse = 1f - Mathf.Exp(-Time.deltaTime / responseSeconds);
            currentTint = Color.Lerp(currentTint, target.Tint, colorResponse);

            ApplyAtmosphere();
            UpdateParticleAnchor();
        }

        private void OnDisable()
        {
            if (Active == this)
            {
                Active = null;
            }

            RestorePreviousState();
            ReleaseRuntimeResources();
        }

        private void OnValidate()
        {
            minimumRegionalIntensity = Mathf.Clamp(minimumRegionalIntensity, 0.25f, 3f);
            maximumRegionalIntensity = Mathf.Clamp(maximumRegionalIntensity, minimumRegionalIntensity, 3f);
            regionalFrequency = Mathf.Max(0.0001f, regionalFrequency);
            responseSeconds = Mathf.Max(0.01f, responseSeconds);
            fogDensityAtIntensityOne = Mathf.Max(0.0001f, fogDensityAtIntensityOne);
            zoneTintStrength = Mathf.Clamp01(zoneTintStrength);
            moteEmissionAtIntensityOne = Mathf.Clamp(moteEmissionAtIntensityOne, 0f, 80f);
            veilEmissionAtIntensityOne = Mathf.Clamp(veilEmissionAtIntensityOne, 0f, 20f);
        }

        public void Configure(Transform observedSubject, Camera renderingCamera, int seed)
        {
            subject = observedSubject;
            outputCamera = renderingCamera;
            worldSeed = seed;

            if (Application.isPlaying)
            {
                InitializeRuntime();
            }
        }

        public DustSample SampleAtPosition(Vector3 worldPosition)
        {
            var regionalIntensity = EvaluateRegionalIntensity(
                worldSeed,
                worldPosition,
                regionalFrequency,
                minimumRegionalIntensity,
                maximumRegionalIntensity);

            var weightedIntensity = 0f;
            var weightedTint = new Color(0f, 0f, 0f, 0f);
            var weightSum = 0f;
            var strongestWeight = 0f;
            foreach (var zone in TopDown3DDustZone.ActiveZones)
            {
                if (zone == null || !zone.isActiveAndEnabled)
                {
                    continue;
                }

                var weight = zone.SampleWeight(worldPosition);
                if (weight <= 0f)
                {
                    continue;
                }

                weightedIntensity += zone.DustIntensity * weight;
                weightedTint += zone.DustTint * weight;
                weightSum += weight;
                strongestWeight = Mathf.Max(strongestWeight, weight);
            }

            var twilightTint = EvaluateTwilightDustColor();
            if (weightSum <= 0f)
            {
                return new DustSample(regionalIntensity, 0f, twilightTint);
            }

            var zoneIntensity = weightedIntensity / weightSum;
            var zoneTint = weightedTint / weightSum;
            return new DustSample(
                Mathf.Lerp(regionalIntensity, zoneIntensity, strongestWeight),
                strongestWeight,
                Color.Lerp(twilightTint, zoneTint, strongestWeight * zoneTintStrength));
        }

        public static float EvaluateRegionalIntensity(
            int seed,
            Vector3 worldPosition,
            float frequency,
            float minimumIntensity,
            float maximumIntensity)
        {
            var safeFrequency = Mathf.Max(0.0001f, frequency);
            var minimum = Mathf.Min(minimumIntensity, maximumIntensity);
            var maximum = Mathf.Max(minimumIntensity, maximumIntensity);
            var offsetX = HashToOffset(seed, 0x2C9277B5u);
            var offsetZ = HashToOffset(seed, 0x9E3779B9u);
            var sampleX = (worldPosition.x + offsetX) * safeFrequency;
            var sampleZ = (worldPosition.z + offsetZ) * safeFrequency;
            var broad = Mathf.PerlinNoise(sampleX, sampleZ);
            var detail = Mathf.PerlinNoise((sampleX * 2.17f) + 31.7f, (sampleZ * 2.17f) - 47.3f);
            var field = Mathf.SmoothStep(0f, 1f, (broad * 0.78f) + (detail * 0.22f));
            return Mathf.Lerp(minimum, maximum, field);
        }

        public static float EvaluateHalfVisibilityDistance(float fogDensity)
        {
            return Mathf.Sqrt(Mathf.Log(2f)) / Mathf.Max(0.0001f, fogDensity);
        }

        private static float HashToOffset(int seed, uint salt)
        {
            unchecked
            {
                var value = (uint)seed ^ salt;
                value ^= value >> 16;
                value *= 0x7FEB352Du;
                value ^= value >> 15;
                value *= 0x846CA68Bu;
                value ^= value >> 16;
                return (value & 0x00FFFFFFu) * (2048f / 16777215f);
            }
        }

        private void InitializeRuntime()
        {
            if (runtimeInitialized || !Application.isPlaying)
            {
                return;
            }

            ResolveReferences();
            previousFogEnabled = RenderSettings.fog;
            previousFogMode = RenderSettings.fogMode;
            previousFogDensity = RenderSettings.fogDensity;
            previousFogColor = RenderSettings.fogColor;

            if (outputCamera != null)
            {
                var additionalCameraData = outputCamera.GetUniversalAdditionalCameraData();
                previousPostProcessing = additionalCameraData.renderPostProcessing;
                additionalCameraData.renderPostProcessing = true;
            }

            EnsurePostProcessing();
            EnsureParticles();
            var initial = SampleAtPosition(subject != null ? subject.position : transform.position);
            currentIntensity = initial.Intensity;
            currentTint = initial.Tint;
            intensityVelocity = 0f;
            runtimeInitialized = true;
            ApplyAtmosphere();
            UpdateParticleAnchor();
        }

        private void ResolveReferences()
        {
            if (subject == null)
            {
                var motor = FindAnyObjectByType<TopDown3DPlayerMotor>();
                subject = motor != null ? motor.transform : null;
            }

            if (outputCamera == null)
            {
                outputCamera = Camera.main;
            }

            if (worldSeed == 0)
            {
                var world = FindAnyObjectByType<TopDown3DProceduralWorld>();
                worldSeed = world != null ? world.WorldSeed : 24681357;
            }
        }

        private Color EvaluateTwilightDustColor()
        {
            var brightness = PerpetualTwilightSun.Active != null
                ? PerpetualTwilightSun.Active.Brightness01
                : 0.5f;
            return Color.Lerp(deepTwilightDust, brightTwilightDust, brightness);
        }

        private void ApplyAtmosphere()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = CurrentFogDensity;
            RenderSettings.fogColor = currentTint;

            var exposure = DustExposure01;
            if (colorAdjustments != null)
            {
                colorAdjustments.postExposure.value = Mathf.Lerp(-0.05f, -0.16f, exposure);
                colorAdjustments.contrast.value = Mathf.Lerp(-7f, -18f, exposure);
                colorAdjustments.saturation.value = Mathf.Lerp(-9f, -25f, exposure);
                colorAdjustments.colorFilter.value = Color.Lerp(
                    Color.white,
                    new Color(1.06f, 0.84f, 0.64f),
                    Mathf.Lerp(0.14f, 0.34f, exposure));
            }

            if (bloom != null)
            {
                bloom.intensity.value = Mathf.Lerp(0.1f, 0.34f, exposure);
                bloom.tint.value = Color.Lerp(Color.white, currentTint, 0.35f);
            }

            if (vignette != null)
            {
                vignette.intensity.value = Mathf.Lerp(0.045f, 0.12f, exposure);
            }

            UpdateParticleAppearance(motes, moteEmissionAtIntensityOne, false);
            UpdateParticleAppearance(veils, veilEmissionAtIntensityOne, true);
        }

        private void EnsurePostProcessing()
        {
            volume = GetComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.weight = 1f;

            runtimeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            runtimeProfile.name = "Runtime Dust Atmosphere Profile";
            runtimeProfile.hideFlags = HideFlags.DontSave;
            colorAdjustments = runtimeProfile.Add<ColorAdjustments>(true);
            bloom = runtimeProfile.Add<Bloom>(true);
            vignette = runtimeProfile.Add<Vignette>(true);
            bloom.threshold.value = 0.85f;
            bloom.scatter.value = 0.72f;
            bloom.highQualityFiltering.value = true;
            vignette.color.value = new Color(0.12f, 0.055f, 0.025f);
            vignette.smoothness.value = 0.55f;
            vignette.rounded.value = false;
            volume.sharedProfile = runtimeProfile;
        }

        private void EnsureParticles()
        {
            particleTexture = CreateSoftDustTexture();
            particleMaterial = CreateParticleMaterial(particleTexture);
            motes = CreateParticleLayer(
                "Suspended Dust Motes",
                420,
                new ParticleSystem.MinMaxCurve(8f, 14f),
                new ParticleSystem.MinMaxCurve(0.035f, 0.16f),
                new Vector3(38f, 10f, 38f),
                0.28f,
                0.12f);
            veils = CreateParticleLayer(
                "Close Dust Veils",
                150,
                new ParticleSystem.MinMaxCurve(10f, 17f),
                new ParticleSystem.MinMaxCurve(2.6f, 6.5f),
                new Vector3(42f, 11f, 42f),
                0.16f,
                0.06f);
        }

        private ParticleSystem CreateParticleLayer(
            string layerName,
            int maximumParticles,
            ParticleSystem.MinMaxCurve lifetime,
            ParticleSystem.MinMaxCurve size,
            Vector3 shapeScale,
            float turbulence,
            float maximumAlpha)
        {
            var layerObject = new GameObject(layerName);
            layerObject.transform.SetParent(transform, false);
            var particles = layerObject.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Shape;
            main.maxParticles = maximumParticles;
            main.startLifetime = lifetime;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.015f, 0.09f);
            main.startSize = size;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.78f, 0.54f, maximumAlpha * 0.55f),
                new Color(0.76f, 0.42f, 0.2f, maximumAlpha));

            var emission = particles.emission;
            emission.rateOverTime = 0f;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = shapeScale;

            var velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = prevailingWind.x;
            velocity.y = prevailingWind.y;
            velocity.z = prevailingWind.z;

            var noise = particles.noise;
            noise.enabled = true;
            noise.quality = ParticleSystemNoiseQuality.Medium;
            noise.strength = turbulence;
            noise.frequency = 0.18f;
            noise.scrollSpeed = 0.035f;
            noise.damping = true;

            var colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f),
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.16f),
                    new GradientAlphaKey(0.85f, 0.72f),
                    new GradientAlphaKey(0f, 1f),
                });
            colorOverLifetime.color = fade;

            var particleRenderer = particles.GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.sharedMaterial = particleMaterial;
            particleRenderer.shadowCastingMode = ShadowCastingMode.Off;
            particleRenderer.receiveShadows = false;
            particles.Play(true);
            return particles;
        }

        private void UpdateParticleAppearance(ParticleSystem particles, float emissionAtIntensityOne, bool veil)
        {
            if (particles == null)
            {
                return;
            }

            var emission = particles.emission;
            emission.rateOverTime = emissionAtIntensityOne * Mathf.Clamp(currentIntensity, 0.25f, 2.5f);
            var alpha = veil
                ? Mathf.Lerp(0.026f, 0.075f, DustExposure01)
                : Mathf.Lerp(0.058f, 0.145f, DustExposure01);
            var nearColor = Color.Lerp(Color.white, currentTint, veil ? 0.55f : 0.72f);
            var farColor = Color.Lerp(new Color(0.72f, 0.49f, 0.28f), currentTint, 0.8f);
            nearColor.a = alpha * 0.55f;
            farColor.a = alpha;
            var main = particles.main;
            main.startColor = new ParticleSystem.MinMaxGradient(nearColor, farColor);
        }

        private void UpdateParticleAnchor()
        {
            if (outputCamera == null)
            {
                return;
            }

            transform.position = outputCamera.transform.position + (outputCamera.transform.forward * 10f);
        }

        private static Texture2D CreateSoftDustTexture()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
            {
                name = "Runtime Soft Dust",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave,
            };
            var pixels = new Color[size * size];
            var random = new System.Random(7193);
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var centeredX = ((x + 0.5f) / size * 2f) - 1f;
                    var centeredY = ((y + 0.5f) / size * 2f) - 1f;
                    var radial = Mathf.Clamp01(1f - Mathf.Sqrt((centeredX * centeredX) + (centeredY * centeredY)));
                    var softness = radial * radial * (3f - (2f * radial));
                    var variation = Mathf.Lerp(0.82f, 1f, (float)random.NextDouble());
                    pixels[(y * size) + x] = new Color(1f, 1f, 1f, softness * variation);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Material CreateParticleMaterial(Texture2D texture)
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
            {
                return null;
            }

            var material = new Material(shader)
            {
                name = "Runtime Dust Particle Material",
                hideFlags = HideFlags.DontSave,
                renderQueue = (int)RenderQueue.Transparent,
            };
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            return material;
        }

        private void RestorePreviousState()
        {
            if (!runtimeInitialized)
            {
                return;
            }

            RenderSettings.fog = previousFogEnabled;
            RenderSettings.fogMode = previousFogMode;
            RenderSettings.fogDensity = previousFogDensity;
            RenderSettings.fogColor = previousFogColor;
            if (outputCamera != null)
            {
                outputCamera.GetUniversalAdditionalCameraData().renderPostProcessing = previousPostProcessing;
            }
        }

        private void ReleaseRuntimeResources()
        {
            runtimeInitialized = false;
            colorAdjustments = null;
            bloom = null;
            vignette = null;
            if (volume != null && volume.sharedProfile == runtimeProfile)
            {
                volume.sharedProfile = null;
            }

            DestroyRuntimeObject(motes != null ? motes.gameObject : null);
            DestroyRuntimeObject(veils != null ? veils.gameObject : null);
            DestroyRuntimeObject(runtimeProfile);
            DestroyRuntimeObject(particleMaterial);
            DestroyRuntimeObject(particleTexture);
            runtimeProfile = null;
            particleMaterial = null;
            particleTexture = null;
            motes = null;
            veils = null;
        }

        private static void DestroyRuntimeObject(UnityEngine.Object target)
        {
            if (target != null)
            {
                Destroy(target);
            }
        }
    }

    internal static class TopDown3DDustAtmosphereBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterForSceneLoads()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureCurrentSceneHasAtmosphere()
        {
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var world = FindInScene<TopDown3DProceduralWorld>(scene);
            if (world == null || FindInScene<TopDown3DDustAtmosphere>(scene) != null)
            {
                return;
            }

            var atmosphereObject = new GameObject("Dust Atmosphere");
            SceneManager.MoveGameObjectToScene(atmosphereObject, scene);
            var motor = FindInScene<TopDown3DPlayerMotor>(scene);
            var sceneCamera = FindInScene<Camera>(scene);
            atmosphereObject
                .AddComponent<TopDown3DDustAtmosphere>()
                .Configure(motor != null ? motor.transform : null, sceneCamera, world.WorldSeed);
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return null;
            }

            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var component = roots[i].GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }
    }
}
