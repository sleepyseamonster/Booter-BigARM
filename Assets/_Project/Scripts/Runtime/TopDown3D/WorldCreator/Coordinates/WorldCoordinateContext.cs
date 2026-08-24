using System;
using System.Collections.Generic;

namespace BooterBigArm.TopDown3D.WorldCreator
{
    public readonly struct WorldLandscapeInfluenceContribution : IEquatable<WorldLandscapeInfluenceContribution>
    {
        public WorldLandscapeInfluenceContribution(
            string influenceId,
            string provinceId,
            string strataFamilyId,
            float weight)
        {
            InfluenceId = WorldStableText.Require(influenceId, nameof(influenceId), 128);
            ProvinceId = WorldStableText.Require(provinceId, nameof(provinceId), 128);
            StrataFamilyId = WorldStableText.Require(strataFamilyId, nameof(strataFamilyId), 128);
            Weight = WorldLandscapeParameters.RequireNormalized(weight, nameof(weight));
        }

        public string InfluenceId { get; }
        public string ProvinceId { get; }
        public string StrataFamilyId { get; }
        public float Weight { get; }

        public bool Equals(WorldLandscapeInfluenceContribution other)
        {
            return string.Equals(InfluenceId, other.InfluenceId, StringComparison.Ordinal)
                && string.Equals(ProvinceId, other.ProvinceId, StringComparison.Ordinal)
                && string.Equals(StrataFamilyId, other.StrataFamilyId, StringComparison.Ordinal)
                && Weight.Equals(other.Weight);
        }

        public override bool Equals(object obj)
        {
            return obj is WorldLandscapeInfluenceContribution other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = StringComparer.Ordinal.GetHashCode(InfluenceId ?? string.Empty);
                hash = hash * 397 ^ StringComparer.Ordinal.GetHashCode(ProvinceId ?? string.Empty);
                hash = hash * 397 ^ StringComparer.Ordinal.GetHashCode(StrataFamilyId ?? string.Empty);
                hash = hash * 397 ^ Weight.GetHashCode();
                return hash;
            }
        }
    }

    public sealed class WorldLandscapeInfluenceSet
    {
        private readonly IReadOnlyList<WorldLandscapeInfluenceContribution> contributions;

        public WorldLandscapeInfluenceSet(
            IEnumerable<WorldLandscapeInfluenceContribution> contributions,
            float declaredDiscontinuitySignal)
        {
            if (contributions == null)
            {
                throw new ArgumentNullException(nameof(contributions));
            }

            var sortedContributions = new List<WorldLandscapeInfluenceContribution>(contributions).ToArray();
            Array.Sort(
                sortedContributions,
                (left, right) => string.CompareOrdinal(left.InfluenceId, right.InfluenceId));
            this.contributions = Array.AsReadOnly(sortedContributions);
            DeclaredDiscontinuitySignal = WorldLandscapeParameters.RequireNormalized(
                declaredDiscontinuitySignal,
                nameof(declaredDiscontinuitySignal));
        }

        public IReadOnlyList<WorldLandscapeInfluenceContribution> Contributions => contributions;
        public float DeclaredDiscontinuitySignal { get; }
    }

    public interface IWorldLandscapeInfluenceField
    {
        string StableId { get; }
        int Version { get; }
        bool NonCanonProofOnly { get; }

        WorldLandscapeInfluenceSet Evaluate(AbsoluteWorldPosition absolutePosition);
    }

    public sealed class WorldCoordinateContext
    {
        private readonly IReadOnlyList<WorldLandscapeInfluenceContribution> contributions;

        internal WorldCoordinateContext(
            WorldCoordinateAddress address,
            AbsoluteWorldPosition absolutePosition,
            WorldFeatureId contextId,
            WorldLandscapeInfluenceContribution[] contributions,
            string dominantProvinceId,
            string dominantStrataFamilyId,
            WorldLandscapeParameters landscape,
            float strataHardness,
            float strataFolding,
            float strataFracture,
            float ironOxideTendency,
            float paleDepositTendency,
            float darkRockTendency,
            float declaredDiscontinuitySignal)
        {
            Address = address;
            AbsolutePosition = absolutePosition;
            ContextId = contextId;
            this.contributions = Array.AsReadOnly(
                (WorldLandscapeInfluenceContribution[])contributions.Clone());
            DominantProvinceId = dominantProvinceId;
            DominantStrataFamilyId = dominantStrataFamilyId;
            Landscape = landscape;
            StrataHardness = strataHardness;
            StrataFolding = strataFolding;
            StrataFracture = strataFracture;
            IronOxideTendency = ironOxideTendency;
            PaleDepositTendency = paleDepositTendency;
            DarkRockTendency = darkRockTendency;
            DeclaredDiscontinuitySignal = declaredDiscontinuitySignal;
        }

        public WorldCoordinateAddress Address { get; }
        public AbsoluteWorldPosition AbsolutePosition { get; }
        public WorldFeatureId ContextId { get; }
        public IReadOnlyList<WorldLandscapeInfluenceContribution> Contributions => contributions;
        public string DominantProvinceId { get; }
        public string DominantStrataFamilyId { get; }
        public WorldLandscapeParameters Landscape { get; }
        public float StrataHardness { get; }
        public float StrataFolding { get; }
        public float StrataFracture { get; }
        public float IronOxideTendency { get; }
        public float PaleDepositTendency { get; }
        public float DarkRockTendency { get; }
        public float DeclaredDiscontinuitySignal { get; }
    }

    public interface IWorldCoordinateContextProvider
    {
        bool TrySample(
            WorldIdentity world,
            IWorldCoordinateModel coordinateModel,
            WorldCoordinateAddress address,
            out WorldCoordinateContext context,
            out string error);
    }
}
