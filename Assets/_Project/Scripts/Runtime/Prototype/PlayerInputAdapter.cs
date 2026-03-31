using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BooterBigArm.Runtime
{
    [DisallowMultipleComponent]
    public sealed class PlayerInputAdapter : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string actionMapName = "Gameplay";
        [SerializeField] private string moveActionName = "Move";
        [SerializeField] private string lookActionName = "Look";
        [SerializeField] private string sprintActionName = "Sprint";
        [SerializeField] private string interactActionName = "Interact";

        private InputActionMap actionMap;
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction sprintAction;
        private InputAction interactAction;

        public Vector2 MoveValue { get; private set; }
        public Vector2 LookValue { get; private set; }
        public bool SprintHeld { get; private set; }
        public event Action InteractPressed;

        public void SetInputActions(InputActionAsset actions)
        {
            inputActions = actions;
        }

        private void OnEnable()
        {
            if (inputActions == null)
            {
                Debug.LogWarning($"{nameof(PlayerInputAdapter)} on {name} has no input actions asset assigned yet.", this);
                return;
            }

            actionMap = inputActions.FindActionMap(actionMapName, false);
            if (actionMap == null)
            {
                Debug.LogWarning(
                    $"{nameof(PlayerInputAdapter)} on {name} could not find action map '{actionMapName}'.",
                    this);
                return;
            }

            moveAction = actionMap.FindAction(moveActionName, false);
            if (moveAction == null)
            {
                Debug.LogWarning(
                    $"{nameof(PlayerInputAdapter)} on {name} could not find action '{moveActionName}'.",
                    this);
                return;
            }

            lookAction = actionMap.FindAction(lookActionName, false);
            if (lookAction == null)
            {
                Debug.LogWarning(
                    $"{nameof(PlayerInputAdapter)} on {name} could not find action '{lookActionName}'.",
                    this);
                return;
            }

            sprintAction = actionMap.FindAction(sprintActionName, false);
            if (sprintAction == null)
            {
                Debug.LogWarning(
                    $"{nameof(PlayerInputAdapter)} on {name} could not find action '{sprintActionName}'.",
                    this);
                return;
            }

            interactAction = actionMap.FindAction(interactActionName, false);
            if (interactAction == null)
            {
                Debug.LogWarning(
                    $"{nameof(PlayerInputAdapter)} on {name} could not find action '{interactActionName}'.",
                    this);
                return;
            }

            moveAction.performed += HandleMove;
            moveAction.canceled += HandleMove;
            lookAction.performed += HandleLook;
            lookAction.canceled += HandleLook;
            sprintAction.performed += HandleSprint;
            sprintAction.canceled += HandleSprint;
            interactAction.performed += HandleInteract;
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

            if (sprintAction != null)
            {
                sprintAction.performed -= HandleSprint;
                sprintAction.canceled -= HandleSprint;
            }

            if (interactAction != null)
            {
                interactAction.performed -= HandleInteract;
            }

            if (actionMap != null)
            {
                actionMap.Disable();
            }

            MoveValue = Vector2.zero;
            LookValue = Vector2.zero;
            SprintHeld = false;
        }

        private void HandleMove(InputAction.CallbackContext context)
        {
            MoveValue = Vector2.ClampMagnitude(context.ReadValue<Vector2>(), 1f);
        }

        private void HandleLook(InputAction.CallbackContext context)
        {
            LookValue = Vector2.ClampMagnitude(context.ReadValue<Vector2>(), 1f);
        }

        private void HandleSprint(InputAction.CallbackContext context)
        {
            SprintHeld = context.ReadValueAsButton();
        }

        private void HandleInteract(InputAction.CallbackContext context)
        {
            if (context.phase != InputActionPhase.Performed)
            {
                return;
            }

            InteractPressed?.Invoke();
        }
    }
}
