using System;
using System.Collections.Generic;
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

        private Collider[] hits = new Collider[HitCapacity];
        private readonly List<MonoBehaviour> targetComponents = new();
        private readonly HashSet<UnityEngine.Object> seenTargets = new();

        public ITopDown3DInteractionTarget SelectedTarget { get; private set; }
        public ITopDown3DInteractable CurrentTarget => SelectedTarget as ITopDown3DInteractable;
        public TopDown3DInteractionTarget CaptureTarget() => new(SelectedTarget);
        public event Action<ITopDown3DInteractable> TargetChanged;
        public event Action<ITopDown3DInteractionTarget> SelectionChanged;

        public void Configure(TopDown3DPlayerMotor playerMotor)
        {
            motor = playerMotor;
        }

        public void RefreshTarget()
        {
            var next = FindBestTarget(false, true);
            if (ReferenceEquals(next, SelectedTarget))
            {
                return;
            }

            SelectedTarget = next;
            TargetChanged?.Invoke(CurrentTarget);
            SelectionChanged?.Invoke(SelectedTarget);
        }

        // Dedicated pickup commands must not compete with ironstone, camp, or cache prompts.
        public TopDown3DHarvesterView FindCanisterTarget()
        {
            return FindBestTarget(true, false) as TopDown3DHarvesterView;
        }

        public bool CanReachTarget(ITopDown3DInteractionTarget target)
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

        private ITopDown3DInteractionTarget FindBestTarget(bool canisterOnly, bool retainCurrent)
        {
            if (motor == null)
            {
                return null;
            }

            var actorPosition = motor.Position;
            var actorFacing = Vector3.ProjectOnPlane(motor.FacingDirection, Vector3.up).normalized;
            int count;
            // Keep a complete result set while reusing capacity after a dense encounter.
            // Never select from a truncated NonAlloc result or cache target eligibility.
            while ((count = Physics.OverlapSphereNonAlloc(actorPosition, scanRadius, hits,
                interactionMask, QueryTriggerInteraction.Collide)) == hits.Length)
                Array.Resize(ref hits, checked(hits.Length * 2));
            var candidates = hits;
            ITopDown3DInteractionTarget best = null;
            seenTargets.Clear();
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
                ITopDown3DInteractionTarget candidate = null;
                if(canisterOnly) candidate=collider.GetComponentInParent<TopDown3DHarvesterView>();
                else
                {
                    targetComponents.Clear(); collider.GetComponentsInParent(false,targetComponents);
                    foreach(var component in targetComponents)
                        if(component is ITopDown3DInteractionTarget target && TopDown3DInteractionTarget.OwnsCollider(target,collider))
                        { candidate=target;break; }
                }
                if (candidate == null || candidate.UnityObject == null || !candidate.IsAvailable
                    || (candidate.UnityObject is Behaviour behaviour && !behaviour.isActiveAndEnabled))
                {
                    continue;
                }

                if(!seenTargets.Add(candidate.UnityObject)) continue;
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

                if (retainCurrent && ReferenceEquals(candidate, SelectedTarget))
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
            if (currentIsValid && !ReferenceEquals(best, SelectedTarget)
                && bestDistance > currentDistance - targetSwitchDistance
                && bestFacing < currentFacing + targetSwitchFacing)
            {
                return SelectedTarget;
            }

            return best;
        }

        private bool HasLineOfSight(
            ITopDown3DInteractionTarget target,
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

            return TopDown3DInteractionTarget.OwnsCollider(target,hit.collider);
        }

        private void OnDisable()
        {
            if (SelectedTarget != null)
            {
                SelectedTarget = null;
                TargetChanged?.Invoke(null);
                SelectionChanged?.Invoke(null);
            }
        }
    }
}
