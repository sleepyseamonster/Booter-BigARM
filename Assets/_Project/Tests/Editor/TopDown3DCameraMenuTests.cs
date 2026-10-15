using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DCameraMenuTests
    {
        private Scene scene;
        private TopDown3DMenuTests menu;
        private TopDown3DCameraOptions options;
        private GameObject cameraObject, target;
        [SetUp] public void SetUp()
        {
            if (!Application.isBatchMode || !System.IO.File.Exists(".camera-options-validation"))
                Assert.Ignore("Run this fixture only in the marked isolated camera candidate.");
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            target = new GameObject("Camera menu target"); cameraObject = new GameObject("Camera menu output", typeof(Camera));
            var rig = cameraObject.AddComponent<TopDown3DCameraRig>(); rig.Configure(target.transform); options = rig.EnableCameraOptions();
            menu = new TopDown3DMenuTests(); menu.SetUp();
        }
        [TearDown] public void TearDown()
        {
            menu?.TearDown(); Object.DestroyImmediate(cameraObject); Object.DestroyImmediate(target);
            if (scene.IsValid()) EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
        private void Command(string key) => typeof(TopDown3DMenuTests).GetMethod("Command", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(menu, new object[] { key });

        [Test] public void AuthoredCameraControlsApplyOnlyAfterSaveAndRemainDeferredUntilResume()
        {
            menu.Menu.Open(); Command("Settings"); Command("CameraOptions");
            Assert.That(menu.Menu.CurrentPanel, Is.EqualTo("Camera"));
            Command("CameraView"); Command("CameraRecenter"); Command("CameraSensX"); Command("CameraInvert");
            Assert.That(options.PreferredView, Is.EqualTo(TopDown3DCameraView.Elevated));
            Command("CameraApply");
            Assert.That(menu.Menu.CurrentPanel, Is.EqualTo("Settings"));
            Assert.That(options.PreferredView, Is.EqualTo(TopDown3DCameraView.ThirdPerson));
            Assert.That(options.EffectiveView, Is.EqualTo(TopDown3DCameraView.Elevated));
        }

        [Test] public void CameraBackRetainsDraftAndDiscardDoesNotChangePreference()
        {
            menu.Menu.Open(); Command("Settings"); Command("CameraOptions"); Command("CameraView");
            Command("CameraBack"); Assert.That(menu.Menu.CurrentPanel, Is.EqualTo("Settings"));
            Command("SettingsBack"); Assert.That(menu.Menu.CurrentPanel, Is.EqualTo("Dialog"));
            Command("DialogAccept");
            Assert.That(options.PreferredView, Is.EqualTo(TopDown3DCameraView.Elevated));
            Assert.That(options.EffectiveView, Is.EqualTo(TopDown3DCameraView.Elevated));
        }
    }
}
