using System.Linq;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DInventoryLoopUiTests
    {
        private GameObject actor, cargoObject, uiObject, eventObject;
        private TopDown3DPlayerInventory kit;
        private TopDown3DBigArmCargo cargo;
        private TopDown3DInputRouter router;
        private TopDown3DInventoryUiController ui;
        private TopDown3DInventoryCanvas canvas;
        private InputActionAsset actions;
        private TopDown3DItemCatalog catalog;
        private InputSettings originalSettings, manualSettings;

        [SetUp]
        public void SetUp()
        {
            originalSettings = InputSystem.settings;
            manualSettings = Object.Instantiate(originalSettings);
            manualSettings.SetInternalFeatureFlag("RUN_PLAYER_UPDATES_IN_EDIT_MODE", true);
            manualSettings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            manualSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings = manualSettings;
            catalog = AssetDatabase.LoadAssetAtPath<TopDown3DItemCatalog>("Assets/_Project/Settings/Items/TopDown3DItemCatalog.asset");
            actions = Object.Instantiate(AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Project/Settings/Input/InputSystem_Actions.inputactions"));
            cargoObject = new GameObject("Inventory test Legger");
            cargoObject.transform.position = new Vector3(1.5f, 0, 0);
            cargo = cargoObject.AddComponent<TopDown3DBigArmCargo>();
            cargo.Configure(catalog, AssetDatabase.LoadAssetAtPath<TopDown3DPackingSettings>("Assets/_Project/Settings/Items/TopDown3DPackingSettings.asset"));
            actor = new GameObject("Inventory test Booter"); actor.SetActive(false);
            router = actor.AddComponent<TopDown3DInputRouter>(); router.Configure(actions);
            kit = actor.AddComponent<TopDown3DPlayerInventory>(); kit.Configure(catalog, 8);
            actor.SetActive(true);
            router.Initialize();
            uiObject = new GameObject("Inventory test canvas", typeof(RectTransform));
            canvas = uiObject.AddComponent<TopDown3DInventoryCanvas>();
            ui = uiObject.AddComponent<TopDown3DInventoryUiController>();
            eventObject = new GameObject("Inventory test EventSystem", typeof(EventSystem));
            ui.Configure(router, kit, null, canvas, eventObject.GetComponent<EventSystem>());
            Physics.SyncTransforms();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(uiObject); Object.DestroyImmediate(actor);
            Object.DestroyImmediate(cargoObject); Object.DestroyImmediate(eventObject);
            Object.DestroyImmediate(actions);
            InputSystem.settings = originalSettings;
            Object.DestroyImmediate(manualSettings);
        }

        [Test]
        public void PanelsShowRealOwnersAndHardCapacityWithoutBalanceScore()
        {
            ui.Open();
            var texts = uiObject.GetComponentsInChildren<Text>().Select(t => t.text).ToArray();
            Assert.That(texts, Does.Contain("BOOTER KIT"));
            Assert.That(texts, Does.Contain("LEGGER CARGO"));
            Assert.That(string.Join(" ", texts), Does.Contain("92.0"));
            Assert.That(string.Join(" ", texts).ToLowerInvariant(), Does.Not.Contain("balance"));
            Assert.That(canvas.SlotViews.Count, Is.EqualTo(8));
            Assert.That(canvas.CargoSlotViews.Count, Is.EqualTo(12));
            Assert.That(canvas.CargoSlotViews[0].Button.navigation.mode, Is.EqualTo(Navigation.Mode.Explicit));
        }

        [TestCase(true)]
        public void TapInteractsButHoldOpensNearbyCargoWithoutTransfer(bool keyboardInput)
        {
            var device = keyboardInput ? (InputDevice)InputSystem.AddDevice<Keyboard>() : InputSystem.AddDevice<Gamepad>();
            var controller = actor.AddComponent<TopDown3DPlayerActionController>();
            try
            {
                controller.Configure(router, null, kit, null, null);
                controller.ConfigureCargo(cargo);
                ui.Configure(router, kit, controller, canvas, ui.EventSystem);
                cargo.State.TryAdd(new TopDown3DItemAmount("tool.micro_dust_harvester", 1));
                var taps = 0;
                router.InteractRequested += () => taps++;
                void Send(bool pressed)
                {
                    if (device is Keyboard keyboard) InputSystem.QueueStateEvent(keyboard,
                        pressed ? new KeyboardState(Key.E) : new KeyboardState());
                    else InputSystem.QueueStateEvent((Gamepad)device,
                        pressed ? new GamepadState().WithButton(GamepadButton.West) : new GamepadState());
                    InputSystem.Update();
                }
                Send(false); Send(true);
                Assert.That(taps, Is.Zero);
                Send(false);
                Assert.That(taps, Is.EqualTo(1));
                Assert.That(ui.IsOpen, Is.False);
                Send(true);
                System.Threading.Thread.Sleep(120);
                InputSystem.Update();
                Assert.That(router.IsCargoHoldActive, Is.True);
                Assert.That(router.CargoHoldProgress, Is.InRange(.1f, .9f));
                Assert.That(ui.IsOpen, Is.False);
                System.Threading.Thread.Sleep(280);
                InputSystem.Update();
                Assert.That(ui.IsOpen, Is.True);
                Assert.That(taps, Is.EqualTo(1));
                Assert.That(ui.EventSystem.currentSelectedGameObject, Is.SameAs(canvas.CargoSlotViews[0].gameObject));
                Assert.That(kit.State.OccupiedSlotCount, Is.Zero);
                Assert.That(cargo.State.Slots[0].Quantity, Is.EqualTo(1));
                Send(false);
                Assert.That(taps, Is.EqualTo(1));
                if (device is Gamepad)
                {
                    Send(true);
                    Assert.That(kit.State.Slots[0].Quantity, Is.EqualTo(1), "A fresh West press may transfer after release.");
                }
            }
            finally { InputSystem.RemoveDevice(device); Object.DestroyImmediate(controller); }
        }

        [TestCase(20f, false)]
        [TestCase(1.5f, true)]
        public void CargoHoldRejectsDistanceAndBlockedAccess(float distance, bool blocked)
        {
            var controller = actor.AddComponent<TopDown3DPlayerActionController>();
            var blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                controller.Configure(router, null, kit, null, null); controller.ConfigureCargo(cargo);
                cargoObject.transform.position = Vector3.right * distance;
                blocker.transform.position = new Vector3(.75f, .5f, 0f); blocker.SetActive(blocked);
                Physics.SyncTransforms();
                Assert.That(controller.CanOpenCargo, Is.False);
            }
            finally { Object.DestroyImmediate(blocker); Object.DestroyImmediate(controller); }
        }

        [Test]
        public void HeldKeyboardNavigationMustReleaseBeforeGameplayMovementResumes()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                ui.Open();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W)); InputSystem.Update();
                ui.Close(); InputSystem.Update();
                Assert.That(router.MoveValue, Is.EqualTo(Vector2.zero));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W)); InputSystem.Update();
                Assert.That(router.MoveValue.y, Is.GreaterThan(.9f));
            }
            finally { InputSystem.RemoveDevice(keyboard); }
        }

        [Test]
        public void InventoryShouldersSelectPredictablePanelsWithoutTogglingBack()
        {
            var pad = InputSystem.AddDevice<Gamepad>();
            try
            {
                ui.Open();
                void Press(GamepadButton button)
                {
                    InputSystem.QueueStateEvent(pad, new GamepadState()); InputSystem.Update();
                    InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(button)); InputSystem.Update();
                }
                Press(GamepadButton.RightShoulder);
                Assert.That(ui.EventSystem.currentSelectedGameObject, Is.SameAs(canvas.CargoSlotViews[0].gameObject));
                Press(GamepadButton.RightShoulder);
                Assert.That(ui.EventSystem.currentSelectedGameObject, Is.SameAs(canvas.CargoSlotViews[0].gameObject));
                Press(GamepadButton.LeftShoulder);
                Assert.That(ui.EventSystem.currentSelectedGameObject, Is.SameAs(canvas.SlotViews[0].gameObject));
                Press(GamepadButton.LeftShoulder);
                Assert.That(ui.EventSystem.currentSelectedGameObject, Is.SameAs(canvas.SlotViews[0].gameObject));
            }
            finally { InputSystem.RemoveDevice(pad); }
        }

        [Test]
        public void CancelWinsAgainstTapAndDeploymentWithinTheSameInputUpdate()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                var taps = 0; var cancels = 0; var deploys = 0;
                router.InteractRequested += () => taps++;
                router.ItemActionCancelRequested += () => cancels++;
                router.DeployCanisterRequested += () => deploys++;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E)); InputSystem.Update();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Backspace, Key.G)); InputSystem.Update();
                Assert.That(taps, Is.Zero); Assert.That(deploys, Is.Zero); Assert.That(cancels, Is.EqualTo(1));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E)); InputSystem.Update();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
                Assert.That(taps, Is.EqualTo(1), "A fresh gesture is accepted; cancellation is not buffered.");
            }
            finally { InputSystem.RemoveDevice(keyboard); }
        }

        [Test]
        public void ModalBoundaryDiscardsTapAndPickupRequests()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                var taps = 0; var pickups = 0;
                router.InteractRequested += () => taps++;
                router.PickupCanisterRequested += () => pickups++;
                router.MenuToggleRequested += router.EnterSystemMenuMode;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E)); InputSystem.Update();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.C, Key.Escape)); InputSystem.Update();
                Assert.That(router.Mode, Is.EqualTo(TopDown3DInputMode.SystemMenu));
                Assert.That(taps, Is.Zero); Assert.That(pickups, Is.Zero);
                router.EnterGameplayMode();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
                Assert.That(taps, Is.Zero); Assert.That(pickups, Is.Zero);
            }
            finally { InputSystem.RemoveDevice(keyboard); }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TapUsesPressedTargetAndNeverRedirectsToAnotherObject(bool originalLeavesRange)
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var presentation = new GameObject("Inactive presentation"); presentation.SetActive(false);
            var first = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var second = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var controller = actor.AddComponent<TopDown3DPlayerActionController>();
            try
            {
                var motor = actor.AddComponent<TopDown3DPlayerMotor>();
                actor.GetComponent<Rigidbody>().isKinematic = true;
                var interaction = actor.AddComponent<TopDown3DInteractionController>(); interaction.Configure(motor);
                var animation = presentation.AddComponent<TopDown3DPlayerAnimationDriver>();
                controller.Configure(router, interaction, kit, motor, animation);
                first.transform.localScale = second.transform.localScale = Vector3.one * .2f;
                first.transform.position = new Vector3(-.35f, .8f, 1.2f);
                second.transform.position = new Vector3(.35f, .8f, 1.7f);
                var firstCache = first.AddComponent<TopDown3DResourceCache>(); firstCache.Configure(catalog, 8, "pressed-cache");
                var secondCache = second.AddComponent<TopDown3DResourceCache>(); secondCache.Configure(catalog, 8, "other-cache");
                TopDown3DResourceCache opened = null;
                controller.CacheAccessRequested += cache => opened = cache;
                Physics.SyncTransforms();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E)); InputSystem.Update();
                first.transform.position = new Vector3(-.35f, .8f, originalLeavesRange ? 8f : 1.7f);
                second.transform.position = new Vector3(.35f, .8f, 1f);
                Physics.SyncTransforms(); interaction.RefreshTarget();
                Assert.That(interaction.CurrentTarget, Is.SameAs(secondCache));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
                Assert.That(opened, originalLeavesRange ? Is.Null : Is.SameAs(firstCache));
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                Object.DestroyImmediate(controller); Object.DestroyImmediate(first); Object.DestroyImmediate(second);
                Object.DestroyImmediate(presentation);
            }
        }

        [Test]
        public void FeedbackKeepsCargoPromptVisibleAndHidesImmediatelyInModalUi()
        {
            var controller = actor.AddComponent<TopDown3DPlayerActionController>();
            var hudObject = new GameObject("Feedback test", typeof(RectTransform));
            try
            {
                controller.Configure(router, null, kit, null, null); controller.ConfigureCargo(cargo);
                var hud = hudObject.AddComponent<TopDown3DInteractionFeedbackHud>();
                hud.Configure(null, controller, router);
                hud.ShowFeedback("Deposit collected");
                Assert.That(hud.DisplayedText, Does.Contain("Deposit collected"));
                Assert.That(hud.DisplayedText, Does.Contain("Open Legger cargo"));
                router.EnterInventoryMode();
                Assert.That(hud.DisplayedText, Is.Empty);
                router.EnterGameplayMode();
                Assert.That(hud.DisplayedText, Does.Contain("Open Legger cargo"));
            }
            finally { Object.DestroyImmediate(hudObject); Object.DestroyImmediate(controller); }
        }

        [Test]
        public void CustomizedHoldTimingKeepsTapAndHoldExclusiveAtTheSavedThreshold()
        {
            var pad = InputSystem.AddDevice<Keyboard>();
            try
            {
                TopDown3DControlPreferencesService.Apply(actions, new TopDown3DControlPreferences { cargoHoldSeconds = .65f });
                var taps = 0; var holds = 0;
                router.InteractRequested += () => taps++; router.CargoOpenRequested += () => holds++;
                InputSystem.QueueStateEvent(pad, new KeyboardState(Key.E)); InputSystem.Update();
                System.Threading.Thread.Sleep(400); InputSystem.Update();
                Assert.That(holds, Is.Zero, "The shipped .35-second threshold must be replaced.");
                InputSystem.QueueStateEvent(pad, new KeyboardState()); InputSystem.Update();
                Assert.That(taps, Is.EqualTo(1));
                InputSystem.QueueStateEvent(pad, new KeyboardState(Key.E)); InputSystem.Update();
                System.Threading.Thread.Sleep(700); InputSystem.Update();
                Assert.That(holds, Is.EqualTo(1)); Assert.That(taps, Is.EqualTo(1));
                InputSystem.QueueStateEvent(pad, new KeyboardState()); InputSystem.Update();
                Assert.That(taps, Is.EqualTo(1), "The completed hold cannot also tap.");
            }
            finally { InputSystem.RemoveDevice(pad); }
        }

        [Test] public void HeldSquareInteractsOnceWithoutOpeningCargoAndTriangleStillOpensInventory()
        {
            var pad = InputSystem.AddDevice<Gamepad>();
            var controller = actor.AddComponent<TopDown3DPlayerActionController>();
            try
            {
                controller.Configure(router, null, kit, null, null); controller.ConfigureCargo(cargo);
                ui.Configure(router, kit, controller, canvas, ui.EventSystem);
                // A saved pairing and hold duration must not revive the retired gamepad shortcut.
                TopDown3DControlPreferencesService.Apply(actions, new TopDown3DControlPreferences {cargoHoldSeconds = .65f});
                var interactions = 0; var cargoRequests = 0;
                router.InteractRequested += () => interactions++; router.CargoOpenRequested += () => cargoRequests++;
                InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.West)); InputSystem.Update();
                Assert.That(interactions, Is.EqualTo(1));
                System.Threading.Thread.Sleep(700); InputSystem.Update();
                Assert.That(router.IsCargoHoldActive, Is.False);
                Assert.That(cargoRequests, Is.Zero); Assert.That(ui.IsOpen, Is.False);
                InputSystem.QueueStateEvent(pad, new GamepadState()); InputSystem.Update();
                Assert.That(interactions, Is.EqualTo(1), "Release must not trigger a second world interaction.");
                InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.North)); InputSystem.Update();
                Assert.That(ui.IsOpen, Is.True); Assert.That(cargoRequests, Is.Zero);
            }
            finally { InputSystem.RemoveDevice(pad); Object.DestroyImmediate(controller); }
        }

        [Test] public void ExistingAuthoredGridsReconnectWithoutCreatingDuplicateSlots()
        {
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            ((System.Collections.Generic.List<TopDown3DInventorySlotView>)typeof(TopDown3DInventoryCanvas).GetField("slotViews", flags).GetValue(canvas)).Clear();
            ((System.Collections.Generic.List<TopDown3DInventorySlotView>)typeof(TopDown3DInventoryCanvas).GetField("cargoSlotViews", flags).GetValue(canvas)).Clear();
            ui.Configure(router, kit, null, canvas, ui.EventSystem);
            Assert.That(canvas.SlotViews.Count, Is.EqualTo(8)); Assert.That(canvas.CargoSlotViews.Count, Is.EqualTo(12));
            Assert.That(uiObject.GetComponentsInChildren<TopDown3DInventorySlotView>(true).Length, Is.EqualTo(20));
        }

        [Test]
        public void GamepadBackCancelsSelectedStackBeforeClosing()
        {
            var pad = InputSystem.AddDevice<Gamepad>();
            try
            {
                kit.State.TryAdd(new TopDown3DItemAmount("tool.micro_dust_harvester", 1));
                ui.Open(); ui.ActivateSlot(0);
                InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.East)); InputSystem.Update();
                Assert.That(ui.IsOpen, Is.True); Assert.That(ui.SelectedSource, Is.EqualTo(-1));
                InputSystem.QueueStateEvent(pad, new GamepadState()); InputSystem.Update();
                InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.East)); InputSystem.Update();
                Assert.That(ui.IsOpen, Is.False);
            }
            finally { InputSystem.RemoveDevice(pad); }
        }

        [Test] public void GameplayBackHasOneContextActionAndNoCompetingCancelBinding()
        {
            var pad = InputSystem.AddDevice<Gamepad>();
            try
            {
                var pickups = 0; var cancels = 0;
                router.PickupCanisterRequested += () => pickups++;
                router.ItemActionCancelRequested += () => cancels++;
                InputSystem.QueueStateEvent(pad, new GamepadState()); InputSystem.Update();
                InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.East)); InputSystem.Update();
                Assert.That(pickups, Is.EqualTo(1)); Assert.That(cancels, Is.Zero);
            }
            finally { InputSystem.RemoveDevice(pad); }
        }

        [Test]
        public void QuickTransferAndPanelSwitchKeepTheCorrectOwnerAndNeverMoveOreIntoKit()
        {
            kit.State.TryAdd(new TopDown3DItemAmount("tool.micro_dust_harvester", 1));
            ui.Open(); ui.SelectSlot(0); ui.QuickTransfer();
            Assert.That(kit.State.OccupiedSlotCount, Is.Zero);
            Assert.That(cargo.State.Slots[0].ItemId, Is.EqualTo("tool.micro_dust_harvester"));
            cargo.State.TryAdd(new TopDown3DItemAmount("resource.ironstone_ore", 1));
            ui.SwitchPanel(); ui.EventSystem.SetSelectedGameObject(canvas.CargoSlotViews[1].gameObject); ui.SelectCargoSlot(1); ui.QuickTransfer();
            Assert.That(kit.State.OccupiedSlotCount, Is.Zero);
            Assert.That(cargo.State.Slots[1].Quantity, Is.EqualTo(1));
            ui.Close(); ui.Open();
            Assert.That(ui.EventSystem.currentSelectedGameObject, Is.SameAs(canvas.CargoSlotViews[1].gameObject));
        }

        [Test]
        public void CargoGoingOutOfReachIsInspectableAndCannotMutate()
        {
            cargo.State.TryAdd(new TopDown3DItemAmount("resource.ironstone_ore", 1));
            ui.Open(true); ui.ActivateCargoSlot(0);
            cargoObject.transform.position = Vector3.right * 20f; Physics.SyncTransforms();
            ui.QuickTransfer();
            Assert.That(cargo.State.Slots[0].Quantity, Is.EqualTo(1));
            Assert.That(kit.State.OccupiedSlotCount, Is.Zero);
            Assert.That(canvas.CargoSlotViews[0].Button.interactable, Is.True);
        }

        [Test]
        public void CacheViewUnloadsLocallyAndClosingRestoresThePersonalKit()
        {
            var owner = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                owner.transform.position = new Vector3(0, 0, 2);
                var cache = owner.AddComponent<TopDown3DResourceCache>();
                cache.Configure(catalog, 24, "test-cache");
                Physics.SyncTransforms();
                cargo.State.TryAdd(new TopDown3DItemAmount("resource.ironstone_ore", 3));
                ui.OpenCache(cache); ui.UnloadCargo();
                Assert.That(cache.State.Slots[0].Quantity, Is.EqualTo(3));
                Assert.That(cargo.State.OccupiedSlotCount, Is.Zero);
                ui.Close(); ui.Open();
                Assert.That(uiObject.GetComponentsInChildren<Text>().Select(t => t.text), Does.Contain("BOOTER KIT"));
                Assert.That(canvas.SlotViews.Count(v => v.gameObject.activeSelf), Is.EqualTo(8));
            }
            finally { Object.DestroyImmediate(owner); }
        }
    }
}
