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
    public sealed class AnimationPlaygroundExpandedGaitTests
    {
        private const string Path = "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/ExpandedForwardGaitReview.asset";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static AnimationPlaygroundForwardLocomotionProfile Policy()
        {
            var result = AssetDatabase.LoadAssetAtPath<AnimationPlaygroundForwardLocomotionProfile>(Path);
            if (result == null) Assert.Ignore("Restore the private expanded review.");
            return result;
        }

        [Test, Category("PrivateAnimationQualification")]
        public void CompleteSprintIsSeparateAndAcceptedFastRunIsRetained()
        {
            var p = Policy();
            Assert.That(p.Validate(p.targetModel, p.locomotion, out var reason, false), Is.True, reason);
            Assert.That(p.Validate(p.targetModel, p.locomotion, out _), Is.False);
            Assert.That(p.qualified || p.terrainQualified, Is.False);
            CollectionAssert.AreEqual(new[] { 1.4f, 3f, 4f, 5f, 6.15f }, p.gaits.Select(g => g.nominalSpeed));
            Assert.That(AssetDatabase.GetAssetPath(p.gaits[3].sourceClip), Does.EndWith("SprintArmDriveReview.anim"));
            Assert.That(p.gaits[4].sourceClip, Is.Not.SameAs(p.gaits[3].sourceClip));
            var source = AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Art/Characters/Prototype/UnityStandardHumanoid/Animations/m@SprintForwards.fbx")
                .OfType<AnimationClip>().Single(c => !c.name.StartsWith("__preview__"));
            Assert.That(p.gaits[4].sourceClip.length, Is.EqualTo(source.length * 2).Within(.00001f));
            Assert.That(p.gaits[4].sourceClip.isHumanMotion && p.gaits[4].sourceClip.isLooping, Is.True);
        }

        [Test, Category("PrivateAnimationQualification")]
        public void AnalogGaitsAreContinuousAndKeepTheMeasuredDistanceClock()
        {
            var p = Policy(); var previous = p.PoseWeights(0);
            for (var step = 0; step <= Mathf.CeilToInt(p.gaits[4].nominalSpeed * 100); step++)
            {
                var speed = step / 100f; var weights = p.PoseWeights(speed); var sum = 0f;
                for (var channel = 0; channel < 6; channel++)
                {
                    Assert.That(weights[channel], Is.InRange(0, 1)); sum += weights[channel];
                    Assert.That(Mathf.Abs(weights[channel] - previous[channel]), Is.LessThan(.02f));
                }
                Assert.That(sum, Is.EqualTo(1).Within(.00001f)); previous = weights;
                for (var gait = 0; gait < 5; gait++)
                {
                    if (weights[gait + 1] < .1f || speed == 0) continue;
                    var source = p.gaits[gait];
                    for (var k = 1; k < source.phaseMap.Length; k++)
                    {
                        var a = source.phaseMap[k - 1]; var b = source.phaseMap[k];
                        var rate = (b.native - a.native) / (b.semantic - a.semantic) * source.sourceClip.length
                            * speed / p.DistanceDerivative(speed, (a.semantic + b.semantic) * .5);
                        Assert.That(rate, Is.InRange(source.blendMinimumRate - .001, source.blendMaximumRate + .001));
                    }
                }
                var cycles = p.AdvanceCycles(speed, .98, .06);
                Assert.That(p.DistanceAtCycles(speed, cycles) - p.DistanceAtCycles(speed, .98), Is.EqualTo(.06).Within(.000001));
            }
            for (var i = 0; i < 5; i++) Assert.That(p.PoseWeights(p.gaits[i].nominalSpeed)[i + 1], Is.EqualTo(1));
            var clock = new AnimationPlaygroundGaitClock();
            clock.Advance(new AnimationPlaygroundGroundedMotion(1, 1, .02, .02, Vector3.zero, Vector3.up, true, true), p);
            for (var tick = 2; tick < 102; tick++)
                clock.Advance(new AnimationPlaygroundGroundedMotion(tick, 1, tick * .02, .02, Vector3.forward * (p.gaits[4].nominalSpeed * .02f), Vector3.up, true, false), p);
            Assert.That(clock.Cycles, Is.EqualTo(2 * p.gaits[4].nominalSpeed / p.gaits[4].metresPerCycle).Within(.00001));
            Assert.That(clock.ContactCount, Is.EqualTo((long)Math.Floor(clock.Cycles * 4)));
        }

        [Test, Category("PrivateAnimationQualification")]
        public void BindingOverridePreparesSixChannelsWithoutRewritingTheSceneAndFailsVisiblyIfMissing()
        {
            var policy = Policy(); using var f = new AnimationPlaygroundForwardReviewTests.Fixture(false);
            var bindings = Object.Instantiate(f.Session.Bindings);
            try
            {
                bindings.forwardReviewOverride = policy; bindings.forwardReviewOverrideRequired = true; f.Session.Configure(bindings);
                Assert.That(f.Session.ExperimentalForwardReview, Is.SameAs(policy));
                Assert.That(f.Session.CheckDependencies(), Is.True);
                typeof(TopDown3DAnimationPlaygroundSession).GetMethod("PreparePlayer", Private).Invoke(f.Session, null);
                Assert.That(f.Session.UnavailableReason, Is.Null);
                Assert.That(f.Driver.LaboratoryForwardReviewConfigured, Is.True);
                bindings.forwardReviewOverride = null;
                Assert.That(f.Session.CheckDependencies(), Is.False);
                f.Session.ConfigureForwardReview(f.Policy);
                Assert.That(f.Session.CheckDependencies(), Is.True, "An explicit fixture/authoring selection remains authoritative.");
                Assert.That(f.Session.ExperimentalForwardReview, Is.SameAs(f.Policy));
            }
            finally { Object.DestroyImmediate(bindings); }
        }

        [Test]
        public void SixChannelStopAndResumePreservePoseAndDoNotInventDistance()
        {
            var envelope = new AnimationPlaygroundForwardPoseEnvelope(); var sprint = new AnimationPlaygroundGaitWeights(); sprint[5] = 1;
            envelope.SampleGaitWeights(sprint, 6, .02f);
            var first = envelope.SampleGaitWeights(AnimationPlaygroundGaitWeights.Idle, 0, .02f);
            Assert.That(first[5], Is.InRange(.9f, 1)); Assert.That(first[0], Is.GreaterThan(0));
            var resumed = envelope.SampleGaitWeights(sprint, 6, .001f);
            Assert.That(Mathf.Abs(resumed[5] - first[5]), Is.LessThan(.001f));
            for (var i = 0; i < 20; i++) envelope.SampleGaitWeights(AnimationPlaygroundGaitWeights.Idle, 0, .02f);
            Assert.That(envelope.GaitWeights[0], Is.EqualTo(1)); Assert.That(envelope.GaitWeights[5], Is.Zero);
        }

        [TestCase(SkinWeights.OneBone)]
        [TestCase(SkinWeights.TwoBones)]
        [TestCase(SkinWeights.FourBones)]
        [Category("PrivateAnimationQualification")]
        public void SixActualGraphChannelsKeepEquippedBooterInsideTheFloorAndHeightEnvelope(SkinWeights quality)
        {
            Policy(); using var f = new AnimationPlaygroundForwardReviewTests.Fixture(true, quality, Path);
            var mesh = new Mesh();
            try
            {
                Assert.That(f.Mixer.GetInput(24).GetInputCount(), Is.EqualTo(6));
                var minimum = float.PositiveInfinity; var maximum = float.NegativeInfinity; var rigid = false; var cloak = false;
                foreach (var speed in new[] { 0f, 1.4f, 2.05f, 2.7f, 3f, 3.1f, 3.5f, 3.9f, 4f, 4.5f, 5f, 5.1f, 5.5f, 5.9f, 6f, 6.15f })
                for (var phase = 0; phase <= 64; phase++)
                {
                    f.Frame(speed, phase / 64d);
                    foreach (var renderer in f.Driver.HumanoidAnimator.transform.parent.GetComponentsInChildren<Renderer>(true))
                    {
                        if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                        Vector3[] vertices;
                        if (renderer is SkinnedMeshRenderer skin) { skin.BakeMesh(mesh, true); vertices = mesh.vertices; cloak |= skin.name == "Cloak cloth skin"; }
                        else if (renderer is MeshRenderer && renderer.TryGetComponent<MeshFilter>(out var filter) && filter.sharedMesh != null)
                        { vertices = filter.sharedMesh.vertices; rigid = true; }
                        else continue;
                        foreach (var vertex in vertices)
                        {
                            var height = renderer.transform.TransformPoint(vertex).y - (f.Player.position.y - .91f);
                            minimum = Mathf.Min(minimum, height); maximum = Mathf.Max(maximum, height);
                        }
                    }
                }
                Assert.That(rigid && cloak, Is.True);
                Assert.That(minimum, Is.GreaterThanOrEqualTo(-.001f)); Assert.That(maximum, Is.LessThanOrEqualTo(1.821f));
                Assert.That(f.Graph.GetOutputCount(), Is.EqualTo(1));
                Assert.That(f.Driver.TryCapturePoseBlend(f, .18f), Is.True, "Action handoff must traverse all six inputs.");
                Assert.That(f.Driver.EndPoseBlend(f), Is.True);
            }
            finally { Object.DestroyImmediate(mesh); }
        }

        [Test]
        public void GamepadPaceUsesMoveDeviceAndRBBindingWithSafeNeutralAndKeyboardFallback()
        {
            var oldSettings = InputSystem.settings; var settings = Object.Instantiate(oldSettings);
            settings.SetInternalFeatureFlag("RUN_PLAYER_UPDATES_IN_EDIT_MODE", true); InputSystem.settings = settings;
            var pad = InputSystem.AddDevice<Gamepad>(); var keyboard = InputSystem.AddDevice<Keyboard>(); var mouse = InputSystem.AddDevice<Mouse>();
            var actions = AnimationPlaygroundInputFactory.Create(AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Project/Settings/Input/InputSystem_Actions.inputactions"));
            var actor = new GameObject("Expanded gamepad fixture"); actor.SetActive(false);
            try
            {
                actor.AddComponent<Rigidbody>(); actor.AddComponent<CapsuleCollider>();
                var router = actor.AddComponent<TopDown3DInputRouter>(); router.Configure(actions); Assert.That(router.Initialize(), Is.True);
                var motor = actor.AddComponent<TopDown3DPlayerMotor>();
                typeof(TopDown3DPlayerMotor).GetField("input", Private).SetValue(motor, router);
                var owner = new object(); motor.BindAnimationPostureOwner(owner); motor.ConfigureLaboratorySpeeds(owner, 1.4f, 3, 5);
                Assert.Throws<ArgumentException>(() => motor.ConfigureLaboratoryGamepadGaits(new object(), 5, 6));
                motor.ConfigureLaboratoryGamepadGaits(owner, 5, 6.15f);
                float Requested(bool sprint) => (float)typeof(TopDown3DPlayerMotor).GetMethod("LaboratoryRequestedSpeed", Private).Invoke(motor, new object[] { sprint });
                foreach (var amount in new[] { .28f, .6f, .8f, 1f })
                {
                    // With an already processed magnitude, the motor must retain analog travel.
                    InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = Vector2.up * Mathf.Lerp(settings.defaultDeadzoneMin, settings.defaultDeadzoneMax, amount) }); InputSystem.Update();
                    Assert.That(router.MoveFromGamepad, Is.True); Assert.That(router.MoveValue.magnitude * Requested(false), Is.EqualTo(amount * 5).Within(.001f));
                }
                InputSystem.QueueStateEvent(mouse, new MouseState { delta = Vector2.right }); InputSystem.Update();
                Assert.That(router.MoveFromGamepad, Is.True, "Looking with the mouse cannot lower a held stick's pace.");
                InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = Vector2.up }.WithButton(GamepadButton.RightShoulder)); InputSystem.Update();
                Assert.That(router.SprintHeld, Is.True); Assert.That(Requested(router.SprintHeld), Is.EqualTo(6.15f));
                router.EnterSystemMenuMode(); Assert.That(router.MoveFromGamepad, Is.False); Assert.That(router.MoveValue, Is.EqualTo(Vector2.zero));
                router.EnterGameplayMode(); InputSystem.Update(); Assert.That(router.MoveValue, Is.EqualTo(Vector2.zero));
                InputSystem.QueueStateEvent(pad, new GamepadState()); InputSystem.Update();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W)); InputSystem.Update();
                Assert.That(router.MoveFromGamepad, Is.False); Assert.That(Requested(false), Is.EqualTo(3)); Assert.That(Requested(true), Is.EqualTo(5));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
                InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = Vector2.up }); InputSystem.Update();
                InputSystem.RemoveDevice(pad); Assert.That(router.MoveFromGamepad, Is.False); Assert.That(router.MoveValue, Is.EqualTo(Vector2.zero));
            }
            finally
            {
                Object.DestroyImmediate(actor); Object.DestroyImmediate(actions);
                if (pad.added) InputSystem.RemoveDevice(pad); InputSystem.RemoveDevice(keyboard); InputSystem.RemoveDevice(mouse);
                InputSystem.settings = oldSettings; Object.DestroyImmediate(settings);
            }
        }
    }
}
