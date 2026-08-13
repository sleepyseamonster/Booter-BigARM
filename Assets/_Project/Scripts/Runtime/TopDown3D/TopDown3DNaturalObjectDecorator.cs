using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BooterBigArm.TopDown3D
{
    public static class TopDown3DNaturalObjectDecorator
    {
        public static void Decorate(
            TopDown3DGeneratedChunk chunk,
            TopDown3DWorldSettings settings,
            Material material,
            Vector2 spawnExclusionCenter)
        {
            if (chunk == null || settings == null || material == null || settings.NaturalObjectCatalog == null)
            {
                return;
            }

            var placements = TopDown3DNaturalObjectPlanner.BuildPlacements(
                settings,
                settings.NaturalObjectCatalog,
                chunk.Coordinate,
                spawnExclusionCenter);
            var scatter = new List<TopDown3DNaturalObjectPlacement>();
            var details = new List<TopDown3DNaturalObjectPlacement>();
            var fineGrayClusters = new List<TopDown3DNaturalObjectPlacement>();
            var obstacleIndex = 0;
            for (var i = 0; i < placements.Count; i++)
            {
                var placement = placements[i];
                switch (placement.Layer)
                {
                    case TopDown3DNaturalObjectLayer.Obstacle:
                        CreateObstacle(chunk.transform, material, placement, ++obstacleIndex);
                        break;
                    case TopDown3DNaturalObjectLayer.Scatter:
                        scatter.Add(placement);
                        break;
                    case TopDown3DNaturalObjectLayer.GroundDetail:
                        details.Add(placement);
                        break;
                    case TopDown3DNaturalObjectLayer.FineGrayCluster:
                        fineGrayClusters.Add(placement);
                        break;
                }
            }

            CreateCombinedLayer(chunk, material, scatter, "Natural Scatter", ShadowCastingMode.On);
            CreateCombinedLayer(chunk, material, details, "Ground Micro Detail", ShadowCastingMode.Off);
            CreateCombinedLayer(
                chunk,
                settings.FineGrayClutterMaterial,
                fineGrayClusters,
                "Fine Gray Ground Clusters",
                ShadowCastingMode.Off);
        }

        private static void CreateObstacle(
            Transform parent,
            Material material,
            TopDown3DNaturalObjectPlacement placement,
            int index)
        {
            var obstacle = new GameObject($"Natural Obstacle {index} - {placement.StableId}");
            obstacle.transform.SetParent(parent, true);
            obstacle.transform.SetPositionAndRotation(placement.Position, placement.Rotation);
            obstacle.transform.localScale = placement.Scale;

            var filter = obstacle.AddComponent<MeshFilter>();
            filter.sharedMesh = TopDown3DNaturalMeshLibrary.GetMesh(placement.Shape, placement.Variant);
            var renderer = obstacle.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;

            var collider = obstacle.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.43f, 0f);
            collider.size = new Vector3(1.55f, 0.86f, 1.55f);

            var lod = obstacle.AddComponent<LODGroup>();
            lod.fadeMode = LODFadeMode.CrossFade;
            lod.animateCrossFading = true;
            lod.SetLODs(new[] { new LOD(0.018f, new Renderer[] { renderer }) });
            lod.RecalculateBounds();
        }

        private static void CreateCombinedLayer(
            TopDown3DGeneratedChunk chunk,
            Material material,
            IReadOnlyList<TopDown3DNaturalObjectPlacement> placements,
            string name,
            ShadowCastingMode shadows)
        {
            if (material == null || placements.Count == 0)
            {
                return;
            }

            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var triangles = new List<int>();
            var worldToChunk = chunk.transform.worldToLocalMatrix;
            for (var i = 0; i < placements.Count; i++)
            {
                var placement = placements[i];
                var source = TopDown3DNaturalMeshLibrary.GetData(placement.Shape, placement.Variant);
                var sourceVertices = source.Vertices;
                var sourceNormals = source.Normals;
                var sourceTriangles = source.Triangles;
                var matrix = worldToChunk * Matrix4x4.TRS(
                    placement.Position,
                    placement.Rotation,
                    placement.Scale);
                var normalMatrix = matrix.inverse.transpose;
                var vertexOffset = vertices.Count;
                for (var vertex = 0; vertex < sourceVertices.Length; vertex++)
                {
                    vertices.Add(matrix.MultiplyPoint3x4(sourceVertices[vertex]));
                    normals.Add(normalMatrix.MultiplyVector(sourceNormals[vertex]).normalized);
                }

                for (var triangle = 0; triangle < sourceTriangles.Length; triangle++)
                {
                    triangles.Add(vertexOffset + sourceTriangles[triangle]);
                }
            }

            var mesh = new Mesh { name = $"Chunk {chunk.Coordinate.x},{chunk.Coordinate.y} {name}" };
            if (vertices.Count > ushort.MaxValue)
            {
                mesh.indexFormat = IndexFormat.UInt32;
            }

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0, true);
            mesh.RecalculateBounds();
            chunk.RegisterGeneratedMesh(mesh);

            var layerObject = new GameObject(name);
            layerObject.transform.SetParent(chunk.transform, false);
            layerObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = layerObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = shadows;
            renderer.receiveShadows = true;
        }
    }
}
