using System;
using System.Globalization;

namespace BooterBigArm.TopDown3D.WorldCreator.GroundedGeology
{
    public enum GroundedGeologyFixtureKind : byte
    {
        TemporaryExistingRock = 1,
        TemporaryExistingFormation = 2,
        HandmadeReference = 3,
        RecipeAsset = 4
    }

    public enum GroundedGeologyElementKind : byte
    {
        AdditiveMass = 1,
        SubtractiveVoid = 2,
        ReferenceSurface = 3
    }

    [Flags]
    public enum GroundedGeologyFieldKind : ushort
    {
        None = 0,
        FootprintSpine = 1 << 0,
        TerrainUpliftApron = 1 << 1,
        RockOccupancy = 1 << 2,
        ContactGap = 1 << 3,
        CreviceConcavity = 1 << 4,
        SlopeCurvature = 1 << 5,
        WindExposureShelter = 1 << 6,
        Sediment = 1 << 7,
        Talus = 1 << 8,
        MaterialWeights = 1 << 9,
        TraversalCollision = 1 << 10,
        ChunkOwnershipHalo = 1 << 11,
        SurfaceNormals = 1 << 12,
        All = (1 << 13) - 1
    }

    public enum GroundedGeologyFieldRepresentation : byte
    {
        AnalyticTwoDimensional = 1,
        SampledTwoDimensional = 2,
        AnalyticThreeDimensional = 3,
        SampledThreeDimensional = 4,
        SortedRecords = 5,
        ClassificationBits = 6,
        DiagnosticOnly = 7
    }

    public enum GroundedGeologyFieldUnit : byte
    {
        Meters = 1,
        Normalized = 2,
        Degrees = 3,
        Identifier = 4,
        MixedDocumentedChannels = 5
    }

    public enum GroundedGeologyResolutionQuality : byte
    {
        Draft = 1,
        Standard = 2,
        Approval = 3,
        Diagnostic = 4
    }

    [Serializable]
    public readonly struct GroundedGeologyVector3Meters : IEquatable<GroundedGeologyVector3Meters>
    {
        public GroundedGeologyVector3Meters(double x, double y, double z)
        {
            X = RequireFinite(x, nameof(x));
            Y = RequireFinite(y, nameof(y));
            Z = RequireFinite(z, nameof(z));
        }

        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        public double MinimumComponent => Math.Min(X, Math.Min(Y, Z));
        public double MaximumComponent => Math.Max(X, Math.Max(Y, Z));

        public GroundedGeologyVector3Meters Scale(double factor)
        {
            factor = RequireFinite(factor, nameof(factor));
            return new GroundedGeologyVector3Meters(X * factor, Y * factor, Z * factor);
        }

        public bool Equals(GroundedGeologyVector3Meters other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
        }

        public override bool Equals(object obj)
        {
            return obj is GroundedGeologyVector3Meters other && Equals(other);
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
            return string.Format(
                CultureInfo.InvariantCulture,
                "({0:R}, {1:R}, {2:R}) m",
                X,
                Y,
                Z);
        }

        internal void AppendTo(ref WorldStableHashBuilder hash)
        {
            hash.Append(BitConverter.DoubleToInt64Bits(X));
            hash.Append(BitConverter.DoubleToInt64Bits(Y));
            hash.Append(BitConverter.DoubleToInt64Bits(Z));
        }

        internal static double RequireFinite(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Meter values must be finite.");
            }

            return value == 0d ? 0d : value;
        }
    }

    [Serializable]
    public readonly struct GroundedGeologyRotation : IEquatable<GroundedGeologyRotation>
    {
        public static readonly GroundedGeologyRotation Identity = new GroundedGeologyRotation(0d, 0d, 0d, 1d);

        public GroundedGeologyRotation(double x, double y, double z, double w)
        {
            x = GroundedGeologyVector3Meters.RequireFinite(x, nameof(x));
            y = GroundedGeologyVector3Meters.RequireFinite(y, nameof(y));
            z = GroundedGeologyVector3Meters.RequireFinite(z, nameof(z));
            w = GroundedGeologyVector3Meters.RequireFinite(w, nameof(w));
            var length = Math.Sqrt(x * x + y * y + z * z + w * w);
            if (!(length > 0d))
            {
                throw new ArgumentOutOfRangeException(nameof(w), "A rotation must have non-zero magnitude.");
            }

            X = NormalizeZero(x / length);
            Y = NormalizeZero(y / length);
            Z = NormalizeZero(z / length);
            W = NormalizeZero(w / length);
        }

        public double X { get; }
        public double Y { get; }
        public double Z { get; }
        public double W { get; }

        public bool Equals(GroundedGeologyRotation other)
        {
            return X.Equals(other.X)
                && Y.Equals(other.Y)
                && Z.Equals(other.Z)
                && W.Equals(other.W);
        }

        public override bool Equals(object obj)
        {
            return obj is GroundedGeologyRotation other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = X.GetHashCode();
                hash = hash * 397 ^ Y.GetHashCode();
                hash = hash * 397 ^ Z.GetHashCode();
                hash = hash * 397 ^ W.GetHashCode();
                return hash;
            }
        }

        internal void AppendTo(ref WorldStableHashBuilder hash)
        {
            hash.Append(BitConverter.DoubleToInt64Bits(X));
            hash.Append(BitConverter.DoubleToInt64Bits(Y));
            hash.Append(BitConverter.DoubleToInt64Bits(Z));
            hash.Append(BitConverter.DoubleToInt64Bits(W));
        }

        private static double NormalizeZero(double value)
        {
            return value == 0d ? 0d : value;
        }
    }

    [Serializable]
    public readonly struct GroundedGeologyBoundsMeters : IEquatable<GroundedGeologyBoundsMeters>
    {
        public GroundedGeologyBoundsMeters(
            GroundedGeologyVector3Meters center,
            GroundedGeologyVector3Meters size)
        {
            if (!(size.X > 0d) || !(size.Y > 0d) || !(size.Z > 0d))
            {
                throw new ArgumentOutOfRangeException(nameof(size), "Every bounded-geology dimension must be positive.");
            }

            Center = center;
            Size = size;
        }

        public GroundedGeologyVector3Meters Center { get; }
        public GroundedGeologyVector3Meters Size { get; }

        public GroundedGeologyVector3Meters Minimum => new GroundedGeologyVector3Meters(
            Center.X - Size.X * 0.5d,
            Center.Y - Size.Y * 0.5d,
            Center.Z - Size.Z * 0.5d);

        public GroundedGeologyVector3Meters Maximum => new GroundedGeologyVector3Meters(
            Center.X + Size.X * 0.5d,
            Center.Y + Size.Y * 0.5d,
            Center.Z + Size.Z * 0.5d);

        public GroundedGeologyBoundsMeters Expanded(double haloMeters)
        {
            haloMeters = GroundedGeologyVector3Meters.RequireFinite(haloMeters, nameof(haloMeters));
            if (haloMeters < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(haloMeters), "Halo cannot be negative.");
            }

            return new GroundedGeologyBoundsMeters(
                Center,
                new GroundedGeologyVector3Meters(
                    Size.X + haloMeters * 2d,
                    Size.Y + haloMeters * 2d,
                    Size.Z + haloMeters * 2d));
        }

        public bool Equals(GroundedGeologyBoundsMeters other)
        {
            return Center.Equals(other.Center) && Size.Equals(other.Size);
        }

        public override bool Equals(object obj)
        {
            return obj is GroundedGeologyBoundsMeters other && Equals(other);
        }

        public override int GetHashCode()
        {
            return unchecked(Center.GetHashCode() * 397 ^ Size.GetHashCode());
        }

        public override string ToString()
        {
            return $"center={Center};size={Size}";
        }

        internal void AppendTo(ref WorldStableHashBuilder hash)
        {
            Center.AppendTo(ref hash);
            Size.AppendTo(ref hash);
        }
    }

    [Serializable]
    public readonly struct GroundedGeologyEvaluationBounds : IEquatable<GroundedGeologyEvaluationBounds>
    {
        public GroundedGeologyEvaluationBounds(GroundedGeologyBoundsMeters core, double haloMeters)
        {
            haloMeters = GroundedGeologyVector3Meters.RequireFinite(haloMeters, nameof(haloMeters));
            if (haloMeters < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(haloMeters), "Halo cannot be negative.");
            }

            Core = core;
            HaloMeters = haloMeters;
        }

        public GroundedGeologyBoundsMeters Core { get; }
        public double HaloMeters { get; }
        public GroundedGeologyBoundsMeters Expanded => Core.Expanded(HaloMeters);

        public bool Equals(GroundedGeologyEvaluationBounds other)
        {
            return Core.Equals(other.Core) && HaloMeters.Equals(other.HaloMeters);
        }

        public override bool Equals(object obj)
        {
            return obj is GroundedGeologyEvaluationBounds other && Equals(other);
        }

        public override int GetHashCode()
        {
            return unchecked(Core.GetHashCode() * 397 ^ HaloMeters.GetHashCode());
        }

        internal void AppendTo(ref WorldStableHashBuilder hash)
        {
            Core.AppendTo(ref hash);
            hash.Append(BitConverter.DoubleToInt64Bits(HaloMeters));
        }
    }

    [Serializable]
    public readonly struct GroundedGeologyAlgorithmVersions : IEquatable<GroundedGeologyAlgorithmVersions>
    {
        public const int CurrentCodecVersion = 1;
        public static readonly GroundedGeologyAlgorithmVersions Initial =
            new GroundedGeologyAlgorithmVersions(1, 1, 1, 1, 1, 1, 1);

        public GroundedGeologyAlgorithmVersions(
            int plan,
            int occupancy,
            int sediment,
            int decoration,
            int materialPacking,
            int collision,
            int representation)
        {
            Plan = RequireVersion(plan, nameof(plan));
            Occupancy = RequireVersion(occupancy, nameof(occupancy));
            Sediment = RequireVersion(sediment, nameof(sediment));
            Decoration = RequireVersion(decoration, nameof(decoration));
            MaterialPacking = RequireVersion(materialPacking, nameof(materialPacking));
            Collision = RequireVersion(collision, nameof(collision));
            Representation = RequireVersion(representation, nameof(representation));
        }

        public int Plan { get; }
        public int Occupancy { get; }
        public int Sediment { get; }
        public int Decoration { get; }
        public int MaterialPacking { get; }
        public int Collision { get; }
        public int Representation { get; }

        public bool Equals(GroundedGeologyAlgorithmVersions other)
        {
            return Plan == other.Plan
                && Occupancy == other.Occupancy
                && Sediment == other.Sediment
                && Decoration == other.Decoration
                && MaterialPacking == other.MaterialPacking
                && Collision == other.Collision
                && Representation == other.Representation;
        }

        public override bool Equals(object obj)
        {
            return obj is GroundedGeologyAlgorithmVersions other && Equals(other);
        }

        public override int GetHashCode()
        {
            var hash = new WorldStableHashBuilder("grounded-geology-versions-v1");
            AppendTo(ref hash);
            return hash.FinishHashCode();
        }

        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "v{0};{1};{2};{3};{4};{5};{6};{7}",
                CurrentCodecVersion,
                Plan,
                Occupancy,
                Sediment,
                Decoration,
                MaterialPacking,
                Collision,
                Representation);
        }

        public static bool TryParse(string value, out GroundedGeologyAlgorithmVersions versions)
        {
            versions = default;
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            var parts = value.Split(';');
            if (parts.Length != 8
                || !string.Equals(parts[0], "v" + CurrentCodecVersion, StringComparison.Ordinal)
                || !TryVersion(parts[1], out var plan)
                || !TryVersion(parts[2], out var occupancy)
                || !TryVersion(parts[3], out var sediment)
                || !TryVersion(parts[4], out var decoration)
                || !TryVersion(parts[5], out var materialPacking)
                || !TryVersion(parts[6], out var collision)
                || !TryVersion(parts[7], out var representation))
            {
                return false;
            }

            versions = new GroundedGeologyAlgorithmVersions(
                plan,
                occupancy,
                sediment,
                decoration,
                materialPacking,
                collision,
                representation);
            return true;
        }

        internal void AppendTo(ref WorldStableHashBuilder hash)
        {
            hash.Append(Plan);
            hash.Append(Occupancy);
            hash.Append(Sediment);
            hash.Append(Decoration);
            hash.Append(MaterialPacking);
            hash.Append(Collision);
            hash.Append(Representation);
        }

        private static int RequireVersion(int value, string parameterName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Algorithm versions cannot be negative.");
            }

            return value;
        }

        private static bool TryVersion(string value, out int version)
        {
            return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out version)
                && version >= 0;
        }
    }
}
