using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace BooterBigArm.TopDown3D
{
    public static class TopDown3DCampSceneInstaller
    {
        public static TopDown3DCampController TryInstall(Scene scene, TopDown3DInputRouter input, TopDown3DPlayerInventory inventory, EventSystem system)
        {
            if (scene.path != "Assets/_Project/Scenes/Production/GreaterWasteland.unity"
                && scene.path != "Assets/_Project/Scenes/Reference/CampSleepStudy.unity") return null;
            var settings = TopDown3DCampSettings.Load();
            if (!settings || !settings.TryValidate(out _) || inventory == null || !inventory.ItemCatalog.TryGetDefinition(TopDown3DCampSettings.ItemId, out _)) return null;
            var controller = inventory.GetComponent<TopDown3DCampController>() ?? inventory.gameObject.AddComponent<TopDown3DCampController>();
            controller.Configure(settings, input, system); return controller;
        }
    }
}
