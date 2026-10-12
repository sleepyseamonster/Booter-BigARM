using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class GreaterWastelandIronstoneTests
    {
        private const string ResourcePath = "Assets/_Project/Settings/World/Resource_IronstoneNode.asset";
        private const string ItemCatalogPath = "Assets/_Project/Settings/Items/TopDown3DItemCatalog.asset";
        private readonly List<GameObject> owned = new List<GameObject>();
        private TopDown3DResourceDefinition definition;
        private TopDown3DItemCatalog catalog;
        private TopDown3DResourceWorldState state;

        [SetUp]
        public void SetUp()
        {
            definition = AssetDatabase.LoadAssetAtPath<TopDown3DResourceDefinition>(ResourcePath);
            catalog = AssetDatabase.LoadAssetAtPath<TopDown3DItemCatalog>(ItemCatalogPath);
            Assert.That(definition, Is.Not.Null);
            state = Own(new GameObject("Ironstone test state")).AddComponent<TopDown3DResourceWorldState>();
        }

        [TearDown]
        public void TearDown()
        {
            for (var i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
            owned.Clear();
        }

        private GameObject Own(GameObject value) { owned.Add(value); return value; }

        private TopDown3DAuthoredIronstoneNode MakeNode(string id = "greater-wasteland:ironstone:v1:authored:test")
        {
            var owner = Own(GameObject.CreatePrimitive(PrimitiveType.Cube));
            owner.name = "Authored ironstone test";
            owner.transform.position = new Vector3(0f, 0.8f, 1.5f);
            var node = owner.AddComponent<TopDown3DAuthoredIronstoneNode>();
            node.Configure(id, definition, state, owner.GetComponent<Collider>(), owner.GetComponents<Renderer>());
            return node;
        }

        [Test]
        public void UnconfiguredNodeCannotBeTargeted()
        {
            var node = Own(new GameObject("Unconfigured node")).AddComponent<TopDown3DIronstoneNode>();
            Assert.That(node.IsAvailable, Is.False);
        }

        [Test]
        public void DuplicateAuthoredIdentityFailsClosed()
        {
            var first = MakeNode();
            var second = Own(GameObject.CreatePrimitive(PrimitiveType.Cube)).AddComponent<TopDown3DAuthoredIronstoneNode>();
            Assert.Throws<System.ArgumentException>(() => second.Configure(first.StableId, definition, state,
                second.GetComponent<Collider>(), second.GetComponents<Renderer>()));
            Assert.That(second.GetComponent<TopDown3DIronstoneNode>().IsAvailable, Is.False);
            Assert.That(first.GetComponent<TopDown3DIronstoneNode>().IsAvailable, Is.True);
        }

        [Test]
        public void DepletionKeepsCollisionAndSurvivesViewReconstruction()
        {
            var authored = MakeNode();
            var node = authored.GetComponent<TopDown3DIronstoneNode>();
            Assert.That(node.TryConsume(out _), Is.True);
            Assert.That(authored.Obstacle.enabled, Is.True);
            Assert.That(node.IsAvailable, Is.False);
            Object.DestroyImmediate(authored.gameObject);
            var reconstructed = MakeNode();
            Assert.That(reconstructed.GetComponent<TopDown3DIronstoneNode>().IsDepleted, Is.True);
            Assert.That(reconstructed.Obstacle.enabled, Is.True);
            Assert.That(reconstructed.GetComponent<Renderer>().sharedMaterial, Is.SameAs(definition.DepletedMaterial));
        }

        [Test]
        public void ApplyingSnapshotRefreshesAlreadyLiveNodesBothWays()
        {
            var authored = MakeNode();
            var node = authored.GetComponent<TopDown3DIronstoneNode>();
            var pristine = state.CaptureSnapshot();
            node.TryConsume(out _);
            var depleted = state.CaptureSnapshot();
            Assert.That(state.ApplySnapshot(pristine), Is.True);
            Assert.That(node.IsAvailable, Is.True);
            Assert.That(node.GetComponent<Renderer>().sharedMaterial, Is.SameAs(definition.ActiveMaterial));
            Assert.That(state.ApplySnapshot(depleted), Is.True);
            Assert.That(node.IsAvailable, Is.False);
            Assert.That(node.GetComponent<Renderer>().sharedMaterial, Is.SameAs(definition.DepletedMaterial));
            Assert.That(authored.Obstacle.enabled, Is.True);
        }

        [Test]
        public void SceneSerializationRetainsIdentityAndStateLinks()
        {
            const string path = "Assets/TemporaryIronstoneSerializationTest.unity";
            var oldActive = SceneManager.GetActiveScene();
            const string hostPath = "Assets/TemporaryIronstoneHost.unity";
            Assert.That(EditorSceneManager.SaveScene(oldActive, hostPath), Is.True);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var world = Own(new GameObject("Serialized resource state"));
                var serializedState = world.AddComponent<TopDown3DResourceWorldState>();
                var owner = Own(GameObject.CreatePrimitive(PrimitiveType.Cube));
                var authored = owner.AddComponent<TopDown3DAuthoredIronstoneNode>();
                authored.Configure("greater-wasteland:ironstone:v1:authored:test", definition, serializedState,
                    owner.GetComponent<Collider>(), owner.GetComponents<Renderer>());
                Assert.That(EditorSceneManager.SaveScene(scene, path), Is.True);
                EditorSceneManager.CloseScene(scene, true);
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                var loaded = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<TopDown3DAuthoredIronstoneNode>()).Single();
                Assert.That(loaded.StableId, Is.EqualTo("greater-wasteland:ironstone:v1:authored:test"));
                Assert.That(loaded.WorldState.gameObject.scene, Is.EqualTo(scene));
                Assert.That(loaded.TryInitialize(out var error), Is.True, error);
                Assert.That(loaded.GetComponent<TopDown3DIronstoneNode>().IsAvailable, Is.True);
            }
            finally
            {
                if (scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
                SceneManager.SetActiveScene(oldActive);
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.DeleteAsset(hostPath);
            }
        }

        [Test]
        public void ProductionInstallerLoadsTwelveLinkedNodesOnceAndIgnoresOtherScenes()
        {
            const string folder = "Assets/TemporaryIronstoneInstallerTest";
            const string path = folder + "/GreaterWasteland.unity";
            AssetDatabase.CreateFolder("Assets", "TemporaryIronstoneInstallerTest");
            var oldActive = SceneManager.GetActiveScene();
            const string hostPath = "Assets/TemporaryIronstoneInstallerHost.unity";
            Assert.That(EditorSceneManager.SaveScene(oldActive, hostPath), Is.True);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                Assert.That(GreaterWastelandIronstoneInstaller.TryInstall(scene), Is.Null);
                Assert.That(EditorSceneManager.SaveScene(scene, path), Is.True);
                var root = GreaterWastelandIronstoneInstaller.TryInstall(scene);
                Assert.That(root, Is.Not.Null);
                Assert.That(GreaterWastelandIronstoneInstaller.TryInstall(scene), Is.SameAs(root));
                var nodes = root.GetComponentsInChildren<TopDown3DAuthoredIronstoneNode>();
                Assert.That(nodes.Length, Is.EqualTo(12));
                Assert.That(nodes.Select(node => node.StableId).Distinct().Count(), Is.EqualTo(12));
                var resourceState = root.GetComponentsInChildren<TopDown3DResourceWorldState>().Single();
                foreach (var node in nodes)
                {
                    Assert.That(node.WorldState, Is.SameAs(resourceState));
                    Assert.That(node.TryInitialize(out var error), Is.True, error);
                    Assert.That(node.GetComponent<TopDown3DIronstoneNode>().IsAvailable, Is.True);
                    Assert.That(node.GetComponent<LODGroup>().GetLODs().Length, Is.EqualTo(3));
                }
                var first = nodes[0].GetComponent<TopDown3DIronstoneNode>();
                Assert.That(first.TryConsume(out _), Is.True);
                Assert.That(first.IsAvailable, Is.False);
                Assert.That(nodes[0].Obstacle.enabled, Is.True);
            }
            finally
            {
                if (scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
                SceneManager.SetActiveScene(oldActive);
                AssetDatabase.DeleteAsset(folder);
                AssetDatabase.DeleteAsset(hostPath);
            }
        }

        [TestCase(false, false, true)]
        [TestCase(true, false, false)]
        [TestCase(false, true, false)]
        public void HarvestRechecksCargoAccessAndCapacityAtCompletion(bool leaveRange, bool fillCargo, bool expectedCargo)
        {
            var node = MakeNode().GetComponent<TopDown3DIronstoneNode>();
            var booter = new TopDown3DInventoryState(catalog, 1);
            var cargo = new TopDown3DInventoryState(catalog,
                new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.BigArm, 1));
            var accessible = true;
            var gather = new TopDown3DGatherActionState(new TopDown3DItemDestinationService(booter, cargo, () => accessible));
            Assert.That(gather.TryBegin(node, Vector3.zero), Is.EqualTo(TopDown3DGatherActionResult.Started));
            if (leaveRange) accessible = false;
            if (fillCargo) Assert.That(cargo.TryAdd(new TopDown3DItemAmount(definition.YieldedItem.ItemId, 99)).Succeeded, Is.True);
            Assert.That(gather.Advance(2f, Vector3.zero), Is.EqualTo(TopDown3DGatherActionResult.Completed));
            Assert.That(gather.LastCompletionWasBigArm, Is.EqualTo(expectedCargo));
            Assert.That(booter.Slots.Sum(slot => slot.Quantity), Is.EqualTo(expectedCargo ? 0 : 1));
            Assert.That(cargo.Slots.Sum(slot => slot.Quantity), Is.EqualTo(fillCargo ? 99 : expectedCargo ? 1 : 0));
            Assert.That(gather.Advance(2f, Vector3.zero), Is.EqualTo(TopDown3DGatherActionResult.Rejected));
            Assert.That(node.IsDepleted, Is.True);
        }

        [Test]
        public void BothDestinationsFillingDuringGatherLeavesResourceUntouched()
        {
            var node = MakeNode().GetComponent<TopDown3DIronstoneNode>();
            var booter = new TopDown3DInventoryState(catalog, 1);
            var cargo = new TopDown3DInventoryState(catalog, new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.BigArm, 1));
            var gather = new TopDown3DGatherActionState(new TopDown3DItemDestinationService(booter, cargo, () => true));
            Assert.That(gather.TryBegin(node, Vector3.zero), Is.EqualTo(TopDown3DGatherActionResult.Started));
            booter.TryAdd(new TopDown3DItemAmount(definition.YieldedItem.ItemId, 99));
            cargo.TryAdd(new TopDown3DItemAmount(definition.YieldedItem.ItemId, 99));
            Assert.That(gather.Advance(2f, Vector3.zero), Is.EqualTo(TopDown3DGatherActionResult.InventoryFull));
            Assert.That(node.IsDepleted, Is.False);
        }

        [Test]
        public void CrowdedScanAndNewObstructionCannotLoseOrCommitTarget()
        {
            var node = MakeNode().GetComponent<TopDown3DIronstoneNode>();
            var actor = Own(new GameObject("Harvest actor"));
            var motor = actor.AddComponent<TopDown3DPlayerMotor>();
            var interaction = actor.AddComponent<TopDown3DInteractionController>();
            interaction.Configure(motor);
            for (var i = 0; i < 48; i++)
            {
                var distractor = Own(GameObject.CreatePrimitive(PrimitiveType.Cube));
                distractor.transform.position = new Vector3(2f + i * 0.01f, 0.8f, -1f);
                distractor.transform.localScale = Vector3.one * 0.05f;
            }
            Physics.SyncTransforms();
            Assert.That(Physics.OverlapSphere(Vector3.zero, 4f).Length, Is.GreaterThan(24));
            interaction.RefreshTarget();
            Assert.That(interaction.CurrentTarget, Is.SameAs(node));
            var inventory = new TopDown3DInventoryState(catalog, 1);
            var gather = new TopDown3DGatherActionState(new TopDown3DItemDestinationService(inventory, null, () => false), interaction.CanReachTarget);
            Assert.That(gather.TryBegin(node, Vector3.zero), Is.EqualTo(TopDown3DGatherActionResult.Started));
            var blocker = Own(GameObject.CreatePrimitive(PrimitiveType.Cube));
            blocker.transform.position = new Vector3(0f, 0.8f, 0.75f);
            blocker.transform.localScale = new Vector3(0.8f, 1.5f, 0.2f);
            Physics.SyncTransforms();
            Assert.That(gather.Advance(2f, Vector3.zero), Is.EqualTo(TopDown3DGatherActionResult.Cancelled));
            Assert.That(inventory.OccupiedSlotCount, Is.Zero);
            Assert.That(node.IsDepleted, Is.False);
        }

        [Test]
        public void ActionControllerResolvesSameSceneCargoWhenEnabled()
        {
            var settings = ScriptableObject.CreateInstance<TopDown3DPackingSettings>();
            settings.ConfigureDefaults();
            try
            {
                var cargo = Own(new GameObject("Serialized scene cargo")).AddComponent<TopDown3DBigArmCargo>();
                cargo.Configure(catalog, settings);
                var actor = Own(new GameObject("Serialized action actor"));
                actor.SetActive(false);
                var inventory = actor.AddComponent<TopDown3DPlayerInventory>();
                inventory.Configure(catalog);
                var action = actor.AddComponent<TopDown3DPlayerActionController>();
                var serialized = new SerializedObject(action);
                serialized.FindProperty("inventory").objectReferenceValue = inventory;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                actor.SetActive(true);
                var field = typeof(TopDown3DPlayerActionController).GetField("bigArmCargo", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(field.GetValue(action), Is.SameAs(cargo));
                actor.SetActive(false);
                actor.SetActive(true);
                Assert.That(field.GetValue(action), Is.SameAs(cargo));
                var notifications = 0;
                cargo.Changed += () => notifications++;
                Assert.That(cargo.State.TryAdd(new TopDown3DItemAmount(definition.YieldedItem.ItemId, 1)).Succeeded, Is.True);
                Assert.That(cargo.LoadProfile.TotalMass, Is.EqualTo(definition.YieldedItem.Mass));
                Assert.That(notifications, Is.EqualTo(1), "Direct harvest delivery must update cargo load exactly once.");
            }
            finally { Object.DestroyImmediate(settings); }
        }
    }
}
