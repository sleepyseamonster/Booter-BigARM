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
        Vial,
        Droplet,
        Ration,
        HealthCross
    }

    [DisallowMultipleComponent]
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public sealed class TopDown3DActionDpadHud : MonoBehaviour
    {
        public const float ReferenceSize = 168f;
        public const float ReferenceMargin = 34f;
        public const int CanvasSortingOrder = TopDown3DGameHudCanvas.CanvasSortingOrder;

        [Header("Editable Layout")]
        [SerializeField, Min(64f)] private float size = ReferenceSize;
        [SerializeField, Min(0f)] private float safeAreaMargin = ReferenceMargin;

        private TopDown3DGameHudCanvas gameHud;
        private TopDown3DActionDpadGraphic dpadGraphic;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;
        private float lastCanvasScale;
        private bool compassLayout;

        public TopDown3DActionDpadGraphic Graphic => dpadGraphic;
        public float Size => size;
        public float SafeAreaMargin => safeAreaMargin;
        public bool CompassLayout => compassLayout;
        public float DetailsLeft => compassLayout ? TopDown3DCompassLayout.DetailsLeft(size) : size + 16f;
        public float DustBottom => compassLayout ? TopDown3DCompassLayout.DustBottom(size) : -23f;
        public event System.Action LayoutChanged;

        public void SetCompassLayout(bool enabled)
        {
            if (compassLayout == enabled) return;
            compassLayout = enabled;
            RefreshSafeArea(force: true);
        }

        [ContextMenu("Rebuild D-Pad Visuals")]
        public void Initialize()
        {
            EnsureGameHudParent();
            EnsureVisualTree();
            RefreshSafeArea(force: true);
        }

        public TopDown3DDpadIcon GetIcon(TopDown3DDpadDirection direction)
        {
            if (dpadGraphic != null && dpadGraphic.SurvivalIcons && direction != TopDown3DDpadDirection.Down)
                return direction == TopDown3DDpadDirection.Left ? TopDown3DDpadIcon.Droplet
                    : direction == TopDown3DDpadDirection.Right ? TopDown3DDpadIcon.Ration : TopDown3DDpadIcon.HealthCross;
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

        public static Vector2 GetBottomLeftAnchoredPosition(Rect safeArea, float canvasScale)
        {
            return GetBottomLeftAnchoredPosition(safeArea, canvasScale, ReferenceMargin);
        }

        public static Vector2 GetBottomLeftAnchoredPosition(Rect safeArea, float canvasScale, float margin)
        {
            var safeScale = Mathf.Max(0.001f, canvasScale);
            return new Vector2(
                margin + (safeArea.xMin / safeScale),
                margin + (safeArea.yMin / safeScale));
        }

        public static TopDown3DActionDpadHud TryInstallForScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return null;
            }

            var gameHud = TopDown3DGameHudCanvas.TryInstallForScene(scene);
            if (gameHud == null)
            {
                return null;
            }

            var existing = TopDown3DGameHudCanvas.FindInScene<TopDown3DActionDpadHud>(scene);
            if (existing != null)
            {
                existing.AttachToGameHud(gameHud);
                existing.Initialize();
                return existing;
            }

            var hudObject = new GameObject("Action D-Pad HUD", typeof(RectTransform));
            hudObject.transform.SetParent(gameHud.transform, false);
            var hud = hudObject.AddComponent<TopDown3DActionDpadHud>();
            hud.gameHud = gameHud;
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

        private void OnValidate()
        {
            size = Mathf.Max(64f, size);
            safeAreaMargin = Mathf.Max(0f, safeAreaMargin);
            RefreshSafeArea(force: true);
            dpadGraphic?.SetVerticesDirty();
        }

        private void EnsureGameHudParent()
        {
            if (gameHud == null)
            {
                gameHud = GetComponentInParent<TopDown3DGameHudCanvas>();
            }

            if (gameHud == null && gameObject.scene.IsValid() && gameObject.scene.isLoaded)
            {
                gameHud = TopDown3DGameHudCanvas.TryInstallForScene(gameObject.scene);
            }

            if (gameHud != null)
            {
                AttachToGameHud(gameHud);
            }
        }

        private void EnsureVisualTree()
        {
            if (dpadGraphic == null)
            {
                dpadGraphic = GetComponentInChildren<TopDown3DActionDpadGraphic>(true);
            }

            if (dpadGraphic == null)
            {
                var dpadObject = new GameObject("D-Pad Indicator", typeof(RectTransform));
                var dpadTransform = dpadObject.GetComponent<RectTransform>();
                dpadTransform.SetParent(transform, false);
                dpadGraphic = dpadObject.AddComponent<TopDown3DActionDpadGraphic>();
            }

            var rectTransform = dpadGraphic.rectTransform;
            if (rectTransform.parent != transform)
            {
                rectTransform.SetParent(transform, false);
            }

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            dpadGraphic.raycastTarget = false;
            if (dpadGraphic.GetComponent<CanvasRenderer>() == null)
                dpadGraphic.gameObject.AddComponent<CanvasRenderer>();

            var obsoleteSafeArea = transform.Find("Safe Area");
            if (obsoleteSafeArea != null && obsoleteSafeArea.childCount == 0)
            {
                DestroyRuntimeObject(obsoleteSafeArea.gameObject);
            }
        }

        private void RefreshSafeArea(bool force)
        {
            if (gameHud == null || Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            var safeArea = Screen.safeArea;
            var screenSize = new Vector2Int(Screen.width, Screen.height);
            var canvasScale = gameHud.Canvas != null ? gameHud.Canvas.scaleFactor : 1f;
            if (!force
                && safeArea == lastSafeArea
                && screenSize == lastScreenSize
                && Mathf.Approximately(canvasScale, lastCanvasScale))
            {
                return;
            }

            lastSafeArea = safeArea;
            lastScreenSize = screenSize;
            lastCanvasScale = canvasScale;
            var rectTransform = (RectTransform)transform;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.zero;
            rectTransform.pivot = Vector2.zero;
            rectTransform.sizeDelta = Vector2.one * size;
            rectTransform.anchoredPosition = GetBottomLeftAnchoredPosition(
                safeArea,
                canvasScale,
                safeAreaMargin) + (compassLayout ? TopDown3DCompassLayout.ExtraInset(size) : Vector2.zero);
            LayoutChanged?.Invoke();
        }

        private void AttachToGameHud(TopDown3DGameHudCanvas owner)
        {
            gameHud = owner;
            if (owner.gameObject == gameObject)
            {
                return;
            }

            if (transform.parent != owner.transform)
            {
                transform.SetParent(owner.transform, false);
            }

            DestroyRuntimeObject(GetComponent<GraphicRaycaster>());
            DestroyRuntimeObject(GetComponent<CanvasScaler>());
            DestroyRuntimeObject(GetComponent<Canvas>());
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

    public abstract class TopDown3DActionDpadGraphicBase : MaskableGraphic
    {
        private static readonly Color32 ShadowColor = new(7, 8, 10, 118);
        private static readonly Color32 BackplateBorderColor = new(172, 146, 99, 126);
        private static readonly Color32 BackplateColor = new(10, 13, 16, 154);
        private static readonly Color32 BorderColor = new(197, 173, 128, 208);
        private static readonly Color32 VialBorderColor = new(93, 190, 177, 230);
        private static readonly Color32 CellColor = new(19, 23, 27, 222);
        private static readonly Color32 PressedCellColor = new(79, 67, 46, 240);
        private static readonly Color32 CellHighlightColor = new(255, 238, 197, 24);
        private static readonly Color32 IconColor = new(231, 221, 199, 240);
        private static readonly Color32 VialLiquidColor = new(91, 190, 177, 255);
        public bool SurvivalIcons { get; private set; }
        public void SetSurvivalIcons(bool enabled) { SurvivalIcons = enabled; SetVerticesDirty(); }

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

            AddCircle(vertexHelper, center + new Vector2(0f, -side * 0.018f), side * 0.5f, 32, ShadowColor);
            AddCircle(vertexHelper, center, side * 0.485f, 32, BackplateBorderColor);
            AddCircle(vertexHelper, center, side * 0.455f, 32, BackplateColor);

            DrawCell(vertexHelper, center + (Vector2.up * step), cellSize, upPressed, BorderColor);
            DrawCell(vertexHelper, center + (Vector2.left * step), cellSize, leftPressed, BorderColor);
            DrawCell(vertexHelper, center + (Vector2.right * step), cellSize, rightPressed, BorderColor);
            DrawCell(vertexHelper, center + (Vector2.down * step), cellSize, downPressed, VialBorderColor);

            AddCircle(vertexHelper, center, cellSize * 0.18f, 16, BorderColor);
            AddCircle(vertexHelper, center, cellSize * 0.125f, 16, CellColor);

            if (SurvivalIcons)
            {
                var up = center + Vector2.up * step;
                var red = new Color32(220, 89, 72, 255);
                AddQuad(vertexHelper, new Rect(up.x-cellSize*.07f, up.y-cellSize*.24f, cellSize*.14f,cellSize*.48f),red);
                AddQuad(vertexHelper, new Rect(up.x-cellSize*.24f, up.y-cellSize*.07f, cellSize*.48f,cellSize*.14f),red);
                var left = center + Vector2.left * step;
                var blue = new Color32(81,190,197,255);
                AddCircle(vertexHelper,left+Vector2.down*cellSize*.08f,cellSize*.14f,16,blue);
                AddTriangle(vertexHelper,left+Vector2.up*cellSize*.25f,left+new Vector2(-.13f,-.05f)*cellSize,left+new Vector2(.13f,-.05f)*cellSize,blue);
                var right = center + Vector2.right * step;
                AddChamferedRect(vertexHelper,new Rect(right.x-cellSize*.23f,right.y-cellSize*.16f,cellSize*.46f,cellSize*.32f),cellSize*.06f,new Color32(216,168,76,255));
                for(var i=-1;i<=1;i++) AddQuad(vertexHelper,new Rect(right.x+cellSize*i*.12f-cellSize*.015f,right.y-cellSize*.1f,cellSize*.03f,cellSize*.2f),CellColor);
            }
            else
            {
                DrawDirectionArrow(vertexHelper, center + (Vector2.up * step), cellSize, Vector2.up);
                DrawDirectionArrow(vertexHelper, center + (Vector2.left * step), cellSize, Vector2.left);
                DrawDirectionArrow(vertexHelper, center + (Vector2.right * step), cellSize, Vector2.right);
            }
            DrawVial(vertexHelper, center + (Vector2.down * step), cellSize);
        }

        private static void DrawCell(
            VertexHelper vertexHelper,
            Vector2 center,
            float size,
            bool pressed,
            Color32 borderColor)
        {
            var shadowRect = RectFromCenter(center + new Vector2(0f, -size * 0.055f), size);
            AddChamferedRect(vertexHelper, shadowRect, size * 0.19f, ShadowColor);

            var outerRect = RectFromCenter(center, size);
            AddChamferedRect(vertexHelper, outerRect, size * 0.19f, borderColor);

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

            var highlightHeight = Mathf.Max(1f, size * 0.055f);
            var highlightRect = new Rect(
                innerRect.xMin + (size * 0.13f),
                innerRect.yMax - highlightHeight - (size * 0.08f),
                innerRect.width - (size * 0.26f),
                highlightHeight);
            AddChamferedRect(vertexHelper, highlightRect, highlightHeight * 0.45f, CellHighlightColor);
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
                center + new Vector2(-5.5f, -7f) * scale,
                center + new Vector2(-3.4f, -9.2f) * scale,
                center + new Vector2(3.4f, -9.2f) * scale,
                center + new Vector2(5.5f, -7f) * scale,
                center + new Vector2(5.5f, 0.6f) * scale,
                center + new Vector2(3.2f, 3f) * scale,
                center + new Vector2(-3.2f, 3f) * scale,
                center + new Vector2(-5.5f, 0.6f) * scale
            };
            AddConvexPolygon(vertexHelper, outer, IconColor);

            var inner = new[]
            {
                center + new Vector2(-3.8f, -6.2f) * scale,
                center + new Vector2(-2.4f, -7.6f) * scale,
                center + new Vector2(2.4f, -7.6f) * scale,
                center + new Vector2(3.8f, -6.2f) * scale,
                center + new Vector2(3.8f, 0.1f) * scale,
                center + new Vector2(2.4f, 1.7f) * scale,
                center + new Vector2(-2.4f, 1.7f) * scale,
                center + new Vector2(-3.8f, 0.1f) * scale
            };
            AddConvexPolygon(vertexHelper, inner, CellColor);

            var liquid = new[]
            {
                center + new Vector2(-2.3f, -7.45f) * scale,
                center + new Vector2(2.3f, -7.45f) * scale,
                center + new Vector2(3.75f, -6.1f) * scale,
                center + new Vector2(3.75f, -3.1f) * scale,
                center + new Vector2(-3.75f, -3.1f) * scale,
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
            vertexHelper.AddTriangle(start, start + 2, start + 1);
        }

        private static void AddCircle(
            VertexHelper vertexHelper,
            Vector2 center,
            float radius,
            int segments,
            Color32 color)
        {
            var centerIndex = vertexHelper.currentVertCount;
            vertexHelper.AddVert(center, color, Vector2.zero);
            for (var i = 0; i < segments; i++)
            {
                var angle = (Mathf.PI * 2f * i) / segments;
                var point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                vertexHelper.AddVert(point, color, Vector2.zero);
            }

            for (var i = 0; i < segments; i++)
            {
                var next = (i + 1) % segments;
                vertexHelper.AddTriangle(centerIndex, centerIndex + next + 1, centerIndex + i + 1);
            }
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
                vertexHelper.AddTriangle(centerIndex, centerIndex + next + 1, centerIndex + i + 1);
            }
        }
    }

}
