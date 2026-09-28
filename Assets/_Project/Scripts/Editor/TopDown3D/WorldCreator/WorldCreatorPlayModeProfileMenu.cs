using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Editor.TopDown3D.WorldCreator
{
    [InitializeOnLoad]
    internal static class WorldCreatorPlayModeProfileMenu
    {
        private const string MenuPath = "Booter & BigARM/World Creator/Reduced Stress Play Mode";

        static WorldCreatorPlayModeProfileMenu()
        {
            EditorApplication.delayCall += RefreshCheckmark;
        }

        [MenuItem(MenuPath, priority = 200)]
        private static void ToggleStressPlayMode()
        {
            var enabled = !EditorPrefs.GetBool(
                TopDown3DPlaytestPerformanceProfile.StressEditorPreferenceKey,
                false);
            EditorPrefs.SetBool(
                TopDown3DPlaytestPerformanceProfile.StressEditorPreferenceKey,
                enabled);
            Menu.SetChecked(MenuPath, enabled);
            Debug.Log(
                enabled
                    ? "[World Creator] Reduced stress Play Mode enabled for the next Play session."
                    : "[World Creator] Full-content Play Mode restored for the next Play session.");
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateStressPlayMode()
        {
            RefreshCheckmark();
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        private static void RefreshCheckmark()
        {
            Menu.SetChecked(
                MenuPath,
                EditorPrefs.GetBool(
                    TopDown3DPlaytestPerformanceProfile.StressEditorPreferenceKey,
                    false));
        }
    }
}
