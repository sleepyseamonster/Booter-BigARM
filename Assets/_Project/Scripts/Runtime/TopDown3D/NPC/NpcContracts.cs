using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public enum NpcBehavior { Idle, Alert, Retreat, Flee, Recover }
    public enum NpcMovementStatus { Idle, Moving, Partial, Queued, Blocked, Unavailable }
    public readonly struct NpcEscapeCandidate
    {
        public readonly Vector3 Position;
        public readonly string Reason;
        public readonly float Score;
        public readonly bool Eligible;
        public NpcEscapeCandidate(Vector3 point, string reason, float score = 0, bool eligible = false)
        { Position = point; Reason = reason; Score = score; Eligible = eligible; }
    }
    [System.Serializable]
    public struct NpcReservation
    {
        public Vector3 center;
        public float radius;
        public NpcReservation(Vector3 point, float distance) { center = point; radius = distance; }
        public bool Contains(Vector3 point, float bodyRadius) => NpcMath.Distance(point, center) < radius + bodyRadius;
    }
    public readonly struct NpcThreatObservation
    {
        public readonly bool Valid;
        public readonly Vector3 Position;
        public readonly double ObservedAt;
        public NpcThreatObservation(Vector3 position, double time)
        { Valid = true; Position = position; ObservedAt = time; }
        public bool IsFresh(double now, float lifetime) => Valid && now >= ObservedAt && now - ObservedAt < lifetime;
    }
    public readonly struct NpcIntent
    {
        public readonly bool Move;
        public readonly Vector3 Threat;
        public readonly float Speed;
        public NpcIntent(Vector3 threat, float speed) { Move = true; Threat = threat; Speed = speed; }
    }
    public static class NpcMath
    {
        public static float Distance(Vector3 a, Vector3 b) => new Vector2(a.x - b.x, a.z - b.z).magnitude;
        public static float SegmentDistance(Vector3 point, Vector3 from, Vector3 to)
        {
            var a = new Vector2(from.x, from.z); var b = new Vector2(to.x, to.z); var p = new Vector2(point.x, point.z);
            var edge = b - a; var ratio = edge.sqrMagnitude < .000001f ? 0 : Mathf.Clamp01(Vector2.Dot(p - a, edge) / edge.sqrMagnitude);
            return Vector2.Distance(p, a + edge * ratio);
        }
        public static bool ReservationsClear(Vector3 from, Vector3 to, float bodyRadius, NpcReservation[] reservations)
        {
            if (reservations != null) foreach (var reservation in reservations)
                if (SegmentDistance(reservation.center, from, to) < reservation.radius + bodyRadius) return false;
            return true;
        }
        public static bool Finite(Vector3 p) => float.IsFinite(p.x) && float.IsFinite(p.y) && float.IsFinite(p.z);
        public static bool CanRearm(bool sessionReady, bool resetBlocked, string failure, uint epoch, uint restoredEpoch)
            => sessionReady && !resetBlocked && failure == null && epoch == restoredEpoch;
    }
}
