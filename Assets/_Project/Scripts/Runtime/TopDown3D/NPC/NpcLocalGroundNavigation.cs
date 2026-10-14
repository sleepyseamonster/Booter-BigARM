using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public sealed class NpcLocalGroundNavigation
    {
        public const int GridSize = 13, MaxCandidates = 8, MaxProbeCalls = 8192;
        private const int Center = 6;
        private const float Cell = 1.25f;
        private readonly bool[,] traversable = new bool[GridSize, GridSize];
        private readonly float[,] heights = new float[GridSize, GridSize];
        private readonly float[,] clearance = new float[GridSize, GridSize];
        private readonly List<Vector2Int> path = new(GridSize * GridSize);
        private readonly List<Vector3> candidate = new(GridSize * GridSize);
        private readonly List<Vector3> best = new(GridSize * GridSize);
        private readonly List<Vector3> route = new(GridSize * GridSize);
        private readonly HashSet<Vector2Int> goals = new();
        private readonly List<NpcEscapeCandidate> reviews = new(MaxCandidates);
        private double nextPlan, committedUntil;
        private int index;
        public NpcMovementStatus Status { get; private set; }
        public string Reason { get; private set; }
        public int Searches { get; private set; }
        public int ProbeCalls { get; private set; }
        public int PlanCount { get; private set; }
        public Vector3 Endpoint => route.Count > 0 ? route[^1] : Vector3.zero;
        public float SelectedScore { get; private set; }
        public IReadOnlyList<Vector3> Route => route;
        public IReadOnlyList<NpcEscapeCandidate> Candidates => reviews;
        public bool HasRoute => index < route.Count;
        public void Cancel(double now, bool immediate = false)
        { route.Clear(); index = 0; committedUntil = 0; nextPlan = immediate ? now : nextPlan; Status = NpcMovementStatus.Idle; }
        public void Veto(double now, NpcTuning t)
        { route.Clear(); index = 0; committedUntil = 0; nextPlan = now + t.replanSeconds; Status = NpcMovementStatus.Blocked; Reason = "Motor veto; retry deadline retained."; }
        public bool TryWaypoint(Vector3 position, out Vector3 waypoint)
        {
            while (index < route.Count && NpcMath.Distance(position, route[index]) < .18f) index++;
            waypoint = index < route.Count ? route[index] : position; return index < route.Count;
        }
        public void Plan(NpcGroundMotor motor, NpcIntent intent, NpcTuning t, Vector3 areaCenter, float areaRadius, double now, NpcReservation[] reservations = null)
        {
            if (now < nextPlan) { if (!HasRoute) Status = NpcMovementStatus.Queued; return; }
            nextPlan = now + t.replanSeconds; PlanCount++; Searches = 0; goals.Clear(); best.Clear(); reviews.Clear();
            motor.BeginProbePass(); var origin = motor.Position;
            for (var x = 0; x < GridSize; x++) for (var z = 0; z < GridSize; z++)
            {
                var point = origin + new Vector3((x - Center) * Cell, 0, (z - Center) * Cell);
                var supported = motor.TryGround(point, t, true, out var ground);
                heights[x, z] = ground.y;
                traversable[x, z] = supported && NpcMath.Distance(ground, areaCenter) + motor.Radius < areaRadius;
                if (reservations != null) foreach (var reservation in reservations)
                    if (reservation.Contains(ground, motor.Radius)) traversable[x, z] = false;
            }
            if (!traversable[Center, Center]) { FinishFailure(motor, "Current support unavailable."); return; }
            heights[Center, Center] = origin.y;
            for (var x = 0; x < GridSize; x++) for (var z = 0; z < GridSize; z++)
            {
                var minimum = Mathf.Min(Mathf.Min(x + 1, GridSize - x), Mathf.Min(z + 1, GridSize - z)) * Cell;
                for (var bx = 0; bx < GridSize; bx++) for (var bz = 0; bz < GridSize; bz++)
                    if (!traversable[bx, bz]) minimum = Mathf.Min(minimum, Vector2.Distance(new(x, z), new(bx, bz)) * Cell);
                clearance[x, z] = Mathf.Max(0, minimum - motor.Radius);
            }
            var away = Vector3.ProjectOnPlane(origin - intent.Threat, Vector3.up);
            if (away.sqrMagnitude < .001f) away = Vector3.forward;
            away.Normalize(); var bestScore = float.NegativeInfinity; var bestPartial = false;
            var preferred = HasRoute ? Vector3.ProjectOnPlane(route[index] - origin, Vector3.up).normalized : away;
            // Stable offsets biased away, then perpendicular; no random or clamped duplicate goals.
            for (var n = 0; n < MaxCandidates; n++)
            {
                if (motor.Probes >= MaxProbeCalls) break;
                var angle = n == 0 ? 0 : n % 2 == 1 ? (n + 1) / 2 * 45 : -n / 2 * 45;
                var direction = Quaternion.Euler(0, angle, 0) * away;
                var goal = new Vector2Int(Center + Mathf.RoundToInt(direction.x * 6), Center + Mathf.RoundToInt(direction.z * 6));
                if (!goals.Add(goal)) continue; // An obstructed goal may still offer legal partial escape progress.
                var goalPoint = origin + new Vector3((goal.x - Center) * Cell, 0, (goal.y - Center) * Cell);
                Searches++;
                if (!TopDown3DLocalGridPathfinder.TryFindPath(traversable, heights, new(Center, Center), goal, t.stepUp, t.stepDown, path))
                { reviews.Add(new(goalPoint, "No reachable progress.")); continue; }
                candidate.Clear(); var previous = origin; var legal = true;
                foreach (var p in path)
                {
                    var point = origin + new Vector3((p.x - Center) * Cell, heights[p.x, p.y] - origin.y, (p.y - Center) * Cell);
                    if (!NpcMath.ReservationsClear(previous, point, motor.Radius, reservations) || !motor.SegmentClear(previous, point)) { legal = false; break; }
                    candidate.Add(point); previous = point;
                }
                if (!legal || candidate.Count == 0) { reviews.Add(new(goalPoint, "Support, sweep or reservation rejected route.")); continue; }
                var score = Score(origin, intent.Threat, candidate, preferred, out var gain, out var exposure);
                if (gain < 1 || exposure > 1) { reviews.Add(new(candidate[^1], gain < 1 ? "Insufficient separation gain." : "Excess route exposure.")); continue; }
                var minClearance = 2f;
                foreach (var p in path) minClearance = Mathf.Min(minClearance, clearance[p.x, p.y]);
                score += .2f * Mathf.Clamp01(minClearance / 2);
                reviews.Add(new(candidate[^1], "Eligible escape.", score, true));
                if (score <= bestScore) continue;
                bestScore = score; best.Clear(); best.AddRange(candidate); bestPartial = path[^1] != goal;
            }
            ProbeCalls = motor.Probes;
            if (ProbeCalls >= MaxProbeCalls && best.Count == 0) { FinishFailure(motor, "Route probe cap reached."); return; }
            // Evaluate current and replacement against today's threat. Never retain an unchecked stale route.
            var keep = HasRoute && ProbeCalls < MaxProbeCalls;
            if (keep)
            {
                candidate.Clear(); for (var i = index; i < route.Count; i++) candidate.Add(route[i]);
                var current = Score(origin, intent.Threat, candidate, preferred, out var gain, out var exposure);
                keep = gain >= 1 && exposure <= 1 && NpcMath.ReservationsClear(origin, candidate[0], motor.Radius, reservations) && motor.SegmentClear(origin, candidate[0]);
                for (var i = 1; keep && i < candidate.Count; i++) keep = NpcMath.ReservationsClear(candidate[i - 1], candidate[i], motor.Radius, reservations) && motor.SegmentClear(candidate[i - 1], candidate[i]);
                var minimum = 2f;
                foreach (var point in candidate)
                {
                    var x = Center + Mathf.RoundToInt((point.x - origin.x) / Cell);
                    var z = Center + Mathf.RoundToInt((point.z - origin.z) / Cell);
                    if (x < 0 || z < 0 || x >= GridSize || z >= GridSize || !traversable[x, z]) { keep = false; break; }
                    minimum = Mathf.Min(minimum, clearance[x, z]);
                }
                current += .2f * Mathf.Clamp01(minimum / 2);
                if (motor.Probes >= MaxProbeCalls) keep = false;
                if (keep && (now < committedUntil || bestScore < current + t.switchMargin))
                { SelectedScore = current; ProbeCalls = motor.Probes; motor.EndProbePass(); return; }
            }
            if (best.Count == 0) { FinishFailure(motor, "No safe escape improves separation."); return; }
            route.Clear(); route.AddRange(best); index = 0; SelectedScore = bestScore;
            committedUntil = now + t.commitmentSeconds; Status = bestPartial ? NpcMovementStatus.Partial : NpcMovementStatus.Moving;
            Reason = bestPartial ? "Safe partial progress; continue local horizon." : "Safe local escape waypoint.";
            motor.EndProbePass();
        }
        // Terms have fixed units: gain / 7.5m, worst approach / 1m, length / 30m, heading dot mapped to [0,1].
        public static float Score(Vector3 origin, Vector3 threat, IReadOnlyList<Vector3> points, Vector3 preferred,
            out float gain, out float exposure)
        {
            var initial = NpcMath.Distance(origin, threat); var minimum = initial; var length = 0f; var previous = origin;
            for (var i = 0; i < points.Count; i++)
            { minimum = Mathf.Min(minimum, NpcMath.SegmentDistance(threat, previous, points[i])); length += Vector3.Distance(previous, points[i]); previous = points[i]; }
            gain = points.Count == 0 ? 0 : NpcMath.Distance(points[^1], threat) - initial;
            exposure = initial - minimum;
            var heading = points.Count == 0 ? Vector3.zero : Vector3.ProjectOnPlane(points[0] - origin, Vector3.up).normalized;
            return .45f * Mathf.Clamp01(gain / 7.5f) - .2f * Mathf.Clamp01(exposure)
                - .1f * Mathf.Clamp01(length / 30) + .05f * (Vector3.Dot(heading, preferred) + 1) * .5f;
        }
        private void FinishFailure(NpcGroundMotor motor, string reason)
        { route.Clear(); index = 0; Status = NpcMovementStatus.Blocked; Reason = reason; ProbeCalls = motor.Probes; motor.EndProbePass(); motor.Stop(); }
    }
}
