using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    // Pure policy: receives knowledge, never reads a hidden target or mutates physics.
    public sealed class SkittishNpcBrain
    {
        public NpcBehavior State { get; private set; }
        public string Reason { get; private set; } = "No threat.";
        private double enteredAt, calmSince = double.NaN;
        public void Reset(double now) { State = NpcBehavior.Idle; enteredAt = now; calmSince = double.NaN; Reason = "Fixture reset."; }
        private void Enter(NpcBehavior state, double now, string reason)
        { if (state != State) { State = state; enteredAt = now; } Reason = reason; }
        public NpcIntent Tick(Vector3 position, NpcThreatObservation observation, double now, NpcTuning tuning)
        {
            var fresh = observation.IsFresh(now, tuning.memorySeconds);
            var distance = fresh ? NpcMath.Distance(position, observation.Position) : float.PositiveInfinity;
            var calm = !fresh || distance >= tuning.calmDistance;
            if (!calm) calmSince = double.NaN;
            if (fresh && distance <= tuning.panic)
                Enter(NpcBehavior.Flee, now, "Threat inside panic distance.");
            else if (State == NpcBehavior.Flee && fresh && distance < tuning.panicRelease)
                Reason = "Retain panic hysteresis.";
            else if (calm)
            {
                if (State != NpcBehavior.Idle)
                {
                    Enter(NpcBehavior.Recover, now, fresh ? "Safe separation." : "Threat memory expired.");
                    if (double.IsNaN(calmSince)) calmSince = now;
                    if (now - calmSince >= tuning.calmSeconds) Enter(NpcBehavior.Idle, now, "Calm hold completed.");
                }
            }
            else if (State == NpcBehavior.Flee)
                Enter(NpcBehavior.Retreat, now, "Panic released; retain distance.");
            else if (State == NpcBehavior.Retreat)
                Reason = "Retain retreat until safe separation.";
            else if (State == NpcBehavior.Idle)
                Enter(NpcBehavior.Alert, now, "Threat detected.");
            else if (distance <= tuning.comfort && (State == NpcBehavior.Recover || now - enteredAt >= tuning.alertSeconds))
                Enter(NpcBehavior.Retreat, now, "Threat inside comfort distance.");
            else if (State == NpcBehavior.Recover)
                Enter(NpcBehavior.Alert, now, "Threat remains nearby.");

            return fresh && (State == NpcBehavior.Retreat || State == NpcBehavior.Flee)
                ? new NpcIntent(observation.Position, State == NpcBehavior.Flee ? tuning.fleeSpeed : tuning.retreatSpeed)
                : default;
        }
    }
}
