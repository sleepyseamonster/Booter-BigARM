using System.Collections.Generic;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DRockFormationGenerationServiceTests
    {
        private const string SettingsPath =
            "Assets/_Project/Settings/World/TopDown3DWorldSettings.asset";

        [Test]
        public void BuildAround_IsDeterministicAndReturnsUniqueStableFormations()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(SettingsPath);
            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.NaturalObjectCatalog, Is.Not.Null);
            Assert.That(settings.MixedFormationTemplate, Is.Not.Null);
            Assert.That(settings.MixedFormationTemplate.HasBakedVariants, Is.True);
            Assert.That(settings.AuthoredFormationCatalog, Is.Not.Null);
            Assert.That(settings.AuthoredFormationCatalog.IsComplete, Is.True);

            var first = TopDown3DRockFormationGenerationService.BuildAround(
                settings,
                Vector2.zero,
                8);
            var second = TopDown3DRockFormationGenerationService.BuildAround(
                settings,
                Vector2.zero,
                8);

            Assert.That(first.Count, Is.GreaterThan(0));
            Assert.That(second.Count, Is.EqualTo(first.Count));
            var ids = new HashSet<string>();
            var selectedTemplates = new HashSet<TopDown3DAuthoredFormationAsset>();
            var foundAuthoredMixedFormation = false;
            for (var i = 0; i < first.Count; i++)
            {
                Assert.That(ids.Add(first[i].StableId), Is.True);
                Assert.That(second[i].StableId, Is.EqualTo(first[i].StableId));
                Assert.That(second[i].Members.Count, Is.EqualTo(first[i].Members.Count));
                Assert.That(first[i].Members.Count, Is.GreaterThan(0));
                foundAuthoredMixedFormation = true;
                var template = settings.SelectAuthoredFormation(first[i].StableId);
                selectedTemplates.Add(template);
                Assert.That(first[i].AuthoredTemplate, Is.SameAs(template));
                var expectedStage = TopDown3DAuthoredFormationPlacement.BuildProceduralStage(
                    template, first[i].StableId);
                var expectedCount = expectedStage.Count;
                Assert.That(first[i].Members.Count, Is.EqualTo(expectedCount),
                    "A streamed formation must retain every seeded workbench member.");
                for (var memberIndex = 0; memberIndex < first[i].Members.Count; memberIndex++)
                {
                    Assert.That(first[i].Members[memberIndex].AuthoredFamily,
                        Is.SameAs(expectedStage[memberIndex].Family));
                    Assert.That(second[i].Members[memberIndex].AuthoredFamily,
                        Is.SameAs(first[i].Members[memberIndex].AuthoredFamily));
                    Assert.That(first[i].Members[memberIndex].AuthoredMaterial, Is.Not.Null);
                }
            }

            Assert.That(foundAuthoredMixedFormation, Is.True,
                "The production search area should realize at least one authored mixed formation.");
            Assert.That(selectedTemplates, Is.EquivalentTo(settings.AuthoredFormationCatalog.Templates),
                "The populated production search must realize both Scatter and Handbuilt Spire.");
        }

        [Test]
        public void AuthoredFormationSelection_UsesBothApprovedTemplatesDeterministically()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(SettingsPath);
            Assert.That(settings, Is.Not.Null);

            var selected = new HashSet<TopDown3DAuthoredFormationAsset>();
            for (var index = 0; index < 128; index++)
            {
                var id = "formation:reservation:" + index;
                var first = settings.SelectAuthoredFormation(id);
                Assert.That(first, Is.SameAs(settings.SelectAuthoredFormation(id)));
                selected.Add(first);
            }

            Assert.That(selected, Is.EquivalentTo(settings.AuthoredFormationCatalog.Templates),
                "Eligible world reservations must use both the scatter and hand-built spire templates.");
        }

        [Test]
        public void ProceduralStages_ProduceDistinctRepeatableWorkbenchCompositions()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(SettingsPath);
            foreach (var template in settings.AuthoredFormationCatalog.Templates)
            {
                var firstRockPoses = new HashSet<Matrix4x4>();
                for (var index = 0; index < 64; index++)
                {
                    var id = "seeded-workbench-formation:" + index;
                    var first = TopDown3DAuthoredFormationPlacement.BuildProceduralStage(template, id);
                    var second = TopDown3DAuthoredFormationPlacement.BuildProceduralStage(template, id);
                    Assert.That(first.Count, Is.EqualTo(second.Count));
                    Assert.That(first.Count, Is.GreaterThanOrEqualTo(15));
                    for (var member = 0; member < first.Count; member++)
                    {
                        Assert.That(first[member].InstanceId, Is.EqualTo(second[member].InstanceId));
                        Assert.That(first[member].LocalPose, Is.EqualTo(second[member].LocalPose));
                        Assert.That(first[member].Family, Is.SameAs(second[member].Family));
                        Assert.That(first[member].Family.IsComplete, Is.True);
                    }
                    firstRockPoses.Add(first[0].LocalPose);
                }
                Assert.That(firstRockPoses.Count, Is.GreaterThan(48),
                    "New reservations should create many distinct arrangements in " + template.name);
            }
        }

        [Test]
        public void BuildAround_RejectsAnUnboundedSearch()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(SettingsPath);
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                TopDown3DRockFormationGenerationService.BuildAround(settings, Vector2.zero, 17));
        }
    }
}
