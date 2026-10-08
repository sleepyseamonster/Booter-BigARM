using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BooterBigArm.Editor
{
    // Also copied beside BadwaterUnitySceneBuilder in the isolated authoring project.
    public static class BadwaterHeightmapStitching
    {
        public const int RenderResolution = 257;

        public static float[,] DecodeSource(byte[] raw, int samples, int row, int column, bool retainedFocusCompatibility = true)
        {
            if (raw.Length != samples * samples * 2)
                throw new ArgumentException("Unexpected RAW byte count.", nameof(raw));
            var heights = new float[samples, samples];
            for (int north = 0; north < samples; north++)
            for (int x = 0; x < samples; x++)
            {
                int offset = (north * samples + x) * 2;
                heights[samples - 1 - north, x] = (raw[offset] | raw[offset + 1] << 8) / 65535f;
            }
            // Retain the original 1m focus perimeter's interpolation to the 2m DEM.
            if (samples == 257 && retainedFocusCompatibility)
            {
                for (int i = 1; i < 256; i += 2)
                {
                    if (row == 12) heights[256, i] = (heights[256, i - 1] + heights[256, i + 1]) * .5f;
                    if (row == 13) heights[0, i] = (heights[0, i - 1] + heights[0, i + 1]) * .5f;
                    if (column == 1) heights[i, 0] = (heights[i - 1, 0] + heights[i + 1, 0]) * .5f;
                    if (column == 2) heights[i, 256] = (heights[i - 1, 256] + heights[i + 1, 256]) * .5f;
                }
            }
            return heights;
        }

        public static float[,] PromoteContextGrid(float[,] source)
        {
            int size = source.GetLength(0);
            if (source.GetLength(1) != size || (size != 129 && size != RenderResolution))
                throw new ArgumentException("Expected a square 129 or 257 sample heightmap.", nameof(source));
            if (size == RenderResolution) return (float[,])source.Clone();
            var result = new float[RenderResolution, RenderResolution];
            for (int z = 0; z < RenderResolution; z++)
            for (int x = 0; x < RenderResolution; x++)
            {
                int x0 = x / 2, z0 = z / 2;
                int x1 = Math.Min(x0 + 1, size - 1), z1 = Math.Min(z0 + 1, size - 1);
                result[z, x] = Mathf.Lerp(
                    Mathf.Lerp(source[z0, x0], source[z0, x1], (x % 2) * .5f),
                    Mathf.Lerp(source[z1, x0], source[z1, x1], (x % 2) * .5f), (z % 2) * .5f);
            }
            return result;
        }

        public static void StitchBorders(IDictionary<Vector2Int, float[,]> tiles)
        {
            var shared = new Dictionary<Vector2Int, float>();
            // Stable geographic order, independent of dictionary insertion or scene fileIDs.
            foreach (var tile in tiles.OrderBy(pair => pair.Key.y).ThenBy(pair => pair.Key.x))
            {
                var heights = tile.Value;
                if (heights.GetLength(0) != RenderResolution || heights.GetLength(1) != RenderResolution)
                    throw new ArgumentException("All rendering grids must have 257 samples.", nameof(tiles));
                for (int i = 0; i < RenderResolution; i++)
                {
                    StitchVertex(shared, tile.Key, heights, i, 0);
                    StitchVertex(shared, tile.Key, heights, i, 256);
                    StitchVertex(shared, tile.Key, heights, 0, i);
                    StitchVertex(shared, tile.Key, heights, 256, i);
                }
            }
        }

        private static void StitchVertex(Dictionary<Vector2Int, float> shared, Vector2Int tile,
            float[,] heights, int x, int z)
        {
            var coordinate = new Vector2Int(tile.x * 256 + x, tile.y * 256 + z);
            if (shared.TryGetValue(coordinate, out float height)) heights[z, x] = height;
            else shared.Add(coordinate, heights[z, x]);
        }
    }
}
