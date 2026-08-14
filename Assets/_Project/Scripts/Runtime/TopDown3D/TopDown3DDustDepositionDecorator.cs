using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BooterBigArm.TopDown3D
{
    public static class TopDown3DDustDepositionDecorator
    {
        private const float MinimumVisibleWeight = 0.025f;

        public static void Decorate(
            TopDown3DGeneratedChunk chunk,
            TopDown3DWorldSettings settings,
            Material fallbackMaterial,
            Vector2 spawnExclusionCenter)
        {
            if (chunk == null
                || settings == null
                || (settings.DepositedDustMaterial == null && fallbackMaterial == null))
            {
                return;
            }

            var plan = TopDown3DDustDepositionPlanner.BuildPlan(
                settings,
                settings.NaturalObjectCatalog,
                chunk.Coordinate,
                spawnExclusionCenter);
            if (!plan.HasVisibleDeposits)
            {
                return;
            }

            var verticesPerAxis = plan.VerticesPerAxis;
            var vertices = new Vector3[verticesPerAxis * verticesPerAxis];
            var colors = new Color32[vertices.Length];
            for (var z = 0; z < verticesPerAxis; z++)
            {
                for (var x = 0; x < verticesPerAxis; x++)
                {
                    var index = z * verticesPerAxis + x;
                    var sample = plan.GetSample(x, z);
                    var worldX = chunk.Coordinate.x * settings.ChunkSize + x * plan.Step;
                    var worldZ = chunk.Coordinate.y * settings.ChunkSize + z * plan.Step;
                    vertices[index] = new Vector3(
                        x * plan.Step,
                        TopDown3DHeightSampler.SampleHeight(settings, worldX, worldZ)
                            + settings.DustSurfaceOffset
                            + sample.Height,
                        z * plan.Step);
                    colors[index] = new Color32(
                        255,
                        255,
                        255,
                        (byte)Mathf.RoundToInt(sample.Weight * 255f));
                }
            }

            var triangles = new List<int>(plan.QuadsPerAxis * plan.QuadsPerAxis * 6);
            for (var z = 0; z < plan.QuadsPerAxis; z++)
            {
                for (var x = 0; x < plan.QuadsPerAxis; x++)
                {
                    if (!CellHasVisibleDeposit(plan, x, z))
                    {
                        continue;
                    }

                    var bottomLeft = z * verticesPerAxis + x;
                    var topLeft = bottomLeft + verticesPerAxis;
                    triangles.Add(bottomLeft);
                    triangles.Add(topLeft);
                    triangles.Add(bottomLeft + 1);
                    triangles.Add(bottomLeft + 1);
                    triangles.Add(topLeft);
                    triangles.Add(topLeft + 1);
                }
            }

            if (triangles.Count == 0)
            {
                return;
            }

            var mesh = new Mesh
            {
                name = $"Chunk {chunk.Coordinate.x},{chunk.Coordinate.y} Deposited Dust"
            };
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0, true);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            chunk.RegisterGeneratedMesh(mesh);

            var dustObject = new GameObject("Wind Deposited Dust");
            dustObject.transform.SetParent(chunk.transform, false);
            dustObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = dustObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = settings.DepositedDustMaterial != null
                ? settings.DepositedDustMaterial
                : fallbackMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
        }

        private static bool CellHasVisibleDeposit(
            TopDown3DDustDepositionPlan plan,
            int x,
            int z)
        {
            return plan.GetSample(x, z).Weight >= MinimumVisibleWeight
                || plan.GetSample(x + 1, z).Weight >= MinimumVisibleWeight
                || plan.GetSample(x, z + 1).Weight >= MinimumVisibleWeight
                || plan.GetSample(x + 1, z + 1).Weight >= MinimumVisibleWeight;
        }
    }
}
