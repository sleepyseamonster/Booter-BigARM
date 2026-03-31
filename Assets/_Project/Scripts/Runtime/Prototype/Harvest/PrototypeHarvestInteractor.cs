using UnityEngine;
using UnityEngine.InputSystem;

namespace BooterBigArm.Runtime
{
    [DisallowMultipleComponent]
    public sealed class PrototypeHarvestInteractor : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string actionMapName = "Gameplay";
        [SerializeField] private string interactActionName = "Interact";
        [SerializeField, Min(0.25f)] private float interactRadius = 1.2f;
        [SerializeField] private LayerMask nodeMask = ~0;

        private readonly Collider2D[] overlapBuffer = new Collider2D[16];
        private InputActionMap actionMap;
        private InputAction interactAction;
        private bool enabledActionMapForSelf;

        private IPrototypeItemReceiver itemReceiver;
        private IPrototypeToolSource toolSource;

        private PrototypeHarvestNode currentTarget;
        private float heldSeconds;
        private bool waitingForRelease;

        public PrototypeHarvestNode CurrentTarget => currentTarget;
        public float CurrentProgress01 => currentTarget == null ? 0f : Mathf.Clamp01(heldSeconds / currentTarget.HarvestSeconds);
        public string LastMessage { get; private set; } = string.Empty;

        public void Configure(InputActionAsset actions)
        {
            inputActions = actions;
        }

        private void Awake()
        {
            ResolveInterfaces();
        }

        private void OnEnable()
        {
            if (inputActions == null)
            {
                Debug.LogWarning($"{nameof(PrototypeHarvestInteractor)} on {name} has no input actions asset assigned yet.", this);
                return;
            }

            actionMap = inputActions.FindActionMap(actionMapName, false);
            if (actionMap == null)
            {
                Debug.LogWarning($"{nameof(PrototypeHarvestInteractor)} on {name} could not find action map '{actionMapName}'.", this);
                return;
            }

            interactAction = actionMap.FindAction(interactActionName, false);
            if (interactAction == null)
            {
                Debug.LogWarning($"{nameof(PrototypeHarvestInteractor)} on {name} could not find action '{interactActionName}'.", this);
                return;
            }

            enabledActionMapForSelf = !actionMap.enabled;
            if (enabledActionMapForSelf)
            {
                actionMap.Enable();
            }
        }

        private void OnDisable()
        {
            if (enabledActionMapForSelf && actionMap != null)
            {
                actionMap.Disable();
            }

            currentTarget = null;
            heldSeconds = 0f;
            waitingForRelease = false;
            enabledActionMapForSelf = false;
        }

        private void Update()
        {
            AcquireTarget();

            var pressed = interactAction != null && interactAction.IsPressed();
            if (!pressed)
            {
                heldSeconds = 0f;
                waitingForRelease = false;
                return;
            }

            if (waitingForRelease)
            {
                return;
            }

            if (currentTarget == null)
            {
                return;
            }

            if (currentTarget.IsDepleted)
            {
                LastMessage = $"{currentTarget.DisplayName} is depleted.";
                heldSeconds = 0f;
                waitingForRelease = !currentTarget.RepeatWhileHeld;
                return;
            }

            var equippedTool = toolSource != null ? toolSource.EquippedToolId : string.Empty;
            if (!currentTarget.CanHarvest(equippedTool))
            {
                LastMessage = string.IsNullOrWhiteSpace(currentTarget.RequiredToolId)
                    ? "Cannot harvest."
                    : $"Requires tool: {currentTarget.RequiredToolId}";
                heldSeconds = 0f;
                return;
            }

            heldSeconds += Time.deltaTime;
            if (heldSeconds < currentTarget.HarvestSeconds)
            {
                return;
            }

            if (itemReceiver == null)
            {
                LastMessage = "No inventory receiver.";
                heldSeconds = 0f;
                waitingForRelease = !currentTarget.RepeatWhileHeld;
                return;
            }

            if (!currentTarget.TryHarvest(itemReceiver, equippedTool))
            {
                LastMessage = "Inventory full.";
                heldSeconds = 0f;
                waitingForRelease = !currentTarget.RepeatWhileHeld;
                return;
            }

            LastMessage = $"Harvested {currentTarget.DisplayName}.";
            heldSeconds = 0f;
            waitingForRelease = !currentTarget.RepeatWhileHeld;
        }

        private void AcquireTarget()
        {
            var count = Physics2D.OverlapCircleNonAlloc(transform.position, interactRadius, overlapBuffer, nodeMask);
            PrototypeHarvestNode best = null;
            var bestDistSq = float.PositiveInfinity;
            for (var i = 0; i < count; i++)
            {
                var col = overlapBuffer[i];
                if (col == null)
                {
                    continue;
                }

                var node = col.GetComponentInParent<PrototypeHarvestNode>();
                if (node == null)
                {
                    continue;
                }

                var distSq = (node.transform.position - transform.position).sqrMagnitude;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    best = node;
                }
            }

            if (best != currentTarget)
            {
                currentTarget = best;
                heldSeconds = 0f;
                waitingForRelease = false;
            }
        }

        private void ResolveInterfaces()
        {
            itemReceiver = null;
            toolSource = null;

            var behaviours = GetComponents<MonoBehaviour>();
            for (var i = 0; i < behaviours.Length; i++)
            {
                var behaviour = behaviours[i];
                if (behaviour == null)
                {
                    continue;
                }

                if (itemReceiver == null && behaviour is IPrototypeItemReceiver receiver)
                {
                    itemReceiver = receiver;
                }

                if (toolSource == null && behaviour is IPrototypeToolSource source)
                {
                    toolSource = source;
                }

                if (itemReceiver != null && toolSource != null)
                {
                    return;
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.42f, 0.81f, 0.94f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, interactRadius);
        }
    }
}
