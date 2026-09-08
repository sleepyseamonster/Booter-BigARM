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
