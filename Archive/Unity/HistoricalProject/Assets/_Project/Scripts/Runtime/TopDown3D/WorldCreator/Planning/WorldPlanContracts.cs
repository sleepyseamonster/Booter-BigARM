using System;

namespace BooterBigArm.TopDown3D.WorldCreator
{
    public readonly struct WorldPlanHaloPolicy : IEquatable<WorldPlanHaloPolicy>
    {
        public WorldPlanHaloPolicy(int topologicalRadius, int maximumParticipants)
        {
            if (topologicalRadius < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(topologicalRadius));
            }

            if (maximumParticipants < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumParticipants));
            }

            TopologicalRadius = topologicalRadius;
            MaximumParticipants = maximumParticipants;
        }

        public int TopologicalRadius { get; }
        public int MaximumParticipants { get; }

        public bool IncludesDistance(int topologicalDistance)
        {
            return topologicalDistance >= 0 && topologicalDistance <= TopologicalRadius;
        }

        public bool AcceptsParticipantCount(int participantCount)
        {
            return participantCount >= 1 && participantCount <= MaximumParticipants;
        }

        public bool Equals(WorldPlanHaloPolicy other)
        {
            return TopologicalRadius == other.TopologicalRadius
                && MaximumParticipants == other.MaximumParticipants;
        }

        public override bool Equals(object obj)
        {
            return obj is WorldPlanHaloPolicy other && Equals(other);
        }

        public override int GetHashCode()
        {
            return unchecked(TopologicalRadius * 397 ^ MaximumParticipants);
        }
    }

    public readonly struct WorldPlanHeader : IEquatable<WorldPlanHeader>
    {
        public WorldPlanHeader(WorldPlanKey key, int formatVersion, WorldFeatureId contentFingerprint)
        {
            if (formatVersion < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(formatVersion));
            }

            if (contentFingerprint.IsEmpty)
            {
                throw new ArgumentException("Plan content fingerprints cannot be empty.", nameof(contentFingerprint));
            }

            Key = key;
            FormatVersion = formatVersion;
            ContentFingerprint = contentFingerprint;
        }

        public WorldPlanKey Key { get; }
        public int FormatVersion { get; }
        public WorldFeatureId ContentFingerprint { get; }

        public bool Equals(WorldPlanHeader other)
        {
            return Key.Equals(other.Key)
                && FormatVersion == other.FormatVersion
                && ContentFingerprint.Equals(other.ContentFingerprint);
        }

        public override bool Equals(object obj)
        {
            return obj is WorldPlanHeader other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = Key.GetHashCode();
                hash = hash * 397 ^ FormatVersion;
                hash = hash * 397 ^ ContentFingerprint.GetHashCode();
                return hash;
            }
        }
    }

    public interface IWorldPlanRecord
    {
        WorldPlanHeader Header { get; }
    }

    public interface IWorldPlanCache<TPlan> where TPlan : IWorldPlanRecord
    {
        int Count { get; }
        bool TryGet(WorldPlanKey key, out TPlan plan);
        void Store(TPlan plan);
        bool Remove(WorldPlanKey key);
        void Clear();
    }
}
