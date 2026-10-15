using UnityEngine;
namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    public sealed class TopDown3DSkyPresentation : MonoBehaviour
    {
        public Camera outputCamera;
        public TopDown3DSkyProfile profile;
        private Material owned, source, previous;
        private Skybox cameraSky;
        private bool created, previousEnabled;
        private int releaseFrame = -1;
        public Material PreviewSky => owned;
        public void Apply(Vector3 direction, float brightness)
        {
            if (!isActiveAndEnabled || outputCamera == null || profile == null || !profile.IsValid) { Release(); return; }
            if (source != profile.material || (cameraSky != null && cameraSky.gameObject != outputCamera.gameObject)) Release();
            if (Application.isPlaying && releaseFrame == Time.frameCount) return;
            if (owned == null)
            {
                cameraSky = outputCamera.GetComponent<Skybox>(); created = cameraSky == null;
                if (created)
                {
                    cameraSky = outputCamera.gameObject.AddComponent<Skybox>();
                    if (!Application.isPlaying) cameraSky.hideFlags = HideFlags.HideAndDontSave;
                }
                previous = cameraSky.material; previousEnabled = cameraSky.enabled;
                source = profile.material;
                owned = new Material(source) { name = "Runtime Modular Twilight Sky", hideFlags = HideFlags.HideAndDontSave };
                cameraSky.material = owned; cameraSky.enabled = true;
            }
            owned.CopyPropertiesFromMaterial(source);
            owned.SetVector("_SunDirection", direction);
            owned.SetFloat("_Exposure", Mathf.Lerp(profile.deepExposure, profile.brightExposure, brightness));
            if (owned.HasProperty("_TwilightBrightness")) owned.SetFloat("_TwilightBrightness", brightness);
            if (owned.HasProperty("_SunAngularRadius")) owned.SetFloat("_SunAngularRadius", profile.sunAngularDiameterDegrees * .5f);
        }
        public void Release()
        {
            if (cameraSky != null && cameraSky.material == owned && owned != null)
            {
                cameraSky.material = previous; cameraSky.enabled = previousEnabled;
                if (created) { if (Application.isPlaying) releaseFrame = Time.frameCount; Dispose(cameraSky); }
            }
            cameraSky = null; source = null;
            if (owned != null) Dispose(owned);
            owned = null; created = false;
        }
        private void OnDisable() => Release();
        private void OnDestroy() => Release();
        private static void Dispose(Object value) { if (Application.isPlaying) Destroy(value); else DestroyImmediate(value); }
    }
}
