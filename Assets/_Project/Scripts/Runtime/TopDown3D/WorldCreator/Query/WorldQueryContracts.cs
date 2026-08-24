using System;

namespace BooterBigArm.TopDown3D.WorldCreator
{
    [Flags]
    public enum WorldSurfaceSemantic : ushort
    {
        None = 0,
        BroadGround = 1 << 0,
        CanyonFloor = 1 << 1,
        CanyonShelf = 1 << 2,
        CanyonWall = 1 << 3,
        BoundedLandform = 1 << 4,
        SiteReservation = 1 << 5,
        Approach = 1 << 6,
        Disturbed = 1 << 7,
        Buried = 1 << 8,
        Weathered = 1 << 9
    }

    public enum WorldAgentKind : byte
    {
        Booter = 1,
        BigArm = 2
    }

    public readonly struct WorldAgentProfile
    {
        public WorldAgentProfile(WorldAgentKind kind, float radius, float height, float maximumSlopeDegrees)
        {
            Kind = kind;
            Radius = SiteIntentReservation.RequirePositive(radius, nameof(radius));
            Height = SiteIntentReservation.RequirePositive(height, nameof(height));
            if (float.IsNaN(maximumSlopeDegrees) || float.IsInfinity(maximumSlopeDegrees)
                || maximumSlopeDegrees <= 0f || maximumSlopeDegrees >= 90f)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumSlopeDegrees));
            }

            MaximumSlopeDegrees = maximumSlopeDegrees;
        }

        public WorldAgentKind Kind { get; }
        public float Radius { get; }
        public float Height { get; }
        public float MaximumSlopeDegrees { get; }

        public static WorldAgentProfile BooterProof => new WorldAgentProfile(WorldAgentKind.Booter, 0.55f, 2f, 43f);
        public static WorldAgentProfile BigArmProof => new WorldAgentProfile(WorldAgentKind.BigArm, 3.5f, 5f, 28f);
    }

    public readonly struct WorldSurfaceSample : IEquatable<WorldSurfaceSample>
    {
        public WorldSurfaceSample(
            AbsoluteWorldPosition position,
            float normalA,
            float normalVertical,
            float normalB,
            WorldSurfaceSemantic semantic,
            WorldFeatureId dominantFeatureId,
            string provinceId,
            string strataFamilyId)
        {
            Position = position;
            NormalA = RequireFinite(normalA, nameof(normalA));
            NormalVertical = RequireFinite(normalVertical, nameof(normalVertical));
            NormalB = RequireFinite(normalB, nameof(normalB));
            Semantic = semantic;
            DominantFeatureId = dominantFeatureId;
            ProvinceId = provinceId ?? string.Empty;
            StrataFamilyId = strataFamilyId ?? string.Empty;
        }

        public AbsoluteWorldPosition Position { get; }
        public float NormalA { get; }
        public float NormalVertical { get; }
        public float NormalB { get; }
        public WorldSurfaceSemantic Semantic { get; }
        public WorldFeatureId DominantFeatureId { get; }
        public string ProvinceId { get; }
        public string StrataFamilyId { get; }

        public bool Equals(WorldSurfaceSample other)
        {
            return Position.Equals(other.Position)
                && NormalA.Equals(other.NormalA)
                && NormalVertical.Equals(other.NormalVertical)
                && NormalB.Equals(other.NormalB)
                && Semantic == other.Semantic
                && DominantFeatureId.Equals(other.DominantFeatureId)
                && string.Equals(ProvinceId, other.ProvinceId, StringComparison.Ordinal)
                && string.Equals(StrataFamilyId, other.StrataFamilyId, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => obj is WorldSurfaceSample other && Equals(other);
        public override int GetHashCode() => Position.GetHashCode();

        private static float RequireFinite(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) throw new ArgumentOutOfRangeException(parameterName);
            return value == 0f ? 0f : value;
        }
    }

    public readonly struct WorldVolumeSample
    {
        public WorldVolumeSample(float signedDistanceToSolid, bool isSolid, WorldFeatureId featureId)
        {
            if (float.IsNaN(signedDistanceToSolid) || float.IsInfinity(signedDistanceToSolid))
                throw new ArgumentOutOfRangeException(nameof(signedDistanceToSolid));
            SignedDistanceToSolid = signedDistanceToSolid;
            IsSolid = isSolid;
            FeatureId = featureId;
        }

        public float SignedDistanceToSolid { get; }
        public bool IsSolid { get; }
        public WorldFeatureId FeatureId { get; }
    }

    public readonly struct WorldAffordanceSample
    {
        public WorldAffordanceSample(bool walkable, bool reservedRoute, float slopeDegrees, WorldFeatureId routeFeatureId)
        {
            Walkable = walkable;
            ReservedRoute = reservedRoute;
            SlopeDegrees = slopeDegrees;
            RouteFeatureId = routeFeatureId;
        }

        public bool Walkable { get; }
        public bool ReservedRoute { get; }
        public float SlopeDegrees { get; }
        public WorldFeatureId RouteFeatureId { get; }
    }

    public interface IWorldQueryService
    {
        bool TrySampleSurface(AbsoluteWorldPosition position, out WorldSurfaceSample sample, out string error);
        bool TrySampleVolume(AbsoluteWorldPosition position, out WorldVolumeSample sample, out string error);
        bool TrySampleAffordance(AbsoluteWorldPosition position, WorldAgentProfile agent, out WorldAffordanceSample sample, out string error);
    }
}
