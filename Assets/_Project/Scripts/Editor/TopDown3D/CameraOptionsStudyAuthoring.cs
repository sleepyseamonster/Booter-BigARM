using System;
using System.IO;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BooterBigArm.Editor
{
    public static class CameraOptionsStudyAuthoring
    {
        public const string StudyPath = "Assets/_Project/Scenes/Reference/CampLoopTesting.unity";
        private static void RequireCandidate()
        {
            if (!File.Exists(".camera-options-validation") || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Use the marked isolated camera candidate in Edit Mode.");
        }
        public static void AuthorFromCli()
        {
            RequireCandidate();
            const string resourceFolder = "Assets/_Project/Settings/Player/Resources";
            if (!AssetDatabase.IsValidFolder(resourceFolder)) AssetDatabase.CreateFolder("Assets/_Project/Settings/Player", "Resources");
            const string profilePath = resourceFolder + "/ThirdPersonCameraProfile.asset";
            if (AssetDatabase.LoadAssetAtPath<TopDown3DThirdPersonCameraProfile>(profilePath) == null)
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<TopDown3DThirdPersonCameraProfile>(), profilePath);
            var profile = AssetDatabase.LoadAssetAtPath<TopDown3DThirdPersonCameraProfile>(profilePath);
            EditorUtility.SetDirty(profile); AssetDatabase.SaveAssetIfDirty(profile);
            TopDown3DMenuPrefabAuthoring.AddCameraOptionsOwnedFromCli();
            ValidateFromCli();
        }

        public static void ValidateFromCli()
        {
            RequireCandidate();
            var scene = EditorSceneManager.OpenScene(StudyPath);
            var roots = scene.GetRootGameObjects();
            if (roots.SelectMany(root => root.GetComponentsInChildren<Camera>(true)).Count() != 1
                || roots.SelectMany(root => root.GetComponentsInChildren<AudioListener>(true)).Count() != 1)
                throw new InvalidOperationException("Keep one output camera and listener.");
            var rig = roots.SelectMany(root => root.GetComponentsInChildren<TopDown3DCameraRig>(true)).Single();
            var profile = Resources.Load<TopDown3DThirdPersonCameraProfile>("ThirdPersonCameraProfile");
            if (profile == null || !profile.IsValid || !profile.IsEnabledForScene(StudyPath)
                || profile.IsEnabledForScene("Assets/_Project/Scenes/Production/GreaterWasteland.unity"))
                throw new InvalidOperationException("Camera profile must explicitly enable the study and retain production scope.");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TopDown3DMenuPrefabAuthoring.PrefabPath);
            if (prefab == null || prefab.transform.Find("Safe Area/Camera") == null
                || prefab.transform.Find("Safe Area/Settings/CameraOptions") == null)
                throw new InvalidOperationException("Authored camera settings are unavailable.");
            Debug.Log("CAMERA_OPTIONS_STUDY_VALID: one output camera/listener; explicit opt-in; authored settings.");
        }

        public static void CaptureFromCli()
        {
            RequireCandidate();
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                throw new InvalidOperationException("Static captures require the isolated graphics-enabled Editor.");
            var scene = EditorSceneManager.OpenScene(StudyPath);
            var rig = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<TopDown3DCameraRig>(true)).Single();
            var serialized = new SerializedObject(rig);
            rig.Configure((Transform)serialized.FindProperty("target").objectReferenceValue,
                (TopDown3DInputRouter)serialized.FindProperty("input").objectReferenceValue);
            var options = rig.EnableCameraOptions();
            var camera = rig.GetComponent<Camera>();
            Directory.CreateDirectory("Logs/CameraOptionsCaptures");
            Capture(camera, "Logs/CameraOptionsCaptures/elevated.png");
            if (!options.TrySetView(TopDown3DCameraView.ThirdPerson)) throw new InvalidOperationException(options.Notice);
            Capture(camera, "Logs/CameraOptionsCaptures/third-person.png");
            var report = "{\"kind\":\"EditMode static comparison\",\"width\":960,\"height\":540,\"cameraCount\":1,\"thirdPersonFov\":"
                + camera.fieldOfView.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}";
            File.WriteAllText("Logs/CameraOptionsCaptures/comparison.json", report);
            options.TrySetView(TopDown3DCameraView.Elevated);
            Debug.Log("CAMERA_OPTIONS_STATIC_CAPTURED");
        }

        private static void Capture(Camera camera, string path)
        {
            var target = RenderTexture.GetTemporary(960, 540, 24);
            var texture = new Texture2D(960, 540, TextureFormat.RGB24, false);
            var previousTarget = camera.targetTexture; var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget; RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target); UnityEngine.Object.DestroyImmediate(texture);
            }
        }

    }
}
