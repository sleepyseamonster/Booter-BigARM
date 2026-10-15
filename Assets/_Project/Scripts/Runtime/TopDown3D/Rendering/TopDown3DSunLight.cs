using UnityEngine;
using UnityEngine.Rendering.Universal;
namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent, RequireComponent(typeof(Light))]
    public sealed class TopDown3DSunLight : MonoBehaviour
    {
        public TopDown3DSunLightingProfile profile;
        public Light Light => GetComponent<Light>();
        public void Apply(PerpetualTwilightSun.SunState state)
        {
            if (!isActiveAndEnabled || profile == null) return;
            var light = Light;
            transform.rotation = Quaternion.Euler(state.ElevationDegrees, state.AzimuthDegrees, 0);
            light.type = LightType.Directional; light.color = profile.Tint(state.Brightness01);
            light.intensity = Mathf.Lerp(profile.deepIntensity, profile.brightIntensity, state.Brightness01);
            light.shadows = LightShadows.Soft; light.shadowStrength = profile.shadowStrength;
            light.shadowBias = profile.shadowBias; light.shadowNormalBias = profile.shadowNormalBias;
            light.shadowNearPlane = .1f; light.bounceIntensity = .2f;
            var data = light.GetUniversalAdditionalLightData();
            data.usePipelineSettings = false; data.softShadowQuality = SoftShadowQuality.High;
            RenderSettings.sun = light;
        }
    }
}
