using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    public sealed class TopDown3DPlayerActionController : MonoBehaviour
    {
        [SerializeField] private TopDown3DInputRouter input;
        [SerializeField] private TopDown3DInteractionController interaction;
        [SerializeField] private TopDown3DPlayerInventory inventory;
        [SerializeField] private TopDown3DPlayerMotor motor;
        [SerializeField] private TopDown3DPlayerAnimationDriver animationDriver;

        private TopDown3DGatherActionState gatherAction;
        private TopDown3DBigArmCargo bigArmCargo;
        private bool subscribed;

        public bool IsGathering => gatherAction != null && gatherAction.IsActive;
        public event Action<string> FeedbackRequested;
        public event Action<bool> GatheringChanged;
        public event Action<TopDown3DBigArmCargo> CargoAccessRequested;

        public void Configure(
            TopDown3DInputRouter inputRouter,
            TopDown3DInteractionController interactionController,
            TopDown3DPlayerInventory playerInventory,
            TopDown3DPlayerMotor playerMotor,
            TopDown3DPlayerAnimationDriver playerAnimation)
        {
            Unsubscribe();
            input = inputRouter;
            interaction = interactionController;
            inventory = playerInventory;
            motor = playerMotor;
            animationDriver = playerAnimation;
            RebuildGatherAction();
            Subscribe();
        }

        public void ConfigureCargo(TopDown3DBigArmCargo cargo)
        {
            bigArmCargo = cargo;
            RebuildGatherAction();
        }

        public TopDown3DGatherActionResult TryBeginCurrentTarget()
        {
            EnsureGatherAction();
            if (gatherAction == null || interaction == null || motor == null || animationDriver == null)
            {
                return TopDown3DGatherActionResult.Rejected;
            }

            var target = interaction.CurrentTarget;
            if (target is TopDown3DBigArmCargoAccess)
            {
                CargoAccessRequested?.Invoke(((TopDown3DBigArmCargoAccess)target).Cargo);
                return TopDown3DGatherActionResult.Completed;
            }
            var result = gatherAction.TryBegin(target, motor.Position);
            if (result == TopDown3DGatherActionResult.InventoryFull)
            {
                FeedbackRequested?.Invoke("Inventory full");
                return result;
            }

            if (result != TopDown3DGatherActionResult.Started)
            {
                return result;
            }

            if (!motor.TryBeginActionConstraint(this)
                || !animationDriver.TryBeginGather(target.ActionDuration))
            {
                gatherAction.Cancel();
                motor.EndActionConstraint(this);
                animationDriver.EndGather();
                return TopDown3DGatherActionResult.Rejected;
            }

            motor.FacePlanarDirection(target.InteractionPoint - motor.Position);
            GatheringChanged?.Invoke(true);
            return result;
        }

        public void CancelGather()
        {
            if (gatherAction == null || !gatherAction.IsActive)
            {
                return;
            }

            gatherAction.Cancel();
            EndPresentation();
        }

        private void Update()
        {
            if (gatherAction == null || !gatherAction.IsActive || motor == null)
            {
                return;
            }

            var target = gatherAction.Target;
            var reward = target != null ? target.Reward : default;
            var result = gatherAction.Advance(Time.deltaTime, motor.Position);
            switch (result)
            {
                case TopDown3DGatherActionResult.Running:
                    return;
                case TopDown3DGatherActionResult.Completed:
                    EndPresentation();
                    FeedbackRequested?.Invoke($"+{reward.Quantity} {ResolveRewardName(reward.ItemId)}");
                    return;
                case TopDown3DGatherActionResult.InventoryFull:
                    EndPresentation();
                    FeedbackRequested?.Invoke("Inventory full");
                    return;
                case TopDown3DGatherActionResult.Cancelled:
                case TopDown3DGatherActionResult.CommitFailed:
                case TopDown3DGatherActionResult.Rejected:
                    EndPresentation();
                    return;
            }
        }

        private string ResolveRewardName(string itemId)
        {
            return inventory != null && inventory.ItemCatalog != null
                && inventory.ItemCatalog.TryGetDefinition(itemId, out var definition)
                ? definition.DisplayName
                : itemId;
        }

        private void HandleInteractRequested()
        {
            TryBeginCurrentTarget();
        }

        private void EnsureGatherAction()
        {
            if (gatherAction == null && inventory != null)
            {
                RebuildGatherAction();
            }
        }

        private void RebuildGatherAction()
        {
            if (inventory == null)
            {
                gatherAction = null;
                return;
            }

            var destinations = new TopDown3DItemDestinationService(
                inventory.State,
                bigArmCargo != null ? bigArmCargo.State : null,
                () => bigArmCargo != null
                    && bigArmCargo.IsInAccessRange(motor != null ? motor.Position : transform.position));
            gatherAction = new TopDown3DGatherActionState(destinations);
        }

        private void EndPresentation()
        {
            animationDriver?.EndGather();
            motor?.EndActionConstraint(this);
            GatheringChanged?.Invoke(false);
        }

        private void Subscribe()
        {
            if (subscribed || !isActiveAndEnabled || input == null)
            {
                return;
            }

            input.InteractRequested += HandleInteractRequested;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (subscribed && input != null)
            {
                input.InteractRequested -= HandleInteractRequested;
            }

            subscribed = false;
        }

        private void OnEnable()
        {
            EnsureGatherAction();
            Subscribe();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsureGatherAction();
        }
#endif

        private void OnDisable()
        {
            Unsubscribe();
            CancelGather();
            motor?.EndActionConstraint(this);
            animationDriver?.EndGather();
        }
    }
}
