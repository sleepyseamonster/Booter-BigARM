using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public readonly struct TopDown3DEscarpmentFeature
    {
        public TopDown3DEscarpmentFeature(
            int cellX,
            int cellZ,
            Vector2 center,
            float radiusX,
            float radiusZ,
            float rotationDegrees,
            float height,
            float edgeWidth,
            float firstPhase,
            float secondPhase,
            int reliefSeed)
        {
            CellX = cellX;
            CellZ = cellZ;
            Center = center;
            RadiusX = radiusX;
            RadiusZ = radiusZ;
            RotationDegrees = rotationDegrees;
            Height = height;
            EdgeWidth = edgeWidth;
            FirstPhase = firstPhase;
            SecondPhase = secondPhase;
            ReliefSeed = reliefSeed;
        }

        public int CellX { get; }
        public int CellZ { get; }
        public Vector2 Center { get; }
        public float RadiusX { get; }
        public float RadiusZ { get; }
        public float RotationDegrees { get; }
        public float Height { get; }
        public float EdgeWidth { get; }
        public float FirstPhase { get; }
        public float SecondPhase { get; }
        public int ReliefSeed { get; }

        public float MaximumExtent => Mathf.Max(RadiusX, RadiusZ) * 1.18f + EdgeWidth;

        public float BoundaryScale(float angle)
        {
            return 1f
                + Mathf.Sin(angle * 3f + FirstPhase) * 0.1f
                + Mathf.Sin(angle * 5f + SecondPhase) * 0.055f;
        }

        public Vector2 SampleBoundary(float angle, float radialOffset)
        {
            var minimumRadius = Mathf.Max(0.01f, Mathf.Min(RadiusX, RadiusZ));
            var scale = Mathf.Max(0.05f, BoundaryScale(angle) + radialOffset / minimumRadius);
            var local = new Vector2(
                Mathf.Cos(angle) * RadiusX * scale,
                Mathf.Sin(angle) * RadiusZ * scale);
            var rotation = RotationDegrees * Mathf.Deg2Rad;
            var cosine = Mathf.Cos(rotation);
            var sine = Mathf.Sin(rotation);
            return Center + new Vector2(
                local.x * cosine - local.y * sine,
                local.x * sine + local.y * cosine);
        }

        public float SampleBlend(float worldX, float worldZ)
        {
            var rotation = -RotationDegrees * Mathf.Deg2Rad;
            var cosine = Mathf.Cos(rotation);
            var sine = Mathf.Sin(rotation);
            var offsetX = worldX - Center.x;
            var offsetZ = worldZ - Center.y;
            var localX = offsetX * cosine - offsetZ * sine;
            var localZ = offsetX * sine + offsetZ * cosine;
            var normalizedX = localX / Mathf.Max(0.01f, RadiusX);
            var normalizedZ = localZ / Mathf.Max(0.01f, RadiusZ);
            var angle = Mathf.Atan2(normalizedZ, normalizedX);
            var normalizedRadius = Mathf.Sqrt(normalizedX * normalizedX + normalizedZ * normalizedZ);
            var signedDistance = (BoundaryScale(angle) - normalizedRadius)
                * Mathf.Min(RadiusX, RadiusZ);
            return Mathf.SmoothStep(
                0f,
                1f,
                Mathf.InverseLerp(-EdgeWidth * 0.5f, EdgeWidth * 0.5f, signedDistance));
        }
    }

    public static class TopDown3DEscarpmentSampler
    {
        private const int CellSearchRadius = 1;
        private const int SeedNamespace = 1979339339;

        public static float SampleElevation(
            TopDown3DWorldSettings settings,
            float worldX,
            float worldZ)
        {
            if (settings == null
                || !settings.GenerateEscarpments
                || settings.EscarpmentRegionChance <= 0f)
            {
                return 0f;
            }

            var regionSize = Mathf.Max(24f, settings.EscarpmentRegionSize);
            var cellX = Mathf.FloorToInt(worldX / regionSize);
            var cellZ = Mathf.FloorToInt(worldZ / regionSize);
            var maximumElevation = 0f;
            for (var z = cellZ - CellSearchRadius; z <= cellZ + CellSearchRadius; z++)
            {
                for (var x = cellX - CellSearchRadius; x <= cellX + CellSearchRadius; x++)
                {
                    if (!TryCreateFeature(settings, x, z, out var feature))
                    {
                        continue;
                    }

                    var blend = feature.SampleBlend(worldX, worldZ);
                    if (blend <= 0f)
                    {
                        continue;
                    }

                    var interior = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 0.95f, blend));
                    var noise = ValueNoise(
                        feature.ReliefSeed,
                        worldX * settings.CragReliefFrequency,
                        worldZ * settings.CragReliefFrequency);
                    var ridge = 1f - Mathf.Abs(noise * 2f - 1f);
                    var relief = ((noise * 2f - 1f) * 0.72f + (ridge - 0.5f) * 0.28f)
                        * settings.CragReliefAmplitude
                        * interior;
                    var elevation = Mathf.Clamp(
                        feature.Height * blend + relief,
                        0f,
                        settings.EscarpmentMaximumHeight);
                    maximumElevation = Mathf.Max(maximumElevation, elevation);
                }
            }

            return maximumElevation;
        }

        public static void CollectFeatures(
            TopDown3DWorldSettings settings,
            Rect worldBounds,
            ICollection<TopDown3DEscarpmentFeature> results)
        {
            if (settings == null
                || results == null
                || !settings.GenerateEscarpments
                || settings.EscarpmentRegionChance <= 0f)
            {
                return;
            }

            var regionSize = Mathf.Max(24f, settings.EscarpmentRegionSize);
            var margin = settings.EscarpmentMaximumRadius * 1.2f
                + settings.EscarpmentEdgeWidth
                + regionSize * 0.15f;
            var minimumCellX = Mathf.FloorToInt((worldBounds.xMin - margin) / regionSize);
            var maximumCellX = Mathf.FloorToInt((worldBounds.xMax + margin) / regionSize);
            var minimumCellZ = Mathf.FloorToInt((worldBounds.yMin - margin) / regionSize);
            var maximumCellZ = Mathf.FloorToInt((worldBounds.yMax + margin) / regionSize);
            for (var z = minimumCellZ; z <= maximumCellZ; z++)
            {
                for (var x = minimumCellX; x <= maximumCellX; x++)
                {
                    if (!TryCreateFeature(settings, x, z, out var feature))
                    {
                        continue;
                    }

                    var extent = feature.MaximumExtent;
                    if (feature.Center.x + extent < worldBounds.xMin
                        || feature.Center.x - extent > worldBounds.xMax
                        || feature.Center.y + extent < worldBounds.yMin
                        || feature.Center.y - extent > worldBounds.yMax)
                    {
                        continue;
                    }

                    results.Add(feature);
                }
            }
        }

        public static bool TryCreateFeature(
            TopDown3DWorldSettings settings,
            int cellX,
            int cellZ,
            out TopDown3DEscarpmentFeature feature)
        {
            feature = default;
            if (settings == null || !settings.GenerateEscarpments)
            {
                return false;
            }

            var seed = unchecked(
                settings.WorldSeed
                ^ SeedNamespace
                ^ settings.EscarpmentGenerationVersion * 486187739);
            if (Hash01(seed, cellX, cellZ, 11) > settings.EscarpmentRegionChance)
            {
                return false;
            }

            var regionSize = Mathf.Max(24f, settings.EscarpmentRegionSize);
            var jitter = regionSize * 0.14f;
            var center = new Vector2(
                (cellX + 0.5f) * regionSize
                    + Mathf.Lerp(-jitter, jitter, Hash01(seed, cellX, cellZ, 23)),
                (cellZ + 0.5f) * regionSize
                    + Mathf.Lerp(-jitter, jitter, Hash01(seed, cellX, cellZ, 37)));
            var maximumRadius = Mathf.Max(
                settings.EscarpmentMinimumRadius,
                settings.EscarpmentMaximumRadius);
            var radiusX = Mathf.Lerp(
                settings.EscarpmentMinimumRadius,
                maximumRadius,
                Hash01(seed, cellX, cellZ, 47));
            var radiusZ = radiusX * Mathf.Lerp(0.58f, 0.84f, Hash01(seed, cellX, cellZ, 59));
            if (Hash01(seed, cellX, cellZ, 61) > 0.5f)
            {
                var swap = radiusX;
                radiusX = radiusZ;
                radiusZ = swap;
            }

            var height = Mathf.Lerp(
                settings.EscarpmentMinimumHeight,
                settings.EscarpmentMaximumHeight,
                Hash01(seed, cellX, cellZ, 71));
            feature = new TopDown3DEscarpmentFeature(
                cellX,
                cellZ,
                center,
                radiusX,
                radiusZ,
                Hash01(seed, cellX, cellZ, 83) * 180f,
                height,
                settings.EscarpmentEdgeWidth,
                Hash01(seed, cellX, cellZ, 97) * Mathf.PI * 2f,
                Hash01(seed, cellX, cellZ, 101) * Mathf.PI * 2f,
                unchecked(seed ^ cellX * 92837111 ^ cellZ * 689287499));
            return true;
        }

        private static float ValueNoise(int seed, float x, float z)
        {
            var x0 = Mathf.FloorToInt(x);
            var z0 = Mathf.FloorToInt(z);
            var tx = Smooth(x - x0);
            var tz = Smooth(z - z0);
            var a = Hash01(seed, x0, z0, 131);
            var b = Hash01(seed, x0 + 1, z0, 131);
            var c = Hash01(seed, x0, z0 + 1, 131);
            var d = Hash01(seed, x0 + 1, z0 + 1, 131);
            return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), tz);
        }

        private static float Smooth(float value)
        {
            return value * value * (3f - 2f * value);
        }

        private static float Hash01(int seed, int x, int z, int salt)
        {
            unchecked
            {
                var hash = (uint)(seed ^ salt * 374761393);
                hash ^= (uint)x * 0x9E3779B9u;
                hash ^= (uint)z * 0x85EBCA6Bu;
                hash ^= hash >> 16;
                hash *= 0x7FEB352Du;
                hash ^= hash >> 15;
                return (hash & 0x00FFFFFFu) / 16777215f;
            }
        }
    }
}
