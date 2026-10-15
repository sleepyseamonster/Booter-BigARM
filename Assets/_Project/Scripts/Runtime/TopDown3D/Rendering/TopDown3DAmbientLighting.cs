using UnityEngine;
using UnityEngine.Rendering;
namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    public sealed class TopDown3DAmbientLighting : MonoBehaviour
    {
        public TopDown3DAmbientLightingProfile profile;
        public void Apply(float brightness)
        {
            if (!isActiveAndEnabled || profile == null) return;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Color.Lerp(profile.deepSky, profile.brightSky, brightness);
            RenderSettings.ambientEquatorColor = Color.Lerp(profile.deepEquator, profile.brightEquator, brightness);
            RenderSettings.ambientGroundColor = Color.Lerp(profile.deepGround, profile.brightGround, brightness);
            RenderSettings.subtractiveShadowColor = profile.subtractiveShadow;
            if (profile.reflection == null) return;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = profile.reflection;
            RenderSettings.reflectionIntensity = Mathf.Lerp(profile.deepReflectionIntensity, profile.brightReflectionIntensity, brightness);
        }
    }
}
