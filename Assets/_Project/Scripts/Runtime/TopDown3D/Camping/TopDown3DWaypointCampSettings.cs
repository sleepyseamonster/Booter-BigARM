using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [CreateAssetMenu(menuName = "Booter & BigARM/Waypoint Camp")]
    public sealed class TopDown3DWaypointCampSettings : ScriptableObject
    {
        public const string ProductionScene = "Assets/_Project/Scenes/Production/GreaterWasteland.unity";
        public string worldId = "greater-wasteland.authored.v1";
        public string siteId = "camp.starting";
        public string displayName = "Starting Camp";
        public Vector3 position = new(-1842, -48.08643f, -1498);
        public Vector3[] returnRoute = {
            new(-1845, -48.30617f, -1498), new(-1845, -47.53708f, -1490),
            new(-1845, -46.9328f, -1482), new(-1845, -45.94396f, -1474), new(-1845, -45.39462f, -1466) };
        [Min(.1f)] public float corridorRadius = 1.5f;
        [Range(1, 4)] public float travelSpeed = 4;
        [Range(.05f, .5f)] public float resourceFloor = .1f;
        [Min(.1f)] public float walkingSpeed = 1.8f;
        public static TopDown3DWaypointCampSettings Load() => Resources.Load<TopDown3DWaypointCampSettings>("TopDown3DWaypointCampSettings");
        public bool TryValidate(out string reason)
        {
            reason = "Waypoint camp definition is incomplete";
            if (string.IsNullOrWhiteSpace(worldId) || string.IsNullOrWhiteSpace(siteId) || string.IsNullOrWhiteSpace(displayName)
                || !Finite(position) || returnRoute == null || returnRoute.Length < 2) return false;
            if (!float.IsFinite(corridorRadius + travelSpeed + resourceFloor + walkingSpeed)
                || corridorRadius < .1f || corridorRadius > 3 || travelSpeed < 1 || travelSpeed > 4
                || resourceFloor <= 0 || resourceFloor >= 1 || walkingSpeed <= 0 || walkingSpeed > 1.8f) return false;
            for (var i = 0; i < returnRoute.Length; i++)
                if (!Finite(returnRoute[i]) || (i > 0 && Vector3.Distance(returnRoute[i - 1], returnRoute[i]) < .1f)) return false;
            reason = null; return true;
        }
        private static bool Finite(Vector3 v) => float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
    }

    // Logical discovery survives view rebuilding within this session. It is not a disk save owner.
    public sealed class TopDown3DWaypointCampRegistry
    {
        public string WorldId { get; }
        public string SiteId { get; }
        public bool Discovered { get; private set; }
        public TopDown3DWaypointCampRegistry(string worldId, string siteId, bool initiallyKnown)
        {
            if (string.IsNullOrWhiteSpace(worldId) || string.IsNullOrWhiteSpace(siteId)) throw new ArgumentException("Camp identity is required");
            WorldId = worldId; SiteId = siteId; Discovered = initiallyKnown;
        }
        public bool Discover(string worldId, string siteId)
        { if (worldId != WorldId || siteId != SiteId) return false; Discovered = true; return true; }
    }

    public static class TopDown3DWaypointRoute
    {
        public static bool TryReturn(Vector3 start, Vector3[] route, float radius, out Vector3[] path, out float distance)
        {
            path = null; distance = 0;
            if (route == null || route.Length < 2 || !float.IsFinite(radius) || radius <= 0
                || !float.IsFinite(start.x) || !float.IsFinite(start.y) || !float.IsFinite(start.z)) return false;
            var nearest = float.PositiveInfinity; var segment = -1; var projection = start;
            for (var i = 1; i < route.Length; i++)
            {
                var a = route[i - 1]; var b = route[i]; var ab = Vector3.ProjectOnPlane(b - a, Vector3.up);
                if (!float.IsFinite(a.x + a.y + a.z + b.x + b.y + b.z) || ab.sqrMagnitude < .0001f) return false;
                var t = Mathf.Clamp01(Vector3.Dot(Vector3.ProjectOnPlane(start - a, Vector3.up), ab) / ab.sqrMagnitude);
                var p = Vector3.Lerp(a, b, t); var delta = Vector3.ProjectOnPlane(start - p, Vector3.up).sqrMagnitude;
                if (delta >= nearest) continue; nearest = delta; segment = i - 1; projection = p;
            }
            if (segment < 0 || nearest > radius * radius || Mathf.Abs(start.y - projection.y) > 2.5f) return false;
            var points = new System.Collections.Generic.List<Vector3>();
            if (Vector3.ProjectOnPlane(start - projection, Vector3.up).magnitude > .15f) points.Add(projection);
            for (var i = segment; i >= 0; i--) points.Add(route[i]);
            var previous = start; foreach (var point in points) { distance += Vector3.Distance(previous, point); previous = point; }
            path = points.ToArray(); return true;
        }
    }
}
