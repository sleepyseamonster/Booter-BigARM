using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BooterBigArm.TopDown3D;
using BooterBigArm.TopDown3D.WorldCreator;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

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

        [Test]
        public void NearMidAndFarTilesCoverTraversalInFullAndReducedProfiles()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(WorldSettingsPath);
            var terrainMaterial = AssetDatabase.LoadAssetAtPath<Material>(TerrainMaterialPath);
            var worldObject = new GameObject("Terrain ring coverage");
            var overrideProperty = typeof(TopDown3DPlaytestPerformanceProfile).GetProperty(
                "StreamingRadiusOverride", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(overrideProperty, Is.Not.Null);
            try
            {
                using var runtime = WorldCreatorProductionRuntime.Create(settings.WorldSeed);
                var generator = new TopDown3DWorldGenerator(settings, runtime);
                var landscape = new TopDown3DFarLandscape(worldObject.transform, settings,
                    generator, runtime, terrainMaterial);
                try
                {
                    foreach (var radiusOverride in new[] { 0, 2 })
                    {
                        overrideProperty.SetValue(null, radiusOverride);
                        foreach (var offsetA in new[] { 0, 3, 7, 11, 15 })
                        foreach (var offsetB in new[] { 0, 7, 15 })
                        {
                            var targetA = offsetA * settings.ChunkSize + 1f;
                            var targetB = offsetB * settings.ChunkSize + 1f;
                            landscape.Refresh(new Vector3(targetA, 0f, targetB), true);
                            var keys = (HashSet<WorldRepresentationKey>)typeof(TopDown3DFarLandscape)
                                .GetField("required", BindingFlags.Instance | BindingFlags.NonPublic)
                                ?.GetValue(landscape);
                            Assert.That(keys, Is.Not.Null);
                            var nearRadius = radiusOverride > 0 ? radiusOverride : settings.StreamingRadius;
                            var nearMinA = (offsetA - nearRadius) * settings.ChunkSize;
                            var nearMinB = (offsetB - nearRadius) * settings.ChunkSize;
                            var nearMaxA = nearMinA + (nearRadius * 2 + 1) * settings.ChunkSize;
                            var nearMaxB = nearMinB + (nearRadius * 2 + 1) * settings.ChunkSize;
                            for (var b = -30; b <= 30; b++)
                            for (var a = -30; a <= 30; a++)
                            {
                                var pointA = targetA + a * settings.ChunkSize;
                                var pointB = targetB + b * settings.ChunkSize;
                                var covered = pointA >= nearMinA && pointA < nearMaxA
                                    && pointB >= nearMinB && pointB < nearMaxB;
                                if (!covered)
                                {
                                    foreach (var key in keys)
                                    {
                                        var minimum = key.Minimum;
                                        if (pointA < minimum.HorizontalA || pointA >= minimum.HorizontalA + key.TileSpan
                                            || pointB < minimum.HorizontalB || pointB >= minimum.HorizontalB + key.TileSpan)
                                            continue;
                                        covered = true;
                                        break;
                                    }
                                }
                                Assert.That(covered, Is.True,
                                    $"Uncovered terrain at {pointA},{pointB}; target {targetA},{targetB}; radius {nearRadius}.");
                            }
                        }
                    }
                }
                finally { landscape.Dispose(); }
            }
            finally
            {
                overrideProperty.SetValue(null, 0);
                Object.DestroyImmediate(worldObject);
            }
        }

        [Test]
        public void FailedNearTerrainIsQueuedForBoundedRetryWithoutChangingChunks()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(WorldSettingsPath);
            var terrainMaterial = AssetDatabase.LoadAssetAtPath<Material>(TerrainMaterialPath);
            var rockMaterial = AssetDatabase.LoadAssetAtPath<Material>(RockMaterialPath);
            var worldObject = new GameObject("Retry near terrain");
            var targetObject = new GameObject("Retry target");
            try
            {
                targetObject.AddComponent<CapsuleCollider>();
                var world = worldObject.AddComponent<TopDown3DProceduralWorld>();
                world.Configure(settings, targetObject.transform, terrainMaterial, rockMaterial);
                InvokePrivate(world, "Start");
                var coordinate = world.CurrentCenterChunk + new Vector2Int(3, 0);
                var runtime = (WorldCreatorProductionRuntime)GetPrivateField(world, "worldCreatorRuntime");
                var key = runtime.CreateRepresentationKey(WorldRepresentationTier.Near,
                    coordinate.x, coordinate.y, settings.ChunkSize);
                var failed = Task.FromResult(new WorldRepresentationRequestOutcome(key, 1L,
                    WorldRepresentationRequestState.Failed, "test failure"));
                var requestType = typeof(TopDown3DProceduralWorld).GetNestedType(
                    "TerrainRequest", BindingFlags.NonPublic);
                var requests = (IDictionary)GetPrivateField(world, "terrainRequests");
                requests.Add(coordinate, System.Activator.CreateInstance(requestType, key, failed));
                LogAssert.Expect(LogType.Error, new Regex("World Creator near representation.*test failure"));
                Assert.That(InvokePrivateResult(world, "TryIntegrateRequestedTerrain"), Is.EqualTo(true));
                var retryDue = (IDictionary)GetPrivateField(world, "terrainRetryDue");
                Assert.That(requests.Contains(coordinate), Is.False);
                Assert.That(retryDue.Contains(coordinate), Is.True);
                retryDue[coordinate] = Time.realtimeSinceStartupAsDouble - 1d;
                Assert.That(InvokePrivateResult(world, "TryRequestDueTerrainRetry"), Is.EqualTo(true));
                Assert.That(requests.Contains(coordinate), Is.True);

                var evictedCoordinate = world.CurrentCenterChunk + new Vector2Int(4, 0);
                var evictedKey = runtime.CreateRepresentationKey(WorldRepresentationTier.Near,
                    evictedCoordinate.x, evictedCoordinate.y, settings.ChunkSize);
                var missingCacheHit = Task.FromResult(new WorldRepresentationRequestOutcome(
                    evictedKey, 0L, WorldRepresentationRequestState.CacheHit));
                requests.Add(evictedCoordinate,
                    System.Activator.CreateInstance(requestType, evictedKey, missingCacheHit));
                // The first completed request may still be integrating; remove it from this
                // focused fixture so the missing-cache result is examined directly.
                requests.Remove(coordinate);
                Assert.That(InvokePrivateResult(world, "TryIntegrateRequestedTerrain"), Is.EqualTo(true));
                Assert.That(retryDue.Contains(evictedCoordinate), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(worldObject);
                Object.DestroyImmediate(targetObject);
            }
        }

        [Test]
        public void FailedDistanceTerrainIsQueuedForBoundedRetryWithoutMoving()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(WorldSettingsPath);
            var terrainMaterial = AssetDatabase.LoadAssetAtPath<Material>(TerrainMaterialPath);
            var worldObject = new GameObject("Retry distance terrain");
            try
            {
                using var runtime = WorldCreatorProductionRuntime.Create(settings.WorldSeed);
                var generator = new TopDown3DWorldGenerator(settings, runtime);
                var landscape = new TopDown3DFarLandscape(worldObject.transform, settings,
                    generator, runtime, terrainMaterial);
                try
                {
                    landscape.Refresh(Vector3.zero, true);
                    var required = (HashSet<WorldRepresentationKey>)typeof(TopDown3DFarLandscape)
                        .GetField("required", BindingFlags.Instance | BindingFlags.NonPublic)
                        ?.GetValue(landscape);
                    var key = default(WorldRepresentationKey);
                    foreach (var candidate in required) { key = candidate; break; }
                    var requests = (IDictionary)typeof(TopDown3DFarLandscape)
                        .GetField("requests", BindingFlags.Instance | BindingFlags.NonPublic)
                        ?.GetValue(landscape);
                    requests.Add(key, Task.FromResult(new WorldRepresentationRequestOutcome(key, 1L,
                        WorldRepresentationRequestState.Failed, "test failure")));
                    LogAssert.Expect(LogType.Error, new Regex("World Creator .* representation .*test failure"));
                    landscape.ProcessReady(0, 1);
                    var retryDue = (IDictionary)typeof(TopDown3DFarLandscape)
                        .GetField("retryDue", BindingFlags.Instance | BindingFlags.NonPublic)
                        ?.GetValue(landscape);
                    Assert.That(requests.Contains(key), Is.False);
                    Assert.That(retryDue.Contains(key), Is.True);
                    var pending = (IList)typeof(TopDown3DFarLandscape)
                        .GetField("pending", BindingFlags.Instance | BindingFlags.NonPublic)
                        ?.GetValue(landscape);
                    pending.Clear();
                    pending.Add(key);
                    retryDue[key] = Time.realtimeSinceStartupAsDouble - 1d;
                    landscape.ProcessReady(1, 0);
                    Assert.That(requests.Contains(key), Is.True);
                }
                finally { landscape.Dispose(); }
            }
            finally { Object.DestroyImmediate(worldObject); }
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
