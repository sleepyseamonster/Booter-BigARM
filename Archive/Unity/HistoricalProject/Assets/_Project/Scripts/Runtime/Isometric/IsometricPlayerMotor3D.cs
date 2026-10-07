using UnityEngine;

namespace BooterBigArm.Runtime
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public sealed class IsometricPlayerMotor3D : MonoBehaviour
    {
        [SerializeField] private PlayerInputAdapter input;
        [SerializeField] private Transform cameraBasis;
        [SerializeField, Min(0.1f)] private float walkSpeed = 5f;
        [SerializeField, Min(0.1f)] private float sprintSpeed = 7.5f;
        [SerializeField, Min(0.1f)] private float acceleration = 28f;
        [SerializeField, Min(0.1f)] private float deceleration = 34f;
        [SerializeField, Min(0.1f)] private float turnSpeedDegrees = 720f;
        [SerializeField, Min(0.1f)] private float groundProbeDistance = 1.2f;
        [SerializeField] private LayerMask groundMask = ~0;

        private Rigidbody body;
        private Vector3 facingDirection = Vector3.forward;

        public Vector3 Velocity => body != null ? body.linearVelocity : Vector3.zero;
        public Vector3 FacingDirection => facingDirection;
        public bool IsGrounded { get; private set; }

        public void Configure(PlayerInputAdapter inputAdapter, Transform movementCamera)
        {
            input = inputAdapter;
            cameraBasis = movementCamera;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        private void FixedUpdate()
        {
            if (input == null || cameraBasis == null)
            {
                return;
            }

            var inputDirection = IsometricMovementBasis.ToWorldDirection(
                input.MoveValue,
                cameraBasis.forward,
                cameraBasis.right);

            IsGrounded = TryGetGroundNormal(out var groundNormal);
            var desiredDirection = inputDirection;
            if (IsGrounded && desiredDirection.sqrMagnitude > 0.0001f)
            {
                var inputMagnitude = desiredDirection.magnitude;
                desiredDirection = Vector3.ProjectOnPlane(desiredDirection, groundNormal).normalized * inputMagnitude;
            }

            var desiredSpeed = input.SprintHeld ? sprintSpeed : walkSpeed;
            var targetVelocity = desiredDirection * desiredSpeed;
            var currentVelocity = body.linearVelocity;
            var currentTraversalVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
            var targetTraversalVelocity = new Vector3(targetVelocity.x, 0f, targetVelocity.z);
            var rate = targetTraversalVelocity.sqrMagnitude > 0.0001f ? acceleration : deceleration;
            var nextTraversalVelocity = Vector3.MoveTowards(
                currentTraversalVelocity,
                targetTraversalVelocity,
                rate * Time.fixedDeltaTime);

            var verticalVelocity = currentVelocity.y;
            if (IsGrounded && desiredDirection.sqrMagnitude > 0.0001f)
            {
                verticalVelocity = targetVelocity.y;
            }

            body.linearVelocity = new Vector3(nextTraversalVelocity.x, verticalVelocity, nextTraversalVelocity.z);

            var planarFacing = new Vector3(desiredDirection.x, 0f, desiredDirection.z);
            if (planarFacing.sqrMagnitude > 0.0001f)
            {
                facingDirection = planarFacing.normalized;
                var targetRotation = Quaternion.LookRotation(facingDirection, Vector3.up);
                body.MoveRotation(Quaternion.RotateTowards(
                    body.rotation,
                    targetRotation,
                    turnSpeedDegrees * Time.fixedDeltaTime));
            }
        }

        private bool TryGetGroundNormal(out Vector3 normal)
        {
            var origin = transform.position + (Vector3.up * 0.2f);
            if (Physics.Raycast(
                    origin,
                    Vector3.down,
                    out var hit,
                    groundProbeDistance,
                    groundMask,
                    QueryTriggerInteraction.Ignore))
            {
                normal = hit.normal;
                return true;
            }

            normal = Vector3.up;
            return false;
        }

        private void OnValidate()
        {
            walkSpeed = Mathf.Max(0.1f, walkSpeed);
            sprintSpeed = Mathf.Max(walkSpeed, sprintSpeed);
            acceleration = Mathf.Max(0.1f, acceleration);
            deceleration = Mathf.Max(0.1f, deceleration);
            turnSpeedDegrees = Mathf.Max(0.1f, turnSpeedDegrees);
            groundProbeDistance = Mathf.Max(0.1f, groundProbeDistance);
        }
    }
}
