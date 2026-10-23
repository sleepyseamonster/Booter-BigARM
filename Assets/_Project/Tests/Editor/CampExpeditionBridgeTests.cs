using System.Collections.Generic;
using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class CampExpeditionBridgeTests
    {
        private TopDown3DMenuTests fixture;
        private Scene isolatedScene;
        private GameObject sessionObject, nativeViewObject;
        private CampExpeditionSession session;
        private static readonly BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        private static Dictionary<int, CampExpeditionSession> Sessions =>
            (Dictionary<int, CampExpeditionSession>)typeof(CampExpeditionSession)
                .GetField("sessions", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);

        [SetUp]
        public void Setup()
        {
            fixture = new TopDown3DMenuTests();
            fixture.SetUp();
        }

        [TearDown]
        public void Cleanup()
        {
            if (session != null)
            {
                Sessions.Remove(session.gameObject.scene.handle);
                typeof(CampExpeditionSession).GetField("view", PrivateInstance).SetValue(session, null);
            }
            if (nativeViewObject != null) Object.DestroyImmediate(nativeViewObject);
            if (sessionObject != null) Object.DestroyImmediate(sessionObject);
            fixture?.TearDown();
            if (isolatedScene.IsValid() && isolatedScene.isLoaded)
                EditorSceneManager.ClosePreviewScene(isolatedScene);
        }

        private void Command(string key)
        {
            typeof(TopDown3DMenuController).GetField("waitingRelease", PrivateInstance).SetValue(fixture.Menu, false);
            typeof(TopDown3DMenuController).GetMethod("OnCommand", PrivateInstance)
                .Invoke(fixture.Menu, new object[] { key });
        }

        [Test]
        public void ExpeditionSettingsApplyReturnsToItsNativeHost()
        {
            fixture.Menu.UseExpeditionHost();
            int returned = 0;
            fixture.Menu.ExpeditionSettingsClosed += () => returned++;
            fixture.Menu.OpenExpeditionSettings();
            Command("TextSize");
            Command("SettingsApply");
            Assert.That(fixture.Menu.IsOpen, Is.False);
            Assert.That(returned, Is.EqualTo(1));
        }

        [Test]
        public void LegacySettingsApplyStillReturnsToLegacyRoot()
        {
            fixture.Menu.Open();
            Command("Settings");
            Command("SettingsApply");
            Assert.That(fixture.Menu.CurrentPanel, Is.EqualTo("Root"));
        }

        [Test]
        public void RepeatedInstallerPreservesNativeReleaseFenceAndTreeNavigation()
        {
            // A preview scene isolates the installer without requiring a saved active scene.
            isolatedScene = EditorSceneManager.NewPreviewScene();
            SceneManager.MoveGameObjectToScene(fixture.Events.gameObject, isolatedScene);
            sessionObject = new GameObject("Synthetic native session owner");
            sessionObject.SetActive(false);
            SceneManager.MoveGameObjectToScene(sessionObject, isolatedScene);
            session = sessionObject.AddComponent<CampExpeditionSession>();
            nativeViewObject = new GameObject("Synthetic native view", typeof(CampExpeditionMenuView));
            SceneManager.MoveGameObjectToScene(nativeViewObject, isolatedScene);
            typeof(CampExpeditionSession).GetField("view", PrivateInstance).SetValue(session, nativeViewObject.GetComponent<CampExpeditionMenuView>());
            typeof(CampExpeditionSession).GetField("module", PrivateInstance).SetValue(session, fixture.Module);
            Sessions[isolatedScene.handle] = session;
            sessionObject.SetActive(true);
            var treeMove = InputActionReference.Create(fixture.Module.actionsAsset.FindAction("UI/Point", true));
            try
            {
                fixture.Module.move = treeMove;
                fixture.Module.cancel = null;
                fixture.Module.submit = null;
                fixture.Module.leftClick = null;
                AssertInstallerModule(isolatedScene);
                AssertInstallerModule(isolatedScene);
                Assert.That(fixture.Module.move, Is.SameAs(treeMove));
                Assert.That(fixture.Module.cancel, Is.Null);
                Assert.That(fixture.Module.submit, Is.Null);
                Assert.That(fixture.Module.leftClick, Is.Null);

                Sessions.Remove(isolatedScene.handle);
                sessionObject.SetActive(false);
                AssertInstallerModule(isolatedScene);
                Assert.That(fixture.Module.move.action.name, Is.EqualTo("Navigate"));
                Assert.That(fixture.Module.cancel.action.name, Is.EqualTo("Cancel"));
                Assert.That(fixture.Module.submit.action.name, Is.EqualTo("Submit"));
                Assert.That(fixture.Module.leftClick.action.name, Is.EqualTo("Click"));
            }
            finally { Object.DestroyImmediate(treeMove); }
        }

        private void AssertInstallerModule(Scene scene)
        {
            var method = typeof(TopDown3DInventoryUiSceneInstaller)
                .GetMethod("EnsureSingleEventSystem", BindingFlags.NonPublic | BindingFlags.Static);
            var arguments = new object[] { scene, fixture.Module.actionsAsset, null };
            Assert.That((bool)method.Invoke(null, arguments), Is.True);
            Assert.That(arguments[2], Is.SameAs(fixture.Events));
            Assert.That(fixture.Events.GetComponents<InputSystemUIInputModule>(), Has.Length.EqualTo(1));
        }
    }
}
