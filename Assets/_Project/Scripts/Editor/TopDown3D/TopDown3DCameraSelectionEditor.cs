using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Editor
{
    [CustomEditor(typeof(TopDown3DCameraSelection))]
    public sealed class TopDown3DCameraSelectionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var selection = (TopDown3DCameraSelection)target;
            EditorGUILayout.HelpBox("Starting View chooses the camera for each Play session. Both rig objects stay enabled. "
                + "Select a child rig to edit its framing. One Main Camera renders either view.", MessageType.Info);
            CameraRigInspector.Controls(selection, TopDown3DCameraView.Elevated);
            CameraRigInspector.Controls(selection, TopDown3DCameraView.ThirdPerson);
            if (Application.isPlaying && selection.Options != null)
                EditorGUILayout.LabelField("Requested / Active", selection.Options.PreferredView + " / " + selection.Options.EffectiveView);
        }
    }

    [CustomEditor(typeof(TopDown3DElevatedCameraSettings))]
    public sealed class TopDown3DElevatedCameraSettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            CameraRigInspector.Controls(((Component)target).GetComponentInParent<TopDown3DCameraSelection>(), TopDown3DCameraView.Elevated);
        }
    }

    [CustomEditor(typeof(TopDown3DThirdPersonCameraSettings))]
    public sealed class TopDown3DThirdPersonCameraSettingsEditor : UnityEditor.Editor
    {
        private UnityEditor.Editor profileEditor;
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var settings = (TopDown3DThirdPersonCameraSettings)target;
            if (settings.profile != null)
            {
                EditorGUILayout.HelpBox("Framing below edits the linked profile asset. Field Of View above is the scene's starting lens; the runtime menu can override it.", MessageType.Info);
                CreateCachedEditor(settings.profile, null, ref profileEditor);
                profileEditor.OnInspectorGUI();
            }
            CameraRigInspector.Controls(settings.GetComponentInParent<TopDown3DCameraSelection>(), TopDown3DCameraView.ThirdPerson);
        }
        private void OnDisable() { if (profileEditor != null) DestroyImmediate(profileEditor); }
    }

    internal static class CameraRigInspector
    {
        internal static void Controls(TopDown3DCameraSelection selection, TopDown3DCameraView view)
        {
            if (selection == null || !selection.IsConfigured) return;
            string label = view == TopDown3DCameraView.Elevated ? "Elevated" : "Third Person";
            if (GUILayout.Button(Application.isPlaying ? "Switch to " + label : "Use " + label + " at Runtime"))
            {
                if (Application.isPlaying) selection.RequestView(view);
                else
                {
                    Undo.RecordObject(selection, "Select starting camera"); selection.startingView = view;
                    EditorUtility.SetDirty(selection);
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(selection.gameObject.scene);
                }
            }
            if (!Application.isPlaying && GUILayout.Button("Preview " + label)) Preview(selection, view);
        }

        internal static void Preview(TopDown3DCameraSelection selection, TopDown3DCameraView view)
        {
            if (Application.isPlaying) return;
            var rig = selection.OutputRig;
            var camera = rig.GetComponent<Camera>();
            Undo.RecordObjects(new Object[] { camera, camera.transform }, "Preview camera framing");
            var previous = rig.GetComponent<TopDown3DCameraOptions>();
            var options = rig.EnableCameraOptions();
            try
            {
                if (view == TopDown3DCameraView.Elevated)
                {
                    if (rig.TryGetElevatedPose(out var position, out var rotation)) rig.RestoreElevatedPose(position, rotation);
                    else Debug.LogWarning("Elevated camera space is blocked; preview retained.", selection);
                }
                else if (!options.TrySetView(view)) Debug.LogWarning(options.Notice, selection);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(rig.gameObject.scene);
            }
            finally
            {
                if (previous == null)
                {
                    // Runtime behaviours do not receive destruction callbacks reliably during Edit Mode authoring.
                    typeof(TopDown3DCameraOptions).GetMethod("OnDestroy", System.Reflection.BindingFlags.Instance
                        | System.Reflection.BindingFlags.NonPublic).Invoke(options, null);
                    Object.DestroyImmediate(options);
                }
            }
        }
    }
}
