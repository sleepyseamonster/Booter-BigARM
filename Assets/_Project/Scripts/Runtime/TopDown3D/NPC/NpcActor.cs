using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent, RequireComponent(typeof(NpcGroundMotor))]
    public sealed class NpcActor : MonoBehaviour
    {
        [SerializeField] private string stableId = "lab.npc.skittish_legger.001";
        [SerializeField] private NpcDefinition definition;
        [SerializeField] private Transform threatTarget;
        [SerializeField] private Vector3 activityCenter;
        [SerializeField] private float activityRadius = 40;
        [SerializeField] private NpcReservation[] reservations = System.Array.Empty<NpcReservation>();
        private NpcTuning tuning;
        private NpcGroundMotor motor;
        private readonly SkittishNpcBrain brain = new();
        private readonly NpcThreatPerception perception = new();
        private readonly NpcLocalGroundNavigation navigation = new();
        private double nextSense, nextDecision, sampledAt;
        private Vector3 sampledPosition;
        private bool initialized, allowed;
        private NpcIntent intent;
        public string StableId => stableId;
        public NpcDefinition Definition => definition;
        public Transform ThreatTarget => threatTarget;
        public NpcBehavior Behavior => brain.State;
        public NpcMovementStatus Movement => navigation.Status;
        public NpcGroundMotor Motor => motor != null ? motor : (motor = GetComponent<NpcGroundMotor>());
        public NpcLocalGroundNavigation Navigation => navigation;
        public NpcThreatObservation Observation => perception.Observation;
        public string Failure { get; private set; }
        public string DecisionReason => brain.Reason;
        public bool IsArmed => initialized && allowed;
        public uint Epoch { get; private set; }
        public uint IntentRevision { get; private set; }
        public NpcTuning Tuning => tuning?.Copy();
        public void Configure(NpcDefinition value, Transform target, Vector3 center, float radius, NpcReservation[] exclusions = null)
        { definition = value; threatTarget = target; activityCenter = center; activityRadius = radius; reservations = exclusions == null ? System.Array.Empty<NpcReservation>() : (NpcReservation[])exclusions.Clone(); }
        public bool Initialize(out string reason)
        {
            reason = "Missing NPC configuration or same-scene target.";
            if (string.IsNullOrWhiteSpace(stableId) || definition == null
                || threatTarget == null || threatTarget.gameObject.scene != gameObject.scene
                || !NpcMath.Finite(activityCenter) || !float.IsFinite(activityRadius) || activityRadius < 10 || activityRadius > 80
                ) { Failure = reason; return false; }
            if (!definition.Validate(out reason) || !Motor.ValidateShape(out reason)) { Failure = reason; return false; }
            if (reservations == null || reservations.Length > 32) { reason = Failure = "Invalid authored reservation count."; return false; }
            foreach (var reservation in reservations)
                if (!NpcMath.Finite(reservation.center) || !float.IsFinite(reservation.radius) || reservation.radius <= 0)
                { reason = Failure = "Invalid authored reservation."; return false; }
            if (!initialized) tuning = definition.Profile.Snapshot();
            if (!Motor.TryGround(Motor.Position, tuning, false, out var grounded) || Vector3.Distance(grounded, Motor.Position) > .12f)
            { reason = Failure = "Initial support/clearance is unavailable; no startup relocation."; return false; }
            initialized = true; Failure = null; reason = null; return true;
        }
        public void SetSimulationAllowed(bool value)
        { allowed = value && initialized; if (!allowed) { Motor.Stop(); navigation.Cancel(Time.timeAsDouble); intent = default; } }
        public void Invalidate()
        {
            Epoch++; IntentRevision++; allowed = false; intent = default; Motor.Stop(); perception.Clear();
            navigation.Cancel(Time.timeAsDouble, true); brain.Reset(Time.timeAsDouble); nextSense = nextDecision = 0;
            sampledAt = 0;
        }
        public bool ApplyTuning(NpcTuning value, out string reason)
        {
            reason = "No tuning snapshot.";
            if (value == null || !value.Validate(out reason)) return false;
            tuning = value.Copy(); IntentRevision++; intent = default; navigation.Cancel(Time.timeAsDouble, true); Motor.Stop();
            nextSense = nextDecision = 0; Failure = null; return true;
        }
        private void FixedUpdate()
        {
            if (!IsArmed) return;
            Tick(Time.timeAsDouble, Time.fixedDeltaTime);
        }
        public void Tick(double now, float dt)
        {
            if (!IsArmed || dt <= 0 || !float.IsFinite(dt) || dt > .05f || !double.IsFinite(now)) return;
            if (threatTarget == null || !threatTarget.gameObject.activeInHierarchy) perception.Clear();
            if (now >= nextSense)
            { perception.Sample(transform, threatTarget, tuning, now); nextSense = now + tuning.perceptionSeconds; }
            if (now >= nextDecision)
            {
                var previous = brain.State;
                intent = brain.Tick(Motor.Position, perception.Observation, now, tuning); nextDecision = now + tuning.decisionSeconds;
                if (previous != brain.State) IntentRevision++;
                if (!intent.Move) { Motor.Stop(); navigation.Cancel(now); }
            }
            if (!intent.Move)
            {
                if (perception.Observation.IsFresh(now, tuning.memorySeconds) && brain.State != NpcBehavior.Idle)
                    Motor.FaceKnownPosition(perception.Observation.Position, tuning, dt);
                return;
            }
            navigation.Plan(Motor, intent, tuning, activityCenter, activityRadius, now, reservations);
            if (!navigation.TryWaypoint(Motor.Position, out var waypoint)) { Motor.Stop(); return; }
            if (!Motor.Step(waypoint, intent.Speed, tuning, dt, out var reason, reservations))
            { Failure = reason; navigation.Veto(now, tuning); return; }
            if (now - sampledAt >= tuning.stuckSeconds)
            {
                if (sampledAt > 0 && Vector3.Distance(Motor.Position, sampledPosition) < tuning.progressMetres) navigation.Veto(now, tuning);
                sampledAt = now; sampledPosition = Motor.Position;
            }
        }
        private void OnDisable() => Invalidate();
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(activityCenter, activityRadius);
            if (tuning != null) { Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, tuning.comfort); }
            Gizmos.color = Color.green; var previous = transform.position;
            foreach (var p in navigation.Route) { Gizmos.DrawLine(previous, p); previous = p; }
            foreach (var candidate in navigation.Candidates)
            { Gizmos.color = candidate.Eligible ? Color.green : Color.red; Gizmos.DrawWireSphere(candidate.Position, .15f); }
        }
    }
}
