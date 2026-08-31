using System.Linq;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DInventoryUiTests
    {
        private const string InputPath = "Assets/_Project/Settings/Input/InputSystem_Actions.inputactions";

        [Test]
        public void InputModes_KeepSystemLiveAndSuppressGameplayDuringInventory()
        {
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            var owner = new GameObject("Input Mode Test");
            owner.SetActive(false);
            try
            {
                var router = owner.AddComponent<TopDown3DInputRouter>();
                router.Configure(actions);
                owner.SetActive(true);
                Assert.That(router.Initialize(), Is.True);
                var gameplay = actions.FindActionMap("Gameplay", true);
                var system = actions.FindActionMap("System", true);
                var ui = actions.FindActionMap("UI", true);

                Assert.That(router.Mode, Is.EqualTo(TopDown3DInputMode.Gameplay));
                Assert.That(gameplay.enabled, Is.True);
                Assert.That(system.enabled, Is.True);
                Assert.That(ui.enabled, Is.False);

                router.EnterInventoryMode();
                Assert.That(router.Mode, Is.EqualTo(TopDown3DInputMode.Inventory));
                Assert.That(gameplay.enabled, Is.False);
                Assert.That(system.enabled, Is.True);
                Assert.That(ui.enabled, Is.True);

                router.EnterInventoryMode();
                Assert.That(router.Mode, Is.EqualTo(TopDown3DInputMode.Inventory));
                router.EnterGameplayMode();
                Assert.That(gameplay.enabled, Is.True);
                Assert.That(system.enabled, Is.True);
                Assert.That(ui.enabled, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void InputAsset_HasCanonicalToggleAndPreservesLegacyOpenInventory()
        {
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            var toggle = actions.FindAction("System/ToggleInventory", true);
            var legacy = actions.FindAction("Gameplay/OpenInventory", true);

            Assert.That(toggle.id.ToString(), Is.EqualTo("3ee79022-8c1b-44bc-a15d-7a8f18e87d38"));
            Assert.That(toggle.bindings.Select(binding => binding.effectivePath),
                Is.EquivalentTo(new[] { "<Keyboard>/tab", "<Gamepad>/buttonNorth" }));
            Assert.That(legacy.id.ToString(), Is.EqualTo("c52a0d4c-3d7f-4e48-98e9-16be3c4f7c99"));
            Assert.That(legacy.bindings.Count, Is.EqualTo(2));
        }

        [Test]
        public void PromptFormatter_UsesCanonicalKeyboardAndGamepadBindingGroups()
        {
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            var interact = actions.FindAction("Gameplay/Interact", true);

            var keyboard = TopDown3DInputPromptUtility.ResolveBindingDisplayName(
                interact,
                "Keyboard&Mouse",
                null,
                "Fallback");
            var gamepad = TopDown3DInputPromptUtility.ResolveBindingDisplayName(
                interact,
                "Gamepad",
                null,
                "Fallback");

            Assert.That(keyboard, Is.EqualTo("E"));
            Assert.That(gamepad, Does.Contain("West"));
            Assert.That(gamepad, Does.Not.Contain("Hold"));
        }

        [Test]
        public void InventoryUi_IsSeparateNavigableAndMovesStacksThroughStateCommands()
        {
            var previousScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            var texture = new Texture2D(2, 2);
            var icon = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), Vector2.one * 0.5f);
            var item = ScriptableObject.CreateInstance<TopDown3DItemDefinition>();
            item.Configure("resource.ironstone_ore", "Ironstone Ore", "Dense ore.", "Resource", icon, 99, 0f);
            var catalog = ScriptableObject.CreateInstance<TopDown3DItemCatalog>();
            catalog.Configure(new[] { item });
            var player = new GameObject("Inventory UI Player");
            player.SetActive(false);
            TopDown3DInventoryUiController controller = null;
            TopDown3DGameHudCanvas gameHud = null;
            var initialTimeScale = Time.timeScale;
            try
            {
                var router = player.AddComponent<TopDown3DInputRouter>();
                router.Configure(actions);
                var inventory = player.AddComponent<TopDown3DPlayerInventory>();
                inventory.Configure(catalog, 16);
                player.AddComponent<Rigidbody>();
                player.AddComponent<CapsuleCollider>();
                player.AddComponent<TopDown3DPlayerMotor>();
                player.SetActive(true);
                controller = TopDown3DInventoryUiSceneInstaller.TryInstallForScene(scene);
                gameHud = TopDown3DGameHudCanvas.TryInstallForScene(scene);

                Assert.That(controller, Is.Not.Null);
                var inventoryCanvas = controller.GetComponent<TopDown3DInventoryCanvas>();
                Assert.That(inventoryCanvas.SlotViews.Count, Is.EqualTo(16));
                Assert.That(inventoryCanvas.ControlHints,
                    Does.Contain(router.GetBindingDisplayName("UI/Submit", "Select")));
                Assert.That(inventoryCanvas.ControlHints,
                    Does.Contain(router.GetBindingDisplayName("System/ToggleInventory", "Tab")));
                Assert.That(inventoryCanvas.ControlHints,
                    Does.Contain(router.GetBindingDisplayName("UI/Cancel", "Cancel")));
                Assert.That(controller.GetComponent<GraphicRaycaster>(), Is.Not.Null);
                Assert.That(gameHud.GetComponent<GraphicRaycaster>(), Is.Null);
                var eventSystems = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<EventSystem>(true))
                    .ToArray();
                Assert.That(eventSystems.Length, Is.EqualTo(1));
                Assert.That(eventSystems[0].GetComponents<InputSystemUIInputModule>().Length, Is.EqualTo(1));

                Assert.That(inventory.State.TryAdd(
                    new TopDown3DItemAmount(item.ItemId, 3)).Succeeded, Is.True);
                controller.Open();
                Assert.That(controller.IsOpen, Is.True);
                Assert.That(router.Mode, Is.EqualTo(TopDown3DInputMode.Inventory));
                Assert.That(Time.timeScale, Is.EqualTo(initialTimeScale));
                Assert.That(controller.EventSystem.currentSelectedGameObject, Is.Not.Null);
                controller.ActivateSlot(0);
                controller.ActivateSlot(1);
                Assert.That(inventory.State.Slots[0].IsEmpty, Is.True);
                Assert.That(inventory.State.Slots[1].ItemId, Is.EqualTo(item.ItemId));
                Assert.That(inventory.State.Slots[1].Quantity, Is.EqualTo(3));

                controller.Close();
                controller.Close();
                Assert.That(router.Mode, Is.EqualTo(TopDown3DInputMode.Gameplay));
                Assert.That(Time.timeScale, Is.EqualTo(initialTimeScale));
                controller.Open();
                controller.Close();
            }
            finally
            {
                Time.timeScale = initialTimeScale;
                if (controller != null)
                {
                    var eventSystem = Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include);
                    if (eventSystem != null)
                    {
                        Object.DestroyImmediate(eventSystem.gameObject);
                    }

                    Object.DestroyImmediate(controller.gameObject);
                }

                if (gameHud != null)
                {
                    Object.DestroyImmediate(gameHud.gameObject);
                }

                Object.DestroyImmediate(player);
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(item);
                Object.DestroyImmediate(icon);
                Object.DestroyImmediate(texture);
                EditorSceneManager.CloseScene(scene, true);
                if (previousScene.IsValid() && previousScene.isLoaded)
                {
                    SceneManager.SetActiveScene(previousScene);
                }
            }
        }
    }
}
