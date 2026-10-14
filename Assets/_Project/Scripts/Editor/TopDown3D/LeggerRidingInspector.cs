using BooterBigArm.TopDown3D;
using UnityEditor;

namespace BooterBigArm.Editor
{
    [CustomEditor(typeof(TopDown3DLeggerRidingController))]
    public sealed class LeggerRidingInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var riding = (TopDown3DLeggerRidingController)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Relationship", riding.State.Phase + " / epoch " + riding.State.Epoch);
            EditorGUILayout.LabelField("Achieved speed", riding.ActualSpeed.ToString("F2") + " m/s");
            EditorGUILayout.LabelField("Terrain motion", riding.Stepping ? "Step continuation" : riding.MotionBlock.ToString());
            EditorGUILayout.LabelField("Last tick work", riding.QueryCount + " queries / " + riding.SubstepCount + " substeps");
            if (!string.IsNullOrEmpty(riding.LastFailure)) EditorGUILayout.HelpBox(riding.LastFailure, MessageType.Info);
            EditorGUILayout.HelpBox("Riding captures tuning at startup. Change the study profile before starting a new session.", MessageType.None);
        }
        public override bool RequiresConstantRepaint() => EditorApplication.isPlaying;
    }
}
