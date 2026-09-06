using System;
using System.Collections.Generic;
using BooterBigArm.TopDown3D;
using BooterBigArm.TopDown3D.WorldCreator;
using UnityEngine;
using UnityEngine.Rendering;

namespace BooterBigArm.Editor
{
    /// <summary>Deposits sand into disposable authoring terrain, never a second overlay/collider.</summary>
    internal static class TopDown3DMixedFormationSand
    {
        internal static void Apply(TopDown3DLandscapeAuthoringSandbox sandbox,
            MeshCollider[] terrain, TopDown3DRockWorkbenchAuthoring[] rocks)
        {
            if (sandbox.SandBuildup <= 0f) return;
            var tiles = new List<BaseTile>(terrain.Length);
            foreach (var collider in terrain) tiles.Add(new BaseTile(collider));
            var sources = new List<TopDown3DDustDepositionPlanner.AuthoredObstruction>();
            var influence = new Bounds();
            foreach (var rock in rocks)
            {
                var bounds = rock.GetComponent<MeshRenderer>().bounds;
                var center = new Vector2(bounds.center.x, bounds.center.z);
                var floor = SampleBase(tiles, center).Height;
                // A suspended upper member is supported by the pile, not a separate ground collar.
                if (bounds.min.y > floor + 0.15f || bounds.max.y <= floor + 0.01f) continue;
                // The widest part of the rock can sit well above its buried base. Anchor the
                // bank to a mesh cross-section near ground contact, not the whole renderer box.
                var contact = ContactFootprint(rock, bounds, floor, sandbox.SandBuildup, out var contactEdges);
                center = new Vector2(contact.center.x, contact.center.z);
                var source = new TopDown3DDustDepositionPlanner.AuthoredObstruction(
                    center, new Vector2(contact.extents.x, contact.extents.z), bounds.max.y - floor, contactEdges);
                sources.Add(source);
                var radius = Mathf.Min(source.HalfSize.x, source.HalfSize.y);
                var extent = source.HalfSize * (1f + 1.25f / radius)
                    + Vector2.one * (sandbox.WorldSettings.DustWakeLength + 0.3f);
                var sourceInfluence = new Bounds(new Vector3(center.x, 0f, center.y),
                    new Vector3(extent.x * 2f, 2f, extent.y * 2f));
                if (sources.Count == 1) influence = sourceInfluence;
                else influence.Encapsulate(sourceInfluence);
            }
            if (sources.Count == 0) return;

            var generator = new TopDown3DWorldGenerator(sandbox.WorldSettings);
            var cache = new Dictionary<Vector2, TopDown3DDustDepositionSample>();
            TopDown3DDustDepositionSample DepositAt(Vector2 position)
            {
                if (!influence.Contains(new Vector3(position.x, 0f, position.y))) return default;
                if (cache.TryGetValue(position, out var cached)) return cached;
                if (!generator.Authority.Materials.TrySample(
                    new AbsoluteWorldPosition(position.x, 0d, position.y), out var material, out var error))
                    throw new InvalidOperationException(error);
                var deposit = TopDown3DDustDepositionPlanner.SampleAuthoredDeposit(
                    sandbox.WorldSettings, material, position, sources, sandbox.SandBuildup);
                cache.Add(position, deposit);
                return deposit;
            }

            foreach (var tile in tiles)
            {
                if (!tile.Intersects(influence)) continue;
                // Integer subdivisions preserve the exact original triangle planes. Sand needs
                // finer geometry than the large-scale ground; only nearby tiles are refined.
                var subdivisions = Mathf.Max(1, Mathf.CeilToInt(tile.Step / 0.1f));
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
                for (var z = 0; z < size; z++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        var index = z * size + x;
                        // Global integer grid gives adjacent tiles identical boundary/halo samples.
                        var point = new Vector2(
                            Mathf.Round(tile.Origin.x / step + x) * step,
                            Mathf.Round(tile.Origin.z / step + z) * step);
                        var basis = tile.Sample(point);
                        var deposit = DepositAt(point);
                        visible |= deposit.Height > 0.00001f;
                        vertices[index] = new Vector3(point.x - tile.Origin.x,
                            basis.Height + deposit.Height, point.y - tile.Origin.z);
                        var normal = basis.Normal;
                        if (deposit.Height > 0f)
                        {
                            var dx = (DepositAt(point + Vector2.right * step).Height
                                - DepositAt(point - Vector2.right * step).Height) / (2f * step);
                            var dz = (DepositAt(point + Vector2.up * step).Height
                                - DepositAt(point - Vector2.up * step).Height) / (2f * step);
                            normal = new Vector3(normal.x - dx * normal.y,
                                normal.y, normal.z - dz * normal.y).normalized;
                        }
                        normals[index] = normal;
                        // Existing terrain packing: deposit grows while exposed gravel/strata recede.
                        var coverage = deposit.Weight;
                        colors[index] = new Color(Mathf.Lerp(basis.Color.r, 1f, coverage),
                            basis.Color.g * (1f - coverage), basis.Color.b * (1f - coverage), basis.Color.a);
                        uvs[index] = point / sandbox.WorldSettings.ChunkSize;
                        // Dedicated deposit-depth mask, independent of the terrain's broad sand biome weight.
                        bankMasks[index] = new Vector2(0f, Mathf.SmoothStep(0f, 1f,
                            Mathf.Clamp01(deposit.Height / 0.25f)));
                    }
                }
                if (!visible) continue;
                var t = 0;
                for (var z = 0; z < quads; z++)
                {
                    for (var x = 0; x < quads; x++)
                    {
                        var bottom = z * size + x;
                        var top = bottom + size;
                        triangles[t++] = bottom;
                        triangles[t++] = top;
                        triangles[t++] = bottom + 1;
                        triangles[t++] = bottom + 1;
                        triangles[t++] = top;
                        triangles[t++] = top + 1;
                    }
                }
                var mesh = tile.Collider.sharedMesh;
                tile.Collider.sharedMesh = null;
                mesh.Clear();
                mesh.indexFormat = vertices.Length > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;
                mesh.SetVertices(vertices);
                mesh.SetNormals(normals);
                mesh.SetColors(colors);
                mesh.SetUVs(0, uvs);
                mesh.SetUVs(1, bankMasks);
                mesh.SetTriangles(triangles, 0, true);
                mesh.RecalculateBounds();
                // Renderer and ground queries see the very same displaced triangles.
                tile.Collider.sharedMesh = mesh;
            }
            Physics.SyncTransforms();
        }

        private static Bounds ContactFootprint(TopDown3DRockWorkbenchAuthoring rock,
            Bounds bounds, float floor, float buildup, out Vector2[] contactEdges)
        {
            var mesh = rock.GetComponent<MeshFilter>().sharedMesh;
            var vertices = mesh.vertices;
            var matrix = rock.transform.localToWorldMatrix;
            for (var i = 0; i < vertices.Length; i++) vertices[i] = matrix.MultiplyPoint3x4(vertices[i]);
            // Sample near the expected berm crest, so it can cover the lower face rather
            // than banking against an oval detached from the visible rock surface.
            var level = Mathf.Clamp(floor + Mathf.Min(buildup, (bounds.max.y - floor) * 0.7f) * 0.65f,
                bounds.min.y + 0.001f, bounds.max.y - 0.001f);
            var found = false;
            var footprint = new Bounds();
            var edges = new List<Vector2>();
            var crossings = new List<Vector2>(3);
            void IncludeCrossing(Vector3 a, Vector3 b)
            {
                if ((a.y < level && b.y < level) || (a.y > level && b.y > level)) return;
                var dy = b.y - a.y;
                if (Mathf.Abs(dy) < 0.000001f) return;
                var point = Vector3.Lerp(a, b, (level - a.y) / dy);
                var planar = new Vector2(point.x, point.z);
                if (crossings.Count == 0 || (crossings[0] - planar).sqrMagnitude > 0.00000001f)
                    crossings.Add(planar);
                if (!found) { footprint = new Bounds(point, Vector3.zero); found = true; }
                else footprint.Encapsulate(point);
            }
            var triangles = mesh.triangles;
            for (var i = 0; i < triangles.Length; i += 3)
            {
                var a = vertices[triangles[i]];
                var b = vertices[triangles[i + 1]];
                var c = vertices[triangles[i + 2]];
                crossings.Clear();
                IncludeCrossing(a, b);
                IncludeCrossing(b, c);
                IncludeCrossing(c, a);
                if (crossings.Count >= 2)
                {
                    edges.Add(crossings[0]);
                    edges.Add(crossings[1]);
                }
            }
            if (!found || edges.Count < 2) throw new InvalidOperationException($"Cannot locate the sand-contact contour of {rock.name}.");
            contactEdges = edges.ToArray();
            return footprint;
        }

        private static Surface SampleBase(List<BaseTile> tiles, Vector2 point)
        {
            foreach (var tile in tiles)
                if (tile.Contains(point)) return tile.Sample(point);
            throw new InvalidOperationException("The mixed formation extends beyond the authoring terrain.");
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
            internal bool Contains(Vector2 point) => point.x >= Origin.x && point.x <= Origin.x + Span
                && point.y >= Origin.z && point.y <= Origin.z + Span;
            internal bool Intersects(Bounds bounds) => Origin.x <= bounds.max.x && Origin.x + Span >= bounds.min.x
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
                int a, b, c;
                float wa, wb, wc;
                if (u + v <= 1f)
                {
                    a = bottom; b = bottom + 1; c = bottom + Resolution;
                    wa = 1f - u - v; wb = u; wc = v;
                }
                else
                {
                    a = bottom + Resolution + 1; b = bottom + Resolution; c = bottom + 1;
                    wa = u + v - 1f; wb = 1f - u; wc = 1f - v;
                }
                return new Surface(vertices[a].y * wa + vertices[b].y * wb + vertices[c].y * wc,
                    (normals[a] * wa + normals[b] * wb + normals[c] * wc).normalized,
                    colors[a] * wa + colors[b] * wb + colors[c] * wc);
            }
        }
    }
}
