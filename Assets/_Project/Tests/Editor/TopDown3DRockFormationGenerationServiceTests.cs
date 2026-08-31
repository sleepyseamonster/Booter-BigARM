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
            for (var i = 0; i < first.Count; i++)
            {
                Assert.That(ids.Add(first[i].StableId), Is.True);
                Assert.That(second[i].StableId, Is.EqualTo(first[i].StableId));
                Assert.That(second[i].Members.Count, Is.EqualTo(first[i].Members.Count));
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
