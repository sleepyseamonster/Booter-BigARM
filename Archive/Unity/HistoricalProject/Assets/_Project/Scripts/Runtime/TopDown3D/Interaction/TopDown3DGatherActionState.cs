using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public enum TopDown3DGatherActionResult
    {
        Rejected,
        Started,
        Running,
        Completed,
        Cancelled,
        InventoryFull,
        CommitFailed
    }

    public sealed class TopDown3DGatherActionState
    {
        private readonly TopDown3DInventoryState inventory;
        private readonly TopDown3DItemDestinationService destinations;
        private ITopDown3DInteractable target;
        private TopDown3DInventoryState destinationInventory;
        private float elapsed;
        private bool committed;

        public TopDown3DGatherActionState(TopDown3DInventoryState inventoryState)
        {
            inventory = inventoryState;
        }

        public TopDown3DGatherActionState(TopDown3DItemDestinationService destinationService)
        {
            destinations = destinationService;
            inventory = null;
        }

        public bool IsActive => target != null;
        public ITopDown3DInteractable Target => target;

        public TopDown3DGatherActionResult TryBegin(
            ITopDown3DInteractable candidate,
            Vector3 actorPosition)
        {
            if (IsActive || !IsValidTarget(candidate, actorPosition))
            {
                return TopDown3DGatherActionResult.Rejected;
            }

            destinationInventory = destinations != null ? destinations.Choose(candidate.Reward.ItemId, candidate.Reward.Quantity).Inventory : inventory;
            if (destinationInventory == null || !destinationInventory.CanAdd(candidate.Reward))
            {
                return TopDown3DGatherActionResult.InventoryFull;
            }

            target = candidate;
            elapsed = 0f;
            committed = false;
            return TopDown3DGatherActionResult.Started;
        }

        public TopDown3DGatherActionResult Advance(float deltaTime, Vector3 actorPosition)
        {
            if (!IsActive)
            {
                return TopDown3DGatherActionResult.Rejected;
            }

            if (!IsValidTarget(target, actorPosition))
            {
                Reset();
                return TopDown3DGatherActionResult.Cancelled;
            }

            elapsed += Mathf.Max(0f, deltaTime);
            if (elapsed < target.ActionDuration)
            {
                return TopDown3DGatherActionResult.Running;
            }

            if (destinationInventory == null || !destinationInventory.CanAdd(target.Reward))
            {
                Reset();
                return TopDown3DGatherActionResult.InventoryFull;
            }

            if (committed)
            {
                Reset();
                return TopDown3DGatherActionResult.Rejected;
            }

            committed = true;
            var reward = target.Reward;
            var addResult = destinationInventory.TryAdd(
                new[] { reward },
                () => target.TryConsume(out _));
            if (!addResult.Succeeded)
            {
                Reset();
                return addResult.Code == TopDown3DInventoryResultCode.InsufficientSpace
                    ? TopDown3DGatherActionResult.InventoryFull
                    : TopDown3DGatherActionResult.CommitFailed;
            }

            Reset();
            return TopDown3DGatherActionResult.Completed;
        }

        public TopDown3DGatherActionResult Cancel()
        {
            if (!IsActive)
            {
                return TopDown3DGatherActionResult.Rejected;
            }

            Reset();
            return TopDown3DGatherActionResult.Cancelled;
        }

        private static bool IsValidTarget(ITopDown3DInteractable candidate, Vector3 actorPosition)
        {
            return candidate != null
                && candidate.UnityObject != null
                && candidate.IsAvailable
                && candidate.ActionDuration > 0f
                && candidate.InteractionRange > 0f
                && Vector3.Distance(actorPosition, candidate.InteractionPoint)
                    <= candidate.InteractionRange;
        }

        private void Reset()
        {
            target = null;
            destinationInventory = null;
            elapsed = 0f;
            committed = false;
        }
    }
}
