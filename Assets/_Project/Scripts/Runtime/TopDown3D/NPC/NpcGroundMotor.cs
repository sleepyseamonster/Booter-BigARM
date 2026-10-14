using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent, RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public sealed class NpcGroundMotor : MonoBehaviour
    {
        public const float Skin = .03f;
        private readonly RaycastHit[] hits = new RaycastHit[32];
        private readonly Collider[] overlaps = new Collider[64];
        private Rigidbody body;
        private CapsuleCollider shape;
        private NpcTuning supportTuning = new();
        private int probeLimit = int.MaxValue;
        public float Speed { get; private set; }
        public int Probes { get; private set; }
        public Vector3 Position => body != null ? body.position : transform.position;
        public float Radius => Shape.radius;
        private CapsuleCollider Shape => shape != null ? shape : (shape = GetComponent<CapsuleCollider>());
        private void Awake() { body = GetComponent<Rigidbody>(); shape = GetComponent<CapsuleCollider>(); }
        public void BeginProbePass() { Probes = 0; probeLimit = NpcLocalGroundNavigation.MaxProbeCalls; }
        public void EndProbePass() => probeLimit = int.MaxValue;
        private bool ClaimProbe() { if (Probes >= probeLimit) return false; Probes++; return true; }
        public bool ValidateShape(out string reason)
        {
            reason = "NPC requires an unscaled upright capsule and one kinematic body.";
            var rb = GetComponent<Rigidbody>(); var c = Shape;
            return rb != null && rb.isKinematic && c != null && c.enabled && !c.isTrigger && c.direction == 1
                && c.radius >= .1f && c.radius <= 1 && c.height >= c.radius * 2 && c.height <= 3
                && Vector3.Distance(c.center, Vector3.up * (c.height * .5f)) < .001f
                && Vector3.Distance(transform.lossyScale, Vector3.one) < .001f
                && Vector3.Dot(transform.up, Vector3.up) > .999f;
        }
        private bool Relevant(Collider c) => c != null && c.gameObject.scene == gameObject.scene
            && !c.transform.IsChildOf(transform);
        private bool Ground(Collider c) => c is TerrainCollider || c.GetComponent<TopDown3DGroundSurface>() != null;
        private void Ends(Vector3 root, out Vector3 lower, out Vector3 upper)
        { lower = root + Vector3.up * Shape.radius; upper = root + Vector3.up * (Shape.height - Shape.radius); }
        public bool Clear(Vector3 root)
        {
            if (!NpcMath.Finite(root)) return false;
            Ends(root, out var a, out var b); if (!ClaimProbe()) return false;
            var count = Physics.OverlapCapsuleNonAlloc(a, b, Shape.radius - .005f, overlaps, ~0, QueryTriggerInteraction.Ignore);
            if (count == overlaps.Length) return false;
            for (var i = 0; i < count; i++) if (Relevant(overlaps[i])) return false;
            return true;
        }
        public bool TryGround(Vector3 probe, NpcTuning tuning, bool broad, out Vector3 root)
        {
            root = probe;
            supportTuning = tuning;
            if (!NpcMath.Finite(probe)) return false;
            var highest = float.NegativeInfinity; var lowest = float.PositiveInfinity;
            for (var sample = 0; sample < 9; sample++)
            {
                var offset = sample == 0 ? Vector3.zero : Quaternion.Euler(0, (sample - 1) * 45, 0) * Vector3.forward * (Shape.radius * .9f);
                var up = broad ? 8 : tuning.stepUp + .1f;
                var down = broad ? 16 : tuning.stepUp + tuning.stepDown + .2f;
                if (!ClaimProbe()) return false;
                var count = Physics.RaycastNonAlloc(probe + offset + Vector3.up * up, Vector3.down, hits, down, ~0, QueryTriggerInteraction.Ignore);
                if (count == hits.Length) return false;
                var best = float.PositiveInfinity; var height = float.NaN;
                for (var i = 0; i < count; i++)
                {
                    var hit = hits[i];
                    if (!Relevant(hit.collider) || !Ground(hit.collider) || Vector3.Angle(hit.normal, Vector3.up) > tuning.slopeDegrees) continue;
                    var delta = Mathf.Abs(hit.point.y + Skin - probe.y);
                    if (delta < best) { best = delta; height = hit.point.y; }
                }
                if (!float.IsFinite(height)) return false;
                highest = Mathf.Max(highest, height); lowest = Mathf.Min(lowest, height);
            }
            if (highest - lowest > Shape.radius * 1.6f * Mathf.Tan(tuning.slopeDegrees * Mathf.Deg2Rad) + .02f) return false;
            root = new Vector3(probe.x, highest + Skin, probe.z);
            if (!broad && (root.y - probe.y > tuning.stepUp || probe.y - root.y > tuning.stepDown)) return false;
            return Clear(root);
        }
        public bool SegmentClear(Vector3 from, Vector3 to)
        {
            var delta = to - from; var length = delta.magnitude;
            if (length < .0001f) return Clear(to);
            // Conservative support corridor sampled at at most 0.15m, in addition to the swept capsule.
            var steps = Mathf.CeilToInt(length / .15f);
            if (steps > 128) return false;
            for (var i = 0; i <= steps; i++)
            {
                var point = Vector3.Lerp(from, to, (float)i / steps);
                if (!TryGround(point, supportTuning, false, out var ground) || Mathf.Abs(ground.y - point.y) > .12f) return false;
            }
            Ends(from, out var a, out var b); if (!ClaimProbe()) return false;
            var count = Physics.CapsuleCastNonAlloc(a, b, Shape.radius - .005f, delta / length, hits, length, ~0, QueryTriggerInteraction.Ignore);
            if (count == hits.Length) return false;
            for (var i = 0; i < count; i++) if (Relevant(hits[i].collider)) return false;
            return Clear(to);
        }
        public bool Step(Vector3 waypoint, float desiredSpeed, NpcTuning tuning, float dt, out string reason, NpcReservation[] reservations = null)
        {
            reason = null;
            if (body == null) Awake();
            var direction = Vector3.ProjectOnPlane(waypoint - Position, Vector3.up);
            Speed = Mathf.MoveTowards(Speed, desiredSpeed, (desiredSpeed > Speed ? tuning.acceleration : tuning.braking) * dt);
            var distance = Mathf.Min(direction.magnitude, Speed * dt);
            var candidate = Position + direction.normalized * distance;
            if (!TryGround(candidate, tuning, false, out var grounded)
                || !NpcMath.ReservationsClear(Position, grounded, Radius, reservations) || !SegmentClear(Position, grounded))
            { Stop(); reason = "Support or body sweep vetoed movement."; return false; }
            body.MovePosition(grounded);
            if (direction.sqrMagnitude > .0001f)
                body.MoveRotation(Quaternion.RotateTowards(body.rotation, Quaternion.LookRotation(direction), tuning.turnDegrees * dt));
            return true;
        }
        public void Stop() { Speed = 0; }
        public void FaceKnownPosition(Vector3 point, NpcTuning tuning, float dt)
        {
            if (body == null) Awake();
            var direction = Vector3.ProjectOnPlane(point - Position, Vector3.up);
            if (NpcMath.Finite(direction) && direction.sqrMagnitude > .001f)
                body.MoveRotation(Quaternion.RotateTowards(body.rotation, Quaternion.LookRotation(direction), tuning.turnDegrees * dt));
        }
        public bool Restore(Vector3 anchor, Quaternion rotation, NpcTuning tuning, out string reason)
        {
            Stop(); Physics.SyncTransforms(); reason = "NPC fixture anchor has unavailable support or clearance.";
            if (!TryGround(anchor, tuning, false, out var ground) || Vector3.Distance(ground, anchor) > .12f) return false;
            if (body == null) Awake();
            body.position = ground; body.rotation = rotation; Physics.SyncTransforms(); reason = null; return true;
        }
    }
}
