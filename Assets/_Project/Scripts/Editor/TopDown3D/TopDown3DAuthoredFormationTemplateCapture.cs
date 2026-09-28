using System;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BooterBigArm.Editor
{
    /// <summary>Promotes a selected scene composition into a stable reusable source prefab.</summary>
    internal static class TopDown3DAuthoredFormationTemplateCapture
    {
        internal const string HandbuiltSpireReferencePath =
            "Assets/_Project/Art/Environment/Rocks/Source/HandbuiltSpireReference.prefab";
        internal const string HandbuiltSpireMeshesPath =
            "Assets/_Project/Art/Environment/Rocks/Source/HandbuiltSpireReferenceMeshes.asset";

        [MenuItem("Booter & BigARM/Capture Selected Formation Template", false, 5)]
        internal static void CaptureSelected()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Capture formation templates outside Play Mode.");
            var sourceRoot = FindSourceRoot(Selection.activeGameObject);
            if (sourceRoot == null)
                throw new InvalidOperationException("Select the hand-built formation root or one of its rocks.");
            Capture(sourceRoot, HandbuiltSpireReferencePath, HandbuiltSpireMeshesPath);
        }

        internal static GameObject Capture(GameObject sourceRoot, string prefabPath, string meshAssetPath)
        {
            if (sourceRoot == null || !sourceRoot.scene.IsValid())
                throw new InvalidOperationException("Capture requires a formation root in a saved scene.");
            if (sourceRoot.transform.rotation != Quaternion.identity
                || (sourceRoot.transform.lossyScale - Vector3.one).sqrMagnitude > 0.000001f)
            {
                throw new InvalidOperationException(
                    "The formation root must have identity rotation and scale; rotate and scale its rocks instead.");
            }
            var sourceRocks = sourceRoot.GetComponentsInChildren<TopDown3DRockWorkbenchAuthoring>(true);
            if (sourceRocks.Length == 0)
                throw new InvalidOperationException("The selected formation has no rock workbench members.");
            foreach (var sourceRock in sourceRocks)
            {
                var mesh = sourceRock.GetComponent<MeshFilter>()?.sharedMesh;
                var material = sourceRock.GetComponent<MeshRenderer>()?.sharedMaterial;
                if (mesh == null || material == null)
                    throw new InvalidOperationException($"{sourceRock.name} needs a generated mesh and material.");
            }

            var staging = UnityEngine.Object.Instantiate(sourceRoot);
            staging.name = System.IO.Path.GetFileNameWithoutExtension(prefabPath);
            try
            {
                NormalizePivot(staging);
                var rocks = staging.GetComponentsInChildren<TopDown3DRockWorkbenchAuthoring>(true);
                for (var i = 0; i < rocks.Length; i++)
                {
                    var filter = rocks[i].GetComponent<MeshFilter>();
                    var savedMesh = UnityEngine.Object.Instantiate(filter.sharedMesh);
                    savedMesh.name = $"{staging.name} Source {i:00}";
                    savedMesh.hideFlags = HideFlags.None;
                    savedMesh = TopDown3DProductionRockBaker.UpsertMesh(meshAssetPath, savedMesh);
                    filter.sharedMesh = savedMesh;
                    var collider = rocks[i].GetComponent<MeshCollider>();
                    if (collider != null) collider.sharedMesh = savedMesh;
                    var serialized = new SerializedObject(rocks[i]);
                    serialized.FindProperty("autoRebuild").boolValue = false;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                AssetDatabase.SaveAssets();
                var prefab = PrefabUtility.SaveAsPrefabAsset(staging, prefabPath);
                if (prefab == null) throw new InvalidOperationException("Unity did not save the formation prefab.");
                TopDown3DMixedFormationAssetBaker.Bake(prefab);
                ConfigureWorkbench(prefab);
                AssetDatabase.SaveAssets();
                Debug.Log($"Captured formation template with {rocks.Length} members: {prefabPath}");
                return prefab;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(staging);
            }
        }

        private static GameObject FindSourceRoot(GameObject selected)
        {
            if (selected == null) return null;
            var root = selected.transform;
            while (root.parent != null
                && root.parent.GetComponent<TopDown3DLandscapeAuthoringSandbox>() == null)
            {
                root = root.parent;
            }
            return root.GetComponentsInChildren<TopDown3DRockWorkbenchAuthoring>(true).Length > 0
                ? root.gameObject
                : null;
        }

        private static void NormalizePivot(GameObject staging)
        {
            var renderers = staging.GetComponentsInChildren<MeshRenderer>(true)
                .Where(renderer => renderer.sharedMaterial != null && renderer.enabled)
                .ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException("The formation has no visible rock renderers.");
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            var worldPivot = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            var localPivot = staging.transform.InverseTransformPoint(worldPivot);
            for (var i = 0; i < staging.transform.childCount; i++)
                staging.transform.GetChild(i).localPosition -= localPivot;
            staging.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            staging.transform.localScale = Vector3.one;
        }

        private static void ConfigureWorkbench(GameObject prefab)
        {
            var sandboxes = UnityEngine.Object.FindObjectsByType<TopDown3DLandscapeAuthoringSandbox>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            var stage = sandboxes.FirstOrDefault(candidate => candidate.RockReference != null);
            if (stage == null) return;
            Undo.RecordObject(stage, "Select Captured Formation Template");
            stage.ConfigureRockReference(prefab);
            var serialized = new SerializedObject(stage);
            serialized.FindProperty("variationEnabled").boolValue = false;
            serialized.FindProperty("variationSeed").intValue = 0;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(stage);
            TopDown3DLandscapeAuthoringSandboxEditor.BuildTerrainContext(stage);
            EditorSceneManager.MarkSceneDirty(stage.gameObject.scene);
            Selection.activeGameObject = stage.gameObject;
        }
    }
}
