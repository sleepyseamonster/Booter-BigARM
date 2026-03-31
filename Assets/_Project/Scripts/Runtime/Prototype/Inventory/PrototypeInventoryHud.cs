using UnityEngine;

namespace BooterBigArm.Runtime
{
    [DisallowMultipleComponent]
    public sealed class PrototypeInventoryHud : MonoBehaviour
    {
        [SerializeField] private PrototypeInventory inventory;
        [SerializeField] private PrototypeHarvestInteractor harvestInteractor;
        [SerializeField, Min(1)] private int maxSlotLines = 8;

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

            var mass = inventory.CurrentCarryMass;
            var cap = inventory.MaxCarryMass;
            var massLabel = cap > 0f ? $"{mass:0.0}/{cap:0.0}" : $"{mass:0.0}";
            GUILayout.Label($"Slots: {inventory.SlotsUsed}/{inventory.SlotCapacity}  Mass: {massLabel}");
            GUILayout.Label($"Burden: {(inventory.BurdenFraction * 100f):0}%");

            var shown = 0;
            for (var i = 0; i < inventory.SlotCapacity && shown < maxSlotLines; i++)
            {
                if (!inventory.TryGetSlot(i, out var slot) || slot.IsEmpty)
                {
                    continue;
                }

                var itemLabel = slot.ItemId;
                inventory.TryGetItemDisplayName(slot.ItemId, out itemLabel);
                GUILayout.Label($"[{i:00}] {itemLabel} x{slot.Count}");
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
