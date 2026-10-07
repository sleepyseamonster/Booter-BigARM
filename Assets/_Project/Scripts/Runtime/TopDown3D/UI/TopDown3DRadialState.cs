using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public enum TopDown3DRadialCommand { Empty, PlayerInventory, CallLegger, DustCanister, AutoPack }

    [Serializable]
    public sealed class TopDown3DRadialPage
    {
        public string id = Guid.NewGuid().ToString("N");
        public string name = "Field";
        public TopDown3DRadialCommand[] slots = {
            TopDown3DRadialCommand.PlayerInventory, TopDown3DRadialCommand.Empty,
            TopDown3DRadialCommand.CallLegger, TopDown3DRadialCommand.DustCanister };
    }

    [Serializable]
    public sealed class TopDown3DRadialPreferences
    {
        public int version = 1;
        public TopDown3DRadialPage[] pages = { new TopDown3DRadialPage() };
        public int defaultPage;
        public bool toggleOpen;
        public bool explicitConfirm;
        public float scale = 1f;

        public bool IsValid()
        {
            if (version != 1 || pages == null || pages.Length < 1 || pages.Length > 4
                || defaultPage < 0 || defaultPage >= pages.Length || float.IsNaN(scale)
                || float.IsInfinity(scale) || scale < 0.85f || scale > 1.35f) return false;
            var ids = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            foreach (var p in pages)
            {
                if (p == null || string.IsNullOrWhiteSpace(p.id) || !ids.Add(p.id)
                    || string.IsNullOrWhiteSpace(p.name) || p.name.Length > 24 || p.slots == null
                    || (p.slots.Length != 4 && p.slots.Length != 8)) return false;
                foreach (var command in p.slots) if (!Enum.IsDefined(typeof(TopDown3DRadialCommand), command)) return false;
            }
            return true;
        }

        public TopDown3DRadialPreferences Clone() => JsonUtility.FromJson<TopDown3DRadialPreferences>(JsonUtility.ToJson(this));
    }

    public static class TopDown3DRadialGeometry
    {
        // Half the gap is the perpendicular distance from either side to its radial seam.
        public static Vector2 EdgePoint(float radius, float boundaryDegrees, float gap, bool after)
        {
            var trim = Mathf.Asin(Mathf.Clamp(gap * 0.5f / radius, 0f, 0.99f)) * Mathf.Rad2Deg;
            return Point(radius, boundaryDegrees + (after ? trim : -trim));
        }

        public static Vector2 Point(float radius, float degrees)
        {
            var a = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * radius;
        }

        public static int Select(Vector2 direction, int count, int previous = -1, float neutral = 0.22f)
        {
            if (direction.sqrMagnitude < neutral * neutral) return -1;
            var angle = Mathf.Repeat(Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg, 360f);
            var width = 360f / count;
            if (previous >= 0 && Mathf.Abs(Mathf.DeltaAngle(previous * width, angle)) <= width * 0.5f + 5f)
                return previous;
            return Mathf.FloorToInt(Mathf.Repeat(angle + width * 0.5f, 360f) / width);
        }
    }
}
