using System;
using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Owns only Terrain.enabled. Native data, collision, neighbours and object activity stay resident.</summary>
    public sealed class FixedTerrainRenderWindow : IDisposable
    {
        private sealed class Tile
        {
            public Terrain Terrain;
            public Bounds Bounds;
            public bool OriginalEnabled;
        }

        private const float SectorSize = 1024f;
        private readonly List<Tile> tiles = new();
        private readonly Dictionary<Vector2Int, List<Tile>> sectors = new();
        private readonly HashSet<Tile> wanted = new();
        private readonly HashSet<Tile> active = new();
        private readonly List<Tile> removals = new();
        private bool initialized;
        private bool disposed;
        public Bounds LandscapeBounds { get; private set; }
        public int Count => tiles.Count;
        public int EnabledCount => active.Count;

        public FixedTerrainRenderWindow(IEnumerable<Terrain> sources)
        {
            if (sources == null) throw new ArgumentNullException(nameof(sources));
            var seen = new HashSet<Terrain>();
            foreach (var terrain in sources)
            {
                if (terrain == null || terrain.terrainData == null) continue;
                if (!seen.Add(terrain)) throw new ArgumentException("Duplicate terrain ownership.", nameof(sources));
                var transform = terrain.transform;
                var size = terrain.terrainData.size;
                if (Quaternion.Angle(transform.rotation, Quaternion.identity) > .001f
                    || (transform.lossyScale - Vector3.one).sqrMagnitude > .000001f
                    || Mathf.Abs(size.x - 256f) > .001f || Mathf.Abs(size.z - 256f) > .001f)
                    throw new ArgumentException("Render window requires the native unrotated 256 m terrain grid.", nameof(sources));
                var bounds = terrain.terrainData.bounds;
                bounds.center += transform.position;
                var tile = new Tile { Terrain = terrain, Bounds = bounds, OriginalEnabled = terrain.enabled };
                if (tiles.Count == 0) LandscapeBounds = bounds;
                else { var landscape = LandscapeBounds; landscape.Encapsulate(bounds); LandscapeBounds = landscape; }
                tiles.Add(tile);
                // Index every intersected sector, including unaligned tiles and negative coordinates.
                var min = Sector(bounds.min); var max = Sector(bounds.max);
                for (int z = min.y; z <= max.y; z++) for (int x = min.x; x <= max.x; x++)
                {
                    var key = new Vector2Int(x, z);
                    if (!sectors.TryGetValue(key, out var list)) sectors.Add(key, list = new List<Tile>());
                    list.Add(tile);
                }
                if (tile.OriginalEnabled) active.Add(tile);
            }
        }

        /// <summary>Visible arrivals are immediate; only departures are budgeted. Empty coverage fails open.</summary>
        public void Update(IReadOnlyList<Bounds> coverage, float retentionMargin = 256f, int departuresPerUpdate = 16)
        {
            if (disposed) throw new ObjectDisposedException(nameof(FixedTerrainRenderWindow));
            if (coverage == null || coverage.Count == 0) { if (initialized) Restore(); return; }
            if (!float.IsFinite(retentionMargin) || retentionMargin < 0 || departuresPerUpdate < 0)
                throw new ArgumentOutOfRangeException(nameof(retentionMargin));
            wanted.Clear();
            for (int i = 0; i < coverage.Count; i++)
            {
                var bounds = coverage[i];
                if (!Finite(bounds.min) || !Finite(bounds.max)) { Restore(); return; }
                Select(bounds);
            }
            foreach (var tile in wanted)
            {
                if (tile.Terrain == null || !tile.OriginalEnabled) continue;
                if (!tile.Terrain.enabled) tile.Terrain.enabled = true;
                active.Add(tile);
            }
            removals.Clear();
            foreach (var tile in active)
            {
                if (wanted.Contains(tile)) continue;
                bool retained = false;
                if (initialized && tile.Terrain != null)
                    for (int i = 0; i < coverage.Count; i++)
                    {
                        var bounds = coverage[i]; bounds.Expand(new Vector3(retentionMargin * 2, 0, retentionMargin * 2));
                        if (IntersectsXZ(bounds, tile.Bounds)) { retained = true; break; }
                    }
                if (!retained) removals.Add(tile);
            }
            // First coverage drops the original full-world set before rendering. Later exits are bounded.
            int limit = initialized ? Math.Min(departuresPerUpdate, removals.Count) : removals.Count;
            for (int i = 0; i < limit; i++)
            {
                var tile = removals[i];
                if (tile.Terrain != null) tile.Terrain.enabled = false;
                active.Remove(tile);
            }
            initialized = true;
        }

        private void Select(Bounds bounds)
        {
            if (!IntersectsXZ(bounds, LandscapeBounds)) return;
            // Clip huge but finite camera projections to the actual landscape before converting to cells.
            var low = bounds.min; var high = bounds.max;
            low.x = Mathf.Max(low.x, LandscapeBounds.min.x); low.z = Mathf.Max(low.z, LandscapeBounds.min.z);
            high.x = Mathf.Min(high.x, LandscapeBounds.max.x); high.z = Mathf.Min(high.z, LandscapeBounds.max.z);
            bounds.SetMinMax(low, high);
            var min = Sector(bounds.min); var max = Sector(bounds.max);
            long area = ((long)max.x - min.x + 1) * ((long)max.y - min.y + 1);
            if (area > sectors.Count * 4L)
            {
                // Horizon shadows or very distant cameras must not enumerate an unbounded empty grid.
                foreach (var pair in sectors)
                {
                    var sector = new Bounds(new Vector3((pair.Key.x + .5f) * SectorSize, 0,
                        (pair.Key.y + .5f) * SectorSize), new Vector3(SectorSize, 0, SectorSize));
                    if (IntersectsXZ(bounds, sector)) SelectTiles(pair.Value, bounds);
                }
                return;
            }
            for (int z = min.y; z <= max.y; z++) for (int x = min.x; x <= max.x; x++)
                if (sectors.TryGetValue(new Vector2Int(x, z), out var list)) SelectTiles(list, bounds);
        }

        private void SelectTiles(List<Tile> list, Bounds bounds)
        {
            foreach (var tile in list) if (tile.OriginalEnabled && IntersectsXZ(bounds, tile.Bounds)) wanted.Add(tile);
        }

        public void Restore()
        {
            active.Clear();
            foreach (var tile in tiles)
            {
                if (tile.Terrain == null) continue;
                if (tile.Terrain.enabled != tile.OriginalEnabled) tile.Terrain.enabled = tile.OriginalEnabled;
                if (tile.OriginalEnabled) active.Add(tile);
            }
            initialized = false;
        }

        public void Dispose() { if (disposed) return; Restore(); disposed = true; }
        public static bool IntersectsXZ(Bounds a, Bounds b) => a.min.x <= b.max.x && a.max.x >= b.min.x
            && a.min.z <= b.max.z && a.max.z >= b.min.z;
        private static Vector2Int Sector(Vector3 position) => new(Mathf.FloorToInt(position.x / SectorSize), Mathf.FloorToInt(position.z / SectorSize));
        private static bool Finite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
