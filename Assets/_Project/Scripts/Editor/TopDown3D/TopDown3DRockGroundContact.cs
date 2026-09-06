using System;
using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.Editor
{
    /// <summary>Fits an authored arrangement to an existing surface without changing its source geometry.</summary>
    internal static class TopDown3DRockGroundContact
    {
        internal readonly struct Member
        {
            internal Member(Vector3 position, Quaternion rotation, Bounds bounds, Vector3[] vertices, int seed)
            {
                Position = position;
                Rotation = rotation;
                Bounds = bounds;
                Vertices = vertices;
                Seed = seed;
            }

            internal Vector3 Position { get; }
            internal Quaternion Rotation { get; }
            internal Bounds Bounds { get; }
            internal Vector3[] Vertices { get; }
            internal int Seed { get; }
        }

        internal readonly struct Pose
        {
            internal Pose(Vector3 position, Quaternion rotation, int group)
            {
                Position = position;
                Rotation = rotation;
                Group = group;
            }

            internal Vector3 Position { get; }
            internal Quaternion Rotation { get; }
            internal int Group { get; }
        }

        internal static Pose[] Fit(
            IReadOnlyList<Member> members,
            Func<Vector3, float> sampleHeight,
            Func<Vector3, Vector3> sampleNormal,
            float minimumBurial,
            float maximumBurial,
            float maximumTilt)
        {
            if (members == null) throw new ArgumentNullException(nameof(members));
            if (sampleHeight == null) throw new ArgumentNullException(nameof(sampleHeight));
            if (sampleNormal == null) throw new ArgumentNullException(nameof(sampleNormal));
            var groups = new int[members.Count];
            var poses = new Pose[members.Count];
            for (var i = 0; i < members.Count; i++)
            {
                if (members[i].Vertices == null || members[i].Vertices.Length == 0)
                    throw new ArgumentException("Every rock needs its actual mesh vertices.", nameof(members));
                groups[i] = i;
            }

            // Conservative contact groups preserve hand-built stacks and touching pairs.
            // The input order is the saved hierarchy order; no random or scene-global state is used.
            for (var i = 0; i < members.Count; i++)
            {
                var contactBounds = members[i].Bounds;
                contactBounds.Expand(0.04f);
                for (var j = i + 1; j < members.Count; j++)
                {
                    if (!contactBounds.Intersects(members[j].Bounds)) continue;
                    var from = groups[j];
                    var to = groups[i];
                    for (var k = 0; k < groups.Length; k++)
                        if (groups[k] == from) groups[k] = to;
                }
            }

            var completed = new HashSet<int>();
            for (var i = 0; i < members.Count; i++)
            {
                var group = groups[i];
                if (!completed.Add(group)) continue;
                var bounds = members[i].Bounds;
                for (var j = i + 1; j < members.Count; j++)
                    if (groups[j] == group) bounds.Encapsulate(members[j].Bounds);
                var pivot = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                var normal = sampleNormal(pivot).normalized;
                if (!Finite(normal) || normal.sqrMagnitude < 0.5f || normal.y <= 0f)
                    throw new InvalidOperationException("Ground contact requires a finite upward terrain normal.");
                var angle = Vector3.Angle(Vector3.up, normal);
                var tilt = Quaternion.Slerp(
                    Quaternion.identity, Quaternion.FromToRotation(Vector3.up, normal),
                    angle > 0.001f ? Mathf.Clamp01(maximumTilt / angle) : 0f);

                var lifts = new float[members.Count];
                var order = new List<int>();
                for (var j = i; j < members.Count; j++)
                {
                    if (groups[j] != group) continue;
                    order.Add(j);
                    var lift = float.NegativeInfinity;
                    var vertices = members[j].Vertices;
                    var stride = Mathf.Max(1, Mathf.CeilToInt(vertices.Length / 192f));
                    var lowest = vertices[0];
                    for (var v = 0; v < vertices.Length; v++)
                    {
                        if (vertices[v].y < lowest.y) lowest = vertices[v];
                        if (v % stride == 0)
                            lift = Mathf.Max(lift, RequiredLift(vertices[v], pivot, tilt, sampleHeight));
                    }
                    lift = Mathf.Max(lift, RequiredLift(lowest, pivot, tilt, sampleHeight));
                    lifts[j] = lift;
                }
                // Resolve lower rocks first. Separate ground-contact members settle independently;
                // upper members inherit the highest supporting displacement instead of flattening.
                order.Sort((a, b) =>
                {
                    var comparison = members[a].Bounds.min.y.CompareTo(members[b].Bounds.min.y);
                    return comparison != 0 ? comparison : a.CompareTo(b);
                });
                var displacements = new float[members.Count];
                for (var n = 0; n < order.Count; n++)
                {
                    var j = order[n];
                    // Narrow the sampling range for small rocks rather than clipping samples,
                    // which would pile up probability at the deep extreme.
                    var deep = Mathf.Min(maximumBurial, members[j].Bounds.size.y * 0.45f);
                    var shallow = Mathf.Min(minimumBurial, deep);
                    var depth = SampleBurial(members[j].Seed, shallow, deep);
                    var displacement = lifts[j] - depth;
                    for (var p = 0; p < n; p++)
                    {
                        var lower = order[p];
                        if (Supports(members[lower].Bounds, members[j].Bounds))
                            displacement = Mathf.Max(displacement, displacements[lower]);
                    }
                    displacements[j] = displacement;
                    poses[j] = new Pose(
                        pivot + tilt * (members[j].Position - pivot) + Vector3.up * displacement,
                        tilt * members[j].Rotation,
                        group);
                }
            }
            return poses;
        }

        private static bool Supports(Bounds lower, Bounds upper)
        {
            return upper.min.y > lower.min.y + 0.02f
                && upper.center.y > lower.center.y
                && lower.max.y >= upper.min.y - 0.04f
                && lower.min.x < upper.max.x && lower.max.x > upper.min.x
                && lower.min.z < upper.max.z && lower.max.z > upper.min.z;
        }

        internal static float SampleBurial(int seed, float minimum, float maximum)
        {
            minimum = Mathf.Clamp(minimum, 0f, 0.3f);
            maximum = Mathf.Clamp(maximum, minimum, 0.3f);
            // Same bounded bell-shaped construction as the accepted body-size distribution.
            // Local integer state only: independent of editor/global random state and terrain tile.
            var state = unchecked((uint)seed ^ 0xA511E9B3u);
            var sum = 0f;
            for (var i = 0; i < 3; i++)
            {
                unchecked
                {
                    state += 0x9E3779B9u;
                    var value = state;
                    value = (value ^ (value >> 16)) * 0x21F0AAADu;
                    value = (value ^ (value >> 15)) * 0x735A2D97u;
                    value ^= value >> 15;
                    sum += (value >> 8) / 16777215f;
                }
            }
            return Mathf.Lerp(minimum, maximum, sum / 3f);
        }

        private static float RequiredLift(
            Vector3 vertex, Vector3 pivot, Quaternion tilt, Func<Vector3, float> sampleHeight)
        {
            var point = pivot + tilt * (vertex - pivot);
            var height = sampleHeight(point);
            if (!Finite(point) || float.IsNaN(height) || float.IsInfinity(height))
                throw new InvalidOperationException("Ground contact received a non-finite surface sample.");
            return height - point.y;
        }

        private static bool Finite(Vector3 value)
        {
            return !float.IsNaN(value.x) && !float.IsInfinity(value.x)
                && !float.IsNaN(value.y) && !float.IsInfinity(value.y)
                && !float.IsNaN(value.z) && !float.IsInfinity(value.z);
        }
    }
}
