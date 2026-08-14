using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BooterBigArm.TopDown3D
{
    public enum TopDown3DDpadDirection
    {
        Up,
        Left,
        Right,
        Down
    }

    public enum TopDown3DDpadIcon
    {
        DirectionArrow,
        Vial
    }

    [DisallowMultipleComponent]
    public sealed class TopDown3DActionDpadHud : MonoBehaviour
    {
        public const float ReferenceSize = 120f;
        public const float ReferenceMargin = 28f;
        public const int CanvasSortingOrder = 120;

        private static readonly Vector2 ReferenceResolution = new(1920f, 1080f);

        private Canvas hudCanvas;
        private CanvasScaler canvasScaler;
        private RectTransform safeAreaRoot;
        private TopDown3DActionDpadGraphic dpadGraphic;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;

        public TopDown3DActionDpadGraphic Graphic => dpadGraphic;

        public void Initialize()
        {
            EnsureCanvas();
            EnsureVisualTree();
            RefreshSafeArea(force: true);
        }

        public TopDown3DDpadIcon GetIcon(TopDown3DDpadDirection direction)
        {
            return direction == TopDown3DDpadDirection.Down
                ? TopDown3DDpadIcon.Vial
                : TopDown3DDpadIcon.DirectionArrow;
        }

        public void SetDirectionPressed(TopDown3DDpadDirection direction, bool pressed)
        {
            Initialize();
            dpadGraphic.SetDirectionPressed(direction, pressed);
        }

        public bool IsDirectionPressed(TopDown3DDpadDirection direction)
        {
            return dpadGraphic != null && dpadGraphic.IsDirectionPressed(direction);
        }

        public static TopDown3DActionDpadHud TryInstallForScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded || FindInScene<TopDown3DInputRouter>(scene) == null)
            {
                return null;
            }

            var existing = FindInScene<TopDown3DActionDpadHud>(scene);
            if (existing != null)
            {
                existing.Initialize();
                return existing;
            }

            var hudObject = new GameObject("Action D-Pad HUD");
            SceneManager.MoveGameObjectToScene(hudObject, scene);
            var hud = hudObject.AddComponent<TopDown3DActionDpadHud>();
            hud.Initialize();
            return hud;
        }

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();
        }

        private void Update()
        {
            RefreshSafeArea(force: false);
        }

        private void EnsureCanvas()
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

        private void EnsureVisualTree()
        {
            if (safeAreaRoot == null)
            {
                var existingSafeArea = transform.Find("Safe Area");
                if (existingSafeArea != null)
                {
                    safeAreaRoot = existingSafeArea as RectTransform;
                }
            }

            if (safeAreaRoot == null)
            {
                var safeAreaObject = new GameObject("Safe Area", typeof(RectTransform));
                safeAreaRoot = safeAreaObject.GetComponent<RectTransform>();
                safeAreaRoot.SetParent(transform, false);
            }

            safeAreaRoot.offsetMin = Vector2.zero;
            safeAreaRoot.offsetMax = Vector2.zero;

            if (dpadGraphic == null)
            {
                dpadGraphic = safeAreaRoot.GetComponentInChildren<TopDown3DActionDpadGraphic>(true);
            }

            if (dpadGraphic == null)
            {
                var dpadObject = new GameObject("D-Pad Indicator", typeof(RectTransform));
                var dpadTransform = dpadObject.GetComponent<RectTransform>();
                dpadTransform.SetParent(safeAreaRoot, false);
                dpadGraphic = dpadObject.AddComponent<TopDown3DActionDpadGraphic>();
            }

            var rectTransform = dpadGraphic.rectTransform;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.zero;
            rectTransform.pivot = Vector2.zero;
            rectTransform.anchoredPosition = new Vector2(ReferenceMargin, ReferenceMargin);
            rectTransform.sizeDelta = Vector2.one * ReferenceSize;
            dpadGraphic.raycastTarget = false;
        }

        private void RefreshSafeArea(bool force)
        {
            if (safeAreaRoot == null || Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            var safeArea = Screen.safeArea;
            var screenSize = new Vector2Int(Screen.width, Screen.height);
            if (!force && safeArea == lastSafeArea && screenSize == lastScreenSize)
            {
                return;
            }

            lastSafeArea = safeArea;
            lastScreenSize = screenSize;
            safeAreaRoot.anchorMin = new Vector2(safeArea.xMin / Screen.width, safeArea.yMin / Screen.height);
            safeAreaRoot.anchorMax = new Vector2(safeArea.xMax / Screen.width, safeArea.yMax / Screen.height);
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
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

    [DisallowMultipleComponent]
    public sealed class TopDown3DActionDpadGraphic : MaskableGraphic
    {
        private static readonly Color32 ShadowColor = new(7, 8, 10, 118);
        private static readonly Color32 BorderColor = new(197, 173, 128, 208);
        private static readonly Color32 CellColor = new(19, 23, 27, 222);
        private static readonly Color32 PressedCellColor = new(79, 67, 46, 240);
        private static readonly Color32 IconColor = new(231, 221, 199, 240);
        private static readonly Color32 VialLiquidColor = new(91, 190, 177, 255);

        [SerializeField] private bool upPressed;
        [SerializeField] private bool leftPressed;
        [SerializeField] private bool rightPressed;
        [SerializeField] private bool downPressed;

        public void SetDirectionPressed(TopDown3DDpadDirection direction, bool pressed)
        {
            switch (direction)
            {
                case TopDown3DDpadDirection.Up:
                    upPressed = pressed;
                    break;
                case TopDown3DDpadDirection.Left:
                    leftPressed = pressed;
                    break;
                case TopDown3DDpadDirection.Right:
                    rightPressed = pressed;
                    break;
                case TopDown3DDpadDirection.Down:
                    downPressed = pressed;
                    break;
            }

            SetVerticesDirty();
        }

        public bool IsDirectionPressed(TopDown3DDpadDirection direction)
        {
            return direction switch
            {
                TopDown3DDpadDirection.Up => upPressed,
                TopDown3DDpadDirection.Left => leftPressed,
                TopDown3DDpadDirection.Right => rightPressed,
                TopDown3DDpadDirection.Down => downPressed,
                _ => false
            };
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();

            var bounds = rectTransform.rect;
            var side = Mathf.Min(bounds.width, bounds.height);
            var gap = side * 0.025f;
            var cellSize = (side - (gap * 2f)) / 3f;
            var step = cellSize + gap;
            var center = bounds.center;

            DrawCell(vertexHelper, center + (Vector2.up * step), cellSize, upPressed);
            DrawCell(vertexHelper, center + (Vector2.left * step), cellSize, leftPressed);
            DrawCell(vertexHelper, center + (Vector2.right * step), cellSize, rightPressed);
            DrawCell(vertexHelper, center + (Vector2.down * step), cellSize, downPressed);

            DrawDirectionArrow(vertexHelper, center + (Vector2.up * step), cellSize, Vector2.up);
            DrawDirectionArrow(vertexHelper, center + (Vector2.left * step), cellSize, Vector2.left);
            DrawDirectionArrow(vertexHelper, center + (Vector2.right * step), cellSize, Vector2.right);
            DrawVial(vertexHelper, center + (Vector2.down * step), cellSize);
        }

        private static void DrawCell(VertexHelper vertexHelper, Vector2 center, float size, bool pressed)
        {
            var shadowRect = RectFromCenter(center + new Vector2(0f, -size * 0.055f), size);
            AddChamferedRect(vertexHelper, shadowRect, size * 0.19f, ShadowColor);

            var outerRect = RectFromCenter(center, size);
            AddChamferedRect(vertexHelper, outerRect, size * 0.19f, BorderColor);

            var borderWidth = Mathf.Max(1.25f, size * 0.045f);
            var innerRect = new Rect(
                outerRect.xMin + borderWidth,
                outerRect.yMin + borderWidth,
                outerRect.width - (borderWidth * 2f),
                outerRect.height - (borderWidth * 2f));
            AddChamferedRect(
                vertexHelper,
                innerRect,
                Mathf.Max(0f, (size * 0.19f) - borderWidth),
                pressed ? PressedCellColor : CellColor);
        }

        private static void DrawDirectionArrow(
            VertexHelper vertexHelper,
            Vector2 center,
            float cellSize,
            Vector2 direction)
        {
            var perpendicular = new Vector2(-direction.y, direction.x);
            var tip = center + (direction * cellSize * 0.22f);
            var baseCenter = center - (direction * cellSize * 0.14f);
            var halfWidth = cellSize * 0.16f;
            AddTriangle(
                vertexHelper,
                tip,
                baseCenter + (perpendicular * halfWidth),
                baseCenter - (perpendicular * halfWidth),
                IconColor);
        }

        private static void DrawVial(VertexHelper vertexHelper, Vector2 center, float cellSize)
        {
            var scale = cellSize / 38f;
            AddQuad(
                vertexHelper,
                new Rect(center.x - (4.8f * scale), center.y + (6f * scale), 9.6f * scale, 2.8f * scale),
                IconColor);
            AddQuad(
                vertexHelper,
                new Rect(center.x - (3.1f * scale), center.y + (2.8f * scale), 6.2f * scale, 4f * scale),
                IconColor);

            var outer = new[]
            {
                center + new Vector2(-3.2f, 3f) * scale,
                center + new Vector2(3.2f, 3f) * scale,
                center + new Vector2(5.5f, 0.6f) * scale,
                center + new Vector2(5.5f, -7f) * scale,
                center + new Vector2(3.4f, -9.2f) * scale,
                center + new Vector2(-3.4f, -9.2f) * scale,
                center + new Vector2(-5.5f, -7f) * scale,
                center + new Vector2(-5.5f, 0.6f) * scale
            };
            AddConvexPolygon(vertexHelper, outer, IconColor);

            var inner = new[]
            {
                center + new Vector2(-2.4f, 1.7f) * scale,
                center + new Vector2(2.4f, 1.7f) * scale,
                center + new Vector2(3.8f, 0.1f) * scale,
                center + new Vector2(3.8f, -6.2f) * scale,
                center + new Vector2(2.4f, -7.6f) * scale,
                center + new Vector2(-2.4f, -7.6f) * scale,
                center + new Vector2(-3.8f, -6.2f) * scale,
                center + new Vector2(-3.8f, 0.1f) * scale
            };
            AddConvexPolygon(vertexHelper, inner, CellColor);

            var liquid = new[]
            {
                center + new Vector2(-3.75f, -3.1f) * scale,
                center + new Vector2(3.75f, -3.1f) * scale,
                center + new Vector2(3.75f, -6.1f) * scale,
                center + new Vector2(2.3f, -7.45f) * scale,
                center + new Vector2(-2.3f, -7.45f) * scale,
                center + new Vector2(-3.75f, -6.1f) * scale
            };
            AddConvexPolygon(vertexHelper, liquid, VialLiquidColor);
        }

        private static Rect RectFromCenter(Vector2 center, float size)
        {
            return new Rect(center.x - (size * 0.5f), center.y - (size * 0.5f), size, size);
        }

        private static void AddChamferedRect(VertexHelper vertexHelper, Rect rect, float chamfer, Color32 color)
        {
            var points = new[]
            {
                new Vector2(rect.xMin + chamfer, rect.yMin),
                new Vector2(rect.xMax - chamfer, rect.yMin),
                new Vector2(rect.xMax, rect.yMin + chamfer),
                new Vector2(rect.xMax, rect.yMax - chamfer),
                new Vector2(rect.xMax - chamfer, rect.yMax),
                new Vector2(rect.xMin + chamfer, rect.yMax),
                new Vector2(rect.xMin, rect.yMax - chamfer),
                new Vector2(rect.xMin, rect.yMin + chamfer)
            };
            AddConvexPolygon(vertexHelper, points, color);
        }

        private static void AddQuad(VertexHelper vertexHelper, Rect rect, Color32 color)
        {
            var start = vertexHelper.currentVertCount;
            vertexHelper.AddVert(new Vector3(rect.xMin, rect.yMin), color, Vector2.zero);
            vertexHelper.AddVert(new Vector3(rect.xMin, rect.yMax), color, Vector2.up);
            vertexHelper.AddVert(new Vector3(rect.xMax, rect.yMax), color, Vector2.one);
            vertexHelper.AddVert(new Vector3(rect.xMax, rect.yMin), color, Vector2.right);
            vertexHelper.AddTriangle(start, start + 1, start + 2);
            vertexHelper.AddTriangle(start, start + 2, start + 3);
        }

        private static void AddTriangle(
            VertexHelper vertexHelper,
            Vector2 first,
            Vector2 second,
            Vector2 third,
            Color32 color)
        {
            var start = vertexHelper.currentVertCount;
            vertexHelper.AddVert(first, color, Vector2.zero);
            vertexHelper.AddVert(second, color, Vector2.zero);
            vertexHelper.AddVert(third, color, Vector2.zero);
            vertexHelper.AddTriangle(start, start + 1, start + 2);
        }

        private static void AddConvexPolygon(VertexHelper vertexHelper, Vector2[] points, Color32 color)
        {
            var center = Vector2.zero;
            for (var i = 0; i < points.Length; i++)
            {
                center += points[i];
            }

            center /= points.Length;
            var centerIndex = vertexHelper.currentVertCount;
            vertexHelper.AddVert(center, color, Vector2.zero);
            for (var i = 0; i < points.Length; i++)
            {
                vertexHelper.AddVert(points[i], color, Vector2.zero);
            }

            for (var i = 0; i < points.Length; i++)
            {
                var next = (i + 1) % points.Length;
                vertexHelper.AddTriangle(centerIndex, centerIndex + i + 1, centerIndex + next + 1);
            }
        }
    }

    internal static class TopDown3DActionDpadHudBootstrap
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
            TopDown3DActionDpadHud.TryInstallForScene(scene);
        }
    }
}
