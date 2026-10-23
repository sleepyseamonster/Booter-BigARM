using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BooterBigArm.TopDown3D
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class TopDown3DCampWorkstationCanvas : MonoBehaviour
    {
        private Canvas canvas;
        private CanvasGroup group;
        private RectTransform safeArea;
        private Text title, detail, notice;
        private RectTransform rows;
        private readonly List<Button> items = new();
        private readonly Dictionary<string, Button> commands = new();
        private Action<string> command;
        private Action<int> selected;
        private Rect lastSafeArea;
        private bool largeText;
        private bool recipeLayout;
        private readonly Dictionary<Text, int> baseSizes = new();

        public bool IsVisible => canvas != null && canvas.enabled;
        public void Initialize(Action<string> onCommand, Action<int> onSelected)
        {
            command = onCommand; selected = onSelected;
            if (canvas != null) return;
            var oldTree = transform.Find("Safe Area");
            if (oldTree != null)
            { oldTree.gameObject.SetActive(false); if (Application.isPlaying) Destroy(oldTree.gameObject); else DestroyImmediate(oldTree.gameObject); }
            canvas = GetComponent<Canvas>(); if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 225;
            var scaler = GetComponent<CanvasScaler>(); if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();
            group = GetComponent<CanvasGroup>(); if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            safeArea = Rect("Safe Area", transform); safeArea.anchorMin = Vector2.zero; safeArea.anchorMax = Vector2.one;
            var panel = Rect("Camp Services", safeArea); panel.sizeDelta = new Vector2(1000, 900);
            var background = panel.gameObject.AddComponent<Image>(); background.color = new Color(.075f, .065f, .055f, .98f);
            title = Text("Title", panel, new Vector2(35, -25), new Vector2(930, 45), 28);
            detail = Text("State", panel, new Vector2(520, -85), new Vector2(445, 210), 21);
            notice = Text("Notice", panel, new Vector2(35, -770), new Vector2(930, 95), 19);
            rows = Rect("Items", panel); Place(rows, new Vector2(35, -135), new Vector2(450, 510));
            var rowGrid = rows.gameObject.AddComponent<GridLayoutGroup>(); rowGrid.cellSize = new Vector2(220, 38);
            rowGrid.spacing = new Vector2(8, 4); rowGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; rowGrid.constraintCount = 2;
            var source = Button("Source", "SOURCE: BOOTER", panel, () => command?.Invoke("Source"));
            Place(source.GetComponent<RectTransform>(), new Vector2(35, -85), new Vector2(450, 42)); commands.Add("Source", source);
            var names = new[] { "Craft", "Insert", "Refine", "Battery", "Canister", "Deploy", "Transfer", "Destination", "Reset", "Close", "OverviewItems", "OverviewStations", "OverviewCompanions", "Station0", "Station1", "Station2", "Sort", "Take", "Call", "ManageTransfer" };
            var labels = new[] { "CRAFT EMPTY BATTERY", "INSERT SELECTED ITEM", "REFINE AND CHARGE", "TAKE BATTERY", "TAKE CANISTER", "DEPLOY SELECTED CANISTER", "TRANSFER SELECTED ITEM", "DESTINATION: BOOTER", "RESET CAMP ECONOMY", "CLOSE", "CAMP OVERVIEW · ITEMS", "CAMP STATIONS", "COMPANIONS", "WORKBENCH", "ARC DUST REFINERY", "IRONSTONE SORTER", "PREPARE ONE SORTING BATCH", "TAKE ENTIRE SELECTED STACK", "CALL LEGGER", "MOVE ENTIRE SELECTED STACK" };
            for (var i = 0; i < names.Length; i++)
            {
                var id = names[i]; var value = Button(id, labels[i], panel, () => command?.Invoke(id));
                Place(value.GetComponent<RectTransform>(), new Vector2(520, -320 - i * 40), new Vector2(445, 36));
                commands.Add(id, value);
            }
            SetLargeText(false); SetVisible(false); UpdateSafeArea();
        }

        public void Present(string heading, string state, string message, IReadOnlyList<string> labels, int selection, bool enabled, bool recipes = false)
        {
            recipeLayout = recipes;
            var grid = rows.GetComponent<GridLayoutGroup>();
            grid.constraintCount = recipes ? 1 : 2; grid.cellSize = recipes ? new Vector2(450, 52) : new Vector2(220, 38);
            detail.rectTransform.sizeDelta = new Vector2(445, recipes ? 260 : 210);
            notice.rectTransform.anchoredPosition = new Vector2(35, recipes ? -720 : -770);
            notice.rectTransform.sizeDelta = new Vector2(930, recipes ? 145 : 95);
            title.text = heading; detail.text = state; notice.text = message;
            while (items.Count < labels.Count)
            {
                var index = items.Count;
                var item = Button("Item " + index, "", rows, () => selected?.Invoke(index));
                item.GetComponentInChildren<Text>().fontSize = 16;
                items.Add(item);
            }
            SetLargeText(largeText);
            for (var i = 0; i < items.Count; i++)
            {
                items[i].gameObject.SetActive(i < labels.Count);
                if (i >= labels.Count) continue;
                items[i].GetComponentInChildren<Text>().text = labels[i];
                items[i].GetComponent<Image>().color = i == selection ? new Color(.4f, .3f, .17f) : new Color(.23f, .18f, .12f);
                items[i].interactable = enabled;
            }
        }
        public void SetCommand(string id, bool available, bool visible = true, string label = null)
        {
            var button = commands[id]; button.gameObject.SetActive(visible); button.interactable = available;
            if (label != null) button.GetComponentInChildren<Text>().text = label;
        }
        public void SetVisible(bool value) { if (canvas == null) return; canvas.enabled = value; if (value && safeArea != null) UpdateSafeArea(); }
        public void SetInputReady(bool ready) { if (group == null) return; group.interactable = ready; group.blocksRaycasts = ready; }
        public void SetLargeText(bool value)
        {
            largeText = value;
            foreach (var text in GetComponentsInChildren<Text>(true))
            {
                if (!baseSizes.TryGetValue(text, out var size)) { size = text.fontSize; baseSizes.Add(text, size); }
                text.fontSize = value ? Mathf.RoundToInt(size * 1.2f) : size;
            }
        }
        public void SelectFirst(EventSystem events)
        { foreach (var button in commands.Values) if (button.gameObject.activeInHierarchy && button.interactable) { events?.SetSelectedGameObject(button.gameObject); return; } }
        public void ArrangeCommands()
        {
            var row = 0;
            foreach (var pair in commands)
            {
                if (pair.Key == "Source" || !pair.Value.gameObject.activeSelf) continue;
                Place(pair.Value.GetComponent<RectTransform>(), new Vector2(520, -(recipeLayout ? 380 : 320) - row++ * 40), new Vector2(445, 36));
            }
        }
        private void Update() { if (IsVisible && lastSafeArea != Screen.safeArea) UpdateSafeArea(); }
        private void UpdateSafeArea()
        {
            var area = Screen.safeArea; lastSafeArea = area;
            safeArea.anchorMin = new Vector2(area.xMin / Mathf.Max(1, Screen.width), area.yMin / Mathf.Max(1, Screen.height));
            safeArea.anchorMax = new Vector2(area.xMax / Mathf.Max(1, Screen.width), area.yMax / Mathf.Max(1, Screen.height));
            safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
        }
        private static RectTransform Rect(string name, Transform parent)
        { var value = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); value.SetParent(parent, false); return value; }
        private static void Place(RectTransform rect, Vector2 position, Vector2 size)
        { rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1); rect.anchoredPosition = position; rect.sizeDelta = size; }
        private static Text Text(string name, Transform parent, Vector2 position, Vector2 size, int fontSize)
        {
            var rect = Rect(name, parent); Place(rect, position, size); var text = rect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = fontSize; text.color = new Color(.94f, .86f, .72f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate; text.raycastTarget = false; return text;
        }
        private static Button Button(string name, string label, Transform parent, Action clicked)
        {
            var rect = Rect(name, parent); var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(.23f, .18f, .12f);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(() => clicked());
            var text = Text("Label", rect, Vector2.zero, Vector2.zero, 18); text.alignment = TextAnchor.MiddleCenter;
            text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one; text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
            text.text = label; return button;
        }
    }
}
