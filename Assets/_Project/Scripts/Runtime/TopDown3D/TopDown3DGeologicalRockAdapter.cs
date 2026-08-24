using System;
using System.Collections.Generic;
using BooterBigArm.TopDown3D.WorldCreator;
using Unity.Profiling;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    /// <summary>
    /// Realizes canonical absolute rock plans with the existing baked mesh families and LODs.
    /// It is deliberately downstream of WorldRockFormationPlanner and owns no geographic density.
    /// </summary>
    internal static class TopDown3DGeologicalRockAdapter
    {
        private const int PlacementAttempts = 10;
        private static readonly ProfilerMarker PlanMarker =
            new ProfilerMarker("TopDown3D.World.PlanGeologicalRocks");

        public static List<TopDown3DRockFormationPlan> BuildPhysicalFormations(
            TopDown3DWorldSettings settings,
            TopDown3DWorldGenerator generator,
            TopDown3DNaturalObjectCatalog catalog,
            Vector2Int chunkCoordinate,
            Vector2 spawnExclusionCenter)
        {
            using (PlanMarker.Auto())
            {
                return BuildPhysicalFormationsCore(
                    settings,
                    generator,
                    catalog,
                    chunkCoordinate,
                    spawnExclusionCenter);
            }
        }

        private static List<TopDown3DRockFormationPlan> BuildPhysicalFormationsCore(
            TopDown3DWorldSettings settings,
            TopDown3DWorldGenerator generator,
            TopDown3DNaturalObjectCatalog catalog,
            Vector2Int chunkCoordinate,
            Vector2 spawnExclusionCenter)
        {
            var output = new List<TopDown3DRockFormationPlan>();
            if (settings == null || generator == null || catalog == null)
                return output;

            var authority = generator.Authority;
            var planner = new WorldRockFormationPlanner(
                authority.Identity,
                authority.CoordinateModel,
                authority.ContextProvider,
                authority.Query);
            var chunkSize = settings.ChunkSize;
            // The existing spawn exclusion is authored in absolute prototype world coordinates.
            // Do not reinterpret it through the current local frame after an origin rebase.
            var protectedCenter = new AbsoluteWorldPosition(
                spawnExclusionCenter.x,
                0d,
                spawnExclusionCenter.y);
            var plans = planner.PlanOwnerArea(
                chunkCoordinate.x * (double)chunkSize,
                chunkCoordinate.y * (double)chunkSize,
                chunkSize,
                chunkSize,
                protectedCenter,
                settings.ClearSpawnRadius);
            for (var i = 0; i < plans.Count; i++)
            {
                if (TryRealize(settings, generator, catalog, plans[i], out var formation))
                    output.Add(formation);
            }
            return output;
        }

        private static bool TryRealize(
            TopDown3DWorldSettings settings,
            TopDown3DWorldGenerator generator,
            TopDown3DNaturalObjectCatalog catalog,
            WorldRockFormationPlan source,
            out TopDown3DRockFormationPlan formation)
        {
            formation = null;
            var members = new List<TopDown3DRockFormationMember>(source.Members.Count);
            var sourceToRealized = new Dictionary<int, int>();
            for (var i = 0; i < source.Members.Count; i++)
            {
                var memberPlan = source.Members[i];
                var requestedTier = ToLegacyTier(memberPlan.Scale);
                var definitions = GetDefinitionsWithFallback(catalog, requestedTier, out var realizedTier);
                if (definitions.Count == 0) continue;
                var definition = SelectDefinition(definitions, Stable01(memberPlan.Id, 0x31UL));
                var variant = Mathf.Min(
                    TopDown3DNaturalObjectCatalog.MeshVariantsPerShape - 1,
                    Mathf.FloorToInt(Stable01(memberPlan.Id, 0x47UL)
                        * TopDown3DNaturalObjectCatalog.MeshVariantsPerShape));
                var range = definition.UniformScaleRange;
                var uniform = Mathf.Lerp(range.x, range.y, Stable01(memberPlan.Id, 0x59UL))
                    * memberPlan.SizeFactor;
                var scale = Vector3.Scale(
                    definition.Proportions * uniform,
                    new Vector3(1f / Mathf.Sqrt(memberPlan.AspectFactor), memberPlan.AspectFactor, 1f));

                var parentIndex = -1;
                TopDown3DRockFormationMember parent = default;
                if (memberPlan.ParentIndex >= 0
                    && sourceToRealized.TryGetValue(memberPlan.ParentIndex, out var realizedParentIndex))
                {
                    parentIndex = realizedParentIndex;
                    parent = members[parentIndex];
                }

                if (!TryPlaceMember(
                        generator,
                        catalog,
                        source,
                        memberPlan,
                        definition,
                        realizedTier,
                        variant,
                        scale,
                        parentIndex,
                        parent,
                        members,
                        out var realized))
                    continue;

                sourceToRealized.Add(memberPlan.MemberIndex, members.Count);
                members.Add(realized);
            }

            if (members.Count == 0) return false;
            GetEnvelope(members, out var envelopeCenter, out var envelopeRadius, out var height);
            var root = members[0];
            var reservationSpan = source.ReservationScale == WorldRockReservationScale.LandformAnchor
                ? WorldRockFormationPlanner.LandformReservationSpan
                : WorldRockFormationPlanner.FormationReservationSpan;
            var rootKey = new TopDown3DRockRootKey(
                root.Tier,
                FoldLegacyCell(Math.Floor(source.Center.HorizontalA / reservationSpan)),
                FoldLegacyCell(Math.Floor(source.Center.HorizontalB / reservationSpan)),
                generator.Authority.Identity.Versions.Decoration);
            formation = new TopDown3DRockFormationPlan(
                rootKey,
                source.Id.ToString(),
                source.Id.GetHashCode(),
                source.ReservationScale == WorldRockReservationScale.LandformAnchor
                    ? TopDown3DNaturalObjectLayer.Landmark
                    : TopDown3DNaturalObjectLayer.Obstacle,
                source.DarkRockTendency >= 0.56f
                    ? TopDown3DRockSurface.Dark
                    : TopDown3DRockSurface.Regular,
                members.ToArray(),
                envelopeCenter,
                envelopeRadius,
                height);
            return true;
        }

        private static bool TryPlaceMember(
            TopDown3DWorldGenerator generator,
            TopDown3DNaturalObjectCatalog catalog,
            WorldRockFormationPlan formation,
            WorldRockMemberPlan plan,
            TopDown3DNaturalObjectDefinition definition,
            TopDown3DRockSizeTier tier,
            int variant,
            Vector3 scale,
            int parentIndex,
            TopDown3DRockFormationMember parent,
            IReadOnlyList<TopDown3DRockFormationMember> existing,
            out TopDown3DRockFormationMember realized)
        {
            realized = default;
            var baseDirection = plan.DirectionDegrees;
            for (var attempt = 0; attempt < PlacementAttempts; attempt++)
            {
                var directionDegrees = baseDirection + attempt * 137.50776f;
                var radians = directionDegrees * Mathf.Deg2Rad;
                var direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
                var absoluteA = formation.Center.HorizontalA;
                var absoluteB = formation.Center.HorizontalB;
                if (parentIndex >= 0)
                {
                    var parentAbsolute = generator.ToAbsolute(
                        parent.Position.x,
                        parent.Position.y,
                        parent.Position.z);
                    var provisionalSupport = GetApproximateSupport(catalog, definition.Shape, variant, scale);
                    var separation = (parent.SupportRadius + provisionalSupport) * plan.SeparationFactor;
                    absoluteA = parentAbsolute.HorizontalA + direction.x * separation;
                    absoluteB = parentAbsolute.HorizontalB + direction.y * separation;
                }

                var queryPosition = new AbsoluteWorldPosition(absoluteA, 0d, absoluteB);
                if (!generator.Authority.Query.TrySampleSurface(queryPosition, out var surface, out _)
                    || !generator.Authority.Query.TrySampleAffordance(
                        queryPosition,
                        WorldAgentProfile.BigArmProof,
                        out var affordance,
                        out _)
                    || affordance.ReservedRoute
                    || (surface.Semantic & (WorldSurfaceSemantic.SiteReservation | WorldSurfaceSemantic.Approach)) != 0)
                    continue;
                if (!generator.TryToLocal(surface.Position, out var local)) continue;

                var normal = new Vector3(surface.NormalA, surface.NormalVertical, surface.NormalB);
                var rotation = GetGroundedRotation(
                    definition,
                    normal,
                    formation.StructuralDirectionDegrees + plan.YawOffsetDegrees + attempt * 3.5f);
                var position = new Vector3(
                    local.X,
                    local.Y - definition.SinkDepth * scale.y,
                    local.Z);
                var support = GetProjectedSupportRadius(catalog, definition.Shape, variant, rotation, scale);
                var bounds = GetWorldBounds(catalog, definition.Shape, variant, position, rotation, scale);
                var candidate = new TopDown3DRockFormationMember(
                    plan.Id.ToString(),
                    definition.StableId,
                    tier,
                    definition.Shape,
                    variant,
                    position,
                    rotation,
                    scale,
                    plan.MemberIndex,
                    parentIndex,
                    support,
                    bounds);
                if (!OverlapsNonParent(candidate, existing, parentIndex))
                {
                    realized = candidate;
                    return true;
                }
            }
            return false;
        }

        private static List<TopDown3DNaturalObjectDefinition> GetDefinitionsWithFallback(
            TopDown3DNaturalObjectCatalog catalog,
            TopDown3DRockSizeTier requested,
            out TopDown3DRockSizeTier realized)
        {
            var fallback = new[]
            {
                requested,
                TopDown3DRockSizeTier.Large,
                TopDown3DRockSizeTier.Medium,
                TopDown3DRockSizeTier.ExtraLarge,
                TopDown3DRockSizeTier.Small,
                TopDown3DRockSizeTier.Massive,
                TopDown3DRockSizeTier.Towering
            };
            for (var tierIndex = 0; tierIndex < fallback.Length; tierIndex++)
            {
                var matches = new List<TopDown3DNaturalObjectDefinition>();
                for (var i = 0; i < catalog.Definitions.Count; i++)
                {
                    var definition = catalog.Definitions[i];
                    if (definition != null && definition.RockSizeTier == fallback[tierIndex])
                        matches.Add(definition);
                }
                if (matches.Count > 0)
                {
                    realized = fallback[tierIndex];
                    return matches;
                }
            }
            realized = TopDown3DRockSizeTier.None;
            return new List<TopDown3DNaturalObjectDefinition>();
        }

        private static TopDown3DNaturalObjectDefinition SelectDefinition(
            IReadOnlyList<TopDown3DNaturalObjectDefinition> definitions,
            float selection)
        {
            var total = 0f;
            for (var i = 0; i < definitions.Count; i++) total += definitions[i].Weight;
            var target = selection * total;
            for (var i = 0; i < definitions.Count; i++)
            {
                target -= definitions[i].Weight;
                if (target <= 0f) return definitions[i];
            }
            return definitions[definitions.Count - 1];
        }

        private static TopDown3DRockSizeTier ToLegacyTier(WorldRockMemberScale scale)
        {
            return scale switch
            {
                WorldRockMemberScale.Small => TopDown3DRockSizeTier.Small,
                WorldRockMemberScale.Medium => TopDown3DRockSizeTier.Medium,
                WorldRockMemberScale.Large => TopDown3DRockSizeTier.Large,
                WorldRockMemberScale.ExtraLarge => TopDown3DRockSizeTier.ExtraLarge,
                WorldRockMemberScale.Massive => TopDown3DRockSizeTier.Massive,
                WorldRockMemberScale.Towering => TopDown3DRockSizeTier.Towering,
                _ => TopDown3DRockSizeTier.Medium
            };
        }

        private static Quaternion GetGroundedRotation(
            TopDown3DNaturalObjectDefinition definition,
            Vector3 normal,
            float yawDegrees)
        {
            var slope = Vector3.Angle(normal, Vector3.up);
            var ratio = slope > 0.001f ? Mathf.Min(1f, definition.MaximumTilt / slope) : 0f;
            return Quaternion.Slerp(
                    Quaternion.identity,
                    Quaternion.FromToRotation(Vector3.up, normal),
                    ratio)
                * Quaternion.AngleAxis(yawDegrees, Vector3.up);
        }

        private static float GetApproximateSupport(
            TopDown3DNaturalObjectCatalog catalog,
            TopDown3DNaturalObjectShape shape,
            int variant,
            Vector3 scale)
        {
            var family = catalog.GetRequiredMeshFamily(shape, variant);
            return 0.5f * Mathf.Sqrt(
                Mathf.Pow(family.ColliderSize.x * scale.x, 2f)
                + Mathf.Pow(family.ColliderSize.z * scale.z, 2f));
        }

        private static float GetProjectedSupportRadius(
            TopDown3DNaturalObjectCatalog catalog,
            TopDown3DNaturalObjectShape shape,
            int variant,
            Quaternion rotation,
            Vector3 scale)
        {
            var vertices = catalog.GetRequiredLod0Data(shape, variant).Vertices;
            var maximum = 0f;
            for (var i = 0; i < vertices.Length; i++)
            {
                var point = rotation * Vector3.Scale(vertices[i], scale);
                maximum = Mathf.Max(maximum, new Vector2(point.x, point.z).magnitude);
            }
            return Mathf.Max(0.05f, maximum);
        }

        private static Bounds GetWorldBounds(
            TopDown3DNaturalObjectCatalog catalog,
            TopDown3DNaturalObjectShape shape,
            int variant,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale)
        {
            var vertices = catalog.GetRequiredLod0Data(shape, variant).Vertices;
            var matrix = Matrix4x4.TRS(position, rotation, scale);
            var bounds = new Bounds(matrix.MultiplyPoint3x4(vertices[0]), Vector3.zero);
            for (var i = 1; i < vertices.Length; i++) bounds.Encapsulate(matrix.MultiplyPoint3x4(vertices[i]));
            return bounds;
        }

        private static bool OverlapsNonParent(
            TopDown3DRockFormationMember candidate,
            IReadOnlyList<TopDown3DRockFormationMember> existing,
            int parentIndex)
        {
            var position = new Vector2(candidate.Position.x, candidate.Position.z);
            for (var i = 0; i < existing.Count; i++)
            {
                if (i == parentIndex) continue;
                var other = existing[i];
                var distance = candidate.SupportRadius + other.SupportRadius;
                var otherPosition = new Vector2(other.Position.x, other.Position.z);
                if ((position - otherPosition).sqrMagnitude < distance * distance) return true;
            }
            return false;
        }

        private static void GetEnvelope(
            IReadOnlyList<TopDown3DRockFormationMember> members,
            out Vector2 center,
            out float radius,
            out float height)
        {
            var bounds = members[0].WorldBounds;
            for (var i = 1; i < members.Count; i++) bounds.Encapsulate(members[i].WorldBounds);
            center = new Vector2(bounds.center.x, bounds.center.z);
            radius = 0f;
            for (var i = 0; i < members.Count; i++)
            {
                var position = new Vector2(members[i].Position.x, members[i].Position.z);
                radius = Mathf.Max(radius, Vector2.Distance(center, position) + members[i].SupportRadius);
            }
            height = bounds.size.y;
        }

        private static float Stable01(WorldFeatureId id, ulong salt)
        {
            var value = id.High ^ RotateLeft(id.Low, 29) ^ salt;
            value ^= value >> 30;
            value *= 0xbf58476d1ce4e5b9UL;
            value ^= value >> 27;
            value *= 0x94d049bb133111ebUL;
            value ^= value >> 31;
            return (float)((value >> 40) * (1d / 16777216d));
        }

        private static ulong RotateLeft(ulong value, int count)
        {
            return (value << count) | (value >> (64 - count));
        }

        private static int FoldLegacyCell(double cell)
        {
            // RootKey remains a compatibility/debug carrier. WorldFeatureId is the identity authority.
            var value = checked((long)cell);
            return unchecked((int)value ^ (int)(value >> 32));
        }
    }
}
