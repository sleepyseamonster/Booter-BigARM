using System;

namespace BooterBigArm.TopDown3D.WorldCreator
{
    public readonly struct LocalOriginFrame : IEquatable<LocalOriginFrame>
    {
        public LocalOriginFrame(
            WorldCoordinateAddress originAddress,
            AbsoluteWorldPosition originPosition,
            double maximumLocalMagnitude)
        {
            if (maximumLocalMagnitude <= 0d
                || double.IsNaN(maximumLocalMagnitude)
                || double.IsInfinity(maximumLocalMagnitude)
                || maximumLocalMagnitude > float.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumLocalMagnitude),
                    maximumLocalMagnitude,
                    "The local frame range must be finite, positive, and representable by a float.");
            }

            OriginAddress = originAddress;
            OriginPosition = originPosition;
            MaximumLocalMagnitude = maximumLocalMagnitude;
        }

        public WorldCoordinateAddress OriginAddress { get; }
        public AbsoluteWorldPosition OriginPosition { get; }
        public double MaximumLocalMagnitude { get; }

        public bool TryToLocal(AbsoluteWorldPosition absolutePosition, out LocalWorldPosition localPosition)
        {
            var x = absolutePosition.HorizontalA - OriginPosition.HorizontalA;
            var y = absolutePosition.Vertical - OriginPosition.Vertical;
            var z = absolutePosition.HorizontalB - OriginPosition.HorizontalB;
            if (!IsRepresentable(x) || !IsRepresentable(y) || !IsRepresentable(z))
            {
                localPosition = default;
                return false;
            }

            localPosition = new LocalWorldPosition((float)x, (float)y, (float)z);
            return true;
        }

        public LocalWorldPosition ToLocal(AbsoluteWorldPosition absolutePosition)
        {
            if (!TryToLocal(absolutePosition, out var localPosition))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(absolutePosition),
                    absolutePosition,
                    "The absolute position is outside this local origin frame.");
            }

            return localPosition;
        }

        public AbsoluteWorldPosition ToAbsolute(LocalWorldPosition localPosition)
        {
            if (!IsRepresentable(localPosition.X)
                || !IsRepresentable(localPosition.Y)
                || !IsRepresentable(localPosition.Z))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(localPosition),
                    localPosition,
                    "The local position is outside this local origin frame.");
            }

            return new AbsoluteWorldPosition(
                OriginPosition.HorizontalA + localPosition.X,
                OriginPosition.Vertical + localPosition.Y,
                OriginPosition.HorizontalB + localPosition.Z);
        }

        public bool Equals(LocalOriginFrame other)
        {
            return OriginAddress.Equals(other.OriginAddress)
                && OriginPosition.Equals(other.OriginPosition)
                && MaximumLocalMagnitude.Equals(other.MaximumLocalMagnitude);
        }

        public override bool Equals(object obj)
        {
            return obj is LocalOriginFrame other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = OriginAddress.GetHashCode();
                hash = hash * 397 ^ OriginPosition.GetHashCode();
                hash = hash * 397 ^ MaximumLocalMagnitude.GetHashCode();
                return hash;
            }
        }

        private bool IsRepresentable(double value)
        {
            return !double.IsNaN(value)
                && !double.IsInfinity(value)
                && Math.Abs(value) <= MaximumLocalMagnitude;
        }
    }
}
