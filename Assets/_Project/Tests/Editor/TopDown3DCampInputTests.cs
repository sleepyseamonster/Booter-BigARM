using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DCampInputTests
    {
        [Test]
        public void CampFocusSkipsUnavailableChoicesAndPreservesNavigation()
        {
            var root = new GameObject("Camp focus fixture", typeof(RectTransform));
            var events = new GameObject("Camp events fixture");
            try
            {
                var system = events.AddComponent<EventSystem>();
                var canvas = root.AddComponent<TopDown3DCampCanvas>(); canvas.Configure(() => { }, () => { }, () => { });
                canvas.Show("Camp", true, false); canvas.SetAvailability(false, false, true); canvas.Focus(system);
                Assert.That(system.currentSelectedGameObject, Is.Null, "Held confirmation must not arm selection.");
                canvas.Show("Camp", true); canvas.Focus(system);
                Assert.That(system.currentSelectedGameObject.name, Is.EqualTo("Pack up"));
                canvas.SetAvailability(false, true, true);
                var pack = system.currentSelectedGameObject.GetComponent<Button>();
                var move = new AxisEventData(system) { moveDir = MoveDirection.Left, moveVector = Vector2.left };
                pack.OnMove(move);
                Assert.That(system.currentSelectedGameObject.name, Is.EqualTo("Rest"));
                canvas.Focus(system);
                Assert.That(system.currentSelectedGameObject.name, Is.EqualTo("Rest"), "Refresh must preserve a usable selection.");
                canvas.SetAvailability(false, false, true); canvas.Focus(system);
                Assert.That(system.currentSelectedGameObject.name, Is.EqualTo("Pack up"));
                system.SetSelectedGameObject(null); canvas.Focus(system);
                Assert.That(system.currentSelectedGameObject.name, Is.EqualTo("Pack up"));
                canvas.SetAvailability(false, false, false); canvas.Focus(system);
                Assert.That(system.currentSelectedGameObject, Is.Null);
                canvas.SetAvailability(true, true, true); canvas.Focus(system);
                Assert.That(system.currentSelectedGameObject.name, Is.EqualTo("Sleep"));
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(events); }
        }
        [TestCase(TopDown3DInputMode.CampPlacement)]
        [TestCase(TopDown3DInputMode.CampActions)]
        [TestCase(TopDown3DInputMode.CampBusy)]
        public void CampModesKeepSystemAvailableAndBlockGameplayInventoryAndEscapeMenu(TopDown3DInputMode mode)
        {
            var actions = Object.Instantiate(AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Project/Settings/Input/InputSystem_Actions.inputactions"));
            var root = new GameObject("Camp input fixture"); root.SetActive(false);
            var router = root.AddComponent<TopDown3DInputRouter>(); router.Configure(actions); root.SetActive(true); router.Initialize();
            var previous = InputSystem.settings; var test = Object.Instantiate(previous);
            test.SetInternalFeatureFlag("RUN_PLAYER_UPDATES_IN_EDIT_MODE", true);
            test.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus; test.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            InputSystem.settings = test;
            var pad = InputSystem.AddDevice<Gamepad>(); var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                router.EnterCampMode(mode);
                Assert.That(actions.FindActionMap("Gameplay").enabled, Is.False);
                Assert.That(actions.FindActionMap("Camp").enabled, Is.True);
                Assert.That(actions.FindActionMap("System").enabled, Is.True);
                var inventory = 0; var menu = 0; router.InventoryToggleRequested += () => inventory++; router.MenuToggleRequested += () => menu++;
                InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.North)); InputSystem.Update();
                Assert.That(inventory, Is.Zero);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); InputSystem.Update();
                Assert.That(menu, Is.Zero); Assert.That(router.CampCancel, Is.True);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
                InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.Start)); InputSystem.Update();
                Assert.That(menu, Is.EqualTo(1));
                router.EnterGameplayMode(); Assert.That(actions.FindActionMap("Camp").enabled, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root); Object.DestroyImmediate(actions);
                InputSystem.RemoveDevice(pad); InputSystem.RemoveDevice(keyboard); InputSystem.settings = previous; Object.DestroyImmediate(test);
            }
        }
        [Test] public void AppendOnlyCampCommandPreservesOldPreferenceMeanings()
        {
            Assert.That((int)TopDown3DRadialCommand.AutoPack, Is.EqualTo(4)); Assert.That((int)TopDown3DRadialCommand.Camp, Is.EqualTo(5));
            var previous = new TopDown3DRadialPreferences(); previous.pages[0].slots[1] = TopDown3DRadialCommand.Empty;
            var restored = JsonUtility.FromJson<TopDown3DRadialPreferences>(JsonUtility.ToJson(previous));
            Assert.That(restored.IsValid(), Is.True); Assert.That(restored.pages[0].slots[1], Is.EqualTo(TopDown3DRadialCommand.Empty));
            Assert.That(new TopDown3DRadialPreferences().pages[0].slots[1], Is.EqualTo(TopDown3DRadialCommand.Camp));
        }
    }
}
