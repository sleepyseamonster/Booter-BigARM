using System;

namespace BooterBigArm.TopDown3D.WorldCreator
{
    /// <summary>
    /// A technical planning-grid index. It is not a player-facing coordinate or a globe region.
    /// </summary>
    public readonly struct CanyonSystemCellIndex : IEquatable<CanyonSystemCellIndex>, IComparable<CanyonSystemCellIndex>
    {
        public CanyonSystemCellIndex(long horizontalA, long horizontalB)
        {
            HorizontalA = horizontalA;
            HorizontalB = horizontalB;
        }

        public long HorizontalA { get; }
        public long HorizontalB { get; }

        public CanyonSystemCellIndex Offset(int horizontalA, int horizontalB)
        {
            return new CanyonSystemCellIndex(
                checked(HorizontalA + horizontalA),
                checked(HorizontalB + horizontalB));
        }

        public AbsoluteWorldPosition GetCenter(double cellSpan)
        {
            ValidateCellSpan(cellSpan);
            return new AbsoluteWorldPosition(
                (HorizontalA + 0.5d) * cellSpan,
                0d,
                (HorizontalB + 0.5d) * cellSpan);
        }

        public static CanyonSystemCellIndex FromAbsolute(AbsoluteWorldPosition position, double cellSpan)
        {
            ValidateCellSpan(cellSpan);
            return new CanyonSystemCellIndex(
                FloorToLong(position.HorizontalA / cellSpan),
                FloorToLong(position.HorizontalB / cellSpan));
        }

        public int CompareTo(CanyonSystemCellIndex other)
        {
            var horizontalAComparison = HorizontalA.CompareTo(other.HorizontalA);
            return horizontalAComparison != 0
                ? horizontalAComparison
                : HorizontalB.CompareTo(other.HorizontalB);
        }

        public bool Equals(CanyonSystemCellIndex other)
        {
            return HorizontalA == other.HorizontalA && HorizontalB == other.HorizontalB;
        }

        public override bool Equals(object obj)
        {
            return obj is CanyonSystemCellIndex other && Equals(other);
        }

        public override int GetHashCode()
        {
            return unchecked(HorizontalA.GetHashCode() * 397 ^ HorizontalB.GetHashCode());
        }

        public override string ToString()
        {
            return $"technical-cell({HorizontalA},{HorizontalB})";
        }

        private static long FloorToLong(double value)
        {
            if (double.IsNaN(value)
                || double.IsInfinity(value)
                || value < -9223372036854775808d
                || value >= 9223372036854775808d)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Planning-cell index is outside the supported integer range.");
            }

            return checked((long)Math.Floor(value));
        }

        private static void ValidateCellSpan(double cellSpan)
        {
            if (double.IsNaN(cellSpan) || double.IsInfinity(cellSpan) || cellSpan <= 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(cellSpan), cellSpan, "Planning-cell span must be finite and positive.");
            }
        }
    }
}
