using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    /// <summary>
    /// Instance-owned deterministic authority for terrain form and geological surface data.
    /// It samples world coordinates directly so chunk and regional borders share exact values.
    /// </summary>
    public sealed class TopDown3DWorldGenerator
    {
        private const float NormalSampleDistance = 0.75f;
        private const int TerrainSeedSalt = 0x2f6e2b1;
        private const int SandTrapSeedSalt = 0x51cc8f6;

        private readonly int worldSeed;
        private readonly int generationVersion;
        private readonly int sandTrapSeed;
        private readonly float baseHeight;
        private readonly TopDown3DGeologyProfile geology;

        public TopDown3DWorldGenerator(TopDown3DWorldSettings settings)
        {
            worldSeed = settings != null ? settings.WorldSeed : 0;
            generationVersion = settings != null ? settings.TerrainGenerationVersion : 1;
            sandTrapSeed = StableHash(worldSeed, generationVersion, SandTrapSeedSalt);
            baseHeight = settings != null ? settings.BaseHeight : 0f;
            geology = settings != null && settings.GeologyProfile != null
                ? settings.GeologyProfile
                : new TopDown3DGeologyProfile();
        }

        public int WorldSeed => worldSeed;
        public int GenerationVersion => generationVersion;
        public float RegionSize => geology.RegionSize;

        public TopDown3DWorldSurfaceSample Sample(float worldX, float worldZ)
        {
            var center = SampleCore(worldX, worldZ);
            var left = SampleCore(worldX - NormalSampleDistance, worldZ).Height;
            var right = SampleCore(worldX + NormalSampleDistance, worldZ).Height;
            var back = SampleCore(worldX, worldZ - NormalSampleDistance).Height;
            var forward = SampleCore(worldX, worldZ + NormalSampleDistance).Height;
            var tangentX = new Vector3(NormalSampleDistance * 2f, right - left, 0f);
            var tangentZ = new Vector3(0f, forward - back, NormalSampleDistance * 2f);
            var normal = Vector3.Cross(tangentZ, tangentX).normalized;

            var curvature = Mathf.Clamp(
                ((left + right + back + forward) * 0.25f - center.Height) / 2.5f,
                -1f,
                1f);
            var slope = 1f - Mathf.Clamp01(normal.y);
            var talus = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.InverseLerp(geology.TalusSlopeStart, geology.TalusSlopeEnd, slope));
            var deposit = Mathf.Clamp01(
                center.SandTrap * 0.72f
                + center.Flow * 0.18f
                + Mathf.Max(0f, curvature) * 0.20f
                + talus * (1f - center.Flow) * 0.12f);
            var steepBedrock = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.InverseLerp(0.16f, 0.42f, slope));
            var patchBedrock = center.RockExposure
                * Mathf.Lerp(0.42f, 1f, center.MesaExposure);
            var bedrock = Mathf.Clamp01(
                Mathf.Max(steepBedrock, patchBedrock)
                - deposit * 0.6f);
            var gravel = Mathf.Clamp01(
                talus * 0.8f
                + Mathf.Abs(curvature) * 0.24f
                + (1f - center.Flow) * center.Weathering * 0.10f);
            var flatness = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.92f, 0.995f, normal.y));
            var sand = Mathf.Clamp01(
                center.SandTrap
                * Mathf.Lerp(0.68f, 1f, flatness)
                * (1f - talus * 0.72f));
            var sandExclusion = 1f - sand * 0.88f;
            gravel *= sandExclusion;
            bedrock *= sandExclusion;
            var corridor = Mathf.Clamp01(
                center.Flow
                * Mathf.Lerp(0.62f, 1f, geology.CorridorWidth)
                * (1f - Mathf.Clamp01(slope * 3.5f)));

            return new TopDown3DWorldSurfaceSample(
                center.Height,
                normal,
                center.FeatureId,
                sand,
                gravel,
                bedrock,
                deposit,
                center.Flow,
                curvature,
                talus,
                center.Lithology,
                center.Weathering,
                corridor);
        }

        public float SampleHeight(float worldX, float worldZ)
        {
            return SampleCore(worldX, worldZ).Height;
        }

        public Vector3 SampleNormal(float worldX, float worldZ, float sampleDistance = NormalSampleDistance)
        {
            var distance = Mathf.Max(0.05f, sampleDistance);
            var left = SampleHeight(worldX - distance, worldZ);
            var right = SampleHeight(worldX + distance, worldZ);
            var back = SampleHeight(worldX, worldZ - distance);
            var forward = SampleHeight(worldX, worldZ + distance);
            var tangentX = new Vector3(distance * 2f, right - left, 0f);
            var tangentZ = new Vector3(0f, forward - back, distance * 2f);
            return Vector3.Cross(tangentZ, tangentX).normalized;
        }

        public Vector2Int WorldToChunk(TopDown3DWorldSettings settings, Vector3 worldPosition)
        {
            var size = settings != null ? Mathf.Max(0.01f, settings.ChunkSize) : 1f;
            return new Vector2Int(
                Mathf.FloorToInt(worldPosition.x / size),
                Mathf.FloorToInt(worldPosition.z / size));
        }

        public bool TryFindWalkablePosition(
            Vector2 desiredPosition,
            float searchRadius,
            float searchStep,
            float maximumSlopeDegrees,
            out Vector3 position)
        {
            position = new Vector3(
                desiredPosition.x,
                SampleHeight(desiredPosition.x, desiredPosition.y),
                desiredPosition.y);
            var step = Mathf.Max(0.25f, searchStep);
            var rings = Mathf.Max(0, Mathf.CeilToInt(Mathf.Max(0f, searchRadius) / step));
            for (var ring = 0; ring <= rings; ring++)
            {
                for (var z = -ring; z <= ring; z++)
                {
                    for (var x = -ring; x <= ring; x++)
                    {
                        if (ring > 0 && Mathf.Abs(x) != ring && Mathf.Abs(z) != ring)
                        {
                            continue;
                        }

                        var worldX = desiredPosition.x + x * step;
                        var worldZ = desiredPosition.y + z * step;
                        var normal = SampleNormal(worldX, worldZ, step * 0.5f);
                        if (Vector3.Angle(normal, Vector3.up) > maximumSlopeDegrees)
                        {
                            continue;
                        }

                        position = new Vector3(worldX, SampleHeight(worldX, worldZ), worldZ);
                        return true;
                    }
                }
            }

            return false;
        }

        public static int StableChunkSeed(int worldSeed, Vector2Int chunk)
        {
            return StableHash(worldSeed, chunk.x, chunk.y);
        }

        private CoreSample SampleCore(float worldX, float worldZ)
        {
            var seed = StableHash(worldSeed, generationVersion, TerrainSeedSalt);
            var regionSize = geology.RegionSize;
            var broadX = worldX / regionSize;
            var broadZ = worldZ / regionSize;
            var warpX = (FractalNoise(seed + 101, broadX * 0.72f, broadZ * 0.72f, 3) - 0.5f) * 0.9f;
            var warpZ = (FractalNoise(seed + 211, broadX * 0.72f, broadZ * 0.72f, 3) - 0.5f) * 0.9f;
            var warpedX = broadX + warpX;
            var warpedZ = broadZ + warpZ;

            var basinSignal = FractalNoise(seed + 307, warpedX * 0.72f, warpedZ * 0.72f, 4) * 2f - 1f;
            var basin = basinSignal * geology.BasinRelief;

            var ridgeNoise = FractalNoise(seed + 401, warpedX * 2.25f, warpedZ * 2.25f, 4);
            var ridge = 1f - Mathf.Abs(ridgeNoise * 2f - 1f);
            ridge = Mathf.Pow(Mathf.Clamp01(ridge), 2.7f);
            var ridgeMask = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.35f, 0.55f, basinSignal));
            var ridgeHeight = ridge * ridgeMask * geology.RidgeRelief;

            var mesaSignal = FractalNoise(seed + 503, warpedX * 1.38f, warpedZ * 1.38f, 3);
            var mesaMask = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.58f, 0.76f, mesaSignal));
            var mesaTopNoise = FractalNoise(seed + 601, warpedX * 4.2f, warpedZ * 4.2f, 2);
            var mesaHeight = mesaMask * geology.MesaRelief * Mathf.Lerp(0.86f, 1.08f, mesaTopNoise);

            var flow = SampleDrainage(seed, worldX, worldZ, regionSize, geology.DrainageWidth);
            var drainageCarve = flow * geology.DrainageDepth * Mathf.Lerp(0.55f, 1f, ridgeMask);

            var preTerraceHeight = baseHeight + basin + ridgeHeight + mesaHeight - drainageCarve;
            var terraceStep = geology.TerraceStepHeight;
            var terraceHeight = preTerraceHeight;
            if (terraceStep > 0.001f)
            {
                var terraced = ShapeErodedTerrace(preTerraceHeight, terraceStep);
                var terraceMask = Mathf.Clamp01(mesaMask * 0.82f + ridgeMask * ridge * 0.35f);
                terraceHeight = Mathf.Lerp(
                    preTerraceHeight,
                    terraced,
                    terraceMask * geology.TerraceStrength);
            }

            var localNoise = FractalNoise(
                seed + 701,
                worldX / Mathf.Max(8f, regionSize * 0.075f),
                worldZ / Mathf.Max(8f, regionSize * 0.075f),
                4) * 2f - 1f;
            var lowReliefMask = Mathf.Clamp01(0.3f + ridgeMask * 0.7f - flow * 0.42f);
            var sandTrap = SampleSandTrapMask(sandTrapSeed, worldX, worldZ);
            var sandTrapDepression = sandTrap * (0.34f + sandTrap * 0.22f);
            var height = terraceHeight
                + localNoise * geology.LocalRelief * lowReliefMask
                - sandTrapDepression;

            var lithology = FractalNoise(seed + 809, broadX * 3.1f, broadZ * 3.1f, 3);
            var weathering = Mathf.Clamp01(
                0.24f
                + FractalNoise(seed + 907, broadX * 7.2f, broadZ * 7.2f, 3) * 0.62f
                + flow * 0.12f);
            var rockPatchNoise = FractalNoise(
                seed + 1459,
                worldX / 24f,
                worldZ / 24f,
                3);
            var rockExposure = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.InverseLerp(
                    0.73f,
                    0.88f,
                    rockPatchNoise + (lithology - 0.5f) * 0.10f));
            var regionX = Mathf.FloorToInt(worldX / regionSize);
            var regionZ = Mathf.FloorToInt(worldZ / regionSize);
            var featureId = StableHash(seed, regionX, regionZ);

            return new CoreSample(
                height,
                featureId,
                flow,
                mesaMask,
                lithology,
                weathering,
                sandTrap,
                rockExposure);
        }

        internal static float ShapeErodedTerrace(float height, float stepHeight)
        {
            if (stepHeight <= 0.001f)
            {
                return height;
            }

            var normalizedHeight = height / stepHeight;
            var lowerLevel = Mathf.Floor(normalizedHeight);
            var levelProgress = normalizedHeight - lowerLevel;

            // Hold broad shelves around each stratum, then cross between them with
            // a continuous eroded slope. Unlike hard floor quantization, both sides
            // meet at the same height and cannot create vertical triangle slivers.
            var erodedSlope = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.InverseLerp(0.22f, 0.78f, levelProgress));
            return (lowerLevel + erodedSlope) * stepHeight;
        }

        internal static float SampleSandTrapMask(
            int worldSeed,
            int generationVersion,
            float worldX,
            float worldZ)
        {
            var seed = StableHash(worldSeed, generationVersion, SandTrapSeedSalt);
            return SampleSandTrapMask(seed, worldX, worldZ);
        }

        private static float SampleSandTrapMask(int seed, float worldX, float worldZ)
        {
            // Sand remains an accent material, but each admitted patch must read as a
            // deliberate basin from the elevated gameplay camera. Larger, more widely
            // spaced cells preserve sparse total coverage while avoiding tiny decals.
            const float cellSize = 43f;
            const float admissionChance = 0.28f;
            var originCellX = Mathf.FloorToInt(worldX / cellSize);
            var originCellZ = Mathf.FloorToInt(worldZ / cellSize);
            var strongestMask = 0f;

            for (var offsetZ = -1; offsetZ <= 1; offsetZ++)
            {
                for (var offsetX = -1; offsetX <= 1; offsetX++)
                {
                    var cellX = originCellX + offsetX;
                    var cellZ = originCellZ + offsetZ;
                    if (Hash01(seed + 1201, cellX, cellZ) > admissionChance)
                    {
                        continue;
                    }

                    var centerX = (cellX + Mathf.Lerp(
                        0.14f,
                        0.86f,
                        Hash01(seed + 1213, cellX, cellZ))) * cellSize;
                    var centerZ = (cellZ + Mathf.Lerp(
                        0.14f,
                        0.86f,
                        Hash01(seed + 1229, cellX, cellZ))) * cellSize;
                    var majorRadius = Mathf.Lerp(
                        6.4f,
                        9.2f,
                        Hash01(seed + 1249, cellX, cellZ));
                    var minorRadius = majorRadius * Mathf.Lerp(
                        0.55f,
                        0.76f,
                        Hash01(seed + 1277, cellX, cellZ));
                    var rotationRadians = Hash01(seed + 1301, cellX, cellZ) * Mathf.PI;
                    var cosine = Mathf.Cos(rotationRadians);
                    var sine = Mathf.Sin(rotationRadians);
                    var deltaX = worldX - centerX;
                    var deltaZ = worldZ - centerZ;
                    var localX = deltaX * cosine + deltaZ * sine;
                    var localZ = -deltaX * sine + deltaZ * cosine;
                    var ellipseDistance = Mathf.Sqrt(
                        localX * localX / (majorRadius * majorRadius)
                        + localZ * localZ / (minorRadius * minorRadius));
                    var erodedEdge = (FractalNoise(
                        seed + 1327,
                        worldX / 5.25f,
                        worldZ / 5.25f,
                        2) - 0.5f) * 0.16f;
                    var edgeProgress = Mathf.InverseLerp(
                        0.56f,
                        1.02f,
                        ellipseDistance + erodedEdge);
                    var mask = 1f - Mathf.SmoothStep(0f, 1f, edgeProgress);
                    strongestMask = Mathf.Max(strongestMask, mask);
                }
            }

            return Mathf.Clamp01(strongestMask);
        }

        private static float SampleDrainage(int seed, float worldX, float worldZ, float regionSize, float width)
        {
            const float rotationX = 0.8660254f;
            const float rotationZ = 0.5f;
            var along = (worldX * rotationX + worldZ * rotationZ) / Mathf.Max(24f, regionSize * 0.74f);
            var across = (-worldX * rotationZ + worldZ * rotationX) / Mathf.Max(24f, regionSize * 0.74f);
            var warp = (FractalNoise(seed + 1009, along * 0.65f, across * 1.1f, 3) - 0.5f) * 1.35f;
            var primaryDistance = Mathf.Abs(Mathf.Sin((across + warp) * Mathf.PI));
            var tributaryWarp = (FractalNoise(seed + 1103, across * 0.9f, along * 1.25f, 2) - 0.5f) * 0.8f;
            var tributaryDistance = Mathf.Abs(Mathf.Sin((along * 1.7f + tributaryWarp) * Mathf.PI));
            var primary = 1f - Mathf.SmoothStep(width, width * 2.4f, primaryDistance);
            var tributary = 1f - Mathf.SmoothStep(width * 0.7f, width * 1.7f, tributaryDistance);
            return Mathf.Clamp01(Mathf.Max(primary, tributary * 0.58f));
        }

        private static float FractalNoise(int seed, float x, float z, int octaves)
        {
            var amplitude = 1f;
            var frequency = 1f;
            var total = 0f;
            var weight = 0f;
            for (var octave = 0; octave < octaves; octave++)
            {
                total += ValueNoise(seed + octave * 1013, x * frequency, z * frequency) * amplitude;
                weight += amplitude;
                amplitude *= 0.5f;
                frequency *= 2.03f;
            }

            return weight > 0f ? total / weight : 0.5f;
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
                var hash = (uint)StableHash(seed, x, z);
                return (hash & 0x00FFFFFFu) / 16777215f;
            }
        }

        private static int StableHash(int seed, int x, int z)
        {
            unchecked
            {
                var hash = (uint)seed;
                hash ^= (uint)(x * 374761393);
                hash = (hash << 13) ^ hash;
                hash ^= (uint)(z * 668265263);
                hash *= 1274126177u;
                hash ^= hash >> 16;
                return (int)hash;
            }
        }

        private readonly struct CoreSample
        {
            public CoreSample(
                float height,
                int featureId,
                float flow,
                float mesaExposure,
                float lithology,
                float weathering,
                float sandTrap,
                float rockExposure)
            {
                Height = height;
                FeatureId = featureId;
                Flow = flow;
                MesaExposure = mesaExposure;
                Lithology = lithology;
                Weathering = weathering;
                SandTrap = sandTrap;
                RockExposure = rockExposure;
            }

            public float Height { get; }
            public int FeatureId { get; }
            public float Flow { get; }
            public float MesaExposure { get; }
            public float Lithology { get; }
            public float Weathering { get; }
            public float SandTrap { get; }
            public float RockExposure { get; }
        }
    }
}
