using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public enum RootBeaconPhase { Exploring, Departing, Extracted }

    /// <summary>One session's immutable arrival identity and departure state. No disk persistence.</summary>
    [Serializable]
    public sealed class RootBeaconRun
    {
        public const float ReturnRadius = 3f;
        public const float DepartureSeconds = 3f;
        [SerializeField] private string runId, worldId;
        [SerializeField] private int seed;
        [SerializeField] private Vector3 origin;
        [SerializeField] private RootBeaconPhase phase;
        [SerializeField] private float departureElapsed;
        public string RunId => runId;
        public string WorldId => worldId;
        public int Seed => seed;
        public Vector3 Origin => origin;
        public RootBeaconPhase Phase { get => phase; private set => phase = value; }
        public float DepartureElapsed { get => departureElapsed; private set => departureElapsed = value; }
        public string BeaconId => WorldId + "/expedition/" + RunId + "/root";

        public RootBeaconRun(string worldId, string runId, int seed, Vector3 origin)
        {
            if (string.IsNullOrWhiteSpace(worldId) || string.IsNullOrWhiteSpace(runId)
                || !Finite(origin)) throw new ArgumentException("A run requires a world, identity and finite arrival point.");
            this.worldId = worldId; this.runId = runId; this.seed = seed; this.origin = origin;
        }

        public bool AtBeacon(Vector3 position) => Finite(position)
            && new Vector2(position.x - Origin.x, position.z - Origin.z).sqrMagnitude <= ReturnRadius * ReturnRadius
            && Mathf.Abs(position.y - Origin.y) <= 1.5f;

        public bool TryDepart(Vector3 position)
        {
            if (Phase != RootBeaconPhase.Exploring || !AtBeacon(position)) return false;
            DepartureElapsed = 0; Phase = RootBeaconPhase.Departing; return true;
        }

        public void Advance(float seconds, Vector3 position, bool canContinue)
        {
            if (Phase != RootBeaconPhase.Departing) return;
            // Proximity is checked at activation. Once requested, movement does not cancel
            // the return countdown; invalid actor data and explicit interruptions still do.
            if (!canContinue || !Finite(position) || !float.IsFinite(seconds) || seconds < 0)
            { Cancel(); return; }
            DepartureElapsed = Mathf.Min(DepartureSeconds, DepartureElapsed + seconds);
            if (DepartureElapsed >= DepartureSeconds) Phase = RootBeaconPhase.Extracted;
        }

        public void Cancel()
        {
            if (Phase != RootBeaconPhase.Departing) return;
            Phase = RootBeaconPhase.Exploring; DepartureElapsed = 0;
        }

        private static bool Finite(Vector3 p) => float.IsFinite(p.x) && float.IsFinite(p.y) && float.IsFinite(p.z);
    }
}
