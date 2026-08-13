using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public sealed class TopDown3DPlayerMotor : MonoBehaviour
    {
        private const int GroundHitCapacity = 8;

        [SerializeField] private TopDown3DInputRouter input;
        [SerializeField] private Transform cameraBasis;
        [SerializeField, Min(0.1f)] private float walkSpeed = 5.4f;
        [SerializeField, Min(0.1f)] private float sprintSpeed = 7.4f;
        [SerializeField, Min(0.1f)] private float acceleration = 30f;
        [SerializeField, Min(0.1f)] private float deceleration = 36f;
        [SerializeField, Min(1f)] private float turnSpeedDegrees = 720f;
        [SerializeField, Range(1f, 75f)] private float maxWalkableSlope = 48f;
        [SerializeField, Min(0.01f)] private float groundProbeDistance = 0.28f;
        [SerializeField] private LayerMask groundMask = ~0;

        private readonly RaycastHit[] groundHits = new RaycastHit[GroundHitCapacity];
        private Rigidbody body;
        private CapsuleCollider capsule;
        private Vector3 facingDirection = Vector3.forward;

        public Vector3 Position => body != null ? body.position : transform.position;
        public Vector3 Velocity => body != null ? body.linearVelocity : Vector3.zero;
        public Vector3 FacingDirection => facingDirection;
        public bool IsGrounded { get; private set; }
        public bool SprintActive { get; private set; }

        public void Configure(TopDown3DInputRouter inputRouter, Transform movementCamera)
        {
            input = inputRouter;
            cameraBasis = movementCamera;
        }

        public void Teleport(Vector3 position)
        {
            EnsureBody();
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.position = position;
            }

            transform.position = position;
        }

        private void Awake()
        {
            EnsureBody();
            capsule = GetComponent<CapsuleCollider>();
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            body.linearDamping = 0f;
        }

        private void FixedUpdate()
        {
            if (input == null || cameraBasis == null)
            {
                return;
            }

            var desiredDirection = TopDown3DMovementBasis.ToWorldDirection(
                input.MoveValue,
                cameraBasis.forward,
                cameraBasis.right);

            IsGrounded = TryGetGroundNormal(out var groundNormal);
            if (IsGrounded && desiredDirection.sqrMagnitude > 0.0001f)
            {
                var inputMagnitude = desiredDirection.magnitude;
                desiredDirection = Vector3.ProjectOnPlane(desiredDirection, groundNormal).normalized * inputMagnitude;
            }

            SprintActive = input.SprintHeld && desiredDirection.sqrMagnitude > 0.0001f;
            var targetVelocity = desiredDirection * (SprintActive ? sprintSpeed : walkSpeed);
            var currentVelocity = body.linearVelocity;
            var currentPlanar = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
            var targetPlanar = new Vector3(targetVelocity.x, 0f, targetVelocity.z);
            var rate = targetPlanar.sqrMagnitude > 0.0001f ? acceleration : deceleration;
            var nextPlanar = Vector3.MoveTowards(currentPlanar, targetPlanar, rate * Time.fixedDeltaTime);

            var verticalVelocity = currentVelocity.y;
            if (IsGrounded)
            {
                verticalVelocity = desiredDirection.sqrMagnitude > 0.0001f
                    ? targetVelocity.y
                    : Mathf.Min(verticalVelocity, -1.5f);
            }

            body.linearVelocity = new Vector3(nextPlanar.x, verticalVelocity, nextPlanar.z);

            var planarFacing = new Vector3(desiredDirection.x, 0f, desiredDirection.z);
            if (planarFacing.sqrMagnitude > 0.0001f)
            {
                facingDirection = planarFacing.normalized;
                body.MoveRotation(Quaternion.RotateTowards(
                    body.rotation,
                    Quaternion.LookRotation(facingDirection, Vector3.up),
                    turnSpeedDegrees * Time.fixedDeltaTime));
            }
        }

        private bool TryGetGroundNormal(out Vector3 normal)
        {
            normal = Vector3.up;
            if (capsule == null)
            {
                return false;
            }

            var scale = transform.lossyScale;
            var radius = capsule.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z)) * 0.88f;
            var halfHeight = Mathf.Max(radius, capsule.height * Mathf.Abs(scale.y) * 0.5f);
            var center = transform.TransformPoint(capsule.center);
            var distance = Mathf.Max(0f, halfHeight - radius) + groundProbeDistance;
            var hitCount = Physics.SphereCastNonAlloc(
                center,
                radius,
                Vector3.down,
                groundHits,
                distance,
                groundMask,
                QueryTriggerInteraction.Ignore);

            var nearestDistance = float.PositiveInfinity;
            var found = false;
            for (var i = 0; i < hitCount; i++)
            {
                var hit = groundHits[i];
                if (hit.collider == null || hit.collider.transform.IsChildOf(transform))
                {
                    continue;
                }

                var slope = Vector3.Angle(hit.normal, Vector3.up);
                if (slope > maxWalkableSlope || hit.distance >= nearestDistance)
                {
                    continue;
                }

                nearestDistance = hit.distance;
                normal = hit.normal;
                found = true;
            }

            return found;
        }

        private void EnsureBody()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }
        }

        private void OnValidate()
        {
            sprintSpeed = Mathf.Max(walkSpeed, sprintSpeed);
            acceleration = Mathf.Max(0.1f, acceleration);
            deceleration = Mathf.Max(0.1f, deceleration);
            turnSpeedDegrees = Mathf.Max(1f, turnSpeedDegrees);
            groundProbeDistance = Mathf.Max(0.01f, groundProbeDistance);
        }
    }
}
