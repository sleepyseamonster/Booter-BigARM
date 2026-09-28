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
        private const int MaximumSectionsPerChunk = 6;
        private const float MinimumSectionSpacing = 2.6f;
        private static readonly string[] SourceNames = { "A", "B", "C", "D", "E" };
        private static readonly Mesh[,] SourceMeshes = new Mesh[5, 3];
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int CrackColorId = Shader.PropertyToID("_CrackColor");
        private static readonly int MineralColorId = Shader.PropertyToID("_MineralColor");
        private static readonly int DustColorId = Shader.PropertyToID("_DustColor");
        private static readonly int RockSeedId = Shader.PropertyToID("_RockSeed01");
        private static readonly int RockSizeId = Shader.PropertyToID("_RockSize");
        private static readonly int GeologyScaleId = Shader.PropertyToID("_RockMetersPerTile");
        private static readonly int SurfacePatchId = Shader.PropertyToID("_SurfacePatchStrength");
        private static readonly int CrackAmountId = Shader.PropertyToID("_CrackAmount");
        private static readonly int SideGritId = Shader.PropertyToID("_SideGritAmount");
        private static readonly int UndersideShaleId = Shader.PropertyToID("_UndersideShaleAmount");
        private static readonly int SideShaleId = Shader.PropertyToID("_SideShalePatchAmount");
        private static readonly int TopShaleId = Shader.PropertyToID("_TopShalePatchAmount");
        private static readonly int WornShineId = Shader.PropertyToID("_WornSmoothnessBoost");
        private static Material cliffMaterial;

        internal static IEnumerable<int> DecorateSteps(
            TopDown3DGeneratedChunk chunk,
            TopDown3DWorldSettings settings,
            WorldCreatorProductionRuntime runtime,
            Vector2 spawnExclusionCenter,
            IReadOnlyList<TopDown3DRockFormationPlan> formations)
        {
            if (chunk == null || settings == null || runtime == null) yield break;
            if (cliffMaterial == null)
                cliffMaterial = Resources.Load<Material>("WorldCreator/CliffWall_LightGray");
            if (cliffMaterial == null) yield break;

            LoadSourceMeshes();

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

            // The halo lets adjacent chunks agree on the stronger section at a seam.
            for (var b = minimumB - 1; b <= maximumB + 1; b++)
            {
                for (var a = minimumA - 1; a <= maximumA + 1; a++)
                {
                    if (study.TryBuild(a, b, out var section, out var error))
                        candidates.Add(section);
                    else if (error != null)
                        throw new InvalidOperationException(
                            $"Cliff section {a},{b} could not sample the world: {error}");
                }
                yield return 0;
            }

            // Suppress by absolute-neighborhood dominance, not the order in which a
            // chunk happened to stream. Only the canonical owner instantiates it.
            candidates.Sort((left, right) =>
            {
                var drop = right.VerticalDrop.CompareTo(left.VerticalDrop);
                return drop != 0 ? drop : left.Id.CompareTo(right.Id);
            });
            var selected = new List<WorldCliffSectionCandidate>(MaximumSectionsPerChunk);
            for (var i = 0; i < candidates.Count && selected.Count < MaximumSectionsPerChunk; i++)
            {
                var section = candidates[i];
                if (section.OwnerCellA < minimumA || section.OwnerCellA > maximumA
                    || section.OwnerCellB < minimumB || section.OwnerCellB > maximumB) continue;
                var clear = true;
                for (var previous = 0; previous < i; previous++)
                {
                    var deltaA = section.Center.HorizontalA - candidates[previous].Center.HorizontalA;
                    var deltaB = section.Center.HorizontalB - candidates[previous].Center.HorizontalB;
                    if (deltaA * deltaA + deltaB * deltaB < MinimumSectionSpacing * MinimumSectionSpacing)
                    {
                        clear = false;
                        break;
                    }
                }
                if (clear && ClearOfAuthoredGround(section, runtime, settings,
                        spawnExclusionCenter, formations)) selected.Add(section);
            }
            selected.Sort((left, right) => left.Id.CompareTo(right.Id));

            for (var i = 0; i < selected.Count; i++)
            {
                var section = selected[i];
                var cluster = new GameObject($"Cliff Rock Formation {section.Id}");
                cluster.transform.SetParent(chunk.DecorationRoot, false);
                cluster.SetActive(false);
                var created = 0;
                var count = GetRockCount(section.CenterSlopeDegrees);
                for (var stone = 0; stone < count; stone++)
                {
                    if (TryCreateRock(chunk, cluster.transform, runtime,
                        section, stone, count)) created++;
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

            // Screen the whole assembly envelope before any colliders are placed.
            var tangentA = -section.OutwardB;
            var tangentB = section.OutwardA;
            for (var row = 0; row < 3; row++)
            for (var side = -1; side <= 1; side++)
            {
                var along = 0.18d + row * 0.25d;
                var point = new AbsoluteWorldPosition(
                    section.Toe.HorizontalA + (section.Rim.HorizontalA - section.Toe.HorizontalA) * along
                        + tangentA * side * 1.7d,
                    0d,
                    section.Toe.HorizontalB + (section.Rim.HorizontalB - section.Toe.HorizontalB) * along
                        + tangentB * side * 1.7d);
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
            WorldCliffSectionCandidate section,
            int stone,
            int count)
        {
            var seed = runtime.Identity.Seed;
            var sectionA = section.OwnerCellA ^ seed;
            var sectionB = section.OwnerCellB;
            var recipe = ChooseRecipe(Hash01(sectionA, sectionB, 31 + stone * 97));
            var mesh = SourceMeshes[recipe, 0];
            var shoulder = count == 3;
            var anchor = stone % 2 == 0;
            var along = shoulder ? 0.46d : anchor ? 0.22d : 0.43d;
            along += (Hash01(sectionA, sectionB, 43 + stone) - 0.5d) * 0.10d;
            var sideways = ((stone + 0.5d) / count - 0.5d) * 2.65d;
            sideways += (Hash01(sectionA, sectionB, 67 + stone) - 0.5d) * 0.20d;
            var tangentA = -section.OutwardB;
            var tangentB = section.OutwardA;
            var point = new AbsoluteWorldPosition(
                section.Toe.HorizontalA + (section.Rim.HorizontalA - section.Toe.HorizontalA) * along
                    + tangentA * sideways + section.OutwardA * (anchor ? 0.25d : -0.08d),
                0d,
                section.Toe.HorizontalB + (section.Rim.HorizontalB - section.Toe.HorizontalB) * along
                    + tangentB * sideways + section.OutwardB * (anchor ? 0.25d : -0.08d));
            if (!runtime.Query.TrySampleSurface(point, out var surface, out var error))
                throw new InvalidOperationException(error);
            // The sample's narrow source stones gain their height through individual
            // nonuniform Y scales. Keep that grammar as the slope grows more vertical.
            var heightFraction = shoulder ? 0.48f : count == 5
                ? (anchor ? 0.73f : 0.48f) : (anchor ? 0.94f : 0.57f);
            var height = Mathf.Clamp((float)section.VerticalDrop * heightFraction
                * Mathf.Lerp(0.88f, 1.10f, (float)Hash01(sectionA, sectionB, 79 + stone)),
                shoulder ? 0.8f : 1.15f, 3.35f);
            var verticalScale = Mathf.Clamp(height / Mathf.Max(0.1f, mesh.bounds.size.y),
                1.57f, 4.73f);
            var width = Mathf.Lerp(0.82f, 1.20f,
                (float)Hash01(sectionA, sectionB, 89 + stone));
            var widthScale = Mathf.Clamp(width / Mathf.Max(0.1f, mesh.bounds.size.x), 0.71f, 1.54f);
            var depthScale = Mathf.Clamp(Mathf.Lerp(0.85f, 1.17f,
                (float)Hash01(sectionA, sectionB, 97 + stone))
                / Mathf.Max(0.1f, mesh.bounds.size.z), 0.71f, 1.42f);
            var burial = shoulder ? 0.20f : 0.32f;
            var basePoint = new AbsoluteWorldPosition(point.HorizontalA,
                surface.Position.Vertical - burial - mesh.bounds.min.y * verticalScale,
                point.HorizontalB);
            if (!runtime.TryToLocal(basePoint, out var local)) return false;

            var rock = new GameObject($"Rock {stone + 1} Source {SourceNames[recipe]}");
            rock.transform.SetParent(parent, false);
            rock.transform.localPosition = chunk.transform.InverseTransformPoint(
                new Vector3(local.X, local.Y, local.Z));
            var outward = new Vector3((float)section.OutwardA, 0f, (float)section.OutwardB);
            var yaw = (float)((Hash01(sectionA, sectionB, 53 + stone) - 0.5d) * 28d);
            var roll = (float)((Hash01(sectionA, sectionB, 61 + stone) - 0.5d) * 12d);
            var lean = -Mathf.Clamp((68f - section.CenterSlopeDegrees) * 0.35f, 0f, 10f);
            rock.transform.localRotation = Quaternion.LookRotation(outward, Vector3.up)
                * Quaternion.Euler(lean, yaw, roll);
            rock.transform.localScale = new Vector3(widthScale, verticalScale, depthScale);

            var collider = rock.AddComponent<BoxCollider>();
            collider.center = mesh.bounds.center;
            collider.size = Vector3.Scale(mesh.bounds.size, new Vector3(0.78f, 0.90f, 0.76f));
            var renderers = new Renderer[3];
            for (var lod = 0; lod < 3; lod++)
            {
                var lodObject = new GameObject($"LOD{lod}");
                lodObject.transform.SetParent(rock.transform, false);
                lodObject.AddComponent<MeshFilter>().sharedMesh = SourceMeshes[recipe, lod];
                var renderer = lodObject.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = cliffMaterial;
                renderer.receiveShadows = true;
                var tint = Mathf.Lerp(1.20f, 1.43f,
                    (float)Hash01(sectionA, sectionB, 107 + stone));
                var block = new MaterialPropertyBlock();
                block.SetColor(BaseColorId, new Color(tint, tint * 1.005f, tint * 1.015f, 1f));
                block.SetColor(CrackColorId, new Color(0.12f, 0.12f, 0.125f, 1f));
                block.SetColor(MineralColorId, new Color(0.66f, 0.67f, 0.69f, 1f));
                block.SetColor(DustColorId, new Color(0.32f, 0.30f, 0.28f, 1f));
                block.SetFloat(RockSeedId, (float)Hash01(sectionA, sectionB, 119 + stone));
                block.SetVector(RockSizeId, new Vector4(mesh.bounds.size.x * widthScale,
                    mesh.bounds.size.y * verticalScale, mesh.bounds.size.z * depthScale, 0f));
                block.SetFloat(GeologyScaleId, 2.4f);
                block.SetFloat(SurfacePatchId, 0.346f);
                block.SetFloat(CrackAmountId, 0.36f);
                block.SetFloat(SideGritId, 0.22f);
                block.SetFloat(UndersideShaleId, 0.20f);
                block.SetFloat(SideShaleId, 0f);
                block.SetFloat(TopShaleId, 0f);
                block.SetFloat(WornShineId, 0.099f);
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

        private static int GetRockCount(float slope)
        {
            if (slope < 50f) return 3;
            return slope < 60f ? 5 : 7;
        }

        private static int ChooseRecipe(double value)
        {
            if (value < 0.55d) return 2; // The sample uses source C most often.
            if (value < 0.77d) return 3;
            if (value < 0.85d) return 0;
            if (value < 0.93d) return 1;
            return 4;
        }

        private static void LoadSourceMeshes()
        {
            for (var recipe = 0; recipe < SourceNames.Length; recipe++)
            for (var lod = 0; lod < 3; lod++)
            {
                if (SourceMeshes[recipe, lod] != null) continue;
                var path = $"WorldCreator/CliffStones/CliffStone_{SourceNames[recipe]}_LOD{lod}";
                SourceMeshes[recipe, lod] = Resources.Load<Mesh>(path);
                if (SourceMeshes[recipe, lod] == null)
                    throw new InvalidOperationException("Missing baked cliff stone: " + path);
            }
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
