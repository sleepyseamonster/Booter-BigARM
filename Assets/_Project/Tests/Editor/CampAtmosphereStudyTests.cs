using System;
using System.Reflection;
using System.IO;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using System.Linq;
using Object = UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class CampAtmosphereStudyTests
    {
        private Scene study, other, previous;
        private Camera camera;
        private PerpetualTwilightSun sun;
        private TopDown3DSceneRenderingOverride binding;
        private TopDown3DAtmosphereStudyProfile profile;
        private Material material;
        private string temporaryScene;
        [SetUp] public void SetUp()
        {
            previous = SceneManager.GetActiveScene();
            if (!File.Exists("snapshot-receipt.json")) throw new InvalidOperationException("Scene lifecycle fixtures require an isolated candidate.");
            study = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            temporaryScene = "Assets/_Project/Tests/Editor/AtmosphereFixture" + Guid.NewGuid().ToString("N") + ".unity";
            if (!EditorSceneManager.SaveScene(study, temporaryScene)) throw new IOException("Cannot save disposable fixture.");
            other = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(study);
            material = new Material(Shader.Find("BooterBigArm/TopDown3D/Camp Ochre Spherical Sky"));
            material.mainTexture = Texture2D.whiteTexture;
            profile = ScriptableObject.CreateInstance<TopDown3DAtmosphereStudyProfile>(); profile.skyMaterial = material;
            camera = NewObject("Study camera", study).AddComponent<Camera>();
            binding = camera.gameObject.AddComponent<TopDown3DSceneRenderingOverride>(); binding.ConfigureAtmosphere(profile);
            var light = NewObject("Study sun", study).AddComponent<Light>();
            light.type = LightType.Directional; sun = light.gameObject.AddComponent<PerpetualTwilightSun>();
        }
        [TearDown] public void TearDown()
        {
            if (sun != null) sun.EndStudyPreview();
            if (study.IsValid()) EditorSceneManager.CloseScene(study, true);
            if (other.IsValid()) EditorSceneManager.CloseScene(other, true);
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            if (!string.IsNullOrEmpty(temporaryScene)) AssetDatabase.DeleteAsset(temporaryScene);
            Object.DestroyImmediate(profile); Object.DestroyImmediate(material);
        }
        private static GameObject NewObject(string name, Scene scene)
        { var go = new GameObject(name); SceneManager.MoveGameObjectToScene(go, scene); return go; }
        [Test] public void SelectionIsCameraLocalAndDoesNotAffectOtherScene()
        {
            Assert.That(TopDown3DSceneRenderingOverride.TryGetAtmosphereProfile(camera, out var selected), Is.True);
            Assert.That(selected, Is.SameAs(profile));
            var unrelated = NewObject("Other camera", other).AddComponent<Camera>();
            Assert.That(TopDown3DSceneRenderingOverride.TryGetAtmosphereProfile(unrelated, out _), Is.False);
            var second = NewObject("Unbound camera", study).AddComponent<Camera>();
            Assert.That(TopDown3DSceneRenderingOverride.TryGetAtmosphereProfile(second, out _), Is.False);
        }
        [Test] public void DuplicateBindingsRejectSelectionAndRejectedPreviewDoesNotChangePhase()
        {
            var second = NewObject("Duplicate", study).AddComponent<Camera>();
            second.gameObject.AddComponent<TopDown3DSceneRenderingOverride>().ConfigureAtmosphere(profile);
            float phase = sun.Cycle01;
            Assert.That(TopDown3DSceneRenderingOverride.TryGetAtmosphereProfile(study, out _), Is.False);
            Assert.Throws<InvalidOperationException>(() => sun.ApplyStudyPreview(.75f, material));
            Assert.That(sun.Cycle01, Is.EqualTo(phase));
        }
        [Test] public void MissingTextureAndNonCameraBindingsAreInvalid()
        {
            material.mainTexture = null;
            Assert.That(profile.IsValid, Is.False);
            Assert.That(TopDown3DSceneRenderingOverride.TryGetAtmosphereProfile(study, out _), Is.False);
            material.mainTexture = Texture2D.whiteTexture; binding.enabled = false;
            NewObject("Invalid owner", study).AddComponent<TopDown3DSceneRenderingOverride>().ConfigureAtmosphere(profile);
            Assert.That(TopDown3DSceneRenderingOverride.TryGetAtmosphereProfile(study, out _), Is.False);
        }
        [Test] public void ExistingDisabledCameraSkyboxIsRestoredAfterRepeatedPreview()
        {
            var sky = camera.gameObject.AddComponent<Skybox>(); sky.material = material; sky.enabled = false;
            for (int i = 0; i < 3; i++)
            {
                sun.ApplyStudyPreview(.25f, material);
                Assert.That(sky.enabled, Is.True); Assert.That(sky.material, Is.Not.SameAs(material));
                sun.EndStudyPreview();
                Assert.That(sky.enabled, Is.False); Assert.That(sky.material, Is.SameAs(material));
            }
        }
        [Test] public void PreviewOwnsLocalSkyAndRestoresAmbientAndLight()
        {
            sun.EndStudyPreview();
            var light = sun.GetComponent<Light>(); light.color = Color.magenta; light.intensity = .3f;
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientSkyColor = Color.green;
            var originalSky = RenderSettings.skybox;
            sun.ApplyStudyPreview(.25f, material);
            Assert.That(RenderSettings.skybox, Is.SameAs(originalSky));
            Assert.That(light.color.b, Is.LessThan(light.color.g));
            sun.EndStudyPreview();
            Assert.That(light.color, Is.EqualTo(Color.magenta)); Assert.That(light.intensity, Is.EqualTo(.3f));
            Assert.That(RenderSettings.ambientMode, Is.EqualTo(AmbientMode.Flat));
            Assert.That(RenderSettings.ambientSkyColor, Is.EqualTo(Color.green));
            Assert.That(camera.GetComponent<Skybox>(), Is.Null);
        }
        [Test] public void InactiveDefaultSunCannotOverwriteStudyEnvironment()
        {
            var foreignLight = NewObject("Foreign sun", other).AddComponent<Light>();
            var foreign = foreignLight.gameObject.AddComponent<PerpetualTwilightSun>();
            sun.ApplyStudyPreview(.25f, material);
            var ambient = RenderSettings.ambientSkyColor; var owner = RenderSettings.sun;
            foreign.SetCycle01(.75f);
            Assert.That(RenderSettings.sun, Is.SameAs(owner)); Assert.That(RenderSettings.ambientSkyColor, Is.EqualTo(ambient));
        }
        [Test] public void ReflectionOwnershipRestoresPriorEnvironment()
        {
            sun.EndStudyPreview();
            var original = new Cubemap(16, TextureFormat.RGBAHalf, true);
            var selected = new Cubemap(16, TextureFormat.RGBAHalf, true);
            try
            {
                RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
                RenderSettings.customReflectionTexture = original; RenderSettings.reflectionIntensity = .3f;
                profile.sunlessReflection = selected;
                sun.ApplyStudyPreview(.25f, material);
                Assert.That(RenderSettings.customReflectionTexture, Is.SameAs(selected));
                sun.EndStudyPreview();
                Assert.That(RenderSettings.customReflectionTexture, Is.SameAs(original));
                Assert.That(RenderSettings.reflectionIntensity, Is.EqualTo(.3f));
            }
            finally { RenderSettings.customReflectionTexture = null; Object.DestroyImmediate(original); Object.DestroyImmediate(selected); }
        }
        [Test] public void InactivePreviewRejectsBeforeMutation()
        {
            float phase = sun.Cycle01; SceneManager.SetActiveScene(other);
            var owner = RenderSettings.sun; var ambient = RenderSettings.ambientSkyColor;
            Assert.Throws<InvalidOperationException>(() => sun.ApplyStudyPreview(.75f, material));
            Assert.That(sun.Cycle01, Is.EqualTo(phase)); Assert.That(RenderSettings.sun, Is.SameAs(owner));
            Assert.That(RenderSettings.ambientSkyColor, Is.EqualTo(ambient));
        }
        [Test] public void ProfileSwapReplacesCloneAndBindingDisableRestoresCamera()
        {
            sun.ApplyStudyPreview(.25f, material); var oldClone = camera.GetComponent<Skybox>().material;
            var next = ScriptableObject.CreateInstance<TopDown3DAtmosphereStudyProfile>();
            try
            {
                next.skyMaterial = material; next.brightSunColor = Color.yellow;
                binding.ConfigureAtmosphere(next); sun.SetCycle01(.25f);
                Assert.That(camera.GetComponent<Skybox>().material, Is.Not.SameAs(oldClone));
                Assert.That(sun.GetComponent<Light>().color, Is.EqualTo(Color.yellow));
                binding.enabled = false; sun.EndStudyPreview();
                Assert.That(camera.GetComponent<Skybox>(), Is.Null);
                Assert.That(TopDown3DSceneRenderingOverride.TryGetAtmosphereProfile(study, out _), Is.False);
            }
            finally { Object.DestroyImmediate(next); }
        }
        [Test] public void DeferredEnvironmentRestoreRunsOnlyForItsScene()
        {
            sun.EndStudyPreview(); RenderSettings.ambientSkyColor = Color.green;
            sun.ApplyStudyPreview(.25f, material);
            SceneManager.SetActiveScene(other); RenderSettings.ambientSkyColor = Color.blue;
            sun.EndStudyPreview(); Assert.That(RenderSettings.ambientSkyColor, Is.EqualTo(Color.blue));
            SceneManager.SetActiveScene(study);
            typeof(PerpetualTwilightSun).GetMethod("RestoreActivatedScene", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { other, study });
            Assert.That(RenderSettings.ambientSkyColor, Is.EqualTo(Color.green));
        }
        [Test] public void PositionalCameraCutInvalidatesFlareEvenWithoutChangedDirection()
        {
            Assert.That(TopDown3DRoundLensFlareFeature.IsCameraCut(Vector3.zero, Vector3.right * 3, Quaternion.identity, Quaternion.identity), Is.True);
            Assert.That(TopDown3DRoundLensFlareFeature.IsCameraCut(Vector3.zero, Vector3.right * .1f, Quaternion.identity, Quaternion.Euler(0, 1, 0)), Is.False);
        }
        [Test] public void AuthoredProfilesAndContextPreserveProductionBoundary()
        {
            foreach (var variant in new[] { "Moderate", "Clear", "Deep" })
            {
                var asset = AssetDatabase.LoadAssetAtPath<TopDown3DAtmosphereStudyProfile>("Assets/_Project/Settings/Studies/CampAtmosphere20261014/" + variant + ".asset");
                Assert.That(asset, Is.Not.Null); Assert.That(asset.IsValid, Is.True);
                Assert.That(asset.skyMaterial.shader.name, Is.EqualTo("BooterBigArm/TopDown3D/Camp Ochre Spherical Sky"));
                var texture = asset.skyMaterial.mainTexture as Texture2D;
                Assert.That(texture.width, Is.EqualTo(texture.height * 2));
                Assert.That(asset.skyMaterial.GetColor("_SunColor").b, Is.LessThan(.15f));
                Assert.That(asset.flareEnabled, Is.False);
                Assert.That(asset.sunlessReflection, Is.Not.Null);
                Assert.That(asset.sunlessReflection.width, Is.EqualTo(128));
            }
            var context = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/TopDown3D/Studies/CampAtmosphere20261014/TerrainContext.prefab");
            Assert.That(context, Is.Not.Null);
            Assert.That(context.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(context.GetComponentsInChildren<Terrain>(true), Is.Empty);
            Assert.That(context.GetComponentsInChildren<MonoBehaviour>(true), Is.Empty);
            Assert.That(context.GetComponentsInChildren<MeshRenderer>(true).All(r => r.shadowCastingMode == ShadowCastingMode.Off), Is.True);
            Assert.That(context.GetComponentsInChildren<MeshFilter>(true).All(f => f.sharedMesh != null), Is.True);
            var production = System.IO.File.ReadAllText("Assets/_Project/Scenes/Production/GreaterWasteland.unity");
            var profileGuid = AssetDatabase.AssetPathToGUID("Assets/_Project/Settings/Studies/CampAtmosphere20261014/Clear.asset");
            Assert.That(production.Contains(profileGuid), Is.False);
        }
    }
}
