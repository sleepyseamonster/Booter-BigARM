using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace BooterBigArm.Tests
{
    // UI and clock/physics fixture only: no production scene or gameplay smoke traversal.
    public sealed class TopDown3DMenuUiIntegrationTests
    {
        private TopDown3DMenuTests fixture;
        private bool background;
        private GameObject probe;

        [UnitySetUp]
        public IEnumerator EnterRuntime()
        {
            if (!Application.isBatchMode) Assert.Ignore("Run this scene-isolated fixture in the disposable validation project.");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            yield return new EnterPlayMode();
            background = Application.runInBackground; Application.runInBackground = true;
            fixture = new TopDown3DMenuTests(); fixture.SetUp();
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        }

        [UnityTearDown]
        public IEnumerator LeaveRuntime()
        {
            fixture?.TearDown(); fixture = null;
            if (probe != null) Object.DestroyImmediate(probe);
            Application.runInBackground = background;
            if (Application.isPlaying) yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator UiModule_HeldSubmitIsBlocked_FreshSubmitAndBackWork_WorldClockContinues()
        {
            fixture.ProcessUiState(new GamepadState().WithButton(GamepadButton.South));
            fixture.Menu.Open();
            fixture.Module.Process(); Assert.That(fixture.Menu.IsOpen, Is.True);
            yield return null;
            fixture.ProcessUiState(new GamepadState());
            Assert.That(fixture.Menu.CurrentPanel, Is.EqualTo("Root"));
            yield return null;
            fixture.Events.SetSelectedGameObject(fixture.MenuRoot.transform.Find("Safe Area/Root/Settings").gameObject);
            fixture.ProcessUiState(new GamepadState().WithButton(GamepadButton.South));
            Assert.That(fixture.Menu.CurrentPanel, Is.EqualTo("Settings"));
            yield return null;
            fixture.ProcessUiState(new GamepadState());
            yield return null;
            fixture.ProcessUiState(new GamepadState().WithButton(GamepadButton.East));
            Assert.That(fixture.Menu.CurrentPanel, Is.EqualTo("Root"));
            var scale = Time.timeScale; var clock = Time.time;
            probe = new GameObject("Live menu physics probe"); var body = probe.AddComponent<Rigidbody>(); body.useGravity = false;
            body.linearVelocity = Vector3.right;
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That(Time.timeScale, Is.EqualTo(scale)); Assert.That(Time.time, Is.GreaterThan(clock));
            Assert.That(probe.transform.position.x, Is.GreaterThan(0));
        }
    }
}
