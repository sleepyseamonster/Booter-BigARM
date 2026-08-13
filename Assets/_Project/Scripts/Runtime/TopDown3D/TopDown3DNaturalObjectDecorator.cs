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
                    case TopDown3DNaturalObjectLayer.Landmark:
                        CreateObstacle(
                            chunk,
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
            TopDown3DGeneratedChunk chunk,
            Material material,
            TopDown3DNaturalObjectPlacement placement,
            int index)
        {
            var typeName = placement.Layer == TopDown3DNaturalObjectLayer.Landmark
                ? "Towering Landmark"
                : placement.MemberCount > 1 ? "Natural Formation" : "Natural Obstacle";
            var obstacle = new GameObject($"{typeName} {index} - {placement.StableId}");
            obstacle.transform.SetParent(chunk.transform, true);
            obstacle.transform.SetPositionAndRotation(placement.Position, placement.Rotation);

            var filter = obstacle.AddComponent<MeshFilter>();
            if (placement.MemberCount > 1)
            {
                var formationMesh = BuildFormationMesh(placement, typeName);
                filter.sharedMesh = formationMesh;
                chunk.RegisterGeneratedMesh(formationMesh);
            }
            else
            {
                obstacle.transform.localScale = placement.Scale;
                filter.sharedMesh = TopDown3DNaturalMeshLibrary.GetMesh(placement.Shape, placement.Variant);
            }

            var renderer = obstacle.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;

            var collider = obstacle.AddComponent<BoxCollider>();
            if (placement.MemberCount > 1
                || placement.Layer == TopDown3DNaturalObjectLayer.Landmark)
            {
                collider.center = filter.sharedMesh.bounds.center;
                collider.size = filter.sharedMesh.bounds.size;
            }
            else
            {
                collider.center = new Vector3(0f, 0.43f, 0f);
                collider.size = new Vector3(1.55f, 0.86f, 1.55f);
            }

            obstacle.AddComponent<TopDown3DTraversalObstacle>();
        }

        private static Mesh BuildFormationMesh(
            TopDown3DNaturalObjectPlacement placement,
            string typeName)
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var triangles = new List<int>();
            AppendMesh(
                TopDown3DNaturalMeshLibrary.GetData(placement.Shape, placement.Variant),
                Matrix4x4.Scale(placement.Scale),
                vertices,
                normals,
                triangles);

            var horizontalScale = Mathf.Max(placement.Scale.x, placement.Scale.z);
            for (var member = 1; member < placement.MemberCount; member++)
            {
                var angle = Hash01(placement.FormationSeed, member, 101) * Mathf.PI * 2f;
                var radius = horizontalScale
                    * Mathf.Lerp(0.34f, 0.58f, Hash01(placement.FormationSeed, member, 211));
                var scaleFactor = Mathf.Lerp(
                    0.52f,
                    0.9f,
                    Hash01(placement.FormationSeed, member, 307));
                var memberScale = Vector3.Scale(
                    placement.Scale * scaleFactor,
                    new Vector3(
                        Mathf.Lerp(0.82f, 1.18f, Hash01(placement.FormationSeed, member, 401)),
                        Mathf.Lerp(0.72f, 1.08f, Hash01(placement.FormationSeed, member, 503)),
                        Mathf.Lerp(0.82f, 1.18f, Hash01(placement.FormationSeed, member, 601))));
                var offset = new Vector3(
                    Mathf.Cos(angle) * radius,
                    -placement.Scale.y
                        * Mathf.Lerp(0.02f, 0.11f, Hash01(placement.FormationSeed, member, 701)),
                    Mathf.Sin(angle) * radius);
                var rotation = Quaternion.Euler(
                    Mathf.Lerp(-8f, 8f, Hash01(placement.FormationSeed, member, 809)),
                    Hash01(placement.FormationSeed, member, 907) * 360f,
                    Mathf.Lerp(-8f, 8f, Hash01(placement.FormationSeed, member, 1009)));
                var shape = SelectFormationShape(placement, member);
                var variant = Mathf.FloorToInt(
                    Hash01(placement.FormationSeed, member, 1103)
                    * TopDown3DNaturalMeshLibrary.VariantsPerShape)
                    % TopDown3DNaturalMeshLibrary.VariantsPerShape;
                AppendMesh(
                    TopDown3DNaturalMeshLibrary.GetData(shape, variant),
                    Matrix4x4.TRS(offset, rotation, memberScale),
                    vertices,
                    normals,
                    triangles);
            }

            var mesh = new Mesh { name = $"{typeName} - {placement.StableId}" };
            if (vertices.Count > ushort.MaxValue)
            {
                mesh.indexFormat = IndexFormat.UInt32;
            }

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0, true);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static TopDown3DNaturalObjectShape SelectFormationShape(
            TopDown3DNaturalObjectPlacement placement,
            int member)
        {
            var selection = Mathf.FloorToInt(Hash01(placement.FormationSeed, member, 1201) * 4f);
            if (selection == 0)
            {
                return placement.Shape;
            }

            if (placement.Layer == TopDown3DNaturalObjectLayer.Landmark && selection == 1)
            {
                return TopDown3DNaturalObjectShape.Shard;
            }

            return selection == 2
                ? TopDown3DNaturalObjectShape.Nodule
                : TopDown3DNaturalObjectShape.Boulder;
        }

        private static void AppendMesh(
            TopDown3DNaturalMeshLibrary.MeshData source,
            Matrix4x4 matrix,
            ICollection<Vector3> vertices,
            ICollection<Vector3> normals,
            ICollection<int> triangles)
        {
            var normalMatrix = matrix.inverse.transpose;
            var vertexOffset = vertices.Count;
            for (var vertex = 0; vertex < source.Vertices.Length; vertex++)
            {
                vertices.Add(matrix.MultiplyPoint3x4(source.Vertices[vertex]));
                normals.Add(normalMatrix.MultiplyVector(source.Normals[vertex]).normalized);
            }

            for (var triangle = 0; triangle < source.Triangles.Length; triangle++)
            {
                triangles.Add(vertexOffset + source.Triangles[triangle]);
            }
        }

        private static float Hash01(int seed, int member, int channel)
        {
            unchecked
            {
                var value = (uint)seed;
                value ^= (uint)member * 0x9E3779B9u;
                value ^= (uint)channel * 0x85EBCA6Bu;
                value ^= value >> 16;
                value *= 0x7FEB352Du;
                value ^= value >> 15;
                return (value & 0x00FFFFFFu) / 16777215f;
            }
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
