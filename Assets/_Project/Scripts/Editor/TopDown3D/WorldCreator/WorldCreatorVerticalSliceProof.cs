using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using BooterBigArm.TopDown3D.WorldCreator;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Editor.WorldCreator
{
    public static class WorldCreatorVerticalSliceProof
    {
        public const string DefaultReportPath = "/tmp/booter-worldcreator-batch10-vertical-slice-report.txt";
        private const int SamplesPerTransect = 65;
        private const double RepresentationTileSpan = 144d;

        private static readonly long[] Seeds = { 24681357L, 3496479L, 8675309L };
        private static readonly Transect[] Transects =
        {
            new Transect("horizontal", -1536d, -1152d, 1536d, -1152d),
            new Transect("diagonal-rebase", -1536d, -1536d, 768d, 768d),
            new Transect("offset", -1152d, 1536d, 1920d, 1536d)
        };

        [MenuItem("Booter & BigARM/World Creator/Export Batch 10 Vertical Slice Proof")]
        public static void ExportFromMenu() => ExportFromCli();

        public static void ExportFromCli()
        {
            var report = BuildReport();
            File.WriteAllText(DefaultReportPath, report, new UTF8Encoding(false));
            Debug.Log($"Wrote Batch 10 World Creator vertical-slice proof to {DefaultReportPath}.\n{report}");
        }

        public static string BuildReport()
        {
            var started = DateTime.UtcNow;
            var arrangementSignatures = new HashSet<ulong>();
            var sourceFingerprints = new HashSet<WorldFeatureId>();
            var builder = new StringBuilder(16384);
            builder.AppendLine("NON-CANON WORLD CREATOR BATCH 10 VERTICAL-SLICE TECHNICAL REPORT");
            builder.AppendLine("Production topology-v2 authority; disposable coordinate/geology adapter; no globe, region, or lore canon.");
            builder.Append("proofEnvelopeMetres=4608x4608, seeds=").Append(Seeds.Length)
                .Append(", transectsPerSeed=").Append(Transects.Length)
                .Append(", samplesPerTransect=").Append(SamplesPerTransect).AppendLine();

            var transectCount = 0;
            for (var seedIndex = 0; seedIndex < Seeds.Length; seedIndex++)
            {
                using var runtime = WorldCreatorProductionRuntime.Create(Seeds[seedIndex]);
                if (!sourceFingerprints.Add(runtime.SourceFingerprint))
                    throw new InvalidOperationException("Production source fingerprints repeated across proof seeds.");
                var rockPlanner = new WorldRockFormationPlanner(
                    runtime.Identity,
                    runtime.CoordinateModel,
                    runtime.ContextProvider,
                    runtime.Query);
                var proofHistory = ValidateProofHistory(runtime);
                var boundedFeatureCount = ValidateBoundedFeatureFamily(runtime);
                builder.Append("seed=").Append(Seeds[seedIndex])
                    .Append(", identity=").Append(runtime.Identity)
                    .Append(", source=").Append(runtime.SourceFingerprint)
                    .Append(", syntheticHistory=").Append(proofHistory.Id)
                    .Append(", boundedFeatureRequests=").Append(boundedFeatureCount)
                    .AppendLine();

                for (var transectIndex = 0; transectIndex < Transects.Length; transectIndex++)
                {
                    var transect = Transects[transectIndex];
                    if (transect.Length < 3000d)
                        throw new InvalidOperationException($"Transect '{transect.Name}' is shorter than 3000 metres.");

                    var minimumHeight = double.MaxValue;
                    var maximumHeight = double.MinValue;
                    var booterWalkable = 0;
                    var bigArmWalkable = 0;
                    var reservedRoutes = 0;
                    var canyonSamples = 0;
                    var boundedSamples = 0;
                    var materialVariation = 0d;
                    var signature = 1469598103934665603UL;
                    WorldSurfaceMaterialSample? previousMaterial = null;
                    for (var sampleIndex = 0; sampleIndex < SamplesPerTransect; sampleIndex++)
                    {
                        var position = transect.Sample(sampleIndex, SamplesPerTransect);
                        if (!runtime.Query.TrySampleSurface(position, out var surface, out var surfaceError))
                            throw new InvalidOperationException(surfaceError);
                        if (!runtime.Materials.TrySample(position, out var material, out var materialError))
                            throw new InvalidOperationException(materialError);
                        if (!runtime.Query.TrySampleAffordance(
                                position,
                                WorldAgentProfile.BooterProof,
                                out var booter,
                                out var booterError))
                            throw new InvalidOperationException(booterError);
                        if (!runtime.Query.TrySampleAffordance(
                                position,
                                WorldAgentProfile.BigArmProof,
                                out var bigArm,
                                out var bigArmError))
                            throw new InvalidOperationException(bigArmError);
                        if (surface.Position.HorizontalA != position.HorizontalA
                            || surface.Position.HorizontalB != position.HorizontalB
                            || material.Position.HorizontalA != position.HorizontalA
                            || material.Position.HorizontalB != position.HorizontalB)
                            throw new InvalidOperationException("A production query changed its absolute sample address.");

                        minimumHeight = Math.Min(minimumHeight, surface.Position.Vertical);
                        maximumHeight = Math.Max(maximumHeight, surface.Position.Vertical);
                        if (booter.Walkable) booterWalkable++;
                        if (bigArm.Walkable) bigArmWalkable++;
                        if (booter.ReservedRoute || bigArm.ReservedRoute) reservedRoutes++;
                        if ((surface.Semantic & (WorldSurfaceSemantic.CanyonFloor
                            | WorldSurfaceSemantic.CanyonShelf
                            | WorldSurfaceSemantic.CanyonWall)) != 0) canyonSamples++;
                        if ((surface.Semantic & WorldSurfaceSemantic.BoundedLandform) != 0) boundedSamples++;
                        if (previousMaterial.HasValue)
                        {
                            materialVariation += Math.Abs(material.Deposit - previousMaterial.Value.Deposit)
                                + Math.Abs(material.StrataExposure - previousMaterial.Value.StrataExposure)
                                + Math.Abs(material.Erosion - previousMaterial.Value.Erosion);
                        }
                        previousMaterial = material;
                        signature = Mix(signature, (ulong)surface.Semantic);
                        signature = Mix(signature, unchecked((ulong)Math.Round(surface.Position.Vertical * 16d)));
                        signature = Mix(signature, unchecked((ulong)Math.Round(material.Deposit * 1024f)));

                        if ((sampleIndex & 15) == 0)
                        {
                            if (!runtime.Query.TrySampleSurface(position, out var repeated, out var repeatError))
                                throw new InvalidOperationException(repeatError);
                            if (!repeated.Equals(surface))
                                throw new InvalidOperationException("Repeated absolute surface query changed inside one proof run.");
                        }
                    }

                    if (booterWalkable == 0 || bigArmWalkable == 0)
                        throw new InvalidOperationException("A proof transect exposed no walkable sample for a required agent profile.");
                    if (!arrangementSignatures.Add(signature))
                        throw new InvalidOperationException("Two complete proof transects produced the same non-identity arrangement signature.");

                    var midpoint = transect.Sample(SamplesPerTransect / 2, SamplesPerTransect);
                    if (!runtime.Query.TrySampleSurface(midpoint, out var beforeRebase, out var beforeError))
                        throw new InvalidOperationException(beforeError);
                    if (transectIndex == 1)
                    {
                        var nextOrigin = new AbsoluteWorldPosition(
                            midpoint.HorizontalA,
                            0d,
                            midpoint.HorizontalB);
                        if (!runtime.TryRebase(nextOrigin))
                            throw new InvalidOperationException("The diagonal proof transect could not rebase its local origin.");
                        if (!runtime.Query.TrySampleSurface(midpoint, out var afterRebase, out var afterError))
                            throw new InvalidOperationException(afterError);
                        if (!afterRebase.Equals(beforeRebase))
                            throw new InvalidOperationException("Canonical surface truth changed across the local-origin rebase.");
                    }

                    ValidateRepresentationAgreement(runtime, midpoint);
                    var rocks = rockPlanner.PlanOwnerArea(
                        midpoint.HorizontalA - 288d,
                        midpoint.HorizontalB - 288d,
                        576d,
                        576d,
                        new AbsoluteWorldPosition(1_000_000d, 0d, 1_000_000d),
                        0d);
                    var rockFingerprints = new HashSet<ulong>();
                    for (var i = 0; i < rocks.Count; i++) rockFingerprints.Add(rocks[i].NoveltyFingerprint);

                    builder.Append("  transect=").Append(transect.Name)
                        .Append(", length=").Append(F(transect.Length))
                        .Append(", heightRange=").Append(F(maximumHeight - minimumHeight))
                        .Append(", canyonSamples=").Append(canyonSamples)
                        .Append(", boundedSamples=").Append(boundedSamples)
                        .Append(", booterWalkable=").Append(booterWalkable).Append('/').Append(SamplesPerTransect)
                        .Append(", bigArmWalkable=").Append(bigArmWalkable).Append('/').Append(SamplesPerTransect)
                        .Append(", reservedRoutes=").Append(reservedRoutes)
                        .Append(", materialVariation=").Append(F(materialVariation))
                        .Append(", rockPlans=").Append(rocks.Count)
                        .Append(", rockFingerprints=").Append(rockFingerprints.Count)
                        .Append(", arrangementSignature=").Append(signature.ToString("x16", CultureInfo.InvariantCulture))
                        .AppendLine();
                    transectCount++;
                }

                if (runtime.Query is UnboundedHybridWorldQueryService query)
                {
                    var cache = query.CaptureCacheSnapshot();
                    if (cache.CanyonPlanCount > cache.MaximumCanyonPlans
                        || cache.TerrainWindowCount > cache.MaximumTerrainWindows)
                        throw new InvalidOperationException("The production query exceeded a declared cache bound.");
                    builder.Append("  queryCache=canyon ").Append(cache.CanyonPlanCount).Append('/')
                        .Append(cache.MaximumCanyonPlans).Append(", terrain ")
                        .Append(cache.TerrainWindowCount).Append('/').Append(cache.MaximumTerrainWindows)
                        .Append(", canyonEvictions=").Append(cache.CanyonEvictions)
                        .Append(", terrainEvictions=").Append(cache.TerrainEvictions).AppendLine();
                }
                var metrics = runtime.CaptureMetrics();
                if (metrics.FailedBuilds != 0 || metrics.StaleRejected != 0)
                    throw new InvalidOperationException("The production representation proof recorded a failed or stale build.");
                builder.Append("  representations=started ").Append(metrics.BuildsStarted)
                    .Append(", completed ").Append(metrics.BuildsCompleted)
                    .Append(", integrated ").Append(metrics.Integrated)
                    .Append(", failed ").Append(metrics.FailedBuilds).AppendLine();
            }

            builder.Append("summary: transects=").Append(transectCount)
                .Append(", uniqueArrangementSignatures=").Append(arrangementSignatures.Count)
                .Append(", uniqueSeedSources=").Append(sourceFingerprints.Count)
                .Append(", hardViolations=0, elapsedSeconds=")
                .Append(F((DateTime.UtcNow - started).TotalSeconds)).AppendLine();
            builder.AppendLine("Automated scope: deterministic absolute queries, semantic/material variation, agent samples, geological novelty, tier agreement, rebasing, and bounded caches.");
            builder.AppendLine("Not automated: fixed-camera artistic composition, visible-destination trust, hands-on traversal feel, GPU rendering, or target-Windows performance acceptance.");
            return builder.ToString();
        }

        private static void ValidateRepresentationAgreement(
            WorldCreatorProductionRuntime runtime,
            AbsoluteWorldPosition midpoint)
        {
            var tileA = checked((long)Math.Floor(midpoint.HorizontalA / RepresentationTileSpan));
            var tileB = checked((long)Math.Floor(midpoint.HorizontalB / RepresentationTileSpan));
            WorldFeatureId? feature = null;
            WorldSurfaceMaterialSample? material = null;
            AbsoluteWorldPosition? position = null;
            foreach (WorldRepresentationTier tier in Enum.GetValues(typeof(WorldRepresentationTier)))
            {
                var key = runtime.CreateRepresentationKey(tier, tileA, tileB, RepresentationTileSpan);
                var outcome = runtime.RequestAsync(key).GetAwaiter().GetResult();
                if (outcome.State == WorldRepresentationRequestState.Failed
                    || outcome.State == WorldRepresentationRequestState.Cancelled)
                    throw new InvalidOperationException(outcome.Error);
                for (var attempt = 0; attempt < 32 && !runtime.TryGetIntegrated(key, out _); attempt++)
                    runtime.DrainIntegrationQueue(2, TimeSpan.FromMilliseconds(2d));
                if (!runtime.TryGetIntegrated(key, out var result))
                    throw new InvalidOperationException("A proof representation did not integrate within its bounded drain.");
                if (!result.SourceFingerprint.Equals(runtime.SourceFingerprint))
                    throw new InvalidOperationException("A representation lost the production source fingerprint.");
                var center = result.VertexCount / 2;
                var nextFeature = result.GetFeatureId(center);
                var nextMaterial = result.GetMaterial(center);
                var nextPosition = result.GetAbsolutePosition(center);
                if (feature.HasValue && !feature.Value.Equals(nextFeature))
                    throw new InvalidOperationException("Near/mid/far center feature identity disagrees.");
                if (material.HasValue && !material.Value.Equals(nextMaterial))
                    throw new InvalidOperationException("Near/mid/far center material semantics disagree.");
                if (position.HasValue && !position.Value.Equals(nextPosition))
                    throw new InvalidOperationException("Near/mid/far center absolute position disagrees.");
                feature = nextFeature;
                material = nextMaterial;
                position = nextPosition;
            }
        }

        private static WorldHistoryPlan ValidateProofHistory(WorldCreatorProductionRuntime runtime)
        {
            var history = runtime.NonCanonProofHistory;
            string validationError = null;
            if (history == null || !history.Reservation.NonCanonProofOnly
                || !history.TryValidate(out validationError))
                throw new InvalidOperationException(validationError ?? "The production proof history is missing or not explicitly non-canon.");
            BoundedTerrainOperation? approach = null;
            for (var i = 0; i < history.Operations.Count; i++)
            {
                if (history.Operations[i].Kind == BoundedTerrainOperationKind.ReserveApproach)
                {
                    approach = history.Operations[i];
                    break;
                }
            }
            string error = null;
            if (!approach.HasValue
                || !runtime.Query.TrySampleSurface(
                    approach.Value.Center,
                    out var sample,
                    out error))
                throw new InvalidOperationException(error ?? "The proof history has no queryable approach operation.");
            if ((sample.Semantic & WorldSurfaceSemantic.SiteReservation) == 0
                || (sample.Semantic & WorldSurfaceSemantic.Approach) == 0)
                throw new InvalidOperationException("The production query did not apply its causal site reservation and approach.");
            var inside = new AbsoluteWorldPosition(
                history.Reservation.Center.HorizontalA + history.Reservation.FootprintRadius - 0.25d,
                0d,
                history.Reservation.Center.HorizontalB);
            var outside = new AbsoluteWorldPosition(
                history.Reservation.Center.HorizontalA + history.Reservation.FootprintRadius + 0.25d,
                0d,
                history.Reservation.Center.HorizontalB);
            if (!runtime.Query.TrySampleSurface(inside, out var insideSample, out var insideError))
                throw new InvalidOperationException(insideError);
            if (!runtime.Query.TrySampleSurface(outside, out var outsideSample, out var outsideError))
                throw new InvalidOperationException(outsideError);
            if ((insideSample.Semantic & WorldSurfaceSemantic.SiteReservation) == 0
                || (outsideSample.Semantic & WorldSurfaceSemantic.SiteReservation) != 0)
                throw new InvalidOperationException("The causal site reservation did not respect its bounded footprint.");
            return history;
        }

        private static int ValidateBoundedFeatureFamily(WorldCreatorProductionRuntime runtime)
        {
            var planner = new CanyonSystemPlanner(
                runtime.ContextProvider,
                CanyonPlannerProfile.CreateNonCanonTechnicalProofProfile());
            var plans = new List<CanyonSystemPlan>(9);
            for (var cellA = -1; cellA <= 1; cellA++)
            {
                for (var cellB = -1; cellB <= 1; cellB++)
                {
                    if (!planner.TryBuild(
                            runtime.Identity,
                            runtime.CoordinateModel,
                            new CanyonSystemCellIndex(cellA, cellB),
                            out var plan,
                            out var error))
                        throw new InvalidOperationException(error);
                    plans.Add(plan);
                }
            }
            var terrain = HybridTerrainCompiler.Compile(runtime.Identity, plans);
            if (terrain.LandformRequests.Count == 0)
                throw new InvalidOperationException("The production canyon plan emitted no bounded feature request.");
            for (var i = 0; i < terrain.LandformRequests.Count; i++)
            {
                var request = terrain.LandformRequests[i];
                if (!runtime.Query.TrySampleSurface(request.Center, out var surface, out var error))
                    throw new InvalidOperationException(error);
                if ((surface.Semantic & WorldSurfaceSemantic.BoundedLandform) != 0)
                    return terrain.LandformRequests.Count;
            }
            throw new InvalidOperationException("Bounded feature plans were not queryable through the production terrain authority.");
        }

        private static ulong Mix(ulong hash, ulong value)
        {
            hash ^= value + 0x9e3779b97f4a7c15UL + (hash << 6) + (hash >> 2);
            return hash;
        }

        private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        private readonly struct Transect
        {
            public Transect(string name, double startA, double startB, double endA, double endB)
            {
                Name = name;
                StartA = startA;
                StartB = startB;
                EndA = endA;
                EndB = endB;
                var deltaA = endA - startA;
                var deltaB = endB - startB;
                Length = Math.Sqrt(deltaA * deltaA + deltaB * deltaB);
            }

            public string Name { get; }
            public double StartA { get; }
            public double StartB { get; }
            public double EndA { get; }
            public double EndB { get; }
            public double Length { get; }

            public AbsoluteWorldPosition Sample(int index, int count)
            {
                var t = index / (double)(count - 1);
                return new AbsoluteWorldPosition(
                    StartA + (EndA - StartA) * t,
                    0d,
                    StartB + (EndB - StartB) * t);
            }
        }
    }
}
