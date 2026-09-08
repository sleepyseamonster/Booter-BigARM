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
        internal const string OutputPath = "Assets/_Project/Art/Environment/Rocks/Generated/MixedPileScatter.asset";

        public static void BakeDefault()
        {
            Bake(AssetDatabase.LoadAssetAtPath<GameObject>(TopDown3DLandscapeAuthoringSandboxEditor.MixedReferencePath));
        }

        public static void BakeGameplayFromCli()
        {
            BakeGameplay(AssetDatabase.LoadAssetAtPath<GameObject>(TopDown3DLandscapeAuthoringSandboxEditor.MixedReferencePath));
        }

        internal static void BakeGameplay(GameObject source)
        {
            // Capture the current saved reference first. This invalidates any previous bake
            // until every member is complete; no runtime catalog/world settings are modified.
            Bake(source);
            var asset = AssetDatabase.LoadAssetAtPath<TopDown3DAuthoredFormationAsset>(OutputPath);
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
                var path = TopDown3DProductionRockBaker.OutputFolder + "/Mixed_"
                    + member.SourceId.Replace(':', '_') + "_Meshes.asset";
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
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
            foreach (var member in asset.Members)
                foreach (var variant in member.BakedVariants)
                    for (var lod = 0; lod < 3; lod++) AssetDatabase.SaveAssetIfDirty(variant.GetLod(lod));
            Debug.Log($"Mixed gameplay bake complete: {asset.Members.Count} members, 3 shapes each, 3 LODs. "
                + $"Library triangles LOD0/1/2: {triangles[0]}/{triangles[1]}/{triangles[2]}. World placement remains unchanged.");
        }

        internal static void Bake(GameObject source)
        {
            if (source == null || AssetDatabase.GetAssetPath(source)
                != TopDown3DLandscapeAuthoringSandboxEditor.MixedReferencePath)
                throw new InvalidOperationException("Capture requires the saved mixed formation reference.");
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
            var asset = AssetDatabase.LoadAssetAtPath<TopDown3DAuthoredFormationAsset>(OutputPath);
            if (asset == null && AssetDatabase.LoadMainAssetAtPath(OutputPath) != null)
                throw new InvalidOperationException("The capture path is occupied by a different asset; it will not be overwritten.");
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<TopDown3DAuthoredFormationAsset>();
                AssetDatabase.CreateAsset(asset, OutputPath);
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
            asset.Configure(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source)),
                AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(source)).ToString(), members.ToArray());
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
            foreach (var member in members) AssetDatabase.SaveAssetIfDirty(member.Material);
            Debug.Log($"Captured {members.Count} authored rocks to {OutputPath}. Sand and world placement are not included.");
        }
    }
}
