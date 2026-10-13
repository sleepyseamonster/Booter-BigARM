using UnityEngine.SceneManagement;

namespace BooterBigArm.TopDown3D
{
    /// <summary>The fixed authored world and its bounded camp-loop test slice share content identities.</summary>
    public static class GreaterWastelandSceneScope
    {
        public const string CampLoopScene = "Assets/_Project/Scenes/Reference/CampLoopTesting.unity";
        public static bool IsCampLoop(Scene scene) => scene.path == CampLoopScene;
        public static bool SupportsAuthoredLoop(Scene scene) => scene.path == TopDown3DWaypointCampSettings.ProductionScene
            || IsCampLoop(scene);
        public static string CheckpointFolder(Scene scene) => IsCampLoop(scene) ? "CampLoopTesting" : "GreaterWasteland";
    }
}
