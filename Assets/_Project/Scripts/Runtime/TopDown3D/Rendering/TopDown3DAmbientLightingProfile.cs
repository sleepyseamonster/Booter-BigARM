using UnityEngine;
namespace BooterBigArm.TopDown3D
{
    [CreateAssetMenu(menuName = "Booter & BigARM/Rendering/Ambient Lighting")]
    public sealed class TopDown3DAmbientLightingProfile : ScriptableObject
    {
        public Color deepSky, brightSky, deepEquator, brightEquator, deepGround, brightGround;
        public Color subtractiveShadow = new Color(.055f, .025f, .05f);
        public Cubemap reflection;
        public float deepReflectionIntensity = .65f, brightReflectionIntensity = 1.05f;
    }
}
