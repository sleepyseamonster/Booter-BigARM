using System.Linq;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DTiltShiftTests
    {
        [Test]
        public void BlurWeight_KeepsThePlayBandSharpAndBlursTheFrameEdges()
        {
            var center = TopDown3DTiltShiftFeature.EvaluateBlurWeight(0.5f, 0.5f, 0.3f, 0.18f);
            var featherMidpoint = TopDown3DTiltShiftFeature.EvaluateBlurWeight(0.74f, 0.5f, 0.3f, 0.18f);
            var horizon = TopDown3DTiltShiftFeature.EvaluateBlurWeight(1f, 0.5f, 0.3f, 0.18f);
            var foreground = TopDown3DTiltShiftFeature.EvaluateBlurWeight(0f, 0.5f, 0.3f, 0.18f);

            Assert.That(center, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(featherMidpoint, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(horizon, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(foreground, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void BlurWeight_WiderFeatherCreatesASofterGradient()
        {
            const float sampleY = 0.75f;
            var narrowFeather = TopDown3DTiltShiftFeature.EvaluateBlurWeight(
                sampleY,
                0.5f,
                0.3f,
                0.1f);
            var wideFeather = TopDown3DTiltShiftFeature.EvaluateBlurWeight(
                sampleY,
                0.5f,
                0.3f,
                0.3f);

            Assert.That(narrowFeather, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(wideFeather, Is.LessThan(narrowFeather));
            Assert.That(wideFeather, Is.GreaterThan(0f));
        }

        [Test]
        public void PerspectiveRenderer_HasOneAdjustableSceneViewTiltShiftFeature()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(
                ConversionBaselineValidator.ConversionRendererPath);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(
                TopDown3DPrototypeBuilder.TiltShiftShaderPath);

            Assert.That(renderer, Is.Not.Null);
            Assert.That(shader, Is.Not.Null);
            var features = renderer.rendererFeatures
                .OfType<TopDown3DTiltShiftFeature>()
                .ToArray();
            Assert.That(features, Has.Length.EqualTo(1));
            Assert.That(features[0].isActive, Is.True);
            Assert.That(features[0].PreviewInSceneView, Is.True);
            Assert.That(features[0].TiltShiftShader, Is.EqualTo(shader));
            Assert.That(features[0].Downsample, Is.InRange(1, 4));
            Assert.That(features[0].FocusCenter, Is.InRange(0f, 1f));
            Assert.That(features[0].SharpBandWidth, Is.InRange(0.05f, 0.8f));
            Assert.That(features[0].FeatherWidth, Is.InRange(0.01f, 0.5f));
            Assert.That(features[0].BlurRadius, Is.InRange(0.5f, 12f));
            Assert.That(
                TopDown3DTiltShiftFeature.CanonicalInjectionPoint,
                Is.EqualTo(RenderPassEvent.BeforeRenderingPostProcessing));
        }
    }
}
