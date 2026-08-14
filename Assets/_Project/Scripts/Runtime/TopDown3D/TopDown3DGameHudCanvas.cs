using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class TopDown3DGameHudCanvas : MonoBehaviour
    {
        public const int CanvasSortingOrder = 120;

        private static readonly Vector2 ReferenceResolution = new(1920f, 1080f);

        private Canvas hudCanvas;
        private CanvasScaler canvasScaler;

        public Canvas Canvas => hudCanvas;
        public CanvasScaler Scaler => canvasScaler;
        public RectTransform RectTransform => (RectTransform)transform;

        public void Initialize()
        {
            hudCanvas = GetComponent<Canvas>();
            if (hudCanvas == null)
            {
                hudCanvas = gameObject.AddComponent<Canvas>();
            }

            hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            hudCanvas.sortingOrder = CanvasSortingOrder;
            hudCanvas.pixelPerfect = false;

            canvasScaler = GetComponent<CanvasScaler>();
            if (canvasScaler == null)
            {
                canvasScaler = gameObject.AddComponent<CanvasScaler>();
            }

            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = ReferenceResolution;
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;

            var raycaster = GetComponent<GraphicRaycaster>();
            if (raycaster != null)
            {
                DestroyRuntimeObject(raycaster);
            }
        }

        public static TopDown3DGameHudCanvas TryInstallForScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded || !ContainsGameplayMarker(scene))
            {
                return null;
            }

            var existing = FindInScene<TopDown3DGameHudCanvas>(scene);
            if (existing != null)
            {
                existing.Initialize();
                return existing;
            }

            var canvasObject = new GameObject("Game HUD Canvas", typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(canvasObject, scene);
            var gameHud = canvasObject.AddComponent<TopDown3DGameHudCanvas>();
            gameHud.Initialize();
            return gameHud;
        }

        public static T FindInScene<T>(Scene scene) where T : Component
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return null;
            }

            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var component = roots[i].GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();
        }

        private static bool ContainsGameplayMarker(Scene scene)
        {
            return FindInScene<TopDown3DInputRouter>(scene) != null
                || FindInScene<TopDown3DPlayerMotor>(scene) != null;
        }

        private static void DestroyRuntimeObject(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }

    internal static class TopDown3DGameHudBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterForSceneLoads()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureCurrentSceneHasHud()
        {
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (TopDown3DGameHudCanvas.TryInstallForScene(scene) == null)
            {
                return;
            }

            TopDown3DActionDpadHud.TryInstallForScene(scene);
            TopDown3DSurvivalHud.TryInstallForScene(scene);
        }
    }
}
