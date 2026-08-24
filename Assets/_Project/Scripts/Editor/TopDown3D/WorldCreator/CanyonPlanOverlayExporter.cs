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
    public static class CanyonPlanOverlayExporter
    {
        public const string DefaultSvgPath = "/tmp/booter-worldcreator-batch3-canyon-overlay.svg";
        public const string DefaultReportPath = "/tmp/booter-worldcreator-batch3-canyon-report.txt";

        [MenuItem("Booter & BigARM/World Creator/Export Batch 3 Non-Canon Canyon Overlay")]
        public static void ExportFromMenu()
        {
            ExportFromCli();
        }

        public static void ExportFromCli()
        {
            var svg = BuildProofOverlaySvg();
            var report = BuildProofReport();
            File.WriteAllText(DefaultSvgPath, svg, new UTF8Encoding(false));
            File.WriteAllText(DefaultReportPath, report, new UTF8Encoding(false));
            Debug.Log(
                $"Wrote non-canon Batch 3 canyon graph overlay to {DefaultSvgPath} and report to {DefaultReportPath}.\n{report}");
        }

        public static string BuildProofOverlaySvg(int radius = 1)
        {
            var proof = BuildProof(radius);
            var span = proof.Profile.CellSpan;
            var minimum = -radius * span;
            var maximum = (radius + 1) * span;
            var margin = span * 0.16d;
            var size = maximum - minimum + margin * 2d;
            var builder = new StringBuilder(32768);
            builder.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            builder.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 ")
                .Append(F(size)).Append(' ').Append(F(size)).AppendLine("\">");
            builder.AppendLine("<title>NON-CANON WORLD CREATOR BATCH 3 CANYON GRAPH OVERLAY</title>");
            builder.AppendLine("<desc>Disposable technical planning evidence only; not globe, coordinate, region, terrain, or lore canon.</desc>");
            builder.AppendLine("<rect width=\"100%\" height=\"100%\" fill=\"#11151c\"/>");
            builder.AppendLine("<g stroke=\"#34404c\" stroke-width=\"2\" fill=\"none\">");
            for (var index = -radius; index <= radius + 1; index++)
            {
                var position = margin + (index + radius) * span;
                builder.Append("<line x1=\"").Append(F(position)).Append("\" y1=\"").Append(F(margin))
                    .Append("\" x2=\"").Append(F(position)).Append("\" y2=\"").Append(F(size - margin))
                    .AppendLine("\"/>");
                builder.Append("<line x1=\"").Append(F(margin)).Append("\" y1=\"").Append(F(position))
                    .Append("\" x2=\"").Append(F(size - margin)).Append("\" y2=\"").Append(F(position))
                    .AppendLine("\"/>");
            }

            builder.AppendLine("</g>");
            var nodeById = CollectNodes(proof.Plans);
            var drawnSegments = new HashSet<WorldFeatureId>();
            builder.AppendLine("<g fill=\"none\" stroke-linecap=\"round\">");
            for (var planIndex = 0; planIndex < proof.Plans.Count; planIndex++)
            {
                var plan = proof.Plans[planIndex];
                for (var segmentIndex = 0; segmentIndex < plan.Segments.Count; segmentIndex++)
                {
                    var segment = plan.Segments[segmentIndex];
                    if (!drawnSegments.Add(segment.Id))
                    {
                        continue;
                    }

                    var from = nodeById[segment.FromNodeId];
                    var to = nodeById[segment.ToNodeId];
                    var color = segment.Traversal.SupportsBigArm ? "#65d5d0" : "#f0a35a";
                    var width = segment.Hierarchy switch
                    {
                        CanyonHierarchyTier.MainSpine => 9,
                        CanyonHierarchyTier.Tributary => 6,
                        _ => 3
                    };
                    builder.Append("<line x1=\"").Append(F(X(from.Position))).Append("\" y1=\"")
                        .Append(F(Y(from.Position))).Append("\" x2=\"").Append(F(X(to.Position)))
                        .Append("\" y2=\"").Append(F(Y(to.Position))).Append("\" stroke=\"")
                        .Append(color).Append("\" stroke-width=\"").Append(width)
                        .Append("\" opacity=\"0.88\"><title>").Append(segment.Hierarchy).Append(" | ")
                        .Append(segment.Traversal.Access).Append(" | ").Append(segment.Id)
                        .AppendLine("</title></line>");
                }
            }

            builder.AppendLine("</g>");
            builder.AppendLine("<g stroke=\"#e8edf2\" stroke-width=\"2\">");
            var drawnNodes = new List<CanyonNodePlan>(nodeById.Values);
            drawnNodes.Sort((left, right) => left.Id.CompareTo(right.Id));
            for (var i = 0; i < drawnNodes.Count; i++)
            {
                var node = drawnNodes[i];
                var isPort = (node.Role & CanyonNodeRole.BoundaryPort) != 0;
                var isAnchor = (node.Role & CanyonNodeRole.RegroupAnchor) != 0;
                var color = isAnchor ? "#f6e27a" : isPort ? "#b6c9dc" : "#d879c5";
                var radiusPixels = isAnchor ? 10 : isPort ? 7 : 5;
                builder.Append("<circle cx=\"").Append(F(X(node.Position))).Append("\" cy=\"")
                    .Append(F(Y(node.Position))).Append("\" r=\"").Append(radiusPixels)
                    .Append("\" fill=\"").Append(color).Append("\"><title>")
                    .Append(node.Role).Append(" | width ").Append(F(node.CrossSection.Width))
                    .Append(" | depth ").Append(F(node.CrossSection.Depth)).Append(" | ")
                    .Append(node.Id).AppendLine("</title></circle>");
            }

            builder.AppendLine("</g>");
            builder.AppendLine("<g font-family=\"monospace\" fill=\"#e8edf2\">");
            builder.Append("<text x=\"").Append(F(margin)).Append("\" y=\"30\" font-size=\"20\">")
                .AppendLine("NON-CANON BATCH 3 CANYON GRAPH</text>");
            builder.Append("<text x=\"").Append(F(margin)).Append("\" y=\"52\" font-size=\"12\">")
                .AppendLine("technical proof only | cyan: BigARM-compatible | orange: Booter-only | yellow: regroup anchor</text>");
            builder.AppendLine("</g></svg>");
            return builder.ToString();

            double X(AbsoluteWorldPosition position)
            {
                return margin + position.HorizontalA - minimum;
            }

            double Y(AbsoluteWorldPosition position)
            {
                return margin + maximum - position.HorizontalB;
            }
        }

        public static string BuildProofReport(int radius = 1)
        {
            var proof = BuildProof(radius);
            var builder = new StringBuilder(16384);
            builder.AppendLine("NON-CANON WORLD CREATOR BATCH 3 CANYON GRAPH REPORT");
            builder.AppendLine("Disposable technical planning evidence only; not globe, coordinate, region, terrain, or lore canon.");
            builder.Append("profile: cellSpan=").Append(F(proof.Profile.CellSpan))
                .Append(", haloRadius=").Append(proof.Profile.HaloPolicy.TopologicalRadius)
                .Append(", maxParticipants=").Append(proof.Profile.HaloPolicy.MaximumParticipants)
                .Append(", cycles=").Append(proof.Profile.AllowDeclaredCycles ? "declared-only" : "acyclic")
                .AppendLine();
            var nodeById = CollectNodes(proof.Plans);
            var segmentIds = new HashSet<WorldFeatureId>();
            for (var i = 0; i < proof.Plans.Count; i++)
            {
                var plan = proof.Plans[i];
                for (var segmentIndex = 0; segmentIndex < plan.Segments.Count; segmentIndex++)
                {
                    segmentIds.Add(plan.Segments[segmentIndex].Id);
                }

                builder.Append(plan.Cell).Append(" fingerprint=").Append(plan.Header.ContentFingerprint)
                    .Append(" nodes=").Append(plan.Nodes.Count)
                    .Append(" segments=").Append(plan.Segments.Count)
                    .Append(" contexts=").Append(plan.PlanningCost.ContextSamples)
                    .Append(" boundaries=").Append(plan.PlanningCost.BoundaryEvaluations)
                    .Append(" regroup=").Append(plan.RegroupAnchorNodeId)
                    .AppendLine();
            }

            var seamCount = 0;
            for (var i = 0; i < proof.Plans.Count; i++)
            {
                for (var j = i + 1; j < proof.Plans.Count; j++)
                {
                    var distance = Math.Abs(proof.Plans[i].Cell.HorizontalA - proof.Plans[j].Cell.HorizontalA)
                        + Math.Abs(proof.Plans[i].Cell.HorizontalB - proof.Plans[j].Cell.HorizontalB);
                    if (distance != 1)
                    {
                        continue;
                    }

                    if (!CanyonGraphValidator.TryValidateBoundaryAgreement(
                            proof.Plans[i],
                            proof.Plans[j],
                            out var error))
                    {
                        throw new InvalidOperationException(error);
                    }

                    seamCount++;
                }
            }

            builder.Append("summary: plans=").Append(proof.Plans.Count)
                .Append(", uniqueNodes=").Append(nodeById.Count)
                .Append(", uniqueSegments=").Append(segmentIds.Count)
                .Append(", validatedSeams=").Append(seamCount)
                .AppendLine();
            return builder.ToString();
        }

        public static IReadOnlyList<CanyonSystemPlan> BuildProofPlans(int radius = 1)
        {
            return BuildProof(radius).Plans;
        }

        private static ProofBuild BuildProof(int radius)
        {
            if (radius < 1 || radius > 4)
            {
                throw new ArgumentOutOfRangeException(nameof(radius));
            }

            WorldCreatorAssetValidator.ValidateFromCli();
            var provinces = AssetDatabase.LoadAssetAtPath<GeologicProvinceCatalog>(
                WorldCreatorBatch2ProofAssetBuilder.ProvinceCatalogPath);
            var strata = AssetDatabase.LoadAssetAtPath<StrataFamilyCatalog>(
                WorldCreatorBatch2ProofAssetBuilder.StrataCatalogPath);
            var influence = AssetDatabase.LoadAssetAtPath<NonCanonProofInfluenceProfile>(
                WorldCreatorBatch2ProofAssetBuilder.InfluenceProfilePath);
            var contextProvider = new WorldCoordinateContextSampler(provinces, strata, influence);
            var profile = CanyonPlannerProfile.CreateNonCanonTechnicalProofProfile();
            var planner = new CanyonSystemPlanner(contextProvider, profile);
            var coordinateModel = new NonCanonProofCoordinateModel();
            var world = new WorldIdentity(24681357L, new WorldVersionManifest(1, 1, 1, 1, 1, 1, 1));
            var plans = new List<CanyonSystemPlan>();
            for (var horizontalA = -radius; horizontalA <= radius; horizontalA++)
            {
                for (var horizontalB = -radius; horizontalB <= radius; horizontalB++)
                {
                    if (!planner.TryBuild(
                            world,
                            coordinateModel,
                            new CanyonSystemCellIndex(horizontalA, horizontalB),
                            out var plan,
                            out var error))
                    {
                        throw new InvalidOperationException(error);
                    }

                    plans.Add(plan);
                }
            }

            plans.Sort((left, right) => left.Cell.CompareTo(right.Cell));
            _ = new CanyonMacroRouteGraph(plans);
            return new ProofBuild(profile, plans.AsReadOnly());
        }

        private static Dictionary<WorldFeatureId, CanyonNodePlan> CollectNodes(
            IReadOnlyList<CanyonSystemPlan> plans)
        {
            var result = new Dictionary<WorldFeatureId, CanyonNodePlan>();
            for (var planIndex = 0; planIndex < plans.Count; planIndex++)
            {
                for (var nodeIndex = 0; nodeIndex < plans[planIndex].Nodes.Count; nodeIndex++)
                {
                    var node = plans[planIndex].Nodes[nodeIndex];
                    if (result.TryGetValue(node.Id, out var existing) && !existing.Equals(node))
                    {
                        throw new InvalidOperationException($"Proof plans disagree about shared node '{node.Id}'.");
                    }

                    result[node.Id] = node;
                }
            }

            return result;
        }

        private static string F(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private readonly struct ProofBuild
        {
            public ProofBuild(CanyonPlannerProfile profile, IReadOnlyList<CanyonSystemPlan> plans)
            {
                Profile = profile;
                Plans = plans;
            }

            public CanyonPlannerProfile Profile { get; }
            public IReadOnlyList<CanyonSystemPlan> Plans { get; }
        }
    }
}
