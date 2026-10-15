using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Editor
{
    [CustomEditor(typeof(TopDown3DSceneRenderingOverride))]
    public sealed class TopDown3DModularPresentationEditor : UnityEditor.Editor
    {
        private UnityEditor.Editor optics;
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cameraOpticsProfile"));
            var selected = serializedObject.FindProperty("cameraOpticsProfile").objectReferenceValue;
            if (selected == null)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("atmosphereProfile"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("disableTiltShift"));
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("opticsFocusTarget"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("opticsFocusOffset"));
            if (serializedObject.ApplyModifiedProperties())
            { TopDown3DCampOpticsStack.Release(((Component)target).GetComponent<Camera>()); CampLookEditorPreview.Refresh(); }
            if (selected == null) return;
            EditorGUILayout.HelpBox("Camera optics asset edits affect every camera referencing this asset. Lighting and sky are configured under Environment.", MessageType.Info);
            CreateCachedEditor(selected, null, ref optics); optics.OnInspectorGUI();
        }
        private void OnDisable() { if (optics != null) DestroyImmediate(optics); }
    }
    [CustomEditor(typeof(TopDown3DCameraOpticsProfile))]
    public sealed class TopDown3DCameraOpticsProfileEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var profile = (TopDown3DCameraOpticsProfile)target;
            string effective = !profile.IsValid ? "Invalid profile: effects suppressed" : !profile.nativeOptics.opticsEnabled
                ? "Off (optics master disabled)" : profile.EffectiveTiltShift ? "Tilt Shift" : profile.EffectiveDepthMode.ToString();
            EditorGUILayout.HelpBox("Effective world blur: " + effective + ". Gaussian before lens supports flare; native post-process DOF suppresses flare. DOF suppresses streaks.", MessageType.Info);
            if (profile.nativeOptics == null) return;
            EditorGUILayout.LabelField("Native Optics Asset", EditorStyles.boldLabel);
            var source = new SerializedObject(profile.nativeOptics); source.Update();
            var property = source.GetIterator(); bool enterChildren = true;
            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (property.name == "m_Script" || property.name == "sunAngularDiameterDegrees") continue;
                EditorGUILayout.PropertyField(property, true);
            }
            if (source.ApplyModifiedProperties()) CampLookEditorPreview.Refresh();
            EditorGUILayout.HelpBox("Apparent sun diameter is owned by the Sky profile.", MessageType.Info);
        }
    }
}
