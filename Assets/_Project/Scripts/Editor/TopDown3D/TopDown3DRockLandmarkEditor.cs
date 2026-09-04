using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using BooterBigArm.TopDown3D;

namespace BooterBigArm.Editor
{
    [CustomEditor(typeof(TopDown3DRockLandmarkAuthoring))]
    public sealed class TopDown3DRockLandmarkEditor : UnityEditor.Editor
    {
        private const string CreateFromSelectionMenuPath =
            "GameObject/Booter & BigARM/Top Down 3D/Create Landmark From Selected Formations";
        private bool showAdvanced;

        internal static bool CanCreateFromSelection => CollectSelectedFormations().Count >= 2;

        public override void OnInspectorGUI()
        {
            var landmark = (TopDown3DRockLandmarkAuthoring)target;
            EditorGUILayout.LabelField("Rock Landmark Generator", EditorStyles.boldLabel);

            serializedObject.Update();
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("landmarkArchetype"),
                new GUIContent("Landmark Type"));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("generatedWidth"),
                new GUIContent("Width", "Approximate horizontal landmark footprint."));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("generatedHeight"),
                new GUIContent("Height", "Approximate height of the tallest formation."));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("generatedAsymmetry"),
                new GUIContent("Asymmetry"));
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.HelpBox(
                "Spire Complex layers tall pile cores, medium shoulders, a broad low apron, and scattered outliers. Every child formation and rock remains editable.",
                MessageType.Info);
            EditorGUILayout.LabelField(
                $"Will generate {landmark.GeneratedFormationCount} editable formations",
                EditorStyles.miniLabel);

            if (GUILayout.Button("Generate New Landmark", GUILayout.Height(36f)))
            {
                TopDown3DRockLandmarkGenerator.GenerateIntoLandmark(
                    landmark,
                    TopDown3DRockWorkbenchBaseRockGenerator.CreateNewSeed(
                        landmark.LandmarkSeed));
                GUIUtility.ExitGUI();
            }
            if (GUILayout.Button("Update Current Landmark With These Settings"))
            {
                TopDown3DRockLandmarkGenerator.GenerateIntoLandmark(
                    landmark,
                    landmark.LandmarkSeed);
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.LabelField(
                "Generation replaces child formations. Unity Undo restores the previous arrangement.",
                EditorStyles.miniLabel);

            var formations = CollectOwnedFormations(landmark);
            var rockCount = 0;
            foreach (var formation in formations)
            {
                rockCount += formation.GetComponentsInChildren<
                    TopDown3DRockWorkbenchAuthoring>(true).Length;
            }
            EditorGUILayout.HelpBox(
                $"Editable formations: {formations.Count}\nEditable rocks: {rockCount}\n{landmark.PreviewStatus}",
                formations.Count == 0 ? MessageType.Warning : MessageType.None);

            showAdvanced = EditorGUILayout.Foldout(
                showAdvanced,
                "Advanced",
                true,
                EditorStyles.foldoutHeader);
            if (!showAdvanced) return;

            var previousSeed = landmark.LandmarkSeed;
            var previouslySharedGeologicalField = landmark.ShareGeologicalField;
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("rockMaterial"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("landmarkSeed"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("approachOpening"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("shareGeologicalField"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("conformToTerrain"));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("memberVoxelSize"),
                new GUIContent("Child Tessellation Size"));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("memberFusionSmoothness"),
                new GUIContent("Child Rock Smoothing"));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("memberSurfaceRelaxation"),
                new GUIContent("Child Surface Relaxation"));
            var previewSettingsChanged = EditorGUI.EndChangeCheck();
            serializedObject.ApplyModifiedProperties();

            if (previewSettingsChanged
                && (previousSeed != landmark.LandmarkSeed
                    || previouslySharedGeologicalField != landmark.ShareGeologicalField))
            {
                foreach (var formation in formations)
                {
                    TopDown3DRockWorkbenchFormationPreview.RequestRebuild(formation, false);
                }
            }

            EditorGUILayout.LabelField(
                "Material, terrain, and child-mesh changes apply on Generate or Update. "
                + "Seed and shared-field changes refresh the current surface preview.",
                EditorStyles.wordWrappedMiniLabel);
        }

        [MenuItem(CreateFromSelectionMenuPath, false, 33)]
        private static void CreateFromSelectedFormations()
        {
            var formations = CollectSelectedFormations();
            if (formations.Count < 2)
            {
                EditorUtility.DisplayDialog(
                    "Create Rock Landmark",
                    "Select at least two Rock Formation Workbench objects, then run this command again.",
                    "OK");
                return;
            }

            foreach (var formation in formations)
            {
                if (formation.GetComponentInParent<TopDown3DRockLandmarkAuthoring>() != null)
                {
                    EditorUtility.DisplayDialog(
                        "Create Rock Landmark",
                        $"'{formation.name}' is already inside a Rock Landmark Workbench.",
                        "OK");
                    return;
                }
            }

            CreateLandmarkFromFormations(formations);
        }

        [MenuItem(CreateFromSelectionMenuPath, true)]
        private static bool ValidateCreateFromSelectedFormations()
        {
            return CanCreateFromSelection;
        }

        internal static TopDown3DRockLandmarkAuthoring CreateLandmarkFromFormations(
            IReadOnlyList<TopDown3DRockWorkbenchFormationAuthoring> formations)
        {
            if (formations == null || formations.Count < 2)
            {
                throw new ArgumentException(
                    "A rock landmark requires at least two formations.",
                    nameof(formations));
            }

            const string undoName = "Create Rock Landmark From Formations";
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(undoName);
            var undoGroup = Undo.GetCurrentGroup();
            try
            {
                var bounds = CalculateSelectionBounds(formations);
                var averageY = 0f;
                var commonParent = formations[0].transform.parent;
                for (var index = 0; index < formations.Count; index++)
                {
                    var formation = formations[index];
                    if (formation == null)
                    {
                        throw new ArgumentException(
                            "A selected formation is missing.",
                            nameof(formations));
                    }
                    if (formation.GetComponentInParent<TopDown3DRockLandmarkAuthoring>() != null)
                    {
                        throw new InvalidOperationException(
                            $"'{formation.name}' is already inside a Rock Landmark Workbench.");
                    }
                    averageY += formation.transform.position.y;
                    if (formation.transform.parent != commonParent) commonParent = null;
                }

                var root = new GameObject("Rock Landmark Workbench");
                Undo.RegisterCreatedObjectUndo(root, undoName);
                root.transform.position = new Vector3(
                    bounds.center.x,
                    averageY / formations.Count,
                    bounds.center.z);
                if (commonParent != null)
                {
                    Undo.SetTransformParent(root.transform, commonParent, undoName);
                }

                var landmark = Undo.AddComponent<TopDown3DRockLandmarkAuthoring>(root);
                var material = formations[0].RockMaterial != null
                    ? formations[0].RockMaterial
                    : AssetDatabase.LoadAssetAtPath<Material>(
                        TopDown3DRockWorkbenchAuthoringEditor.WorkbenchMaterialPath);
                landmark.ConfigureCaptured(
                    material,
                    TopDown3DRockWorkbenchBaseRockGenerator.CreateNewSeed(
                        formations[0].FormationSeed),
                    Mathf.Max(bounds.size.x, bounds.size.z),
                    bounds.size.y);

                foreach (var formation in formations)
                {
                    Undo.SetTransformParent(formation.transform, root.transform, undoName);
                }

                EditorUtility.SetDirty(landmark);
                Selection.activeGameObject = root;
                SceneView.RepaintAll();
                return landmark;
            }
            finally
            {
                Undo.CollapseUndoOperations(undoGroup);
            }
        }

        private static List<TopDown3DRockWorkbenchFormationAuthoring>
            CollectSelectedFormations()
        {
            var formations = new List<TopDown3DRockWorkbenchFormationAuthoring>();
            foreach (var selected in Selection.gameObjects)
            {
                if (selected == null) continue;
                var formation = selected.GetComponentInParent<
                    TopDown3DRockWorkbenchFormationAuthoring>();
                if (formation != null && !formations.Contains(formation))
                {
                    formations.Add(formation);
                }
            }
            return formations;
        }

        private static List<TopDown3DRockWorkbenchFormationAuthoring> CollectOwnedFormations(
            TopDown3DRockLandmarkAuthoring landmark)
        {
            var output = new List<TopDown3DRockWorkbenchFormationAuthoring>();
            foreach (var formation in landmark.GetComponentsInChildren<
                TopDown3DRockWorkbenchFormationAuthoring>(true))
            {
                if (formation.GetComponentInParent<TopDown3DRockLandmarkAuthoring>() == landmark)
                {
                    output.Add(formation);
                }
            }
            return output;
        }

        private static Bounds CalculateSelectionBounds(
            IReadOnlyList<TopDown3DRockWorkbenchFormationAuthoring> formations)
        {
            var bounds = new Bounds(formations[0].transform.position, Vector3.zero);
            for (var index = 0; index < formations.Count; index++)
            {
                var formation = formations[index];
                var foundRenderable = false;
                foreach (var renderer in formation.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer == null) continue;
                    bounds.Encapsulate(renderer.bounds);
                    foundRenderable = true;
                }
                if (!foundRenderable) bounds.Encapsulate(formation.transform.position);
            }
            return bounds;
        }
    }
}
