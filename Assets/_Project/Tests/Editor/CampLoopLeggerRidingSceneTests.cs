using System.IO;
using System.Linq;
using System.Reflection;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

namespace BooterBigArm.Tests
{
    public sealed class CampLoopLeggerRidingSceneTests
    {
        private Scene scene;
        private string production;
        private TopDown3DLeggerRidingController riding;
        private TopDown3DPlayerMotor player;
        private TopDown3DBigArmFollower follower;
        private TopDown3DInputRouter input;
        private TopDown3DAnimationPlaygroundSession lab;
        private LeggerGroundMotion ground;
        private InputActionAsset runtimeInput;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private T One<T>() where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).Single();
        [SetUp] public void SetUp()
        {
            production = File.ReadAllText("Assets/_Project/Scenes/Production/GreaterWasteland.unity");
            scene = EditorSceneManager.OpenScene(TopDown3DAnimationPlaygroundSession.ScenePath);
            riding = One<TopDown3DLeggerRidingController>(); player = One<TopDown3DPlayerMotor>();
            follower = One<TopDown3DBigArmFollower>(); input = One<TopDown3DInputRouter>(); lab = One<TopDown3DAnimationPlaygroundSession>();
        }
        [TearDown] public void TearDown()
        {
            if (riding != null && riding.HasRider)
            {
                input.SetLaboratoryResetBlocked(true); riding.InvalidateForReset(1);
                riding.TryRestoreForReset(out _);
            }
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (runtimeInput != null) Object.DestroyImmediate(runtimeInput);
            Assert.That(File.ReadAllText("Assets/_Project/Scenes/Production/GreaterWasteland.unity"), Is.EqualTo(production));
        }
        private void PrepareActors()
        {
            // Match the lab's runtime input clone; initialization must not alter shared asset overrides.
            runtimeInput = AnimationPlaygroundInputFactory.Create(input.InputActions);
            input.Configure(runtimeInput);
            input.Initialize();
            typeof(TopDown3DPlayerMotor).GetMethod("Awake", Private).Invoke(player, null);
            typeof(TopDown3DBigArmFollower).GetMethod("Awake", Private).Invoke(follower, null);
            typeof(TopDown3DBigArmCargoAccess).GetMethod("Awake", Private).Invoke(follower.GetComponent<TopDown3DBigArmCargoAccess>(), null);
            Physics.SyncTransforms(); Assert.That(follower.TryInitializeOnGround(), Is.True);
            var coordinator = player.gameObject.AddComponent<AnimationPlaygroundActionCoordinator>();
            typeof(TopDown3DAnimationPlaygroundSession).GetField("coordinator", Private).SetValue(lab, coordinator);
            player.BindAnimationPostureOwner(coordinator);
            typeof(TopDown3DPlayerActionController).GetMethod("Start", Private).Invoke(player.GetComponent<TopDown3DPlayerActionController>(), null);
            typeof(TopDown3DAnimationPlaygroundSession).GetField("<Readiness>k__BackingField", Private).SetValue(lab, AnimationPlaygroundReadiness.VisualReady);
            Assert.That(riding.Bind(), Is.True, riding.LastFailure);
            ground = (LeggerGroundMotion)typeof(TopDown3DLeggerRidingController).GetField("ground", Private).GetValue(riding);
            ground.BeginTick();
            var frame = (Transform)typeof(TopDown3DLeggerRidingController).GetField("seatFrame", Private).GetValue(riding);
            Assert.That(ground.TryExit(frame.position, follower.transform.position + Vector3.left * 1.5f, follower.transform.rotation,
                player.MaxWalkableSlope, out var exit), Is.True, "Fixture must provide a real supported approach.");
            player.Teleport(exit); Physics.SyncTransforms();
            typeof(TopDown3DPlayerMotor).GetField("<IsGrounded>k__BackingField", Private).SetValue(player, true);
        }
        private void CompleteMount()
        {
            var mode = Physics.simulationMode;
            try
            {
                Physics.simulationMode = SimulationMode.Script;
                for (int i = 0; i < 90 && riding.State.Phase == LeggerRidePhase.Mounting; i++)
                {
                    typeof(TopDown3DPlayerMotor).GetMethod("FixedUpdate", Private).Invoke(player, null);
                    riding.TickRiding(.02f); Physics.Simulate(.02f); Physics.SyncTransforms();
                }
            }
            finally { Physics.simulationMode = mode; }
            Assert.That(riding.State.Phase, Is.EqualTo(LeggerRidePhase.Mounted), riding.LastFailure);
        }
        [Test] public void SerializedInstallationRetainsNativeTerrainAndSingleOwners()
        { CampLoopLeggerRidingValidator.Validate(scene); Assert.That(TopDown3DAnimationPlaygroundSession.IsSessionOnly(scene), Is.True); }
        [Test] public void ActualSceneSupportsRepeatedAnimationFreeMountAndDismount()
        {
            PrepareActors();
            for (int i = 0; i < 3; i++)
            {
                Assert.That(riding.RequestMount(), Is.True, riding.LastFailure); CompleteMount();
                var position = follower.RideBody.position;
                Assert.That(player.IsMounted, Is.True);
                Assert.That(riding.RequestDismount(), Is.True); riding.TickRiding(.02f);
                Assert.That(riding.State.Phase, Is.EqualTo(LeggerRidePhase.OnFoot), riding.LastFailure);
                Assert.That(player.IsMounted, Is.False);
                Assert.That(Vector3.Distance(follower.RideBody.position, position), Is.LessThan(.01f));
                Physics.SyncTransforms();
            }
        }
        [Test] public void InstalledTargetWinsOverCargoAndPreservesCapturedIdentity()
        {
            PrepareActors(); var scanner = One<TopDown3DInteractionController>();
            player.FacePlanarDirection(follower.transform.position - player.Position); scanner.RefreshTarget();
            Assert.That(scanner.SelectedTarget, Is.TypeOf<TopDown3DLeggerMountTarget>());
            var capture = scanner.CaptureTarget(); Assert.That(capture.IsValid, Is.True);
            Assert.That(riding.RequestMount(), Is.True); CompleteMount();
            Assert.That(capture.IsValid, Is.False);
        }
        [Test] public void SharedDispatcherReservesMountAndCargoKeepsOtherActivityGates()
        {
            PrepareActors(); var scanner = One<TopDown3DInteractionController>();
            var actions = player.GetComponent<TopDown3DPlayerActionController>();
            player.FacePlanarDirection(follower.transform.position - player.Position); scanner.RefreshTarget();
            Assert.That(actions.TryBeginCurrentTarget(), Is.EqualTo(TopDown3DGatherActionResult.Completed));
            Assert.That(riding.State.Phase, Is.EqualTo(LeggerRidePhase.Mounting));
            Assert.That(actions.Activity.MotorOwned, Is.True); Assert.That(actions.CanOpenCargo, Is.False);
            CompleteMount();
            Assert.That(actions.IsBusy, Is.True);
            Assert.That(actions.CanOpenCargo, Is.EqualTo(TopDown3DCargoAccess.CanAccess(follower.GetComponent<TopDown3DBigArmCargo>(), player.transform)));
            var receiver = new BusyStation(); actions.ConfigureStationActions(receiver);
            Assert.That(actions.CanOpenCargo, Is.False); receiver.Busy = false;
            actions.ConfigureStationActions(null);
        }
        private sealed class BusyStation : ITopDown3DStationActionReceiver
        {
            public bool Busy = true;
            public bool IsBusy => Busy;
            public bool TryBegin(TopDown3DInteractionTarget target) => false;
        }
        [Test] public void DisabledOnFootRidingControllerRestoresLegacyCargoTarget()
        {
            PrepareActors(); riding.enabled = false;
            var scanner = One<TopDown3DInteractionController>();
            player.FacePlanarDirection(follower.transform.position - player.Position); scanner.RefreshTarget();
            Assert.That(scanner.SelectedTarget, Is.TypeOf<TopDown3DBigArmCargoAccess>());
        }
        [Test] public void NativeSceneResetRestoresStandingRiderAndRetainsCompanionCoordinate()
        {
            PrepareActors(); Assert.That(riding.RequestMount(), Is.True); CompleteMount();
            var position = follower.RideBody.position;
            input.SetLaboratoryResetBlocked(true); lab.ResetRegistry.Register(riding.ResetParticipant);
            Assert.That(lab.ResetRegistry.TryReset(), Is.True, lab.ResetRegistry.LastFailure);
            Assert.That(player.IsMounted, Is.False);
            Assert.That(player.GetComponent<Rigidbody>().useGravity, Is.True);
            Assert.That(follower.RideBody.position, Is.EqualTo(position));
        }
        [Test] public void AllBlockedNativeExitsKeepTheRiderPassiveAndMounted()
        {
            PrepareActors(); Assert.That(riding.RequestMount(), Is.True); CompleteMount();
            var anchors = (Transform[])typeof(TopDown3DLeggerRidingController).GetField("exitFrames", Private).GetValue(riding);
            var blockers = anchors.Select(anchor =>
            {
                var go = new GameObject("Changed exit obstruction");
                go.transform.position = anchor.position + Vector3.up;
                go.AddComponent<BoxCollider>().size = new Vector3(.8f, 6, .8f); return go;
            }).ToArray();
            try
            {
                Physics.SyncTransforms(); Assert.That(riding.RequestDismount(), Is.True); riding.TickRiding(.02f);
                Assert.That(riding.State.Phase, Is.EqualTo(LeggerRidePhase.Mounted));
                Assert.That(player.IsMounted, Is.True); Assert.That(player.GetComponent<Rigidbody>().isKinematic, Is.True);
                Assert.That(riding.LastFailure, Does.Contain("No safe exit"));
            }
            finally { foreach (var go in blockers) Object.DestroyImmediate(go); Physics.SyncTransforms(); }
        }
        [Test] public void NativeMountedMovementUsesOneResolvedPoseRegardlessOfCallbackOrder()
        {
            PrepareActors(); Assert.That(riding.RequestMount(), Is.True); CompleteMount();
            var initial = follower.RideBody.position;
            var mode = Physics.simulationMode;
            try
            {
                Physics.simulationMode = SimulationMode.Script;
                typeof(TopDown3DInputRouter).GetField("<MoveValue>k__BackingField", Private).SetValue(input, Vector2.up * .4f);
                var seat = (Transform)typeof(TopDown3DLeggerRidingController).GetField("seatFrame", Private).GetValue(riding);
                var offset = follower.transform.InverseTransformPoint(seat.position);
                for (int i = 0; i < 12; i++)
                {
                    var callback = typeof(TopDown3DPlayerMotor).GetMethod("FixedUpdate", Private);
                    if (i % 2 == 0) callback.Invoke(player, null);
                    riding.TickRiding(.02f); Physics.Simulate(.02f); Physics.SyncTransforms();
                    if (i % 2 != 0) callback.Invoke(player, null);
                    Assert.That(Vector3.Distance(player.Position, follower.RideBody.position + follower.RideBody.rotation * offset), Is.LessThan(.015f));
                    Assert.That(riding.QueryCount, Is.LessThanOrEqualTo(256));
                    Assert.That(riding.SubstepCount, Is.LessThanOrEqualTo(8));
                }
                Assert.That(Vector3.Distance(initial, follower.RideBody.position), Is.GreaterThan(.05f),
                    $"{riding.State.Phase}: {riding.MotionBlock}, input {input.MoveValue}, pose {follower.RideBody.position}, queries {riding.QueryCount}, {riding.LastFailure}");
            }
            finally { Physics.simulationMode = mode; input.ClearLaboratoryIntent(); }
        }
    }
}
