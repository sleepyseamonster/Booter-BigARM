using UnityEngine;

namespace BooterBigArm.Runtime
{
    [DisallowMultipleComponent]
    public sealed class PrototypeInventoryHud : MonoBehaviour
    {
        [SerializeField] private PrototypeInventory inventory;
        [SerializeField] private PrototypeHarvestInteractor harvestInteractor;
        [SerializeField] private PrototypeDustCanisterController dustCanisterController;
        [SerializeField, Min(1)] private int maxSummaryLines = 8;

        private void Awake()
        {
            if (inventory == null)
            {
                inventory = FindAnyObjectByType<PrototypeInventory>();
            }

            if (harvestInteractor == null)
            {
                harvestInteractor = FindAnyObjectByType<PrototypeHarvestInteractor>();
            }

            if (dustCanisterController == null)
            {
                dustCanisterController = FindAnyObjectByType<PrototypeDustCanisterController>();
            }
        }

        private void OnGUI()
        {
            if (inventory == null)
            {
                return;
            }

            const int width = 360;
            const int height = 320;

            GUILayout.BeginArea(new Rect(12f, 210f, width, height), GUI.skin.box);
            GUILayout.Label("Inventory");

            var summaries = inventory.GetItemSummaries();
            GUILayout.Label($"Item types: {summaries.Length}");
            GUILayout.Label("Resources stack to 99. Tools use normal carry.");

            var shown = 0;
            for (var i = 0; i < summaries.Length && shown < maxSummaryLines; i++)
            {
                var summary = summaries[i];
                if (string.IsNullOrWhiteSpace(summary.ItemId) || summary.TotalCount <= 0)
                {
                    continue;
                }

                var stackLabel = summary.StackCount > 1 ? $" ({summary.StackCount} stacks)" : string.Empty;
                GUILayout.Label($"{summary.DisplayName} x{summary.TotalCount}{stackLabel}");
                shown++;
            }

            if (shown == 0)
            {
                GUILayout.Label("(empty)");
            }

            if (harvestInteractor != null)
            {
                GUILayout.Space(6f);
                GUILayout.Label("Harvest");
                if (harvestInteractor.CurrentNode != null)
                {
                    GUILayout.Label($"{harvestInteractor.CurrentNode.DisplayName} ({harvestInteractor.CurrentNode.Kind})");
                }
                else
                {
                    GUILayout.Label("No node in range");
                }

                GUILayout.Label(harvestInteractor.LastMessage);
            }

            if (dustCanisterController != null)
            {
                GUILayout.Space(6f);
                GUILayout.Label("Dust Canister");
                GUILayout.Label("D-pad down deploys. East picks up.");
                if (dustCanisterController.ActiveCanister != null)
                {
                    GUILayout.Label($"Deployed: {dustCanisterController.ActiveCanister.StoredDust:0}/{dustCanisterController.ActiveCanister.MaxDust:0} dust");
                }
                else
                {
                    GUILayout.Label("In inventory");
                }

                if (!string.IsNullOrWhiteSpace(dustCanisterController.LastStatusMessage))
                {
                    GUILayout.Label(dustCanisterController.LastStatusMessage);
                }
            }

            GUILayout.EndArea();
        }
    }
}
