using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Editor
{
    [InitializeOnLoad]
    public static class TopDown3DGameHudSceneInstaller
    {
        public const string ScenePath = "Assets/_Project/Scenes/TopDown3D/TopDown3DPrototype.unity";

        static TopDown3DGameHudSceneInstaller()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            ScheduleEnsure();
        }

        [MenuItem("Booter & BigARM/Top Down 3D/Ensure Editable Game HUD")]
        public static void EnsureFromMenu()
        {
            EnsureEditableHudForActiveScene(logResult: true);
        }

        public static TopDown3DActionDpadHud EnsureEditableHudForActiveScene(bool logResult = false)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (logResult)
                {
                    Debug.LogWarning("Exit Play Mode before installing the editable Game HUD scene objects.");
                }

                return null;
            }

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath)
            {
                if (logResult)
                {
                    Debug.LogWarning($"Open {ScenePath} before installing the editable Game HUD.");
                }

                return null;
            }

            var existingDpad = TopDown3DGameHudCanvas.FindInScene<TopDown3DActionDpadHud>(scene);
            if (existingDpad != null)
            {
                if (logResult)
                {
                    Debug.Log("The editable Action D-Pad HUD is already present in the active scene.", existingDpad);
                }

                return existingDpad;
            }

            var existingCanvas = TopDown3DGameHudCanvas.FindInScene<TopDown3DGameHudCanvas>(scene);
            var dpad = TopDown3DActionDpadHud.TryInstallForScene(scene);
            if (dpad == null)
            {
                Debug.LogError("Could not install the editable Action D-Pad HUD because the scene has no gameplay marker.");
                return null;
            }

            var gameHud = dpad.GetComponentInParent<TopDown3DGameHudCanvas>();
            var undoRoot = existingCanvas == null ? gameHud.gameObject : dpad.gameObject;
            Undo.RegisterCreatedObjectUndo(undoRoot, "Install Editable Game HUD");
            EditorSceneManager.MarkSceneDirty(scene);

            Debug.Log(
                "Installed Game HUD Canvas > Action D-Pad HUD > D-Pad Indicator as editable scene objects. Review them in the Hierarchy, then save the scene.",
                dpad);
            return dpad;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                ScheduleEnsure();
            }
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (scene.path == ScenePath)
            {
                ScheduleEnsure();
            }
        }

        private static void ScheduleEnsure()
        {
            EditorApplication.delayCall -= EnsureAfterDelay;
            EditorApplication.delayCall += EnsureAfterDelay;
        }

        private static void EnsureAfterDelay()
        {
            EnsureEditableHudForActiveScene();
        }
    }
}
