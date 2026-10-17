using System;
using System.Collections.Generic;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BooterBigArm.Tests
{
    // In-memory profile contracts over existing humanoid source data. These
    // tests do not construct an actor, load a scene, drive input or step physics.
    [Category("PrivateAnimationQualification")]
    public sealed class AnimationPlaygroundMovingTurnProfileTests
    {
        private AnimationPlaygroundForwardLocomotionProfile accepted;
        private readonly List<AnimationClip> sources = new();
        private AnimationPlaygroundMovingTurnProfile profile;
        private readonly List<Object> owned = new();
        private AnimationPlaygroundForwardGait Gait => profile.bands[0].left;

        [OneTimeSetUp] public void LoadReadOnlyAcceptedSources()
        {
            accepted = AssetDatabase.LoadAssetAtPath<AnimationPlaygroundForwardLocomotionProfile>(
                "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/ExpandedForwardGaitReview.asset");
            Assert.That(accepted, Is.Not.Null, "Restore the private accepted forward profile.");
            Assert.That(accepted.Validate(accepted.targetModel, accepted.locomotion, out var reason, false), Is.True, reason);
            for (var i = 0; i < 5; i++)
            for (var side = 0; side < 2; side++)
            {
                var source = Object.Instantiate(accepted.gaits[i].sourceClip);
                source.name = $"In-memory arc contract {i}/{side}";
                sources.Add(source);
            }
        }

        [OneTimeTearDown] public void ReleaseReadOnlySourceCopies()
        {
            foreach (var source in sources) Object.DestroyImmediate(source);
            sources.Clear();
        }

        [SetUp] public void CreatePairedProfile()
        {
            profile = ScriptableObject.CreateInstance<AnimationPlaygroundMovingTurnProfile>();
            owned.Add(profile);
            profile.forwardProfile = accepted;
            profile.upperBodyBones = new[] { HumanBodyBones.Spine, HumanBodyBones.Chest, HumanBodyBones.LeftUpperArm, HumanBodyBones.RightUpperArm };
            profile.bands = new AnimationPlaygroundMovingTurnBand[5];
            for (var i = 0; i < 5; i++) profile.bands[i] = new AnimationPlaygroundMovingTurnBand
            {
                title = $"Band {i}", provenance = "In-memory validation fixture; no source asset is edited.",
                sourceCurvatureDegreesPerMetre = 20, maximumWeight = .5f,
                left = Copy(accepted.gaits[i], sources[i * 2]), right = Copy(accepted.gaits[i], sources[i * 2 + 1]),
                leftPose = Pose(sources[i * 2], profile.upperBodyBones.Length), rightPose = Pose(sources[i * 2 + 1], profile.upperBodyBones.Length)
            };
        }

        [TearDown] public void ReleaseProfileCopies()
        {
            foreach (var item in owned) Object.DestroyImmediate(item);
            owned.Clear();
        }

        private static AnimationPlaygroundMovingTurnBonePose Pose(AnimationClip source, int bones)
        {
            var rotations = new Quaternion[2 * bones];
            for (var i = 0; i < rotations.Length; i++) rotations[i] = Quaternion.identity;
            return new AnimationPlaygroundMovingTurnBonePose { sourceClip = source, samples = 2, rotations = rotations };
        }

        private static AnimationPlaygroundForwardGait Copy(AnimationPlaygroundForwardGait source, AnimationClip clip)
            => new()
            {
                role = source.role, sourceClip = clip, nominalSpeed = source.nominalSpeed, metresPerCycle = source.metresPerCycle,
                pureMinimumRate = source.pureMinimumRate, pureMaximumRate = source.pureMaximumRate,
                blendMinimumRate = source.blendMinimumRate, blendMaximumRate = source.blendMaximumRate,
                nativeContacts = (float[])source.nativeContacts.Clone(), phaseMap = (AnimationPlaygroundNativePhase[])source.phaseMap.Clone(),
                signedHeight = new AnimationCurve(source.signedHeight.keys)
            };

        private void Rejected(string reasonPart = null)
        {
            Assert.That(profile.Validate(accepted, out var reason), Is.False);
            Assert.That(reason, Is.Not.Null.And.Not.Empty);
            if (reasonPart != null) StringAssert.Contains(reasonPart, reason);
        }

        [Test] public void CompletePairedBandsShareTheAcceptedClockWithoutMutatingItsData()
        {
            var before = EditorJsonUtility.ToJson(accepted);
            Assert.That(profile.Validate(accepted, out var reason), Is.True, reason);
            Assert.That(profile.TargetModel, Is.SameAs(accepted.targetModel));
            Assert.That(profile.FootProjection, Is.SameAs(accepted.footProjection));
            Assert.That(EditorJsonUtility.ToJson(accepted), Is.EqualTo(before));
            for (var i = 0; i < 5; i++)
            foreach (var arc in new[] { profile.bands[i].left, profile.bands[i].right })
            {
                Assert.That(arc.role, Is.EqualTo(accepted.gaits[i].role));
                Assert.That(arc.nominalSpeed, Is.EqualTo(accepted.gaits[i].nominalSpeed));
                Assert.That(arc.metresPerCycle, Is.EqualTo(accepted.gaits[i].metresPerCycle));
                for (var phase = 0; phase <= 16; phase++)
                    Assert.That(arc.SourceSeconds(phase / 16d), Is.EqualTo(accepted.gaits[i].SourceSeconds(phase / 16d)).Within(.000001));
            }
        }

        [Test] public void MissingOrDifferentPairingCannotValidate()
        {
            Assert.That(profile.Validate(null, out _), Is.False);
            var other = Object.Instantiate(accepted); owned.Add(other);
            Assert.That(profile.Validate(other, out _), Is.False);
            profile.forwardProfile = null; Rejected("paired");
            Assert.That(profile.TargetModel, Is.Null); Assert.That(profile.FootProjection, Is.Null);
        }

        [Test] public void AnInvalidPairedProjectionDoesNotGetAcceptedThroughTheExtension()
        {
            var other = Object.Instantiate(accepted); owned.Add(other); other.footProjection = null;
            profile.forwardProfile = other;
            Assert.That(profile.Validate(other, out var reason), Is.False);
            StringAssert.Contains("paired", reason);
        }

        [TestCase("schema")] [TestCase("null bands")] [TestCase("four bands")]
        [TestCase("six bands")] [TestCase("null band")]
        public void IncompleteOrUnsupportedProfilesAreRejected(string failure)
        {
            if (failure == "schema") profile.schema = 2;
            if (failure == "null bands") profile.bands = null;
            if (failure == "four bands") Array.Resize(ref profile.bands, 4);
            if (failure == "six bands") Array.Resize(ref profile.bands, 6);
            if (failure == "null band") profile.bands[2] = null;
            Rejected();
        }

        [TestCase("title")] [TestCase("provenance")] [TestCase("zero curvature")]
        [TestCase("negative curvature")] [TestCase("NaN curvature")] [TestCase("infinite curvature")]
        [TestCase("zero weight")] [TestCase("negative weight")] [TestCase("over weight")] [TestCase("NaN weight")]
        public void BandsRequireProvenanceAndFiniteBoundedContributions(string failure)
        {
            var band = profile.bands[0];
            switch (failure)
            {
                case "title": band.title = " "; break;
                case "provenance": band.provenance = null; break;
                case "zero curvature": band.sourceCurvatureDegreesPerMetre = 0; break;
                case "negative curvature": band.sourceCurvatureDegreesPerMetre = -20; break;
                case "NaN curvature": band.sourceCurvatureDegreesPerMetre = float.NaN; break;
                case "infinite curvature": band.sourceCurvatureDegreesPerMetre = float.PositiveInfinity; break;
                case "zero weight": band.maximumWeight = 0; break;
                case "negative weight": band.maximumWeight = -.1f; break;
                case "over weight": band.maximumWeight = .650001f; break;
                case "NaN weight": band.maximumWeight = float.NaN; break;
            }
            Rejected("band 0");
        }

        [Test] public void AllAllowedUpperBonesCanBePairedWithoutIncludingRootOrLegs()
        {
            profile.upperBodyBones = new[] { HumanBodyBones.Spine, HumanBodyBones.Chest, HumanBodyBones.UpperChest,
                HumanBodyBones.Neck, HumanBodyBones.Head, HumanBodyBones.LeftShoulder, HumanBodyBones.LeftUpperArm,
                HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, HumanBodyBones.RightShoulder,
                HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand };
            foreach (var band in profile.bands)
            { band.leftPose = Pose(band.left.sourceClip, 13); band.rightPose = Pose(band.right.sourceClip, 13); }
            Assert.That(profile.Validate(accepted, out var reason), Is.True, reason);
        }

        [TestCase(HumanBodyBones.Hips)] [TestCase(HumanBodyBones.LeftUpperLeg)]
        [TestCase(HumanBodyBones.LeftLowerLeg)] [TestCase(HumanBodyBones.LeftFoot)]
        [TestCase(HumanBodyBones.RightUpperLeg)] [TestCase(HumanBodyBones.RightToes)]
        [TestCase(HumanBodyBones.LastBone)] [TestCase((HumanBodyBones)(-1))]
        public void TapesCannotAuthorizeRootHipsLegsOrUnknownBones(HumanBodyBones bone)
        {
            profile.upperBodyBones[0] = bone;
            Rejected("allowed upper bones");
        }

        [TestCase("null")] [TestCase("empty")] [TestCase("duplicate")]
        public void UpperBoneSelectionMustBeNonemptyAndUnique(string failure)
        {
            if (failure == "null") profile.upperBodyBones = null;
            if (failure == "empty") profile.upperBodyBones = Array.Empty<HumanBodyBones>();
            if (failure == "duplicate") profile.upperBodyBones[1] = profile.upperBodyBones[0];
            Rejected();
        }

        [TestCase("missing left")] [TestCase("missing right")] [TestCase("missing source")]
        [TestCase("wrong side source")] [TestCase("wrong band source")]
        [TestCase("too few samples")] [TestCase("too many samples")]
        [TestCase("missing rotations")] [TestCase("short rotations")] [TestCase("long rotations")]
        [TestCase("zero rotation")] [TestCase("NaN rotation")] [TestCase("infinite rotation")]
        [TestCase("unnormalized rotation")] [TestCase("open seam")]
        public void UpperTapesRequireCompleteNormalizedSourcePairedLoops(string failure)
        {
            var pose = profile.bands[0].leftPose;
            switch (failure)
            {
                case "missing left": profile.bands[0].leftPose = null; break;
                case "missing right": profile.bands[0].rightPose = null; break;
                case "missing source": pose.sourceClip = null; break;
                case "wrong side source": pose.sourceClip = profile.bands[0].right.sourceClip; break;
                case "wrong band source": pose.sourceClip = profile.bands[2].left.sourceClip; break;
                case "too few samples": pose.samples = 1; break;
                case "too many samples": pose.samples = 2050; break;
                case "missing rotations": pose.rotations = null; break;
                case "short rotations": Array.Resize(ref pose.rotations, pose.rotations.Length - 1); break;
                case "long rotations": Array.Resize(ref pose.rotations, pose.rotations.Length + 1); break;
                case "zero rotation": pose.rotations[0] = new Quaternion(0, 0, 0, 0); break;
                case "NaN rotation": pose.rotations[0] = new Quaternion(float.NaN, 0, 0, 1); break;
                case "infinite rotation": pose.rotations[0] = new Quaternion(0, float.PositiveInfinity, 0, 1); break;
                case "unnormalized rotation": pose.rotations[0] = new Quaternion(0, 0, 0, 1.01f); break;
                case "open seam": pose.rotations[profile.upperBodyBones.Length] = Quaternion.AngleAxis(.2f, Vector3.up); break;
            }
            Rejected("pose:");
        }

        [Test] public void AntipodalQuaternionsCloseTheSameOrientationWithoutALoopSpin()
        {
            var pose = profile.bands[0].leftPose;
            pose.rotations[profile.upperBodyBones.Length] = new Quaternion(0, 0, 0, -1);
            Assert.That(profile.Validate(accepted, out var reason), Is.True, reason);
            Assert.That(Quaternion.Angle(pose.Sample(.5f, 0, profile.upperBodyBones.Length), Quaternion.identity), Is.LessThan(.01f));
        }

        [Test] public void UpperTapeSamplingInterpolatesFlattenedBonesAndWrapsSemanticPhase()
        {
            var pose = new AnimationPlaygroundMovingTurnBonePose { samples = 3, rotations = new[] {
                Quaternion.identity, Quaternion.AngleAxis(10, Vector3.up),
                Quaternion.AngleAxis(90, Vector3.right), Quaternion.AngleAxis(-50, Vector3.up),
                Quaternion.identity, Quaternion.AngleAxis(10, Vector3.up) } };
            foreach (var phase in new[] { .25f, .75f, 1.25f, -.25f })
            {
                Assert.That(Quaternion.Angle(pose.Sample(phase, 0, 2), Quaternion.AngleAxis(45, Vector3.right)), Is.LessThan(.05f));
                Assert.That(Quaternion.Angle(pose.Sample(phase, 1, 2), Quaternion.AngleAxis(-20, Vector3.up)), Is.LessThan(.05f));
            }
            Assert.That(Quaternion.Angle(pose.Sample(0, 0, 2), Quaternion.identity), Is.LessThan(.01f));
            Assert.That(Quaternion.Angle(pose.Sample(1, 1, 2), Quaternion.AngleAxis(10, Vector3.up)), Is.LessThan(.05f));
        }

        [TestCase("NaN phase")] [TestCase("infinite phase")] [TestCase("negative bone")]
        [TestCase("past bone")] [TestCase("zero bones")] [TestCase("overflow bones")]
        [TestCase("missing rotations")] [TestCase("short rotations")] [TestCase("zero rotation")]
        public void InvalidTapeSamplingReturnsFiniteIdentity(string failure)
        {
            var pose = Pose(null, 2); var phase = .25f; var bone = 0; var count = 2;
            if (failure == "NaN phase") phase = float.NaN;
            if (failure == "infinite phase") phase = float.PositiveInfinity;
            if (failure == "negative bone") bone = -1;
            if (failure == "past bone") bone = 2;
            if (failure == "zero bones") count = 0;
            if (failure == "overflow bones") count = int.MaxValue;
            if (failure == "missing rotations") pose.rotations = null;
            if (failure == "short rotations") pose.rotations = new Quaternion[1];
            if (failure == "zero rotation") pose.rotations[0] = new Quaternion(0, 0, 0, 0);
            Assert.That(pose.Sample(phase, bone, count), Is.EqualTo(Quaternion.identity));
        }

        [Test] public void MaximumReviewedWeightRemainsAccepted()
        {
            foreach (var band in profile.bands) band.maximumWeight = .65f;
            Assert.That(profile.Validate(accepted, out var reason), Is.True, reason);
        }

        [TestCase("role")] [TestCase("speed")] [TestCase("distance")]
        [TestCase("order")] [TestCase("null side")]
        public void ArcBandsCannotRedefineOrReorderAcceptedGaits(string failure)
        {
            if (failure == "role") Gait.role = TopDown3DLocomotionClipRole.Idle;
            if (failure == "speed") Gait.nominalSpeed += .000001f;
            if (failure == "distance") Gait.metresPerCycle += .000001f;
            if (failure == "order") (profile.bands[0], profile.bands[1]) = (profile.bands[1], profile.bands[0]);
            if (failure == "null side") profile.bands[0].right = null;
            Rejected("exactly match");
        }

        [TestCase("accepted own")] [TestCase("accepted other")]
        [TestCase("same side pair")] [TestCase("another band")]
        public void EveryArcSourceMustBeUniqueAndCannotAliasTheAcceptedForwardFamily(string failure)
        {
            if (failure == "accepted own") Gait.sourceClip = accepted.gaits[0].sourceClip;
            if (failure == "accepted other") Gait.sourceClip = accepted.gaits[4].sourceClip;
            if (failure == "same side pair") profile.bands[0].right.sourceClip = Gait.sourceClip;
            if (failure == "another band") profile.bands[2].right.sourceClip = Gait.sourceClip;
            Rejected("unique arc source");
        }

        [TestCase(false)] [TestCase(true)]
        public void MissingOrNonHumanoidSourcesAreRejected(bool createNonHumanoid)
        {
            if (createNonHumanoid) { var clip = new AnimationClip(); owned.Add(clip); Gait.sourceClip = clip; }
            else Gait.sourceClip = null;
            Rejected("looping humanoid");
        }

        [Test] public void AHumanoidSourceMustRetainItsFullLoop()
        {
            var clip = Object.Instantiate(sources[0]); owned.Add(clip);
            var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings); Gait.sourceClip = clip;
            Assert.That(clip.isHumanMotion, Is.True); Assert.That(clip.isLooping, Is.False);
            Rejected("looping humanoid");
        }

        [TestCase("contacts null")] [TestCase("contacts short")] [TestCase("contacts duplicate")]
        [TestCase("contacts NaN")] [TestCase("contacts outside")] [TestCase("map null")]
        [TestCase("map short")] [TestCase("map long")] [TestCase("map NaN")]
        [TestCase("map descending")] [TestCase("map native descending")]
        [TestCase("map start")] [TestCase("map end")] [TestCase("map wrap")]
        [TestCase("semantic contact")]
        public void NativeContactAndTimeCalibrationMustRetainTheWholeOrderedLoop(string failure)
        {
            switch (failure)
            {
                case "contacts null": Gait.nativeContacts = null; break;
                case "contacts short": Gait.nativeContacts = new float[3]; break;
                case "contacts duplicate": Gait.nativeContacts[1] = Gait.nativeContacts[0]; break;
                case "contacts NaN": Gait.nativeContacts[2] = float.NaN; break;
                case "contacts outside": Gait.nativeContacts[3] = 1; break;
                case "map null": Gait.phaseMap = null; break;
                case "map short": Gait.phaseMap = new AnimationPlaygroundNativePhase[1]; break;
                case "map long": Gait.phaseMap = new AnimationPlaygroundNativePhase[257]; break;
                case "map NaN": Gait.phaseMap[1].native = float.NaN; break;
                case "map descending": Gait.phaseMap[1].semantic = Gait.phaseMap[0].semantic; break;
                case "map native descending": Gait.phaseMap[1].native = Gait.phaseMap[0].native; break;
                case "map start": Gait.phaseMap[0].semantic = .001f; break;
                case "map end": Gait.phaseMap[^1].semantic = .999f; break;
                case "map wrap": Gait.phaseMap[^1].native -= .001f; break;
                case "semantic contact": Gait.nativeContacts[1] += .002f; break;
            }
            Rejected();
        }

        [TestCase("null")] [TestCase("short")] [TestCase("high")] [TestCase("low")]
        [TestCase("open loop")] [TestCase("nonlinear")] [TestCase("weighted")]
        public void HeightCalibrationMustStayClosedLinearAndInsideAcceptedBounds(string failure)
        {
            switch (failure)
            {
                case "null": Gait.signedHeight = null; break;
                case "short": Gait.signedHeight = new AnimationCurve(new Keyframe(0, 0)); break;
                case "high": Gait.signedHeight = AnimationCurve.Linear(0, .08001f, 1, .08001f); break;
                case "low": Gait.signedHeight = AnimationCurve.Linear(0, -.03001f, 1, -.03001f); break;
                case "open loop": Gait.signedHeight = AnimationCurve.Linear(0, 0, 1, .002f); break;
                case "nonlinear": Gait.signedHeight = new AnimationCurve(new Keyframe(0, 0), new Keyframe(.5f, .01f), new Keyframe(1, 0)); break;
                case "weighted":
                    var keys = Gait.signedHeight.keys; keys[0].weightedMode = WeightedMode.Both; Gait.signedHeight.keys = keys; break;
            }
            Rejected();
        }

        [TestCase("pure low")] [TestCase("pure high")] [TestCase("pure reversed")]
        [TestCase("blend low")] [TestCase("blend high")] [TestCase("blend reversed")] [TestCase("NaN")]
        public void DeclaredSourceRateBudgetsCannotExpandTheReviewedBounds(string failure)
        {
            switch (failure)
            {
                case "pure low": Gait.pureMinimumRate = .64f; break;
                case "pure high": Gait.pureMaximumRate = 1.36f; break;
                case "pure reversed": Gait.pureMinimumRate = 1.2f; Gait.pureMaximumRate = .8f; break;
                case "blend low": Gait.blendMinimumRate = .44f; break;
                case "blend high": Gait.blendMaximumRate = 1.66f; break;
                case "blend reversed": Gait.blendMinimumRate = 1.6f; Gait.blendMaximumRate = .5f; break;
                case "NaN": Gait.blendMaximumRate = float.NaN; break;
            }
            Rejected("source-rate bounds");
        }

        [Test] public void ActualNativeSegmentsMustFitTheirDeclaredPureRateBudget()
        {
            var c = Gait.nativeContacts;
            Gait.phaseMap = new[] { new AnimationPlaygroundNativePhase(0, c[0]),
                new AnimationPlaygroundNativePhase(.0001f, Mathf.Lerp(c[0], c[1], .1f)),
                new AnimationPlaygroundNativePhase(.25f, c[1]), new AnimationPlaygroundNativePhase(.5f, c[2]),
                new AnimationPlaygroundNativePhase(.75f, c[3]), new AnimationPlaygroundNativePhase(1, c[0] + 1) };
            Rejected("pure-gait playback");
        }

        [Test] public void MixedGaitPlaybackOutsideItsBudgetIsCulledWithoutWideningTheBudget()
        {
            var before = EditorJsonUtility.ToJson(accepted);
            Gait.blendMinimumRate = 1.6f; Gait.blendMaximumRate = 1.65f;
            Assert.That(profile.RateWeight(0, .05, Gait.nominalSpeed, false), Is.Zero);
            Assert.That(profile.Validate(accepted, out var reason), Is.True, reason);
            Assert.That(Gait.blendMinimumRate, Is.EqualTo(1.6f));
            Assert.That(Gait.blendMaximumRate, Is.EqualTo(1.65f));
            Assert.That(EditorJsonUtility.ToJson(accepted), Is.EqualTo(before));
        }

        // Control the first native phase segment while retaining whole-loop
        // endpoints. These boundary tests exercise the rate envelope directly;
        // full contact/pure-rate qualification remains covered independently.
        private void SetSourceRate(float rate, bool right, float speed)
        {
            var gait = right ? profile.bands[0].right : profile.bands[0].left;
            var derivative = (float)(rate * accepted.DistanceDerivative(speed, .05) / (gait.sourceClip.length * speed));
            gait.phaseMap = new[] { new AnimationPlaygroundNativePhase(0, 0),
                new AnimationPlaygroundNativePhase(.1f, derivative * .1f), new AnimationPlaygroundNativePhase(1, 1) };
        }

        [TestCase(.44f, 0f, false)] [TestCase(.45f, 0f, false)] [TestCase(.50f, .5f, false)]
        [TestCase(.55f, 1f, false)] [TestCase(1.05f, 1f, false)] [TestCase(1.55f, 1f, false)]
        [TestCase(1.60f, .5f, false)] [TestCase(1.65f, 0f, false)] [TestCase(1.66f, 0f, false)]
        [TestCase(.44f, 0f, true)] [TestCase(.45f, 0f, true)] [TestCase(.50f, .5f, true)]
        [TestCase(.55f, 1f, true)] [TestCase(1.05f, 1f, true)] [TestCase(1.55f, 1f, true)]
        [TestCase(1.60f, .5f, true)] [TestCase(1.65f, 0f, true)] [TestCase(1.66f, 0f, true)]
        public void RateWeightSmoothlyEntersAndLeavesTheStoredPlaybackBudget(float rate, float weight, bool right)
        {
            var speed = accepted.gaits[0].nominalSpeed;
            SetSourceRate(rate, right, speed);
            Assert.That(profile.RateWeight(0, .05, speed, right), Is.EqualTo(weight).Within(.00001f));
            Assert.That(profile.RateWeight(0, 12.05, speed, right), Is.EqualTo(weight).Within(.00001f));
        }

        [TestCase(.7f, 0f)] [TestCase(.8f, 1f)] [TestCase(1.2f, 1f)] [TestCase(1.3f, 0f)]
        public void RateWeightRespectsNarrowerStoredSourceBounds(float rate, float weight)
        {
            Gait.blendMinimumRate = .7f; Gait.blendMaximumRate = 1.3f;
            var speed = accepted.gaits[0].nominalSpeed;
            SetSourceRate(rate, false, speed);
            Assert.That(profile.RateWeight(0, .05, speed, false), Is.EqualTo(weight).Within(.00001f));
        }

        [Test] public void RateWeightSelectsTheRequestedSideWithoutAdvancingOrChangingForwardData()
        {
            var before = EditorJsonUtility.ToJson(accepted);
            var speed = accepted.gaits[0].nominalSpeed;
            SetSourceRate(.5f, false, speed); SetSourceRate(1.05f, true, speed);
            for (var i = 0; i < 8; i++)
            {
                Assert.That(profile.RateWeight(0, .05, speed, false), Is.EqualTo(.5f).Within(.00001f));
                Assert.That(profile.RateWeight(0, .05, speed, true), Is.EqualTo(1).Within(.00001f));
            }
            Assert.That(EditorJsonUtility.ToJson(accepted), Is.EqualTo(before));
        }

        [Test] public void SignificantMixedArcsRemainAvailableAndUnsafeOnesReturnToTheStraightBaseline()
        {
            var before = EditorJsonUtility.ToJson(accepted);
            var end = accepted.walkToJogBlendEndSpeed > 0 ? accepted.walkToJogBlendEndSpeed : accepted.gaits[1].nominalSpeed;
            var speed = Mathf.Lerp(accepted.gaits[0].nominalSpeed, end, .1f);
            var forwardWeight = accepted.PoseWeights(speed)[1];
            var maximum = 0f; var retainedPhase = 0d;
            for (var phase = 0; phase < 32; phase++)
            {
                var contribution = profile.RateWeight(0, phase / 32d, speed, false) * profile.bands[0].maximumWeight * forwardWeight;
                if (contribution > maximum) { maximum = contribution; retainedPhase = phase / 32d; }
            }
            Assert.That(maximum, Is.GreaterThan(accepted.significantWeight));
            Assert.That(profile.Validate(accepted, out var reason), Is.True, reason);
            Gait.blendMinimumRate = 1.6f; Gait.blendMaximumRate = 1.65f;
            Assert.That(profile.RateWeight(0, retainedPhase, speed, false), Is.Zero);
            Assert.That(profile.Validate(accepted, out reason), Is.True, reason);
            Assert.That(accepted.PoseWeights(speed)[1], Is.EqualTo(forwardWeight));
            Assert.That(EditorJsonUtility.ToJson(accepted), Is.EqualTo(before));
        }

        [TestCase("negative band")] [TestCase("past band")] [TestCase("NaN cycles")]
        [TestCase("infinite cycles")] [TestCase("negative cycles")] [TestCase("NaN speed")]
        [TestCase("infinite speed")] [TestCase("negative speed")] [TestCase("zero speed")]
        [TestCase("missing pair")] [TestCase("missing bands")] [TestCase("missing band")]
        [TestCase("missing side")] [TestCase("missing source")] [TestCase("short map")]
        [TestCase("NaN map")] [TestCase("collapsed map")] [TestCase("invalid budget")]
        [TestCase("collapsed budget")] [TestCase("missing forward data")]
        public void InvalidRateWeightCallsSafelyReturnZero(string failure)
        {
            var band = 0; var cycles = .05; var speed = accepted.gaits[0].nominalSpeed;
            switch (failure)
            {
                case "negative band": band = -1; break;
                case "past band": band = 5; break;
                case "NaN cycles": cycles = double.NaN; break;
                case "infinite cycles": cycles = double.PositiveInfinity; break;
                case "negative cycles": cycles = -.05; break;
                case "NaN speed": speed = float.NaN; break;
                case "infinite speed": speed = float.PositiveInfinity; break;
                case "negative speed": speed = -1; break;
                case "zero speed": speed = 0; break;
                case "missing pair": profile.forwardProfile = null; break;
                case "missing bands": profile.bands = null; break;
                case "missing band": profile.bands[0] = null; break;
                case "missing side": profile.bands[0].left = null; break;
                case "missing source": Gait.sourceClip = null; break;
                case "short map": Gait.phaseMap = new AnimationPlaygroundNativePhase[1]; break;
                case "NaN map": Gait.phaseMap = new[] { new AnimationPlaygroundNativePhase(0, 0), new AnimationPlaygroundNativePhase(1, float.NaN) }; break;
                case "collapsed map": Gait.phaseMap = new[] { new AnimationPlaygroundNativePhase(0, 0), new AnimationPlaygroundNativePhase(1, 0) }; break;
                case "invalid budget": Gait.blendMaximumRate = float.NaN; break;
                case "collapsed budget": Gait.blendMinimumRate = Gait.blendMaximumRate; break;
                case "missing forward data":
                    var other = Object.Instantiate(accepted); owned.Add(other); other.gaits = null; profile.forwardProfile = other; break;
            }
            Assert.That(profile.RateWeight(band, cycles, speed, false), Is.Zero);
        }
    }
}
