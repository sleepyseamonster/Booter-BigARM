using UnityEditor;
using UnityEngine;

namespace BooterBigArm.TopDown3D.Editor
{
    internal static class TopDown3DHarvesterPlaytestGrant
    {
        [MenuItem("Booter & BigARM/Top Down 3D/Grant Micro Dust Harvester (Play Mode)")]
        private static void Grant()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Enter Play Mode before granting a micro dust harvester.");
                return;
            }

            var inventory = Object.FindFirstObjectByType<TopDown3DPlayerInventory>();
            if (inventory == null)
            {
                Debug.LogWarning("Booter's TopDown3D inventory is not available.");
                return;
            }

            var result = inventory.State.TryAdd(new TopDown3DItemAmount(
                TopDown3DHarvesterSettings.CanisterItemId, 1));
            if (result.Succeeded)
                Debug.Log("Granted one micro dust harvester. Use Deploy Canister to preview and place it.");
            else
                Debug.LogWarning($"Could not grant a micro dust harvester: {result.Message}");
        }
    }
}
