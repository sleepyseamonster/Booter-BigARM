using System;
using System.Collections.Generic;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Editor
{
    /// <summary>
    /// Deterministically bakes the representative Broken World rock family into project mesh assets.
    /// This is an editor authoring path only; player/runtime generation consumes catalog references.
    /// </summary>
    public static class TopDown3DProductionRockBaker
    {
        public const string OutputFolder = "Assets/_Project/Art/Environment/Rocks/Generated";

        private const float Lod0ScreenHeight = 0.24f;
        private const float Lod1ScreenHeight = 0.085f;
        private const float Lod2ScreenHeight = 0.015f;

        [MenuItem("Booter & BigARM/Top Down 3D/Bake Production Rock Family")]
        public static void BakeFromMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Bake Production Rock Family",
                    "Bake or update deterministic rock mesh assets and replace the catalog's baked-mesh references?",
                    "Bake",
                    "Cancel"))
            {
                return;
            }

            Bake();
        }

        public static void BakeFromCli()
        {
            Bake();
        }

        public static void Bake()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<TopDown3DNaturalObjectCatalog>(
                TopDown3DPrototypeBuilder.NaturalObjectCatalogPath);
            if (catalog == null)
            {
                throw new InvalidOperationException(
                    $"Missing natural-object catalog at {TopDown3DPrototypeBuilder.NaturalObjectCatalogPath}.");
            }

            EnsureFolder(OutputFolder);
            var families = new List<TopDown3DNaturalMeshFamily>();
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (TopDown3DNaturalObjectShape shape in Enum.GetValues(typeof(TopDown3DNaturalObjectShape)))
                {
                    for (var variant = 0; variant < TopDown3DNaturalObjectCatalog.MeshVariantsPerShape; variant++)
                    {
                        var meshes = BakeFamilyAsset(shape, variant);
                        var colliderBounds = meshes.Lod0.bounds;
                        colliderBounds.size = Vector3.Scale(
                            colliderBounds.size,
                            new Vector3(0.9f, 0.94f, 0.9f));
                        var family = new TopDown3DNaturalMeshFamily();
                        family.Configure(
                            GetStableId(shape, variant),
                            shape,
                            variant,
                            meshes.Lod0,
                            meshes.Lod1,
                            meshes.Lod2,
                            colliderBounds,
                            Lod0ScreenHeight,
                            Lod1ScreenHeight,
                            Lod2ScreenHeight);
                        families.Add(family);
                    }
                }

                catalog.ReplaceBakedMeshFamilies(families);
                EditorUtility.SetDirty(catalog);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var errors = TopDown3DLandscapeAssetValidator.CollectRockFamilyErrors(catalog);
            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    "Production rock bake failed validation:\n- " + string.Join("\n- ", errors));
            }

            Debug.Log(
                $"Baked {families.Count} deterministic production rock families into {OutputFolder}.");
        }

        internal static Mesh BuildMesh(
            TopDown3DNaturalObjectShape shape,
            int variant,
            int lod)
        {
            if (lod < 0 || lod > 2)
            {
                throw new ArgumentOutOfRangeException(nameof(lod));
            }

            variant = TopDown3DNaturalObjectCatalog.NormalizeMeshVariant(variant);
            var profile = GetProfile(shape);
            var segments = Mathf.Max(5, profile.BaseSegments - lod * 2);
            var layers = Mathf.Max(3, profile.BaseLayers - lod);
            var rings = new Vector3[layers, segments];
            var fractureAngle = Hash01((int)shape, variant, 491, 37) * Mathf.PI * 2f;
            var fractureDirection = new Vector2(Mathf.Cos(fractureAngle), Mathf.Sin(fractureAngle));
            var driftDirection = new Vector2(-fractureDirection.y, fractureDirection.x);

            for (var layer = 0; layer < layers; layer++)
            {
                var layerT = layers > 1 ? layer / (float)(layers - 1) : 0f;
                var taper = Mathf.Lerp(1f, profile.TopScale, Mathf.Pow(layerT, profile.TaperPower));
                var shoulder = Mathf.Sin(layerT * Mathf.PI) * profile.ShoulderBulge;
                var undercut = layer == 0 ? profile.BaseUndercut : 0f;
                var strata = 1f + Mathf.Sin((layer + variant * 0.37f) * 2.4f) * profile.StrataStrength;
                var layerScale = Mathf.Max(0.12f, (taper + shoulder - undercut) * strata);
                var centerDrift = driftDirection
                    * profile.Drift
                    * (layerT * layerT)
                    * Mathf.Lerp(-1f, 1f, Hash01((int)shape, variant, layer, 613));

                for (var segment = 0; segment < segments; segment++)
                {
                    var angle = segment / (float)segments * Mathf.PI * 2f;
                    var silhouette = Mathf.Lerp(
                        0.82f,
                        1.18f,
                        Hash01((int)shape, variant, segment, 719));
                    var layerJitter = Mathf.Lerp(
                        0.94f,
                        1.06f,
                        Hash01((int)shape, variant, layer, segment + 811));
                    var radialScale = layerScale * silhouette * layerJitter;
                    var x = Mathf.Cos(angle) * profile.RadiusX * radialScale + centerDrift.x;
                    var z = Mathf.Sin(angle) * profile.RadiusZ * radialScale + centerDrift.y;

                    var projected = x * fractureDirection.x + z * fractureDirection.y;
                    var fractureStart = Mathf.Lerp(0.32f, 0.55f, Hash01((int)shape, variant, 907, 53));
                    var fracture = Mathf.Max(0f, projected - fractureStart) * profile.FractureStrength;
                    x -= fractureDirection.x * fracture;
                    z -= fractureDirection.y * fracture;

                    var y = profile.Height * layerT;
                    if (layer > 0 && layer < layers - 1)
                    {
                        y += profile.Height
                            * Mathf.Lerp(-0.018f, 0.018f, Hash01(variant, segment, layer, 1013));
                    }

                    rings[layer, segment] = new Vector3(x, y, z);
                }
            }

            var vertices = new List<Vector3>(segments * layers * 8);
            var normals = new List<Vector3>(segments * layers * 8);
            var triangles = new List<int>(segments * layers * 8);
            for (var layer = 0; layer < layers - 1; layer++)
            {
                for (var segment = 0; segment < segments; segment++)
                {
                    var next = (segment + 1) % segments;
                    AddTriangle(
                        rings[layer, segment],
                        rings[layer + 1, next],
                        rings[layer, next],
                        vertices,
                        normals,
                        triangles);
                    AddTriangle(
                        rings[layer, segment],
                        rings[layer + 1, segment],
                        rings[layer + 1, next],
                        vertices,
                        normals,
                        triangles);
                }
            }

            var topLayer = layers - 1;
            var bottomCenter = AverageRing(rings, 0, segments);
            var topCenter = AverageRing(rings, topLayer, segments);
            for (var segment = 0; segment < segments; segment++)
            {
                var next = (segment + 1) % segments;
                AddTriangle(
                    bottomCenter,
                    rings[0, segment],
                    rings[0, next],
                    vertices,
                    normals,
                    triangles);
                AddTriangle(
                    topCenter,
                    rings[topLayer, next],
                    rings[topLayer, segment],
                    vertices,
                    normals,
                    triangles);
            }

            var mesh = new Mesh
            {
                name = $"BrokenWorld_{shape}_{variant:00}_LOD{lod}"
            };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0, true);
            mesh.RecalculateBounds();
            return mesh;
        }

        internal static string GetStableId(TopDown3DNaturalObjectShape shape, int variant)
        {
            return $"broken-world-{shape.ToString().ToLowerInvariant()}-{TopDown3DNaturalObjectCatalog.NormalizeMeshVariant(variant):00}";
        }

        private static BakedMeshSet BakeFamilyAsset(TopDown3DNaturalObjectShape shape, int variant)
        {
            var basePath = $"{OutputFolder}/BrokenWorld_{shape}_{variant:00}";
            var lod0 = UpsertMesh(basePath + "_LOD0.asset", BuildMesh(shape, variant, 0));
            var lod1 = UpsertMesh(basePath + "_LOD1.asset", BuildMesh(shape, variant, 1));
            var lod2 = UpsertMesh(basePath + "_LOD2.asset", BuildMesh(shape, variant, 2));
            return new BakedMeshSet(lod0, lod1, lod2);
        }

        private static Mesh UpsertMesh(string assetPath, Mesh generated)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (var i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Mesh existing && existing.name == generated.name)
                {
                    EditorUtility.CopySerialized(generated, existing);
                    existing.name = generated.name;
                    EditorUtility.SetDirty(existing);
                    UnityEngine.Object.DestroyImmediate(generated);
                    return existing;
                }
            }

            if (assets.Length == 0)
            {
                AssetDatabase.CreateAsset(generated, assetPath);
            }
            else
            {
                AssetDatabase.AddObjectToAsset(generated, assetPath);
            }

            return generated;
        }

        private static Vector3 AverageRing(Vector3[,] rings, int layer, int segments)
        {
            var total = Vector3.zero;
            for (var segment = 0; segment < segments; segment++)
            {
                total += rings[layer, segment];
            }

            return total / segments;
        }

        private static void AddTriangle(
            Vector3 a,
            Vector3 b,
            Vector3 c,
            ICollection<Vector3> vertices,
            ICollection<Vector3> normals,
            ICollection<int> triangles)
        {
            var normal = Vector3.Cross(b - a, c - a).normalized;
            var index = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);
            triangles.Add(index);
            triangles.Add(index + 1);
            triangles.Add(index + 2);
        }

        private static RockProfile GetProfile(TopDown3DNaturalObjectShape shape)
        {
            switch (shape)
            {
                case TopDown3DNaturalObjectShape.Pebble:
                    return new RockProfile(10, 4, 0.68f, 0.56f, 0.5f, 0.58f, 1.25f, 0.1f, 0.08f, 0.07f, 0.38f);
                case TopDown3DNaturalObjectShape.Shard:
                    return new RockProfile(8, 5, 0.5f, 0.37f, 1.2f, 0.22f, 1.1f, 0.06f, 0.03f, 0.14f, 0.5f);
                case TopDown3DNaturalObjectShape.Slab:
                    return new RockProfile(10, 4, 1.05f, 0.68f, 0.4f, 0.74f, 1.45f, 0.08f, 0.04f, 0.06f, 0.42f);
                case TopDown3DNaturalObjectShape.Nodule:
                    return new RockProfile(11, 5, 0.72f, 0.68f, 0.78f, 0.5f, 1.35f, 0.14f, 0.07f, 0.08f, 0.34f);
                case TopDown3DNaturalObjectShape.Outcrop:
                    return new RockProfile(12, 6, 1.45f, 1.05f, 1.2f, 0.48f, 1.2f, 0.16f, 0.08f, 0.16f, 0.48f);
                case TopDown3DNaturalObjectShape.Cliff:
                    return new RockProfile(12, 6, 1.85f, 0.62f, 1.75f, 0.66f, 1.65f, 0.12f, 0.05f, 0.2f, 0.56f);
                case TopDown3DNaturalObjectShape.Talus:
                    return new RockProfile(12, 4, 1.4f, 1.05f, 0.42f, 0.7f, 1.5f, 0.2f, 0.12f, 0.06f, 0.4f);
                case TopDown3DNaturalObjectShape.HeroSpire:
                    return new RockProfile(12, 7, 1.05f, 0.78f, 2.8f, 0.16f, 1.08f, 0.08f, 0.03f, 0.32f, 0.58f);
                default:
                    return new RockProfile(11, 5, 0.92f, 0.82f, 1.05f, 0.42f, 1.3f, 0.16f, 0.08f, 0.12f, 0.44f);
            }
        }

        private static float Hash01(int a, int b, int c, int d)
        {
            unchecked
            {
                var hash = (uint)a * 0x9E3779B9u;
                hash ^= (uint)b * 0x85EBCA6Bu;
                hash ^= (uint)c * 0xC2B2AE35u;
                hash ^= (uint)d * 0x27D4EB2Fu;
                hash ^= hash >> 16;
                hash *= 0x7FEB352Du;
                hash ^= hash >> 15;
                return (hash & 0x00FFFFFFu) / 16777215f;
            }
        }

        private static void EnsureFolder(string path)
        {
            var segments = path.Split('/');
            var current = segments[0];
            for (var i = 1; i < segments.Length; i++)
            {
                var next = current + "/" + segments[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[i]);
                }

                current = next;
            }
        }

        private readonly struct BakedMeshSet
        {
            internal BakedMeshSet(Mesh lod0, Mesh lod1, Mesh lod2)
            {
                Lod0 = lod0;
                Lod1 = lod1;
                Lod2 = lod2;
            }

            internal Mesh Lod0 { get; }
            internal Mesh Lod1 { get; }
            internal Mesh Lod2 { get; }
        }

        private readonly struct RockProfile
        {
            internal RockProfile(
                int baseSegments,
                int baseLayers,
                float radiusX,
                float radiusZ,
                float height,
                float topScale,
                float taperPower,
                float shoulderBulge,
                float baseUndercut,
                float drift,
                float fractureStrength)
            {
                BaseSegments = baseSegments;
                BaseLayers = baseLayers;
                RadiusX = radiusX;
                RadiusZ = radiusZ;
                Height = height;
                TopScale = topScale;
                TaperPower = taperPower;
                ShoulderBulge = shoulderBulge;
                BaseUndercut = baseUndercut;
                Drift = drift;
                FractureStrength = fractureStrength;
                StrataStrength = 0.055f;
            }

            internal int BaseSegments { get; }
            internal int BaseLayers { get; }
            internal float RadiusX { get; }
            internal float RadiusZ { get; }
            internal float Height { get; }
            internal float TopScale { get; }
            internal float TaperPower { get; }
            internal float ShoulderBulge { get; }
            internal float BaseUndercut { get; }
            internal float Drift { get; }
            internal float FractureStrength { get; }
            internal float StrataStrength { get; }
        }
    }
}
