using UnityEngine;
namespace BooterBigArm.TopDown3D
{
    [CreateAssetMenu(menuName = "Booter & BigARM/Rendering/Sun Lighting")]
    public sealed class TopDown3DSunLightingProfile : ScriptableObject
    {
        public Color deepColor, brightColor;
        [Min(0)] public float deepIntensity = .68f, brightIntensity = 1.18f;
        [Range(0, 1)] public float shadowStrength = .96f;
        public float shadowBias = .2f, shadowNormalBias = .35f;
        public Color Tint(float phaseBrightness) => Color.Lerp(deepColor, brightColor, phaseBrightness);
    }
}
