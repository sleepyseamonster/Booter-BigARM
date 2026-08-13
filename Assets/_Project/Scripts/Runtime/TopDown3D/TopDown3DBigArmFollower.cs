using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
    public sealed class TopDown3DBigArmFollower : MonoBehaviour
    {
        public enum FollowState
        {
            Idle,
            Follow,
            Avoid,
            Recover,
            Recall
        }

        private static readonly float[] AvoidanceAngles = { 0f, 32f, -32f, 64f, -64f, 105f, -105f };
        private const int GroundHitCapacity = 12;
        private const int OverlapCapacity = 16;

        [SerializeField] private Transform followTarget;
        [SerializeField] private Transform cameraBasis;
        [SerializeField] private TopDown3DInputRouter input;
        [SerializeField, Min(1f)] private float followDistance = 4.2f;
        [SerializeField] private float screenSideOffset = -1.8f;
        [SerializeField, Min(0.1f)] private float moveSpeed = 4.2f;
        [SerializeField, Min(0.1f)] private float turnSpeedDegrees = 300f;
        [SerializeField, Min(0.1f)] private float idleRadius = 1.1f;
        [SerializeField, Min(0.5f)] private float avoidanceProbeDistance = 1.8f;
        [SerializeField, Min(2f)] private float recallDistance = 18f;
        [SerializeField, Min(0.1f)] private float groundClearance = 0.82f;
        [SerializeField, Min(0.2f)] private float stuckCheckSeconds = 1.25f;
        [SerializeField] private LayerMask movementMask = ~0;

        private readonly RaycastHit[] groundHits = new RaycastHit[GroundHitCapacity];
        private readonly Collider[] overlaps = new Collider[OverlapCapacity];
        private Rigidbody body;
        private BoxCollider bodyCollider;
        private TopDown3DInputRouter subscribedInput;
        private Vector3 stuckSamplePosition;
        private float stuckTimer;
        private bool recallRequested;

        public FollowState State { get; private set; } = FollowState.Idle;

        public void Configure(Transform target, Transform movementCamera, TopDown3DInputRouter inputRouter)
        {
            followTarget = target;
            cameraBasis = movementCamera;
            input = inputRouter;
            RefreshInputSubscription();
        }

        public void RequestRecall()
        {
            recallRequested = true;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            bodyCollider = GetComponent<BoxCollider>();
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            stuckSamplePosition = transform.position;
        }

        private void OnEnable()
        {
            RefreshInputSubscription();
        }

        private void OnDisable()
        {
            SetSubscribedInput(null);
        }

        private void FixedUpdate()
        {
            if (followTarget == null || body == null)
            {
                return;
            }

            var desired = GetDesiredFollowPosition();
            var planarDistance = PlanarDistance(body.position, followTarget.position);
            if (recallRequested || planarDistance > recallDistance)
            {
                State = FollowState.Recall;
                RecallNearTarget(desired);
                recallRequested = false;
                ResetStuckTracking();
                return;
            }

            var toDesired = desired - body.position;
            toDesired.y = 0f;
            if (toDesired.magnitude <= idleRadius)
            {
                State = FollowState.Idle;
                ResetStuckTracking();
                return;
            }

            var direction = ChooseMovementDirection(toDesired.normalized);
            var movement = direction * moveSpeed * Time.fixedDeltaTime;
            var candidate = body.position + movement;
            if (TryProjectToGround(candidate, out var groundedCandidate))
            {
                State = Mathf.Abs(Vector3.SignedAngle(toDesired.normalized, direction, Vector3.up)) > 1f
                    ? FollowState.Avoid
                    : FollowState.Follow;
                body.MovePosition(groundedCandidate);
                body.MoveRotation(Quaternion.RotateTowards(
                    body.rotation,
                    Quaternion.LookRotation(direction, Vector3.up),
                    turnSpeedDegrees * Time.fixedDeltaTime));
            }

            UpdateStuckTracking(toDesired.sqrMagnitude > idleRadius * idleRadius);
        }

        private Vector3 GetDesiredFollowPosition()
        {
            var cameraForward = cameraBasis != null
                ? Vector3.ProjectOnPlane(cameraBasis.forward, Vector3.up).normalized
                : Vector3.forward;
            var cameraRight = cameraBasis != null
                ? Vector3.ProjectOnPlane(cameraBasis.right, Vector3.up).normalized
                : Vector3.right;
            var desired = followTarget.position - cameraForward * followDistance + cameraRight * screenSideOffset;
            return TryProjectToGround(desired, out var grounded) ? grounded : desired;
        }

        private Vector3 ChooseMovementDirection(Vector3 desiredDirection)
        {
            var recoveryBias = State == FollowState.Recover ? 45f : 0f;
            for (var i = 0; i < AvoidanceAngles.Length; i++)
            {
                var candidate = Quaternion.AngleAxis(AvoidanceAngles[i] + recoveryBias, Vector3.up) * desiredDirection;
                if (!body.SweepTest(candidate, out var hit, avoidanceProbeDistance, QueryTriggerInteraction.Ignore)
                    || hit.collider == null
                    || hit.collider.GetComponent<TopDown3DGroundSurface>() != null)
                {
                    return candidate.normalized;
                }
            }

            return Quaternion.AngleAxis(110f, Vector3.up) * desiredDirection;
        }

        private bool TryProjectToGround(Vector3 position, out Vector3 grounded)
        {
            var origin = position + Vector3.up * 10f;
            var count = Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                groundHits,
                30f,
                movementMask,
                QueryTriggerInteraction.Ignore);
            var nearest = float.PositiveInfinity;
            var found = false;
            grounded = position;
            for (var i = 0; i < count; i++)
            {
                var hit = groundHits[i];
                if (hit.collider == null
                    || hit.collider.transform.IsChildOf(transform)
                    || hit.collider.GetComponent<TopDown3DGroundSurface>() == null
                    || hit.distance >= nearest)
                {
                    continue;
                }

                nearest = hit.distance;
                grounded = hit.point + Vector3.up * groundClearance;
                found = true;
            }

            return found;
        }

        private void RecallNearTarget(Vector3 desired)
        {
            var cameraRight = cameraBasis != null
                ? Vector3.ProjectOnPlane(cameraBasis.right, Vector3.up).normalized
                : Vector3.right;
            var candidates = new[]
            {
                desired,
                desired + cameraRight * 2.5f,
                desired - cameraRight * 2.5f,
                followTarget.position - cameraRight * 3.5f
            };

            for (var i = 0; i < candidates.Length; i++)
            {
                if (!TryProjectToGround(candidates[i], out var grounded) || !IsPositionClear(grounded))
                {
                    continue;
                }

                body.position = grounded;
                var facing = Vector3.ProjectOnPlane(followTarget.position - grounded, Vector3.up);
                if (facing.sqrMagnitude > 0.0001f)
                {
                    body.rotation = Quaternion.LookRotation(facing.normalized, Vector3.up);
                }

                return;
            }
        }

        private bool IsPositionClear(Vector3 position)
        {
            var extents = bodyCollider != null ? Vector3.Scale(bodyCollider.size, transform.lossyScale) * 0.42f : Vector3.one;
            var count = Physics.OverlapBoxNonAlloc(
                position,
                extents,
                overlaps,
                body.rotation,
                movementMask,
                QueryTriggerInteraction.Ignore);
            for (var i = 0; i < count; i++)
            {
                var collider = overlaps[i];
                if (collider == null
                    || collider.transform.IsChildOf(transform)
                    || collider.transform.IsChildOf(followTarget)
                    || collider.GetComponent<TopDown3DGroundSurface>() != null)
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private void UpdateStuckTracking(bool wantsToMove)
        {
            if (!wantsToMove)
            {
                ResetStuckTracking();
                return;
            }

            stuckTimer += Time.fixedDeltaTime;
            if (stuckTimer < stuckCheckSeconds)
            {
                return;
            }

            var moved = PlanarDistance(body.position, stuckSamplePosition);
            State = moved < 0.2f ? FollowState.Recover : State;
            stuckSamplePosition = body.position;
            stuckTimer = 0f;
        }

        private void ResetStuckTracking()
        {
            stuckSamplePosition = body != null ? body.position : transform.position;
            stuckTimer = 0f;
        }

        private void RefreshInputSubscription()
        {
            SetSubscribedInput(isActiveAndEnabled ? input : null);
        }

        private void SetSubscribedInput(TopDown3DInputRouter router)
        {
            if (subscribedInput == router)
            {
                return;
            }

            if (subscribedInput != null)
            {
                subscribedInput.RecallRequested -= RequestRecall;
            }

            subscribedInput = router;
            if (subscribedInput != null)
            {
                subscribedInput.RecallRequested += RequestRecall;
            }
        }

        private static float PlanarDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
