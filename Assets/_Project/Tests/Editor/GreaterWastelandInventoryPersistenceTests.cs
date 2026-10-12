using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class GreaterWastelandInventoryPersistenceTests
    {
        private Scene scene;
        private HashSet<GameObject> previousRoots;
        private TerrainData terrainData;
        private TopDown3DPlayerInventory kit;
        private TopDown3DBigArmCargo cargo;
        private TopDown3DResourceCache cache;
        private TopDown3DResourceWorldState resources;
        private TopDown3DPlacedHarvesterState harvesters;
        private GreaterWastelandInventorySaveService service;
        private TopDown3DItemCatalog catalog;
        private static void Set(object owner, string name, object value) => owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(owner, value);
        private GreaterWastelandInventorySnapshot Capture() => (GreaterWastelandInventorySnapshot)typeof(GreaterWastelandInventorySaveService)
            .GetMethod("Capture", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(service, null);

        [SetUp] public void Setup()
        {
            scene = SceneManager.GetActiveScene(); previousRoots = new HashSet<GameObject>(scene.GetRootGameObjects());
            terrainData = new TerrainData { heightmapResolution = 33, size = new Vector3(80, 10, 80) };
            Terrain.CreateTerrainGameObject(terrainData).transform.position = new Vector3(-1850, -50, -1510);
            catalog = AssetDatabase.LoadAssetAtPath<TopDown3DItemCatalog>("Assets/_Project/Settings/Items/TopDown3DItemCatalog.asset");
            var actor = new GameObject("Checkpoint Booter"); actor.SetActive(false); actor.transform.position = new Vector3(-1845, -49, -1495);
            var input = actor.AddComponent<TopDown3DInputRouter>();
            input.Configure(AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>("Assets/_Project/Settings/Input/InputSystem_Actions.inputactions"));
            kit = actor.AddComponent<TopDown3DPlayerInventory>(); kit.Configure(catalog, 16);
            var motor = actor.AddComponent<TopDown3DPlayerMotor>(); actor.AddComponent<TopDown3DPlayerAnimationDriver>();
            actor.AddComponent<TopDown3DPlayerActionController>();
            var vitals = actor.AddComponent<TopDown3DSurvivalVitals>(); vitals.Configure(TopDown3DSurvivalSettings.Load(), true);
            var algae = actor.AddComponent<TopDown3DAlgaeCanisterState>(); algae.Configure(TopDown3DAlgaeCanisterSettings.Load());
            var camp = actor.AddComponent<TopDown3DCampController>();
            camp.Configure(TopDown3DCampSettings.Load(), input, new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>());
            var legger = new GameObject("Checkpoint Legger"); legger.transform.position = new Vector3(-1810, -49, -1495);
            legger.AddComponent<TopDown3DBigArmState>();
            cargo = legger.AddComponent<TopDown3DBigArmCargo>(); cargo.Configure(catalog,
                AssetDatabase.LoadAssetAtPath<TopDown3DPackingSettings>("Assets/_Project/Settings/Items/TopDown3DPackingSettings.asset"));
            legger.AddComponent<TopDown3DBigArmFollower>();
            cache = new GameObject("Cache").AddComponent<TopDown3DResourceCache>(); cache.Configure(catalog, 24, "test-cache");
            resources = new GameObject("Resources").AddComponent<TopDown3DResourceWorldState>();
            var field = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/TopDown3D/Resources/GreaterWastelandIronstone.prefab"));
            Object.DestroyImmediate(resources.gameObject); resources = field.GetComponent<TopDown3DResourceWorldState>();
            foreach (var node in field.GetComponentsInChildren<TopDown3DAuthoredIronstoneNode>()) Assert.That(node.TryInitialize(out _), Is.True);
            harvesters = new GameObject("Harvesters").AddComponent<TopDown3DPlacedHarvesterState>();
            harvesters.ConfigureAuthored("test-world", TopDown3DHarvesterSettings.Load());
            service = new GameObject("Checkpoint").AddComponent<GreaterWastelandInventorySaveService>();
            Set(service, "kit", kit); Set(service, "cargo", cargo); Set(service, "cache", cache); Set(service, "resources", resources);
            Set(service, "harvesters", harvesters); Set(service, "vitals", vitals); Set(service, "algae", algae); Set(service, "camp", camp); Set(service, "motor", motor);
            kit.State.TryAdd(new TopDown3DItemAmount("tool.micro_dust_harvester", 1));
            cargo.State.TryAdd(new TopDown3DItemAmount("resource.ironstone_ore", 2));
            cache.State.TryAdd(new TopDown3DItemAmount("resource.ironstone_ore", 3));
            var first = field.GetComponentInChildren<TopDown3DIronstoneNode>(); Assert.That(first.TryConsume(out _), Is.True);
            Physics.SyncTransforms();
        }
        [TearDown] public void Cleanup()
        {
            if (previousRoots != null) foreach (var root in scene.GetRootGameObjects()) if (!previousRoots.Contains(root)) Object.DestroyImmediate(root);
            Object.DestroyImmediate(terrainData);
        }

        [Test] public void JointRestorePreservesCargoCacheDepletionAndSeparatedCompanionPosition()
        {
            var snapshot = Capture(); cargo.State.TryRemove("resource.ironstone_ore", 2); cache.State.TryRemove("resource.ironstone_ore", 3);
            cargo.transform.position = kit.transform.position;
            var observed = false; cargo.State.Changed += () =>
            {
                observed = true; Assert.That(cache.State.Slots[0].Quantity, Is.EqualTo(3));
                Assert.That(resources.GetRemainingUses(snapshot.resources.Deltas[0].StableId, 1), Is.Zero);
                Assert.That(cargo.transform.position, Is.EqualTo(snapshot.leggerPosition));
            };
            Assert.That(service.Restore(snapshot), Is.True); Assert.That(observed, Is.True);
            Assert.That(cargo.State.Slots[0].Quantity, Is.EqualTo(2));
            Assert.That(Vector3.Distance(kit.transform.position, cargo.transform.position), Is.GreaterThan(30));
        }
        [TestCase("world")][TestCase("capacity")][TestCase("unknown-node")][TestCase("position")][TestCase("algae")]
        public void InvalidCheckpointDoesNotApplyAnyParticipant(string problem)
        {
            var snapshot = Capture(); var before = GreaterWastelandInventoryFile.Encode(snapshot);
            if (problem == "world") snapshot.worldId = "other-world";
            if (problem == "capacity") snapshot.kit = new TopDown3DInventoryState(catalog, 8).CaptureSnapshot();
            if (problem == "unknown-node") snapshot.resources = JsonUtility.FromJson<TopDown3DResourceWorldSnapshot>(
                "{\"version\":1,\"resourceGenerationVersion\":1,\"deltas\":[{\"stableId\":\"missing\",\"remainingUses\":0}]}");
            if (problem == "position") snapshot.leggerPosition = new Vector3(9000, -49, 9000);
            if (problem == "algae") snapshot.algae.remaining = double.NaN;
            Assert.That(service.Restore(snapshot), Is.False);
            Assert.That(GreaterWastelandInventoryFile.Encode(Capture()), Is.EqualTo(before));
        }
        [Test] public void FileRoundTripChecksIntegrityAndRetainsPreviousBackup()
        {
            var directory = Path.Combine(Path.GetTempPath(), "InventoryCheckpointTests", Guid.NewGuid().ToString("N"));
            var path = Path.Combine(directory, "checkpoint.json");
            GreaterWastelandInventoryFile.Write(path, Capture()); var first = File.ReadAllText(path);
            cargo.State.TryAdd(new TopDown3DItemAmount("resource.ironstone_ore", 1));
            GreaterWastelandInventoryFile.Write(path, Capture());
            Assert.That(File.ReadAllText(path + ".backup"), Is.EqualTo(first));
            Assert.That(GreaterWastelandInventoryFile.Read(path, out var loaded), Is.True);
            Assert.That(loaded.cargo.Slots[0].Quantity, Is.EqualTo(3));
            Assert.That(GreaterWastelandInventoryFile.TryDecode(first.Replace("ironstone_ore", "changed_ore"), out _), Is.False);
            var current = File.ReadAllText(path); File.WriteAllText(path + ".pending", "retained recovery data");
            Assert.Throws<IOException>(() => GreaterWastelandInventoryFile.Write(path, Capture()));
            Assert.That(File.ReadAllText(path), Is.EqualTo(current));
            Assert.That(File.ReadAllText(path + ".pending"), Is.EqualTo("retained recovery data"));
            // Preserve these small exact file-write artifacts for diagnosis; no real save path is touched.
        }
        [Test] public void AutomaticCheckpointAcknowledgesCapturedRevisionAfterLiveStateChanges()
        {
            using var started = new System.Threading.ManualResetEventSlim();
            using var release = new System.Threading.ManualResetEventSlim();
            string detached = null;
            var writer = new GreaterWastelandCheckpointWriter("unused", (path, text) =>
            {
                detached = text; started.Set();
                if (!release.Wait(2000)) throw new TimeoutException("Fixture checkpoint was not released.");
            });
            Set(service, "writer", writer);
            var capturedRevision = cargo.State.Revision;
            try
            {
                typeof(GreaterWastelandInventorySaveService).GetMethod("QueueCheckpoint", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(service, null);
                Assert.That(started.Wait(2000), Is.True);
                Assert.That(service.WriteStatus, Is.EqualTo(GreaterWastelandInventorySaveService.CheckpointWriteStatus.Queued));
                Assert.That(cargo.State.TryAdd(new TopDown3DItemAmount("resource.ironstone_ore", 1)).Succeeded, Is.True);
                release.Set();
                var observe = typeof(GreaterWastelandInventorySaveService).GetMethod("ObserveWriter", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(System.Threading.SpinWait.SpinUntil(() =>
                {
                    observe.Invoke(service, null);
                    return service.WriteStatus == GreaterWastelandInventorySaveService.CheckpointWriteStatus.Completed;
                }, 2000), Is.True);
                Assert.That(service.CompletedCargoRevision, Is.EqualTo(capturedRevision));
                Assert.That(service.CompletedCargoRevision, Is.LessThan(cargo.State.Revision));
                Assert.That(GreaterWastelandInventoryFile.TryDecode(detached, out var captured), Is.True);
                Assert.That(captured.cargo.Slots[0].Quantity, Is.EqualTo(2));
                Assert.That(cargo.State.Slots[0].Quantity, Is.EqualTo(3));
            }
            finally { release.Set(); writer.Drain(out _, out _); }
        }
        [Test] public void InventoryOwnersReconstructTheirOwnRetainedSnapshots()
        {
            Set(kit, "reloadSnapshot", kit.CaptureSnapshot()); Set(cargo, "reloadSnapshot", cargo.State.CaptureSnapshot());
            Set(cache, "reloadSnapshot", cache.State.CaptureSnapshot());
            kit.OnAfterDeserialize(); cargo.OnAfterDeserialize(); cache.OnAfterDeserialize();
            Assert.That(kit.State.Slots[0].ItemId, Is.EqualTo("tool.micro_dust_harvester"));
            Assert.That(cargo.State.Slots[0].Quantity, Is.EqualTo(2)); Assert.That(cache.State.Slots[0].Quantity, Is.EqualTo(3));
        }
        [Test] public void DustFilledDeviceReturnsToolAndDustToSeparateOwnersExactlyOnce()
        {
            Assert.That(harvesters.TryPlace(new Vector3(-1840, -50, -1495), kit.State, out var id), Is.True);
            var saved = harvesters.CaptureSnapshot();
            var json = JsonUtility.ToJson(saved).Replace("\"dust\":0", "\"dust\":4");
            Assert.That(harvesters.ApplySnapshot(JsonUtility.FromJson<TopDown3DPlacedHarvesterSnapshot>(json)), Is.True);
            Assert.That(harvesters.TryPickup(id, kit.State, cargo.State, out _, () => false), Is.False);
            Assert.That(harvesters.DeployedCount, Is.EqualTo(1));
            Assert.That(harvesters.TryPickup(id, kit.State, cargo.State, out var dust, () => true), Is.True);
            Assert.That(dust, Is.EqualTo(4)); Assert.That(kit.State.Slots[0].ItemId, Is.EqualTo("tool.micro_dust_harvester"));
            Assert.That(cargo.State.Slots[1].ItemId, Is.EqualTo("resource.airborne_dust"));
            Assert.That(harvesters.TryPickup(id, kit.State, cargo.State, out _, () => true), Is.False);
        }
        [Test] public void ResourceAndDeviceDeltasSurviveStateReconstructionTogether()
        {
            Assert.That(harvesters.TryPlace(new Vector3(-1840, -50, -1495), kit.State, out var id), Is.True);
            var resourceSnapshot = resources.CaptureSnapshot(); var deviceSnapshot = harvesters.CaptureSnapshot();
            Set(resources, "reloadSnapshot", resourceSnapshot); Set(harvesters, "reloadSnapshot", deviceSnapshot);
            Assert.That(resources.ApplySnapshot(JsonUtility.FromJson<TopDown3DResourceWorldSnapshot>("{\"version\":1,\"resourceGenerationVersion\":1,\"deltas\":[]}")), Is.True);
            Assert.That(harvesters.TryPickup(id, kit.State, cargo.State, out _, () => true), Is.True);
            resources.OnAfterDeserialize(); harvesters.OnAfterDeserialize(); Assert.That(harvesters.RestoreRetainedState(), Is.True);
            Assert.That(resources.GetRemainingUses(resourceSnapshot.Deltas[0].StableId, 1), Is.Zero);
            Assert.That(harvesters.DeployedCount, Is.EqualTo(1));
        }
        [Test] public void DeployedCampRestoresAndRemainsPackableByItsOwner()
        {
            var snapshot = Capture(); snapshot.campDeployed = true;
            snapshot.camp = TopDown3DCampRecord.Create(new Vector3(-1825, -50, -1500), Quaternion.identity);
            Assert.That(service.Restore(snapshot), Is.True);
            var owner = kit.GetComponent<TopDown3DCampController>();
            Assert.That(owner.Record.id, Is.EqualTo(snapshot.camp.id)); Assert.That(owner.Camp.CanPack, Is.True);
            var empty = Capture(); empty.campDeployed = false; empty.camp = null;
            Assert.That(service.Restore(empty), Is.True); Assert.That(owner.Record, Is.Null); Assert.That(owner.Camp, Is.Null);
        }
    }
}
