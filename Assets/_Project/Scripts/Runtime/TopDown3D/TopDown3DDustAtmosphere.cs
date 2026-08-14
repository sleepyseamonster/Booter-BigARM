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
        public const float DefaultClumpFrequency = 0.038f;
        public const float DefaultClumpStrength = 0.58f;
        public const float DefaultPocketCellSize = 144f;
        public const float DefaultPocketSpawnChance = 0.72f;
        public const float DefaultMinimumPocketRadius = 54f;
        public const float DefaultMaximumPocketRadius = 72f;
        public const float DefaultPocketEdgeBlend = 18f;
        public const float DefaultPocketCenterJitter = 0.2f;
        public const float DefaultFogDensityAtIntensityOne = 0.0295f;
        public const float DefaultChromaticAberrationIntensity = 0.12f;
        public const float DefaultMaximumForwardPhase = 1.65f;
        public const float DefaultMoteEmissionAtIntensityOne = 56f;
        public const float DefaultVeilEmissionAtIntensityOne = 6.5f;
        public const float DefaultClearAirMoteEmissionMultiplier = 0.9f;
        public const float DefaultClearAirVeilEmissionMultiplier = 0.35f;
        public static readonly bool DefaultGlobalHazeEnabled = false;
        public const string SunlitMoteShaderName = "Universal Render Pipeline/Particles/Lit";
        public const string SoftVeilShaderName = "Universal Render Pipeline/Particles/Unlit";
        public const int DensityMapResolution = 64;
        public const float DensityMapExtent = 128f;
        public const float DensityMapRefreshSeconds = 0.25f;
        public static readonly Color DefaultDeepRustDust = new Color(0.38f, 0.11f, 0.055f);
        public static readonly Color DefaultBrightRustDust = new Color(0.68f, 0.24f, 0.09f);
        public static readonly Color DefaultDenseRustDust = new Color(0.74f, 0.205f, 0.07f);
        public static readonly Color DefaultShelteredRustDust = new Color(0.43f, 0.16f, 0.08f);
        public static readonly Color DefaultRustColorFilter = new Color(1.07f, 0.76f, 0.62f);
        public static readonly Color DefaultRustParticleDust = new Color(0.66f, 0.24f, 0.09f);

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

        public readonly struct VolumetricRenderState
        {
            public VolumetricRenderState(
                Texture2D densityMap,
                Vector4 densityMapParams,
                Color tint,
                float extinctionAtIntensityOne,
                float scatteringAlbedo,
                float anisotropy,
                float maximumForwardPhase,
                float ambientScattering,
                float maximumMarchDistance)
            {
                DensityMap = densityMap;
                DensityMapParams = densityMapParams;
                Tint = tint;
                ExtinctionAtIntensityOne = extinctionAtIntensityOne;
                ScatteringAlbedo = scatteringAlbedo;
                Anisotropy = anisotropy;
                MaximumForwardPhase = maximumForwardPhase;
                AmbientScattering = ambientScattering;
                MaximumMarchDistance = maximumMarchDistance;
            }

            public Texture2D DensityMap { get; }
            public Vector4 DensityMapParams { get; }
            public Color Tint { get; }
            public float ExtinctionAtIntensityOne { get; }
            public float ScatteringAlbedo { get; }
            public float Anisotropy { get; }
            public float MaximumForwardPhase { get; }
            public float AmbientScattering { get; }
            public float MaximumMarchDistance { get; }
        }

        [Header("Runtime Posture")]
        [SerializeField] private bool globalHazeEnabled = DefaultGlobalHazeEnabled;

        [Header("Observer")]
        [SerializeField] private Transform subject;
        [SerializeField] private Camera outputCamera;
        [SerializeField] private int worldSeed = 24681357;

        [Header("Procedural Dust Pockets")]
        [SerializeField, Min(18f)] private float pocketCellSize = DefaultPocketCellSize;
        [SerializeField, Range(0f, 1f)] private float pocketSpawnChance = DefaultPocketSpawnChance;
        [SerializeField, Min(1f)] private float minimumPocketRadius = DefaultMinimumPocketRadius;
        [SerializeField, Min(1f)] private float maximumPocketRadius = DefaultMaximumPocketRadius;
        [SerializeField, Min(0f)] private float pocketEdgeBlend = DefaultPocketEdgeBlend;
        [SerializeField, Range(0f, 0.4f)] private float pocketCenterJitter = DefaultPocketCenterJitter;

        [Header("Dust Inside Pockets")]
        [SerializeField, Range(0.25f, 3f)] private float minimumRegionalIntensity =
            DefaultMinimumRegionalIntensity;
        [SerializeField, Range(0.25f, 3f)] private float maximumRegionalIntensity =
            DefaultMaximumRegionalIntensity;
        [SerializeField, Min(0.0001f)] private float regionalFrequency = 0.0065f;
        [SerializeField, Min(0.0001f)] private float clumpFrequency = DefaultClumpFrequency;
        [SerializeField, Range(0f, 1f)] private float clumpStrength = DefaultClumpStrength;
        [SerializeField, Min(0.01f)] private float responseSeconds = 1.8f;

        [Header("Distance Haze")]
        [SerializeField, Min(0.0001f)] private float fogDensityAtIntensityOne =
            DefaultFogDensityAtIntensityOne;
        [SerializeField] private Color deepTwilightDust = DefaultDeepRustDust;
        [SerializeField] private Color brightTwilightDust = DefaultBrightRustDust;
        [SerializeField, Range(0f, 1f)] private float zoneTintStrength = 0.42f;
        [SerializeField, Range(0f, 1f)] private float scatteringAlbedo = 0.86f;
        [SerializeField, Range(-0.9f, 0.9f)] private float scatteringAnisotropy = 0.5f;
        [SerializeField, Range(1f, 4f)] private float maximumForwardPhase =
            DefaultMaximumForwardPhase;
        [SerializeField, Range(0f, 1f)] private float ambientScattering = 0.18f;
        [SerializeField, Range(10f, 100f)] private float maximumMarchDistance = 60f;

        [Header("Lens Treatment")]
        [SerializeField, Range(0f, 1f)] private float chromaticAberrationIntensity =
            DefaultChromaticAberrationIntensity;

        [Header("Close Haze")]
        [SerializeField, Range(0f, 120f)] private float moteEmissionAtIntensityOne =
            DefaultMoteEmissionAtIntensityOne;
        [SerializeField, Range(0f, 20f)] private float veilEmissionAtIntensityOne =
            DefaultVeilEmissionAtIntensityOne;
        [SerializeField] private Vector3 prevailingWind = new Vector3(0.14f, 0.015f, 0.05f);

        private Volume volume;
        private VolumeProfile runtimeProfile;
        private ColorAdjustments colorAdjustments;
        private Bloom bloom;
        private Vignette vignette;
        private ChromaticAberration chromaticAberration;
        private ParticleSystem motes;
        private ParticleSystem veils;
        private Material particleMaterial;
        private Material veilParticleMaterial;
        private Mesh moteParticleMesh;
        private Texture2D particleTexture;
        private Texture2D densityMap;
        private float[] densityMapPixels;
        private Vector2 densityMapMinimum;
        private Vector4 densityMapParams;
        private float nextDensityMapRefreshTime;
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

        public bool GlobalHazeEnabled => globalHazeEnabled;
        public float CurrentDustIntensity => currentIntensity;
        public float DustExposure01 => Mathf.InverseLerp(0.85f, 1.9f, currentIntensity);
        public float ApproximateVisibilityDistance => EvaluateHalfVisibilityDistance(CurrentFogDensity);
        public float CurrentFogDensity => fogDensityAtIntensityOne * Mathf.Max(0f, currentIntensity);
        public float ExtinctionAtIntensityOne =>
            TopDown3DDustOptics.ConvertLegacyDensityToExtinction(fogDensityAtIntensityOne);

        private void OnEnable()
        {
            if (!Application.isPlaying || !globalHazeEnabled)
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
            if (!Application.isPlaying || !globalHazeEnabled)
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

            UpdateDensityMap(false);
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
            pocketCellSize = Mathf.Max(18f, pocketCellSize);
            pocketSpawnChance = Mathf.Clamp01(pocketSpawnChance);
            minimumPocketRadius = Mathf.Max(1f, minimumPocketRadius);
            maximumPocketRadius = Mathf.Max(minimumPocketRadius, maximumPocketRadius);
            pocketEdgeBlend = Mathf.Clamp(pocketEdgeBlend, 0f, minimumPocketRadius);
            pocketCenterJitter = Mathf.Clamp(pocketCenterJitter, 0f, 0.4f);
            minimumRegionalIntensity = Mathf.Clamp(minimumRegionalIntensity, 0.25f, 3f);
            maximumRegionalIntensity = Mathf.Clamp(maximumRegionalIntensity, minimumRegionalIntensity, 3f);
            regionalFrequency = Mathf.Max(0.0001f, regionalFrequency);
            clumpFrequency = Mathf.Max(0.0001f, clumpFrequency);
            clumpStrength = Mathf.Clamp01(clumpStrength);
            responseSeconds = Mathf.Max(0.01f, responseSeconds);
            fogDensityAtIntensityOne = Mathf.Max(0.0001f, fogDensityAtIntensityOne);
            zoneTintStrength = Mathf.Clamp01(zoneTintStrength);
            scatteringAlbedo = Mathf.Clamp01(scatteringAlbedo);
            scatteringAnisotropy = Mathf.Clamp(scatteringAnisotropy, -0.9f, 0.9f);
            maximumForwardPhase = Mathf.Clamp(maximumForwardPhase, 1f, 4f);
            ambientScattering = Mathf.Clamp01(ambientScattering);
            maximumMarchDistance = Mathf.Clamp(maximumMarchDistance, 10f, 100f);
            chromaticAberrationIntensity = Mathf.Clamp01(chromaticAberrationIntensity);
            moteEmissionAtIntensityOne = Mathf.Clamp(moteEmissionAtIntensityOne, 0f, 120f);
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
            var regionalIntensity = EvaluatePocketIntensity(
                worldSeed,
                worldPosition,
                pocketCellSize,
                pocketSpawnChance,
                minimumPocketRadius,
                maximumPocketRadius,
                pocketEdgeBlend,
                pocketCenterJitter,
                regionalFrequency,
                clumpFrequency,
                clumpStrength,
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

        public bool TryGetVolumetricRenderState(Camera camera, out VolumetricRenderState state)
        {
            if (!globalHazeEnabled
                || !runtimeInitialized
                || camera == null
                || camera != outputCamera
                || densityMap == null)
            {
                state = default;
                return false;
            }

            state = new VolumetricRenderState(
                densityMap,
                densityMapParams,
                currentTint,
                ExtinctionAtIntensityOne,
                scatteringAlbedo,
                scatteringAnisotropy,
                maximumForwardPhase,
                ambientScattering,
                maximumMarchDistance);
            return true;
        }

        public static float EvaluateRegionalIntensity(
            int seed,
            Vector3 worldPosition,
            float frequency,
            float minimumIntensity,
            float maximumIntensity)
        {
            return EvaluateRegionalIntensity(
                seed,
                worldPosition,
                frequency,
                DefaultClumpFrequency,
                DefaultClumpStrength,
                minimumIntensity,
                maximumIntensity);
        }

        public static float EvaluateRegionalIntensity(
            int seed,
            Vector3 worldPosition,
            float frequency,
            float densityClumpFrequency,
            float densityClumpStrength,
            float minimumIntensity,
            float maximumIntensity)
        {
            var safeFrequency = Mathf.Max(0.0001f, frequency);
            var safeClumpFrequency = Mathf.Max(0.0001f, densityClumpFrequency);
            var minimum = Mathf.Min(minimumIntensity, maximumIntensity);
            var maximum = Mathf.Max(minimumIntensity, maximumIntensity);
            var offsetX = HashToOffset(seed, 0x2C9277B5u);
            var offsetZ = HashToOffset(seed, 0x9E3779B9u);
            var sampleX = (worldPosition.x + offsetX) * safeFrequency;
            var sampleZ = (worldPosition.z + offsetZ) * safeFrequency;
            var broad = Mathf.PerlinNoise(sampleX, sampleZ);
            var detail = Mathf.PerlinNoise((sampleX * 2.17f) + 31.7f, (sampleZ * 2.17f) - 47.3f);
            var clumpOffsetX = HashToOffset(seed, 0xD1B54A35u);
            var clumpOffsetZ = HashToOffset(seed, 0x94D049BBu);
            var clumps = Mathf.PerlinNoise(
                (worldPosition.x + clumpOffsetX) * safeClumpFrequency,
                (worldPosition.z + clumpOffsetZ) * safeClumpFrequency);
            var broadField = (broad * 0.72f) + (detail * 0.28f);
            var field = EvaluateClumpEmphasis(broadField, clumps, densityClumpStrength);
            return Mathf.Lerp(minimum, maximum, field);
        }

        public static float EvaluateClumpEmphasis(
            float broadField,
            float clumpNoise,
            float densityClumpStrength)
        {
            var clumpShape = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.40f, 0.64f, clumpNoise));
            var emphasized = Mathf.Lerp(
                Mathf.Clamp01(broadField),
                clumpShape,
                Mathf.Clamp01(densityClumpStrength));
            return Mathf.SmoothStep(0f, 1f, emphasized);
        }

        public static float EvaluatePocketIntensity(
            int seed,
            Vector3 worldPosition,
            float cellSize,
            float spawnChance,
            float minimumRadius,
            float maximumRadius,
            float edgeBlend,
            float centerJitter,
            float regionalFrequency,
            float minimumIntensity,
            float maximumIntensity)
        {
            return EvaluatePocketIntensity(
                seed,
                worldPosition,
                cellSize,
                spawnChance,
                minimumRadius,
                maximumRadius,
                edgeBlend,
                centerJitter,
                regionalFrequency,
                DefaultClumpFrequency,
                DefaultClumpStrength,
                minimumIntensity,
                maximumIntensity);
        }

        public static float EvaluatePocketIntensity(
            int seed,
            Vector3 worldPosition,
            float cellSize,
            float spawnChance,
            float minimumRadius,
            float maximumRadius,
            float edgeBlend,
            float centerJitter,
            float regionalFrequency,
            float densityClumpFrequency,
            float densityClumpStrength,
            float minimumIntensity,
            float maximumIntensity)
        {
            var safeCellSize = Mathf.Max(1f, cellSize);
            var safeSpawnChance = Mathf.Clamp01(spawnChance);
            var safeMinimumRadius = Mathf.Max(1f, Mathf.Min(minimumRadius, maximumRadius));
            var safeMaximumRadius = Mathf.Max(safeMinimumRadius, Mathf.Max(minimumRadius, maximumRadius));
            var safeEdgeBlend = Mathf.Clamp(edgeBlend, 0f, safeMinimumRadius);
            var safeCenterJitter = Mathf.Clamp(centerJitter, 0f, 0.4f);
            var baseCellX = Mathf.FloorToInt(worldPosition.x / safeCellSize);
            var baseCellZ = Mathf.FloorToInt(worldPosition.z / safeCellSize);
            var neighborRange = Mathf.Max(
                1,
                Mathf.CeilToInt(
                    (safeMaximumRadius / safeCellSize)
                    + safeCenterJitter
                    - 0.5f));
            var regionalIntensity = EvaluateRegionalIntensity(
                seed,
                worldPosition,
                regionalFrequency,
                densityClumpFrequency,
                densityClumpStrength,
                minimumIntensity,
                maximumIntensity);
            var strongestInfluence = 0f;

            for (var offsetZ = -neighborRange; offsetZ <= neighborRange; offsetZ++)
            {
                for (var offsetX = -neighborRange; offsetX <= neighborRange; offsetX++)
                {
                    var cellX = baseCellX + offsetX;
                    var cellZ = baseCellZ + offsetZ;
                    var identity = HashCell(seed, cellX, cellZ, 0xA511E9B3u);
                    if (safeSpawnChance <= 0f
                        || (safeSpawnChance < 1f && HashToUnit(identity) >= safeSpawnChance))
                    {
                        continue;
                    }

                    var jitterDistance = safeCellSize * safeCenterJitter;
                    var centerX = (cellX + 0.5f) * safeCellSize
                        + (HashToSignedUnit(HashCell(seed, cellX, cellZ, 0x63D83595u)) * jitterDistance);
                    var centerZ = (cellZ + 0.5f) * safeCellSize
                        + (HashToSignedUnit(HashCell(seed, cellX, cellZ, 0xB5297A4Du)) * jitterDistance);
                    var radius = Mathf.Lerp(
                        safeMinimumRadius,
                        safeMaximumRadius,
                        HashToUnit(HashCell(seed, cellX, cellZ, 0x68E31DA4u)));
                    var aspect = Mathf.Lerp(
                        0.72f,
                        1f,
                        HashToUnit(HashCell(seed, cellX, cellZ, 0x1B56C4E9u)));
                    var radiusX = radius;
                    var radiusZ = radius * aspect;
                    if (HashToUnit(HashCell(seed, cellX, cellZ, 0xC2B2AE35u)) < 0.5f)
                    {
                        (radiusX, radiusZ) = (radiusZ, radiusX);
                    }

                    var angle = HashToUnit(HashCell(seed, cellX, cellZ, 0x27D4EB2Fu))
                        * Mathf.PI
                        * 2f;
                    var sine = Mathf.Sin(angle);
                    var cosine = Mathf.Cos(angle);
                    var deltaX = worldPosition.x - centerX;
                    var deltaZ = worldPosition.z - centerZ;
                    var localX = (deltaX * cosine) + (deltaZ * sine);
                    var localZ = (-deltaX * sine) + (deltaZ * cosine);
                    var normalizedDistance = Mathf.Sqrt(
                        ((localX * localX) / (radiusX * radiusX))
                        + ((localZ * localZ) / (radiusZ * radiusZ)));
                    if (normalizedDistance >= 1f)
                    {
                        continue;
                    }

                    var distanceInsideEdge = (1f - normalizedDistance) * Mathf.Min(radiusX, radiusZ);
                    var influence = safeEdgeBlend <= 0f
                        ? 1f
                        : Mathf.SmoothStep(0f, 1f, distanceInsideEdge / safeEdgeBlend);
                    strongestInfluence = Mathf.Max(strongestInfluence, influence);
                }
            }

            return regionalIntensity * strongestInfluence;
        }

        public static float EvaluateHalfVisibilityDistance(float fogDensity)
        {
            return fogDensity > 0f
                ? Mathf.Sqrt(Mathf.Log(2f)) / fogDensity
                : float.PositiveInfinity;
        }

        public static float EvaluateParticleEmissionRate(
            float emissionAtIntensityOne,
            float regionalIntensity,
            float clearAirMultiplier)
        {
            var response = Mathf.Max(
                Mathf.Clamp01(clearAirMultiplier),
                Mathf.Clamp(regionalIntensity, 0f, 2.5f));
            return Mathf.Max(0f, emissionAtIntensityOne) * response;
        }

        private static uint HashCell(int seed, int cellX, int cellZ, uint salt)
        {
            unchecked
            {
                var value = (uint)seed ^ salt;
                value ^= (uint)cellX * 0x9E3779B9u;
                value ^= (uint)cellZ * 0x85EBCA6Bu;
                value ^= value >> 16;
                value *= 0x7FEB352Du;
                value ^= value >> 15;
                value *= 0x846CA68Bu;
                value ^= value >> 16;
                return value;
            }
        }

        private static float HashToUnit(uint value)
        {
            return (value & 0x00FFFFFFu) / 16777215f;
        }

        private static float HashToSignedUnit(uint value)
        {
            return (HashToUnit(value) * 2f) - 1f;
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
            if (runtimeInitialized || !Application.isPlaying || !globalHazeEnabled)
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
            EnsureDensityMap();
            var initial = SampleAtPosition(subject != null ? subject.position : transform.position);
            currentIntensity = initial.Intensity;
            currentTint = initial.Tint;
            intensityVelocity = 0f;
            runtimeInitialized = true;
            UpdateDensityMap(true);
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
            RenderSettings.fog = false;

            var exposure = DustExposure01;
            if (colorAdjustments != null)
            {
                colorAdjustments.postExposure.value = Mathf.Lerp(0f, -0.16f, exposure);
                colorAdjustments.contrast.value = Mathf.Lerp(0f, -18f, exposure);
                colorAdjustments.saturation.value = Mathf.Lerp(0f, -25f, exposure);
                colorAdjustments.colorFilter.value = Color.Lerp(
                    Color.white,
                    DefaultRustColorFilter,
                    0.34f * exposure);
            }

            if (bloom != null)
            {
                bloom.intensity.value = Mathf.Lerp(0f, 0.34f, exposure);
                bloom.tint.value = Color.Lerp(Color.white, currentTint, 0.35f);
            }

            if (vignette != null)
            {
                vignette.intensity.value = Mathf.Lerp(0f, 0.12f, exposure);
            }

            if (chromaticAberration != null)
            {
                chromaticAberration.intensity.value = chromaticAberrationIntensity;
            }

            UpdateParticleAppearance(motes, moteEmissionAtIntensityOne, false);
            UpdateParticleAppearance(veils, veilEmissionAtIntensityOne, true);
        }

        private void EnsureDensityMap()
        {
            if (densityMap != null)
            {
                return;
            }

            densityMap = new Texture2D(
                DensityMapResolution,
                DensityMapResolution,
                TextureFormat.RFloat,
                false,
                true)
            {
                name = "Runtime TopDown3D Dust Density",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave,
            };
            densityMapPixels = new float[DensityMapResolution * DensityMapResolution];
            nextDensityMapRefreshTime = 0f;
        }

        private void UpdateDensityMap(bool force)
        {
            if (densityMap == null || densityMapPixels == null)
            {
                return;
            }

            var center = outputCamera != null
                ? outputCamera.transform.position
                : subject != null
                    ? subject.position
                    : transform.position;
            var snappedMinimum = TopDown3DDustOptics.SnapDensityMapMinimum(
                center,
                DensityMapExtent,
                DensityMapResolution);
            var movedToNewCell = snappedMinimum != densityMapMinimum;
            if (!force && !movedToNewCell && Time.unscaledTime < nextDensityMapRefreshTime)
            {
                return;
            }

            densityMapMinimum = snappedMinimum;
            var cellSize = DensityMapExtent / DensityMapResolution;
            for (var y = 0; y < DensityMapResolution; y++)
            {
                for (var x = 0; x < DensityMapResolution; x++)
                {
                    var samplePosition = new Vector3(
                        densityMapMinimum.x + ((x + 0.5f) * cellSize),
                        center.y,
                        densityMapMinimum.y + ((y + 0.5f) * cellSize));
                    densityMapPixels[(y * DensityMapResolution) + x] =
                        SampleAtPosition(samplePosition).Intensity;
                }
            }

            densityMap.SetPixelData(densityMapPixels, 0);
            densityMap.Apply(false, false);
            var inverseExtent = 1f / DensityMapExtent;
            densityMapParams = new Vector4(
                densityMapMinimum.x,
                densityMapMinimum.y,
                inverseExtent,
                inverseExtent);
            nextDensityMapRefreshTime = Time.unscaledTime + DensityMapRefreshSeconds;
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
            chromaticAberration = runtimeProfile.Add<ChromaticAberration>(true);
            chromaticAberration.intensity.value = chromaticAberrationIntensity;
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
            moteParticleMesh = CreateDustMoteMesh();
            particleMaterial = CreateParticleMaterial(Texture2D.whiteTexture, true);
            veilParticleMaterial = CreateParticleMaterial(particleTexture, false);
            motes = CreateParticleLayer(
                "Suspended Dust Motes",
                1100,
                new ParticleSystem.MinMaxCurve(10f, 18f),
                new ParticleSystem.MinMaxCurve(0.045f, 0.17f),
                new Vector3(34f, 8f, 34f),
                0.28f,
                0.15f,
                particleMaterial,
                moteParticleMesh);
            veils = CreateParticleLayer(
                "Close Dust Veils",
                150,
                new ParticleSystem.MinMaxCurve(10f, 17f),
                new ParticleSystem.MinMaxCurve(2.6f, 6.5f),
                new Vector3(42f, 11f, 42f),
                0.16f,
                0.06f,
                veilParticleMaterial,
                null);
        }

        private ParticleSystem CreateParticleLayer(
            string layerName,
            int maximumParticles,
            ParticleSystem.MinMaxCurve lifetime,
            ParticleSystem.MinMaxCurve size,
            Vector3 shapeScale,
            float turbulence,
            float maximumAlpha,
            Material layerMaterial,
            Mesh layerMesh)
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
            main.startRotation3D = layerMesh != null;
            if (layerMesh != null)
            {
                main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            }
            else
            {
                main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            }
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
            particleRenderer.renderMode = layerMesh != null
                ? ParticleSystemRenderMode.Mesh
                : ParticleSystemRenderMode.Billboard;
            if (layerMesh != null)
            {
                particleRenderer.mesh = layerMesh;
            }
            particleRenderer.sharedMaterial = layerMaterial;
            particleRenderer.shadowCastingMode = ShadowCastingMode.Off;
            particleRenderer.receiveShadows = layerMesh != null;
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
            emission.rateOverTime = EvaluateParticleEmissionRate(
                emissionAtIntensityOne,
                currentIntensity,
                veil
                    ? DefaultClearAirVeilEmissionMultiplier
                    : DefaultClearAirMoteEmissionMultiplier);
            var alpha = veil
                ? Mathf.Lerp(0.032f, 0.082f, DustExposure01)
                : Mathf.Lerp(0.085f, 0.18f, DustExposure01);
            var nearColor = Color.Lerp(Color.white, currentTint, veil ? 0.55f : 0.72f);
            var farColor = Color.Lerp(DefaultRustParticleDust, currentTint, 0.8f);
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

        internal static Texture2D CreateSoftDustTexture()
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

        private static Mesh CreateDustMoteMesh()
        {
            var mesh = new Mesh
            {
                name = "Runtime Sunlit Dust Mote",
                hideFlags = HideFlags.DontSave,
            };
            mesh.SetVertices(new[]
            {
                new Vector3(0f, 0.5f, 0f),
                new Vector3(0f, -0.5f, 0f),
                new Vector3(-0.5f, 0f, 0f),
                new Vector3(0.5f, 0f, 0f),
                new Vector3(0f, 0f, 0.5f),
                new Vector3(0f, 0f, -0.5f),
            });
            mesh.SetTriangles(new[]
            {
                0, 4, 3,
                0, 2, 4,
                0, 5, 2,
                0, 3, 5,
                1, 3, 4,
                1, 4, 2,
                1, 2, 5,
                1, 5, 3,
            }, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.UploadMeshData(true);
            return mesh;
        }

        internal static Material CreateParticleMaterial(Texture2D texture, bool receivesSunlight)
        {
            var shader = Shader.Find(receivesSunlight ? SunlitMoteShaderName : SoftVeilShaderName);
            if (shader == null)
            {
                return null;
            }

            var material = new Material(shader)
            {
                name = receivesSunlight
                    ? "Runtime Sunlit Dust Mote Material"
                    : "Runtime Soft Dust Veil Material",
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
            if (receivesSunlight)
            {
                material.SetFloat("_Metallic", 0.03f);
                material.SetFloat("_Smoothness", 0.72f);
                material.SetFloat("_ReceiveShadows", 1f);
                material.DisableKeyword("_RECEIVE_SHADOWS_OFF");
            }
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
            chromaticAberration = null;
            if (volume != null && volume.sharedProfile == runtimeProfile)
            {
                volume.sharedProfile = null;
            }

            DestroyRuntimeObject(motes != null ? motes.gameObject : null);
            DestroyRuntimeObject(veils != null ? veils.gameObject : null);
            DestroyRuntimeObject(runtimeProfile);
            DestroyRuntimeObject(particleMaterial);
            DestroyRuntimeObject(veilParticleMaterial);
            DestroyRuntimeObject(moteParticleMesh);
            DestroyRuntimeObject(particleTexture);
            DestroyRuntimeObject(densityMap);
            runtimeProfile = null;
            particleMaterial = null;
            veilParticleMaterial = null;
            moteParticleMesh = null;
            particleTexture = null;
            densityMap = null;
            densityMapPixels = null;
            densityMapParams = default;
            nextDensityMapRefreshTime = 0f;
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
            if (!TopDown3DDustAtmosphere.DefaultGlobalHazeEnabled)
            {
                return;
            }

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
