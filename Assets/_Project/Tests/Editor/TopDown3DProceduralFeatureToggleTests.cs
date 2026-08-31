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
        public void ActiveWorld_UsesVersionedGeologyAndKeepsDepositedDustDormant()
        {
            var settings = LoadSettings();

            Assert.That(settings.TerrainGenerationVersion, Is.GreaterThanOrEqualTo(2));
            Assert.That(settings.GeologyProfile, Is.Not.Null);
            Assert.That(settings.GenerateDepositedDust, Is.False);
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
                    new TopDown3DWorldGenerator(settings),
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
        public void FreshSettings_ProvideGeologyAndDepositedDustSettings()
        {
            var settings = ScriptableObject.CreateInstance<TopDown3DWorldSettings>();
            try
            {
                Assert.That(settings.GeologyProfile, Is.Not.Null);
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
