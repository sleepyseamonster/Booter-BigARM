using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

namespace BooterBigArm.TopDown3D
{
    public static class TopDown3DNaturalObjectDecorator
    {
        private const int RockSurfaceCount = 3;

        private static readonly ProfilerMarker DecorateMarker =
            new ProfilerMarker("TopDown3D.World.DecorateNaturalObjects");
        private static readonly ProfilerMarker PlanMarker =
            new ProfilerMarker("TopDown3D.World.PlanNaturalObjects");
        private static readonly ProfilerMarker BuildFormationMarker =
            new ProfilerMarker("TopDown3D.World.BuildRockFormation");
        private static readonly ProfilerMarker BuildCombinedLayerMarker =
            new ProfilerMarker("TopDown3D.World.BuildCombinedNaturalLayer");

        public static void Decorate(
            TopDown3DGeneratedChunk chunk,
            TopDown3DWorldSettings settings,
            Material material,
            Vector2 spawnExclusionCenter)
        {
            using (DecorateMarker.Auto())
            {
                if (chunk == null || settings == null || material == null || settings.NaturalObjectCatalog == null)
                {
                    return;
                }

                TopDown3DNaturalObjectChunkPlan plan;
                using (PlanMarker.Auto())
                {
                    plan = TopDown3DNaturalObjectPlanner.BuildChunkPlan(
                        settings,
                        settings.NaturalObjectCatalog,
                        chunk.Coordinate,
                        spawnExclusionCenter);
                }

                var scatter = CreateSurfaceBuckets((settings.ScatterObjectsPerChunk + 2) / 3);
                var details = CreateSurfaceBuckets((settings.GroundDetailsPerChunk + 2) / 3);
                var fineGrayClusters = new List<TopDown3DNaturalObjectPlacement>(
                    settings.FineGrayClutterPerChunk);
                for (var i = 0; i < plan.CosmeticPlacements.Count; i++)
                {
                    var placement = plan.CosmeticPlacements[i];
                    switch (placement.Layer)
                    {
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

                for (var i = 0; i < plan.PhysicalFormations.Count; i++)
                {
                    var formation = plan.PhysicalFormations[i];
                    CreateFormation(
                        chunk,
                        ResolveRockMaterial(settings, material, formation.Surface),
                        formation,
                        i + 1);
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
        }

        private static void CreateFormation(
            TopDown3DGeneratedChunk chunk,
            Material material,
            TopDown3DRockFormationPlan formation,
            int index)
        {
            using (BuildFormationMarker.Auto())
            {
                if (formation.Members.Count == 0)
                {
                    return;
                }

                var rootMember = formation.Members[0];
                var typeName = rootMember.Tier == TopDown3DRockSizeTier.Towering
                    ? "Towering Rock Formation"
                    : rootMember.Tier == TopDown3DRockSizeTier.Massive
                        ? "Massive Rock Formation"
                        : "Large Rock Formation";
                var root = new GameObject($"{typeName} {index} - {formation.StableId}");
                root.transform.SetParent(chunk.transform, true);
                root.transform.SetPositionAndRotation(rootMember.Position, rootMember.Rotation);

                var mesh = BuildFormationMesh(formation, root.transform.worldToLocalMatrix, typeName);
                chunk.RegisterGeneratedMesh(mesh);
                root.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = root.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;

                AddRootCollider(root, rootMember);
                for (var memberIndex = 1; memberIndex < formation.Members.Count; memberIndex++)
                {
                    AddChildCollider(root.transform, formation.Members[memberIndex]);
                }

                root.AddComponent<TopDown3DTraversalObstacle>();
                ReleaseRuntimeCpuMeshData(mesh);
            }
        }

        private static void AddRootCollider(
            GameObject root,
            TopDown3DRockFormationMember member)
        {
            var sourceBounds = TopDown3DNaturalMeshLibrary.GetMesh(
                member.Shape,
                member.Variant).bounds;
            var collider = root.AddComponent<BoxCollider>();
            collider.center = Vector3.Scale(sourceBounds.center, member.Scale);
            collider.size = Vector3.Scale(sourceBounds.size, Abs(member.Scale));
        }

        private static void AddChildCollider(
            Transform root,
            TopDown3DRockFormationMember member)
        {
            var child = new GameObject($"Rock Collider {member.MemberIndex} - {member.StableId}");
            child.transform.SetParent(root, true);
            child.transform.SetPositionAndRotation(member.Position, member.Rotation);
            child.transform.localScale = member.Scale;
            var sourceBounds = TopDown3DNaturalMeshLibrary.GetMesh(
                member.Shape,
                member.Variant).bounds;
            var collider = child.AddComponent<BoxCollider>();
            collider.center = sourceBounds.center;
            collider.size = sourceBounds.size;
        }

        private static Mesh BuildFormationMesh(
            TopDown3DRockFormationPlan formation,
            Matrix4x4 worldToRoot,
            string typeName)
        {
            GetFormationMeshCounts(formation, out var vertexCount, out var triangleCount);
            var vertices = new List<Vector3>(vertexCount);
            var normals = new List<Vector3>(vertexCount);
            var triangles = new List<int>(triangleCount);
            for (var i = 0; i < formation.Members.Count; i++)
            {
                var member = formation.Members[i];
                AppendMesh(
                    TopDown3DNaturalMeshLibrary.GetData(member.Shape, member.Variant),
                    worldToRoot * Matrix4x4.TRS(
                        member.Position,
                        member.Rotation,
                        member.Scale),
                    vertices,
                    normals,
                    triangles);
            }

            var mesh = new Mesh { name = $"{typeName} - {formation.StableId}" };
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

        private static List<TopDown3DNaturalObjectPlacement>[] CreateSurfaceBuckets(int capacityPerBucket)
        {
            var buckets = new List<TopDown3DNaturalObjectPlacement>[RockSurfaceCount];
            for (var i = 0; i < buckets.Length; i++)
            {
                buckets[i] = new List<TopDown3DNaturalObjectPlacement>(capacityPerBucket);
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

        private static Vector3 Abs(Vector3 value)
        {
            return new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
        }

        private static void AppendMesh(
            TopDown3DNaturalMeshLibrary.MeshData source,
            Matrix4x4 matrix,
            List<Vector3> vertices,
            List<Vector3> normals,
            List<int> triangles)
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

        private static void CreateCombinedLayer(
            TopDown3DGeneratedChunk chunk,
            Material material,
            IReadOnlyList<TopDown3DNaturalObjectPlacement> placements,
            string name,
            ShadowCastingMode shadows)
        {
            using (BuildCombinedLayerMarker.Auto())
            {
                if (material == null || placements.Count == 0)
                {
                    return;
                }

                GetPlacementMeshCounts(placements, out var vertexCount, out var triangleCount);
                var vertices = new List<Vector3>(vertexCount);
                var normals = new List<Vector3>(vertexCount);
                var triangles = new List<int>(triangleCount);
                var worldToChunk = chunk.transform.worldToLocalMatrix;
                for (var i = 0; i < placements.Count; i++)
                {
                    var placement = placements[i];
                    AppendMesh(
                        TopDown3DNaturalMeshLibrary.GetData(placement.Shape, placement.Variant),
                        worldToChunk * Matrix4x4.TRS(
                            placement.Position,
                            placement.Rotation,
                            placement.Scale),
                        vertices,
                        normals,
                        triangles);
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
                ReleaseRuntimeCpuMeshData(mesh);
            }
        }

        private static void GetFormationMeshCounts(
            TopDown3DRockFormationPlan formation,
            out int vertexCount,
            out int triangleCount)
        {
            vertexCount = 0;
            triangleCount = 0;
            for (var i = 0; i < formation.Members.Count; i++)
            {
                var member = formation.Members[i];
                var data = TopDown3DNaturalMeshLibrary.GetData(member.Shape, member.Variant);
                vertexCount += data.Vertices.Length;
                triangleCount += data.Triangles.Length;
            }
        }

        private static void GetPlacementMeshCounts(
            IReadOnlyList<TopDown3DNaturalObjectPlacement> placements,
            out int vertexCount,
            out int triangleCount)
        {
            vertexCount = 0;
            triangleCount = 0;
            for (var i = 0; i < placements.Count; i++)
            {
                var placement = placements[i];
                var data = TopDown3DNaturalMeshLibrary.GetData(placement.Shape, placement.Variant);
                vertexCount += data.Vertices.Length;
                triangleCount += data.Triangles.Length;
            }
        }

        private static void ReleaseRuntimeCpuMeshData(Mesh mesh)
        {
            if (Application.isPlaying && mesh != null)
            {
                mesh.UploadMeshData(true);
            }
        }
    }
}
