using System.Linq;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DAnamorphicStreakTests
    {
        [Test]
        public void HighlightWeight_RejectsDimPixelsAndFeathersBrightPixels()
        {
            var dim = TopDown3DAnamorphicStreakFeature.EvaluateHighlightWeight(0.5f, 0.88f);
            var feathered = TopDown3DAnamorphicStreakFeature.EvaluateHighlightWeight(0.94f, 0.88f);
            var bright = TopDown3DAnamorphicStreakFeature.EvaluateHighlightWeight(1.1f, 0.88f);

            Assert.That(dim, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(feathered, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(bright, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void PerspectiveRenderer_HasOneAdjustableSceneViewAnamorphicStreakFeature()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(
                ConversionBaselineValidator.ConversionRendererPath);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(
                TopDown3DPrototypeBuilder.AnamorphicStreakShaderPath);

            Assert.That(renderer, Is.Not.Null);
            Assert.That(shader, Is.Not.Null);
            var features = renderer.rendererFeatures
                .OfType<TopDown3DAnamorphicStreakFeature>()
                .ToArray();
            Assert.That(features, Has.Length.EqualTo(1));
            Assert.That(features[0].isActive, Is.True);
            Assert.That(features[0].StreakEnabled, Is.True);
            Assert.That(features[0].PreviewInSceneView, Is.True);
            Assert.That(features[0].StreakShader, Is.EqualTo(shader));
            Assert.That(features[0].Downsample, Is.InRange(1, 4));
            Assert.That(features[0].Intensity, Is.InRange(0f, 2f));
            Assert.That(features[0].Length, Is.InRange(0f, 1f));
            Assert.That(features[0].Threshold, Is.InRange(0f, 4f));
            Assert.That(features[0].Orientation, Is.InRange(-180f, 180f));
            Assert.That(features[0].ChromaticSeparation, Is.InRange(0f, 0.15f));
            Assert.That(
                TopDown3DAnamorphicStreakFeature.CanonicalInjectionPoint,
                Is.EqualTo(RenderPassEvent.BeforeRenderingPostProcessing));
        }
    }
}
