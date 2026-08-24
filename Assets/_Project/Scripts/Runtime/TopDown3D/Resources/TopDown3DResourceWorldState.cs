using System;
using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [Serializable]
    public sealed class TopDown3DResourceNodeDelta
    {
        [SerializeField] private string stableId;
        [SerializeField] private int remainingUses;

        public string StableId => stableId;
        public int RemainingUses => remainingUses;

        internal static TopDown3DResourceNodeDelta Create(string id, int uses)
        {
            return new TopDown3DResourceNodeDelta { stableId = id, remainingUses = uses };
        }
    }

    [Serializable]
    public sealed class TopDown3DResourceWorldSnapshot
    {
        [SerializeField] private int version;
        [SerializeField] private int resourceGenerationVersion;
        [SerializeField] private List<TopDown3DResourceNodeDelta> deltas =
            new List<TopDown3DResourceNodeDelta>();

        public int Version => version;
        public int ResourceGenerationVersion => resourceGenerationVersion;
        public IReadOnlyList<TopDown3DResourceNodeDelta> Deltas => deltas;

        internal static TopDown3DResourceWorldSnapshot Create(
            int snapshotVersion,
            int generationVersion,
            IEnumerable<KeyValuePair<string, int>> records)
        {
            var ordered = new List<KeyValuePair<string, int>>(records);
            ordered.Sort((left, right) => string.CompareOrdinal(left.Key, right.Key));
            var snapshot = new TopDown3DResourceWorldSnapshot
            {
                version = snapshotVersion,
                resourceGenerationVersion = generationVersion
            };
            for (var i = 0; i < ordered.Count; i++)
            {
                snapshot.deltas.Add(TopDown3DResourceNodeDelta.Create(ordered[i].Key, ordered[i].Value));
            }

            return snapshot;
        }
    }

    [DisallowMultipleComponent]
    public sealed class TopDown3DResourceWorldState : MonoBehaviour
    {
        public const int CurrentSnapshotVersion = 1;

        [SerializeField, Min(1)] private int resourceGenerationVersion = 1;
        private readonly Dictionary<string, int> deltas = new Dictionary<string, int>(StringComparer.Ordinal);

        public int ResourceGenerationVersion => resourceGenerationVersion;
        public event Action<string, int> Changed;

        public void Configure(int generationVersion)
        {
            generationVersion = Mathf.Max(1, generationVersion);
            if (resourceGenerationVersion != generationVersion && deltas.Count > 0)
            {
                throw new InvalidOperationException("Cannot change resource generation version while runtime deltas exist.");
            }

            resourceGenerationVersion = generationVersion;
        }

        public int GetRemainingUses(string stableId, int pristineUses)
        {
            if (string.IsNullOrWhiteSpace(stableId) || pristineUses < 1)
            {
                throw new ArgumentException("Resource identity and pristine uses must be valid.");
            }

            return deltas.TryGetValue(stableId, out var remaining)
                ? remaining
                : pristineUses;
        }

        public bool TryConsume(string stableId, int pristineUses, out int remainingUses)
        {
            remainingUses = GetRemainingUses(stableId, pristineUses);
            if (remainingUses <= 0)
            {
                return false;
            }

            remainingUses--;
            deltas[stableId] = remainingUses;
            Changed?.Invoke(stableId, remainingUses);
            return true;
        }

        public TopDown3DResourceWorldSnapshot CaptureSnapshot()
        {
            return TopDown3DResourceWorldSnapshot.Create(
                CurrentSnapshotVersion,
                resourceGenerationVersion,
                deltas);
        }

        public bool ApplySnapshot(TopDown3DResourceWorldSnapshot snapshot)
        {
            if (snapshot == null || snapshot.Version != CurrentSnapshotVersion
                || snapshot.ResourceGenerationVersion != resourceGenerationVersion
                || snapshot.Deltas == null)
            {
                return false;
            }

            var next = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < snapshot.Deltas.Count; i++)
            {
                var delta = snapshot.Deltas[i];
                if (delta == null || string.IsNullOrWhiteSpace(delta.StableId)
                    || delta.RemainingUses < 0 || !next.TryAdd(delta.StableId, delta.RemainingUses))
                {
                    return false;
                }
            }

            deltas.Clear();
            foreach (var pair in next)
            {
                deltas.Add(pair.Key, pair.Value);
            }

            return true;
        }
    }
}
