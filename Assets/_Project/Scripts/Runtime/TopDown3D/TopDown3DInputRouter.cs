using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BooterBigArm.TopDown3D
{
    public enum TopDown3DInputMode
    {
        Disabled,
        Gameplay,
        Inventory,
        Radial,
        SystemMenu
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
        [SerializeField] private string deployCanisterActionName = "DeployCanister";
        [SerializeField] private string pickupCanisterActionName = "PickupCanister";
        [SerializeField] private string recallActionName = "RecallBigArm";
        [SerializeField] private string toggleInventoryActionName = "ToggleInventory";
        [SerializeField] private string uiCancelActionName = "Cancel";

        private InputActionMap gameplayMap;
        private InputActionMap systemMap;
        private InputActionMap uiMap;
        private InputActionMap radialMap;
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction cameraLookAheadAction;
        private InputAction sprintAction;
        private InputAction interactAction;
        private InputAction deployCanisterAction;
        private InputAction pickupCanisterAction;
        private InputAction recallAction;
        private InputAction toggleInventoryAction;
        private InputAction uiCancelAction;
        private InputAction menuAction;
        private TopDown3DInputMode inputUpdateMode;
        private bool menuRequested, menuBackRequested, awaitMoveNeutral;
        private bool actionsBound;
        private bool awaitLookNeutral;
        private int ignoreMouseLookUntilFrame = -1;

        public Vector2 MoveValue { get; private set; }
        public Vector2 CameraLookValue { get; private set; }
        public Vector2 MouseLookDelta => Mode == TopDown3DInputMode.Gameplay && Application.isFocused
            && Time.frameCount > ignoreMouseLookUntilFrame && Mouse.current != null
            ? Mouse.current.delta.ReadValue()
            : Vector2.zero;
        public bool CameraLookAheadHeld { get; private set; }
        public bool SprintHeld { get; private set; }
        public string LastInputDevice { get; private set; } = "None";
        public InputActionAsset InputActions => inputActions;
        public TopDown3DInputMode Mode { get; private set; }
        public TopDown3DPromptDevice PromptDevice { get; private set; }

        public event Action RecallRequested;
        public event Action InteractRequested;
        public event Action DeployCanisterRequested;
        public event Action PickupCanisterRequested;
        public event Action InventoryToggleRequested;
        public event Action UiCancelRequested;
        public event Action MenuToggleRequested;
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
            ReleaseCursor();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (focused && Mode == TopDown3DInputMode.Gameplay)
            {
                CaptureCursor();
            }
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
            deployCanisterAction.performed += HandleDeployCanister;
            pickupCanisterAction.performed += HandlePickupCanister;
            recallAction.performed += HandleRecall;
            toggleInventoryAction.performed += HandleToggleInventory;
            uiCancelAction.performed += HandleUiCancel;
            if (menuAction != null) menuAction.performed += HandleMenu;
            InputSystem.onBeforeUpdate += BeforeInputUpdate;
            InputSystem.onAfterUpdate += AfterInputUpdate;
            gameplayMap.actionTriggered += HandleActionTriggered;
            systemMap.actionTriggered += HandleActionTriggered;
            uiMap.actionTriggered += HandleActionTriggered;
            if (radialMap != null) radialMap.actionTriggered += HandleActionTriggered;
            InputSystem.onDeviceChange += HandleDeviceChange;
            actionsBound = true;
            SetPromptDevice(FindAvailableGamepad() != null
                ? TopDown3DPromptDevice.Gamepad
                : TopDown3DPromptDevice.KeyboardMouse);
            SetMode(TopDown3DInputMode.Gameplay);
        }

        private void UnbindActions()
        {
            if (menuAction != null) menuAction.performed -= HandleMenu;
            InputSystem.onBeforeUpdate -= BeforeInputUpdate;
            InputSystem.onAfterUpdate -= AfterInputUpdate;
            menuRequested = menuBackRequested = false;
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

            if (deployCanisterAction != null)
                deployCanisterAction.performed -= HandleDeployCanister;
            if (pickupCanisterAction != null)
                pickupCanisterAction.performed -= HandlePickupCanister;

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
            if (radialMap != null) radialMap.actionTriggered -= HandleActionTriggered;

            SetMode(TopDown3DInputMode.Disabled);
            actionsBound = false;
            gameplayMap = null;
            systemMap = null;
            uiMap = null;
            radialMap = null;
            moveAction = null;
            lookAction = null;
            cameraLookAheadAction = null;
            sprintAction = null;
            interactAction = null;
            deployCanisterAction = null;
            pickupCanisterAction = null;
            recallAction = null;
            toggleInventoryAction = null;
            uiCancelAction = null;
            menuAction = null;
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
            radialMap = inputActions.FindActionMap("Radial", false);
            moveAction = gameplayMap?.FindAction(moveActionName, false);
            lookAction = gameplayMap?.FindAction(lookActionName, false);
            cameraLookAheadAction = gameplayMap?.FindAction(cameraLookAheadActionName, false);
            sprintAction = gameplayMap?.FindAction(sprintActionName, false);
            interactAction = gameplayMap?.FindAction(interactActionName, false);
            deployCanisterAction = gameplayMap?.FindAction(deployCanisterActionName, false);
            pickupCanisterAction = gameplayMap?.FindAction(pickupCanisterActionName, false);
            recallAction = gameplayMap?.FindAction(recallActionName, false);
            toggleInventoryAction = systemMap?.FindAction(toggleInventoryActionName, false);
            uiCancelAction = uiMap?.FindAction(uiCancelActionName, false);
            menuAction = systemMap?.FindAction("ToggleMenu", false);
            if (gameplayMap != null
                && moveAction != null
                && lookAction != null
                && cameraLookAheadAction != null
                && sprintAction != null
                && interactAction != null
                && deployCanisterAction != null
                && pickupCanisterAction != null
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
            if (awaitMoveNeutral)
            {
                if (context.ReadValue<Vector2>().sqrMagnitude < 0.0484f) awaitMoveNeutral = false;
                MoveValue = Vector2.zero;
                return;
            }
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
            // Pointer delta is read separately so its pixel units are not treated as a stick rate.
            if (!(context.control?.device is Gamepad))
            {
                return;
            }

            if (awaitLookNeutral)
            {
                if (context.ReadValue<Vector2>().sqrMagnitude < 0.0484f) awaitLookNeutral = false;
                CameraLookValue = Vector2.zero;
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

        private void HandleDeployCanister(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Performed)
                DeployCanisterRequested?.Invoke();
        }

        private void HandlePickupCanister(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Performed)
                PickupCanisterRequested?.Invoke();
        }

        private void HandleToggleInventory(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Performed && Mode != TopDown3DInputMode.Radial
                && Mode != TopDown3DInputMode.SystemMenu)
            {
                InventoryToggleRequested?.Invoke();
            }
        }

        private void HandleUiCancel(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Performed)
            {
                if (inputUpdateMode == TopDown3DInputMode.SystemMenu)
                { menuBackRequested = true; return; }
                UiCancelRequested?.Invoke();
            }
        }

        private void BeforeInputUpdate() => inputUpdateMode = Mode;

        private void HandleMenu(InputAction.CallbackContext context)
        {
            if (Application.isPlaying && !Application.isFocused) return;
            if (inputUpdateMode == TopDown3DInputMode.Disabled) return;
            var escape = context.control.device is Keyboard;
            // Escape already belongs to inventory/radial Back in these captured contexts.
            if (escape && (inputUpdateMode == TopDown3DInputMode.Inventory
                || inputUpdateMode == TopDown3DInputMode.Radial)) return;
            if (escape && inputUpdateMode == TopDown3DInputMode.SystemMenu) menuBackRequested = true;
            else menuRequested = true;
        }

        private void AfterInputUpdate()
        {
            var toggle = menuRequested; var back = menuBackRequested;
            menuRequested = menuBackRequested = false;
            if (toggle) MenuToggleRequested?.Invoke();
            else if (back) UiCancelRequested?.Invoke();
        }

        public void EnterSystemMenuMode() => SetMode(TopDown3DInputMode.SystemMenu);

        public void EnterGameplayMode()
        {
            SetMode(TopDown3DInputMode.Gameplay);
        }

        public void EnterInventoryMode()
        {
            SetMode(TopDown3DInputMode.Inventory);
        }

        public void EnterRadialMode() => SetMode(TopDown3DInputMode.Radial);

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

            // System remains enabled across modal transitions so a held opener is not canceled.
            var previousMode = Mode;
            Mode = mode;
            if (mode == TopDown3DInputMode.Gameplay && previousMode != TopDown3DInputMode.Gameplay)
            {
                ignoreMouseLookUntilFrame = Time.frameCount + 1;
                awaitMoveNeutral = previousMode == TopDown3DInputMode.SystemMenu
                    && Gamepad.current != null && Gamepad.current.leftStick.ReadValue().sqrMagnitude > 0.0484f;
                awaitLookNeutral = (previousMode == TopDown3DInputMode.Radial || previousMode == TopDown3DInputMode.SystemMenu)
                    && Gamepad.current != null && Gamepad.current.rightStick.ReadValue().sqrMagnitude > 0.0484f;
            }
            gameplayMap?.Disable();
            uiMap?.Disable();
            radialMap?.Disable();
            if (mode == TopDown3DInputMode.Disabled) systemMap?.Disable();
            else systemMap?.Enable();
            ClearGameplayIntent();
            switch (mode)
            {
                case TopDown3DInputMode.Gameplay:
                    gameplayMap?.Enable();
                    systemMap?.Enable();
                    break;
                case TopDown3DInputMode.Inventory:
                case TopDown3DInputMode.SystemMenu:
                    uiMap?.Enable();
                    systemMap?.Enable();
                    break;
                case TopDown3DInputMode.Radial:
                    radialMap?.Enable();
                    break;
            }

            Mode = mode;
            if (mode == TopDown3DInputMode.Gameplay)
            {
                CaptureCursor();
            }
            else
            {
                ReleaseCursor();
            }
            ModeChanged?.Invoke(mode);
            if (mode == TopDown3DInputMode.Disabled)
            {
                // UI teardown can restore module references, which re-enable individual actions.
                // Disabled remains the final input authority after those listeners finish.
                gameplayMap?.Disable(); uiMap?.Disable(); radialMap?.Disable(); systemMap?.Disable();
            }
        }

        private static void CaptureCursor()
        {
            if (Application.isPlaying && Application.isFocused && Mouse.current != null)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private static void ReleaseCursor()
        {
            if (Application.isPlaying)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
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
            // Enabling a pointer action samples its cached position; that is not a device switch.
            if (context.action.actionMap == radialMap && context.action.name == "Point"
                && context.control?.device is Mouse mouse && mouse.delta.ReadValue().sqrMagnitude < 0.1f) return;
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
