using UnityEngine;
using UnityEngine.InputSystem;

namespace BooterBigArm.Runtime
{
    [DisallowMultipleComponent]
    public sealed class PlayerInputAdapter : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string actionMapName = "Player";
        [SerializeField] private string moveActionName = "Move";
        [SerializeField] private string lookActionName = "Look";

        private InputActionMap actionMap;
        private InputAction moveAction;
        private InputAction lookAction;

        public Vector2 MoveValue { get; private set; }
        public Vector2 LookValue { get; private set; }

        public void SetInputActions(InputActionAsset actions)
        {
            inputActions = actions;
        }

        private void OnEnable()
        {
            if (inputActions == null)
            {
                Debug.LogError($"{nameof(PlayerInputAdapter)} on {name} has no input actions asset assigned.", this);
                enabled = false;
                return;
            }

            actionMap = inputActions.FindActionMap(actionMapName, false);
            if (actionMap == null)
            {
                Debug.LogError(
                    $"{nameof(PlayerInputAdapter)} on {name} could not find action map '{actionMapName}'.",
                    this);
                enabled = false;
                return;
            }

            moveAction = actionMap.FindAction(moveActionName, false);
            if (moveAction == null)
            {
                Debug.LogError(
                    $"{nameof(PlayerInputAdapter)} on {name} could not find action '{moveActionName}'.",
                    this);
                enabled = false;
                return;
            }

            lookAction = actionMap.FindAction(lookActionName, false);
            if (lookAction == null)
            {
                Debug.LogError(
                    $"{nameof(PlayerInputAdapter)} on {name} could not find action '{lookActionName}'.",
                    this);
                enabled = false;
                return;
            }

            moveAction.performed += HandleMove;
            moveAction.canceled += HandleMove;
            lookAction.performed += HandleLook;
            lookAction.canceled += HandleLook;
            actionMap.Enable();
        }

        private void OnDisable()
        {
            if (moveAction != null)
            {
                moveAction.performed -= HandleMove;
                moveAction.canceled -= HandleMove;
            }

            if (lookAction != null)
            {
                lookAction.performed -= HandleLook;
                lookAction.canceled -= HandleLook;
            }

            if (actionMap != null)
            {
                actionMap.Disable();
            }

            MoveValue = Vector2.zero;
            LookValue = Vector2.zero;
        }

        private void HandleMove(InputAction.CallbackContext context)
        {
            MoveValue = Vector2.ClampMagnitude(context.ReadValue<Vector2>(), 1f);
        }

        private void HandleLook(InputAction.CallbackContext context)
        {
            LookValue = Vector2.ClampMagnitude(context.ReadValue<Vector2>(), 1f);
        }
    }
}
