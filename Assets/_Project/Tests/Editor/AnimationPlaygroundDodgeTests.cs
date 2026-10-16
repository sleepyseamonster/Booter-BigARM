using System;
using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class AnimationPlaygroundDodgeTests
    {
        internal const string ProfilePath = "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/DodgeProfile.asset";
        internal static AnimationPlaygroundDodgeProfile Profile()
        {
            var p = AssetDatabase.LoadAssetAtPath<AnimationPlaygroundDodgeProfile>(ProfilePath);
            Assert.That(p, Is.Not.Null, "Restore the qualified private dodge bundle."); return p;
        }
        [Test] public void CompleteGroundedSourceHasBoundedDisplacementAndNoSecondRootWriter()
        {
            var p = Profile(); Assert.That(p.Validate(p.targetModel, out var reason), Is.True, reason);
            Assert.That(p.roll.length, Is.EqualTo(.95f).Within(.00001)); Assert.That(p.roll.isLooping, Is.False);
            foreach (var b in AnimationUtility.GetCurveBindings(p.roll))
                if (b.propertyName == "RootT.x" || b.propertyName == "RootT.z")
                    foreach (var key in AnimationUtility.GetEditorCurve(p.roll, b).keys) Assert.That(key.value, Is.Zero);
            var distance = 0f;
            for (var i = 0; i < 200; i++)
            {
                var speed = p.Speed(p.roll.length * i / 200, p.roll.length * (i + 1) / 200);
                Assert.That(speed, Is.InRange(0, p.maximumSpeed)); distance += speed * p.roll.length / 200;
            }
            Assert.That(distance, Is.EqualTo(p.distance).Within(.0001));
            var copy = Object.Instantiate(p);
            try
            {
                copy.distance = float.NaN; Assert.That(copy.Validate(p.targetModel, out _), Is.False);
                copy.distance = p.distance; copy.qualified = false; Assert.That(copy.Validate(p.targetModel, out _), Is.False);
                copy.qualified = true; copy.travel = AnimationCurve.Linear(0, 1, 1, 0); Assert.That(copy.Validate(p.targetModel, out _), Is.False);
            }
            finally { Object.DestroyImmediate(copy); }
        }
        [Test] public void MissingRequiredDodgeCannotSilentlyDisableTheRequestedControl()
        {
            var b = Object.Instantiate(AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(
                "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/PlayerBindings.asset"));
            try
            {
                b.dodgeRequired = true; b.dodge = null; Assert.That(b.ValidatePlayer(out var reason), Is.False); Assert.That(reason, Does.Contain("dodge"));
                b.dodgeRequired = false; Assert.That(b.ValidatePlayer(out reason), Is.True, reason);
            }
            finally { Object.DestroyImmediate(b); }
        }
        [Test] public void RollAcquiresOncePausesAndBlendsBackWithoutLeakingOwners()
        {
            using var f = new Fixture(); var before = f.Body.position; f.Body.linearVelocity = Vector3.forward * 4.9f;
            Assert.That(f.Dodge.TryDodge(), Is.True, f.Dodge.LastFailure); Assert.That(f.Dodge.TryDodge(), Is.False);
            Assert.That(f.Body.linearVelocity.z, Is.EqualTo(4.9f)); f.Tick(1);
            Assert.That(f.Body.linearVelocity.z, Is.GreaterThan(3.5f), "Entry blending must not brake a run to zero."); f.Tick(3);
            f.Paused = true; var time = f.Actions.PhysicalStageSeconds; f.Tick(10);
            Assert.That(f.Actions.PhysicalStageSeconds, Is.EqualTo(time)); Assert.That(f.Body.linearVelocity, Is.EqualTo(Vector3.zero));
            f.Paused = false; f.Tick(70);
            Assert.That(f.Dodge.Active, Is.False); Assert.That(f.Actions.IsBusy, Is.False); Assert.That(f.Driver.PoseBlendOwned, Is.False);
            Assert.That(f.Motor.IsActionConstrained, Is.False); Assert.That(f.Body.position, Is.EqualTo(before));
            Assert.That(f.Body.useGravity, Is.True); Assert.That(f.Dodge.TryDodge(), Is.True);
        }
        [Test] public void BusyAirborneResetAndDisposalCannotLeaveARollLease()
        {
            using var f = new Fixture(); f.Busy = true; Assert.That(f.Dodge.TryDodge(), Is.False); f.Busy = false;
            typeof(TopDown3DPlayerMotor).GetProperty("IsGrounded").SetValue(f.Motor, false); Assert.That(f.Dodge.TryDodge(), Is.False);
            typeof(TopDown3DPlayerMotor).GetProperty("IsGrounded").SetValue(f.Motor, true); Assert.That(f.Dodge.TryDodge(), Is.True);
            f.Tick(10); f.Dodge.InvalidateForReset(2); Assert.That(f.Actions.IsBusy, Is.False); Assert.That(f.Motor.IsActionConstrained, Is.False);
            Assert.That(f.Dodge.TryRestoreForReset(out _), Is.True); Assert.That(f.Dodge.TryDodge(), Is.True);
            f.Dodge.DisposeOwnedServices(); Assert.That(f.Driver.PoseBlendOwned, Is.False); Assert.That(f.Motor.IsActionConstrained, Is.False);
        }
        [Test] public void HeldMovementAndLTFacingSurviveTheRollAndItsRunOut()
        {
            using var input = new AnimationPlaygroundStandingStrafeTests.InputFixture(); using var f = new Fixture();
            var basis = new GameObject("Dodge fixture camera basis");
            try
            {
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(TopDown3DPlayerMotor).GetField("input", flags).SetValue(f.Motor, input.Router);
                typeof(TopDown3DPlayerMotor).GetField("cameraBasis", flags).SetValue(f.Motor, basis.transform);
                f.Motor.ConfigureLaboratorySpeeds(f.Actions, 1.4f, 3, 4.9f); f.Motor.ConfigureLaboratoryGamepadGaits(f.Actions, 4.9f, 6.15f);
                f.Motor.ConfigureLaboratoryStandingStrafe(f.Actions, AnimationPlaygroundDirectionalTests.Profile(), () => true);
                f.Body.rotation = Quaternion.Euler(0, 37, 0); input.PadState(new GamepadState());
                var args = new object[] { Vector3.zero }; typeof(TopDown3DPlayerMotor).GetMethod("ApplyStandingStrafeIntent", flags).Invoke(f.Motor, args);
                input.PadState(new GamepadState { leftTrigger = 1, leftStick = Vector2.right });
                args[0] = Vector3.right; typeof(TopDown3DPlayerMotor).GetMethod("ApplyStandingStrafeIntent", flags).Invoke(f.Motor, args);
                Assert.That(f.Motor.LaboratoryStandingFacingLocked, Is.True); Assert.That(f.Dodge.TryDodge(), Is.True, f.Dodge.LastFailure);
                f.Tick(48);
                Assert.That(input.Router.CameraLookAheadHeld, Is.True); Assert.That(f.Motor.LaboratoryStandingFacingLocked, Is.True);
                Assert.That(Vector3.Angle(f.Body.rotation * Vector3.forward, Quaternion.Euler(0, 37, 0) * Vector3.forward), Is.LessThan(.01f));
                Assert.That(Vector3.ProjectOnPlane(f.Body.linearVelocity, Vector3.up).magnitude, Is.GreaterThan(4));
            }
            finally { Object.DestroyImmediate(basis); }
        }

        [Test] public void DeathAndDisableReleaseBothMotionAndPresentation()
        {
            using var f = new Fixture();
            var vitals = f.Motor.GetComponent<TopDown3DSurvivalVitals>();
            Assert.That(vitals, Is.Not.Null);
            var health = typeof(TopDown3DSurvivalVitals).GetField("health", BindingFlags.Instance | BindingFlags.NonPublic);
            health.SetValue(vitals, 0f); Assert.That(f.Dodge.TryDodge(), Is.False);
            health.SetValue(vitals, 100f); Assert.That(f.Dodge.TryDodge(), Is.True);
            health.SetValue(vitals, 0f); f.Tick(1);
            Assert.That(f.Dodge.Active, Is.False); Assert.That(f.Actions.IsBusy, Is.False);
            Assert.That(f.Driver.PoseBlendOwned, Is.False); Assert.That(f.Motor.IsActionConstrained, Is.False);
            health.SetValue(vitals, 100f); Assert.That(f.Dodge.TryRestoreForReset(out _), Is.True);
            Assert.That(f.Dodge.TryDodge(), Is.True); f.Dodge.enabled = false;
            typeof(AnimationPlaygroundDodgeController).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(f.Dodge, null);
            Assert.That(f.Dodge.Active, Is.False); Assert.That(f.Actions.IsBusy, Is.False);
            Assert.That(f.Driver.PoseBlendOwned, Is.False); Assert.That(f.Motor.IsActionConstrained, Is.False);
        }

        internal sealed class Fixture : IDisposable
        {
            public readonly AnimationPlaygroundForwardReviewTests.Fixture Forward;
            readonly AnimationPlaygroundDirectionalTests.Fixture directional;
            public TopDown3DPlayerMotor Motor => Forward.Motor;
            public TopDown3DPlayerAnimationDriver Driver => Forward.Driver;
            public Rigidbody Body => Forward.Player;
            public readonly AnimationPlaygroundActionCoordinator Actions;
            public readonly AnimationPlaygroundDodgeController Dodge;
            readonly GameObject floor;
            public bool Paused, Busy;
            public Fixture()
            {
                directional = new AnimationPlaygroundDirectionalTests.Fixture(SkinWeights.FourBones);
                Forward = directional.Forward;
                Forward.Frame(4.9f, .2);
                floor = GameObject.CreatePrimitive(PrimitiveType.Cube); SceneManager.MoveGameObjectToScene(floor, Forward.Scene);
                floor.transform.position = Body.position - Vector3.up * 1.01f; floor.transform.localScale = new Vector3(20, .2f, 20);
                floor.AddComponent<TopDown3DGroundSurface>(); Physics.SyncTransforms();
                typeof(TopDown3DPlayerMotor).GetProperty("IsGrounded").SetValue(Motor, true);
                var registry = new AnimationPlaygroundResetRegistry(); Actions = Forward.Actor.AddComponent<AnimationPlaygroundActionCoordinator>();
                Actions.Configure(Motor, Driver, registry, new[] { Body.position }, () => Paused);
                Dodge = Forward.Actor.AddComponent<AnimationPlaygroundDodgeController>();
                Dodge.Configure(Motor, Driver, Actions, Profile(), registry, () => Paused, () => Busy);
            }
            public void Tick(int count)
            {
                for (var i = 0; i < count; i++) typeof(TopDown3DPlayerMotor).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(Motor, null);
            }
            public void Dispose() { Dodge.DisposeOwnedServices(); Actions.DisposeOwnedServices(); directional.Dispose(); }
        }
    }
}
