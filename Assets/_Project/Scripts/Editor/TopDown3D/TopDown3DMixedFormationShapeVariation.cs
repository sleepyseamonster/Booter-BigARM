using System;
using System.Collections.Generic;
using BooterBigArm.TopDown3D;
using UnityEngine;

namespace BooterBigArm.Editor
{
    internal static class TopDown3DMixedFormationShapeVariation
    {
        internal static void Apply(TopDown3DRockWorkbenchAuthoring rock, int seed)
        {
            var filter = rock.GetComponent<MeshFilter>();
            var targetBounds = filter.sharedMesh.bounds;
            // Use the workbench recipe/mesher, not its interactive command (which changes
            // selection, transforms and Undo). Never replace the saved source volumes.
            var temporary = new GameObject("Temporary rock shape recipe") { hideFlags = HideFlags.HideAndDontSave };
            Mesh mesh = null;
            try
            {
                var plan = TopDown3DRockWorkbenchBaseRockGenerator.CreatePlanForAuthoring(
                    rock, seed, rock.GeneratedWidth, rock.GeneratedHeight);
                var boxes = new List<TopDown3DRockWorkbenchBox>(plan.Count);
                for (var i = 0; i < plan.Count; i++)
                {
                    var spec = plan[i];
                    var volume = new GameObject("Shape volume") { hideFlags = HideFlags.HideAndDontSave };
                    volume.transform.SetParent(temporary.transform, false);
                    volume.transform.localPosition = spec.LocalPosition;
                    volume.transform.localRotation = spec.LocalRotation;
                    volume.transform.localScale = spec.LocalScale;
                    boxes.Add(new TopDown3DRockWorkbenchBox(temporary.transform, volume.transform,
                        spec.SourceShape, TopDown3DRockWorkbenchBaseRockGenerator.DeriveVolumeShapeSeed(seed, i),
                        spec.Operation, rock.GeneratedEdgeDamage));
                }
                if (!TopDown3DRockWorkbenchMesher.TryBuild(boxes, rock.VoxelSize, rock.FusionSmoothness,
                    rock.SurfaceRelaxation, out var result, out var error))
                    throw new InvalidOperationException($"Cannot regenerate {rock.name}: {error}");
                mesh = result.CreateMesh(rock.name + " — Shape Variation");
                var bounds = mesh.bounds;
                if (bounds.size.x <= 0f || bounds.size.y <= 0f || bounds.size.z <= 0f)
                    throw new InvalidOperationException("Regenerated rock has an empty size envelope.");
                var scale = new Vector3(targetBounds.size.x / bounds.size.x,
                    targetBounds.size.y / bounds.size.y, targetBounds.size.z / bounds.size.z);
                var vertices = mesh.vertices;
                for (var i = 0; i < vertices.Length; i++)
                    vertices[i] = targetBounds.center + Vector3.Scale(vertices[i] - bounds.center, scale);
                mesh.vertices = vertices;
                mesh.RecalculateNormals();
                mesh.RecalculateTangents();
                mesh.RecalculateBounds();
                mesh.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontUnloadUnusedAsset;
                // Materials, surface seed, pose and fitting are deliberately untouched.
                rock.GetComponent<MeshCollider>().sharedMesh = mesh;
                filter.sharedMesh = mesh;
                mesh = null; // The disposable context now owns it and clears it on rebuild.
            }
            finally
            {
                if (mesh != null) UnityEngine.Object.DestroyImmediate(mesh);
                UnityEngine.Object.DestroyImmediate(temporary);
            }
        }
    }
}
