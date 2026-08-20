using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class TopDown3DInventoryCanvas : MonoBehaviour
    {
        public const int CanvasSortingOrder = 220;

        private readonly List<TopDown3DInventorySlotView> slotViews =
            new List<TopDown3DInventorySlotView>();
        private readonly List<TopDown3DInventorySlotView> cargoSlotViews =
            new List<TopDown3DInventorySlotView>();
        private Canvas inventoryCanvas;
        private GraphicRaycaster raycaster;
        private RectTransform safeAreaRoot;
        private RectTransform panel;
        private Text detailName;
        private Text detailDescription;
        private Text summary;
        private Text controls;
        private Text cargoSummary;
        private Button autoPackButton;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;

        public IReadOnlyList<TopDown3DInventorySlotView> SlotViews => slotViews;
        public IReadOnlyList<TopDown3DInventorySlotView> CargoSlotViews => cargoSlotViews;
        public bool IsVisible => inventoryCanvas != null && inventoryCanvas.enabled;
        public RectTransform SafeAreaRoot => safeAreaRoot;
        public string ControlHints => controls != null ? controls.text : string.Empty;

        public void Initialize(
            int capacity,
            Action<int> slotActivated,
            Action<int> slotSelected)
        {
            EnsureCanvas();
            EnsureVisualTree(Mathf.Max(1, capacity), slotActivated, slotSelected);
            RefreshSafeArea(true);
        }

        public void InitializeDual(int bootCapacity, int cargoCapacity, Action<int> bootActivated, Action<int> bootSelected, Action<int> cargoActivated, Action<int> cargoSelected, Action autoPack)
        {
            Initialize(bootCapacity, bootActivated, bootSelected);
            var grid = EnsureRect("BigARM Mount Grid", panel);
            grid.anchorMin = new Vector2(0f, 1f); grid.anchorMax = new Vector2(0f, 1f); grid.pivot = new Vector2(0f, 1f);
            grid.anchoredPosition = new Vector2(472f, -94f); grid.sizeDelta = new Vector2(416f, 280f);
            var layout = grid.GetComponent<GridLayoutGroup>() ?? grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(86f, 70f); layout.spacing = new Vector2(6f, 6f); layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount; layout.constraintCount = 4;
            var title = EnsureText("BigARM Title", panel); title.text = "BIGARM LOADFRAME"; title.fontSize = 19; title.fontStyle = FontStyle.Bold; Place(title.rectTransform, new Vector2(472f, -66f), new Vector2(416f, 28f));
            while (cargoSlotViews.Count < cargoCapacity)
            {
                var index = cargoSlotViews.Count;
                var slotObject = new GameObject($"BigARM Mount {index + 1}", typeof(RectTransform), typeof(Image), typeof(Button));
                slotObject.transform.SetParent(grid, false);
                cargoSlotViews.Add(slotObject.AddComponent<TopDown3DInventorySlotView>());
            }
            for (var i = 0; i < cargoSlotViews.Count; i++)
            {
                cargoSlotViews[i].gameObject.SetActive(i < cargoCapacity);
                if (i < cargoCapacity) cargoSlotViews[i].Initialize(i, cargoActivated, cargoSelected);
            }
            cargoSummary = EnsureText("BigARM Load Summary", panel); cargoSummary.fontSize = 16; cargoSummary.alignment = TextAnchor.UpperLeft; Place(cargoSummary.rectTransform, new Vector2(472f, -392f), new Vector2(416f, 58f));
            autoPackButton = EnsureButton("Auto-Pack", panel, "AUTO-PACK", autoPack); Place(autoPackButton.GetComponent<RectTransform>(), new Vector2(472f, -470f), new Vector2(190f, 42f));
        }

        public void SetCargoSummary(string text) { if (cargoSummary != null) cargoSummary.text = text ?? string.Empty; }

        public void SetVisible(bool visible)
        {
            EnsureCanvas();
            inventoryCanvas.enabled = visible;
            raycaster.enabled = visible;
            if (panel != null)
            {
                panel.gameObject.SetActive(visible);
            }
        }

        public void SetDetails(string itemName, string description, string slotSummary)
        {
            detailName.text = itemName ?? string.Empty;
            detailDescription.text = description ?? string.Empty;
            summary.text = slotSummary ?? string.Empty;
        }

        public void SetControlHints(string text)
        {
            if (controls != null)
            {
                controls.text = text ?? string.Empty;
            }
        }

        private void Awake()
        {
            EnsureCanvas();
        }

        private void Update()
        {
            RefreshSafeArea(false);
        }

        private void EnsureCanvas()
        {
            inventoryCanvas = GetComponent<Canvas>();
            if (inventoryCanvas == null)
            {
                inventoryCanvas = gameObject.AddComponent<Canvas>();
            }

            inventoryCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            inventoryCanvas.sortingOrder = CanvasSortingOrder;
            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = gameObject.AddComponent<CanvasScaler>();
            }
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            raycaster = GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                raycaster = gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        private void EnsureVisualTree(
            int capacity,
            Action<int> slotActivated,
            Action<int> slotSelected)
        {
            safeAreaRoot = EnsureRect("Safe Area", transform);
            safeAreaRoot.anchorMin = Vector2.zero;
            safeAreaRoot.anchorMax = Vector2.one;
            safeAreaRoot.offsetMin = Vector2.zero;
            safeAreaRoot.offsetMax = Vector2.zero;

            var dim = EnsureImage("World Dim", safeAreaRoot, new Color32(3, 4, 5, 188));
            Stretch(dim.rectTransform);
            panel = EnsureRect("Inventory Panel", safeAreaRoot);
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(920f, 600f);
            var panelImage = panel.GetComponent<Image>();
            if (panelImage == null)
            {
                panelImage = panel.gameObject.AddComponent<Image>();
            }
            panelImage.color = new Color32(12, 15, 18, 252);

            var title = EnsureText("Title", panel);
            title.text = "INVENTORY";
            title.fontSize = 30;
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleLeft;
            Place(title.rectTransform, new Vector2(32f, -24f), new Vector2(520f, 48f));

            summary = EnsureText("Summary", panel);
            summary.fontSize = 17;
            summary.alignment = TextAnchor.MiddleRight;
            summary.color = new Color32(195, 178, 143, 255);
            summary.rectTransform.anchorMin = new Vector2(1f, 1f);
            summary.rectTransform.anchorMax = new Vector2(1f, 1f);
            summary.rectTransform.pivot = new Vector2(1f, 1f);
            summary.rectTransform.anchoredPosition = new Vector2(-32f, -24f);
            summary.rectTransform.sizeDelta = new Vector2(280f, 48f);

            var grid = EnsureRect("Slot Grid", panel);
            grid.anchorMin = new Vector2(0f, 1f);
            grid.anchorMax = new Vector2(0f, 1f);
            grid.pivot = new Vector2(0f, 1f);
            grid.anchoredPosition = new Vector2(32f, -94f);
            grid.sizeDelta = new Vector2(404f, 404f);
            var layout = grid.GetComponent<GridLayoutGroup>();
            if (layout == null)
            {
                layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            }
            layout.cellSize = new Vector2(92f, 92f);
            layout.spacing = new Vector2(12f, 12f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 4;
            layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            layout.startAxis = GridLayoutGroup.Axis.Horizontal;

            while (slotViews.Count < capacity)
            {
                var index = slotViews.Count;
                var slotObject = new GameObject(
                    $"Slot {index + 1}",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(Button));
                slotObject.transform.SetParent(grid, false);
                slotViews.Add(slotObject.AddComponent<TopDown3DInventorySlotView>());
            }

            for (var i = 0; i < slotViews.Count; i++)
            {
                var active = i < capacity;
                slotViews[i].gameObject.SetActive(active);
                if (active)
                {
                    slotViews[i].Initialize(i, slotActivated, slotSelected);
                }
            }

            var details = EnsureImage("Item Details", panel, new Color32(24, 28, 32, 250));
            details.rectTransform.anchorMin = new Vector2(0f, 1f);
            details.rectTransform.anchorMax = new Vector2(0f, 1f);
            details.rectTransform.pivot = new Vector2(0f, 1f);
            details.rectTransform.anchoredPosition = new Vector2(472f, -94f);
            details.rectTransform.sizeDelta = new Vector2(416f, 404f);
            detailName = EnsureText("Item Name", details.rectTransform);
            detailName.fontSize = 25;
            detailName.fontStyle = FontStyle.Bold;
            detailName.alignment = TextAnchor.UpperLeft;
            Place(detailName.rectTransform, new Vector2(22f, -22f), new Vector2(372f, 52f));
            detailDescription = EnsureText("Description", details.rectTransform);
            detailDescription.fontSize = 18;
            detailDescription.alignment = TextAnchor.UpperLeft;
            detailDescription.horizontalOverflow = HorizontalWrapMode.Wrap;
            detailDescription.verticalOverflow = VerticalWrapMode.Overflow;
            Place(detailDescription.rectTransform, new Vector2(22f, -92f), new Vector2(372f, 230f));

            controls = EnsureText("Controls", panel);
            controls.fontSize = 16;
            controls.alignment = TextAnchor.MiddleCenter;
            controls.color = new Color32(177, 164, 137, 255);
            controls.rectTransform.anchorMin = new Vector2(0f, 0f);
            controls.rectTransform.anchorMax = new Vector2(1f, 0f);
            controls.rectTransform.pivot = new Vector2(0.5f, 0f);
            controls.rectTransform.anchoredPosition = new Vector2(0f, 22f);
            controls.rectTransform.sizeDelta = new Vector2(-64f, 38f);
        }

        private void RefreshSafeArea(bool force)
        {
            if (safeAreaRoot == null || Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            var safe = Screen.safeArea;
            var screen = new Vector2Int(Screen.width, Screen.height);
            if (!force && safe == lastSafeArea && screen == lastScreenSize)
            {
                return;
            }

            lastSafeArea = safe;
            lastScreenSize = screen;
            safeAreaRoot.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            safeAreaRoot.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            safeAreaRoot.offsetMin = Vector2.zero;
            safeAreaRoot.offsetMax = Vector2.zero;
        }

        private static RectTransform EnsureRect(string name, Transform parent)
        {
            var existing = parent.Find(name) as RectTransform;
            if (existing != null)
            {
                return existing;
            }

            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static Image EnsureImage(string name, Transform parent, Color color)
        {
            var rect = EnsureRect(name, parent);
            var image = rect.GetComponent<Image>();
            if (image == null)
            {
                image = rect.gameObject.AddComponent<Image>();
            }
            image.color = color;
            return image;
        }

        private static Text EnsureText(string name, Transform parent)
        {
            var rect = EnsureRect(name, parent);
            var text = rect.GetComponent<Text>();
            if (text == null)
            {
                text = rect.gameObject.AddComponent<Text>();
            }
            text.font ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.color = new Color32(235, 225, 204, 255);
            text.raycastTarget = false;
            return text;
        }

        private static Button EnsureButton(string name, Transform parent, string label, Action clicked)
        {
            var rect = EnsureRect(name, parent); var button = rect.GetComponent<Button>() ?? rect.gameObject.AddComponent<Button>();
            var image = rect.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>(); image.color = new Color32(92, 70, 36, 255); button.targetGraphic = image; button.onClick.RemoveAllListeners(); if (clicked != null) button.onClick.AddListener(() => clicked());
            var text = EnsureText("Label", rect); text.text = label; text.alignment = TextAnchor.MiddleCenter; Place(text.rectTransform, Vector2.zero, new Vector2(190f, 42f));
            return button;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
