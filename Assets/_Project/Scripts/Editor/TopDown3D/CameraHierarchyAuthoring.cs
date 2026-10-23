using System;
using System.IO;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Editor
{
    public static class CameraHierarchyAuthoring
    {
        public static TopDown3DCameraSelection Install(TopDown3DCameraRig rig)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || rig == null)
                throw new InvalidOperationException("Camera authoring requires a scene rig in Edit Mode.");
            if (rig.CameraSelection != null) return rig.CameraSelection;
            var profile = Resources.Load<TopDown3DThirdPersonCameraProfile>("ThirdPersonCameraProfile");
            if (profile == null || !profile.IsEnabledForScene(rig.gameObject.scene.path))
                throw new InvalidOperationException("This scene must explicitly enable the camera study.");
            var root = Create("Camera Options", rig.transform.parent, rig.gameObject.scene);
            var selection = Undo.AddComponent<TopDown3DCameraSelection>(root);
            selection.startingView = TopDown3DCameraView.Elevated;
            var elevated = Undo.AddComponent<TopDown3DElevatedCameraSettings>(Create("Elevated Camera Rig", root.transform, root.scene));
            var third = Undo.AddComponent<TopDown3DThirdPersonCameraSettings>(Create("Third Person Camera Rig", root.transform, root.scene));
            var source = new SerializedObject(rig);
            var destination = new SerializedObject(elevated);
            var iterator = destination.GetIterator();
            while (iterator.NextVisible(true))
                if (iterator.name != "m_Script" && source.FindProperty(iterator.propertyPath) != null)
                    destination.CopyFromSerializedProperty(source.FindProperty(iterator.propertyPath));
            destination.ApplyModifiedProperties();
            selection.elevated = elevated; selection.thirdPerson = third; third.profile = profile;
            var setup = new SerializedObject(selection);
            setup.FindProperty("outputRig").objectReferenceValue = rig; setup.ApplyModifiedProperties();
            source.FindProperty("cameraSelection").objectReferenceValue = selection; source.ApplyModifiedProperties();
            EditorUtility.SetDirty(selection); EditorUtility.SetDirty(third);
            EditorSceneManager.MarkSceneDirty(root.scene);
            return selection;
        }

        private static GameObject Create(string name, Transform parent, Scene scene)
        {
            var item = new GameObject(name);
            SceneManager.MoveGameObjectToScene(item, scene);
            item.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(item, "Create camera rigs");
            return item;
        }

        public static void InstallLiveFromCli()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != CameraOptionsStudyAuthoring.StudyPath || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("The camera study must already be open in Edit Mode.");
            Directory.CreateDirectory("Logs/CameraHierarchy");
            EditorSceneManager.SaveScene(scene, "Logs/CameraHierarchy/before.unity", true);
            var rig = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<TopDown3DCameraRig>(true)).Single();
            Install(rig);
            EditorSceneManager.SaveScene(scene, "Logs/CameraHierarchy/after.unity", true);
            Debug.Log("CAMERA_HIERARCHY_INSTALLED: live Undo-backed edit; source scene not saved.");
        }

        public static void InstallCandidateFromCli()
        {
            if (!File.Exists(".camera-options-validation")) throw new InvalidOperationException("Use the marked camera candidate.");
            var scene = EditorSceneManager.OpenScene(CameraOptionsStudyAuthoring.StudyPath);
            var rig = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<TopDown3DCameraRig>(true)).Single();
            Install(rig); EditorSceneManager.SaveScene(scene);
            if (scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Camera>(true)).Count() != 1)
                throw new InvalidOperationException("The hierarchy must retain one rendering camera.");
            Debug.Log("CAMERA_HIERARCHY_CANDIDATE_VALID");
        }

        public static void ValidateCandidateFromCli()
        {
            if (!File.Exists(".camera-options-validation")) throw new InvalidOperationException("Use the marked camera candidate.");
            var scene = EditorSceneManager.OpenScene(CameraOptionsStudyAuthoring.StudyPath);
            ValidateScene(scene);
        }

        public static void ValidateScene(Scene scene)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != CameraOptionsStudyAuthoring.StudyPath)
                throw new InvalidOperationException("Validate the camera study in Edit Mode.");
            var components = scene.GetRootGameObjects();
            var rig = components.SelectMany(root => root.GetComponentsInChildren<TopDown3DCameraRig>(true)).Single();
            var selection = rig.CameraSelection;
            if (selection == null || !selection.IsConfigured || selection.OutputRig != rig
                || selection.startingView != TopDown3DCameraView.Elevated)
                throw new InvalidOperationException("The authored scene must select its two configured rigs and start in Elevated.");
            var legacy = new SerializedObject(rig); var settings = new SerializedObject(selection.elevated);
            var property = settings.GetIterator();
            if (property.NextVisible(true)) do
            {
                var source = legacy.FindProperty(property.propertyPath);
                if (property.name != "m_Script" && source != null && !Equals(property.boxedValue, source.boxedValue))
                    throw new InvalidOperationException("Elevated migration changed " + property.propertyPath);
            } while (property.NextVisible(false));
            var camera = rig.GetComponent<Camera>(); var identity = camera.GetInstanceID();
            CameraRigInspector.Preview(selection, TopDown3DCameraView.ThirdPerson);
            var thirdPosition = camera.transform.position;
            if (!Mathf.Approximately(camera.fieldOfView, selection.thirdPerson.fieldOfView))
                throw new InvalidOperationException("Third-person preview did not apply its authored lens.");
            CameraRigInspector.Preview(selection, TopDown3DCameraView.Elevated);
            if (!Mathf.Approximately(camera.fieldOfView, selection.elevated.fieldOfView)
                || Vector3.Distance(camera.transform.position, thirdPosition) < 1
                || camera.GetInstanceID() != identity || !rig.isActiveAndEnabled
                || rig.GetComponent<TopDown3DCameraOptions>() != null || selection.thirdPerson.transform.childCount != 0
                || components.SelectMany(root => root.GetComponentsInChildren<Camera>(true)).Count() != 1
                || components.SelectMany(root => root.GetComponentsInChildren<AudioListener>(true)).Count() != 1)
                throw new InvalidOperationException("Independent previews must preserve output identity and clean up temporary solvers.");
            Directory.CreateDirectory("Logs/CameraHierarchy");
            File.WriteAllText("Logs/CameraHierarchy/validated.json", "{\"configuredRigs\":2,\"cameraCount\":1,\"audioListeners\":1,"
                + "\"startingView\":\"Elevated\",\"elevatedSettingsPreserved\":true,\"bothPreviewsPassed\":true,\"previewCleanupPassed\":true}");
            Debug.Log("CAMERA_HIERARCHY_VALIDATED: authored settings; independent previews; one output; clean temporary ownership.");
        }
    }
}
