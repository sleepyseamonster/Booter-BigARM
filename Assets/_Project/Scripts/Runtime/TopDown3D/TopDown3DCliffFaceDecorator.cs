using System;
using System.Collections.Generic;
using BooterBigArm.TopDown3D.WorldCreator;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    /// <summary>
    /// Connects compatible steep terrain sections into solid bedrock spans. An
    /// absolute section owns each outgoing span, independent of chunk load order.
    /// </summary>
    internal static class TopDown3DCliffFaceDecorator
    {
        private const double CellSpan = 3d;
        private const double MaximumLinkDistance = 4.3d;
        private const double MinimumDirectionAlignment = 0.72d;
        private const double MinimumFacingAlignment = 0.82d;
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

            var profile = new WorldCliffStudyProfile(
                CellSpan, 0.75d, 8, 43f, 1.75d, 3, 5d, 3d);
            var study = new WorldCliffSectionStudy(
                runtime.Identity, runtime.CoordinateModel, runtime.Query, profile);
            var chunkSize = (double)settings.ChunkSize;
            var minimumA = (long)Math.Ceiling(chunk.Coordinate.x * chunkSize / CellSpan - 0.5d);
            var maximumA = (long)Math.Ceiling((chunk.Coordinate.x + 1d) * chunkSize / CellSpan - 0.5d) - 1L;
            var minimumB = (long)Math.Ceiling(chunk.Coordinate.y * chunkSize / CellSpan - 0.5d);
            var maximumB = (long)Math.Ceiling((chunk.Coordinate.y + 1d) * chunkSize / CellSpan - 0.5d) - 1L;
            var candidates = new Dictionary<(long A, long B), WorldCliffSectionCandidate>();

            // Two cells of halo cover each endpoint's own neighbor decision. Both
            // sides of a chunk border therefore see the same candidate chain.
            for (var b = minimumB - 2; b <= maximumB + 2; b++)
            {
                for (var a = minimumA - 2; a <= maximumA + 2; a++)
                {
                    if (study.TryBuild(a, b, out var section, out var error))
                        candidates.Add((a, b), section);
                    else if (error != null)
                        throw new InvalidOperationException(
                            $"Cliff section {a},{b} could not sample the world: {error}");
                }
                yield return 0;
            }

            var clearance = new Dictionary<(long A, long B), bool>();
            for (var b = minimumB; b <= maximumB; b++)
            for (var a = minimumA; a <= maximumA; a++)
            {
                if (!candidates.TryGetValue((a, b), out var first)) continue;
                if (!TryFindNeighbor(first, candidates, true, out var last)) continue;
                if (!TryFindNeighbor(last, candidates, false, out var reverse)
                    || reverse.OwnerCellA != first.OwnerCellA
                    || reverse.OwnerCellB != first.OwnerCellB) continue;
                if (!IsClear(first, clearance, runtime, settings,
                        spawnExclusionCenter, formations)
                    || !IsClear(last, clearance, runtime, settings,
                        spawnExclusionCenter, formations)) continue;
                if (TryCreateFace(chunk, runtime, first, last)) yield return 0;
            }
        }

        private static bool TryFindNeighbor(
            WorldCliffSectionCandidate section,
            Dictionary<(long A, long B), WorldCliffSectionCandidate> candidates,
            bool forward,
            out WorldCliffSectionCandidate result)
        {
            result = default;
            var bestScore = double.NegativeInfinity;
            var tangentA = -section.OutwardB * (forward ? 1d : -1d);
            var tangentB = section.OutwardA * (forward ? 1d : -1d);
            for (var db = -1; db <= 1; db++)
            for (var da = -1; da <= 1; da++)
            {
                if (da == 0 && db == 0) continue;
                if (!candidates.TryGetValue((section.OwnerCellA + da, section.OwnerCellB + db),
                        out var candidate)) continue;
                var deltaA = candidate.Center.HorizontalA - section.Center.HorizontalA;
                var deltaB = candidate.Center.HorizontalB - section.Center.HorizontalB;
                var distance = Math.Sqrt(deltaA * deltaA + deltaB * deltaB);
                if (distance > MaximumLinkDistance) continue;
                var direction = (deltaA * tangentA + deltaB * tangentB) / distance;
                var facing = section.OutwardA * candidate.OutwardA
                    + section.OutwardB * candidate.OutwardB;
                if (direction < MinimumDirectionAlignment || facing < MinimumFacingAlignment)
                    continue;
                if (Math.Abs(candidate.Rim.Vertical - section.Rim.Vertical) > 2.5d
                    || Math.Abs(candidate.Toe.Vertical - section.Toe.Vertical) > 2.5d) continue;
                var score = direction * facing / distance;
                if (score <= bestScore) continue;
                bestScore = score;
                result = candidate;
            }
            return bestScore > double.NegativeInfinity;
        }

        private static bool IsClear(
            WorldCliffSectionCandidate section,
            Dictionary<(long A, long B), bool> clearance,
            WorldCreatorProductionRuntime runtime,
            TopDown3DWorldSettings settings,
            Vector2 spawnExclusionCenter,
            IReadOnlyList<TopDown3DRockFormationPlan> formations)
        {
            var key = (section.OwnerCellA, section.OwnerCellB);
            if (clearance.TryGetValue(key, out var clear)) return clear;
            clear = ClearOfAuthoredGround(section, runtime, settings,
                spawnExclusionCenter, formations);
            clearance.Add(key, clear);
            return clear;
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

        private static bool TryCreateFace(
            TopDown3DGeneratedChunk chunk,
            WorldCreatorProductionRuntime runtime,
            WorldCliffSectionCandidate first,
            WorldCliffSectionCandidate last)
        {
            if (!TryToChunkLocal(chunk, runtime, first.Rim, out var firstRim)
                || !TryToChunkLocal(chunk, runtime, first.Toe, out var firstToe)
                || !TryToChunkLocal(chunk, runtime, last.Rim, out var lastRim)
                || !TryToChunkLocal(chunk, runtime, last.Toe, out var lastToe)) return false;

            var firstOutward = new Vector3((float)first.OutwardA, 0f, (float)first.OutwardB);
            var lastOutward = new Vector3((float)last.OutwardA, 0f, (float)last.OutwardB);
            var seed = unchecked((int)(Hash01(first.OwnerCellA ^ runtime.Identity.Seed,
                first.OwnerCellB ^ last.OwnerCellA, 131) * int.MaxValue));
            var near = TopDown3DCliffFaceMeshBuilder.Build(firstRim, firstToe, firstOutward,
                lastRim, lastToe, lastOutward, seed, true);
            var far = TopDown3DCliffFaceMeshBuilder.Build(firstRim, firstToe, firstOutward,
                lastRim, lastToe, lastOutward, seed, false);

            var face = new GameObject($"Cliff Bedrock {first.Id} to {last.Id}");
            face.transform.SetParent(chunk.DecorationRoot, false);
            face.AddComponent<TopDown3DTraversalObstacle>();
            face.AddComponent<MeshCollider>().sharedMesh = far;
            chunk.RegisterDecorationMesh(near);
            chunk.RegisterDecorationMesh(far);
            var renderers = new Renderer[2];
            for (var lod = 0; lod < 2; lod++)
            {
                var child = new GameObject($"LOD{lod}");
                child.transform.SetParent(face.transform, false);
                child.AddComponent<MeshFilter>().sharedMesh = lod == 0 ? near : far;
                var renderer = child.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = cliffMaterial;
                renderer.receiveShadows = true;
                ApplyStoneSurface(renderer, near.bounds.size, first, last, seed);
                renderers[lod] = renderer;
            }
            var group = face.AddComponent<LODGroup>();
            group.SetLODs(new[]
            {
                new LOD(0.08f, new[] { renderers[0] }),
                new LOD(0.002f, new[] { renderers[1] })
            });
            group.RecalculateBounds();
            return true;
        }

        private static bool TryToChunkLocal(TopDown3DGeneratedChunk chunk,
            WorldCreatorProductionRuntime runtime, AbsoluteWorldPosition position,
            out Vector3 local)
        {
            local = default;
            if (!runtime.TryToLocal(position, out var point)) return false;
            local = chunk.transform.InverseTransformPoint(new Vector3(point.X, point.Y, point.Z));
            return true;
        }

        private static void ApplyStoneSurface(Renderer renderer, Vector3 size,
            WorldCliffSectionCandidate first, WorldCliffSectionCandidate last, int seed)
        {
            var tint = Mathf.Lerp(1.24f, 1.38f,
                (float)Hash01(first.OwnerCellA, last.OwnerCellB, seed));
            var block = new MaterialPropertyBlock();
            block.SetColor(BaseColorId, new Color(tint, tint * 1.005f, tint * 1.015f, 1f));
            block.SetColor(CrackColorId, new Color(0.12f, 0.12f, 0.125f, 1f));
            block.SetColor(MineralColorId, new Color(0.66f, 0.67f, 0.69f, 1f));
            block.SetColor(DustColorId, new Color(0.32f, 0.30f, 0.28f, 1f));
            block.SetFloat(RockSeedId, (float)Hash01(first.OwnerCellA, last.OwnerCellB, 149));
            block.SetVector(RockSizeId, new Vector4(size.x, size.y, size.z, 0f));
            block.SetFloat(GeologyScaleId, 2.4f);
            block.SetFloat(SurfacePatchId, 0.346f);
            block.SetFloat(CrackAmountId, 0.36f);
            block.SetFloat(SideGritId, 0.22f);
            block.SetFloat(UndersideShaleId, 0.20f);
            block.SetFloat(SideShaleId, 0f);
            block.SetFloat(TopShaleId, 0f);
            block.SetFloat(WornShineId, 0.099f);
            renderer.SetPropertyBlock(block);
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
