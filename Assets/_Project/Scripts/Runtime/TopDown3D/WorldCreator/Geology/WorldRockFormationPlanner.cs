using System;
using System.Collections.Generic;

namespace BooterBigArm.TopDown3D.WorldCreator
{
    public enum WorldRockReservationScale : byte
    {
        Formation = 1,
        LandformAnchor = 2
    }

    public enum WorldRockCompositionGoal : byte
    {
        BrokenStack = 1,
        StructuralRidge = 2,
        TalusFan = 3,
        SolitaryCrown = 4
    }

    public enum WorldRockMemberScale : byte
    {
        Small = 1,
        Medium = 2,
        Large = 3,
        ExtraLarge = 4,
        Massive = 5,
        Towering = 6
    }

    public readonly struct WorldRockMemberPlan
    {
        public WorldRockMemberPlan(
            WorldFeatureId id,
            int memberIndex,
            int parentIndex,
            int generation,
            WorldRockMemberScale scale,
            float directionDegrees,
            float separationFactor,
            float sizeFactor,
            float aspectFactor,
            float yawOffsetDegrees)
        {
            if (id.IsEmpty) throw new ArgumentException("Rock members require stable identity.", nameof(id));
            if (memberIndex < 0 || parentIndex >= memberIndex || generation < 0)
                throw new ArgumentOutOfRangeException(nameof(memberIndex));
            Id = id;
            MemberIndex = memberIndex;
            ParentIndex = parentIndex;
            Generation = generation;
            Scale = scale;
            DirectionDegrees = RequireFinite(directionDegrees, nameof(directionDegrees));
            SeparationFactor = RequirePositive(separationFactor, nameof(separationFactor));
            SizeFactor = RequirePositive(sizeFactor, nameof(sizeFactor));
            AspectFactor = RequirePositive(aspectFactor, nameof(aspectFactor));
            YawOffsetDegrees = RequireFinite(yawOffsetDegrees, nameof(yawOffsetDegrees));
        }

        public WorldFeatureId Id { get; }
        public int MemberIndex { get; }
        public int ParentIndex { get; }
        public int Generation { get; }
        public WorldRockMemberScale Scale { get; }
        public float DirectionDegrees { get; }
        public float SeparationFactor { get; }
        public float SizeFactor { get; }
        public float AspectFactor { get; }
        public float YawOffsetDegrees { get; }

        private static float RequireFinite(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                throw new ArgumentOutOfRangeException(parameterName);
            return value == 0f ? 0f : value;
        }

        private static float RequirePositive(float value, string parameterName)
        {
            value = RequireFinite(value, parameterName);
            if (value <= 0f) throw new ArgumentOutOfRangeException(parameterName);
            return value;
        }
    }

    public sealed class WorldRockFormationPlan
    {
        internal WorldRockFormationPlan(
            WorldFeatureId id,
            WorldFeatureId contextId,
            WorldCoordinateAddress ownerAddress,
            AbsoluteWorldPosition center,
            WorldRockReservationScale reservationScale,
            WorldRockCompositionGoal compositionGoal,
            float structuralDirectionDegrees,
            ulong noveltyFingerprint,
            string provinceId,
            string strataFamilyId,
            float darkRockTendency,
            WorldRockMemberPlan[] members)
        {
            Id = id;
            ContextId = contextId;
            OwnerAddress = ownerAddress;
            Center = center;
            ReservationScale = reservationScale;
            CompositionGoal = compositionGoal;
            StructuralDirectionDegrees = structuralDirectionDegrees;
            NoveltyFingerprint = noveltyFingerprint;
            ProvinceId = provinceId ?? string.Empty;
            StrataFamilyId = strataFamilyId ?? string.Empty;
            DarkRockTendency = darkRockTendency;
            Members = Array.AsReadOnly(members);
        }

        public WorldFeatureId Id { get; }
        public WorldFeatureId ContextId { get; }
        public WorldCoordinateAddress OwnerAddress { get; }
        public AbsoluteWorldPosition Center { get; }
        public WorldRockReservationScale ReservationScale { get; }
        public WorldRockCompositionGoal CompositionGoal { get; }
        public float StructuralDirectionDegrees { get; }
        public ulong NoveltyFingerprint { get; }
        public string ProvinceId { get; }
        public string StrataFamilyId { get; }
        public float DarkRockTendency { get; }
        public IReadOnlyList<WorldRockMemberPlan> Members { get; }
    }

    /// <summary>
    /// Pure absolute-space geological composition authority. Chunk coordinates select owners only;
    /// they do not author formation shape, identity, orientation, or genealogy.
    /// </summary>
    public sealed class WorldRockFormationPlanner
    {
        public const double FormationReservationSpan = 32d;
        public const double LandformReservationSpan = 96d;
        private static readonly WorldSeedNamespace FormationNamespace =
            new WorldSeedNamespace(WorldVersionDomain.Decoration, "geological-rock-formations");

        private readonly WorldIdentity world;
        private readonly IWorldCoordinateModel coordinateModel;
        private readonly IWorldCoordinateContextProvider contextProvider;
        private readonly IWorldQueryService query;

        public WorldRockFormationPlanner(
            WorldIdentity world,
            IWorldCoordinateModel coordinateModel,
            IWorldCoordinateContextProvider contextProvider,
            IWorldQueryService query)
        {
            this.world = world;
            this.coordinateModel = coordinateModel ?? throw new ArgumentNullException(nameof(coordinateModel));
            this.contextProvider = contextProvider ?? throw new ArgumentNullException(nameof(contextProvider));
            this.query = query ?? throw new ArgumentNullException(nameof(query));
        }

        public IReadOnlyList<WorldRockFormationPlan> PlanOwnerArea(
            double minimumA,
            double minimumB,
            double spanA,
            double spanB,
            AbsoluteWorldPosition protectedCenter,
            double protectedRadius)
        {
            if (!(spanA > 0d) || !(spanB > 0d) || protectedRadius < 0d)
                throw new ArgumentOutOfRangeException(nameof(spanA));

            var output = new List<WorldRockFormationPlan>(4);
            PlanLattice(
                minimumA,
                minimumB,
                spanA,
                spanB,
                protectedCenter,
                protectedRadius,
                WorldRockReservationScale.LandformAnchor,
                LandformReservationSpan,
                output);
            PlanLattice(
                minimumA,
                minimumB,
                spanA,
                spanB,
                protectedCenter,
                protectedRadius,
                WorldRockReservationScale.Formation,
                FormationReservationSpan,
                output);
            output.Sort((left, right) => left.Id.CompareTo(right.Id));
            return output.AsReadOnly();
        }

        private void PlanLattice(
            double minimumA,
            double minimumB,
            double spanA,
            double spanB,
            AbsoluteWorldPosition protectedCenter,
            double protectedRadius,
            WorldRockReservationScale scale,
            double reservationSpan,
            ICollection<WorldRockFormationPlan> output)
        {
            var maximumA = minimumA + spanA;
            var maximumB = minimumB + spanB;
            var minCellA = checked((long)Math.Floor(minimumA / reservationSpan));
            var maxCellA = checked((long)Math.Floor(maximumA / reservationSpan));
            var minCellB = checked((long)Math.Floor(minimumB / reservationSpan));
            var maxCellB = checked((long)Math.Floor(maximumB / reservationSpan));
            for (var cellB = minCellB; cellB <= maxCellB; cellB++)
            {
                for (var cellA = minCellA; cellA <= maxCellA; cellA++)
                {
                    if (!TryPlanReservation(cellA, cellB, scale, reservationSpan, out var plan))
                        continue;
                    if (plan.Center.HorizontalA < minimumA || plan.Center.HorizontalA >= maximumA
                        || plan.Center.HorizontalB < minimumB || plan.Center.HorizontalB >= maximumB)
                        continue;
                    var deltaA = plan.Center.HorizontalA - protectedCenter.HorizontalA;
                    var deltaB = plan.Center.HorizontalB - protectedCenter.HorizontalB;
                    if (deltaA * deltaA + deltaB * deltaB < protectedRadius * protectedRadius)
                        continue;
                    if (scale == WorldRockReservationScale.Formation
                        && IsInsideAnchorReservation(plan.Center))
                        continue;
                    output.Add(plan);
                }
            }
        }

        private bool TryPlanReservation(
            long cellA,
            long cellB,
            WorldRockReservationScale scale,
            double reservationSpan,
            out WorldRockFormationPlan plan)
        {
            plan = null;
            var nominal = new AbsoluteWorldPosition(
                (cellA + 0.5d) * reservationSpan,
                0d,
                (cellB + 0.5d) * reservationSpan);
            var nominalAddress = coordinateModel.Encode(nominal);
            var stream = FormationNamespace.DeriveSeed(
                world,
                nominalAddress,
                scale == WorldRockReservationScale.LandformAnchor ? 2UL : 1UL);
            var jitter = reservationSpan * (scale == WorldRockReservationScale.LandformAnchor ? 0.24d : 0.30d);
            var centerInput = new AbsoluteWorldPosition(
                nominal.HorizontalA + (Hash01(stream, 11UL) * 2d - 1d) * jitter,
                0d,
                nominal.HorizontalB + (Hash01(stream, 17UL) * 2d - 1d) * jitter);
            var ownerAddress = coordinateModel.Encode(centerInput);
            if (!contextProvider.TrySample(world, coordinateModel, ownerAddress, out var context, out _)
                || !query.TrySampleSurface(centerInput, out var surface, out _)
                || !query.TrySampleAffordance(centerInput, WorldAgentProfile.BigArmProof, out var affordance, out _))
                return false;

            var forbidden = WorldSurfaceSemantic.SiteReservation | WorldSurfaceSemantic.Approach;
            if ((surface.Semantic & forbidden) != 0 || affordance.ReservedRoute)
                return false;

            var density = context.Landscape.VisualDensity;
            var geology = 0.34f * context.StrataFracture
                + 0.26f * context.StrataHardness
                + 0.20f * context.Landscape.Relief
                + 0.20f * context.Landscape.Weathering;
            var admission = scale == WorldRockReservationScale.LandformAnchor
                ? 0.26f + density * 0.38f + geology * 0.24f
                : 0.34f + density * 0.34f + geology * 0.22f;
            if (Hash01(stream, 23UL) > Math.Min(0.92d, admission))
                return false;

            var center = new AbsoluteWorldPosition(
                centerInput.HorizontalA,
                surface.Position.Vertical,
                centerInput.HorizontalB);
            var id = WorldFeatureId.Create(
                world,
                FormationNamespace,
                ownerAddress,
                $"reservation:{(byte)scale}:{cellA}:{cellB}");
            var goal = SelectGoal(stream, context, scale);
            var heading = NormalizeDegrees(
                context.Landscape.StructuralDirection * 360f
                + (float)((Hash01(stream, 31UL) * 2d - 1d)
                    * (34d - context.Landscape.StructuralAnisotropy * 24d)));
            var members = BuildMembers(id, ownerAddress, stream, goal, scale, heading, context);
            var fingerprint = BuildNoveltyFingerprint(goal, heading, members);
            plan = new WorldRockFormationPlan(
                id,
                context.ContextId,
                ownerAddress,
                center,
                scale,
                goal,
                heading,
                fingerprint,
                context.DominantProvinceId,
                context.DominantStrataFamilyId,
                context.DarkRockTendency,
                members);
            return true;
        }

        private WorldRockMemberPlan[] BuildMembers(
            WorldFeatureId formationId,
            WorldCoordinateAddress ownerAddress,
            ulong stream,
            WorldRockCompositionGoal goal,
            WorldRockReservationScale reservationScale,
            float heading,
            WorldCoordinateContext context)
        {
            var minimum = reservationScale == WorldRockReservationScale.LandformAnchor ? 5 : 3;
            var range = reservationScale == WorldRockReservationScale.LandformAnchor ? 5 : 4;
            var memberCount = minimum + (int)Math.Floor(Hash01(stream, 41UL) * range);
            var members = new WorldRockMemberPlan[memberCount];
            for (var i = 0; i < memberCount; i++)
            {
                var memberSeed = Mix(stream + (ulong)(i + 1) * 0x9e3779b97f4a7c15UL);
                var parent = i == 0 ? -1 : SelectParent(goal, i, memberSeed);
                var generation = parent < 0 ? 0 : members[parent].Generation + 1;
                var scale = SelectScale(reservationScale, goal, i, generation, memberSeed);
                var direction = SelectMemberDirection(goal, heading, i, memberSeed);
                var separation = 0.90f + (float)Hash01(memberSeed, 53UL) * 0.24f;
                var size = 0.78f + (float)Hash01(memberSeed, 59UL) * 0.48f;
                var aspect = 0.78f + (float)Hash01(memberSeed, 61UL) * 0.60f
                    + context.StrataFolding * 0.12f;
                var yawOffset = (float)((Hash01(memberSeed, 67UL) * 2d - 1d) * 26d);
                var id = WorldFeatureId.Create(
                    world,
                    FormationNamespace,
                    ownerAddress,
                    $"{formationId}:member:{i}:generation:{generation}");
                members[i] = new WorldRockMemberPlan(
                    id,
                    i,
                    parent,
                    generation,
                    scale,
                    direction,
                    separation,
                    size,
                    aspect,
                    yawOffset);
            }
            return members;
        }

        private bool IsInsideAnchorReservation(AbsoluteWorldPosition position)
        {
            var cellA = checked((long)Math.Floor(position.HorizontalA / LandformReservationSpan));
            var cellB = checked((long)Math.Floor(position.HorizontalB / LandformReservationSpan));
            for (var b = cellB - 1; b <= cellB + 1; b++)
            {
                for (var a = cellA - 1; a <= cellA + 1; a++)
                {
                    if (!TryGetAnchorCenter(a, b, out var center)) continue;
                    var deltaA = center.HorizontalA - position.HorizontalA;
                    var deltaB = center.HorizontalB - position.HorizontalB;
                    if (deltaA * deltaA + deltaB * deltaB < 31d * 31d) return true;
                }
            }
            return false;
        }

        private bool TryGetAnchorCenter(long cellA, long cellB, out AbsoluteWorldPosition center)
        {
            var nominal = new AbsoluteWorldPosition(
                (cellA + 0.5d) * LandformReservationSpan,
                0d,
                (cellB + 0.5d) * LandformReservationSpan);
            var address = coordinateModel.Encode(nominal);
            var stream = FormationNamespace.DeriveSeed(world, address, 2UL);
            var jitter = LandformReservationSpan * 0.24d;
            center = new AbsoluteWorldPosition(
                nominal.HorizontalA + (Hash01(stream, 11UL) * 2d - 1d) * jitter,
                0d,
                nominal.HorizontalB + (Hash01(stream, 17UL) * 2d - 1d) * jitter);
            return true;
        }

        private static WorldRockCompositionGoal SelectGoal(
            ulong stream,
            WorldCoordinateContext context,
            WorldRockReservationScale scale)
        {
            var sample = Hash01(stream, 37UL);
            if (scale == WorldRockReservationScale.LandformAnchor
                && sample < 0.22d + context.Landscape.LandmarkCadence * 0.22d)
                return WorldRockCompositionGoal.SolitaryCrown;
            if (sample < 0.45d + context.Landscape.StructuralAnisotropy * 0.24d)
                return WorldRockCompositionGoal.StructuralRidge;
            if (sample < 0.72d + context.Landscape.Weathering * 0.14d)
                return WorldRockCompositionGoal.TalusFan;
            return WorldRockCompositionGoal.BrokenStack;
        }

        private static int SelectParent(WorldRockCompositionGoal goal, int index, ulong seed)
        {
            if (goal == WorldRockCompositionGoal.StructuralRidge) return index - 1;
            if (goal == WorldRockCompositionGoal.TalusFan && index > 2) return 1 + (index - 2) / 2;
            if (goal == WorldRockCompositionGoal.BrokenStack && index > 2 && Hash01(seed, 71UL) > 0.48d)
                return index - 2;
            return 0;
        }

        private static WorldRockMemberScale SelectScale(
            WorldRockReservationScale reservationScale,
            WorldRockCompositionGoal goal,
            int index,
            int generation,
            ulong seed)
        {
            if (index == 0)
            {
                if (reservationScale == WorldRockReservationScale.LandformAnchor)
                    return goal == WorldRockCompositionGoal.SolitaryCrown
                        ? WorldRockMemberScale.Towering
                        : Hash01(seed, 73UL) > 0.45d
                            ? WorldRockMemberScale.Massive
                            : WorldRockMemberScale.ExtraLarge;
                return Hash01(seed, 79UL) > 0.55d
                    ? WorldRockMemberScale.ExtraLarge
                    : WorldRockMemberScale.Large;
            }
            var rank = reservationScale == WorldRockReservationScale.LandformAnchor ? 4 : 3;
            rank = Math.Max(1, rank - Math.Min(3, generation));
            return (WorldRockMemberScale)rank;
        }

        private static float SelectMemberDirection(
            WorldRockCompositionGoal goal,
            float heading,
            int index,
            ulong seed)
        {
            if (index == 0) return heading;
            var jitter = (float)((Hash01(seed, 83UL) * 2d - 1d) * 18d);
            return goal switch
            {
                WorldRockCompositionGoal.StructuralRidge => NormalizeDegrees(heading + jitter),
                WorldRockCompositionGoal.TalusFan => NormalizeDegrees(heading + 180f + (index - 1) * 29f + jitter),
                WorldRockCompositionGoal.SolitaryCrown => NormalizeDegrees(heading + index * 137.50776f + jitter),
                _ => NormalizeDegrees(heading + index * 91f + jitter)
            };
        }

        private static ulong BuildNoveltyFingerprint(
            WorldRockCompositionGoal goal,
            float heading,
            IReadOnlyList<WorldRockMemberPlan> members)
        {
            var hash = new WorldStableHashBuilder("world-rock-novelty-fingerprint-v1");
            hash.Append((byte)goal);
            hash.Append(members.Count);
            hash.Append((int)Math.Floor(NormalizeDegrees(heading) / 15f));
            for (var i = 0; i < members.Count; i++)
            {
                hash.Append((byte)members[i].Scale);
                hash.Append(members[i].ParentIndex);
                hash.Append((int)Math.Floor(members[i].SizeFactor * 8f));
                hash.Append((int)Math.Floor(members[i].AspectFactor * 8f));
                hash.Append((int)Math.Floor(NormalizeDegrees(members[i].DirectionDegrees) / 20f));
            }
            return hash.Finish64();
        }

        private static float NormalizeDegrees(float value)
        {
            value %= 360f;
            return value < 0f ? value + 360f : value;
        }

        private static double Hash01(ulong seed, ulong salt)
        {
            var value = Mix(seed ^ (salt + 0x9e3779b97f4a7c15UL));
            return (value >> 11) * (1d / 9007199254740992d);
        }

        private static ulong Mix(ulong value)
        {
            value += 0x9e3779b97f4a7c15UL;
            value = (value ^ (value >> 30)) * 0xbf58476d1ce4e5b9UL;
            value = (value ^ (value >> 27)) * 0x94d049bb133111ebUL;
            return value ^ (value >> 31);
        }
    }
}
