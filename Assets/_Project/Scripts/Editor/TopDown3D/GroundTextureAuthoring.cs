using System;
using System.Collections.Generic;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Editor
{
    public static class GroundTextureAuthoring
    {
        public const string MaterialPath = "Assets/_Project/Materials/TopDown3D/GroundTextureSticker.mat";
        public const float SurfaceOffset = .015f;

        private static bool IsSurface(Collider c) => c is TerrainCollider || c is MeshCollider
            && (c.GetComponent<TopDown3DGroundSurface>() != null
                || c.gameObject.scene.path == FixedTerrainDressingStudyBuilder.ScenePath && c.name == "Sampled terrain patch");

        public static Collider[] Surfaces(Scene scene) => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Collider>())
            .Where(c => c.enabled && !c.isTrigger && IsSurface(c)).ToArray();

        public static bool TryHit(Scene scene, Ray ray, out RaycastHit nearest, Collider[] cachedSurfaces = null)
        {
            nearest = default; float distance = float.PositiveInfinity;
            // Collider-specific queries avoid painting a different additive scene or a prop on top of terrain.
            foreach (var collider in cachedSurfaces ?? Surfaces(scene))
            {
                if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy || collider.gameObject.scene != scene) continue;
                if (!collider.bounds.IntersectRay(ray)) continue;
                if (collider.Raycast(ray, out var hit, 100000) && hit.distance < distance)
                { distance = hit.distance; nearest = hit; }
            }
            return distance < float.PositiveInfinity;
        }

        public static GroundTextureSticker Create(Scene scene, Vector3 position, Texture2D texture, Vector2 size, float yaw)
        {
            if (!scene.IsValid() || !scene.isLoaded || texture == null) throw new ArgumentException("Choose a loaded scene and texture.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath)
                ?? throw new InvalidOperationException("Import the GroundTextureSticker material first.");
            var go = new GameObject("Ground Texture - " + texture.name);
            SceneManager.MoveGameObjectToScene(go, scene);
            Undo.RegisterCreatedObjectUndo(go, "Place ground texture sticker");
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            var sticker = Undo.AddComponent<GroundTextureSticker>(go);
            sticker.Configure(Guid.NewGuid().ToString("N"), texture, size);
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            if (!Conform(sticker, false))
            {
                Undo.DestroyObjectImmediate(go);
                throw new InvalidOperationException("No ground under this sticker. Choose terrain or marked ground mesh.");
            }
            EditorSceneManager.MarkSceneDirty(scene);
            return sticker;
        }

        public static bool Conform(GroundTextureSticker sticker, bool recordUndo = true)
        {
            if (sticker == null || EditorApplication.isPlayingOrWillChangePlaymode) return false;
            if (recordUndo) Undo.RecordObject(sticker, "Conform ground texture sticker");
            var size = sticker.Size;
            int nx = Mathf.Clamp(Mathf.CeilToInt(size.x / sticker.SampleSpacing), 2, 64);
            int nz = Mathf.Clamp(Mathf.CeilToInt(size.y / sticker.SampleSpacing), 2, 64);
            var vertices = new Vector3[(nx + 1) * (nz + 1)]; var normals = new Vector3[vertices.Length];
            var uv = new Vector2[vertices.Length]; var valid = new bool[vertices.Length]; var indices = new List<int>();
            var scene = sticker.gameObject.scene;
            var surfaces = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Collider>())
                .Where(c => c.enabled && !c.isTrigger && IsSurface(c)).ToArray();
            Physics.SyncTransforms();
            // Cache a bounded surface list once for this edit; sample only colliders whose XZ bounds overlap the patch.
            var patchBounds = new Bounds(sticker.transform.position, Vector3.zero);
            foreach (var corner in new[] { new Vector3(-size.x / 2, 0, -size.y / 2), new Vector3(size.x / 2, 0, -size.y / 2),
                new Vector3(-size.x / 2, 0, size.y / 2), new Vector3(size.x / 2, 0, size.y / 2) })
                patchBounds.Encapsulate(sticker.transform.TransformPoint(corner));
            surfaces = surfaces.Where(c => c.bounds.max.x >= patchBounds.min.x && c.bounds.min.x <= patchBounds.max.x
                && c.bounds.max.z >= patchBounds.min.z && c.bounds.min.z <= patchBounds.max.z).ToArray();
            float top = surfaces.Length == 0 ? 0 : surfaces.Max(c => c.bounds.max.y) + 1;
            float bottom = surfaces.Length == 0 ? 0 : surfaces.Min(c => c.bounds.min.y) - 1;
            for (int z = 0; z <= nz; z++) for (int x = 0; x <= nx; x++)
            {
                int i = z * (nx + 1) + x; uv[i] = new Vector2((float)x / nx, (float)z / nz);
                var point = sticker.transform.TransformPoint(new Vector3((uv[i].x - .5f) * size.x, 0, (uv[i].y - .5f) * size.y));
                float y = float.NegativeInfinity; RaycastHit chosen = default;
                foreach (var surface in surfaces)
                {
                    var bounds = surface.bounds;
                    if (point.x < bounds.min.x || point.x > bounds.max.x || point.z < bounds.min.z || point.z > bounds.max.z) continue;
                    if (surface.Raycast(new Ray(new Vector3(point.x, top, point.z), Vector3.down), out var hit, top - bottom)
                        && hit.point.y > y) { chosen = hit; y = hit.point.y; }
                }
                valid[i] = !float.IsNegativeInfinity(y);
                var world = valid[i] ? chosen.point + chosen.normal * SurfaceOffset : point;
                vertices[i] = sticker.transform.InverseTransformPoint(world);
                normals[i] = valid[i] ? sticker.transform.InverseTransformDirection(chosen.normal).normalized : Vector3.up;
            }
            for (int z = 0; z < nz; z++) for (int x = 0; x < nx; x++)
            {
                int a = z * (nx + 1) + x, b = a + nx + 1, c = a + 1, d = b + 1;
                // Fail closed for each entire cell around holes/missing samples.
                if (!valid[a] || !valid[b] || !valid[c] || !valid[d]) continue;
                indices.AddRange(new[] { a, b, c, c, b, d });
            }
            sticker.SetBakedGeometry(vertices, normals, uv, indices.ToArray());
            EditorUtility.SetDirty(sticker);
            EditorSceneManager.MarkSceneDirty(scene);
            return indices.Count > 0;
        }

        /// <summary>One bounded native alpha-map edit. A stroke's Undo group is owned by the UI.</summary>
        public static bool PaintTerrain(Terrain terrain, TerrainLayer layer, Vector3 center, float radius, float strength, float softness,
            ISet<TerrainData> strokeUndoTargets = null)
        {
            if (terrain == null || !terrain.enabled || terrain.terrainData == null || layer == null || radius <= 0 || strength <= 0) return false;
            if (!Finite(radius) || !Finite(strength) || !Finite(softness) || !Finite(center.x) || !Finite(center.y) || !Finite(center.z)) return false;
            var collider = terrain.GetComponent<TerrainCollider>();
            if (!terrain.gameObject.activeInHierarchy || collider == null || !collider.enabled || collider.terrainData != terrain.terrainData) return false;
            var data = terrain.terrainData; var origin = terrain.transform.position; var size = data.size;
            if (center.x + radius < origin.x || center.x - radius > origin.x + size.x
                || center.z + radius < origin.z || center.z - radius > origin.z + size.z) return false;
            if (terrain.transform.rotation != Quaternion.identity || terrain.transform.lossyScale != Vector3.one)
                throw new InvalidOperationException("Native painting requires unrotated, unit-scale terrain.");
            int width = data.alphamapWidth, height = data.alphamapHeight;
            int x0 = Mathf.Clamp(Mathf.FloorToInt((center.x - radius - origin.x) / size.x * width), 0, width - 1);
            int z0 = Mathf.Clamp(Mathf.FloorToInt((center.z - radius - origin.z) / size.z * height), 0, height - 1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt((center.x + radius - origin.x) / size.x * width), 0, width);
            int z1 = Mathf.Clamp(Mathf.CeilToInt((center.z + radius - origin.z) / size.z * height), 0, height);
            if (x1 <= x0 || z1 <= z0) return false;
            var amounts = new float[z1 - z0, x1 - x0]; bool any = false;
            for (int z = 0; z < amounts.GetLength(0); z++) for (int x = 0; x < amounts.GetLength(1); x++)
            {
                float wx = origin.x + (x0 + x + .5f) / width * size.x;
                float wz = origin.z + (z0 + z + .5f) / height * size.z;
                float distance = Vector2.Distance(new Vector2(wx, wz), new Vector2(center.x, center.z)) / radius;
                if (distance >= 1) continue;
                int hx = Mathf.Clamp(Mathf.FloorToInt((wx - origin.x) / size.x * data.holesResolution), 0, data.holesResolution - 1);
                int hz = Mathf.Clamp(Mathf.FloorToInt((wz - origin.z) / size.z * data.holesResolution), 0, data.holesResolution - 1);
                if (data.IsHole(hx, hz)) continue;
                amounts[z, x] = Mathf.Clamp01(strength) * (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(1 - Mathf.Clamp(softness, .01f, 1), 1, distance)));
                any |= amounts[z, x] > 0;
            }
            if (!any) return false;
            var layers = data.terrainLayers;
            int target = Array.IndexOf(layers, layer);
            if (strokeUndoTargets == null || strokeUndoTargets.Add(data))
            {
                Undo.RegisterCompleteObjectUndo(data, "Paint terrain texture");
                foreach (var alphaTexture in data.alphamapTextures)
                    Undo.RegisterCompleteObjectUndo(alphaTexture, "Paint terrain texture");
            }
            if (target < 0)
            {
                target = layers.Length;
                Array.Resize(ref layers, target + 1); layers[target] = layer; data.terrainLayers = layers;
            }
            var weights = data.GetAlphamaps(x0, z0, x1 - x0, z1 - z0);
            var packed = new int[weights.GetLength(2)]; var fractions = new float[packed.Length];
            bool changed = false;
            for (int z = 0; z < weights.GetLength(0); z++) for (int x = 0; x < weights.GetLength(1); x++)
            {
                float amount = amounts[z, x]; if (amount <= 0) continue;
                float sum = 0;
                for (int l = 0; l < weights.GetLength(2); l++)
                { weights[z, x, l] = Mathf.Lerp(weights[z, x, l], l == target ? 1 : 0, amount); sum += weights[z, x, l]; }
                if (sum > 0)
                {
                    // Native alpha textures are UNorm8. Largest-remainder packing keeps the stored
                    // channels summing to 255 instead of independently rounding them to 254/256.
                    int total = 0;
                    for (int l = 0; l < packed.Length; l++)
                    {
                        float scaled = weights[z, x, l] / sum * 255f;
                        packed[l] = Mathf.FloorToInt(scaled); fractions[l] = scaled - packed[l]; total += packed[l];
                    }
                    for (int remaining = 255 - total; remaining > 0; remaining--)
                    {
                        int best = 0; for (int l = 1; l < packed.Length; l++) if (fractions[l] > fractions[best]) best = l;
                        packed[best]++; fractions[best] = -1;
                    }
                    for (int l = 0; l < packed.Length; l++) weights[z, x, l] = packed[l] / 255f;
                }
                changed = true;
            }
            if (changed) { data.SetAlphamaps(x0, z0, weights); EditorUtility.SetDirty(data); }
            return changed;
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
