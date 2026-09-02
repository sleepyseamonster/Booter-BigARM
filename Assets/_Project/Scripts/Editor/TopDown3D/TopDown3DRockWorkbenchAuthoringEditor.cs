using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using BooterBigArm.TopDown3D;

namespace BooterBigArm.Editor
{
    [CustomEditor(typeof(TopDown3DRockWorkbenchAuthoring))]
    public sealed class TopDown3DRockWorkbenchAuthoringEditor : UnityEditor.Editor
    {
        internal const string WorkbenchMaterialPath =
            "Assets/_Project/Materials/TopDown3D/RockWorkbench_NeutralPBR.mat";

        [MenuItem("GameObject/Booter & BigARM/Top Down 3D/Rock Workbench", false, 20)]
        private static void CreateWorkbench(MenuCommand command)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(WorkbenchMaterialPath);
            if (material == null)
            {
                throw new InvalidOperationException("The Rock Workbench PBR material could not be found.");
            }

            const string undoName = "Create Rock Workbench";
            Undo.SetCurrentGroupName(undoName);
            var undoGroup = Undo.GetCurrentGroup();
            try
            {
                var rootObject = new GameObject("Rock Workbench");
                Undo.RegisterCreatedObjectUndo(rootObject, undoName);
                GameObjectUtility.SetParentAndAlign(rootObject, command.context as GameObject);
                var authoring = Undo.AddComponent<TopDown3DRockWorkbenchAuthoring>(rootObject);
                authoring.Configure(material);
                rootObject.GetComponent<MeshRenderer>().sharedMaterial = material;

                AddVolume(authoring, false);
                Selection.activeGameObject = rootObject;
                TopDown3DRockWorkbenchPreview.RequestRebuild(authoring, true);
            }
            finally
            {
                Undo.CollapseUndoOperations(undoGroup);
            }
        }

        public override void OnInspectorGUI()
        {
            var authoring = (TopDown3DRockWorkbenchAuthoring)target;
            UpgradeLegacyMaterial(authoring);

            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            var settingsChanged = EditorGUI.EndChangeCheck();
            serializedObject.ApplyModifiedProperties();

            if (settingsChanged && authoring.AutoRebuild)
            {
                TopDown3DRockWorkbenchPreview.RequestRebuild(authoring, false);
                SceneView.RepaintAll();
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Each Cube Volume is an editable source, not a separate finished mesh. Move, rotate, and scale those child objects in the Scene view. The workbench remeshes their combined volume so overlapping cubes become one continuous surface. Its layered PBR surface assigns weathered top stone and directional wall strata by slope, then adds seeded smooth, grainy, shiny, cracked, mineral, and dusty regions without authored UVs.",
                MessageType.Info);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Base Rock Generator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Overall Size scales the rock and automatically adds masses as it grows. Verticality changes horizontal spread versus upward growth. Lopsidedness changes balanced versus one-sided growth. Compaction changes distinct lobes versus a dense body. Generator sliders affect the generated arrangement; click Apply Settings to Current Rock to compare the same seed. Every resulting cube remains editable.",
                MessageType.None);
            EditorGUILayout.LabelField("Automatic Cube Count", authoring.GeneratedCubeCount.ToString());
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Generate New Base Rock"))
                {
                    TopDown3DRockWorkbenchBaseRockGenerator.GenerateIntoWorkbench(
                        authoring,
                        TopDown3DRockWorkbenchBaseRockGenerator.CreateNewSeed(authoring.GenerationSeed));
                }
                if (GUILayout.Button("Apply Settings to Current Rock"))
                {
                    TopDown3DRockWorkbenchBaseRockGenerator.GenerateIntoWorkbench(
                        authoring,
                        authoring.GenerationSeed);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add Cube Volume")) AddVolume(authoring, true);
                if (GUILayout.Button("Rebuild Now"))
                {
                    TopDown3DRockWorkbenchPreview.RequestRebuild(authoring, true);
                }
            }

            if (GUILayout.Button("Clear Preview Mesh"))
            {
                TopDown3DRockWorkbenchPreview.ClearPreview(authoring, "Preview cleared. Source volumes were preserved.");
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Seed Variation Gallery", EditorStyles.boldLabel);
            if (TopDown3DRockWorkbenchVariationGallery.IsGalleryItem(authoring))
            {
                EditorGUILayout.HelpBox(
                    "This is one disposable gallery sample. Edit This Rock detaches it as a normal workbench without saving a mesh or prefab.",
                    MessageType.None);
                if (GUILayout.Button("Edit This Rock"))
                {
                    TopDown3DRockWorkbenchVariationGallery.DetachForEditing(authoring);
                }
                if (GUILayout.Button("Clear Remaining Gallery"))
                {
                    TopDown3DRockWorkbenchVariationGallery.ClearGallery();
                    GUIUtility.ExitGUI();
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Generate twelve temporary rocks with identical controls and different deterministic seeds. The gallery is not saved into the scene and creates no mesh assets or prefabs.",
                    MessageType.None);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Generate Seed Gallery"))
                    {
                        TopDown3DRockWorkbenchVariationGallery.CreateOrReplaceGallery(
                            authoring,
                            authoring.GenerationSeed);
                    }
                    using (new EditorGUI.DisabledScope(
                               !TopDown3DRockWorkbenchVariationGallery.GalleryExists))
                    {
                        if (GUILayout.Button("Reroll Gallery"))
                        {
                            TopDown3DRockWorkbenchVariationGallery.CreateOrReplaceGallery(
                                authoring,
                                TopDown3DRockWorkbenchBaseRockGenerator.CreateNewSeed(
                                    authoring.GenerationSeed));
                        }
                    }
                }
                using (new EditorGUI.DisabledScope(
                           !TopDown3DRockWorkbenchVariationGallery.GalleryExists))
                {
                    if (GUILayout.Button("Clear Gallery"))
                    {
                        TopDown3DRockWorkbenchVariationGallery.ClearGallery();
                    }
                }
            }

            EditorGUILayout.Space();
            var statusType = authoring.PreviewStatus.IndexOf("invalid", StringComparison.OrdinalIgnoreCase) >= 0
                || authoring.PreviewStatus.IndexOf("could not", StringComparison.OrdinalIgnoreCase) >= 0
                ? MessageType.Error
                : authoring.PreviewStatus.IndexOf("disconnected", StringComparison.OrdinalIgnoreCase) >= 0
                    ? MessageType.Warning
                    : MessageType.None;
            EditorGUILayout.HelpBox(authoring.PreviewStatus, statusType);
        }

        private static void UpgradeLegacyMaterial(TopDown3DRockWorkbenchAuthoring authoring)
        {
            if (authoring == null) return;

            var current = authoring.RockMaterial;
            var currentPath = current == null ? string.Empty : AssetDatabase.GetAssetPath(current);
            if (current != null
                && !string.Equals(currentPath, TopDown3DPrototypeBuilder.RockMaterialPath, StringComparison.Ordinal))
            {
                return;
            }

            var improved = AssetDatabase.LoadAssetAtPath<Material>(WorkbenchMaterialPath);
            if (improved == null || improved == current) return;

            Undo.RecordObject(authoring, "Upgrade Rock Workbench Surface");
            authoring.Configure(improved);
            var renderer = authoring.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                Undo.RecordObject(renderer, "Upgrade Rock Workbench Surface");
                renderer.sharedMaterial = improved;
                EditorUtility.SetDirty(renderer);
            }
            EditorUtility.SetDirty(authoring);
            TopDown3DRockWorkbenchPreview.RequestRebuild(authoring, true);
            SceneView.RepaintAll();
        }

        internal static void AddVolume(TopDown3DRockWorkbenchAuthoring authoring, bool selectVolume)
        {
            if (authoring == null) return;

            var existing = authoring.GetComponentsInChildren<TopDown3DRockVolumeNode>(true);
            var volumeObject = new GameObject($"Cube Volume {existing.Length + 1}");
            Undo.RegisterCreatedObjectUndo(volumeObject, "Add Rock Cube Volume");
            Undo.SetTransformParent(volumeObject.transform, authoring.transform, "Parent Rock Cube Volume");
            volumeObject.transform.localPosition = new Vector3(existing.Length * 1.2f, 0f, 0f);
            volumeObject.transform.localRotation = Quaternion.identity;
            volumeObject.transform.localScale = new Vector3(2f, 2f, 2f);
            var node = Undo.AddComponent<TopDown3DRockVolumeNode>(volumeObject);
            node.SetShapeSeed(TopDown3DRockWorkbenchBaseRockGenerator.DeriveVolumeShapeSeed(
                authoring.GenerationSeed,
                existing.Length));
            EditorUtility.SetDirty(node);
            if (selectVolume) Selection.activeGameObject = volumeObject;
            TopDown3DRockWorkbenchPreview.RequestRebuild(authoring, true);
        }

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected | GizmoType.Pickable)]
        private static void DrawVolumeGizmo(TopDown3DRockVolumeNode node, GizmoType gizmoType)
        {
            if (node == null) return;
            var workbench = node.GetComponentInParent<TopDown3DRockWorkbenchAuthoring>();
            if (workbench == null || !workbench.ShowSourceVolumes) return;

            var selected = (gizmoType & GizmoType.Selected) != 0;
            Gizmos.color = node.ContributesToRock
                ? selected ? new Color(0.3f, 0.95f, 1f, 1f) : new Color(0.2f, 0.75f, 1f, 0.75f)
                : new Color(0.5f, 0.5f, 0.5f, 0.55f);
            Gizmos.matrix = node.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
            Gizmos.matrix = Matrix4x4.identity;
        }

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected)]
        private static void DrawGallerySeedLabel(
            TopDown3DRockWorkbenchAuthoring authoring,
            GizmoType gizmoType)
        {
            if (!TopDown3DRockWorkbenchVariationGallery.IsGalleryItem(authoring)) return;

            var labelPosition = authoring.transform.position
                + Vector3.up * Mathf.Max(1f, authoring.GeneratedOverallScale * 0.65f);
            Handles.Label(labelPosition, $"Seed {authoring.GenerationSeed}", EditorStyles.boldLabel);
        }
    }

    internal static class TopDown3DRockWorkbenchVariationGallery
    {
        internal const int VariationCount = 12;
        internal const int ColumnCount = 4;
        private const string GalleryRootPrefix = "Rock Seed Variation Gallery — Base ";

        internal static bool GalleryExists => FindGalleryRoot() != null;

        internal static int DeriveSeed(int baseSeed, int index)
        {
            if (index <= 0) return baseSeed;

            unchecked
            {
                var value = (uint)baseSeed + 0x9E3779B9u * (uint)index;
                value ^= value >> 16;
                value *= 0x7FEB352Du;
                value ^= value >> 15;
                value *= 0x846CA68Bu;
                value ^= value >> 16;
                return (int)value;
            }
        }

        internal static GameObject CreateOrReplaceGallery(
            TopDown3DRockWorkbenchAuthoring source,
            int baseSeed)
        {
            if (source == null || IsGalleryItem(source)) return null;

            const string undoName = "Generate Rock Seed Gallery";
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(undoName);
            var undoGroup = Undo.GetCurrentGroup();
            try
            {
                ClearGalleryInternal();

                var root = new GameObject($"{GalleryRootPrefix}{baseSeed}")
                {
                    hideFlags = HideFlags.DontSaveInEditor
                };
                Undo.RegisterCreatedObjectUndo(root, undoName);
                SceneManager.MoveGameObjectToScene(root, source.gameObject.scene);

                var spacing = Mathf.Max(3f, source.GeneratedOverallScale * 1.65f);
                root.transform.position = source.transform.position
                    + Vector3.right * spacing * 3.25f;

                var rowCount = Mathf.CeilToInt(VariationCount / (float)ColumnCount);
                for (var index = 0; index < VariationCount; index++)
                {
                    var seed = DeriveSeed(baseSeed, index);
                    var column = index % ColumnCount;
                    var row = index / ColumnCount;
                    var itemObject = new GameObject($"Rock {index + 1:00} — Seed {seed}");
                    Undo.RegisterCreatedObjectUndo(itemObject, undoName);
                    Undo.SetTransformParent(itemObject.transform, root.transform, undoName);
                    itemObject.transform.localPosition = new Vector3(
                        (column - (ColumnCount - 1) * 0.5f) * spacing,
                        0f,
                        (row - (rowCount - 1) * 0.5f) * spacing);
                    itemObject.transform.rotation = source.transform.rotation;

                    var item = Undo.AddComponent<TopDown3DRockWorkbenchAuthoring>(itemObject);
                    EditorUtility.CopySerialized(source, item);
                    ConfigureGalleryItem(item, false);
                    var renderer = itemObject.GetComponent<MeshRenderer>();
                    if (renderer != null) renderer.sharedMaterial = item.RockMaterial;
                    TopDown3DRockWorkbenchBaseRockGenerator.GenerateIntoWorkbench(item, seed);
                    SetHierarchyTemporary(itemObject, true);
                }

                Selection.activeGameObject = source.gameObject;
                SceneView.RepaintAll();
                return root;
            }
            finally
            {
                Undo.CollapseUndoOperations(undoGroup);
            }
        }

        internal static bool IsGalleryItem(TopDown3DRockWorkbenchAuthoring authoring)
        {
            return authoring != null
                && authoring.transform.parent != null
                && IsGalleryRoot(authoring.transform.parent.gameObject);
        }

        internal static void DetachForEditing(TopDown3DRockWorkbenchAuthoring authoring)
        {
            if (!IsGalleryItem(authoring)) return;

            const string undoName = "Edit Gallery Rock";
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(undoName);
            var undoGroup = Undo.GetCurrentGroup();
            try
            {
                var galleryRoot = authoring.transform.parent;
                Undo.SetTransformParent(authoring.transform, null, undoName);
                SetHierarchyTemporary(authoring.gameObject, false);
                Undo.RecordObject(authoring.gameObject, undoName);
                authoring.gameObject.name = $"Rock Workbench — Seed {authoring.GenerationSeed}";
                ConfigureGalleryItem(authoring, true);
                TopDown3DRockWorkbenchPreview.RequestRebuild(authoring, true);

                if (galleryRoot != null && galleryRoot.childCount == 0)
                {
                    Undo.DestroyObjectImmediate(galleryRoot.gameObject);
                }

                Selection.activeGameObject = authoring.gameObject;
                SceneView.RepaintAll();
            }
            finally
            {
                Undo.CollapseUndoOperations(undoGroup);
            }
        }

        internal static void ClearGallery()
        {
            const string undoName = "Clear Rock Seed Gallery";
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(undoName);
            var undoGroup = Undo.GetCurrentGroup();
            try
            {
                ClearGalleryInternal();
                SceneView.RepaintAll();
            }
            finally
            {
                Undo.CollapseUndoOperations(undoGroup);
            }
        }

        internal static GameObject FindGalleryRoot()
        {
            foreach (var candidate in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (candidate != null
                    && candidate.scene.IsValid()
                    && candidate.scene.isLoaded
                    && IsGalleryRoot(candidate))
                {
                    return candidate;
                }
            }
            return null;
        }

        private static void ClearGalleryInternal()
        {
            var root = FindGalleryRoot();
            if (root == null) return;

            foreach (var authoring in root.GetComponentsInChildren<TopDown3DRockWorkbenchAuthoring>(true))
            {
                TopDown3DRockWorkbenchPreview.ClearPreview(
                    authoring,
                    "Temporary gallery preview cleared.");
            }
            Undo.DestroyObjectImmediate(root);
        }

        private static bool IsGalleryRoot(GameObject candidate)
        {
            return candidate != null
                && candidate.name.StartsWith(GalleryRootPrefix, StringComparison.Ordinal)
                && (candidate.hideFlags & HideFlags.DontSaveInEditor) != 0;
        }

        private static void ConfigureGalleryItem(
            TopDown3DRockWorkbenchAuthoring authoring,
            bool editable)
        {
            var serialized = new SerializedObject(authoring);
            serialized.FindProperty("autoRebuild").boolValue = editable;
            serialized.FindProperty("showSourceVolumes").boolValue = editable;
            serialized.FindProperty("updateCollider").boolValue = editable;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(authoring);

            var collider = authoring.GetComponent<MeshCollider>();
            if (collider != null && !editable) collider.sharedMesh = null;
        }

        private static void SetHierarchyTemporary(GameObject root, bool temporary)
        {
            if (root == null) return;

            foreach (var item in root.GetComponentsInChildren<Transform>(true))
            {
                item.gameObject.hideFlags = temporary
                    ? item.gameObject.hideFlags | HideFlags.DontSaveInEditor
                    : item.gameObject.hideFlags & ~HideFlags.DontSaveInEditor;
            }
        }
    }

    [CustomEditor(typeof(TopDown3DRockVolumeNode))]
    public sealed class TopDown3DRockVolumeNodeEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var node = (TopDown3DRockVolumeNode)target;
            var workbench = node.GetComponentInParent<TopDown3DRockWorkbenchAuthoring>();
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Use this object's Transform to shape the source cube. The rendered rock belongs to the parent Rock Workbench.",
                MessageType.Info);
            using (new EditorGUI.DisabledScope(workbench == null))
            {
                if (GUILayout.Button("Add Another Cube Volume"))
                {
                    TopDown3DRockWorkbenchAuthoringEditor.AddVolume(workbench, true);
                }
                if (GUILayout.Button("Select Rock Workbench")) Selection.activeGameObject = workbench.gameObject;
            }
        }
    }
}
