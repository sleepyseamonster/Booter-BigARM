using System;
using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Bounded deterministic selection on existing collision terrain. Never edits terrain.</summary>
    public static class RootBeaconArrival
    {
        public readonly struct Site
        {
            public readonly Vector3 Player, Beacon, Companion;
            public Site(Vector3 player, Vector3 beacon, Vector3 companion)
            { Player = player; Beacon = beacon; Companion = companion; }
        }

        public static bool TryChoose(IEnumerable<Terrain> sources, int seed, TopDown3DPlayerMotor player,
            TopDown3DBigArmFollower companion, out Site site)
        {
            site = default;
            if (sources == null || player == null) return false;
            var terrains = new List<Terrain>();
            foreach (var t in sources)
                if (t != null && t.isActiveAndEnabled && t.terrainData != null
                    && t.GetComponent<TerrainCollider>() is { enabled: true } c && c.terrainData == t.terrainData
                    && Quaternion.Angle(t.transform.rotation, Quaternion.identity) < .001f
                    && (t.transform.lossyScale - Vector3.one).sqrMagnitude < .000001f
                    && t.terrainData.size.x > 24 && t.terrainData.size.z > 24) terrains.Add(t);
            terrains.Sort((a, b) =>
            {
                int x = a.transform.position.x.CompareTo(b.transform.position.x);
                return x != 0 ? x : a.transform.position.z.CompareTo(b.transform.position.z);
            });
            if (terrains.Count == 0) return false;
            var random = new System.Random(seed);
            double totalArea = 0;
            foreach (var t in terrains) totalArea += (t.terrainData.size.x - 24) * (t.terrainData.size.z - 24);
            for (int attempt = 0; attempt < 256; attempt++)
            {
                double pick = random.NextDouble() * totalArea;
                var terrain = terrains[terrains.Count - 1];
                foreach (var t in terrains)
                {
                    pick -= (t.terrainData.size.x - 24) * (t.terrainData.size.z - 24);
                    if (pick <= 0) { terrain = t; break; }
                }
                var origin = terrain.transform.position; var size = terrain.terrainData.size;
                var point = origin + new Vector3(12 + (float)random.NextDouble() * (size.x - 24), 0,
                    12 + (float)random.NextDouble() * (size.z - 24));
                var collider = terrain.GetComponent<TerrainCollider>();
                if (!Support(terrain, collider, point, out var ground)) continue;
                // A supported local patch, including the walking approach and companion footprint.
                bool supported = true;
                for (int i = 0; i < 16; i++)
                {
                    float angle = i * Mathf.PI / 8;
                    if (!Support(terrain, collider, ground + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 8, out var rim)
                        || Mathf.Abs(rim.y - ground.y) > 2.5f) { supported = false; break; }
                }
                if (!supported || !Support(terrain, collider, ground + Vector3.back * 2, out var beacon)
                    || !Support(terrain, collider, ground + Vector3.right * 5, out var legger)) continue;
                if (player.PopulationPreparation != null && !player.PopulationPreparation(new Bounds(ground, new Vector3(24, 0, 24)))) continue;
                if (!PatchClear(ground, player, companion)) continue;
                if (!ClearActor(player.GetComponent<Collider>(), player.transform, ground, player, companion)) continue;
                if (companion != null && !ClearActor(companion.GetComponent<Collider>(), companion.transform, legger, player, companion)) continue;
                // Sample the complete short approaches, rather than trusting only their endpoints.
                for (int i = 0; i <= 10; i++)
                {
                    var p = Vector3.Lerp(beacon, legger, i / 10f);
                    if (!Support(terrain, collider, p, out var step) || Mathf.Abs(step.y - ground.y) > 2.5f)
                    { supported = false; break; }
                }
                if (supported) { site = new Site(ground, beacon, legger); return true; }
            }
            return false; // No unsafe fallback to an arbitrary coordinate or unsupported authored spawn.
        }

        private static bool Support(Terrain terrain, TerrainCollider collider, Vector3 p, out Vector3 ground)
        {
            ground = default; var o = terrain.transform.position; var size = terrain.terrainData.size;
            if (p.x < o.x || p.z < o.z || p.x > o.x + size.x || p.z > o.z + size.z) return false;
            if (!collider.Raycast(new Ray(new Vector3(p.x, o.y + size.y + 2, p.z), Vector3.down), out var hit, size.y + 4)
                || Vector3.Angle(hit.normal, Vector3.up) > 20) return false;
            ground = hit.point; return true;
        }

        private static bool PatchClear(Vector3 ground, TopDown3DPlayerMotor player, TopDown3DBigArmFollower companion)
        {
            foreach (var c in Physics.OverlapBox(ground + Vector3.up * 2.5f, new Vector3(8, 2.3f, 8), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                if (!Ignored(c, player, companion)) return false;
            return true;
        }

        private static bool ClearActor(Collider shape, Transform actor, Vector3 ground, TopDown3DPlayerMotor player, TopDown3DBigArmFollower companion)
        {
            if (shape == null || !shape.enabled) return false;
            var bounds = shape.bounds;
            var center = StandingPosition(shape, ground) + bounds.center - actor.position;
            foreach (var c in Physics.OverlapBox(center, bounds.extents + Vector3.one * .08f, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                if (!Ignored(c, player, companion)) return false;
            return true;
        }

        // Camp Loop's calibrated capsule is centred on its root, not on its feet.
        // Use the actual world-space collider bottom rather than assuming a pivot convention.
        public static Vector3 StandingPosition(Collider shape, Vector3 ground) => ground
            + Vector3.up * (shape.transform.position.y - shape.bounds.min.y + .12f);

        private static bool Ignored(Collider c, TopDown3DPlayerMotor player, TopDown3DBigArmFollower companion) => c is TerrainCollider
            || c.transform.IsChildOf(player.transform) || companion != null && c.transform.IsChildOf(companion.transform);
    }
}
