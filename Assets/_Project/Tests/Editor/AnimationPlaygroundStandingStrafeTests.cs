using System;
using System.Linq;
using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class AnimationPlaygroundStandingStrafeTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string ProfilePath = "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/StandingStrafeReview.asset";
        private static AnimationPlaygroundStandingStrafeProfile Profile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<AnimationPlaygroundStandingStrafeProfile>(ProfilePath);
            if (profile == null) Assert.Ignore("Restore the private standing strafe review.");
            return profile;
        }

        [Test]
        public void FacingRelativeAxisConesPreserveAnalogMagnitudeAndRejectUncoveredTravel()
        {
            foreach (var yaw in new[] { 0f, 37, 90, 179, 270 })
            {
                var rotation = Quaternion.Euler(0, yaw, 0); var forward = rotation * Vector3.forward;
                foreach (var item in new[] { (Vector3.forward * .4f, AnimationPlaygroundStandingTravel.Forward),
                    (Vector3.left * .3f, AnimationPlaygroundStandingTravel.Left), (Vector3.right * .7f, AnimationPlaygroundStandingTravel.Right) })
                {
                    var expected = rotation * item.Item1;
                    Assert.That(AnimationPlaygroundStandingStrafeMath.Classify(expected, forward, out var projected), Is.EqualTo(item.Item2));
                    Assert.That(Vector3.Distance(expected, projected), Is.LessThan(.00001f));
                }
                foreach (var direction in new[] { Vector3.back, new Vector3(1, 0, 1).normalized, new Vector3(-1, 0, -1).normalized })
                {
                    Assert.That(AnimationPlaygroundStandingStrafeMath.Classify(rotation * direction, forward, out var projected), Is.EqualTo(AnimationPlaygroundStandingTravel.Unsupported));
                    Assert.That(projected, Is.EqualTo(Vector3.zero));
                }
            }
            Assert.That(AnimationPlaygroundStandingStrafeMath.Classify(Vector3.zero, Vector3.forward, out _), Is.EqualTo(AnimationPlaygroundStandingTravel.Idle));
            Assert.That(AnimationPlaygroundStandingStrafeMath.Classify(Vector3.one, Vector3.up, out _), Is.EqualTo(AnimationPlaygroundStandingTravel.Unsupported));
        }

        [Test]
        public void LTAlsoKeepsLookAheadAndRequiresNeutralAfterMenusAndDeviceRemoval()
        {
            using var f = new InputFixture();
            f.PadState(new GamepadState { leftTrigger = 1 });
            Assert.That(f.Router.CameraLookAheadHeld, Is.True); Assert.That(f.Router.LaboratoryStandingFacingHeld, Is.True);
            f.Router.EnterSystemMenuMode(); Assert.That(f.Router.LaboratoryStandingFacingHeld, Is.False);
            f.Router.EnterGameplayMode(); InputSystem.Update(); Assert.That(f.Router.LaboratoryStandingFacingHeld, Is.False);
            f.PadState(new GamepadState()); f.PadState(new GamepadState { leftTrigger = 1 });
            Assert.That(f.Router.LaboratoryStandingFacingHeld, Is.True);
            InputSystem.RemoveDevice(f.Pad); Assert.That(f.Router.LaboratoryStandingFacingHeld, Is.False);
            InputSystem.AddDevice(f.Pad); f.PadState(new GamepadState { leftTrigger = 1 });
            Assert.That(f.Router.LaboratoryStandingFacingHeld, Is.False, "A reconnected held trigger must return to neutral.");
            f.PadState(new GamepadState()); f.PadState(new GamepadState { leftTrigger = 1 });
            Assert.That(f.Router.LaboratoryStandingFacingHeld, Is.True);
            Assert.That(f.Actions.FindAction("Gameplay/CameraLookAhead", true).bindings.Any(b => b.path == "<Gamepad>/leftTrigger"), Is.True);
        }

        [Test, Category("PrivateAnimationQualification")]
        public void MotorCapturesCompletedFacingBrakesAxisChangesAndReleasesAtOwnedBoundaries()
        {
            using var f = new InputFixture(); var motor = f.Actor.AddComponent<TopDown3DPlayerMotor>();
            typeof(TopDown3DPlayerMotor).GetField("input", Private).SetValue(motor, f.Router);
            var body = f.Actor.GetComponent<Rigidbody>(); var owner = new object(); var profile = Profile();
            motor.BindAnimationPostureOwner(owner);
            Assert.Throws<ArgumentException>(() => motor.ConfigureLaboratoryStandingStrafe(new object(), profile, () => true));
            motor.ConfigureLaboratoryStandingStrafe(owner, profile, () => true);
            typeof(TopDown3DPlayerMotor).GetProperty("IsGrounded").SetValue(motor, true);
            Vector3 Apply(Vector3 direction)
            {
                var args = new object[] { direction };
                typeof(TopDown3DPlayerMotor).GetMethod("ApplyStandingStrafeIntent", Private).Invoke(motor, args);
                return (Vector3)args[0];
            }
            Apply(Vector3.zero); // neutral arms the newly configured lease
            body.rotation = Quaternion.Euler(0, 90, 0);
            f.PadState(new GamepadState { leftTrigger = 1 });
            Assert.That(Vector3.Distance(Apply(Vector3.forward * .4f), Vector3.forward * .4f), Is.LessThan(.00001));
            Assert.That(motor.LaboratoryStandingTravel, Is.EqualTo(AnimationPlaygroundStandingTravel.Left));
            var captured = (Vector3)typeof(TopDown3DPlayerMotor).GetField("standingStrafeFacing", Private).GetValue(motor);
            Assert.That(Vector3.Distance(captured, Vector3.right), Is.LessThan(.00001));
            body.linearVelocity = Vector3.forward * .5f;
            Assert.That(Apply(Vector3.back), Is.EqualTo(Vector3.zero), "A side reversal brakes before changing sources.");
            Assert.That(motor.LaboratoryStandingTravel, Is.EqualTo(AnimationPlaygroundStandingTravel.Left));
            body.linearVelocity = Vector3.zero; Apply(Vector3.back);
            Assert.That(motor.LaboratoryStandingTravel, Is.EqualTo(AnimationPlaygroundStandingTravel.Right));
            var capped = (float)typeof(TopDown3DPlayerMotor).GetMethod("StandingStrafeRequestedSpeed", Private).Invoke(motor, new object[] { 6f });
            Assert.That(capped, Is.EqualTo(.5f));
            Apply(Vector3.left); Assert.That(motor.LaboratoryStandingDirectionUnsupported, Is.True);
            Assert.That(motor.TryBeginActionConstraint(owner), Is.True); Assert.That(motor.LaboratoryStandingFacingLocked, Is.False);
            motor.EndActionConstraint(owner); Apply(Vector3.forward); Assert.That(motor.LaboratoryStandingFacingLocked, Is.False, "Held LT cannot reacquire after an action.");
            f.PadState(new GamepadState()); Apply(Vector3.zero);
            f.PadState(new GamepadState { leftTrigger = 1 }); Apply(Vector3.forward);
            Assert.That(motor.LaboratoryStandingFacingLocked, Is.True);
            motor.Teleport(Vector3.zero); Assert.That(motor.LaboratoryStandingFacingLocked, Is.False);
            Assert.That(motor.ReleaseAnimationPostureOwner(owner), Is.True);
        }

        [Test, Category("PrivateAnimationQualification")]
        public void PrivateReviewRetainsCompleteUnequalContactLoopsAndBoundedStrideRates()
        {
            var p = Profile(); Assert.That(p.Validate(p.targetModel, p.footProjection, out var reason), Is.True, reason);
            foreach (var gait in new[] { p.left, p.right })
            {
                Assert.That(gait.sourceClip.length, Is.EqualTo(2 * 1.4666667f).Within(.00001));
                Assert.That(p.maximumSpeed * gait.sourceClip.length / gait.metresPerCycle, Is.InRange(.65, 1.35));
                Assert.That(gait.nativeContacts[1] - gait.nativeContacts[0], Is.Not.EqualTo(.25f).Within(.01f));
            }
            var clone = Object.Instantiate(p);
            try { clone.maximumSpeed = 6; Assert.That(clone.Validate(p.targetModel, p.footProjection, out _), Is.False); }
            finally { Object.DestroyImmediate(clone); }
            var bindings = Object.Instantiate(AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(
                "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/PlayerBindings.asset"));
            try
            {
                bindings.standingStrafeReviewRequired = true; bindings.standingStrafeReview = null;
                Assert.That(bindings.ValidatePlayer(out reason), Is.False); Assert.That(reason, Does.Contain("standing strafe"));
            }
            finally { Object.DestroyImmediate(bindings); }
        }

        [TestCase(SkinWeights.OneBone)]
        [TestCase(SkinWeights.TwoBones)]
        [TestCase(SkinWeights.FourBones)]
        [Category("PrivateAnimationQualification")]
        public void ActualSingleGraphClearsEquippedBodyAndCapturesBothSides(SkinWeights quality)
        {
            using var f = new AnimationPlaygroundForwardReviewTests.Fixture(false, quality,
                "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/ExpandedForwardGaitReview.asset");
            f.Driver.ConfigureLaboratoryForwardReview(f.Policy, f.Station);
            f.Driver.ConfigureLaboratoryStandingStrafeReview(Profile()); f.Call("Awake"); f.Actor.SetActive(true);
            f.Driver.ConfigureCamp(TopDown3DCampSettings.Load()); f.Driver.SetCampPresentation(-1, 1, false, true);
            var graph = f.Get<PlayableGraph>("playableGraph"); var mesh = new Mesh();
            try
            {
                var minimum = float.PositiveInfinity; var maximum = float.NegativeInfinity;
                var cloak = false; var rigid = false;
                foreach (var side in new[] { AnimationPlaygroundStandingTravel.Left, AnimationPlaygroundStandingTravel.Right })
                foreach (var weight in new[] { .25f, .5f, .75f, 1f })
                for (var frame = 0; frame <= (weight == 1 ? 128 : 32); frame++)
                {
                    f.Call("PrepareForwardGraphUpdate");
                    typeof(TopDown3DPlayerAnimationDriver).GetField("standingStrafeSide", Private).SetValue(f.Driver, side);
                    typeof(TopDown3DPlayerAnimationDriver).GetMethod("PresentStandingStrafeFrame", Private).Invoke(f.Driver,
                        new object[] { weight == 1 ? .5f : weight * .3f, frame / (weight == 1 ? 128d : 32d), .15f });
                    f.Call("LateUpdate");
                    foreach (var skin in f.Driver.HumanoidAnimator.transform.parent.GetComponentsInChildren<SkinnedMeshRenderer>())
                    {
                        cloak |= skin.name == "Cloak cloth skin"; rigid |= skin.name == "Rigid backpack skin";
                        skin.BakeMesh(mesh, true);
                        foreach (var vertex in mesh.vertices)
                        {
                            var height = skin.transform.TransformPoint(vertex).y - (f.Player.position.y - .91f);
                            minimum = Mathf.Min(minimum, height); maximum = Mathf.Max(maximum, height);
                        }
                    }
                    Assert.That(graph.GetOutputCount(), Is.EqualTo(1));
                }
                Assert.That(cloak && rigid, Is.True); Assert.That(minimum, Is.GreaterThanOrEqualTo(-.001f));
                Assert.That(maximum, Is.LessThanOrEqualTo(1.821f));
                Assert.That(f.Driver.TryCapturePoseBlend(f, .15f), Is.True); Assert.That(f.Driver.EndPoseBlend(f), Is.True);
                f.Call("PrepareForwardGraphUpdate"); Assert.That(f.Get<UnityEngine.Animations.AnimationMixerPlayable>("mixer").GetInputWeight(25), Is.Zero);
            }
            finally { Object.DestroyImmediate(mesh); }
        }

        [Test, Category("PrivateAnimationQualification")]
        public void CompletedLateralDistanceUsesNativeContactsAndSupportOwnershipVetoesPresentation()
        {
            using var f = new AnimationPlaygroundForwardReviewTests.Fixture(false, SkinWeights.FourBones,
                "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/ExpandedForwardGaitReview.asset");
            var profile = Profile(); f.Driver.ConfigureLaboratoryForwardReview(f.Policy, f.Station);
            f.Driver.ConfigureLaboratoryStandingStrafeReview(profile); f.Call("Awake"); f.Actor.SetActive(true);
            typeof(TopDown3DPlayerMotor).GetField("standingStrafeLocked", Private).SetValue(f.Motor, true);
            typeof(TopDown3DPlayerMotor).GetProperty("LaboratoryStandingTravel").SetValue(f.Motor, AnimationPlaygroundStandingTravel.Left);
            void Advance(long tick, bool rebase = false)
            {
                var sample = new AnimationPlaygroundGroundedMotion(tick, 17, Time.fixedTimeAsDouble, .02,
                    Vector3.left * .01f, Vector3.up, true, rebase);
                typeof(TopDown3DPlayerMotor).GetProperty("LaboratoryGroundedMotion").SetValue(f.Motor, sample);
                typeof(TopDown3DPlayerAnimationDriver).GetMethod("AdvanceForwardMotion", Private).Invoke(f.Driver, new object[] { sample });
            }
            Advance(1, true); for (var tick = 2; tick <= 101; tick++) Advance(tick);
            var clock = f.Get<AnimationPlaygroundGaitClock>("standingStrafeClock");
            Assert.That(clock.Cycles, Is.EqualTo(1 / profile.left.metresPerCycle).Within(.00001));
            Assert.That(f.Driver.LaboratoryForwardSpeed, Is.Zero);
            var count = profile.left.nativeContacts.Count(p => p > 0 && p <= clock.Cycles);
            Assert.That(f.Get<int>("standingStrafePlantCount"), Is.EqualTo(count));
            Advance(101); Assert.That(f.Get<int>("standingStrafePlantCount"), Is.EqualTo(count), "Duplicate completed ticks cannot repeat native plants.");
            Assert.That(f.TryPresent(), Is.True); Assert.That(f.Driver.LaboratoryStandingStrafeActive, Is.True);
            f.Call("PrepareForwardGraphUpdate"); f.Support(1);
            Assert.That(f.TryPresent(), Is.False); Assert.That(f.Driver.LaboratoryStandingStrafeActive, Is.False);
            f.Support(0); f.Station.enabled = false; Assert.That(f.TryPresent(), Is.False);
            f.Station.enabled = true; f.Support(0);
            Advance(102, true); Assert.That(f.Get<int>("standingStrafePlantCount"), Is.Zero);
            var before = clock.Cycles;
            typeof(TopDown3DPlayerAnimationDriver).GetMethod("AdvanceForwardMotion", Private).Invoke(f.Driver, new object[] {
                new AnimationPlaygroundGroundedMotion(103, 17, Time.fixedTimeAsDouble, .02, Vector3.right * .01f, Vector3.up, true, false) });
            Assert.That(clock.Cycles, Is.EqualTo(before)); Assert.That(f.Get<bool>("standingStrafeMotionUnavailable"), Is.True);
            typeof(TopDown3DPlayerAnimationDriver).GetMethod("AdvanceForwardMotion", Private).Invoke(f.Driver, new object[] {
                new AnimationPlaygroundGroundedMotion(104, 17, Time.fixedTimeAsDouble, .02, Vector3.left * .02f, Vector3.up, true, false) });
            Assert.That(clock.Cycles, Is.EqualTo(before), "External pushes cannot overcrank the native lateral source.");
            Assert.That(f.Get<bool>("standingStrafeMotionUnavailable"), Is.True);
            Assert.That(f.Motor.TryBeginActionConstraint(f), Is.True); Assert.That(f.Driver.StandingStrafeReviewSupportAvailable(), Is.False);
            f.Motor.EndActionConstraint(f);
        }

        private sealed class InputFixture : IDisposable
        {
            private readonly InputSettings previous = InputSystem.settings;
            private readonly InputSettings settings;
            public readonly GameObject Actor;
            public readonly Gamepad Pad;
            public readonly InputActionAsset Actions;
            public readonly TopDown3DInputRouter Router;
            public InputFixture()
            {
                settings = Object.Instantiate(previous); settings.SetInternalFeatureFlag("RUN_PLAYER_UPDATES_IN_EDIT_MODE", true); InputSystem.settings = settings;
                Pad = InputSystem.AddDevice<Gamepad>();
                Actions = AnimationPlaygroundInputFactory.Create(AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Project/Settings/Input/InputSystem_Actions.inputactions"));
                Actor = new GameObject("Standing strafe contract fixture"); Actor.SetActive(false);
                Actor.AddComponent<Rigidbody>(); Actor.AddComponent<CapsuleCollider>();
                Router = Actor.AddComponent<TopDown3DInputRouter>(); Router.Configure(Actions); Assert.That(Router.Initialize(), Is.True);
                PadState(new GamepadState());
            }
            public void PadState(GamepadState state) { InputSystem.QueueStateEvent(Pad, state); InputSystem.Update(); }
            public void Dispose()
            {
                Object.DestroyImmediate(Actor); Object.DestroyImmediate(Actions);
                if (Pad.added) InputSystem.RemoveDevice(Pad);
                InputSystem.settings = previous; Object.DestroyImmediate(settings);
            }
        }
    }
}
