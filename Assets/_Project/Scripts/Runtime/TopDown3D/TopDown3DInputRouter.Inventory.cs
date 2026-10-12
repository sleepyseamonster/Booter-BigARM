using System;
using UnityEngine.InputSystem;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DInputRouter
    {
        private InputAction cancelItemAction;
        private InputAction openCargoAction;
        private bool cargoRequested;
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
        }
        private void HandleItemCancel(InputAction.CallbackContext context)
        {
            if (Mode == TopDown3DInputMode.Gameplay) ItemActionCancelRequested?.Invoke();
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
