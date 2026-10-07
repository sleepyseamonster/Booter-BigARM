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
            WaitingForTerrain,
            WaitingForRoute
        }

        private const int GroundHitCapacity = 12;
        private const int ObstacleHitCapacity = 16;
        private const int RouteGridSize = 13;
        private const int RouteGridCenter = RouteGridSize / 2;

        [SerializeField] private Transform followTarget;
        [SerializeField] private Transform cameraBasis;
        [SerializeField] private TopDown3DInputRouter input;
        [SerializeField] private TopDown3DBigArmCargo cargo;
        [SerializeField] private TopDown3DBigArmState companionState;
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
        [Header("Legger Ground Route")]
        [SerializeField, Range(1f, 60f)] private float maximumRouteSlope = 38f;
        [SerializeField, Min(0.05f)] private float maximumStepUp = 0.55f;
        [SerializeField, Min(0.05f)] private float maximumStepDown = 0.75f;
        [SerializeField, Range(0.8f, 2f)] private float routeCellSize = 1.25f;
        [SerializeField, Min(0.1f)] private float routeReplanSeconds = 0.75f;
        [SerializeField] private LayerMask movementMask = ~0;

        private readonly RaycastHit[] groundHits = new RaycastHit[GroundHitCapacity];
        private readonly Collider[] obstacleHits = new Collider[ObstacleHitCapacity];
        private readonly List<Vector3> targetTrail = new List<Vector3>(192);
        private readonly List<Vector2Int> gridPath = new List<Vector2Int>(RouteGridSize * RouteGridSize);
        private readonly List<Vector3> route = new List<Vector3>(RouteGridSize * RouteGridSize);
        private readonly bool[,] routeTraversable = new bool[RouteGridSize, RouteGridSize];
        private readonly float[,] routeHeights = new float[RouteGridSize, RouteGridSize];
        private Rigidbody body;
        private BoxCollider bodyCollider;
        private Rigidbody followTargetBody;
        private TopDown3DInputRouter subscribedInput;
        private Vector3 stuckSamplePosition;
        private float stuckTimer;
        private float currentSpeed;
        private bool callRequested;
        private bool automaticCatchUp;
        private bool startupGroundingComplete;
        private int routeIndex;
        private float routeReplanAt;
        private Vector3 routeGoal;

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
            cargo = GetComponent<TopDown3DBigArmCargo>();
            companionState = GetComponent<TopDown3DBigArmState>();
            targetTrail.Clear();
            InvalidateRoute();
            RefreshInputSubscription();
        }

        public void RequestRecall()
        {
            // "Recall" asks the Legger to traverse back urgently. It never relocates him.
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
            bodyCollider = GetComponent<BoxCollider>();
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
            startupGroundingComplete = false;
        }

        private void FixedUpdate()
        {
            if (followTarget == null || body == null)
            {
                return;
            }

            if (!startupGroundingComplete)
            {
                if (!TryInitializeOnGround())
                {
                    State = FollowState.WaitingForTerrain;
                }

                return;
            }

            RecordTargetTrail();
            var desired = GetDesiredFollowPosition();
            var distanceToDesired = PlanarDistance(body.position, desired);
            if (distanceToDesired <= idleRadius)
            {
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * Time.fixedDeltaTime);
                State = FollowState.Idle;
                InvalidateRoute();
                ResetStuckTracking();
                return;
            }
            if (!TryGetRouteDirection(desired, out var desiredDirection))
            {
                State = FollowState.WaitingForRoute;
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * Time.fixedDeltaTime);
                return;
            }
            var distanceToBooter = PlanarDistance(body.position, followTarget.position);
            UpdateCatchUpIntent(distanceToBooter);
            var catchUpActive = callRequested || automaticCatchUp;

            var desiredSpeed = CalculateDesiredSpeed(
                distanceToDesired,
                idleRadius,
                slowdownRadius,
                GetCruiseSpeed(),
                catchUpSpeed,
                catchUpActive);
            var loadSpeed = cargo != null ? cargo.LoadProfile.SpeedMultiplier : 1f;
            desiredSpeed *= loadSpeed;
            var loadAcceleration = cargo != null ? cargo.LoadProfile.AccelerationMultiplier : 1f;
            currentSpeed = Mathf.MoveTowards(
                currentSpeed,
                desiredSpeed,
                (desiredSpeed > currentSpeed ? acceleration : deceleration) * loadAcceleration * Time.fixedDeltaTime);

            var direction = desiredDirection;
            var turnAngle = Vector3.Angle(transform.forward, direction);
            var turnSpeedFactor = Mathf.Lerp(1f, sharpTurnSpeedScale, turnAngle / 180f);
            var movement = direction * currentSpeed * turnSpeedFactor * Time.fixedDeltaTime;
            var candidate = body.position + movement;
            if (!TryProjectToGround(candidate, out var groundedCandidate)
                || !IsStepTraversable(body.position, groundedCandidate)
                || IsOccupied(groundedCandidate))
            {
                InvalidateRoute();
                State = FollowState.WaitingForRoute;
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * Time.fixedDeltaTime);
                return;
            }

            var avoidanceAngle = Mathf.Abs(Vector3.SignedAngle(
                Vector3.ProjectOnPlane(desired - body.position, Vector3.up), direction, Vector3.up));
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
            companionState?.PublishPosition(body.position);

            UpdateStuckTracking(distanceToDesired > idleRadius);
        }

        internal bool TryInitializeOnGround()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            if (body == null)
            {
                return false;
            }

            // Booter is placed from the generated world's current safe-spawn elevation, while
            // The Legger's serialized position can still contain the older prototype elevation.
            // Use Booter's live height as the startup reference and a deliberately broad one-time
            // terrain probe. Normal following keeps the tighter projection window below.
            var startupProbe = body.position;
            if (followTarget != null)
            {
                startupProbe.y = followTarget.position.y;
            }

            if (!TryProjectToGround(startupProbe, 40f, 120f, out var groundedPosition))
            {
                return false;
            }

            // The companion starts before procedural terrain exists. Teleporting once after its
            // ground collider appears avoids sweeping the kinematic body vertically through Booter.
            body.position = groundedPosition;
            currentSpeed = 0f;
            startupGroundingComplete = true;
            InvalidateRoute();
            State = FollowState.Idle;
            ResetStuckTracking();
            GetComponent<TopDown3DBigArmState>()?.PublishPosition(body.position);
            return true;
        }

        internal void PrepareInitialWorldStartPosition(Vector3 groundPosition)
        {
            if (startupGroundingComplete)
            {
                return;
            }

            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            var position = groundPosition + Vector3.up * groundClearance;
            if (body != null)
            {
                body.position = position;
            }

            transform.position = position;
            GetComponent<TopDown3DBigArmState>()?.PublishPosition(position);
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

        private bool TryGetRouteDirection(Vector3 desired, out Vector3 direction)
        {
            direction = Vector3.zero;
            if (route.Count == 0 || routeIndex >= route.Count || Time.time >= routeReplanAt
                || PlanarDistance(routeGoal, desired) > routeCellSize * 2f)
            {
                if (!BuildRoute(desired))
                    return false;
            }

            while (routeIndex < route.Count && PlanarDistance(body.position, route[routeIndex]) < 0.3f)
                routeIndex++;
            if (routeIndex >= route.Count)
            {
                if (!BuildRoute(desired))
                    return false;
            }

            direction = Vector3.ProjectOnPlane(route[routeIndex] - body.position, Vector3.up).normalized;
            return direction.sqrMagnitude > 0.0001f;
        }

        private bool BuildRoute(Vector3 desired)
        {
            route.Clear();
            routeIndex = 0;
            routeGoal = desired;
            routeReplanAt = Time.time + routeReplanSeconds;
            for (var x = 0; x < RouteGridSize; x++)
            for (var z = 0; z < RouteGridSize; z++)
            {
                var probe = body.position + new Vector3(
                    (x - RouteGridCenter) * routeCellSize, 0f,
                    (z - RouteGridCenter) * routeCellSize);
                var valid = TryProjectToGround(probe, out var ground);
                routeTraversable[x, z] = valid && !IsOccupied(ground);
                routeHeights[x, z] = ground.y;
            }

            // The live position is already occupied by this Rigidbody; preserve its
            // exact elevation even when the grid probe lands on a nearby triangle.
            routeTraversable[RouteGridCenter, RouteGridCenter] = true;
            routeHeights[RouteGridCenter, RouteGridCenter] = body.position.y;
            var goal = new Vector2Int(
                Mathf.Clamp(RouteGridCenter + Mathf.RoundToInt((desired.x - body.position.x) / routeCellSize), 0, RouteGridSize - 1),
                Mathf.Clamp(RouteGridCenter + Mathf.RoundToInt((desired.z - body.position.z) / routeCellSize), 0, RouteGridSize - 1));
            if (!TopDown3DLocalGridPathfinder.TryFindPath(routeTraversable, routeHeights,
                    new Vector2Int(RouteGridCenter, RouteGridCenter), goal,
                    maximumStepUp, maximumStepDown, gridPath))
                return false;

            foreach (var cell in gridPath)
                route.Add(new Vector3(
                    body.position.x + (cell.x - RouteGridCenter) * routeCellSize,
                    routeHeights[cell.x, cell.y],
                    body.position.z + (cell.y - RouteGridCenter) * routeCellSize));
            return route.Count > 0;
        }

        private bool IsOccupied(Vector3 grounded)
        {
            var scale = transform.lossyScale;
            var half = Vector3.Scale(bodyCollider.size, new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z))) * 0.48f;
            var center = grounded + transform.TransformVector(bodyCollider.center);
            var count = Physics.OverlapBoxNonAlloc(center, half, obstacleHits,
                body.rotation, movementMask, QueryTriggerInteraction.Ignore);
            if (count == obstacleHits.Length)
                return true;
            for (var i = 0; i < count; i++)
            {
                var other = obstacleHits[i];
                if (other != null && !other.transform.IsChildOf(transform)
                    && other.GetComponent<TopDown3DGroundSurface>() == null)
                    return true;
            }
            return false;
        }

        private bool IsStepTraversable(Vector3 from, Vector3 to)
        {
            var rise = to.y - from.y;
            return rise <= maximumStepUp && rise >= -maximumStepDown;
        }

        private void InvalidateRoute()
        {
            route.Clear();
            routeIndex = 0;
            routeReplanAt = 0f;
        }

        private bool TryProjectToGround(Vector3 position, out Vector3 grounded)
        {
            return TryProjectToGround(position, 10f, 30f, out grounded);
        }

        private bool TryProjectToGround(
            Vector3 position,
            float probeHeight,
            float probeDistance,
            out Vector3 grounded)
        {
            var origin = position + Vector3.up * probeHeight;
            var count = Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                groundHits,
                probeDistance,
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
                    || hit.distance >= nearest
                    || Vector3.Angle(hit.normal, Vector3.up) > maximumRouteSlope)
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
            maximumStepDown = Mathf.Max(maximumStepUp, maximumStepDown);
        }

        private static float PlanarDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
