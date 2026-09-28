using System;
using System.Collections.Generic;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace BooterBigArm.Editor
{
    public static class TopDown3DMixedFormationAssetBaker
    {
        internal const string SourceFolder = "Assets/_Project/Art/Environment/Rocks/Source";
        internal const string GeneratedFolder = "Assets/_Project/Art/Environment/Rocks/Generated";
        internal const string OutputPath = "Assets/_Project/Art/Environment/Rocks/Generated/MixedPileScatter.asset";
        internal const string CatalogPath =
            "Assets/_Project/Art/Environment/Rocks/Generated/AuthoredFormationCatalog.asset";

        public static void BakeDefault()
        {
            Bake(AssetDatabase.LoadAssetAtPath<GameObject>(TopDown3DLandscapeAuthoringSandboxEditor.MixedReferencePath));
        }

        public static void BakeGameplayFromCli()
        {
            BakeGameplay(AssetDatabase.LoadAssetAtPath<GameObject>(TopDown3DLandscapeAuthoringSandboxEditor.MixedReferencePath));
        }

        [MenuItem("Booter & BigARM/Bake All Authored Formation Gameplay Meshes", false, 4)]
        public static void BakeAllGameplay()
        {
            var guids = AssetDatabase.FindAssets("t:GameObject", new[] { SourceFolder });
            var sources = new List<GameObject>();
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!path.EndsWith("Reference.prefab", StringComparison.Ordinal)) continue;
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (source != null) sources.Add(source);
            }

            sources.Sort((left, right) => string.CompareOrdinal(
                AssetDatabase.GetAssetPath(left), AssetDatabase.GetAssetPath(right)));
            if (sources.Count == 0)
                throw new InvalidOperationException("No authored formation reference prefabs were found.");
            var bakedCount = 0;
            foreach (var source in sources)
            {
                var existing = AssetDatabase.LoadAssetAtPath<TopDown3DAuthoredFormationAsset>(
                    GetOutputPath(source));
                // Reuse only a complete bake of the current saved reference. A changed
                // prefab must not silently leave its previous gameplay meshes in the world.
                var sourceRevision = AssetDatabase.GetAssetDependencyHash(
                    AssetDatabase.GetAssetPath(source)).ToString();
                var loadedStageMatches = true;
                TopDown3DLandscapeAuthoringSandbox loadedStage = null;
                foreach (var stage in Resources.FindObjectsOfTypeAll<TopDown3DLandscapeAuthoringSandbox>())
                {
                    if (stage == null || !stage.gameObject.scene.isLoaded || stage.RockReference != source) continue;
                    loadedStage = stage;
                    loadedStageMatches = existing != null
                        && existing.ApprovedStageVariationEnabled == stage.VariationEnabled
                        && existing.ApprovedStageSeed == stage.VariationSeed;
                    break;
                }
                if (existing != null && existing.HasBakedVariants && existing.HasApprovedStage
                    && existing.SourceRevision == sourceRevision && loadedStageMatches)
                {
                    if (loadedStage != null && (existing.SurfaceTreatment.ShallowBurial != loadedStage.RockBurial
                        || existing.SurfaceTreatment.DeepBurial != loadedStage.MaximumRockBurial
                        || existing.SurfaceTreatment.MaximumGroundTilt != loadedStage.MaximumRockTilt))
                    {
                        existing.SurfaceTreatment.SetGroundFit(loadedStage.RockBurial,
                            loadedStage.MaximumRockBurial, loadedStage.MaximumRockTilt);
                        EditorUtility.SetDirty(existing);
                        AssetDatabase.SaveAssetIfDirty(existing);
                    }
                    continue;
                }
                BakeGameplay(source);
                bakedCount++;
            }
            UpdateCatalog();
            AssetDatabase.SaveAssets();
            Debug.Log($"Gameplay-baked {bakedCount} missing, incomplete, or stale authored formation templates.");
        }

        internal static void BakeGameplay(GameObject source)
        {
            // Capture the current saved reference first. This invalidates any previous bake
            // until every member is complete; no runtime catalog/world settings are modified.
            Bake(source);
            var outputPath = GetOutputPath(source);
            var asset = AssetDatabase.LoadAssetAtPath<TopDown3DAuthoredFormationAsset>(outputPath);
            foreach (var stage in Resources.FindObjectsOfTypeAll<TopDown3DLandscapeAuthoringSandbox>())
            {
                if (stage == null || !stage.gameObject.scene.isLoaded || stage.RockReference != source) continue;
                asset.SetApprovedStageConfiguration(stage.VariationEnabled, stage.VariationSeed);
                asset.SurfaceTreatment.SetGroundFit(
                    stage.RockBurial, stage.MaximumRockBurial, stage.MaximumRockTilt);
                break;
            }
            var sourceRocks = source.GetComponentsInChildren<TopDown3DRockWorkbenchAuthoring>(true);
            var byId = new Dictionary<string, int>();
            for (var i = 0; i < sourceRocks.Length; i++)
            {
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sourceRocks[i], out string guid, out long localId);
                byId.Add(guid + ":" + localId, i);
            }
            long[] triangles = new long[3];
            foreach (var member in asset.Members)
            {
                var index = byId[member.SourceId];
                var rock = sourceRocks[index];
                var variants = new TopDown3DNaturalMeshFamily[TopDown3DNaturalObjectCatalog.MeshVariantsPerShape];
                var path = GetGameplayMeshPath(source, member.SourceId);
                if (AssetDatabase.LoadMainAssetAtPath(path) != null
                    && !(AssetDatabase.LoadMainAssetAtPath(path) is Mesh))
                    throw new InvalidOperationException("Gameplay mesh path is occupied by a different asset: " + path);
                for (var variant = 0; variant < variants.Length; variant++)
                {
                    // Same shape seeds as authoring variations 1..3; LODs share that seed.
                    var seed = unchecked(rock.GenerationSeed ^ (variant + 1) * 486187739 ^ index * 16777619);
                    var lods = new Mesh[3];
                    for (var lod = 0; lod < lods.Length; lod++)
                    {
                        var mesh = TopDown3DMixedFormationShapeVariation.BuildMesh(rock, seed,
                            member.Mesh.bounds, 1 << lod);
                        // Unity names a main asset after its file on import. Match that name
                        // initially so subsequent upserts reuse it rather than add a duplicate.
                        mesh.name = variant == 0 && lod == 0
                            ? System.IO.Path.GetFileNameWithoutExtension(path)
                            : $"Mixed {member.SourceId} Variant {variant} LOD{lod}";
                        mesh.hideFlags = HideFlags.None;
                        lods[lod] = TopDown3DProductionRockBaker.UpsertMesh(path, mesh);
                        triangles[lod] += lods[lod].triangles.Length / 3;
                    }
                    if (lods[1].triangles.Length >= lods[0].triangles.Length
                        || lods[2].triangles.Length >= lods[1].triangles.Length)
                        throw new InvalidOperationException("LOD triangle counts must decrease: " + member.SourceId);
                    var family = new TopDown3DNaturalMeshFamily();
                    family.Configure(member.SourceId + ":shape:" + variant,
                        TopDown3DNaturalObjectShape.Boulder, variant, lods[0], lods[1], lods[2],
                        member.Mesh.bounds, 0.24f, 0.085f, 0.015f);
                    variants[variant] = family;
                }
                member.SetBakedVariants(variants);
                // Retire only the unreferenced first-bake duplicate from this new asset format.
                // All current family references above now point to the stable main mesh.
                foreach (var candidate in AssetDatabase.LoadAllAssetsAtPath(path))
                    if (candidate is Mesh duplicate && duplicate.name == $"Mixed {member.SourceId} Variant 0 LOD0")
                        UnityEngine.Object.DestroyImmediate(duplicate, true);
                Debug.Log($"Prepared mixed member {index + 1}/{sourceRocks.Length}");
            }
            if (!asset.HasBakedVariants) throw new InvalidOperationException("Mixed gameplay mesh library is incomplete.");
            BakeApprovedStage(source, asset, sourceRocks, byId);
            if (!asset.HasApprovedStage)
                throw new InvalidOperationException("The approved workbench stage needs a complete gameplay bake.");
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
            foreach (var member in asset.Members)
                foreach (var variant in member.BakedVariants)
                    for (var lod = 0; lod < 3; lod++) AssetDatabase.SaveAssetIfDirty(variant.GetLod(lod));
            foreach (var entry in asset.ApprovedStageEntries)
                for (var lod = 0; lod < 3; lod++) AssetDatabase.SaveAssetIfDirty(entry.Family.GetLod(lod));
            foreach (var generation in asset.ProceduralGenerations)
                foreach (var entry in generation.Entries)
                    for (var lod = 0; lod < 3; lod++) AssetDatabase.SaveAssetIfDirty(entry.Family.GetLod(lod));
            UpdateCatalog();
            Debug.Log($"Mixed gameplay bake complete: {asset.Members.Count} members, 3 shapes each, 3 LODs. "
                + $"Library triangles LOD0/1/2: {triangles[0]}/{triangles[1]}/{triangles[2]}. World placement remains unchanged.");
        }

        private static void BakeApprovedStage(GameObject source, TopDown3DAuthoredFormationAsset asset,
            TopDown3DRockWorkbenchAuthoring[] sourceRocks, Dictionary<string, int> byId)
        {
            asset.SetApprovedStage(BuildStage(source, asset, sourceRocks, byId,
                asset.ApprovedStageSeed, asset.ApprovedStageVariationEnabled, "Approved"));
            var generations = new TopDown3DAuthoredFormationAsset.BakedGeneration[3];
            var accepted = 0;
            for (var seed = 1; seed <= 64 && accepted < generations.Length; seed++)
            {
                if (asset.ApprovedStageVariationEnabled && seed == asset.ApprovedStageSeed) continue;
                if (!CanBuildStage(asset, sourceRocks, byId, seed)) continue;
                generations[accepted++] = new TopDown3DAuthoredFormationAsset.BakedGeneration(seed,
                    BuildStage(source, asset, sourceRocks, byId, seed, true, "Variation" + seed));
            }
            if (accepted < generations.Length)
                throw new InvalidOperationException("The workbench recipe has fewer than three bakeable procedural generations.");
            asset.SetProceduralGenerations(generations);
        }

        private static bool CanBuildStage(TopDown3DAuthoredFormationAsset asset,
            TopDown3DRockWorkbenchAuthoring[] sourceRocks, Dictionary<string, int> byId, int seed)
        {
            var layout = TopDown3DAuthoredFormationVariation.GenerateLayout(asset, seed);
            for (var i = 0; i < layout.Entries.Count; i++)
            {
                var member = asset.Members[layout.Entries[i].SourceIndex];
                var rock = sourceRocks[byId[member.SourceId]];
                var shapeSeed = unchecked(rock.GenerationSeed ^ seed * 486187739 ^ i * 16777619);
                for (var lod = 0; lod < 3; lod++)
                {
                    Mesh mesh = null;
                    try { mesh = TopDown3DMixedFormationShapeVariation.BuildMesh(rock,
                        shapeSeed, member.Mesh.bounds, 1 << lod); }
                    catch (InvalidOperationException error)
                    {
                        if (!error.Message.Contains("scalar field did not produce a surface")) throw;
                        Debug.LogWarning($"Skipping unbakeable workbench generation {seed}: {error.Message}");
                        return false;
                    }
                    finally { if (mesh != null) UnityEngine.Object.DestroyImmediate(mesh); }
                }
            }
            return true;
        }

        private static TopDown3DAuthoredFormationAsset.ApprovedStageEntry[] BuildStage(
            GameObject source, TopDown3DAuthoredFormationAsset asset,
            TopDown3DRockWorkbenchAuthoring[] sourceRocks, Dictionary<string, int> byId,
            int stageSeed, bool variationEnabled, string key)
        {
            var layout = variationEnabled
                ? TopDown3DAuthoredFormationVariation.GenerateLayout(asset, stageSeed) : null;
            var count = layout != null ? layout.Entries.Count : asset.Members.Count;
            var entries = new List<TopDown3DAuthoredFormationAsset.ApprovedStageEntry>(count);
            var displayedBounds = new Bounds[count];
            for (var i = 0; i < count; i++)
            {
                var sourceIndex = layout != null ? layout.Entries[i].SourceIndex : i;
                var member = asset.Members[sourceIndex];
                var instanceId = layout != null ? layout.Entries[i].InstanceId : member.SourceId;
                var pose = layout != null ? layout.Entries[i].Transform : member.LocalPose;
                var rock = sourceRocks[byId[member.SourceId]];
                var shapeSeed = unchecked(rock.GenerationSeed ^ stageSeed * 486187739
                    ^ i * 16777619);
                var path = TopDown3DProductionRockBaker.OutputFolder + "/"
                    + System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(source))
                    + "_" + key + "_" + i + "_Meshes.asset";
                var lods = new Mesh[3];
                for (var lod = 0; lod < 3; lod++)
                {
                    if (lod == 0 && !variationEnabled)
                    {
                        // Scatter's accepted stage displays the saved source silhouette.
                        lods[lod] = member.Mesh;
                        continue;
                    }
                    var mesh = TopDown3DMixedFormationShapeVariation.BuildMesh(rock, shapeSeed,
                        member.Mesh.bounds, 1 << lod);
                    mesh.name = key + " Stage " + i + " LOD" + lod;
                    mesh.hideFlags = HideFlags.None;
                    lods[lod] = TopDown3DProductionRockBaker.UpsertMesh(path, mesh);
                }
                var family = new TopDown3DNaturalMeshFamily();
                family.Configure(instanceId + ":" + key, TopDown3DNaturalObjectShape.Boulder, 0,
                    lods[0], lods[1], lods[2], member.Mesh.bounds, 0.24f, 0.085f, 0.015f);
                displayedBounds[i] = TransformBounds(lods[0].bounds, pose);
                entries.Add(new TopDown3DAuthoredFormationAsset.ApprovedStageEntry(
                    sourceIndex, instanceId, pose, family));
            }
            if (variationEnabled
                && asset.VariationProfile == TopDown3DAuthoredFormationVariationProfile.HandbuiltSpire)
            {
                var supported = TopDown3DAuthoredFormationVariation.SupportedMask(displayedBounds);
                for (var i = supported.Length - 1; i >= 0; i--)
                    if (!supported[i]) entries.RemoveAt(i);
            }
            return entries.ToArray();
        }

        private static Bounds TransformBounds(Bounds bounds, Matrix4x4 matrix)
        {
            var output = new Bounds(matrix.MultiplyPoint3x4(bounds.center), Vector3.zero);
            for (var i = 0; i < 8; i++)
                output.Encapsulate(matrix.MultiplyPoint3x4(bounds.center + Vector3.Scale(bounds.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1))));
            return output;
        }

        internal static void Bake(GameObject source)
        {
            ValidateSource(source);
            var sourcePath = AssetDatabase.GetAssetPath(source);
            var outputPath = GetOutputPath(source);
            var rocks = source.GetComponentsInChildren<TopDown3DRockWorkbenchAuthoring>(true);
            if (rocks.Length == 0) throw new InvalidOperationException("The reference has no rocks.");
            // Validate everything before modifying the existing capture.
            foreach (var rock in rocks)
            {
                var mesh = rock.GetComponent<MeshFilter>()?.sharedMesh;
                var material = rock.GetComponent<MeshRenderer>()?.sharedMaterial;
                if (mesh == null || !EditorUtility.IsPersistent(mesh) || material == null)
                    throw new InvalidOperationException($"{rock.name} needs a saved mesh and material.");
            }
            var asset = AssetDatabase.LoadAssetAtPath<TopDown3DAuthoredFormationAsset>(outputPath);
            if (asset == null && AssetDatabase.LoadMainAssetAtPath(outputPath) != null)
                throw new InvalidOperationException("The capture path is occupied by a different asset; it will not be overwritten.");
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<TopDown3DAuthoredFormationAsset>();
                AssetDatabase.CreateAsset(asset, outputPath);
            }
            var existing = new Dictionary<string, Material>();
            foreach (var member in asset.Members) existing.Add(member.SourceId, member.Material);
            var members = new List<TopDown3DAuthoredFormationAsset.Member>();
            foreach (var rock in rocks)
            {
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(rock, out string guid, out long localId);
                var id = guid + ":" + localId;
                var renderer = rock.GetComponent<MeshRenderer>();
                if (!existing.TryGetValue(id, out var bakedMaterial))
                {
                    bakedMaterial = new Material(renderer.sharedMaterial) { name = "Rock " + localId };
                    AssetDatabase.AddObjectToAsset(bakedMaterial, asset);
                }
                bakedMaterial.shader = renderer.sharedMaterial.shader;
                bakedMaterial.CopyPropertiesFromMaterial(renderer.sharedMaterial);
                // Property blocks are not serialized: freeze the canonical surface on a disposable renderer.
                var temporary = new GameObject("Formation surface capture");
                try
                {
                    temporary.hideFlags = HideFlags.HideAndDontSave;
                    var preview = temporary.AddComponent<MeshRenderer>();
                    preview.sharedMaterial = renderer.sharedMaterial;
                    TopDown3DRockWorkbenchPreview.ApplySurfaceProperties(rock, preview,
                        rock.GetComponent<MeshFilter>().sharedMesh.bounds.size, rock.GenerationSeed, Vector3.zero, 0f, 6f);
                    TopDown3DMixedFormationClutter.ApplyFormationReadability(preview);
                    var block = new MaterialPropertyBlock();
                    preview.GetPropertyBlock(block);
                    var shader = bakedMaterial.shader;
                    for (var p = 0; p < shader.GetPropertyCount(); p++)
                    {
                        var property = shader.GetPropertyNameId(p);
                        switch (shader.GetPropertyType(p))
                        {
                            case ShaderPropertyType.Color:
                                if (block.HasColor(property)) bakedMaterial.SetColor(property, block.GetColor(property));
                                break;
                            case ShaderPropertyType.Vector:
                                if (block.HasVector(property)) bakedMaterial.SetVector(property, block.GetVector(property));
                                break;
                            case ShaderPropertyType.Float:
                            case ShaderPropertyType.Range:
                                if (block.HasFloat(property)) bakedMaterial.SetFloat(property, block.GetFloat(property));
                                break;
                        }
                    }
                }
                finally { UnityEngine.Object.DestroyImmediate(temporary); }
                EditorUtility.SetDirty(bakedMaterial);
                members.Add(new TopDown3DAuthoredFormationAsset.Member(id,
                    rock.GetComponent<MeshFilter>().sharedMesh, bakedMaterial,
                    source.transform.worldToLocalMatrix * rock.transform.localToWorldMatrix));
            }
            members.Sort((a, b) => string.CompareOrdinal(a.SourceId, b.SourceId));
            var profile = sourcePath == TopDown3DAuthoredFormationTemplateCapture.HandbuiltSpireReferencePath
                ? TopDown3DAuthoredFormationVariationProfile.HandbuiltSpire
                : TopDown3DAuthoredFormationVariationProfile.FixedCluster;
            asset.Configure(AssetDatabase.AssetPathToGUID(sourcePath),
                AssetDatabase.GetAssetDependencyHash(sourcePath).ToString(), profile, members.ToArray());
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
            foreach (var member in members) AssetDatabase.SaveAssetIfDirty(member.Material);
            UpdateCatalog();
            Debug.Log($"Captured {members.Count} authored rocks to {outputPath}. Sand and world placement are not included.");
        }

        internal static string GetOutputPath(GameObject source)
        {
            ValidateSource(source);
            var sourcePath = AssetDatabase.GetAssetPath(source);
            if (sourcePath == TopDown3DLandscapeAuthoringSandboxEditor.MixedReferencePath)
                return OutputPath;
            var name = System.IO.Path.GetFileNameWithoutExtension(sourcePath);
            if (name.EndsWith("Reference", StringComparison.Ordinal))
                name = name.Substring(0, name.Length - "Reference".Length);
            return GeneratedFolder + "/" + name + ".asset";
        }

        private static string GetGameplayMeshPath(GameObject source, string sourceId)
        {
            if (AssetDatabase.GetAssetPath(source)
                == TopDown3DLandscapeAuthoringSandboxEditor.MixedReferencePath)
            {
                return TopDown3DProductionRockBaker.OutputFolder + "/Mixed_"
                    + sourceId.Replace(':', '_') + "_Meshes.asset";
            }
            var sourceName = System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(source));
            if (sourceName.EndsWith("Reference", StringComparison.Ordinal))
                sourceName = sourceName.Substring(0, sourceName.Length - "Reference".Length);
            return TopDown3DProductionRockBaker.OutputFolder + "/" + sourceName + "_"
                + sourceId.Replace(':', '_') + "_Meshes.asset";
        }

        private static void ValidateSource(GameObject source)
        {
            if (source == null) throw new InvalidOperationException("Capture requires a saved formation prefab.");
            var sourcePath = AssetDatabase.GetAssetPath(source);
            if (string.IsNullOrWhiteSpace(sourcePath)
                || !sourcePath.StartsWith(SourceFolder + "/", StringComparison.Ordinal)
                || !sourcePath.EndsWith("Reference.prefab", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Formation sources must be saved as *Reference.prefab under {SourceFolder}.");
            }
        }

        private static void UpdateCatalog()
        {
            var templates = new List<TopDown3DAuthoredFormationAsset>();
            var guids = AssetDatabase.FindAssets("t:TopDown3DAuthoredFormationAsset",
                new[] { GeneratedFolder });
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var template = AssetDatabase.LoadAssetAtPath<TopDown3DAuthoredFormationAsset>(path);
                if (template != null && template.HasBakedVariants && template.HasApprovedStage
                    && !string.IsNullOrWhiteSpace(template.SourceGuid)) templates.Add(template);
            }
            templates.Sort((left, right) => string.CompareOrdinal(left.SourceGuid, right.SourceGuid));
            var catalog = AssetDatabase.LoadAssetAtPath<TopDown3DAuthoredFormationCatalog>(CatalogPath);
            if (catalog == null && AssetDatabase.LoadMainAssetAtPath(CatalogPath) != null)
                throw new InvalidOperationException("The formation catalog path is occupied by a different asset.");
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<TopDown3DAuthoredFormationCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            catalog.Configure(templates.ToArray());
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
        }
    }
}
