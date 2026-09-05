using System;
using System.Collections.Generic;

namespace BooterBigArm.TopDown3D.WorldCreator.GroundedGeology
{
    [Serializable]
    public readonly struct GroundedGeologyResolutionProfile : IEquatable<GroundedGeologyResolutionProfile>
    {
        public const int DefaultMaximumCellsPerAxis = 96;
        public const int DefaultMaximumSamplePoints = 1_000_000;

        public GroundedGeologyResolutionProfile(
            GroundedGeologyResolutionQuality quality,
            int requestedCellsAcrossMinimum,
            int maximumCellsPerAxis = DefaultMaximumCellsPerAxis,
            int maximumSamplePoints = DefaultMaximumSamplePoints)
        {
            if (!Enum.IsDefined(typeof(GroundedGeologyResolutionQuality), quality))
                throw new ArgumentOutOfRangeException(nameof(quality));
            if (requestedCellsAcrossMinimum < 2)
                throw new ArgumentOutOfRangeException(nameof(requestedCellsAcrossMinimum));
            if (maximumCellsPerAxis < 2)
                throw new ArgumentOutOfRangeException(nameof(maximumCellsPerAxis));
            if (maximumSamplePoints < 27)
                throw new ArgumentOutOfRangeException(nameof(maximumSamplePoints));

            Quality = quality;
            RequestedCellsAcrossMinimum = requestedCellsAcrossMinimum;
            MaximumCellsPerAxis = maximumCellsPerAxis;
            MaximumSamplePoints = maximumSamplePoints;
        }

        public GroundedGeologyResolutionQuality Quality { get; }
        public int RequestedCellsAcrossMinimum { get; }
        public int MaximumCellsPerAxis { get; }
        public int MaximumSamplePoints { get; }

        public static GroundedGeologyResolutionProfile ForQuality(GroundedGeologyResolutionQuality quality)
        {
            var requested = quality switch
            {
                GroundedGeologyResolutionQuality.Draft => 24,
                GroundedGeologyResolutionQuality.Standard => 40,
                GroundedGeologyResolutionQuality.Approval => 64,
                GroundedGeologyResolutionQuality.Diagnostic => 96,
                _ => throw new ArgumentOutOfRangeException(nameof(quality))
            };
            return new GroundedGeologyResolutionProfile(quality, requested);
        }

        public GroundedGeologyResolutionMetrics Resolve(GroundedGeologyBoundsMeters bounds)
        {
            var size = bounds.Size;
            var minimumDimension = size.MinimumComponent;
            if (!(minimumDimension > 0d))
                throw new ArgumentOutOfRangeException(nameof(bounds));

            var requestedStep = minimumDimension / RequestedCellsAcrossMinimum;
            var effectiveStep = Math.Max(requestedStep, size.MaximumComponent / MaximumCellsPerAxis);
            var cellsX = 0;
            var cellsY = 0;
            var cellsZ = 0;
            var samplePoints = 0L;

            for (var iteration = 0; iteration < 16; iteration++)
            {
                cellsX = ResolveCells(size.X, effectiveStep, MaximumCellsPerAxis);
                cellsY = ResolveCells(size.Y, effectiveStep, MaximumCellsPerAxis);
                cellsZ = ResolveCells(size.Z, effectiveStep, MaximumCellsPerAxis);
                samplePoints = checked((long)(cellsX + 1) * (cellsY + 1) * (cellsZ + 1));
                if (samplePoints <= MaximumSamplePoints)
                    break;

                var scale = Math.Pow(samplePoints / (double)MaximumSamplePoints, 1d / 3d);
                effectiveStep *= Math.Max(1.000001d, scale * 1.000001d);
            }

            if (samplePoints > MaximumSamplePoints)
            {
                throw new InvalidOperationException("Resolution could not be reduced under the declared sample budget.");
            }

            return new GroundedGeologyResolutionMetrics(
                RequestedCellsAcrossMinimum,
                minimumDimension / effectiveStep,
                requestedStep,
                effectiveStep,
                cellsX,
                cellsY,
                cellsZ,
                samplePoints,
                effectiveStep > requestedStep * (1d + 1e-10d));
        }

        public bool Equals(GroundedGeologyResolutionProfile other)
        {
            return Quality == other.Quality
                && RequestedCellsAcrossMinimum == other.RequestedCellsAcrossMinimum
                && MaximumCellsPerAxis == other.MaximumCellsPerAxis
                && MaximumSamplePoints == other.MaximumSamplePoints;
        }

        public override bool Equals(object obj)
        {
            return obj is GroundedGeologyResolutionProfile other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)Quality;
                hash = hash * 397 ^ RequestedCellsAcrossMinimum;
                hash = hash * 397 ^ MaximumCellsPerAxis;
                hash = hash * 397 ^ MaximumSamplePoints;
                return hash;
            }
        }

        internal void AppendTo(ref WorldStableHashBuilder hash)
        {
            hash.Append((byte)Quality);
            hash.Append(RequestedCellsAcrossMinimum);
            hash.Append(MaximumCellsPerAxis);
            hash.Append(MaximumSamplePoints);
        }

        private static int ResolveCells(double dimension, double step, int maximum)
        {
            return Math.Max(2, Math.Min(maximum, checked((int)Math.Ceiling(dimension / step))));
        }
    }

    [Serializable]
    public readonly struct GroundedGeologyResolutionMetrics : IEquatable<GroundedGeologyResolutionMetrics>
    {
        public GroundedGeologyResolutionMetrics(
            int requestedCellsAcrossMinimum,
            double effectiveCellsAcrossMinimum,
            double requestedCellSizeMeters,
            double effectiveCellSizeMeters,
            int cellsX,
            int cellsY,
            int cellsZ,
            long samplePointCount,
            bool wasCapped)
        {
            RequestedCellsAcrossMinimum = requestedCellsAcrossMinimum;
            EffectiveCellsAcrossMinimum = effectiveCellsAcrossMinimum;
            RequestedCellSizeMeters = requestedCellSizeMeters;
            EffectiveCellSizeMeters = effectiveCellSizeMeters;
            CellsX = cellsX;
            CellsY = cellsY;
            CellsZ = cellsZ;
            SamplePointCount = samplePointCount;
            WasCapped = wasCapped;
        }

        public int RequestedCellsAcrossMinimum { get; }
        public double EffectiveCellsAcrossMinimum { get; }
        public double RequestedCellSizeMeters { get; }
        public double EffectiveCellSizeMeters { get; }
        public int CellsX { get; }
        public int CellsY { get; }
        public int CellsZ { get; }
        public long SamplePointCount { get; }
        public bool WasCapped { get; }

        public bool Equals(GroundedGeologyResolutionMetrics other)
        {
            return RequestedCellsAcrossMinimum == other.RequestedCellsAcrossMinimum
                && EffectiveCellsAcrossMinimum.Equals(other.EffectiveCellsAcrossMinimum)
                && RequestedCellSizeMeters.Equals(other.RequestedCellSizeMeters)
                && EffectiveCellSizeMeters.Equals(other.EffectiveCellSizeMeters)
                && CellsX == other.CellsX
                && CellsY == other.CellsY
                && CellsZ == other.CellsZ
                && SamplePointCount == other.SamplePointCount
                && WasCapped == other.WasCapped;
        }

        public override bool Equals(object obj)
        {
            return obj is GroundedGeologyResolutionMetrics other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = RequestedCellsAcrossMinimum;
                hash = hash * 397 ^ EffectiveCellsAcrossMinimum.GetHashCode();
                hash = hash * 397 ^ EffectiveCellSizeMeters.GetHashCode();
                hash = hash * 397 ^ CellsX;
                hash = hash * 397 ^ CellsY;
                hash = hash * 397 ^ CellsZ;
                hash = hash * 397 ^ SamplePointCount.GetHashCode();
                return hash;
            }
        }

        internal void AppendTo(ref WorldStableHashBuilder hash)
        {
            hash.Append(RequestedCellsAcrossMinimum);
            hash.Append(BitConverter.DoubleToInt64Bits(EffectiveCellsAcrossMinimum));
            hash.Append(BitConverter.DoubleToInt64Bits(RequestedCellSizeMeters));
            hash.Append(BitConverter.DoubleToInt64Bits(EffectiveCellSizeMeters));
            hash.Append(CellsX);
            hash.Append(CellsY);
            hash.Append(CellsZ);
            hash.Append(SamplePointCount);
            hash.Append((byte)(WasCapped ? 1 : 0));
        }
    }

    public interface IGroundedGeologyFieldDescriptor
    {
        GroundedGeologyFieldKind Kind { get; }
        GroundedGeologyFieldRepresentation Representation { get; }
        GroundedGeologyFieldUnit Unit { get; }
        bool IsReferenceOnly { get; }
    }

    [Serializable]
    public readonly struct GroundedGeologyFieldDescriptor : IGroundedGeologyFieldDescriptor, IEquatable<GroundedGeologyFieldDescriptor>
    {
        public GroundedGeologyFieldDescriptor(
            GroundedGeologyFieldKind kind,
            GroundedGeologyFieldRepresentation representation,
            GroundedGeologyFieldUnit unit,
            bool isReferenceOnly = true)
        {
            var kindValue = (int)kind;
            if (kind == GroundedGeologyFieldKind.None || (kindValue & (kindValue - 1)) != 0)
                throw new ArgumentOutOfRangeException(nameof(kind), "A descriptor names exactly one field.");
            if (!Enum.IsDefined(typeof(GroundedGeologyFieldRepresentation), representation))
                throw new ArgumentOutOfRangeException(nameof(representation));
            if (!Enum.IsDefined(typeof(GroundedGeologyFieldUnit), unit))
                throw new ArgumentOutOfRangeException(nameof(unit));
            Kind = kind;
            Representation = representation;
            Unit = unit;
            IsReferenceOnly = isReferenceOnly;
        }

        public GroundedGeologyFieldKind Kind { get; }
        public GroundedGeologyFieldRepresentation Representation { get; }
        public GroundedGeologyFieldUnit Unit { get; }
        public bool IsReferenceOnly { get; }

        public bool Equals(GroundedGeologyFieldDescriptor other)
        {
            return Kind == other.Kind
                && Representation == other.Representation
                && Unit == other.Unit
                && IsReferenceOnly == other.IsReferenceOnly;
        }

        public override bool Equals(object obj)
        {
            return obj is GroundedGeologyFieldDescriptor other && Equals(other);
        }

        public override int GetHashCode()
        {
            return unchecked((int)Kind * 397 ^ (int)Representation * 31 ^ (int)Unit);
        }

        internal void AppendTo(ref WorldStableHashBuilder hash)
        {
            hash.Append((int)Kind);
            hash.Append((byte)Representation);
            hash.Append((byte)Unit);
            hash.Append((byte)(IsReferenceOnly ? 1 : 0));
        }
    }

    [Serializable]
    public sealed class GroundedGeologyGenerationInput
    {
        public GroundedGeologyGenerationInput(
            WorldIdentity world,
            GroundedGeologyRecipe recipe,
            WorldCoordinateAddress ownerAddress,
            GroundedGeologyEvaluationBounds evaluationBounds,
            GroundedGeologyResolutionProfile resolution,
            GroundedGeologyFieldKind requestedFields,
            WorldFeatureId surfaceContextSignature = default,
            WorldFeatureId authoredConstraintSignature = default)
        {
            if (recipe == null) throw new ArgumentNullException(nameof(recipe));
            if (requestedFields == GroundedGeologyFieldKind.None
                || (requestedFields & ~GroundedGeologyFieldKind.All) != 0)
                throw new ArgumentOutOfRangeException(nameof(requestedFields));

            World = world;
            Recipe = recipe;
            OwnerAddress = ownerAddress;
            EvaluationBounds = evaluationBounds;
            Resolution = resolution;
            RequestedFields = requestedFields;
            SurfaceContextSignature = surfaceContextSignature;
            AuthoredConstraintSignature = authoredConstraintSignature;
        }

        public WorldIdentity World { get; }
        public GroundedGeologyRecipe Recipe { get; }
        public WorldCoordinateAddress OwnerAddress { get; }
        public GroundedGeologyEvaluationBounds EvaluationBounds { get; }
        public GroundedGeologyResolutionProfile Resolution { get; }
        public GroundedGeologyFieldKind RequestedFields { get; }
        public WorldFeatureId SurfaceContextSignature { get; }
        public WorldFeatureId AuthoredConstraintSignature { get; }
    }

    [Serializable]
    public sealed class GroundedGeologyResult
    {
        private readonly IReadOnlyList<GroundedGeologyFieldDescriptor> fields;
        private readonly IReadOnlyList<WorldFeatureId> stableSubfeatureIds;

        internal GroundedGeologyResult(
            WorldFeatureId featureId,
            ulong featureSeed,
            WorldFeatureId signature,
            GroundedGeologyGenerationInput input,
            GroundedGeologyResolutionMetrics resolution,
            GroundedGeologyFieldDescriptor[] fields,
            WorldFeatureId[] stableSubfeatureIds)
        {
            FeatureId = featureId;
            FeatureSeed = featureSeed;
            Signature = signature;
            RecipeContentSignature = input.Recipe.ContentSignature;
            SourceHierarchySignature = input.Recipe.SourceHierarchySignature;
            Bounds = input.EvaluationBounds;
            Resolution = resolution;
            AlgorithmVersions = input.Recipe.AlgorithmVersions;
            IsTemporaryNonCanon = input.Recipe.IsTemporaryNonCanon;
            this.fields = Array.AsReadOnly((GroundedGeologyFieldDescriptor[])fields.Clone());
            this.stableSubfeatureIds = Array.AsReadOnly((WorldFeatureId[])stableSubfeatureIds.Clone());
        }

        public WorldFeatureId FeatureId { get; }
        public ulong FeatureSeed { get; }
        public WorldFeatureId Signature { get; }
        public WorldFeatureId RecipeContentSignature { get; }
        public WorldFeatureId SourceHierarchySignature { get; }
        public GroundedGeologyEvaluationBounds Bounds { get; }
        public GroundedGeologyResolutionMetrics Resolution { get; }
        public GroundedGeologyAlgorithmVersions AlgorithmVersions { get; }
        public bool IsTemporaryNonCanon { get; }
        public IReadOnlyList<GroundedGeologyFieldDescriptor> Fields => fields;
        public IReadOnlyList<WorldFeatureId> StableSubfeatureIds => stableSubfeatureIds;
    }

    /// <summary>
    /// M1 reference compiler. It emits deterministic metadata and placeholder descriptors only;
    /// every physical geology field remains unimplemented until its own authorized milestone.
    /// </summary>
    public static class GroundedGeologyReferenceCompiler
    {
        public static GroundedGeologyResult Compile(GroundedGeologyGenerationInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            GroundedGeologyValidation.RequireValid(input);

            var featureId = GroundedGeologyIdentity.CreateFeatureId(
                input.World,
                input.OwnerAddress,
                input.Recipe,
                0);
            var featureSeed = GroundedGeologyIdentity.CreateFeatureSeed(
                input.World,
                input.OwnerAddress,
                input.Recipe);
            var resolution = input.Resolution.Resolve(input.EvaluationBounds.Expanded);
            var fields = CreateFieldDescriptors(input.RequestedFields);
            var subfeatureIds = new WorldFeatureId[input.Recipe.StructuralElements.Count];
            for (var i = 0; i < subfeatureIds.Length; i++)
            {
                var element = input.Recipe.StructuralElements[i];
                subfeatureIds[i] = GroundedGeologyIdentity.CreateStableSubfeatureId(
                    featureId,
                    element.Kind.ToString(),
                    element.StableKey);
            }

            var signature = BuildResultSignature(input, featureId, featureSeed, resolution, fields, subfeatureIds);
            return new GroundedGeologyResult(
                featureId,
                featureSeed,
                signature,
                input,
                resolution,
                fields,
                subfeatureIds);
        }

        private static GroundedGeologyFieldDescriptor[] CreateFieldDescriptors(GroundedGeologyFieldKind requested)
        {
            var list = new List<GroundedGeologyFieldDescriptor>();
            Add(list, requested, GroundedGeologyFieldKind.FootprintSpine, GroundedGeologyFieldRepresentation.AnalyticTwoDimensional, GroundedGeologyFieldUnit.Meters);
            Add(list, requested, GroundedGeologyFieldKind.TerrainUpliftApron, GroundedGeologyFieldRepresentation.SampledTwoDimensional, GroundedGeologyFieldUnit.Meters);
            Add(list, requested, GroundedGeologyFieldKind.RockOccupancy, GroundedGeologyFieldRepresentation.AnalyticThreeDimensional, GroundedGeologyFieldUnit.Meters);
            Add(list, requested, GroundedGeologyFieldKind.ContactGap, GroundedGeologyFieldRepresentation.SampledTwoDimensional, GroundedGeologyFieldUnit.MixedDocumentedChannels);
            Add(list, requested, GroundedGeologyFieldKind.CreviceConcavity, GroundedGeologyFieldRepresentation.SampledTwoDimensional, GroundedGeologyFieldUnit.Normalized);
            Add(list, requested, GroundedGeologyFieldKind.SlopeCurvature, GroundedGeologyFieldRepresentation.SampledTwoDimensional, GroundedGeologyFieldUnit.MixedDocumentedChannels);
            Add(list, requested, GroundedGeologyFieldKind.WindExposureShelter, GroundedGeologyFieldRepresentation.SampledTwoDimensional, GroundedGeologyFieldUnit.Normalized);
            Add(list, requested, GroundedGeologyFieldKind.Sediment, GroundedGeologyFieldRepresentation.SampledTwoDimensional, GroundedGeologyFieldUnit.Meters);
            Add(list, requested, GroundedGeologyFieldKind.Talus, GroundedGeologyFieldRepresentation.SortedRecords, GroundedGeologyFieldUnit.Normalized);
            Add(list, requested, GroundedGeologyFieldKind.MaterialWeights, GroundedGeologyFieldRepresentation.SampledTwoDimensional, GroundedGeologyFieldUnit.Normalized);
            Add(list, requested, GroundedGeologyFieldKind.TraversalCollision, GroundedGeologyFieldRepresentation.ClassificationBits, GroundedGeologyFieldUnit.MixedDocumentedChannels);
            Add(list, requested, GroundedGeologyFieldKind.ChunkOwnershipHalo, GroundedGeologyFieldRepresentation.DiagnosticOnly, GroundedGeologyFieldUnit.Identifier);
            Add(list, requested, GroundedGeologyFieldKind.SurfaceNormals, GroundedGeologyFieldRepresentation.DiagnosticOnly, GroundedGeologyFieldUnit.Normalized);
            return list.ToArray();
        }

        private static void Add(
            ICollection<GroundedGeologyFieldDescriptor> output,
            GroundedGeologyFieldKind requested,
            GroundedGeologyFieldKind kind,
            GroundedGeologyFieldRepresentation representation,
            GroundedGeologyFieldUnit unit)
        {
            if ((requested & kind) != 0)
                output.Add(new GroundedGeologyFieldDescriptor(kind, representation, unit));
        }

        private static WorldFeatureId BuildResultSignature(
            GroundedGeologyGenerationInput input,
            WorldFeatureId featureId,
            ulong featureSeed,
            GroundedGeologyResolutionMetrics resolution,
            IReadOnlyList<GroundedGeologyFieldDescriptor> fields,
            IReadOnlyList<WorldFeatureId> subfeatureIds)
        {
            var hash = new WorldStableHashBuilder("grounded-geology-reference-result-v1");
            input.World.AppendTo(ref hash);
            input.OwnerAddress.AppendTo(ref hash);
            input.Recipe.AppendCanonicalData(ref hash);
            input.EvaluationBounds.AppendTo(ref hash);
            input.Resolution.AppendTo(ref hash);
            hash.Append((int)input.RequestedFields);
            hash.Append(input.SurfaceContextSignature.High);
            hash.Append(input.SurfaceContextSignature.Low);
            hash.Append(input.AuthoredConstraintSignature.High);
            hash.Append(input.AuthoredConstraintSignature.Low);
            hash.Append(featureId.High);
            hash.Append(featureId.Low);
            hash.Append(featureSeed);
            resolution.AppendTo(ref hash);
            hash.Append(fields.Count);
            for (var i = 0; i < fields.Count; i++) fields[i].AppendTo(ref hash);
            hash.Append(subfeatureIds.Count);
            for (var i = 0; i < subfeatureIds.Count; i++)
            {
                hash.Append(subfeatureIds[i].High);
                hash.Append(subfeatureIds[i].Low);
            }
            hash.Finish128(out var high, out var low);
            return new WorldFeatureId(high, low);
        }
    }

    public static class GroundedGeologyValidation
    {
        public static bool TryValidate(GroundedGeologyGenerationInput input, out string error)
        {
            if (input == null)
            {
                error = "Generation input is required.";
                return false;
            }

            if (input.Recipe == null)
            {
                error = "Generation input requires an immutable recipe.";
                return false;
            }

            if (string.IsNullOrEmpty(input.OwnerAddress.ModelId)
                || string.IsNullOrEmpty(input.OwnerAddress.CanonicalValue))
            {
                error = "Generation input requires a canonical World Creator owner address.";
                return false;
            }

            if (input.Recipe.StructuralElements.Count == 0)
            {
                error = "Recipe has no structural elements.";
                return false;
            }

            if (input.EvaluationBounds.HaloMeters < 0d)
            {
                error = "Evaluation halo cannot be negative.";
                return false;
            }

            if ((input.RequestedFields & ~GroundedGeologyFieldKind.All) != 0
                || input.RequestedFields == GroundedGeologyFieldKind.None)
            {
                error = "At least one known field descriptor must be requested.";
                return false;
            }

            error = null;
            return true;
        }

        public static void RequireValid(GroundedGeologyGenerationInput input)
        {
            if (!TryValidate(input, out var error))
                throw new ArgumentException(error, nameof(input));
        }
    }
}
