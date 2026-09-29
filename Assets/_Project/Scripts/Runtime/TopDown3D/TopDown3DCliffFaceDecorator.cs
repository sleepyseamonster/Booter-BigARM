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
        private static readonly int GeologicalSeamId = Shader.PropertyToID("_GeologicalSeamAmount");
        private static readonly Mesh[,] cliffStoneLods = new Mesh[5, 3];
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
            var spans = WorldCliffFacePlanner.PlanOwnedSpans(
                candidates, minimumA, maximumA, minimumB, maximumB);
            foreach (var span in spans)
            {
                if (!IsClear(span.First, clearance, runtime, settings,
                        spawnExclusionCenter, formations)
                    || !IsClear(span.Last, clearance, runtime, settings,
                        spawnExclusionCenter, formations)) continue;
                if (TryCreateFace(chunk, settings, runtime, spawnExclusionCenter,
                        formations, span)) yield return 0;
            }
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
            TopDown3DWorldSettings settings,
            WorldCreatorProductionRuntime runtime,
            Vector2 spawnExclusionCenter,
            IReadOnlyList<TopDown3DRockFormationPlan> formations,
            WorldCliffFaceSpan span)
        {
            var first = span.First;
            var last = span.Last;
            if (!TryToChunkLocal(chunk, runtime, first.Rim, out var firstRim)
                || !TryToChunkLocal(chunk, runtime, first.Toe, out var firstToe)
                || !TryToChunkLocal(chunk, runtime, last.Rim, out var lastRim)
                || !TryToChunkLocal(chunk, runtime, last.Toe, out var lastToe)) return false;

            var firstOutward = new Vector3((float)first.OutwardA, 0f, (float)first.OutwardB);
            var lastOutward = new Vector3((float)last.OutwardA, 0f, (float)last.OutwardB);
            var seed = unchecked((int)(Hash01(first.OwnerCellA ^ runtime.Identity.Seed,
                first.OwnerCellB ^ last.OwnerCellA, 131) * int.MaxValue));
            var strataShift = StableStrataShift(first.ParentFeatureId, first.StrataFamilyId);
            var firstRhythm = SectionRhythm(first, strataShift);
            var lastRhythm = SectionRhythm(last, strataShift);
            var near = TopDown3DCliffFaceMeshBuilder.Build(firstRim, firstToe, firstOutward,
                lastRim, lastToe, lastOutward, seed, true, strataShift,
                firstRhythm, lastRhythm);
            var far = TopDown3DCliffFaceMeshBuilder.Build(firstRim, firstToe, firstOutward,
                lastRim, lastToe, lastOutward, seed, false, strataShift,
                firstRhythm, lastRhythm);

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
            AddWorkbenchButtress(face.transform, first, last, firstToe, lastToe,
                firstOutward, lastOutward, seed);
            AddToeDebris(chunk, settings, runtime, spawnExclusionCenter,
                formations, first, last, firstOutward, lastOutward, seed);
            return true;
        }

        private static void AddWorkbenchButtress(Transform parent,
            WorldCliffSectionCandidate first, WorldCliffSectionCandidate last,
            Vector3 firstToe, Vector3 lastToe,
            Vector3 firstOutward, Vector3 lastOutward, int seed)
        {
            var drop = Mathf.Min((float)first.VerticalDrop, (float)last.VerticalDrop);
            if (drop < 4f || Hash01(first.OwnerCellA, first.OwnerCellB, 211) > 0.23d)
                return;
            var variant = Mathf.Min(4, (int)(Hash01(first.OwnerCellA,
                last.OwnerCellB, 223) * 5d));
            if (!TryGetCliffStone(variant, 0, out var source)) return;
            var outward = (firstOutward + lastOutward).normalized;
            var root = new GameObject($"Workbench Cliff Buttress {first.Id}");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = Vector3.Lerp(firstToe, lastToe, 0.5f)
                + outward * 0.25f + Vector3.down * 0.23f;
            root.transform.localRotation = Quaternion.Euler(0f,
                Mathf.Atan2(outward.x, outward.z) * Mathf.Rad2Deg
                    + (float)(Hash01(first.OwnerCellA, last.OwnerCellB, 227) * 40d - 20d),
                0f);
            var scale = Mathf.Lerp(1.25f, 2.05f,
                (float)Hash01(first.OwnerCellA, last.OwnerCellB, 229));
            root.transform.localScale = new Vector3(scale, scale * 2.8f, scale);
            var bounds = source.bounds;
            root.AddComponent<TopDown3DTraversalObstacle>();
            var collider = root.AddComponent<BoxCollider>();
            collider.center = bounds.center;
            collider.size = bounds.size * 0.82f;
            AddStoneLods(root.transform, variant, first, last, seed, bounds.size);
        }

        private static void AddToeDebris(
            TopDown3DGeneratedChunk chunk,
            TopDown3DWorldSettings settings,
            WorldCreatorProductionRuntime runtime,
            Vector2 spawnExclusionCenter,
            IReadOnlyList<TopDown3DRockFormationPlan> formations,
            WorldCliffSectionCandidate first,
            WorldCliffSectionCandidate last,
            Vector3 firstOutward, Vector3 lastOutward, int seed)
        {
            if (Hash01(first.OwnerCellA, first.OwnerCellB, 239) > 0.36d) return;
            var outward = (firstOutward + lastOutward).normalized;
            var distance = 1.4d + Hash01(first.OwnerCellA, last.OwnerCellB, 241) * 2.6d;
            var point = new AbsoluteWorldPosition(
                (first.Toe.HorizontalA + last.Toe.HorizontalA) * 0.5d + outward.x * distance,
                0d,
                (first.Toe.HorizontalB + last.Toe.HorizontalB) * 0.5d + outward.z * distance);
            var chunkSize = (double)settings.ChunkSize;
            if (point.HorizontalA < chunk.Coordinate.x * chunkSize
                || point.HorizontalA >= (chunk.Coordinate.x + 1d) * chunkSize
                || point.HorizontalB < chunk.Coordinate.y * chunkSize
                || point.HorizontalB >= (chunk.Coordinate.y + 1d) * chunkSize)
                return;
            if (!runtime.Query.TrySampleSurface(point, out var surface, out _)
                || (surface.Semantic & (WorldSurfaceSemantic.SiteReservation
                    | WorldSurfaceSemantic.Approach)) != 0
                || !runtime.Query.TrySampleAffordance(point, WorldAgentProfile.BooterProof,
                    out var booter, out _)
                || !runtime.Query.TrySampleAffordance(point, WorldAgentProfile.BigArmProof,
                    out var bigArm, out _)
                || booter.ReservedRoute || bigArm.ReservedRoute
                || booter.SlopeDegrees > 27f || bigArm.SlopeDegrees > 27f
                || !runtime.TryToLocal(surface.Position, out var local)) return;
            var center = new Vector2(local.X, local.Z);
            if (Vector2.Distance(center, spawnExclusionCenter)
                < settings.ClearSpawnRadius + 2f) return;
            if (formations != null)
                for (var i = 0; i < formations.Count; i++)
                    if (Vector2.Distance(center, formations[i].EnvelopeCenter)
                        < formations[i].EnvelopeRadius + 1f) return;

            var variant = Mathf.Min(4, (int)(Hash01(first.OwnerCellA,
                last.OwnerCellB, 251) * 5d));
            if (!TryGetCliffStone(variant, 0, out var source)) return;
            var root = new GameObject($"Cliff Toe Debris {first.Id}");
            root.transform.SetParent(chunk.DecorationRoot, false);
            root.transform.localPosition = chunk.transform.InverseTransformPoint(
                new Vector3(local.X, local.Y, local.Z)) + Vector3.down * 0.12f;
            root.transform.localRotation = Quaternion.Euler(0f,
                (float)(Hash01(first.OwnerCellA, last.OwnerCellB, 257) * 360d), 0f);
            var scale = Mathf.Lerp(0.85f, 1.35f,
                (float)Hash01(first.OwnerCellA, last.OwnerCellB, 263));
            root.transform.localScale = new Vector3(scale, scale * 1.15f, scale);
            root.AddComponent<TopDown3DTraversalObstacle>();
            var collider = root.AddComponent<BoxCollider>();
            collider.center = source.bounds.center;
            collider.size = source.bounds.size * 0.85f;
            AddStoneLods(root.transform, variant, first, last, seed, source.bounds.size);
        }

        private static void AddStoneLods(Transform root, int variant,
            WorldCliffSectionCandidate first, WorldCliffSectionCandidate last,
            int seed, Vector3 sourceSize)
        {
            var renderers = new Renderer[3];
            for (var lod = 0; lod < renderers.Length; lod++)
            {
                if (!TryGetCliffStone(variant, lod, out var mesh)) return;
                var child = new GameObject($"LOD{lod}");
                child.transform.SetParent(root, false);
                child.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = child.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = cliffMaterial;
                ApplyStoneSurface(renderer, sourceSize, first, last, seed);
                renderers[lod] = renderer;
            }
            var group = root.gameObject.AddComponent<LODGroup>();
            group.SetLODs(new[]
            {
                new LOD(0.08f, new[] { renderers[0] }),
                new LOD(0.025f, new[] { renderers[1] }),
                new LOD(0.002f, new[] { renderers[2] })
            });
            group.RecalculateBounds();
        }

        private static bool TryGetCliffStone(int variant, int lod, out Mesh mesh)
        {
            mesh = cliffStoneLods[variant, lod];
            if (mesh != null) return true;
            mesh = Resources.Load<Mesh>($"WorldCreator/CliffStones/CliffStone_{(char)('A' + variant)}_LOD{lod}");
            cliffStoneLods[variant, lod] = mesh;
            return mesh != null;
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
            block.SetFloat(GeologicalSeamId, 0.50f);
            renderer.SetPropertyBlock(block);
        }

        private static float StableStrataShift(WorldFeatureId parent, string strata)
        {
            unchecked
            {
                var hash = 2166136261u;
                var key = parent + ":" + strata;
                for (var i = 0; i < key.Length; i++)
                    hash = (hash ^ key[i]) * 16777619u;
                return ((hash & 1023u) / 1023f - 0.5f) * 0.06f;
            }
        }

        private static float SectionRhythm(WorldCliffSectionCandidate section,
            float parentPhase)
        {
            var distancePhase = section.Center.HorizontalA * 0.052d
                + section.Center.HorizontalB * 0.031d;
            return (float)Math.Sin(distancePhase + parentPhase * 24d);
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
