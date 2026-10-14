using System.Collections.Generic;
using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DInteractionFeedbackHudTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly List<GameObject> objects = new();
        private readonly Vector3 origin = new(23456f, 23456f, 23456f);
        private InputSettings originalSettings, manualSettings;
        private InputActionAsset actions;
        private TopDown3DInputRouter router;
        private TopDown3DInteractionFeedbackHud hud;
        private TopDown3DBigArmCargo cargo;

        [SetUp]
        public void SetUp()
        {
            originalSettings = InputSystem.settings;
            manualSettings = Object.Instantiate(originalSettings);
            manualSettings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            manualSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings = manualSettings;
            actions = Object.Instantiate(AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                "Assets/_Project/Settings/Input/InputSystem_Actions.inputactions"));
            var actor = Create("HUD test actor", origin); actor.SetActive(false);
            router = actor.AddComponent<TopDown3DInputRouter>(); router.Configure(actions);
            actor.SetActive(true);
            Assert.That(router.Initialize(), Is.True);
            var cargoOwner = Create("HUD test cargo", origin + Vector3.right * 2f);
            cargo = cargoOwner.AddComponent<TopDown3DBigArmCargo>();
            cargo.Configure(AssetDatabase.LoadAssetAtPath<TopDown3DItemCatalog>(
                    "Assets/_Project/Settings/Items/TopDown3DItemCatalog.asset"),
                AssetDatabase.LoadAssetAtPath<TopDown3DPackingSettings>(
                    "Assets/_Project/Settings/Items/TopDown3DPackingSettings.asset"));
            var controller = actor.AddComponent<TopDown3DPlayerActionController>();
            controller.Configure(router, null, null, null, null); controller.ConfigureCargo(cargo);
            var hudOwner = Create("HUD test feedback", Vector3.zero, typeof(RectTransform));
            hud = hudOwner.AddComponent<TopDown3DInteractionFeedbackHud>();
            Physics.SyncTransforms();
            hud.Configure(null, controller, router);
            Assert.That(hud.DisplayedText, Does.Contain("Open Legger cargo"));
        }

        [TearDown]
        public void TearDown()
        {
            // Views unsubscribe while their router/action owners are still alive.
            if (hud != null) Object.DestroyImmediate(hud.gameObject);
            for (var i = objects.Count - 1; i >= 0; i--)
                if (objects[i] != null) Object.DestroyImmediate(objects[i]);
            objects.Clear();
            if (actions != null) Object.DestroyImmediate(actions);
            InputSystem.settings = originalSettings;
            if (manualSettings != null) Object.DestroyImmediate(manualSettings);
            Physics.SyncTransforms();
        }

        [Test]
        public void UnchangedPrompt_DoesNotDirtyTextAgain()
        {
            var target = Target("Mine ironstone");
            Refresh(target);
            var text = hud.DisplayedText;
            var label = hud.GetComponent<Text>();
            var dirtyCalls = 0;
            label.RegisterDirtyVerticesCallback(() => dirtyCalls++);
            for (var i = 0; i < 10; i++) Refresh(target);
            Assert.That(hud.DisplayedText, Is.EqualTo(text));
            Assert.That(dirtyCalls, Is.Zero, "Unchanged presentation must not repeatedly dirty the text geometry.");
            target.Prompt = "Mine adjacent ironstone";
            Refresh(target);
            Assert.That(dirtyCalls, Is.GreaterThan(0), "Changed presentation proves the dirty observer is active.");
        }

        [Test]
        public void TargetPromptAndAvailability_StayFreshForSameTargetAndReplacement()
        {
            var target = Target("Mine ironstone");
            Refresh(target);
            Assert.That(hud.DisplayedText, Does.Contain("Mine ironstone"));
            target.Prompt = "Deposit depleted";
            Refresh(target);
            Assert.That(hud.DisplayedText, Does.Contain("Deposit depleted"));
            Assert.That(hud.DisplayedText, Does.Not.Contain("Mine ironstone"));
            target.Available = false;
            Refresh(target);
            Assert.That(hud.DisplayedText, Does.Not.Contain("Deposit depleted"));
            Assert.That(hud.DisplayedText, Does.Contain("Open Legger cargo"));
            target.Available = true;
            Refresh(target);
            Assert.That(hud.DisplayedText, Does.Contain("Deposit depleted"));
            var replacement = Target("Pick up canister");
            Refresh(replacement);
            Assert.That(hud.DisplayedText, Does.Contain("Pick up canister"));
            Assert.That(hud.DisplayedText, Does.Not.Contain("Deposit depleted"));
        }

        [Test]
        public void CargoPrompt_RechecksObstructionRangeAndDisabledAccess()
        {
            var wall = Create("HUD test wall", origin + new Vector3(1f, .5f, 0));
            wall.AddComponent<BoxCollider>().size = Vector3.one * .2f;
            Physics.SyncTransforms(); Refresh(null);
            Assert.That(hud.DisplayedText, Does.Not.Contain("Open Legger cargo"));
            Object.DestroyImmediate(wall);
            Physics.SyncTransforms(); Refresh(null);
            Assert.That(hud.DisplayedText, Does.Contain("Open Legger cargo"));
            cargo.transform.position = origin + Vector3.right * (cargo.AccessRange + 1f);
            Physics.SyncTransforms(); Refresh(null);
            Assert.That(hud.DisplayedText, Does.Not.Contain("Open Legger cargo"));
            cargo.transform.position = origin + Vector3.right * 2f;
            cargo.enabled = false;
            Physics.SyncTransforms(); Refresh(null);
            Assert.That(hud.DisplayedText, Does.Not.Contain("Open Legger cargo"));
            cargo.enabled = true;
            Refresh(null);
            Assert.That(hud.DisplayedText, Does.Contain("Open Legger cargo"));
        }

        [Test]
        public void ExpiredFeedback_IsRemovedByPeriodicUpdateWithoutLosingCargoPrompt()
        {
            hud.ShowFeedback("Deposit collected");
            Assert.That(hud.DisplayedText, Does.Contain("Deposit collected"));
            // Place the clock at its expiry boundary without sleeping or changing
            // global Unity time; exercise the ordinary periodic update consumer.
            SetClock("feedbackRemaining", 0f);
            SetClock("promptRefreshRemaining", 0f);
            typeof(TopDown3DInteractionFeedbackHud).GetMethod("Update", PrivateInstance).Invoke(hud, null);
            Assert.That(hud.DisplayedText, Does.Not.Contain("Deposit collected"));
            Assert.That(hud.DisplayedText, Does.Contain("Open Legger cargo"));
        }

        [Test]
        public void ModalReturn_RestoresFeedbackAndPromptWithoutTargetChange()
        {
            hud.ShowFeedback("Canister recovered");
            router.EnterInventoryMode();
            Assert.That(hud.DisplayedText, Is.Empty);
            Assert.That(hud.GetComponent<Text>().enabled, Is.False);
            router.EnterGameplayMode();
            Assert.That(hud.DisplayedText, Does.Contain("Canister recovered"));
            Assert.That(hud.DisplayedText, Does.Contain("Open Legger cargo"));
            Assert.That(hud.GetComponent<Text>().enabled, Is.True);
        }

        [Test]
        public void BindingRefresh_UpdatesCargoBindingWhileOtherwiseUnchanged()
        {
            var original = hud.DisplayedText;
            OverrideCargoBinding();
            var expected = router.GetBindingDisplayName("Gameplay/OpenCargo", "E");
            router.RefreshControlPrompts();
            Assert.That(hud.DisplayedText, Does.Contain("[Hold " + expected + "]"));
            Assert.That(hud.DisplayedText, Is.Not.EqualTo(original));
        }

        [Test]
        public void ReenabledHud_UsesBindingChangedWhileUnsubscribed()
        {
            var original = hud.DisplayedText;
            hud.enabled = false;
            OverrideCargoBinding();
            router.RefreshControlPrompts();
            hud.enabled = true;
            Refresh(null);
            var expected = router.GetBindingDisplayName("Gameplay/OpenCargo", "E");
            Assert.That(hud.DisplayedText, Does.Contain("[Hold " + expected + "]"));
            Assert.That(hud.DisplayedText, Is.Not.EqualTo(original));
        }

        [Test]
        public void DestroyedLabel_IsRebuiltWithCurrentUnchangedPrompt()
        {
            var target = Target("Mine ironstone");
            Refresh(target);
            var original = hud.DisplayedText;
            Object.DestroyImmediate(hud.GetComponent<Text>());
            Refresh(target);
            Assert.That(hud.GetComponents<Text>().Length, Is.EqualTo(1));
            Assert.That(hud.DisplayedText, Is.EqualTo(original));
            Assert.That(hud.GetComponent<Text>().enabled, Is.True);
            Assert.That(hud.GetComponent<Text>().raycastTarget, Is.False);
            Assert.That(hud.GetComponent<Text>().font, Is.Not.Null);
        }

        private void Refresh(ITopDown3DInteractable target) => typeof(TopDown3DInteractionFeedbackHud)
            .GetMethod("RefreshPrompt", PrivateInstance).Invoke(hud, new object[] { target });

        private void SetClock(string name, float value) => typeof(TopDown3DInteractionFeedbackHud)
            .GetField(name, PrivateInstance).SetValue(hud, value);

        private void OverrideCargoBinding()
        {
            var action = actions.FindAction("Gameplay/OpenCargo", true);
            for (var i = 0; i < action.bindings.Count; i++)
            {
                var binding = action.bindings[i];
                if (binding.groups?.Contains("Gamepad") == true) action.ApplyBindingOverride(i, "<Gamepad>/buttonEast");
                else if (binding.groups?.Contains("Keyboard&Mouse") == true) action.ApplyBindingOverride(i, "<Keyboard>/f");
            }
        }

        [Test] public void GamepadWorldPromptDoesNotAdvertiseTheRetiredCargoHold()
        {
            typeof(TopDown3DInputRouter).GetMethod("SetPromptDevice", PrivateInstance)
                .Invoke(router, new object[] {TopDown3DPromptDevice.Gamepad});
            Refresh(Target("Mine ironstone"));
            Assert.That(hud.DisplayedText, Does.Contain("Mine ironstone"));
            Assert.That(hud.DisplayedText, Does.Not.Contain("Hold"));
            Assert.That(hud.DisplayedText, Does.Not.Contain("Open Legger cargo"));
        }

        private MutableTarget Target(string prompt) => new(Create("HUD target", origin + Vector3.forward), prompt);

        private GameObject Create(string name, Vector3 position, params System.Type[] components)
        {
            var value = new GameObject(name, components); objects.Add(value); value.transform.position = position; return value;
        }

        private sealed class MutableTarget : ITopDown3DInteractable
        {
            private readonly GameObject owner;
            public MutableTarget(GameObject owner, string prompt) { this.owner = owner; Prompt = prompt; }
            public bool Available { get; set; } = true;
            public string Prompt { get; set; }
            public string StableId => owner != null ? owner.name : "destroyed";
            public Object UnityObject => owner;
            public Vector3 InteractionPoint => owner.transform.position;
            public float InteractionRange => 3f;
            public float ActionDuration => 1f;
            public TopDown3DItemAmount Reward => default;
            public bool IsAvailable => Available && owner != null;
            public bool TryConsume(out int remainingUses) { remainingUses = 0; return false; }
        }
    }
}
