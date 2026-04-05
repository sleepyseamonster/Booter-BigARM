using UnityEngine;
using UnityEngine.InputSystem;

namespace BooterBigArm.Runtime
{
    [DisallowMultipleComponent]
    public sealed class PrototypeInventoryUiController : MonoBehaviour
    {
        private enum ActivePanel
        {
            None,
            Backpack,
            BigArm
        }

        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string actionMapName = "Gameplay";
        [SerializeField] private string inventoryActionName = "OpenInventory";
        [SerializeField, Min(0.1f)] private float holdThresholdSeconds = 0.35f;
        [SerializeField] private PrototypeInventoryHud backpackHud;
        [SerializeField] private PrototypeInventoryHud bigArmHud;

        private InputActionMap actionMap;
        private InputAction inventoryAction;
        private bool enabledActionMapForSelf;
        private bool wasPressed;
        private float pressedSeconds;
        private bool holdTriggered;
        private ActivePanel activePanel = ActivePanel.None;

        public void Configure(
            InputActionAsset actions,
            PrototypeInventoryHud backpack,
            PrototypeInventoryHud bigArm,
            float holdThreshold)
        {
            inputActions = actions;
            backpackHud = backpack;
            bigArmHud = bigArm;
            holdThresholdSeconds = Mathf.Max(0.1f, holdThreshold);
        }

        private void OnEnable()
        {
            if (inputActions == null)
            {
                Debug.LogWarning($"{nameof(PrototypeInventoryUiController)} on {name} has no input actions asset assigned yet.", this);
                return;
            }

            actionMap = inputActions.FindActionMap(actionMapName, false);
            if (actionMap == null)
            {
                Debug.LogWarning(
                    $"{nameof(PrototypeInventoryUiController)} on {name} could not find action map '{actionMapName}'.",
                    this);
                return;
            }

            inventoryAction = actionMap.FindAction(inventoryActionName, false);
            if (inventoryAction == null)
            {
                Debug.LogWarning(
                    $"{nameof(PrototypeInventoryUiController)} on {name} could not find action '{inventoryActionName}'.",
                    this);
                return;
            }

            enabledActionMapForSelf = !actionMap.enabled;
            if (enabledActionMapForSelf)
            {
                actionMap.Enable();
            }

            SetVisiblePanel(ActivePanel.None);
        }

        private void OnDisable()
        {
            if (enabledActionMapForSelf && actionMap != null)
            {
                actionMap.Disable();
            }

            wasPressed = false;
            pressedSeconds = 0f;
            holdTriggered = false;
            enabledActionMapForSelf = false;
            SetVisiblePanel(ActivePanel.None);
        }

        private void Update()
        {
            if (inventoryAction == null)
            {
                return;
            }

            var pressed = inventoryAction.IsPressed();
            if (pressed && !wasPressed)
            {
                pressedSeconds = 0f;
                holdTriggered = false;
            }

            if (pressed)
            {
                pressedSeconds += Time.unscaledDeltaTime;
                if (!holdTriggered && pressedSeconds >= holdThresholdSeconds)
                {
                    SetVisiblePanel(ActivePanel.BigArm);
                    holdTriggered = true;
                }
            }

            if (!pressed && wasPressed && !holdTriggered)
            {
                SetVisiblePanel(ActivePanel.Backpack);
            }

            wasPressed = pressed;
        }

        private void SetVisiblePanel(ActivePanel panel)
        {
            activePanel = panel;

            if (backpackHud != null)
            {
                backpackHud.SetVisible(panel == ActivePanel.Backpack);
            }

            if (bigArmHud != null)
            {
                bigArmHud.SetVisible(panel == ActivePanel.BigArm);
            }
        }
    }
}
