using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class TopDown3DSurvivalHud : MonoBehaviour
    {
        public const float ReferenceWidth = 300f;
        public const float ReferenceHeight = 152f;
        public const float ReferenceMargin = 34f;
        public const float ReferenceGap = 5f;
        public const float ReferencePadding = 8f;

        private static readonly Color32 ShadowColor = new(3, 4, 5, 122);
        private static readonly Color32 PanelColor = new(7, 8, 10, 224);
        private static readonly Color32 CellColor = new(19, 23, 27, 240);
        private static readonly Color32 BorderColor = new(197, 173, 128, 210);
        private static readonly Color32 HighlightColor = new(250, 224, 168, 44);
        private static readonly Color32 TrackColor = new(6, 8, 9, 255);
        private static readonly Color32 LabelColor = new(231, 219, 190, 245);
        private static readonly Color32 HealthColor = new(186, 56, 43, 255);
        private static readonly Color32 HungerColor = new(199, 145, 51, 255);
        private static readonly Color32 ThirstColor = new(56, 166, 171, 255);
        private static readonly Color32 OxygenColor = new(138, 186, 199, 255);
        private static readonly string[] VitalLabels = { "HEALTH", "HUNGER", "THIRST", "OXYGEN" };
        private static readonly Color32[] VitalColors = { HealthColor, HungerColor, ThirstColor, OxygenColor };

        [SerializeField] private TopDown3DSurvivalVitals vitals;

        private readonly RectTransform[] fillRects = new RectTransform[4];
        private TopDown3DGameHudCanvas gameHud;
        private RectTransform panelRect;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;
        private float lastCanvasScale;
        private bool subscribed;

        public TopDown3DSurvivalVitals Vitals => vitals;
        public TopDown3DGameHudCanvas GameHud => gameHud;
        public RectTransform PanelRect => panelRect;

        public void Configure(TopDown3DSurvivalVitals survivalVitals)
        {
            UnsubscribeFromVitals();
            vitals = survivalVitals;
            SubscribeToVitals();
            Initialize();
            RefreshMeters();
        }

        public static Vector2 GetTopLeftAnchoredPosition(Rect safeArea, int screenHeight, float canvasScale)
        {
            var safeScale = Mathf.Max(0.001f, canvasScale);
            return new Vector2(
                ReferenceMargin + (safeArea.xMin / safeScale),
                -(ReferenceMargin + ((screenHeight - safeArea.yMax) / safeScale)));
        }

        public static TopDown3DSurvivalHud TryInstallForScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return null;
            }

            var player = TopDown3DGameHudCanvas.FindInScene<TopDown3DPlayerMotor>(scene);
            if (player == null)
            {
                return null;
            }

            var survivalVitals = player.GetComponent<TopDown3DSurvivalVitals>();
            if (survivalVitals == null)
            {
                survivalVitals = player.gameObject.AddComponent<TopDown3DSurvivalVitals>();
            }

            var gameHud = TopDown3DGameHudCanvas.TryInstallForScene(scene);
            if (gameHud == null)
            {
                return null;
            }

            var existing = TopDown3DGameHudCanvas.FindInScene<TopDown3DSurvivalHud>(scene);
            if (existing != null && existing.transform is RectTransform)
            {
                existing.AttachToGameHud(gameHud);
                existing.Configure(survivalVitals);
                return existing;
            }

            if (existing != null)
            {
                DestroyRuntimeObject(existing.gameObject);
            }

            var hudObject = new GameObject("Survival HUD", typeof(RectTransform));
            hudObject.transform.SetParent(gameHud.transform, false);
            var hud = hudObject.AddComponent<TopDown3DSurvivalHud>();
            hud.gameHud = gameHud;
            hud.Configure(survivalVitals);
            return hud;
        }

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            SubscribeToVitals();
            Initialize();
            RefreshMeters();
        }

        private void OnDisable()
        {
            UnsubscribeFromVitals();
        }

        private void Update()
        {
            RefreshSafeArea(force: false);
        }

        private void Initialize()
        {
            panelRect = transform as RectTransform;
            if (panelRect == null)
            {
                return;
            }

            if (gameHud == null)
            {
                gameHud = GetComponentInParent<TopDown3DGameHudCanvas>();
            }

            if (gameHud != null)
            {
                AttachToGameHud(gameHud);
            }

            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.sizeDelta = new Vector2(ReferenceWidth, ReferenceHeight);
            EnsureVisualTree();
            RefreshSafeArea(force: true);
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
        }

        private void EnsureVisualTree()
        {
            var shadow = EnsureImage("Drop Shadow", ShadowColor);
            Stretch(shadow.rectTransform, new Vector2(4f, -5f), new Vector2(4f, -5f));
            shadow.transform.SetAsFirstSibling();

            var border = EnsureImage("Border", BorderColor);
            Stretch(border.rectTransform, Vector2.zero, Vector2.zero);
            border.transform.SetSiblingIndex(1);

            var background = EnsureImage("Background", PanelColor);
            Stretch(background.rectTransform, Vector2.one * 2f, Vector2.one * -2f);
            background.transform.SetSiblingIndex(2);

            var highlight = EnsureImage("Top Highlight", HighlightColor);
            highlight.rectTransform.anchorMin = new Vector2(0f, 1f);
            highlight.rectTransform.anchorMax = Vector2.one;
            highlight.rectTransform.pivot = new Vector2(0.5f, 1f);
            highlight.rectTransform.offsetMin = new Vector2(2f, -4f);
            highlight.rectTransform.offsetMax = new Vector2(-2f, -2f);
            highlight.transform.SetSiblingIndex(3);

            var rowHeight = (ReferenceHeight - (ReferencePadding * 2f) - (ReferenceGap * 3f)) * 0.25f;
            for (var i = 0; i < 4; i++)
            {
                EnsureVitalRow(i, rowHeight);
            }
        }

        private void EnsureVitalRow(int index, float rowHeight)
        {
            var row = EnsureImage($"Vital Row {index}", CellColor);
            var rowRect = row.rectTransform;
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(1f, 1f);
            rowRect.pivot = new Vector2(0.5f, 1f);
            rowRect.sizeDelta = new Vector2(-(ReferencePadding * 2f), rowHeight);
            rowRect.anchoredPosition = new Vector2(
                0f,
                -(ReferencePadding + (index * (rowHeight + ReferenceGap))));

            var pip = EnsureImage("Pip", VitalColors[index], rowRect);
            pip.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            pip.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            pip.rectTransform.pivot = new Vector2(0f, 0.5f);
            pip.rectTransform.anchoredPosition = new Vector2(7f, 0f);
            pip.rectTransform.sizeDelta = new Vector2(4f, 14f);

            var label = EnsureText("Label", rowRect);
            label.text = VitalLabels[index];
            label.color = LabelColor;
            label.fontSize = 13;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleLeft;
            label.raycastTarget = false;
            label.rectTransform.anchorMin = new Vector2(0f, 0f);
            label.rectTransform.anchorMax = new Vector2(0f, 1f);
            label.rectTransform.pivot = new Vector2(0f, 0.5f);
            label.rectTransform.anchoredPosition = new Vector2(18f, 0f);
            label.rectTransform.sizeDelta = new Vector2(62f, 0f);

            var track = EnsureImage("Track", TrackColor, rowRect);
            track.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            track.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            track.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            track.rectTransform.offsetMin = new Vector2(84f, -6f);
            track.rectTransform.offsetMax = new Vector2(-7f, 6f);

            var fill = EnsureImage("Fill", VitalColors[index], track.rectTransform);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;
            fillRects[index] = fill.rectTransform;

            var sheen = EnsureImage("Sheen", new Color32(255, 255, 255, 56), fill.rectTransform);
            sheen.rectTransform.anchorMin = new Vector2(0f, 0.66f);
            sheen.rectTransform.anchorMax = Vector2.one;
            sheen.rectTransform.offsetMin = Vector2.zero;
            sheen.rectTransform.offsetMax = Vector2.zero;
        }

        private Image EnsureImage(string objectName, Color color, RectTransform parent = null)
        {
            var owner = parent != null ? parent : panelRect;
            var child = owner.Find(objectName);
            if (child == null)
            {
                var childObject = new GameObject(objectName, typeof(RectTransform));
                child = childObject.GetComponent<RectTransform>();
                child.SetParent(owner, false);
            }

            var image = child.GetComponent<Image>();
            if (image == null)
            {
                image = child.gameObject.AddComponent<Image>();
            }

            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Text EnsureText(string objectName, RectTransform parent)
        {
            var child = parent.Find(objectName);
            if (child == null)
            {
                var childObject = new GameObject(objectName, typeof(RectTransform));
                child = childObject.GetComponent<RectTransform>();
                child.SetParent(parent, false);
            }

            var text = child.GetComponent<Text>();
            if (text == null)
            {
                text = child.gameObject.AddComponent<Text>();
            }

            if (text.font == null)
            {
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            return text;
        }

        private void RefreshSafeArea(bool force)
        {
            if (panelRect == null || gameHud == null || Screen.width <= 0 || Screen.height <= 0)
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
            panelRect.anchoredPosition = GetTopLeftAnchoredPosition(safeArea, Screen.height, canvasScale);
        }

        private void RefreshMeters()
        {
            if (vitals == null)
            {
                return;
            }

            for (var i = 0; i < fillRects.Length; i++)
            {
                if (fillRects[i] == null)
                {
                    continue;
                }

                var normalized = vitals.GetNormalizedValue((TopDown3DSurvivalVital)i);
                fillRects[i].anchorMax = new Vector2(normalized, 1f);
            }
        }

        private void SubscribeToVitals()
        {
            if (subscribed || vitals == null)
            {
                return;
            }

            vitals.Changed += RefreshMeters;
            subscribed = true;
        }

        private void UnsubscribeFromVitals()
        {
            if (!subscribed || vitals == null)
            {
                subscribed = false;
                return;
            }

            vitals.Changed -= RefreshMeters;
            subscribed = false;
        }

        private static void Stretch(RectTransform rectTransform, Vector2 offsetMin, Vector2 offsetMax)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
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

}
