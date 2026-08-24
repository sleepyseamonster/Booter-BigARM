using BooterBigArm.Editor.WorldCreator;
using BooterBigArm.TopDown3D.WorldCreator;
using NUnit.Framework;
using UnityEditor;

namespace BooterBigArm.Tests.WorldCreator
{
    public sealed class WorldCreatorBatch2AssetTests
    {
        [Test]
        public void ProofAssetsValidateAndRemainDormant()
        {
            Assert.That(WorldCreatorAssetValidator.CollectErrors(), Is.Empty);
        }

        [Test]
        public void ProofAssetsRemainExplicitlyNonCanon()
        {
            var provinces = AssetDatabase.LoadAssetAtPath<GeologicProvinceCatalog>(
                WorldCreatorBatch2ProofAssetBuilder.ProvinceCatalogPath);
            var strata = AssetDatabase.LoadAssetAtPath<StrataFamilyCatalog>(
                WorldCreatorBatch2ProofAssetBuilder.StrataCatalogPath);
            var influence = AssetDatabase.LoadAssetAtPath<NonCanonProofInfluenceProfile>(
                WorldCreatorBatch2ProofAssetBuilder.InfluenceProfilePath);

            Assert.That(provinces, Is.Not.Null);
            Assert.That(strata, Is.Not.Null);
            Assert.That(influence, Is.Not.Null);
            Assert.That(provinces.NonCanonProofOnly, Is.True);
            Assert.That(strata.NonCanonProofOnly, Is.True);
            Assert.That(influence.NonCanonProofOnly, Is.True);
            Assert.That(influence.StableId, Does.StartWith(NonCanonProofInfluenceProfile.RequiredIdPrefix));
        }

        [Test]
        public void TransectReportIsDeterministicAndSelfLabelsAsNonCanon()
        {
            var first = WorldPlanInspector.BuildBatch2TransectReport(9);
            var second = WorldPlanInspector.BuildBatch2TransectReport(9);

            Assert.That(second, Is.EqualTo(first));
            Assert.That(first, Does.StartWith("NON-CANON WORLD CREATOR BATCH 2 PROOF TRANSECT"));
            Assert.That(first, Does.Contain("not globe, coordinate, region, or lore canon"));
            Assert.That(first, Does.Contain("gradual-side"));
            Assert.That(first, Does.Contain("hard-side"));
        }
    }
}
