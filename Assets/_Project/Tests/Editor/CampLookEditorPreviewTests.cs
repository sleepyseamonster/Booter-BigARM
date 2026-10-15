using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.IO;

namespace BooterBigArm.Tests
{
    public sealed class CampLookEditorPreviewTests
    {
        private TopDown3DCampLook look;
        private Camera camera, view;
        private Material sourceSky;
        [SetUp] public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Camp Look"); look = root.AddComponent<TopDown3DCampLook>();
            camera = new GameObject("Main Camera").AddComponent<Camera>(); camera.enabled = false;
            camera.transform.SetParent(root.transform);
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            var profile = AssetDatabase.LoadAssetAtPath<TopDown3DAtmosphereStudyProfile>(CampOpticsAuthoring.Settings + "/CombinedTwilight.asset");
            sourceSky = profile.skyMaterial;
            camera.gameObject.AddComponent<TopDown3DSceneRenderingOverride>().ConfigureAtmosphere(profile);
            var light = new GameObject("Sun").AddComponent<Light>(); light.type = LightType.Directional;
            light.transform.SetParent(root.transform);
            var sun = light.gameObject.AddComponent<PerpetualTwilightSun>(); sun.Configure(light); sun.EndStudyPreview();
            look.gameCamera = camera; look.sun = sun;
            view = new GameObject("Unowned Scene camera").AddComponent<Camera>();
            view.enabled = false; view.cameraType = CameraType.SceneView;
        }
        [TearDown] public void TearDown()
        {
            CampLookEditorPreview.Release();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
        [Test] public void FrozenPreviewUsesOwnedSkyAndRestoresBorrowedCameraState()
        {
            var cycle = look.sun.Cycle01; var sourceJson = EditorJsonUtility.ToJson(sourceSky);
            var environment = RenderSettings.skybox;
            view.allowHDR = false;
            var lightColor = look.sun.GetComponent<Light>().color;
            var rotation = look.sun.transform.rotation;
            CampLookEditorPreview.Refresh();
            Assert.That(look.sun.EditorPreviewSky, Is.Not.Null.And.Not.SameAs(sourceSky));
            CampLookEditorPreview.BeginCamera(default, view);
            Assert.That(view.allowHDR, Is.True);
            Assert.That(TopDown3DSceneRenderingOverride.TryGetAtmosphereProfile(view, out var selected), Is.True);
            Assert.That(selected, Is.SameAs(AssetDatabase.LoadAssetAtPath<TopDown3DAtmosphereStudyProfile>(CampOpticsAuthoring.Settings + "/CombinedTwilight.asset")));
            Assert.That(view.GetComponent<Skybox>().material, Is.SameAs(look.sun.EditorPreviewSky));
            Assert.That(RenderSettings.skybox, Is.SameAs(environment));
            CampLookEditorPreview.EndCamera(default, view);
            Assert.That(view.allowHDR, Is.False);
            Assert.That(TopDown3DSceneRenderingOverride.IsAuthoringCamera(view), Is.False);
            Assert.That(view.GetComponent<Skybox>(), Is.Null);
            Assert.That(view.GetComponent<UniversalAdditionalCameraData>(), Is.Null);
            Assert.That(look.sun.Cycle01, Is.EqualTo(cycle));
            Assert.That(EditorJsonUtility.ToJson(sourceSky), Is.EqualTo(sourceJson));
            CampLookEditorPreview.Release();
            Assert.That(look.sun.GetComponent<Light>().color, Is.EqualTo(lightColor));
            Assert.That(Quaternion.Angle(rotation, look.sun.transform.rotation), Is.LessThan(.001f));
        }
        [Test] public void PreviewCanDisableOpticsAndReleaseWithoutChangingGameCamera()
        {
            look.previewSceneViewEffects = false;
            CampLookEditorPreview.Refresh(); CampLookEditorPreview.BeginCamera(default, view);
            Assert.That(TopDown3DSceneRenderingOverride.AuthoringEffectsEnabled(view), Is.False);
            Assert.That(TopDown3DSceneRenderingOverride.AuthoringEffectsEnabled(camera), Is.True);
            CampLookEditorPreview.Release();
            Assert.That(TopDown3DSceneRenderingOverride.IsAuthoringCamera(view), Is.False);
            Assert.That(look.sun.EditorPreviewSky, Is.Null);
        }
        [Test] public void UnregisteredSceneCameraIsRejectedAndDuplicateLookReleasesPreview()
        {
            Assert.That(TopDown3DSceneRenderingOverride.TryGetAtmosphereProfile(view, out _), Is.False);
            CampLookEditorPreview.Refresh();
            new GameObject("Duplicate Look").AddComponent<TopDown3DCampLook>();
            CampLookEditorPreview.Refresh();
            Assert.That(look.sun.EditorPreviewSky, Is.Null);
        }
        [Test] public void SceneSavingReleasesTransientMaterialAndCameraComponents()
        {
            var path = "Assets/_Project/Tests/Editor/CampLookSaveFixture.unity";
            try
            {
                CampLookEditorPreview.Refresh(); CampLookEditorPreview.BeginCamera(default, view);
                Assert.That(look.sun.EditorPreviewSky, Is.Not.Null);
                Assert.That(EditorSceneManager.SaveScene(look.gameObject.scene, path), Is.True);
                Assert.That(look.sun.EditorPreviewSky, Is.Null);
                Assert.That(view.GetComponent<Skybox>(), Is.Null);
                Assert.That(TopDown3DSceneRenderingOverride.IsAuthoringCamera(view), Is.False);
                StringAssert.DoesNotContain("Runtime Martian", File.ReadAllText(path));
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                AssetDatabase.DeleteAsset(path);
            }
        }
    }
}
