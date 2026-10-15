using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DEnvironmentModulesTests
    {
        private TopDown3DEnvironmentRig rig;
        private TopDown3DSunLightingProfile lightProfile;
        private TopDown3DAmbientLightingProfile fillProfile;
        [SetUp] public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            rig = new GameObject("Environment").AddComponent<TopDown3DEnvironmentRig>();
            rig.clock = new GameObject("Clock").AddComponent<TopDown3DTwilightClock>();
            rig.clock.Configure(true, 1200, .783f, 3.7f, 2.5f, -3, 15.4f);
            var light = new GameObject("Sun").AddComponent<Light>();
            rig.compatibilitySun = light.gameObject.AddComponent<PerpetualTwilightSun>();
            rig.sunLight = light.gameObject.AddComponent<TopDown3DSunLight>();
            lightProfile = ScriptableObject.CreateInstance<TopDown3DSunLightingProfile>();
            lightProfile.deepColor = Color.red; lightProfile.brightColor = Color.yellow;
            lightProfile.deepIntensity = 1.74f; lightProfile.brightIntensity = .97f; rig.sunLight.profile = lightProfile;
            rig.ambient = new GameObject("Ambient").AddComponent<TopDown3DAmbientLighting>();
            fillProfile = ScriptableObject.CreateInstance<TopDown3DAmbientLightingProfile>();
            fillProfile.deepSky = Color.red; fillProfile.brightSky = Color.yellow; rig.ambient.profile = fillProfile;
            rig.compatibilitySun.ConfigureModules(rig);
        }
        [TearDown] public void TearDown()
        {
            rig.EndPreview();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Object.DestroyImmediate(lightProfile); Object.DestroyImmediate(fillProfile);
        }
        [Test] public void ClockAndFacadeKeepExistingCycleAndScaledTimeRules()
        {
            float before = rig.clock.Phase; rig.clock.Advance(60);
            Assert.That(rig.compatibilitySun.Cycle01, Is.EqualTo(Mathf.Repeat(before + .05f, 1)).Within(.00001f));
            var expected = PerpetualTwilightSun.EvaluateCycle(rig.clock.Phase, 3.7f, 2.5f, -3, 15.4f);
            Assert.That(rig.State.Brightness01, Is.EqualTo(expected.Brightness01));
            rig.compatibilitySun.SetCycleEnabled(false); before = rig.clock.Phase; rig.clock.Advance(100);
            Assert.That(rig.clock.Phase, Is.EqualTo(before));
            rig.compatibilitySun.SetCycle01(.25f); Assert.That(rig.clock.Phase, Is.EqualTo(.25f));
            rig.clock.enabled = false; rig.clock.SetRunning(true); rig.clock.Advance(100);
            Assert.That(rig.clock.Phase, Is.EqualTo(.25f));
        }
        [Test] public void LightingOperatesWithoutCameraSkyOrOptics()
        {
            rig.RefreshPreview(); Assert.That(rig.IsOwner, Is.True);
            Assert.That(rig.sunLight.Light.color, Is.EqualTo(lightProfile.Tint(rig.State.Brightness01)));
            Assert.That(RenderSettings.sun, Is.SameAs(rig.sunLight.Light));
        }
        [Test] public void AmbientDisableRestoresFillWhileDirectLightKeepsOperating()
        {
            Color before = RenderSettings.ambientSkyColor;
            rig.RefreshPreview(); Assert.That(RenderSettings.ambientSkyColor, Is.Not.EqualTo(before));
            rig.ambient.enabled = false; rig.Apply();
            Assert.That(RenderSettings.ambientSkyColor, Is.EqualTo(before));
            Assert.That(rig.sunLight.Light.color, Is.EqualTo(lightProfile.Tint(rig.State.Brightness01)));
        }
        [Test] public void DuplicateEnvironmentReleasesExistingOwnership()
        {
            Color before = RenderSettings.ambientSkyColor;
            rig.RefreshPreview(); new GameObject("Duplicate").AddComponent<TopDown3DEnvironmentRig>(); rig.Apply();
            Assert.That(rig.IsOwner, Is.False); Assert.That(RenderSettings.ambientSkyColor, Is.EqualTo(before));
        }
        [Test] public void TeardownDoesNotOverwriteANewerAmbientWriter()
        {
            rig.RefreshPreview(); RenderSettings.ambientSkyColor = Color.cyan; rig.EndPreview();
            Assert.That(RenderSettings.ambientSkyColor, Is.EqualTo(Color.cyan));
        }
        [Test] public void InactiveSceneCannotClaimTheEnvironment()
        {
            var original = rig.gameObject.scene;
            var foreign = EditorSceneManager.NewPreviewScene();
            var roots = original.GetRootGameObjects();
            foreach (var root in roots) SceneManager.MoveGameObjectToScene(root, foreign);
            try
            {
                Color before = RenderSettings.ambientSkyColor;
                rig.RefreshPreview(); Assert.That(rig.IsOwner, Is.False); Assert.That(RenderSettings.ambientSkyColor, Is.EqualTo(before));
            }
            finally { foreach (var root in roots) if (root != null) SceneManager.MoveGameObjectToScene(root, original); EditorSceneManager.ClosePreviewScene(foreign); }
        }
        [Test] public void DisabledModuleNeverRestoresPropertiesWrittenByOthers()
        {
            rig.sunLight.enabled = false; rig.RefreshPreview(); rig.sunLight.Light.intensity = 4.321f;
            rig.Apply(); rig.EndPreview(); Assert.That(rig.sunLight.Light.intensity, Is.EqualTo(4.321f));
        }
        [Test] public void DestroyedSunCanReleaseWithoutDereferencingDestroyedObjects()
        {
            rig.RefreshPreview(); Object.DestroyImmediate(rig.sunLight.gameObject);
            Assert.DoesNotThrow(() => rig.EndPreview());
        }
        [Test] public void ReplacingLightRestoresTheOldLightAndLeasesTheNewLight()
        {
            var old = rig.sunLight; float before = old.Light.intensity; rig.RefreshPreview();
            var next = new GameObject("New Sun").AddComponent<TopDown3DSunLight>();
            float nextBefore = next.Light.intensity; next.profile = lightProfile; rig.sunLight = next; rig.Apply();
            Assert.That(old.Light.intensity, Is.EqualTo(before)); rig.EndPreview(); Assert.That(next.Light.intensity, Is.EqualTo(nextBefore));
        }
        [Test] public void MissingSkyCameraDoesNotDisableWorldLighting()
        {
            rig.sky = new GameObject("Unbound sky").AddComponent<TopDown3DSkyPresentation>();
            rig.RefreshPreview(); Assert.That(rig.IsOwner, Is.True);
            Assert.That(RenderSettings.sun, Is.SameAs(rig.sunLight.Light));
            rig.sky.enabled = false; rig.Apply(); Assert.That(rig.IsOwner, Is.True);
        }
    }
}
