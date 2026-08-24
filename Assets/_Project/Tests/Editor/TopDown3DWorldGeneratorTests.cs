using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DWorldGeneratorTests
    {
        private const string WorldSettingsPath = "Assets/_Project/Settings/World/TopDown3DWorldSettings.asset";

        [Test]
        public void SurfaceSample_IsDeterministicAndNormalized()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(WorldSettingsPath);
            Assert.That(settings, Is.Not.Null);
            var firstGenerator = new TopDown3DWorldGenerator(settings);
            var secondGenerator = new TopDown3DWorldGenerator(settings);
            var first = firstGenerator.Sample(137.25f, -418.75f);
            var second = secondGenerator.Sample(137.25f, -418.75f);

            Assert.That(second.Height, Is.EqualTo(first.Height));
            Assert.That(second.FeatureId, Is.EqualTo(first.FeatureId));
            Assert.That(second.ToVertexColor(), Is.EqualTo(first.ToVertexColor()));
            Assert.That(first.SandWeight, Is.InRange(0f, 1f));
            Assert.That(first.GravelWeight, Is.InRange(0f, 1f));
            Assert.That(first.BedrockWeight, Is.InRange(0f, 1f));
            Assert.That(first.Normal.sqrMagnitude, Is.EqualTo(1f).Within(0.00001f));
        }

        [Test]
        public void SurfaceSignals_StayInsideDocumentedRanges()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(WorldSettingsPath);
            Assert.That(settings, Is.Not.Null);
            var generator = new TopDown3DWorldGenerator(settings);

            for (var z = -576f; z <= 576f; z += 37f)
            {
                for (var x = -576f; x <= 576f; x += 41f)
                {
                    var sample = generator.Sample(x, z);
                    Assert.That(float.IsNaN(sample.Height) || float.IsInfinity(sample.Height), Is.False);
                    Assert.That(sample.Flow, Is.InRange(0f, 1f));
                    Assert.That(sample.Talus, Is.InRange(0f, 1f));
                    Assert.That(sample.Lithology, Is.InRange(0f, 1f));
                    Assert.That(sample.Weathering, Is.InRange(0f, 1f));
                    Assert.That(sample.TraversalCorridor, Is.InRange(0f, 1f));
                }
            }
        }

        [Test]
        public void ErodedTerrace_IsContinuousAcrossStratumBoundaries()
        {
            const float stepHeight = 2.4f;
            const float boundary = stepHeight * 3f;
            const float epsilon = 0.0001f;

            var below = TopDown3DWorldGenerator.ShapeErodedTerrace(boundary - epsilon, stepHeight);
            var atBoundary = TopDown3DWorldGenerator.ShapeErodedTerrace(boundary, stepHeight);
            var above = TopDown3DWorldGenerator.ShapeErodedTerrace(boundary + epsilon, stepHeight);

            Assert.That(Mathf.Abs(atBoundary - below), Is.LessThan(0.001f));
            Assert.That(Mathf.Abs(above - atBoundary), Is.LessThan(0.001f));
        }

        [Test]
        public void ErodedTerrace_PreservesBroadGeologicalShelves()
        {
            const float stepHeight = 2.4f;
            var lowerShelf = TopDown3DWorldGenerator.ShapeErodedTerrace(stepHeight * 2.10f, stepHeight);
            var upperShelf = TopDown3DWorldGenerator.ShapeErodedTerrace(stepHeight * 2.90f, stepHeight);

            Assert.That(lowerShelf, Is.EqualTo(stepHeight * 2f).Within(0.0001f));
            Assert.That(upperShelf, Is.EqualTo(stepHeight * 3f).Within(0.0001f));
        }

        [Test]
        public void SandTrapMask_IsSparseDeterministicAndChunkIndependent()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(WorldSettingsPath);
            Assert.That(settings, Is.Not.Null);
            var coveredSamples = 0;
            var totalSamples = 0;
            var strongestSample = 0f;

            for (var z = -126f; z <= 126f; z += 3f)
            {
                for (var x = -126f; x <= 126f; x += 3f)
                {
                    var first = TopDown3DWorldGenerator.SampleSandTrapMask(
                        settings.WorldSeed,
                        settings.TerrainGenerationVersion,
                        x,
                        z);
                    var second = TopDown3DWorldGenerator.SampleSandTrapMask(
                        settings.WorldSeed,
                        settings.TerrainGenerationVersion,
                        x,
                        z);
                    Assert.That(second, Is.EqualTo(first));
                    strongestSample = Mathf.Max(strongestSample, first);
                    if (first >= 0.5f)
                    {
                        coveredSamples++;
                    }

                    totalSamples++;
                }
            }

            var coverage = coveredSamples / (float)totalSamples;
            Assert.That(
                coverage,
                Is.InRange(0.006f, 0.05f),
                $"Terrain generation {settings.TerrainGenerationVersion} produced "
                + $"a strongest sand mask of {strongestSample:0.###}.");

            const float seamX = 18f;
            const float seamZ = 7.25f;
            var left = TopDown3DWorldGenerator.SampleSandTrapMask(
                settings.WorldSeed,
                settings.TerrainGenerationVersion,
                seamX - 0.0001f,
                seamZ);
            var right = TopDown3DWorldGenerator.SampleSandTrapMask(
                settings.WorldSeed,
                settings.TerrainGenerationVersion,
                seamX + 0.0001f,
                seamZ);
            Assert.That(Mathf.Abs(right - left), Is.LessThan(0.001f));
        }

        [Test]
        public void SandTrapMask_ContainsGameplayScaleBasinsWithoutBroadSandFields()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(WorldSettingsPath);
            Assert.That(settings, Is.Not.Null);
            var longestRun = 0;

            for (var z = -180; z <= 180; z++)
            {
                var currentRun = 0;
                for (var x = -180; x <= 180; x++)
                {
                    var mask = TopDown3DWorldGenerator.SampleSandTrapMask(
                        settings.WorldSeed,
                        settings.TerrainGenerationVersion,
                        x,
                        z);
                    currentRun = mask >= 0.45f ? currentRun + 1 : 0;
                    longestRun = Mathf.Max(longestRun, currentRun);
                }
            }

            for (var x = -180; x <= 180; x++)
            {
                var currentRun = 0;
                for (var z = -180; z <= 180; z++)
                {
                    var mask = TopDown3DWorldGenerator.SampleSandTrapMask(
                        settings.WorldSeed,
                        settings.TerrainGenerationVersion,
                        x,
                        z);
                    currentRun = mask >= 0.45f ? currentRun + 1 : 0;
                    longestRun = Mathf.Max(longestRun, currentRun);
                }
            }

            Assert.That(longestRun, Is.InRange(12, 24));
        }

        [Test]
        public void SandTrapMask_StartAreaContainsVisibleBasinOutsideClearSpawn()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(WorldSettingsPath);
            Assert.That(settings, Is.Not.Null);
            var strongestStartAreaMask = 0f;

            for (var z = -35; z <= 35; z++)
            {
                for (var x = -35; x <= 35; x++)
                {
                    var distance = Mathf.Sqrt(x * x + z * z);
                    if (distance < settings.ClearSpawnRadius + 4f)
                    {
                        continue;
                    }

                    strongestStartAreaMask = Mathf.Max(
                        strongestStartAreaMask,
                        TopDown3DWorldGenerator.SampleSandTrapMask(
                            settings.WorldSeed,
                            settings.TerrainGenerationVersion,
                            x,
                            z));
                }
            }

            Assert.That(strongestStartAreaMask, Is.GreaterThanOrEqualTo(0.9f));
        }

        [Test]
        public void AdjacentChunkMeshes_ShareExactSurfaceWeights()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(WorldSettingsPath);
            Assert.That(settings, Is.Not.Null);
            var generator = new TopDown3DWorldGenerator(settings);
            var left = TopDown3DChunkMeshBuilder.BuildData(settings, generator, Vector2Int.zero);
            var right = TopDown3DChunkMeshBuilder.BuildData(settings, generator, Vector2Int.right);
            var verticesPerAxis = settings.QuadsPerAxis + 1;

            for (var z = 0; z < verticesPerAxis; z++)
            {
                var leftColor = left.Colors[z * verticesPerAxis + settings.QuadsPerAxis];
                var rightColor = right.Colors[z * verticesPerAxis];
                Assert.That(rightColor, Is.EqualTo(leftColor), $"Surface-weight seam at row {z}.");
            }
        }

        [Test]
        public void RegionalFeatureIdentity_IsStableAwayFromRegionBoundaries()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(WorldSettingsPath);
            Assert.That(settings, Is.Not.Null);
            var generator = new TopDown3DWorldGenerator(settings);
            var regionSize = generator.RegionSize;
            var first = generator.Sample(regionSize * 2.2f, regionSize * -1.3f);
            var second = generator.Sample(regionSize * 2.35f, regionSize * -1.15f);

            Assert.That(second.FeatureId, Is.EqualTo(first.FeatureId));
        }
    }
}
