using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Editor
{
    [CustomEditor(typeof(TopDown3DLandscapeGroundStamp))]
    public sealed class TopDown3DLandscapeGroundStampEditor : UnityEditor.Editor
    {
        [MenuItem("Booter & BigARM/Top Down 3D/Add Ground Paint and Sand Bank Stamp")]
        private static void AddStamp()
        {
            var selected = Selection.activeGameObject;
            var sandbox = selected != null
                ? selected.GetComponentInParent<TopDown3DLandscapeAuthoringSandbox>()
                : null;
            if (sandbox == null)
            {
                foreach (var candidate in Object.FindObjectsByType<TopDown3DLandscapeAuthoringSandbox>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (candidate.gameObject.scene.path != LandscapeAuthoringSandboxBuilder.ScenePath
                        || candidate.RockReference != null) continue;
                    sandbox = candidate;
                    break;
                }
            }
            if (sandbox == null || sandbox.gameObject.scene.path != LandscapeAuthoringSandboxBuilder.ScenePath)
            {
                EditorUtility.DisplayDialog("Ground Stamp",
                    "Open the Landscape Authoring Sandbox before adding a ground stamp.", "OK");
                return;
            }

            var stampObject = new GameObject("Ground Paint and Sand Bank Stamp");
            Undo.RegisterCreatedObjectUndo(stampObject, "Add Ground Stamp");
            Undo.SetTransformParent(stampObject.transform, sandbox.transform, "Add Ground Stamp");
            stampObject.transform.position = sandbox.transform.position;
            Undo.AddComponent<TopDown3DLandscapeGroundStamp>(stampObject);
            Selection.activeGameObject = stampObject;
            TopDown3DLandscapeAuthoringSandboxEditor.BuildTerrainContext(sandbox);
        }

        public override void OnInspectorGUI()
        {
            var stamp = (TopDown3DLandscapeGroundStamp)target;
            EditorGUILayout.HelpBox(
                "Move this control across the ground. Radius and edge blend set the painted area; "
                + "Sand Bank Height raises the same terrain mesh and collider. Set height to zero for paint only.",
                MessageType.Info);
            DrawDefaultInspector();
            var sandbox = stamp.GetComponentInParent<TopDown3DLandscapeAuthoringSandbox>();
            using (new EditorGUI.DisabledScope(sandbox == null))
            {
                if (GUILayout.Button("Update Ground Preview"))
                    TopDown3DLandscapeAuthoringSandboxEditor.BuildTerrainContext(sandbox);
            }
        }

        private void OnSceneGUI()
        {
            var stamp = (TopDown3DLandscapeGroundStamp)target;
            Handles.color = new Color(1f, 0.78f, 0.36f, 0.85f);
            Handles.DrawWireDisc(stamp.transform.position, Vector3.up, stamp.Radius);
        }
    }
}
