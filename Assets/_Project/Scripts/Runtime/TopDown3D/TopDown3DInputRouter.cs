using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    public sealed class TopDown3DInputRouter : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string gameplayMapName = "Gameplay";
        [SerializeField] private string moveActionName = "Move";
        [SerializeField] private string sprintActionName = "Sprint";
        [SerializeField] private string recallActionName = "RecallBigArm";
        [SerializeField, Range(0f, 0.5f)] private float moveDeadzone = 0.12f;

        private InputActionMap gameplayMap;
        private InputAction moveAction;
        private InputAction sprintAction;
        private InputAction recallAction;

        public Vector2 MoveValue { get; private set; }
        public bool SprintHeld { get; private set; }
        public string LastInputDevice { get; private set; } = "None";

        public event Action RecallRequested;

        public void Configure(InputActionAsset actions)
        {
            inputActions = actions;
        }

        private void OnEnable()
        {
            if (!TryResolveActions())
            {
                return;
            }

            moveAction.performed += HandleMove;
            moveAction.canceled += HandleMove;
            sprintAction.performed += HandleSprint;
            sprintAction.canceled += HandleSprint;
            recallAction.performed += HandleRecall;
            gameplayMap.Enable();
        }

        private void OnDisable()
        {
            if (moveAction != null)
            {
                moveAction.performed -= HandleMove;
                moveAction.canceled -= HandleMove;
            }

            if (sprintAction != null)
            {
                sprintAction.performed -= HandleSprint;
                sprintAction.canceled -= HandleSprint;
            }

            if (recallAction != null)
            {
                recallAction.performed -= HandleRecall;
            }

            gameplayMap?.Disable();
            gameplayMap = null;
            moveAction = null;
            sprintAction = null;
            recallAction = null;
            MoveValue = Vector2.zero;
            SprintHeld = false;
        }

        private bool TryResolveActions()
        {
            if (inputActions == null)
            {
                Debug.LogError($"{nameof(TopDown3DInputRouter)} requires an InputActionAsset.", this);
                return false;
            }

            gameplayMap = inputActions.FindActionMap(gameplayMapName, false);
            moveAction = gameplayMap?.FindAction(moveActionName, false);
            sprintAction = gameplayMap?.FindAction(sprintActionName, false);
            recallAction = gameplayMap?.FindAction(recallActionName, false);
            if (gameplayMap != null && moveAction != null && sprintAction != null && recallAction != null)
            {
                return true;
            }

            Debug.LogError(
                $"{nameof(TopDown3DInputRouter)} could not resolve Gameplay/Move/Sprint/RecallBigArm from the assigned input asset.",
                this);
            return false;
        }

        private void HandleMove(InputAction.CallbackContext context)
        {
            RecordDevice(context);
            var value = Vector2.ClampMagnitude(context.ReadValue<Vector2>(), 1f);
            var magnitude = value.magnitude;
            if (magnitude <= moveDeadzone)
            {
                MoveValue = Vector2.zero;
                return;
            }

            var scaledMagnitude = Mathf.InverseLerp(moveDeadzone, 1f, magnitude);
            MoveValue = value.normalized * scaledMagnitude;
        }

        private void HandleSprint(InputAction.CallbackContext context)
        {
            RecordDevice(context);
            SprintHeld = context.ReadValueAsButton();
        }

        private void HandleRecall(InputAction.CallbackContext context)
        {
            RecordDevice(context);
            if (context.phase == InputActionPhase.Performed)
            {
                RecallRequested?.Invoke();
            }
        }

        private void RecordDevice(InputAction.CallbackContext context)
        {
            if (context.control?.device != null)
            {
                LastInputDevice = context.control.device.displayName;
            }
        }
    }
}
