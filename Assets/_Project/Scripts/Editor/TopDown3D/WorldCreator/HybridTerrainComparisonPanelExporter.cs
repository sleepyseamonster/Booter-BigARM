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
    public static class HybridTerrainComparisonPanelExporter
    {
        public const string DefaultSvgPath = "/tmp/booter-worldcreator-batch4-hybrid-panels.svg";
        public const string DefaultReportPath = "/tmp/booter-worldcreator-batch4-hybrid-report.txt";

        [MenuItem("Booter & BigARM/World Creator/Export Batch 4 Non-Canon Hybrid Terrain Panels")]
        public static void ExportFromMenu() => ExportFromCli();

        public static void ExportFromCli()
        {
            File.WriteAllText(DefaultSvgPath, BuildProofPanelSvg(), new UTF8Encoding(false));
            File.WriteAllText(DefaultReportPath, BuildProofReport(), new UTF8Encoding(false));
            Debug.Log($"Wrote non-canon Batch 4 hybrid terrain evidence to {DefaultSvgPath} and {DefaultReportPath}.");
        }

        public static ProofFixture BuildProofFixture(bool includeHistory)
        {
            WorldCreatorAssetValidator.ValidateFromCli();
            var provinces = AssetDatabase.LoadAssetAtPath<GeologicProvinceCatalog>(
                WorldCreatorBatch2ProofAssetBuilder.ProvinceCatalogPath);
            var strata = AssetDatabase.LoadAssetAtPath<StrataFamilyCatalog>(
                WorldCreatorBatch2ProofAssetBuilder.StrataCatalogPath);
            var influence = AssetDatabase.LoadAssetAtPath<NonCanonProofInfluenceProfile>(
                WorldCreatorBatch2ProofAssetBuilder.InfluenceProfilePath);
            var context = new WorldCoordinateContextSampler(provinces, strata, influence);
            var model = new NonCanonProofCoordinateModel();
            var world = new WorldIdentity(24681357L, new WorldVersionManifest(1, 1, 1, 1, 1, 1, 1));
            var plans = CanyonPlanOverlayExporter.BuildProofPlans(1);
            var histories = new List<WorldHistoryPlan>();
            if (includeHistory)
            {
                histories.Add(HybridTerrainCompiler.CreateNonCanonSyntheticHistoryFixture(
                    world,
                    model,
                    new AbsoluteWorldPosition(72d, 0d, 54d)));
            }

            var terrainPlan = HybridTerrainCompiler.Compile(world, plans, histories);
            var query = new DormantHybridWorldQueryService(terrainPlan, model, context);
            return new ProofFixture(world, model, context, plans, terrainPlan, query);
        }

        public static string BuildProofPanelSvg(int sampleCount = 48)
        {
            if (sampleCount < 12 || sampleCount > 96) throw new ArgumentOutOfRangeException(nameof(sampleCount));
            var withoutHistory = BuildProofFixture(false);
            var withHistory = BuildProofFixture(true);
            const double extent = 288d;
            const double panelSize = 432d;
            const double gutter = 24d;
            var cell = panelSize / sampleCount;
            var builder = new StringBuilder(512000);
            builder.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            builder.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 ")
                .Append(F(panelSize * 3d + gutter * 4d)).Append(" 520\">")
                .AppendLine("<title>NON-CANON WORLD CREATOR BATCH 4 HYBRID TERRAIN PANELS</title>");
            builder.AppendLine("<rect width=\"100%\" height=\"100%\" fill=\"#11151c\"/>");
            AppendPanel(builder, withoutHistory.Query, gutter, 64d, panelSize, cell, sampleCount, extent, PanelMode.Height);
            AppendPanel(builder, withHistory.Query, gutter * 2d + panelSize, 64d, panelSize, cell, sampleCount, extent, PanelMode.Height);
            AppendPanel(builder, withHistory.Query, gutter * 3d + panelSize * 2d, 64d, panelSize, cell, sampleCount, extent, PanelMode.Semantic);
            builder.AppendLine("<g fill=\"#e8edf2\" font-family=\"monospace\" font-size=\"15\">");
            builder.Append("<text x=\"").Append(F(gutter)).AppendLine("\" y=\"35\">hybrid canyon/province height</text>");
            builder.Append("<text x=\"").Append(F(gutter * 2d + panelSize)).AppendLine("\" y=\"35\">same terrain + synthetic history</text>");
            builder.Append("<text x=\"").Append(F(gutter * 3d + panelSize * 2d)).AppendLine("\" y=\"35\">semantic query classification</text>");
            builder.AppendLine("<text x=\"24\" y=\"510\">NON-CANON TECHNICAL PROOF ONLY | dormant adapter | production terrain unchanged</text>");
            builder.AppendLine("</g></svg>");
            return builder.ToString();
        }

        public static string BuildProofReport()
        {
            var fixture = BuildProofFixture(true);
            var builder = new StringBuilder();
            builder.AppendLine("NON-CANON WORLD CREATOR BATCH 4 HYBRID TERRAIN QUERY REPORT");
            builder.AppendLine("Dormant technical evidence only; production terrain, globe, coordinate regions, and lore remain unchanged.");
            builder.Append("fingerprint=").Append(fixture.Plan.Fingerprint).AppendLine();
            builder.Append("canyonSegments=").Append(fixture.Plan.CanyonSegments.Count)
                .Append(", boundedLandformRequests=").Append(fixture.Plan.LandformRequests.Count)
                .Append(", syntheticHistories=").Append(fixture.Plan.Histories.Count).AppendLine();
            builder.AppendLine("streaming: immutable absolute-key plans can unload and rebuild without scene ownership");
            builder.AppendLine("stable identity: canyon, bounded-feature, history, reservation, and operation IDs are deterministic");
            builder.AppendLine("authored constraints: agent clearance plus reservation footprint and vertical authority are enforced");
            builder.AppendLine("persisted deltas: not applicable in Batch 4; canonical immutable IDs are prepared for later delta references");
            return builder.ToString();
        }

        private static void AppendPanel(
            StringBuilder builder,
            IWorldQueryService query,
            double offsetX,
            double offsetY,
            double panelSize,
            double cell,
            int samples,
            double extent,
            PanelMode mode)
        {
            builder.AppendLine("<g>");
            for (var z = 0; z < samples; z++)
            {
                for (var x = 0; x < samples; x++)
                {
                    var worldA = -extent + (x + 0.5d) / samples * extent * 2d;
                    var worldB = -extent + (z + 0.5d) / samples * extent * 2d;
                    if (!query.TrySampleSurface(new AbsoluteWorldPosition(worldA, 0d, worldB), out var sample, out var error))
                        throw new InvalidOperationException(error);
                    var color = mode == PanelMode.Height ? HeightColor(sample.Position.Vertical) : SemanticColor(sample.Semantic);
                    builder.Append("<rect x=\"").Append(F(offsetX + x * cell)).Append("\" y=\"")
                        .Append(F(offsetY + (samples - z - 1) * cell)).Append("\" width=\"")
                        .Append(F(cell + 0.2d)).Append("\" height=\"").Append(F(cell + 0.2d))
                        .Append("\" fill=\"").Append(color).AppendLine("\"/>");
                }
            }
            builder.Append("<rect x=\"").Append(F(offsetX)).Append("\" y=\"").Append(F(offsetY))
                .Append("\" width=\"").Append(F(panelSize)).Append("\" height=\"").Append(F(panelSize))
                .AppendLine("\" fill=\"none\" stroke=\"#dbe4ec\" stroke-width=\"2\"/></g>");
        }

        private static string HeightColor(double height)
        {
            var normalized = Math.Max(0d, Math.Min(1d, (height + 90d) / 180d));
            var red = (int)(52 + normalized * 151);
            var green = (int)(43 + normalized * 97);
            var blue = (int)(42 + normalized * 58);
            return $"#{red:x2}{green:x2}{blue:x2}";
        }

        private static string SemanticColor(WorldSurfaceSemantic semantic)
        {
            if ((semantic & WorldSurfaceSemantic.Approach) != 0) return "#f4d35e";
            if ((semantic & WorldSurfaceSemantic.Disturbed) != 0) return "#d1495b";
            if ((semantic & WorldSurfaceSemantic.Buried) != 0) return "#b68f40";
            if ((semantic & WorldSurfaceSemantic.SiteReservation) != 0) return "#8f63b8";
            if ((semantic & WorldSurfaceSemantic.BoundedLandform) != 0) return "#ef8354";
            if ((semantic & WorldSurfaceSemantic.CanyonFloor) != 0) return "#315b63";
            if ((semantic & WorldSurfaceSemantic.CanyonShelf) != 0) return "#4f7772";
            if ((semantic & WorldSurfaceSemantic.CanyonWall) != 0) return "#7b6651";
            return "#8c8072";
        }

        private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        private enum PanelMode : byte { Height, Semantic }

        public sealed class ProofFixture
        {
            internal ProofFixture(
                WorldIdentity world,
                IWorldCoordinateModel model,
                IWorldCoordinateContextProvider context,
                IReadOnlyList<CanyonSystemPlan> canyonPlans,
                HybridTerrainPlan plan,
                DormantHybridWorldQueryService query)
            {
                World = world;
                Model = model;
                Context = context;
                CanyonPlans = canyonPlans;
                Plan = plan;
                Query = query;
            }

            public WorldIdentity World { get; }
            public IWorldCoordinateModel Model { get; }
            public IWorldCoordinateContextProvider Context { get; }
            public IReadOnlyList<CanyonSystemPlan> CanyonPlans { get; }
            public HybridTerrainPlan Plan { get; }
            public DormantHybridWorldQueryService Query { get; }
        }
    }
}
