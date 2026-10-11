using System;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BooterBigArm.Editor
{
    public static class TopDown3DCampProductionValidator
    {
        public static void ValidateAllFromCli()
        {
            TopDown3DRadialProductionValidator.ValidateFromCli();
            TopDown3DMenuProductionValidator.ValidateFromCli();
            ValidateFromCli();
        }
        public static void ValidateFromCli()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Production/GreaterWasteland.unity", OpenSceneMode.Single);
            var settings = TopDown3DCampSettings.Load();
            if (settings == null || !settings.TryValidate(out _)) throw new InvalidOperationException("Camp assets or tuning incomplete");
            if (!settings.sleepClip.isHumanMotion || !settings.restClip.isHumanMotion) throw new InvalidOperationException("Camp poses must be Humanoid clips");
            var components = scene.GetRootGameObjects().SelectMany(value => value.GetComponentsInChildren<Component>(true)).ToArray();
            var inventory = components.OfType<TopDown3DPlayerInventory>().Single();
            if (inventory.StartingItems.Count(value => value.ItemId == TopDown3DCampSettings.ItemId && value.Quantity == 1) != 1)
                throw new InvalidOperationException("Production starter kit missing or duplicated");
            if (!inventory.ItemCatalog.TryGetDefinition(TopDown3DCampSettings.ItemId, out var item) || !item.TryValidate(out _))
                throw new InvalidOperationException("Camp definition missing");
            var input = components.OfType<TopDown3DInputRouter>().Single();
            var campMap = input.InputActions.FindActionMap("Camp", true);
            foreach (var name in new[] { "Move", "Look", "Confirm", "Cancel", "RotateLeft", "RotateRight", "FaceSun" }) campMap.FindAction(name, true);
            if (components.Any(value => value != null && (value.GetType().Name == "TopDown3DSaveService" || value.GetType().Name == "TopDown3DProceduralWorld")))
                throw new InvalidOperationException("Camp production depends on a generated-world service");
            if (components.OfType<PerpetualTwilightSun>().Single().CycleDurationSeconds != 1200) throw new InvalidOperationException("Sun baseline changed");
            var ui = TopDown3DInventoryUiSceneInstaller.TryInstallForScene(scene);
            var controller = inventory.GetComponent<TopDown3DCampController>();
            if (ui == null || controller == null || !controller.IsConfigured || controller.Record != null
                || controller.Phase != TopDown3DCampPhase.Idle) throw new InvalidOperationException("Camp installation failed");
            if (TopDown3DCampSceneInstaller.TryInstall(scene, input, inventory, ui.EventSystem) != controller)
                throw new InvalidOperationException("Repeated installation duplicates camp controller");
            if (scene.GetRootGameObjects().SelectMany(value => value.GetComponentsInChildren<UnityEngine.EventSystems.EventSystem>(true)).Count() != 1)
                throw new InvalidOperationException("Camp creates a second EventSystem");
            if (scene.GetRootGameObjects().SelectMany(value => value.GetComponentsInChildren<TopDown3DWorldSpeedController>(true)).Single().HasRequest)
                throw new InvalidOperationException("Startup acceleration active");
            Debug.Log("CAMP PRODUCTION VALIDATION PASSED: session-owned registry, one starter kit, complete meshes/poses/input, idempotent install, one EventSystem, no active acceleration or generated-world dependencies. No scene saved.");
        }
    }
}
