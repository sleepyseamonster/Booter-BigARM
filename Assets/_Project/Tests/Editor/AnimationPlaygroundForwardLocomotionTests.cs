using System;
using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class AnimationPlaygroundForwardLocomotionTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static AnimationPlaygroundForwardLocomotionProfile Policy()
        {
            var policy = AssetDatabase.LoadAssetAtPath<AnimationPlaygroundForwardLocomotionProfile>(
                "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/ForwardLocomotionDistanceAlignedCandidate.asset");
            if (policy == null) Assert.Ignore("Restore the private measured forward candidate.");
            return policy;
        }

        [Test, Category("PrivateAnimationQualification")]
        public void MeasuredPolicyRetainsOriginalSourcesAndLegacyValidationContract()
        {
            var policy = Policy();
            Assert.That(policy.Validate(policy.targetModel, policy.locomotion, out var reason, false), Is.True, reason);
            Assert.That(policy.locomotion.TryValidate(out reason), Is.True, reason);
            foreach (var gait in policy.gaits)
            {
                Assert.That(AssetDatabase.GetAssetPath(gait.sourceClip).EndsWith(".FBX", StringComparison.OrdinalIgnoreCase), Is.True);
                Assert.That(gait.sourceClip.isHumanMotion && gait.sourceClip.isLooping, Is.True);
                Assert.That(gait.sourceClip, Is.SameAs(policy.locomotion.GetRequiredEntry(gait.role).Clip));
                var before = gait.SourceSeconds(.999999);
                var after = gait.SourceSeconds(1.000001);
                Assert.That(Math.Abs(after - before), Is.LessThan(.0001), "Semantic wrap must preserve the native pose time.");
            }
        }

        [Test, Category("PrivateAnimationQualification")]
        public void DraftAndMismatchedPoliciesCannotBootstrapThePlayer()
        {
            var original = Policy(); var policy = Object.Instantiate(original);
            var bindings = Object.Instantiate(AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(
                "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/PlayerBindings.asset"));
            try
            {
                policy.qualified = false; bindings.locomotion = policy.locomotion; bindings.forwardLocomotion = policy;
                Assert.That(bindings.ValidatePlayer(out var reason), Is.False);
                Assert.That(reason, Does.Contain("awaiting"));
                policy.qualified = true;
                Assert.That(bindings.ValidatePlayer(out reason), Is.True, reason);
                Assert.That(policy.Validate(policy.targetModel, null, out _, false), Is.False);
                policy.schema = 2;
                Assert.That(bindings.ValidatePlayer(out _), Is.False);
            }
            finally { Object.DestroyImmediate(bindings); Object.DestroyImmediate(policy); }
        }

        [Test, Category("PrivateAnimationQualification")]
        public void SourceSegmentsAndSignedHeightCannotBypassReviewedBudgets()
        {
            var policy = Object.Instantiate(Policy());
            try
            {
                var gait = policy.gaits[0];
                gait.phaseMap[1].semantic = gait.phaseMap[0].semantic
                    + (gait.phaseMap[1].semantic - gait.phaseMap[0].semantic) * .01f;
                Assert.That(policy.Validate(policy.targetModel, policy.locomotion, out var reason, false), Is.False);
                Assert.That(reason, Does.Contain("source segment"));
            }
            finally { Object.DestroyImmediate(policy); }
            policy = Object.Instantiate(Policy());
            try
            {
                policy.gaits[0].signedHeight = AnimationCurve.Linear(0, -.031f, 1, -.031f);
                Assert.That(policy.Validate(policy.targetModel, policy.locomotion, out var reason, false), Is.False);
                Assert.That(reason, Does.Contain("height"));
            }
            finally { Object.DestroyImmediate(policy); }
            policy = Object.Instantiate(Policy());
            try
            {
                var curve = AnimationCurve.Linear(0, 0, 1, 0);
                var key = curve.keys[0]; key.outTangent = 10; curve.MoveKey(0, key);
                policy.gaits[0].signedHeight = curve;
                Assert.That(policy.Validate(policy.targetModel, policy.locomotion, out var reason, false), Is.False);
                Assert.That(reason, Does.Contain("linear"));
            }
            finally { Object.DestroyImmediate(policy); }
            policy = Object.Instantiate(Policy());
            try
            {
                policy.secondRightDistanceAdjustment = new[] { new Vector2(0, 0), new Vector2(.1f, -.6f), new Vector2(1.4f, 0) };
                Assert.That(policy.Validate(policy.targetModel, policy.locomotion, out var reason, false), Is.False);
                Assert.That(reason, Does.Contain("walk/run interval"));
            }
            finally { Object.DestroyImmediate(policy); }
        }

        [Test, Category("PrivateAnimationQualification")]
        public void PartialTravelShortensStrideWhilePreservingCadenceAndActualGait()
        {
            var policy = Policy();
            var nominal = policy.gaits[0].nominalSpeed;
            Assert.That(.2 / policy.CycleDistance(.2f), Is.EqualTo(nominal / policy.CycleDistance(nominal)).Within(.00001));
            var weights = policy.Weights(.2f);
            Assert.That(weights.x, Is.GreaterThan(.8f)); Assert.That(weights.z + weights.w, Is.Zero);
            Assert.That(weights.x + weights.y, Is.EqualTo(1).Within(.00001));
            Assert.That(policy.Weights(2.7f).z, Is.EqualTo(1).Within(.00001));
            Assert.That(policy.Weights(3.8f).w, Is.EqualTo(1).Within(.00001));
        }

        [Test, Category("PrivateAnimationQualification")]
        public void NativeRateBudgetsHoldAcrossGaitThresholdsAndSignificantContributors()
        {
            var policy = Policy();
            for (var step = 0; step <= 1024; step++)
            {
                var speed = Mathf.Lerp(.18f, 3.8f, step / 1024f);
                var weights = policy.Weights(speed);
                for (var role = 0; role < 3; role++)
                {
                    if (weights[role + 1] < .1f) continue;
                    var gait = policy.gaits[role]; var length = gait.sourceClip.length;
                    for (var key = 1; key < gait.phaseMap.Length; key++)
                    {
                        var a = gait.phaseMap[key - 1]; var b = gait.phaseMap[key];
                        foreach (var phase in new[] { (double)a.semantic, Math.Max(a.semantic, b.semantic - .00000001),
                            Math.Clamp(.4, a.semantic, b.semantic), Math.Clamp(.45, a.semantic, b.semantic),
                            Math.Clamp(.55, a.semantic, b.semantic), Math.Clamp(.6, a.semantic, b.semantic) })
                        {
                            var rate = speed / policy.DistanceDerivative(speed, phase) * length * (b.native - a.native) / (b.semantic - a.semantic);
                            Assert.That(rate, Is.InRange(gait.blendMinimumRate - .001, gait.blendMaximumRate + .001));
                        }
                    }
                }
            }
        }

        [Test, Category("PrivateAnimationQualification")]
        public void MeasuredDistanceInvertsAcrossWrapsAndPreservesTickSubdivision()
        {
            var policy = Policy();
            foreach (var speed in new[] { .2f, 1.4f, 1.725f, 2.05f, 2.375f, 2.57f, 2.7f, 3.25f, 3.8f })
            foreach (var start in new[] { -.03, .4, .49, .57, .99, 12.99 })
            {
                var end = policy.AdvanceCycles(speed, start, 3.7);
                Assert.That(policy.DistanceAtCycles(speed, end) - policy.DistanceAtCycles(speed, start), Is.EqualTo(3.7).Within(.00000001));
                var split = start;
                for (var i = 0; i < 37; i++) split = policy.AdvanceCycles(speed, split, .1);
                Assert.That(split, Is.EqualTo(end).Within(.00000004));
                Assert.That(policy.AdvanceCycles(speed, start, 0), Is.EqualTo(start));
            }
            var clock = new AnimationPlaygroundGaitClock();
            AnimationPlaygroundGroundedMotion Sample(long tick, float speed, float distance) =>
                new(tick, 1, tick * .02, distance / speed, Vector3.forward * distance, Vector3.up, true, false);
            clock.Advance(Sample(1, 2.375f, .0475f), policy);
            for (var tick = 2; tick <= 250; tick++) clock.Advance(Sample(tick, 2.375f, .0475f), policy);
            var cycles = clock.Cycles; var contacts = clock.ContactCount;
            Assert.That(contacts, Is.EqualTo((long)Math.Floor(cycles * 4)));
            for (var frame = 0; frame <= 100; frame++) clock.RenderCycles(frame / 100f, 2.375f, policy);
            Assert.That(clock.Cycles, Is.EqualTo(cycles)); Assert.That(clock.ContactCount, Is.EqualTo(contacts));
            Assert.That(clock.Advance(Sample(250, 2.375f, .0475f), policy).Count, Is.Zero);
        }

        [Test, Category("PrivateAnimationQualification")]
        public void FrameLifecyclePreservesIdleTimeAndDoesNotDuplicateMotionSubscriptions()
        {
            var policy = Object.Instantiate(Policy()); policy.qualified = true;
            var scene = EditorSceneManager.OpenScene(TopDown3DAnimationPlaygroundSession.ScenePath, OpenSceneMode.Additive);
            var preview = EditorSceneManager.NewPreviewScene(); GameObject actor = null;
            var bindings = Object.Instantiate(TopDown3DAnimationPlaygroundSession.Find(scene).Bindings);
            try
            {
                bindings.locomotion = policy.locomotion; bindings.forwardLocomotion = policy;
                actor = new GameObject("Forward frame lifecycle"); actor.SetActive(false); SceneManager.MoveGameObjectToScene(actor, preview);
                var motor = actor.AddComponent<TopDown3DPlayerMotor>(); var driver = actor.AddComponent<TopDown3DPlayerAnimationDriver>();
                EditorUtility.CopySerialized(TopDown3DAnimationPlaygroundSession.Find(scene).Player.GetComponent<TopDown3DPlayerAnimationDriver>(), driver);
                driver.ConfigureLaboratory(bindings);
                void Call(string method) => typeof(TopDown3DPlayerAnimationDriver).GetMethod(method, Private).Invoke(driver, null);
                Call("Awake"); Assert.That(driver.LaboratoryForwardReady, Is.True);
                var frame = (Action<float, double, float>)Delegate.CreateDelegate(typeof(Action<float, double, float>), driver,
                    typeof(TopDown3DPlayerAnimationDriver).GetMethod("PresentForwardFrame", Private));
                var clips = (AnimationClipPlayable[])typeof(TopDown3DPlayerAnimationDriver).GetField("clipPlayables", Private).GetValue(driver);
                var eventField = typeof(TopDown3DPlayerMotor).GetField("LaboratoryGroundedMotionCompleted", Private);
                int Subscribers() => ((Delegate)eventField.GetValue(motor))?.GetInvocationList().Length ?? 0;
                Assert.That(Subscribers(), Is.EqualTo(1)); Call("OnEnable"); Call("OnEnable");
                Assert.That(Subscribers(), Is.EqualTo(1));
                for (var i = 1; i <= 4; i++)
                {
                    Call("PrepareForwardGraphUpdate"); frame(0, 0, .1f);
                    Call("EmitForwardFootEffectsForRenderedPose");
                    Assert.That(clips[0].GetTime(), Is.EqualTo(i * .1).Within(.00001));
                    Assert.That(driver.LaboratoryForwardCycles, Is.Zero);
                    Assert.That(driver.LaboratoryForwardContacts, Is.Zero);
                }
                // An intervening presentation owner gets a frame without standing
                // presentation. Its return starts the same supported idle phase.
                var owner = new object();
                Assert.That(driver.TryBeginAnimationPresentation(owner, policy.gaits[0].sourceClip), Is.True);
                Call("PrepareForwardGraphUpdate");
                Assert.That(driver.EndAnimationPresentation(owner), Is.True);
                Call("PrepareForwardGraphUpdate"); frame(0, 0, .1f);
                Assert.That(clips[0].GetTime(), Is.EqualTo(.1).Within(.00001));
                Call("OnDisable"); Assert.That(Subscribers(), Is.Zero);
                Call("OnEnable"); Assert.That(Subscribers(), Is.EqualTo(1));
            }
            finally
            {
                if (actor != null)
                    typeof(TopDown3DPlayerAnimationDriver).GetMethod("OnDestroy", Private).Invoke(actor.GetComponent<TopDown3DPlayerAnimationDriver>(), null);
                if (actor != null) Object.DestroyImmediate(actor);
                Object.DestroyImmediate(bindings); Object.DestroyImmediate(policy);
                EditorSceneManager.ClosePreviewScene(preview); EditorSceneManager.CloseScene(scene, true);
            }
        }

        [TestCase(SkinWeights.OneBone)]
        [TestCase(SkinWeights.TwoBones)]
        [TestCase(SkinWeights.FourBones)]
        [Category("PrivateAnimationQualification")]
        public void ActualDriverUsesOneManualPoseAndNeverMovesThePhysicsRoot(SkinWeights quality)
        {
            var policy = Policy(); var previousQuality = QualitySettings.skinWeights;
            var scene = EditorSceneManager.OpenScene(TopDown3DAnimationPlaygroundSession.ScenePath, OpenSceneMode.Additive);
            var preview = EditorSceneManager.NewPreviewScene(); GameObject actor = null;
            var bindings = Object.Instantiate(TopDown3DAnimationPlaygroundSession.Find(scene).Bindings);
            try
            {
                QualitySettings.skinWeights = quality;
                bindings.locomotion = policy.locomotion; bindings.forwardLocomotion = null;
                actor = new GameObject("Offline forward pose contract"); actor.SetActive(false);
                SceneManager.MoveGameObjectToScene(actor, preview); actor.AddComponent<TopDown3DPlayerMotor>();
                var driver = actor.AddComponent<TopDown3DPlayerAnimationDriver>();
                EditorUtility.CopySerialized(TopDown3DAnimationPlaygroundSession.Find(scene).Player.GetComponent<TopDown3DPlayerAnimationDriver>(), driver);
                driver.ConfigureLaboratory(bindings);
                typeof(TopDown3DPlayerAnimationDriver).GetMethod("Awake", Private).Invoke(driver, null);
                typeof(TopDown3DPlayerAnimationDriver).GetField("laboratoryForwardProfile", Private).SetValue(driver, policy);
                var render = (Action<float, double, double>)Delegate.CreateDelegate(typeof(Action<float, double, double>), driver,
                    typeof(TopDown3DPlayerAnimationDriver).GetMethod("PresentForwardLocomotionPose", Private));
                var graph = (PlayableGraph)typeof(TopDown3DPlayerAnimationDriver).GetField("playableGraph", Private).GetValue(driver);
                var clips = (AnimationClipPlayable[])typeof(TopDown3DPlayerAnimationDriver).GetField("clipPlayables", Private).GetValue(driver);
                var sampler = (AnimationPlaygroundFootProjection.Sampler)typeof(TopDown3DPlayerAnimationDriver)
                    .GetField("footProjectionSampler", Private).GetValue(driver);
                foreach (var speed in new[] { 1.4f, 2.05f, 2.7f, 3.25f, 3.8f })
                    foreach (var phase in new[] { 0d, .25, .5, .75 })
                    {
                        render(speed, phase, 0);
                        Assert.That(graph.GetTimeUpdateMode(), Is.EqualTo(DirectorUpdateMode.Manual));
                        Assert.That(graph.GetOutputCount(), Is.EqualTo(1));
                        Assert.That(sampler.MinimumWorldY(), Is.GreaterThanOrEqualTo(-bindings.capsuleHeight * .5f - .00001f));
                        var time = clips[2].GetTime(); render(speed, phase, 0);
                        Assert.That(clips[2].GetTime(), Is.EqualTo(time).Within(.0000001));
                        Assert.That(clips[2].GetSpeed(), Is.Zero);
                        Assert.That(actor.transform.position, Is.EqualTo(Vector3.zero));
                        Assert.That(actor.GetComponent<Rigidbody>().position, Is.EqualTo(Vector3.zero));
                    }
            }
            finally
            {
                QualitySettings.skinWeights = previousQuality;
                if (actor != null)
                    typeof(TopDown3DPlayerAnimationDriver).GetMethod("OnDestroy", Private).Invoke(actor.GetComponent<TopDown3DPlayerAnimationDriver>(), null);
                if (actor != null) Object.DestroyImmediate(actor); Object.DestroyImmediate(bindings);
                EditorSceneManager.ClosePreviewScene(preview); EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
