using System;
using System.Collections.Generic;

namespace BooterBigArm.TopDown3D.WorldCreator
{
    public readonly struct WorldCliffDebrisCandidate
    {
        public WorldCliffDebrisCandidate(WorldFeatureId id,
            WorldFeatureId sourceFaceId, AbsoluteWorldPosition position,
            int sourceRockVariant, float targetWidth, int ordinal)
        {
            Id = id;
            SourceFaceId = sourceFaceId;
            Position = position;
            SourceRockVariant = sourceRockVariant;
            TargetWidth = targetWidth;
            Ordinal = ordinal;
        }

        public WorldFeatureId Id { get; }
        public WorldFeatureId SourceFaceId { get; }
        public AbsoluteWorldPosition Position { get; }
        public int SourceRockVariant { get; }
        public float TargetWidth { get; }
        public int Ordinal { get; }
    }

    /// <summary>
    /// Plans a short talus runout from a real cliff toe. The source face, absolute
    /// coordinates and decoration version own identity; chunk order does not.
    /// Runtime placement still checks terrain contact and authored clearances.
    /// </summary>
    public static class WorldCliffDebrisPlanner
    {
        private static readonly WorldSeedNamespace DebrisNamespace =
            new WorldSeedNamespace(WorldVersionDomain.Decoration, "decoration.cliff-talus");

        public static IReadOnlyList<WorldCliffDebrisCandidate> Plan(
            WorldIdentity world, IWorldCoordinateModel coordinates,
            WorldCliffFaceSpan source)
        {
            if (coordinates == null) throw new ArgumentNullException(nameof(coordinates));
            var first = source.First;
            var last = source.Last;
            var drop = Math.Min(first.VerticalDrop, last.VerticalDrop);
            if (drop < 3d) return Array.Empty<WorldCliffDebrisCandidate>();

            var sourceAddress = coordinates.Encode(first.Center);
            var seed = DebrisNamespace.DeriveSeed(world, sourceAddress);
            if (Hash01(seed, 0) > 0.20d) return Array.Empty<WorldCliffDebrisCandidate>();
            var count = drop >= 24d ? 3 : drop >= 8d ? 2 : 1;
            var outwardA = first.OutwardA + last.OutwardA;
            var outwardB = first.OutwardB + last.OutwardB;
            var length = Math.Sqrt(outwardA * outwardA + outwardB * outwardB);
            if (length < 1e-6d) return Array.Empty<WorldCliffDebrisCandidate>();
            outwardA /= length;
            outwardB /= length;
            var toeA = (first.Toe.HorizontalA + last.Toe.HorizontalA) * 0.5d;
            var toeB = (first.Toe.HorizontalB + last.Toe.HorizontalB) * 0.5d;
            var results = new List<WorldCliffDebrisCandidate>(count);
            for (var ordinal = 0; ordinal < count; ordinal++)
            {
                var runout = Math.Min(first.DebrisRunout, last.DebrisRunout);
                var halfWidth = Math.Min(first.DebrisHalfWidth, last.DebrisHalfWidth);
                var distance = runout * (0.25d + ordinal * 0.22d
                    + Hash01(seed, ordinal * 7 + 1) * 0.12d);
                var lateral = (Hash01(seed, ordinal * 7 + 2) * 2d - 1d)
                    * halfWidth * (ordinal == 0 ? 0.16d : 0.72d);
                var position = new AbsoluteWorldPosition(
                    toeA + outwardA * distance - outwardB * lateral,
                    0d,
                    toeB + outwardB * distance + outwardA * lateral);
                var influence = (first.SampleDebrisInfluence(position)
                    + last.SampleDebrisInfluence(position)) * 0.5f;
                if (influence <= 0.01f) continue;
                var width = ordinal == 0
                    ? 3.5f + (float)Hash01(seed, ordinal * 7 + 3) * 2.0f
                    : ordinal == 1
                        ? 1.8f + (float)Hash01(seed, ordinal * 7 + 3) * 1.2f
                        : 0.8f + (float)Hash01(seed, ordinal * 7 + 3) * 0.8f;
                var variant = Math.Min(4, (int)(Hash01(seed, ordinal * 7 + 4) * 5d));
                var id = WorldFeatureId.Create(world, DebrisNamespace,
                    coordinates.Encode(position), source.Id + ":" + ordinal);
                results.Add(new WorldCliffDebrisCandidate(id, source.Id, position,
                    variant, width, ordinal));
            }

            return results.AsReadOnly();
        }

        private static double Hash01(ulong seed, int ordinal)
        {
            unchecked
            {
                var value = seed ^ ((ulong)(uint)ordinal + 1UL) * 0x9E3779B185EBCA87UL;
                value ^= value >> 30;
                value *= 0xBF58476D1CE4E5B9UL;
                value ^= value >> 27;
                value *= 0x94D049BB133111EBUL;
                value ^= value >> 31;
                return (value & 0xFFFFFFUL) / 16777215d;
            }
        }
    }
}
