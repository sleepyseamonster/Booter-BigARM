using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace BooterBigArm.TopDown3D
{
    public enum TopDown3DWorldBlurMode { Off, TiltShift, DepthOfField }

    [CreateAssetMenu(menuName = "Booter & BigARM/Rendering/Camera Optics Profile")]
    public sealed class TopDown3DCameraOpticsProfile : ScriptableObject
    {
        public const int CurrentVersion = 1;
        public int version = CurrentVersion;
        public TopDown3DCampOpticsProfile nativeOptics;
        public TopDown3DWorldBlurMode blurMode = TopDown3DWorldBlurMode.DepthOfField;
        public bool streakEnabled;
        public bool flareEnabled;
        [Range(0f, 2f)] public float flareIntensity = .16f;
        public bool useReferenceResolutionTiltShift;
        [Header("Legacy blur sampling; local to this camera module")]
        [Range(1, 4)] public int legacyDownsample = TopDown3DTiltShiftFeature.DefaultDownsample;
        [Range(0f, 1f)] public float legacyFocusCenter = TopDown3DTiltShiftFeature.DefaultFocusCenter;
        [Range(.05f, .8f)] public float legacySharpBandWidth = TopDown3DTiltShiftFeature.DefaultSharpBandWidth;
        [Range(.01f, .5f)] public float legacyFeatherWidth = TopDown3DTiltShiftFeature.DefaultFeatherWidth;
        [Range(.5f, 12f)] public float legacyBlurRadius = TopDown3DTiltShiftFeature.DefaultBlurRadius;
        [Header("Reference-resolution blur sampling")]
        [Range(1, 4)] public int tiltShiftDownsample = 2;
        [Range(360f, 4320f)] public float tiltShiftReferenceHeight = 1080f;
        [Range(0f, 24f)] public float tiltShiftBlurRadiusPixels = 7f;
        [Range(-90f, 90f)] public float tiltShiftBandAngleDegrees;
        [Range(0f, 1f)] public float tiltShiftFocusCenter = .5f;
        [Range(.05f, .8f)] public float tiltShiftSharpBandWidth = .3f;
        [Range(.01f, .5f)] public float tiltShiftFeatherWidth = .28f;

        public bool IsValid => version == CurrentVersion && nativeOptics != null && nativeOptics.IsValid
            && (int)blurMode >= 0 && (int)blurMode <= (int)TopDown3DWorldBlurMode.DepthOfField
            && float.IsFinite(flareIntensity) && float.IsFinite(tiltShiftReferenceHeight)
            && float.IsFinite(tiltShiftBlurRadiusPixels) && float.IsFinite(tiltShiftBandAngleDegrees)
            && float.IsFinite(tiltShiftFocusCenter) && float.IsFinite(tiltShiftSharpBandWidth)
            && float.IsFinite(tiltShiftFeatherWidth) && float.IsFinite(legacyFocusCenter)
            && float.IsFinite(legacySharpBandWidth) && float.IsFinite(legacyFeatherWidth)
            && float.IsFinite(legacyBlurRadius);
        public DepthOfFieldMode EffectiveDepthMode => IsValid && nativeOptics.opticsEnabled
            && blurMode == TopDown3DWorldBlurMode.DepthOfField ? nativeOptics.depthMode : DepthOfFieldMode.Off;
        public bool EffectiveTiltShift => IsValid && nativeOptics.opticsEnabled
            && blurMode == TopDown3DWorldBlurMode.TiltShift;
        public bool EffectiveStreak => IsValid && nativeOptics.opticsEnabled && streakEnabled
            && EffectiveDepthMode == DepthOfFieldMode.Off;
    }
}
