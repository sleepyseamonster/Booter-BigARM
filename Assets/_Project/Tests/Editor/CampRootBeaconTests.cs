using System;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class CampRootBeaconTests
    {
        [Test] public void BeaconRunSurvivesUnitySerializationAndCanStillDepart()
        {
            var owner=new GameObject("Beacon reload fixture");owner.SetActive(false);
            var restoredOwner=new GameObject("Restored beacon reload fixture");restoredOwner.SetActive(false);
            try
            {
                var beacon=owner.AddComponent<CampRootBeaconSession>();
                var original=new RootBeaconRun("camp","retained",72,new Vector3(-10,4,30));
                var restored=restoredOwner.AddComponent<CampRootBeaconSession>();
                UnityEditor.EditorJsonUtility.FromJsonOverwrite(UnityEditor.EditorJsonUtility.ToJson(beacon),restored);
                Assert.That(restored.Run,Is.Null,"An uninitialized beacon must remain unavailable after reload.");
                typeof(CampRootBeaconSession).GetProperty("Run").SetValue(beacon,original);
                UnityEditor.EditorJsonUtility.FromJsonOverwrite(UnityEditor.EditorJsonUtility.ToJson(beacon),restored);
                Assert.That(restored.Run,Is.Not.Null);
                Assert.That(restored.Run.BeaconId,Is.EqualTo(original.BeaconId));
                Assert.That(restored.Run.Seed,Is.EqualTo(72));
                Assert.That(restored.Run.Origin,Is.EqualTo(original.Origin));
                Assert.That(restored.Run.TryDepart(original.Origin),Is.True);
                restored.Run.Advance(3,original.Origin,true);
                Assert.That(restored.Run.Phase,Is.EqualTo(RootBeaconPhase.Extracted));
            }
            finally{Object.DestroyImmediate(owner);Object.DestroyImmediate(restoredOwner);}
        }

        [TestCase(false)] [TestCase(true)]
        public void DepartureSerializationRetainsProgressAndCompletedRunsCannotRestart(bool complete)
        {
            var run=new RootBeaconRun("camp","retained",42,Vector3.zero);
            run.TryDepart(Vector3.zero);run.Advance(complete?3:1.25f,Vector3.zero,true);
            var restored=JsonUtility.FromJson<RootBeaconRun>(JsonUtility.ToJson(run));
            Assert.That(restored.Phase,Is.EqualTo(run.Phase));
            Assert.That(restored.DepartureElapsed,Is.EqualTo(run.DepartureElapsed));
            Assert.That(restored.TryDepart(Vector3.zero),Is.False);
            restored.Advance(3,Vector3.zero,true);
            Assert.That(restored.Phase,Is.EqualTo(RootBeaconPhase.Extracted));
        }

        [Test] public void InstallationIsRestrictedToCampLoopTesting()
        {
            Assert.That(CampRootBeaconSession.SupportsScene("Assets/_Project/Scenes/Reference/CampLoopTesting.unity"), Is.True);
            Assert.That(CampRootBeaconSession.SupportsScene(GreaterWastelandTerrainRendering.ScenePath), Is.False);
            Assert.That(CampRootBeaconSession.SupportsScene(null), Is.False);
            Assert.That(CampRootBeaconSession.SupportsScene(""), Is.False);
        }

        [Test] public void ReturningRequiresTheOriginalLocationAndGroundLevel()
        {
            var run = new RootBeaconRun("camp", "one", 12, new Vector3(-100, 30, -200));
            Assert.That(run.TryDepart(new Vector3(-90, 30, -200)), Is.False);
            Assert.That(run.TryDepart(new Vector3(-100, 34, -200)), Is.False);
            Assert.That(run.TryDepart(new Vector3(float.NaN, 30, -200)), Is.False);
            Assert.That(run.TryDepart(new Vector3(-99, 30, -200)), Is.True);
            Assert.That(run.Origin, Is.EqualTo(new Vector3(-100, 30, -200)));
        }

        [Test] public void DepartureIsOnceOnlyAndAllowsMovementAfterNearbyActivation()
        {
            var run = new RootBeaconRun("camp", "one", 12, Vector3.zero);
            Assert.That(run.TryDepart(Vector3.zero), Is.True);
            run.Advance(2, Vector3.zero, true);
            Assert.That(run.Phase, Is.EqualTo(RootBeaconPhase.Departing));
            run.Advance(1, Vector3.right * 8, true);
            Assert.That(run.Phase, Is.EqualTo(RootBeaconPhase.Extracted));
            Assert.That(run.TryDepart(Vector3.zero), Is.False);
            run.Cancel(); Assert.That(run.Phase, Is.EqualTo(RootBeaconPhase.Extracted));
        }

        [Test] public void InterruptedDepartureRequiresAFreshActivation()
        {
            var run = new RootBeaconRun("camp", "one", 12, Vector3.zero);
            run.TryDepart(Vector3.zero); run.Advance(2.9f, Vector3.zero, true);
            run.Advance(.2f, Vector3.zero, false);
            run.Advance(20, Vector3.zero, true);
            Assert.That(run.Phase, Is.EqualTo(RootBeaconPhase.Exploring));
            run.TryDepart(Vector3.zero); run.Advance(float.PositiveInfinity, Vector3.zero, true);
            Assert.That(run.Phase, Is.EqualTo(RootBeaconPhase.Exploring));
        }

        [Test] public void RunIdentitySeparatesBeaconsAndRejectsInvalidOrigins()
        {
            var a = new RootBeaconRun("camp", "one", 12, Vector3.zero);
            var b = new RootBeaconRun("camp", "two", 12, Vector3.zero);
            Assert.That(a.BeaconId, Is.Not.EqualTo(b.BeaconId));
            Assert.Throws<ArgumentException>(() => new RootBeaconRun("", "one", 0, Vector3.zero));
            Assert.Throws<ArgumentException>(() => new RootBeaconRun("camp", "one", 0, Vector3.positiveInfinity));
        }

        [Test] public void ArrivalIsRepeatableIndependentOfTerrainEnumerationAndDoesNotMoveActors()
        {
            var first = TerrainAt(new Vector3(-512, 20, -512));
            var second = TerrainAt(new Vector3(-256, 20, -512));
            var actor = Actor();
            var original = actor.transform.position;
            try
            {
                Assert.That(RootBeaconArrival.TryChoose(new[] { first, second }, 438, actor, null, out var a), Is.True);
                Assert.That(RootBeaconArrival.TryChoose(new[] { second, first }, 438, actor, null, out var b), Is.True);
                Assert.That(a.Player, Is.EqualTo(b.Player)); Assert.That(a.Beacon, Is.EqualTo(b.Beacon));
                Assert.That(a.Player.y, Is.EqualTo(20).Within(.01f));
                Assert.That(actor.transform.position, Is.EqualTo(original));
                bool differs = false;
                for (int i = 1; i <= 4; i++)
                {
                    Assert.That(RootBeaconArrival.TryChoose(new[] { first, second }, i, actor, null, out var other), Is.True);
                    differs |= (other.Player - a.Player).sqrMagnitude > 1;
                }
                Assert.That(differs, Is.True);
            }
            finally { DestroyTerrain(first); DestroyTerrain(second); Object.DestroyImmediate(actor.gameObject); }
        }

        [Test] public void MissingGroundAndBlockedMapsNeverProduceAFallbackSpawn()
        {
            var terrain = TerrainAt(Vector3.zero); var actor = Actor();
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = new Vector3(128, 3, 128); wall.transform.localScale = new Vector3(256, 5, 256);
            Physics.SyncTransforms();
            try
            {
                Assert.That(RootBeaconArrival.TryChoose(new[] { terrain }, 32, actor, null, out _), Is.False);
                Assert.That(RootBeaconArrival.TryChoose(Array.Empty<Terrain>(), 32, actor, null, out _), Is.False);
                wall.SetActive(false); terrain.GetComponent<TerrainCollider>().enabled = false;
                Assert.That(RootBeaconArrival.TryChoose(new[] { terrain }, 32, actor, null, out _), Is.False);
            }
            finally { Object.DestroyImmediate(wall); DestroyTerrain(terrain); Object.DestroyImmediate(actor.gameObject); }
        }

        [Test] public void ArrivalRespectsBothCenteredAndFootBasedColliderPivots()
        {
            var actor = Actor(); var shape = actor.GetComponent<CapsuleCollider>();
            try
            {
                var ground = new Vector3(-300, 42, -500);
                Assert.That(RootBeaconArrival.StandingPosition(shape, ground).y, Is.EqualTo(42.12f).Within(.001f));
                shape.center = Vector3.zero; Physics.SyncTransforms();
                Assert.That(RootBeaconArrival.StandingPosition(shape, ground).y, Is.EqualTo(43.02f).Within(.001f));
            }
            finally { Object.DestroyImmediate(actor.gameObject); }
        }

        [Test] public void TerrainHolesAndSteepSlopesAreRejected()
        {
            var terrain = TerrainAt(Vector3.zero); var actor = Actor();
            try
            {
                var holes = new bool[terrain.terrainData.holesResolution, terrain.terrainData.holesResolution];
                terrain.terrainData.SetHoles(0, 0, holes); Physics.SyncTransforms();
                Assert.That(RootBeaconArrival.TryChoose(new[] { terrain }, 7, actor, null, out _), Is.False);
                for (int z = 0; z < holes.GetLength(0); z++) for (int x = 0; x < holes.GetLength(1); x++) holes[z, x] = true;
                terrain.terrainData.SetHoles(0, 0, holes);
                terrain.terrainData.size = new Vector3(256, 512, 256);
                var heights = new float[33, 33];
                for (int z = 0; z < 33; z++) for (int x = 0; x < 33; x++) heights[z, x] = x / 32f;
                terrain.terrainData.SetHeights(0, 0, heights); Physics.SyncTransforms();
                Assert.That(RootBeaconArrival.TryChoose(new[] { terrain }, 7, actor, null, out _), Is.False);
            }
            finally { DestroyTerrain(terrain); Object.DestroyImmediate(actor.gameObject); }
        }

        [Test] public void CampNativeTerrainSupportsSeveralArrivalSeedsWithoutMovingOrEditingAnything()
        {
            var scene = EditorSceneManager.OpenScene(CampRootBeaconSession.ScenePath, OpenSceneMode.Single);
            try
            {
                var player = TopDown3DGameHudCanvas.FindInScene<TopDown3DPlayerMotor>(scene);
                var companion = TopDown3DGameHudCanvas.FindInScene<TopDown3DBigArmFollower>(scene);
                Assert.That(player, Is.Not.Null); Assert.That(companion, Is.Not.Null);
                var originalPlayer = player.transform.position; var originalCompanion = companion.transform.position;
                var terrain = new System.Collections.Generic.List<Terrain>();
                foreach (var root in scene.GetRootGameObjects()) terrain.AddRange(root.GetComponentsInChildren<Terrain>(false));
                Physics.SyncTransforms();
                foreach (int seed in new[] { 1, 12, 32, 438, 615, 1003, 2026, 20261021 })
                {
                    Assert.That(RootBeaconArrival.TryChoose(terrain, seed, player, companion, out var site), Is.True, "seed " + seed);
                    Assert.That(Vector3.Distance(site.Player, site.Beacon), Is.LessThan(3));
                    Assert.That(Vector3.Distance(site.Companion, site.Beacon), Is.LessThan(10));
                }
                Assert.That(player.transform.position, Is.EqualTo(originalPlayer));
                Assert.That(companion.transform.position, Is.EqualTo(originalCompanion));
                Assert.That(scene.isDirty, Is.False);
            }
            finally { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); }
        }

        private static Terrain TerrainAt(Vector3 position)
        {
            var data = new TerrainData { heightmapResolution = 33, size = new Vector3(256, 20, 256) };
            var go = Terrain.CreateTerrainGameObject(data); go.transform.position = position;
            Physics.SyncTransforms(); return go.GetComponent<Terrain>();
        }
        private static void DestroyTerrain(Terrain t) { var data = t.terrainData; Object.DestroyImmediate(t.gameObject); Object.DestroyImmediate(data); }
        private static TopDown3DPlayerMotor Actor()
        {
            var go = new GameObject("Arrival fixture", typeof(Rigidbody), typeof(CapsuleCollider));
            go.transform.position = new Vector3(-2000, 30, -2000);
            var capsule = go.GetComponent<CapsuleCollider>(); capsule.height = 1.8f; capsule.radius = .35f; capsule.center = Vector3.up * .9f;
            var motor = go.AddComponent<TopDown3DPlayerMotor>(); Physics.SyncTransforms(); return motor;
        }
    }
}
