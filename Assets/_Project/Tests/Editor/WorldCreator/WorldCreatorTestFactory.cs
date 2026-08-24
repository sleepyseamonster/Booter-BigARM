using System;
using System.Globalization;
using BooterBigArm.TopDown3D.WorldCreator;

namespace BooterBigArm.Tests.WorldCreator
{
    internal static class WorldCreatorTestFactory
    {
        public static WorldIdentity CreateWorld(
            long seed = 24681357L,
            int topology = 1,
            int coordinate = 1,
            int landform = 1,
            int material = 1,
            int decoration = 1,
            int resource = 1,
            int site = 1)
        {
            return new WorldIdentity(
                seed,
                new WorldVersionManifest(
                    topology,
                    coordinate,
                    landform,
                    material,
                    decoration,
                    resource,
                    site));
        }
    }

    /// <summary>
    /// Disposable test-only adapter. It is intentionally not a production coordinate schema.
    /// </summary>
    internal sealed class NonCanonCoordinateModel : IWorldCoordinateModel
    {
        public NonCanonCoordinateModel(string modelId = "test.non-canon.coordinate", int modelVersion = 1)
        {
            ModelId = modelId;
            ModelVersion = modelVersion;
        }

        public string ModelId { get; }
        public int ModelVersion { get; }

        public WorldCoordinateAddress Encode(AbsoluteWorldPosition absolutePosition)
        {
            var canonicalValue = absolutePosition.HorizontalA.ToString("R", CultureInfo.InvariantCulture)
                + "|" + absolutePosition.Vertical.ToString("R", CultureInfo.InvariantCulture)
                + "|" + absolutePosition.HorizontalB.ToString("R", CultureInfo.InvariantCulture);
            return new WorldCoordinateAddress(ModelId, ModelVersion, canonicalValue);
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
