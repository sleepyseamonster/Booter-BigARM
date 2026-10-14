using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [CreateAssetMenu(menuName = "Booter & BigARM/Rendering/Atmosphere Study Profile")]
    public sealed class TopDown3DAtmosphereStudyProfile : ScriptableObject
    {
        public Material skyMaterial;
        public Cubemap sunlessReflection;
        [Header("Twilight presentation only; the existing sun owns direction and time")]
        public Color deepSunColor = new Color(1f, .48f, .10f);
        public Color brightSunColor = new Color(1f, .72f, .20f);
        public Color deepSkyFill = new Color(.18f, .085f, .04f);
        public Color brightSkyFill = new Color(.34f, .22f, .12f);
        public Color deepEquatorFill = new Color(.15f, .075f, .045f);
        public Color brightEquatorFill = new Color(.40f, .23f, .12f);
        public Color deepGroundFill = new Color(.055f, .03f, .02f);
        public Color brightGroundFill = new Color(.15f, .085f, .045f);
        [Range(.1f, 2f)] public float deepSkyExposure = .65f;
        [Range(.1f, 2f)] public float brightSkyExposure = 1.05f;
        [Header("Per-camera custom optics; shared renderer assets remain unchanged")]
        public TopDown3DCampOpticsProfile nativeOptics;
        public bool tiltShiftEnabled;
        public bool streakEnabled;
        public bool flareEnabled;
        public bool bloomEnabled = true;
        [Range(0f, 2f)] public float bloomIntensity = .10f;
        [Range(0f, 2f)] public float sunAureoleIntensity = .12f;
        public Color bloomTint = new Color(1f, .72f, .20f);
        [Range(0f, 2f)] public float flareIntensity = .16f;
        [Range(3, 7)] public int bloomLevels = 4;
        [Range(1, 4)] public int bloomDownsample = 2;

        [Header("Opt-in reference-resolution artistic blur; radius is one tap step")]
        public bool useReferenceResolutionTiltShift;
        [Range(1, 4)] public int tiltShiftDownsample = 2;
        [Range(360f, 4320f)] public float tiltShiftReferenceHeight = 1080f;
        [Range(0f, 24f)] public float tiltShiftBlurRadiusPixels = 7f;
        [Range(-90f, 90f)] public float tiltShiftBandAngleDegrees;
        [Range(0f, 1f)] public float tiltShiftFocusCenter = .5f;
        [Range(.05f, .8f)] public float tiltShiftSharpBandWidth = .3f;
        [Range(.01f, .5f)] public float tiltShiftFeatherWidth = .28f;

        public bool IsValid => skyMaterial != null && skyMaterial.shader != null && skyMaterial.shader.isSupported && skyMaterial.mainTexture != null
            && skyMaterial.HasProperty("_SunDirection") && skyMaterial.HasProperty("_Exposure");
    }
}
