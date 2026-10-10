using System;
using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Main-thread, read-only queries over the fixed native terrain grid.
    /// Rebuild this index if terrain ownership/transforms change; it never streams or edits terrain.</summary>
    public sealed class FixedTerrainSurfaceIndex
    {
        public readonly struct Sample
        {
            public readonly Vector3 Position;
            public readonly Vector3 Normal;
            public readonly Terrain Terrain;
            public float SlopeDegrees => Vector3.Angle(Normal, Vector3.up);
            public Sample(Vector3 position, Vector3 normal, Terrain terrain)
            { Position = position; Normal = normal; Terrain = terrain; }
        }

        private readonly Dictionary<Vector2Int, Terrain> tiles = new Dictionary<Vector2Int, Terrain>();
        private readonly float tileSize;
        public string Revision { get; }
        public int Count => tiles.Count;

        public FixedTerrainSurfaceIndex(IEnumerable<Terrain> sources, string revision, float size = 256f)
        {
            if (sources == null) throw new ArgumentNullException(nameof(sources));
            if (string.IsNullOrWhiteSpace(revision)) throw new ArgumentException("A terrain revision is required.", nameof(revision));
            if (float.IsNaN(size) || float.IsInfinity(size) || size <= 0f) throw new ArgumentOutOfRangeException(nameof(size));
            tileSize = size;
            Revision = revision;
            foreach (var terrain in sources)
            {
                if (terrain == null || terrain.terrainData == null) continue;
                var transform = terrain.transform;
                var dataSize = terrain.terrainData.size;
                var origin = transform.position;
                var cell = new Vector2Int(Mathf.RoundToInt(origin.x / size), Mathf.RoundToInt(origin.z / size));
                if (Quaternion.Angle(transform.rotation, Quaternion.identity) > .001f
                    || (transform.lossyScale - Vector3.one).sqrMagnitude > .000001f
                    || Mathf.Abs(dataSize.x - size) > .001f || Mathf.Abs(dataSize.z - size) > .001f
                    || Mathf.Abs(origin.x - cell.x * size) > .001f || Mathf.Abs(origin.z - cell.y * size) > .001f)
                    throw new ArgumentException("Fixed terrain must be unrotated, unit-scale, and aligned to the declared grid.");
                if (tiles.ContainsKey(cell)) throw new ArgumentException($"Duplicate fixed terrain cell {cell}.");
                tiles.Add(cell, terrain);
            }
        }

        public bool TrySample(Vector2 worldXZ, out Sample sample)
        {
            sample = default;
            if (float.IsNaN(worldXZ.x) || float.IsInfinity(worldXZ.x)
                || float.IsNaN(worldXZ.y) || float.IsInfinity(worldXZ.y)) return false;
            var cell = new Vector2Int(Mathf.FloorToInt(worldXZ.x / tileSize), Mathf.FloorToInt(worldXZ.y / tileSize));
            // Fixed priority makes seam ownership independent of enumeration order.
            // Neighbours are checked only at inclusive borders, including the outer edge.
            if (TryTile(cell, worldXZ, out sample)) return true;
            if (TryTile(cell + Vector2Int.left, worldXZ, out sample)) return true;
            if (TryTile(cell + Vector2Int.down, worldXZ, out sample)) return true;
            return TryTile(cell + Vector2Int.left + Vector2Int.down, worldXZ, out sample);
        }

        private bool TryTile(Vector2Int cell, Vector2 xz, out Sample sample)
        {
            sample = default;
            if (!tiles.TryGetValue(cell, out var terrain) || terrain == null
                || !terrain.enabled || !terrain.gameObject.activeInHierarchy || terrain.terrainData == null) return false;
            var collider = terrain.GetComponent<TerrainCollider>();
            if (collider == null || !collider.enabled || collider.terrainData != terrain.terrainData) return false;
            var origin = terrain.transform.position;
            var size = terrain.terrainData.size;
            if (xz.x < origin.x || xz.x > origin.x + size.x || xz.y < origin.z || xz.y > origin.z + size.z) return false;
            var ray = new Ray(new Vector3(xz.x, origin.y + size.y + 1f, xz.y), Vector3.down);
            if (!collider.Raycast(ray, out var hit, size.y + 2f))
            {
                // PhysX can omit a ray exactly on an outer triangle edge. Probe just inside
                // that same tile only at a true border; never bridge a hole or outside point.
                bool border = xz.x == origin.x || xz.x == origin.x + size.x
                    || xz.y == origin.z || xz.y == origin.z + size.z;
                if (!border) return false;
                ray.origin = new Vector3(Mathf.Clamp(xz.x, origin.x + .001f, origin.x + size.x - .001f),
                    ray.origin.y, Mathf.Clamp(xz.y, origin.z + .001f, origin.z + size.z - .001f));
                if (!collider.Raycast(ray, out hit, size.y + 2f)) return false;
                hit.point = new Vector3(xz.x, hit.point.y, xz.y);
            }
            // Collision is the grounding authority (including holes); smooth native normals guide art placement.
            var normal = terrain.terrainData.GetInterpolatedNormal((xz.x - origin.x) / size.x, (xz.y - origin.z) / size.z);
            sample = new Sample(hit.point, normal.normalized, terrain);
            return true;
        }
    }
}
