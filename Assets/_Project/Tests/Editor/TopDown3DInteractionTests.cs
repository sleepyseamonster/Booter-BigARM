using System.Linq;
using System.Reflection;
using BooterBigArm.TopDown3D;
using BooterBigArm.TopDown3D.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DInteractionTests
    {
        private const string InputPath = "Assets/_Project/Settings/Input/InputSystem_Actions.inputactions";

        private Texture2D texture;
        private Sprite icon;
        private TopDown3DItemDefinition item;
        private TopDown3DItemCatalog catalog;

        [SetUp]
        public void SetUp()
        {
            texture = new Texture2D(2, 2);
            icon = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), Vector2.one * 0.5f);
            item = ScriptableObject.CreateInstance<TopDown3DItemDefinition>();
            item.Configure("resource.ironstone_ore", "Ironstone Ore", "Test", "Resource", icon, 1, 0f);
            catalog = ScriptableObject.CreateInstance<TopDown3DItemCatalog>();
            catalog.Configure(new[] { item });
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(item);
            Object.DestroyImmediate(icon);
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void InputAsset_InteractIsAnImmediateKeyboardAndGamepadButton()
        {
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            var interact = actions.FindAction("Gameplay/Interact", true);

            Assert.That(string.IsNullOrEmpty(interact.interactions), Is.True);
            Assert.That(interact.bindings.Select(binding => binding.effectivePath),
                Is.EquivalentTo(new[] { "<Gamepad>/buttonWest", "<Keyboard>/e" }));
        }

        [Test]
        public void PlayerActionController_RebuildsSerializedGatherStateWhenEnabled()
        {
            var player = new GameObject("Serialized Gather Player");
            player.SetActive(false);
            try
            {
                var inventory = player.AddComponent<TopDown3DPlayerInventory>();
                inventory.Configure(catalog, 2);
                var controller = player.AddComponent<TopDown3DPlayerActionController>();
                var serializedController = new SerializedObject(controller);
                serializedController.FindProperty("inventory").objectReferenceValue = inventory;
                serializedController.ApplyModifiedPropertiesWithoutUndo();

                player.SetActive(true);

                var gatherState = typeof(TopDown3DPlayerActionController).GetField(
                    "gatherAction",
                    BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(controller);
                Assert.That(gatherState, Is.Not.Null,
                    "Scene-loaded action controllers must rebuild their non-serialized gather state.");
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void ValidCompletion_AwardsAndConsumesExactlyOnce()
        {
            var inventory = new TopDown3DInventoryState(catalog, 2);
            var inventoryChangeCount = 0;
            inventory.Changed += () => inventoryChangeCount++;
            using var target = new FakeTarget(item.ItemId, Vector3.forward, consumeSucceeds: true);
            var action = new TopDown3DGatherActionState(inventory);

            Assert.That(action.TryBegin(target, Vector3.zero), Is.EqualTo(TopDown3DGatherActionResult.Started));
            Assert.That(action.Advance(0.5f, Vector3.zero), Is.EqualTo(TopDown3DGatherActionResult.Running));
            Assert.That(action.Advance(0.5f, Vector3.zero), Is.EqualTo(TopDown3DGatherActionResult.Completed));
            Assert.That(action.Advance(1f, Vector3.zero), Is.EqualTo(TopDown3DGatherActionResult.Rejected));
            Assert.That(target.ConsumeCount, Is.EqualTo(1));
            Assert.That(inventory.Slots.Sum(slot => slot.Quantity), Is.EqualTo(1));
            Assert.That(inventoryChangeCount, Is.EqualTo(1));
        }

        [Test]
        public void FullInventory_RejectsBeforeActionAndLeavesTargetUntouched()
        {
            var inventory = new TopDown3DInventoryState(catalog, 1);
            Assert.That(inventory.TryAdd(new TopDown3DItemAmount(item.ItemId, 1)).Succeeded, Is.True);
            using var target = new FakeTarget(item.ItemId, Vector3.forward, consumeSucceeds: true);
            var action = new TopDown3DGatherActionState(inventory);

            Assert.That(action.TryBegin(target, Vector3.zero), Is.EqualTo(TopDown3DGatherActionResult.InventoryFull));
            Assert.That(target.ConsumeCount, Is.Zero);
            Assert.That(inventory.Slots.Sum(slot => slot.Quantity), Is.EqualTo(1));
        }

        [Test]
        public void CancelRangeExitAndDestroyedTarget_AwardNothing()
        {
            var inventory = new TopDown3DInventoryState(catalog, 4);
            var action = new TopDown3DGatherActionState(inventory);
            using (var cancelled = new FakeTarget(item.ItemId, Vector3.forward, consumeSucceeds: true))
            {
                Assert.That(action.TryBegin(cancelled, Vector3.zero), Is.EqualTo(TopDown3DGatherActionResult.Started));
                Assert.That(action.Cancel(), Is.EqualTo(TopDown3DGatherActionResult.Cancelled));
                Assert.That(cancelled.ConsumeCount, Is.Zero);
            }

            using (var distant = new FakeTarget(item.ItemId, Vector3.forward, consumeSucceeds: true))
            {
                Assert.That(action.TryBegin(distant, Vector3.zero), Is.EqualTo(TopDown3DGatherActionResult.Started));
                Assert.That(action.Advance(1f, Vector3.back * 10f), Is.EqualTo(TopDown3DGatherActionResult.Cancelled));
                Assert.That(distant.ConsumeCount, Is.Zero);
            }

            var unloaded = new FakeTarget(item.ItemId, Vector3.forward, consumeSucceeds: true);
            Assert.That(action.TryBegin(unloaded, Vector3.zero), Is.EqualTo(TopDown3DGatherActionResult.Started));
            unloaded.Dispose();
            Assert.That(action.Advance(1f, Vector3.zero), Is.EqualTo(TopDown3DGatherActionResult.Cancelled));
            Assert.That(inventory.OccupiedSlotCount, Is.Zero);
        }

        [Test]
        public void FailedResourceCommit_RollsBackInventoryAndCannotDuplicate()
        {
            var inventory = new TopDown3DInventoryState(catalog, 2);
            var inventoryChangeCount = 0;
            inventory.Changed += () => inventoryChangeCount++;
            using var target = new FakeTarget(item.ItemId, Vector3.forward, consumeSucceeds: false);
            var action = new TopDown3DGatherActionState(inventory);

            Assert.That(action.TryBegin(target, Vector3.zero), Is.EqualTo(TopDown3DGatherActionResult.Started));
            Assert.That(action.Advance(1f, Vector3.zero), Is.EqualTo(TopDown3DGatherActionResult.CommitFailed));
            Assert.That(action.Advance(1f, Vector3.zero), Is.EqualTo(TopDown3DGatherActionResult.Rejected));
            Assert.That(inventory.OccupiedSlotCount, Is.Zero);
            Assert.That(target.ConsumeCount, Is.EqualTo(1));
            Assert.That(inventoryChangeCount, Is.Zero);
        }

        [Test]
        public void MotorActionConstraint_IsOwnerBoundAndFacesTarget()
        {
            var player = new GameObject("Constrained Motor");
            try
            {
                player.AddComponent<Rigidbody>();
                player.AddComponent<CapsuleCollider>();
                var motor = player.AddComponent<TopDown3DPlayerMotor>();
                var owner = new object();

                Assert.That(motor.TryBeginActionConstraint(owner), Is.True);
                Assert.That(motor.TryBeginActionConstraint(new object()), Is.False);
                Assert.That(motor.IsActionConstrained, Is.True);
                motor.FacePlanarDirection(Vector3.right);
                Assert.That(Vector3.Dot(motor.FacingDirection, Vector3.right), Is.GreaterThan(0.999f));
                motor.EndActionConstraint(new object());
                Assert.That(motor.IsActionConstrained, Is.True);
                motor.EndActionConstraint(owner);
                Assert.That(motor.IsActionConstrained, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void GatherAnimation_IsHumanoidInPlaceAndNonLooping()
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                TopDown3DIronstoneAssetBuilder.GatherAnimationPath);

            Assert.That(clip, Is.Not.Null);
            Assert.That(clip.humanMotion, Is.True);
            Assert.That(clip.isLooping, Is.False);
            Assert.That(clip.length, Is.EqualTo(1.1f).Within(0.001f));
            var bindings = AnimationUtility.GetCurveBindings(clip);
            Assert.That(bindings, Is.Not.Empty);
            Assert.That(bindings.All(binding =>
                !binding.propertyName.StartsWith("RootT")
                && !binding.propertyName.StartsWith("RootQ")
                && !binding.propertyName.StartsWith("MotionT")
                && !binding.propertyName.StartsWith("MotionQ")), Is.True);
        }

        [Test]
        public void Targeting_RejectsBlockedNodeAndSelectsItWhenLineOfSightClears()
        {
            var player = new GameObject("Targeting Player");
            var nodeObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var stateObject = new GameObject("Resource State");
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var active = new Material(shader);
            var depleted = new Material(shader);
            var definition = ScriptableObject.CreateInstance<TopDown3DResourceDefinition>();
            try
            {
                player.transform.position = Vector3.zero;
                player.AddComponent<Rigidbody>().isKinematic = true;
                player.AddComponent<CapsuleCollider>();
                var motor = player.AddComponent<TopDown3DPlayerMotor>();
                motor.FacePlanarDirection(Vector3.forward);
                var controller = player.AddComponent<TopDown3DInteractionController>();
                controller.Configure(motor);
                definition.Configure(
                    "resource.ironstone_node",
                    "unused-test-family",
                    TopDown3DNaturalObjectShape.Outcrop,
                    0,
                    active,
                    depleted,
                    item,
                    1,
                    1,
                    2.2f,
                    1f,
                    "Gather Ironstone",
                    1f,
                    8f,
                    2f);
                var state = stateObject.AddComponent<TopDown3DResourceWorldState>();
                state.Configure(1);
                nodeObject.transform.position = new Vector3(0f, 0.8f, 1.5f);
                var node = nodeObject.AddComponent<TopDown3DIronstoneNode>();
                var placement = new TopDown3DResourceNodePlacement(
                    "resource:test:node",
                    definition.ResourceId,
                    Vector2Int.zero,
                    nodeObject.transform.position,
                    Quaternion.identity,
                    Vector3.one,
                    0,
                    0);
                node.Configure(
                    placement,
                    definition,
                    state,
                    new Renderer[] { nodeObject.GetComponent<Renderer>() },
                    nodeObject.GetComponent<Collider>());
                blocker.transform.position = new Vector3(0f, 0.8f, 0.75f);
                blocker.transform.localScale = new Vector3(0.8f, 1.5f, 0.2f);
                Physics.SyncTransforms();

                controller.RefreshTarget();
                Assert.That(controller.CurrentTarget, Is.Null);

                blocker.SetActive(false);
                Physics.SyncTransforms();
                controller.RefreshTarget();
                Assert.That(controller.CurrentTarget, Is.SameAs(node));
            }
            finally
            {
                Object.DestroyImmediate(definition);
                Object.DestroyImmediate(active);
                Object.DestroyImmediate(depleted);
                Object.DestroyImmediate(stateObject);
                Object.DestroyImmediate(blocker);
                Object.DestroyImmediate(nodeObject);
                Object.DestroyImmediate(player);
            }
        }

        private sealed class FakeTarget : ITopDown3DInteractable, System.IDisposable
        {
            private readonly bool consumeSucceeds;
            private ScriptableObject marker;

            public FakeTarget(string itemId, Vector3 point, bool consumeSucceeds)
            {
                marker = ScriptableObject.CreateInstance<Marker>();
                Reward = new TopDown3DItemAmount(itemId, 1);
                InteractionPoint = point;
                this.consumeSucceeds = consumeSucceeds;
            }

            public Object UnityObject => marker;
            public string StableId => "resource:test";
            public string Prompt => "Gather Ironstone";
            public Vector3 InteractionPoint { get; }
            public float InteractionRange => 2.2f;
            public float ActionDuration => 1f;
            public TopDown3DItemAmount Reward { get; }
            public bool IsAvailable => marker != null;
            public int ConsumeCount { get; private set; }

            public bool TryConsume(out int remainingUses)
            {
                ConsumeCount++;
                remainingUses = consumeSucceeds ? 0 : 1;
                return consumeSucceeds;
            }

            public void Dispose()
            {
                if (marker != null)
                {
                    Object.DestroyImmediate(marker);
                    marker = null;
                }
            }

            private sealed class Marker : ScriptableObject
            {
            }
        }
    }
}
