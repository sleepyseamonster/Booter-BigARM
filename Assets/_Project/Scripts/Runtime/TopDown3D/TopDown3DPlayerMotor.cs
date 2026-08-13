using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public sealed class TopDown3DPlayerMotor : MonoBehaviour
    {
        private const int GroundHitCapacity = 8;
        private const int TraversalHitCapacity = 12;
        private const int TraversalOverlapCapacity = 12;

        [SerializeField] private TopDown3DInputRouter input;
        [SerializeField] private Transform cameraBasis;
        [SerializeField, Min(0.1f)] private float walkSpeed = 4.2f;
        [SerializeField, Min(0.1f)] private float sprintSpeed = 7.4f;
        [SerializeField, Min(0.1f)] private float acceleration = 30f;
        [SerializeField, Min(0.1f)] private float deceleration = 36f;
        [SerializeField, Min(1f)] private float turnSpeedDegrees = 720f;
        [SerializeField, Range(1f, 75f)] private float maxWalkableSlope = 48f;
        [SerializeField, Min(0.01f)] private float groundProbeDistance = 0.28f;
        [SerializeField] private LayerMask groundMask = ~0;

        [Header("Smart Traversal")]
        [SerializeField] private bool smartTraversalEnabled = true;
        [SerializeField, Range(0.1f, 1f)] private float minimumTraversalInput = 0.35f;
        [SerializeField, Min(0.1f)] private float traversalProbeDistance = 1.25f;
        [SerializeField, Min(0.1f)] private float maximumVaultHeight = 1.15f;
        [SerializeField, Range(0f, 1f)] private float sideThresholdRatio = 0.48f;
        [SerializeField, Min(0.05f)] private float vaultDuration = 0.48f;
        [SerializeField, Min(0f)] private float minimumVaultArcHeight = 0.9f;
        [SerializeField, Min(0f)] private float vaultArcClearance = 0.18f;
        [SerializeField, Min(0f)] private float vaultLandingClearance = 0.35f;
        [SerializeField, Min(0.05f)] private float spinDuration = 0.34f;
        [SerializeField, Min(0f)] private float spinSideDistance = 1.15f;
        [SerializeField, Min(0f)] private float spinForwardDistance = 0.75f;
        [SerializeField, Min(0f)] private float traversalCooldown = 0.2f;
        [SerializeField] private LayerMask traversalMask = ~0;

        private readonly RaycastHit[] groundHits = new RaycastHit[GroundHitCapacity];
        private readonly RaycastHit[] traversalHits = new RaycastHit[TraversalHitCapacity];
        private readonly Collider[] traversalOverlaps = new Collider[TraversalOverlapCapacity];
        private Rigidbody body;
        private CapsuleCollider capsule;
        private Vector3 facingDirection = Vector3.forward;
        private Vector3 traversalStart;
        private Vector3 traversalControl;
        private Vector3 traversalEnd;
        private Vector3 traversalDirection;
        private Quaternion traversalStartRotation;
        private Quaternion traversalEndRotation;
        private float activeTraversalDuration;
        private float activeTraversalArcHeight;
        private float activeTraversalElapsed;
        private float activeSpinDirection;
        private float traversalCooldownRemaining;
        private bool gravityBeforeTraversal;

        public Vector3 Position => body != null ? body.position : transform.position;
        public Vector3 Velocity => body != null ? body.linearVelocity : Vector3.zero;
        public Vector3 FacingDirection => facingDirection;
        public bool IsGrounded { get; private set; }
        public bool SprintActive { get; private set; }
        public TopDown3DTraversalMove ActiveTraversal { get; private set; }
        public float ActiveTraversalDuration => activeTraversalDuration;

        public void Configure(TopDown3DInputRouter inputRouter, Transform movementCamera)
        {
            input = inputRouter;
            cameraBasis = movementCamera;
        }

        public void Teleport(Vector3 position)
        {
            EnsureBody();
            CancelTraversal();
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
            traversalCooldownRemaining = Mathf.Max(0f, traversalCooldownRemaining - Time.fixedDeltaTime);
            if (ActiveTraversal != TopDown3DTraversalMove.None)
            {
                AdvanceTraversal();
                return;
            }

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
            if (SprintActive && TryBeginTraversal(desiredDirection))
            {
                return;
            }

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

        private bool TryBeginTraversal(Vector3 desiredDirection)
        {
            if (!smartTraversalEnabled
                || !IsGrounded
                || traversalCooldownRemaining > 0f
                || desiredDirection.sqrMagnitude < minimumTraversalInput * minimumTraversalInput)
            {
                return false;
            }

            var forward = Vector3.ProjectOnPlane(desiredDirection, Vector3.up);
            if (forward.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            forward.Normalize();
            GetWorldCapsule(body.position, 0.92f, out var top, out var bottom, out var radius);
            var hitCount = Physics.CapsuleCastNonAlloc(
                top,
                bottom,
                radius,
                forward,
                traversalHits,
                traversalProbeDistance,
                traversalMask,
                QueryTriggerInteraction.Ignore);

            Collider obstacleCollider = null;
            var nearestDistance = float.PositiveInfinity;
            for (var i = 0; i < hitCount; i++)
            {
                var hit = traversalHits[i];
                if (hit.collider == null
                    || hit.collider.transform.IsChildOf(transform)
                    || hit.distance >= nearestDistance
                    || hit.collider.GetComponentInParent<TopDown3DTraversalObstacle>() == null)
                {
                    continue;
                }

                obstacleCollider = hit.collider;
                nearestDistance = hit.distance;
            }

            if (obstacleCollider == null)
            {
                return false;
            }

            var bounds = obstacleCollider.bounds;
            var footHeight = bottom.y - radius;
            var obstacleHeight = Mathf.Max(0f, bounds.max.y - footHeight);
            var lateral = Vector3.Cross(Vector3.up, forward).normalized;
            var lateralOffset = Vector3.Dot(body.position - bounds.center, lateral);
            var lateralHalfExtent = TopDown3DTraversalPlanner.ProjectedExtent(bounds, lateral);
            var move = TopDown3DTraversalPlanner.SelectMove(
                obstacleHeight,
                maximumVaultHeight,
                lateralOffset,
                lateralHalfExtent,
                sideThresholdRatio);

            switch (move)
            {
                case TopDown3DTraversalMove.Spin:
                    return TryBeginSpin(obstacleCollider, forward, lateral, lateralOffset, lateralHalfExtent, radius);
                case TopDown3DTraversalMove.Vault:
                    return TryBeginVault(obstacleCollider, forward, obstacleHeight, radius);
                default:
                    return false;
            }
        }

        private bool TryBeginSpin(
            Collider obstacle,
            Vector3 forward,
            Vector3 lateral,
            float lateralOffset,
            float lateralHalfExtent,
            float playerRadius)
        {
            var outwardSign = lateralOffset >= 0f ? 1f : -1f;
            var outward = lateral * outwardSign;
            var requiredSideDistance = lateralHalfExtent
                - Mathf.Abs(lateralOffset)
                + playerRadius
                + vaultLandingClearance;
            var sideDistance = Mathf.Max(spinSideDistance, requiredSideDistance);
            var planarTarget = body.position
                + outward * sideDistance
                + forward * spinForwardDistance;
            if (!TryResolveGroundedTarget(planarTarget, obstacle, out var target))
            {
                return false;
            }

            var control = body.position + outward * sideDistance;
            BeginTraversal(
                TopDown3DTraversalMove.Spin,
                target,
                control,
                forward,
                spinDuration,
                0f,
                outwardSign);
            return true;
        }

        private bool TryBeginVault(
            Collider obstacle,
            Vector3 forward,
            float obstacleHeight,
            float playerRadius)
        {
            var bounds = obstacle.bounds;
            var distanceToCenter = Vector3.Dot(bounds.center - body.position, forward);
            var obstacleHalfDepth = TopDown3DTraversalPlanner.ProjectedExtent(bounds, forward);
            var forwardDistance = Mathf.Max(
                traversalProbeDistance,
                distanceToCenter + obstacleHalfDepth + playerRadius + vaultLandingClearance);
            var planarTarget = body.position + forward * forwardDistance;
            if (!TryResolveGroundedTarget(planarTarget, obstacle, out var target))
            {
                return false;
            }

            var arcHeight = Mathf.Max(minimumVaultArcHeight, obstacleHeight + vaultArcClearance);
            BeginTraversal(
                TopDown3DTraversalMove.Vault,
                target,
                Vector3.zero,
                forward,
                vaultDuration,
                arcHeight,
                0f);
            return true;
        }

        private void BeginTraversal(
            TopDown3DTraversalMove move,
            Vector3 target,
            Vector3 control,
            Vector3 direction,
            float duration,
            float arcHeight,
            float spinDirection)
        {
            ActiveTraversal = move;
            traversalStart = body.position;
            traversalControl = control;
            traversalEnd = target;
            traversalDirection = direction;
            traversalStartRotation = body.rotation;
            traversalEndRotation = Quaternion.LookRotation(direction, Vector3.up);
            activeTraversalDuration = Mathf.Max(0.05f, duration);
            activeTraversalArcHeight = Mathf.Max(0f, arcHeight);
            activeTraversalElapsed = 0f;
            activeSpinDirection = spinDirection;
            gravityBeforeTraversal = body.useGravity;
            body.useGravity = false;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            IsGrounded = false;
            SprintActive = true;
            facingDirection = direction;
        }

        private void AdvanceTraversal()
        {
            activeTraversalElapsed += Time.fixedDeltaTime;
            var normalizedTime = Mathf.Clamp01(activeTraversalElapsed / activeTraversalDuration);
            var easedTime = normalizedTime * normalizedTime * (3f - 2f * normalizedTime);
            var position = ActiveTraversal == TopDown3DTraversalMove.Vault
                ? TopDown3DTraversalPlanner.CalculateVaultPoint(
                    traversalStart,
                    traversalEnd,
                    activeTraversalArcHeight,
                    easedTime)
                : TopDown3DTraversalPlanner.CalculateQuadraticPoint(
                    traversalStart,
                    traversalControl,
                    traversalEnd,
                    easedTime);

            var facingRotation = Quaternion.Slerp(traversalStartRotation, traversalEndRotation, easedTime);
            var rotation = ActiveTraversal == TopDown3DTraversalMove.Spin
                ? Quaternion.AngleAxis(activeSpinDirection * 360f * easedTime, Vector3.up) * facingRotation
                : facingRotation;
            body.MovePosition(position);
            body.MoveRotation(rotation);

            if (normalizedTime < 1f)
            {
                return;
            }

            var completedMove = ActiveTraversal;
            ActiveTraversal = TopDown3DTraversalMove.None;
            body.useGravity = gravityBeforeTraversal;
            body.linearVelocity = traversalDirection * sprintSpeed * 0.65f;
            traversalCooldownRemaining = traversalCooldown;
            SprintActive = input != null && input.SprintHeld;
            IsGrounded = completedMove == TopDown3DTraversalMove.Spin;
        }

        private bool TryResolveGroundedTarget(
            Vector3 planarTarget,
            Collider sourceObstacle,
            out Vector3 groundedTarget)
        {
            groundedTarget = planarTarget;
            GetWorldCapsule(body.position, 1f, out _, out var currentBottom, out var currentRadius);
            var bodyToFoot = body.position.y - (currentBottom.y - currentRadius);
            var rayOrigin = new Vector3(
                planarTarget.x,
                body.position.y + maximumVaultHeight + bodyToFoot + 2f,
                planarTarget.z);
            var rayDistance = maximumVaultHeight + bodyToFoot + 6f;
            var hitCount = Physics.RaycastNonAlloc(
                rayOrigin,
                Vector3.down,
                traversalHits,
                rayDistance,
                groundMask,
                QueryTriggerInteraction.Ignore);

            Collider groundCollider = null;
            var groundPoint = Vector3.zero;
            var nearestDistance = float.PositiveInfinity;
            for (var i = 0; i < hitCount; i++)
            {
                var hit = traversalHits[i];
                if (hit.collider == null
                    || hit.collider == sourceObstacle
                    || hit.collider.transform.IsChildOf(transform)
                    || hit.collider.GetComponentInParent<TopDown3DTraversalObstacle>() != null
                    || hit.distance >= nearestDistance
                    || Vector3.Angle(hit.normal, Vector3.up) > maxWalkableSlope)
                {
                    continue;
                }

                groundCollider = hit.collider;
                groundPoint = hit.point;
                nearestDistance = hit.distance;
            }

            if (groundCollider == null)
            {
                return false;
            }

            groundedTarget = new Vector3(planarTarget.x, groundPoint.y + bodyToFoot, planarTarget.z);
            return IsCapsuleClearAt(groundedTarget, groundCollider);
        }

        private bool IsCapsuleClearAt(Vector3 bodyPosition, Collider groundCollider)
        {
            GetWorldCapsule(bodyPosition, 0.94f, out var top, out var bottom, out var radius);
            var overlapCount = Physics.OverlapCapsuleNonAlloc(
                top,
                bottom,
                radius,
                traversalOverlaps,
                traversalMask,
                QueryTriggerInteraction.Ignore);
            for (var i = 0; i < overlapCount; i++)
            {
                var overlap = traversalOverlaps[i];
                if (overlap != null
                    && overlap != groundCollider
                    && overlap.GetComponentInParent<TopDown3DGroundSurface>() == null
                    && !overlap.transform.IsChildOf(transform))
                {
                    return false;
                }
            }

            return true;
        }

        private void GetWorldCapsule(
            Vector3 bodyPosition,
            float radiusScale,
            out Vector3 top,
            out Vector3 bottom,
            out float radius)
        {
            var scale = transform.lossyScale;
            radius = capsule.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z)) * radiusScale;
            var height = Mathf.Max(radius * 2f, capsule.height * Mathf.Abs(scale.y));
            var scaledCenter = Vector3.Scale(capsule.center, scale);
            var center = bodyPosition + transform.rotation * scaledCenter;
            var segmentHalfLength = Mathf.Max(0f, height * 0.5f - radius);
            top = center + Vector3.up * segmentHalfLength;
            bottom = center - Vector3.up * segmentHalfLength;
        }

        private void CancelTraversal()
        {
            if (ActiveTraversal == TopDown3DTraversalMove.None || body == null)
            {
                return;
            }

            ActiveTraversal = TopDown3DTraversalMove.None;
            body.useGravity = gravityBeforeTraversal;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            traversalCooldownRemaining = 0f;
            SprintActive = false;
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

        private void OnDisable()
        {
            CancelTraversal();
        }

        private void OnValidate()
        {
            sprintSpeed = Mathf.Max(walkSpeed, sprintSpeed);
            acceleration = Mathf.Max(0.1f, acceleration);
            deceleration = Mathf.Max(0.1f, deceleration);
            turnSpeedDegrees = Mathf.Max(1f, turnSpeedDegrees);
            groundProbeDistance = Mathf.Max(0.01f, groundProbeDistance);
            minimumTraversalInput = Mathf.Clamp(minimumTraversalInput, 0.1f, 1f);
            traversalProbeDistance = Mathf.Max(0.1f, traversalProbeDistance);
            maximumVaultHeight = Mathf.Max(0.1f, maximumVaultHeight);
            sideThresholdRatio = Mathf.Clamp01(sideThresholdRatio);
            vaultDuration = Mathf.Max(0.05f, vaultDuration);
            minimumVaultArcHeight = Mathf.Max(0f, minimumVaultArcHeight);
            vaultArcClearance = Mathf.Max(0f, vaultArcClearance);
            vaultLandingClearance = Mathf.Max(0f, vaultLandingClearance);
            spinDuration = Mathf.Max(0.05f, spinDuration);
            spinSideDistance = Mathf.Max(0f, spinSideDistance);
            spinForwardDistance = Mathf.Max(0f, spinForwardDistance);
            traversalCooldown = Mathf.Max(0f, traversalCooldown);
        }
    }
}
