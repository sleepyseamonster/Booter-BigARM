using UnityEngine;

namespace BooterBigArm.Runtime
{
    [DisallowMultipleComponent]
    public sealed class PrototypeInventoryHud : MonoBehaviour
    {
        [SerializeField] private PrototypeInventory inventory;
        [SerializeField] private PrototypeHarvestInteractor harvestInteractor;
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
            GUILayout.Label($"Resource types: {summaries.Length}");
            GUILayout.Label("Resources stack to 99 and do not count against carry.");

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

            GUILayout.EndArea();
        }
    }
}
