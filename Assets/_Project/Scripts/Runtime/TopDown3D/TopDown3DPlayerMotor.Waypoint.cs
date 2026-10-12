using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DPlayerMotor
    {
        private bool waypointTravelActive;
        private Vector3[] waypointPath;
        private int waypointIndex;
        private float waypointSpeed;
        public event Action<bool> WaypointTravelFinished;

        public bool CanWalkWaypointSegment(Vector3 from, Vector3 to)
        {
            if (capsule == null || !float.IsFinite(from.x + from.y + from.z + to.x + to.y + to.z)) return false;
            var steps = Mathf.CeilToInt(Vector3.Distance(from, to) / .5f);
            if (steps > 200) return false;
            var previous = from;
            var occupants = new Collider[32];
            for (var i = 0; i <= steps; i++)
            {
                var p = Vector3.Lerp(from, to, steps == 0 ? 1 : (float)i / steps);
                if (!TopDown3DCampPlacement.TryGround(p, out var ground, out var normal)
                    || Vector3.Angle(normal, Vector3.up) > Mathf.Min(maxWalkableSlope, 30f)
                    || (i > 0 && Mathf.Abs(ground.y - previous.y) > .5f)) return false;
                var radius = capsule.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
                var height = capsule.height * transform.lossyScale.y;
                var bottom = ground + Vector3.up * (radius + .08f);
                var top = ground + Vector3.up * Mathf.Max(radius + .08f, height - radius);
                var count = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, occupants, groundMask, QueryTriggerInteraction.Ignore);
                if (count >= occupants.Length) return false;
                for (var j = 0; j < count; j++)
                {
                    var c = occupants[j];
                    if (c is TerrainCollider || c.GetComponentInParent<TopDown3DGroundSurface>() != null
                        || c.transform.IsChildOf(transform) || c.GetComponentInParent<TopDown3DBigArmFollower>() != null) continue;
                    return false;
                }
                previous = ground;
            }
            return true;
        }

        public bool TryBeginWaypointTravel(object owner, Vector3[] path, float speed)
        {
            if (!CanBeginCampAction || path == null || path.Length == 0 || !float.IsFinite(speed) || speed <= 0 || speed > 1.8f) return false;
            var prior = Position;
            foreach (var point in path) { if (!CanWalkWaypointSegment(prior, point)) return false; prior = point; }
            if (!TryBeginActionConstraint(owner)) return false;
            waypointPath = (Vector3[])path.Clone(); waypointIndex = 0; waypointSpeed = speed; waypointTravelActive = true;
            return true;
        }

        private void AdvanceWaypointTravel()
        {
            var delta = Vector3.ProjectOnPlane(waypointPath[waypointIndex] - Position, Vector3.up);
            if (delta.magnitude < .15f)
            {
                if (++waypointIndex >= waypointPath.Length) { FinishWaypointTravel(true); return; }
                delta = Vector3.ProjectOnPlane(waypointPath[waypointIndex] - Position, Vector3.up);
            }
            var step = Mathf.Min(delta.magnitude, waypointSpeed * Time.fixedDeltaTime);
            var direction = delta.normalized;
            if (!TryGetGroundNormal(out var normal) || Vector3.Angle(normal, Vector3.up) > Mathf.Min(maxWalkableSlope, 30f)
                || !TopDown3DCampPlacement.TryGround(Position + direction * (step + .2f), out var support, out var slope)
                || Vector3.Angle(slope, Vector3.up) > Mathf.Min(maxWalkableSlope, 30f)
                || Mathf.Abs(support.y - (Position.y + capsule.center.y - capsule.height * .5f)) > .55f
                || body.SweepTest(direction, out _, step + .04f, QueryTriggerInteraction.Ignore))
            { FinishWaypointTravel(false); return; }
            FacePlanarDirection(direction);
            body.linearVelocity = direction * (step / Time.fixedDeltaTime) + Vector3.up * body.linearVelocity.y;
            PublishPresentationSnapshot(direction * waypointSpeed, direction * waypointSpeed, false);
        }

        public void EndWaypointTravel(object owner)
        {
            if (!ReferenceEquals(actionConstraintOwner, owner)) return;
            if (body != null) body.linearVelocity = Vector3.up * body.linearVelocity.y;
            EndActionConstraint(owner);
        }

        private void FinishWaypointTravel(bool success)
        {
            waypointTravelActive = false;
            body.linearVelocity = Vector3.up * body.linearVelocity.y;
            WaypointTravelFinished?.Invoke(success);
        }
    }
}
