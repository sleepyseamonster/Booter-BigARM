using System;
using System.Globalization;
using System.IO;
using System.Text;
using BooterBigArm.TopDown3D.WorldCreator;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Editor.WorldCreator
{
    public static class WorldPlanInspector
    {
        public const string DefaultReportPath = "/tmp/booter-worldcreator-batch2-transect.txt";

        [MenuItem("Booter & BigARM/World Creator/Log Batch 2 Non-Canon Transect")]
        public static void LogFromMenu()
        {
            Debug.Log(BuildBatch2TransectReport());
        }

        public static void InspectBatch2FromCli()
        {
            var report = BuildBatch2TransectReport();
            File.WriteAllText(DefaultReportPath, report, new UTF8Encoding(false));
            Debug.Log($"Wrote non-canon World Creator Batch 2 transect report to {DefaultReportPath}.\n{report}");
        }

        public static string BuildBatch2TransectReport(int samplesPerTrack = 17)
        {
            if (samplesPerTrack < 3 || samplesPerTrack > 129)
            {
                throw new ArgumentOutOfRangeException(nameof(samplesPerTrack));
            }

            WorldCreatorAssetValidator.ValidateFromCli();
            var provinces = AssetDatabase.LoadAssetAtPath<GeologicProvinceCatalog>(
                WorldCreatorBatch2ProofAssetBuilder.ProvinceCatalogPath);
            var strata = AssetDatabase.LoadAssetAtPath<StrataFamilyCatalog>(
                WorldCreatorBatch2ProofAssetBuilder.StrataCatalogPath);
            var influence = AssetDatabase.LoadAssetAtPath<NonCanonProofInfluenceProfile>(
                WorldCreatorBatch2ProofAssetBuilder.InfluenceProfilePath);
            var model = new NonCanonProofCoordinateModel();
            var sampler = new WorldCoordinateContextSampler(provinces, strata, influence);
            var world = new WorldIdentity(24681357L, new WorldVersionManifest(1, 1, 1, 1, 1, 1, 1));
            var start = influence.GradualStartHorizontalA - 384d;
            var end = influence.GradualEndHorizontalA + 384d;
            var belowBoundary = influence.IntentionalBoundaryHorizontalB - 0.001d;
            var aboveBoundary = influence.IntentionalBoundaryHorizontalB + 0.001d;
            var builder = new StringBuilder(8192);
            builder.AppendLine("NON-CANON WORLD CREATOR BATCH 2 PROOF TRANSECT");
            builder.AppendLine("Disposable technical evidence only; not globe, coordinate, region, or lore canon.");
            builder.AppendLine("columns: track,index,address,influenceWeight,hardSignal,province,strata,relief,canyonDensity,canyonDepth,fracture,contextId");
            AppendTrack("gradual-side", belowBoundary);
            AppendTrack("hard-side", aboveBoundary);
            return builder.ToString();

            void AppendTrack(string track, double horizontalB)
            {
                for (var i = 0; i < samplesPerTrack; i++)
                {
                    var t = i / (double)(samplesPerTrack - 1);
                    var position = new AbsoluteWorldPosition(start + (end - start) * t, 0d, horizontalB);
                    var address = model.Encode(position);
                    if (!sampler.TrySample(world, model, address, out var context, out var error))
                    {
                        throw new InvalidOperationException(error);
                    }

                    builder.Append(track).Append(',')
                        .Append(i.ToString(CultureInfo.InvariantCulture)).Append(',')
                        .Append(address.CanonicalValue).Append(',')
                        .Append(GetInfluenceWeight(context).ToString("R", CultureInfo.InvariantCulture)).Append(',')
                        .Append(context.DeclaredDiscontinuitySignal.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                        .Append(context.DominantProvinceId).Append(',')
                        .Append(context.DominantStrataFamilyId).Append(',')
                        .Append(context.Landscape.Relief.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                        .Append(context.Landscape.CanyonDensity.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                        .Append(context.Landscape.CanyonDepth.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                        .Append(context.StrataFracture.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                        .Append(context.ContextId)
                        .AppendLine();
                }
            }
        }

        private static float GetInfluenceWeight(WorldCoordinateContext context)
        {
            for (var i = 0; i < context.Contributions.Count; i++)
            {
                var contribution = context.Contributions[i];
                if (string.Equals(
                        contribution.InfluenceId,
                        NonCanonProofInfluenceProfile.RequiredIdPrefix + "influence.fractured",
                        StringComparison.Ordinal))
                {
                    return contribution.Weight;
                }
            }

            throw new InvalidOperationException("The proof context is missing its fractured influence contribution.");
        }
    }
}
