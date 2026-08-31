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
        private static readonly ProfilerMarker BuildFormationMarker =
            new ProfilerMarker("TopDown3D.World.PrepareRockFormation");
        private static readonly ProfilerMarker BuildCombinedLayerMarker =
            new ProfilerMarker("TopDown3D.World.BuildCombinedNaturalLayer");

        internal static void Decorate(
            TopDown3DGeneratedChunk chunk,
            TopDown3DWorldSettings settings,
            Material material,
            TopDown3DNaturalObjectChunkPlan plan)
        {
            using (DecorateMarker.Auto())
            {
                if (chunk == null
                    || settings == null
                    || material == null
                    || plan == null
                    || settings.NaturalObjectCatalog == null)
                {
                    return;
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
                        settings.NaturalObjectCatalog,
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
                        settings.NaturalObjectCatalog,
                        surfaceMaterial,
                        scatter[surfaceIndex],
                        $"Natural Scatter - {surfaceName}",
                        ShadowCastingMode.On);
                    CreateCombinedLayer(
                        chunk,
                        settings.NaturalObjectCatalog,
                        surfaceMaterial,
                        details[surfaceIndex],
                        $"Ground Micro Detail - {surfaceName}",
                        ShadowCastingMode.Off);
                }

                CreateCombinedLayer(
                    chunk,
                    settings.NaturalObjectCatalog,
                    settings.FineGrayClutterMaterial,
                    fineGrayClusters,
                    "Fine Gray Ground Clusters",
                    ShadowCastingMode.Off);
            }
        }

        private static void CreateFormation(
            TopDown3DGeneratedChunk chunk,
            TopDown3DNaturalObjectCatalog catalog,
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

                var root = new GameObject(
                    $"{GetTierName(formation.Members[0].Tier)} Rock Formation {index} - {formation.StableId}");
                root.transform.SetParent(chunk.DecorationRoot, false);
                for (var memberIndex = 0; memberIndex < formation.Members.Count; memberIndex++)
                {
                    var member = formation.Members[memberIndex];
                    var memberObject = new GameObject($"Rock {member.MemberIndex} - {member.StableId}");
                    memberObject.transform.SetParent(root.transform, true);
                    memberObject.transform.SetPositionAndRotation(member.Position, member.Rotation);
                    memberObject.transform.localScale = member.Scale;
                    var family = catalog.GetRequiredMeshFamily(
                        member.Shape,
                        member.Variant);
                    var collider = memberObject.AddComponent<BoxCollider>();
                    collider.center = family.ColliderCenter;
                    collider.size = family.ColliderSize;
                }

                CreateFormationLods(chunk, root, catalog, material, formation);
                root.AddComponent<TopDown3DTraversalObstacle>();
            }
        }

        private static void CreateFormationLods(
            TopDown3DGeneratedChunk chunk,
            GameObject root,
            TopDown3DNaturalObjectCatalog catalog,
            Material material,
            TopDown3DRockFormationPlan formation)
        {
            var renderers = new Renderer[3];
            var lod0ScreenHeight = 0f;
            var lod1ScreenHeight = 0f;
            var lod2ScreenHeight = 0f;
            var worldToRoot = root.transform.worldToLocalMatrix;
            for (var lodIndex = 0; lodIndex < renderers.Length; lodIndex++)
            {
                var combines = new CombineInstance[formation.Members.Count];
                var vertexCount = 0;
                for (var memberIndex = 0; memberIndex < formation.Members.Count; memberIndex++)
                {
                    var member = formation.Members[memberIndex];
                    var family = catalog.GetRequiredMeshFamily(member.Shape, member.Variant);
                    var sourceMesh = family.GetLod(lodIndex);
                    vertexCount += sourceMesh.vertexCount;
                    combines[memberIndex] = new CombineInstance
                    {
                        mesh = sourceMesh,
                        transform = worldToRoot * Matrix4x4.TRS(
                            member.Position,
                            member.Rotation,
                            member.Scale)
                    };
                    lod0ScreenHeight = Mathf.Max(lod0ScreenHeight, family.Lod0ScreenHeight);
                    lod1ScreenHeight = Mathf.Max(lod1ScreenHeight, family.Lod1ScreenHeight);
                    lod2ScreenHeight = Mathf.Max(lod2ScreenHeight, family.Lod2ScreenHeight);
                }

                var mesh = new Mesh { name = $"{root.name} LOD{lodIndex}" };
                if (vertexCount > ushort.MaxValue)
                {
                    mesh.indexFormat = IndexFormat.UInt32;
                }

                mesh.CombineMeshes(combines, true, true, false);
                chunk.RegisterDecorationMesh(mesh);

                var lodObject = new GameObject($"LOD{lodIndex}");
                lodObject.transform.SetParent(root.transform, false);
                lodObject.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = lodObject.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
                renderers[lodIndex] = renderer;
            }

            lod1ScreenHeight = Mathf.Min(lod1ScreenHeight, lod0ScreenHeight - 0.001f);
            lod2ScreenHeight = Mathf.Min(lod2ScreenHeight, lod1ScreenHeight - 0.001f);
            var lodGroup = root.AddComponent<LODGroup>();
            lodGroup.SetLODs(new[]
            {
                new LOD(lod0ScreenHeight, new[] { renderers[0] }),
                new LOD(lod1ScreenHeight, new[] { renderers[1] }),
                new LOD(lod2ScreenHeight, new[] { renderers[2] })
            });
            lodGroup.RecalculateBounds();
            for (var i = 0; i < renderers.Length; i++)
            {
                ReleaseRuntimeCpuMeshData(renderers[i].GetComponent<MeshFilter>().sharedMesh);
            }
        }

        internal static Renderer[] CreateMemberLods(
            GameObject memberObject,
            TopDown3DNaturalMeshFamily family,
            Material material)
        {
            var renderers = new Renderer[3];
            for (var lodIndex = 0; lodIndex < renderers.Length; lodIndex++)
            {
                var lodObject = new GameObject($"LOD{lodIndex}");
                lodObject.transform.SetParent(memberObject.transform, false);
                lodObject.AddComponent<MeshFilter>().sharedMesh = family.GetLod(lodIndex);
                var renderer = lodObject.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
                renderers[lodIndex] = renderer;
            }

            var lodGroup = memberObject.AddComponent<LODGroup>();
            lodGroup.SetLODs(new[]
            {
                new LOD(family.Lod0ScreenHeight, new[] { renderers[0] }),
                new LOD(family.Lod1ScreenHeight, new[] { renderers[1] }),
                new LOD(family.Lod2ScreenHeight, new[] { renderers[2] })
            });
            lodGroup.RecalculateBounds();
            return renderers;
        }

        private static string GetTierName(TopDown3DRockSizeTier tier)
        {
            return tier == TopDown3DRockSizeTier.ExtraLarge ? "Extra Large" : tier.ToString();
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

        private static void AppendMesh(
            TopDown3DNaturalMeshData source,
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
            TopDown3DNaturalObjectCatalog catalog,
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

                GetPlacementMeshCounts(catalog, placements, out var vertexCount, out var triangleCount);
                var vertices = new List<Vector3>(vertexCount);
                var normals = new List<Vector3>(vertexCount);
                var triangles = new List<int>(triangleCount);
                var worldToChunk = chunk.transform.worldToLocalMatrix;
                for (var i = 0; i < placements.Count; i++)
                {
                    var placement = placements[i];
                    AppendMesh(
                        catalog.GetRequiredLod0Data(placement.Shape, placement.Variant),
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
                chunk.RegisterDecorationMesh(mesh);

                var layerObject = new GameObject(name);
                layerObject.transform.SetParent(chunk.DecorationRoot, false);
                layerObject.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = layerObject.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = shadows;
                renderer.receiveShadows = true;
                ReleaseRuntimeCpuMeshData(mesh);
            }
        }

        private static void GetPlacementMeshCounts(
            TopDown3DNaturalObjectCatalog catalog,
            IReadOnlyList<TopDown3DNaturalObjectPlacement> placements,
            out int vertexCount,
            out int triangleCount)
        {
            vertexCount = 0;
            triangleCount = 0;
            for (var i = 0; i < placements.Count; i++)
            {
                var placement = placements[i];
                var data = catalog.GetRequiredLod0Data(placement.Shape, placement.Variant);
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
