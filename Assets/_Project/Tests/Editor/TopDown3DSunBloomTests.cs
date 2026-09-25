using System.Reflection;
using System.Linq;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DSunBloomTests
    {
        [Test]
        public void HighlightWeight_FeathersThresholdAndPreservesHdrEnergy()
        {
            Assert.That(TopDown3DSunBloomFeature.EvaluateHighlightWeight(0.4f, 0.82f, 0.48f), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(TopDown3DSunBloomFeature.EvaluateHighlightWeight(0.82f, 0.82f, 0.48f), Is.GreaterThan(0f));
            Assert.That(TopDown3DSunBloomFeature.EvaluateHighlightWeight(2f, 0.82f, 0.48f), Is.GreaterThan(0.5f));
        }

        [Test]
        public void ScaleWeight_SeparatesTightMediumAndBroadBloom()
        {
            Assert.That(TopDown3DSunBloomFeature.EvaluateScaleWeight(0f, 0.9f, 0.72f, 0.48f),
                Is.EqualTo(0.9f).Within(0.0001f));
            Assert.That(TopDown3DSunBloomFeature.EvaluateScaleWeight(0.5f, 0.9f, 0.72f, 0.48f),
                Is.EqualTo(0.72f).Within(0.0001f));
            Assert.That(TopDown3DSunBloomFeature.EvaluateScaleWeight(1f, 0.9f, 0.72f, 0.48f),
                Is.EqualTo(0.48f).Within(0.0001f));
        }

        [Test]
        public void DustAtmosphere_DoesNotAddACompetingStockBloom()
        {
            var gameObject = new GameObject("Bloom ownership contract");
            var profile = default(VolumeProfile);
            try
            {
                var volume = gameObject.AddComponent<Volume>();
                var atmosphere = gameObject.AddComponent<TopDown3DDustAtmosphere>();
                var ensurePostProcessing = typeof(TopDown3DDustAtmosphere).GetMethod(
                    "EnsurePostProcessing",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.That(ensurePostProcessing, Is.Not.Null);
                ensurePostProcessing.Invoke(atmosphere, null);
                profile = volume.sharedProfile;

                Assert.That(profile, Is.Not.Null);
                Assert.That(profile.TryGet(out Bloom _), Is.False,
                    "The renderer-owned multi-scale bloom must be the sole TopDown3D bloom owner.");
            }
            finally
            {
                if (profile != null)
                {
                    Object.DestroyImmediate(profile);
                }

                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void PerspectiveRenderer_HasBloomAfterRoundFlare()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(
                ConversionBaselineValidator.ConversionRendererPath);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(TopDown3DPrototypeBuilder.SunBloomShaderPath);
            Assert.That(renderer, Is.Not.Null);
            Assert.That(shader, Is.Not.Null);

            var bloom = renderer.rendererFeatures.OfType<TopDown3DSunBloomFeature>().Single();
            var flareIndex = renderer.rendererFeatures.FindIndex(feature => feature is TopDown3DRoundLensFlareFeature);
            var bloomIndex = renderer.rendererFeatures.FindIndex(feature => feature is TopDown3DSunBloomFeature);
            Assert.That(bloomIndex, Is.GreaterThan(flareIndex));
            Assert.That(bloom.BloomShader, Is.EqualTo(shader));
            Assert.That(bloom.BloomEnabled, Is.True);
            Assert.That(bloom.PreviewInSceneView, Is.True);
            Assert.That(bloom.Downsample, Is.InRange(1, 4));
            Assert.That(bloom.PyramidLevels, Is.InRange(3, 7));
            Assert.That(bloom.Intensity, Is.InRange(0f, 2f));
            Assert.That(bloom.Threshold, Is.InRange(0f, 4f));
            Assert.That(bloom.SoftKnee, Is.InRange(0f, 1f));
            Assert.That(bloom.Scatter, Is.InRange(0f, 1f));
            Assert.That(bloom.Clamp, Is.InRange(1f, 32f));
            Assert.That(bloom.TightWeight, Is.InRange(0f, 2f));
            Assert.That(bloom.MediumWeight, Is.InRange(0f, 2f));
            Assert.That(bloom.BroadWeight, Is.InRange(0f, 2f));
            Assert.That(bloom.SunAureoleIntensity, Is.InRange(0f, 2f));
            Assert.That(bloom.SunAureoleRadius, Is.InRange(0.02f, 0.5f));
        }
    }
}
