using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using System.Linq;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DCampIntegrationTests
    {
        private TopDown3DCampController camp;
        private TopDown3DPlayerInventory inventory;
        private TopDown3DPlayerMotor motor;
        private TopDown3DSurvivalVitals vitals;
        private TopDown3DPlayerAnimationDriver animation;
        private Gamepad pad;
        private InputSettings original, manual;
        [SetUp] public void Setup()
        {
            var scene = EditorSceneManager.OpenScene(BooterBigArm.Editor.TopDown3DCampAssetBuilder.StudyPath, OpenSceneMode.Single);
            var all = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Component>(true)).ToArray();
            inventory = all.OfType<TopDown3DPlayerInventory>().Single(); motor = inventory.GetComponent<TopDown3DPlayerMotor>();
            vitals = inventory.GetComponent<TopDown3DSurvivalVitals>(); vitals.Configure(vitals.Settings, true);
            original = InputSystem.settings; manual = Object.Instantiate(original);
            manual.SetInternalFeatureFlag("RUN_PLAYER_UPDATES_IN_EDIT_MODE", true); manual.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            manual.updateMode = InputSettings.UpdateMode.ProcessEventsManually; InputSystem.settings = manual;
            pad = InputSystem.AddDevice<Gamepad>();
            MotorMessage("Awake"); Physics.SyncTransforms(); MotorMessage("FixedUpdate");
            animation = inventory.GetComponent<TopDown3DPlayerAnimationDriver>();
            typeof(TopDown3DPlayerAnimationDriver).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(animation, null);
            Assert.That(animation.ConfigureCamp(TopDown3DCampSettings.Load()), Is.True);
            TopDown3DInventoryUiSceneInstaller.TryInstallForScene(scene);
            camp = inventory.GetComponent<TopDown3DCampController>(); Assert.That(camp, Is.Not.Null);
        }
        [TearDown] public void Cleanup()
        {
            if (camp != null) camp.InterruptForSystemMenu();
            if (animation != null) typeof(TopDown3DPlayerAnimationDriver).GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(animation, null);
            InputSystem.RemoveDevice(pad); InputSystem.settings = original; Object.DestroyImmediate(manual);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
        private void Tick() => Invoke("Update");
        private void MotorMessage(string method)
            => typeof(TopDown3DPlayerMotor).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(motor, null);
        private void Invoke(string method, params object[] args)
            => typeof(TopDown3DCampController).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(camp, args);
        private void Input(GamepadState state) { InputSystem.QueueStateEvent(pad, state); InputSystem.Update(); }
        private void Elapse(float seconds) { typeof(TopDown3DCampController).GetField("elapsed", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(camp, seconds); Tick(); }
        private void Deploy()
        {
            Assert.That(camp.CallUp(), Is.True); Input(new GamepadState()); Tick();
            Input(new GamepadState().WithButton(GamepadButton.South)); Tick();
            Assert.That(camp.Phase, Is.EqualTo(TopDown3DCampPhase.SettingUp));
            Input(new GamepadState()); Elapse(3);
            Assert.That(camp.Record, Is.Not.Null); Assert.That(camp.KitCarried, Is.False);
            Assert.That(camp.Phase, Is.EqualTo(TopDown3DCampPhase.Idle));
        }
        [Test] public void FreshConfirmDeploysOneCampAndPackingReturnsSameKit()
        {
            Deploy(); var record = camp.Record;
            Assert.That(camp.CallUp(), Is.True); Assert.That(camp.Record, Is.SameAs(record));
            Input(new GamepadState()); Tick(); Invoke("BeginPack"); Elapse(3);
            Assert.That(camp.Record, Is.Null); Assert.That(camp.KitCarried, Is.True);
            Assert.That(inventory.State.Slots.Where(slot => slot.ItemId == TopDown3DCampSettings.ItemId).Sum(slot => slot.Quantity), Is.EqualTo(1));
        }
        [Test] public void PreviewCancellationConsumesNothingAndReleasesMotor()
        {
            Assert.That(camp.CallUp(), Is.True); camp.Cancel("Canceled");
            Assert.That(camp.Record, Is.Null); Assert.That(camp.KitCarried, Is.True);
            Assert.That(motor.IsActionConstrained, Is.False);
        }
        [Test] public void DepletedHealthCannotStartAnotherCampAction()
        {
            vitals.SetValue(TopDown3DSurvivalVital.Health, 0);
            Assert.That(camp.CanCallUp, Is.False); Assert.That(camp.Record, Is.Null); Assert.That(camp.KitCarried, Is.True);
        }
        [TestCase(true)]
        [TestCase(false)]
        public void FullReserveCanStartAndRemainInCampActivity(bool sleep)
        {
            var baseline = Time.timeScale;
            try
            {
                Time.timeScale = 1; Deploy(); vitals.SetValue(TopDown3DSurvivalVital.Reserve, vitals.Settings.MaximumReserve);
                Assert.That(camp.CallUp(), Is.True); Input(new GamepadState()); Tick(); Invoke("BeginRecovery", sleep);
                Assert.That(camp.Phase, Is.EqualTo(TopDown3DCampPhase.Approaching));
                MotorMessage("FixedUpdate"); Elapse(1);
                Assert.That(camp.Phase, Is.EqualTo(sleep ? TopDown3DCampPhase.Sleeping : TopDown3DCampPhase.Resting));
                vitals.ReconcileRuntimeClock(Time.timeAsDouble, 0, false);
                Assert.That(vitals.Activity, Is.EqualTo(sleep ? TopDown3DSurvivalActivity.Sleep : TopDown3DSurvivalActivity.Rest));
                var reserveAtStart = vitals.Reserve;
                Assert.That(reserveAtStart, Is.GreaterThan(camp.Settings.sleepTarget * vitals.Settings.MaximumReserve));
                vitals.AdvanceActivity(10);
                Assert.That(camp.Phase, Is.EqualTo(sleep ? TopDown3DCampPhase.Sleeping : TopDown3DCampPhase.Resting));
                Assert.That(vitals.Reserve, Is.EqualTo(reserveAtStart));
                camp.NotifyDanger("Manual test wake");
                Assert.That(Time.timeScale, Is.EqualTo(1));
            }
            finally { Time.timeScale = baseline; }
        }
        [Test] public void HealthLossWakesAndReleasesSpeedWithoutDeletingCamp()
        {
            var baseline = Time.timeScale;
            try
            {
                Time.timeScale = 1; Deploy(); vitals.SetValue(TopDown3DSurvivalVital.Reserve, 20);
                Assert.That(camp.CallUp(), Is.True); Input(new GamepadState()); Tick(); Invoke("BeginRecovery", true);
                Assert.That(camp.Phase, Is.EqualTo(TopDown3DCampPhase.Approaching));
                MotorMessage("FixedUpdate"); Assert.That(camp.Phase, Is.EqualTo(TopDown3DCampPhase.Reclining));
                Elapse(1); Assert.That(camp.Phase, Is.EqualTo(TopDown3DCampPhase.Sleeping));
                Assert.That(Time.timeScale, Is.EqualTo(4));
                vitals.SetValue(TopDown3DSurvivalVital.Health, 90);
                Assert.That(Time.timeScale, Is.EqualTo(1)); Assert.That(camp.Phase, Is.EqualTo(TopDown3DCampPhase.Waking));
                Elapse(1); Assert.That(camp.Record, Is.Not.Null); Assert.That(motor.IsActionConstrained, Is.False);
            }
            finally { Time.timeScale = baseline; }
        }
        [Test] public void RepeatedInstallerDoesNotResetKitOrDuplicateWorldOwner()
        {
            Deploy(); var record = camp.Record; var scene = inventory.gameObject.scene;
            TopDown3DInventoryUiSceneInstaller.TryInstallForScene(scene);
            Assert.That(camp.Record, Is.SameAs(record)); Assert.That(camp.KitCarried, Is.False);
            var all = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
            var world = all.Single(t => t.name == "Camp World");
            Assert.That(world.parent.name, Is.EqualTo("World"));
            Assert.That(camp.Camp.transform.parent, Is.SameAs(world));
            Assert.That(all.Single(t => t.name == "World Simulation").parent.name, Is.EqualTo("Systems"));
            foreach (var name in new[] { "Radial Menu", "System Menu", "Camp Canvas" })
                Assert.That(all.Single(t => t.name == name).parent.name, Is.EqualTo("UI"));
        }
        [Test] public void FocusLossCancelsPreviewAndDoesNotConsumeOrRetrieveKit()
        {
            Assert.That(camp.CallUp(), Is.True);
            Invoke("OnApplicationFocus", false);
            Assert.That(camp.Phase, Is.EqualTo(TopDown3DCampPhase.Idle));
            Assert.That(camp.KitCarried, Is.True); Assert.That(motor.IsActionConstrained, Is.False);
            Deploy(); var record = camp.Record;
            Assert.That(camp.CallUp(), Is.True); Invoke("OnApplicationFocus", false);
            Assert.That(camp.Record, Is.SameAs(record)); Assert.That(camp.KitCarried, Is.False);
        }
        [Test] public void CloakBindsToExistingHumanoidBonesWithRuntimeOwnedMeshes()
        {
            var graph = (UnityEngine.Playables.PlayableGraph)typeof(TopDown3DPlayerAnimationDriver)
                .GetField("playableGraph", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(animation);
            graph.Evaluate(0);
            typeof(TopDown3DPlayerAnimationDriver).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(animation, null);
            var skins = inventory.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(skin => skin.name == "Cloak cloth skin").ToArray();
            Assert.That(skins.Length, Is.EqualTo(2));
            foreach (var skin in skins)
            {
                Assert.That(skin.bones.Length, Is.EqualTo(2)); Assert.That(skin.sharedMesh.vertexCount, Is.GreaterThan(0));
                Assert.That(skin.sharedMesh.boneWeights.All(weight => Mathf.Abs(weight.weight0 + weight.weight1 - 1) < .001f), Is.True);
            }
        }
    }
}
