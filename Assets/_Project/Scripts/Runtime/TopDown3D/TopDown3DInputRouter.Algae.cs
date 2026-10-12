using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DInputRouter
    {
        private InputAction drinkAlgae, eatAlgae, treatAlgae;
        private int algaeRequests;
        private bool algaeAwaitNeutral;
        private bool algaeAmbiguous;
        private DpadControl algaeOriginDpad;
        public bool HasAlgaeActions => drinkAlgae != null && eatAlgae != null && treatAlgae != null;
        public event Action<TopDown3DAlgaeUse> AlgaeUseRequested;

        private void BindAlgaeActions()
        {
            drinkAlgae = gameplayMap?.FindAction("DrinkAlgae", false);
            eatAlgae = gameplayMap?.FindAction("EatAlgae", false);
            treatAlgae = gameplayMap?.FindAction("TreatWithAlgae", false);
            if (HasAlgaeActions)
            {
                drinkAlgae.performed += CaptureAlgae;
                eatAlgae.performed += CaptureAlgae;
                treatAlgae.performed += CaptureAlgae;
            }
            DisarmAlgae();
        }
        private void UnbindAlgaeActions()
        {
            if (drinkAlgae != null) drinkAlgae.performed -= CaptureAlgae;
            if (eatAlgae != null) eatAlgae.performed -= CaptureAlgae;
            if (treatAlgae != null) treatAlgae.performed -= CaptureAlgae;
            drinkAlgae = eatAlgae = treatAlgae = null; algaeOriginDpad = null; DisarmAlgae();
        }
        private void DisarmAlgae() { algaeRequests = 0; algaeAwaitNeutral = true; algaeOriginDpad = null; algaeAmbiguous = false; }
        private void CaptureAlgae(InputAction.CallbackContext context)
        {
            if (algaeAwaitNeutral || inputUpdateMode != TopDown3DInputMode.Gameplay
                || Mode != TopDown3DInputMode.Gameplay || Application.isPlaying && !Application.isFocused) return;
            if (context.control.parent is DpadControl dpad)
            {
                algaeOriginDpad = dpad;
                var value = dpad.ReadValue();
                algaeAmbiguous |= Mathf.Abs(value.x) > .1f && Mathf.Abs(value.y) > .1f;
            }
            algaeRequests |= context.action == drinkAlgae ? 1 : context.action == eatAlgae ? 2 : 4;
        }
        private static bool PhysicalHeld(InputAction action)
        {
            if (action == null) return false;
            foreach (var control in action.controls) if (control.device.added && control is ButtonControl button && button.isPressed) return true;
            return false;
        }
        private void ResolveAlgaeInput(bool modalRequested)
        {
            if (!HasAlgaeActions) return;
            var requests = algaeRequests; algaeRequests = 0;
            var ambiguous = algaeAmbiguous; algaeAmbiguous = false;
            var heldCount = (PhysicalHeld(drinkAlgae) ? 1 : 0) + (PhysicalHeld(eatAlgae) ? 1 : 0) + (PhysicalHeld(treatAlgae) ? 1 : 0);
            var dpadValue = algaeOriginDpad != null && algaeOriginDpad.device.added ? algaeOriginDpad.ReadValue() : Vector2.zero;
            if (heldCount == 0 && dpadValue.sqrMagnitude < .01f)
            { algaeAwaitNeutral = false; algaeOriginDpad = null; }
            if (requests == 0) return;
            algaeAwaitNeutral = heldCount > 0 || dpadValue.sqrMagnitude >= .01f;
            // Resolve after the whole Input System update, including simultaneous keys and modal requests.
            if (ambiguous || modalRequested || inputUpdateMode != TopDown3DInputMode.Gameplay || Mode != TopDown3DInputMode.Gameplay
                || heldCount > 1 || (requests & (requests - 1)) != 0
                || Mathf.Abs(dpadValue.x) > .1f && Mathf.Abs(dpadValue.y) > .1f) return;
            AlgaeUseRequested?.Invoke(requests == 1 ? TopDown3DAlgaeUse.Drink : requests == 2 ? TopDown3DAlgaeUse.Eat : TopDown3DAlgaeUse.Treat);
        }
    }
}
