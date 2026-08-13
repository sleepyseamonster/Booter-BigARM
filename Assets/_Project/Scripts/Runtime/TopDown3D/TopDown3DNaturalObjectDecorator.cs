using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BooterBigArm.TopDown3D
{
    public static class TopDown3DNaturalObjectDecorator
    {
        private const int RockSurfaceCount = 3;

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
            var scatter = CreateSurfaceBuckets();
            var details = CreateSurfaceBuckets();
            var fineGrayClusters = new List<TopDown3DNaturalObjectPlacement>();
            var obstacleIndex = 0;
            for (var i = 0; i < placements.Count; i++)
            {
                var placement = placements[i];
                switch (placement.Layer)
                {
                    case TopDown3DNaturalObjectLayer.Obstacle:
                        CreateObstacle(
                            chunk.transform,
                            ResolveRockMaterial(settings, material, placement.Surface),
                            placement,
                            ++obstacleIndex);
                        break;
                    case TopDown3DNaturalObjectLayer.Scatter:
                        scatter[(int)placement.Surface].Add(placement);
                        break;
                    case TopDown3DNaturalObjectLayer.GroundDetail:
                        details[(int)placement.Surface].Add(placement);
                        break;
                    case TopDown3DNaturalObjectLayer.FineGrayCluster:
                        fineGrayClusters.Add(placement);
                        break;
                }
            }

            for (var surfaceIndex = 0; surfaceIndex < RockSurfaceCount; surfaceIndex++)
            {
                var surface = (TopDown3DRockSurface)surfaceIndex;
                var surfaceMaterial = ResolveRockMaterial(settings, material, surface);
                var surfaceName = GetSurfaceName(surface);
                CreateCombinedLayer(
                    chunk,
                    surfaceMaterial,
                    scatter[surfaceIndex],
                    $"Natural Scatter - {surfaceName}",
                    ShadowCastingMode.On);
                CreateCombinedLayer(
                    chunk,
                    surfaceMaterial,
                    details[surfaceIndex],
                    $"Ground Micro Detail - {surfaceName}",
                    ShadowCastingMode.Off);
            }

            CreateCombinedLayer(
                chunk,
                settings.FineGrayClutterMaterial,
                fineGrayClusters,
                "Fine Gray Ground Clusters",
                ShadowCastingMode.Off);
        }

        private static List<TopDown3DNaturalObjectPlacement>[] CreateSurfaceBuckets()
        {
            var buckets = new List<TopDown3DNaturalObjectPlacement>[RockSurfaceCount];
            for (var i = 0; i < buckets.Length; i++)
            {
                buckets[i] = new List<TopDown3DNaturalObjectPlacement>();
            }

            return buckets;
        }

        private static Material ResolveRockMaterial(
            TopDown3DWorldSettings settings,
            Material regularMaterial,
            TopDown3DRockSurface surface)
        {
            switch (surface)
            {
                case TopDown3DRockSurface.Dark:
                    return settings.DarkRockMaterial != null ? settings.DarkRockMaterial : regularMaterial;
                case TopDown3DRockSurface.Teal:
                    return settings.TealRockMaterial != null ? settings.TealRockMaterial : regularMaterial;
                default:
                    return regularMaterial;
            }
        }

        private static string GetSurfaceName(TopDown3DRockSurface surface)
        {
            switch (surface)
            {
                case TopDown3DRockSurface.Dark:
                    return "Dark";
                case TopDown3DRockSurface.Teal:
                    return "Teal";
                default:
                    return "Regular";
            }
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
            obstacle.AddComponent<TopDown3DTraversalObstacle>();
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
