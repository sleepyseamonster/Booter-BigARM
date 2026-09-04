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
        private bool showAdvanced;

        [MenuItem("GameObject/Booter & BigARM/Top Down 3D/New Random Rock", false, 20)]
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

                TopDown3DRockWorkbenchBaseRockGenerator.GenerateIntoWorkbench(
                    authoring,
                    TopDown3DRockWorkbenchBaseRockGenerator.CreateNewSeed(
                        authoring.GenerationSeed));
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
            EditorGUILayout.LabelField("Rock Generator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Choose the broad shape, then click Generate New Rock. Every result stays editable in the Scene.",
                MessageType.Info);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("generatedOverallScale"),
                new GUIContent("Width", "Physical width and depth in meters."));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("generatedHeight"),
                new GUIContent("Height", "Physical vertical height in meters, independent from width."));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("generatedAsymmetry"),
                new GUIContent("Lopsidedness"));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("generatedOverlap"),
                new GUIContent("Compaction"));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("showSourceVolumes"),
                new GUIContent("Show Editing Volumes"));
            var settingsChanged = EditorGUI.EndChangeCheck();
            serializedObject.ApplyModifiedProperties();

            if (settingsChanged && authoring.AutoRebuild)
            {
                TopDown3DRockWorkbenchPreview.RequestRebuild(authoring, false);
                SceneView.RepaintAll();
            }

            EditorGUILayout.LabelField(
                $"Uses {authoring.GeneratedCubeCount} editable source masses",
                EditorStyles.miniLabel);
            if (GUILayout.Button("Generate New Rock", GUILayout.Height(34f)))
            {
                TopDown3DRockWorkbenchBaseRockGenerator.GenerateIntoWorkbench(
                    authoring,
                    TopDown3DRockWorkbenchBaseRockGenerator.CreateNewSeed(authoring.GenerationSeed));
            }
            if (GUILayout.Button("Update Current Rock With These Settings"))
            {
                TopDown3DRockWorkbenchBaseRockGenerator.GenerateIntoWorkbench(
                    authoring,
                    authoring.GenerationSeed);
            }

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(
                       !TopDown3DRockWorkbenchFormationEditor.CanCreateFromSelection))
            {
                if (GUILayout.Button("Create Formation From Selected Rocks"))
                {
                    TopDown3DRockWorkbenchFormationEditor.CreateFromSelectedRocks();
                    GUIUtility.ExitGUI();
                }
            }
            EditorGUILayout.LabelField(
                "Select two or more rocks to enable formation creation.",
                EditorStyles.miniLabel);

            EditorGUILayout.Space();
            var statusType = authoring.PreviewStatus.IndexOf("invalid", StringComparison.OrdinalIgnoreCase) >= 0
                || authoring.PreviewStatus.IndexOf("could not", StringComparison.OrdinalIgnoreCase) >= 0
                ? MessageType.Error
                : authoring.PreviewStatus.IndexOf("disconnected", StringComparison.OrdinalIgnoreCase) >= 0
                    ? MessageType.Warning
                    : MessageType.None;
            EditorGUILayout.HelpBox(authoring.PreviewStatus, statusType);

            showAdvanced = EditorGUILayout.Foldout(
                showAdvanced,
                "Advanced",
                true,
                EditorStyles.foldoutHeader);
            if (showAdvanced) DrawAdvancedControls(authoring);
        }

        private void DrawAdvancedControls(TopDown3DRockWorkbenchAuthoring authoring)
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("Mesh Preview", EditorStyles.boldLabel);
            DrawProperty("rockMaterial");
            DrawTessellationDetail(serializedObject.FindProperty("voxelSize"));
            DrawProperty("fusionSmoothness");
            DrawProperty("surfaceRelaxation");
            DrawProperty("autoRebuild");
            DrawProperty("updateCollider");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Surface", EditorStyles.boldLabel);
            DrawProperty("geologyScale");
            DrawProperty("surfaceVariation");
            DrawProperty("crackAmount");
            DrawProperty("sideGrit");
            DrawProperty("undersideShale");
            DrawProperty("sideShalePatches");
            DrawProperty("topShalePatches");
            DrawProperty("wornShine");
            DrawProperty("generationSeed");
            var changed = EditorGUI.EndChangeCheck();
            serializedObject.ApplyModifiedProperties();
            if (changed && authoring.AutoRebuild)
            {
                TopDown3DRockWorkbenchPreview.RequestRebuild(authoring, false);
            }

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add Editing Volume")) AddVolume(authoring, true);
                if (GUILayout.Button("Rebuild Mesh"))
                    TopDown3DRockWorkbenchPreview.RequestRebuild(authoring, true);
            }
            if (GUILayout.Button("Clear Preview Mesh"))
            {
                TopDown3DRockWorkbenchPreview.ClearPreview(
                    authoring,
                    "Preview cleared. Source volumes were preserved.");
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Seed Gallery", EditorStyles.boldLabel);
            if (TopDown3DRockWorkbenchVariationGallery.IsGalleryItem(authoring))
            {
                if (GUILayout.Button("Edit This Gallery Rock"))
                    TopDown3DRockWorkbenchVariationGallery.DetachForEditing(authoring);
                if (GUILayout.Button("Clear Remaining Gallery"))
                {
                    TopDown3DRockWorkbenchVariationGallery.ClearGallery();
                    GUIUtility.ExitGUI();
                }
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Generate Gallery"))
                {
                    TopDown3DRockWorkbenchVariationGallery.CreateOrReplaceGallery(
                        authoring,
                        authoring.GenerationSeed);
                }
                using (new EditorGUI.DisabledScope(
                           !TopDown3DRockWorkbenchVariationGallery.GalleryExists))
                {
                    if (GUILayout.Button("Clear Gallery"))
                        TopDown3DRockWorkbenchVariationGallery.ClearGallery();
                }
            }
        }

        private static void DrawTessellationDetail(SerializedProperty voxelSize)
        {
            var detail = TopDown3DRockWorkbenchAuthoring.VoxelSizeToTessellationDetail(
                voxelSize.floatValue);
            EditorGUI.BeginChangeCheck();
            var adjustedDetail = EditorGUILayout.Slider(
                new GUIContent(
                    "Tessellation Detail",
                    "Controls surface sampling detail. 1 rebuilds fastest; 5 produces the finest silhouette."),
                detail,
                TopDown3DRockWorkbenchAuthoring.MinimumTessellationDetail,
                TopDown3DRockWorkbenchAuthoring.MaximumTessellationDetail);
            if (EditorGUI.EndChangeCheck())
            {
                voxelSize.floatValue = TopDown3DRockWorkbenchAuthoring.TessellationDetailToVoxelSize(
                    adjustedDetail);
            }
        }

        private void DrawProperty(string name)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty(name));
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
            var volumeObject = new GameObject($"Weathered Block Volume {existing.Length + 1}");
            Undo.RegisterCreatedObjectUndo(volumeObject, "Add Rock Source Volume");
            Undo.SetTransformParent(volumeObject.transform, authoring.transform, "Parent Rock Source Volume");
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
            switch (node.SourceShape)
            {
                case TopDown3DRockSourceShape.Wedge:
                    DrawWedgeGizmo();
                    break;
                case TopDown3DRockSourceShape.TaperedStone:
                    DrawTaperedStoneGizmo();
                    break;
                default:
                    Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
                    break;
            }
            Gizmos.matrix = Matrix4x4.identity;
        }

        private static void DrawWedgeGizmo()
        {
            var backBottomLeft = new Vector3(-0.5f, -0.5f, -0.5f);
            var backBottomRight = new Vector3(0.5f, -0.5f, -0.5f);
            var backTopLeft = new Vector3(-0.5f, 0.5f, -0.5f);
            var frontBottomLeft = new Vector3(-0.5f, -0.5f, 0.5f);
            var frontBottomRight = new Vector3(0.5f, -0.5f, 0.5f);
            var frontTopLeft = new Vector3(-0.5f, 0.5f, 0.5f);
            DrawTriangle(backBottomLeft, backBottomRight, backTopLeft);
            DrawTriangle(frontBottomLeft, frontBottomRight, frontTopLeft);
            Gizmos.DrawLine(backBottomLeft, frontBottomLeft);
            Gizmos.DrawLine(backBottomRight, frontBottomRight);
            Gizmos.DrawLine(backTopLeft, frontTopLeft);
        }

        private static void DrawTriangle(Vector3 first, Vector3 second, Vector3 third)
        {
            Gizmos.DrawLine(first, second);
            Gizmos.DrawLine(second, third);
            Gizmos.DrawLine(third, first);
        }

        private static void DrawTaperedStoneGizmo()
        {
            const float topHalfExtent = 0.2f;
            var bottom = new[]
            {
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, -0.5f, 0.5f),
                new Vector3(-0.5f, -0.5f, 0.5f)
            };
            var top = new[]
            {
                new Vector3(-topHalfExtent, 0.5f, -topHalfExtent),
                new Vector3(topHalfExtent, 0.5f, -topHalfExtent),
                new Vector3(topHalfExtent, 0.5f, topHalfExtent),
                new Vector3(-topHalfExtent, 0.5f, topHalfExtent)
            };
            for (var index = 0; index < 4; index++)
            {
                var next = (index + 1) % 4;
                Gizmos.DrawLine(bottom[index], bottom[next]);
                Gizmos.DrawLine(top[index], top[next]);
                Gizmos.DrawLine(bottom[index], top[index]);
            }
        }

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected)]
        private static void DrawGallerySeedLabel(
            TopDown3DRockWorkbenchAuthoring authoring,
            GizmoType gizmoType)
        {
            if (!TopDown3DRockWorkbenchVariationGallery.IsGalleryItem(authoring)) return;

            var labelPosition = authoring.transform.position
                + Vector3.up * Mathf.Max(1f, authoring.GeneratedHeight * 0.65f);
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
                "Choose a source shape, then use this object's Transform to position, rotate, and scale it. The rendered rock belongs to the parent Rock Workbench.",
                MessageType.Info);
            using (new EditorGUI.DisabledScope(workbench == null))
            {
                if (GUILayout.Button("Add Another Source Volume"))
                {
                    TopDown3DRockWorkbenchAuthoringEditor.AddVolume(workbench, true);
                }
                if (GUILayout.Button("Select Rock Workbench")) Selection.activeGameObject = workbench.gameObject;
            }
        }
    }
}
