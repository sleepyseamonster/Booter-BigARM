using UnityEngine;
using UnityEngine.InputSystem;

namespace BooterBigArm.Runtime
{
    [DisallowMultipleComponent]
    public sealed class PrototypeBigArmCommandAdapter : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string actionMapName = "Gameplay";
        [SerializeField] private string recallActionName = "RecallBigArm";
        [SerializeField] private string scoutActionName = "ScoutBigArm";
        [SerializeField] private string dangerPingActionName = "PingBigArmDanger";
        [SerializeField] private PrototypeBigArmAiController bigArmController;
        [SerializeField] private PlayerMotor2D playerMotor;
        [SerializeField] private PrototypeBigArmThreatSignal threatSignal;
        [SerializeField, Min(0.5f)] private float dangerPingDuration = 5f;

        private InputActionMap actionMap;
        private InputAction recallAction;
        private InputAction scoutAction;
        private InputAction dangerPingAction;
        private float dangerPingTimer;
        private bool dangerPingActive;

        public void Configure(
            InputActionAsset actions,
            PrototypeBigArmAiController controller,
            PlayerMotor2D player,
            PrototypeBigArmThreatSignal signal)
        {
            inputActions = actions;
            bigArmController = controller;
            playerMotor = player;
            threatSignal = signal;
        }

        private void OnEnable()
        {
            if (inputActions == null)
            {
                Debug.LogWarning($"{nameof(PrototypeBigArmCommandAdapter)} on {name} has no input actions asset assigned yet.", this);
                return;
            }

            actionMap = inputActions.FindActionMap(actionMapName, false);
            if (actionMap == null)
            {
                Debug.LogWarning(
                    $"{nameof(PrototypeBigArmCommandAdapter)} on {name} could not find action map '{actionMapName}'.",
                    this);
                return;
            }

            recallAction = actionMap.FindAction(recallActionName, false);
            scoutAction = actionMap.FindAction(scoutActionName, false);
            dangerPingAction = actionMap.FindAction(dangerPingActionName, false);

            if (recallAction == null || scoutAction == null || dangerPingAction == null)
            {
                Debug.LogWarning(
                    $"{nameof(PrototypeBigArmCommandAdapter)} on {name} is missing one or more BigARM command actions.",
                    this);
                return;
            }

            recallAction.performed += HandleRecall;
            scoutAction.performed += HandleScout;
            dangerPingAction.performed += HandleDangerPing;
            actionMap.Enable();
        }

        private void OnDisable()
        {
            if (recallAction != null)
            {
                recallAction.performed -= HandleRecall;
            }

            if (scoutAction != null)
            {
                scoutAction.performed -= HandleScout;
            }

            if (dangerPingAction != null)
            {
                dangerPingAction.performed -= HandleDangerPing;
            }

            if (actionMap != null)
            {
                actionMap.Disable();
            }
        }

        private void Update()
        {
            if (!dangerPingActive || threatSignal == null)
            {
                return;
            }

            dangerPingTimer -= Time.deltaTime;
            if (dangerPingTimer > 0f)
            {
                return;
            }

            dangerPingActive = false;
            threatSignal.gameObject.SetActive(false);
        }

        private void HandleRecall(InputAction.CallbackContext context)
        {
            if (context.phase != InputActionPhase.Performed || bigArmController == null)
            {
                return;
            }

            bigArmController.RequestRecall();
        }

        private void HandleScout(InputAction.CallbackContext context)
        {
            if (context.phase != InputActionPhase.Performed || bigArmController == null)
            {
                return;
            }

            bigArmController.RequestScout();
        }

        private void HandleDangerPing(InputAction.CallbackContext context)
        {
            if (context.phase != InputActionPhase.Performed || threatSignal == null)
            {
                return;
            }

            var anchor = playerMotor != null ? playerMotor.transform.position : transform.position;
            var forward = playerMotor != null && playerMotor.Velocity.sqrMagnitude > 0.01f
                ? (Vector3)playerMotor.Velocity.normalized
                : Vector3.right;
            threatSignal.transform.position = anchor + forward * 8f;
            threatSignal.gameObject.SetActive(true);
            dangerPingActive = true;
            dangerPingTimer = dangerPingDuration;
        }
    }
}
