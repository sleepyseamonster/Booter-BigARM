using System;
using System.Globalization;

namespace BooterBigArm.TopDown3D.WorldCreator
{
    /// <summary>
    /// Disposable Cartesian adapter for production-path technical proof only. It deliberately
    /// defines no globe bounds, projection, wrapping, thematic notation, regions, or lore and
    /// must be replaced when the user authors the real coordinate model.
    /// </summary>
    public sealed class NonCanonTechnicalCoordinateModel : IWorldCoordinateModel
    {
        public const string ProofModelId = "proof.non-canon.technical-cartesian";

        public string ModelId => ProofModelId;
        public int ModelVersion => 1;

        public WorldCoordinateAddress Encode(AbsoluteWorldPosition absolutePosition)
        {
            var canonical = absolutePosition.HorizontalA.ToString("R", CultureInfo.InvariantCulture)
                + "|" + absolutePosition.Vertical.ToString("R", CultureInfo.InvariantCulture)
                + "|" + absolutePosition.HorizontalB.ToString("R", CultureInfo.InvariantCulture);
            return new WorldCoordinateAddress(ModelId, ModelVersion, canonical);
        }

        public bool TryResolve(WorldCoordinateAddress address, out AbsoluteWorldPosition absolutePosition)
        {
            absolutePosition = default;
            if (!address.IsCompatibleWith(this)) return false;
            var value = address.CanonicalValue.AsSpan();
            var firstSeparator = value.IndexOf('|');
            if (firstSeparator < 0) return false;
            var remainder = value.Slice(firstSeparator + 1);
            var secondSeparator = remainder.IndexOf('|');
            if (secondSeparator < 0 || remainder.Slice(secondSeparator + 1).IndexOf('|') >= 0
                || !double.TryParse(value.Slice(0, firstSeparator), NumberStyles.Float,
                    CultureInfo.InvariantCulture, out var horizontalA)
                || !double.TryParse(remainder.Slice(0, secondSeparator), NumberStyles.Float,
                    CultureInfo.InvariantCulture, out var vertical)
                || !double.TryParse(remainder.Slice(secondSeparator + 1), NumberStyles.Float,
                    CultureInfo.InvariantCulture, out var horizontalB))
            {
                return false;
            }

            absolutePosition = new AbsoluteWorldPosition(horizontalA, vertical, horizontalB);
            return true;
        }
    }
}
