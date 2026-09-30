using System;
using System.Collections.Generic;
using BooterBigArm.TopDown3D;
using BooterBigArm.TopDown3D.WorldCreator;
using UnityEngine;
using UnityEngine.Rendering;

namespace BooterBigArm.Editor
{
    /// <summary>
    /// Generates the terrain-owned sand field. Formation contacts are optional local
    /// contributors; the landscape field itself never asks a rock planner for input.
    /// </summary>
    internal static class TopDown3DLandscapeSandPreview
    {
        private static readonly TopDown3DRockFormationPlan[] NoFormationSources =
            Array.Empty<TopDown3DRockFormationPlan>();

        internal static void Apply(
            TopDown3DLandscapeAuthoringSandbox sandbox,
            MeshCollider[] terrain,
            IReadOnlyList<TopDown3DDustDepositionPlanner.AuthoredObstruction> formationContacts = null)
        {
            var landscapeAuthority = TopDown3DLandscapeAuthoringSandboxEditor
                .ResolveLandscapeAuthority(sandbox);
            var landscapeAmount = landscapeAuthority.LandscapeSand;
            var landscapeReliefScale = landscapeAuthority.LandscapeSandRelief
                / Mathf.Max(0.0001f, sandbox.WorldSettings.DustMaximumBaseHeight);
            var buildup = sandbox.SandBuildup;
            var hasFormationBerms = formationContacts != null && formationContacts.Count > 0 && buildup > 0f;
            var stamps = sandbox.GetComponentsInChildren<TopDown3DLandscapeGroundStamp>(true);
            if (landscapeAmount <= 0f && !hasFormationBerms && stamps.Length == 0) return;

            var tiles = new List<BaseTile>(terrain.Length);
            foreach (var collider in terrain) tiles.Add(new BaseTile(collider));
            var influence = hasFormationBerms
                ? BuildFormationInfluence(sandbox, formationContacts)
                : default;
            var generator = new TopDown3DWorldGenerator(sandbox.WorldSettings);
            var cache = new Dictionary<Vector2, TopDown3DDustDepositionSample>();

            TopDown3DDustDepositionSample DepositAt(Vector2 position)
            {
                if (cache.TryGetValue(position, out var cached)) return cached;
                if (!generator.Authority.Materials.TrySample(
                    new AbsoluteWorldPosition(position.x, 0d, position.y), out var material, out var error))
                    throw new InvalidOperationException(error);

                var terrainDeposit = landscapeAmount > 0f
                    ? SampleTerrainAt(sandbox.WorldSettings, generator, position)
                    : default;
                var formationDeposit = hasFormationBerms
                    && influence.Contains(new Vector3(position.x, 0f, position.y))
                    ? TopDown3DDustDepositionPlanner.SampleAuthoredDeposit(
                        sandbox.WorldSettings, material, position, formationContacts, buildup)
                    : default;
                var deposit = new TopDown3DDustDepositionSample(
                    Mathf.Max(formationDeposit.Weight, terrainDeposit.Weight * landscapeAmount),
                    Mathf.Max(
                        formationDeposit.Height,
                        terrainDeposit.Height * landscapeAmount * landscapeReliefScale),
                    formationDeposit.ShelterWeight,
                    checked((float)material.Position.Vertical),
                    material.WindExposure,
                    material.Erosion,
                    material.Deposit);
                cache.Add(position, deposit);
                return deposit;
            }

            foreach (var tile in tiles)
            {
                var hasStamp = false;
                foreach (var stamp in stamps)
                {
                    if (stamp == null || !stamp.isActiveAndEnabled) continue;
                    var diameter = stamp.Radius * 2f;
                    if (!tile.Intersects(new Bounds(stamp.transform.position,
                            new Vector3(diameter, 2f, diameter)))) continue;
                    hasStamp = true;
                    break;
                }
                if (landscapeAmount <= 0f && !hasStamp
                    && (!hasFormationBerms || !tile.Intersects(influence))) continue;
                // Broad landscape sand intentionally uses the accepted 20 cm preview density.
                // A formation-only review keeps the tighter 10 cm contact resolution.
                var previewSpacing = landscapeAmount > 0f ? 0.2f : 0.1f;
                ApplyToTile(sandbox, tile, previewSpacing, DepositAt, stamps);
            }
            Physics.SyncTransforms();
        }

        internal static TopDown3DDustDepositionSample SampleTerrainAt(
            TopDown3DWorldSettings settings,
            TopDown3DWorldGenerator generator,
            Vector2 position)
        {
            return TopDown3DDustDepositionPlanner.SampleAt(
                settings, generator, position, NoFormationSources);
        }

        private static Bounds BuildFormationInfluence(
            TopDown3DLandscapeAuthoringSandbox sandbox,
            IReadOnlyList<TopDown3DDustDepositionPlanner.AuthoredObstruction> contacts)
        {
            var influence = new Bounds();
            for (var i = 0; i < contacts.Count; i++)
            {
                var source = contacts[i];
                var radius = Mathf.Min(source.HalfSize.x, source.HalfSize.y);
                var extent = source.HalfSize * (1f + 3.6f / Mathf.Max(0.01f, radius))
                    + Vector2.one * (sandbox.WorldSettings.DustWakeLength + 0.3f);
                var sourceInfluence = new Bounds(
                    new Vector3(source.Center.x, 0f, source.Center.y),
                    new Vector3(extent.x * 2f, 2f, extent.y * 2f));
                if (i == 0) influence = sourceInfluence;
                else influence.Encapsulate(sourceInfluence);
            }
            return influence;
        }

        private static void ApplyToTile(
            TopDown3DLandscapeAuthoringSandbox sandbox,
            BaseTile tile,
            float previewSpacing,
            Func<Vector2, TopDown3DDustDepositionSample> depositAt,
            TopDown3DLandscapeGroundStamp[] stamps)
        {
            var subdivisions = Mathf.Max(1, Mathf.CeilToInt(tile.Step / previewSpacing));
            var quads = (tile.Resolution - 1) * subdivisions;
            var size = quads + 1;
            var step = tile.Step / subdivisions;
            var vertices = new Vector3[size * size];
            var normals = new Vector3[vertices.Length];
            var colors = new Color[vertices.Length];
            var uvs = new Vector2[vertices.Length];
            var bankMasks = new Vector2[vertices.Length];
            var triangles = new int[quads * quads * 6];
            var visible = false;
            float StampHeightAt(Vector2 point)
            {
                var height = 0f;
                foreach (var stamp in stamps)
                    if (stamp != null && stamp.isActiveAndEnabled)
                        height = Mathf.Max(height, stamp.HeightAt(point));
                return height;
            }
            for (var z = 0; z < size; z++)
            {
                for (var x = 0; x < size; x++)
                {
                    var index = z * size + x;
                    // Global integer grid gives adjacent tiles identical boundary samples.
                    var point = new Vector2(
                        Mathf.Round(tile.Origin.x / step + x) * step,
                        Mathf.Round(tile.Origin.z / step + z) * step);
                    var basis = tile.Sample(point);
                    var deposit = depositAt(point);
                    var stampHeight = StampHeightAt(point);
                    visible |= deposit.Height > 0.00001f || stampHeight > 0.00001f;
                    vertices[index] = new Vector3(
                        point.x - tile.Origin.x,
                        basis.Height + deposit.Height + stampHeight,
                        point.y - tile.Origin.z);
                    var normal = basis.Normal;
                    if (deposit.Height > 0f || stampHeight > 0f)
                    {
                        var dx = (depositAt(point + Vector2.right * step).Height
                            + StampHeightAt(point + Vector2.right * step)
                            - depositAt(point - Vector2.right * step).Height
                            - StampHeightAt(point - Vector2.right * step)) / (2f * step);
                        var dz = (depositAt(point + Vector2.up * step).Height
                            + StampHeightAt(point + Vector2.up * step)
                            - depositAt(point - Vector2.up * step).Height
                            - StampHeightAt(point - Vector2.up * step)) / (2f * step);
                        normal = new Vector3(
                            normal.x - dx * normal.y,
                            normal.y,
                            normal.z - dz * normal.y).normalized;
                    }
                    normals[index] = normal;
                    var coverage = deposit.Weight;
                    var color = new Color(
                        Mathf.Lerp(basis.Color.r, 1f, coverage),
                        basis.Color.g * (1f - coverage),
                        basis.Color.b * (1f - coverage),
                        basis.Color.a);
                    foreach (var stamp in stamps)
                    {
                        if (stamp == null || !stamp.isActiveAndEnabled
                            || stamp.Surface == TopDown3DGroundStampSurface.KeepExisting) continue;
                        var weight = stamp.SurfaceWeight(point);
                        if (weight <= 0f) continue;
                        visible = true;
                        var target = SurfaceColor(stamp.Surface, color.a);
                        color = Color.Lerp(color, target, weight);
                    }
                    colors[index] = color;
                    uvs[index] = point / sandbox.WorldSettings.ChunkSize;
                    bankMasks[index] = new Vector2(
                        0f,
                        Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(deposit.Height / 0.25f)));
                }
            }
            if (!visible) return;

            var triangleIndex = 0;
            for (var z = 0; z < quads; z++)
            {
                for (var x = 0; x < quads; x++)
                {
                    var bottom = z * size + x;
                    var top = bottom + size;
                    triangles[triangleIndex++] = bottom;
                    triangles[triangleIndex++] = top;
                    triangles[triangleIndex++] = bottom + 1;
                    triangles[triangleIndex++] = bottom + 1;
                    triangles[triangleIndex++] = top;
                    triangles[triangleIndex++] = top + 1;
                }
            }

            var mesh = tile.Collider.sharedMesh;
            tile.Collider.sharedMesh = null;
            mesh.Clear();
            mesh.indexFormat = vertices.Length > ushort.MaxValue
                ? IndexFormat.UInt32
                : IndexFormat.UInt16;
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetColors(colors);
            mesh.SetUVs(0, uvs);
            mesh.SetUVs(1, bankMasks);
            mesh.SetTriangles(triangles, 0, true);
            mesh.RecalculateBounds();
            tile.Collider.sharedMesh = mesh;
        }

        private static Color SurfaceColor(TopDown3DGroundStampSurface surface, float weathering)
        {
            switch (surface)
            {
                case TopDown3DGroundStampSurface.RedDirt: return new Color(0f, 0f, 0f, weathering);
                case TopDown3DGroundStampSurface.TanSand: return new Color(1f, 0f, 0f, weathering);
                case TopDown3DGroundStampSurface.ShaleGravel: return new Color(0f, 1f, 0f, weathering);
                case TopDown3DGroundStampSurface.RockyShale: return new Color(0f, 0.3f, 1f, weathering);
                default: throw new ArgumentOutOfRangeException(nameof(surface));
            }
        }

        private readonly struct Surface
        {
            internal Surface(float height, Vector3 normal, Color color)
            {
                Height = height;
                Normal = normal;
                Color = color;
            }

            internal float Height { get; }
            internal Vector3 Normal { get; }
            internal Color Color { get; }
        }

        private sealed class BaseTile
        {
            private readonly Vector3[] vertices;
            private readonly Vector3[] normals;
            private readonly Color[] colors;

            internal BaseTile(MeshCollider collider)
            {
                Collider = collider;
                Origin = collider.transform.position;
                var mesh = collider.sharedMesh;
                vertices = mesh.vertices;
                normals = mesh.normals;
                colors = mesh.colors;
                Resolution = Mathf.RoundToInt(Mathf.Sqrt(vertices.Length));
                if (Resolution < 2 || Resolution * Resolution != vertices.Length)
                    throw new InvalidOperationException("Sand authoring requires the production terrain grid.");
                Step = vertices[1].x - vertices[0].x;
                if (Step <= 0f || normals.Length != vertices.Length || colors.Length != vertices.Length)
                    throw new InvalidOperationException("Terrain grid is missing surface data.");
            }

            internal MeshCollider Collider { get; }
            internal Vector3 Origin { get; }
            internal int Resolution { get; }
            internal float Step { get; }
            private float Span => Step * (Resolution - 1);

            internal bool Intersects(Bounds bounds) =>
                Origin.x <= bounds.max.x && Origin.x + Span >= bounds.min.x
                && Origin.z <= bounds.max.z && Origin.z + Span >= bounds.min.z;

            internal Surface Sample(Vector2 point)
            {
                var gx = Mathf.Clamp((point.x - Origin.x) / Step, 0f, Resolution - 1);
                var gz = Mathf.Clamp((point.y - Origin.z) / Step, 0f, Resolution - 1);
                var x = Mathf.Min(Mathf.FloorToInt(gx), Resolution - 2);
                var z = Mathf.Min(Mathf.FloorToInt(gz), Resolution - 2);
                var u = gx - x;
                var v = gz - z;
                var bottom = z * Resolution + x;
                int a;
                int b;
                int c;
                float wa;
                float wb;
                float wc;
                if (u + v <= 1f)
                {
                    a = bottom;
                    b = bottom + 1;
                    c = bottom + Resolution;
                    wa = 1f - u - v;
                    wb = u;
                    wc = v;
                }
                else
                {
                    a = bottom + Resolution + 1;
                    b = bottom + Resolution;
                    c = bottom + 1;
                    wa = u + v - 1f;
                    wb = 1f - u;
                    wc = 1f - v;
                }
                return new Surface(
                    vertices[a].y * wa + vertices[b].y * wb + vertices[c].y * wc,
                    (normals[a] * wa + normals[b] * wb + normals[c] * wc).normalized,
                    colors[a] * wa + colors[b] * wb + colors[c] * wc);
            }
        }
    }
}
