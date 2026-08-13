using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public static class TopDown3DHeightSampler
    {
        public static float SampleHeight(TopDown3DWorldSettings settings, float worldX, float worldZ)
        {
            if (settings == null)
            {
                return 0f;
            }

            var amplitude = 1f;
            var frequency = settings.NoiseFrequency;
            var total = 0f;
            var weight = 0f;
            for (var octave = 0; octave < settings.NoiseOctaves; octave++)
            {
                var sample = ValueNoise(
                    settings.WorldSeed + octave * 1013,
                    worldX * frequency,
                    worldZ * frequency);
                total += ((sample * 2f) - 1f) * amplitude;
                weight += amplitude;
                amplitude *= settings.NoisePersistence;
                frequency *= settings.NoiseLacunarity;
            }

            var normalized = weight > 0f ? total / weight : 0f;
            return settings.BaseHeight + normalized * settings.HeightAmplitude;
        }

        public static Vector2Int WorldToChunk(TopDown3DWorldSettings settings, Vector3 worldPosition)
        {
            var size = settings != null ? Mathf.Max(0.01f, settings.ChunkSize) : 1f;
            return new Vector2Int(
                Mathf.FloorToInt(worldPosition.x / size),
                Mathf.FloorToInt(worldPosition.z / size));
        }

        public static int StableChunkSeed(int worldSeed, Vector2Int chunk)
        {
            unchecked
            {
                var hash = worldSeed;
                hash = hash * 397 ^ chunk.x;
                hash = hash * 397 ^ chunk.y;
                hash ^= hash >> 16;
                return hash;
            }
        }

        private static float ValueNoise(int seed, float x, float z)
        {
            var x0 = Mathf.FloorToInt(x);
            var z0 = Mathf.FloorToInt(z);
            var tx = Smooth(x - x0);
            var tz = Smooth(z - z0);
            var a = Hash01(seed, x0, z0);
            var b = Hash01(seed, x0 + 1, z0);
            var c = Hash01(seed, x0, z0 + 1);
            var d = Hash01(seed, x0 + 1, z0 + 1);
            return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), tz);
        }

        private static float Smooth(float value)
        {
            return value * value * (3f - 2f * value);
        }

        private static float Hash01(int seed, int x, int z)
        {
            unchecked
            {
                var hash = (uint)seed;
                hash ^= (uint)(x * 374761393);
                hash = (hash << 13) ^ hash;
                hash ^= (uint)(z * 668265263);
                hash *= 1274126177u;
                hash ^= hash >> 16;
                return (hash & 0x00FFFFFFu) / 16777215f;
            }
        }
    }
}
