using System;
using BooterBigArm.TopDown3D.WorldCreator;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    /// <summary>
    /// Compatibility-facing adapter over the canonical World Creator query authority.
    /// It contains no terrain-shape algorithm and may not become a second macro authority.
    /// </summary>
    public sealed class TopDown3DWorldGenerator
    {
        private readonly WorldCreatorProductionAuthority authority;
        private readonly WorldCreatorProductionRuntime runtime;
        private readonly LocalOriginFrame fixedFrame;

        public TopDown3DWorldGenerator(TopDown3DWorldSettings settings)
        {
            WorldSeed = settings != null ? settings.WorldSeed : 0;
            authority = WorldCreatorProductionRuntime.GetSharedAuthority(WorldSeed);
            var origin = new AbsoluteWorldPosition(0d, 0d, 0d);
            fixedFrame = new LocalOriginFrame(
                authority.CoordinateModel.Encode(origin),
                origin,
                authority.Profile.LocalFrameRadius);
        }

        internal TopDown3DWorldGenerator(
            TopDown3DWorldSettings settings,
            WorldCreatorProductionRuntime productionRuntime)
        {
            runtime = productionRuntime ?? throw new ArgumentNullException(nameof(productionRuntime));
            authority = runtime.Authority;
            WorldSeed = settings != null ? settings.WorldSeed : checked((int)runtime.Identity.Seed);
            fixedFrame = runtime.CurrentFrame;
        }

        public int WorldSeed { get; }
        public int GenerationVersion => authority.Identity.Versions.Topology;
        public float RegionSize => checked((float)CanyonPlannerProfile.CreateNonCanonTechnicalProofProfile().CellSpan);
        public IWorldQueryService QueryService => authority.Query;

        public TopDown3DWorldSurfaceSample Sample(float worldX, float worldZ)
        {
            var absolute = ToAbsolute(worldX, 0f, worldZ);
            if (!authority.TrySampleSurface(absolute, out var surface, out var error))
            {
                throw new InvalidOperationException(error);
            }

            var normal = new Vector3(surface.NormalA, surface.NormalVertical, surface.NormalB);
            var slope = 1f - Mathf.Clamp01(normal.y);
            var isFloor = (surface.Semantic & WorldSurfaceSemantic.CanyonFloor) != 0;
            var isShelf = (surface.Semantic & WorldSurfaceSemantic.CanyonShelf) != 0;
            var isWall = (surface.Semantic & WorldSurfaceSemantic.CanyonWall) != 0;
            var isBuried = (surface.Semantic & WorldSurfaceSemantic.Buried) != 0;
            var isWeathered = (surface.Semantic & WorldSurfaceSemantic.Weathered) != 0;
            var isDisturbed = (surface.Semantic & WorldSurfaceSemantic.Disturbed) != 0;
            var sand = isFloor || isBuried ? 0.48f : 0.04f;
            var gravel = Mathf.Clamp01((isShelf ? 0.68f : 0.16f) + slope * 0.4f);
            var bedrock = Mathf.Clamp01((isWall ? 0.88f : 0.22f) + slope * 0.55f);
            var deposit = isBuried || isDisturbed ? 0.72f : isFloor ? 0.36f : 0.12f;
            var flow = isFloor ? 1f : isShelf ? 0.45f : 0f;
            var weathering = isWeathered ? 0.9f : 0.5f;
            var lithology = StableText01(surface.StrataFamilyId);
            var traversalCorridor = (surface.Semantic & WorldSurfaceSemantic.Approach) != 0
                ? 1f
                : 0f;

            return new TopDown3DWorldSurfaceSample(
                checked((float)surface.Position.Vertical),
                normal,
                surface.DominantFeatureId.GetHashCode(),
                sand,
                gravel,
                bedrock,
                deposit,
                flow,
                0f,
                slope,
                lithology,
                weathering,
                traversalCorridor);
        }

        public float SampleHeight(float worldX, float worldZ)
        {
            var absolute = ToAbsolute(worldX, 0f, worldZ);
            if (!authority.TrySampleSurface(absolute, out var surface, out var error))
            {
                throw new InvalidOperationException(error);
            }

            return checked((float)surface.Position.Vertical);
        }

        public Vector3 SampleNormal(float worldX, float worldZ, float sampleDistance = 0.75f)
        {
            var absolute = ToAbsolute(worldX, 0f, worldZ);
            if (!authority.TrySampleSurface(absolute, out var surface, out var error))
            {
                throw new InvalidOperationException(error);
            }

            return new Vector3(surface.NormalA, surface.NormalVertical, surface.NormalB);
        }

        public Vector2Int WorldToChunk(TopDown3DWorldSettings settings, Vector3 localPosition)
        {
            var size = settings != null ? Math.Max(0.01d, settings.ChunkSize) : 1d;
            var absolute = ToAbsolute(localPosition.x, localPosition.y, localPosition.z);
            return new Vector2Int(
                checked((int)Math.Floor(absolute.HorizontalA / size)),
                checked((int)Math.Floor(absolute.HorizontalB / size)));
        }

        public bool TryFindWalkablePosition(
            Vector2 desiredPosition,
            float searchRadius,
            float searchStep,
            float maximumSlopeDegrees,
            out Vector3 position)
        {
            position = default;
            var step = Mathf.Max(0.25f, searchStep);
            var rings = Mathf.Max(0, Mathf.CeilToInt(Mathf.Max(0f, searchRadius) / step));
            var profile = new WorldAgentProfile(
                WorldAgentKind.Booter,
                WorldAgentProfile.BooterProof.Radius,
                WorldAgentProfile.BooterProof.Height,
                Mathf.Clamp(maximumSlopeDegrees, 1f, 89f));
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

                        var localX = desiredPosition.x + x * step;
                        var localZ = desiredPosition.y + z * step;
                        var absolute = ToAbsolute(localX, 0f, localZ);
                        if (!authority.Query.TrySampleAffordance(
                                absolute,
                                profile,
                                out var affordance,
                                out _)
                            || !affordance.Walkable
                            || !authority.TrySampleSurface(absolute, out var surface, out _))
                        {
                            continue;
                        }

                        position = new Vector3(localX, checked((float)surface.Position.Vertical), localZ);
                        return true;
                    }
                }
            }

            return false;
        }

        internal AbsoluteWorldPosition ToAbsolute(float localX, float localY, float localZ)
        {
            return CurrentFrame.ToAbsolute(new LocalWorldPosition(localX, localY, localZ));
        }

        internal bool TryToLocal(AbsoluteWorldPosition absolute, out LocalWorldPosition local)
        {
            return CurrentFrame.TryToLocal(absolute, out local);
        }

        private LocalOriginFrame CurrentFrame => runtime != null ? runtime.CurrentFrame : fixedFrame;

        private static float StableText01(string value)
        {
            unchecked
            {
                uint hash = 2166136261u;
                var text = value ?? string.Empty;
                for (var i = 0; i < text.Length; i++)
                {
                    hash ^= text[i];
                    hash *= 16777619u;
                }

                return (hash & 0x00ffffffu) / 16777215f;
            }
        }
    }
}
