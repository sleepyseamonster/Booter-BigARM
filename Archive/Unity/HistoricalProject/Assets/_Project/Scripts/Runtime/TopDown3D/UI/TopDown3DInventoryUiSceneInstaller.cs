using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace BooterBigArm.TopDown3D
{
    public static class TopDown3DInventoryUiSceneInstaller
    {
        public static TopDown3DInventoryUiController TryInstallForScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return null;
            }

            var input = TopDown3DGameHudCanvas.FindInScene<TopDown3DInputRouter>(scene);
            var inventory = TopDown3DGameHudCanvas.FindInScene<TopDown3DPlayerInventory>(scene);
            var action = TopDown3DGameHudCanvas.FindInScene<TopDown3DPlayerActionController>(scene);
            if (input == null || inventory == null || input.InputActions == null)
            {
                return null;
            }

            input.Initialize();
            if (!EnsureSingleEventSystem(scene, input.InputActions, out var eventSystem))
            {
                return null;
            }

            var existing = TopDown3DGameHudCanvas.FindInScene<TopDown3DInventoryUiController>(scene);
            TopDown3DInventoryCanvas canvas;
            if (existing == null)
            {
                var canvasObject = new GameObject("Inventory Canvas", typeof(RectTransform));
                SceneManager.MoveGameObjectToScene(canvasObject, scene);
                canvas = canvasObject.AddComponent<TopDown3DInventoryCanvas>();
                existing = canvasObject.AddComponent<TopDown3DInventoryUiController>();
            }
            else
            {
                canvas = existing.GetComponent<TopDown3DInventoryCanvas>()
                    ?? existing.gameObject.AddComponent<TopDown3DInventoryCanvas>();
            }

            existing.Configure(input, inventory, action, canvas, eventSystem);
            return existing;
        }

        private static bool EnsureSingleEventSystem(
            Scene scene,
            InputActionAsset actions,
            out EventSystem resolvedEventSystem)
        {
            resolvedEventSystem = null;
            var systems = new List<EventSystem>();
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                systems.AddRange(roots[i].GetComponentsInChildren<EventSystem>(true));
            }

            if (systems.Count > 1)
            {
                Debug.LogError("TopDown3D inventory requires exactly one EventSystem; multiple authorities were found.");
                return false;
            }

            EventSystem eventSystem;
            if (systems.Count == 0)
            {
                var eventObject = new GameObject("EventSystem", typeof(EventSystem));
                SceneManager.MoveGameObjectToScene(eventObject, scene);
                eventSystem = eventObject.GetComponent<EventSystem>();
            }
            else
            {
                eventSystem = systems[0];
            }

            var module = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (module == null)
            {
                module = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }
            ConfigureInputModule(module, actions);
            resolvedEventSystem = eventSystem;
            return true;
        }

        private static void ConfigureInputModule(
            InputSystemUIInputModule module,
            InputActionAsset actions)
        {
            var ui = actions.FindActionMap("UI", true);
            module.move = InputActionReference.Create(ui.FindAction("Navigate", true));
            module.submit = InputActionReference.Create(ui.FindAction("Submit", true));
            module.cancel = InputActionReference.Create(ui.FindAction("Cancel", true));
            module.point = InputActionReference.Create(ui.FindAction("Point", true));
            module.leftClick = InputActionReference.Create(ui.FindAction("Click", true));
            module.rightClick = InputActionReference.Create(ui.FindAction("RightClick", true));
            module.middleClick = InputActionReference.Create(ui.FindAction("MiddleClick", true));
            module.scrollWheel = InputActionReference.Create(ui.FindAction("ScrollWheel", true));
            module.trackedDevicePosition = InputActionReference.Create(
                ui.FindAction("TrackedDevicePosition", true));
            module.trackedDeviceOrientation = InputActionReference.Create(
                ui.FindAction("TrackedDeviceOrientation", true));
            module.deselectOnBackgroundClick = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterForSceneLoads()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureCurrentSceneHasInventoryUi()
        {
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryInstallForScene(scene);
        }
    }
}
