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
            var foundAuthoredMixedFormation = false;
            for (var i = 0; i < first.Count; i++)
            {
                Assert.That(ids.Add(first[i].StableId), Is.True);
                Assert.That(second[i].StableId, Is.EqualTo(first[i].StableId));
                Assert.That(second[i].Members.Count, Is.EqualTo(first[i].Members.Count));
                if (first[i].Members.Count != settings.MixedFormationTemplate.Members.Count
                    || first[i].Members[0].AuthoredFamily == null)
                {
                    continue;
                }

                foundAuthoredMixedFormation = true;
                for (var memberIndex = 0; memberIndex < first[i].Members.Count; memberIndex++)
                {
                    Assert.That(first[i].Members[memberIndex].AuthoredFamily, Is.Not.Null);
                    Assert.That(first[i].Members[memberIndex].AuthoredMaterial, Is.Not.Null);
                }
            }

            Assert.That(foundAuthoredMixedFormation, Is.True,
                "The production search area should realize at least one baked mixed formation.");
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
