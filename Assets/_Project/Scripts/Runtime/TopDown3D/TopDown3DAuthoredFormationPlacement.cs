using System;
using System.Collections.Generic;
using BooterBigArm.TopDown3D.WorldCreator;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Realizes an existing canonical reservation; never creates a second geographic scatter.</summary>
    internal static class TopDown3DAuthoredFormationPlacement
    {
        internal const float GameWorldScale = 1.85f;
        private sealed class RejectedSurface : Exception { }

        internal static bool TryBuild(TopDown3DWorldSettings settings, TopDown3DWorldGenerator generator,
            WorldRockFormationPlan reservation, TopDown3DAuthoredFormationAsset template,
            Vector2 spawnCenter, out TopDown3DRockFormationPlan result)
        {
            result = null;
            if (template == null || !template.HasBakedVariants || !template.HasApprovedStage)
                throw new InvalidOperationException("The selected authored world template needs a complete approved workbench-stage bake.");
            if (!generator.TryToLocal(reservation.Center, out var localCenter)) return false;
            var seed = Hash(reservation.Id.ToString());
            var source = template.Members;
            // The workbench's approved rocks supply the detailed mesh library. Its seeded
            // composition rules make a fresh, repeatable arrangement for each world reservation.
            var stage = BuildProceduralStage(template, reservation.Id.ToString());
            var originalBounds = BoundsAt(stage[0].Family.Lod0.bounds, stage[0].LocalPose);
            for (var i = 1; i < stage.Count; i++)
                originalBounds.Encapsulate(BoundsAt(stage[i].Family.Lod0.bounds, stage[i].LocalPose));
            var pivot = originalBounds.center;
            var yaw = (uint)Hash(reservation.Id + ":yaw") / (float)uint.MaxValue * 360f;
            var placement = Matrix4x4.Translate(new Vector3(
                    localCenter.X - pivot.x, 0f, localCenter.Z - pivot.z))
                * Matrix4x4.Translate(pivot)
                * Matrix4x4.Rotate(Quaternion.Euler(0f, yaw, 0f))
                * Matrix4x4.Scale(Vector3.one * GameWorldScale)
                * Matrix4x4.Translate(-pivot);
            var contacts = new List<TopDown3DRockGroundContact.Member>(stage.Count);
            var scales = new Vector3[stage.Count];
            var cache = new Dictionary<Vector2, (float height, Vector3 normal)>();
            (float height, Vector3 normal) Surface(Vector3 point)
            {
                var key = new Vector2(point.x, point.z);
                if (cache.TryGetValue(key, out var cached)) return cached;
                var absolute = generator.ToAbsolute(point.x, 0f, point.z);
                var dx = absolute.HorizontalA - spawnCenter.x;
                var dz = absolute.HorizontalB - spawnCenter.y;
                if (dx * dx + dz * dz < settings.ClearSpawnRadius * settings.ClearSpawnRadius)
                    throw new RejectedSurface();
                if (!generator.Authority.Query.TrySampleSurface(absolute, out var surface, out var error))
                    throw new InvalidOperationException(error);
                var query = generator.Authority.Query;
                WorldAffordanceSample affordance;
                var sampledAffordance = query is UnboundedHybridWorldQueryService unbounded
                    ? unbounded.TrySampleAffordance(absolute, surface, WorldAgentProfile.BigArmProof,
                        out affordance, out error)
                    : query.TrySampleAffordance(absolute, WorldAgentProfile.BigArmProof,
                        out affordance, out error);
                if (!sampledAffordance) throw new InvalidOperationException(error);
                if (affordance.ReservedRoute
                    || (surface.Semantic & (WorldSurfaceSemantic.SiteReservation | WorldSurfaceSemantic.Approach)) != 0
                    || !generator.TryToLocal(surface.Position, out var local)) throw new RejectedSurface();
                var normal = new Vector3(surface.NormalA, surface.NormalVertical, surface.NormalB);
                if (Vector3.Angle(Vector3.up, normal) > 35f) throw new RejectedSurface();
                var sample = (local.Y, normal);
                cache.Add(key, sample);
                return sample;
            }
            try
            {
                for (var i = 0; i < stage.Count; i++)
                {
                    var entry = stage[i];
                    var matrix = placement * entry.LocalPose;
                    scales[i] = matrix.lossyScale;
                    var memberSeed = Hash(reservation.Id + ":authored:" + entry.InstanceId);
                    // The stage bake is the same silhouette the Mixed Formation workbench displays.
                    var selectedMesh = entry.Family.Lod0;
                    var vertices = selectedMesh.vertices;
                    for (var v = 0; v < vertices.Length; v++) vertices[v] = matrix.MultiplyPoint3x4(vertices[v]);
                    var bounds = BoundsAt(selectedMesh.bounds, matrix);
                    contacts.Add(new TopDown3DRockGroundContact.Member(matrix.GetColumn(3), matrix.rotation,
                        bounds, vertices, memberSeed));
                }
                var groundFit = template.SurfaceTreatment;
                // Burial is authored in workbench meters. Scale it with the rock meshes so
                // the exposed proportion survives the larger game-world presentation.
                var poses = TopDown3DRockGroundContact.Fit(contacts, p => Surface(p).height,
                    p => Surface(p).normal, groundFit.ShallowBurial * GameWorldScale,
                    groundFit.DeepBurial * GameWorldScale, groundFit.MaximumGroundTilt);
                var members = new TopDown3DRockFormationMember[stage.Count];
                var envelope = new Bounds();
                for (var i = 0; i < members.Length; i++)
                {
                    var entry = stage[i];
                    var sourceMember = source[entry.SourceIndex];
                    var family = entry.Family;
                    var matrix = Matrix4x4.TRS(poses[i].Position, poses[i].Rotation, scales[i]);
                    var bounds = BoundsAt(family.Lod0.bounds, matrix);
                    // Check final footprint as well as the vertices queried while fitting.
                    for (var corner = 0; corner < 4; corner++)
                        Surface(new Vector3((corner & 1) == 0 ? bounds.min.x : bounds.max.x, 0f,
                            (corner & 2) == 0 ? bounds.min.z : bounds.max.z));
                    var radius = new Vector2(bounds.extents.x, bounds.extents.z).magnitude;
                    members[i] = new TopDown3DRockFormationMember(
                        reservation.Id + ":authored:" + entry.InstanceId, family.StableId,
                        TopDown3DRockSizeTier.Medium, family.Shape, 0, poses[i].Position,
                        poses[i].Rotation, scales[i], i, -1, radius, bounds, family, sourceMember.Material,
                        Surface(bounds.center).height);
                    if (i == 0) envelope = bounds; else envelope.Encapsulate(bounds);
                }
                var span = WorldRockFormationPlanner.FormationReservationSpan;
                var absoluteCenter = generator.ToAbsolute(envelope.center.x, envelope.center.y, envelope.center.z);
                var protectedRadius = settings.ClearSpawnRadius
                    + new Vector2(envelope.extents.x, envelope.extents.z).magnitude;
                var spawnDx = absoluteCenter.HorizontalA - spawnCenter.x;
                var spawnDz = absoluteCenter.HorizontalB - spawnCenter.y;
                if (spawnDx * spawnDx + spawnDz * spawnDz < protectedRadius * protectedRadius) return false;
                var rootKey = new TopDown3DRockRootKey(TopDown3DRockSizeTier.Medium,
                    TopDown3DGeologicalRockAdapter.FoldLegacyCell(Math.Floor(reservation.Center.HorizontalA / span)),
                    TopDown3DGeologicalRockAdapter.FoldLegacyCell(Math.Floor(reservation.Center.HorizontalB / span)),
                    generator.Authority.Identity.Versions.Decoration);
                result = new TopDown3DRockFormationPlan(rootKey, reservation.Id.ToString(), seed,
                    TopDown3DNaturalObjectLayer.Obstacle, TopDown3DRockSurface.Regular, members,
                    new Vector2(envelope.center.x, envelope.center.z),
                    new Vector2(envelope.extents.x, envelope.extents.z).magnitude, envelope.size.y,
                    template);
                return true;
            }
            catch (RejectedSurface) { return false; }
        }

        private static Bounds BoundsAt(Bounds bounds, Matrix4x4 matrix)
        {
            var output = new Bounds(matrix.MultiplyPoint3x4(bounds.center), Vector3.zero);
            for (var i = 0; i < 8; i++)
                output.Encapsulate(matrix.MultiplyPoint3x4(bounds.center + Vector3.Scale(bounds.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1))));
            return output;
        }

        private static int Hash(string text)
        {
            unchecked
            {
                uint hash = 2166136261u;
                foreach (var c in text) hash = (hash ^ c) * 16777619u;
                return (int)hash;
            }
        }

        internal static IReadOnlyList<TopDown3DAuthoredFormationAsset.ApprovedStageEntry>
            BuildProceduralStage(TopDown3DAuthoredFormationAsset template, string reservationId)
        {
            if (template == null || !template.HasBakedVariants || !template.HasApprovedStage)
                throw new ArgumentException("Procedural formation requires a complete workbench bake.", nameof(template));
            if (string.IsNullOrWhiteSpace(reservationId))
                throw new ArgumentException("Procedural formation requires a stable reservation ID.", nameof(reservationId));

            var layout = TopDown3DAuthoredFormationVariation.GenerateLayout(
                template, Hash(reservationId + ":layout"));
            var entries = new TopDown3DAuthoredFormationAsset.ApprovedStageEntry[layout.Entries.Count];
            for (var i = 0; i < entries.Length; i++)
            {
                var layoutEntry = layout.Entries[i];
                var variants = template.Members[layoutEntry.SourceIndex].BakedVariants;
                var variantHash = (uint)Hash(reservationId + ":shape:" + layoutEntry.InstanceId);
                var family = variants[(int)(variantHash % (uint)variants.Count)];
                entries[i] = new TopDown3DAuthoredFormationAsset.ApprovedStageEntry(
                    layoutEntry.SourceIndex, layoutEntry.InstanceId, layoutEntry.Transform, family);
            }
            return entries;
        }

        internal static int SelectGenerationIndex(TopDown3DAuthoredFormationAsset template,
            string reservationId)
        {
            if (template == null || !template.HasApprovedStage)
                throw new ArgumentException("Generation selection requires a complete formation bake.", nameof(template));
            if (string.IsNullOrWhiteSpace(reservationId))
                throw new ArgumentException("Generation selection requires a stable reservation ID.", nameof(reservationId));
            unchecked
            {
                // Catalog selection uses FNV modulo two. Avalanche its bits before the
                // generation modulo so each approved family can reach every generation.
                var value = (uint)Hash(reservationId + ":generation");
                value = (value ^ (value >> 16)) * 0x7FEB352Du;
                value = (value ^ (value >> 15)) * 0x846CA68Bu;
                value ^= value >> 16;
                return (int)(value % (uint)(template.ProceduralGenerations.Count + 1));
            }
        }
    }
}
