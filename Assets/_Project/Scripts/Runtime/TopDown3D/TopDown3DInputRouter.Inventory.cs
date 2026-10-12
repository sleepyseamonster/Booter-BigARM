using System;
using UnityEngine.InputSystem;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DInputRouter
    {
        private InputAction cancelItemAction;
        private InputAction openCargoAction;
        private bool cargoRequested;
        private bool interactRequested, pickupRequested, itemCancelRequested;
        public event Action InteractionPressStarted;
        public bool IsCargoHoldActive => Mode == TopDown3DInputMode.Gameplay
            && openCargoAction?.phase == InputActionPhase.Started;
        public float CargoHoldProgress => IsCargoHoldActive ? openCargoAction.GetTimeoutCompletionPercentage() : 0f;
        public event Action CargoOpenRequested;
        public event Action ItemActionCancelRequested;
        private void BindInventoryActions()
        {
            cancelItemAction = gameplayMap?.FindAction("CancelItemAction", false);
            if (cancelItemAction != null) cancelItemAction.performed += HandleItemCancel;
            openCargoAction = gameplayMap?.FindAction("OpenCargo", false);
            if (openCargoAction != null) openCargoAction.performed += HandleCargoOpen;
        }
        private void UnbindInventoryActions()
        {
            if (cancelItemAction != null) cancelItemAction.performed -= HandleItemCancel;
            cancelItemAction = null;
            if (openCargoAction != null) openCargoAction.performed -= HandleCargoOpen;
            openCargoAction = null;
            cargoRequested = false;
            interactRequested = pickupRequested = itemCancelRequested = false;
        }
        private void HandleItemCancel(InputAction.CallbackContext context)
        {
            if (CanCaptureGameplayCommand) itemCancelRequested = true;
        }
        private bool CanCaptureGameplayCommand => Mode == TopDown3DInputMode.Gameplay
            && inputUpdateMode == TopDown3DInputMode.Gameplay
            && (!UnityEngine.Application.isPlaying || UnityEngine.Application.isFocused);
        private void HandleInteractionStarted(InputAction.CallbackContext context)
        {
            if (CanCaptureGameplayCommand) InteractionPressStarted?.Invoke();
        }
        private bool ResolveGameplayItemInput(bool modalRequested)
        {
            var interact = interactRequested; var pickup = pickupRequested; var cancel = itemCancelRequested;
            interactRequested = pickupRequested = itemCancelRequested = false;
            if (modalRequested || !CanCaptureGameplayCommand) return false;
            // Cancel wins independently of callback/binding order. Never queue a later world action.
            if (cancel) { ItemActionCancelRequested?.Invoke(); return true; }
            if (pickup) { PickupCanisterRequested?.Invoke(); return true; }
            if (interact) InteractRequested?.Invoke();
            return interact;
        }
        private void HandleCargoOpen(InputAction.CallbackContext context)
        {
            if (Mode == TopDown3DInputMode.Gameplay && inputUpdateMode == TopDown3DInputMode.Gameplay
                && (!UnityEngine.Application.isPlaying || UnityEngine.Application.isFocused)) cargoRequested = true;
        }
        private void ResolveCargoInput(bool modalRequested)
        {
            var requested = cargoRequested;
            cargoRequested = false;
            if (requested && !modalRequested && Mode == TopDown3DInputMode.Gameplay
                && inputUpdateMode == TopDown3DInputMode.Gameplay) CargoOpenRequested?.Invoke();
        }
    }
}
