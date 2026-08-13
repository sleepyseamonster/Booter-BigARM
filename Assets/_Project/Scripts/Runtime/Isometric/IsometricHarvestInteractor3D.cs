using UnityEngine;
using UnityEngine.InputSystem;

namespace BooterBigArm.Runtime
{
    [DisallowMultipleComponent]
    public sealed class IsometricHarvestInteractor3D : MonoBehaviour
    {
        private const int OverlapCapacity = 16;

        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string actionMapName = "Gameplay";
        [SerializeField] private string interactActionName = "Interact";
        [SerializeField] private IsometricPlayerMotor3D playerMotor;
        [SerializeField] private PrototypeInventory inventory;
        [SerializeField, Min(0.1f)] private float interactionDistance = 1.6f;
        [SerializeField, Min(0.1f)] private float interactionRadius = 1.25f;
        [SerializeField] private LayerMask interactionMask = ~0;

        private readonly Collider[] overlapResults = new Collider[OverlapCapacity];
        private InputAction interactAction;
        private IsometricHarvestNode3D currentNode;
        private float holdProgress;

        public IsometricHarvestNode3D CurrentNode => currentNode;
        public float NormalizedProgress => currentNode != null
            ? Mathf.Clamp01(holdProgress / currentNode.HoldDuration)
            : 0f;
        public string Prompt => currentNode == null
            ? ""
            : $"Hold Interact: {currentNode.DisplayName} ({NormalizedProgress:P0})";

        public void Configure(
            InputActionAsset actions,
            IsometricPlayerMotor3D motor,
            PrototypeInventory targetInventory)
        {
            inputActions = actions;
            playerMotor = motor;
            inventory = targetInventory;
        }

        private void OnEnable()
        {
            interactAction = inputActions != null
                ? inputActions.FindActionMap(actionMapName, false)?.FindAction(interactActionName, false)
                : null;
            interactAction?.Enable();
        }

        private void OnDisable()
        {
            interactAction?.Disable();
            interactAction = null;
            currentNode = null;
            holdProgress = 0f;
        }

        private void Update()
        {
            var nearest = FindNearestAvailableNode();
            if (nearest != currentNode)
            {
                currentNode = nearest;
                holdProgress = 0f;
            }

            if (currentNode == null || interactAction == null || !interactAction.IsPressed())
            {
                holdProgress = 0f;
                return;
            }

            holdProgress += Time.deltaTime;
            if (holdProgress < currentNode.HoldDuration)
            {
                return;
            }

            if (currentNode.TryHarvest(inventory))
            {
                currentNode = null;
            }

            holdProgress = 0f;
        }

        private IsometricHarvestNode3D FindNearestAvailableNode()
        {
            var facing = playerMotor != null ? playerMotor.FacingDirection : transform.forward;
            var center = transform.position + (facing * interactionDistance);
            var count = Physics.OverlapSphereNonAlloc(
                center,
                interactionRadius,
                overlapResults,
                interactionMask,
                QueryTriggerInteraction.Collide);

            IsometricHarvestNode3D nearest = null;
            var nearestDistance = float.PositiveInfinity;
            for (var i = 0; i < count; i++)
            {
                var node = overlapResults[i] != null
                    ? overlapResults[i].GetComponentInParent<IsometricHarvestNode3D>()
                    : null;
                if (node == null || !node.IsAvailable)
                {
                    continue;
                }

                var distance = (node.transform.position - transform.position).sqrMagnitude;
                if (distance >= nearestDistance)
                {
                    continue;
                }

                nearest = node;
                nearestDistance = distance;
            }

            return nearest;
        }

        private void OnDrawGizmosSelected()
        {
            var facing = playerMotor != null ? playerMotor.FacingDirection : transform.forward;
            Gizmos.color = new Color(1f, 0.75f, 0.1f, 0.45f);
            Gizmos.DrawWireSphere(transform.position + (facing * interactionDistance), interactionRadius);
        }
    }
}
