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
        private static void PairLegacySpeeds(TopDown3DAnimationPlaygroundBindings bindings, AnimationPlaygroundForwardLocomotionProfile policy)
        {
            // These tests qualify the retained three-gait source, independently
            // of the current five-band review selected on the author's binding.
            bindings.slowWalkSpeed = policy.gaits[0].nominalSpeed;
            bindings.runSpeed = policy.gaits[1].nominalSpeed;
            bindings.sprintSpeed = policy.gaits[^1].nominalSpeed;
            bindings.movingTurns = null; bindings.movingTurnsRequired = false;
        }
        private static AnimationPlaygroundForwardLocomotionProfile Policy()
        {
            var policy = AssetDatabase.LoadAssetAtPath<AnimationPlaygroundForwardLocomotionProfile>(
                "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/ForwardLocomotionDistanceAlignedCandidate.asset");
            if (policy == null) Assert.Ignore("Restore the private measured forward candidate.");
            return policy;
        }

        [Test, Category("PrivateAnimationQualification")]
        public void MeasuredPolicyRetainsSourceProvenanceAndLegacyValidationContract()
        {
            var policy = Policy();
            Assert.That(policy.Validate(policy.targetModel, policy.locomotion, out var reason, false), Is.True, reason);
            Assert.That(policy.locomotion.TryValidate(out reason), Is.True, reason);
            foreach (var gait in policy.gaits)
            {
                var path = AssetDatabase.GetAssetPath(gait.sourceClip);
                if (!path.EndsWith(".FBX", StringComparison.OrdinalIgnoreCase))
                {
                    Assert.That(gait.role, Is.EqualTo(TopDown3DLocomotionClipRole.Sprint));
                    Assert.That(path, Is.EqualTo("Assets/_Project/Art/Animations/Mixamo/Derived/SprintArmDriveReview.anim"));
                    AnimationClip source = null;
                    foreach (var asset in AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Art/Characters/Animations/MoCapCentral/CoreMotion/Locomotion/MCU_am_Loco_Run_Fwd_NoRM.FBX"))
                        if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__")) source = clip;
                    Assert.That(source, Is.Not.Null);
                    Assert.That(gait.sourceClip.length, Is.EqualTo(source.length).Within(.000001));
                    var originals = AnimationUtility.GetCurveBindings(source);
                    Assert.That(AnimationUtility.GetCurveBindings(gait.sourceClip).Length, Is.EqualTo(originals.Length));
                    var changed = 0;
                    foreach (var binding in originals)
                    {
                        var keys = AnimationUtility.GetEditorCurve(gait.sourceClip, binding).keys;
                        var baseline = AnimationUtility.GetEditorCurve(source, binding).keys;
                        var upperBody = binding.path == "" && (binding.propertyName == "Left Arm Front-Back" || binding.propertyName == "Right Arm Front-Back"
                            || binding.propertyName == "Left Arm Down-Up" || binding.propertyName == "Right Arm Down-Up"
                            || binding.propertyName == "Chest Front-Back" || binding.propertyName == "UpperChest Front-Back");
                        if (upperBody)
                        {
                            changed++;
                            Assert.That(keys.Length, Is.EqualTo(baseline.Length));
                            for (var i = 0; i < keys.Length; i++)
                            {
                                Assert.That(keys[i].time, Is.EqualTo(baseline[i].time));
                                Assert.That(keys[i].value, Is.InRange(-1f, 1f));
                            }
                        }
                        else CollectionAssert.AreEqual(baseline, keys, "Preserve source root, leg, foot and other muscle curves: " + binding.propertyName);
                    }
                    Assert.That(changed, Is.EqualTo(6));
                }
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
                policy.qualified = false; PairLegacySpeeds(bindings, policy); bindings.locomotion = policy.locomotion; bindings.forwardLocomotion = policy;
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
        public void OptionalJogPlateauPreservesPureJogAndRejectsInvalidBlendBounds()
        {
            var policy = Object.Instantiate(Policy());
            try
            {
                var walk = policy.gaits[0].nominalSpeed; var jog = policy.gaits[1].nominalSpeed;
                var previous = policy.walkToJogBlendEndSpeed > 0 ? policy.walkToJogBlendEndSpeed : jog;
                var end = Mathf.Lerp(walk, jog, .8f);
                for (var i = 0; i < policy.secondRightDistanceAdjustment.Length; i++)
                {
                    var p = policy.secondRightDistanceAdjustment[i];
                    if (p.x > walk && p.x <= previous) p.x = Mathf.Lerp(walk, end, Mathf.InverseLerp(walk, previous, p.x));
                    policy.secondRightDistanceAdjustment[i] = p;
                }
                policy.walkToJogBlendEndSpeed = end;
                Assert.That(policy.Validate(policy.targetModel, policy.locomotion, out var reason, false), Is.True, reason);
                foreach (var speed in new[] { end, (end + jog) * .5f, jog })
                    Assert.That(policy.Weights(speed), Is.EqualTo(new Vector4(0, 0, 1, 0)));
                policy.walkToJogBlendEndSpeed = jog + .1f;
                Assert.That(policy.Validate(policy.targetModel, policy.locomotion, out _, false), Is.False);
                policy.walkToJogBlendEndSpeed = float.NaN;
                Assert.That(policy.Validate(policy.targetModel, policy.locomotion, out _, false), Is.False);
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
            Assert.That(policy.Weights(policy.gaits[1].nominalSpeed).z, Is.EqualTo(1).Within(.00001));
            Assert.That(policy.Weights(policy.gaits[2].nominalSpeed).w, Is.EqualTo(1).Within(.00001));
        }

        [Test, Category("PrivateAnimationQualification")]
        public void NativeRateBudgetsHoldAcrossGaitThresholdsAndSignificantContributors()
        {
            var policy = Policy();
            for (var step = 0; step <= 1024; step++)
            {
                var speed = Mathf.Lerp(.18f, policy.gaits[2].nominalSpeed, step / 1024f);
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
                PairLegacySpeeds(bindings, policy); bindings.locomotion = policy.locomotion; bindings.forwardLocomotion = policy;
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
                actor.SetActive(true);
                Assert.That(driver.HumanoidAnimator.isActiveAndEnabled, Is.True);
                typeof(TopDown3DPlayerAnimationDriver).GetField("laboratoryForwardProfile", Private).SetValue(driver, policy);
                var render = (Action<float, double, double>)Delegate.CreateDelegate(typeof(Action<float, double, double>), driver,
                    typeof(TopDown3DPlayerAnimationDriver).GetMethod("PresentForwardLocomotionPose", Private));
                var graph = (PlayableGraph)typeof(TopDown3DPlayerAnimationDriver).GetField("playableGraph", Private).GetValue(driver);
                var clips = (AnimationClipPlayable[])typeof(TopDown3DPlayerAnimationDriver).GetField("clipPlayables", Private).GetValue(driver);
                var sampler = (AnimationPlaygroundFootProjection.Sampler)typeof(TopDown3DPlayerAnimationDriver)
                    .GetField("footProjectionSampler", Private).GetValue(driver);
                var trackedFoot = driver.HumanoidAnimator.GetBoneTransform(HumanBodyBones.LeftFoot);
                foreach (var speed in new[] { 1.4f, 2.05f, 2.7f, 3.25f, 3.8f })
                {
                    var firstFoot = Vector3.zero;
                    foreach (var phase in new[] { 0d, .25, .5, .75 })
                    {
                        render(speed, phase, 0);
                        if (phase == 0) firstFoot = trackedFoot.position;
                        if (phase == .25) Assert.That(Vector3.Distance(trackedFoot.position, firstFoot), Is.GreaterThan(.03f), "The source must move the active rig.");
                        Assert.That(graph.GetTimeUpdateMode(), Is.EqualTo(DirectorUpdateMode.Manual));
                        Assert.That(graph.GetOutputCount(), Is.EqualTo(1));
                        Assert.That(sampler.MinimumWorldY(), Is.GreaterThanOrEqualTo(-bindings.capsuleHeight * .5f - .00001f));
                        var time = clips[2].GetTime(); var position = trackedFoot.position; render(speed, phase, 0);
                        Assert.That(clips[2].GetTime(), Is.EqualTo(time).Within(.0000001));
                        Assert.That(Vector3.Distance(trackedFoot.position, position), Is.LessThan(.00001f));
                        Assert.That(clips[2].GetSpeed(), Is.Zero);
                        Assert.That(actor.transform.position, Is.EqualTo(Vector3.zero));
                        Assert.That(actor.GetComponent<Rigidbody>().position, Is.EqualTo(Vector3.zero));
                    }
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

        [TestCase(30)] [TestCase(60)] [TestCase(144)]
        [Category("PrivateAnimationQualification")]
        public void CompletedTickTraceOwnsContactsWhileBlockedPoseSettles(int renderRate)
        {
            var policy = Object.Instantiate(Policy()); policy.qualified = true;
            var scene = EditorSceneManager.OpenScene(TopDown3DAnimationPlaygroundSession.ScenePath, OpenSceneMode.Additive);
            var preview = scene; GameObject actor = null, ground = null;
            var bindings = Object.Instantiate(TopDown3DAnimationPlaygroundSession.Find(scene).Bindings);
            try
            {
                PairLegacySpeeds(bindings, policy); bindings.locomotion = policy.locomotion; bindings.forwardLocomotion = policy;
                actor = new GameObject("Completed tick trace"); actor.SetActive(false); SceneManager.MoveGameObjectToScene(actor, preview);
                var motor = actor.AddComponent<TopDown3DPlayerMotor>(); var driver = actor.AddComponent<TopDown3DPlayerAnimationDriver>();
                EditorUtility.CopySerialized(TopDown3DAnimationPlaygroundSession.Find(scene).Player.GetComponent<TopDown3DPlayerAnimationDriver>(), driver);
                driver.ConfigureLaboratory(bindings);
                void Call(string method) => typeof(TopDown3DPlayerAnimationDriver).GetMethod(method, Private).Invoke(driver, null);
                Call("Awake"); actor.SetActive(true); actor.transform.position = new Vector3(250, 20, 250);
                typeof(TopDown3DPlayerMotor).GetProperty("IsGrounded").SetValue(motor, true);
                ground = new GameObject("Deterministic contact-query floor"); SceneManager.MoveGameObjectToScene(ground, preview);
                ground.transform.position = new Vector3(250, 20 - bindings.capsuleHeight * .5f - .05f, 250);
                ground.AddComponent<BoxCollider>().size = new Vector3(8, .1f, 8); ground.AddComponent<TopDown3DGroundSurface>();
                Physics.SyncTransforms();
                var publish = (Action<AnimationPlaygroundGroundedMotion>)typeof(TopDown3DPlayerMotor)
                    .GetField("LaboratoryGroundedMotionCompleted", Private).GetValue(motor);
                var frame = (Action<float, double, float>)Delegate.CreateDelegate(typeof(Action<float, double, float>), driver,
                    typeof(TopDown3DPlayerAnimationDriver).GetMethod("PresentForwardFrame", Private));
                var effects = (Action<double, double>)Delegate.CreateDelegate(typeof(Action<double, double>), driver,
                    typeof(TopDown3DPlayerAnimationDriver).GetMethod("EmitForwardRenderedPlants", Private));
                var clock = (AnimationPlaygroundGaitClock)typeof(TopDown3DPlayerAnimationDriver).GetField("forwardClock", Private).GetValue(driver);
                var count = 0; driver.FootContact += _ => count++;
                var nextTick = 0; var previousSpeed = 0f; var speed = 0f;
                var stoppedCycles = 0d; var stoppedContacts = 0L; var blockedEffects = 0;
                for (var rendered = 0; rendered <= renderRate * 3; rendered++)
                {
                    var now = rendered / (double)renderRate;
                    while (nextTick * .02 <= now + .00000001)
                    {
                        previousSpeed = speed;
                        var requested = nextTick < 100 ? Mathf.Min(3.8f, nextTick * .17f) : 0;
                        var sample = new AnimationPlaygroundGroundedMotion(nextTick, 1, nextTick * .02, .02,
                            Vector3.forward * (requested * .02f), Vector3.up, true, nextTick == 0);
                        publish(sample); speed = driver.LaboratoryForwardSpeed; nextTick++;
                        // A repeated publication cannot add a second contact.
                        var ledger = driver.LaboratoryForwardContacts; publish(sample);
                        Assert.That(driver.LaboratoryForwardContacts, Is.EqualTo(ledger));
                        if (nextTick == 101)
                        { stoppedCycles = clock.Cycles; stoppedContacts = clock.ContactCount; blockedEffects = count; }
                    }
                    var alpha = Mathf.Clamp01((float)((now - (nextTick - 1) * .02) / .02));
                    var cycles = clock.RenderCycles(alpha, speed, policy);
                    Call("PrepareForwardGraphUpdate"); frame(Mathf.Lerp(previousSpeed, speed, alpha), cycles, 1f / renderRate);
                    var before = count; effects(cycles, (nextTick - 1) * .02);
                    Assert.That(count - before, Is.LessThanOrEqualTo(2));
                    var after = count; effects(cycles, (nextTick - 1) * .02); Assert.That(count, Is.EqualTo(after));
                    if (now >= 2.3)
                    {
                        Assert.That(driver.LaboratoryForwardPoseSpeed, Is.Zero);
                        Assert.That(clock.Cycles, Is.EqualTo(stoppedCycles));
                        Assert.That(clock.ContactCount, Is.EqualTo(stoppedContacts));
                        Assert.That(count, Is.EqualTo(blockedEffects));
                    }
                }
                Assert.That(clock.ContactCount, Is.GreaterThan(0), $"Completed samples must reach the driver: active={driver.isActiveAndEnabled}, ready={driver.LaboratoryForwardReady}.");
                Assert.That(count, Is.GreaterThan(0), "The marked floor must exercise actual displayed-pose effects.");
                Assert.That(clock.ContactCount, Is.EqualTo((long)Math.Floor(clock.Cycles * 4)));
                Assert.That(actor.transform.position, Is.EqualTo(new Vector3(250, 20, 250)));
                var effectsBeforeHitch = count; var discardedBeforeHitch = driver.LaboratoryDiscardedFootEffects;
                publish(new AnimationPlaygroundGroundedMotion(199, 1, 4.8, .8, Vector3.forward * 3.04f, Vector3.up, true, false));
                Assert.That(clock.ContactCount - stoppedContacts, Is.GreaterThanOrEqualTo(2));
                Assert.That(clock.ContactCount, Is.EqualTo((long)Math.Floor(clock.Cycles * 4)));
                frame(3.8f, clock.Cycles, .8f); effects(clock.Cycles, 5.1);
                Assert.That(count, Is.EqualTo(effectsBeforeHitch), "Stale cosmetic contacts must not replay after a hitch.");
                Assert.That(driver.LaboratoryDiscardedFootEffects, Is.GreaterThan(discardedBeforeHitch));
                var pending = new AnimationPlaygroundGroundedMotion(200, 1, 5.6, .8, Vector3.forward * 3.04f, Vector3.up, true, false);
                publish(pending);
                typeof(TopDown3DPlayerMotor).GetProperty("LaboratoryGroundedMotion").SetValue(motor, pending);
                frame(3.8f, clock.Cycles, .02f);
                Assert.That((int)typeof(TopDown3DPlayerAnimationDriver).GetField("forwardPlantCount", Private).GetValue(driver), Is.GreaterThan(0));
                // Reset may occur after Update but before LateUpdate. The motor's
                // invalidated observation must veto the previously rendered pose.
                typeof(TopDown3DPlayerMotor).GetProperty("LaboratoryGroundedMotion").SetValue(motor, default(AnimationPlaygroundGroundedMotion));
                Call("EmitForwardFootEffectsForRenderedPose");
                Assert.That(count, Is.EqualTo(effectsBeforeHitch));
                Assert.That((int)typeof(TopDown3DPlayerAnimationDriver).GetField("forwardPlantCount", Private).GetValue(driver), Is.Zero);
                var contactCount = clock.ContactCount;
                var beforeOwnershipChange = clock.Cycles;
                publish(new AnimationPlaygroundGroundedMotion(201, 2, 6, .4, Vector3.forward * 10, Vector3.up, false, true));
                Assert.That(clock.ContactCount, Is.EqualTo(contactCount));
                Assert.That(clock.Cycles, Is.EqualTo(beforeOwnershipChange));
            }
            finally
            {
                if (actor != null) typeof(TopDown3DPlayerAnimationDriver).GetMethod("OnDestroy", Private).Invoke(actor.GetComponent<TopDown3DPlayerAnimationDriver>(), null);
                if (actor != null) Object.DestroyImmediate(actor); if (ground != null) Object.DestroyImmediate(ground);
                Object.DestroyImmediate(bindings); Object.DestroyImmediate(policy);
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
