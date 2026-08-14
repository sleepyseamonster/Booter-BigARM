using System.Linq;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DVolumetricDustTests
    {
        [Test]
        public void GlobalDustHaze_IsParkedByDefaultWhileTheImplementationRemainsInstalled()
        {
            Assert.That(TopDown3DDustAtmosphere.DefaultGlobalHazeEnabled, Is.False);

            var atmosphereObject = new GameObject("Parked dust atmosphere contract");
            try
            {
                var atmosphere = atmosphereObject.AddComponent<TopDown3DDustAtmosphere>();

                Assert.That(atmosphere.GlobalHazeEnabled, Is.False);
                Assert.That(TopDown3DDustAtmosphere.Active, Is.Null);
                Assert.That(
                    atmosphere.TryGetVolumetricRenderState(null, out _),
                    Is.False);
            }
            finally
            {
                Object.DestroyImmediate(atmosphereObject);
            }
        }

        [Test]
        public void LegacyVisibilityContract_MapsToBeerLambertExtinction()
        {
            var extinctionAtIntensityOne = TopDown3DDustOptics.ConvertLegacyDensityToExtinction(
                TopDown3DDustAtmosphere.DefaultFogDensityAtIntensityOne);

            Assert.That(extinctionAtIntensityOne, Is.EqualTo(0.02456036f).Within(0.000001f));
            Assert.That(
                TopDown3DDustOptics.EvaluateHalfVisibilityDistance(
                    extinctionAtIntensityOne * TopDown3DDustAtmosphere.DefaultMinimumRegionalIntensity),
                Is.InRange(26f, 27f));
            Assert.That(
                TopDown3DDustOptics.EvaluateHalfVisibilityDistance(
                    extinctionAtIntensityOne * TopDown3DDustAtmosphere.DefaultMaximumRegionalIntensity),
                Is.InRange(17f, 18f));
        }

        [Test]
        public void Transmittance_IsHalfAtCalibratedHalfVisibilityAndMonotonic()
        {
            const float extinction = 0.02456036f;
            var halfVisibility = TopDown3DDustOptics.EvaluateHalfVisibilityDistance(extinction);

            Assert.That(TopDown3DDustOptics.EvaluateTransmittance(extinction, 0f), Is.EqualTo(1f));
            Assert.That(
                TopDown3DDustOptics.EvaluateTransmittance(extinction, halfVisibility),
                Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(
                TopDown3DDustOptics.EvaluateTransmittance(extinction, halfVisibility * 2f),
                Is.LessThan(0.5f));
        }

        [Test]
        public void DustPhaseFunction_FavorsTheLowSunFacingView()
        {
            var forward = TopDown3DDustOptics.EvaluateNormalizedHenyeyGreenstein(1f, 0.5f);
            var side = TopDown3DDustOptics.EvaluateNormalizedHenyeyGreenstein(0f, 0.5f);
            var backward = TopDown3DDustOptics.EvaluateNormalizedHenyeyGreenstein(-1f, 0.5f);

            Assert.That(forward, Is.GreaterThan(side));
            Assert.That(side, Is.GreaterThan(backward));
            Assert.That(
                TopDown3DDustOptics.EvaluateNormalizedHenyeyGreenstein(0.4f, 0f),
                Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void DustPhaseFunction_PreservesSunDirectionWithoutObscuringTheView()
        {
            var rawForward = TopDown3DDustOptics.EvaluateNormalizedHenyeyGreenstein(1f, 0.5f);
            var safeForward = TopDown3DDustOptics.EvaluateVisibilitySafePhase(
                1f,
                0.5f,
                TopDown3DDustAtmosphere.DefaultMaximumForwardPhase);
            var safeSide = TopDown3DDustOptics.EvaluateVisibilitySafePhase(
                0f,
                0.5f,
                TopDown3DDustAtmosphere.DefaultMaximumForwardPhase);
            var safeBackward = TopDown3DDustOptics.EvaluateVisibilitySafePhase(
                -1f,
                0.5f,
                TopDown3DDustAtmosphere.DefaultMaximumForwardPhase);

            Assert.That(rawForward, Is.EqualTo(6f).Within(0.0001f));
            Assert.That(safeForward, Is.LessThan(TopDown3DDustAtmosphere.DefaultMaximumForwardPhase));
            Assert.That(safeForward, Is.GreaterThan(safeSide));
            Assert.That(safeSide, Is.GreaterThan(safeBackward));
            Assert.That(safeForward, Is.InRange(1.55f, 1.60f));
        }

        [Test]
        public void DensityMapSnapping_IsStableInsideOneCell()
        {
            var first = TopDown3DDustOptics.SnapDensityMapMinimum(
                new Vector3(10.1f, 0f, -7.8f),
                TopDown3DDustAtmosphere.DensityMapExtent,
                TopDown3DDustAtmosphere.DensityMapResolution);
            var nearby = TopDown3DDustOptics.SnapDensityMapMinimum(
                new Vector3(10.8f, 3f, -7.2f),
                TopDown3DDustAtmosphere.DensityMapExtent,
                TopDown3DDustAtmosphere.DensityMapResolution);

            Assert.That(nearby, Is.EqualTo(first));
            Assert.That(Mathf.Repeat(first.x, 2f), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(Mathf.Repeat(first.y, 2f), Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void PerspectiveRenderer_HasOneCanonicalVolumetricDustFeature()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(
                ConversionBaselineValidator.ConversionRendererPath);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(
                TopDown3DPrototypeBuilder.VolumetricDustShaderPath);

            Assert.That(renderer, Is.Not.Null);
            Assert.That(shader, Is.Not.Null);
            var features = renderer.rendererFeatures
                .OfType<TopDown3DVolumetricDustFeature>()
                .ToArray();
            Assert.That(features, Has.Length.EqualTo(1));
            Assert.That(features[0].isActive, Is.True);
            Assert.That(features[0].VolumetricShader, Is.EqualTo(shader));
            Assert.That(features[0].Downsample, Is.EqualTo(2));
            Assert.That(features[0].RaymarchSteps, Is.EqualTo(16));
            Assert.That(features[0].ShadowSamples, Is.EqualTo(8));
        }

        [Test]
        public void PerpetualTwilightSun_DefaultCycleRemainsTwentyMinutes()
        {
            Assert.That(PerpetualTwilightSun.DefaultCycleDurationSeconds, Is.EqualTo(1200f));
        }

        [Test]
        public void DustPalette_ReadsAsRustRatherThanOchre()
        {
            var deep = TopDown3DDustAtmosphere.DefaultDeepRustDust;
            var bright = TopDown3DDustAtmosphere.DefaultBrightRustDust;
            var dense = TopDown3DDustAtmosphere.DefaultDenseRustDust;

            Assert.That(deep.r, Is.GreaterThan(deep.g * 3f));
            Assert.That(bright.r, Is.GreaterThan(bright.g * 2.5f));
            Assert.That(dense.r, Is.GreaterThan(dense.g * 3.5f));
            Assert.That(deep.g, Is.GreaterThan(deep.b));
            Assert.That(bright.g, Is.GreaterThan(bright.b));
            Assert.That(bright.grayscale, Is.GreaterThan(deep.grayscale));
        }

        [Test]
        public void SuspendedMotes_HaveDedicatedSunlitAndSoftVeilShaders()
        {
            var sunlitMoteShader = Shader.Find(TopDown3DDustAtmosphere.SunlitMoteShaderName);
            var softVeilShader = Shader.Find(TopDown3DDustAtmosphere.SoftVeilShaderName);

            Assert.That(sunlitMoteShader, Is.Not.Null);
            Assert.That(softVeilShader, Is.Not.Null);
            Assert.That(sunlitMoteShader, Is.Not.EqualTo(softVeilShader));
        }

        [Test]
        public void SuspendedParticles_KeepAnExplicitClearAirFloorAndIncreaseInsidePockets()
        {
            var clearMotes = TopDown3DDustAtmosphere.EvaluateParticleEmissionRate(
                TopDown3DDustAtmosphere.DefaultMoteEmissionAtIntensityOne,
                0f,
                TopDown3DDustAtmosphere.DefaultClearAirMoteEmissionMultiplier);
            var pocketMotes = TopDown3DDustAtmosphere.EvaluateParticleEmissionRate(
                TopDown3DDustAtmosphere.DefaultMoteEmissionAtIntensityOne,
                TopDown3DDustAtmosphere.DefaultMaximumRegionalIntensity,
                TopDown3DDustAtmosphere.DefaultClearAirMoteEmissionMultiplier);
            var clearVeils = TopDown3DDustAtmosphere.EvaluateParticleEmissionRate(
                TopDown3DDustAtmosphere.DefaultVeilEmissionAtIntensityOne,
                0f,
                TopDown3DDustAtmosphere.DefaultClearAirVeilEmissionMultiplier);

            Assert.That(clearMotes, Is.GreaterThan(50f));
            Assert.That(clearVeils, Is.GreaterThan(0f));
            Assert.That(pocketMotes, Is.GreaterThan(clearMotes));
        }
    }
}
