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
            var area = rocks[0].GetComponent<MeshRenderer>().bounds;
            foreach (var rock in rocks)
            {
                var rockBounds = rock.GetComponent<MeshRenderer>().bounds;
                bounds.Add(rockBounds);
                area.Encapsulate(rockBounds);
            }
            area.Expand(3f);
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
                var mask = new Vector2[vertices.Length];
                for (var i = 0; i < vertices.Length; i++)
                    mask[i] = new Vector2(Coverage(ground.transform.TransformPoint(vertices[i])), 0f);
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
            const float spacing = 0.4f;
            for (var z = Mathf.FloorToInt(area.min.z / spacing); z <= Mathf.CeilToInt(area.max.z / spacing); z++)
            for (var x = Mathf.FloorToInt(area.min.x / spacing); x <= Mathf.CeilToInt(area.max.x / spacing); x++)
            {
                var seed = unchecked(sandbox.WorldSettings.WorldSeed ^ x * 73856093 ^ z * 19349663);
                var point = new Vector3((x + Unit(seed)) * spacing, 0f, (z + Unit(seed ^ 1717)) * spacing);
                // Denser real stones without changing the accepted texture coverage or sizes.
                var chance = Coverage(point) * 0.48f;
                if (instances.Count >= 192 || Unit(seed ^ 7171) > chance) continue;
                var hitGround = false;
                var hit = default(RaycastHit);
                MeshCollider supportingGround = null;
                foreach (var ground in terrain)
                {
                    var box = ground.bounds;
                    var start = new Vector3(point.x, box.max.y + 1f, point.z);
                    if (!ground.Raycast(new Ray(start, Vector3.down), out hit, box.size.y + 2f)) continue;
                    supportingGround = ground;
                    hitGround = true;
                    break;
                }
                if (!hitGround || hit.normal.y < 0.85f) continue;
                var indices = triangles[supportingGround];
                var tint = colors[supportingGround];
                var t = hit.triangleIndex * 3;
                var bary = hit.barycentricCoordinate;
                var sand = tint[indices[t]].r * bary.x + tint[indices[t + 1]].r * bary.y
                    + tint[indices[t + 2]].r * bary.z;
                if (Unit(seed ^ 9091) < Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.5f, 0.85f, sand))) continue;
                var occupied = false;
                foreach (var rock in bounds)
                {
                    var exclusion = rock;
                    exclusion.Expand(0.14f);
                    if (exclusion.Contains(hit.point + Vector3.up * 0.04f)) { occupied = true; break; }
                }
                if (occupied || !catalog.TryGetMeshFamily(TopDown3DNaturalObjectShape.Pebble,
                        (int)(Unit(seed ^ 3131) * 2.999f), out var family)) continue;
                var source = family.Lod2;
                if (source == null) continue;
                var size = Mathf.Lerp(0.055f, 0.16f,
                    (Unit(seed ^ 1919) + Unit(seed ^ 2929) + Unit(seed ^ 3939)) / 3f);
                var dimensions = source.bounds.size;
                var scale = size / Mathf.Max(dimensions.x, dimensions.y, dimensions.z);
                var rotation = Quaternion.FromToRotation(Vector3.up, hit.normal)
                    * Quaternion.Euler(0f, Unit(seed ^ 4141) * 360f, 0f);
                var shape = Matrix4x4.TRS(Vector3.zero, rotation, new Vector3(scale, scale * 0.65f, scale))
                    * Matrix4x4.Translate(-source.bounds.center);
                var bottom = float.PositiveInfinity;
                foreach (var vertex in source.vertices) bottom = Mathf.Min(bottom, shape.MultiplyPoint3x4(vertex).y);
                var pose = Matrix4x4.Translate(hit.point + Vector3.up * (-bottom - size * 0.22f)) * shape;
                instances.Add(new CombineInstance { mesh = source, transform = parent.worldToLocalMatrix * pose });
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
