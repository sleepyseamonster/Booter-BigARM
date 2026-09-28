using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using BooterBigArm.TopDown3D.WorldCreator;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Editor.WorldCreator
{
    /// <summary>
    /// Read-only calibration of cliff candidates against the current production query.
    /// It writes data outside Assets and never changes the terrain or scene.
    /// </summary>
    public static class WorldCliffStudyExporter
    {
        public const string DefaultReportPath = "/tmp/booter-cliff-study-report.txt";
        public const string DefaultCrossSectionsPath = "/tmp/booter-cliff-study-cross-sections.csv";
        private const long ProofSeed = 24681357L;
        private const double NearChunkSpan = 18d;
        private const double CrossSectionStep = 0.75d;

        [MenuItem("Booter & BigARM/World Creator/Export Cliff Section Study")]
        public static void ExportFromMenu() => ExportFromCli();

        public static void ExportFromCli()
        {
            using (var runtime = WorldCreatorProductionRuntime.Create(ProofSeed))
            {
                var study = new WorldCliffSectionStudy(
                    runtime.Identity,
                    runtime.CoordinateModel,
                    runtime.Query,
                    WorldCliffStudyProfile.CreateTechnicalStudy());
                // The first area and a second offset patch; both are technical windows,
                // not accepted cliff locations or changes to the world profile.
                var windows = new[]
                {
                    new StudyWindow("origin", -16, 15, -16, 15),
                    new StudyWindow("east", 16, 47, -16, 15),
                    new StudyWindow("north", -16, 15, 16, 47),
                    new StudyWindow("diagonal", 16, 47, 16, 47)
                };
                var all = new List<WorldCliffSectionCandidate>();
                var report = new StringBuilder();
                report.AppendLine("NON-CANON CLIFF SECTION CALIBRATION");
                report.AppendLine("Read-only production-query sample; not a visual, collision, or gameplay acceptance test.");
                report.Append("seed=").Append(ProofSeed)
                    .Append(", versions=").Append(runtime.Identity.Versions)
                    .Append(", sourceFingerprint=").Append(runtime.SourceFingerprint).AppendLine();
                report.AppendLine("profile: 1.5 m owner grid; 0.75 m trace; 35 deg minimum slope; 1.25 m minimum drop; 5 m debris runout");
                report.AppendLine("windows use absolute A/B coordinates in metres; candidates are independent of 18 m streaming chunks");
                foreach (var window in windows)
                {
                    if (!study.TryBuildWindow(
                        window.MinimumA, window.MaximumA, window.MinimumB, window.MaximumB,
                        out var candidates, out var error))
                        throw new InvalidOperationException(error ?? "The cliff study window failed.");
                    all.AddRange(candidates);
                    var maxDrop = candidates.Count == 0 ? 0d : candidates.Max(candidate => candidate.VerticalDrop);
                    var plannedParents = candidates.Where(candidate => !candidate.ParentFeatureId.IsEmpty)
                        .Select(candidate => candidate.ParentFeatureId).Distinct().Count();
                    report.Append(window.Name).Append(": cells=").Append(window.CellCount)
                        .Append(", candidates=").Append(candidates.Count)
                        .Append(", plannedParents=").Append(plannedParents)
                        .Append(", broadGroundSections=").Append(candidates.Count - candidates.Count(candidate => !candidate.ParentFeatureId.IsEmpty))
                        .Append(", nearChunkBorder=").Append(candidates.Count(IsNearChunkBorder))
                        .Append(", maxDrop=").Append(F(maxDrop)).AppendLine(" m");
                }

                var selected = new List<WorldCliffSectionCandidate>();
                foreach (var candidate in all.OrderByDescending(item => item.VerticalDrop).ThenBy(item => item.Id))
                {
                    if (selected.Count == 4) break;
                    if (selected.Any(previous => HorizontalDistance(previous.Center, candidate.Center) < 12d)) continue;
                    selected.Add(candidate);
                }
                var crossSections = new StringBuilder();
                crossSections.AppendLine("candidateId,ownerCellA,ownerCellB,distanceFromToeM,absoluteA,absoluteB,heightM,slopeDegrees,strataExposure,deposit,sediment,debrisInfluence");
                foreach (var candidate in selected)
                {
                    report.Append("section=").Append(candidate.Id)
                        .Append(", owner=").Append(candidate.OwnerCellA).Append(':').Append(candidate.OwnerCellB)
                        .Append(", rim=").Append(candidate.Rim)
                        .Append(", toe=").Append(candidate.Toe)
                        .Append(", drop=").Append(F(candidate.VerticalDrop))
                        .Append(", strata=").Append(candidate.StrataFamilyId)
                        .AppendLine();
                    AppendCrossSection(runtime, candidate, crossSections);
                }
                if (selected.Count == 0)
                    report.AppendLine("No section met the technical threshold in these windows. Expand the survey or adjust the study profile before considering face geometry.");
                report.AppendLine("Interpretation: a candidate marks a steep source only. Caprock breaks, ledges, apron shape, route-safe placement, and the user's visual target remain unproven.");

                File.WriteAllText(DefaultReportPath, report.ToString(), new UTF8Encoding(false));
                File.WriteAllText(DefaultCrossSectionsPath, crossSections.ToString(), new UTF8Encoding(false));
                Debug.Log($"Wrote cliff study to {DefaultReportPath} and {DefaultCrossSectionsPath}.\n{report}");
            }
        }

        private static void AppendCrossSection(
            WorldCreatorProductionRuntime runtime,
            WorldCliffSectionCandidate candidate,
            StringBuilder builder)
        {
            var rimOffset = (candidate.Rim.HorizontalA - candidate.Toe.HorizontalA) * candidate.OutwardA
                + (candidate.Rim.HorizontalB - candidate.Toe.HorizontalB) * candidate.OutwardB;
            var start = Math.Floor((rimOffset - 1.5d) / CrossSectionStep);
            var end = Math.Ceiling(candidate.DebrisRunout / CrossSectionStep);
            for (var index = start; index <= end; index++)
            {
                var distance = index * CrossSectionStep;
                var point = new AbsoluteWorldPosition(
                    candidate.Toe.HorizontalA + distance * candidate.OutwardA,
                    0d,
                    candidate.Toe.HorizontalB + distance * candidate.OutwardB);
                if (!runtime.Query.TrySampleSurface(point, out var surface, out var surfaceError))
                    throw new InvalidOperationException(surfaceError ?? "Cliff cross-section surface query failed.");
                if (!runtime.Materials.TrySample(surface.Position, out var material, out var materialError))
                    throw new InvalidOperationException(materialError ?? "Cliff cross-section material query failed.");
                var slope = Math.Acos(Math.Max(-1d, Math.Min(1d, surface.NormalVertical))) * 180d / Math.PI;
                builder.Append(candidate.Id).Append(',')
                    .Append(candidate.OwnerCellA).Append(',').Append(candidate.OwnerCellB).Append(',')
                    .Append(F(distance)).Append(',')
                    .Append(F(point.HorizontalA)).Append(',').Append(F(point.HorizontalB)).Append(',')
                    .Append(F(surface.Position.Vertical)).Append(',').Append(F(slope)).Append(',')
                    .Append(F(material.StrataExposure)).Append(',').Append(F(material.Deposit)).Append(',')
                    .Append(F(material.Sediment)).Append(',')
                    .Append(F(candidate.SampleDebrisInfluence(surface.Position))).AppendLine();
            }
        }

        private static bool IsNearChunkBorder(WorldCliffSectionCandidate candidate)
        {
            return DistanceToNearestMultiple(candidate.Center.HorizontalA, NearChunkSpan) <= 0.75d
                || DistanceToNearestMultiple(candidate.Center.HorizontalB, NearChunkSpan) <= 0.75d;
        }

        private static double DistanceToNearestMultiple(double value, double span)
        {
            var quotient = value / span;
            return Math.Abs(quotient - Math.Round(quotient)) * span;
        }

        private static double HorizontalDistance(AbsoluteWorldPosition left, AbsoluteWorldPosition right)
        {
            var deltaA = left.HorizontalA - right.HorizontalA;
            var deltaB = left.HorizontalB - right.HorizontalB;
            return Math.Sqrt(deltaA * deltaA + deltaB * deltaB);
        }

        private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        private readonly struct StudyWindow
        {
            public StudyWindow(string name, long minimumA, long maximumA, long minimumB, long maximumB)
            {
                Name = name;
                MinimumA = minimumA;
                MaximumA = maximumA;
                MinimumB = minimumB;
                MaximumB = maximumB;
            }

            public string Name { get; }
            public long MinimumA { get; }
            public long MaximumA { get; }
            public long MinimumB { get; }
            public long MaximumB { get; }
            public long CellCount => (MaximumA - MinimumA + 1) * (MaximumB - MinimumB + 1);
        }
    }
}
