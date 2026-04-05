using UnityEngine;

namespace BooterBigArm.Runtime
{
    [DisallowMultipleComponent]
    public sealed class PrototypeInventoryHud : MonoBehaviour
    {
        [SerializeField] private string title = "Inventory";
        [SerializeField] private PrototypeInventory inventory;
        [SerializeField] private PrototypeInventory transferTarget;
        [SerializeField] private string transferButtonLabel = "Transfer";
        [SerializeField] private Vector2 panelPosition = new Vector2(12f, 210f);
        [SerializeField] private Vector2 panelSize = new Vector2(360f, 320f);
        [SerializeField] private Vector2 scrollPosition;
        [SerializeField] private bool isVisible;

        private string lastTransferMessage = string.Empty;

        public bool IsVisible => isVisible;

        public void Configure(
            string hudTitle,
            PrototypeInventory sourceInventory,
            PrototypeInventory targetInventory,
            string buttonLabel,
            Vector2 position,
            Vector2 size)
        {
            title = string.IsNullOrWhiteSpace(hudTitle) ? "Inventory" : hudTitle;
            inventory = sourceInventory;
            transferTarget = targetInventory;
            transferButtonLabel = string.IsNullOrWhiteSpace(buttonLabel) ? "Transfer" : buttonLabel;
            panelPosition = position;
            panelSize = size;
        }

        public void SetVisible(bool visible)
        {
            isVisible = visible;
        }

        private void Awake()
        {
            if (inventory == null)
            {
                inventory = FindAnyObjectByType<PrototypeInventory>();
            }
        }

        private void OnGUI()
        {
            if (!isVisible || inventory == null)
            {
                return;
            }

            var rect = new Rect(panelPosition.x, panelPosition.y, panelSize.x, panelSize.y);
            GUILayout.BeginArea(rect, GUI.skin.box);
            GUILayout.Label(title);
            GUILayout.Label($"Slots: {inventory.SlotsUsed}/{inventory.SlotCapacity}");
            if (inventory.MaxCarryMass > 0f)
            {
                GUILayout.Label($"Carry: {inventory.CurrentCarryMass:0.0}/{inventory.MaxCarryMass:0.0}");
                GUILayout.Label($"Load: {inventory.BurdenFraction:0.00}");
            }
            else
            {
                GUILayout.Label("Storage: unlimited");
            }

            if (!string.IsNullOrWhiteSpace(lastTransferMessage))
            {
                GUILayout.Label(lastTransferMessage);
            }

            var summaries = inventory.GetItemSummaries();
            var scrollHeight = Mathf.Max(110f, panelSize.y - 112f);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(scrollHeight));
            if (summaries.Length == 0)
            {
                GUILayout.Label("(empty)");
            }
            else
            {
                for (var i = 0; i < summaries.Length; i++)
                {
                    var summary = summaries[i];
                    if (string.IsNullOrWhiteSpace(summary.ItemId) || summary.TotalCount <= 0)
                    {
                        continue;
                    }

                    GUILayout.BeginHorizontal();
                    var stackSuffix = summary.StackCount > 1 ? $" ({summary.StackCount} stacks)" : string.Empty;
                    GUILayout.Label($"{summary.DisplayName} x{summary.TotalCount}{stackSuffix}");

                    if (transferTarget != null && !ReferenceEquals(transferTarget, inventory))
                    {
                        var canTransfer = GUILayout.Button(transferButtonLabel, GUILayout.Width(94f));
                        if (canTransfer)
                        {
                            if (inventory.TryTransferItemTo(transferTarget, summary.ItemId, summary.TotalCount, out var moved) && moved > 0)
                            {
                                lastTransferMessage = $"{transferButtonLabel} {moved} {summary.DisplayName}.";
                            }
                            else
                            {
                                lastTransferMessage = $"Unable to {transferButtonLabel.ToLowerInvariant()} {summary.DisplayName}.";
                            }
                        }
                    }

                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
