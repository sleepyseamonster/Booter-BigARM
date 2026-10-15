using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace BooterBigArm.TopDown3D
{
    // Coordinates leases and explicit references. Clock, light, fill and sky remain independent modules.
    [ExecuteAlways, DisallowMultipleComponent, DefaultExecutionOrder(-500)]
    public sealed class TopDown3DEnvironmentRig : MonoBehaviour
    {
        [HideInInspector] public int migrationVersion;
        public PerpetualTwilightSun compatibilitySun;
        public TopDown3DTwilightClock clock;
        public TopDown3DSunLight sunLight;
        public TopDown3DAmbientLighting ambient;
        public TopDown3DSkyPresentation sky;
        private static readonly HashSet<TopDown3DEnvironmentRig> active = new HashSet<TopDown3DEnvironmentRig>();
        private static readonly Dictionary<int, Lease> pending = new Dictionary<int, Lease>();
        private Lease lease;
        private TopDown3DSunLight acquiredSun;
        private TopDown3DAmbientLighting acquiredAmbient;
        private TopDown3DSkyPresentation acquiredSky;
        private TopDown3DSunLightingProfile acquiredSunProfile;
        private TopDown3DAmbientLightingProfile acquiredAmbientProfile;
        private Cubemap acquiredReflection;
        private int moduleMask;
        private bool preview;
        public PerpetualTwilightSun.SunState State => clock != null ? clock.State : default;
        public Vector3 DirectionToSun => Quaternion.Euler(State.ElevationDegrees, State.AzimuthDegrees, 0) * Vector3.back;
        public Material PreviewSky => sky != null ? sky.PreviewSky : null;
        public static TopDown3DEnvironmentRig ForScene(Scene scene)
        {
            TopDown3DEnvironmentRig found = null;
            foreach (var rig in active)
            {
                if (rig == null || !rig.isActiveAndEnabled || rig.gameObject.scene != scene) continue;
                if (found != null) return null;
                found = rig;
            }
            return found;
        }
        private void OnEnable()
        {
            active.Add(this);
            SceneManager.activeSceneChanged -= Activated; SceneManager.activeSceneChanged += Activated;
            SceneManager.sceneUnloaded -= Unloaded; SceneManager.sceneUnloaded += Unloaded;
        }
        private static void Activated(Scene oldScene, Scene scene)
        {
            if (!pending.TryGetValue(scene.handle, out var restore)) return;
            pending.Remove(scene.handle); restore.RestoreGlobal();
        }
        private static void Unloaded(Scene scene) => pending.Remove(scene.handle);
        private void Update() { if (Application.isPlaying || preview) Apply(); }
        public void RefreshPreview() { if (Application.isPlaying) return; preview = true; Apply(); }
        public void EndPreview() { preview = false; Release(); }
        public bool IsOwner => isActiveAndEnabled && clock != null && compatibilitySun != null
            && compatibilitySun.EnvironmentModules == this
            && compatibilitySun.isActiveAndEnabled && compatibilitySun.gameObject.scene == gameObject.scene
            && clock.gameObject.scene == gameObject.scene && ForScene(gameObject.scene) == this
            && PerpetualTwilightSun.EnabledCount(gameObject.scene) == 1
            && SceneManager.GetActiveScene() == gameObject.scene;
        public void Apply()
        {
            if (!IsOwner) { Release(); return; }
            Activated(default, gameObject.scene);
            int mask = (sunLight != null && sunLight.isActiveAndEnabled && sunLight.profile != null && sunLight.gameObject.scene == gameObject.scene ? 1 : 0)
                | (ambient != null && ambient.isActiveAndEnabled && ambient.profile != null && ambient.gameObject.scene == gameObject.scene ? 2 : 0)
                | (sky != null && sky.isActiveAndEnabled && sky.profile != null && sky.profile.IsValid && sky.gameObject.scene == gameObject.scene
                    && sky.outputCamera != null && sky.outputCamera.gameObject.scene == gameObject.scene ? 4 : 0);
            if (lease != null && (moduleMask != mask || acquiredSun != sunLight || acquiredAmbient != ambient || acquiredSky != sky
                || acquiredSunProfile != (sunLight != null ? sunLight.profile : null)
                || acquiredAmbientProfile != (ambient != null ? ambient.profile : null)
                || acquiredReflection != (ambient != null && ambient.profile != null ? ambient.profile.reflection : null))) Release();
            moduleMask = mask;
            if (lease == null)
            {
                acquiredSun = sunLight; acquiredAmbient = ambient; acquiredSky = sky;
                acquiredSunProfile = sunLight != null ? sunLight.profile : null;
                acquiredAmbientProfile = ambient != null ? ambient.profile : null;
                acquiredReflection = acquiredAmbientProfile != null ? acquiredAmbientProfile.reflection : null;
                lease = new Lease((mask & 1) != 0 ? sunLight.Light : null, (mask & 2) != 0, acquiredReflection != null);
            }
            if ((mask & 1) != 0) sunLight.Apply(State);
            if ((mask & 2) != 0) ambient.Apply(State.Brightness01);
            if ((mask & 4) != 0) sky.Apply(DirectionToSun, State.Brightness01);
            lease.Mark();
            compatibilitySun.SetModularAuthority(this, true);
        }
        private void Release()
        {
            if (compatibilitySun != null) compatibilitySun.SetModularAuthority(this, false);
            if ((moduleMask & 4) != 0 && acquiredSky != null) acquiredSky.Release();
            if (lease == null) return;
            lease.RestoreLight();
            if (SceneManager.GetActiveScene() == gameObject.scene) lease.RestoreGlobal();
            else if (gameObject.scene.IsValid() && gameObject.scene.isLoaded) pending[gameObject.scene.handle] = lease;
            lease = null;
        }
        private void OnDisable() { active.Remove(this); preview = false; Release(); }
        private void OnDestroy() { active.Remove(this); Release(); }

        private sealed class Lease
        {
            private readonly List<Action> mark = new List<Action>(), global = new List<Action>(), light = new List<Action>();
            private void Watch<T>(Func<T> get, Action<T> set, List<Action> restores, Func<bool> alive = null)
            {
                T before = get(), last = before;
                mark.Add(() => { if (alive == null || alive()) last = get(); });
                restores.Add(() => { if ((alive == null || alive()) && EqualityComparer<T>.Default.Equals(get(), last)) set(before); });
            }
            public Lease(Light sun, bool ambient, bool reflection)
            {
                if (sun != null) Watch(() => RenderSettings.sun, v => RenderSettings.sun = v != null ? v : null, global);
                if (ambient)
                {
                Watch(() => RenderSettings.ambientMode, v => RenderSettings.ambientMode = v, global);
                Watch(() => RenderSettings.ambientSkyColor, v => RenderSettings.ambientSkyColor = v, global);
                Watch(() => RenderSettings.ambientEquatorColor, v => RenderSettings.ambientEquatorColor = v, global);
                Watch(() => RenderSettings.ambientGroundColor, v => RenderSettings.ambientGroundColor = v, global);
                Watch(() => RenderSettings.subtractiveShadowColor, v => RenderSettings.subtractiveShadowColor = v, global);
                }
                if (ambient && reflection)
                {
                Watch(() => RenderSettings.defaultReflectionMode, v => RenderSettings.defaultReflectionMode = v, global);
                Watch(() => RenderSettings.customReflectionTexture, v => RenderSettings.customReflectionTexture = v, global);
                Watch(() => RenderSettings.reflectionIntensity, v => RenderSettings.reflectionIntensity = v, global);
                }
                if (sun == null) return;
                void WatchLight<T>(Func<T> get, Action<T> set) => Watch(get, set, light, () => sun != null);
                WatchLight(() => sun.transform.rotation, v => sun.transform.rotation = v);
                WatchLight(() => sun.type, v => sun.type = v);
                WatchLight(() => sun.color, v => sun.color = v);
                WatchLight(() => sun.intensity, v => sun.intensity = v);
                WatchLight(() => sun.shadows, v => sun.shadows = v);
                WatchLight(() => sun.shadowStrength, v => sun.shadowStrength = v);
                WatchLight(() => sun.shadowBias, v => sun.shadowBias = v);
                WatchLight(() => sun.shadowNormalBias, v => sun.shadowNormalBias = v);
                WatchLight(() => sun.shadowNearPlane, v => sun.shadowNearPlane = v);
                WatchLight(() => sun.bounceIntensity, v => sun.bounceIntensity = v);
                var data = sun.GetUniversalAdditionalLightData();
                Watch(() => data.usePipelineSettings, v => data.usePipelineSettings = v, light, () => sun != null && data != null);
                Watch(() => data.softShadowQuality, v => data.softShadowQuality = v, light, () => sun != null && data != null);
            }
            public void Mark() { foreach (var action in mark) action(); }
            public void RestoreGlobal() { foreach (var action in global) action(); }
            public void RestoreLight() { foreach (var action in light) action(); }
        }
    }
}
