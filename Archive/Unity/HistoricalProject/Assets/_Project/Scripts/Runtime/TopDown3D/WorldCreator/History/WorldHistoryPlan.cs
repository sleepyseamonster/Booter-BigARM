using System;
using System.Collections.Generic;

namespace BooterBigArm.TopDown3D.WorldCreator
{
    public enum WorldHistoryPhase : byte
    {
        GeologicFormation = 1,
        ConstructionAndOccupation = 2,
        Destruction = 3,
        BurialAndWeathering = 4,
        PresentState = 5
    }

    public enum BoundedTerrainOperationKind : byte
    {
        ReserveApproach = 1,
        LevelFoundation = 2,
        Excavation = 3,
        DestructionCut = 4,
        BurialDeposit = 5,
        Weathering = 6
    }

    /// <summary>
    /// A causal request made before any site geometry exists. Batch 4 fixtures are explicitly
    /// non-canon and prove only that future site generators can negotiate with terrain planning.
    /// </summary>
    public readonly struct SiteIntentReservation : IEquatable<SiteIntentReservation>
    {
        public SiteIntentReservation(
            WorldFeatureId id,
            AbsoluteWorldPosition center,
            float footprintRadius,
            AbsoluteWorldPosition approachAnchor,
            float approachHalfWidth,
            float maximumVerticalChange,
            bool nonCanonProofOnly)
        {
            if (id.IsEmpty)
            {
                throw new ArgumentException("Site reservations require a stable identity.", nameof(id));
            }

            Id = id;
            Center = center;
            FootprintRadius = RequirePositive(footprintRadius, nameof(footprintRadius));
            ApproachAnchor = approachAnchor;
            ApproachHalfWidth = RequirePositive(approachHalfWidth, nameof(approachHalfWidth));
            MaximumVerticalChange = RequirePositive(maximumVerticalChange, nameof(maximumVerticalChange));
            NonCanonProofOnly = nonCanonProofOnly;
        }

        public WorldFeatureId Id { get; }
        public AbsoluteWorldPosition Center { get; }
        public float FootprintRadius { get; }
        public AbsoluteWorldPosition ApproachAnchor { get; }
        public float ApproachHalfWidth { get; }
        public float MaximumVerticalChange { get; }
        public bool NonCanonProofOnly { get; }

        public bool Equals(SiteIntentReservation other)
        {
            return Id.Equals(other.Id)
                && Center.Equals(other.Center)
                && FootprintRadius.Equals(other.FootprintRadius)
                && ApproachAnchor.Equals(other.ApproachAnchor)
                && ApproachHalfWidth.Equals(other.ApproachHalfWidth)
                && MaximumVerticalChange.Equals(other.MaximumVerticalChange)
                && NonCanonProofOnly == other.NonCanonProofOnly;
        }

        public override bool Equals(object obj) => obj is SiteIntentReservation other && Equals(other);
        public override int GetHashCode() => Id.GetHashCode();

        internal static float RequirePositive(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "The value must be finite and positive.");
            }

            return value;
        }
    }

    public readonly struct BoundedTerrainOperation : IEquatable<BoundedTerrainOperation>
    {
        public BoundedTerrainOperation(
            WorldFeatureId id,
            WorldFeatureId reservationId,
            WorldHistoryPhase phase,
            BoundedTerrainOperationKind kind,
            AbsoluteWorldPosition center,
            float radius,
            float signedVerticalChange)
        {
            if (id.IsEmpty || reservationId.IsEmpty)
            {
                throw new ArgumentException("Terrain operations require stable operation and reservation identities.");
            }

            if (phase == WorldHistoryPhase.GeologicFormation || phase == WorldHistoryPhase.PresentState)
            {
                throw new ArgumentOutOfRangeException(nameof(phase), "Site terrain operations belong to an intervening history phase.");
            }

            if (float.IsNaN(signedVerticalChange) || float.IsInfinity(signedVerticalChange))
            {
                throw new ArgumentOutOfRangeException(nameof(signedVerticalChange));
            }

            Id = id;
            ReservationId = reservationId;
            Phase = phase;
            Kind = kind;
            Center = center;
            Radius = SiteIntentReservation.RequirePositive(radius, nameof(radius));
            SignedVerticalChange = signedVerticalChange == 0f ? 0f : signedVerticalChange;
        }

        public WorldFeatureId Id { get; }
        public WorldFeatureId ReservationId { get; }
        public WorldHistoryPhase Phase { get; }
        public BoundedTerrainOperationKind Kind { get; }
        public AbsoluteWorldPosition Center { get; }
        public float Radius { get; }
        public float SignedVerticalChange { get; }

        public bool Equals(BoundedTerrainOperation other)
        {
            return Id.Equals(other.Id)
                && ReservationId.Equals(other.ReservationId)
                && Phase == other.Phase
                && Kind == other.Kind
                && Center.Equals(other.Center)
                && Radius.Equals(other.Radius)
                && SignedVerticalChange.Equals(other.SignedVerticalChange);
        }

        public override bool Equals(object obj) => obj is BoundedTerrainOperation other && Equals(other);
        public override int GetHashCode() => Id.GetHashCode();
    }

    public sealed class WorldHistoryPlan
    {
        private readonly IReadOnlyList<WorldHistoryPhase> phases;
        private readonly IReadOnlyList<BoundedTerrainOperation> operations;

        public WorldHistoryPlan(
            WorldFeatureId id,
            SiteIntentReservation reservation,
            IEnumerable<WorldHistoryPhase> phases,
            IEnumerable<BoundedTerrainOperation> operations)
        {
            if (id.IsEmpty)
            {
                throw new ArgumentException("World history plans require a stable identity.", nameof(id));
            }

            Id = id;
            Reservation = reservation;
            this.phases = CopyPhases(phases);
            this.operations = CopyOperations(operations);
            if (!TryValidate(out var error))
            {
                throw new ArgumentException(error);
            }
        }

        public WorldFeatureId Id { get; }
        public SiteIntentReservation Reservation { get; }
        public IReadOnlyList<WorldHistoryPhase> Phases => phases;
        public IReadOnlyList<BoundedTerrainOperation> Operations => operations;

        public bool TryValidate(out string error)
        {
            if (phases.Count != 5
                || phases[0] != WorldHistoryPhase.GeologicFormation
                || phases[4] != WorldHistoryPhase.PresentState)
            {
                error = "A history plan must declare the complete geologic-to-present phase sequence.";
                return false;
            }

            for (var i = 1; i < phases.Count; i++)
            {
                if ((int)phases[i] != (int)phases[i - 1] + 1)
                {
                    error = "World history phases must be complete and strictly ordered.";
                    return false;
                }
            }

            for (var i = 0; i < operations.Count; i++)
            {
                var operation = operations[i];
                if (!operation.ReservationId.Equals(Reservation.Id))
                {
                    error = "A terrain operation references a different site reservation.";
                    return false;
                }

                if (i > 0 && (int)operation.Phase < (int)operations[i - 1].Phase)
                {
                    error = "Terrain operations must follow causal history order.";
                    return false;
                }

                var distance = HorizontalDistance(operation.Center, Reservation.Center);
                if (distance + operation.Radius > Reservation.FootprintRadius + 0.001d)
                {
                    error = "A terrain operation exceeds its reserved bounded footprint.";
                    return false;
                }

                if (Math.Abs(operation.SignedVerticalChange) > Reservation.MaximumVerticalChange)
                {
                    error = "A terrain operation exceeds its reserved vertical authority.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        private static IReadOnlyList<WorldHistoryPhase> CopyPhases(IEnumerable<WorldHistoryPhase> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new List<WorldHistoryPhase>(source).AsReadOnly();
        }

        private static IReadOnlyList<BoundedTerrainOperation> CopyOperations(IEnumerable<BoundedTerrainOperation> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new List<BoundedTerrainOperation>(source).AsReadOnly();
        }

        internal static double HorizontalDistance(AbsoluteWorldPosition left, AbsoluteWorldPosition right)
        {
            var a = left.HorizontalA - right.HorizontalA;
            var b = left.HorizontalB - right.HorizontalB;
            return Math.Sqrt(a * a + b * b);
        }
    }
}
