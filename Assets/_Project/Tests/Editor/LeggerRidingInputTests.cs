using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace BooterBigArm.Tests
{
    public sealed class LeggerRidingInputTests
    {
        private GameObject inputObject, ownerObject;
        private InputActionAsset asset;
        private TopDown3DInputRouter router;
        private TopDown3DLeggerRidingController owner;
        private Gamepad pad;
        private InputSettings originalSettings, settings;
        [SetUp] public void SetUp()
        {
            originalSettings = InputSystem.settings; settings = Object.Instantiate(originalSettings);
            settings.SetInternalFeatureFlag("RUN_PLAYER_UPDATES_IN_EDIT_MODE", true); InputSystem.settings = settings;
            var source = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Project/Settings/Input/InputSystem_Actions.inputactions");
            asset = AnimationPlaygroundInputFactory.Create(source);
            inputObject = new GameObject("Riding input fixture"); inputObject.SetActive(false);
            router = inputObject.AddComponent<TopDown3DInputRouter>(); router.Configure(asset);
            ownerObject = new GameObject("Riding command recipient"); ownerObject.SetActive(false);
            owner = ownerObject.AddComponent<TopDown3DLeggerRidingController>();
            // This fixture isolates command arbitration; collision/attachment use real actors in other fixtures.
            typeof(TopDown3DLeggerRidingController).GetField("bound", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(owner, true);
            typeof(TopDown3DLeggerRidingController).GetField("input", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(owner, router);
            owner.State.BeginMount(); owner.State.CommitMount(owner.State.Epoch);
            router.BindRidingInput(owner); pad = InputSystem.AddDevice<Gamepad>(); inputObject.SetActive(true);
            Assert.That(router.Initialize(), Is.True);
            InputSystem.QueueStateEvent(pad, new GamepadState()); InputSystem.Update();
        }
        [TearDown] public void TearDown()
        {
            owner.State.Clear(); Object.DestroyImmediate(inputObject); Object.DestroyImmediate(ownerObject);
            InputSystem.RemoveDevice(pad); Object.DestroyImmediate(asset);
            InputSystem.settings = originalSettings; Object.DestroyImmediate(settings);
        }
        [Test] public void GamepadTapIsConsumedOnceAndCannotTriggerWorldInteraction()
        {
            int world = 0; router.InteractRequested += () => world++;
            InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.West)); InputSystem.Update();
            Assert.That(owner.State.Phase, Is.EqualTo(LeggerRidePhase.Dismounting)); Assert.That(world, Is.Zero);
            uint epoch = owner.State.Epoch;
            InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.West)); InputSystem.Update();
            Assert.That(owner.State.Epoch, Is.EqualTo(epoch)); Assert.That(world, Is.Zero);
        }
        [Test] public void ModalContextSuppressesDismountAndHeldMovementMustRearm()
        {
            InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = Vector2.up }); InputSystem.Update();
            router.EnterInventoryMode();
            InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.West)); InputSystem.Update();
            Assert.That(owner.State.Phase, Is.EqualTo(LeggerRidePhase.Mounted));
            InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = Vector2.up }); InputSystem.Update();
            router.EnterGameplayMode(); Assert.That(router.MoveValue, Is.EqualTo(Vector2.zero));
            InputSystem.QueueStateEvent(pad, new GamepadState()); InputSystem.Update();
            InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = Vector2.up }); InputSystem.Update();
            Assert.That(router.MoveValue.y, Is.GreaterThan(.5f));
        }
        [Test] public void JumpCannotRunAnOnFootLabActionWhileMounted()
        {
            int jumps = 0; router.AnimationJumpRequested += () => jumps++;
            InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.South)); InputSystem.Update();
            Assert.That(jumps, Is.Zero); Assert.That(owner.State.Phase, Is.EqualTo(LeggerRidePhase.Mounted));
        }
        [Test] public void HeldInteractAcrossModalReturnRequiresReleaseBeforeDismount()
        {
            router.EnterInventoryMode();
            InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.West)); InputSystem.Update();
            router.EnterGameplayMode(); InputSystem.Update();
            Assert.That(owner.State.Phase, Is.EqualTo(LeggerRidePhase.Mounted));
            InputSystem.QueueStateEvent(pad, new GamepadState()); InputSystem.Update();
            InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.West)); InputSystem.Update();
            Assert.That(owner.State.Phase, Is.EqualTo(LeggerRidePhase.Dismounting));
        }
        [Test] public void ReboundKeyboardHoldCannotAlsoDismountAndTapStillWorks()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>(); int cargo = 0;
            router.CargoOpenRequested += () => cargo++;
            try
            {
                foreach (var path in new[] { "Gameplay/Interact", "Gameplay/OpenCargo" })
                {
                    var action = asset.FindAction(path, true);
                    for (int i = 0; i < action.bindings.Count; i++)
                        if (action.bindings[i].path == "<Keyboard>/e") action.ApplyBindingOverride(i, "<Keyboard>/r");
                }
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.R)); InputSystem.Update();
                System.Threading.Thread.Sleep(420); InputSystem.Update();
                Assert.That(cargo, Is.EqualTo(1)); Assert.That(owner.State.Phase, Is.EqualTo(LeggerRidePhase.Mounted));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
                Assert.That(owner.State.Phase, Is.EqualTo(LeggerRidePhase.Mounted));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.R)); InputSystem.Update();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
                Assert.That(owner.State.Phase, Is.EqualTo(LeggerRidePhase.Dismounting)); Assert.That(cargo, Is.EqualTo(1));
            }
            finally { InputSystem.RemoveDevice(keyboard); }
        }
    }
}
