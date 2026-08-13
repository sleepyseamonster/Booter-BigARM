using UnityEngine;
using UnityEngine.InputSystem;

namespace BooterBigArm.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public sealed class IsometricBigArmFollower3D : MonoBehaviour
    {
        [SerializeField] private Transform followTarget;
        [SerializeField] private Transform cameraBasis;
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string actionMapName = "Gameplay";
        [SerializeField] private string recallActionName = "RecallBigArm";
        [SerializeField, Min(1f)] private float followDistance = 5.25f;
        [SerializeField, Min(0.1f)] private float followSpeed = 6f;
        [SerializeField, Min(1f)] private float recallDistance = 14f;
        [SerializeField, Min(0.05f)] private float positionTolerance = 0.25f;

        private Rigidbody body;
        private InputAction recallAction;

        public void Configure(Transform target, Transform movementCamera, InputActionAsset actions)
        {
            followTarget = target;
            cameraBasis = movementCamera;
            inputActions = actions;
        }

        public Vector3 GetDesiredPosition()
        {
            if (followTarget == null)
            {
                return transform.position;
            }

            var screenRight = cameraBasis != null
                ? Vector3.ProjectOnPlane(cameraBasis.right, Vector3.up)
                : Vector3.right;
            if (screenRight.sqrMagnitude <= 0.0001f)
            {
                screenRight = Vector3.right;
            }

            screenRight.Normalize();
            var destination = followTarget.position - (screenRight * followDistance);
            destination.y = transform.position.y;
            return destination;
        }

        public void RequestRecall()
        {
            EnsureBody();
            if (followTarget == null || body == null)
            {
                return;
            }

            body.position = GetDesiredPosition();
        }

        private void Awake()
        {
            EnsureBody();
        }

        private void EnsureBody()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            if (body == null)
            {
                return;
            }

            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
        }

        private void OnEnable()
        {
            recallAction = inputActions != null
                ? inputActions.FindActionMap(actionMapName, false)?.FindAction(recallActionName, false)
                : null;
            if (recallAction != null)
            {
                recallAction.performed += HandleRecall;
                recallAction.Enable();
            }
        }

        private void OnDisable()
        {
            if (recallAction != null)
            {
                recallAction.performed -= HandleRecall;
                recallAction.Disable();
                recallAction = null;
            }
        }

        private void FixedUpdate()
        {
            if (followTarget == null)
            {
                return;
            }

            var offset = transform.position - followTarget.position;
            offset.y = 0f;
            if (offset.magnitude > recallDistance)
            {
                RequestRecall();
                return;
            }

            var destination = GetDesiredPosition();
            if ((destination - transform.position).sqrMagnitude <= positionTolerance * positionTolerance)
            {
                return;
            }

            var next = Vector3.MoveTowards(transform.position, destination, followSpeed * Time.fixedDeltaTime);
            body.MovePosition(next);

            var direction = followTarget.position - next;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
            {
                body.MoveRotation(Quaternion.LookRotation(direction.normalized, Vector3.up));
            }
        }

        private void HandleRecall(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Performed)
            {
                RequestRecall();
            }
        }
    }
}
