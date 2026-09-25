using System.Linq;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DRoundLensFlareTests
    {
        [Test]
        public void OffscreenVisibility_ReachesPastTheFrameThenFades()
        {
            var onScreen = TopDown3DRoundLensFlareFeature.EvaluateOffscreenVisibility(
                new Vector2(0.5f, 0.5f),
                0.35f);
            var nearEdge = TopDown3DRoundLensFlareFeature.EvaluateOffscreenVisibility(
                new Vector2(1.175f, 0.5f),
                0.35f);
            var beyondReach = TopDown3DRoundLensFlareFeature.EvaluateOffscreenVisibility(
                new Vector2(1.5f, 0.5f),
                0.35f);

            Assert.That(onScreen, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(nearEdge, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(beyondReach, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void OcclusionResponse_CanHideTheCoreWithoutSwitchingOffTheAureole()
        {
            var hiddenCore = TopDown3DRoundLensFlareFeature.EvaluateOcclusionResponse(0f, 1f);
            var persistentAureole = TopDown3DRoundLensFlareFeature.EvaluateOcclusionResponse(0f, 0.08f);
            var partialGhosts = TopDown3DRoundLensFlareFeature.EvaluateOcclusionResponse(0f, 0.42f);

            Assert.That(hiddenCore, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(persistentAureole, Is.EqualTo(0.92f).Within(0.0001f));
            Assert.That(partialGhosts, Is.EqualTo(0.58f).Within(0.0001f));
        }

        [Test]
        public void TemporalOcclusion_SmoothsAbruptVisibilityChanges()
        {
            var smoothed = TopDown3DRoundLensFlareFeature.EvaluateTemporalOcclusion(1f, 0f, 0.82f);

            Assert.That(smoothed, Is.EqualTo(0.82f).Within(0.0001f));
            Assert.That(smoothed, Is.GreaterThan(0f));
            Assert.That(smoothed, Is.LessThan(1f));
        }

        [Test]
        public void PerspectiveRenderer_HasOneAdjustableSceneViewRoundLensFlareFeature()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(
                ConversionBaselineValidator.ConversionRendererPath);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(
                TopDown3DPrototypeBuilder.RoundLensFlareShaderPath);
            var primarySprite = AssetDatabase.LoadAssetAtPath<Texture2D>(
                TopDown3DPrototypeBuilder.RoundLensFlarePrimarySpritePath);
            var ringSprite = AssetDatabase.LoadAssetAtPath<Texture2D>(
                TopDown3DPrototypeBuilder.RoundLensFlareGhostRingSpritePath);
            var apertureSprite = AssetDatabase.LoadAssetAtPath<Texture2D>(
                TopDown3DPrototypeBuilder.RoundLensFlareApertureGhostSpritePath);

            Assert.That(renderer, Is.Not.Null);
            Assert.That(shader, Is.Not.Null);
            Assert.That(primarySprite, Is.Not.Null);
            Assert.That(ringSprite, Is.Not.Null);
            Assert.That(apertureSprite, Is.Not.Null);
            var features = renderer.rendererFeatures
                .OfType<TopDown3DRoundLensFlareFeature>()
                .ToArray();
            Assert.That(features, Has.Length.EqualTo(1));
            Assert.That(features[0].isActive, Is.True);
            Assert.That(features[0].FlareEnabled, Is.True);
            Assert.That(features[0].PreviewInSceneView, Is.True);
            Assert.That(features[0].FlareShader, Is.EqualTo(shader));
            Assert.That(features[0].PrimaryFlareSprite, Is.EqualTo(primarySprite));
            Assert.That(features[0].GhostRingSprite, Is.EqualTo(ringSprite));
            Assert.That(features[0].ApertureGhostSprite, Is.EqualTo(apertureSprite));
            Assert.That(features[0].Intensity, Is.InRange(0f, 2f));
            Assert.That(features[0].Radius, Is.InRange(0.02f, 0.75f));
            Assert.That(features[0].Anisotropy, Is.InRange(0.4f, 2.5f));
            Assert.That(features[0].GhostReach, Is.InRange(0f, 3f));
            Assert.That(features[0].EdgeReach, Is.InRange(0f, 1f));
            Assert.That(features[0].HaloThickness, Is.InRange(0.03f, 0.5f));
            Assert.That(features[0].HdrEnergy, Is.InRange(0.5f, 6f));
            Assert.That(features[0].OcclusionRadius, Is.InRange(0.005f, 0.25f));
            Assert.That(features[0].CoreOcclusion, Is.InRange(0f, 1f));
            Assert.That(features[0].AureoleOcclusion, Is.InRange(0f, 1f));
            Assert.That(features[0].GhostOcclusion, Is.InRange(0f, 1f));
            Assert.That(features[0].OcclusionStability, Is.InRange(0f, 0.95f));
            Assert.That(features[0].CoreOcclusion, Is.GreaterThan(features[0].AureoleOcclusion));
            Assert.That(
                TopDown3DRoundLensFlareFeature.CanonicalInjectionPoint,
                Is.EqualTo(RenderPassEvent.BeforeRenderingPostProcessing));
        }

        [TestCase(TopDown3DPrototypeBuilder.RoundLensFlarePrimarySpritePath)]
        [TestCase(TopDown3DPrototypeBuilder.RoundLensFlareGhostRingSpritePath)]
        [TestCase(TopDown3DPrototypeBuilder.RoundLensFlareApertureGhostSpritePath)]
        public void FlareTexture_PreservesSmoothTransparentEdges(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.alphaIsTransparency, Is.True);
            Assert.That(importer.mipmapEnabled, Is.True);
            Assert.That(importer.borderMipmap, Is.True);
            Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
            Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
        }
    }
}
