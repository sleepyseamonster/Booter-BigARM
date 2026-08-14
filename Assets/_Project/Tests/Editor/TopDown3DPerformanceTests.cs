using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DPerformanceTests
    {
        private const string WorldSettingsPath =
            "Assets/_Project/Settings/World/TopDown3DWorldSettings.asset";
        private const string TerrainMaterialPath =
            "Assets/_Project/Materials/TopDown3D/Greybox_Terrain.mat";
        private const string RockMaterialPath =
            "Assets/_Project/Materials/TopDown3D/Greybox_Rock.mat";

        [Test]
        public void StartupBuildsImmediateTerrainButDefersNonCenterDecoration()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(WorldSettingsPath);
            var terrainMaterial = AssetDatabase.LoadAssetAtPath<Material>(TerrainMaterialPath);
            var rockMaterial = AssetDatabase.LoadAssetAtPath<Material>(RockMaterialPath);
            Assert.That(settings, Is.Not.Null);
            Assert.That(terrainMaterial, Is.Not.Null);
            Assert.That(rockMaterial, Is.Not.Null);

            var worldObject = new GameObject("Staged startup performance contract");
            var targetObject = new GameObject("Staged startup target");
            try
            {
                targetObject.AddComponent<CapsuleCollider>();
                var world = worldObject.AddComponent<TopDown3DProceduralWorld>();
                world.Configure(settings, targetObject.transform, terrainMaterial, rockMaterial);

                InvokePrivate(world, "Start");

                var immediateDiameter = settings.ImmediateLoadRadius * 2 + 1;
                var immediateChunkCount = immediateDiameter * immediateDiameter;
                var streamingDiameter = settings.StreamingRadius * 2 + 1;
                var streamingChunkCount = streamingDiameter * streamingDiameter;
                Assert.That(world.LoadedChunkCount, Is.EqualTo(immediateChunkCount));
                Assert.That(world.DecoratedChunkCount, Is.EqualTo(1));
                Assert.That(world.PendingDecorationCount, Is.EqualTo(immediateChunkCount - 1));
                Assert.That(
                    world.PendingTerrainChunkCount,
                    Is.EqualTo(streamingChunkCount - immediateChunkCount));

                InvokePrivate(world, "ProcessPendingChunks", 1);

                Assert.That(world.LoadedChunkCount, Is.EqualTo(immediateChunkCount));
                Assert.That(world.DecoratedChunkCount, Is.EqualTo(2));
                Assert.That(world.PendingDecorationCount, Is.EqualTo(immediateChunkCount - 2));

                targetObject.transform.position += Vector3.right * settings.ChunkSize;
                InvokePrivate(world, "RefreshChunks", false);
                var pendingAfterCenterChange = world.PendingDecorationCount;
                InvokePrivate(world, "RefreshChunks", false);

                Assert.That(world.PendingDecorationCount, Is.EqualTo(pendingAfterCenterChange));
                Assert.That(pendingAfterCenterChange, Is.EqualTo(immediateChunkCount - 2));
            }
            finally
            {
                Object.DestroyImmediate(worldObject);
                Object.DestroyImmediate(targetObject);
            }
        }

        private static void InvokePrivate(
            TopDown3DProceduralWorld world,
            string methodName,
            params object[] arguments)
        {
            var method = typeof(TopDown3DProceduralWorld).GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Missing lifecycle method {methodName}.");
            method.Invoke(world, arguments);
        }
    }
}
