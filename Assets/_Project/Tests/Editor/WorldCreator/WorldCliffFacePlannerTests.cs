using System.Collections.Generic;
using BooterBigArm.TopDown3D.WorldCreator;
using NUnit.Framework;

namespace BooterBigArm.Tests.WorldCreator
{
    public sealed class WorldCliffFacePlannerTests
    {
        [Test]
        public void BorderOwnerAndBuildOrderCannotChangeTheSameCanyonSpan()
        {
            var parent = new WorldFeatureId(10UL, 20UL);
            var first = Section(1, 0, parent, 101UL);
            var last = Section(0, 0, parent, 102UL);
            var forward = new Dictionary<(long A, long B), WorldCliffSectionCandidate>
            {
                [(1, 0)] = first, [(0, 0)] = last
            };
            var reverse = new Dictionary<(long A, long B), WorldCliffSectionCandidate>
            {
                [(0, 0)] = last, [(1, 0)] = first
            };

            var fromOwner = WorldCliffFacePlanner.PlanOwnedSpans(forward, 1, 1, 0, 0);
            var fromOtherOrder = WorldCliffFacePlanner.PlanOwnedSpans(reverse, 1, 1, 0, 0);
            var fromNeighborOwner = WorldCliffFacePlanner.PlanOwnedSpans(reverse, 0, 0, 0, 0);

            Assert.That(fromOwner.Count, Is.EqualTo(1));
            Assert.That(fromOwner[0].Id, Is.EqualTo(first.Id));
            Assert.That(fromOwner[0].ParentFeatureId, Is.EqualTo(parent));
            Assert.That(fromOwner[0].Last.Id, Is.EqualTo(last.Id));
            Assert.That(fromOtherOrder[0].Id, Is.EqualTo(fromOwner[0].Id));
            Assert.That(fromNeighborOwner, Is.Empty);
        }

        [Test]
        public void CanyonParentAndStrataCannotJoinAcrossAContact()
        {
            var first = Section(1, 0, new WorldFeatureId(10UL, 20UL), 101UL);
            var differentParent = Section(0, 0, new WorldFeatureId(30UL, 40UL), 102UL);
            var differentStrata = Section(0, 0, first.ParentFeatureId, 103UL, "other.strata");
            var candidates = new Dictionary<(long A, long B), WorldCliffSectionCandidate>
            {
                [(1, 0)] = first, [(0, 0)] = differentParent
            };
            Assert.That(WorldCliffFacePlanner.PlanOwnedSpans(candidates, 1, 1, 0, 0), Is.Empty);
            candidates[(0, 0)] = differentStrata;
            Assert.That(WorldCliffFacePlanner.PlanOwnedSpans(candidates, 1, 1, 0, 0), Is.Empty);
        }

        private static WorldCliffSectionCandidate Section(
            long a, long b, WorldFeatureId parent, ulong ordinal,
            string strata = "test.strata")
        {
            var centerA = (a + 0.5d) * 3d;
            var centerB = (b + 0.5d) * 3d;
            return new WorldCliffSectionCandidate(
                new WorldFeatureId(1UL, ordinal), parent, a, b,
                new AbsoluteWorldPosition(centerA, 2d, centerB),
                new AbsoluteWorldPosition(centerA, 4d, centerB - 1d),
                new AbsoluteWorldPosition(centerA, 0d, centerB + 1d),
                0d, 1d, 50f, strata, 5d, 3d);
        }
    }
}
