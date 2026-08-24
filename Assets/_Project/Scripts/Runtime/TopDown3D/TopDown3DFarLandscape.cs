using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

namespace BooterBigArm.TopDown3D
{
    /// <summary>
    /// Coarse, non-colliding middle and far terrain derived from the canonical world generator.
    /// Rebuilds only when the streaming target crosses a stable regional anchor.
    /// </summary>
    internal sealed class TopDown3DFarLandscape
    {
        private static readonly ProfilerMarker RebuildMarker =
            new ProfilerMarker("TopDown3D.World.RebuildFarLandscape");

        private readonly Transform parent;
        private readonly TopDown3DWorldSettings settings;
        private readonly TopDown3DWorldGenerator generator;
        private readonly Material material;

        private GameObject root;
        private Vector2Int anchorCell = new Vector2Int(int.MinValue, int.MinValue);

        internal TopDown3DFarLandscape(
            Transform parent,
            TopDown3DWorldSettings settings,
            TopDown3DWorldGenerator generator,
            Material material)
        {
            this.parent = parent;
            this.settings = settings;
            this.generator = generator;
            this.material = material;
        }

        internal void Refresh(Vector3 targetPosition, bool force)
        {
            if (parent == null || settings == null || generator == null || material == null)
            {
                return;
            }

            var anchorStep = Mathf.Max(settings.ChunkSize * 4f, generator.RegionSize * 0.25f);
            var nextCell = new Vector2Int(
                Mathf.FloorToInt(targetPosition.x / anchorStep),
                Mathf.FloorToInt(targetPosition.z / anchorStep));
            if (!force && nextCell == anchorCell)
            {
                return;
            }

            anchorCell = nextCell;
            var anchor = new Vector2(
                (nextCell.x + 0.5f) * anchorStep,
                (nextCell.y + 0.5f) * anchorStep);
            Rebuild(anchor);
        }

        internal void Dispose()
        {
            DestroyRoot();
        }

        private void Rebuild(Vector2 anchor)
        {
            using (RebuildMarker.Auto())
            {
                DestroyRoot();
                root = new GameObject("Distant Geological Landscape");
                root.transform.SetParent(parent, false);
                root.transform.localPosition = new Vector3(anchor.x, 0f, anchor.y);

                var nearExtent = (settings.StreamingRadius + 1f) * settings.ChunkSize;
                CreateRing(
                    "Middle Landscape Ring",
                    anchor,
                    nearExtent,
                    360f,
                    80);
                CreateRing(
                    "Far Landscape Ring",
                    anchor,
                    360f,
                    576f,
                    48);
            }
        }

        private void CreateRing(
            string name,
            Vector2 anchor,
            float innerHalfExtent,
            float outerHalfExtent,
            int quadsPerAxis)
        {
            var verticesPerAxis = quadsPerAxis + 1;
            var vertexCount = verticesPerAxis * verticesPerAxis;
            var vertices = new Vector3[vertexCount];
            var normals = new Vector3[vertexCount];
            var colors = new Color[vertexCount];
            var uvs = new Vector2[vertexCount];
            var step = outerHalfExtent * 2f / quadsPerAxis;
            for (var z = 0; z < verticesPerAxis; z++)
            {
                for (var x = 0; x < verticesPerAxis; x++)
                {
                    var index = z * verticesPerAxis + x;
                    var localX = -outerHalfExtent + x * step;
                    var localZ = -outerHalfExtent + z * step;
                    var worldX = anchor.x + localX;
                    var worldZ = anchor.y + localZ;
                    var surface = generator.Sample(worldX, worldZ);
                    vertices[index] = new Vector3(localX, surface.Height, localZ);
                    normals[index] = surface.Normal;
                    colors[index] = surface.ToVertexColor();
                    uvs[index] = new Vector2(worldX / settings.ChunkSize, worldZ / settings.ChunkSize);
                }
            }

            var triangles = new int[quadsPerAxis * quadsPerAxis * 6];
            var triangleIndex = 0;
            for (var z = 0; z < quadsPerAxis; z++)
            {
                for (var x = 0; x < quadsPerAxis; x++)
                {
                    var centerX = -outerHalfExtent + (x + 0.5f) * step;
                    var centerZ = -outerHalfExtent + (z + 0.5f) * step;
                    if (Mathf.Abs(centerX) < innerHalfExtent
                        && Mathf.Abs(centerZ) < innerHalfExtent)
                    {
                        continue;
                    }

                    var bottomLeft = z * verticesPerAxis + x;
                    var topLeft = bottomLeft + verticesPerAxis;
                    triangles[triangleIndex++] = bottomLeft;
                    triangles[triangleIndex++] = topLeft;
                    triangles[triangleIndex++] = bottomLeft + 1;
                    triangles[triangleIndex++] = bottomLeft + 1;
                    triangles[triangleIndex++] = topLeft;
                    triangles[triangleIndex++] = topLeft + 1;
                }
            }

            if (triangleIndex != triangles.Length)
            {
                System.Array.Resize(ref triangles, triangleIndex);
            }

            var mesh = new Mesh { name = name };
            mesh.indexFormat = vertexCount > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetColors(colors);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0, true);
            mesh.RecalculateBounds();
            if (Application.isPlaying)
            {
                mesh.UploadMeshData(true);
            }

            var ringObject = new GameObject(name);
            ringObject.transform.SetParent(root.transform, false);
            ringObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = ringObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
        }

        private void DestroyRoot()
        {
            if (root == null)
            {
                return;
            }

            var filters = root.GetComponentsInChildren<MeshFilter>();
            for (var i = 0; i < filters.Length; i++)
            {
                var mesh = filters[i].sharedMesh;
                if (mesh == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Object.Destroy(mesh);
                }
                else
                {
                    Object.DestroyImmediate(mesh);
                }
            }

            if (Application.isPlaying)
            {
                Object.Destroy(root);
            }
            else
            {
                Object.DestroyImmediate(root);
            }

            root = null;
        }
    }
}
