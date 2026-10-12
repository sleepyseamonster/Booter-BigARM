using System;
using UnityEngine.InputSystem;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DInputRouter
    {
        private InputAction cancelItemAction;
        public event Action ItemActionCancelRequested;
        private void BindInventoryActions()
        {
            cancelItemAction = gameplayMap?.FindAction("CancelItemAction", false);
            if (cancelItemAction != null) cancelItemAction.performed += HandleItemCancel;
        }
        private void UnbindInventoryActions()
        {
            if (cancelItemAction != null) cancelItemAction.performed -= HandleItemCancel;
            cancelItemAction = null;
        }
        private void HandleItemCancel(InputAction.CallbackContext context)
        {
            if (Mode == TopDown3DInputMode.Gameplay) ItemActionCancelRequested?.Invoke();
        }
    }
}
