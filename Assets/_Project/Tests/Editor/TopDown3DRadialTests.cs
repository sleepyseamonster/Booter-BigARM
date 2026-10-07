using System.Collections.Generic;
using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DRadialTests
    {
        private readonly List<Object> owned = new List<Object>();
        private T Own<T>(T item) where T : Object { owned.Add(item); return item; }
        [TearDown] public void Cleanup() { for (var i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]); owned.Clear(); }

        [TestCase(-45f)] [TestCase(45f)] [TestCase(135f)] [TestCase(225f)]
        public void SeamEdges_HaveConstantPerpendicularGapAtBothRadii(float seam)
        {
            var normal = TopDown3DRadialGeometry.Point(1f, seam + 90f);
            foreach (var radius in new[] { 68f, 207f, 210f })
            {
                var left = TopDown3DRadialGeometry.EdgePoint(radius, seam, 6f, false);
                var right = TopDown3DRadialGeometry.EdgePoint(radius, seam, 6f, true);
                Assert.That(Vector2.Dot(right - left, normal), Is.EqualTo(6f).Within(0.001f));
            }
        }

        [Test]
        public void RingMesh_HasNoCenterTrianglesAndFacesCanvas()
        {
            var root = Own(new GameObject("Ring geometry", typeof(RectTransform)));
            var graphic = root.AddComponent<TopDown3DRadialRingGraphic>();
            graphic.rectTransform.sizeDelta = new Vector2(420f, 420f); graphic.Selected = 0;
            using (var vh = new VertexHelper())
            {
                typeof(TopDown3DRadialRingGraphic).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Invoke(graphic, new object[] { vh });
                var mesh = Own(new Mesh()); vh.FillMesh(mesh); var points = mesh.vertices; var triangles = mesh.triangles;
                foreach (var p in points) Assert.That(new Vector2(p.x, p.y).magnitude, Is.GreaterThanOrEqualTo(67.99f));
                for (var i = 0; i < triangles.Length; i += 3)
                    Assert.That(Vector3.Cross(points[triangles[i + 1]] - points[triangles[i]], points[triangles[i + 2]] - points[triangles[i]]).z, Is.LessThan(0f));
            }
        }

        [Test]
        public void DirectionalSelection_ClearsCenterAndResistsBoundaryJitter()
        {
            Assert.That(TopDown3DRadialGeometry.Select(Vector2.up, 4), Is.EqualTo(0));
            Assert.That(TopDown3DRadialGeometry.Select(Vector2.right, 4), Is.EqualTo(1));
            Assert.That(TopDown3DRadialGeometry.Select(Vector2.down, 4), Is.EqualTo(2));
            Assert.That(TopDown3DRadialGeometry.Select(Vector2.left, 4), Is.EqualTo(3));
            Assert.That(TopDown3DRadialGeometry.Select(TopDown3DRadialGeometry.Point(1f, 48f), 4, 0), Is.EqualTo(0));
            Assert.That(TopDown3DRadialGeometry.Select(TopDown3DRadialGeometry.Point(1f, 52f), 4, 0), Is.EqualTo(1));
            Assert.That(TopDown3DRadialGeometry.Select(Vector2.zero, 4, 0), Is.EqualTo(-1));
        }

        [Test]
        public void Preferences_RejectCorruptionAndCloneWithoutSharingAssignments()
        {
            var p = new TopDown3DRadialPreferences(); Assert.That(p.IsValid(), Is.True);
            var copy = p.Clone(); copy.pages[0].slots[0] = TopDown3DRadialCommand.Empty;
            Assert.That(p.pages[0].slots[0], Is.EqualTo(TopDown3DRadialCommand.PlayerInventory));
            copy.scale = float.NaN; Assert.That(copy.IsValid(), Is.False);
            copy.scale = 1f; copy.pages[0].slots[0] = (TopDown3DRadialCommand)999; Assert.That(copy.IsValid(), Is.False);
        }

        [Test]
        public void ModalTransition_PreservesHeldSystemOpenerAndBlocksInventoryShortcut()
        {
            var actions = Own(Object.Instantiate(AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Project/Settings/Input/InputSystem_Actions.inputactions")));
            var root = Own(new GameObject("Radial mode")); root.SetActive(false);
            var router = root.AddComponent<TopDown3DInputRouter>(); router.Configure(actions); root.SetActive(true);
            Assert.That(router.Initialize(), Is.True);
            var previousSettings = InputSystem.settings;
            var testSettings = Own(Object.Instantiate(previousSettings));
            testSettings.SetInternalFeatureFlag("RUN_PLAYER_UPDATES_IN_EDIT_MODE", true);
            testSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            testSettings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            InputSystem.settings = testSettings;
            var gamepad = InputSystem.AddDevice<Gamepad>();
            try
            {
                var opener = actions.FindAction("System/OpenRadial", true); var canceled = 0; opener.canceled += _ => canceled++;
                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.LeftShoulder)); InputSystem.Update();
                Assert.That(opener.IsPressed(), Is.True); router.EnterRadialMode();
                Assert.That(canceled, Is.Zero); Assert.That(opener.IsPressed(), Is.True);
                Assert.That(actions.FindActionMap("Gameplay").enabled, Is.False);
                Assert.That(actions.FindActionMap("Radial").enabled, Is.True);
                var toggles = 0; router.InventoryToggleRequested += () => toggles++;
                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.North).WithButton(GamepadButton.LeftShoulder)); InputSystem.Update();
                Assert.That(toggles, Is.Zero);
                InputSystem.QueueStateEvent(gamepad, new GamepadState { rightStick = Vector2.up }.WithButton(GamepadButton.LeftShoulder)); InputSystem.Update();
                router.EnterGameplayMode(); InputSystem.Update();
                Assert.That(actions.FindActionMap("Radial").enabled, Is.False);
                Assert.That(router.CameraLookValue, Is.EqualTo(Vector2.zero), "Menu selection must not immediately orbit the camera.");
                InputSystem.QueueStateEvent(gamepad, new GamepadState()); InputSystem.Update();
                InputSystem.QueueStateEvent(gamepad, new GamepadState { rightStick = Vector2.right }); InputSystem.Update();
                Assert.That(router.CameraLookValue.x, Is.GreaterThan(0.5f));
            }
            finally { InputSystem.RemoveDevice(gamepad); InputSystem.settings = previousSettings; }
        }

        [Test]
        public void AuthoredPlacement_ConsumesOnlyOnCommitAndRestoresIdentity()
        {
            var tex = Own(new Texture2D(2, 2)); var icon = Own(Sprite.Create(tex, new Rect(0, 0, 2, 2), Vector2.one * 0.5f));
            var canister = Own(ScriptableObject.CreateInstance<TopDown3DItemDefinition>());
            canister.Configure(TopDown3DHarvesterSettings.CanisterItemId, "Canister", "", "Tool", icon, 10, 0);
            var catalog = Own(ScriptableObject.CreateInstance<TopDown3DItemCatalog>()); catalog.Configure(new[] { canister });
            var inventory = new TopDown3DInventoryState(catalog, 2); inventory.TryAdd(new TopDown3DItemAmount(canister.ItemId, 1));
            var root = Own(new GameObject("Authored harvester")); var state = root.AddComponent<TopDown3DPlacedHarvesterState>();
            state.ConfigureAuthored("authored.scene.guid", Own(ScriptableObject.CreateInstance<TopDown3DHarvesterSettings>()));
            Assert.That(state.TryPlace(new Vector3(float.NaN, 0, 0), inventory, out _), Is.False);
            Assert.That(inventory.Slots[0].Quantity, Is.EqualTo(1));
            Assert.That(state.TryPlace(new Vector3(10, 0, 10), inventory, out var id), Is.True);
            Assert.That(inventory.OccupiedSlotCount, Is.Zero);
            var snapshot = state.CaptureSnapshot(); Assert.That(state.CanApplySnapshot(snapshot), Is.True);
            state.ConfigureAuthored("different.scene.guid", state.Settings); Assert.That(state.CanApplySnapshot(snapshot), Is.False);
            state.ConfigureAuthored("authored.scene.guid", state.Settings); Assert.That(state.ApplySnapshot(snapshot), Is.True);
            Assert.That(state.TryPickup(id, inventory, out var dust), Is.True); Assert.That(dust, Is.Zero);
            Assert.That(inventory.Slots[0].Quantity, Is.EqualTo(1)); Assert.That(state.DeployedCount, Is.Zero);
        }

        [Test]
        public void CallOver_TargetsCloseArrivalWithoutRelocatingCompanion()
        {
            var player = Own(new GameObject("Callover target")); player.transform.position = new Vector3(10, 0, 10);
            var companion = Own(new GameObject("Callover companion"));
            var follower = companion.AddComponent<TopDown3DBigArmFollower>();
            follower.Configure(player.transform, null, null);
            typeof(TopDown3DBigArmFollower).GetField("body", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(follower, companion.GetComponent<Rigidbody>());
            var before = companion.transform.position;
            follower.RequestCallOver();
            Assert.That(companion.transform.position, Is.EqualTo(before));
            var destination = (Vector3)typeof(TopDown3DBigArmFollower).GetMethod("GetDesiredFollowPosition", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(follower, null);
            Assert.That(Vector3.ProjectOnPlane(destination - player.transform.position, Vector3.up).magnitude, Is.InRange(1.1f, 1.36f));
        }

        [Test]
        public void MenuSession_CancelAndReleaseTogetherNeverCommit_ValidReleaseOpensInventoryOnce()
        {
            var priorSettings = InputSystem.settings;
            var settings = Own(Object.Instantiate(priorSettings));
            settings.SetInternalFeatureFlag("RUN_PLAYER_UPDATES_IN_EDIT_MODE", true);
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            InputSystem.settings = settings;
            var pad = InputSystem.AddDevice<Gamepad>();
            var actions = Own(Object.Instantiate(AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Project/Settings/Input/InputSystem_Actions.inputactions")));
            try
            {
                var player = Own(new GameObject("Radial session player")); player.SetActive(false);
                var router = player.AddComponent<TopDown3DInputRouter>(); router.Configure(actions); router.Initialize();
                var tex = Own(new Texture2D(2, 2)); var icon = Own(Sprite.Create(tex, new Rect(0, 0, 2, 2), Vector2.one * 0.5f));
                var item = Own(ScriptableObject.CreateInstance<TopDown3DItemDefinition>()); item.Configure("test.item", "Item", "", "Test", icon, 10, 0);
                var catalog = Own(ScriptableObject.CreateInstance<TopDown3DItemCatalog>()); catalog.Configure(new[] { item });
                var kit = player.AddComponent<TopDown3DPlayerInventory>(); kit.Configure(catalog);
                var inventoryRoot = Own(new GameObject("Inventory", typeof(RectTransform)));
                var inventoryCanvas = inventoryRoot.AddComponent<TopDown3DInventoryCanvas>();
                var inventoryUi = inventoryRoot.AddComponent<TopDown3DInventoryUiController>();
                inventoryUi.Configure(router, kit, null, inventoryCanvas, null);
                var menuRoot = Own(new GameObject("Radial", typeof(RectTransform)));
                var view = menuRoot.AddComponent<TopDown3DRadialCanvas>(); view.Build();
                var menu = menuRoot.AddComponent<TopDown3DRadialMenuController>(); menu.Configure(router, kit, inventoryUi, null, null, null, view);
                var tick = typeof(TopDown3DRadialMenuController).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
                void Step(GamepadState state) { InputSystem.QueueStateEvent(pad, state); InputSystem.Update(); tick.Invoke(menu, null); }
                Step(new GamepadState().WithButton(GamepadButton.LeftShoulder));
                Step(new GamepadState { rightStick = Vector2.up }.WithButton(GamepadButton.LeftShoulder));
                Assert.That(menu.SelectedSector, Is.EqualTo(0));
                Step(new GamepadState { rightStick = Vector2.up }.WithButton(GamepadButton.East));
                Assert.That(menu.IsOpen, Is.False); Assert.That(inventoryUi.IsOpen, Is.False);
                Step(new GamepadState());
                Step(new GamepadState().WithButton(GamepadButton.LeftShoulder));
                Step(new GamepadState { rightStick = Vector2.up }.WithButton(GamepadButton.LeftShoulder));
                Step(new GamepadState { rightStick = Vector2.up });
                Assert.That(inventoryUi.IsOpen, Is.True); Assert.That(menu.IsOpen, Is.False);
                tick.Invoke(menu, null); Assert.That(inventoryUi.IsOpen, Is.True);
            }
            finally { actions.Disable(); InputSystem.RemoveDevice(pad); InputSystem.settings = priorSettings; }
        }
    }
}
