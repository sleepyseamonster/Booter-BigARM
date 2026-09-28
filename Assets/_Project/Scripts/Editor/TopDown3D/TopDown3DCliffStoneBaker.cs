using System;
using System.Collections.Generic;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Editor
{
    /// <summary>Bakes the five distinct saved cliff workbench recipes into small runtime LODs.</summary>
    public static class TopDown3DCliffStoneBaker
    {
        private const string OutputFolder = "Assets/_Project/Resources/WorldCreator/CliffStones";
        private static readonly int[] SourceSeeds = { 801855505, 839315657, 1602256044, -309069799, 117959393 };
        private static readonly string[] SourceNames = { "A", "B", "C", "D", "E" };
        private static readonly float[] VoxelSizes = { 0.075f, 0.13f, 0.21f };

        [MenuItem("Booter & BigARM/Bake Cliff Stone Runtime Meshes", false, 5)]
        public static void BakeFromCli()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TopDown3DCliffWallSourceCapture.PrefabPath);
            if (prefab == null) throw new InvalidOperationException("The saved cliff wall reference is missing.");
            if (!AssetDatabase.IsValidFolder(OutputFolder))
                AssetDatabase.CreateFolder("Assets/_Project/Resources/WorldCreator", "CliffStones");

            var copy = UnityEngine.Object.Instantiate(prefab);
            try
            {
                copy.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                copy.transform.localScale = Vector3.one;
                var bySeed = new Dictionary<int, TopDown3DRockWorkbenchAuthoring>();
                foreach (var rock in copy.GetComponentsInChildren<TopDown3DRockWorkbenchAuthoring>(true))
                    if (!bySeed.ContainsKey(rock.GenerationSeed)) bySeed.Add(rock.GenerationSeed, rock);
                for (var recipe = 0; recipe < SourceSeeds.Length; recipe++)
                {
                    if (!bySeed.TryGetValue(SourceSeeds[recipe], out var rock))
                        throw new InvalidOperationException("Missing cliff source recipe " + SourceNames[recipe]);
                    var nodes = rock.GetComponentsInChildren<TopDown3DRockVolumeNode>(false);
                    var boxes = new List<TopDown3DRockWorkbenchBox>(nodes.Length);
                    foreach (var node in nodes)
                        if (node.ContributesToRock && node.gameObject.activeInHierarchy)
                            boxes.Add(new TopDown3DRockWorkbenchBox(rock.transform, node.transform,
                                node.SourceShape, node.ShapeSeed, node.Operation, rock.GeneratedEdgeDamage));
                    if (boxes.Count != 2)
                        throw new InvalidOperationException("Cliff recipe " + SourceNames[recipe] + " must retain its two saved volumes.");
                    var previousTriangles = int.MaxValue;
                    for (var lod = 0; lod < VoxelSizes.Length; lod++)
                    {
                        if (!TopDown3DRockWorkbenchMesher.TryBuild(boxes, VoxelSizes[lod],
                                rock.FusionSmoothness, rock.SurfaceRelaxation, out var result, out var error))
                            throw new InvalidOperationException("Cliff recipe " + SourceNames[recipe] + ": " + error);
                        var name = $"CliffStone_{SourceNames[recipe]}_LOD{lod}";
                        var mesh = result.CreateMesh(name);
                        mesh.name = name;
                        mesh.hideFlags = HideFlags.None;
                        var triangles = mesh.triangles.Length / 3;
                        if (triangles >= previousTriangles)
                            throw new InvalidOperationException(name + " did not reduce triangles from the preceding LOD.");
                        previousTriangles = triangles;
                        TopDown3DProductionRockBaker.UpsertMesh(OutputFolder + "/" + name + ".asset", mesh);
                        Debug.Log($"Baked {name}: {triangles} triangles from saved source volumes.");
                    }
                }
                AssetDatabase.SaveAssets();
            }
            finally { UnityEngine.Object.DestroyImmediate(copy); }
        }
    }
}
