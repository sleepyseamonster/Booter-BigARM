using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public enum LeggerRidePhase { OnFoot, Mounting, Mounted, Dismounting, Faulted }
    public enum LeggerRideBlock { None, Unavailable, Busy, OutOfReach, EntryBlocked, NoSafeExit, MissingSupport, Obstacle, Budget, Suspended }

    // Relationship identity is independent of input mode and of a particular collider instance.
    public sealed class LeggerRideState
    {
        public LeggerRidePhase Phase { get; private set; }
        public uint Epoch { get; private set; } = 1;
        public bool OwnsMount => Phase != LeggerRidePhase.OnFoot;
        public bool HasRider => Phase == LeggerRidePhase.Mounted || Phase == LeggerRidePhase.Dismounting || Phase == LeggerRidePhase.Faulted;
        public bool ExitRearmed { get; private set; }
        public float EntrySeconds { get; private set; }
        public bool BeginMount()
        {
            if (OwnsMount) return false;
            Epoch++; Phase = LeggerRidePhase.Mounting; EntrySeconds = 0; return true;
        }
        public bool CommitMount(uint epoch)
        {
            if (epoch != Epoch || Phase != LeggerRidePhase.Mounting) return false;
            Phase = LeggerRidePhase.Mounted; return true;
        }
        public bool RequestExit()
        {
            if (Phase != LeggerRidePhase.Mounted) return false;
            Phase = LeggerRidePhase.Dismounting; ExitRearmed = false; return true;
        }
        public bool ObserveExitInput(Vector2 move)
        {
            if (Phase != LeggerRidePhase.Dismounting) return false;
            if (move.sqrMagnitude < .0484f) ExitRearmed = true;
            else if (ExitRearmed) { Phase = LeggerRidePhase.Mounted; return true; }
            return false;
        }
        public void RejectExit() { if (Phase == LeggerRidePhase.Dismounting) Phase = LeggerRidePhase.Mounted; }
        public bool AdvanceEntry(float dt, float timeout)
        {
            if (Phase != LeggerRidePhase.Mounting) return false;
            EntrySeconds += Mathf.Max(0, dt);
            if (EntrySeconds < timeout) return false;
            Clear(); return true;
        }
        public void Fault() { Epoch++; Phase = LeggerRidePhase.Faulted; }
        public void InvalidateCommands() { Epoch++; ExitRearmed = false; }
        public void Clear() { Epoch++; Phase = LeggerRidePhase.OnFoot; EntrySeconds = 0; ExitRearmed = false; }
    }
}
