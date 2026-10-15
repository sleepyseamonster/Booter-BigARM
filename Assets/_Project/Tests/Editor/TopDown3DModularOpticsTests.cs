using System.Linq;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DModularOpticsTests
    {
        private Scene scene;
        private Camera camera;
        private TopDown3DSceneRenderingOverride binding;
        private TopDown3DCampOpticsProfile native;
        private TopDown3DCameraOpticsProfile profile;

        [SetUp]
        public void SetUp()
        {
            scene = EditorSceneManager.NewPreviewScene();
            var owner = new GameObject("Modular camera");
            SceneManager.MoveGameObjectToScene(owner, scene);
            camera = owner.AddComponent<Camera>();
            owner.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
            binding = owner.AddComponent<TopDown3DSceneRenderingOverride>();
            native = ScriptableObject.CreateInstance<TopDown3DCampOpticsProfile>();
            native.depthMode = DepthOfFieldMode.Gaussian;
            profile = ScriptableObject.CreateInstance<TopDown3DCameraOpticsProfile>();
            profile.nativeOptics = native;
            binding.ConfigureOptics(profile);
        }

        [TearDown]
        public void TearDown()
        {
            if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
            Object.DestroyImmediate(profile);
            Object.DestroyImmediate(native);
        }

        [Test]
        public void OpticsSelectionDoesNotRequireASkyOrAtmosphereProfile()
        {
            Assert.That(TopDown3DSceneRenderingOverride.TryGetAtmosphereProfile(camera, out _), Is.False);
            Assert.That(TopDown3DSceneRenderingOverride.TryGetOpticsProfile(camera, out var selected), Is.True);
            Assert.That(selected, Is.SameAs(native));
            Assert.That(TopDown3DSceneRenderingOverride.CameraOpticsFor(camera), Is.SameAs(profile));
        }

        [Test]
        public void DisabledAndInvalidModularBindingsKeepTheirExplicitSelectionWithoutFallback()
        {
            binding.enabled = false;
            Assert.That(TopDown3DCameraOpticsFeature.IsModularCamera(camera), Is.True);
            Assert.That(TopDown3DSceneRenderingOverride.TryGetOpticsProfile(camera, out _), Is.False);
            Assert.That(TopDown3DSceneRenderingOverride.CameraOpticsFor(camera), Is.Null);
            binding.enabled = true;
            profile.version = -1;
            Assert.That(TopDown3DCameraOpticsFeature.IsModularCamera(camera), Is.True);
            Assert.That(TopDown3DSceneRenderingOverride.TryGetOpticsProfile(camera, out _), Is.False);
            Assert.That(TopDown3DSceneRenderingOverride.CameraOpticsFor(camera), Is.Null);
        }

        [Test]
        public void BlurSelectionPreservesDormantNativeSettings()
        {
            string before = EditorJsonUtility.ToJson(native);
            profile.blurMode = TopDown3DWorldBlurMode.Off;
            Assert.That(profile.EffectiveDepthMode, Is.EqualTo(DepthOfFieldMode.Off));
            Assert.That(profile.EffectiveTiltShift, Is.False);
            profile.blurMode = TopDown3DWorldBlurMode.TiltShift;
            Assert.That(profile.EffectiveDepthMode, Is.EqualTo(DepthOfFieldMode.Off));
            Assert.That(profile.EffectiveTiltShift, Is.True);
            profile.blurMode = TopDown3DWorldBlurMode.DepthOfField;
            Assert.That(profile.EffectiveDepthMode, Is.EqualTo(DepthOfFieldMode.Gaussian));
            Assert.That(profile.EffectiveTiltShift, Is.False);
            Assert.That(EditorJsonUtility.ToJson(native), Is.EqualTo(before));
        }

        [Test]
        public void MasterOffSuppressesArtisticBlurAndStreaks()
        {
            profile.blurMode = TopDown3DWorldBlurMode.TiltShift;
            profile.streakEnabled = true;
            native.opticsEnabled = false;
            Assert.That(profile.EffectiveTiltShift, Is.False);
            Assert.That(profile.EffectiveStreak, Is.False);
            Assert.That(profile.EffectiveDepthMode, Is.EqualTo(DepthOfFieldMode.Off));
        }

        [Test]
        public void PerspectiveRendererHasOneOpticsEntryPointBeforeLensEffects()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(ConversionBaselineValidator.ConversionRendererPath);
            Assert.That(renderer, Is.Not.Null);
            Assert.That(renderer.rendererFeatures.OfType<TopDown3DCameraOpticsFeature>().Count(), Is.EqualTo(1));
            int opticsIndex = renderer.rendererFeatures.FindIndex(f => f is TopDown3DCameraOpticsFeature);
            int flareIndex = renderer.rendererFeatures.FindIndex(f => f is TopDown3DRoundLensFlareFeature);
            Assert.That(opticsIndex, Is.LessThan(flareIndex));
        }
    }
}
