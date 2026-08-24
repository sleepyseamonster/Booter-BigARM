using System;
using System.Globalization;
using BooterBigArm.TopDown3D.WorldCreator;

namespace BooterBigArm.Editor.WorldCreator
{
    /// <summary>
    /// Disposable editor-only planning-space adapter for World Creator proof tooling.
    /// It is not a thematic coordinate notation, globe model, or production fallback.
    /// </summary>
    public sealed class NonCanonProofCoordinateModel : IWorldCoordinateModel
    {
        public const string ProofModelId = "proof.non-canon.coordinate.cartesian";

        public string ModelId => ProofModelId;
        public int ModelVersion => 1;

        public WorldCoordinateAddress Encode(AbsoluteWorldPosition absolutePosition)
        {
            var value = absolutePosition.HorizontalA.ToString("R", CultureInfo.InvariantCulture)
                + "|" + absolutePosition.Vertical.ToString("R", CultureInfo.InvariantCulture)
                + "|" + absolutePosition.HorizontalB.ToString("R", CultureInfo.InvariantCulture);
            return new WorldCoordinateAddress(ModelId, ModelVersion, value);
        }

        public bool TryResolve(WorldCoordinateAddress address, out AbsoluteWorldPosition absolutePosition)
        {
            absolutePosition = default;
            if (!address.IsCompatibleWith(this))
            {
                return false;
            }

            var parts = address.CanonicalValue.Split('|');
            if (parts.Length != 3
                || !TryParse(parts[0], out var horizontalA)
                || !TryParse(parts[1], out var vertical)
                || !TryParse(parts[2], out var horizontalB))
            {
                return false;
            }

            absolutePosition = new AbsoluteWorldPosition(horizontalA, vertical, horizontalB);
            return true;
        }

        private static bool TryParse(string value, out double result)
        {
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }
    }
}
