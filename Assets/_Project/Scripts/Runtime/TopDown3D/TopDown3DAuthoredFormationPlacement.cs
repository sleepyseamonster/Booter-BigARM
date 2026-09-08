using System;
using System.Collections.Generic;
using BooterBigArm.TopDown3D.WorldCreator;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Realizes an existing canonical reservation; never creates a second geographic scatter.</summary>
    internal static class TopDown3DAuthoredFormationPlacement
    {
        private sealed class RejectedSurface : Exception { }

        internal static bool TryBuild(TopDown3DWorldSettings settings, TopDown3DWorldGenerator generator,
            WorldRockFormationPlan reservation, Vector2 spawnCenter, out TopDown3DRockFormationPlan result)
        {
            result = null;
            var template = settings.MixedFormationTemplate;
            if (template == null || !template.HasBakedVariants)
                throw new InvalidOperationException("The mixed world template needs a complete gameplay mesh bake.");
            if (!generator.TryToLocal(reservation.Center, out var localCenter)) return false;
            var seed = Hash(reservation.Id.ToString());
            var source = template.Members;
            var matrices = TopDown3DAuthoredFormationVariation.Generate(template, seed);
            var originalBounds = BoundsAt(source[0].Mesh.bounds, source[0].LocalPose);
            for (var i = 1; i < source.Count; i++)
                originalBounds.Encapsulate(BoundsAt(source[i].Mesh.bounds, source[i].LocalPose));
            var offset = new Vector3(localCenter.X - originalBounds.center.x, 0f,
                localCenter.Z - originalBounds.center.z);
            var contacts = new List<TopDown3DRockGroundContact.Member>(source.Count);
            var scales = new Vector3[source.Count];
            var variants = new int[source.Count];
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
                if (!generator.Authority.Query.TrySampleAffordance(absolute, WorldAgentProfile.BigArmProof,
                    out var affordance, out error)) throw new InvalidOperationException(error);
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
                for (var i = 0; i < source.Count; i++)
                {
                    var matrix = Matrix4x4.Translate(offset) * matrices[i];
                    scales[i] = matrix.lossyScale;
                    var memberSeed = Hash(reservation.Id + ":authored:" + source[i].SourceId);
                    variants[i] = (int)((uint)memberSeed % (uint)source[i].BakedVariants.Count);
                    // Fit the source envelope/geometry first, as in the accepted workbench;
                    // choosing a fresh baked silhouette does not trigger contact repair.
                    var vertices = source[i].Mesh.vertices;
                    for (var v = 0; v < vertices.Length; v++) vertices[v] = matrix.MultiplyPoint3x4(vertices[v]);
                    var bounds = BoundsAt(source[i].Mesh.bounds, matrix);
                    contacts.Add(new TopDown3DRockGroundContact.Member(matrix.GetColumn(3), matrix.rotation,
                        bounds, vertices, memberSeed));
                    Surface(bounds.center);
                }
                var poses = TopDown3DRockGroundContact.Fit(contacts, p => Surface(p).height,
                    p => Surface(p).normal, 0.035f, 0.6f, 20f);
                var members = new TopDown3DRockFormationMember[source.Count];
                var envelope = new Bounds();
                for (var i = 0; i < members.Length; i++)
                {
                    var family = source[i].BakedVariants[variants[i]];
                    var matrix = Matrix4x4.TRS(poses[i].Position, poses[i].Rotation, scales[i]);
                    var bounds = BoundsAt(family.Lod0.bounds, matrix);
                    // Check final footprint as well as the vertices queried while fitting.
                    for (var corner = 0; corner < 4; corner++)
                        Surface(new Vector3((corner & 1) == 0 ? bounds.min.x : bounds.max.x, 0f,
                            (corner & 2) == 0 ? bounds.min.z : bounds.max.z));
                    var radius = new Vector2(bounds.extents.x, bounds.extents.z).magnitude;
                    members[i] = new TopDown3DRockFormationMember(
                        reservation.Id + ":authored:" + source[i].SourceId, family.StableId,
                        TopDown3DRockSizeTier.Medium, family.Shape, variants[i], poses[i].Position,
                        poses[i].Rotation, scales[i], i, -1, radius, bounds, family, source[i].Material);
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
                    new Vector2(envelope.extents.x, envelope.extents.z).magnitude, envelope.size.y);
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
    }
}
