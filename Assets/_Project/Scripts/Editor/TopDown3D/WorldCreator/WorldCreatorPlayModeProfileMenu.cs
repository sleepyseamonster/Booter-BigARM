using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Editor.TopDown3D.WorldCreator
{
    [InitializeOnLoad]
    internal static class WorldCreatorPlayModeProfileMenu
    {
        private const string MenuPath = "Booter & BigARM/World Creator/Full-Content Play Mode";

        static WorldCreatorPlayModeProfileMenu()
        {
            EditorApplication.delayCall += RefreshCheckmark;
        }

        [MenuItem(MenuPath, priority = 200)]
        private static void ToggleFullContentPlayMode()
        {
            var enabled = !EditorPrefs.GetBool(
                TopDown3DPlaytestPerformanceProfile.FullContentEditorPreferenceKey,
                false);
            EditorPrefs.SetBool(
                TopDown3DPlaytestPerformanceProfile.FullContentEditorPreferenceKey,
                enabled);
            Menu.SetChecked(MenuPath, enabled);
            Debug.Log(
                enabled
                    ? "[World Creator] Full-content Play Mode enabled. The next Play session will use production landscape settings with telemetry."
                    : "[World Creator] Stress Play Mode restored. The next Play session will use the low-cost diagnostic profile.");
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateFullContentPlayMode()
        {
            RefreshCheckmark();
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        private static void RefreshCheckmark()
        {
            Menu.SetChecked(
                MenuPath,
                EditorPrefs.GetBool(
                    TopDown3DPlaytestPerformanceProfile.FullContentEditorPreferenceKey,
                    false));
        }
    }
}
