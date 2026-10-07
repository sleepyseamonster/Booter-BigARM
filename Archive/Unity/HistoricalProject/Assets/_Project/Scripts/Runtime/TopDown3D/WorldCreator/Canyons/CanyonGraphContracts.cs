using System;
using System.Collections.Generic;

namespace BooterBigArm.TopDown3D.WorldCreator
{
    public enum CanyonBoundarySide : byte
    {
        West = 1,
        East = 2,
        South = 3,
        North = 4
    }

    public enum CanyonHierarchyTier : byte
    {
        MainSpine = 1,
        Tributary = 2,
        MinorBranch = 3
    }

    [Flags]
    public enum CanyonNodeRole : ushort
    {
        None = 0,
        BoundaryPort = 1 << 0,
        Spine = 1 << 1,
        Branch = 1 << 2,
        Confluence = 1 << 3,
        Source = 1 << 4,
        Termination = 1 << 5,
        LocalOpportunity = 1 << 6,
        RegroupAnchor = 1 << 7
    }

    public enum CanyonTraversalAccess : byte
    {
        BooterOnly = 1,
        BooterAndBigArm = 2
    }

    public enum CanyonMacroRouteKind : byte
    {
        PrimarySpine = 1,
        BigArmRegroupNetwork = 2,
        BooterOpportunity = 3
    }

    public readonly struct CanyonShelfProfile : IEquatable<CanyonShelfProfile>
    {
        public CanyonShelfProfile(float lower, float middle, float upper)
        {
            Lower = RequireNormalized(lower, nameof(lower));
            Middle = RequireNormalized(middle, nameof(middle));
            Upper = RequireNormalized(upper, nameof(upper));
            if (!(Lower < Middle && Middle < Upper))
            {
                throw new ArgumentException("Canyon shelf levels must be strictly increasing.");
            }
        }

        public float Lower { get; }
        public float Middle { get; }
        public float Upper { get; }

        public bool Equals(CanyonShelfProfile other)
        {
            return Lower.Equals(other.Lower) && Middle.Equals(other.Middle) && Upper.Equals(other.Upper);
        }

        public override bool Equals(object obj)
        {
            return obj is CanyonShelfProfile other && Equals(other);
        }

        public override int GetHashCode()
        {
            return unchecked(Lower.GetHashCode() * 397 ^ Middle.GetHashCode() * 31 ^ Upper.GetHashCode());
        }

        private static float RequireNormalized(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f || value > 1f)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Shelf levels must be finite and normalized.");
            }

            return value;
        }
    }

    public readonly struct CanyonCrossSection : IEquatable<CanyonCrossSection>
    {
        public CanyonCrossSection(float width, float depth, CanyonShelfProfile shelves)
        {
            Width = RequirePositive(width, nameof(width));
            Depth = RequirePositive(depth, nameof(depth));
            Shelves = shelves;
        }

        public float Width { get; }
        public float Depth { get; }
        public CanyonShelfProfile Shelves { get; }

        public bool Equals(CanyonCrossSection other)
        {
            return Width.Equals(other.Width) && Depth.Equals(other.Depth) && Shelves.Equals(other.Shelves);
        }

        public override bool Equals(object obj)
        {
            return obj is CanyonCrossSection other && Equals(other);
        }

        public override int GetHashCode()
        {
            return unchecked(Width.GetHashCode() * 397 ^ Depth.GetHashCode() * 31 ^ Shelves.GetHashCode());
        }

        private static float RequirePositive(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Canyon dimensions must be finite and positive.");
            }

            return value;
        }
    }

    public readonly struct CanyonNodePlan : IEquatable<CanyonNodePlan>
    {
        public CanyonNodePlan(
            WorldFeatureId id,
            WorldPlanKey owner,
            AbsoluteWorldPosition position,
            CanyonCrossSection crossSection,
            CanyonNodeRole role)
        {
            if (id.IsEmpty)
            {
                throw new ArgumentException("Canyon nodes require a stable identity.", nameof(id));
            }

            if (role == CanyonNodeRole.None)
            {
                throw new ArgumentOutOfRangeException(nameof(role));
            }

            Id = id;
            Owner = owner;
            Position = position;
            CrossSection = crossSection;
            Role = role;
        }

        public WorldFeatureId Id { get; }
        public WorldPlanKey Owner { get; }
        public AbsoluteWorldPosition Position { get; }
        public CanyonCrossSection CrossSection { get; }
        public CanyonNodeRole Role { get; }

        public bool Equals(CanyonNodePlan other)
        {
            return Id.Equals(other.Id)
                && Owner.Equals(other.Owner)
                && Position.Equals(other.Position)
                && CrossSection.Equals(other.CrossSection)
                && Role == other.Role;
        }

        public override bool Equals(object obj)
        {
            return obj is CanyonNodePlan other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }

    public readonly struct CanyonTraversalReservation : IEquatable<CanyonTraversalReservation>
    {
        public CanyonTraversalReservation(
            WorldFeatureId id,
            CanyonTraversalAccess access,
            float minimumClearWidth,
            float minimumClearHeight,
            float routeCost)
        {
            if (id.IsEmpty)
            {
                throw new ArgumentException("Traversal reservations require a stable identity.", nameof(id));
            }

            Id = id;
            Access = access;
            MinimumClearWidth = RequirePositive(minimumClearWidth, nameof(minimumClearWidth));
            MinimumClearHeight = RequirePositive(minimumClearHeight, nameof(minimumClearHeight));
            RouteCost = RequirePositive(routeCost, nameof(routeCost));
        }

        public WorldFeatureId Id { get; }
        public CanyonTraversalAccess Access { get; }
        public float MinimumClearWidth { get; }
        public float MinimumClearHeight { get; }
        public float RouteCost { get; }

        public bool SupportsBigArm => Access == CanyonTraversalAccess.BooterAndBigArm;

        public bool Equals(CanyonTraversalReservation other)
        {
            return Id.Equals(other.Id)
                && Access == other.Access
                && MinimumClearWidth.Equals(other.MinimumClearWidth)
                && MinimumClearHeight.Equals(other.MinimumClearHeight)
                && RouteCost.Equals(other.RouteCost);
        }

        public override bool Equals(object obj)
        {
            return obj is CanyonTraversalReservation other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        private static float RequirePositive(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Traversal values must be finite and positive.");
            }

            return value;
        }
    }

    public readonly struct CanyonSegmentPlan : IEquatable<CanyonSegmentPlan>
    {
        public CanyonSegmentPlan(
            WorldFeatureId id,
            WorldPlanKey owner,
            WorldFeatureId fromNodeId,
            WorldFeatureId toNodeId,
            CanyonHierarchyTier hierarchy,
            float profileVariation,
            CanyonTraversalReservation traversal,
            string declaredCycleId = null)
        {
            if (id.IsEmpty || fromNodeId.IsEmpty || toNodeId.IsEmpty)
            {
                throw new ArgumentException("Canyon segments and endpoints require stable identities.");
            }

            if (fromNodeId.Equals(toNodeId))
            {
                throw new ArgumentException("A canyon segment cannot connect a node to itself.");
            }

            if (float.IsNaN(profileVariation)
                || float.IsInfinity(profileVariation)
                || profileVariation < 0f
                || profileVariation > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(profileVariation));
            }

            Id = id;
            Owner = owner;
            FromNodeId = fromNodeId;
            ToNodeId = toNodeId;
            Hierarchy = hierarchy;
            ProfileVariation = profileVariation;
            Traversal = traversal;
            DeclaredCycleId = string.IsNullOrEmpty(declaredCycleId)
                ? string.Empty
                : WorldStableText.Require(declaredCycleId, nameof(declaredCycleId), 128);
        }

        public WorldFeatureId Id { get; }
        public WorldPlanKey Owner { get; }
        public WorldFeatureId FromNodeId { get; }
        public WorldFeatureId ToNodeId { get; }
        public CanyonHierarchyTier Hierarchy { get; }
        public float ProfileVariation { get; }
        public CanyonTraversalReservation Traversal { get; }
        public string DeclaredCycleId { get; }
        public bool DeclaresCycle => DeclaredCycleId.Length > 0;

        public bool Equals(CanyonSegmentPlan other)
        {
            return Id.Equals(other.Id)
                && Owner.Equals(other.Owner)
                && FromNodeId.Equals(other.FromNodeId)
                && ToNodeId.Equals(other.ToNodeId)
                && Hierarchy == other.Hierarchy
                && ProfileVariation.Equals(other.ProfileVariation)
                && Traversal.Equals(other.Traversal)
                && string.Equals(DeclaredCycleId, other.DeclaredCycleId, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is CanyonSegmentPlan other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }

    public readonly struct CanyonBoundaryPortReference : IEquatable<CanyonBoundaryPortReference>
    {
        public CanyonBoundaryPortReference(
            CanyonBoundarySide side,
            WorldPlanBoundaryKey boundary,
            WorldFeatureId portNodeId)
        {
            if (portNodeId.IsEmpty)
            {
                throw new ArgumentException("Boundary ports require a stable node identity.", nameof(portNodeId));
            }

            Side = side;
            Boundary = boundary;
            PortNodeId = portNodeId;
        }

        public CanyonBoundarySide Side { get; }
        public WorldPlanBoundaryKey Boundary { get; }
        public WorldFeatureId PortNodeId { get; }
        public WorldPlanKey CanonicalOwner => Boundary.Owner;

        public bool Equals(CanyonBoundaryPortReference other)
        {
            return Side == other.Side
                && Boundary.Equals(other.Boundary)
                && PortNodeId.Equals(other.PortNodeId);
        }

        public override bool Equals(object obj)
        {
            return obj is CanyonBoundaryPortReference other && Equals(other);
        }

        public override int GetHashCode()
        {
            return unchecked((int)Side * 397 ^ Boundary.GetHashCode() * 31 ^ PortNodeId.GetHashCode());
        }
    }

    public sealed class CanyonMacroRoutePlan
    {
        private readonly IReadOnlyList<WorldFeatureId> segmentIds;

        public CanyonMacroRoutePlan(
            WorldFeatureId id,
            CanyonMacroRouteKind kind,
            CanyonTraversalAccess access,
            IEnumerable<WorldFeatureId> segmentIds)
        {
            if (id.IsEmpty)
            {
                throw new ArgumentException("Macro routes require a stable identity.", nameof(id));
            }

            if (segmentIds == null)
            {
                throw new ArgumentNullException(nameof(segmentIds));
            }

            var sorted = new List<WorldFeatureId>(segmentIds);
            sorted.Sort();
            if (sorted.Count == 0)
            {
                throw new ArgumentException("Macro routes require at least one segment.", nameof(segmentIds));
            }

            for (var i = 1; i < sorted.Count; i++)
            {
                if (sorted[i].Equals(sorted[i - 1]))
                {
                    throw new ArgumentException("Macro routes cannot contain duplicate segments.", nameof(segmentIds));
                }
            }

            Id = id;
            Kind = kind;
            Access = access;
            this.segmentIds = sorted.AsReadOnly();
        }

        public WorldFeatureId Id { get; }
        public CanyonMacroRouteKind Kind { get; }
        public CanyonTraversalAccess Access { get; }
        public IReadOnlyList<WorldFeatureId> SegmentIds => segmentIds;
    }

    public readonly struct CanyonPlanningCost : IEquatable<CanyonPlanningCost>
    {
        public CanyonPlanningCost(int contextSamples, int boundaryEvaluations, int nodeCount, int segmentCount)
        {
            if (contextSamples < 0 || boundaryEvaluations < 0 || nodeCount < 0 || segmentCount < 0)
            {
                throw new ArgumentOutOfRangeException("Planning-cost values cannot be negative.");
            }

            ContextSamples = contextSamples;
            BoundaryEvaluations = boundaryEvaluations;
            NodeCount = nodeCount;
            SegmentCount = segmentCount;
        }

        public int ContextSamples { get; }
        public int BoundaryEvaluations { get; }
        public int NodeCount { get; }
        public int SegmentCount { get; }

        public bool Equals(CanyonPlanningCost other)
        {
            return ContextSamples == other.ContextSamples
                && BoundaryEvaluations == other.BoundaryEvaluations
                && NodeCount == other.NodeCount
                && SegmentCount == other.SegmentCount;
        }

        public override bool Equals(object obj)
        {
            return obj is CanyonPlanningCost other && Equals(other);
        }

        public override int GetHashCode()
        {
            return unchecked(ContextSamples * 397 ^ BoundaryEvaluations * 31 ^ NodeCount * 17 ^ SegmentCount);
        }
    }

    public readonly struct CanyonPlannerProfile : IEquatable<CanyonPlannerProfile>
    {
        public CanyonPlannerProfile(
            double cellSpan,
            int maximumLocalBranchCount,
            float booterClearWidth,
            float booterClearHeight,
            float bigArmClearWidth,
            float bigArmClearHeight,
            bool allowDeclaredCycles,
            int formatVersion)
        {
            if (double.IsNaN(cellSpan) || double.IsInfinity(cellSpan) || cellSpan <= 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(cellSpan));
            }

            if (maximumLocalBranchCount < 1 || maximumLocalBranchCount > 4)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumLocalBranchCount));
            }

            BooterClearWidth = RequirePositive(booterClearWidth, nameof(booterClearWidth));
            BooterClearHeight = RequirePositive(booterClearHeight, nameof(booterClearHeight));
            BigArmClearWidth = RequirePositive(bigArmClearWidth, nameof(bigArmClearWidth));
            BigArmClearHeight = RequirePositive(bigArmClearHeight, nameof(bigArmClearHeight));
            if (BigArmClearWidth < BooterClearWidth || BigArmClearHeight < BooterClearHeight)
            {
                throw new ArgumentException("BigARM clearance cannot be smaller than Booter clearance.");
            }

            if (formatVersion < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(formatVersion));
            }

            CellSpan = cellSpan;
            MaximumLocalBranchCount = maximumLocalBranchCount;
            AllowDeclaredCycles = allowDeclaredCycles;
            FormatVersion = formatVersion;
        }

        public double CellSpan { get; }
        public int MaximumLocalBranchCount { get; }
        public float BooterClearWidth { get; }
        public float BooterClearHeight { get; }
        public float BigArmClearWidth { get; }
        public float BigArmClearHeight { get; }
        public bool AllowDeclaredCycles { get; }
        public int FormatVersion { get; }
        public int MaximumContextSamples => 5;
        public int MaximumBoundaryEvaluations => 4;
        public int MaximumNodes => 6 + MaximumLocalBranchCount;
        public int MaximumSegments => 5 + MaximumLocalBranchCount;
        public WorldPlanHaloPolicy HaloPolicy => new WorldPlanHaloPolicy(1, MaximumContextSamples);

        public static CanyonPlannerProfile CreateNonCanonTechnicalProofProfile()
        {
            return new CanyonPlannerProfile(576d, 2, 2.5f, 3f, 8f, 5f, false, 1);
        }

        public bool Equals(CanyonPlannerProfile other)
        {
            return CellSpan.Equals(other.CellSpan)
                && MaximumLocalBranchCount == other.MaximumLocalBranchCount
                && BooterClearWidth.Equals(other.BooterClearWidth)
                && BooterClearHeight.Equals(other.BooterClearHeight)
                && BigArmClearWidth.Equals(other.BigArmClearWidth)
                && BigArmClearHeight.Equals(other.BigArmClearHeight)
                && AllowDeclaredCycles == other.AllowDeclaredCycles
                && FormatVersion == other.FormatVersion;
        }

        public override bool Equals(object obj)
        {
            return obj is CanyonPlannerProfile other && Equals(other);
        }

        public override int GetHashCode()
        {
            return unchecked(CellSpan.GetHashCode() * 397 ^ MaximumLocalBranchCount * 31 ^ FormatVersion);
        }

        private static float RequirePositive(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Clearances must be finite and positive.");
            }

            return value;
        }
    }
}
