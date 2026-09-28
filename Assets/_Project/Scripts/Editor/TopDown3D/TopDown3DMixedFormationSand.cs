using System;
using System.Collections.Generic;
using BooterBigArm.TopDown3D;
using UnityEngine;

namespace BooterBigArm.Editor
{
    /// <summary>Finds frozen formation contacts for local berms and formation clutter.</summary>
    internal static class TopDown3DMixedFormationSand
    {
        internal static IReadOnlyList<TopDown3DDustDepositionPlanner.AuthoredObstruction> CollectGroundContacts(
            TopDown3DLandscapeAuthoringSandbox sandbox,
            MeshCollider[] terrain, TopDown3DRockWorkbenchAuthoring[] rocks, bool includeSand = true)
        {
            var previews = new List<TopDown3DContactRockPreview>(rocks.Length);
            foreach (var rock in rocks)
                previews.Add(new TopDown3DContactRockPreview(rock.transform,
                    rock.GetComponent<MeshFilter>(), rock.GetComponent<MeshRenderer>(), rock.GenerationSeed));
            return CollectGroundContacts(sandbox, terrain, previews, includeSand);
        }

        internal static IReadOnlyList<TopDown3DDustDepositionPlanner.AuthoredObstruction> CollectGroundContacts(
            TopDown3DLandscapeAuthoringSandbox sandbox,
            MeshCollider[] terrain, IReadOnlyList<TopDown3DContactRockPreview> rocks,
            bool includeSand = true)
        {
            var buildup = includeSand ? sandbox.SandBuildup : 0f;
            var tiles = new List<BaseTile>(terrain.Length);
            foreach (var collider in terrain) tiles.Add(new BaseTile(collider));
            var sources = new List<TopDown3DDustDepositionPlanner.AuthoredObstruction>();
            foreach (var rock in rocks)
            {
                if (rock == null || rock.Filter == null || rock.Filter.sharedMesh == null
                    || rock.Renderer == null) continue;
                var bounds = rock.Renderer.bounds;
                var center = new Vector2(bounds.center.x, bounds.center.z);
                var floor = SampleBase(tiles, center).Height;
                // A suspended upper member is supported by the pile, not a separate ground collar.
                if (bounds.min.y > floor + 0.15f || bounds.max.y <= floor + 0.01f) continue;
                // The widest part of the rock can sit well above its buried base. Anchor the
                // bank to a mesh cross-section near ground contact, not the whole renderer box.
                // Contact detection must remain available when the raised sand field is off;
                // the flat visual collar still needs a stable section through the rock base.
                var contactSampleHeight = Mathf.Max(buildup,
                    sandbox.ContactSandTrimHeight > 0f ? 0.12f : 0f);
                var contact = ContactFootprint(rock.Filter.sharedMesh, rock.Transform.localToWorldMatrix,
                    rock.Transform.name, bounds, floor, contactSampleHeight, out var contactEdges);
                center = new Vector2(contact.center.x, contact.center.z);
                var source = new TopDown3DDustDepositionPlanner.AuthoredObstruction(
                    center, new Vector2(contact.extents.x, contact.extents.z), bounds.max.y - floor, contactEdges);
                sources.Add(source);
            }
            return sources;
        }

        private static Bounds ContactFootprint(Mesh mesh, Matrix4x4 matrix, string rockName,
            Bounds bounds, float floor, float buildup, out Vector2[] contactEdges)
        {
            var vertices = mesh.vertices;
            for (var i = 0; i < vertices.Length; i++) vertices[i] = matrix.MultiplyPoint3x4(vertices[i]);
            // Sample near the expected berm crest, so it can cover the lower face rather
            // than banking against an oval detached from the visible rock surface.
            var footprint = new Bounds();
            var edges = new List<Vector2>();
            var crossings = new List<Vector2>(3);
            bool TrySlice(float level)
            {
                var found = false;
                footprint = new Bounds();
                edges.Clear();
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
                    if (crossings.Count < 2) continue;
                    edges.Add(crossings[0]);
                    edges.Add(crossings[1]);
                }
                return found && edges.Count >= 2;
            }

            var level = Mathf.Clamp(floor + Mathf.Min(buildup, (bounds.max.y - floor) * 0.7f) * 0.65f,
                bounds.min.y + 0.001f, bounds.max.y - 0.001f);
            if (!TrySlice(level))
            {
                // Raised terrain can nearly bury shallow clutter. Use a stable slice through
                // the lower visible body before falling back to its grounded lower envelope.
                // Some workbench meshes contain a very thin lower cap whose triangles do not
                // cross either horizontal sample plane. They still need a bounded contact
                // contour: failing the whole stage leaves the playable authoring scene stale.
                var lowerLevel = Mathf.Lerp(bounds.min.y, bounds.max.y, 0.18f);
                if (!TrySlice(Mathf.Clamp(lowerLevel, bounds.min.y + 0.001f, bounds.max.y - 0.001f)))
                {
                    footprint = LowerEnvelope(vertices, bounds);
                    edges.Clear();
                    AddBoundsEdges(footprint, edges);
                }
            }
            contactEdges = edges.ToArray();
            return footprint;
        }

        private static Bounds LowerEnvelope(Vector3[] vertices, Bounds bounds)
        {
            var lowerBand = bounds.min.y + Mathf.Max(0.025f, bounds.size.y * 0.2f);
            var found = false;
            var envelope = new Bounds();
            for (var i = 0; i < vertices.Length; i++)
            {
                var vertex = vertices[i];
                if (vertex.y > lowerBand) continue;
                if (!found)
                {
                    envelope = new Bounds(vertex, Vector3.zero);
                    found = true;
                }
                else envelope.Encapsulate(vertex);
            }

            if (!found) envelope = bounds;
            var minimumExtent = 0.025f;
            envelope.extents = new Vector3(
                Mathf.Max(envelope.extents.x, minimumExtent),
                0f,
                Mathf.Max(envelope.extents.z, minimumExtent));
            return envelope;
        }

        private static void AddBoundsEdges(Bounds footprint, List<Vector2> edges)
        {
            var min = footprint.min;
            var max = footprint.max;
            var southwest = new Vector2(min.x, min.z);
            var southeast = new Vector2(max.x, min.z);
            var northeast = new Vector2(max.x, max.z);
            var northwest = new Vector2(min.x, max.z);
            edges.Add(southwest); edges.Add(southeast);
            edges.Add(southeast); edges.Add(northeast);
            edges.Add(northeast); edges.Add(northwest);
            edges.Add(northwest); edges.Add(southwest);
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
