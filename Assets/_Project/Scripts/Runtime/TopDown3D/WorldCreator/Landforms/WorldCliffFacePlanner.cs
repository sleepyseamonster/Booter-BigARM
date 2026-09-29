using System;
using System.Collections.Generic;

namespace BooterBigArm.TopDown3D.WorldCreator
{
    /// <summary>
    /// A chunk-owned span of one absolute geological face. A canyon section retains
    /// its canonical segment parent; broad-ground sections remain independently owned.
    /// </summary>
    public readonly struct WorldCliffFaceSpan
    {
        public WorldCliffFaceSpan(WorldCliffSectionCandidate first,
            WorldCliffSectionCandidate last)
        {
            First = first;
            Last = last;
            Id = first.Id;
            ParentFeatureId = first.ParentFeatureId;
            StrataFamilyId = first.StrataFamilyId;
        }

        public WorldFeatureId Id { get; }
        public WorldFeatureId ParentFeatureId { get; }
        public string StrataFamilyId { get; }
        public WorldCliffSectionCandidate First { get; }
        public WorldCliffSectionCandidate Last { get; }
    }

    /// <summary>
    /// Links sampled cliff sections in absolute space. The caller supplies a two-cell
    /// halo, then requests only the owner cells it will realize. No Unity objects or
    /// chunk-local random state participate in the layout decision.
    /// </summary>
    public static class WorldCliffFacePlanner
    {
        private const double MaximumLinkDistance = 4.3d;
        private const double MinimumDirectionAlignment = 0.72d;
        private const double MinimumFacingAlignment = 0.82d;

        public static IReadOnlyList<WorldCliffFaceSpan> PlanOwnedSpans(
            IReadOnlyDictionary<(long A, long B), WorldCliffSectionCandidate> candidates,
            long minimumA, long maximumA, long minimumB, long maximumB)
        {
            if (candidates == null) throw new ArgumentNullException(nameof(candidates));
            if (minimumA > maximumA || minimumB > maximumB)
                throw new ArgumentOutOfRangeException(nameof(maximumA));

            var spans = new List<WorldCliffFaceSpan>();
            for (var b = minimumB; b <= maximumB; b++)
            for (var a = minimumA; a <= maximumA; a++)
            {
                if (!candidates.TryGetValue((a, b), out var first)
                    || !TryFindNeighbor(first, candidates, true, out var last)
                    || !TryFindNeighbor(last, candidates, false, out var reverse)
                    || reverse.OwnerCellA != first.OwnerCellA
                    || reverse.OwnerCellB != first.OwnerCellB)
                    continue;
                spans.Add(new WorldCliffFaceSpan(first, last));
            }
            return spans.AsReadOnly();
        }

        private static bool TryFindNeighbor(
            WorldCliffSectionCandidate section,
            IReadOnlyDictionary<(long A, long B), WorldCliffSectionCandidate> candidates,
            bool forward,
            out WorldCliffSectionCandidate result)
        {
            result = default;
            var bestScore = double.NegativeInfinity;
            var tangentA = -section.OutwardB * (forward ? 1d : -1d);
            var tangentB = section.OutwardA * (forward ? 1d : -1d);
            for (var db = -1; db <= 1; db++)
            for (var da = -1; da <= 1; da++)
            {
                if (da == 0 && db == 0) continue;
                if (!candidates.TryGetValue((section.OwnerCellA + da,
                        section.OwnerCellB + db), out var candidate)
                    || !section.ParentFeatureId.Equals(candidate.ParentFeatureId)
                    || !string.Equals(section.StrataFamilyId, candidate.StrataFamilyId,
                        StringComparison.Ordinal)) continue;

                var deltaA = candidate.Center.HorizontalA - section.Center.HorizontalA;
                var deltaB = candidate.Center.HorizontalB - section.Center.HorizontalB;
                var distance = Math.Sqrt(deltaA * deltaA + deltaB * deltaB);
                if (distance <= 1e-6d || distance > MaximumLinkDistance) continue;
                var direction = (deltaA * tangentA + deltaB * tangentB) / distance;
                var facing = section.OutwardA * candidate.OutwardA
                    + section.OutwardB * candidate.OutwardB;
                if (direction < MinimumDirectionAlignment || facing < MinimumFacingAlignment
                    || Math.Abs(candidate.Rim.Vertical - section.Rim.Vertical) > 2.5d
                    || Math.Abs(candidate.Toe.Vertical - section.Toe.Vertical) > 2.5d)
                    continue;

                var score = direction * facing / distance;
                if (score < bestScore || (score == bestScore
                    && result.Id.CompareTo(candidate.Id) < 0)) continue;
                bestScore = score;
                result = candidate;
            }
            return bestScore > double.NegativeInfinity;
        }
    }
}
