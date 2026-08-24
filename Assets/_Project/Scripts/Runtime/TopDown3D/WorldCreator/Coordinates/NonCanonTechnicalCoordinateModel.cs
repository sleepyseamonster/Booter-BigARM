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
            var parts = address.CanonicalValue.Split('|');
            if (parts.Length != 3
                || !double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var horizontalA)
                || !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var vertical)
                || !double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var horizontalB))
            {
                return false;
            }

            absolutePosition = new AbsoluteWorldPosition(horizontalA, vertical, horizontalB);
            return true;
        }
    }
}
