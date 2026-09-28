using System;
using System.Linq;
using BooterBigArm.TopDown3D.WorldCreator;
using NUnit.Framework;

namespace BooterBigArm.Tests.WorldCreator
{
    public sealed class WorldCliffSectionStudyTests
    {
        [Test]
        public void SteepSectionHasStableSourceAndDownhillToeDeposit()
        {
            var study = CreateStudy();
            Assert.That(study.TryBuild(-1, 0, out var section, out var error), Is.True, error);
            Assert.That(section.Id.IsEmpty, Is.False);
            Assert.That(section.ParentFeatureId.IsEmpty, Is.True);
            Assert.That(section.StrataFamilyId, Is.EqualTo("study.strata"));
            Assert.That(section.VerticalDrop, Is.GreaterThan(1.25d));
            Assert.That(section.OutwardA, Is.GreaterThan(0d));
            Assert.That(section.Rim.Vertical, Is.GreaterThan(section.Toe.Vertical));
            Assert.That(section.SampleDebrisInfluence(section.Toe), Is.EqualTo(1f));
            Assert.That(section.SampleDebrisInfluence(section.Rim), Is.Zero);
            Assert.That(section.SampleDebrisInfluence(new AbsoluteWorldPosition(
                section.Toe.HorizontalA + section.OutwardA * 6d, 0d,
                section.Toe.HorizontalB + section.OutwardB * 6d)), Is.Zero);
        }

        [Test]
        public void OverlappingWindowsAgreeRegardlessOfRequestOrderAndUnrelatedVersions()
        {
            var study = CreateStudy();
            Assert.That(study.TryBuildWindow(-2, 1, -1, 1, out var first, out var firstError), Is.True, firstError);
            Assert.That(study.TryBuildWindow(-1, 2, 0, 2, out var second, out var secondError), Is.True, secondError);
            Assert.That(first.Count, Is.GreaterThan(0));
            var sharedFirst = first.Single(candidate => candidate.OwnerCellA == -1 && candidate.OwnerCellB == 0);
            var sharedSecond = second.Single(candidate => candidate.OwnerCellA == -1 && candidate.OwnerCellB == 0);
            Assert.That(sharedSecond.Id, Is.EqualTo(sharedFirst.Id));
            Assert.That(sharedSecond.Rim, Is.EqualTo(sharedFirst.Rim));
            Assert.That(sharedSecond.Toe, Is.EqualTo(sharedFirst.Toe));
            Assert.That(first.Select(candidate => candidate.Id), Is.Ordered);

            var materialChanged = CreateStudy(material: 2);
            Assert.That(materialChanged.TryBuild(-1, 0, out var sameLandform, out var materialError), Is.True, materialError);
            Assert.That(sameLandform.Id, Is.EqualTo(sharedFirst.Id));
            var landformChanged = CreateStudy(landform: 2);
            Assert.That(landformChanged.TryBuild(-1, 0, out var differentLandform, out var landformError), Is.True, landformError);
            Assert.That(differentLandform.Id, Is.Not.EqualTo(sharedFirst.Id));
        }

        [Test]
        public void ReservedRouteOrFlatGroundDoesNotEmitASection()
        {
            var reservedStudy = CreateStudy(reservedRoute: true);
            Assert.That(reservedStudy.TryBuild(-1, 0, out _, out var reservedError), Is.False);
            Assert.That(reservedError, Is.Null);

            var flatStudy = CreateStudy(amplitude: 0d);
            Assert.That(flatStudy.TryBuild(-1, 0, out _, out var flatError), Is.False);
            Assert.That(flatError, Is.Null);
        }

        [Test]
        public void OnlyPlannedLandformsSupplyAParentFeature()
        {
            var broadGround = CreateStudy();
            var boundedLandform = CreateStudy(semantic: WorldSurfaceSemantic.BoundedLandform);
            Assert.That(broadGround.TryBuild(-1, 0, out var broad, out var broadError), Is.True, broadError);
            Assert.That(boundedLandform.TryBuild(-1, 0, out var bounded, out var boundedError), Is.True, boundedError);
            Assert.That(broad.ParentFeatureId.IsEmpty, Is.True);
            Assert.That(bounded.ParentFeatureId, Is.EqualTo(SyntheticScarpQuery.ParentId));
            Assert.That(bounded.Id, Is.Not.EqualTo(broad.Id));
        }

        [Test]
        public void WindowLimitAlsoRejectsExtremeCellRangesWithoutOverflow()
        {
            var study = CreateStudy();
            Assert.Throws<ArgumentOutOfRangeException>(() => study.TryBuildWindow(
                long.MinValue, long.MaxValue, 0, 0, out _, out _));
        }

        private static WorldCliffSectionStudy CreateStudy(
            bool reservedRoute = false,
            double amplitude = 2d,
            int landform = 1,
            int material = 1,
            WorldSurfaceSemantic semantic = WorldSurfaceSemantic.BroadGround)
        {
            return new WorldCliffSectionStudy(
                WorldCreatorTestFactory.CreateWorld(landform: landform, material: material),
                new NonCanonCoordinateModel(),
                new SyntheticScarpQuery(amplitude, reservedRoute, semantic),
                WorldCliffStudyProfile.CreateTechnicalStudy());
        }

        private sealed class SyntheticScarpQuery : IWorldQueryService
        {
            public static readonly WorldFeatureId ParentId = new WorldFeatureId(17UL, 23UL);
            private readonly double amplitude;
            private readonly bool reservedRoute;
            private readonly WorldSurfaceSemantic semantic;

            public SyntheticScarpQuery(double amplitude, bool reservedRoute, WorldSurfaceSemantic semantic)
            {
                this.amplitude = amplitude;
                this.reservedRoute = reservedRoute;
                this.semantic = semantic;
            }

            public bool TrySampleSurface(
                AbsoluteWorldPosition position,
                out WorldSurfaceSample sample,
                out string error)
            {
                var scaledA = position.HorizontalA / 0.8d;
                var tanh = Math.Tanh(scaledA);
                var height = amplitude * (1d - tanh);
                var derivative = -amplitude * (1d - tanh * tanh) / 0.8d;
                var normalLength = Math.Sqrt(derivative * derivative + 1d);
                sample = new WorldSurfaceSample(
                    new AbsoluteWorldPosition(position.HorizontalA, height, position.HorizontalB),
                    (float)(-derivative / normalLength),
                    (float)(1d / normalLength),
                    0f,
                    semantic,
                    ParentId,
                    "study.province",
                    "study.strata");
                error = null;
                return true;
            }

            public bool TrySampleVolume(
                AbsoluteWorldPosition position,
                out WorldVolumeSample sample,
                out string error)
            {
                sample = new WorldVolumeSample(0f, true, ParentId);
                error = null;
                return true;
            }

            public bool TrySampleAffordance(
                AbsoluteWorldPosition position,
                WorldAgentProfile agent,
                out WorldAffordanceSample sample,
                out string error)
            {
                sample = new WorldAffordanceSample(true, reservedRoute, 0f, WorldFeatureId.Empty);
                error = null;
                return true;
            }
        }
    }
}
