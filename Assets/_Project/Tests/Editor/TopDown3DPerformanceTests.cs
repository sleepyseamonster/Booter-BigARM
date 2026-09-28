using System.Collections;
using System.IO;
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
        private const string TerrainShaderPath =
            "Assets/_Project/Shaders/TopDown3D/BrokenWorldTerrainBlend.shader";

        [Test]
        public void FastTerrainPathRetainsMediumShaleTransitionAlbedo()
        {
            var source = File.ReadAllText(TerrainShaderPath);
            var fastPathStart = source.IndexOf("half3 farBaseAlbedo", System.StringComparison.Ordinal);
            var fastPathEnd = source.IndexOf("SurfaceData farSurfaceData", System.StringComparison.Ordinal);

            Assert.That(fastPathStart, Is.GreaterThanOrEqualTo(0));
            Assert.That(fastPathEnd, Is.GreaterThan(fastPathStart));
            var fastPath = source.Substring(fastPathStart, fastPathEnd - fastPathStart);
            StringAssert.Contains("farRockyMidTransitionAlbedo", fastPath);
            StringAssert.Contains("_RockyMidTransitionMap", fastPath);
            StringAssert.Contains("farRockyShaleBand", fastPath);
            StringAssert.Contains("farRockySurfaceAlbedo", fastPath);
        }

        [Test]
        public void StartupQueuesImmediateTerrainWithoutSynchronousMeshConstruction()
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
                var targetBody = targetObject.AddComponent<Rigidbody>();
                targetBody.useGravity = false;
                var world = worldObject.AddComponent<TopDown3DProceduralWorld>();
                world.Configure(settings, targetObject.transform, terrainMaterial, rockMaterial);

                InvokePrivate(world, "Start");

                var immediateDiameter = settings.ImmediateLoadRadius * 2 + 1;
                var immediateChunkCount = immediateDiameter * immediateDiameter;
                var streamingDiameter = settings.StreamingRadius * 2 + 1;
                var streamingChunkCount = streamingDiameter * streamingDiameter;
                Assert.That(world.LoadedChunkCount, Is.Zero);
                Assert.That(world.DecoratedChunkCount, Is.Zero);
                Assert.That(world.PendingDecorationCount, Is.Zero);
                Assert.That(world.PendingTerrainChunkCount, Is.EqualTo(streamingChunkCount));
                Assert.That(GetPrivateCollectionCount(world, "terrainRequests"), Is.EqualTo(immediateChunkCount));
                Assert.That(
                    GetPrivateCollectionCount(world, "pendingChunks"),
                    Is.EqualTo(streamingChunkCount - immediateChunkCount));
                Assert.That(targetBody.isKinematic, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(worldObject);
                Object.DestroyImmediate(targetObject);
            }
        }

        [Test]
        public void UnfinishedDecorationCanBeCancelledBeforeChunkPublication()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(WorldSettingsPath);
            var terrainMaterial = AssetDatabase.LoadAssetAtPath<Material>(TerrainMaterialPath);
            var rockMaterial = AssetDatabase.LoadAssetAtPath<Material>(RockMaterialPath);
            var worldObject = new GameObject("Cancellable formation streaming");
            var targetObject = new GameObject("Streaming target");
            var chunkObject = new GameObject("Unfinished chunk");
            try
            {
                targetObject.AddComponent<CapsuleCollider>();
                worldObject.AddComponent<TopDown3DProceduralWorld>().Configure(
                    settings, targetObject.transform, terrainMaterial, rockMaterial);
                var world = worldObject.GetComponent<TopDown3DProceduralWorld>();
                InvokePrivate(world, "Start");
                var coordinate = world.CurrentCenterChunk;
                var chunk = chunkObject.AddComponent<TopDown3DGeneratedChunk>();
                chunk.Initialize(coordinate, null);
                var chunks = (System.Collections.IDictionary)GetPrivateField(world, "loadedChunks");
                chunks.Add(coordinate, chunk);
                InvokePrivate(world, "EnqueueDecoration", coordinate);
                Assert.That((bool)InvokePrivateResult(world, "TryProcessDecoration"), Is.True);
                Assert.That(world.PendingDecorationCount, Is.EqualTo(1));
                Assert.That(world.DecoratedChunkCount, Is.Zero);
                InvokePrivate(world, "CancelActiveDecoration", false);
                Assert.That(world.PendingDecorationCount, Is.Zero);
                Assert.That(world.DecoratedChunkCount, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(worldObject);
                Object.DestroyImmediate(targetObject);
                Object.DestroyImmediate(chunkObject);
            }
        }

        private static object GetPrivateField(TopDown3DProceduralWorld world, string name) =>
            typeof(TopDown3DProceduralWorld).GetField(name,
                BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(world);

        private static object InvokePrivateResult(TopDown3DProceduralWorld world,
            string methodName, params object[] arguments) =>
            typeof(TopDown3DProceduralWorld).GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(world, arguments);

        private static int GetPrivateCollectionCount(TopDown3DProceduralWorld world, string fieldName)
        {
            var field = typeof(TopDown3DProceduralWorld).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing staged-work collection {fieldName}.");
            var collection = field.GetValue(world) as ICollection;
            Assert.That(collection, Is.Not.Null, $"Staged-work field {fieldName} is not a collection.");
            return collection.Count;
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
