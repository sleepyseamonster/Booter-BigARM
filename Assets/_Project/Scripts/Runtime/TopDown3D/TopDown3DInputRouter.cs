using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BooterBigArm.TopDown3D
{
    public enum TopDown3DInputMode
    {
        Disabled,
        Gameplay,
        Inventory
    }

    public enum TopDown3DPromptDevice
    {
        KeyboardMouse,
        Gamepad
    }

    [DisallowMultipleComponent]
    public sealed class TopDown3DInputRouter : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string gameplayMapName = "Gameplay";
        [SerializeField] private string systemMapName = "System";
        [SerializeField] private string uiMapName = "UI";
        [SerializeField] private string moveActionName = "Move";
        [SerializeField] private string lookActionName = "Look";
        [SerializeField] private string cameraLookAheadActionName = "CameraLookAhead";
        [SerializeField] private string sprintActionName = "Sprint";
        [SerializeField] private string interactActionName = "Interact";
        [SerializeField] private string recallActionName = "RecallBigArm";
        [SerializeField] private string toggleInventoryActionName = "ToggleInventory";
        [SerializeField] private string uiCancelActionName = "Cancel";

        private InputActionMap gameplayMap;
        private InputActionMap systemMap;
        private InputActionMap uiMap;
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction cameraLookAheadAction;
        private InputAction sprintAction;
        private InputAction interactAction;
        private InputAction recallAction;
        private InputAction toggleInventoryAction;
        private InputAction uiCancelAction;
        private bool actionsBound;

        public Vector2 MoveValue { get; private set; }
        public Vector2 CameraLookValue { get; private set; }
        public bool CameraLookAheadHeld { get; private set; }
        public bool SprintHeld { get; private set; }
        public string LastInputDevice { get; private set; } = "None";
        public InputActionAsset InputActions => inputActions;
        public TopDown3DInputMode Mode { get; private set; }
        public TopDown3DPromptDevice PromptDevice { get; private set; }

        public event Action RecallRequested;
        public event Action InteractRequested;
        public event Action InventoryToggleRequested;
        public event Action UiCancelRequested;
        public event Action<TopDown3DInputMode> ModeChanged;
        public event Action<TopDown3DPromptDevice> PromptDeviceChanged;

        public void Configure(InputActionAsset actions)
        {
            UnbindActions();
            inputActions = actions;
            if (isActiveAndEnabled)
            {
                BindActions();
            }
        }

        public bool Initialize()
        {
            BindActions();
            return actionsBound;
        }

        private void OnEnable()
        {
            BindActions();
        }

        private void OnDisable()
        {
            UnbindActions();
        }

        private void BindActions()
        {
            if (actionsBound || !TryResolveActions())
            {
                return;
            }

            moveAction.performed += HandleMove;
            moveAction.canceled += HandleMove;
            lookAction.performed += HandleLook;
            lookAction.canceled += HandleLook;
            cameraLookAheadAction.performed += HandleCameraLookAhead;
            cameraLookAheadAction.canceled += HandleCameraLookAhead;
            sprintAction.performed += HandleSprint;
            sprintAction.canceled += HandleSprint;
            interactAction.performed += HandleInteract;
            recallAction.performed += HandleRecall;
            toggleInventoryAction.performed += HandleToggleInventory;
            uiCancelAction.performed += HandleUiCancel;
            gameplayMap.actionTriggered += HandleActionTriggered;
            systemMap.actionTriggered += HandleActionTriggered;
            uiMap.actionTriggered += HandleActionTriggered;
            InputSystem.onDeviceChange += HandleDeviceChange;
            actionsBound = true;
            SetPromptDevice(FindAvailableGamepad() != null
                ? TopDown3DPromptDevice.Gamepad
                : TopDown3DPromptDevice.KeyboardMouse);
            SetMode(TopDown3DInputMode.Gameplay);
        }

        private void UnbindActions()
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

            if (lookAction != null)
            {
                lookAction.performed -= HandleLook;
                lookAction.canceled -= HandleLook;
            }

            if (cameraLookAheadAction != null)
            {
                cameraLookAheadAction.performed -= HandleCameraLookAhead;
                cameraLookAheadAction.canceled -= HandleCameraLookAhead;
            }

            if (recallAction != null)
            {
                recallAction.performed -= HandleRecall;
            }

            if (interactAction != null)
            {
                interactAction.performed -= HandleInteract;
            }

            if (toggleInventoryAction != null)
            {
                toggleInventoryAction.performed -= HandleToggleInventory;
            }

            if (uiCancelAction != null)
            {
                uiCancelAction.performed -= HandleUiCancel;
            }

            if (gameplayMap != null)
            {
                gameplayMap.actionTriggered -= HandleActionTriggered;
            }

            if (systemMap != null)
            {
                systemMap.actionTriggered -= HandleActionTriggered;
            }

            if (uiMap != null)
            {
                uiMap.actionTriggered -= HandleActionTriggered;
            }

            InputSystem.onDeviceChange -= HandleDeviceChange;

            SetMode(TopDown3DInputMode.Disabled);
            actionsBound = false;
            gameplayMap = null;
            systemMap = null;
            uiMap = null;
            moveAction = null;
            lookAction = null;
            cameraLookAheadAction = null;
            sprintAction = null;
            interactAction = null;
            recallAction = null;
            toggleInventoryAction = null;
            uiCancelAction = null;
            MoveValue = Vector2.zero;
            CameraLookValue = Vector2.zero;
            CameraLookAheadHeld = false;
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
            systemMap = inputActions.FindActionMap(systemMapName, false);
            uiMap = inputActions.FindActionMap(uiMapName, false);
            moveAction = gameplayMap?.FindAction(moveActionName, false);
            lookAction = gameplayMap?.FindAction(lookActionName, false);
            cameraLookAheadAction = gameplayMap?.FindAction(cameraLookAheadActionName, false);
            sprintAction = gameplayMap?.FindAction(sprintActionName, false);
            interactAction = gameplayMap?.FindAction(interactActionName, false);
            recallAction = gameplayMap?.FindAction(recallActionName, false);
            toggleInventoryAction = systemMap?.FindAction(toggleInventoryActionName, false);
            uiCancelAction = uiMap?.FindAction(uiCancelActionName, false);
            if (gameplayMap != null
                && moveAction != null
                && lookAction != null
                && cameraLookAheadAction != null
                && sprintAction != null
                && interactAction != null
                && recallAction != null
                && systemMap != null
                && uiMap != null
                && toggleInventoryAction != null
                && uiCancelAction != null)
            {
                return true;
            }

            Debug.LogError(
                $"{nameof(TopDown3DInputRouter)} could not resolve the required Gameplay, System, and UI actions from the assigned input asset.",
                this);
            return false;
        }

        private void HandleMove(InputAction.CallbackContext context)
        {
            MoveValue = TopDown3DInputMath.ClampMove(context.ReadValue<Vector2>());
        }

        private void HandleSprint(InputAction.CallbackContext context)
        {
            SprintHeld = context.ReadValueAsButton();
        }

        private void HandleCameraLookAhead(InputAction.CallbackContext context)
        {
            CameraLookAheadHeld = context.ReadValueAsButton();
        }

        private void HandleLook(InputAction.CallbackContext context)
        {
            // The shared Look action also carries pointer delta. This foundation pass intentionally
            // consumes only the gamepad stick so pixel delta and normalized stick rate are never mixed.
            if (!(context.control?.device is Gamepad))
            {
                return;
            }

            CameraLookValue = context.canceled
                ? Vector2.zero
                : Vector2.ClampMagnitude(context.ReadValue<Vector2>(), 1f);
        }

        private void HandleRecall(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Performed)
            {
                RecallRequested?.Invoke();
            }
        }

        private void HandleInteract(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Performed)
            {
                InteractRequested?.Invoke();
            }
        }

        private void HandleToggleInventory(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Performed)
            {
                InventoryToggleRequested?.Invoke();
            }
        }

        private void HandleUiCancel(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Performed)
            {
                UiCancelRequested?.Invoke();
            }
        }

        public void EnterGameplayMode()
        {
            SetMode(TopDown3DInputMode.Gameplay);
        }

        public void EnterInventoryMode()
        {
            SetMode(TopDown3DInputMode.Inventory);
        }

        public string GetBindingDisplayName(string actionPath, string fallback)
        {
            var action = inputActions?.FindAction(actionPath, false);
            var group = PromptDevice == TopDown3DPromptDevice.Gamepad
                ? "Gamepad"
                : "Keyboard&Mouse";
            var device = PromptDevice == TopDown3DPromptDevice.Gamepad
                ? (InputDevice)FindAvailableGamepad()
                : Keyboard.current;
            return TopDown3DInputPromptUtility.ResolveBindingDisplayName(
                action,
                group,
                device,
                fallback);
        }

        private void SetMode(TopDown3DInputMode mode)
        {
            if (mode == Mode)
            {
                return;
            }

            gameplayMap?.Disable();
            uiMap?.Disable();
            systemMap?.Disable();
            ClearGameplayIntent();
            switch (mode)
            {
                case TopDown3DInputMode.Gameplay:
                    gameplayMap?.Enable();
                    systemMap?.Enable();
                    break;
                case TopDown3DInputMode.Inventory:
                    uiMap?.Enable();
                    systemMap?.Enable();
                    break;
            }

            Mode = mode;
            ModeChanged?.Invoke(mode);
        }

        private void ClearGameplayIntent()
        {
            MoveValue = Vector2.zero;
            CameraLookValue = Vector2.zero;
            CameraLookAheadHeld = false;
            SprintHeld = false;
        }

        private void HandleActionTriggered(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Started
                || context.phase == InputActionPhase.Performed)
            {
                RecordDevice(context.control?.device);
            }
        }

        private void RecordDevice(InputDevice device)
        {
            if (device == null)
            {
                return;
            }

            LastInputDevice = device.displayName;
            if (device is Gamepad)
            {
                SetPromptDevice(TopDown3DPromptDevice.Gamepad);
            }
            else if (device is Keyboard || device is Mouse)
            {
                SetPromptDevice(TopDown3DPromptDevice.KeyboardMouse);
            }
        }

        private void HandleDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (!(device is Gamepad))
            {
                return;
            }

            switch (change)
            {
                case InputDeviceChange.Added:
                case InputDeviceChange.Reconnected:
                case InputDeviceChange.Enabled:
                    SetPromptDevice(TopDown3DPromptDevice.Gamepad);
                    break;
                case InputDeviceChange.Removed:
                case InputDeviceChange.Disconnected:
                case InputDeviceChange.Disabled:
                    if (FindAvailableGamepad(device) == null)
                    {
                        SetPromptDevice(TopDown3DPromptDevice.KeyboardMouse);
                    }
                    break;
            }
        }

        private void SetPromptDevice(TopDown3DPromptDevice device)
        {
            if (PromptDevice == device)
            {
                return;
            }

            PromptDevice = device;
            PromptDeviceChanged?.Invoke(device);
        }

        private static Gamepad FindAvailableGamepad(InputDevice excludedDevice = null)
        {
            var current = Gamepad.current;
            if (current != null
                && current != excludedDevice
                && current.added
                && current.enabled)
            {
                return current;
            }

            for (var i = 0; i < Gamepad.all.Count; i++)
            {
                var gamepad = Gamepad.all[i];
                if (gamepad != excludedDevice && gamepad.added && gamepad.enabled)
                {
                    return gamepad;
                }
            }

            return null;
        }
    }

    public static class TopDown3DInputMath
    {
        public static Vector2 ClampMove(Vector2 value)
        {
            return Vector2.ClampMagnitude(value, 1f);
        }
    }

    internal static class TopDown3DInputPromptUtility
    {
        private const InputBinding.DisplayStringOptions DisplayOptions =
            InputBinding.DisplayStringOptions.DontIncludeInteractions
            | InputBinding.DisplayStringOptions.DontUseShortDisplayNames;

        public static string ResolveBindingDisplayName(
            InputAction action,
            string bindingGroup,
            InputDevice device,
            string fallback)
        {
            if (action == null || string.IsNullOrWhiteSpace(bindingGroup))
            {
                return fallback ?? string.Empty;
            }

            var bindingMask = InputBinding.MaskByGroup(bindingGroup);
            if (device != null)
            {
                for (var i = 0; i < action.controls.Count; i++)
                {
                    var control = action.controls[i];
                    if (control.device != device)
                    {
                        continue;
                    }

                    var bindingIndex = action.GetBindingIndexForControl(control);
                    if (bindingIndex >= 0 && bindingMask.Matches(action.bindings[bindingIndex]))
                    {
                        var displayName = !string.IsNullOrWhiteSpace(control.shortDisplayName)
                            ? control.shortDisplayName
                            : control.displayName;
                        if (!string.IsNullOrWhiteSpace(displayName))
                        {
                            return displayName;
                        }
                    }
                }
            }

            var resolved = action.GetBindingDisplayString(
                DisplayOptions,
                bindingGroup);
            return string.IsNullOrWhiteSpace(resolved)
                ? fallback ?? string.Empty
                : resolved;
        }
    }
}
