using System.Collections.Generic;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DProceduralFeatureToggleTests
    {
        private const string WorldSettingsPath =
            "Assets/_Project/Settings/World/TopDown3DWorldSettings.asset";

        [Test]
        public void ActiveWorld_KeepsEscarpmentsAndDepositedDustDormant()
        {
            var settings = LoadSettings();

            Assert.That(settings.GenerateEscarpments, Is.False);
            Assert.That(settings.GenerateDepositedDust, Is.False);
            Assert.That(
                TopDown3DEscarpmentSampler.SampleElevation(settings, 32f, -19f),
                Is.EqualTo(0f));

            var features = new List<TopDown3DEscarpmentFeature>();
            TopDown3DEscarpmentSampler.CollectFeatures(
                settings,
                new Rect(-120f, -120f, 240f, 240f),
                features);
            Assert.That(features, Is.Empty);
        }

        [Test]
        public void DisabledDustDecorator_CreatesNoRuntimeOverlay()
        {
            var settings = LoadSettings();
            var chunkObject = new GameObject("Dormant Dust Test Chunk");
            try
            {
                var chunk = chunkObject.AddComponent<TopDown3DGeneratedChunk>();
                chunk.Initialize(Vector2Int.zero, null);

                TopDown3DDustDepositionDecorator.Decorate(
                    chunk,
                    settings,
                    settings.DepositedDustMaterial,
                    new Vector2(10000f, 10000f));

                Assert.That(chunk.transform.Find("Wind Deposited Dust"), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(chunkObject);
            }
        }

        [Test]
        public void FreshSettings_KeepBothModulesAvailableForLater()
        {
            var settings = ScriptableObject.CreateInstance<TopDown3DWorldSettings>();
            try
            {
                Assert.That(settings.GenerateEscarpments, Is.True);
                Assert.That(settings.GenerateDepositedDust, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }

        private static TopDown3DWorldSettings LoadSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(WorldSettingsPath);
            Assert.That(settings, Is.Not.Null);
            return settings;
        }
    }
}
