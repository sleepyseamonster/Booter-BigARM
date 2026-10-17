using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DPlayerMotor
    {
        private RaycastHit[] groundProbeHits = new RaycastHit[8];
        private RaycastHit[] trueSurfaceHits = new RaycastHit[8];
        private RaycastHit[] surfaceStepHits = new RaycastHit[16];
        private Collider[] surfaceStepOverlaps = new Collider[16];
        private const float LaboratoryMaximumStepHeight = .28f;
        private const float SurfaceStepSkin = .01f;

        // Physical contact is independent of static animation qualification.
        // Existing movable Camp Loop props can support a landing/re-jump while
        // the completed-distance/foot adaptation retains its static-only gate.
        private bool CanUsePhysicalGroundCollider(Collider support)
        {
            if (support == null || !support.enabled || !support.gameObject.activeInHierarchy || support.isTrigger
                || support.transform.IsChildOf(transform)) return false;
            if (support.GetComponentInParent<TopDown3DGroundSurface>() != null) return true;
            if (animationPostureOwner == null || support.attachedRigidbody == null || support.attachedRigidbody == body
                || support.gameObject.scene != gameObject.scene) return false;
            var session = TopDown3DAnimationPlaygroundSession.Find(gameObject.scene);
            return session != null && session.isActiveAndEnabled && session.IsLab;
        }

        // A bounded position correction inside the existing motor, never a new
        // movement owner or a change to the requested planar gait/facing.
        private bool TryApplyLaboratoryGroundStep(Vector3 planarVelocity)
        {
            if (!laboratoryUphillGaitEnabled || animationPostureOwner == null || !IsGrounded
                || laboratoryPostureSuspended || AnimationPosture != AnimationPlaygroundPosture.Standing
                || IsMounted || IsActionConstrained && !JumpUsesGroundedLocomotion
                || ActiveTraversal != TopDown3DTraversalMove.None || climbMode != TopDown3DClimbMode.None
                || laboratoryGroundSupport == null || laboratoryGroundSupport.attachedRigidbody != null
                || body == null || capsule == null || !Finite(planarVelocity)) return false;
            planarVelocity.y = 0;
            var speed = planarVelocity.magnitude;
            if (speed < .1f || !float.IsFinite(speed)) return false;
            var direction = planarVelocity / speed;
            var travel = speed * Time.fixedDeltaTime;
            var maximum = Mathf.Min(Mathf.Min(LaboratoryMaximumStepHeight, groundProbeDistance),
                capsule.height * Mathf.Abs(transform.lossyScale.y) * .25f);
            if (maximum <= SurfaceStepSkin * 3) return false;
            GetWorldCapsule(body.position, 1, out var top, out var bottom, out var radius);
            var foot = bottom - Vector3.up * radius;
            var ahead = foot + direction * (radius + travel + SurfaceStepSkin);
            var hasAhead = TryGetStepTop(ahead, foot.y, maximum, out var aheadHit);
            var aheadRise = hasAhead ? aheadHit.point.y - foot.y : float.PositiveInfinity;

            if (hasAhead && aheadRise > SurfaceStepSkin * 2 && aheadRise + SurfaceStepSkin <= maximum
                && StepCastBlocked(top, bottom, radius - SurfaceStepSkin, direction,
                    travel + SurfaceStepSkin * 2, onlySteep: true))
            {
                var rise = aheadRise + SurfaceStepSkin;
                var lifted = body.position + Vector3.up * rise;
                if (!StepCapsuleClear(lifted) || !StepCapsuleClear(lifted + direction * travel)
                    || StepCastBlocked(top, bottom, radius - SurfaceStepSkin, Vector3.up, rise)
                    || StepCastBlocked(top + Vector3.up * rise, bottom + Vector3.up * rise,
                        radius - SurfaceStepSkin, direction, travel + SurfaceStepSkin)) return false;
                body.position = lifted;
                return true;
            }

            // Do not immediately pull a completed step back to the trailing
            // floor while the leading foot is already level with its tread.
            if (hasAhead && Mathf.Abs(aheadRise) <= .025f) return false;
            if (body.linearVelocity.y > .1f) return false;
            if (!TryReadTrueSurfaceNormal(foot + Vector3.up * GroundNormalRayLift,
                    GroundNormalRayLift + maximum + SurfaceStepSkin, laboratoryGroundSupport,
                    out var normal, out var point) || !TopDown3DSlopeMath.IsWalkable(normal, maxWalkableSlope)) return false;
            var plane = new Plane(normal, point);
            // Account for the rounded capsule on a slope; the bottommost point
            // alone would snap a correctly supported capsule into the ramp.
            var gap = (plane.GetDistanceToPoint(bottom) - radius) / Mathf.Max(.01f, normal.y);
            if (gap <= .03f || gap > maximum) return false;
            var down = gap - SurfaceStepSkin;
            var lowered = body.position - Vector3.up * down;
            if (!StepCapsuleClear(lowered)
                || StepCastBlocked(top, bottom, radius - SurfaceStepSkin, Vector3.down, down)) return false;
            body.position = lowered;
            return true;
        }

        private bool TryGetStepTop(Vector3 ahead, float footY, float maximum, out RaycastHit support)
        {
            support = default; int count;
            while (true)
            {
                count = Physics.RaycastNonAlloc(new Vector3(ahead.x, footY + maximum + .02f, ahead.z),
                    Vector3.down, surfaceStepHits, maximum * 2 + .04f, groundMask, QueryTriggerInteraction.Ignore);
                if (count < surfaceStepHits.Length) break;
                if (surfaceStepHits.Length >= 4096) return false;
                Array.Resize(ref surfaceStepHits, surfaceStepHits.Length * 2);
            }
            var nearest = float.PositiveInfinity;
            for (var i = 0; i < count; i++)
            {
                var hit = surfaceStepHits[i];
                if (hit.collider == null || hit.collider.transform.IsChildOf(transform)
                    || hit.collider.attachedRigidbody != null || hit.distance >= nearest
                    || hit.collider.GetComponentInParent<TopDown3DGroundSurface>() == null
                    || !TopDown3DSlopeMath.IsWalkable(hit.normal, maxWalkableSlope)) continue;
                nearest = hit.distance; support = hit;
            }
            return support.collider != null;
        }

        private bool StepCastBlocked(Vector3 top, Vector3 bottom, float radius, Vector3 direction,
            float distance, bool onlySteep = false)
        {
            int count;
            while (true)
            {
                count = Physics.CapsuleCastNonAlloc(top, bottom, Mathf.Max(.01f, radius), direction,
                    surfaceStepHits, distance, ~0, QueryTriggerInteraction.Ignore);
                if (count < surfaceStepHits.Length) break;
                if (surfaceStepHits.Length >= 4096) return true;
                Array.Resize(ref surfaceStepHits, surfaceStepHits.Length * 2);
            }
            for (var i = 0; i < count; i++)
            {
                var hit = surfaceStepHits[i];
                if (hit.collider == null || hit.collider.transform.IsChildOf(transform)
                    || Vector3.Dot(hit.normal, direction) >= -.01f) continue;
                if (!onlySteep || !TopDown3DSlopeMath.IsWalkable(hit.normal, maxWalkableSlope)) return true;
            }
            return false;
        }

        private bool StepCapsuleClear(Vector3 position)
        {
            GetWorldCapsule(position, 1, out var top, out var bottom, out var radius);
            int count;
            while (true)
            {
                count = Physics.OverlapCapsuleNonAlloc(top, bottom, Mathf.Max(.01f, radius - SurfaceStepSkin),
                    surfaceStepOverlaps, ~0, QueryTriggerInteraction.Ignore);
                if (count < surfaceStepOverlaps.Length) break;
                if (surfaceStepOverlaps.Length >= 4096) return false;
                Array.Resize(ref surfaceStepOverlaps, surfaceStepOverlaps.Length * 2);
            }
            for (var i = 0; i < count; i++)
                if (surfaceStepOverlaps[i] != null && !surfaceStepOverlaps[i].transform.IsChildOf(transform)) return false;
            return true;
        }
    }
}
