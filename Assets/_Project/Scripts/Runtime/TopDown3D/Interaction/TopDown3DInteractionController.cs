using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    public sealed class TopDown3DInteractionController : MonoBehaviour
    {
        private const int HitCapacity = 24;

        [SerializeField] private TopDown3DPlayerMotor motor;
        [SerializeField, Min(0.5f)] private float scanRadius = 4f;
        [SerializeField, Range(-1f, 1f)] private float minimumFacingDot = -0.25f;
        [SerializeField] private LayerMask interactionMask = ~0;
        [SerializeField] private LayerMask lineOfSightMask = ~0;

        private readonly Collider[] hits = new Collider[HitCapacity];

        public ITopDown3DInteractable CurrentTarget { get; private set; }
        public event Action<ITopDown3DInteractable> TargetChanged;

        public void Configure(TopDown3DPlayerMotor playerMotor)
        {
            motor = playerMotor;
        }

        public void RefreshTarget()
        {
            var next = FindBestTarget();
            if (ReferenceEquals(next, CurrentTarget))
            {
                return;
            }

            CurrentTarget = next;
            TargetChanged?.Invoke(CurrentTarget);
        }

        private void Update()
        {
            RefreshTarget();
        }

        private ITopDown3DInteractable FindBestTarget()
        {
            if (motor == null)
            {
                return null;
            }

            var actorPosition = motor.Position;
            var actorFacing = Vector3.ProjectOnPlane(motor.FacingDirection, Vector3.up).normalized;
            var count = Physics.OverlapSphereNonAlloc(
                actorPosition,
                scanRadius,
                hits,
                interactionMask,
                QueryTriggerInteraction.Collide);
            ITopDown3DInteractable best = null;
            var bestDistance = float.PositiveInfinity;
            var bestFacing = float.NegativeInfinity;
            for (var i = 0; i < count; i++)
            {
                var collider = hits[i];
                var candidate = collider != null
                    ? collider.GetComponentInParent<TopDown3DBigArmCargoAccess>() as ITopDown3DInteractable
                    : null;
                candidate ??= collider != null
                    ? collider.GetComponentInParent<TopDown3DIronstoneNode>()
                    : null;
                if (candidate == null || !candidate.IsAvailable)
                {
                    continue;
                }

                var delta = candidate.InteractionPoint - actorPosition;
                var distance = delta.magnitude;
                var planarDirection = Vector3.ProjectOnPlane(delta, Vector3.up);
                var facing = planarDirection.sqrMagnitude > 0.0001f
                    ? Vector3.Dot(actorFacing, planarDirection.normalized)
                    : 1f;
                if (distance > candidate.InteractionRange || facing < minimumFacingDot
                    || !HasLineOfSight(candidate, collider, actorPosition))
                {
                    continue;
                }

                if (best == null
                    || distance < bestDistance - 0.001f
                    || (Mathf.Abs(distance - bestDistance) <= 0.001f
                        && (facing > bestFacing + 0.001f
                            || (Mathf.Abs(facing - bestFacing) <= 0.001f
                                && string.CompareOrdinal(candidate.StableId, best.StableId) < 0))))
                {
                    best = candidate;
                    bestDistance = distance;
                    bestFacing = facing;
                }
            }

            return best;
        }

        private bool HasLineOfSight(
            ITopDown3DInteractable target,
            Collider targetCollider,
            Vector3 actorPosition)
        {
            var origin = actorPosition + Vector3.up * 0.8f;
            var delta = target.InteractionPoint - origin;
            var distance = delta.magnitude;
            if (distance <= 0.001f)
            {
                return true;
            }

            if (!Physics.Raycast(
                    origin,
                    delta / distance,
                    out var hit,
                    distance + 0.05f,
                    lineOfSightMask,
                    QueryTriggerInteraction.Collide))
            {
                return true;
            }

            return hit.collider == targetCollider
                || hit.collider.GetComponentInParent<MonoBehaviour>() == target.UnityObject
                || hit.collider.GetComponentInParent<TopDown3DIronstoneNode>() == target.UnityObject;
        }

        private void OnDisable()
        {
            if (CurrentTarget != null)
            {
                CurrentTarget = null;
                TargetChanged?.Invoke(null);
            }
        }
    }
}
