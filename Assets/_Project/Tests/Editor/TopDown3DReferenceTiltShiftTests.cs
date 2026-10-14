using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DReferenceTiltShiftTests
    {
        [TestCase(1280, 720)]
        [TestCase(1920, 1080)]
        [TestCase(3840, 2160)]
        [TestCase(3440, 1440)]
        public void TapSteps_HaveEqualHorizontalAndVerticalPixelReach(int width, int height)
        {
            var step = TopDown3DTiltShiftFeature.CalculateReferenceBlurStep(7f, 1080f, width / (float)height);
            Assert.That(step.x * width, Is.EqualTo(step.y * height).Within(.0001f));
            Assert.That(step.y * height * 4f, Is.EqualTo(28f * height / 1080f).Within(.0001f),
                "Radius is the tap step; the outermost kernel sample is four steps away.");
        }

        [Test]
        public void Downsampling_ChangesSampleGridButNotSourcePixelReach()
        {
            var step = TopDown3DTiltShiftFeature.CalculateReferenceBlurStep(7f, 1080f, 16f / 9f);
            for (var downsample = 1; downsample <= 4; downsample++)
            {
                var horizontalIntermediatePixels = step.x * (1920f / downsample);
                var verticalIntermediatePixels = step.y * (1080f / downsample);
                Assert.That(horizontalIntermediatePixels * downsample, Is.EqualTo(7f).Within(.0001f));
                Assert.That(verticalIntermediatePixels * downsample, Is.EqualTo(7f).Within(.0001f));
            }
        }

        [Test]
        public void ZeroAngle_PreservesLegacyBandWeightAtEveryScreenRow()
        {
            for (var row = 0; row <= 100; row++)
            {
                var y = row / 100f;
                var legacy = TopDown3DTiltShiftFeature.EvaluateBlurWeight(y, .43f, .3f, .28f);
                var corrected = TopDown3DTiltShiftFeature.EvaluateReferenceBlurWeight(
                    new Vector2(.12f, y), 21f / 9f, 0f, .43f, .3f, .28f);
                Assert.That(corrected, Is.EqualTo(legacy).Within(.00001f));
            }
        }

        [Test]
        public void RotatedBand_PreservesPhysicalAngleAcrossAspectRatios()
        {
            foreach (var aspect in new[] { 1f, 16f / 9f, 21f / 9f })
            {
                // Equal displacement in screen-height units lies on a 45 degree band.
                var diagonal = new Vector2(.5f + .3f / aspect, .8f);
                Assert.That(TopDown3DTiltShiftFeature.EvaluateReferenceBlurWeight(
                    diagonal, aspect, 45f, .5f, .1f, .2f), Is.EqualTo(0f).Within(.00001f));
                var perpendicular = new Vector2(.5f - .3f / aspect, .8f);
                Assert.That(TopDown3DTiltShiftFeature.EvaluateReferenceBlurWeight(
                    perpendicular, aspect, 45f, .5f, .1f, .2f), Is.EqualTo(1f).Within(.00001f));
            }
        }

        [Test]
        public void VerticalBand_HasSharpCenterAndFeatheredSideEdges()
        {
            Assert.That(TopDown3DTiltShiftFeature.EvaluateReferenceBlurWeight(
                new Vector2(.5f, .1f), 1f, 90f, .5f, .3f, .2f), Is.EqualTo(0f));
            Assert.That(TopDown3DTiltShiftFeature.EvaluateReferenceBlurWeight(
                new Vector2(.75f, .5f), 1f, 90f, .5f, .3f, .2f), Is.EqualTo(.5f).Within(.00001f));
            Assert.That(TopDown3DTiltShiftFeature.EvaluateReferenceBlurWeight(
                new Vector2(1f, .5f), 1f, 90f, .5f, .3f, .2f), Is.EqualTo(1f));
        }

        [Test]
        public void TapStepBounds_AllowExactZeroAndClampAuthoredExtremes()
        {
            Assert.That(TopDown3DTiltShiftFeature.CalculateReferenceBlurStep(0f, 1080f, 1f), Is.EqualTo(Vector2.zero));
            Assert.That(TopDown3DTiltShiftFeature.CalculateReferenceBlurStep(-10f, 1080f, 1f), Is.EqualTo(Vector2.zero));
            var maximum = TopDown3DTiltShiftFeature.CalculateReferenceBlurStep(100f, 1f, 1f);
            Assert.That(maximum.x, Is.EqualTo(24f / 360f).Within(.00001f));
            Assert.That(maximum.y, Is.EqualTo(maximum.x));
        }

        [Test]
        public void ExistingProfilesAndFeatureDefaults_RemainLegacyUntilOptedIn()
        {
            var profile = ScriptableObject.CreateInstance<TopDown3DAtmosphereStudyProfile>();
            var feature = ScriptableObject.CreateInstance<TopDown3DTiltShiftFeature>();
            try
            {
                Assert.That(profile.useReferenceResolutionTiltShift, Is.False);
                Assert.That(feature.Downsample, Is.EqualTo(2));
                Assert.That(feature.BlurRadius, Is.EqualTo(7f));
                Assert.That(feature.FocusCenter, Is.EqualTo(.5f));
                Assert.That(feature.SharpBandWidth, Is.EqualTo(.3f));
                Assert.That(feature.FeatherWidth, Is.EqualTo(.28f));
            }
            finally
            {
                Object.DestroyImmediate(feature);
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void MasterOffOrNativeDof_SuppressesArtisticBandEvenWhenBothAuthored()
        {
            var profile = ScriptableObject.CreateInstance<TopDown3DAtmosphereStudyProfile>();
            var optics = ScriptableObject.CreateInstance<TopDown3DCampOpticsProfile>();
            try
            {
                profile.tiltShiftEnabled = true;
                profile.nativeOptics = optics;
                Assert.That(TopDown3DTiltShiftFeature.SuppressArtisticBlur(profile), Is.False);
                optics.opticsEnabled = false;
                Assert.That(TopDown3DTiltShiftFeature.SuppressArtisticBlur(profile), Is.True);
                optics.opticsEnabled = true;
                optics.depthMode = DepthOfFieldMode.Gaussian;
                Assert.That(TopDown3DTiltShiftFeature.SuppressArtisticBlur(profile), Is.True);
                optics.depthMode = DepthOfFieldMode.Bokeh;
                Assert.That(TopDown3DTiltShiftFeature.SuppressArtisticBlur(profile), Is.True);
                profile.nativeOptics = null;
                Assert.That(TopDown3DTiltShiftFeature.SuppressArtisticBlur(profile), Is.False);
            }
            finally { Object.DestroyImmediate(profile); Object.DestroyImmediate(optics); }
        }
    }
}
