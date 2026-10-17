using System;
using System.Collections.Generic;
using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class AnimationPlaygroundDodgeTransitionTests
    {
        static IEnumerable<TestCaseData> Directions()
        {
            foreach (var speed in new[] { 4.9f, 6.15f })
            for (var heading = 0; heading < 360; heading += 45)
                yield return new TestCaseData(speed, (float)heading).SetName($"DodgeRecovery_{speed:R}_{heading}");
        }

        [TestCaseSource(nameof(Directions))]
        public void RecoveryKeepsHeldLTFacingAndTheSnapshotWorldHeading(float speed, float heading)
        {
            using var f = new Fixture(speed, heading, true);
            Assert.That(f.Dodge.TryDodge(), Is.True, f.Dodge.LastFailure);
            var releaseSeen = false; var recoveryFrames = 0;
            for (var tick = 0; tick < 65; tick++)
            {
                var wasRolling = f.Motor.DodgeLocomotionActive;
                var beforeYaw = f.VisualForward;
                f.Step();
                Assert.That(f.Motor.LaboratoryStandingFacingLocked, Is.True, $"LT lease lost at tick {tick + 1}");
                Assert.That(Vector3.Angle(f.Body.rotation * Vector3.forward, Vector3.forward), Is.LessThan(.01f));
                if (wasRolling && !f.Motor.DodgeLocomotionActive)
                {
                    releaseSeen = true;
                    Assert.That(Vector3.Angle(beforeYaw, f.VisualForward), Is.LessThan(1), "Release must preserve snapshot world yaw.");
                }
                if (!f.Motor.DodgeLocomotionActive && f.Driver.PoseBlendOwned)
                {
                    recoveryFrames++;
                    Assert.That(f.Driver.StandingStrafeReviewSupportAvailable(), Is.True);
                    var local = f.Get<Vector2>("directionalLocal");
                    var expected = AnimationPlaygroundDirectionalMath.Local(f.Body.linearVelocity, f.Motor.FacingDirection).normalized;
                    Assert.That(Vector2.Angle(local, expected), Is.LessThan(.1f), "The recovery underlay must keep the actual slope-tangent movement direction.");
                }
                Assert.That(f.Get<PlayableGraph>("playableGraph").GetOutputCount(), Is.EqualTo(1));
            }
            Assert.That(releaseSeen, Is.True); Assert.That(recoveryFrames, Is.GreaterThan(1));
            Assert.That(f.Dodge.Active || f.Actions.IsBusy || f.Driver.PoseBlendOwned, Is.False);
            Assert.That(f.Input.Router.CameraLookAheadHeld, Is.True); Assert.That(f.Body.useGravity, Is.True);
        }

        [TestCase(4.9f)] [TestCase(6.15f)]
        public void MovingEntryCannotBrakeAtTheStandingRollCurveRate(float speed)
        {
            using var f = new Fixture(speed, 0, false);
            var start = Vector3.ProjectOnPlane(f.Body.linearVelocity, f.Normal).magnitude;
            var previous = start; var origin = f.Body.position;
            Assert.That(f.Dodge.TryDodge(), Is.True);
            for (var tick = 1; tick <= 15; tick++)
            {
                f.Step(); var current = Vector3.ProjectOnPlane(f.Body.linearVelocity, f.Normal).magnitude;
                Assert.That(previous - current, Is.LessThanOrEqualTo(8 * Time.fixedDeltaTime + .002f), $"Braking spike at tick {tick}");
                if (tick == 4) Assert.That(current, Is.GreaterThanOrEqualTo(start - .642f));
                Assert.That(current, Is.LessThanOrEqualTo(AnimationPlaygroundDodgeTests.Profile().maximumSpeed + .001f));
                previous = current;
            }
            Assert.That(f.Body.position, Is.EqualTo(origin), "This command contract must not simulate a trajectory.");
        }

        [Test]
        public void GroundedReleaseRetainsTheRayForTheFirstRecoveryObservation()
        {
            using var f = new Fixture(6.15f, 90, true);
            Assert.That(f.Dodge.TryDodge(), Is.True);
            for (var tick = 0; tick < 48; tick++) f.Step(false);
            Assert.That(f.Motor.DodgeLocomotionActive, Is.False); Assert.That(f.Driver.PoseBlendOwned, Is.True);
            // EditMode's fixed clock did not move. Publish one distinct completion
            // time, using the retained real support ray, without replacing support.
            typeof(TopDown3DPlayerMotor).GetMethod("ObserveCompletedLaboratoryMotionAt", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(f.Motor, new object[] { Time.fixedTimeAsDouble + 1 });
            Assert.That(f.Motor.TryGetLaboratorySupportPlane(out _, true), Is.True);
            Assert.That(f.Driver.StandingStrafeReviewSupportAvailable(), Is.True);
            f.Step(false); Assert.That(f.Motor.LaboratoryStandingFacingLocked, Is.True);
        }

        [TestCase(90f)] [TestCase(180f)]
        public void RecoverySnapshotKeepsTheEquippedJointsAtTheSameSourceTime(float heading)
        {
            using var f = new Fixture(6.15f, heading, true);
            Assert.That(f.Dodge.TryDodge(), Is.True);
            for (var tick = 0; tick < 47; tick++) f.Step();
            f.Driver.SetAnimationPresentationTime(f.Actions, AnimationPlaygroundDodgeTests.Profile().roll.length);
            f.Forward.Call("PrepareForwardGraphUpdate"); f.Forward.Call("PresentAnimationAction"); f.Forward.Call("LateUpdate");
            var animator = f.Get<Animator>("humanoidAnimator");
            var names = new[] { HumanBodyBones.Hips, HumanBodyBones.Chest, HumanBodyBones.LeftFoot,
                HumanBodyBones.RightFoot, HumanBodyBones.LeftHand, HumanBodyBones.RightHand };
            var before = new Vector3[names.Length];
            for (var i = 0; i < names.Length; i++) before[i] = animator.GetBoneTransform(names[i]).position;
            typeof(AnimationPlaygroundDodgeController).GetMethod("Finish", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(f.Dodge, null);
            f.Forward.Call("PrepareForwardGraphUpdate");
            typeof(TopDown3DPlayerAnimationDriver).GetMethod("PresentDodgeRunOutBlendAt", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(f.Driver, new object[] { 0f, 0f });
            f.Forward.Call("LateUpdate");
            for (var i = 0; i < names.Length; i++)
                Assert.That(Vector3.Distance(before[i], animator.GetBoneTransform(names[i]).position), Is.LessThanOrEqualTo(.002f), names[i].ToString());
        }

        [TestCase(false, 90f)] [TestCase(false, 180f)] [TestCase(true, 180f)]
        public void ReleasedOrAbsentLTDoesNotRotateTheSnapshotOnTheReleaseTick(bool releaseLT, float heading)
        {
            using var f = new Fixture(6.15f, heading, releaseLT);
            Assert.That(f.Dodge.TryDodge(), Is.True);
            for (var tick = 0; tick < 47; tick++) f.Step();
            if (releaseLT) f.State(6.15f, false);
            var before = f.VisualForward; f.Step();
            Assert.That(f.Motor.DodgeLocomotionActive, Is.False);
            Assert.That(Vector3.Angle(before, f.VisualForward), Is.LessThan(1));
            for (var tick = 0; tick < 17; tick++) f.Step();
            Assert.That(f.Motor.LaboratoryStandingFacingLocked, Is.False);
            Assert.That(f.Dodge.Active || f.Driver.PoseBlendOwned || f.Actions.IsBusy, Is.False);
        }

        [TestCase(false)] [TestCase(true)]
        public void PausedRollAndRecoveryKeepTheirTimeAndResumeToTheCurrentMovement(bool recovery)
        {
            using var f = new Fixture(6.15f, 90, true);
            Assert.That(f.Dodge.TryDodge(), Is.True);
            for (var tick = 0; tick < (recovery ? 50 : 4); tick++) f.Step();
            var stage = f.Actions.PhysicalStageSeconds; var elapsed = f.Get<double>("poseBlendElapsed");
            var heading = f.VisualForward; f.Paused = true;
            for (var tick = 0; tick < 5; tick++) f.Step();
            Assert.That(f.Actions.PhysicalStageSeconds, Is.EqualTo(stage));
            Assert.That(f.Get<double>("poseBlendElapsed"), Is.EqualTo(elapsed));
            Assert.That(Vector3.Angle(heading, f.VisualForward), Is.LessThan(.01f));
            f.State(4.9f, true, Vector2.up); f.Paused = false;
            for (var tick = 0; tick < 65; tick++) f.Step();
            Assert.That(f.Dodge.Active || f.Actions.IsBusy || f.Driver.PoseBlendOwned, Is.False);
            Assert.That(f.Motor.LaboratoryStandingFacingLocked, Is.True);
            Assert.That(Vector3.ProjectOnPlane(f.Body.linearVelocity, f.Normal).magnitude, Is.EqualTo(4.9f).Within(.02f));
            Assert.That(f.Dodge.TryDodge(), Is.True); f.Dodge.Cancel();
            Assert.That(f.Actions.IsBusy || f.Driver.PoseBlendOwned || f.Motor.IsActionConstrained, Is.False);
            Assert.That(f.Body.useGravity, Is.True);
        }

        [TestCase(0f, 0f, 0f)] [TestCase(0f, 1f, 4.9f)] [TestCase(1f, 0f, 4.9f)] [TestCase(0f, -1f, 4f)]
        public void LateInputUsesTheAcceptedOrdinaryAndDirectionalSpeedLimits(float x, float y, float expected)
        {
            using var f = new Fixture(6.15f, 0, true);
            Assert.That(f.Dodge.TryDodge(), Is.True);
            for (var tick = 0; tick < 34; tick++) f.Step();
            f.State(4.9f, true, new Vector2(x, y));
            for (var tick = 0; tick < 31; tick++) f.Step();
            // Neutral ordinary locomotion retains a downward ground-snap velocity.
            // With physics deliberately unstepped, only its horizontal command is zero.
            var velocity = Vector3.ProjectOnPlane(f.Body.linearVelocity, expected == 0 ? Vector3.up : f.Normal);
            Assert.That(velocity.magnitude, Is.EqualTo(expected).Within(.02f));
            Assert.That(f.Dodge.Active || f.Actions.IsBusy || f.Driver.PoseBlendOwned, Is.False);
            Assert.That(f.Motor.LaboratoryStandingFacingLocked, Is.True);
        }

        [Test]
        public void DodgeRunOutUsesTheSameFortyDegreeUphillLimitAsOrdinaryMovement()
        {
            using var f = new Fixture(6.15f, 0, false);
            Assert.That(f.Dodge.TryDodge(), Is.True);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SceneManager.MoveGameObjectToScene(floor, f.Forward.Scene);
            try
            {
                floor.name = "Static forty-degree policy query";
                floor.transform.SetPositionAndRotation(f.Body.position + Vector3.up * 10, Quaternion.Euler(-40, 0, 0));
                floor.transform.localScale = new Vector3(20, .1f, 20); floor.AddComponent<TopDown3DGroundSurface>();
                var normal = floor.transform.up; var top = floor.transform.position + normal * .05f;
                var position = top + Vector3.up * (.91f + .2f * (1 / normal.y - 1));
                f.Forward.Actor.transform.position = position; f.Body.position = position;
                Physics.SyncTransforms();
                var uphill = f.Motor.DodgeRunOutVelocity(f.Actions);
                Assert.That(uphill.magnitude, Is.EqualTo(1.4f).Within(.001f));
                Assert.That(Vector3.Dot(uphill.normalized, normal), Is.EqualTo(0).Within(.001f));
                f.State(6.15f, false, Vector2.down);
                Assert.That(f.Motor.DodgeRunOutVelocity(f.Actions).magnitude, Is.EqualTo(6.15f).Within(.001f));
            }
            finally { Object.DestroyImmediate(floor); }
        }

        [Test]
        public void RecoveryDoesNotBypassDisabledOrMovedNativeSupport()
        {
            using var f = new Fixture(6.15f, 90, true);
            Assert.That(f.Dodge.TryDodge(), Is.True);
            for (var tick = 0; tick < 49; tick++) f.Step();
            var support = f.Motor.LaboratorySupportCollider; Assert.That(support, Is.Not.Null);
            var position = support.transform.position;
            try
            {
                support.enabled = false;
                Assert.That(f.Driver.StandingStrafeReviewSupportAvailable(), Is.False);
                support.enabled = true; support.transform.position += Vector3.up;
                Assert.That(f.Driver.StandingStrafeReviewSupportAvailable(), Is.False);
            }
            finally { support.enabled = true; support.transform.position = position; Physics.SyncTransforms(); }
        }

        [TestCase(false)] [TestCase(true)]
        public void RecoveryInterpolatesTheExistingCompletedMotionClockWithoutAdvancingIt(bool locked)
        {
            using var f = new Fixture(6.15f, locked ? 90 : 0, locked);
            Assert.That(f.Dodge.TryDodge(), Is.True);
            for (var tick = 0; tick < 48; tick++) f.Step();
            var direction = locked ? Vector3.right : Vector3.forward;
            for (var tick = 0; tick < 4; tick++) f.Complete(2000 + tick, 4.9f, direction, tick == 0);
            var clock = f.Get<AnimationPlaygroundGaitClock>("forwardClock");
            var cycles = clock.Cycles; var contacts = clock.ContactCount;
            Assert.That(clock.Travelling, Is.True);
            var profile = f.Forward.Session.Bindings.forwardReviewOverride;
            var at = typeof(TopDown3DPlayerAnimationDriver).GetMethod("PresentDodgeRunOutBlendAt", BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (var alpha in new[] { 0f, .5f, 1f })
            {
                f.Driver.SetPoseBlendElapsed(f.Dodge, .08);
                f.Forward.Call("PrepareForwardGraphUpdate");
                Assert.That((bool)at.Invoke(f.Driver, new object[] { alpha, 1f / 60 }), Is.True);
                f.Forward.Call("LateUpdate");
                var expected = locked ? clock.RenderCycles(alpha) : clock.RenderCycles(alpha, 4.9f, profile);
                Assert.That(f.Driver.LaboratoryRenderedCycles, Is.EqualTo(expected).Within(.0000001));
                Assert.That(clock.Cycles, Is.EqualTo(cycles)); Assert.That(clock.ContactCount, Is.EqualTo(contacts));
            }
            f.Driver.EndPoseBlend(f.Dodge); f.Forward.Call("PrepareForwardGraphUpdate");
            Assert.That((bool)typeof(TopDown3DPlayerAnimationDriver).GetMethod(locked ? "TryPresentDirectional" : "TryPresentForwardLocomotion",
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(f.Driver, null), Is.True);
            Assert.That(clock.Cycles, Is.EqualTo(cycles));
        }

        internal sealed class Fixture : IDisposable
        {
            const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
            public readonly AnimationPlaygroundNativeGroundTests.NativeFixture Native;
            public readonly AnimationPlaygroundStandingStrafeTests.InputFixture Input;
            public AnimationPlaygroundForwardReviewTests.Fixture Forward => Native.Inner.Forward;
            public TopDown3DPlayerMotor Motor => Native.Inner.Motor;
            public TopDown3DPlayerAnimationDriver Driver => Native.Inner.Driver;
            public Rigidbody Body => Forward.Player;
            public readonly AnimationPlaygroundActionCoordinator Actions;
            public readonly AnimationPlaygroundDodgeController Dodge;
            public readonly Vector2 Stick;
            public readonly Vector3 Normal;
            readonly Plane groundPlane;
            readonly Collider support;
            public bool Paused;
            readonly GameObject basis;
            int tick;
            public Vector3 VisualForward => Vector3.ProjectOnPlane(Get<GameObject>("visualInstance").transform.forward, Vector3.up).normalized;
            public T Get<T>(string field) => (T)typeof(TopDown3DPlayerAnimationDriver).GetField(field, Private).GetValue(Driver);
            public Fixture(float speed, float heading, bool locked, SkinWeights quality = SkinWeights.FourBones)
            {
                Input = new AnimationPlaygroundStandingStrafeTests.InputFixture();
                Native = new AnimationPlaygroundNativeGroundTests.NativeFixture(quality);
                Native.Place(AnimationPlaygroundNativeGroundTests.Sites[0], 0);
                var b = Forward.Session.Bindings; var registry = new AnimationPlaygroundResetRegistry();
                Actions = Forward.Actor.AddComponent<AnimationPlaygroundActionCoordinator>();
                Actions.Configure(Motor, Driver, registry, new[] { Body.position }, () => Paused);
                Motor.ConfigureLaboratorySpeeds(Actions, b.slowWalkSpeed, b.runSpeed, b.sprintSpeed);
                Motor.ConfigureLaboratoryGamepadGaits(Actions, b.forwardReviewOverride.gaits[3].nominalSpeed, b.forwardReviewOverride.gaits[4].nominalSpeed);
                Motor.BlockUnqualifiedLaboratoryTraversal(Actions);
                Motor.ConfigureLaboratoryStandingStrafe(Actions, b.standingStrafeReview, Driver.StandingStrafeReviewSupportAvailable);
                Native.Place(AnimationPlaygroundNativeGroundTests.Sites[0], 0);
                Assert.That(Motor.TryGetLaboratorySupportPlane(out var plane, true), Is.True); Normal = plane.normal; groundPlane = plane;
                support = Motor.LaboratorySupportCollider;
                basis = new GameObject("Offline dodge transition basis"); SceneManager.MoveGameObjectToScene(basis, Forward.Scene);
                typeof(TopDown3DPlayerMotor).GetField("input", Private).SetValue(Motor, Input.Router);
                typeof(TopDown3DPlayerMotor).GetField("cameraBasis", Private).SetValue(Motor, basis.transform);
                Motor.FacePlanarDirection(Vector3.forward);
                typeof(TopDown3DPlayerMotor).GetMethod("ApplyStandingStrafeIntent", Private).Invoke(Motor, new object[] { Vector3.zero });
                Stick = new Vector2(Mathf.Sin(heading * Mathf.Deg2Rad), Mathf.Cos(heading * Mathf.Deg2Rad));
                State(speed, locked);
                typeof(TopDown3DPlayerMotor).GetMethod("ApplyStandingStrafeIntent", Private).Invoke(Motor, new object[] { new Vector3(Stick.x, 0, Stick.y) });
                Native.Inner.Frame(speed, .25, Vector2.up, 1);
                Body.linearVelocity = TopDown3DSlopeMath.ProjectDirectionOnSlope(Vector3.forward, Normal, 45) * speed;
                Dodge = Forward.Actor.AddComponent<AnimationPlaygroundDodgeController>();
                Dodge.Configure(Motor, Driver, Actions, b.dodge, registry, () => Paused, () => false);
                Assert.That(Driver.StandingStrafeReviewSupportAvailable(), Is.True);
                Assert.That(Motor.LaboratoryStandingFacingLocked, Is.EqualTo(locked));
            }
            public void State(float speed, bool locked, Vector2? direction = null)
            {
                var state = new GamepadState { leftStick = direction ?? Stick, leftTrigger = locked ? 1 : 0 };
                if (speed > 5) state = state.WithButton(GamepadButton.RightShoulder);
                Input.PadState(state);
            }
            public void Step(bool observations = true)
            {
                typeof(TopDown3DPlayerMotor).GetMethod("FixedUpdate", Private).Invoke(Motor, null); tick++;
                if (observations && !Motor.DodgeLocomotionActive)
                {
                    // Explicit completed-motion unit observations. The actor never
                    // moves and no physics step or Play Mode is run.
                    Native.Inner.SetSupport(groundPlane, support);
                    var sample = new AnimationPlaygroundGroundedMotion(1000 + tick, 314, Time.fixedTimeAsDouble,
                        Time.fixedDeltaTime, Body.linearVelocity * Time.fixedDeltaTime, Normal, true, tick == 48);
                    typeof(TopDown3DPlayerMotor).GetProperty("LaboratoryGroundedMotion").SetValue(Motor, sample);
                    typeof(TopDown3DPlayerAnimationDriver).GetMethod("AdvanceForwardMotion", Private).Invoke(Driver, new object[] { sample });
                }
                Forward.Call("Update"); Forward.Call("LateUpdate");
            }
            public void Complete(long completedTick, float speed, Vector3 direction, bool rebase)
            {
                Native.Inner.SetSupport(groundPlane, support);
                var displacement = TopDown3DSlopeMath.ProjectDirectionOnSlope(direction, Normal, 45) * speed * Time.fixedDeltaTime;
                var sample = new AnimationPlaygroundGroundedMotion(completedTick, 315, Time.fixedTimeAsDouble,
                    Time.fixedDeltaTime, displacement, Normal, true, rebase);
                typeof(TopDown3DPlayerMotor).GetProperty("LaboratoryGroundedMotion").SetValue(Motor, sample);
                typeof(TopDown3DPlayerAnimationDriver).GetMethod("AdvanceForwardMotion", Private).Invoke(Driver, new object[] { sample });
            }
            public void Dispose()
            {
                Dodge.DisposeOwnedServices(); Actions.DisposeOwnedServices();
                Object.DestroyImmediate(basis); Native.Dispose(); Input.Dispose();
            }
        }
    }
}
