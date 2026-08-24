using System;
using BooterBigArm.TopDown3D.WorldCreator;
using NUnit.Framework;

namespace BooterBigArm.Tests.WorldCreator
{
    public sealed class WorldHistoryPlanTests
    {
        [Test]
        public void SyntheticFixtureDeclaresCompleteCausalOrderAndBoundedOperations()
        {
            var world = WorldCreatorTestFactory.CreateWorld();
            var model = new NonCanonCoordinateModel();
            var history = HybridTerrainCompiler.CreateNonCanonSyntheticHistoryFixture(
                world,
                model,
                new AbsoluteWorldPosition(100d, 0d, -80d));

            Assert.That(history.TryValidate(out var error), Is.True, error);
            Assert.That(history.Reservation.NonCanonProofOnly, Is.True);
            Assert.That(history.Phases, Is.EqualTo(new[]
            {
                WorldHistoryPhase.GeologicFormation,
                WorldHistoryPhase.ConstructionAndOccupation,
                WorldHistoryPhase.Destruction,
                WorldHistoryPhase.BurialAndWeathering,
                WorldHistoryPhase.PresentState
            }));
            Assert.That(history.Operations.Count, Is.EqualTo(5));
        }

        [Test]
        public void MissingOrMisorderedHistoryPhasesAreRejected()
        {
            var fixture = CreateFixture(out var reservation, out var operation);
            Assert.Throws<ArgumentException>(() => new WorldHistoryPlan(
                fixture,
                reservation,
                new[]
                {
                    WorldHistoryPhase.GeologicFormation,
                    WorldHistoryPhase.Destruction,
                    WorldHistoryPhase.ConstructionAndOccupation,
                    WorldHistoryPhase.BurialAndWeathering,
                    WorldHistoryPhase.PresentState
                },
                new[] { operation }));
        }

        [Test]
        public void OperationsOutsideFootprintOrVerticalAuthorityAreRejected()
        {
            var world = WorldCreatorTestFactory.CreateWorld();
            var model = new NonCanonCoordinateModel();
            var address = model.Encode(new AbsoluteWorldPosition(0d, 0d, 0d));
            var seedNamespace = new WorldSeedNamespace(WorldVersionDomain.Site, "site.history-bounds-test");
            var reservationId = WorldFeatureId.Create(world, seedNamespace, address, "reservation");
            var reservation = new SiteIntentReservation(
                reservationId,
                new AbsoluteWorldPosition(0d, 0d, 0d),
                20f,
                new AbsoluteWorldPosition(-10d, 0d, 0d),
                3f,
                5f,
                true);
            var outside = new BoundedTerrainOperation(
                WorldFeatureId.Create(world, seedNamespace, address, "outside"),
                reservationId,
                WorldHistoryPhase.Destruction,
                BoundedTerrainOperationKind.DestructionCut,
                new AbsoluteWorldPosition(18d, 0d, 0d),
                5f,
                -3f);
            var tooDeep = new BoundedTerrainOperation(
                WorldFeatureId.Create(world, seedNamespace, address, "too-deep"),
                reservationId,
                WorldHistoryPhase.Destruction,
                BoundedTerrainOperationKind.DestructionCut,
                new AbsoluteWorldPosition(0d, 0d, 0d),
                5f,
                -6f);
            var phases = CompletePhases();
            var historyId = WorldFeatureId.Create(world, seedNamespace, address, "history");
            Assert.Throws<ArgumentException>(() => new WorldHistoryPlan(historyId, reservation, phases, new[] { outside }));
            Assert.Throws<ArgumentException>(() => new WorldHistoryPlan(historyId, reservation, phases, new[] { tooDeep }));
        }

        private static WorldFeatureId CreateFixture(out SiteIntentReservation reservation, out BoundedTerrainOperation operation)
        {
            var world = WorldCreatorTestFactory.CreateWorld();
            var model = new NonCanonCoordinateModel();
            var address = model.Encode(new AbsoluteWorldPosition(0d, 0d, 0d));
            var seedNamespace = new WorldSeedNamespace(WorldVersionDomain.Site, "site.history-order-test");
            var reservationId = WorldFeatureId.Create(world, seedNamespace, address, "reservation");
            reservation = new SiteIntentReservation(reservationId, new AbsoluteWorldPosition(0d, 0d, 0d), 20f, new AbsoluteWorldPosition(-10d, 0d, 0d), 3f, 5f, true);
            operation = new BoundedTerrainOperation(
                WorldFeatureId.Create(world, seedNamespace, address, "operation"),
                reservationId,
                WorldHistoryPhase.Destruction,
                BoundedTerrainOperationKind.DestructionCut,
                new AbsoluteWorldPosition(0d, 0d, 0d),
                5f,
                -2f);
            return WorldFeatureId.Create(world, seedNamespace, address, "history");
        }

        private static WorldHistoryPhase[] CompletePhases()
        {
            return new[]
            {
                WorldHistoryPhase.GeologicFormation,
                WorldHistoryPhase.ConstructionAndOccupation,
                WorldHistoryPhase.Destruction,
                WorldHistoryPhase.BurialAndWeathering,
                WorldHistoryPhase.PresentState
            };
        }
    }
}
