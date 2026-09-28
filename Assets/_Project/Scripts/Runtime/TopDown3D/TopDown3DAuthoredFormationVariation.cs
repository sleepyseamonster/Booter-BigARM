using System;
using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Seeded composition transforms; terrain fitting remains the placement owner's job.</summary>
    public static class TopDown3DAuthoredFormationVariation
    {
        public readonly struct Entry
        {
            internal Entry(int sourceIndex, string instanceId, Matrix4x4 transform)
            {
                SourceIndex = sourceIndex;
                InstanceId = instanceId;
                Transform = transform;
            }

            public int SourceIndex { get; }
            public string InstanceId { get; }
            public Matrix4x4 Transform { get; }
        }

        public sealed class Layout
        {
            internal Layout(Entry[] entries) => Entries = Array.AsReadOnly(entries);
            public IReadOnlyList<Entry> Entries { get; }
        }

        public static Matrix4x4[] Generate(TopDown3DAuthoredFormationAsset asset, int seed)
        {
            if (asset == null) throw new ArgumentNullException(nameof(asset));
            var members = asset.Members;
            var count = members.Count;
            var result = new Matrix4x4[count];
            var bounds = new Bounds[count];
            var groups = new int[count];
            if (count == 0) return result;
            for (var i = 0; i < count; i++)
            {
                groups[i] = i;
                bounds[i] = TransformBounds(members[i].Mesh.bounds, members[i].LocalPose);
            }
            for (var i = 0; i < count; i++)
            {
                var expanded = bounds[i];
                expanded.Expand(0.04f);
                for (var j = i + 1; j < count; j++)
                {
                    if (!expanded.Intersects(bounds[j])) continue;
                    var from = groups[j];
                    var to = groups[i];
                    for (var k = 0; k < count; k++) if (groups[k] == from) groups[k] = to;
                }
            }
            var whole = bounds[0];
            for (var i = 1; i < count; i++) whole.Encapsulate(bounds[i]);
            var center = new Vector3(whole.center.x, whole.min.y, whole.center.z);
            var global = Matrix4x4.Translate(center)
                * Matrix4x4.Rotate(Quaternion.Euler(0f, Unit(seed, "formation", 0) * 360f, 0f))
                * Matrix4x4.Translate(-center);
            for (var group = 0; group < count; group++)
            {
                if (groups[group] != group) continue;
                var groupBounds = bounds[group];
                for (var j = group + 1; j < count; j++)
                    if (groups[j] == group) groupBounds.Encapsulate(bounds[j]);
                var pivot = new Vector3(groupBounds.center.x, groupBounds.min.y, groupBounds.center.z);
                var id = members[group].SourceId;
                var bell = (Unit(seed, id, 1) + Unit(seed, id, 2) + Unit(seed, id, 3)) / 3f;
                var scale = Mathf.Lerp(0.85f, 1.15f, bell);
                var outward = pivot - center;
                outward.y = 0f;
                var offset = outward * Mathf.Lerp(-0.08f, 0.18f, Unit(seed, id, 4));
                var change = Matrix4x4.TRS(pivot + offset,
                    Quaternion.Euler(0f, Mathf.Lerp(-25f, 25f, Unit(seed, id, 5)), 0f), Vector3.one * scale)
                    * Matrix4x4.Translate(-pivot);
                // Reject new cross-group overlap; never move another group to accommodate it.
                var candidate = TransformBounds(groupBounds, change);
                for (var j = 0; j < count; j++)
                    if (groups[j] != group && candidate.Intersects(bounds[j]))
                    { change = Matrix4x4.identity; break; }
                for (var j = 0; j < count; j++)
                {
                    if (groups[j] != group) continue;
                    result[j] = global * change * members[j].LocalPose;
                    bounds[j] = TransformBounds(bounds[j], change);
                }
            }
            return result;
        }

        public static Layout GenerateLayout(TopDown3DAuthoredFormationAsset asset, int seed)
        {
            if (asset == null) throw new ArgumentNullException(nameof(asset));
            var transforms = Generate(asset, seed);
            if (asset.VariationProfile != TopDown3DAuthoredFormationVariationProfile.HandbuiltSpire)
            {
                var fixedEntries = new Entry[transforms.Length];
                for (var i = 0; i < transforms.Length; i++)
                    fixedEntries[i] = new Entry(i, asset.Members[i].SourceId, transforms[i]);
                return new Layout(fixedEntries);
            }
            return GenerateHandbuiltSpireLayout(asset, seed, transforms);
        }

        private static Layout GenerateHandbuiltSpireLayout(
            TopDown3DAuthoredFormationAsset asset, int seed, Matrix4x4[] transforms)
        {
            var members = asset.Members;
            var count = members.Count;
            if (count == 0) return new Layout(Array.Empty<Entry>());
            var bounds = new Bounds[count];
            var whole = TransformBounds(members[0].Mesh.bounds, transforms[0]);
            for (var i = 0; i < count; i++)
            {
                bounds[i] = TransformBounds(members[i].Mesh.bounds, transforms[i]);
                if (i > 0) whole.Encapsulate(bounds[i]);
            }

            var heights = new float[count];
            for (var i = 0; i < count; i++) heights[i] = bounds[i].size.y;
            Array.Sort(heights);
            var medianHeight = heights[heights.Length / 2];
            var tall = new List<int>();
            for (var i = 0; i < count; i++)
            {
                var horizontal = Mathf.Max(bounds[i].size.x, bounds[i].size.z);
                if (bounds[i].size.y >= medianHeight * 1.65f
                    && bounds[i].size.y >= horizontal * 1.3f) tall.Add(i);
            }
            tall.Sort((left, right) => bounds[right].size.y.CompareTo(bounds[left].size.y));

            // The standing stones define the hand-built silhouette. Preserve their
            // authored height and let the peripheral rocks supply count variation.
            var keep = new bool[count];
            for (var i = 0; i < count; i++) keep[i] = true;
            var target = Mathf.Clamp(count + Mathf.FloorToInt(Unit(seed, "spire-count", 32) * 7f) - 3,
                count - 3, count + 3);
            var keptCount = count;
            if (keptCount > target)
            {
                var optional = new List<int>();
                for (var i = 0; i < count; i++)
                {
                    if (!keep[i] || tall.Contains(i)) continue;
                    optional.Add(i);
                }
                optional.Sort((left, right) =>
                {
                    var leftRadius = HorizontalRadius(bounds[left].center - whole.center);
                    var rightRadius = HorizontalRadius(bounds[right].center - whole.center);
                    var radiusOrder = rightRadius.CompareTo(leftRadius);
                    if (radiusOrder != 0) return radiusOrder;
                    return Unit(seed, members[left].SourceId, 33)
                        .CompareTo(Unit(seed, members[right].SourceId, 33));
                });
                for (var i = 0; i < optional.Count && keptCount > target; i++)
                {
                    keep[optional[i]] = false;
                    keptCount--;
                }
            }

            RemoveUnsupported(members, transforms, keep, bounds, whole.min.y);
            var entries = new List<Entry>(count + 3);
            for (var i = 0; i < count; i++)
                if (keep[i]) entries.Add(new Entry(i, members[i].SourceId, transforms[i]));

            // Positive count variation uses grounded copies of the smallest peripheral stones.
            // They are placed on the formation floor and never become suspended satellites.
            var supplement = 0;
            while (entries.Count < target && supplement < 3)
            {
                var sourceIndex = SmallGroundedSource(bounds, keep, whole.min.y, seed, supplement);
                if (sourceIndex < 0) break;
                var sourceBounds = TransformBounds(members[sourceIndex].Mesh.bounds, transforms[sourceIndex]);
                var scale = Mathf.Lerp(0.72f, 0.98f,
                    Unit(seed, members[sourceIndex].SourceId, 40 + supplement));
                var angle = Unit(seed, "spire-extra-angle", 40 + supplement) * Mathf.PI * 2f;
                var radius = Mathf.Lerp(
                    Mathf.Max(whole.extents.x, whole.extents.z) * 0.72f,
                    Mathf.Max(whole.extents.x, whole.extents.z) * 1.05f,
                    Unit(seed, "spire-extra-radius", 40 + supplement));
                var targetCenter = new Vector3(
                    whole.center.x + Mathf.Cos(angle) * radius,
                    0f,
                    whole.center.z + Mathf.Sin(angle) * radius);
                var pivot = new Vector3(sourceBounds.center.x, sourceBounds.min.y, sourceBounds.center.z);
                var candidate = Matrix4x4.Translate(pivot)
                    * Matrix4x4.Rotate(Quaternion.Euler(0f,
                        Unit(seed, "spire-extra-yaw", 40 + supplement) * 360f, 0f))
                    * Matrix4x4.Scale(Vector3.one * scale)
                    * Matrix4x4.Translate(-pivot) * transforms[sourceIndex];
                var candidateBounds = TransformBounds(members[sourceIndex].Mesh.bounds, candidate);
                targetCenter.y = candidateBounds.center.y
                    + whole.min.y - candidateBounds.min.y + 0.015f;
                candidate = Matrix4x4.Translate(targetCenter - candidateBounds.center) * candidate;
                entries.Add(new Entry(sourceIndex,
                    members[sourceIndex].SourceId + ":extra:" + supplement, candidate));
                supplement++;
            }
            return new Layout(entries.ToArray());
        }

        private static void RemoveUnsupported(IReadOnlyList<TopDown3DAuthoredFormationAsset.Member> members,
            Matrix4x4[] transforms, bool[] keep, Bounds[] bounds, float floor)
        {
            var order = new List<int>();
            for (var i = 0; i < keep.Length; i++)
            {
                bounds[i] = TransformBounds(members[i].Mesh.bounds, transforms[i]);
                if (keep[i]) order.Add(i);
            }
            order.Sort((left, right) => bounds[left].min.y.CompareTo(bounds[right].min.y));
            var supported = new List<int>();
            foreach (var index in order)
            {
                var candidate = bounds[index];
                var groundTolerance = Mathf.Max(0.08f, candidate.size.y * 0.12f);
                var hasSupport = candidate.min.y <= floor + groundTolerance;
                for (var i = 0; !hasSupport && i < supported.Count; i++)
                    hasSupport = Supports(bounds[supported[i]], candidate);
                if (!hasSupport)
                {
                    keep[index] = false;
                    continue;
                }
                supported.Add(index);
            }
        }

        private static bool Supports(Bounds lower, Bounds upper)
        {
            if (lower.center.y >= upper.center.y) return false;
            var tolerance = Mathf.Max(0.06f, Mathf.Min(lower.size.y, upper.size.y) * 0.16f);
            if (lower.max.y < upper.min.y - tolerance) return false;
            var overlapX = Mathf.Min(lower.max.x, upper.max.x) - Mathf.Max(lower.min.x, upper.min.x);
            var overlapZ = Mathf.Min(lower.max.z, upper.max.z) - Mathf.Max(lower.min.z, upper.min.z);
            if (overlapX <= 0f || overlapZ <= 0f) return false;
            var upperArea = Mathf.Max(0.0001f, upper.size.x * upper.size.z);
            return overlapX * overlapZ / upperArea >= 0.06f;
        }

        public static bool[] SupportedMask(IReadOnlyList<Bounds> displayedBounds)
        {
            if (displayedBounds == null) throw new ArgumentNullException(nameof(displayedBounds));
            var result = new bool[displayedBounds.Count];
            if (displayedBounds.Count == 0) return result;
            var floor = displayedBounds[0].min.y;
            var order = new List<int>(displayedBounds.Count);
            for (var i = 0; i < displayedBounds.Count; i++)
            {
                floor = Mathf.Min(floor, displayedBounds[i].min.y);
                order.Add(i);
            }
            order.Sort((left, right) => displayedBounds[left].min.y
                .CompareTo(displayedBounds[right].min.y));
            var supported = new List<int>();
            foreach (var index in order)
            {
                var candidate = displayedBounds[index];
                var grounded = candidate.min.y <= floor + Mathf.Max(0.08f, candidate.size.y * 0.12f);
                for (var i = 0; !grounded && i < supported.Count; i++)
                    grounded = Supports(displayedBounds[supported[i]], candidate);
                result[index] = grounded;
                if (grounded) supported.Add(index);
            }
            return result;
        }

        private static int SmallGroundedSource(Bounds[] bounds, bool[] keep, float floor,
            int seed, int supplement)
        {
            var candidates = new List<int>();
            for (var i = 0; i < bounds.Length; i++)
                if (keep[i] && bounds[i].min.y <= floor + Mathf.Max(0.08f, bounds[i].size.y * 0.12f))
                    candidates.Add(i);
            candidates.Sort((left, right) => BoundsVolume(bounds[left]).CompareTo(BoundsVolume(bounds[right])));
            if (candidates.Count == 0) return -1;
            var pool = Mathf.Min(8, candidates.Count);
            return candidates[Mathf.FloorToInt(Unit(seed, "spire-extra-source", supplement) * pool) % pool];
        }

        private static float HorizontalRadius(Vector3 value)
            => Mathf.Sqrt(value.x * value.x + value.z * value.z);

        private static float BoundsVolume(Bounds bounds)
            => bounds.size.x * bounds.size.y * bounds.size.z;

        private static Bounds TransformBounds(Bounds bounds, Matrix4x4 matrix)
        {
            var result = new Bounds(matrix.MultiplyPoint3x4(bounds.center), Vector3.zero);
            for (var c = 0; c < 8; c++)
                result.Encapsulate(matrix.MultiplyPoint3x4(bounds.center + Vector3.Scale(bounds.extents,
                    new Vector3((c & 1) == 0 ? -1 : 1, (c & 2) == 0 ? -1 : 1, (c & 4) == 0 ? -1 : 1))));
            return result;
        }

        private static float Unit(int seed, string id, int channel)
        {
            unchecked
            {
                uint hash = 2166136261u ^ (uint)seed ^ ((uint)channel * 486187739u);
                foreach (var c in id) hash = (hash ^ c) * 16777619u;
                hash ^= hash >> 16; hash *= 0x7feb352du; hash ^= hash >> 15;
                return (hash & 0xffffffu) / 16777216f;
            }
        }
    }
}
