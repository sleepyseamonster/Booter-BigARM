using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class TopDown3DCameraRig : MonoBehaviour
    {
        private const int ObstructionHitCapacity = 16;
        private const int OriginRecoveryProbeCount = 128;
        private const int OriginRecoveryRefinementCount = 8;
        public const float DefaultMaximumLookAheadDistance = 12f;
        public const float DefaultLookAheadSpeed = 12.6f;
        public const float DefaultLookAheadReturnSpeed = 37.8f;
        public const float DefaultMinimumPitchDegrees = 26f;

        [SerializeField] private Transform target;
        private TopDown3DCameraOptions cameraOptions;
        internal Transform FollowTarget => target;
        internal TopDown3DInputRouter InputRouter => input;
        internal bool ExternalViewOwnsOutput => cameraOptions != null && cameraOptions.OwnsOutput;
        public TopDown3DCameraOptions CameraOptions => cameraOptions;
        internal void RegisterCameraOptions(TopDown3DCameraOptions owner)
        {
            if (owner == null || owner.gameObject != gameObject) throw new System.InvalidOperationException("Camera options must share the output camera.");
            cameraOptions = owner;
        }
        internal bool HasRidingFrame => ridingFramingOwner != null;
        internal Vector3 FollowAnchorPosition => ridingFrame != null ? ridingFrame.position : target.position;
        internal float TargetHeightOffset => targetOffset.y;
        [SerializeField] private TopDown3DInputRouter input;
        [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.15f, 0f);
        [SerializeField, Range(20f, 75f)] private float pitchDegrees = 50f;
        [SerializeField] private float yawDegrees = 40f;
        [SerializeField, Range(20f, 75f)] private float minimumPitchDegrees = DefaultMinimumPitchDegrees;
        [SerializeField, Range(20f, 75f)] private float maximumPitchDegrees = 65f;
        [SerializeField, Range(30f, 240f)] private float yawSpeedDegrees = 120f;
        [SerializeField, Range(20f, 180f)] private float pitchSpeedDegrees = 70f;
        [SerializeField, Min(0f)] private float mouseSensitivityDegreesPerPixel = 0.30f;
        [SerializeField, Min(2f)] private float distance = 25f;
        [SerializeField, Range(20f, 80f)] private float fieldOfView = 48f;
        [SerializeField, Min(0f)] private float followSmoothTime = 0.14f;
        [SerializeField, Min(0f)] private float maximumLookAheadDistance = DefaultMaximumLookAheadDistance;
        [SerializeField, Min(0f)] private float lookAheadSpeed = DefaultLookAheadSpeed;
        [SerializeField, Min(0f)] private float lookAheadReturnSpeed = DefaultLookAheadReturnSpeed;
        [SerializeField, Min(0.05f)] private float obstructionRadius = 0.35f;
        [SerializeField, Min(1f)] private float minimumDistance = 4f;
        [SerializeField] private LayerMask obstructionMask = ~0;
        private readonly RaycastHit[] obstructionHits = new RaycastHit[ObstructionHitCapacity];
        private readonly Collider[] obstructionOverlaps = new Collider[ObstructionHitCapacity];
        private Camera outputCamera;
        private BadwaterCameraRange authoredCameraRange;
        private Vector3 smoothedTarget;
        private Vector3 targetVelocity;
        private Vector3 lookAheadOffset;
        private TopDown3DLeggerRidingController ridingFramingOwner;
        private Transform ridingFrame;
        private Transform ridingMount;
        private float ridingFramingBlend;
        private Vector3 ridingEntryTarget, lastRidingFrame;
        internal void SetRidingFrame(TopDown3DLeggerRidingController owner, Transform frame, Transform mountRoot)
        {
            if (owner == null || ridingFramingOwner != null && ridingFramingOwner != owner) return;
            ridingEntryTarget = smoothedTarget;
            ridingFramingOwner = owner; ridingFrame = frame; ridingMount = mountRoot;
        }
        internal void ReleaseRidingFrame(TopDown3DLeggerRidingController owner)
        {
            if (ridingFramingOwner != owner) return;
            ridingFrame = null;
            // Keep the old mount excluded until the framing blend has returned to Booter.
        }
        private bool initialized;
        private bool hasVerifiedClearPosition;
        private Vector3 verifiedClearPosition;

        public float PitchDegrees => pitchDegrees;
        public float YawDegrees => yawDegrees;
        public float Distance => distance;
        public float MaximumLookAheadDistance => maximumLookAheadDistance;
        public float LookAheadSpeed => lookAheadSpeed;
        public float LookAheadReturnSpeed => lookAheadReturnSpeed;
        public Vector3 LookAheadOffset => lookAheadOffset;
        public float MinimumPitchDegrees => minimumPitchDegrees;
        public bool HasUnresolvedClearance { get; private set; }

        public void Configure(Transform followTarget, TopDown3DInputRouter inputRouter = null)
        {
            target = followTarget;
            input = inputRouter;
            SnapToTarget();
        }

        public void SetViewAngles(float yaw, float pitch)
        {
            yawDegrees = Mathf.Repeat(yaw, 360f);
            pitchDegrees = Mathf.Clamp(pitch, minimumPitchDegrees, maximumPitchDegrees);
            SnapToTarget();
        }

        private void Awake()
        {
            outputCamera = GetComponent<Camera>();
            authoredCameraRange = GetComponent<BadwaterCameraRange>();
            EnsurePostProcessing();
            ResolveInput();
            ApplyLens();
        }

        public TopDown3DCameraOptions EnableCameraOptions()
        {
            ResolveInput();
            cameraOptions = GetComponent<TopDown3DCameraOptions>();
            if (cameraOptions == null) cameraOptions = gameObject.AddComponent<TopDown3DCameraOptions>();
            cameraOptions.Configure(this);
            return cameraOptions;
        }

        private void OnEnable()
        {
            SnapToTarget();
        }

        private void LateUpdate()
        {
            if (target == null || ExternalViewOwnsOutput)
            {
                return;
            }

            ApplyLens();
            ApplyCameraInput();
            var followPosition = UpdateFollowPosition(Time.deltaTime);
            var rotation = Quaternion.Euler(pitchDegrees, yawDegrees, 0f);
            ApplyResolvedPose(followPosition + lookAheadOffset, -(rotation * Vector3.forward), rotation);
        }

        internal Vector3 UpdateFollowPosition(float deltaTime)
        {
            var rawTarget = target.position + targetOffset;
            ridingFramingBlend = Mathf.MoveTowards(ridingFramingBlend, ridingFrame != null ? 1 : 0, deltaTime / .2f);
            if (ridingFrame != null)
            {
                lastRidingFrame = ridingFrame.position + targetOffset;
                rawTarget = Vector3.Lerp(ridingEntryTarget, lastRidingFrame, ridingFramingBlend);
            }
            else if (ridingFramingOwner != null) rawTarget = Vector3.Lerp(rawTarget, lastRidingFrame, ridingFramingBlend);
            if (ridingFrame == null && ridingFramingBlend <= 0) { ridingFramingOwner = null; ridingMount = null; }
            smoothedTarget = followSmoothTime <= 0f
                ? rawTarget
                : Vector3.SmoothDamp(smoothedTarget, rawTarget, ref targetVelocity, followSmoothTime, Mathf.Infinity, deltaTime);
            return smoothedTarget;
        }

        public static float CalculateYaw(float currentYaw, float inputValue, float speedDegrees, float deltaTime)
        {
            return Mathf.Repeat(
                currentYaw + Mathf.Clamp(inputValue, -1f, 1f) * Mathf.Max(0f, speedDegrees) * Mathf.Max(0f, deltaTime),
                360f);
        }

        public static float CalculatePitch(
            float currentPitch,
            float inputValue,
            float speedDegrees,
            float deltaTime,
            float minimumPitch,
            float maximumPitch)
        {
            var lower = Mathf.Min(minimumPitch, maximumPitch);
            var upper = Mathf.Max(minimumPitch, maximumPitch);
            return Mathf.Clamp(
                currentPitch - Mathf.Clamp(inputValue, -1f, 1f) * Mathf.Max(0f, speedDegrees) * Mathf.Max(0f, deltaTime),
                lower,
                upper);
        }

        public static Vector3 CalculateLookAheadOffset(
            Vector3 currentOffset,
            Vector2 stickInput,
            Vector3 cameraForward,
            Vector3 cameraRight,
            bool lookAheadHeld,
            float maximumDistance,
            float outwardSpeed,
            float returnSpeed,
            float deltaTime)
        {
            maximumDistance = Mathf.Max(0f, maximumDistance);
            deltaTime = Mathf.Max(0f, deltaTime);
            currentOffset = Vector3.ClampMagnitude(
                Vector3.ProjectOnPlane(currentOffset, Vector3.up),
                maximumDistance);
            var stick = Vector2.ClampMagnitude(stickInput, 1f);
            if (!lookAheadHeld || stick.sqrMagnitude <= 0.0001f)
            {
                return Vector3.MoveTowards(
                    currentOffset,
                    Vector3.zero,
                    Mathf.Max(0f, returnSpeed) * deltaTime);
            }

            var forward = Vector3.ProjectOnPlane(cameraForward, Vector3.up);
            var right = Vector3.ProjectOnPlane(cameraRight, Vector3.up);
            forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
            right = right.sqrMagnitude > 0.0001f ? right.normalized : Vector3.right;
            var panDirection = (forward * stick.y) + (right * stick.x);
            if (panDirection.sqrMagnitude <= 0.0001f)
            {
                return currentOffset;
            }

            var displacement = panDirection.normalized
                * (Mathf.Max(0f, outwardSpeed) * stick.magnitude * deltaTime);
            return Vector3.ClampMagnitude(currentOffset + displacement, maximumDistance);
        }

        private void ApplyCameraInput()
        {
            ResolveInput();
            var lookAheadHeld = input != null && input.CameraLookAheadHeld;
            var look = input != null ? input.CameraLookValue : Vector2.zero;
            var mouseDelta = input != null ? input.MouseLookDelta : Vector2.zero;
            if (!lookAheadHeld)
            {
                ApplyOrbitInput(look);
            }

            yawDegrees = Mathf.Repeat(yawDegrees + mouseDelta.x * mouseSensitivityDegreesPerPixel, 360f);
            pitchDegrees = Mathf.Clamp(
                pitchDegrees - mouseDelta.y * mouseSensitivityDegreesPerPixel,
                minimumPitchDegrees,
                maximumPitchDegrees);

            var yawRotation = Quaternion.Euler(0f, yawDegrees, 0f);
            lookAheadOffset = CalculateLookAheadOffset(
                lookAheadOffset,
                look,
                yawRotation * Vector3.forward,
                yawRotation * Vector3.right,
                lookAheadHeld,
                maximumLookAheadDistance,
                lookAheadSpeed,
                lookAheadReturnSpeed,
                Time.deltaTime);
        }

        private void ApplyOrbitInput(Vector2 look)
        {
            yawDegrees = CalculateYaw(yawDegrees, look.x, yawSpeedDegrees, Time.deltaTime);
            pitchDegrees = CalculatePitch(
                pitchDegrees,
                look.y,
                pitchSpeedDegrees,
                Time.deltaTime,
                minimumPitchDegrees,
                maximumPitchDegrees);
        }

        private void ResolveInput()
        {
            if (input == null)
            {
                input = FindAnyObjectByType<TopDown3DInputRouter>();
            }
        }

        internal bool TryResolveDistance(Vector3 origin, Vector3 backward, out float resolvedDistance)
            => TryResolveDistance(origin, backward, distance, obstructionRadius, out resolvedDistance);

        internal bool TryResolveDistance(Vector3 origin, Vector3 backward, float boomDistance, float radius, out float resolvedDistance)
        {
            resolvedDistance = 0f;
            var startDistance = 0f;
            if (HasBlockingOverlap(origin, radius))
            {
                // Sphere casts do not reliably report blockers containing their origin.
                // Search only the authored boom, with a fixed upper bound on recovery work.
                var foundClear = false;
                var previousProbe = 0f;
                for (var probe = 1; probe <= OriginRecoveryProbeCount; probe++)
                {
                    var candidate = boomDistance * probe / OriginRecoveryProbeCount;
                    if (HasBlockingOverlap(origin + backward * candidate, radius))
                    {
                        previousProbe = candidate;
                        continue;
                    }

                    var blocked = previousProbe;
                    var clear = candidate;
                    for (var refinement = 0; refinement < OriginRecoveryRefinementCount; refinement++)
                    {
                        var midpoint = (blocked + clear) * 0.5f;
                        if (HasBlockingOverlap(origin + backward * midpoint, radius)) blocked = midpoint;
                        else clear = midpoint;
                    }

                    startDistance = clear;
                    foundClear = true;
                    break;
                }

                if (!foundClear) return false;
            }

            var hitCount = Physics.SphereCastNonAlloc(
                origin + backward * startDistance,
                radius,
                backward,
                obstructionHits,
                boomDistance - startDistance,
                obstructionMask,
                QueryTriggerInteraction.Ignore);

            // NonAlloc results are unordered; a full buffer can omit the closest blocker.
            var hits = hitCount == obstructionHits.Length
                ? Physics.SphereCastAll(origin + backward * startDistance, radius,
                    backward, boomDistance - startDistance, obstructionMask, QueryTriggerInteraction.Ignore)
                : obstructionHits;
            if (hits != obstructionHits) hitCount = hits.Length;
            var nearest = boomDistance;
            for (var i = 0; i < hitCount; i++)
            {
                var hit = hits[i];
                if (!IsBlockingCollider(hit.collider))
                {
                    continue;
                }

                nearest = Mathf.Min(nearest, startDistance + hit.distance - radius);
            }

            // Clearance takes precedence over the preferred minimum framing distance.
            resolvedDistance = Mathf.Clamp(nearest, startDistance, boomDistance);
            return !HasBlockingOverlap(origin + backward * resolvedDistance, radius);
        }

        private bool IsBlockingCollider(Collider collider)
        {
            return collider != null && (target == null || !collider.transform.IsChildOf(target))
                && (ridingMount == null || !collider.transform.IsChildOf(ridingMount));
        }

        internal bool HasBlockingOverlap(Vector3 position) => HasBlockingOverlap(position, obstructionRadius);

        internal bool HasBlockingOverlap(Vector3 position, float radius)
        {
            var count = Physics.OverlapSphereNonAlloc(position, radius,
                obstructionOverlaps, obstructionMask, QueryTriggerInteraction.Ignore);
            var overlaps = count == obstructionOverlaps.Length
                ? Physics.OverlapSphere(position, radius, obstructionMask, QueryTriggerInteraction.Ignore)
                : obstructionOverlaps;
            if (overlaps != obstructionOverlaps) count = overlaps.Length;
            for (var i = 0; i < count; i++)
                if (IsBlockingCollider(overlaps[i])) return true;
            return false;
        }

        private void ApplyResolvedPose(Vector3 origin, Vector3 backward, Quaternion rotation)
        {
            HasUnresolvedClearance = !TryResolveDistance(origin, backward, out var resolvedDistance);
            if (HasUnresolvedClearance)
            {
                // An enclosed boom needs a different framing decision. Never accept an unsafe
                // endpoint; reuse a previously verified position only while it is still clear.
                if (hasVerifiedClearPosition && !HasBlockingOverlap(verifiedClearPosition))
                    transform.SetPositionAndRotation(verifiedClearPosition, rotation);
                return;
            }

            verifiedClearPosition = origin + backward * resolvedDistance;
            hasVerifiedClearPosition = true;
            transform.SetPositionAndRotation(verifiedClearPosition, rotation);
        }

        internal void SnapToTarget()
        {
            if (target == null)
            {
                return;
            }

            smoothedTarget = target.position + targetOffset;
            targetVelocity = Vector3.zero;
            lookAheadOffset = Vector3.zero;
            initialized = true;
            if (ExternalViewOwnsOutput) { cameraOptions.InvalidateFollow(); return; }
            var rotation = Quaternion.Euler(pitchDegrees, yawDegrees, 0f);
            ApplyResolvedPose(smoothedTarget, -(rotation * Vector3.forward), rotation);
        }

        private void ApplyLens()
        {
            if (ExternalViewOwnsOutput) return;
            if (outputCamera == null)
            {
                outputCamera = GetComponent<Camera>();
            }

            EnsurePostProcessing();
            outputCamera.orthographic = false;
            outputCamera.fieldOfView = fieldOfView;
            outputCamera.nearClipPlane = 0.1f;
            // Authored terrain has one range owner. The generated reference retains its original lens.
            if (authoredCameraRange == null) authoredCameraRange = GetComponent<BadwaterCameraRange>();
            if (authoredCameraRange != null && authoredCameraRange.isActiveAndEnabled)
                authoredCameraRange.Apply();
            else if (authoredCameraRange == null)
                outputCamera.farClipPlane = 1300f;
            if (!initialized && target != null)
            {
                SnapToTarget();
            }
        }

        internal bool TryGetElevatedPose(out Vector3 position, out Quaternion rotation)
        {
            rotation = Quaternion.Euler(pitchDegrees, yawDegrees, 0);
            var origin = ridingFrame != null ? ridingFrame.position + targetOffset : target.position + targetOffset;
            var backward = -(rotation * Vector3.forward);
            var clear = TryResolveDistance(origin, backward, out var resolved);
            position = origin + backward * resolved;
            return clear;
        }

        internal void RememberVerifiedPose(Vector3 position)
        {
            verifiedClearPosition = position; hasVerifiedClearPosition = true; HasUnresolvedClearance = false;
        }

        internal void RestoreElevatedPose(Vector3 position, Quaternion rotation)
        {
            smoothedTarget = (ridingFrame != null ? ridingFrame.position : target.position) + targetOffset;
            targetVelocity = Vector3.zero; lookAheadOffset = Vector3.zero; initialized = true;
            verifiedClearPosition = position; hasVerifiedClearPosition = true; HasUnresolvedClearance = false;
            transform.SetPositionAndRotation(position, rotation);
            ApplyLens();
        }

        private void EnsurePostProcessing()
        {
            if (!Application.isPlaying || outputCamera == null)
            {
                return;
            }

            var additionalCameraData = outputCamera.GetComponent<UniversalAdditionalCameraData>();
            if (additionalCameraData == null)
            {
                additionalCameraData = outputCamera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }

            additionalCameraData.renderPostProcessing = true;
        }

        private void OnValidate()
        {
            distance = Mathf.Max(2f, distance);
            minimumPitchDegrees = Mathf.Clamp(minimumPitchDegrees, 20f, 75f);
            maximumPitchDegrees = Mathf.Clamp(maximumPitchDegrees, minimumPitchDegrees, 75f);
            pitchDegrees = Mathf.Clamp(pitchDegrees, minimumPitchDegrees, maximumPitchDegrees);
            yawSpeedDegrees = Mathf.Clamp(yawSpeedDegrees, 30f, 240f);
            pitchSpeedDegrees = Mathf.Clamp(pitchSpeedDegrees, 20f, 180f);
            mouseSensitivityDegreesPerPixel = Mathf.Max(0f, mouseSensitivityDegreesPerPixel);
            minimumDistance = Mathf.Clamp(minimumDistance, 1f, distance);
            followSmoothTime = Mathf.Max(0f, followSmoothTime);
            maximumLookAheadDistance = Mathf.Max(0f, maximumLookAheadDistance);
            lookAheadSpeed = Mathf.Max(0f, lookAheadSpeed);
            lookAheadReturnSpeed = Mathf.Max(0f, lookAheadReturnSpeed);
            obstructionRadius = Mathf.Max(0.05f, obstructionRadius);
            ApplyLens();
        }
    }
}
