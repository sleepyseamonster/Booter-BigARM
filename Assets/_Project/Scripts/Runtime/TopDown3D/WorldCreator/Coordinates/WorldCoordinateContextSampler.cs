using System;
using System.Collections.Generic;

namespace BooterBigArm.TopDown3D.WorldCreator
{
    public sealed class WorldCoordinateContextSampler : IWorldCoordinateContextProvider
    {
        private const int MaximumContributions = 16;
        private static readonly WorldSeedNamespace ContextNamespace =
            new WorldSeedNamespace(WorldVersionDomain.Landform, "world-coordinate-context");

        private readonly GeologicProvinceCatalog provinceCatalog;
        private readonly StrataFamilyCatalog strataCatalog;
        private readonly IWorldLandscapeInfluenceField influenceField;

        public WorldCoordinateContextSampler(
            GeologicProvinceCatalog provinceCatalog,
            StrataFamilyCatalog strataCatalog,
            IWorldLandscapeInfluenceField influenceField)
        {
            this.provinceCatalog = provinceCatalog != null
                ? provinceCatalog
                : throw new ArgumentNullException(nameof(provinceCatalog));
            this.strataCatalog = strataCatalog != null
                ? strataCatalog
                : throw new ArgumentNullException(nameof(strataCatalog));
            this.influenceField = influenceField ?? throw new ArgumentNullException(nameof(influenceField));
        }

        public bool TrySample(
            WorldIdentity world,
            IWorldCoordinateModel coordinateModel,
            WorldCoordinateAddress address,
            out WorldCoordinateContext context,
            out string error)
        {
            context = null;
            if (coordinateModel == null)
            {
                error = "A coordinate model is required.";
                return false;
            }

            if (!address.IsCompatibleWith(coordinateModel))
            {
                error = "The address does not belong to the supplied coordinate model.";
                return false;
            }

            if (!coordinateModel.TryResolve(address, out var absolutePosition))
            {
                error = "The coordinate model could not resolve the supplied address.";
                return false;
            }

            if (!provinceCatalog.TryValidate(out error) || !strataCatalog.TryValidate(out error))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(influenceField.StableId) || influenceField.Version < 1)
            {
                error = "The landscape influence field has an invalid stable identity or version.";
                return false;
            }

            var set = influenceField.Evaluate(absolutePosition);
            if (!TryResolveContributions(set, out var resolved, out error))
            {
                return false;
            }

            var aggregate = Aggregate(resolved, set.DeclaredDiscontinuitySignal);
            var localFingerprint = BuildLocalFingerprint(resolved, set.DeclaredDiscontinuitySignal);
            var contextId = WorldFeatureId.Create(
                world,
                ContextNamespace,
                address,
                localFingerprint);
            context = new WorldCoordinateContext(
                address,
                absolutePosition,
                contextId,
                GetContributions(resolved),
                aggregate.DominantProvinceId,
                aggregate.DominantStrataFamilyId,
                aggregate.Landscape,
                aggregate.StrataHardness,
                aggregate.StrataFolding,
                aggregate.StrataFracture,
                aggregate.IronOxideTendency,
                aggregate.PaleDepositTendency,
                aggregate.DarkRockTendency,
                set.DeclaredDiscontinuitySignal);
            error = null;
            return true;
        }

        private bool TryResolveContributions(
            WorldLandscapeInfluenceSet set,
            out ResolvedContribution[] resolved,
            out string error)
        {
            resolved = null;
            if (set == null
                || set.Contributions.Count == 0
                || set.Contributions.Count > MaximumContributions)
            {
                error = $"Landscape context requires between 1 and {MaximumContributions} contributions.";
                return false;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            var output = new ResolvedContribution[set.Contributions.Count];
            var totalWeight = 0f;
            for (var i = 0; i < set.Contributions.Count; i++)
            {
                var contribution = set.Contributions[i];
                if (!ids.Add(contribution.InfluenceId))
                {
                    error = $"Landscape context contains duplicate influence ID '{contribution.InfluenceId}'.";
                    return false;
                }

                if (!provinceCatalog.TryGet(contribution.ProvinceId, out var province))
                {
                    error = $"Landscape influence '{contribution.InfluenceId}' references unknown province '{contribution.ProvinceId}'.";
                    return false;
                }

                if (!strataCatalog.TryGet(contribution.StrataFamilyId, out var strata))
                {
                    error = $"Landscape influence '{contribution.InfluenceId}' references unknown strata '{contribution.StrataFamilyId}'.";
                    return false;
                }

                totalWeight += contribution.Weight;
                output[i] = new ResolvedContribution(contribution, province, strata);
            }

            if (Math.Abs(totalWeight - 1f) > 0.0001f)
            {
                error = $"Landscape influence weights must total 1; found {totalWeight:R}.";
                return false;
            }

            resolved = output;
            error = null;
            return true;
        }

        private AggregateResult Aggregate(
            IReadOnlyList<ResolvedContribution> resolved,
            float declaredDiscontinuitySignal)
        {
            var baseElevationBias = 0f;
            var relief = 0f;
            var structuralDirection = 0f;
            var structuralAnisotropy = 0f;
            var canyonDensity = 0f;
            var canyonBranching = 0f;
            var canyonDepth = 0f;
            var canyonWidth = 0f;
            var weathering = 0f;
            var sediment = 0f;
            var prevailingWindDirection = 0f;
            var windStrength = 0f;
            var landmarkCadence = 0f;
            var visualDensity = 0f;
            var strataHardness = 0f;
            var strataFolding = 0f;
            var strataFracture = 0f;
            var ironOxideTendency = 0f;
            var paleDepositTendency = 0f;
            var darkRockTendency = 0f;
            var dominantIndex = 0;

            for (var i = 0; i < resolved.Count; i++)
            {
                var item = resolved[i];
                var weight = item.Contribution.Weight;
                var landscape = item.Province.Landscape;
                baseElevationBias += landscape.BaseElevationBias * weight;
                relief += landscape.Relief * weight;
                structuralDirection += landscape.StructuralDirection * weight;
                structuralAnisotropy += landscape.StructuralAnisotropy * weight;
                canyonDensity += landscape.CanyonDensity * weight;
                canyonBranching += landscape.CanyonBranching * weight;
                canyonDepth += landscape.CanyonDepth * weight;
                canyonWidth += landscape.CanyonWidth * weight;
                weathering += landscape.Weathering * weight;
                sediment += landscape.Sediment * weight;
                prevailingWindDirection += landscape.PrevailingWindDirection * weight;
                windStrength += landscape.WindStrength * weight;
                landmarkCadence += landscape.LandmarkCadence * weight;
                visualDensity += landscape.VisualDensity * weight;
                strataHardness += item.Strata.Hardness * weight;
                strataFolding += item.Strata.Folding * weight;
                strataFracture += item.Strata.Fracture * weight;
                ironOxideTendency += item.Strata.IronOxideTendency * weight;
                paleDepositTendency += item.Strata.PaleDepositTendency * weight;
                darkRockTendency += item.Strata.DarkRockTendency * weight;
                if (weight > resolved[dominantIndex].Contribution.Weight)
                {
                    dominantIndex = i;
                }
            }

            var parameters = new WorldLandscapeParameters(
                ClampSigned(baseElevationBias),
                Clamp01(relief),
                Clamp01(structuralDirection),
                Clamp01(structuralAnisotropy),
                Clamp01(canyonDensity),
                Clamp01(canyonBranching),
                Clamp01(canyonDepth),
                Clamp01(canyonWidth),
                Clamp01(weathering),
                Clamp01(sediment),
                Clamp01(prevailingWindDirection),
                Clamp01(windStrength),
                Clamp01(landmarkCadence),
                Clamp01(visualDensity));
            return new AggregateResult(
                resolved[dominantIndex].Contribution.ProvinceId,
                resolved[dominantIndex].Contribution.StrataFamilyId,
                parameters,
                Clamp01(strataHardness),
                Clamp01(strataFolding),
                Clamp01(strataFracture),
                Clamp01(ironOxideTendency),
                Clamp01(paleDepositTendency),
                Clamp01(darkRockTendency),
                WorldLandscapeParameters.RequireNormalized(
                    declaredDiscontinuitySignal,
                    nameof(declaredDiscontinuitySignal)));
        }

        private string BuildLocalFingerprint(
            IReadOnlyList<ResolvedContribution> resolved,
            float declaredDiscontinuitySignal)
        {
            var builder = new WorldStableHashBuilder("world-coordinate-context-content-v1");
            builder.Append(influenceField.StableId);
            builder.Append(influenceField.Version);
            for (var i = 0; i < resolved.Count; i++)
            {
                var contribution = resolved[i].Contribution;
                builder.Append(contribution.InfluenceId);
                builder.Append(contribution.ProvinceId);
                builder.Append(contribution.StrataFamilyId);
                builder.Append(BitConverter.SingleToInt32Bits(contribution.Weight));
            }

            builder.Append(BitConverter.SingleToInt32Bits(declaredDiscontinuitySignal));
            builder.Finish128(out var high, out var low);
            return new WorldFeatureId(high, low).ToString();
        }

        private static float Clamp01(float value)
        {
            return Math.Max(0f, Math.Min(1f, value));
        }

        private static float ClampSigned(float value)
        {
            return Math.Max(-1f, Math.Min(1f, value));
        }

        private static WorldLandscapeInfluenceContribution[] GetContributions(
            IReadOnlyList<ResolvedContribution> resolved)
        {
            var output = new WorldLandscapeInfluenceContribution[resolved.Count];
            for (var i = 0; i < resolved.Count; i++)
            {
                output[i] = resolved[i].Contribution;
            }

            return output;
        }

        private readonly struct ResolvedContribution
        {
            public ResolvedContribution(
                WorldLandscapeInfluenceContribution contribution,
                GeologicProvinceDefinition province,
                StrataFamilyDefinition strata)
            {
                Contribution = contribution;
                Province = province;
                Strata = strata;
            }

            public WorldLandscapeInfluenceContribution Contribution { get; }
            public GeologicProvinceDefinition Province { get; }
            public StrataFamilyDefinition Strata { get; }
        }

        private readonly struct AggregateResult
        {
            public AggregateResult(
                string dominantProvinceId,
                string dominantStrataFamilyId,
                WorldLandscapeParameters landscape,
                float strataHardness,
                float strataFolding,
                float strataFracture,
                float ironOxideTendency,
                float paleDepositTendency,
                float darkRockTendency,
                float declaredDiscontinuitySignal)
            {
                DominantProvinceId = dominantProvinceId;
                DominantStrataFamilyId = dominantStrataFamilyId;
                Landscape = landscape;
                StrataHardness = strataHardness;
                StrataFolding = strataFolding;
                StrataFracture = strataFracture;
                IronOxideTendency = ironOxideTendency;
                PaleDepositTendency = paleDepositTendency;
                DarkRockTendency = darkRockTendency;
                DeclaredDiscontinuitySignal = declaredDiscontinuitySignal;
            }

            public string DominantProvinceId { get; }
            public string DominantStrataFamilyId { get; }
            public WorldLandscapeParameters Landscape { get; }
            public float StrataHardness { get; }
            public float StrataFolding { get; }
            public float StrataFracture { get; }
            public float IronOxideTendency { get; }
            public float PaleDepositTendency { get; }
            public float DarkRockTendency { get; }
            public float DeclaredDiscontinuitySignal { get; }
        }
    }
}
