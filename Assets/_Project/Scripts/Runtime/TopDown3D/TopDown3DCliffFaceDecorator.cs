using System;
using System.Collections.Generic;
using BooterBigArm.TopDown3D.WorldCreator;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    /// <summary>
    /// Builds real baked rock formations on steep terrain. Absolute section owners and
    /// the world seed determine each cluster; chunk load order never chooses a variant.
    /// </summary>
    internal static class TopDown3DCliffFaceDecorator
    {
        private const double CellSpan = 3d;
        private const int MaximumSectionsPerChunk = 10;
        private const float MinimumSectionSpacing = 2.6f;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int CrackColorId = Shader.PropertyToID("_CrackColor");
        private static readonly int MineralColorId = Shader.PropertyToID("_MineralColor");
        private static Material cliffMaterial;

        internal static IEnumerable<int> DecorateSteps(
            TopDown3DGeneratedChunk chunk,
            TopDown3DWorldSettings settings,
            WorldCreatorProductionRuntime runtime,
            Vector2 spawnExclusionCenter,
            IReadOnlyList<TopDown3DRockFormationPlan> formations)
        {
            if (chunk == null || settings == null || runtime == null
                || settings.NaturalObjectCatalog == null) yield break;
            if (cliffMaterial == null)
                cliffMaterial = Resources.Load<Material>("WorldCreator/CliffWall_LightGray");
            if (cliffMaterial == null) yield break;

            var catalog = settings.NaturalObjectCatalog;
            for (var variant = 0; variant < TopDown3DNaturalObjectCatalog.MeshVariantsPerShape; variant++)
                catalog.GetRequiredMeshFamily(TopDown3DNaturalObjectShape.Cliff, variant);

            var profile = new WorldCliffStudyProfile(
                CellSpan, 0.75d, 8, 43f, 1.75d, 3, 5d, 3d);
            var study = new WorldCliffSectionStudy(
                runtime.Identity, runtime.CoordinateModel, runtime.Query, profile);
            var chunkSize = (double)settings.ChunkSize;
            var minimumA = (long)Math.Ceiling(chunk.Coordinate.x * chunkSize / CellSpan - 0.5d);
            var maximumA = (long)Math.Ceiling((chunk.Coordinate.x + 1d) * chunkSize / CellSpan - 0.5d) - 1L;
            var minimumB = (long)Math.Ceiling(chunk.Coordinate.y * chunkSize / CellSpan - 0.5d);
            var maximumB = (long)Math.Ceiling((chunk.Coordinate.y + 1d) * chunkSize / CellSpan - 0.5d) - 1L;
            var candidates = new List<WorldCliffSectionCandidate>(36);

            for (var b = minimumB; b <= maximumB; b++)
            {
                for (var a = minimumA; a <= maximumA; a++)
                {
                    if (study.TryBuild(a, b, out var section, out var error))
                    {
                        if (ClearOfAuthoredGround(section, runtime, settings,
                            spawnExclusionCenter, formations)) candidates.Add(section);
                    }
                    else if (error != null)
                        throw new InvalidOperationException(
                            $"Cliff section {a},{b} could not sample the world: {error}");
                }
                yield return 0;
            }

            // The strongest non-overlapping sections are kept. Sorting by stable ID
            // after admission also gives a repeatable hierarchy order after reload.
            candidates.Sort((left, right) =>
            {
                var drop = right.VerticalDrop.CompareTo(left.VerticalDrop);
                return drop != 0 ? drop : left.Id.CompareTo(right.Id);
            });
            var selected = new List<WorldCliffSectionCandidate>(MaximumSectionsPerChunk);
            for (var i = 0; i < candidates.Count && selected.Count < MaximumSectionsPerChunk; i++)
            {
                var section = candidates[i];
                var clear = true;
                for (var previous = 0; previous < selected.Count; previous++)
                {
                    var deltaA = section.Center.HorizontalA - selected[previous].Center.HorizontalA;
                    var deltaB = section.Center.HorizontalB - selected[previous].Center.HorizontalB;
                    if (deltaA * deltaA + deltaB * deltaB < MinimumSectionSpacing * MinimumSectionSpacing)
                    {
                        clear = false;
                        break;
                    }
                }
                if (clear) selected.Add(section);
            }
            selected.Sort((left, right) => left.Id.CompareTo(right.Id));

            for (var i = 0; i < selected.Count; i++)
            {
                var section = selected[i];
                var cluster = new GameObject($"Cliff Rock Formation {section.Id}");
                cluster.transform.SetParent(chunk.DecorationRoot, false);
                cluster.SetActive(false);
                var created = 0;
                for (var tier = 0; tier < 2; tier++)
                {
                    if (TryCreateRock(chunk, cluster.transform, runtime, catalog,
                        section, tier)) created++;
                    yield return 0;
                }
                if (created > 0)
                {
                    cluster.AddComponent<TopDown3DTraversalObstacle>();
                    cluster.SetActive(true);
                }
                else DestroyOwned(cluster);
            }
        }

        private static bool ClearOfAuthoredGround(
            WorldCliffSectionCandidate section,
            WorldCreatorProductionRuntime runtime,
            TopDown3DWorldSettings settings,
            Vector2 spawnExclusionCenter,
            IReadOnlyList<TopDown3DRockFormationPlan> formations)
        {
            if (!runtime.TryToLocal(section.Center, out var local)) return false;
            var center = new Vector2(local.X, local.Z);
            if (Vector2.Distance(center, spawnExclusionCenter)
                < settings.ClearSpawnRadius + 2.5f) return false;
            if (formations != null)
            {
                for (var i = 0; i < formations.Count; i++)
                    if (Vector2.Distance(center, formations[i].EnvelopeCenter)
                        < formations[i].EnvelopeRadius + 2.5f) return false;
            }

            // A rock extends past the candidate center. Check both approach sides
            // at each tier so its collider does not cover a reserved route or site.
            var tangentA = -section.OutwardB;
            var tangentB = section.OutwardA;
            for (var tier = 0; tier < 2; tier++)
            for (var side = -1; side <= 1; side++)
            {
                var along = tier == 0 ? 0.28d : 0.72d;
                var point = new AbsoluteWorldPosition(
                    section.Toe.HorizontalA + (section.Rim.HorizontalA - section.Toe.HorizontalA) * along
                        + tangentA * side * 1.8d,
                    0d,
                    section.Toe.HorizontalB + (section.Rim.HorizontalB - section.Toe.HorizontalB) * along
                        + tangentB * side * 1.8d);
                if (!runtime.Query.TrySampleSurface(point, out var surface, out var error))
                    throw new InvalidOperationException(error);
                if ((surface.Semantic & WorldSurfaceSemantic.SiteReservation) != 0) return false;
                if (!runtime.Query.TrySampleAffordance(point, WorldAgentProfile.BooterProof,
                        out var booter, out error)
                    || !runtime.Query.TrySampleAffordance(point, WorldAgentProfile.BigArmProof,
                        out var bigArm, out error))
                    throw new InvalidOperationException(error);
                if (booter.ReservedRoute || bigArm.ReservedRoute) return false;
            }
            return true;
        }

        private static bool TryCreateRock(
            TopDown3DGeneratedChunk chunk,
            Transform parent,
            WorldCreatorProductionRuntime runtime,
            TopDown3DNaturalObjectCatalog catalog,
            WorldCliffSectionCandidate section,
            int tier)
        {
            var seed = runtime.Identity.Seed;
            var sectionA = section.OwnerCellA ^ seed;
            var sectionB = section.OwnerCellB;
            var variant = (int)(Hash01(sectionA, sectionB, 31 + tier) *
                TopDown3DNaturalObjectCatalog.MeshVariantsPerShape);
            variant = Mathf.Clamp(variant, 0, TopDown3DNaturalObjectCatalog.MeshVariantsPerShape - 1);
            var family = catalog.GetRequiredMeshFamily(TopDown3DNaturalObjectShape.Cliff, variant);
            var along = tier == 0 ? 0.28d : 0.72d;
            var sideways = (Hash01(sectionA, sectionB, 43 + tier) - 0.5d) * 0.65d;
            var tangentA = -section.OutwardB;
            var tangentB = section.OutwardA;
            var point = new AbsoluteWorldPosition(
                section.Toe.HorizontalA + (section.Rim.HorizontalA - section.Toe.HorizontalA) * along
                    + tangentA * sideways + section.OutwardA * 0.16d,
                0d,
                section.Toe.HorizontalB + (section.Rim.HorizontalB - section.Toe.HorizontalB) * along
                    + tangentB * sideways + section.OutwardB * 0.16d);
            if (!runtime.Query.TrySampleSurface(point, out var surface, out var error))
                throw new InvalidOperationException(error);
            var height = Mathf.Clamp((float)section.VerticalDrop *
                (tier == 0 ? 0.57f : 0.53f), 1.4f, 3.6f);
            var basePoint = new AbsoluteWorldPosition(point.HorizontalA,
                surface.Position.Vertical - 0.28d, point.HorizontalB);
            if (!runtime.TryToLocal(basePoint, out var local)) return false;

            var rock = new GameObject($"Rock {tier + 1} Variant {variant}");
            rock.transform.SetParent(parent, false);
            rock.transform.localPosition = chunk.transform.InverseTransformPoint(
                new Vector3(local.X, local.Y, local.Z));
            var outward = new Vector3((float)section.OutwardA, 0f, (float)section.OutwardB);
            var yaw = (float)((Hash01(sectionA, sectionB, 53 + tier) - 0.5d) * 22d);
            var roll = (float)((Hash01(sectionA, sectionB, 61 + tier) - 0.5d) * 10d);
            rock.transform.localRotation = Quaternion.LookRotation(outward, Vector3.up)
                * Quaternion.Euler(0f, yaw, roll);
            var width = Mathf.Lerp(0.72f, 1.05f,
                (float)Hash01(sectionA, sectionB, 71 + tier));
            var depth = Mathf.Lerp(0.95f, 1.35f,
                (float)Hash01(sectionA, sectionB, 79 + tier));
            rock.transform.localScale = new Vector3(
                width, height / Mathf.Max(0.1f, family.Lod0.bounds.size.y), depth);

            var collider = rock.AddComponent<BoxCollider>();
            collider.center = family.ColliderCenter;
            collider.size = Vector3.Scale(family.ColliderSize, new Vector3(0.82f, 0.88f, 0.8f));
            var renderers = new Renderer[3];
            for (var lod = 0; lod < 3; lod++)
            {
                var lodObject = new GameObject($"LOD{lod}");
                lodObject.transform.SetParent(rock.transform, false);
                lodObject.AddComponent<MeshFilter>().sharedMesh = family.GetLod(lod);
                var renderer = lodObject.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = cliffMaterial;
                renderer.receiveShadows = true;
                var tint = Mathf.Lerp(1.17f, 1.43f,
                    (float)Hash01(sectionA, sectionB, 89 + tier));
                var block = new MaterialPropertyBlock();
                block.SetColor(BaseColorId, new Color(tint, tint * 1.005f, tint * 1.015f, 1f));
                block.SetColor(CrackColorId, new Color(0.12f, 0.12f, 0.125f, 1f));
                block.SetColor(MineralColorId, new Color(0.66f, 0.67f, 0.69f, 1f));
                renderer.SetPropertyBlock(block);
                renderers[lod] = renderer;
            }
            var group = rock.AddComponent<LODGroup>();
            group.SetLODs(new[]
            {
                new LOD(0.11f, new[] { renderers[0] }),
                new LOD(0.035f, new[] { renderers[1] }),
                new LOD(0.003f, new[] { renderers[2] })
            });
            group.RecalculateBounds();
            return true;
        }

        private static void DestroyOwned(UnityEngine.Object target)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(target);
            else UnityEngine.Object.DestroyImmediate(target);
        }

        private static double Hash01(long a, long b, int salt)
        {
            unchecked
            {
                var value = (ulong)a * 0x9E3779B185EBCA87UL
                    ^ (ulong)b * 0xC2B2AE3D27D4EB4FUL ^ (uint)salt;
                value ^= value >> 30;
                value *= 0xBF58476D1CE4E5B9UL;
                value ^= value >> 27;
                value *= 0x94D049BB133111EBUL;
                value ^= value >> 31;
                return (value & 0xFFFFFFUL) / 16777215d;
            }
        }
    }
}
