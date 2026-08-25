using System.Collections;
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
