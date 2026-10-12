using System.Linq;
using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DWaypointRouteTests
    {
        private readonly Vector3[] route = { Vector3.zero, new(0, 0, 8), new(0, 0, 16) };
        [Test] public void ReturnFromMiddleFollowsEarlierNodesOnly()
        {
            Assert.That(TopDown3DWaypointRoute.TryReturn(new(1, 1, 12), route, 1.5f, out var path, out var distance), Is.True);
            Assert.That(path.Select(p => p.z), Is.EqualTo(new[] {12f, 8f, 0f})); Assert.That(distance, Is.GreaterThan(12));
        }
        [TestCase(2, 0, 8)] [TestCase(0, 6, 8)] [TestCase(0, 0, 20)]
        public void OutsideCorridorOrWrongHeightCannotReturn(float x, float y, float z)
        { Assert.That(TopDown3DWaypointRoute.TryReturn(new(x, y, z), route, 1.5f, out _, out _), Is.False); }
        [Test] public void QueriesAreDeterministicAndPreserveAuthoredRoute()
        {
            var initial = (Vector3[])route.Clone();
            TopDown3DWaypointRoute.TryReturn(new(0, 1, 10), route, 1.5f, out var a, out var da);
            TopDown3DWaypointRoute.TryReturn(new(0, 1, 10), route, 1.5f, out var b, out var db);
            Assert.That(a, Is.EqualTo(b)); Assert.That(da, Is.EqualTo(db)); Assert.That(route, Is.EqualTo(initial));
        }
        [Test] public void InvalidRouteFailsClosed()
        {
            Assert.That(TopDown3DWaypointRoute.TryReturn(Vector3.zero, new[] {Vector3.zero, Vector3.zero}, 1, out _, out _), Is.False);
            Assert.That(TopDown3DWaypointRoute.TryReturn(new(float.NaN, 0, 0), route, 1, out _, out _), Is.False);
        }
        [Test] public void DiscoveryUsesWorldAndSiteIdentity()
        {
            var registry = new TopDown3DWaypointCampRegistry("world", "camp", false);
            Assert.That(registry.Discover("other", "camp"), Is.False); Assert.That(registry.Discovered, Is.False);
            Assert.That(registry.Discover("world", "other"), Is.False);
            Assert.That(registry.Discover("world", "camp"), Is.True); Assert.That(registry.Discovered, Is.True);
            Assert.That(registry.Discover("world", "camp"), Is.True);
        }
        [Test] public void DefinitionRejectsInvalidTuning()
        {
            var definition = ScriptableObject.CreateInstance<TopDown3DWaypointCampSettings>();
            try { Assert.That(definition.TryValidate(out _), Is.True); definition.travelSpeed = 5; Assert.That(definition.TryValidate(out _), Is.False); }
            finally { Object.DestroyImmediate(definition); }
        }
        [Test] public void NearestRoutePointProvidesGuidanceWithoutGrantingTravel()
        {
            Assert.That(TopDown3DWaypointRoute.TryClosestPoint(new(4, 1, 12), route, out var point, out _, out var distance), Is.True);
            Assert.That(point, Is.EqualTo(new Vector3(0, 0, 12))); Assert.That(distance, Is.EqualTo(4));
            Assert.That(TopDown3DWaypointRoute.TryReturn(new(4, 1, 12), route, 1.5f, out _, out _), Is.False);
        }
        [Test] public void SmallFrameMovementsAccumulateIntoProgress()
        {
            var anchor = Vector3.zero; var progressWindows = 0;
            for (var i = 1; i <= 500; i++)
            {
                var position = new Vector3(0, 0, i * .001f);
                if (!TopDown3DWaypointCampController.HasMeaningfulProgress(anchor, position)) continue;
                progressWindows++; anchor = position;
            }
            Assert.That(progressWindows, Is.GreaterThanOrEqualTo(4));
            Assert.That(TopDown3DWaypointCampController.HasMeaningfulProgress(Vector3.zero, Vector3.up), Is.False);
        }
    }

    public sealed class TopDown3DWaypointCampIntegrationTests
    {
        private TopDown3DWaypointCampController waypoint;
        private TopDown3DCampController recovery;
        private TopDown3DPlayerMotor motor;
        private TopDown3DPlayerInventory inventory;
        private TopDown3DInputRouter input;
        private TopDown3DSurvivalVitals vitals;
        private TopDown3DBigArmFollower legger;
        private TopDown3DWaypointCampSettings definition;
        private InputSettings original, manual;
        private float baselineSpeed;
        private Gamepad pad;
        [SetUp] public void Setup()
        {
            baselineSpeed = Time.timeScale; Time.timeScale = 1;
            var scene = EditorSceneManager.OpenScene(BooterBigArm.Editor.TopDown3DCampAssetBuilder.StudyPath, OpenSceneMode.Single);
            var components = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Component>(true)).ToArray();
            inventory = components.OfType<TopDown3DPlayerInventory>().Single(); motor = inventory.GetComponent<TopDown3DPlayerMotor>();
            vitals = inventory.GetComponent<TopDown3DSurvivalVitals>(); input = components.OfType<TopDown3DInputRouter>().Single();
            original = InputSystem.settings; manual = Object.Instantiate(original);
            manual.SetInternalFeatureFlag("RUN_PLAYER_UPDATES_IN_EDIT_MODE", true); manual.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            InputSystem.settings = manual;
            pad = InputSystem.AddDevice<Gamepad>();
            Call(motor, "Awake"); Physics.SyncTransforms(); Call(motor, "FixedUpdate");
            var animation = inventory.GetComponent<TopDown3DPlayerAnimationDriver>(); Call(animation, "Awake");
            Assert.That(animation.ConfigureCamp(TopDown3DCampSettings.Load()), Is.True);
            vitals.Configure(vitals.Settings, true);
            var ui = TopDown3DInventoryUiSceneInstaller.TryInstallForScene(scene); recovery = inventory.GetComponent<TopDown3DCampController>();
            legger = components.OfType<TopDown3DBigArmFollower>().Single(); Call(legger, "Awake");
            definition = ScriptableObject.CreateInstance<TopDown3DWaypointCampSettings>();
            definition.position = new Vector3(4, 0, 0); definition.returnRoute = new[] { new Vector3(0, 0, 0), new Vector3(0, 0, 8), new Vector3(0, 0, 16) };
            waypoint = inventory.gameObject.AddComponent<TopDown3DWaypointCampController>(); waypoint.Configure(definition, input, ui.EventSystem, recovery);
            Assert.That(waypoint.Site, Is.Not.Null); input.EnterGameplayMode();
        }
        [TearDown] public void Cleanup()
        {
            if (waypoint != null) Object.DestroyImmediate(waypoint);
            if (recovery != null) recovery.InterruptForSystemMenu();
            if (pad != null) InputSystem.RemoveDevice(pad);
            InputSystem.settings = original; if (manual != null) Object.DestroyImmediate(manual);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (definition != null) Object.DestroyImmediate(definition); Time.timeScale = baselineSpeed;
        }
        private static void Call(object target, string name, params object[] args)
            => target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
        private void Place(Vector3 ground)
        { motor.Teleport(ground + Vector3.up * 1.25f); Physics.SyncTransforms(); Call(motor, "FixedUpdate"); }
        private void Regroup()
        { legger.transform.position = motor.Position + new Vector3(-1.7f, -.43f, 0); Physics.SyncTransforms(); }
        private void Arm() => typeof(TopDown3DWaypointCampController).GetField("armed", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(waypoint, true);
        [Test] public void SiteAndPortableKitHaveIndependentOwnership()
        { Assert.That(waypoint.Site.CanPack, Is.False); Assert.That(waypoint.Site.StableId, Is.EqualTo("camp.starting")); Assert.That(recovery.Record, Is.Null); Assert.That(recovery.KitCarried, Is.True); }
        [Test] public void EstablishedRecoveryWorksWithoutKitAndCannotPack()
        {
            inventory.State.TryRemove(TopDown3DCampSettings.ItemId, 1, () => true);
            Place(new(4, 0, 1.2f)); Assert.That(recovery.OpenActions(waypoint.Site), Is.True);
            typeof(TopDown3DCampController).GetField("armed", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(recovery, true);
            Call(recovery, "BeginPack"); Assert.That(recovery.Phase, Is.EqualTo(TopDown3DCampPhase.Actions));
            Call(recovery, "BeginRecovery", true); Assert.That(recovery.Phase, Is.EqualTo(TopDown3DCampPhase.Approaching));
            Call(motor, "FixedUpdate"); Assert.That(recovery.Phase, Is.EqualTo(TopDown3DCampPhase.Reclining));
            Assert.That(recovery.Record, Is.Null); Assert.That(recovery.KitCarried, Is.False);
        }
        [Test] public void SharedSpeedSurvivesPortableControllerDestruction()
        {
            var scene = inventory.gameObject.scene; var speed = TopDown3DWorldSpeedController.ForScene(scene);
            Object.DestroyImmediate(waypoint); Object.DestroyImmediate(recovery);
            Assert.That(TopDown3DWorldSpeedController.ForScene(scene), Is.SameAs(speed));
            var owner = new object(); Assert.That(speed.TryAcquire(owner, 4), Is.True); Assert.That(speed.TryAcquire(new object(), 4), Is.False); speed.Release(owner); Assert.That(Time.timeScale, Is.EqualTo(1));
        }
        [Test] public void MapCancelPreservesActorPositionsAndVitals()
        {
            var position = motor.Position; var companion = legger.transform.position; var hunger = vitals.Hunger;
            Assert.That(waypoint.Open(), Is.True); waypoint.Close(); Assert.That(motor.IsActionConstrained, Is.False);
            Assert.That(motor.Position, Is.EqualTo(position)); Assert.That(vitals.Hunger, Is.EqualTo(hunger)); Assert.That(legger.transform.position, Is.EqualTo(companion));
        }
        [Test] public void OutsideRouteAndDistantCompanionExplainFailure()
        {
            Place(new(4, 0, 8)); Assert.That(waypoint.TryQuote(out _, out _, out var reason), Is.False); Assert.That(reason, Does.Contain("Outside"));
            Place(new(0, 0, 8)); legger.transform.position = new Vector3(-20, 1, 0);
            Assert.That(waypoint.TryQuote(out _, out _, out reason), Is.False); Assert.That(reason, Does.Contain("Regroup"));
        }
        [Test] public void LowSuppliesPreventDepartureWithoutSpending()
        {
            Place(new(0, 0, 8)); Regroup(); vitals.SetValue(TopDown3DSurvivalVital.Thirst, 1);
            Assert.That(waypoint.TryQuote(out _, out _, out var reason), Is.False); Assert.That(reason, Does.Contain("Supplies")); Assert.That(vitals.Thirst, Is.EqualTo(1));
        }
        [Test] public void ObstaclePreventsRouteCommit()
        {
            Place(new(0, 0, 8)); Regroup(); var blocker = GameObject.CreatePrimitive(PrimitiveType.Cube); blocker.transform.position = new(0, 1, 4); blocker.transform.localScale = new(3, 2, 1); Physics.SyncTransforms();
            try { Assert.That(waypoint.TryQuote(out _, out _, out var reason), Is.False); Assert.That(reason, Does.Contain("blocked")); }
            finally { Object.DestroyImmediate(blocker); }
        }
        [TestCase("Damage")] [TestCase("Reset")] [TestCase("SpeedLost")]
        public void InterruptionsReleaseTravelWithoutRelocation(string interruption)
        {
            Place(new(0, 0, 8)); Regroup(); var booter = motor.Position; var companion = legger.transform.position;
            Assert.That(waypoint.Open(), Is.True); Arm(); waypoint.BeginReturn(); Assert.That(waypoint.Traveling, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(4)); Call(waypoint, interruption);
            Assert.That(waypoint.Traveling, Is.False); Assert.That(Time.timeScale, Is.EqualTo(1)); Assert.That(motor.IsActionConstrained, Is.False);
            Assert.That(motor.Position, Is.EqualTo(booter)); Assert.That(legger.transform.position, Is.EqualTo(companion));
        }
        [Test] public void MenuCancelsTravelWithoutResumingOnClose()
        {
            Place(new(0, 0, 8)); Regroup(); Assert.That(waypoint.Open(), Is.True); Arm(); waypoint.BeginReturn(); Assert.That(waypoint.Traveling, Is.True);
            input.EnterSystemMenuMode(); Assert.That(waypoint.Traveling, Is.False); Assert.That(Time.timeScale, Is.EqualTo(1));
            input.EnterGameplayMode(); Assert.That(waypoint.Traveling, Is.False);
        }
        [Test] public void ConfigurationIsIdempotent()
        {
            var site = waypoint.Site; var registry = waypoint.Registry;
            waypoint.Configure(definition, input, null, recovery);
            Assert.That(waypoint.Site, Is.SameAs(site)); Assert.That(waypoint.Registry, Is.SameAs(registry));
        }
        [Test] public void OpeningSubmitMustReleaseBeforeReturnCanStart()
        {
            Place(new(0, 0, 8)); Regroup();
            InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.South)); InputSystem.Update();
            Assert.That(waypoint.Open(), Is.True); Call(waypoint, "Update"); waypoint.BeginReturn();
            Assert.That(waypoint.IsOpen, Is.True); Assert.That(waypoint.Traveling, Is.False); Assert.That(Time.timeScale, Is.EqualTo(1));
            InputSystem.QueueStateEvent(pad, new GamepadState()); InputSystem.Update(); Call(waypoint, "Update");
            waypoint.BeginReturn(); Assert.That(waypoint.Traveling, Is.True);
        }
        [Test] public void PhysicalReturnCompletesWithContinuousActorMovementAndUnchangedCargo()
        {
            Place(new(0, 0, 8)); Regroup();
            var cargo = legger.GetComponent<TopDown3DBigArmCargo>(); var beforeCargo = JsonUtility.ToJson(cargo.CaptureSnapshot());
            Assert.That(waypoint.Open(), Is.True); Arm(); waypoint.BeginReturn(); Assert.That(waypoint.Traveling, Is.True);
            var priorMode = Physics.simulationMode; Physics.simulationMode = SimulationMode.Script;
            try
            {
                for (var step = 0; step < 600 && waypoint.Traveling; step++)
                {
                    var booter = motor.Position; var companion = legger.transform.position;
                    Call(motor, "FixedUpdate"); Call(legger, "FixedUpdate"); Physics.Simulate(.02f); Call(waypoint, "Update");
                    Assert.That(Vector3.ProjectOnPlane(motor.Position - booter, Vector3.up).magnitude, Is.LessThan(.2f));
                    Assert.That(Vector3.ProjectOnPlane(legger.transform.position - companion, Vector3.up).magnitude, Is.LessThan(.25f));
                }
                Assert.That(waypoint.Traveling, Is.False); Assert.That(Vector3.ProjectOnPlane(motor.Position, Vector3.up).magnitude, Is.LessThan(.3f), waypoint.LastTravelResult);
                Assert.That(Time.timeScale, Is.EqualTo(1)); Assert.That(motor.IsActionConstrained, Is.False);
                Assert.That(JsonUtility.ToJson(cargo.CaptureSnapshot()), Is.EqualTo(beforeCargo));
            }
            finally { Physics.simulationMode = priorMode; }
        }
        [Test] public void FocusLossReleasesSpeedAndDoesNotResume()
        {
            Place(new(0, 0, 8)); Regroup(); Assert.That(waypoint.Open(), Is.True); Arm(); waypoint.BeginReturn();
            Call(waypoint, "OnApplicationFocus", false); Assert.That(waypoint.Traveling, Is.False); Assert.That(Time.timeScale, Is.EqualTo(1));
            Call(waypoint, "OnApplicationFocus", true); Assert.That(waypoint.Traveling, Is.False);
        }
        [Test] public void DestroyedInterfaceReleasesTravelSafely()
        {
            Place(new(0, 0, 8)); Regroup(); Assert.That(waypoint.Open(), Is.True); Arm(); waypoint.BeginReturn();
            var view = (TopDown3DCampCanvas)typeof(TopDown3DWaypointCampController).GetField("view", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(waypoint);
            Object.DestroyImmediate(view.gameObject); Call(waypoint, "Update");
            Assert.That(waypoint.Traveling, Is.False); Assert.That(Time.timeScale, Is.EqualTo(1)); Assert.That(motor.IsActionConstrained, Is.False);
        }
        [TestCase(true)] [TestCase(false)]
        public void CampShortcutUsesNearbyEstablishedSiteInsteadOfPlacement(bool keepKit)
        {
            if (!keepKit) inventory.State.TryRemove(TopDown3DCampSettings.ItemId, 1, () => true);
            Place(new(4, 0, 1.2f)); Assert.That(recovery.CanCallUp, Is.True); Assert.That(recovery.CallUp(), Is.True);
            Assert.That(recovery.Phase, Is.EqualTo(TopDown3DCampPhase.Actions)); Assert.That(recovery.RecoveryTarget, Is.SameAs(waypoint.Site));
            Assert.That(recovery.Record, Is.Null); Assert.That(recovery.KitCarried, Is.EqualTo(keepKit));
        }
        [Test] public void CampShortcutCannotUseDistantEstablishedSiteWithoutKit()
        {
            inventory.State.TryRemove(TopDown3DCampSettings.ItemId, 1, () => true); Place(new(0, 0, 8));
            Assert.That(recovery.CanCallUp, Is.False); Assert.That(recovery.RecoveryTarget, Is.Null);
        }
        [Test] public void RegroupKeepsPanelOpenAndDoesNotRestartConsumedCommand()
        {
            Place(new(0, 0, 8)); legger.transform.position = new Vector3(-20, .82f, 8); Physics.SyncTransforms();
            Assert.That(waypoint.Open(), Is.True); Arm(); waypoint.RequestRegroup();
            Assert.That(waypoint.IsOpen, Is.True); Assert.That(waypoint.RegroupRequested, Is.True); Assert.That(motor.IsActionConstrained, Is.True);
            var pending = typeof(TopDown3DBigArmFollower).GetField("callRequested", BindingFlags.Instance | BindingFlags.NonPublic);
            pending.SetValue(legger, false); Arm(); waypoint.RequestRegroup(); Assert.That((bool)pending.GetValue(legger), Is.False);
        }
        [Test] public void RegroupArrivalEnablesExplicitReturnWithoutAutomaticDeparture()
        {
            Place(new(0, 0, 8)); legger.transform.position = new Vector3(-20, .82f, 8); Physics.SyncTransforms();
            Assert.That(waypoint.Open(), Is.True); Arm(); waypoint.RequestRegroup(); Regroup(); Call(waypoint, "Refresh");
            Assert.That(waypoint.RegroupRequested, Is.False); Assert.That(waypoint.IsOpen, Is.True); Assert.That(waypoint.Traveling, Is.False);
            Assert.That(waypoint.TryQuote(out _, out _, out _), Is.True); Arm(); waypoint.BeginReturn(); Assert.That(waypoint.Traveling, Is.True);
        }
        [Test] public void OffRouteFeedbackProvidesDirectionAndDistance()
        {
            Place(new(4, 0, 8)); Assert.That(waypoint.RouteGuidance(), Does.Contain("4 m W"));
            Place(new(0, 6, 8)); Assert.That(waypoint.RouteGuidance(), Does.Contain("ground level"));
        }
        [Test] public void UnavailableAndBlockedLeggerHaveDistinctFeedback()
        {
            Place(new(0, 0, 8)); Regroup(); legger.enabled = false;
            Assert.That(waypoint.TryQuote(out _, out _, out var reason), Is.False); Assert.That(reason, Does.Contain("unavailable"));
            legger.enabled = true;
            typeof(TopDown3DBigArmFollower).GetProperty("State").SetValue(legger, TopDown3DBigArmFollower.FollowState.WaitingForRoute);
            Assert.That(waypoint.TryQuote(out _, out _, out reason), Is.False); Assert.That(reason, Does.Contain("shared route"));
        }
    }
}
