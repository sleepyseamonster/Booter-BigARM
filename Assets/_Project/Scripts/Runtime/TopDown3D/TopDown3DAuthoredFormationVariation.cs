using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Seeded composition transforms; terrain fitting remains the placement owner's job.</summary>
    public static class TopDown3DAuthoredFormationVariation
    {
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
