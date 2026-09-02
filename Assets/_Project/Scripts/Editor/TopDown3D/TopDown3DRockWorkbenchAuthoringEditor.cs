using System;
using UnityEditor;
using UnityEngine;
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
                "Each Cube Volume is an editable source, not a separate finished mesh. Move, rotate, and scale those child objects in the Scene view. The workbench remeshes their combined volume so overlapping cubes become one continuous surface. Its default PBR surface uses world-space triplanar base color, normal, roughness, occlusion, and upward dust, so it remains continuous across the fused mesh without authored UVs.",
                MessageType.Info);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Base Rock Generator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Generate New Base Rock builds a dominant core, tapered support masses, and smaller details inside the Overall Size envelope. Verticality changes the silhouette from a horizontal spread to an upward spine. The change is one Undo step, the seed can be regenerated exactly, and every resulting cube remains editable.",
                MessageType.None);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Generate New Base Rock"))
                {
                    TopDown3DRockWorkbenchBaseRockGenerator.GenerateIntoWorkbench(
                        authoring,
                        TopDown3DRockWorkbenchBaseRockGenerator.CreateNewSeed(authoring.GenerationSeed));
                }
                if (GUILayout.Button("Regenerate Current Seed"))
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
            Undo.AddComponent<TopDown3DRockVolumeNode>(volumeObject);
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
