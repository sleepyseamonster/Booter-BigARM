using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace BooterBigArm.TopDown3D
{
    [CreateAssetMenu(menuName = "Booter & BigARM/Rendering/Camp Optics Profile")]
    public sealed class TopDown3DCampOpticsProfile : ScriptableObject
    {
        public const int CurrentVersion = 1;
        public int version = CurrentVersion;
        public bool opticsEnabled = true;
        [HideInInspector] public bool collectDiagnostics;
        [Range(-4f, 4f)] public float postExposure;
        public TonemappingMode tonemapping = TonemappingMode.Neutral;
        public bool bloomEnabled = true;
        [Range(0f, 2f)] public float bloomIntensity = .18f;
        [Range(0f, 5f)] public float bloomThreshold = 1f;
        [Range(0f, 1f)] public float bloomScatter = .65f;
        public bool highQualityBloom = true;
        public BloomFilterMode bloomFilter = BloomFilterMode.Gaussian;
        public BloomDownscaleMode bloomDownscale = BloomDownscaleMode.Half;
        [Range(2, 8)] public int bloomMaxIterations = 6;
        public Texture dirtTexture;
        [Range(0f, 5f)] public float dirtIntensity = .3f;
        public DepthOfFieldMode depthMode = DepthOfFieldMode.Off;
        public bool gaussianBeforeLens;
        public bool gaussianHighQualitySampling = true;
        [Min(.1f)] public float focusDistance = 20f;
        public bool followPlayerFocus;
        [Range(.01f, 2f)] public float focusSmoothingSeconds = .25f;
        [Min(0f)] public float readableFarPadding = 5f;
        [Min(.1f)] public float farBlurTransition = 45f;
        [Min(.1f)] public float farStart = 25f;
        [Min(.2f)] public float farEnd = 70f;
        [Range(1f, 32f)] public float aperture = 8f;
        [Range(1f, 150f)] public float focalLength = 50f;
        [Range(0f, 2f)] public float gaussianRadius = .6f;
        [Range(.05f, 5f)] public float sunAngularDiameterDegrees = .7f;
        [Range(.001f, 1f)] public float flareVisibilityAttackSeconds = .06f;
        [Range(.001f, 1f)] public float flareVisibilityReleaseSeconds = .12f;
        public bool IsValid => version == CurrentVersion && float.IsFinite(postExposure)
            && float.IsFinite(focusDistance) && focusDistance > 0f
            && float.IsFinite(farStart) && float.IsFinite(farEnd) && farEnd > farStart
            && float.IsFinite(bloomIntensity) && float.IsFinite(bloomThreshold) && float.IsFinite(bloomScatter)
            && float.IsFinite(dirtIntensity) && float.IsFinite(aperture) && float.IsFinite(focalLength)
            && float.IsFinite(gaussianRadius) && float.IsFinite(focusSmoothingSeconds)
            && float.IsFinite(readableFarPadding) && float.IsFinite(farBlurTransition)
            && float.IsFinite(sunAngularDiameterDegrees) && float.IsFinite(flareVisibilityAttackSeconds)
            && float.IsFinite(flareVisibilityReleaseSeconds)
            && (int)depthMode >= 0 && (int)depthMode <= (int)DepthOfFieldMode.Bokeh
            && (!gaussianBeforeLens || depthMode != DepthOfFieldMode.Bokeh)
            && (tonemapping == TonemappingMode.None || tonemapping == TonemappingMode.Neutral || tonemapping == TonemappingMode.ACES)
            && (bloomFilter == BloomFilterMode.Gaussian || bloomFilter == BloomFilterMode.Dual || bloomFilter == BloomFilterMode.Kawase)
            && (bloomDownscale == BloomDownscaleMode.Half || bloomDownscale == BloomDownscaleMode.Quarter);
    }
}
