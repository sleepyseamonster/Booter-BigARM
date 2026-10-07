using System;
using System.IO;
using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using Object = UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DMenuTests
    {
        private GameObject player, inventoryRoot, radialRoot, menuRoot, eventRoot;
        private InputActionAsset actions;
        private InputSettings originalSettings, testSettings;
        private Gamepad pad;
        private Keyboard keyboard;
        private TopDown3DMenuController menu;
        private TopDown3DInputRouter router;
        private TopDown3DInventoryUiController inventory;
        private TopDown3DRadialMenuController radial;
        private TopDown3DItemCatalog catalog;
        private TopDown3DItemDefinition item;
        private Texture2D texture;
        private Sprite icon;
        private string directory;
        private float initialTime;
        public TopDown3DMenuController Menu => menu;
        public Gamepad Pad => pad;
        public GameObject MenuRoot => menuRoot;
        public EventSystem Events => eventRoot.GetComponent<EventSystem>();
        public InputSystemUIInputModule Module => eventRoot.GetComponent<InputSystemUIInputModule>();
        public void ProcessUiState(GamepadState state) { InputSystem.QueueStateEvent(pad, state); InputSystem.Update(); Tick(menu); Module.Process(); }

        [SetUp]
        public void SetUp()
        {
            initialTime = Time.timeScale;
            directory = Path.Combine(Path.GetTempPath(), "BooterMenuTests", Guid.NewGuid().ToString("N"));
            originalSettings = InputSystem.settings;
            testSettings = Object.Instantiate(originalSettings);
            testSettings.SetInternalFeatureFlag("RUN_PLAYER_UPDATES_IN_EDIT_MODE", true);
            testSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            testSettings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            InputSystem.settings = testSettings;
            pad = InputSystem.AddDevice<Gamepad>(); keyboard = InputSystem.AddDevice<Keyboard>();
            actions = Object.Instantiate(AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Project/Settings/Input/InputSystem_Actions.inputactions"));
            player = new GameObject("Menu test player"); player.SetActive(false);
            router = player.AddComponent<TopDown3DInputRouter>(); router.Configure(actions); router.Initialize();
            var kit = player.AddComponent<TopDown3DPlayerInventory>();
            texture = new Texture2D(2, 2); icon = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f);
            item = ScriptableObject.CreateInstance<TopDown3DItemDefinition>(); item.Configure("menu.test", "Item", "", "Test", icon, 10, 0);
            catalog = ScriptableObject.CreateInstance<TopDown3DItemCatalog>(); catalog.Configure(new[] { item }); kit.Configure(catalog);
            eventRoot = new GameObject("Menu test EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var system = eventRoot.GetComponent<EventSystem>(); var module = eventRoot.GetComponent<InputSystemUIInputModule>();
            module.actionsAsset = actions;
            module.submit = InputActionReference.Create(actions.FindAction("UI/Submit"));
            module.move = InputActionReference.Create(actions.FindAction("UI/Navigate"));
            module.cancel = InputActionReference.Create(actions.FindAction("UI/Cancel"));
            module.leftClick = InputActionReference.Create(actions.FindAction("UI/Click"));
            // EditMode does not run the BaseInputModule lifecycle on this synthetic fixture.
            if (!Application.isPlaying) typeof(InputSystemUIInputModule).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(module, null);
            inventoryRoot = new GameObject("Test inventory", typeof(RectTransform));
            var canvas = inventoryRoot.AddComponent<TopDown3DInventoryCanvas>(); inventory = inventoryRoot.AddComponent<TopDown3DInventoryUiController>();
            inventory.Configure(router, kit, null, canvas, system);
            radialRoot = new GameObject("Test wheel", typeof(RectTransform));
            var wheelView = radialRoot.AddComponent<TopDown3DRadialCanvas>(); wheelView.Build(); radial = radialRoot.AddComponent<TopDown3DRadialMenuController>();
            radial.Configure(router, kit, inventory, null, null, null, wheelView, Path.Combine(directory, "radial-layout-v1.json"));
            radial.SetPreferencePathForTests(Path.Combine(directory, "radial-layout-v1.json"));
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/Resources/SystemMenu.prefab");
            Assert.That(prefab, Is.Not.Null, "Author the canonical menu prefab before running these tests.");
            menuRoot = Object.Instantiate(prefab); menu = menuRoot.AddComponent<TopDown3DMenuController>();
            menu.Configure(router, inventory, radial, menuRoot.GetComponent<TopDown3DMenuView>(), module, directory);
            menuRoot.SetActive(true); player.SetActive(true);
        }

        private static void Tick(object owner) => owner.GetType().GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(owner, null);
        private void Step(GamepadState state) { InputSystem.QueueStateEvent(pad, state); InputSystem.Update(); Tick(menu); Tick(radial); }
        private void Command(string name)
        { Tick(menu); typeof(TopDown3DMenuController).GetMethod("OnCommand", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(menu, new object[] { name }); }

        [TearDown]
        public void TearDown()
        {
            if (eventRoot != null && !Application.isPlaying) typeof(InputSystemUIInputModule).GetMethod("OnDisable", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(eventRoot.GetComponent<InputSystemUIInputModule>(), null);
            foreach (var root in new[] { menuRoot, radialRoot, inventoryRoot, player, eventRoot }) if (root != null) Object.DestroyImmediate(root);
            if (actions != null) Object.DestroyImmediate(actions); if (catalog != null) Object.DestroyImmediate(catalog);
            if (item != null) Object.DestroyImmediate(item); if (icon != null) Object.DestroyImmediate(icon); if (texture != null) Object.DestroyImmediate(texture);
            if (pad != null) InputSystem.RemoveDevice(pad); if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            InputSystem.settings = originalSettings; if (testSettings != null) Object.DestroyImmediate(testSettings);
            Time.timeScale = initialTime;
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        [Test]
        public void Start_OpensOnceAndDoesNotChangeSimulationTime()
        {
            Time.timeScale = 0.7f;
            Assert.That(actions.FindAction("System/ToggleMenu").controls, Does.Contain(pad.startButton));
            Step(new GamepadState().WithButton(GamepadButton.Start));
            Assert.That(menu.CurrentPanel, Is.EqualTo("Root")); Assert.That(router.Mode, Is.EqualTo(TopDown3DInputMode.SystemMenu));
            Step(new GamepadState().WithButton(GamepadButton.Start)); Assert.That(menu.IsOpen, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(0.7f));
            Step(new GamepadState()); Step(new GamepadState().WithButton(GamepadButton.Start));
            Assert.That(menu.IsOpen, Is.False); Assert.That(router.Mode, Is.EqualTo(TopDown3DInputMode.Gameplay));
            Assert.That(Time.timeScale, Is.EqualTo(0.7f));
        }

        [Test]
        public void Escape_OpensOnceThenBacksOutWithoutDoubleTransition()
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); InputSystem.Update(); Tick(menu);
            Assert.That(menu.CurrentPanel, Is.EqualTo("Root"));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
            Command("Settings");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); InputSystem.Update();
            Assert.That(menu.CurrentPanel, Is.EqualTo("Root"));
        }

        [Test]
        public void Inventory_IsSuspendedAndSystemToggleCannotCloseIt()
        {
            inventory.Open(); Step(new GamepadState().WithButton(GamepadButton.Start));
            Assert.That(inventory.IsSuspended, Is.True);
            Step(new GamepadState()); Step(new GamepadState().WithButton(GamepadButton.North));
            Assert.That(inventory.IsSuspended, Is.True); Assert.That(menu.IsOpen, Is.True);
            menu.Back(); Assert.That(inventory.IsOpen, Is.True); Assert.That(inventory.IsSuspended, Is.False);
            Assert.That(router.Mode, Is.EqualTo(TopDown3DInputMode.Inventory));
        }

        [Test]
        public void Start_WinsOverWheelStickReleaseWithoutCommand()
        {
            Step(new GamepadState().WithButton(GamepadButton.LeftShoulder));
            Step(new GamepadState { rightStick = Vector2.up }.WithButton(GamepadButton.LeftShoulder));
            Step(new GamepadState().WithButton(GamepadButton.LeftShoulder).WithButton(GamepadButton.Start));
            Assert.That(menu.IsOpen, Is.True); Assert.That(radial.IsOpen, Is.False); Assert.That(inventory.IsOpen, Is.False);
        }

        [Test]
        public void Setup_DirtyBackProtectsPreferencesAndExplicitDiscardReturnsRoot()
        {
            menu.Open(); Command("RadialSetup"); Command("Slot0"); Command("Assign0");
            menu.Back(); Assert.That(menu.CurrentPanel, Is.EqualTo("Dialog"));
            Command("DialogCancel"); Assert.That(menu.CurrentPanel, Is.EqualTo("Setup"));
            menu.Back(); Command("DialogAccept"); Assert.That(menu.CurrentPanel, Is.EqualTo("Root"));
            Assert.That(radial.Preferences.pages[0].slots[0], Is.EqualTo(TopDown3DRadialCommand.PlayerInventory));
        }

        [Test]
        public void SetupApply_PersistsExistingSchemaAndReturnsToInvokingInventory()
        {
            inventory.Open(); menu.OpenSetupFromInventory(); Command("Slot0"); Command("Assign0"); Command("SetupApply");
            Assert.That(menu.IsOpen, Is.False); Assert.That(inventory.IsOpen, Is.True);
            var data = JsonUtility.FromJson<TopDown3DRadialPreferences>(File.ReadAllText(Path.Combine(directory, "radial-layout-v1.json")));
            Assert.That(data.IsValid(), Is.True); Assert.That(data.pages[0].slots[0], Is.EqualTo(TopDown3DRadialCommand.Empty));
        }

        [Test]
        public void Exit_RequiresExplicitConfirmAndCannotBeAcceptedByStart()
        {
            var count = 0; menu.ExitRequested = () => count++;
            menu.Open(); Command("Exit"); menu.Toggle(); Assert.That(count, Is.Zero);
            menu.Back(); Assert.That(count, Is.Zero); Command("Exit"); Command("DialogAccept"); Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void DeletingEarlierPage_RetainsDefaultByStableIdentity()
        {
            menu.Open(); Command("RadialSetup"); Command("AddPage"); Command("AddPage"); Command("AddPage");
            Command("PreviousPage"); Command("DefaultPage");
            Command("PreviousPage"); Command("PreviousPage");
            Command("DeletePage"); Command("DialogAccept"); Command("SetupApply");
            Assert.That(radial.Preferences.pages.Length, Is.EqualTo(3)); Assert.That(radial.Preferences.defaultPage, Is.EqualTo(1));
        }

        [Test]
        public void DisabledInput_ClosesCoveredMenuWithoutReenablingGameplay()
        {
            inventory.Open(); menu.Open(); player.SetActive(false);
            // Exercise the lifecycle explicitly in the synthetic EditMode fixture.
            typeof(TopDown3DInputRouter).GetMethod("OnDisable", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(router, null);
            Assert.That(menu.IsOpen, Is.False); Assert.That(router.Mode, Is.EqualTo(TopDown3DInputMode.Disabled));
            Assert.That(actions.FindActionMap("UI").enabled, Is.False); Assert.That(actions.FindActionMap("System").enabled, Is.False);
            Assert.That(inventory.IsSuspended, Is.False); Assert.That(inventory.IsOpen, Is.False);
        }

        [Test]
        public void Settings_UnknownVersionIsPreservedAndFailedWritesRetainAcceptedState()
        {
            Directory.CreateDirectory(directory); var path = Path.Combine(directory, "menu-settings-v1.json");
            File.WriteAllText(path, "{\"version\":2,\"future\":true}"); var future = new TopDown3DMenuSettingsService(directory);
            Assert.That(future.CanSave, Is.False); Assert.That(future.TrySave(new TopDown3DMenuSettings(), out _), Is.False);
            Assert.That(File.ReadAllText(path), Does.Contain("future"));
            var blocker = Path.Combine(directory, "blocker"); File.WriteAllText(blocker, "x");
            var failure = new TopDown3DMenuSettingsService(blocker);
            Assert.That(failure.TrySave(new TopDown3DMenuSettings { largeText = true }, out _), Is.False);
            Assert.That(failure.Accepted.largeText, Is.False);
        }
    }
}
