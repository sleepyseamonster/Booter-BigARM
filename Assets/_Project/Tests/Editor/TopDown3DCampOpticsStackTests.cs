using System.Collections.Generic;
using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace BooterBigArm.Tests
{
    // Exercises CPU ownership contracts only. Pixel output and GPU costs need rendering fixtures.
    public sealed class TopDown3DCampOpticsStackTests
    {
        private Scene studyScene, unrelatedScene;
        private Camera camera;
        private UniversalAdditionalCameraData cameraData;
        private TopDown3DSceneRenderingOverride binding;
        private TopDown3DAtmosphereStudyProfile atmosphere;
        private TopDown3DCampOpticsProfile optics;
        private Material sky;
        private VolumeStack originalManagerStack;
        private readonly List<VolumeStack> fixtureStacks = new List<VolumeStack>();

        [OneTimeSetUp]
        public void EnsurePipelineInitialized()
        {
            if (VolumeManager.instance.isInitialized) return;
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("Native optics isolation requires a graphics-enabled Unity runner.");
            var previousTarget = RenderTexture.active;
            GameObject initializer = null;
            RenderTexture target = null;
            try
            {
                initializer = new GameObject("Temporary optics pipeline initializer") { hideFlags = HideFlags.HideAndDontSave };
                var initializerCamera = initializer.AddComponent<Camera>();
                initializerCamera.enabled = false;
                initializerCamera.cullingMask = 0;
                initializerCamera.clearFlags = CameraClearFlags.SolidColor;
                initializerCamera.backgroundColor = Color.black;
                target = new RenderTexture(16, 16, 16) { hideFlags = HideFlags.HideAndDontSave };
                initializerCamera.targetTexture = target;
                initializerCamera.Render();
                Assert.That(VolumeManager.instance.isInitialized, Is.True,
                    "A bounded Camera.Render must initialize the configured graphics pipeline.");
            }
            finally
            {
                RenderTexture.active = previousTarget;
                if (initializer != null) Object.DestroyImmediate(initializer);
                if (target != null) { target.Release(); Object.DestroyImmediate(target); }
            }
        }

        [SetUp]
        public void SetUp()
        {
            // OneTimeSetUp creates the pipeline through rendering. Never directly initialize/
            // deinitialize the shared manager: that would invalidate other cameras' stacks.
            if (!VolumeManager.instance.isInitialized)
                Assert.Fail("VolumeManager became uninitialized after the graphics pipeline setup.");
            originalManagerStack = VolumeManager.instance.stack;
            studyScene = EditorSceneManager.NewPreviewScene();
            unrelatedScene = EditorSceneManager.NewPreviewScene();
            var shader = Shader.Find("BooterBigArm/TopDown3D/Camp Ochre Spherical Sky");
            Assert.That(shader, Is.Not.Null, "Study shader must be imported.");
            sky = new Material(shader) { mainTexture = Texture2D.whiteTexture };
            atmosphere = ScriptableObject.CreateInstance<TopDown3DAtmosphereStudyProfile>();
            optics = ScriptableObject.CreateInstance<TopDown3DCampOpticsProfile>();
            atmosphere.skyMaterial = sky;
            atmosphere.nativeOptics = optics;
            camera = NewCamera("Bound camp optics camera", studyScene);
            cameraData = camera.GetComponent<UniversalAdditionalCameraData>();
            cameraData.volumeLayerMask = 0; // No fixture-owned Volume or layer allocation.
            binding = camera.gameObject.AddComponent<TopDown3DSceneRenderingOverride>();
            binding.ConfigureAtmosphere(atmosphere);
        }

        [TearDown]
        public void TearDown()
        {
            if (camera != null) TopDown3DCampOpticsStack.Release(camera);
            if (originalManagerStack != null && originalManagerStack.isValid)
                VolumeManager.instance.stack = originalManagerStack;
            // Dispose borrowed fixture stacks before destroying camera data, so its null
            // setter cannot place a valid fixture stack in URP's reusable camera pool.
            foreach (var stack in fixtureStacks)
                if (stack != null && stack.isValid) VolumeManager.instance.DestroyStack(stack);
            fixtureStacks.Clear();
            if (studyScene.IsValid()) EditorSceneManager.ClosePreviewScene(studyScene);
            if (unrelatedScene.IsValid()) EditorSceneManager.ClosePreviewScene(unrelatedScene);
            Object.DestroyImmediate(atmosphere);
            Object.DestroyImmediate(optics);
            Object.DestroyImmediate(sky);
        }

        private static Camera NewCamera(string name, Scene scene)
        {
            var owner = new GameObject(name);
            SceneManager.MoveGameObjectToScene(owner, scene);
            var result = owner.AddComponent<Camera>();
            owner.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
            return result;
        }

        private VolumeStack NewFixtureStack()
        {
            var result = VolumeManager.instance.CreateStack();
            fixtureStacks.Add(result);
            return result;
        }

        private static void EndCamera(Camera endedCamera)
        {
            // Deliver the callback without rendering or invoking gameplay.
            var callback = typeof(TopDown3DCampOpticsStack).GetMethod("EndCamera", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(callback, Is.Not.Null);
            callback.Invoke(null, new object[] { default(ScriptableRenderContext), endedCamera });
        }

        [Test]
        public void SelectedCameraGetsNativeValuesWithoutMutatingSourceAssets()
        {
            optics.postExposure = -.75f;
            optics.tonemapping = TonemappingMode.ACES;
            optics.bloomIntensity = .31f;
            optics.bloomThreshold = 1.4f;
            optics.bloomScatter = .48f;
            optics.dirtTexture = Texture2D.grayTexture;
            optics.dirtIntensity = .62f;
            optics.depthMode = DepthOfFieldMode.Gaussian;
            optics.focusDistance = 24f;
            optics.farStart = 32f;
            optics.farEnd = 74f;
            var opticsBefore = EditorJsonUtility.ToJson(optics);
            var atmosphereBefore = EditorJsonUtility.ToJson(atmosphere);
            var skyBefore = EditorJsonUtility.ToJson(sky);
            var pipeline = AssetDatabase.LoadMainAssetAtPath("Assets/_Project/Settings/Rendering/URP/UniversalRP.asset");
            Assert.That(pipeline, Is.Not.Null);
            var pipelineBefore = EditorJsonUtility.ToJson(pipeline);

            Assert.That(TopDown3DCampOpticsStack.TryApply(camera), Is.True);
            var selected = VolumeManager.instance.stack;
            Assert.That(selected, Is.Not.SameAs(originalManagerStack));
            var bloom = selected.GetComponent<Bloom>();
            Assert.That(bloom.intensity.value, Is.EqualTo(.31f));
            Assert.That(bloom.threshold.value, Is.EqualTo(1.4f));
            Assert.That(bloom.scatter.value, Is.EqualTo(.48f));
            Assert.That(bloom.tint.value, Is.EqualTo(Color.white));
            Assert.That(bloom.highQualityFiltering.value, Is.True);
            Assert.That(bloom.dirtTexture.value, Is.SameAs(Texture2D.grayTexture));
            Assert.That(bloom.dirtIntensity.value, Is.EqualTo(.62f));
            var dof = selected.GetComponent<DepthOfField>();
            Assert.That(dof.mode.value, Is.EqualTo(DepthOfFieldMode.Gaussian));
            Assert.That(dof.focusDistance.value, Is.EqualTo(24f));
            Assert.That(dof.gaussianStart.value, Is.EqualTo(32f));
            Assert.That(dof.gaussianEnd.value, Is.EqualTo(74f));
            Assert.That(selected.GetComponent<ColorAdjustments>().postExposure.value, Is.EqualTo(-.75f));
            Assert.That(selected.GetComponent<Tonemapping>().mode.value, Is.EqualTo(TonemappingMode.ACES));
            Assert.That(EditorJsonUtility.ToJson(optics), Is.EqualTo(opticsBefore));
            Assert.That(EditorJsonUtility.ToJson(atmosphere), Is.EqualTo(atmosphereBefore));
            Assert.That(EditorJsonUtility.ToJson(sky), Is.EqualTo(skyBefore));
            Assert.That(EditorJsonUtility.ToJson(pipeline), Is.EqualTo(pipelineBefore));
        }

        [Test]
        public void EndAndReleasePreserveBorrowedStacksAndCameraSettings()
        {
            var borrowedManager = NewFixtureStack();
            var borrowedCamera = NewFixtureStack();
            VolumeManager.instance.stack = borrowedManager;
            cameraData.volumeStack = borrowedCamera;
            var settingsBefore = EditorJsonUtility.ToJson(cameraData);
            var modeBefore = camera.GetVolumeFrameworkUpdateMode();
            var triggerBefore = cameraData.volumeTrigger;
            var maskBefore = cameraData.volumeLayerMask;
            Assert.That(TopDown3DCampOpticsStack.TryApply(camera), Is.True);
            var owned = VolumeManager.instance.stack;
            Assert.That(cameraData.volumeStack, Is.SameAs(borrowedCamera));
            EndCamera(camera);
            Assert.That(VolumeManager.instance.stack, Is.SameAs(borrowedManager));
            Assert.That(TopDown3DCampOpticsStack.TryApply(camera), Is.True);
            Assert.That(VolumeManager.instance.stack, Is.SameAs(owned), "Stack is reused after a completed camera.");
            TopDown3DCampOpticsStack.Release(camera);
            Assert.That(VolumeManager.instance.stack, Is.SameAs(borrowedManager));
            Assert.That(owned.isValid, Is.False);
            Assert.That(borrowedManager.isValid && borrowedCamera.isValid, Is.True);
            Assert.That(cameraData.volumeStack, Is.SameAs(borrowedCamera));
            Assert.That(camera.GetVolumeFrameworkUpdateMode(), Is.EqualTo(modeBefore));
            Assert.That(cameraData.volumeTrigger, Is.SameAs(triggerBefore));
            Assert.That(cameraData.volumeLayerMask, Is.EqualTo(maskBefore));
            Assert.That(EditorJsonUtility.ToJson(cameraData), Is.EqualTo(settingsBefore));
        }

        [Test]
        public void TeardownDoesNotOverwriteANewerManagerOwner()
        {
            Assert.That(TopDown3DCampOpticsStack.TryApply(camera), Is.True);
            var owned = VolumeManager.instance.stack;
            var newer = NewFixtureStack();
            VolumeManager.instance.stack = newer;
            EndCamera(camera);
            Assert.That(VolumeManager.instance.stack, Is.SameAs(newer));
            TopDown3DCampOpticsStack.Release(camera);
            Assert.That(VolumeManager.instance.stack, Is.SameAs(newer));
            Assert.That(owned.isValid, Is.False);
        }

        [Test]
        public void UnboundSameSceneAndAdditiveSceneCamerasAreExcluded()
        {
            var sameScene = NewCamera("Unbound same-scene camera", studyScene);
            var unrelated = NewCamera("Additive unrelated camera", unrelatedScene);
            Assert.That(TopDown3DCampOpticsStack.TryApply(sameScene), Is.False);
            Assert.That(TopDown3DCampOpticsStack.TryApply(unrelated), Is.False);
            Assert.That(TopDown3DCampOpticsStack.TryApply(null), Is.False);
            Assert.That(VolumeManager.instance.stack, Is.SameAs(originalManagerStack));
            Assert.That(TopDown3DCampOpticsStack.TryApply(camera), Is.True);
            var owned = VolumeManager.instance.stack;
            Assert.That(TopDown3DCampOpticsStack.TryApply(sameScene), Is.False);
            EndCamera(sameScene);
            Assert.That(VolumeManager.instance.stack, Is.SameAs(owned));
            EndCamera(camera);
            Assert.That(VolumeManager.instance.stack, Is.SameAs(originalManagerStack));
        }

        [TestCase("version")]
        [TestCase("exposure")]
        [TestCase("focus")]
        [TestCase("range")]
        public void InvalidOpticsProfileCannotAcquireManagerStack(string invalidField)
        {
            switch (invalidField)
            {
                case "version": optics.version = -1; break;
                case "exposure": optics.postExposure = float.NaN; break;
                case "focus": optics.focusDistance = 0f; break;
                case "range": optics.farEnd = optics.farStart; break;
            }
            Assert.That(optics.IsValid, Is.False);
            Assert.That(TopDown3DCampOpticsStack.TryApply(camera), Is.False);
            Assert.That(VolumeManager.instance.stack, Is.SameAs(originalManagerStack));
        }

        [Test]
        public void DuplicateOrDisabledBindingsAndMissingNativeProfileRejectSelection()
        {
            atmosphere.nativeOptics = null;
            Assert.That(TopDown3DCampOpticsStack.TryApply(camera), Is.False);
            atmosphere.nativeOptics = optics;
            var duplicate = NewCamera("Duplicate owner", studyScene).gameObject.AddComponent<TopDown3DSceneRenderingOverride>();
            duplicate.ConfigureAtmosphere(atmosphere);
            Assert.That(TopDown3DCampOpticsStack.TryApply(camera), Is.False);
            duplicate.enabled = false;
            binding.enabled = false;
            Assert.That(TopDown3DCampOpticsStack.TryApply(camera), Is.False);
            Assert.That(VolumeManager.instance.stack, Is.SameAs(originalManagerStack));
        }

        [Test]
        public void OverlayCameraCannotAcquireNativeStack()
        {
            cameraData.renderType = CameraRenderType.Overlay;
            Assert.That(TopDown3DCampOpticsStack.TryApply(camera), Is.False);
            Assert.That(VolumeManager.instance.stack, Is.SameAs(originalManagerStack));
        }

        [Test]
        public void MissingCameraDataAndInvalidAtmosphereRejectSelection()
        {
            atmosphere.skyMaterial = null;
            Assert.That(TopDown3DCampOpticsStack.TryApply(camera), Is.False);
            atmosphere.skyMaterial = sky;
            Object.DestroyImmediate(cameraData);
            Assert.That(TopDown3DCampOpticsStack.TryApply(camera), Is.False);
            Assert.That(VolumeManager.instance.stack, Is.SameAs(originalManagerStack));
        }

        [Test]
        public void BindingDisableReleasesOwnedStackImmediately()
        {
            Assert.That(TopDown3DCampOpticsStack.TryApply(camera), Is.True);
            var owned = VolumeManager.instance.stack;
            binding.enabled = false;
            Assert.That(VolumeManager.instance.stack, Is.SameAs(originalManagerStack));
            Assert.That(owned.isValid, Is.False);
            Assert.That(TopDown3DCampOpticsStack.TryApply(camera), Is.False);
            // Release is deliberately idempotent after the binding's teardown.
            TopDown3DCampOpticsStack.Release(camera);
            Assert.That(VolumeManager.instance.stack, Is.SameAs(originalManagerStack));
        }

        [Test]
        public void DisposedPreviousStackRestoresValidMainStack()
        {
            var borrowed = VolumeManager.instance.CreateStack();
            fixtureStacks.Add(borrowed);
            VolumeManager.instance.stack = borrowed;
            Assert.That(TopDown3DCampOpticsStack.TryApply(camera), Is.True);
            VolumeManager.instance.DestroyStack(borrowed);
            TopDown3DCampOpticsStack.Restore(camera);
            Assert.That(VolumeManager.instance.stack.isValid, Is.True);
            Assert.That(VolumeManager.instance.stack, Is.Not.SameAs(borrowed));
        }

        [Test]
        public void ExplicitFocusUsesEyeDepthAndResetsOnTargetWarp()
        {
            var target = new GameObject("Explicit focus anchor");
            SceneManager.MoveGameObjectToScene(target, studyScene);
            try
            {
                camera.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                target.transform.position = new Vector3(30f, 0f, 40f);
                optics.followPlayerFocus = true; optics.depthMode = DepthOfFieldMode.Bokeh;
                binding.ConfigureFocusTarget(target.transform, Vector3.zero);
                Assert.That(TopDown3DCampOpticsStack.TryApply(camera), Is.True);
                Assert.That(VolumeManager.instance.stack.GetComponent<DepthOfField>().focusDistance.value, Is.EqualTo(40f).Within(.001f));
                TopDown3DCampOpticsStack.Restore(camera);
                target.transform.position = new Vector3(30f, 0f, 60f);
                Assert.That(TopDown3DCampOpticsStack.TryApply(camera), Is.True);
                Assert.That(VolumeManager.instance.stack.GetComponent<DepthOfField>().focusDistance.value, Is.EqualTo(60f).Within(.001f));
            }
            finally { Object.DestroyImmediate(target); }
        }

        [Test]
        public void ProfileSwapAndMasterOffResetOpticalValuesButKeepDisplayControls()
        {
            optics.depthMode = DepthOfFieldMode.Bokeh;
            optics.focusDistance = 18f;
            optics.aperture = 5.6f;
            optics.focalLength = 60f;
            Assert.That(TopDown3DCampOpticsStack.TryApply(camera), Is.True);
            var owned = VolumeManager.instance.stack;
            var dof = owned.GetComponent<DepthOfField>();
            Assert.That(dof.mode.value, Is.EqualTo(DepthOfFieldMode.Bokeh));
            Assert.That(dof.aperture.value, Is.EqualTo(5.6f));
            Assert.That(dof.focalLength.value, Is.EqualTo(60f));
            EndCamera(camera);
            var replacement = ScriptableObject.CreateInstance<TopDown3DCampOpticsProfile>();
            try
            {
                replacement.opticsEnabled = false;
                replacement.postExposure = .5f;
                replacement.tonemapping = TonemappingMode.Neutral;
                replacement.depthMode = DepthOfFieldMode.Gaussian;
                replacement.bloomIntensity = 1f;
                replacement.dirtIntensity = 1f;
                atmosphere.nativeOptics = replacement;
                Assert.That(TopDown3DCampOpticsStack.TryApply(camera), Is.True);
                Assert.That(VolumeManager.instance.stack, Is.SameAs(owned));
                Assert.That(owned.GetComponent<Bloom>().intensity.value, Is.Zero);
                Assert.That(owned.GetComponent<Bloom>().dirtIntensity.value, Is.Zero);
                Assert.That(owned.GetComponent<DepthOfField>().mode.value, Is.EqualTo(DepthOfFieldMode.Off));
                Assert.That(owned.GetComponent<ColorAdjustments>().postExposure.value, Is.EqualTo(.5f));
                Assert.That(owned.GetComponent<Tonemapping>().mode.value, Is.EqualTo(TonemappingMode.Neutral));
                EndCamera(camera);
                Assert.That(VolumeManager.instance.stack, Is.SameAs(originalManagerStack));
            }
            finally { atmosphere.nativeOptics = optics; Object.DestroyImmediate(replacement); }
        }
    }
}
