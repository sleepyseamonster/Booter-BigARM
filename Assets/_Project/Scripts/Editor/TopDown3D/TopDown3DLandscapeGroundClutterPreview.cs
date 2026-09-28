using System;
using System.Collections.Generic;
using BooterBigArm.TopDown3D;
using UnityEngine;
using UnityEngine.Rendering;

namespace BooterBigArm.Editor
{
    /// <summary>
    /// Reuses the production natural-object plan to preview cosmetic terrain clutter on the
    /// disposable flat formation stage. The preview owns no persistent world state.
    /// </summary>
    internal static class TopDown3DLandscapeGroundClutterPreview
    {
        private const string RootName = "Natural Landscape Clutter";

        internal static int Apply(
            TopDown3DLandscapeAuthoringSandbox sandbox,
            Transform parent,
            MeshCollider[] terrain,
            TopDown3DRockWorkbenchAuthoring[] formationRocks)
        {
            return Apply(sandbox, parent, terrain, formationRocks, null);
        }

        internal static int Apply(
            TopDown3DLandscapeAuthoringSandbox sandbox,
            Transform parent,
            MeshCollider[] terrain,
            TopDown3DRockWorkbenchAuthoring[] formationRocks,
            List<TopDown3DContactRockPreview> contactRocks)
        {
            if (sandbox == null || parent == null || terrain == null || terrain.Length == 0
                || sandbox.LandscapeClutter <= 0f || sandbox.WorldSettings == null
                || sandbox.WorldSettings.NaturalObjectCatalog == null)
            {
                return 0;
            }

            var settings = sandbox.WorldSettings;
            var catalog = settings.NaturalObjectCatalog;
            var terrainBounds = terrain[0].bounds;
            for (var i = 1; i < terrain.Length; i++) terrainBounds.Encapsulate(terrain[i].bounds);

            var rockBounds = new List<Bounds>(formationRocks != null ? formationRocks.Length : 0);
            if (formationRocks != null)
            {
                for (var i = 0; i < formationRocks.Length; i++)
                {
                    var renderer = formationRocks[i] != null
                        ? formationRocks[i].GetComponent<MeshRenderer>()
                        : null;
                    if (renderer != null) rockBounds.Add(renderer.bounds);
                }
            }

            var regularMaterial = sandbox.RegularRockMaterial;
            if (regularMaterial == null && formationRocks != null && formationRocks.Length > 0)
            {
                var renderer = formationRocks[0].GetComponent<MeshRenderer>();
                if (renderer != null) regularMaterial = renderer.sharedMaterial;
            }
            if (regularMaterial == null) return 0;

            var root = new GameObject(RootName)
            {
                hideFlags = HideFlags.DontSaveInEditor | HideFlags.NotEditable
            };
            root.transform.SetParent(parent, false);

            var buckets = new Dictionary<BucketKey, List<CombineInstance>>();
            var placementCount = 0;
            var generator = new TopDown3DWorldGenerator(settings);
            var chunkSize = settings.ChunkSize;
            var minimumChunk = new Vector2Int(
                Mathf.FloorToInt(terrainBounds.min.x / chunkSize),
                Mathf.FloorToInt(terrainBounds.min.z / chunkSize));
            var maximumChunk = new Vector2Int(
                Mathf.FloorToInt((terrainBounds.max.x - 0.0001f) / chunkSize),
                Mathf.FloorToInt((terrainBounds.max.z - 0.0001f) / chunkSize));
            var distantExclusion = new Vector2(1000000f, 1000000f);

            for (var chunkZ = minimumChunk.y; chunkZ <= maximumChunk.y; chunkZ++)
            {
                for (var chunkX = minimumChunk.x; chunkX <= maximumChunk.x; chunkX++)
                {
                    var plan = TopDown3DNaturalObjectPlanner.BuildChunkPlan(
                        settings,
                        generator,
                        catalog,
                        new Vector2Int(chunkX, chunkZ),
                        distantExclusion);
                    for (var placementIndex = 0;
                         placementIndex < plan.CosmeticPlacements.Count;
                         placementIndex++)
                    {
                        var placement = plan.CosmeticPlacements[placementIndex];
                        if (!Admit(placement, sandbox.LandscapeClutter)
                            || placement.Position.x < terrainBounds.min.x
                            || placement.Position.x > terrainBounds.max.x
                            || placement.Position.z < terrainBounds.min.z
                            || placement.Position.z > terrainBounds.max.z
                            || !TryGround(terrain, placement.Position, out var hit)
                            || IntersectsFormation(hit.point, placement.FootprintRadius, rockBounds)
                            || !catalog.TryGetMeshFamily(placement.Shape, placement.Variant, out var family)
                            || family.Lod2 == null)
                        {
                            continue;
                        }

                        var material = ResolveMaterial(settings, regularMaterial, placement);
                        if (material == null) continue;
                        var key = new BucketKey(placement.Layer, placement.Surface, material);
                        if (!buckets.TryGetValue(key, out var instances))
                        {
                            instances = new List<CombineInstance>();
                            buckets.Add(key, instances);
                        }

                        var yaw = Quaternion.AngleAxis(placement.Rotation.eulerAngles.y, Vector3.up);
                        var rotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * yaw;
                        var localShape = Matrix4x4.TRS(Vector3.zero, rotation, placement.Scale);
                        var bottom = TransformedBottom(family.Lod2.bounds, localShape);
                        var sink = Mathf.Min(placement.FootprintRadius * 0.12f,
                            family.Lod2.bounds.size.y * placement.Scale.y * 0.16f);
                        var position = hit.point + Vector3.up * (-bottom - sink);
                        var worldPose = Matrix4x4.TRS(position, rotation, placement.Scale);
                        instances.Add(new CombineInstance
                        {
                            mesh = family.Lod2,
                            transform = parent.worldToLocalMatrix * worldPose
                        });
                        if (contactRocks != null && placement.FootprintRadius >= 0.12f)
                            AddContactProxy(root.transform, family.Lod2, material, worldPose,
                                placement.StableId, contactRocks);
                        placementCount++;
                    }
                }
            }

            foreach (var pair in buckets)
            {
                if (pair.Value.Count == 0) continue;
                var mesh = new Mesh
                {
                    name = $"Landscape {pair.Key.Layer} {pair.Key.Surface}",
                    indexFormat = IndexFormat.UInt32,
                    hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontUnloadUnusedAsset
                };
                mesh.CombineMeshes(pair.Value.ToArray(), true, true);
                var child = new GameObject(mesh.name)
                {
                    hideFlags = HideFlags.DontSaveInEditor | HideFlags.NotEditable
                };
                child.transform.SetParent(root.transform, false);
                child.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = child.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = pair.Key.Material;
                renderer.shadowCastingMode = pair.Key.Layer == TopDown3DNaturalObjectLayer.Scatter
                    ? ShadowCastingMode.On
                    : ShadowCastingMode.Off;
                renderer.receiveShadows = true;
            }

            if (root.transform.childCount == 0) UnityEngine.Object.DestroyImmediate(root);
            return placementCount;
        }

        private static void AddContactProxy(
            Transform root,
            Mesh mesh,
            Material material,
            Matrix4x4 worldPose,
            string stableId,
            List<TopDown3DContactRockPreview> contactRocks)
        {
            var proxy = new GameObject("Landscape Stone Contact Proxy")
            {
                hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInEditor | HideFlags.NotEditable
            };
            proxy.transform.SetParent(root, false);
            var localPose = root.worldToLocalMatrix * worldPose;
            proxy.transform.localPosition = localPose.GetColumn(3);
            proxy.transform.localRotation = localPose.rotation;
            proxy.transform.localScale = localPose.lossyScale;
            var filter = proxy.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = proxy.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.enabled = false;
            contactRocks.Add(new TopDown3DContactRockPreview(
                proxy.transform, filter, renderer, StableHash(stableId)));
        }

        private static int StableHash(string text)
        {
            unchecked
            {
                uint hash = 2166136261u;
                text ??= string.Empty;
                for (var i = 0; i < text.Length; i++) hash = (hash ^ text[i]) * 16777619u;
                return (int)hash;
            }
        }

        private static bool TryGround(MeshCollider[] terrain, Vector3 position, out RaycastHit hit)
        {
            for (var i = 0; i < terrain.Length; i++)
            {
                var bounds = terrain[i].bounds;
                if (position.x < bounds.min.x || position.x > bounds.max.x
                    || position.z < bounds.min.z || position.z > bounds.max.z)
                {
                    continue;
                }
                var origin = new Vector3(position.x, bounds.max.y + 1f, position.z);
                if (terrain[i].Raycast(new Ray(origin, Vector3.down), out hit, bounds.size.y + 2f))
                    return true;
            }
            hit = default;
            return false;
        }

        private static bool IntersectsFormation(Vector3 point, float footprint, List<Bounds> rocks)
        {
            for (var i = 0; i < rocks.Count; i++)
            {
                var bounds = rocks[i];
                bounds.Expand(new Vector3(footprint * 1.3f, 0f, footprint * 1.3f));
                if (point.x >= bounds.min.x && point.x <= bounds.max.x
                    && point.z >= bounds.min.z && point.z <= bounds.max.z)
                {
                    return true;
                }
            }
            return false;
        }

        private static Material ResolveMaterial(
            TopDown3DWorldSettings settings,
            Material regularMaterial,
            TopDown3DNaturalObjectPlacement placement)
        {
            if (placement.Layer == TopDown3DNaturalObjectLayer.FineGrayCluster)
                return settings.FineGrayClutterMaterial != null
                    ? settings.FineGrayClutterMaterial
                    : regularMaterial;
            if (placement.Surface == TopDown3DRockSurface.Dark && settings.DarkRockMaterial != null)
                return settings.DarkRockMaterial;
            if (placement.Surface == TopDown3DRockSurface.Teal && settings.TealRockMaterial != null)
                return settings.TealRockMaterial;
            return regularMaterial;
        }

        private static float TransformedBottom(Bounds bounds, Matrix4x4 matrix)
        {
            var minimum = float.PositiveInfinity;
            for (var z = -1; z <= 1; z += 2)
            {
                for (var y = -1; y <= 1; y += 2)
                {
                    for (var x = -1; x <= 1; x += 2)
                    {
                        var corner = bounds.center + Vector3.Scale(
                            bounds.extents, new Vector3(x, y, z));
                        minimum = Mathf.Min(minimum, matrix.MultiplyPoint3x4(corner).y);
                    }
                }
            }
            return minimum;
        }

        private static bool Admit(TopDown3DNaturalObjectPlacement placement, float amount)
        {
            if (amount >= 0.999f) return true;
            unchecked
            {
                var hash = 2166136261u;
                var id = placement.StableId ?? string.Empty;
                for (var i = 0; i < id.Length; i++) hash = (hash ^ id[i]) * 16777619u;
                hash = (hash ^ (uint)Mathf.RoundToInt(placement.Position.x * 100f)) * 16777619u;
                hash = (hash ^ (uint)Mathf.RoundToInt(placement.Position.z * 100f)) * 16777619u;
                hash ^= hash >> 16;
                hash *= 0x7FEB352Du;
                hash ^= hash >> 15;
                return (hash & 0x00FFFFFFu) / 16777215f <= amount;
            }
        }

        private readonly struct BucketKey : IEquatable<BucketKey>
        {
            internal BucketKey(
                TopDown3DNaturalObjectLayer layer,
                TopDown3DRockSurface surface,
                Material material)
            {
                Layer = layer;
                Surface = surface;
                Material = material;
            }

            internal TopDown3DNaturalObjectLayer Layer { get; }
            internal TopDown3DRockSurface Surface { get; }
            internal Material Material { get; }

            public bool Equals(BucketKey other)
            {
                return Layer == other.Layer && Surface == other.Surface && Material == other.Material;
            }

            public override bool Equals(object obj)
            {
                return obj is BucketKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = (int)Layer;
                    hash = hash * 397 ^ (int)Surface;
                    hash = hash * 397 ^ (Material != null ? Material.GetInstanceID() : 0);
                    return hash;
                }
            }
        }
    }
}
