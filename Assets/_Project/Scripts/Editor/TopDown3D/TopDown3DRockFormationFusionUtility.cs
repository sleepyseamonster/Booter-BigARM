using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BooterBigArm.TopDown3D;

namespace BooterBigArm.Editor
{
    /// <summary>
    /// Turns intersecting authored rock members into Boolean-unioned, saved mesh assets.
    /// The untouched source members remain under the formation so an author can restore
    /// and adjust them before attempting another union.
    /// </summary>
    internal static class TopDown3DRockFormationFusionUtility
    {
        private const string SourceMembersRootName = "Source Rock Members";
        private const string FusedMembersRootName = "Fused Rock Members";
        private const string SceneFusedRootName = "Fused Rock Formations (Scene)";
        private const string GeneratedMeshFolder = "Assets/_Project/Art/Generated/RockFormations";

        internal static bool HasFusedMembers(TopDown3DRockFormationAuthoring authoring)
        {
            return authoring != null
                && authoring.GeneratedMembersRoot != null
                && authoring.GeneratedMembersRoot.Find(FusedMembersRootName) != null;
        }

        internal static bool HasOverlappingEditableMembers(TopDown3DRockFormationAuthoring authoring)
        {
            var members = CollectMembers(authoring);
            if (members.Count < 2) return false;
            Physics.SyncTransforms();
            for (var first = 0; first < members.Count; first++)
            {
                for (var second = first + 1; second < members.Count; second++)
                {
                    if (MembersOverlap(members[first].Filter, members[first].Collider, members[second].Filter, members[second].Collider)) return true;
                }
            }

            return false;
        }

        internal static int GetEditableMembersTransformHash(TopDown3DRockFormationAuthoring authoring)
        {
            unchecked
            {
                var hash = 17;
                var members = CollectMembers(authoring);
                for (var index = 0; index < members.Count; index++)
                {
                    var transform = members[index].Filter.transform;
                    hash = hash * 31 + transform.localPosition.GetHashCode();
                    hash = hash * 31 + transform.localRotation.GetHashCode();
                    hash = hash * 31 + transform.localScale.GetHashCode();
                    hash = hash * 31 + (transform.gameObject.activeSelf ? 1 : 0);
                }

                return hash;
            }
        }

        internal static bool TryFuseAcrossFormations(
            IReadOnlyList<TopDown3DRockFormationAuthoring> formations,
            out int fusedClusterCount,
            out string error)
        {
            fusedClusterCount = 0;
            error = null;
            if (!TopDown3DManifoldNative.IsSupportedPlatform || formations == null) return false;

            var members = CollectSceneMembers(formations);
            if (members.Count < 2) return false;

            Physics.SyncTransforms();
            var clusters = BuildCrossFormationClusters(members);
            if (clusters.Count == 0) return false;

            var scene = clusters[0][0].Filter.gameObject.scene;
            var fusedRoot = GetOrCreateSceneFusedRoot(scene);
            var results = new List<SceneFusionResult>(clusters.Count);
            try
            {
                for (var clusterIndex = 0; clusterIndex < clusters.Count; clusterIndex++)
                {
                    var cluster = clusters[clusterIndex];
                    var solids = new List<TopDown3DIndexedMeshData>(cluster.Count);
                    for (var memberIndex = 0; memberIndex < cluster.Count; memberIndex++)
                    {
                        solids.Add(BuildSolid(cluster[memberIndex].Filter, fusedRoot.transform));
                    }

                    if (!TopDown3DRockFormationMeshBuilder.TryBuildUnion(solids, out var data, out var fusionError))
                    {
                        error = $"Scene rock cluster {clusterIndex + 1} could not be fused: {fusionError}";
                        RecordStatus(cluster, error);
                        return false;
                    }

                    results.Add(new SceneFusionResult(cluster, data));
                }
            }
            catch (Exception exception)
            {
                error = $"Scene rock fusion could not prepare its source meshes: {exception.Message}";
                for (var index = 0; index < clusters.Count; index++) RecordStatus(clusters[index], error);
                return false;
            }

            Undo.SetCurrentGroupName("Fuse Overlapping Rock Formations");
            var undoGroup = Undo.GetCurrentGroup();
            try
            {
                for (var resultIndex = 0; resultIndex < results.Count; resultIndex++)
                {
                    var result = results[resultIndex];
                    var mesh = TopDown3DRockFormationMeshBuilder.CreateUnityMesh(
                        result.MeshData,
                        $"Fused Scene Rock Cluster {resultIndex + 1}");
                    AssetDatabase.CreateAsset(
                        mesh,
                        AssetDatabase.GenerateUniqueAssetPath(
                            $"{GeneratedMeshFolder}/Scene_Rock_Formation_Fused_{resultIndex + 1}.asset"));
                    Undo.RegisterCreatedObjectUndo(mesh, "Fuse Overlapping Rock Formations");

                    var fusedObject = new GameObject($"Fused Rock Formation {resultIndex + 1}");
                    Undo.RegisterCreatedObjectUndo(fusedObject, "Fuse Overlapping Rock Formations");
                    fusedObject.transform.SetParent(fusedRoot.transform, false);
                    fusedObject.AddComponent<MeshFilter>().sharedMesh = mesh;
                    fusedObject.AddComponent<MeshRenderer>().sharedMaterial = result.Material;
                    fusedObject.AddComponent<MeshCollider>().sharedMesh = mesh;

                    for (var memberIndex = 0; memberIndex < result.Members.Count; memberIndex++)
                    {
                        var memberObject = result.Members[memberIndex].Filter.gameObject;
                        Undo.RecordObject(memberObject, "Fuse Overlapping Rock Formations");
                        memberObject.SetActive(false);
                    }

                    RecordStatus(result.Members, "Fused with an overlapping rock formation in the scene.");
                }

                fusedClusterCount = results.Count;
                EditorSceneManager.MarkSceneDirty(scene);
                return true;
            }
            catch (Exception exception)
            {
                Undo.RevertAllDownToGroup(undoGroup);
                error = $"Could not create scene-fused rock assets: {exception.Message}";
                return false;
            }
            finally
            {
                Undo.CollapseUndoOperations(undoGroup);
            }
        }

        internal static bool HasSceneFusedMembers(UnityEngine.SceneManagement.Scene scene)
        {
            return FindSceneFusedRoot(scene) != null;
        }

        internal static void RestoreSceneFusedMembers(UnityEngine.SceneManagement.Scene scene)
        {
            var fusedRoot = FindSceneFusedRoot(scene);
            if (fusedRoot == null) return;

            var formations = UnityEngine.Object.FindObjectsByType<TopDown3DRockFormationAuthoring>();
            for (var index = 0; index < formations.Length; index++)
            {
                if (formations[index].gameObject.scene != scene) continue;
                RestoreIndividualMembers(formations[index], false);
                var filters = formations[index].GetComponentsInChildren<MeshFilter>(true);
                for (var filterIndex = 0; filterIndex < filters.Length; filterIndex++)
                {
                    if (!filters[filterIndex].gameObject.activeSelf)
                    {
                        Undo.RecordObject(filters[filterIndex].gameObject, "Restore Scene Rock Fusion");
                        filters[filterIndex].gameObject.SetActive(true);
                    }
                }
            }

            Undo.DestroyObjectImmediate(fusedRoot.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        internal static bool TryFuseOverlappingMembers(
            TopDown3DRockFormationAuthoring authoring,
            out int fusedClusterCount,
            out string error)
        {
            fusedClusterCount = 0;
            error = null;
            if (authoring == null || authoring.GeneratedMembersRoot == null)
            {
                error = "Generate a formation before fusing its rock members.";
                return false;
            }

            if (!TopDown3DManifoldNative.IsSupportedPlatform)
            {
                error = "Rock fusion is currently available only in the macOS Unity Editor.";
                RecordStatus(authoring, error);
                return false;
            }

            var generatedRoot = authoring.GeneratedMembersRoot;
            var sourceRoot = EnsureSourceMembersRoot(generatedRoot);
            RestoreIndividualMembers(authoring, false);
            sourceRoot.gameObject.SetActive(true);

            var members = CollectMembers(sourceRoot);
            if (members.Count < 2)
            {
                error = "At least two editable rock members are needed for a Boolean fusion.";
                RecordStatus(authoring, error);
                return false;
            }

            Physics.SyncTransforms();
            var clusters = BuildOverlappingClusters(members);
            if (clusters.Count == 0)
            {
                error = "No rock volumes overlap yet. Move members slightly into each other, then fuse.";
                RecordStatus(authoring, error);
                return false;
            }

            var fusionResults = new List<FusionResult>(clusters.Count);
            try
            {
                for (var clusterIndex = 0; clusterIndex < clusters.Count; clusterIndex++)
                {
                    var cluster = clusters[clusterIndex];
                    var solids = new List<TopDown3DIndexedMeshData>(cluster.Count);
                    for (var memberIndex = 0; memberIndex < cluster.Count; memberIndex++)
                    {
                        solids.Add(BuildSolid(cluster[memberIndex].Filter, generatedRoot));
                    }

                    if (!TopDown3DRockFormationMeshBuilder.TryBuildUnion(solids, out var data, out var fusionError))
                    {
                        error = $"Rock cluster {clusterIndex + 1} could not be fused: {fusionError}";
                        RecordStatus(authoring, error);
                        return false;
                    }

                    fusionResults.Add(new FusionResult(cluster, data));
                }
            }
            catch (Exception exception)
            {
                error = $"Rock fusion could not prepare its source meshes: {exception.Message}";
                RecordStatus(authoring, error);
                return false;
            }

            Undo.SetCurrentGroupName("Fuse Overlapping Rock Members");
            var undoGroup = Undo.GetCurrentGroup();
            try
            {
                var fusedRoot = new GameObject(FusedMembersRootName);
                Undo.RegisterCreatedObjectUndo(fusedRoot, "Fuse Overlapping Rock Members");
                fusedRoot.transform.SetParent(generatedRoot, false);
                var usedAssetNames = new HashSet<string>(StringComparer.Ordinal);
                for (var resultIndex = 0; resultIndex < fusionResults.Count; resultIndex++)
                {
                    var result = fusionResults[resultIndex];
                    var mesh = TopDown3DRockFormationMeshBuilder.CreateUnityMesh(
                        result.MeshData,
                        $"Fused Rock Cluster {resultIndex + 1}");
                    var assetPath = CreateFusedMeshAsset(authoring, mesh, resultIndex, usedAssetNames);
                    Undo.RegisterCreatedObjectUndo(mesh, "Fuse Overlapping Rock Members");

                    var fusedObject = new GameObject($"Fused Rock Cluster {resultIndex + 1}");
                    Undo.RegisterCreatedObjectUndo(fusedObject, "Fuse Overlapping Rock Members");
                    fusedObject.transform.SetParent(fusedRoot.transform, false);
                    fusedObject.AddComponent<MeshFilter>().sharedMesh = mesh;
                    fusedObject.AddComponent<MeshRenderer>().sharedMaterial = result.Material;
                    fusedObject.AddComponent<MeshCollider>().sharedMesh = mesh;

                    for (var memberIndex = 0; memberIndex < result.Members.Count; memberIndex++)
                    {
                        Undo.RecordObject(result.Members[memberIndex].Filter.gameObject, "Fuse Overlapping Rock Members");
                        result.Members[memberIndex].Filter.gameObject.SetActive(false);
                    }
                }

                fusedClusterCount = fusionResults.Count;
                generatedRoot.name = "Fused Rock Members (Source Editable)";
                RecordStatus(authoring, $"Fused {fusedClusterCount} overlapping rock cluster(s).");
                EditorSceneManager.MarkSceneDirty(authoring.gameObject.scene);
                return true;
            }
            catch (Exception exception)
            {
                Undo.RevertAllDownToGroup(undoGroup);
                error = $"Could not create fused rock assets: {exception.Message}";
                RecordStatus(authoring, error);
                return false;
            }
            finally
            {
                Undo.CollapseUndoOperations(undoGroup);
            }
        }

        internal static void RestoreIndividualMembers(
            TopDown3DRockFormationAuthoring authoring,
            bool registerUndo)
        {
            if (authoring == null || authoring.GeneratedMembersRoot == null) return;
            var generatedRoot = authoring.GeneratedMembersRoot;
            var fusedRoot = generatedRoot.Find(FusedMembersRootName);
            var sourceRoot = generatedRoot.Find(SourceMembersRootName);
            if (fusedRoot != null)
            {
                if (registerUndo)
                {
                    Undo.DestroyObjectImmediate(fusedRoot.gameObject);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(fusedRoot.gameObject);
                }
            }

            if (sourceRoot == null) return;
            generatedRoot.name = "Generated Rock Members";
            var filters = sourceRoot.GetComponentsInChildren<MeshFilter>(true);
            for (var i = 0; i < filters.Length; i++)
            {
                var member = filters[i].gameObject;
                if (member.activeSelf) continue;
                if (registerUndo) Undo.RecordObject(member, "Restore Individual Rock Members");
                member.SetActive(true);
            }

            if (registerUndo)
            {
                EditorSceneManager.MarkSceneDirty(authoring.gameObject.scene);
            }
        }

        private static Transform EnsureSourceMembersRoot(Transform generatedRoot)
        {
            var sourceRoot = generatedRoot.Find(SourceMembersRootName);
            if (sourceRoot != null) return sourceRoot;

            var rootObject = new GameObject(SourceMembersRootName);
            Undo.RegisterCreatedObjectUndo(rootObject, "Prepare Rock Members for Fusion");
            sourceRoot = rootObject.transform;
            sourceRoot.SetParent(generatedRoot, false);

            var toMove = new List<Transform>();
            for (var index = 0; index < generatedRoot.childCount; index++)
            {
                var child = generatedRoot.GetChild(index);
                if (child != sourceRoot && child.GetComponent<MeshFilter>() != null)
                {
                    toMove.Add(child);
                }
            }

            for (var index = 0; index < toMove.Count; index++)
            {
                Undo.SetTransformParent(toMove[index], sourceRoot, "Prepare Rock Members for Fusion");
            }

            return sourceRoot;
        }

        private static List<Member> CollectMembers(Transform sourceRoot)
        {
            var filters = sourceRoot.GetComponentsInChildren<MeshFilter>(false);
            var members = new List<Member>(filters.Length);
            for (var index = 0; index < filters.Length; index++)
            {
                var filter = filters[index];
                var collider = filter.GetComponent<Collider>();
                if (filter.sharedMesh != null && collider != null)
                {
                    members.Add(new Member(filter, collider));
                }
            }

            return members;
        }

        private static List<Member> CollectMembers(TopDown3DRockFormationAuthoring authoring)
        {
            if (authoring == null || authoring.GeneratedMembersRoot == null)
            {
                return new List<Member>();
            }

            var sourceRoot = authoring.GeneratedMembersRoot.Find(SourceMembersRootName)
                ?? authoring.GeneratedMembersRoot;
            return CollectMembers(sourceRoot);
        }

        private static List<SceneMember> CollectSceneMembers(
            IReadOnlyList<TopDown3DRockFormationAuthoring> formations)
        {
            var members = new List<SceneMember>();
            for (var formationIndex = 0; formationIndex < formations.Count; formationIndex++)
            {
                var formation = formations[formationIndex];
                if (formation == null || !formation.AutoFuseOverlappingMembers) continue;
                var formationMembers = CollectMembers(formation);
                for (var memberIndex = 0; memberIndex < formationMembers.Count; memberIndex++)
                {
                    members.Add(new SceneMember(formation, formationMembers[memberIndex]));
                }
            }

            return members;
        }

        private static List<List<SceneMember>> BuildCrossFormationClusters(
            IReadOnlyList<SceneMember> members)
        {
            var parent = new int[members.Count];
            for (var index = 0; index < parent.Length; index++) parent[index] = index;
            for (var first = 0; first < members.Count; first++)
            {
                for (var second = first + 1; second < members.Count; second++)
                {
                    if (members[first].Owner == members[second].Owner) continue;
                    if (MembersOverlap(members[first].Filter, members[first].Collider, members[second].Filter, members[second].Collider))
                    {
                        Union(parent, first, second);
                    }
                }
            }

            var clustersByRoot = new Dictionary<int, List<SceneMember>>();
            for (var index = 0; index < members.Count; index++)
            {
                var root = Find(parent, index);
                if (!clustersByRoot.TryGetValue(root, out var cluster))
                {
                    cluster = new List<SceneMember>();
                    clustersByRoot.Add(root, cluster);
                }

                cluster.Add(members[index]);
            }

            var clusters = new List<List<SceneMember>>();
            foreach (var cluster in clustersByRoot.Values)
            {
                var owners = new HashSet<TopDown3DRockFormationAuthoring>();
                for (var index = 0; index < cluster.Count; index++) owners.Add(cluster[index].Owner);
                if (owners.Count > 1) clusters.Add(cluster);
            }

            return clusters;
        }

        private static List<List<Member>> BuildOverlappingClusters(IReadOnlyList<Member> members)
        {
            var parent = new int[members.Count];
            for (var index = 0; index < parent.Length; index++) parent[index] = index;

            for (var first = 0; first < members.Count; first++)
            {
                for (var second = first + 1; second < members.Count; second++)
                {
                    if (MembersOverlap(members[first].Filter, members[first].Collider, members[second].Filter, members[second].Collider))
                    {
                        Union(parent, first, second);
                    }
                }
            }

            var clustersByRoot = new Dictionary<int, List<Member>>();
            for (var index = 0; index < members.Count; index++)
            {
                var root = Find(parent, index);
                if (!clustersByRoot.TryGetValue(root, out var cluster))
                {
                    cluster = new List<Member>();
                    clustersByRoot.Add(root, cluster);
                }

                cluster.Add(members[index]);
            }

            var clusters = new List<List<Member>>();
            foreach (var cluster in clustersByRoot.Values)
            {
                if (cluster.Count > 1) clusters.Add(cluster);
            }

            return clusters;
        }

        private static TopDown3DIndexedMeshData BuildSolid(MeshFilter filter, Transform fusedRoot)
        {
            var mesh = filter.sharedMesh;
            var matrix = fusedRoot.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            var vertices = mesh.vertices;
            for (var index = 0; index < vertices.Length; index++)
            {
                vertices[index] = matrix.MultiplyPoint3x4(vertices[index]);
            }

            var triangles = mesh.triangles;
            if (matrix.determinant < 0f)
            {
                for (var index = 0; index < triangles.Length; index += 3)
                {
                    var temporary = triangles[index + 1];
                    triangles[index + 1] = triangles[index + 2];
                    triangles[index + 2] = temporary;
                }
            }

            var solid = TopDown3DRockMeshTopology.Normalize(vertices, triangles);
            var report = TopDown3DRockMeshTopology.Validate(solid);
            if (!report.IsValid || report.SignedVolume <= 0.0)
            {
                throw new InvalidOperationException($"{filter.name} is not a valid rock volume: {report.Error}");
            }

            return solid;
        }

        private static string CreateFusedMeshAsset(
            TopDown3DRockFormationAuthoring authoring,
            Mesh mesh,
            int clusterIndex,
            ISet<string> usedAssetNames)
        {
            EnsureFolder("Assets/_Project/Art");
            EnsureFolder("Assets/_Project/Art/Generated");
            EnsureFolder(GeneratedMeshFolder);
            var rawName = authoring.name.Replace(' ', '_').Replace('—', '-');
            var basePath = $"{GeneratedMeshFolder}/{rawName}_Fused_{clusterIndex + 1}.asset";
            while (!usedAssetNames.Add(basePath))
            {
                basePath = $"{GeneratedMeshFolder}/{rawName}_Fused_{clusterIndex + 1}_{usedAssetNames.Count}.asset";
            }

            var assetPath = AssetDatabase.GenerateUniqueAssetPath(basePath);
            AssetDatabase.CreateAsset(mesh, assetPath);
            return assetPath;
        }

        private static bool MembersOverlap(
            MeshFilter firstFilter,
            Collider firstCollider,
            MeshFilter secondFilter,
            Collider secondCollider)
        {
            if (Physics.ComputePenetration(
                    firstCollider,
                    firstCollider.transform.position,
                    firstCollider.transform.rotation,
                    secondCollider,
                    secondCollider.transform.position,
                    secondCollider.transform.rotation,
                    out _,
                    out _))
            {
                return true;
            }

            // The supplied colliders are deliberately inexpensive authoring proxies.
            // Fall back to rendered bounds so visibly embedded rocks are considered for
            // a Boolean union even when those proxies do not intersect.
            var firstRenderer = firstFilter.GetComponent<MeshRenderer>();
            var secondRenderer = secondFilter.GetComponent<MeshRenderer>();
            return firstRenderer != null
                && secondRenderer != null
                && firstRenderer.bounds.Intersects(secondRenderer.bounds);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
            {
                throw new InvalidOperationException($"Invalid generated asset folder {path}.");
            }

            AssetDatabase.CreateFolder(parent, name);
        }

        private static GameObject GetOrCreateSceneFusedRoot(UnityEngine.SceneManagement.Scene scene)
        {
            var existing = FindSceneFusedRoot(scene);
            if (existing != null) return existing;
            EnsureFolder("Assets/_Project/Art");
            EnsureFolder("Assets/_Project/Art/Generated");
            EnsureFolder(GeneratedMeshFolder);
            var root = new GameObject(SceneFusedRootName);
            Undo.RegisterCreatedObjectUndo(root, "Fuse Overlapping Rock Formations");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            return root;
        }

        private static GameObject FindSceneFusedRoot(UnityEngine.SceneManagement.Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return null;
            var roots = scene.GetRootGameObjects();
            for (var index = 0; index < roots.Length; index++)
            {
                if (roots[index].name == SceneFusedRootName) return roots[index];
            }

            return null;
        }

        private static void RecordStatus(TopDown3DRockFormationAuthoring authoring, string status)
        {
            if (authoring == null) return;
            authoring.SetFusionStatus(status);
            EditorUtility.SetDirty(authoring);
        }

        private static void RecordStatus(IReadOnlyList<SceneMember> members, string status)
        {
            var recorded = new HashSet<TopDown3DRockFormationAuthoring>();
            for (var index = 0; index < members.Count; index++)
            {
                if (recorded.Add(members[index].Owner)) RecordStatus(members[index].Owner, status);
            }
        }

        private static int Find(IList<int> parent, int index)
        {
            while (parent[index] != index) index = parent[index];
            return index;
        }

        private static void Union(IList<int> parent, int first, int second)
        {
            first = Find(parent, first);
            second = Find(parent, second);
            if (first != second) parent[second] = first;
        }

        private readonly struct Member
        {
            internal Member(MeshFilter filter, Collider collider)
            {
                Filter = filter;
                Collider = collider;
            }

            internal MeshFilter Filter { get; }
            internal Collider Collider { get; }
        }

        private readonly struct SceneMember
        {
            internal SceneMember(TopDown3DRockFormationAuthoring owner, Member member)
            {
                Owner = owner;
                Filter = member.Filter;
                Collider = member.Collider;
            }

            internal TopDown3DRockFormationAuthoring Owner { get; }
            internal MeshFilter Filter { get; }
            internal Collider Collider { get; }
        }

        private readonly struct FusionResult
        {
            internal FusionResult(IReadOnlyList<Member> members, TopDown3DRockFormationMeshData meshData)
            {
                Members = members;
                MeshData = meshData;
                Material = members[0].Filter.GetComponent<MeshRenderer>().sharedMaterial;
            }

            internal IReadOnlyList<Member> Members { get; }
            internal TopDown3DRockFormationMeshData MeshData { get; }
            internal Material Material { get; }
        }

        private readonly struct SceneFusionResult
        {
            internal SceneFusionResult(IReadOnlyList<SceneMember> members, TopDown3DRockFormationMeshData meshData)
            {
                Members = members;
                MeshData = meshData;
                Material = members[0].Filter.GetComponent<MeshRenderer>().sharedMaterial;
            }

            internal IReadOnlyList<SceneMember> Members { get; }
            internal TopDown3DRockFormationMeshData MeshData { get; }
            internal Material Material { get; }
        }
    }
}
