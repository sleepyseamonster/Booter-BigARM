using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DConservativeFlareTests
    {
        [TestCase(0f, true, 1f)]
        [TestCase(1f, false, 1f)]
        [TestCase(.0000001f, true, 0f)]
        [TestCase(.9999999f, false, 0f)]
        [TestCase(.5f, true, 0f)]
        [TestCase(.5f, false, 0f)]
        public void DirectionalSun_OnlyClearBackgroundDepthIsVisible(float depth, bool reversedZ, float expected)
            => Assert.That(TopDown3DRoundLensFlareFeature.EvaluateBackgroundDepthVisibility(depth, reversedZ, true),
                Is.EqualTo(expected));

        [Test]
        public void MissingOrInvalidDepth_IsBlockedEvenWhenFallbackLooksLikeSky()
        {
            Assert.That(TopDown3DRoundLensFlareFeature.EvaluateBackgroundDepthVisibility(1f, false, false), Is.Zero);
            Assert.That(TopDown3DRoundLensFlareFeature.EvaluateBackgroundDepthVisibility(0f, true, false), Is.Zero);
            Assert.That(TopDown3DRoundLensFlareFeature.EvaluateBackgroundDepthVisibility(float.NaN, true, true), Is.Zero);
            Assert.That(TopDown3DRoundLensFlareFeature.EvaluateBackgroundDepthVisibility(-1f, true, true), Is.Zero);
        }

        [Test]
        public void OffscreenSamples_AreUnknownAndBlockedWhileLegacyReachRemainsAvailable()
        {
            var offscreen = new Vector2(1.01f, .5f);
            Assert.That(TopDown3DRoundLensFlareFeature.EvaluateConservativeOffscreenVisibility(offscreen), Is.Zero);
            Assert.That(TopDown3DRoundLensFlareFeature.EvaluateOffscreenVisibility(offscreen, .35f), Is.GreaterThan(0f));
            Assert.That(TopDown3DRoundLensFlareFeature.EvaluateConservativeOffscreenVisibility(new Vector2(.5f, .5f)), Is.EqualTo(1f));
            Assert.That(TopDown3DRoundLensFlareFeature.EvaluateConservativeOffscreenVisibility(new Vector2(float.NaN, .5f)), Is.Zero);
        }

        [Test]
        public void SolarDiscRadius_FollowsAngularDiameterAndFovRatherThanFarClip()
        {
            var radius = TopDown3DRoundLensFlareFeature.CalculateSolarDiscViewportRadius(.53f, 48f);
            Assert.That(radius, Is.InRange(.005f, .006f));
            Assert.That(TopDown3DRoundLensFlareFeature.CalculateSolarDiscViewportRadius(.53f, 24f), Is.GreaterThan(radius));
            Assert.That(TopDown3DRoundLensFlareFeature.CalculateSolarDiscViewportRadius(1.06f, 48f),
                Is.EqualTo(radius * 2f).Within(.00001f));
        }

        [TestCase(0f, 1f)]
        [TestCase(1f, .2f)]
        public void TimedHistory_IsFrameRateIndependentWithSeparateAttackAndRelease(float previous, float current)
        {
            var single = TopDown3DRoundLensFlareFeature.EvaluateTimedVisibility(previous, current, .2f, .06f, .12f);
            var subdivided = previous;
            for (var i = 0; i < 12; i++)
                subdivided = TopDown3DRoundLensFlareFeature.EvaluateTimedVisibility(subdivided, current, .2f / 12f, .06f, .12f);
            Assert.That(subdivided, Is.EqualTo(single).Within(.00001f));
            var tau = current > previous ? .06f : .12f;
            var oneTimeConstant = TopDown3DRoundLensFlareFeature.EvaluateTimedVisibility(previous, current, tau, .06f, .12f);
            Assert.That(Mathf.Abs(oneTimeConstant - current),
                Is.EqualTo(Mathf.Abs(previous - current) * Mathf.Exp(-1f)).Within(.00001f));
        }

        [Test]
        public void CompleteOcclusion_OverridesHistoryImmediately()
            => Assert.That(TopDown3DRoundLensFlareFeature.EvaluateTimedVisibility(1f, 0f, .001f, .06f, .12f), Is.Zero);

        [Test]
        public void CameraCutsProfileSwapAndResolutionChanges_ResetImmediately()
        {
            var resolution = new Vector2Int(1920, 1080);
            Assert.That(Reset(true, 1, 1, resolution, resolution, Vector3.zero, Quaternion.identity), Is.False);
            Assert.That(Reset(false, 1, 1, resolution, resolution, Vector3.zero, Quaternion.identity), Is.True);
            Assert.That(Reset(true, 1, 2, resolution, resolution, Vector3.zero, Quaternion.identity), Is.True);
            Assert.That(Reset(true, 1, 1, resolution, new Vector2Int(1280, 720), Vector3.zero, Quaternion.identity), Is.True);
            Assert.That(Reset(true, 1, 1, resolution, resolution, Vector3.right * 3f, Quaternion.identity), Is.True);
            Assert.That(Reset(true, 1, 1, resolution, resolution, Vector3.zero, Quaternion.Euler(0f, 21f, 0f)), Is.True);
        }

        [Test]
        public void LegacyTemporalAndPartialOcclusionContracts_RemainUnchanged()
        {
            Assert.That(TopDown3DRoundLensFlareFeature.EvaluateTemporalOcclusion(1f, 0f, .82f), Is.EqualTo(.82f).Within(.00001f));
            Assert.That(TopDown3DRoundLensFlareFeature.EvaluateOcclusionResponse(0f, .08f), Is.EqualTo(.92f).Within(.00001f));
        }

        private static bool Reset(bool valid, int previousKey, int key, Vector2Int previousSize,
            Vector2Int size, Vector3 position, Quaternion rotation)
            => TopDown3DRoundLensFlareFeature.ShouldResetVisibilityHistory(valid, previousKey, key,
                previousSize, size, Vector3.zero, position, Quaternion.identity, rotation);
    }
}
