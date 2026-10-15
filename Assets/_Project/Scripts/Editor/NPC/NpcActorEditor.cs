using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Editor
{
    [CustomEditor(typeof(NpcActor))]
    public sealed class NpcActorEditor : UnityEditor.Editor
    {
        private NpcTuning draft;
        private string error;
        public override void OnInspectorGUI()
        {
            var actor = (NpcActor)target;
            if (!Application.isPlaying) { DrawDefaultInspector(); return; }
            EditorGUILayout.LabelField("Behavior", actor.Behavior + " / " + actor.Movement);
            EditorGUILayout.LabelField("Decision", actor.DecisionReason);
            EditorGUILayout.LabelField("Route", actor.Navigation.Reason ?? "No active route");
            EditorGUILayout.LabelField("Armed / epoch", actor.IsArmed + " / " + actor.Epoch);
            var trial = actor.GetComponent<CampLoopNpcTrial>();
            if (trial != null && !string.IsNullOrEmpty(trial.Failure))
                EditorGUILayout.HelpBox(trial.Failure, MessageType.Warning);
            EditorGUILayout.LabelField("Searches / probes", actor.Navigation.Searches + " / " + actor.Navigation.ProbeCalls);
            EditorGUILayout.LabelField("Selected score", actor.Navigation.SelectedScore.ToString("F3"));
            EditorGUILayout.LabelField("Observation age", actor.Observation.Valid ? (Time.timeAsDouble - actor.Observation.ObservedAt).ToString("F2") : "None");
            if (draft == null) draft = actor.Tuning;
            if (draft == null) { EditorGUILayout.HelpBox(actor.Failure ?? "Awaiting lab readiness.", MessageType.Info); return; }
            EditorGUILayout.LabelField("Temporary session tuning", EditorStyles.boldLabel);
            foreach (var group in NpcTuningFields.Groups)
            {
                EditorGUILayout.LabelField(group[0], EditorStyles.boldLabel);
                for (var i = 1; i < group.Length; i++)
                {
                    var field = typeof(NpcTuning).GetField(group[i]);
                    field.SetValue(draft, EditorGUILayout.FloatField(ObjectNames.NicifyVariableName(field.Name), (float)field.GetValue(draft)));
                }
            }
            if (GUILayout.Button("Apply Tuning")) actor.ApplyTuning(draft, out error);
            if (GUILayout.Button("Restore Authored Tuning") && actor.Definition != null)
            { draft = actor.Definition.Profile.Snapshot(); actor.ApplyTuning(draft, out error); }
            if (!string.IsNullOrEmpty(error)) EditorGUILayout.HelpBox(error, MessageType.Warning);
            if (!string.IsNullOrEmpty(actor.Failure)) EditorGUILayout.HelpBox(actor.Failure, MessageType.Warning);
            foreach (var candidate in actor.Navigation.Candidates)
                EditorGUILayout.LabelField(candidate.Reason, candidate.Eligible ? candidate.Score.ToString("F3") : "Rejected");
            Repaint();
        }
    }
    internal static class NpcTuningFields
    {
        public static readonly string[][] Groups =
        {
            new[] { "Perception", "detection", "maximumVerticalDistance", "perceptionSeconds", "memorySeconds" },
            new[] { "Skittish Behavior", "calmDistance", "comfort", "panicRelease", "panic", "alertSeconds", "calmSeconds", "decisionSeconds", "commitmentSeconds", "switchMargin" },
            new[] { "Ground Movement", "retreatSpeed", "fleeSpeed", "acceleration", "braking", "turnDegrees", "replanSeconds", "slopeDegrees", "stepUp", "stepDown", "stuckSeconds", "progressMetres" }
        };
    }
    [CustomEditor(typeof(SkittishNpcProfile))]
    public sealed class SkittishNpcProfileEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            if (Application.isPlaying) { EditorGUILayout.HelpBox("Use the actor's temporary tuning during Play Mode; authored assets stay unchanged.", MessageType.Info); return; }
            var tuning = serializedObject.FindProperty("tuning");
            foreach (var group in NpcTuningFields.Groups)
            {
                EditorGUILayout.LabelField(group[0], EditorStyles.boldLabel);
                for (var i = 1; i < group.Length; i++) EditorGUILayout.PropertyField(tuning.FindPropertyRelative(group[i]));
            }
            serializedObject.ApplyModifiedProperties();
            if (!((SkittishNpcProfile)target).Snapshot().Validate(out var reason)) EditorGUILayout.HelpBox(reason, MessageType.Warning);
        }
    }
}
