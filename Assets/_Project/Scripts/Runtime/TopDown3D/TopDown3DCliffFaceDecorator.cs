using System;
using System.Collections.Generic;
using BooterBigArm.TopDown3D.WorldCreator;
using UnityEngine;
using UnityEngine.Rendering;

namespace BooterBigArm.TopDown3D
{
    /// <summary>
    /// Visual rock faces on steep ground. The canonical terrain remains the physical surface.
    /// Every section is owned by an absolute grid cell, so streaming order and local rebases
    /// cannot choose a different wall.
    /// </summary>
    internal static class TopDown3DCliffFaceDecorator
    {
        private const double CellSpan = 3d;
        private const int Columns = 4;
        private const int Rows = 3;
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
            var vertices = new List<Vector3>(1024);
            var triangles = new List<int>(1536);

            for (var b = minimumB; b <= maximumB; b++)
            {
                for (var a = minimumA; a <= maximumA; a++)
                {
                    if (study.TryBuild(a, b, out var section, out var error))
                    {
                        if (ClearOfAuthoredGround(section, runtime, settings,
                            spawnExclusionCenter, formations))
                            AppendSection(chunk, runtime, section, vertices, triangles);
                    }
                    else if (error != null)
                    {
                        throw new InvalidOperationException(
                            $"Cliff section {a},{b} could not sample the world: {error}");
                    }
                }
                // One absolute-grid row per frame keeps this inside decoration streaming.
                yield return 0;
            }

            if (triangles.Count == 0) yield break;
            var mesh = new Mesh { name = $"Cliff Faces {chunk.Coordinate.x},{chunk.Coordinate.y}" };
            if (vertices.Count > ushort.MaxValue) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0, true);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            chunk.RegisterDecorationMesh(mesh);

            var faceObject = new GameObject("Procedural Cliff Faces");
            faceObject.transform.SetParent(chunk.DecorationRoot, false);
            faceObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = faceObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = cliffMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            var block = new MaterialPropertyBlock();
            block.SetColor(BaseColorId, new Color(1.3f, 1.31f, 1.33f, 1f));
            block.SetColor(CrackColorId, new Color(0.12f, 0.12f, 0.125f, 1f));
            block.SetColor(MineralColorId, new Color(0.66f, 0.67f, 0.69f, 1f));
            renderer.SetPropertyBlock(block);
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
                < settings.ClearSpawnRadius + 2f) return false;
            if (formations == null) return true;
            for (var i = 0; i < formations.Count; i++)
            {
                var formation = formations[i];
                if (Vector2.Distance(center, formation.EnvelopeCenter)
                    < formation.EnvelopeRadius + 2f) return false;
            }
            return true;
        }

        private static void AppendSection(
            TopDown3DGeneratedChunk chunk,
            WorldCreatorProductionRuntime runtime,
            WorldCliffSectionCandidate section,
            List<Vector3> vertices,
            List<int> triangles)
        {
            var grid = new Vector3[(Columns + 1) * (Rows + 1)];
            var tangentA = -section.OutwardB;
            var tangentB = section.OutwardA;
            var lengthA = section.Toe.HorizontalA - section.Rim.HorizontalA;
            var lengthB = section.Toe.HorizontalB - section.Rim.HorizontalB;
            var seed = runtime.Identity.Seed;
            var halfWidth = 1.25d + 0.3d * Hash01(section.OwnerCellA ^ seed, section.OwnerCellB, 11);
            var phase = Hash01(section.OwnerCellA ^ seed, section.OwnerCellB, 17);
            for (var row = 0; row <= Rows; row++)
            {
                var down = (double)row / Rows;
                for (var column = 0; column <= Columns; column++)
                {
                    var across = 2d * column / Columns - 1d;
                    var stagger = row == 0 || row == Rows ? 0d
                        : (Hash01((section.OwnerCellA + column) ^ seed, section.OwnerCellB + row, 23) - 0.5d) * 0.16d;
                    var a = section.Rim.HorizontalA + lengthA * down
                        + tangentA * (across * halfWidth + stagger);
                    var b = section.Rim.HorizontalB + lengthB * down
                        + tangentB * (across * halfWidth + stagger);
                    var point = new AbsoluteWorldPosition(a, 0d, b);
                    if (!runtime.Query.TrySampleSurface(point, out var surface, out var error))
                        throw new InvalidOperationException(error);
                    var ledge = row == 1 ? 0.13d + 0.1d * phase
                        : row == 2 ? 0.09d + 0.08d * (1d - phase) : 0.035d;
                    // A shallow relief follows sampled terrain. No unsupported overhang or
                    // climbable contact is presented by this visual-only first pass.
                    var raised = new AbsoluteWorldPosition(
                        surface.Position.HorizontalA + section.OutwardA * ledge,
                        surface.Position.Vertical + 0.04d + ledge * 0.18d,
                        surface.Position.HorizontalB + section.OutwardB * ledge);
                    if (!runtime.TryToLocal(raised, out var local))
                        throw new InvalidOperationException("Cliff vertex fell outside the current local frame.");
                    grid[row * (Columns + 1) + column] = chunk.transform.InverseTransformPoint(
                        new Vector3(local.X, local.Y, local.Z));
                }
            }

            for (var row = 0; row < Rows; row++)
            for (var column = 0; column < Columns; column++)
            {
                var first = row * (Columns + 1) + column;
                var next = first + Columns + 1;
                // Independent triangle vertices give the stone its hard fracture planes.
                // The material is double sided for the opposite approach.
                if (((column + row + (int)(phase * 17d)) & 1) == 0)
                {
                    AppendTriangle(grid[first], grid[next], grid[first + 1], vertices, triangles);
                    AppendTriangle(grid[first + 1], grid[next], grid[next + 1], vertices, triangles);
                }
                else
                {
                    AppendTriangle(grid[first], grid[next], grid[next + 1], vertices, triangles);
                    AppendTriangle(grid[first], grid[next + 1], grid[first + 1], vertices, triangles);
                }
            }
        }

        private static void AppendTriangle(Vector3 a, Vector3 b, Vector3 c,
            List<Vector3> vertices, List<int> triangles)
        {
            var first = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            triangles.Add(first);
            triangles.Add(first + 1);
            triangles.Add(first + 2);
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
