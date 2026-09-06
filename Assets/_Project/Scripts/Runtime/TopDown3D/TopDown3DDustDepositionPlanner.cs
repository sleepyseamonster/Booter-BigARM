using System;
using System.Collections.Generic;
using BooterBigArm.TopDown3D.WorldCreator;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public readonly struct TopDown3DDustDepositionSample : IEquatable<TopDown3DDustDepositionSample>
    {
        public TopDown3DDustDepositionSample(float weight, float height, float shelterWeight)
            : this(weight, height, shelterWeight, 0f, 0f, 0f, 0f)
        {
        }

        public TopDown3DDustDepositionSample(
            float weight,
            float height,
            float shelterWeight,
            float surfaceHeight,
            float windExposure,
            float erosion,
            float semanticDeposit)
        {
            Weight = Mathf.Clamp01(weight);
            Height = Mathf.Max(0f, height);
            ShelterWeight = Mathf.Clamp01(shelterWeight);
            SurfaceHeight = surfaceHeight;
            WindExposure = Mathf.Clamp01(windExposure);
            Erosion = Mathf.Clamp01(erosion);
            SemanticDeposit = Mathf.Clamp01(semanticDeposit);
        }

        public float Weight { get; }
        public float Height { get; }
        public float ShelterWeight { get; }
        public float SurfaceHeight { get; }
        public float WindExposure { get; }
        public float Erosion { get; }
        public float SemanticDeposit { get; }

        public bool Equals(TopDown3DDustDepositionSample other)
        {
            return Weight.Equals(other.Weight)
                && Height.Equals(other.Height)
                && ShelterWeight.Equals(other.ShelterWeight)
                && SurfaceHeight.Equals(other.SurfaceHeight)
                && WindExposure.Equals(other.WindExposure)
                && Erosion.Equals(other.Erosion)
                && SemanticDeposit.Equals(other.SemanticDeposit);
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
                hash = hash * 397 ^ SurfaceHeight.GetHashCode();
                hash = hash * 397 ^ SemanticDeposit.GetHashCode();
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

        /// <summary>Frozen, already-seated authored rock base; never obtained by recursively planning rocks.</summary>
        public readonly struct AuthoredObstruction
        {
            public AuthoredObstruction(Vector2 center, Vector2 halfSize, float exposedHeight)
            {
                Center = center;
                HalfSize = new Vector2(Mathf.Max(0.05f, halfSize.x), Mathf.Max(0.05f, halfSize.y));
                ExposedHeight = Mathf.Max(0f, exposedHeight);
            }

            public Vector2 Center { get; }
            public Vector2 HalfSize { get; }
            public float ExposedHeight { get; }
        }

        public static TopDown3DDustDepositionSample SampleAuthoredDeposit(
            TopDown3DWorldSettings settings,
            WorldSurfaceMaterialSample material,
            Vector2 position,
            IReadOnlyList<AuthoredObstruction> obstructions,
            float buildup)
        {
            var strongest = 0f;
            var height = 0f;
            var wind = DirectionFromTurns(material.PrevailingWindDirection);
            var acrossWind = new Vector2(-wind.y, wind.x);
            var slopeGate = 1f - SmoothStepRange(settings.MaximumDustDepositionSlope * 0.7f,
                settings.MaximumDustDepositionSlope, material.SlopeDegrees);
            var supply = Mathf.Lerp(0.55f, 1f, material.Sediment)
                * Mathf.Lerp(0.7f, 1f, material.Deposit)
                * Mathf.Lerp(1f, 0.72f, material.WindExposure * material.Erosion);
            if (buildup <= 0f || slopeGate <= 0f || obstructions == null) return default;
            foreach (var source in obstructions)
            {
                if (source.ExposedHeight <= 0.01f) continue;
                var delta = position - source.Center;
                var radius = Mathf.Min(source.HalfSize.x, source.HalfSize.y);
                var radial = new Vector2(delta.x / source.HalfSize.x, delta.y / source.HalfSize.y).magnitude;
                var edgeDistance = Mathf.Max(0f, radial - 1f) * radius;
                var skirtWidth = Mathf.Clamp(source.ExposedHeight * 0.8f + 0.1f, 0.14f, 0.55f);
                var along = Vector2.Dot(delta, wind);
                var across = Mathf.Abs(Vector2.Dot(delta, acrossWind));
                var lee = SmoothStepRange(-radius, radius, along);
                var skirt = (1f - SmoothStepRange(0f, skirtWidth, edgeDistance))
                    * Mathf.Lerp(0.22f, 0.8f, lee);
                var length = Mathf.Min(settings.DustWakeLength,
                    Mathf.Max(0.65f, source.ExposedHeight * 4f + radius));
                var farFade = 1f - SmoothStepRange(length * 0.2f, length, along);
                var width = Mathf.Max(source.HalfSize.x, source.HalfSize.y)
                    * settings.DustWakeWidthMultiplier * Mathf.Sqrt(farFade);
                var wake = SmoothStepRange(0f, radius, along) * farFade
                    * (1f - SmoothStepRange(width * 0.2f, Mathf.Max(0.001f, width), across));
                var weight = Mathf.Max(skirt, wake) * slopeGate * supply;
                strongest = Mathf.Max(strongest, weight);
                // Maximum, not sum: touching rocks must not create towering additive mounds.
                height = Mathf.Max(height, weight * Mathf.Min(buildup, source.ExposedHeight * 0.95f));
            }
            return new TopDown3DDustDepositionSample(
                strongest * Mathf.Clamp01(buildup / 0.12f), height, strongest,
                checked((float)material.Position.Vertical), material.WindExposure, material.Erosion, material.Deposit);
        }

        public static TopDown3DDustDepositionPlan BuildPlan(
            TopDown3DWorldSettings settings,
            TopDown3DWorldGenerator generator,
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
                generator,
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
                    var sample = SampleAtCanonical(
                        settings,
                        generator,
                        worldPosition,
                        physicalSources);
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
            IReadOnlyList<TopDown3DRockFormationPlan> physicalSources)
        {
            return SampleShelter(settings, worldPosition, physicalSources).Weight;
        }

        public static TopDown3DDustDepositionSample SampleAt(
            TopDown3DWorldSettings settings,
            TopDown3DWorldGenerator generator,
            Vector2 worldPosition,
            IReadOnlyList<TopDown3DRockFormationPlan> physicalSources)
        {
            return SampleAtCore(
                settings,
                generator,
                worldPosition,
                physicalSources,
                false);
        }

        private static TopDown3DDustDepositionSample SampleAtCanonical(
            TopDown3DWorldSettings settings,
            TopDown3DWorldGenerator generator,
            Vector2 worldPosition,
            IReadOnlyList<TopDown3DRockFormationPlan> physicalSources)
        {
            return SampleAtCore(
                settings,
                generator,
                worldPosition,
                physicalSources,
                true);
        }

        private static TopDown3DDustDepositionSample SampleAtCore(
            TopDown3DWorldSettings settings,
            TopDown3DWorldGenerator generator,
            Vector2 worldPosition,
            IReadOnlyList<TopDown3DRockFormationPlan> physicalSources,
            bool useSemanticWind)
        {
            var absolute = new AbsoluteWorldPosition(worldPosition.x, 0d, worldPosition.y);
            if (!generator.Authority.Materials.TrySample(absolute, out var material, out var error))
            {
                throw new InvalidOperationException(error);
            }

            var baseWeight = SmoothStepRange(0.38f, 0.72f, material.Deposit);
            var baseHeight = baseWeight
                * settings.DustMaximumBaseHeight
                * Mathf.Lerp(0.62f, 1f, material.Sediment)
                * Mathf.Lerp(0.72f, 1f, material.Shelter);
            var wind = useSemanticWind
                ? DirectionFromTurns(material.PrevailingWindDirection)
                : GetPrevailingWindDirection(settings);
            var shelter = SampleShelter(
                settings,
                generator,
                worldPosition,
                wind,
                physicalSources,
                true);
            var slope = material.SlopeDegrees;
            var slopeAttenuation = 1f - SmoothStepRange(
                settings.MaximumDustDepositionSlope * 0.7f,
                settings.MaximumDustDepositionSlope,
                slope);
            return new TopDown3DDustDepositionSample(
                Mathf.Max(baseWeight, shelter.Weight) * slopeAttenuation,
                Mathf.Max(baseHeight, shelter.Height) * slopeAttenuation,
                shelter.Weight * slopeAttenuation,
                checked((float)material.Position.Vertical),
                material.WindExposure,
                material.Erosion,
                material.Deposit);
        }

        private static List<TopDown3DRockFormationPlan> CollectPhysicalSources(
            TopDown3DWorldSettings settings,
            TopDown3DWorldGenerator generator,
            TopDown3DNaturalObjectCatalog catalog,
            Vector2Int chunkCoordinate,
            Vector2 spawnExclusionCenter)
        {
            var sources = new List<TopDown3DRockFormationPlan>();
            if (catalog == null)
            {
                return sources;
            }

            for (var z = -1; z <= 1; z++)
            {
                for (var x = -1; x <= 1; x++)
                {
                    sources.AddRange(TopDown3DGeologicalRockAdapter.BuildPhysicalFormations(
                        settings,
                        generator,
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
            IReadOnlyList<TopDown3DRockFormationPlan> physicalSources)
        {
            return SampleShelter(
                settings,
                null,
                worldPosition,
                GetPrevailingWindDirection(settings),
                physicalSources,
                false);
        }

        private static ShelterSample SampleShelter(
            TopDown3DWorldSettings settings,
            TopDown3DWorldGenerator generator,
            Vector2 worldPosition,
            Vector2 wind,
            IReadOnlyList<TopDown3DRockFormationPlan> physicalSources,
            bool sourcesAreLocal)
        {
            if (physicalSources == null || physicalSources.Count == 0)
            {
                return default;
            }

            var crossWind = new Vector2(-wind.y, wind.x);
            var strongestWeight = 0f;
            var greatestHeight = 0f;
            for (var i = 0; i < physicalSources.Count; i++)
            {
                var source = physicalSources[i];
                var sourcePosition = source.EnvelopeCenter;
                if (sourcesAreLocal && generator != null)
                {
                    var absolute = generator.ToAbsolute(sourcePosition.x, 0f, sourcePosition.y);
                    sourcePosition = new Vector2(
                        checked((float)absolute.HorizontalA),
                        checked((float)absolute.HorizontalB));
                }
                var delta = worldPosition - sourcePosition;
                var downwind = Vector2.Dot(delta, wind);
                var sourceRadius = Mathf.Max(0.35f, source.EnvelopeRadius);
                var obstructionScale = SmoothStepRange(0.35f, 1.5f, sourceRadius);
                var wakeLength = Mathf.Min(
                    settings.DustWakeLength * 1.28f,
                        settings.DustWakeLength * Mathf.Lerp(0.65f, 1f, obstructionScale)
                        + sourceRadius
                        * 0.26f);
                if (downwind < -sourceRadius * 0.15f || downwind > wakeLength)
                {
                    continue;
                }

                var normalizedDistance = Mathf.Clamp01(downwind / Mathf.Max(0.01f, wakeLength));
                var curveDirection = HashSigned(source.Seed ^ 0x59D3A417);
                var curvedCenter = Mathf.Sin(normalizedDistance * Mathf.PI)
                    * sourceRadius
                    * 0.28f
                    * curveDirection;
                var across = Mathf.Abs(Vector2.Dot(delta, crossWind) - curvedCenter);
                var wakeWidth = Mathf.Max(
                    0.38f,
                    sourceRadius * settings.DustWakeWidthMultiplier * 0.9f);
                var nearFade = SmoothStepRange(
                    -sourceRadius * 0.15f,
                    sourceRadius * 0.48f,
                    downwind);
                var farFade = 1f - SmoothStepRange(
                    wakeLength * 0.56f,
                    wakeLength,
                    downwind);
                if (farFade <= 0f)
                {
                    continue;
                }

                var taperedWakeWidth = wakeWidth * Mathf.Sqrt(farFade);
                if (across > taperedWakeWidth)
                {
                    continue;
                }

                var lateralFade = 1f - SmoothStepRange(
                    taperedWakeWidth * 0.3f,
                    taperedWakeWidth,
                    across);
                var sourceAdmission = Mathf.Lerp(0.18f, 1f, obstructionScale);
                var weight = Mathf.Clamp01(
                    nearFade * farFade * lateralFade * sourceAdmission);
                var sourceHeight = Mathf.Min(
                    2.35f,
                    Mathf.Lerp(
                        0.78f,
                        2.35f,
                        SmoothStepRange(0.5f, 9f, source.Height)));
                strongestWeight = Mathf.Max(strongestWeight, weight);
                greatestHeight = Mathf.Max(
                    greatestHeight,
                    weight * farFade * settings.DustMaximumWakeHeight * sourceHeight);
            }

            return new ShelterSample(strongestWeight, greatestHeight);
        }

        private static Vector2 DirectionFromTurns(float turns)
        {
            var radians = turns * Mathf.PI * 2f;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)).normalized;
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
