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
        private static Material terrainFaceMaterial;

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
                CellSpan, 0.75d, 8, 43f, 1.75d, 3, 9d, 4.5d);
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
            var nearTerrainHeights = new Dictionary<(long A, long B), float>();
            var spans = WorldCliffFacePlanner.PlanOwnedSpans(
                candidates, minimumA, maximumA, minimumB, maximumB);
            foreach (var span in spans)
            {
                if (!IsClear(span.First, clearance, runtime, settings,
                        spawnExclusionCenter, formations)
                    || !IsClear(span.Last, clearance, runtime, settings,
                        spawnExclusionCenter, formations)) continue;
                if (TryCreateFace(chunk, settings, runtime, spawnExclusionCenter,
                        formations, span, nearTerrainHeights)) yield return 0;
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
            WorldCliffFaceSpan span,
            Dictionary<(long A, long B), float> nearTerrainHeights)
        {
            var first = span.First;
            var last = span.Last;
            if (!TryToChunkLocal(chunk, settings, runtime, first.Rim, out var firstRim)
                || !TryToChunkLocal(chunk, settings, runtime, first.Toe, out var firstToe)
                || !TryToChunkLocal(chunk, settings, runtime, last.Rim, out var lastRim)
                || !TryToChunkLocal(chunk, settings, runtime, last.Toe, out var lastToe)) return false;

            var firstOutward = new Vector3((float)first.OutwardA, 0f, (float)first.OutwardB);
            var lastOutward = new Vector3((float)last.OutwardA, 0f, (float)last.OutwardB);
            var seed = unchecked((int)(Hash01(first.OwnerCellA ^ runtime.Identity.Seed,
                first.OwnerCellB ^ last.OwnerCellA, 131) * int.MaxValue));
            var strataShift = StableStrataShift(first.ParentFeatureId, first.StrataFamilyId);
            var firstRhythm = SectionRhythm(first, strataShift);
            var lastRhythm = SectionRhythm(last, strataShift);
            var rockParent = chunk.DecorationRoot;
            if (runtime.Profile.RenderFittedCliffFaces
                && Environment.GetEnvironmentVariable("CANYON_DISABLE_FITTED_FACE") != "1"
                && first.CenterSlopeDegrees >= 60f && last.CenterSlopeDegrees >= 60f)
            {
                float RenderedTerrainHeight(Vector3 vertex) =>
                    SampleRenderedNearHeight(chunk, settings, runtime, vertex,
                        nearTerrainHeights);
                var near = TopDown3DCliffFaceMeshBuilder.Build(firstRim, firstToe, firstOutward,
                    lastRim, lastToe, lastOutward, seed, true, strataShift,
                    firstRhythm, lastRhythm, RenderedTerrainHeight);
                if (FitsRenderedNearTerrain(near, RenderedTerrainHeight))
                {
                    var far = TopDown3DCliffFaceMeshBuilder.Build(firstRim, firstToe, firstOutward,
                        lastRim, lastToe, lastOutward, seed, false, strataShift,
                        firstRhythm, lastRhythm, RenderedTerrainHeight);
                    var originalFace = Environment.GetEnvironmentVariable("CANYON_ORIGINAL_FACE_MATERIAL") == "1";
                    if (!originalFace)
                    {
                        var terrainColors = new Dictionary<Vector2, Color>(64);
                        ApplyTerrainFaceColors(chunk, settings, runtime, near, terrainColors);
                        ApplyTerrainFaceColors(chunk, settings, runtime, far, terrainColors);
                        terrainFaceMaterial ??= Resources.Load<Material>("WorldCreator/CanyonTerrainFace");
                        if (terrainFaceMaterial == null)
                            throw new InvalidOperationException("Terrain-matched cliff material missing.");
                    }

                    var face = new GameObject($"Cliff Bedrock {first.Id} to {last.Id}");
                    face.transform.SetParent(chunk.DecorationRoot, false);
                    face.AddComponent<TopDown3DTraversalObstacle>();
                    face.AddComponent<MeshCollider>().sharedMesh = near;
                    chunk.RegisterDecorationMesh(near);
                    chunk.RegisterDecorationMesh(far);
                    var renderers = new Renderer[2];
                    for (var lod = 0; lod < 2; lod++)
                    {
                        var child = new GameObject($"LOD{lod}");
                        child.transform.SetParent(face.transform, false);
                        child.AddComponent<MeshFilter>().sharedMesh = lod == 0 ? near : far;
                        var renderer = child.AddComponent<MeshRenderer>();
                        renderer.sharedMaterial = originalFace ? cliffMaterial : terrainFaceMaterial;
                        renderer.receiveShadows = true;
                        if (originalFace)
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
                    rockParent = face.transform;
                }
                else
                {
                    if (Application.isPlaying) UnityEngine.Object.Destroy(near);
                    else UnityEngine.Object.DestroyImmediate(near);
                }
            }
            AddWorkbenchButtress(rockParent, first, last, firstToe, lastToe,
                firstOutward, lastOutward, seed, runtime);
            AddWallRock(chunk, settings, runtime, span, seed);
            AddToeDebris(chunk, settings, runtime, spawnExclusionCenter,
                formations, span, seed);
            return true;
        }

        private static void ApplyTerrainFaceColors(
            TopDown3DGeneratedChunk chunk, TopDown3DWorldSettings settings,
            WorldCreatorProductionRuntime runtime, Mesh mesh,
            Dictionary<Vector2, Color> cache)
        {
            var vertices = mesh.vertices;
            var colors = new Color[vertices.Length];
            for (var index = 0; index < vertices.Length; index++)
            {
                var vertex = vertices[index];
                var key = new Vector2(vertex.x, vertex.z);
                if (cache.TryGetValue(key, out colors[index])) continue;
                var absolute = new AbsoluteWorldPosition(
                    chunk.Coordinate.x * (double)settings.ChunkSize + vertex.x,
                    0d,
                    chunk.Coordinate.y * (double)settings.ChunkSize + vertex.z);
                if (!runtime.Authority.Materials.TrySample(absolute,
                        out var material, out var error))
                    throw new InvalidOperationException(error);
                colors[index] = WorldTerrainMaterialPackingAdapter.Pack(material);
                cache.Add(key, colors[index]);
            }
            mesh.SetColors(colors);
        }

        private static bool FitsRenderedNearTerrain(Mesh mesh,
            Func<Vector3, float> renderedTerrainHeight)
        {
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;
            for (var index = 0; index < triangles.Length; index += 3)
            {
                var center = (vertices[triangles[index]]
                    + vertices[triangles[index + 1]]
                    + vertices[triangles[index + 2]]) / 3f;
                if (center.y - renderedTerrainHeight(center) > 1f)
                    return false;
            }
            return true;
        }

        private static float SampleRenderedNearHeight(
            TopDown3DGeneratedChunk chunk, TopDown3DWorldSettings settings,
            WorldCreatorProductionRuntime runtime, Vector3 localVertex,
            Dictionary<(long A, long B), float> heights)
        {
            var step = settings.ChunkSize / (double)settings.QuadsPerAxis;
            var absoluteA = chunk.Coordinate.x * (double)settings.ChunkSize + localVertex.x;
            var absoluteB = chunk.Coordinate.y * (double)settings.ChunkSize + localVertex.z;
            var cellA = (long)Math.Floor(absoluteA / step);
            var cellB = (long)Math.Floor(absoluteB / step);
            var u = (float)(absoluteA / step - cellA);
            var v = (float)(absoluteB / step - cellB);
            float Height(long sampleA, long sampleB)
            {
                if (heights.TryGetValue((sampleA, sampleB), out var cached))
                    return cached;
                var point = new AbsoluteWorldPosition(sampleA * step,
                    0d, sampleB * step);
                if (!runtime.Query.TrySampleSurface(point, out var surface, out var error))
                    throw new InvalidOperationException(error ?? "Near terrain height was outside the local frame.");
                var height = (float)surface.Position.Vertical;
                heights.Add((sampleA, sampleB), height);
                return height;
            }
            var lowerLeft = Height(cellA, cellB);
            var lowerRight = Height(cellA + 1L, cellB);
            var upperLeft = Height(cellA, cellB + 1L);
            if (u + v <= 1f)
                return lowerLeft * (1f - u - v) + lowerRight * u + upperLeft * v;
            var upperRight = Height(cellA + 1L, cellB + 1L);
            return lowerRight * (1f - v) + upperLeft * (1f - u)
                + upperRight * (u + v - 1f);
        }

        private static void AddWallRock(TopDown3DGeneratedChunk chunk,
            TopDown3DWorldSettings settings, WorldCreatorProductionRuntime runtime,
            WorldCliffFaceSpan span, int seed)
        {
            var first = span.First;
            var last = span.Last;
            var drop = Mathf.Min((float)first.VerticalDrop, (float)last.VerticalDrop);
            if (drop < 8f || Hash01(first.OwnerCellA, last.OwnerCellB, 317) > 0.24d)
                return;
            var point = new AbsoluteWorldPosition(
                (first.Center.HorizontalA + last.Center.HorizontalA) * 0.5d,
                0d,
                (first.Center.HorizontalB + last.Center.HorizontalB) * 0.5d);
            if ((int)Math.Floor(point.HorizontalA / settings.ChunkSize) != chunk.Coordinate.x
                || (int)Math.Floor(point.HorizontalB / settings.ChunkSize) != chunk.Coordinate.y)
                return;
            if (!runtime.Query.TrySampleSurface(point, out var surface, out var error)
                || !runtime.Query.TrySampleAffordance(point,
                    WorldAgentProfile.BooterProof, out var booter, out error)
                || !runtime.Query.TrySampleAffordance(point,
                    WorldAgentProfile.BigArmProof, out var bigArm, out error))
                throw new InvalidOperationException(error ?? "Canyon wall rock could not sample the world.");
            if ((surface.Semantic & WorldSurfaceSemantic.CanyonWall) == 0
                || (surface.Semantic & (WorldSurfaceSemantic.SiteReservation
                    | WorldSurfaceSemantic.Approach)) != 0
                || surface.NormalVertical > 0.65f
                || booter.Walkable || bigArm.Walkable
                || booter.ReservedRoute || bigArm.ReservedRoute)
                return;
            var variant = Mathf.Min(4, (int)(Hash01(first.OwnerCellA,
                last.OwnerCellB, 319) * 5d));
            if (!TryGetCliffStone(variant, 0, out var source)) return;
            var bounds = source.bounds;
            var width = Mathf.Lerp(3.6f, 6.4f,
                (float)Hash01(first.OwnerCellA, last.OwnerCellB, 321));
            var height = Mathf.Min(drop * 0.34f, width * 1.35f);
            var size = new Vector3(width / bounds.size.x,
                height / bounds.size.y, width * 0.62f / bounds.size.z);
            var outward = new Vector3((float)(first.OutwardA + last.OutwardA), 0f,
                (float)(first.OutwardB + last.OutwardB)).normalized;
            var yaw = Mathf.Atan2(outward.x, outward.z) * Mathf.Rad2Deg;
            var rotation = Quaternion.Euler(0f, yaw, 0f);
            if (!TryToChunkLocal(chunk, settings, runtime, surface.Position,
                    out var local)) return;
            var anchor = local + outward * 0.6f + Vector3.up * (height * 0.12f);

            var root = new GameObject($"Workbench Wall Rock {first.Id}");
            root.transform.SetParent(chunk.DecorationRoot, false);
            root.transform.localRotation = rotation;
            root.transform.localScale = size;
            root.transform.localPosition = anchor
                - rotation * Vector3.Scale(bounds.center, size);
            root.AddComponent<TopDown3DTraversalObstacle>();
            var collider = root.AddComponent<BoxCollider>();
            collider.center = bounds.center;
            collider.size = bounds.size * 0.82f;
            AddStoneLods(root.transform, variant, first, last, seed, bounds.size);
        }

        private static void AddWorkbenchButtress(Transform parent,
            WorldCliffSectionCandidate first, WorldCliffSectionCandidate last,
            Vector3 firstToe, Vector3 lastToe,
            Vector3 firstOutward, Vector3 lastOutward, int seed,
            WorldCreatorProductionRuntime runtime)
        {
            if (Environment.GetEnvironmentVariable("CANYON_DISABLE_BUTTRESS") == "1") return;
            var drop = Mathf.Min((float)first.VerticalDrop, (float)last.VerticalDrop);
            if (drop < 4f || Hash01(first.OwnerCellA, first.OwnerCellB, 211) > 0.10d)
                return;
            var variant = Mathf.Min(4, (int)(Hash01(first.OwnerCellA,
                last.OwnerCellB, 223) * 5d));
            if (!TryGetCliffStone(variant, 0, out var source)) return;
            var outward = (firstOutward + lastOutward).normalized;
            var position = Vector3.Lerp(firstToe, lastToe, 0.5f)
                + outward * 0.25f + Vector3.down * 0.23f;
            var rotation = Quaternion.Euler(0f,
                Mathf.Atan2(outward.x, outward.z) * Mathf.Rad2Deg
                    + (float)(Hash01(first.OwnerCellA, last.OwnerCellB, 227) * 40d - 20d),
                0f);
            var scale = Mathf.Lerp(3.5f, 5.5f,
                (float)Hash01(first.OwnerCellA, last.OwnerCellB, 229));
            var size = new Vector3(scale, scale * 2.0f, scale);
            var bounds = source.bounds;
            position += Vector3.up * (-bounds.min.y * size.y - 0.30f);
            if (!IsButtressFootprintClear(parent, position, rotation, size,
                    bounds, runtime)) return;

            var root = new GameObject($"Workbench Cliff Buttress {first.Id}");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;
            root.transform.localRotation = rotation;
            root.transform.localScale = size;
            root.AddComponent<TopDown3DTraversalObstacle>();
            var collider = root.AddComponent<BoxCollider>();
            collider.center = bounds.center;
            collider.size = bounds.size * 0.82f;
            AddStoneLods(root.transform, variant, first, last, seed, bounds.size);
        }

        private static bool IsButtressFootprintClear(Transform parent,
            Vector3 position, Quaternion rotation, Vector3 scale, Bounds sourceBounds,
            WorldCreatorProductionRuntime runtime)
        {
            var localToWorld = parent.localToWorldMatrix
                * Matrix4x4.TRS(position, rotation, scale);
            var halfX = sourceBounds.size.x * 0.41f + 0.8f / scale.x;
            var halfZ = sourceBounds.size.z * 0.41f + 0.8f / scale.z;
            for (var z = -2; z <= 2; z++)
            for (var x = -2; x <= 2; x++)
            {
                var local = sourceBounds.center + new Vector3(
                    halfX * x * 0.5f, 0f, halfZ * z * 0.5f);
                var world = localToWorld.MultiplyPoint3x4(local);
                var absolute = runtime.ToAbsolute(world.x, 0f, world.z);
                if (!runtime.Query.TrySampleSurface(absolute, out var surface, out var error))
                    throw new InvalidOperationException(error);
                if ((surface.Semantic & WorldSurfaceSemantic.CanyonFloor) == 0) continue;
                var halfHeight = sourceBounds.size.y * 0.41f;
                var bottom = localToWorld.MultiplyPoint3x4(
                    local - Vector3.up * halfHeight).y;
                var top = localToWorld.MultiplyPoint3x4(
                    local + Vector3.up * halfHeight).y;
                if (surface.Position.Vertical < bottom - 1.6d
                    || surface.Position.Vertical > top + 0.3d) continue;
                if (!runtime.Query.TrySampleAffordance(absolute,
                        WorldAgentProfile.BooterProof, out var booter, out error)
                    || !runtime.Query.TrySampleAffordance(absolute,
                        WorldAgentProfile.BigArmProof, out var bigArm, out error))
                    throw new InvalidOperationException(error);
                if (booter.Walkable || bigArm.Walkable
                    || booter.ReservedRoute || bigArm.ReservedRoute) return false;
            }
            return true;
        }

        private static void AddToeDebris(
            TopDown3DGeneratedChunk chunk,
            TopDown3DWorldSettings settings,
            WorldCreatorProductionRuntime runtime,
            Vector2 spawnExclusionCenter,
            IReadOnlyList<TopDown3DRockFormationPlan> formations,
            WorldCliffFaceSpan span, int seed)
        {
            var candidates = WorldCliffDebrisPlanner.Plan(runtime.Identity,
                runtime.CoordinateModel, span);
            foreach (var candidate in candidates)
            {
                var point = candidate.Position;
                if (!TryGetCliffStone(candidate.SourceRockVariant, 0, out var source)
                    || !IsDebrisFootprintClear(candidate, settings, runtime,
                        spawnExclusionCenter, formations, out var local)) continue;

                var root = new GameObject($"Cliff Toe Debris {candidate.Id}");
                root.transform.SetParent(chunk.DecorationRoot, false);
                var bounds = source.bounds;
                var yaw = (float)(Hash01(span.First.OwnerCellA,
                    span.Last.OwnerCellB, 257 + candidate.Ordinal) * 360d);
                var tumble = Mathf.Lerp(68f, 105f,
                    (float)Hash01(span.First.OwnerCellA,
                        span.Last.OwnerCellB, 263 + candidate.Ordinal));
                var roll = Mathf.Lerp(-22f, 22f,
                    (float)Hash01(span.First.OwnerCellA,
                        span.Last.OwnerCellB, 269 + candidate.Ordinal));
                root.transform.localRotation = Quaternion.Euler(tumble, yaw, roll);
                var scale = candidate.TargetWidth / Mathf.Max(
                    bounds.size.x, bounds.size.y, bounds.size.z);
                root.transform.localScale = new Vector3(scale, scale * 0.85f, scale);
                var ground = chunk.transform.InverseTransformPoint(
                    new Vector3(local.X, local.Y, local.Z));
                var rotatedCenter = root.transform.localRotation * Vector3.Scale(
                    bounds.center, root.transform.localScale);
                var half = Vector3.Scale(bounds.size * 0.41f,
                    root.transform.localScale);
                var rotation = root.transform.localRotation;
                var supportY = Mathf.Abs((rotation * Vector3.right).y) * half.x
                    + Mathf.Abs((rotation * Vector3.up).y) * half.y
                    + Mathf.Abs((rotation * Vector3.forward).y) * half.z;
                var burial = Mathf.Min(0.24f, candidate.TargetWidth * 0.08f);
                root.transform.localPosition = ground
                    + new Vector3(-rotatedCenter.x,
                        supportY - rotatedCenter.y - burial,
                        -rotatedCenter.z);
                root.AddComponent<TopDown3DTraversalObstacle>();
                var collider = root.AddComponent<BoxCollider>();
                collider.center = bounds.center;
                collider.size = bounds.size * 0.82f;
                AddStoneLods(root.transform, candidate.SourceRockVariant,
                    span.First, span.Last, seed, bounds.size);
            }
        }

        private static bool IsDebrisFootprintClear(
            WorldCliffDebrisCandidate candidate,
            TopDown3DWorldSettings settings,
            WorldCreatorProductionRuntime runtime,
            Vector2 spawnExclusionCenter,
            IReadOnlyList<TopDown3DRockFormationPlan> formations,
            out LocalWorldPosition local)
        {
            local = default;
            var radius = candidate.TargetWidth * 0.5f + 0.35f;
            var offsets = new[]
            {
                Vector2.zero, Vector2.right * radius, Vector2.left * radius,
                Vector2.up * radius, Vector2.down * radius
            };
            for (var i = 0; i < offsets.Length; i++)
            {
                var point = new AbsoluteWorldPosition(
                    candidate.Position.HorizontalA + offsets[i].x,
                    0d,
                    candidate.Position.HorizontalB + offsets[i].y);
                if (!runtime.Query.TrySampleSurface(point, out var surface, out _)
                    || (surface.Semantic & (WorldSurfaceSemantic.SiteReservation
                        | WorldSurfaceSemantic.Approach)) != 0
                    || !runtime.Query.TrySampleAffordance(point, WorldAgentProfile.BooterProof,
                        out var booter, out _)
                    || !runtime.Query.TrySampleAffordance(point, WorldAgentProfile.BigArmProof,
                        out var bigArm, out _)
                    || booter.ReservedRoute || bigArm.ReservedRoute
                    || booter.SlopeDegrees > 27f || bigArm.SlopeDegrees > 27f
                    || !runtime.TryToLocal(surface.Position, out var sampleLocal)) return false;
                var center = new Vector2(sampleLocal.X, sampleLocal.Z);
                if (Vector2.Distance(center, spawnExclusionCenter)
                    < settings.ClearSpawnRadius + radius) return false;
                if (formations != null)
                    for (var formation = 0; formation < formations.Count; formation++)
                        if (Vector2.Distance(center, formations[formation].EnvelopeCenter)
                            < formations[formation].EnvelopeRadius + radius) return false;
                if (i == 0) local = sampleLocal;
            }

            return true;
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
            TopDown3DWorldSettings settings, WorldCreatorProductionRuntime runtime,
            AbsoluteWorldPosition position,
            out Vector3 local)
        {
            local = default;
            if (!runtime.TryToLocal(position, out _)) return false;
            local = new Vector3(
                (float)(position.HorizontalA - chunk.Coordinate.x * (double)settings.ChunkSize),
                (float)position.Vertical,
                (float)(position.HorizontalB - chunk.Coordinate.y * (double)settings.ChunkSize));
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
