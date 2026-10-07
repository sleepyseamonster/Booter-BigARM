using UnityEngine;

namespace BooterBigArm.Runtime
{
    [DisallowMultipleComponent]
    public sealed class IsometricSpikeOverlay : MonoBehaviour
    {
        [SerializeField] private IsometricCameraProjectionToggle projectionToggle;
        [SerializeField] private IsometricHarvestInteractor3D harvestInteractor;
        [SerializeField] private PrototypeInventory inventory;

        public void Configure(
            IsometricCameraProjectionToggle cameraToggle,
            IsometricHarvestInteractor3D interactor,
            PrototypeInventory playerInventory)
        {
            projectionToggle = cameraToggle;
            harvestInteractor = interactor;
            inventory = playerInventory;
        }

        private void OnGUI()
        {
            var label = projectionToggle != null ? projectionToggle.ProjectionLabel : "Unknown projection";
            var prompt = harvestInteractor != null ? harvestInteractor.Prompt : "";
            var ironstone = inventory != null ? inventory.GetItemCount("ironstone") : 0;
            var scrap = inventory != null ? inventory.GetItemCount("scrap_metal") : 0;

            GUILayout.BeginArea(new Rect(16f, 16f, 420f, 155f), GUI.skin.box);
            GUILayout.Label("ISOMETRIC CONVERSION LAB — protected spike");
            GUILayout.Label($"Camera: {label} (P toggles comparison)");
            GUILayout.Label("Move: gamepad left stick / WASD   Sprint: assigned Sprint control");
            GUILayout.Label("Interact: hold assigned Interact control   Recall BigARM: assigned Recall control");
            GUILayout.Label($"Inventory proof — Ironstone: {ironstone}   Scrap: {scrap}");
            if (!string.IsNullOrWhiteSpace(prompt))
            {
                GUILayout.Label(prompt);
            }

            GUILayout.EndArea();
        }
    }
}
