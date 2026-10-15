using System;
using System.Linq;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class AnimationPlaygroundTurnProfileTests
    {
        private const string Candidate = "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/ForwardTurnCandidate.asset";
        private static AnimationPlaygroundTurnSource Source(int sign) => new() {
            direction = sign < 0 ? AnimationPlaygroundTurnDirection.Left : AnimationPlaygroundTurnDirection.Right,
            duration = 1.5f,
            samples = new[] { new AnimationPlaygroundTurnSample(0, 0), new AnimationPlaygroundTurnSample(.25f, 30 * sign),
                new AnimationPlaygroundTurnSample(.9f, 120 * sign), new AnimationPlaygroundTurnSample(1.5f, 180 * sign) }
        };

        [TestCase(-1)]
        [TestCase(1)]
        public void SignedYawAndNativeTimeRoundTripWithoutRetiming(int sign)
        {
            var source = Source(sign);
            for (var i = 0; i <= 100; i++)
            {
                var seconds = 1.5f * i / 100;
                var angle = source.DegreesAtSeconds(seconds);
                Assert.That(angle * sign, Is.InRange(0f, 180f));
                Assert.That(source.SecondsAtDegrees(angle), Is.EqualTo(seconds).Within(.000001f));
            }
            Assert.That(source.SecondsAtDegrees(30 * sign), Is.EqualTo(.25f));
            Assert.That(source.DegreesAtSeconds(.9f), Is.EqualTo(120 * sign));
            Assert.That(source.DegreesAtSeconds(-1), Is.Zero);
            Assert.That(source.DegreesAtSeconds(100), Is.EqualTo(180 * sign));
            Assert.That(source.SecondsAtDegrees(360 * sign), Is.EqualTo(1.5f));
        }

        [TestCase(-1)]
        [TestCase(1)]
        public void WrongDirectionAndNonfiniteQueriesFailVisibly(int sign)
        {
            var source = Source(sign);
            Assert.Throws<ArgumentOutOfRangeException>(() => source.SecondsAtDegrees(-sign));
            Assert.Throws<ArgumentOutOfRangeException>(() => source.SecondsAtDegrees(float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => source.SecondsAtDegrees(float.PositiveInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(() => source.DegreesAtSeconds(float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => source.DegreesAtSeconds(float.NegativeInfinity));
            source.samples = Array.Empty<AnimationPlaygroundTurnSample>();
            Assert.Throws<InvalidOperationException>(() => source.DegreesAtSeconds(0));
        }

        [Test]
        public void ASourceYawPauseRetainsItsEarliestTime()
        {
            var source = Source(1);
            source.samples = new[] { new AnimationPlaygroundTurnSample(0, 0), new AnimationPlaygroundTurnSample(.4f, 45),
                new AnimationPlaygroundTurnSample(.8f, 45), new AnimationPlaygroundTurnSample(1.5f, 180) };
            Assert.That(source.DegreesAtSeconds(.6f), Is.EqualTo(45));
            Assert.That(source.SecondsAtDegrees(45), Is.EqualTo(.4f));
            Assert.That(source.SecondsAtDegrees(46), Is.GreaterThan(.8f));
        }

        [Test, Category("PrivateAnimationQualification")]
        public void NamedEvaluatedSourceDataRemainsUnqualifiedAndUnbound()
        {
            var profile = AssetDatabase.LoadAssetAtPath<AnimationPlaygroundTurnProfile>(Candidate);
            Assert.That(profile, Is.Not.Null, "Materialize the private candidate from sealed source/contact observations first.");
            Assert.That(profile.qualified, Is.False);
            Assert.That(profile.Validate(profile.targetModel, out var error, false), Is.True, error);
            Assert.That(profile.Validate(profile.targetModel, out error), Is.False);
            Assert.That(error, Does.Contain("awaiting qualification"));
            Assert.That(profile.left.clip.name, Is.EqualTo("RapidTurningLeft"));
            Assert.That(profile.right.clip.name, Is.EqualTo("RapidTurningRight_Mirror"));
            Assert.That(profile.left.samples.Length, Is.GreaterThanOrEqualTo(180));
            Assert.That(profile.right.samples.Length, Is.EqualTo(profile.left.samples.Length));
            Assert.That(profile.left.DegreesAtSeconds(profile.left.duration), Is.EqualTo(-180).Within(.001f));
            Assert.That(profile.right.DegreesAtSeconds(profile.right.duration), Is.EqualTo(180).Within(.001f));
            Assert.That(AssetDatabase.GetDependencies(TopDown3DAnimationPlaygroundSession.ScenePath, true), Does.Not.Contain(Candidate));
        }

        [TestCase("duplicate-time")]
        [TestCase("nonfinite-time")]
        [TestCase("backwards-yaw")]
        [TestCase("wrong-sign")]
        [TestCase("partial-source")]
        [TestCase("discarded-root-travel")]
        [TestCase("discarded-root-height")]
        [TestCase("duration-mismatch")]
        [TestCase("missing-clip")]
        [Category("PrivateAnimationQualification")]
        public void InvalidSourceCannotPassTheExplicitOfflineRoute(string mutation)
        {
            var original = AssetDatabase.LoadAssetAtPath<AnimationPlaygroundTurnProfile>(Candidate);
            Assert.That(original, Is.Not.Null);
            var clone = Object.Instantiate(original);
            try
            {
                var source = clone.left;
                switch (mutation)
                {
                    case "duplicate-time": source.samples[1].seconds = 0; break;
                    case "nonfinite-time": source.samples[1].seconds = float.NaN; break;
                    case "backwards-yaw": source.samples[2].degrees = source.samples[1].degrees + 1; break;
                    case "wrong-sign": source.samples[1].degrees = 1; break;
                    case "partial-source": source.samples[^1].degrees = -179; break;
                    case "discarded-root-travel": source.maximumPlanarRootExcursion = .002f; break;
                    case "discarded-root-height": source.maximumVerticalRootExcursion = .002f; break;
                    case "duration-mismatch": source.duration += .1f; break;
                    case "missing-clip": source.clip = null; break;
                }
                Assert.That(clone.Validate(clone.targetModel, out var error, false), Is.False);
                Assert.That(error, Is.Not.Null.And.Not.Empty);
                Assert.That(original.Validate(original.targetModel, out error, false), Is.True, "Mutation must not leak into source data.");
            }
            finally { Object.DestroyImmediate(clone); }
        }

        [TestCase("receipt")]
        [TestCase("calibration")]
        [TestCase("duplicate-clip")]
        [TestCase("swapped-direction")]
        [TestCase("swapped-clips")]
        [TestCase("sampled-name")]
        [Category("PrivateAnimationQualification")]
        public void PairingAndProvenanceAreRequiredEvenOffline(string mutation)
        {
            var original = AssetDatabase.LoadAssetAtPath<AnimationPlaygroundTurnProfile>(Candidate);
            Assert.That(original, Is.Not.Null);
            var clone = Object.Instantiate(original);
            try
            {
                switch (mutation)
                {
                    case "receipt": clone.sourceReceiptSha256 = new string('x', 64); break;
                    case "calibration": clone.visualScale = 0; break;
                    case "duplicate-clip": clone.right.clip = clone.left.clip; break;
                    case "swapped-direction": clone.right.direction = AnimationPlaygroundTurnDirection.Left; break;
                    case "swapped-clips": (clone.left.clip, clone.right.clip) = (clone.right.clip, clone.left.clip); break;
                    case "sampled-name": clone.left.sampledClipName = "Unobserved clip"; break;
                }
                Assert.That(clone.Validate(clone.targetModel, out _, false), Is.False);
            }
            finally { Object.DestroyImmediate(clone); }
        }
    }
}
