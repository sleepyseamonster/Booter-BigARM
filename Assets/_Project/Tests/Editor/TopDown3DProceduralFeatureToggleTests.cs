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
        public void ActiveWorld_UsesVersionedGeologyWithRaisedDepositsDisabled()
        {
            var settings = LoadSettings();

            Assert.That(settings.TerrainGenerationVersion, Is.GreaterThanOrEqualTo(2));
            Assert.That(settings.GeologyProfile, Is.Not.Null);
            Assert.That(settings.GenerateDepositedDust, Is.False);
        }

        [Test]
        public void EnabledTerrainSand_RebuildsIdenticallyAfterDecorationUnload()
        {
            var settings = Object.Instantiate(LoadSettings());
            var serialized = new SerializedObject(settings);
            serialized.FindProperty("generateDepositedDust").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var generator = new TopDown3DWorldGenerator(settings);
            var exclusion = new Vector2(10000f, 10000f);
            var coordinate = FindVisibleDepositChunk(settings, generator, exclusion);
            var chunkObject = new GameObject("Streaming Terrain Sand Test Chunk");
            try
            {
                var chunk = chunkObject.AddComponent<TopDown3DGeneratedChunk>();
                chunk.Initialize(coordinate, null);

                TopDown3DDustDepositionDecorator.Decorate(
                    chunk,
                    settings,
                    generator,
                    settings.DepositedDustMaterial,
                    exclusion);

                var first = chunk.transform.Find("Streamed Decoration/Wind Deposited Dust");
                Assert.That(first, Is.Not.Null);
                var firstMesh = first.GetComponent<MeshFilter>().sharedMesh;
                var firstVertices = firstMesh.vertices;
                var firstTriangles = firstMesh.triangles;
                Assert.That(chunk.DecorationMeshCount, Is.EqualTo(1));

                chunk.ClearDecoration();
                Assert.That(chunk.transform.Find("Streamed Decoration"), Is.Null);
                Assert.That(chunk.DecorationMeshCount, Is.Zero);

                TopDown3DDustDepositionDecorator.Decorate(
                    chunk,
                    settings,
                    generator,
                    settings.DepositedDustMaterial,
                    exclusion);

                var rebuilt = chunk.transform.Find("Streamed Decoration/Wind Deposited Dust");
                Assert.That(rebuilt, Is.Not.Null);
                var rebuiltMesh = rebuilt.GetComponent<MeshFilter>().sharedMesh;
                Assert.That(rebuiltMesh.vertices, Is.EqualTo(firstVertices));
                Assert.That(rebuiltMesh.triangles, Is.EqualTo(firstTriangles));
            }
            finally
            {
                Object.DestroyImmediate(chunkObject);
                Object.DestroyImmediate(settings);
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

        private static Vector2Int FindVisibleDepositChunk(
            TopDown3DWorldSettings settings,
            TopDown3DWorldGenerator generator,
            Vector2 exclusion)
        {
            for (var z = -8; z <= 8; z++)
            {
                for (var x = -8; x <= 8; x++)
                {
                    var coordinate = new Vector2Int(x, z);
                    if (TopDown3DDustDepositionPlanner.BuildPlan(
                            settings,
                            generator,
                            settings.NaturalObjectCatalog,
                            coordinate,
                            exclusion).HasVisibleDeposits)
                    {
                        return coordinate;
                    }
                }
            }

            Assert.Fail("Expected at least one visible terrain-sand chunk in the bounded search area.");
            return default;
        }
    }
}
