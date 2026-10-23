using System;
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
        CommitFailed,
        AwaitingCargo,
        DestinationUnavailable
    }

    public sealed class TopDown3DGatherActionState
    {
        private readonly TopDown3DInventoryState inventory;
        private readonly TopDown3DItemDestinationService destinations;
        private readonly Func<ITopDown3DInteractable, bool> canReachTarget;
        private ITopDown3DInteractable target;
        private TopDown3DInteractionTarget capturedTarget;
        private TopDown3DInventoryState destinationInventory;
        private float elapsed;
        private bool committed;

        public TopDown3DGatherActionState(TopDown3DInventoryState inventoryState)
        {
            inventory = inventoryState;
        }

        public TopDown3DGatherActionState(TopDown3DItemDestinationService destinationService,
            Func<ITopDown3DInteractable, bool> reachability = null)
        {
            destinations = destinationService;
            inventory = null;
            canReachTarget = reachability;
        }

        public bool IsActive => target != null;
        public ITopDown3DInteractable Target => target;
        public bool LastCompletionWasBigArm { get; private set; }
        public string FailureMessage { get; private set; }

        public TopDown3DGatherActionResult TryBegin(
            ITopDown3DInteractable candidate,
            Vector3 actorPosition)
        {
            if (IsActive || !IsValidTarget(candidate, actorPosition))
            {
                return TopDown3DGatherActionResult.Rejected;
            }

            var destination = destinations != null ? destinations.Choose(candidate.Reward.ItemId, candidate.Reward.Quantity)
                : new TopDown3DItemDestination(inventory, false);
            destinationInventory = destination.Inventory;
            FailureMessage = destination.Message;
            if (destinationInventory == null || !destinationInventory.CanAdd(candidate.Reward))
            {
                return destination.Code == TopDown3DInventoryResultCode.DestinationUnavailable
                    ? TopDown3DGatherActionResult.DestinationUnavailable : TopDown3DGatherActionResult.InventoryFull;
            }

            target = candidate;
            capturedTarget = new TopDown3DInteractionTarget(candidate);
            elapsed = 0f;
            committed = false;
            LastCompletionWasBigArm = false;
            return TopDown3DGatherActionResult.Started;
        }

        public TopDown3DGatherActionResult Advance(float deltaTime, Vector3 actorPosition)
        {
            if (!IsActive)
            {
                return TopDown3DGatherActionResult.Rejected;
            }

            if (!capturedTarget.IsValid || !IsValidTarget(target, actorPosition))
            {
                Reset();
                return TopDown3DGatherActionResult.Cancelled;
            }

            elapsed += Mathf.Max(0f, deltaTime);
            if (elapsed < target.ActionDuration)
            {
                return TopDown3DGatherActionResult.Running;
            }

            // Cargo may have moved or filled during the animation. Resolve the destination at commit.
            var destination = destinations != null
                ? destinations.Choose(target.Reward.ItemId, target.Reward.Quantity)
                : new TopDown3DItemDestination(inventory, false);
            destinationInventory = destination.Inventory;
            FailureMessage = destination.Message;
            if (destinationInventory == null || !destinationInventory.CanAdd(target.Reward))
            {
                Reset();
                return destination.Code == TopDown3DInventoryResultCode.DestinationUnavailable
                    ? TopDown3DGatherActionResult.DestinationUnavailable : TopDown3DGatherActionResult.InventoryFull;
            }

            if (committed)
            {
                Reset();
                return TopDown3DGatherActionResult.Rejected;
            }

            committed = true;
            if(target is ICampDurableGatherTarget durable)
            {
                bool success=capturedTarget.IsValid && IsValidTarget(target,actorPosition) && durable.TryCommitHarvest(destinationInventory,out var error);
                if(!success)FailureMessage=durable.Failure;
                LastCompletionWasBigArm=success&&destination.IsBigArm;Reset();
                return success?TopDown3DGatherActionResult.Completed:TopDown3DGatherActionResult.CommitFailed;
            }
            var reward = target.Reward;
            var addResult = destinationInventory.TryAdd(
                new[] { reward },
                () => capturedTarget.IsValid && IsValidTarget(target, actorPosition) && target.TryConsume(out _));
            if (!addResult.Succeeded)
            {
                Reset();
                return addResult.Code == TopDown3DInventoryResultCode.InsufficientSpace
                    ? TopDown3DGatherActionResult.InventoryFull
                    : TopDown3DGatherActionResult.CommitFailed;
            }

            LastCompletionWasBigArm = destination.IsBigArm;
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

        private bool IsValidTarget(ITopDown3DInteractable candidate, Vector3 actorPosition)
        {
            return candidate != null
                && candidate.UnityObject != null
                && candidate.IsAvailable
                && candidate.ActionDuration > 0f
                && candidate.InteractionRange > 0f
                && Vector3.Distance(actorPosition, candidate.InteractionPoint)
                    <= candidate.InteractionRange
                && (canReachTarget == null || canReachTarget(candidate));
        }

        private void Reset()
        {
            target = null;
            capturedTarget = default;
            destinationInventory = null;
            elapsed = 0f;
            committed = false;
        }
    }
}
