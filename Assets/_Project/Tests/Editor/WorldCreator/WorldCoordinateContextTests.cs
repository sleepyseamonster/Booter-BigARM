using System;
using System.Collections.Generic;
using System.Globalization;
using BooterBigArm.TopDown3D.WorldCreator;
using NUnit.Framework;
using UnityEngine;

namespace BooterBigArm.Tests.WorldCreator
{
    public sealed class WorldCoordinateContextTests
    {
        private const string BaselineProvinceId = "proof.non-canon.province.baseline";
        private const string InfluenceProvinceId = "proof.non-canon.province.influence";
        private const string BaselineStrataId = "proof.non-canon.strata.baseline";
        private const string InfluenceStrataId = "proof.non-canon.strata.influence";

        private readonly List<UnityEngine.Object> ownedObjects = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            for (var i = ownedObjects.Count - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(ownedObjects[i]);
            }

            ownedObjects.Clear();
        }

        [Test]
        public void SameAbsoluteAddressProducesDeterministicContext()
        {
            var sampler = CreateSampler(out _);
            var model = new NonCanonCoordinateModel();
            var world = WorldCreatorTestFactory.CreateWorld();
            var address = model.Encode(new AbsoluteWorldPosition(127.25d, -9d, 42.5d));

            Assert.That(sampler.TrySample(world, model, address, out var first, out var firstError), Is.True, firstError);
            Assert.That(sampler.TrySample(world, model, address, out var second, out var secondError), Is.True, secondError);

            Assert.That(second.ContextId, Is.EqualTo(first.ContextId));
            Assert.That(second.AbsolutePosition, Is.EqualTo(first.AbsolutePosition));
            Assert.That(second.Landscape, Is.EqualTo(first.Landscape));
            Assert.That(second.Contributions, Is.EqualTo(first.Contributions));
        }

        [Test]
        public void GradualInfluenceIsSmoothAndMonotonicBeforeDeclaredBoundary()
        {
            var sampler = CreateSampler(out var profile);
            var model = new NonCanonCoordinateModel();
            var world = WorldCreatorTestFactory.CreateWorld();
            var previousWeight = -1f;

            for (var horizontalA = -1024d; horizontalA <= 1024d; horizontalA += 32d)
            {
                var context = Sample(
                    sampler,
                    world,
                    model,
                    new AbsoluteWorldPosition(horizontalA, 0d, profile.IntentionalBoundaryHorizontalB - 1d));
                var weight = GetInfluenceWeight(context);
                Assert.That(weight, Is.GreaterThanOrEqualTo(previousWeight));
                if (previousWeight >= 0f)
                {
                    Assert.That(weight - previousWeight, Is.LessThan(0.08f));
                }

                Assert.That(context.DeclaredDiscontinuitySignal, Is.Zero);
                previousWeight = weight;
            }
        }

        [Test]
        public void DeclaredContactProducesOnlyTheConfiguredHardStep()
        {
            var sampler = CreateSampler(out var profile);
            var model = new NonCanonCoordinateModel();
            var world = WorldCreatorTestFactory.CreateWorld();
            var before = Sample(
                sampler,
                world,
                model,
                new AbsoluteWorldPosition(0d, 0d, profile.IntentionalBoundaryHorizontalB - 0.001d));
            var after = Sample(
                sampler,
                world,
                model,
                new AbsoluteWorldPosition(0d, 0d, profile.IntentionalBoundaryHorizontalB + 0.001d));

            Assert.That(
                GetInfluenceWeight(after) - GetInfluenceWeight(before),
                Is.EqualTo(profile.IntentionalBoundaryWeight).Within(0.000001f));
            Assert.That(before.DeclaredDiscontinuitySignal, Is.Zero);
            Assert.That(after.DeclaredDiscontinuitySignal, Is.EqualTo(1f));
        }

        [Test]
        public void SampledParametersRemainFiniteAndBoundedAcrossProofGrid()
        {
            var sampler = CreateSampler(out _);
            var model = new NonCanonCoordinateModel();
            var world = WorldCreatorTestFactory.CreateWorld();

            for (var horizontalA = -2048d; horizontalA <= 2048d; horizontalA += 128d)
            {
                for (var horizontalB = -512d; horizontalB <= 1024d; horizontalB += 128d)
                {
                    var context = Sample(
                        sampler,
                        world,
                        model,
                        new AbsoluteWorldPosition(horizontalA, 0d, horizontalB));
                    Assert.That(context.Landscape.TryValidate(out var error), Is.True, error);
                    AssertNormalized(context.StrataHardness);
                    AssertNormalized(context.StrataFolding);
                    AssertNormalized(context.StrataFracture);
                    AssertNormalized(context.IronOxideTendency);
                    AssertNormalized(context.PaleDepositTendency);
                    AssertNormalized(context.DarkRockTendency);
                    AssertNormalized(context.DeclaredDiscontinuitySignal);
                }
            }
        }

        [Test]
        public void CoordinateAdapterCanBeSubstitutedWithoutChangingLandscapeEvaluation()
        {
            var sampler = CreateSampler(out _);
            var firstModel = new NonCanonCoordinateModel();
            var secondModel = new AlternateNonCanonCoordinateModel();
            var world = WorldCreatorTestFactory.CreateWorld();
            var absolute = new AbsoluteWorldPosition(312.75d, 4d, 401.25d);
            var first = Sample(sampler, world, firstModel, absolute);
            var second = Sample(sampler, world, secondModel, absolute);

            Assert.That(second.AbsolutePosition, Is.EqualTo(first.AbsolutePosition));
            Assert.That(second.Landscape, Is.EqualTo(first.Landscape));
            Assert.That(second.StrataHardness, Is.EqualTo(first.StrataHardness));
            Assert.That(second.StrataFracture, Is.EqualTo(first.StrataFracture));
            Assert.That(second.Contributions, Is.EqualTo(first.Contributions));
            Assert.That(second.ContextId, Is.Not.EqualTo(first.ContextId));
        }

        [Test]
        public void SamplerRejectsAnAddressOwnedByAnotherCoordinateModel()
        {
            var sampler = CreateSampler(out _);
            var owner = new NonCanonCoordinateModel();
            var other = new AlternateNonCanonCoordinateModel();
            var address = owner.Encode(new AbsoluteWorldPosition(1d, 2d, 3d));

            Assert.That(
                sampler.TrySample(WorldCreatorTestFactory.CreateWorld(), other, address, out _, out var error),
                Is.False);
            Assert.That(error, Does.Contain("does not belong"));
        }

        private WorldCoordinateContextSampler CreateSampler(out NonCanonProofInfluenceProfile profile)
        {
            var provinces = Own(ScriptableObject.CreateInstance<GeologicProvinceCatalog>());
            provinces.ConfigureProofData(new[]
            {
                new GeologicProvinceDefinition(
                    BaselineProvinceId,
                    "[NON-CANON TEST] Baseline",
                    new WorldLandscapeParameters(
                        -0.2f, 0.2f, 0.1f, 0.3f, 0.2f, 0.1f, 0.3f,
                        0.7f, 0.4f, 0.6f, 0.2f, 0.5f, 0.1f, 0.3f)),
                new GeologicProvinceDefinition(
                    InfluenceProvinceId,
                    "[NON-CANON TEST] Influence",
                    new WorldLandscapeParameters(
                        0.4f, 0.9f, 0.8f, 0.9f, 0.85f, 0.75f, 0.95f,
                        0.25f, 0.8f, 0.2f, 0.7f, 0.9f, 0.5f, 0.8f))
            });
            var strata = Own(ScriptableObject.CreateInstance<StrataFamilyCatalog>());
            strata.ConfigureProofData(new[]
            {
                new StrataFamilyDefinition(
                    BaselineStrataId,
                    "[NON-CANON TEST] Baseline",
                    0.2f, 0.3f, 0.25f, 0.6f, 0.2f, 0.4f),
                new StrataFamilyDefinition(
                    InfluenceStrataId,
                    "[NON-CANON TEST] Influence",
                    0.9f, 0.8f, 0.95f, 0.85f, 0.7f, 0.55f)
            });
            profile = Own(ScriptableObject.CreateInstance<NonCanonProofInfluenceProfile>());
            profile.ConfigureProofData(
                "proof.non-canon.influence.test",
                1,
                BaselineProvinceId,
                BaselineStrataId,
                InfluenceProvinceId,
                InfluenceStrataId,
                -768d,
                768d,
                384d,
                0.35f);
            return new WorldCoordinateContextSampler(provinces, strata, profile);
        }

        private T Own<T>(T value) where T : UnityEngine.Object
        {
            ownedObjects.Add(value);
            return value;
        }

        private static WorldCoordinateContext Sample(
            WorldCoordinateContextSampler sampler,
            WorldIdentity world,
            IWorldCoordinateModel model,
            AbsoluteWorldPosition absolute)
        {
            var address = model.Encode(absolute);
            Assert.That(sampler.TrySample(world, model, address, out var context, out var error), Is.True, error);
            return context;
        }

        private static float GetInfluenceWeight(WorldCoordinateContext context)
        {
            for (var i = 0; i < context.Contributions.Count; i++)
            {
                if (context.Contributions[i].InfluenceId.EndsWith(".fractured", StringComparison.Ordinal))
                {
                    return context.Contributions[i].Weight;
                }
            }

            Assert.Fail("Expected the fractured proof influence contribution.");
            return 0f;
        }

        private static void AssertNormalized(float value)
        {
            Assert.That(float.IsNaN(value) || float.IsInfinity(value), Is.False);
            Assert.That(value, Is.InRange(0f, 1f));
        }

        private sealed class AlternateNonCanonCoordinateModel : IWorldCoordinateModel
        {
            public string ModelId => "test.non-canon.coordinate.alternate";
            public int ModelVersion => 1;

            public WorldCoordinateAddress Encode(AbsoluteWorldPosition absolutePosition)
            {
                var value = absolutePosition.HorizontalB.ToString("R", CultureInfo.InvariantCulture)
                    + ";" + absolutePosition.HorizontalA.ToString("R", CultureInfo.InvariantCulture)
                    + ";" + absolutePosition.Vertical.ToString("R", CultureInfo.InvariantCulture);
                return new WorldCoordinateAddress(ModelId, ModelVersion, value);
            }

            public bool TryResolve(WorldCoordinateAddress address, out AbsoluteWorldPosition absolutePosition)
            {
                absolutePosition = default;
                if (!address.IsCompatibleWith(this))
                {
                    return false;
                }

                var parts = address.CanonicalValue.Split(';');
                if (parts.Length != 3
                    || !double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var horizontalB)
                    || !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var horizontalA)
                    || !double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var vertical))
                {
                    return false;
                }

                absolutePosition = new AbsoluteWorldPosition(horizontalA, vertical, horizontalB);
                return true;
            }
        }
    }
}
