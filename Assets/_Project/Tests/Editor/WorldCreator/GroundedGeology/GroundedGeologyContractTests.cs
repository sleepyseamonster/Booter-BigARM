using System;
using System.Linq;
using BooterBigArm.TopDown3D.WorldCreator;
using BooterBigArm.TopDown3D.WorldCreator.GroundedGeology;
using NUnit.Framework;
using UnityEngine;

namespace BooterBigArm.Tests.WorldCreator.GroundedGeology
{
    public sealed class GroundedGeologyContractTests
    {
        [Test]
        public void Recipe_CopiesAndCanonicallySortsStructuralElements()
        {
            var second = CreateElement("b", 2d);
            var first = CreateElement("a", 1d);
            var source = new[] { second, first };
            var recipe = CreateRecipe(source);

            source[0] = CreateElement("changed", 8d);

            Assert.That(recipe.StructuralElements.Select(item => item.StableKey), Is.EqualTo(new[] { "a", "b" }));
            Assert.That(recipe.StructuralElements[1], Is.EqualTo(second));
            Assert.That(recipe.IsTemporaryNonCanon, Is.True);
        }

        [Test]
        public void Recipe_RejectsInvalidTemporaryIdentityAndDuplicateElements()
        {
            Assert.Throws<ArgumentException>(() => CreateRecipe(
                new[] { CreateElement("same", 1d), CreateElement("same", 2d) }));

            Assert.Throws<ArgumentException>(() => new GroundedGeologyRecipe(
                "production-looking-id",
                1,
                GroundedGeologyFixtureKind.TemporaryExistingRock,
                true,
                new AbsoluteWorldPosition(0d, 0d, 0d),
                GroundedGeologyRotation.Identity,
                new GroundedGeologyBoundsMeters(
                    new GroundedGeologyVector3Meters(0d, 0d, 0d),
                    new GroundedGeologyVector3Meters(1d, 1d, 1d)),
                GroundedGeologyAlgorithmVersions.Initial,
                1UL,
                true,
                0.1d,
                0d,
                0d,
                1d,
                new WorldFeatureId(1UL, 2UL),
                new[] { CreateElement("a", 1d) }));
        }

        [Test]
        public void AlgorithmVersions_RoundTripWithoutCollapsingDomains()
        {
            var versions = new GroundedGeologyAlgorithmVersions(2, 3, 5, 7, 11, 13, 17);

            Assert.That(GroundedGeologyAlgorithmVersions.TryParse(versions.ToString(), out var parsed), Is.True);
            Assert.That(parsed, Is.EqualTo(versions));
            Assert.That(parsed.Occupancy, Is.EqualTo(3));
            Assert.That(parsed.MaterialPacking, Is.EqualTo(11));
            Assert.That(GroundedGeologyAlgorithmVersions.TryParse("v0;1;1;1;1;1;1;1", out _), Is.False);
        }

        [Test]
        public void Identity_UsesStableOwnerAndIndependentSaltedStreams()
        {
            var recipe = CreateRecipe(new[] { CreateElement("a", 1d) });
            var world = WorldCreatorTestFactory.CreateWorld();
            var address = CreateOwnerAddress();
            var firstFeature = GroundedGeologyIdentity.CreateFeatureId(world, address, recipe, 0);
            var secondFeature = GroundedGeologyIdentity.CreateFeatureId(world, address, recipe, 0);
            var featureSeed = GroundedGeologyIdentity.CreateFeatureSeed(world, address, recipe);
            var occupancy = GroundedGeologyIdentity.CreateStream(featureSeed, "occupancy");
            var material = GroundedGeologyIdentity.CreateStream(featureSeed, "material-diagnostics");

            Assert.That(firstFeature, Is.EqualTo(secondFeature));
            Assert.That(firstFeature.IsEmpty, Is.False);
            Assert.That(occupancy.Seed, Is.Not.EqualTo(material.Seed));
            Assert.That(occupancy.Sample(9UL), Is.EqualTo(
                GroundedGeologyIdentity.CreateStream(featureSeed, "occupancy").Sample(9UL)));
            Assert.That(
                GroundedGeologyIdentity.CreateStableSubfeatureId(firstFeature, "mass", "a"),
                Is.EqualTo(GroundedGeologyIdentity.CreateStableSubfeatureId(secondFeature, "mass", "a")));
        }

        [Test]
        public void ReferenceCompiler_IsOrderIndependentAndMetadataOnly()
        {
            var firstRecipe = CreateRecipe(new[] { CreateElement("b", 2d), CreateElement("a", 1d) });
            var secondRecipe = CreateRecipe(new[] { CreateElement("a", 1d), CreateElement("b", 2d) });
            var first = GroundedGeologyReferenceCompiler.Compile(CreateInput(firstRecipe));
            var second = GroundedGeologyReferenceCompiler.Compile(CreateInput(secondRecipe));

            Assert.That(first.Signature, Is.EqualTo(second.Signature));
            Assert.That(first.FeatureId, Is.EqualTo(second.FeatureId));
            Assert.That(first.StableSubfeatureIds, Is.EqualTo(second.StableSubfeatureIds));
            Assert.That(first.Fields, Is.Not.Empty);
            Assert.That(first.Fields.All(field => field.IsReferenceOnly), Is.True);
            Assert.That(first.IsTemporaryNonCanon, Is.True);
        }

        [Test]
        public void Resolution_ReportsRequestedAndEffectiveValuesUnderCaps()
        {
            var bounds = new GroundedGeologyBoundsMeters(
                new GroundedGeologyVector3Meters(0d, 0d, 0d),
                new GroundedGeologyVector3Meters(1d, 20d, 1d));
            var profile = new GroundedGeologyResolutionProfile(
                GroundedGeologyResolutionQuality.Approval,
                64,
                48,
                80_000);
            var metrics = profile.Resolve(bounds);

            Assert.That(metrics.RequestedCellsAcrossMinimum, Is.EqualTo(64));
            Assert.That(metrics.EffectiveCellsAcrossMinimum, Is.LessThan(64d));
            Assert.That(metrics.CellsX, Is.LessThanOrEqualTo(48));
            Assert.That(metrics.CellsY, Is.LessThanOrEqualTo(48));
            Assert.That(metrics.CellsZ, Is.LessThanOrEqualTo(48));
            Assert.That(metrics.SamplePointCount, Is.LessThanOrEqualTo(80_000));
            Assert.That(metrics.WasCapped, Is.True);
        }

        [Test]
        public void EvaluationBounds_ExpandByDeclaredMeterHalo()
        {
            var core = new GroundedGeologyBoundsMeters(
                new GroundedGeologyVector3Meters(2d, 3d, 4d),
                new GroundedGeologyVector3Meters(10d, 6d, 8d));
            var evaluation = new GroundedGeologyEvaluationBounds(core, 1.5d);

            Assert.That(evaluation.Expanded.Center, Is.EqualTo(core.Center));
            Assert.That(evaluation.Expanded.Size, Is.EqualTo(new GroundedGeologyVector3Meters(13d, 9d, 11d)));
        }

        [Test]
        public void RuntimeContracts_DoNotExposeUnityObjectReferences()
        {
            var types = new[]
            {
                typeof(GroundedGeologyRecipe),
                typeof(GroundedGeologyGenerationInput),
                typeof(GroundedGeologyResult),
                typeof(GroundedGeologyFieldDescriptor)
            };

            foreach (var type in types)
            {
                var offending = type.GetProperties()
                    .FirstOrDefault(property => typeof(UnityEngine.Object).IsAssignableFrom(property.PropertyType));
                Assert.That(offending, Is.Null, type.FullName);
                Assert.That(type.Assembly.GetName().Name, Is.EqualTo("BooterBigArm.TopDown3D.Runtime"));
            }
        }

        private static GroundedGeologyGenerationInput CreateInput(GroundedGeologyRecipe recipe)
        {
            return new GroundedGeologyGenerationInput(
                WorldCreatorTestFactory.CreateWorld(),
                recipe,
                CreateOwnerAddress(),
                new GroundedGeologyEvaluationBounds(recipe.LocalPhysicalBounds, 0.25d),
                GroundedGeologyResolutionProfile.ForQuality(GroundedGeologyResolutionQuality.Draft),
                GroundedGeologyFieldKind.All);
        }

        private static WorldCoordinateAddress CreateOwnerAddress()
        {
            return new WorldCoordinateAddress("test.grounded-geology", 1, "owner:0:0");
        }

        private static GroundedGeologyRecipe CreateRecipe(GroundedGeologyStructuralElement[] elements)
        {
            return new GroundedGeologyRecipe(
                "temporary.test.grounded-geology",
                GroundedGeologyRecipe.CurrentSchemaVersion,
                GroundedGeologyFixtureKind.TemporaryExistingRock,
                true,
                new AbsoluteWorldPosition(10d, 2d, -4d),
                GroundedGeologyRotation.Identity,
                new GroundedGeologyBoundsMeters(
                    new GroundedGeologyVector3Meters(0d, 0.5d, 0d),
                    new GroundedGeologyVector3Meters(2d, 1d, 2d)),
                GroundedGeologyAlgorithmVersions.Initial,
                73UL,
                true,
                0.025d,
                0.04d,
                0d,
                0.2d,
                new WorldFeatureId(1UL, 2UL),
                elements);
        }

        private static GroundedGeologyStructuralElement CreateElement(string key, double x)
        {
            return new GroundedGeologyStructuralElement(
                key,
                GroundedGeologyElementKind.AdditiveMass,
                new GroundedGeologyVector3Meters(x, 0.5d, 0d),
                GroundedGeologyRotation.Identity,
                new GroundedGeologyVector3Meters(1d, 1d, 1d),
                1,
                99,
                true);
        }
    }
}
