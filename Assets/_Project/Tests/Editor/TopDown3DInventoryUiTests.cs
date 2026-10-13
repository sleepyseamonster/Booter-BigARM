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
        public void SlotView_ReconnectsClonedAuthoredChildrenAndRetainsNavigation()
        {
            var authored = new GameObject("Authored slot", typeof(RectTransform), typeof(Image), typeof(Button));
            GameObject clone = null;
            try
            {
                var authoredIcon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                authoredIcon.transform.SetParent(authored.transform, false);
                var authoredQuantity = new GameObject("Quantity", typeof(RectTransform), typeof(Text));
                authoredQuantity.transform.SetParent(authored.transform, false);
                authored.AddComponent<TopDown3DInventorySlotView>();
                var navigation = authored.GetComponent<Button>().navigation;
                navigation.mode = Navigation.Mode.Explicit;
                authored.GetComponent<Button>().navigation = navigation;
                clone = Object.Instantiate(authored);
                var icon = clone.transform.Find("Icon").GetComponent<Image>();
                var quantity = clone.transform.Find("Quantity").GetComponent<Text>();
                var view = clone.GetComponent<TopDown3DInventorySlotView>();
                var activated = -1;
                view.Initialize(7, index => activated = index, null);
                var definition = SlotTestDefinition();
                var slot = new TopDown3DInventorySlot(); slot.Set(definition.ItemId, 3);
                view.Refresh(slot, definition, true);
                view.Refresh(slot, definition, true);

                Assert.That(clone.transform.childCount, Is.EqualTo(2));
                Assert.That(clone.transform.Find("Icon").GetComponent<Image>(), Is.SameAs(icon));
                Assert.That(clone.transform.Find("Quantity").GetComponent<Text>(), Is.SameAs(quantity));
                Assert.That(icon.enabled, Is.True);
                Assert.That(icon.sprite, Is.SameAs(definition.Icon));
                Assert.That(icon.raycastTarget, Is.False);
                Assert.That(quantity.text, Is.EqualTo("3"));
                Assert.That(quantity.raycastTarget, Is.False);
                Assert.That(view.Button.targetGraphic, Is.SameAs(clone.GetComponent<Image>()));
                Assert.That(view.Button.navigation.mode, Is.EqualTo(Navigation.Mode.Explicit));
                Assert.That((Color32)clone.GetComponent<Image>().color, Is.EqualTo(new Color32(146, 111, 53, 255)));
                view.Button.onClick.Invoke();
                Assert.That(activated, Is.EqualTo(7));
            }
            finally
            {
                if (clone != null) Object.DestroyImmediate(clone);
                Object.DestroyImmediate(authored);
            }
        }

        [TestCase("Icon", false)]
        [TestCase("Quantity", false)]
        [TestCase("Icon", true)]
        [TestCase("Quantity", true)]
        public void SlotView_RepairsDestroyedChildAndUsesReplacementCallbacks(string childName, bool componentOnly)
        {
            var owner = new GameObject("Repair slot", typeof(RectTransform), typeof(Image), typeof(Button));
            try
            {
                var view = owner.AddComponent<TopDown3DInventorySlotView>();
                var oldCalls = 0; var newCalls = 0; var externalCalls = 0; var selected = -1;
                view.Initialize(1, _ => oldCalls++, _ => oldCalls++);
                view.Button.onClick.AddListener(() => externalCalls++);
                view.Initialize(4, index => { Assert.That(index, Is.EqualTo(4)); newCalls++; }, index => selected = index);
                view.Initialize(4, index => { Assert.That(index, Is.EqualTo(4)); newCalls++; }, index => selected = index);
                var destroyedChild = owner.transform.Find(childName);
                var siblingName = childName == "Icon" ? "Quantity" : "Icon";
                var sibling = owner.transform.Find(siblingName);
                if (componentOnly)
                    Object.DestroyImmediate(childName == "Icon"
                        ? (Object)destroyedChild.GetComponent<Image>() : destroyedChild.GetComponent<Text>());
                else Object.DestroyImmediate(destroyedChild.gameObject);
                var definition = SlotTestDefinition();
                var slot = new TopDown3DInventorySlot(); slot.Set(definition.ItemId, 3);
                view.Refresh(slot, definition, false);
                view.Refresh(slot, definition, false);

                Assert.That(owner.transform.childCount, Is.EqualTo(2));
                Assert.That(owner.transform.Find(siblingName), Is.SameAs(sibling));
                if (componentOnly) Assert.That(owner.transform.Find(childName), Is.SameAs(destroyedChild));
                Assert.That(owner.transform.Find("Icon").GetComponent<Image>().enabled, Is.True);
                Assert.That(owner.transform.Find("Icon").GetComponent<Image>().sprite, Is.SameAs(definition.Icon));
                Assert.That(owner.transform.Find("Quantity").GetComponent<Text>().text, Is.EqualTo("3"));
                view.Button.onClick.Invoke();
                view.OnSelect(null);
                Assert.That(oldCalls, Is.Zero);
                Assert.That(newCalls, Is.EqualTo(1), "Repeated initialization/repair must not duplicate listeners.");
                Assert.That(externalCalls, Is.EqualTo(1), "Other button listeners remain attached.");
                Assert.That(selected, Is.EqualTo(4));
            }
            finally { Object.DestroyImmediate(owner); }
        }

        private static TopDown3DItemDefinition SlotTestDefinition()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<TopDown3DItemCatalog>(
                "Assets/_Project/Settings/Items/TopDown3DItemCatalog.asset");
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.TryGetDefinition("resource.ironstone_ore", out var definition), Is.True);
            return definition;
        }

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
            // The isolated batch runner supplies an Untitled scene; additive editor scenes require a saved base.
            if (Application.isBatchMode && string.IsNullOrEmpty(previousScene.path))
                previousScene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/TopDown3D/GameplaySetup.unity", OpenSceneMode.Single);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            var texture = new Texture2D(2, 2);
            var icon = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), Vector2.one * 0.5f);
            var item = ScriptableObject.CreateInstance<TopDown3DItemDefinition>();
            item.Configure("resource.ironstone_ore", "Ironstone Ore", "Dense ore.", "Test", icon, 99, 0f);
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
