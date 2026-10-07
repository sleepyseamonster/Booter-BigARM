using System;

namespace BooterBigArm.TopDown3D.WorldCreator
{
    /// <summary>
    /// A model-defined, double-precision planning position. This is not persistent geographic identity;
    /// WorldCoordinateAddress owns that responsibility.
    /// </summary>
    public readonly struct AbsoluteWorldPosition : IEquatable<AbsoluteWorldPosition>
    {
        public AbsoluteWorldPosition(double horizontalA, double vertical, double horizontalB)
        {
            HorizontalA = NormalizeFinite(horizontalA, nameof(horizontalA));
            Vertical = NormalizeFinite(vertical, nameof(vertical));
            HorizontalB = NormalizeFinite(horizontalB, nameof(horizontalB));
        }

        public double HorizontalA { get; }
        public double Vertical { get; }
        public double HorizontalB { get; }

        public bool Equals(AbsoluteWorldPosition other)
        {
            return HorizontalA.Equals(other.HorizontalA)
                && Vertical.Equals(other.Vertical)
                && HorizontalB.Equals(other.HorizontalB);
        }

        public override bool Equals(object obj)
        {
            return obj is AbsoluteWorldPosition other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = HorizontalA.GetHashCode();
                hash = hash * 397 ^ Vertical.GetHashCode();
                hash = hash * 397 ^ HorizontalB.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return $"({HorizontalA:R}, {Vertical:R}, {HorizontalB:R})";
        }

        private static double NormalizeFinite(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Absolute positions must be finite.");
            }

            return value == 0d ? 0d : value;
        }
    }

    public readonly struct LocalWorldPosition : IEquatable<LocalWorldPosition>
    {
        public LocalWorldPosition(float x, float y, float z)
        {
            X = ValidateFinite(x, nameof(x));
            Y = ValidateFinite(y, nameof(y));
            Z = ValidateFinite(z, nameof(z));
        }

        public float X { get; }
        public float Y { get; }
        public float Z { get; }

        public bool Equals(LocalWorldPosition other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
        }

        public override bool Equals(object obj)
        {
            return obj is LocalWorldPosition other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = X.GetHashCode();
                hash = hash * 397 ^ Y.GetHashCode();
                hash = hash * 397 ^ Z.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return $"({X:R}, {Y:R}, {Z:R})";
        }

        private static float ValidateFinite(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Local positions must be finite.");
            }

            return value == 0f ? 0f : value;
        }
    }
}
