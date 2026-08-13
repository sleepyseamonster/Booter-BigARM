using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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
            CatchUp,
            WaitingForTerrain
        }

        private static readonly float[] AvoidanceAngles = { 0f, 32f, -32f, 64f, -64f, 105f, -105f };
        private const int GroundHitCapacity = 12;

        [SerializeField] private Transform followTarget;
        [SerializeField] private Transform cameraBasis;
        [SerializeField] private TopDown3DInputRouter input;
        [SerializeField, Min(1f)] private float followDistance = 4.2f;
        [SerializeField, Min(0.1f)] private float moveSpeed = 5.8f;
        [SerializeField, Min(0.1f)] private float catchUpSpeed = 8.4f;
        [SerializeField, Min(0.1f)] private float acceleration = 14f;
        [SerializeField, Min(0.1f)] private float deceleration = 20f;
        [SerializeField, Min(0.1f)] private float turnSpeedDegrees = 300f;
        [SerializeField, Range(0.1f, 1f)] private float sharpTurnSpeedScale = 0.45f;
        [SerializeField, Min(0.1f)] private float idleRadius = 0.85f;
        [SerializeField, Min(0.2f)] private float slowdownRadius = 2.8f;
        [SerializeField, Min(0.5f)] private float avoidanceProbeDistance = 1.8f;
        [FormerlySerializedAs("recallDistance")]
        [SerializeField, Min(2f)] private float catchUpDistance = 18f;
        [SerializeField, Min(1f)] private float catchUpReleaseDistance = 7f;
        [SerializeField, Min(0.25f)] private float trailSampleDistance = 1.1f;
        [SerializeField, Min(10f)] private float trailRetentionDistance = 180f;
        [SerializeField, Min(0.1f)] private float groundClearance = 0.82f;
        [SerializeField, Min(0.2f)] private float stuckCheckSeconds = 1.25f;
        [SerializeField] private LayerMask movementMask = ~0;

        private readonly RaycastHit[] groundHits = new RaycastHit[GroundHitCapacity];
        private readonly List<Vector3> targetTrail = new List<Vector3>(192);
        private Rigidbody body;
        private Rigidbody followTargetBody;
        private TopDown3DInputRouter subscribedInput;
        private Vector3 stuckSamplePosition;
        private float stuckTimer;
        private float currentSpeed;
        private bool callRequested;
        private bool automaticCatchUp;

        public FollowState State { get; private set; } = FollowState.Idle;
        public float CurrentSpeed => currentSpeed;
        public float DistanceToBooter => followTarget != null
            ? PlanarDistance(transform.position, followTarget.position)
            : 0f;

        public void Configure(Transform target, Transform movementCamera, TopDown3DInputRouter inputRouter)
        {
            followTarget = target;
            followTargetBody = followTarget != null ? followTarget.GetComponent<Rigidbody>() : null;
            cameraBasis = movementCamera;
            input = inputRouter;
            targetTrail.Clear();
            RefreshInputSubscription();
        }

        public void RequestRecall()
        {
            // "Recall" means ask BigARM to traverse back urgently. It never relocates him.
            callRequested = true;
        }

        public static float CalculateDesiredSpeed(
            float distanceToDestination,
            float stopRadius,
            float slowdownDistance,
            float cruiseSpeed,
            float urgentSpeed,
            bool urgent)
        {
            if (distanceToDestination <= stopRadius)
            {
                return 0f;
            }

            var safeSlowdownDistance = Mathf.Max(stopRadius + 0.01f, slowdownDistance);
            var speedScale = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.InverseLerp(stopRadius, safeSlowdownDistance, distanceToDestination));
            return Mathf.Max(0f, urgent ? urgentSpeed : cruiseSpeed) * speedScale;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            followTargetBody = followTarget != null ? followTarget.GetComponent<Rigidbody>() : null;
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

            RecordTargetTrail();
            var desired = GetDesiredFollowPosition();
            var distanceToBooter = PlanarDistance(body.position, followTarget.position);
            UpdateCatchUpIntent(distanceToBooter);
            var catchUpActive = callRequested || automaticCatchUp;

            var toDesired = desired - body.position;
            toDesired.y = 0f;
            var distanceToDesired = toDesired.magnitude;
            var desiredSpeed = CalculateDesiredSpeed(
                distanceToDesired,
                idleRadius,
                slowdownRadius,
                GetCruiseSpeed(),
                catchUpSpeed,
                catchUpActive);
            currentSpeed = Mathf.MoveTowards(
                currentSpeed,
                desiredSpeed,
                (desiredSpeed > currentSpeed ? acceleration : deceleration) * Time.fixedDeltaTime);

            if (distanceToDesired <= idleRadius && currentSpeed <= 0.01f)
            {
                State = FollowState.Idle;
                ResetStuckTracking();
                return;
            }

            if (toDesired.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            var desiredDirection = toDesired.normalized;
            var direction = ChooseMovementDirection(desiredDirection);
            var turnAngle = Vector3.Angle(transform.forward, direction);
            var turnSpeedFactor = Mathf.Lerp(1f, sharpTurnSpeedScale, turnAngle / 180f);
            var movement = direction * currentSpeed * turnSpeedFactor * Time.fixedDeltaTime;
            var candidate = body.position + movement;
            if (!TryProjectToGround(candidate, out var groundedCandidate))
            {
                State = FollowState.WaitingForTerrain;
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * Time.fixedDeltaTime);
                return;
            }

            var avoidanceAngle = Mathf.Abs(Vector3.SignedAngle(desiredDirection, direction, Vector3.up));
            State = catchUpActive
                ? FollowState.CatchUp
                : avoidanceAngle > 1f
                    ? FollowState.Avoid
                    : FollowState.Follow;
            body.MovePosition(groundedCandidate);
            body.MoveRotation(Quaternion.RotateTowards(
                body.rotation,
                Quaternion.LookRotation(direction, Vector3.up),
                turnSpeedDegrees * Time.fixedDeltaTime));

            UpdateStuckTracking(distanceToDesired > idleRadius);
        }

        private void UpdateCatchUpIntent(float distanceToBooter)
        {
            if (distanceToBooter >= catchUpDistance)
            {
                automaticCatchUp = true;
            }

            if (distanceToBooter <= catchUpReleaseDistance)
            {
                automaticCatchUp = false;
                callRequested = false;
            }
        }

        private float GetCruiseSpeed()
        {
            if (followTargetBody == null)
            {
                return moveSpeed;
            }

            var targetVelocity = followTargetBody.linearVelocity;
            targetVelocity.y = 0f;
            return Mathf.Max(moveSpeed, targetVelocity.magnitude + 0.4f);
        }

        private void RecordTargetTrail()
        {
            if (targetTrail.Count == 0)
            {
                SeedTargetTrail();
                return;
            }

            var currentTargetPosition = followTarget.position;
            if (PlanarDistance(targetTrail[targetTrail.Count - 1], currentTargetPosition) < trailSampleDistance)
            {
                return;
            }

            targetTrail.Add(currentTargetPosition);
            TrimTargetTrail();
        }

        private void SeedTargetTrail()
        {
            var targetPosition = followTarget.position;
            var behindDirection = body.position - targetPosition;
            behindDirection.y = 0f;
            if (behindDirection.sqrMagnitude <= 0.0001f)
            {
                behindDirection = cameraBasis != null
                    ? -Vector3.ProjectOnPlane(cameraBasis.forward, Vector3.up)
                    : Vector3.back;
            }

            if (behindDirection.sqrMagnitude <= 0.0001f)
            {
                behindDirection = Vector3.back;
            }

            behindDirection.Normalize();
            targetTrail.Add(targetPosition + behindDirection * followDistance);
            targetTrail.Add(targetPosition);
        }

        private void TrimTargetTrail()
        {
            var retainedDistance = 0f;
            for (var i = targetTrail.Count - 1; i > 0; i--)
            {
                retainedDistance += PlanarDistance(targetTrail[i], targetTrail[i - 1]);
                if (retainedDistance <= trailRetentionDistance)
                {
                    continue;
                }

                targetTrail.RemoveRange(0, i);
                return;
            }
        }

        private Vector3 GetDesiredFollowPosition()
        {
            var desired = GetTrailPositionBehindTarget(followDistance);
            return TryProjectToGround(desired, out var grounded) ? grounded : desired;
        }

        private Vector3 GetTrailPositionBehindTarget(float distanceBehindTarget)
        {
            if (targetTrail.Count == 0)
            {
                return followTarget.position;
            }

            var remaining = Mathf.Max(0f, distanceBehindTarget);
            var newer = followTarget.position;
            for (var i = targetTrail.Count - 1; i >= 0; i--)
            {
                var older = targetTrail[i];
                var segmentLength = PlanarDistance(newer, older);
                if (segmentLength >= remaining && segmentLength > 0.0001f)
                {
                    return Vector3.Lerp(newer, older, remaining / segmentLength);
                }

                remaining -= segmentLength;
                newer = older;
            }

            return targetTrail[0];
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

        private void OnValidate()
        {
            moveSpeed = Mathf.Max(0.1f, moveSpeed);
            catchUpSpeed = Mathf.Max(moveSpeed, catchUpSpeed);
            acceleration = Mathf.Max(0.1f, acceleration);
            deceleration = Mathf.Max(0.1f, deceleration);
            turnSpeedDegrees = Mathf.Max(0.1f, turnSpeedDegrees);
            idleRadius = Mathf.Max(0.1f, idleRadius);
            slowdownRadius = Mathf.Max(idleRadius + 0.01f, slowdownRadius);
            catchUpDistance = Mathf.Max(followDistance + idleRadius, catchUpDistance);
            catchUpReleaseDistance = Mathf.Clamp(catchUpReleaseDistance, followDistance, catchUpDistance);
            trailSampleDistance = Mathf.Max(0.25f, trailSampleDistance);
            trailRetentionDistance = Mathf.Max(followDistance * 2f, trailRetentionDistance);
        }

        private static float PlanarDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
