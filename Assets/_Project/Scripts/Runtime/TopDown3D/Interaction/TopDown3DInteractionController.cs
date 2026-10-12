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
        [SerializeField, Min(0f)] private float targetSwitchDistance = 0.2f;
        [SerializeField, Range(0f, 2f)] private float targetSwitchFacing = 0.35f;
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
            var next = FindBestTarget(false, true);
            if (ReferenceEquals(next, CurrentTarget))
            {
                return;
            }

            CurrentTarget = next;
            TargetChanged?.Invoke(CurrentTarget);
        }

        // Dedicated pickup commands must not compete with ironstone, camp, or cache prompts.
        public TopDown3DHarvesterView FindCanisterTarget()
        {
            return FindBestTarget(true, false) as TopDown3DHarvesterView;
        }

        public bool CanReachTarget(ITopDown3DInteractable target)
        {
            if (motor == null || target == null || target.UnityObject == null || !target.IsAvailable
                || (target.UnityObject is Behaviour behaviour && !behaviour.isActiveAndEnabled)
                || Vector3.Distance(motor.Position, target.InteractionPoint) > target.InteractionRange)
                return false;
            var component = target.UnityObject as Component;
            return component != null && HasLineOfSight(target,
                component.GetComponentInChildren<Collider>(), motor.Position);
        }

        private void Update()
        {
            RefreshTarget();
        }

        private ITopDown3DInteractable FindBestTarget(bool canisterOnly, bool retainCurrent)
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
            // A saturated NonAlloc buffer is incomplete and cannot select a deterministic nearest target.
            var candidates = count == hits.Length
                ? Physics.OverlapSphere(actorPosition, scanRadius, interactionMask, QueryTriggerInteraction.Collide)
                : hits;
            if (candidates != hits) count = candidates.Length;
            ITopDown3DInteractable best = null;
            var bestDistance = float.PositiveInfinity;
            var bestFacing = float.NegativeInfinity;
            var currentDistance = float.PositiveInfinity;
            var currentFacing = float.NegativeInfinity;
            var currentIsValid = false;
            for (var i = 0; i < count; i++)
            {
                var collider = candidates[i];
                // Cargo has its own hold command and must never steal a world interaction.
                if (collider == null) continue;
                ITopDown3DInteractable candidate = canisterOnly
                    ? collider.GetComponentInParent<TopDown3DHarvesterView>()
                    : collider.GetComponentInParent<TopDown3DIronstoneNode>();
                candidate ??= collider.GetComponentInParent<TopDown3DHarvesterView>();
                if (!canisterOnly)
                {
                    candidate ??= collider.GetComponentInParent<TopDown3DCampView>();
                    candidate ??= collider.GetComponentInParent<TopDown3DResourceCache>();
                }
                if (candidate == null || candidate.UnityObject == null || !candidate.IsAvailable
                    || (candidate.UnityObject is Behaviour behaviour && !behaviour.isActiveAndEnabled))
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

                if (retainCurrent && ReferenceEquals(candidate, CurrentTarget))
                {
                    currentIsValid = true;
                    currentDistance = distance;
                    currentFacing = facing;
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

            // Revalidate through this scan before retaining: unavailable, blocked, behind,
            // out-of-range, and unloaded targets must drop immediately.
            if (currentIsValid && !ReferenceEquals(best, CurrentTarget)
                && bestDistance > currentDistance - targetSwitchDistance
                && bestFacing < currentFacing + targetSwitchFacing)
            {
                return CurrentTarget;
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
                || hit.collider.GetComponentInParent<TopDown3DIronstoneNode>() == target.UnityObject
                || hit.collider.GetComponentInParent<TopDown3DHarvesterView>() == target.UnityObject
                || hit.collider.GetComponentInParent<TopDown3DCampView>() == target.UnityObject
                || hit.collider.GetComponentInParent<TopDown3DResourceCache>() == target.UnityObject;
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
