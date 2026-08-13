using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class TopDown3DCameraRig : MonoBehaviour
    {
        private const int ObstructionHitCapacity = 16;

        [SerializeField] private Transform target;
        [SerializeField] private TopDown3DInputRouter input;
        [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.15f, 0f);
        [SerializeField, Range(20f, 75f)] private float pitchDegrees = 50f;
        [SerializeField] private float yawDegrees = 40f;
        [SerializeField, Range(20f, 75f)] private float minimumPitchDegrees = 38f;
        [SerializeField, Range(20f, 75f)] private float maximumPitchDegrees = 65f;
        [SerializeField, Range(30f, 240f)] private float yawSpeedDegrees = 120f;
        [SerializeField, Range(20f, 180f)] private float pitchSpeedDegrees = 70f;
        [SerializeField, Min(2f)] private float distance = 18f;
        [SerializeField, Range(20f, 80f)] private float fieldOfView = 48f;
        [SerializeField, Min(0f)] private float followSmoothTime = 0.14f;
        [SerializeField, Min(0.05f)] private float obstructionRadius = 0.35f;
        [SerializeField, Min(1f)] private float minimumDistance = 4f;
        [SerializeField] private LayerMask obstructionMask = ~0;

        private readonly RaycastHit[] obstructionHits = new RaycastHit[ObstructionHitCapacity];
        private Camera outputCamera;
        private Vector3 smoothedTarget;
        private Vector3 targetVelocity;
        private bool initialized;

        public float PitchDegrees => pitchDegrees;
        public float YawDegrees => yawDegrees;
        public float Distance => distance;

        public void Configure(Transform followTarget, TopDown3DInputRouter inputRouter = null)
        {
            target = followTarget;
            input = inputRouter;
            SnapToTarget();
        }

        private void Awake()
        {
            outputCamera = GetComponent<Camera>();
            ResolveInput();
            ApplyLens();
        }

        private void OnEnable()
        {
            SnapToTarget();
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            ApplyLens();
            ApplyOrbitInput();
            var rawTarget = target.position + targetOffset;
            smoothedTarget = followSmoothTime <= 0f
                ? rawTarget
                : Vector3.SmoothDamp(smoothedTarget, rawTarget, ref targetVelocity, followSmoothTime);

            var rotation = Quaternion.Euler(pitchDegrees, yawDegrees, 0f);
            var backward = -(rotation * Vector3.forward);
            var resolvedDistance = ResolveDistance(smoothedTarget, backward);
            transform.SetPositionAndRotation(smoothedTarget + backward * resolvedDistance, rotation);
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

        private void ApplyOrbitInput()
        {
            ResolveInput();
            if (input == null)
            {
                return;
            }

            var look = input.CameraLookValue;
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
                input = FindFirstObjectByType<TopDown3DInputRouter>();
            }
        }

        private float ResolveDistance(Vector3 origin, Vector3 backward)
        {
            var hitCount = Physics.SphereCastNonAlloc(
                origin,
                obstructionRadius,
                backward,
                obstructionHits,
                distance,
                obstructionMask,
                QueryTriggerInteraction.Ignore);

            var nearest = distance;
            for (var i = 0; i < hitCount; i++)
            {
                var hit = obstructionHits[i];
                if (hit.collider == null || (target != null && hit.collider.transform.IsChildOf(target)))
                {
                    continue;
                }

                nearest = Mathf.Min(nearest, hit.distance - obstructionRadius);
            }

            return Mathf.Clamp(nearest, minimumDistance, distance);
        }

        private void SnapToTarget()
        {
            if (target == null)
            {
                return;
            }

            smoothedTarget = target.position + targetOffset;
            targetVelocity = Vector3.zero;
            initialized = true;
            var rotation = Quaternion.Euler(pitchDegrees, yawDegrees, 0f);
            transform.SetPositionAndRotation(smoothedTarget - (rotation * Vector3.forward * distance), rotation);
        }

        private void ApplyLens()
        {
            if (outputCamera == null)
            {
                outputCamera = GetComponent<Camera>();
            }

            outputCamera.orthographic = false;
            outputCamera.fieldOfView = fieldOfView;
            outputCamera.nearClipPlane = 0.1f;
            outputCamera.farClipPlane = 300f;
            if (!initialized && target != null)
            {
                SnapToTarget();
            }
        }

        private void OnValidate()
        {
            distance = Mathf.Max(2f, distance);
            minimumPitchDegrees = Mathf.Clamp(minimumPitchDegrees, 20f, 75f);
            maximumPitchDegrees = Mathf.Clamp(maximumPitchDegrees, minimumPitchDegrees, 75f);
            pitchDegrees = Mathf.Clamp(pitchDegrees, minimumPitchDegrees, maximumPitchDegrees);
            yawSpeedDegrees = Mathf.Clamp(yawSpeedDegrees, 30f, 240f);
            pitchSpeedDegrees = Mathf.Clamp(pitchSpeedDegrees, 20f, 180f);
            minimumDistance = Mathf.Clamp(minimumDistance, 1f, distance);
            followSmoothTime = Mathf.Max(0f, followSmoothTime);
            obstructionRadius = Mathf.Max(0.05f, obstructionRadius);
            ApplyLens();
        }
    }
}
