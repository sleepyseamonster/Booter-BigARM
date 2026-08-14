using UnityEngine;
using UnityEngine.InputSystem;

namespace BooterBigArm.Runtime
{
    [DisallowMultipleComponent]
    public sealed class PrototypeSystemInputAdapter : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string actionMapName = "System";
        [SerializeField] private string saveActionName = "Save";
        [SerializeField] private string loadActionName = "Load";
        [SerializeField] private string rebuildActionName = "Rebuild";
        [SerializeField] private string nextSeedActionName = "NextSeed";
        [SerializeField] private PrototypeSaveLoadController saveLoadController;

        private InputActionMap actionMap;
        private InputAction saveAction;
        private InputAction loadAction;
        private InputAction rebuildAction;
        private InputAction nextSeedAction;

        public void Configure(InputActionAsset actions, PrototypeSaveLoadController controller)
        {
            inputActions = actions;
            saveLoadController = controller;
        }

        private void OnEnable()
        {
            if (inputActions == null)
            {
                Debug.LogWarning($"{nameof(PrototypeSystemInputAdapter)} on {name} has no input actions asset assigned yet.", this);
                return;
            }

            actionMap = inputActions.FindActionMap(actionMapName, false);
            if (actionMap == null)
            {
                Debug.LogWarning(
                    $"{nameof(PrototypeSystemInputAdapter)} on {name} could not find action map '{actionMapName}'.",
                    this);
                return;
            }

            saveAction = actionMap.FindAction(saveActionName, false);
            loadAction = actionMap.FindAction(loadActionName, false);
            rebuildAction = actionMap.FindAction(rebuildActionName, false);
            nextSeedAction = actionMap.FindAction(nextSeedActionName, false);

            if (saveAction == null || loadAction == null || rebuildAction == null || nextSeedAction == null)
            {
                Debug.LogWarning(
                    $"{nameof(PrototypeSystemInputAdapter)} on {name} is missing one or more system actions.",
                    this);
                return;
            }

            saveAction.performed += HandleSave;
            loadAction.performed += HandleLoad;
            rebuildAction.performed += HandleRebuild;
            nextSeedAction.performed += HandleNextSeed;
            actionMap.Enable();
        }

        private void OnDisable()
        {
            if (saveAction != null)
            {
                saveAction.performed -= HandleSave;
            }

            if (loadAction != null)
            {
                loadAction.performed -= HandleLoad;
            }

            if (rebuildAction != null)
            {
                rebuildAction.performed -= HandleRebuild;
            }

            if (nextSeedAction != null)
            {
                nextSeedAction.performed -= HandleNextSeed;
            }

            if (actionMap != null)
            {
                actionMap.Disable();
            }
        }

        private void HandleSave(InputAction.CallbackContext context)
        {
            if (context.phase != InputActionPhase.Performed || saveLoadController == null)
            {
                return;
            }

            saveLoadController.SaveCurrentState();
        }

        private void HandleLoad(InputAction.CallbackContext context)
        {
            if (context.phase != InputActionPhase.Performed || saveLoadController == null)
            {
                return;
            }

            saveLoadController.LoadLatestState();
        }

        private void HandleRebuild(InputAction.CallbackContext context)
        {
            if (context.phase != InputActionPhase.Performed || saveLoadController == null)
            {
                return;
            }

            saveLoadController.RebuildCurrentWorld();
        }

        private void HandleNextSeed(InputAction.CallbackContext context)
        {
            if (context.phase != InputActionPhase.Performed || saveLoadController == null)
            {
                return;
            }

            saveLoadController.AdvanceWorldSeed();
        }
    }
}
