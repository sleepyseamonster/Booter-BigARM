using System;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BooterBigArm.Editor
{
    public static class TopDown3DRadialProductionValidator
    {
        public static void ValidateFromCli()
        {
            BadwaterPlayableSceneBuilder.ValidateFromCli();
            var errors = ConversionBaselineValidator.CollectErrors();
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var inventoryUi = TopDown3DInventoryUiSceneInstaller.TryInstallForScene(scene);
            var radial = TopDown3DGameHudCanvas.FindInScene<TopDown3DRadialMenuController>(scene);
            var devices = TopDown3DGameHudCanvas.FindInScene<TopDown3DPlacedHarvesterState>(scene);
            if (inventoryUi == null || radial == null || devices == null || !devices.IsAuthored)
                throw new InvalidOperationException("Production radial or authored canister wiring is missing.");
            if (devices.transform.parent != null || devices.GetComponent<TopDown3DPlayerInventory>() != null)
                throw new InvalidOperationException("Placed devices must have a stationary world owner, not Booter's transform.");
            var systems = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Component>(true))
                .Count(component => component != null && component.GetType().Name == "EventSystem");
            if (systems != 1) throw new InvalidOperationException("Production requires one EventSystem.");
            if (!radial.Preferences.IsValid() || radial.IsOpen)
                throw new InvalidOperationException("Radial defaults are invalid or start open.");
            Debug.Log("RADIAL PRODUCTION VALIDATION PASSED: authored world, one EventSystem, installed radial and canister authorities, valid closed menu.");
            // No scene or project content is saved by this validator.
        }
    }
}
