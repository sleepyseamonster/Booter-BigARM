using System;
using System.Collections.Generic;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace BooterBigArm.Editor
{
    internal static class TopDown3DMixedFormationClutter
    {
        internal static void Apply(TopDown3DLandscapeAuthoringSandbox sandbox, Transform parent,
            MeshCollider[] terrain, TopDown3DRockWorkbenchAuthoring[] rocks)
        {
            if (sandbox.GroundClutter <= 0f || rocks.Length == 0) return;
            const string textureFolder = "Assets/_Project/Art/Environment/Ground/SandDirt/";
            var pebbleColor = AssetDatabase.LoadAssetAtPath<Texture2D>(textureFolder + "MixedGroundPebbles_Albedo.png");
            var pebbleHeight = AssetDatabase.LoadAssetAtPath<Texture2D>(textureFolder + "MixedGroundPebbles_Height.png");
            if (pebbleColor == null || pebbleHeight == null)
                throw new InvalidOperationException("Mixed ground needs its pebble color and height textures.");
            var bounds = new List<Bounds>(rocks.Length);
            foreach (var rock in rocks)
            {
                var rockBounds = rock.GetComponent<MeshRenderer>().bounds;
                bounds.Add(rockBounds);
            }
            float Coverage(Vector3 point)
            {
                var closest = float.PositiveInfinity;
                foreach (var rock in bounds)
                {
                    var dx = Mathf.Max(0f, Mathf.Abs(point.x - rock.center.x) - rock.extents.x);
                    var dz = Mathf.Max(0f, Mathf.Abs(point.z - rock.center.z) - rock.extents.z);
                    closest = Mathf.Min(closest, Mathf.Sqrt(dx * dx + dz * dz));
                }
                return Mathf.SmoothStep(1f, 0f, Mathf.Clamp01(closest / 1.5f)) * sandbox.GroundClutter;
            }
            var triangles = new Dictionary<MeshCollider, int[]>();
            var colors = new Dictionary<MeshCollider, Color[]>();
            foreach (var ground in terrain)
            {
                var mesh = ground.sharedMesh;
                var vertices = mesh.vertices;
                var mask = mesh.uv2;
                if (mask.Length != vertices.Length) mask = new Vector2[vertices.Length];
                for (var i = 0; i < vertices.Length; i++)
                    mask[i].x = Coverage(ground.transform.TransformPoint(vertices[i]));
                mesh.SetUVs(1, mask);
                var groundRenderer = ground.GetComponent<MeshRenderer>();
                var properties = new MaterialPropertyBlock();
                groundRenderer.GetPropertyBlock(properties);
                properties.SetFloat("_PebbleDetail", 1f);
                properties.SetTexture("_PebbleAlbedoMap", pebbleColor);
                properties.SetTexture("_PebbleHeightMap", pebbleHeight);
                groundRenderer.SetPropertyBlock(properties);
                triangles.Add(ground, mesh.triangles);
                colors.Add(ground, mesh.colors);
            }

            var catalog = sandbox.WorldSettings.NaturalObjectCatalog;
            if (catalog == null) return;
            var instances = new List<CombineInstance>();
            bool GroundAt(Vector3 point, out RaycastHit hit, out MeshCollider supportingGround)
            {
                foreach (var ground in terrain)
                {
                    var box = ground.bounds;
                    if (point.x < box.min.x || point.x > box.max.x
                        || point.z < box.min.z || point.z > box.max.z) continue;
                    var start = new Vector3(point.x, box.max.y + 1f, point.z);
                    if (!ground.Raycast(new Ray(start, Vector3.down), out hit, box.size.y + 2f)) continue;
                    supportingGround = ground;
                    return true;
                }
                hit = default;
                supportingGround = null;
                return false;
            }

            // Seed irregular pockets from individual ground-contact rocks, not a world grid.
            // Upper pile members must not project extra clutter clusters onto the ground below.
            var clusters = new List<(Vector3 Center, int Seed)>();
            for (var rockIndex = 0; rockIndex < rocks.Length; rockIndex++)
            {
                var box = bounds[rockIndex];
                if (!GroundAt(box.center, out var floor, out _) || box.min.y > floor.point.y + 0.18f) continue;
                var rockSeed = unchecked(sandbox.WorldSettings.WorldSeed ^ rocks[rockIndex].GenerationSeed
                    ^ rockIndex * 486187739);
                var firstAngle = Unit(rockSeed ^ 5171) * Mathf.PI * 2f;
                for (var pocket = 0; pocket < 2; pocket++)
                {
                    var seed = unchecked(rockSeed ^ (pocket + 1) * 19349663);
                    var angle = firstAngle + pocket * Mathf.Lerp(2.1f, 4.1f, Unit(seed ^ 6262));
                    var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                    var edge = Mathf.Min(box.extents.x / Mathf.Max(0.001f, Mathf.Abs(direction.x)),
                        box.extents.z / Mathf.Max(0.001f, Mathf.Abs(direction.z)));
                    clusters.Add((box.center + direction * (edge + Mathf.Lerp(0.08f, 0.22f, Unit(seed))), seed));
                }
            }
            var placed = new List<Vector4>();
            // Round-robin attempts give every pocket a chance before the shared mesh cap.
            for (var attempt = 0; attempt < 18 && instances.Count < 192; attempt++)
            foreach (var cluster in clusters)
            {
                if (instances.Count >= 192) break;
                var seed = unchecked(cluster.Seed ^ (attempt + 1) * 73856093);
                if (Unit(seed ^ 7171) > sandbox.GroundClutter * 0.85f) continue;
                float Bell(int salt) => (Unit(seed ^ salt) + Unit(seed ^ (salt + 7919))
                    + Unit(seed ^ (salt + 15401))) / 3f;
                var spread = Mathf.Lerp(0.3f, 0.55f, Unit(cluster.Seed ^ 8383));
                var point = cluster.Center + new Vector3((Bell(1171) - 0.5f) * spread * 2f, 0f,
                    (Bell(2171) - 0.5f) * spread * 2f);
                var sizeClass = Unit(seed ^ 1515);
                var size = sizeClass < 0.55f ? Mathf.Lerp(0.07f, 0.16f, Bell(1919))
                    : sizeClass < 0.85f ? Mathf.Lerp(0.16f, 0.28f, Bell(1919))
                    : Mathf.Lerp(0.28f, 0.42f, Bell(1919));
                if (!GroundAt(point, out var hit, out var supportingGround) || hit.normal.y < 0.85f) continue;
                var indices = triangles[supportingGround];
                var tint = colors[supportingGround];
                var t = hit.triangleIndex * 3;
                var bary = hit.barycentricCoordinate;
                var sand = tint[indices[t]].r * bary.x + tint[indices[t + 1]].r * bary.y
                    + tint[indices[t + 2]].r * bary.z;
                // Sand hides some chips, not every cluster next to a deposited skirt.
                var sandWeight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.5f, 0.85f, sand));
                if (Unit(seed ^ 9091) < sandWeight * (sizeClass < 0.55f ? 0.55f : 0.3f)) continue;
                var occupied = false;
                foreach (var rock in bounds)
                {
                    var exclusion = rock;
                    exclusion.Expand(size * 0.6f);
                    if (exclusion.Contains(hit.point + Vector3.up * size * 0.2f)) { occupied = true; break; }
                }
                foreach (var previous in placed)
                {
                    var delta = new Vector2(hit.point.x - previous.x, hit.point.z - previous.z);
                    var separation = (size + previous.w) * 0.36f;
                    if (delta.sqrMagnitude < separation * separation) { occupied = true; break; }
                }
                if (occupied || !catalog.TryGetMeshFamily(TopDown3DNaturalObjectShape.Nodule,
                        (int)(Unit(seed ^ 3131) * 2.999f), out var family)) continue;
                // Approved irregular geometry and smooth normals, not the old polygonal pebble prisms.
                // LOD2 retains the softened silhouette while bounding this editor preview's mesh cost.
                var source = family.Lod2;
                if (source == null) continue;
                var dimensions = source.bounds.size;
                var scale = size / Mathf.Max(0.001f, Mathf.Max(dimensions.x, dimensions.z));
                var heightRatio = sizeClass < 0.55f ? Mathf.Lerp(0.32f, 0.5f, Bell(5151))
                    : sizeClass < 0.85f ? Mathf.Lerp(0.5f, 0.7f, Bell(5151))
                    : Mathf.Lerp(0.75f, 0.95f, Bell(5151));
                var verticalScale = size * heightRatio / Mathf.Max(0.001f, dimensions.y);
                var rotation = Quaternion.FromToRotation(Vector3.up, hit.normal)
                    * Quaternion.Euler(0f, Unit(seed ^ 4141) * 360f, 0f);
                var shape = Matrix4x4.TRS(Vector3.zero, rotation,
                    new Vector3(scale * Mathf.Lerp(0.85f, 1f, Unit(seed ^ 6161)), verticalScale,
                        scale * Mathf.Lerp(0.85f, 1f, Unit(seed ^ 8181))))
                    * Matrix4x4.Translate(-source.bounds.center);
                var bottom = float.PositiveInfinity;
                foreach (var vertex in source.vertices) bottom = Mathf.Min(bottom, shape.MultiplyPoint3x4(vertex).y);
                var pose = Matrix4x4.Translate(hit.point + Vector3.up * (-bottom - size * Mathf.Lerp(0.22f, 0.34f, sandWeight))) * shape;
                instances.Add(new CombineInstance { mesh = source, transform = parent.worldToLocalMatrix * pose });
                placed.Add(new Vector4(hit.point.x, hit.point.y, hit.point.z, size));
            }
            if (instances.Count == 0) return;
            var output = new Mesh { name = "Mixed Surface Stones", indexFormat = IndexFormat.UInt32,
                hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontUnloadUnusedAsset };
            output.CombineMeshes(instances.ToArray(), true, true);
            var child = new GameObject("Surface Stones") { hideFlags = HideFlags.DontSaveInEditor | HideFlags.NotEditable };
            child.transform.SetParent(parent, false);
            child.AddComponent<MeshFilter>().sharedMesh = output;
            var renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = rocks[0].GetComponent<MeshRenderer>().sharedMaterial;
            TopDown3DRockWorkbenchPreview.ApplySurfaceProperties(rocks[0], renderer, Vector3.one * 0.12f,
                sandbox.WorldSettings.WorldSeed, Vector3.zero, 0f, 6f);
        }

        private static float Unit(int seed)
        {
            unchecked
            {
                var value = (uint)seed;
                value = (value ^ (value >> 16)) * 0x7FEB352Du;
                value = (value ^ (value >> 15)) * 0x846CA68Bu;
                return ((value ^ (value >> 16)) & 0xFFFFFF) / 16777215f;
            }
        }
    }
}
