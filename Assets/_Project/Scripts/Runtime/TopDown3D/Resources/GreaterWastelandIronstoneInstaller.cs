using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Production-only resource overlay, independent of native terrain and its expansion authoring.</summary>
    public static class GreaterWastelandIronstoneInstaller
    {
        public const string ResourceName = "GreaterWastelandIronstone";
        public const string RootName = "Greater Wasteland Ironstone";

        public static GameObject TryInstall(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded || scene.name != "GreaterWasteland") return null;
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == RootName) return root;
            var prefab = Resources.Load<GameObject>(ResourceName);
            if (prefab == null)
            {
                Debug.LogError("Greater Wasteland ironstone prefab is missing.");
                return null;
            }
            var instance = Object.Instantiate(prefab);
            instance.name = RootName;
            SceneManager.MoveGameObjectToScene(instance, scene);
            return instance;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallLoadedScenes()
        {
            for (var i = 0; i < SceneManager.sceneCount; i++) TryInstall(SceneManager.GetSceneAt(i));
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => TryInstall(scene);
    }
}
