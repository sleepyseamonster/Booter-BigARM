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

        [MenuItem("Booter & BigARM/Create Rock Workbench", false, 1)]
        private static void CreateWorkbenchFromMainMenu()
        {
            CreateWorkbench(new MenuCommand(null));
        }

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
                authoring.ConfigureGoldenRockDefaults();
                rootObject.GetComponent<MeshRenderer>().sharedMaterial = material;

                TopDown3DRockWorkbenchBaseRockGenerator.GenerateNewIntoWorkbench(authoring);
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
            EditorGUILayout.LabelField("Rock Workbench", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                authoring.IsFormationMember
                    ? "Set the physical size and broad shape, then generate. The project rock material, mesh, "
                      + "and collider update automatically."
                    : "Generate New Rock creates a seeded size, shape, facing, and ground depth. Edit the Body "
                      + "values when you want a specific size, then use Regenerate This Rock. Expand 'Rock Shape "
                      + "(Edit These)' only when you want to shape the rock by hand.",
                MessageType.Info);
            if (authoring.NeedsStandalonePhysicalScaleUpgrade)
            {
                EditorGUILayout.HelpBox(
                    "This is an older standalone rock. Generate or regenerate it once to preserve its accepted size "
                    + "with a clean (1, 1, 1) root scale.",
                    MessageType.Warning);
            }
            if (IsHiddenInScene(authoring))
            {
                EditorGUILayout.HelpBox(
                    "This Rock Workbench is hidden by Unity's Scene Visibility control. "
                    + "Its generated mesh still exists, so Unity may show only the selection outline.",
                    MessageType.Warning);
                if (GUILayout.Button("Show Rock In Scene"))
                {
                    ShowInScene(authoring);
                }
            }
            EditorGUI.BeginChangeCheck();
            if (authoring.IsFormationMember)
            {
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("generatedOverallScale"),
                    new GUIContent("Width", "Physical width and depth in meters."));
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("generatedHeight"),
                    new GUIContent("Height", "Physical vertical height in meters, independent from width."));
            }
            else
            {
                DrawPhysicalDimension(
                    serializedObject.FindProperty("generatedOverallScale"),
                    authoring,
                    new GUIContent(
                        "Body Width",
                        "Width of the source body in meters before the automatic resting pose."),
                    TopDown3DRockWorkbenchAuthoring.MinimumStandaloneRockWidth,
                    TopDown3DRockWorkbenchAuthoring.MaximumStandaloneRockWidth);
                DrawPhysicalDimension(
                    serializedObject.FindProperty("generatedHeight"),
                    authoring,
                    new GUIContent(
                        "Body Length",
                        "Long dimension of the source body in meters before the automatic resting pose."),
                    TopDown3DRockWorkbenchAuthoring.MinimumStandaloneRockHeight,
                    TopDown3DRockWorkbenchAuthoring.MaximumStandaloneRockHeight);
            }
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("generatedAsymmetry"),
                new GUIContent("Lopsidedness"));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("generatedOverlap"),
                new GUIContent("Compaction"));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("generatedMajorFractures"),
                new GUIContent("Major Fractures"));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("generatedEdgeDamage"),
                new GUIContent("Edge Damage"));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("showSourceVolumes"),
                new GUIContent("Show Editing Volumes"));
            if (!authoring.IsFormationMember)
            {
                EditorGUILayout.LabelField(
                    "New Rock Size",
                    "Bell curve — 0.2 to 1.2 m per dimension",
                    EditorStyles.miniLabel);
                EditorGUILayout.LabelField(
                    "Resting Pose",
                    "Automatic — Seeded turn & ground depth",
                    EditorStyles.miniLabel);
            }
            var settingsChanged = EditorGUI.EndChangeCheck();
            serializedObject.ApplyModifiedProperties();

            if (settingsChanged && authoring.AutoRebuild)
            {
                TopDown3DRockWorkbenchPreview.RequestRebuild(authoring, false);
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Generate New Rock", GUILayout.Height(34f)))
            {
                TopDown3DRockWorkbenchBaseRockGenerator.GenerateNewIntoWorkbench(authoring);
            }
            if (GUILayout.Button("Regenerate This Rock"))
            {
                TopDown3DRockWorkbenchBaseRockGenerator.GenerateIntoWorkbench(
                    authoring,
                    authoring.GenerationSeed);
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Use This Rock Style In World Creator", GUILayout.Height(28f)))
            {
                if (EditorUtility.DisplayDialog(
                        "Use This Rock Style In World Creator",
                        "Capture this Workbench as the approved source, build three deterministic Boulder variants, "
                        + "and update only those slots in the existing World Creator catalog?",
                        "Build Approved Rocks",
                        "Cancel"))
                {
                    TopDown3DProductionRockBaker.BakeApprovedFamily(authoring);
                }
            }
            EditorGUILayout.LabelField(
                "This leaves world placement, streaming, and saved-object identity unchanged.",
                EditorStyles.miniLabel);

            EditorGUILayout.Space();
            var statusType = GetStatusType(authoring.PreviewStatus);
            EditorGUILayout.HelpBox(
                statusType == MessageType.Info && authoring.GeneratedMesh != null
                    ? "Rock ready. The visible mesh, collider, and project rock material are current."
                    : authoring.PreviewStatus,
                statusType);

            showAdvanced = EditorGUILayout.Foldout(
                showAdvanced,
                "Advanced",
                true,
                EditorStyles.foldoutHeader);
            if (showAdvanced) DrawAdvancedControls(authoring);
        }

        private static void DrawPhysicalDimension(
            SerializedProperty property,
            TopDown3DRockWorkbenchAuthoring authoring,
            GUIContent label,
            float minimum,
            float maximum)
        {
            var factor = authoring.PendingStandalonePhysicalScaleFactor;
            var valueMeters = Mathf.Clamp(property.floatValue * factor, minimum, maximum);
            var adjustedMeters = EditorGUILayout.Slider(label, valueMeters, minimum, maximum);
            if (!Mathf.Approximately(adjustedMeters, valueMeters))
                property.floatValue = adjustedMeters / factor;
        }

        private static MessageType GetStatusType(string status)
        {
            if (string.IsNullOrWhiteSpace(status)) return MessageType.Info;
            if (status.IndexOf("invalid", StringComparison.OrdinalIgnoreCase) >= 0
                || status.IndexOf("could not", StringComparison.OrdinalIgnoreCase) >= 0
                || status.IndexOf("failed", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return MessageType.Error;
            }

            return status.IndexOf("disconnected", StringComparison.OrdinalIgnoreCase) >= 0
                ? MessageType.Warning
                : MessageType.Info;
        }

        internal static bool IsHiddenInScene(TopDown3DRockWorkbenchAuthoring authoring)
        {
            return authoring != null
                && SceneVisibilityManager.instance.IsHidden(authoring.gameObject);
        }

        internal static void ShowInScene(TopDown3DRockWorkbenchAuthoring authoring)
        {
            if (authoring == null) return;
            SceneVisibilityManager.instance.Show(authoring.gameObject, true);
            SceneView.RepaintAll();
        }

        private void DrawAdvancedControls(TopDown3DRockWorkbenchAuthoring authoring)
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("Mesh Preview", EditorStyles.boldLabel);
            DrawProperty("rockMaterial");
            DrawTessellationDetail(serializedObject.FindProperty("voxelSize"), authoring);
            DrawPhysicalFusionSmoothness(
                serializedObject.FindProperty("fusionSmoothness"),
                authoring);
            DrawProperty("surfaceRelaxation");
            DrawProperty("autoRebuild");
            DrawProperty("updateCollider");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Surface", EditorStyles.boldLabel);
            DrawProperty("surfacePreset");
            DrawProperty("colorVariation");
            DrawProperty("environmentDustColor");
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

            EditorGUILayout.LabelField(
                $"Shape recipe: {authoring.GeneratedCubeCount} stone masses + "
                + $"{authoring.GeneratedFractureCount} fracture cuts",
                EditorStyles.miniLabel);
            EditorGUILayout.HelpBox(authoring.PreviewStatus, GetStatusType(authoring.PreviewStatus));

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add Editing Volume")) AddVolume(authoring, true);
                if (GUILayout.Button("Add Fracture Cut")) AddFractureVolume(authoring, true);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Rebuild Mesh"))
                    TopDown3DRockWorkbenchPreview.RequestRebuild(authoring, true);
                if (GUILayout.Button("Clear Preview Mesh"))
                {
                    TopDown3DRockWorkbenchPreview.ClearPreview(
                        authoring,
                        "Preview cleared. Source volumes were preserved.");
                }
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
        }

        private static void DrawTessellationDetail(
            SerializedProperty voxelSize,
            TopDown3DRockWorkbenchAuthoring authoring)
        {
            if (authoring.IsFormationMember)
            {
                var formationDetail = TopDown3DRockWorkbenchAuthoring.VoxelSizeToTessellationDetail(
                    voxelSize.floatValue);
                EditorGUI.BeginChangeCheck();
                var adjustedFormationDetail = EditorGUILayout.Slider(
                    new GUIContent(
                        "Tessellation Detail",
                        "Controls surface sampling detail. 1 rebuilds fastest; 5 produces the finest silhouette."),
                    formationDetail,
                    TopDown3DRockWorkbenchAuthoring.MinimumTessellationDetail,
                    TopDown3DRockWorkbenchAuthoring.MaximumTessellationDetail);
                if (EditorGUI.EndChangeCheck())
                {
                    voxelSize.floatValue = TopDown3DRockWorkbenchAuthoring.TessellationDetailToVoxelSize(
                        adjustedFormationDetail);
                }
                return;
            }

            var factor = authoring.PendingStandalonePhysicalScaleFactor;
            var valueMeters = voxelSize.floatValue * factor;
            var detail = TopDown3DRockWorkbenchAuthoring.StandaloneVoxelSizeToTessellationDetail(
                valueMeters);
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
                voxelSize.floatValue =
                    TopDown3DRockWorkbenchAuthoring.StandaloneTessellationDetailToVoxelSize(
                        adjustedDetail) / factor;
            }
        }

        private static void DrawPhysicalFusionSmoothness(
            SerializedProperty smoothness,
            TopDown3DRockWorkbenchAuthoring authoring)
        {
            if (authoring.IsFormationMember)
            {
                EditorGUILayout.PropertyField(smoothness);
                return;
            }

            var factor = authoring.PendingStandalonePhysicalScaleFactor;
            var valueMeters = Mathf.Clamp(smoothness.floatValue * factor, 0f, 0.1f);
            var adjustedMeters = EditorGUILayout.Slider(
                new GUIContent(
                    "Fusion Smoothness",
                    "Physical blend width in meters between overlapping stone masses."),
                valueMeters,
                0f,
                0.1f);
            if (!Mathf.Approximately(adjustedMeters, valueMeters))
                smoothness.floatValue = adjustedMeters / factor;
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
            var sourceGroup = TopDown3DRockWorkbenchBaseRockGenerator.GetOrCreateSourceGroup(
                authoring,
                "Add Rock Source Volume");
            Undo.SetTransformParent(volumeObject.transform, sourceGroup, "Parent Rock Source Volume");
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

        internal static void AddFractureVolume(
            TopDown3DRockWorkbenchAuthoring authoring,
            bool selectVolume)
        {
            if (authoring == null) return;

            var existing = authoring.GetComponentsInChildren<TopDown3DRockVolumeNode>(true);
            var volumeObject = new GameObject($"Fracture Cut Volume {existing.Length + 1}");
            Undo.RegisterCreatedObjectUndo(volumeObject, "Add Rock Fracture Cut");
            var sourceGroup = TopDown3DRockWorkbenchBaseRockGenerator.GetOrCreateSourceGroup(
                authoring,
                "Add Rock Fracture Cut");
            Undo.SetTransformParent(volumeObject.transform, sourceGroup, "Parent Rock Fracture Cut");
            volumeObject.transform.localPosition = new Vector3(0f, authoring.GeneratedHeight * 0.56f, 0f);
            volumeObject.transform.localRotation = Quaternion.Euler(0f, 35f, 8f);
            volumeObject.transform.localScale = new Vector3(
                Mathf.Max(0.08f, authoring.GeneratedWidth * 0.045f),
                authoring.GeneratedHeight * 0.72f,
                authoring.GeneratedWidth * 0.62f);
            var node = Undo.AddComponent<TopDown3DRockVolumeNode>(volumeObject);
            node.SetSourceShape(TopDown3DRockSourceShape.FractureCut);
            node.SetOperation(TopDown3DRockVolumeOperation.Subtractive);
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
                ? node.Operation == TopDown3DRockVolumeOperation.Subtractive
                    ? selected ? new Color(1f, 0.48f, 0.18f, 1f) : new Color(1f, 0.34f, 0.12f, 0.72f)
                    : selected ? new Color(0.3f, 0.95f, 1f, 1f) : new Color(0.2f, 0.75f, 1f, 0.75f)
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
                "Additive volumes build stone. Subtractive fracture volumes carve it. Use this object's Transform to position, rotate, and scale the editable volume.",
                MessageType.Info);
            using (new EditorGUI.DisabledScope(workbench == null))
            {
                if (GUILayout.Button("Add Another Source Volume"))
                {
                    TopDown3DRockWorkbenchAuthoringEditor.AddVolume(workbench, true);
                }
                if (GUILayout.Button("Add Fracture Cut"))
                {
                    TopDown3DRockWorkbenchAuthoringEditor.AddFractureVolume(workbench, true);
                }
                if (GUILayout.Button("Select Rock Workbench")) Selection.activeGameObject = workbench.gameObject;
            }
        }
    }
}
