using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace BooterBigArm.TopDown3D
{
    public static class TopDown3DMenuSceneInstaller
    {
        public static TopDown3DMenuController TryInstall(Scene scene, TopDown3DInputRouter input,
            TopDown3DInventoryUiController inventory, EventSystem eventSystem, string preferenceDirectory = null)
        {
            var existing = TopDown3DGameHudCanvas.FindInScene<TopDown3DMenuController>(scene);
            if (existing != null && existing.IsConfigured) return existing;
            var radial = TopDown3DGameHudCanvas.FindInScene<TopDown3DRadialMenuController>(scene);
            var prefab = Resources.Load<GameObject>("SystemMenu");
            if (radial == null || prefab == null || eventSystem == null)
            { Debug.LogError("System menu requires SystemMenu.prefab, radial controller, and the existing EventSystem."); return null; }
            var instance = Object.Instantiate(prefab); instance.name = "System Menu";
            instance.SetActive(false); SceneManager.MoveGameObjectToScene(instance, scene);
            var view = instance.GetComponent<TopDown3DMenuView>();
            var controller = instance.AddComponent<TopDown3DMenuController>();
            var range = TopDown3DGameHudCanvas.FindInScene<BadwaterCameraRange>(scene);
            controller.Configure(input, inventory, radial, view, eventSystem.GetComponent<InputSystemUIInputModule>(), preferenceDirectory, range);
            instance.SetActive(true);
            return controller;
        }
    }
}
