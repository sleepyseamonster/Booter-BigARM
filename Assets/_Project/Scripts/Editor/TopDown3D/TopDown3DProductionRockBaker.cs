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
        public const string ApprovedRecipeFolder =
            "Assets/_Project/Art/Environment/Rocks/Source";
        public const string ApprovedRecipePath =
            ApprovedRecipeFolder + "/ApprovedBoulderFamilyRecipe.asset";

        private const float Lod0ScreenHeight = 0.24f;
        private const float Lod1ScreenHeight = 0.085f;
        private const float Lod2ScreenHeight = 0.015f;
        private const float ApprovedFamilyHorizontalSpan = 2f;
        private const int ApprovedFamilySeedSalt = unchecked((int)0x6A09E667);
        private static readonly float[] ApprovedFamilyVoxelSizes = { 0.045f, 0.09f, 0.18f };

        [MenuItem("Booter & BigARM/Build Approved Rocks Into World Creator", false, 2)]
        public static void BakeApprovedFromMenu()
        {
            var source = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponentInParent<TopDown3DRockWorkbenchAuthoring>()
                : null;
            var sourceDescription = source != null
                ? $"capture '{source.name}' as the approved source"
                : "use the saved approved source recipe";
            if (!EditorUtility.DisplayDialog(
                    "Build Approved Rocks Into World Creator",
                    $"This will {sourceDescription}, update only the three production Boulder mesh slots, "
                    + "and carry the approved layered rock surface into the existing production materials. "
                    + "World placement, streaming, and saved-object identity will not change.",
                    "Build Approved Rocks",
                    "Cancel"))
            {
                return;
            }

            BakeApprovedFamily(source);
        }

        public static void BakeApprovedFromCli()
        {
            BakeApprovedFamily(null);
        }

        internal static void BakeApprovedFamily(TopDown3DRockWorkbenchAuthoring source)
        {
            if (source != null && source.IsFormationMember)
            {
                throw new InvalidOperationException(
                    "Select a standalone Rock Workbench, not a member of a formation.");
            }

            var catalog = AssetDatabase.LoadAssetAtPath<TopDown3DNaturalObjectCatalog>(
                TopDown3DPrototypeBuilder.NaturalObjectCatalogPath);
            if (catalog == null)
            {
                throw new InvalidOperationException(
                    $"Missing natural-object catalog at {TopDown3DPrototypeBuilder.NaturalObjectCatalogPath}.");
            }

            var recipe = LoadOrCreateApprovedRecipe(source);
            ValidateApprovedRecipe(recipe);
            EnsureFolder(OutputFolder);

            var bakedCount = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                for (var variant = 0;
                     variant < TopDown3DNaturalObjectCatalog.MeshVariantsPerShape;
                     variant++)
                {
                    var recipeVariant = recipe.Variants[variant];
                    var meshes = BakeApprovedFamilyAsset(recipe, recipeVariant, variant);
                    var colliderBounds = meshes.Lod0.bounds;
                    colliderBounds.size = Vector3.Scale(
                        colliderBounds.size,
                        new Vector3(0.9f, 0.94f, 0.9f));
                    var family = new TopDown3DNaturalMeshFamily();
                    family.Configure(
                        GetStableId(TopDown3DNaturalObjectShape.Boulder, variant),
                        TopDown3DNaturalObjectShape.Boulder,
                        variant,
                        meshes.Lod0,
                        meshes.Lod1,
                        meshes.Lod2,
                        colliderBounds,
                        Lod0ScreenHeight,
                        Lod1ScreenHeight,
                        Lod2ScreenHeight);
                    catalog.ReplaceBakedMeshFamily(family);
                    bakedCount++;
                }

                SynchronizeApprovedProductionMaterials(recipe);
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
                    "Approved Boulder family bake failed validation:\n- "
                    + string.Join("\n- ", errors));
            }

            Debug.Log(
                $"Built {bakedCount} approved Boulder variants from {ApprovedRecipePath} into the existing "
                + "World Creator catalog. The other 24 production mesh slots were preserved.");
        }

        [MenuItem("Booter & BigARM/Top Down 3D/Advanced/Rebuild Legacy Production Rock Catalog")]
        public static void BakeFromMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Rebuild Legacy Production Rock Catalog",
                    "This advanced command rebuilds all 27 legacy mesh slots and replaces the catalog. "
                    + "Use 'Build Approved Rocks Into World Creator' for the current visual workflow.",
                    "Rebuild All 27",
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

        private static TopDown3DApprovedRockFamilyRecipe LoadOrCreateApprovedRecipe(
            TopDown3DRockWorkbenchAuthoring source)
        {
            var suppliedSource = source != null;
            EnsureFolder(ApprovedRecipeFolder);
            var recipe = AssetDatabase.LoadAssetAtPath<TopDown3DApprovedRockFamilyRecipe>(
                ApprovedRecipePath);
            var created = recipe == null;
            if (created)
            {
                recipe = ScriptableObject.CreateInstance<TopDown3DApprovedRockFamilyRecipe>();
                recipe.name = "ApprovedBoulderFamilyRecipe";
            }

            GameObject temporarySource = null;
            try
            {
                if (source == null && created)
                {
                    temporarySource = new GameObject("Approved Golden Rock Source");
                    source = temporarySource.AddComponent<TopDown3DRockWorkbenchAuthoring>();
                    source.Configure(AssetDatabase.LoadAssetAtPath<Material>(
                        TopDown3DRockWorkbenchAuthoringEditor.WorkbenchMaterialPath));
                    source.ConfigureGoldenRockDefaults();
                }

                if (source != null)
                {
                    recipe.Configure(
                        source,
                        CreateApprovedRecipeVariants(source, suppliedSource));
                    EditorUtility.SetDirty(recipe);
                }

                if (created)
                {
                    AssetDatabase.CreateAsset(recipe, ApprovedRecipePath);
                }

                return recipe;
            }
            finally
            {
                if (temporarySource != null)
                {
                    UnityEngine.Object.DestroyImmediate(temporarySource);
                }
            }
        }

        private static List<TopDown3DApprovedRockRecipeVariant> CreateApprovedRecipeVariants(
            TopDown3DRockWorkbenchAuthoring source,
            bool captureSourceVolumes)
        {
            var variants = new List<TopDown3DApprovedRockRecipeVariant>(
                TopDown3DNaturalObjectCatalog.MeshVariantsPerShape);
            for (var variant = 0;
                 variant < TopDown3DNaturalObjectCatalog.MeshVariantsPerShape;
                 variant++)
            {
                var seed = variant == 0
                    ? source.GenerationSeed
                    : TopDown3DRockWorkbenchBaseRockGenerator.DeriveVolumeShapeSeed(
                        source.GenerationSeed ^ ApprovedFamilySeedSalt,
                        variant - 1);
                var dimensions = variant == 0
                    ? new Vector2(source.GeneratedWidth, source.GeneratedHeight)
                    : TopDown3DRockWorkbenchBaseRockGenerator.CreateBellCurvedStandaloneDimensions(seed);
                var volumes = variant == 0 && captureSourceVolumes
                    ? CaptureRecipeVolumes(source)
                    : CreateRecipeVolumesFromPlan(
                        TopDown3DRockWorkbenchBaseRockGenerator.CreatePlanForAuthoring(
                            source,
                            seed,
                            dimensions.x,
                            dimensions.y),
                        seed);
                if (volumes.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"Approved Boulder recipe variant {variant} has no source volumes.");
                }

                var recipeVariant = new TopDown3DApprovedRockRecipeVariant();
                recipeVariant.Configure(
                    variant,
                    seed,
                    dimensions.x,
                    dimensions.y,
                    volumes);
                variants.Add(recipeVariant);
            }

            return variants;
        }

        private static List<TopDown3DApprovedRockRecipeVolume> CaptureRecipeVolumes(
            TopDown3DRockWorkbenchAuthoring source)
        {
            var nodes = source.GetComponentsInChildren<TopDown3DRockVolumeNode>(true);
            var volumes = new List<TopDown3DApprovedRockRecipeVolume>(nodes.Length);
            var rootWorldToLocal = source.transform.worldToLocalMatrix;
            for (var index = 0; index < nodes.Length; index++)
            {
                var node = nodes[index];
                if (node == null || !node.ContributesToRock) continue;

                var matrix = rootWorldToLocal * node.transform.localToWorldMatrix;
                var right = matrix.MultiplyVector(Vector3.right);
                var up = matrix.MultiplyVector(Vector3.up);
                var forward = matrix.MultiplyVector(Vector3.forward);
                var scale = new Vector3(right.magnitude, up.magnitude, forward.magnitude);
                var rotation = Quaternion.LookRotation(
                    forward / Mathf.Max(0.0001f, scale.z),
                    up / Mathf.Max(0.0001f, scale.y));
                var volume = new TopDown3DApprovedRockRecipeVolume();
                volume.Configure(
                    node.name,
                    InferRecipeRole(node),
                    matrix.MultiplyPoint3x4(Vector3.zero),
                    rotation,
                    scale,
                    node.SourceShape,
                    node.Operation,
                    node.ShapeSeed);
                volumes.Add(volume);
            }

            return volumes;
        }

        private static List<TopDown3DApprovedRockRecipeVolume> CreateRecipeVolumesFromPlan(
            IReadOnlyList<TopDown3DRockWorkbenchVolumeSpec> plan,
            int seed)
        {
            var volumes = new List<TopDown3DApprovedRockRecipeVolume>(plan.Count);
            for (var index = 0; index < plan.Count; index++)
            {
                var spec = plan[index];
                var volume = new TopDown3DApprovedRockRecipeVolume();
                volume.Configure(
                    CreateRecipeVolumeName(spec, index),
                    ConvertRecipeRole(spec.Role),
                    spec.LocalPosition,
                    spec.LocalRotation,
                    spec.LocalScale,
                    spec.SourceShape,
                    spec.Operation,
                    TopDown3DRockWorkbenchBaseRockGenerator.DeriveVolumeShapeSeed(seed, index));
                volumes.Add(volume);
            }

            return volumes;
        }

        private static TopDown3DApprovedRockMassRole InferRecipeRole(
            TopDown3DRockVolumeNode node)
        {
            if (node.name.IndexOf("Core", StringComparison.OrdinalIgnoreCase) >= 0)
                return TopDown3DApprovedRockMassRole.Core;
            if (node.name.IndexOf("Support", StringComparison.OrdinalIgnoreCase) >= 0)
                return TopDown3DApprovedRockMassRole.Support;
            return TopDown3DApprovedRockMassRole.Detail;
        }

        private static TopDown3DApprovedRockMassRole ConvertRecipeRole(
            TopDown3DRockWorkbenchMassRole role)
        {
            switch (role)
            {
                case TopDown3DRockWorkbenchMassRole.Core:
                    return TopDown3DApprovedRockMassRole.Core;
                case TopDown3DRockWorkbenchMassRole.Support:
                    return TopDown3DApprovedRockMassRole.Support;
                default:
                    return TopDown3DApprovedRockMassRole.Detail;
            }
        }

        private static string CreateRecipeVolumeName(
            TopDown3DRockWorkbenchVolumeSpec spec,
            int index)
        {
            if (spec.Operation == TopDown3DRockVolumeOperation.Subtractive)
                return $"Fracture Cut {index + 1}";
            switch (spec.Role)
            {
                case TopDown3DRockWorkbenchMassRole.Core:
                    return $"Core Mass {index + 1}";
                case TopDown3DRockWorkbenchMassRole.Support:
                    return $"Support Mass {index + 1}";
                default:
                    return $"Detail Mass {index + 1}";
            }
        }

        private static void ValidateApprovedRecipe(TopDown3DApprovedRockFamilyRecipe recipe)
        {
            if (recipe == null)
                throw new InvalidOperationException("The approved Boulder family recipe is missing.");
            if (recipe.SourceMaterial == null)
                throw new InvalidOperationException("The approved Boulder family recipe has no source material.");
            if (recipe.Variants.Count != TopDown3DNaturalObjectCatalog.MeshVariantsPerShape)
            {
                throw new InvalidOperationException(
                    $"The approved Boulder recipe requires exactly {TopDown3DNaturalObjectCatalog.MeshVariantsPerShape} variants.");
            }

            for (var variant = 0; variant < recipe.Variants.Count; variant++)
            {
                var entry = recipe.Variants[variant];
                if (entry == null || entry.Variant != variant || entry.Volumes.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"Approved Boulder recipe slot {variant} is incomplete or out of order.");
                }
            }
        }

        private static BakedMeshSet BakeApprovedFamilyAsset(
            TopDown3DApprovedRockFamilyRecipe recipe,
            TopDown3DApprovedRockRecipeVariant recipeVariant,
            int variant)
        {
            var generated = BuildApprovedMeshSet(recipe, recipeVariant, variant);
            var basePath = $"{OutputFolder}/BrokenWorld_Boulder_{variant:00}";
            return new BakedMeshSet(
                UpsertMesh(basePath + "_LOD0.asset", generated.Lod0),
                UpsertMesh(basePath + "_LOD1.asset", generated.Lod1),
                UpsertMesh(basePath + "_LOD2.asset", generated.Lod2));
        }

        private static BakedMeshSet BuildApprovedMeshSet(
            TopDown3DApprovedRockFamilyRecipe recipe,
            TopDown3DApprovedRockRecipeVariant recipeVariant,
            int variant)
        {
            var root = new GameObject($"Approved Boulder {variant:00} Bake Source");
            try
            {
                var sourceBounds = CalculateRecipeBounds(recipeVariant.Volumes);
                var horizontalSpan = Mathf.Max(sourceBounds.size.x, sourceBounds.size.z);
                if (horizontalSpan <= 0.001f)
                {
                    throw new InvalidOperationException(
                        $"Approved Boulder variant {variant} has invalid horizontal bounds.");
                }

                var normalizationScale = ApprovedFamilyHorizontalSpan / horizontalSpan;
                // The recipe's local y = 0 plane is the ground plane established by the
                // Workbench. Preserve it so the seeded burial depth survives production baking.
                // Only horizontal centering belongs to mesh normalization.
                var sourceOrigin = new Vector3(
                    sourceBounds.center.x,
                    0f,
                    sourceBounds.center.z);
                var boxes = new List<TopDown3DRockWorkbenchBox>(recipeVariant.Volumes.Count);
                for (var index = 0; index < recipeVariant.Volumes.Count; index++)
                {
                    var volume = recipeVariant.Volumes[index];
                    var volumeObject = new GameObject(volume.Name);
                    volumeObject.transform.SetParent(root.transform, false);
                    volumeObject.transform.localPosition =
                        (volume.LocalPosition - sourceOrigin) * normalizationScale;
                    volumeObject.transform.localRotation = volume.LocalRotation;
                    volumeObject.transform.localScale = volume.LocalScale * normalizationScale;
                    boxes.Add(new TopDown3DRockWorkbenchBox(
                        root.transform,
                        volumeObject.transform,
                        volume.SourceShape,
                        volume.ShapeSeed,
                        volume.Operation,
                        recipe.EdgeDamage));
                }

                var smoothness = recipe.FusionSmoothness * normalizationScale;
                var meshes = new Mesh[3];
                var previousTriangleCount = int.MaxValue;
                for (var lod = 0; lod < meshes.Length; lod++)
                {
                    var requestedVoxelSize = ApprovedFamilyVoxelSizes[lod];
                    for (var attempt = 0; attempt < 5; attempt++)
                    {
                        if (!TopDown3DRockWorkbenchMesher.TryBuild(
                                boxes,
                                requestedVoxelSize,
                                smoothness,
                                recipe.SurfaceRelaxation,
                                out var result,
                                out var error))
                        {
                            throw new InvalidOperationException(
                                $"Could not bake approved Boulder {variant:00} LOD{lod}: {error}");
                        }

                        var mesh = result.CreateMesh($"BrokenWorld_Boulder_{variant:00}_LOD{lod}");
                        mesh.hideFlags = HideFlags.None;
                        CenterMeshHorizontally(mesh);
                        var triangleCount = mesh.triangles.Length / 3;
                        if (triangleCount < previousTriangleCount)
                        {
                            meshes[lod] = mesh;
                            previousTriangleCount = triangleCount;
                            break;
                        }

                        UnityEngine.Object.DestroyImmediate(mesh);
                        requestedVoxelSize *= 1.45f;
                    }

                    if (meshes[lod] == null)
                    {
                        throw new InvalidOperationException(
                            $"Approved Boulder {variant:00} LOD{lod} could not reduce triangle count.");
                    }
                }

                return new BakedMeshSet(meshes[0], meshes[1], meshes[2]);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static Bounds CalculateRecipeBounds(
            IReadOnlyList<TopDown3DApprovedRockRecipeVolume> volumes)
        {
            var foundAdditive = false;
            var bounds = new Bounds();
            for (var index = 0; index < volumes.Count; index++)
            {
                var volume = volumes[index];
                if (volume.Operation == TopDown3DRockVolumeOperation.Subtractive) continue;
                var extents = CalculateRotatedExtents(volume.LocalRotation, volume.LocalScale);
                var volumeBounds = new Bounds(volume.LocalPosition, extents * 2f);
                if (!foundAdditive)
                {
                    bounds = volumeBounds;
                    foundAdditive = true;
                }
                else
                {
                    bounds.Encapsulate(volumeBounds);
                }
            }

            if (!foundAdditive)
                throw new InvalidOperationException("The approved recipe contains no additive stone volumes.");
            return bounds;
        }

        private static Vector3 CalculateRotatedExtents(Quaternion rotation, Vector3 scale)
        {
            var right = rotation * new Vector3(scale.x * 0.5f, 0f, 0f);
            var up = rotation * new Vector3(0f, scale.y * 0.5f, 0f);
            var forward = rotation * new Vector3(0f, 0f, scale.z * 0.5f);
            return new Vector3(
                Mathf.Abs(right.x) + Mathf.Abs(up.x) + Mathf.Abs(forward.x),
                Mathf.Abs(right.y) + Mathf.Abs(up.y) + Mathf.Abs(forward.y),
                Mathf.Abs(right.z) + Mathf.Abs(up.z) + Mathf.Abs(forward.z));
        }

        private static void CenterMeshHorizontally(Mesh mesh)
        {
            mesh.RecalculateBounds();
            var bounds = mesh.bounds;
            var offset = new Vector3(bounds.center.x, 0f, bounds.center.z);
            var vertices = mesh.vertices;
            for (var index = 0; index < vertices.Length; index++)
                vertices[index] -= offset;
            mesh.vertices = vertices;
            mesh.RecalculateBounds();
        }

        private static void SynchronizeApprovedProductionMaterials(
            TopDown3DApprovedRockFamilyRecipe recipe)
        {
            SynchronizeApprovedProductionMaterial(
                recipe,
                TopDown3DPrototypeBuilder.RockMaterialPath,
                TopDown3DRockSurface.Regular);
            SynchronizeApprovedProductionMaterial(
                recipe,
                TopDown3DPrototypeBuilder.DarkRockMaterialPath,
                TopDown3DRockSurface.Dark);
            SynchronizeApprovedProductionMaterial(
                recipe,
                TopDown3DPrototypeBuilder.TealRockMaterialPath,
                TopDown3DRockSurface.Teal);
        }

        internal static bool SynchronizeApprovedProductionMaterialsFromSavedRecipe()
        {
            var recipe = AssetDatabase.LoadAssetAtPath<TopDown3DApprovedRockFamilyRecipe>(
                ApprovedRecipePath);
            if (recipe == null) return false;

            ValidateApprovedRecipe(recipe);
            SynchronizeApprovedProductionMaterials(recipe);
            return true;
        }

        internal static void CollectApprovedProductionMaterialErrors(
            ICollection<string> errors)
        {
            if (errors == null) throw new ArgumentNullException(nameof(errors));

            var recipe = AssetDatabase.LoadAssetAtPath<TopDown3DApprovedRockFamilyRecipe>(
                ApprovedRecipePath);
            if (recipe == null)
            {
                errors.Add($"The approved rock recipe is missing at {ApprovedRecipePath}.");
                return;
            }
            if (recipe.SourceMaterial == null)
            {
                errors.Add("The approved rock recipe has no layered source material.");
                return;
            }

            CollectApprovedProductionMaterialErrors(
                recipe,
                TopDown3DPrototypeBuilder.RockMaterialPath,
                "regular",
                errors);
            CollectApprovedProductionMaterialErrors(
                recipe,
                TopDown3DPrototypeBuilder.DarkRockMaterialPath,
                "dark",
                errors);
            CollectApprovedProductionMaterialErrors(
                recipe,
                TopDown3DPrototypeBuilder.TealRockMaterialPath,
                "teal",
                errors);
        }

        private static void CollectApprovedProductionMaterialErrors(
            TopDown3DApprovedRockFamilyRecipe recipe,
            string materialPath,
            string surfaceName,
            ICollection<string> errors)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                errors.Add($"The production {surfaceName}-rock material is missing.");
                return;
            }
            if (material.shader != recipe.SourceMaterial.shader)
            {
                errors.Add(
                    $"The production {surfaceName}-rock material must use the approved layered Workbench shader.");
                return;
            }

            var textureProperties = new[]
            {
                "_BaseMap",
                "_NormalMap",
                "_SurfaceMap",
                "_TopBaseMap",
                "_TopNormalMap",
                "_TopSurfaceMap",
                "_CrackMap",
                "_GritBaseMap",
                "_GritNormalMap",
                "_GritSurfaceMap",
                "_BottomBaseMap",
                "_BottomNormalMap",
                "_BottomSurfaceMap"
            };
            for (var index = 0; index < textureProperties.Length; index++)
            {
                var property = textureProperties[index];
                if (material.GetTexture(property) == recipe.SourceMaterial.GetTexture(property)) continue;
                errors.Add(
                    $"The production {surfaceName}-rock material is missing approved texture layer {property}.");
            }

            if (!Mathf.Approximately(material.GetFloat("_RockMetersPerTile"), recipe.GeologyScale)
                || !Mathf.Approximately(material.GetFloat("_SurfacePatchStrength"), recipe.SurfaceVariation)
                || !Mathf.Approximately(material.GetFloat("_CrackAmount"), recipe.CrackAmount)
                || !Mathf.Approximately(material.GetFloat("_SideGritAmount"), recipe.SideGrit)
                || !Mathf.Approximately(material.GetFloat("_UndersideShaleAmount"), recipe.UndersideShale)
                || !Mathf.Approximately(material.GetFloat("_SideShalePatchAmount"), recipe.SideShalePatches)
                || !Mathf.Approximately(material.GetFloat("_TopShalePatchAmount"), recipe.TopShalePatches)
                || !Mathf.Approximately(material.GetFloat("_WornSmoothnessBoost"), recipe.WornShine))
            {
                errors.Add(
                    $"The production {surfaceName}-rock material does not match the approved Workbench surface controls.");
            }
        }

        private static void SynchronizeApprovedProductionMaterial(
            TopDown3DApprovedRockFamilyRecipe recipe,
            string materialPath,
            TopDown3DRockSurface surface)
        {
            var target = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (target == null)
                throw new InvalidOperationException($"Missing production rock material at {materialPath}.");

            var preservedName = target.name;
            EditorUtility.CopySerialized(recipe.SourceMaterial, target);
            target.name = preservedName;
            var tint = CalculateApprovedRockTint(recipe, surface);
            SetMaterialColor(target, "_BaseColor", tint);
            SetMaterialColor(target, "_Color", tint);
            SetMaterialColor(target, "_CrackColor", new Color(0.018f, 0.015f, 0.013f, 1f));
            SetMaterialColor(target, "_MineralColor", new Color(0.58f, 0.51f, 0.41f, 1f));
            SetMaterialColor(target, "_DustColor", recipe.EnvironmentDustColor);
            SetMaterialFloat(target, "_RockMetersPerTile", recipe.GeologyScale);
            SetMaterialFloat(target, "_SurfacePatchStrength", recipe.SurfaceVariation);
            SetMaterialFloat(target, "_CrackAmount", recipe.CrackAmount);
            SetMaterialFloat(target, "_SideGritAmount", recipe.SideGrit);
            SetMaterialFloat(target, "_UndersideShaleAmount", recipe.UndersideShale);
            SetMaterialFloat(target, "_SideShalePatchAmount", recipe.SideShalePatches);
            SetMaterialFloat(target, "_TopShalePatchAmount", recipe.TopShalePatches);
            SetMaterialFloat(target, "_WornSmoothnessBoost", recipe.WornShine);
            SetMaterialFloat(target, "_SmoothnessMin", 0.015f);
            SetMaterialFloat(target, "_SmoothnessMax", 0.14f);
            SetMaterialFloat(target, "_NormalStrength", 1.08f);
            SetMaterialFloat(target, "_MacroStrength", Mathf.Lerp(0.12f, 0.2f, recipe.ColorVariation));
            SetMaterialFloat(
                target,
                "_RockSeed01",
                TopDown3DRockWorkbenchPreview.SeedToUnitFloat(
                    recipe.Variants[(int)surface].GenerationSeed));
            if (target.HasProperty("_RockSize"))
                target.SetVector("_RockSize", new Vector4(2f, 1.25f, 2f, 0f));
            if (target.HasProperty("_RockOriginWS"))
                target.SetVector("_RockOriginWS", Vector4.zero);
            EditorUtility.SetDirty(target);
        }

        private static Color CalculateApprovedRockTint(
            TopDown3DApprovedRockFamilyRecipe recipe,
            TopDown3DRockSurface surface)
        {
            var seed = recipe.Variants[(int)surface].GenerationSeed;
            var cool = TopDown3DRockWorkbenchPreview.SeedToUnitFloat(
                seed ^ unchecked((int)0x3C6EF372));
            var value = TopDown3DRockWorkbenchPreview.SeedToUnitFloat(
                seed ^ unchecked((int)0xBB67AE85));
            var charcoal = Color.Lerp(
                new Color(0.42f, 0.385f, 0.34f, 1f),
                new Color(0.315f, 0.335f, 0.35f, 1f),
                cool);
            charcoal *= Mathf.Lerp(0.78f, 1.12f, value);
            charcoal = Color.Lerp(
                new Color(0.375f, 0.37f, 0.355f, 1f),
                charcoal,
                recipe.ColorVariation);
            if (surface == TopDown3DRockSurface.Dark)
                charcoal *= 0.78f;
            else if (surface == TopDown3DRockSurface.Teal)
                charcoal = Color.Lerp(charcoal, new Color(0.22f, 0.39f, 0.41f, 1f), 0.42f);
            charcoal.a = 1f;
            return charcoal;
        }

        private static void SetMaterialFloat(Material material, string property, float value)
        {
            if (material.HasProperty(property)) material.SetFloat(property, value);
        }

        private static void SetMaterialColor(Material material, string property, Color value)
        {
            if (material.HasProperty(property)) material.SetColor(property, value);
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
