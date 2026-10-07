using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.TopDown3D
{
    public static class TopDown3DRadialSceneInstaller
    {
        public static TopDown3DRadialMenuController TryInstall(Scene scene, TopDown3DInputRouter input,
            TopDown3DPlayerInventory inventory, TopDown3DPlayerActionController action,
            TopDown3DInventoryUiController inventoryUi, TopDown3DInventoryCanvas inventoryCanvas, string preferenceDirectory = null)
        {
            if (input.InputActions.FindAction("System/OpenRadial", false) == null) return null;
            var existing = TopDown3DGameHudCanvas.FindInScene<TopDown3DRadialMenuController>(scene);
            if (existing != null) return existing;
            var follower = TopDown3DGameHudCanvas.FindInScene<TopDown3DBigArmFollower>(scene);
            var cargo = TopDown3DGameHudCanvas.FindInScene<TopDown3DBigArmCargo>(scene);
            // Authored world identity is the preserved Greater Wasteland asset GUID, not its mutable path.
            if (action != null && (scene.name == "GreaterWasteland" || scene.name == "GameplaySetup"))
            {
                var state = TopDown3DGameHudCanvas.FindInScene<TopDown3DPlacedHarvesterState>(scene);
                if (state == null)
                {
                    // Devices must remain at their placement point when Booter moves.
                    var registryRoot = new GameObject("Authored Canister State");
                    SceneManager.MoveGameObjectToScene(registryRoot, scene);
                    state = registryRoot.AddComponent<TopDown3DPlacedHarvesterState>();
                }
                state.ConfigureAuthored("4de5dd018ee194314a00fd369e2d3eeb", TopDown3DHarvesterSettings.Load());
                action.ConfigureHarvester(state, null);
            }
            var root = new GameObject("Radial Menu", typeof(RectTransform)); SceneManager.MoveGameObjectToScene(root, scene);
            var view = root.AddComponent<TopDown3DRadialCanvas>(); view.Build();
            var controller = root.AddComponent<TopDown3DRadialMenuController>();
            controller.Configure(input, inventory, inventoryUi, action, follower, cargo, view,
                preferenceDirectory == null ? null : System.IO.Path.Combine(preferenceDirectory, "radial-layout-v1.json"));
            inventoryCanvas.ConfigureRadialCustomization(controller.OpenCustomization);
            return controller;
        }
    }
}
