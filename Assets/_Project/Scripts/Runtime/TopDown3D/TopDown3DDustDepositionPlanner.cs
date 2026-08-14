using System;
using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public readonly struct TopDown3DDustDepositionSample : IEquatable<TopDown3DDustDepositionSample>
    {
        public TopDown3DDustDepositionSample(float weight, float height, float shelterWeight)
        {
            Weight = Mathf.Clamp01(weight);
            Height = Mathf.Max(0f, height);
            ShelterWeight = Mathf.Clamp01(shelterWeight);
        }

        public float Weight { get; }
        public float Height { get; }
        public float ShelterWeight { get; }

        public bool Equals(TopDown3DDustDepositionSample other)
        {
            return Weight.Equals(other.Weight)
                && Height.Equals(other.Height)
                && ShelterWeight.Equals(other.ShelterWeight);
        }

        public override bool Equals(object obj)
        {
            return obj is TopDown3DDustDepositionSample other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = Weight.GetHashCode();
                hash = hash * 397 ^ Height.GetHashCode();
                hash = hash * 397 ^ ShelterWeight.GetHashCode();
                return hash;
            }
        }
    }

    public sealed class TopDown3DDustDepositionPlan
    {
        private readonly TopDown3DDustDepositionSample[] samples;

        internal TopDown3DDustDepositionPlan(
            int quadsPerAxis,
            float step,
            TopDown3DDustDepositionSample[] samples,
            bool hasVisibleDeposits)
        {
            QuadsPerAxis = quadsPerAxis;
            Step = step;
            this.samples = samples;
            HasVisibleDeposits = hasVisibleDeposits;
        }

        public int QuadsPerAxis { get; }
        public int VerticesPerAxis => QuadsPerAxis + 1;
        public float Step { get; }
        public bool HasVisibleDeposits { get; }

        public TopDown3DDustDepositionSample GetSample(int x, int z)
        {
            return samples[z * VerticesPerAxis + x];
        }
    }

    public static class TopDown3DDustDepositionPlanner
    {
        private const float MinimumVisibleWeight = 0.025f;

        public static TopDown3DDustDepositionPlan BuildPlan(
            TopDown3DWorldSettings settings,
            TopDown3DNaturalObjectCatalog catalog,
            Vector2Int chunkCoordinate,
            Vector2 spawnExclusionCenter)
        {
            if (settings == null)
            {
                return new TopDown3DDustDepositionPlan(
                    1,
                    1f,
                    new TopDown3DDustDepositionSample[4],
                    false);
            }

            var quads = settings.DustOverlayQuadsPerAxis;
            var verticesPerAxis = quads + 1;
            var step = settings.ChunkSize / quads;
            var samples = new TopDown3DDustDepositionSample[verticesPerAxis * verticesPerAxis];
            var physicalSources = CollectPhysicalSources(
                settings,
                catalog,
                chunkCoordinate,
                spawnExclusionCenter);
            var origin = new Vector2(
                chunkCoordinate.x * settings.ChunkSize,
                chunkCoordinate.y * settings.ChunkSize);
            var hasVisibleDeposits = false;
            for (var z = 0; z < verticesPerAxis; z++)
            {
                for (var x = 0; x < verticesPerAxis; x++)
                {
                    var worldPosition = origin + new Vector2(x * step, z * step);
                    var sample = SampleAt(settings, worldPosition, physicalSources);
                    samples[z * verticesPerAxis + x] = sample;
                    hasVisibleDeposits |= sample.Weight >= MinimumVisibleWeight;
                }
            }

            return new TopDown3DDustDepositionPlan(
                quads,
                step,
                samples,
                hasVisibleDeposits);
        }

        public static Vector2 GetPrevailingWindDirection(TopDown3DWorldSettings settings)
        {
            var radians = settings.PrevailingWindDegrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)).normalized;
        }

        public static float SampleBaseWeight(
            TopDown3DWorldSettings settings,
            Vector2 worldPosition)
        {
            var wind = GetPrevailingWindDirection(settings);
            var crossWind = new Vector2(-wind.y, wind.x);
            var along = Vector2.Dot(worldPosition, wind);
            var across = Vector2.Dot(worldPosition, crossWind);
            var seed = StableHash(
                settings.WorldSeed,
                settings.DustDepositionGenerationVersion,
                0x4B1D72A9);
            var pocket = FractalNoise(
                seed ^ 0x239A51C7,
                worldPosition.x * settings.DustPocketFrequency,
                worldPosition.y * settings.DustPocketFrequency);
            var windrow = FractalNoise(
                seed ^ 0x6D2F41B3,
                along * settings.DustPocketFrequency * 1.15f,
                across * settings.DustWindrowFrequency);
            var ripple = ValueNoise(
                seed ^ 0x17C8E529,
                along * settings.DustPocketFrequency * 3.4f,
                across * settings.DustWindrowFrequency * 1.8f);
            var erosion = FractalNoise(
                seed ^ 0x51E37A2D,
                along * settings.DustPocketFrequency * 1.85f + 13.7f,
                across * settings.DustWindrowFrequency * 0.72f - 9.3f);
            var erosionGate = SmoothStepRange(0.36f, 0.76f, erosion);
            var deposition = pocket * 0.54f + windrow * 0.34f + ripple * 0.12f;
            deposition -= (1f - erosionGate) * 0.16f;
            return SmoothStepRange(
                Mathf.Min(0.94f, settings.DustCoverageThreshold + 0.035f),
                Mathf.Min(0.99f, settings.DustCoverageThreshold + 0.2f),
                deposition);
        }

        public static float SampleShelterWeight(
            TopDown3DWorldSettings settings,
            Vector2 worldPosition,
            IReadOnlyList<TopDown3DNaturalObjectPlacement> physicalSources)
        {
            return SampleShelter(settings, worldPosition, physicalSources).Weight;
        }

        public static TopDown3DDustDepositionSample SampleAt(
            TopDown3DWorldSettings settings,
            Vector2 worldPosition,
            IReadOnlyList<TopDown3DNaturalObjectPlacement> physicalSources)
        {
            var baseWeight = SampleBaseWeight(settings, worldPosition);
            var heightNoiseSeed = StableHash(
                settings.WorldSeed,
                settings.DustDepositionGenerationVersion,
                0x35A4C91D);
            var heightNoise = ValueNoise(
                heightNoiseSeed,
                worldPosition.x * settings.DustPocketFrequency * 2.7f,
                worldPosition.y * settings.DustPocketFrequency * 2.7f);
            var baseHeight = baseWeight
                * settings.DustMaximumBaseHeight
                * Mathf.Lerp(0.68f, 1f, heightNoise);
            var shelter = SampleShelter(settings, worldPosition, physicalSources);
            var normal = TopDown3DHeightSampler.SampleNormal(
                settings,
                worldPosition.x,
                worldPosition.y);
            var slope = Vector3.Angle(normal, Vector3.up);
            var slopeAttenuation = 1f - SmoothStepRange(
                settings.MaximumDustDepositionSlope * 0.7f,
                settings.MaximumDustDepositionSlope,
                slope);
            return new TopDown3DDustDepositionSample(
                Mathf.Max(baseWeight, shelter.Weight) * slopeAttenuation,
                Mathf.Max(baseHeight, shelter.Height) * slopeAttenuation,
                shelter.Weight * slopeAttenuation);
        }

        private static List<TopDown3DNaturalObjectPlacement> CollectPhysicalSources(
            TopDown3DWorldSettings settings,
            TopDown3DNaturalObjectCatalog catalog,
            Vector2Int chunkCoordinate,
            Vector2 spawnExclusionCenter)
        {
            var sources = new List<TopDown3DNaturalObjectPlacement>();
            if (catalog == null)
            {
                return sources;
            }

            for (var z = -1; z <= 1; z++)
            {
                for (var x = -1; x <= 1; x++)
                {
                    sources.AddRange(TopDown3DNaturalObjectPlanner.BuildPhysicalPlacements(
                        settings,
                        catalog,
                        chunkCoordinate + new Vector2Int(x, z),
                        spawnExclusionCenter));
                }
            }

            return sources;
        }

        private static ShelterSample SampleShelter(
            TopDown3DWorldSettings settings,
            Vector2 worldPosition,
            IReadOnlyList<TopDown3DNaturalObjectPlacement> physicalSources)
        {
            if (physicalSources == null || physicalSources.Count == 0)
            {
                return default;
            }

            var wind = GetPrevailingWindDirection(settings);
            var crossWind = new Vector2(-wind.y, wind.x);
            var strongestWeight = 0f;
            var greatestHeight = 0f;
            for (var i = 0; i < physicalSources.Count; i++)
            {
                var source = physicalSources[i];
                var sourcePosition = new Vector2(source.Position.x, source.Position.z);
                var delta = worldPosition - sourcePosition;
                var downwind = Vector2.Dot(delta, wind);
                var sourceRadius = Mathf.Max(0.35f, source.FootprintRadius);
                var obstructionScale = SmoothStepRange(0.35f, 1.5f, sourceRadius);
                var wakeLength = Mathf.Min(
                    settings.DustWakeLength * 1.28f,
                    settings.DustWakeLength * Mathf.Lerp(0.65f, 1f, obstructionScale)
                        + sourceRadius
                        * (source.Layer == TopDown3DNaturalObjectLayer.Landmark ? 0.32f : 0.22f));
                if (downwind < -sourceRadius * 0.15f || downwind > wakeLength)
                {
                    continue;
                }

                var normalizedDistance = Mathf.Clamp01(downwind / Mathf.Max(0.01f, wakeLength));
                var curveDirection = HashSigned(source.FormationSeed ^ 0x59D3A417);
                var curvedCenter = Mathf.Sin(normalizedDistance * Mathf.PI)
                    * sourceRadius
                    * 0.28f
                    * curveDirection;
                var across = Mathf.Abs(Vector2.Dot(delta, crossWind) - curvedCenter);
                var wakeWidth = Mathf.Max(
                    0.38f,
                    sourceRadius * settings.DustWakeWidthMultiplier * 0.9f);
                if (across > wakeWidth)
                {
                    continue;
                }

                var nearFade = SmoothStepRange(
                    -sourceRadius * 0.15f,
                    sourceRadius * 0.48f,
                    downwind);
                var farFade = 1f - SmoothStepRange(
                    wakeLength * 0.56f,
                    wakeLength,
                    downwind);
                var lateralFade = 1f - SmoothStepRange(
                    wakeWidth * 0.3f,
                    wakeWidth,
                    across);
                var sourceAdmission = Mathf.Lerp(0.18f, 1f, obstructionScale);
                var weight = Mathf.Clamp01(
                    nearFade * farFade * lateralFade * sourceAdmission);
                var footprintHeight = Mathf.Lerp(
                    0.78f,
                    1.9f,
                    SmoothStepRange(0.35f, 3.2f, sourceRadius));
                var formationHeight = 1f
                    + Mathf.Min(5, source.MemberCount - 1) * 0.085f;
                var landmarkHeight = source.Layer == TopDown3DNaturalObjectLayer.Landmark
                    ? 1.18f
                    : 1f;
                var sourceHeight = Mathf.Min(
                    2.35f,
                    footprintHeight * formationHeight * landmarkHeight);
                strongestWeight = Mathf.Max(strongestWeight, weight);
                greatestHeight = Mathf.Max(
                    greatestHeight,
                    weight * settings.DustMaximumWakeHeight * sourceHeight);
            }

            return new ShelterSample(strongestWeight, greatestHeight);
        }

        private static float FractalNoise(int seed, float x, float z)
        {
            var value = 0f;
            var weight = 0f;
            var amplitude = 0.55f;
            for (var octave = 0; octave < 3; octave++)
            {
                value += ValueNoise(seed + octave * 1619, x, z) * amplitude;
                weight += amplitude;
                x = x * 2.03f + 11.7f;
                z = z * 2.03f - 7.9f;
                amplitude *= 0.55f;
            }

            return weight > 0f ? value / weight : 0f;
        }

        private static float ValueNoise(int seed, float x, float z)
        {
            var x0 = Mathf.FloorToInt(x);
            var z0 = Mathf.FloorToInt(z);
            var tx = Smooth(x - x0);
            var tz = Smooth(z - z0);
            var a = Hash01(StableHash(seed, x0, z0));
            var b = Hash01(StableHash(seed, x0 + 1, z0));
            var c = Hash01(StableHash(seed, x0, z0 + 1));
            var d = Hash01(StableHash(seed, x0 + 1, z0 + 1));
            return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), tz);
        }

        private static float Smooth(float value)
        {
            return value * value * (3f - 2f * value);
        }

        private static float SmoothStepRange(float minimum, float maximum, float value)
        {
            var range = maximum - minimum;
            if (Mathf.Abs(range) <= 0.000001f)
            {
                return value >= maximum ? 1f : 0f;
            }

            return Smooth(Mathf.Clamp01((value - minimum) / range));
        }

        private static int StableHash(int a, int b, int c)
        {
            unchecked
            {
                var hash = (uint)a;
                hash ^= (uint)b * 0x9E3779B9u;
                hash = (hash << 13) | (hash >> 19);
                hash ^= (uint)c * 0x85EBCA6Bu;
                hash ^= hash >> 16;
                hash *= 0x7FEB352Du;
                hash ^= hash >> 15;
                return (int)hash;
            }
        }

        private static float Hash01(int hash)
        {
            unchecked
            {
                var value = (uint)hash;
                value ^= value >> 16;
                value *= 0x7FEB352Du;
                value ^= value >> 15;
                return (value & 0x00FFFFFFu) / 16777215f;
            }
        }

        private static float HashSigned(int hash)
        {
            return Hash01(hash) * 2f - 1f;
        }

        private readonly struct ShelterSample
        {
            public ShelterSample(float weight, float height)
            {
                Weight = weight;
                Height = height;
            }

            public float Weight { get; }
            public float Height { get; }
        }
    }
}
