using System;
using System.Collections.Generic;

namespace BooterBigArm.TopDown3D.WorldCreator.GroundedGeology
{
    [Serializable]
    public readonly struct GroundedGeologyStructuralElement : IEquatable<GroundedGeologyStructuralElement>
    {
        public GroundedGeologyStructuralElement(
            string stableKey,
            GroundedGeologyElementKind kind,
            GroundedGeologyVector3Meters localPositionMeters,
            GroundedGeologyRotation localRotation,
            GroundedGeologyVector3Meters sizeMeters,
            int shapeCode,
            int shapeSeed,
            bool contributes)
        {
            StableKey = WorldStableText.Require(stableKey, nameof(stableKey), 256);
            if (!Enum.IsDefined(typeof(GroundedGeologyElementKind), kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            if (!(sizeMeters.X > 0d) || !(sizeMeters.Y > 0d) || !(sizeMeters.Z > 0d))
            {
                throw new ArgumentOutOfRangeException(nameof(sizeMeters), "Structural element dimensions must be positive meters.");
            }

            Kind = kind;
            LocalPositionMeters = localPositionMeters;
            LocalRotation = localRotation;
            SizeMeters = sizeMeters;
            ShapeCode = shapeCode;
            ShapeSeed = shapeSeed;
            Contributes = contributes;
        }

        public string StableKey { get; }
        public GroundedGeologyElementKind Kind { get; }
        public GroundedGeologyVector3Meters LocalPositionMeters { get; }
        public GroundedGeologyRotation LocalRotation { get; }
        public GroundedGeologyVector3Meters SizeMeters { get; }
        public int ShapeCode { get; }
        public int ShapeSeed { get; }
        public bool Contributes { get; }

        public bool Equals(GroundedGeologyStructuralElement other)
        {
            return string.Equals(StableKey, other.StableKey, StringComparison.Ordinal)
                && Kind == other.Kind
                && LocalPositionMeters.Equals(other.LocalPositionMeters)
                && LocalRotation.Equals(other.LocalRotation)
                && SizeMeters.Equals(other.SizeMeters)
                && ShapeCode == other.ShapeCode
                && ShapeSeed == other.ShapeSeed
                && Contributes == other.Contributes;
        }

        public override bool Equals(object obj)
        {
            return obj is GroundedGeologyStructuralElement other && Equals(other);
        }

        public override int GetHashCode()
        {
            var hash = new WorldStableHashBuilder("grounded-geology-element-v1");
            AppendTo(ref hash);
            return hash.FinishHashCode();
        }

        internal void AppendTo(ref WorldStableHashBuilder hash)
        {
            hash.Append(StableKey);
            hash.Append((byte)Kind);
            LocalPositionMeters.AppendTo(ref hash);
            LocalRotation.AppendTo(ref hash);
            SizeMeters.AppendTo(ref hash);
            hash.Append(ShapeCode);
            hash.Append(ShapeSeed);
            hash.Append((byte)(Contributes ? 1 : 0));
        }
    }

    /// <summary>
    /// Immutable authoring intent. It contains meter-valued data and stable references only;
    /// generated meshes, grids, GameObjects, and other UnityEngine.Object instances are excluded.
    /// </summary>
    [Serializable]
    public sealed class GroundedGeologyRecipe
    {
        public const int CurrentSchemaVersion = 1;

        private readonly IReadOnlyList<GroundedGeologyStructuralElement> structuralElements;

        public GroundedGeologyRecipe(
            string stableId,
            int schemaVersion,
            GroundedGeologyFixtureKind fixtureKind,
            bool isTemporaryNonCanon,
            AbsoluteWorldPosition absoluteAnchor,
            GroundedGeologyRotation orientation,
            GroundedGeologyBoundsMeters localPhysicalBounds,
            GroundedGeologyAlgorithmVersions algorithmVersions,
            ulong seedSalt,
            bool seedLocked,
            double sampleSpacingMeters,
            double blendWidthMeters,
            double seamWidthMeters,
            double materialWavelengthMeters,
            WorldFeatureId sourceHierarchySignature,
            IEnumerable<GroundedGeologyStructuralElement> elements)
        {
            StableId = WorldStableText.Require(stableId, nameof(stableId), 256);
            if (schemaVersion <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(schemaVersion), "Recipe schema version must be positive.");
            }

            if (!Enum.IsDefined(typeof(GroundedGeologyFixtureKind), fixtureKind))
            {
                throw new ArgumentOutOfRangeException(nameof(fixtureKind));
            }

            if (isTemporaryNonCanon
                && !StableId.StartsWith("temporary.", StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Temporary non-canon recipes require a 'temporary.' stable-ID prefix.",
                    nameof(stableId));
            }

            if (sourceHierarchySignature.IsEmpty)
            {
                throw new ArgumentException("Fixture recipes require a source hierarchy signature.", nameof(sourceHierarchySignature));
            }

            SchemaVersion = schemaVersion;
            FixtureKind = fixtureKind;
            IsTemporaryNonCanon = isTemporaryNonCanon;
            AbsoluteAnchor = absoluteAnchor;
            Orientation = orientation;
            LocalPhysicalBounds = localPhysicalBounds;
            AlgorithmVersions = algorithmVersions;
            SeedSalt = seedSalt;
            SeedLocked = seedLocked;
            SampleSpacingMeters = RequireNonNegative(sampleSpacingMeters, nameof(sampleSpacingMeters), false);
            BlendWidthMeters = RequireNonNegative(blendWidthMeters, nameof(blendWidthMeters), true);
            SeamWidthMeters = RequireNonNegative(seamWidthMeters, nameof(seamWidthMeters), true);
            MaterialWavelengthMeters = RequireNonNegative(materialWavelengthMeters, nameof(materialWavelengthMeters), false);
            SourceHierarchySignature = sourceHierarchySignature;

            var copy = elements == null
                ? throw new ArgumentNullException(nameof(elements))
                : new List<GroundedGeologyStructuralElement>(elements);
            if (copy.Count == 0)
            {
                throw new ArgumentException("A fixture recipe requires at least one structural element.", nameof(elements));
            }

            copy.Sort((left, right) => string.CompareOrdinal(left.StableKey, right.StableKey));
            for (var i = 1; i < copy.Count; i++)
            {
                if (string.Equals(copy[i - 1].StableKey, copy[i].StableKey, StringComparison.Ordinal))
                {
                    throw new ArgumentException($"Duplicate structural element key '{copy[i].StableKey}'.", nameof(elements));
                }
            }

            structuralElements = copy.AsReadOnly();
            ContentSignature = BuildContentSignature();
        }

        public string StableId { get; }
        public int SchemaVersion { get; }
        public GroundedGeologyFixtureKind FixtureKind { get; }
        public bool IsTemporaryNonCanon { get; }
        public AbsoluteWorldPosition AbsoluteAnchor { get; }
        public GroundedGeologyRotation Orientation { get; }
        public GroundedGeologyBoundsMeters LocalPhysicalBounds { get; }
        public GroundedGeologyAlgorithmVersions AlgorithmVersions { get; }
        public ulong SeedSalt { get; }
        public bool SeedLocked { get; }
        public double SampleSpacingMeters { get; }
        public double BlendWidthMeters { get; }
        public double SeamWidthMeters { get; }
        public double MaterialWavelengthMeters { get; }
        public WorldFeatureId SourceHierarchySignature { get; }
        public WorldFeatureId ContentSignature { get; }
        public IReadOnlyList<GroundedGeologyStructuralElement> StructuralElements => structuralElements;

        internal void AppendCanonicalData(ref WorldStableHashBuilder hash)
        {
            hash.Append(StableId);
            hash.Append(SchemaVersion);
            hash.Append((byte)FixtureKind);
            hash.Append((byte)(IsTemporaryNonCanon ? 1 : 0));
            hash.Append(BitConverter.DoubleToInt64Bits(AbsoluteAnchor.HorizontalA));
            hash.Append(BitConverter.DoubleToInt64Bits(AbsoluteAnchor.Vertical));
            hash.Append(BitConverter.DoubleToInt64Bits(AbsoluteAnchor.HorizontalB));
            Orientation.AppendTo(ref hash);
            LocalPhysicalBounds.AppendTo(ref hash);
            AlgorithmVersions.AppendTo(ref hash);
            hash.Append(SeedSalt);
            hash.Append((byte)(SeedLocked ? 1 : 0));
            hash.Append(BitConverter.DoubleToInt64Bits(SampleSpacingMeters));
            hash.Append(BitConverter.DoubleToInt64Bits(BlendWidthMeters));
            hash.Append(BitConverter.DoubleToInt64Bits(SeamWidthMeters));
            hash.Append(BitConverter.DoubleToInt64Bits(MaterialWavelengthMeters));
            hash.Append(SourceHierarchySignature.High);
            hash.Append(SourceHierarchySignature.Low);
            hash.Append(structuralElements.Count);
            for (var i = 0; i < structuralElements.Count; i++)
            {
                structuralElements[i].AppendTo(ref hash);
            }
        }

        private WorldFeatureId BuildContentSignature()
        {
            var hash = new WorldStableHashBuilder("grounded-geology-recipe-content-v1");
            AppendCanonicalData(ref hash);
            hash.Finish128(out var high, out var low);
            return new WorldFeatureId(high, low);
        }

        private static double RequireNonNegative(double value, string parameterName, bool allowZero)
        {
            value = GroundedGeologyVector3Meters.RequireFinite(value, parameterName);
            if (value < 0d || (!allowZero && !(value > 0d)))
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    allowZero ? "Value cannot be negative." : "Value must be positive.");
            }

            return value;
        }
    }
}
