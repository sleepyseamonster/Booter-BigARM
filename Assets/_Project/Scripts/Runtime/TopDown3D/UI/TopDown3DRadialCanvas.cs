using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;

namespace BooterBigArm.TopDown3D
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class TopDown3DRadialCanvas : MonoBehaviour
    {
        private Canvas canvas;
        private RectTransform safe;
        private GameObject wheelPanel, editorPanel, detailBacking;
        private RectTransform wheel;
        private TopDown3DRadialRingGraphic ring;
        private Text pageTitle, detail, hints;
        private readonly Text[] labels = new Text[8];
        public RectTransform Wheel => wheel;
        public bool IsVisible => canvas != null && canvas.enabled;

        private static readonly Color Ink = new Color32(242, 234, 221, 255);
        private static readonly Color Back = new Color32(27, 29, 28, 255);

        public void Build()
        {
            canvas = gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 240;
            var scaler = gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();
            safe = Rect("Safe Area", transform, Vector2.zero, Vector2.zero);
            safe.anchorMin = Vector2.zero; safe.anchorMax = Vector2.one; safe.offsetMin = safe.offsetMax = Vector2.zero;
            wheelPanel = Rect("Field Menu", safe, Vector2.zero, new Vector2(650, 680)).gameObject;
            wheel = Rect("Compass Ring", wheelPanel.transform, new Vector2(0, 25), new Vector2(420, 420));
            ring = wheel.gameObject.AddComponent<TopDown3DRadialRingGraphic>(); ring.color = new Color32(48, 52, 49, 255); ring.raycastTarget = false;
            Rect("Page Backing", wheelPanel.transform, new Vector2(0, 285), new Vector2(460, 36)).gameObject.AddComponent<Image>().color = Back;
            pageTitle = Label("Page", wheelPanel.transform, new Vector2(0, 285), new Vector2(460, 36), "FIELD", 20);
            detailBacking = Rect("Detail Backing", wheelPanel.transform, new Vector2(0, -240), new Vector2(620, 65)).gameObject;
            detailBacking.AddComponent<Image>().color = Back;
            detail = Label("Detail", wheelPanel.transform, new Vector2(0, -240), new Vector2(620, 65), "", 23);
            Rect("Hint Backing", wheelPanel.transform, new Vector2(0, -315), new Vector2(640, 55)).gameObject.AddComponent<Image>().color = Back;
            hints = Label("Hints", wheelPanel.transform, new Vector2(0, -315), new Vector2(640, 55), "", 18);
            for (var i = 0; i < labels.Length; i++) labels[i] = Label("Entry " + i, wheel, Vector2.zero, new Vector2(105, 62), "", 20);
            SetVisible(false);
        }

        public void Refresh(TopDown3DRadialPage page, int selected, string description, string bindings, float scale, bool explicitConfirm = false, int canisterCount = 0)
        {
            wheelPanel.SetActive(true); if (editorPanel != null) editorPanel.SetActive(false);
            wheelPanel.transform.localScale = Vector3.one * scale;
            if (ring.Sectors != page.slots.Length || ring.Selected != selected)
            { ring.Sectors = page.slots.Length; ring.Selected = selected; ring.SetVerticesDirty(); }
            pageTitle.text = page.name.ToUpperInvariant();
            detailBacking.SetActive(selected >= 0);
            detail.gameObject.SetActive(selected >= 0);
            detail.text = description; hints.text = bindings;
            for (var i = 0; i < labels.Length; i++)
            {
                labels[i].gameObject.SetActive(i < page.slots.Length);
                if (i >= page.slots.Length) continue;
                labels[i].rectTransform.anchoredPosition = TopDown3DRadialGeometry.Point(145f, i * 360f / page.slots.Length);
                labels[i].text = Name(page.slots[i]) + (page.slots[i] == TopDown3DRadialCommand.DustCanister ? " ×" + canisterCount : ""); labels[i].color = i == selected ? ring.Accent : Ink;
                labels[i].fontSize = page.slots.Length == 8 ? 17 : 20;
                labels[i].rectTransform.sizeDelta = page.slots.Length == 8 ? new Vector2(90, 66) : new Vector2(105, 62);
            }
        }

        public void SetVisible(bool show) { canvas.enabled = show; GetComponent<GraphicRaycaster>().enabled = show; }

        private void Update()
        {
            if (safe == null || Screen.width < 1 || Screen.height < 1) return;
            var area = Screen.safeArea;
            safe.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            safe.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
        }

        public static string Name(TopDown3DRadialCommand command) => command switch {
            TopDown3DRadialCommand.PlayerInventory => "Inventory",
            TopDown3DRadialCommand.CallLegger => "Call Legger",
            TopDown3DRadialCommand.DustCanister => "Dust Canister",
            TopDown3DRadialCommand.AutoPack => "Auto-Pack",
            _ => "—" };

        public void ShowEditor(TopDown3DRadialPreferences draft, int page, int sector,
            Action<int> selectPage, Action<int> selectSector, Action<TopDown3DRadialCommand> assign,
            Action add, Action remove, Action resize, Action apply, Action cancel, Action settings, Action scale, string message)
        {
            var selectedName = editorPanel != null && EventSystem.current != null
                && EventSystem.current.currentSelectedGameObject != null
                && EventSystem.current.currentSelectedGameObject.transform.IsChildOf(editorPanel.transform)
                ? EventSystem.current.currentSelectedGameObject.name : null;
            if (editorPanel != null) { editorPanel.SetActive(false); Destroy(editorPanel); }
            wheelPanel.SetActive(false);
            editorPanel = Rect("Customize Radial", safe, Vector2.zero, new Vector2(920, 690)).gameObject;
            var bg = editorPanel.AddComponent<Image>(); bg.color = Back;
            Label("Title", editorPanel.transform, new Vector2(0, 305), new Vector2(850, 40), "CUSTOMIZE RADIAL", 26);
            var first = Button("Previous page", editorPanel.transform, new Vector2(-340, 245), () => selectPage((page + draft.pages.Length - 1) % draft.pages.Length));
            Button("Next page", editorPanel.transform, new Vector2(-130, 245), () => selectPage((page + 1) % draft.pages.Length));
            Button("Add page", editorPanel.transform, new Vector2(100, 245), add).interactable = draft.pages.Length < 4;
            Button("Delete page", editorPanel.transform, new Vector2(310, 245), remove).interactable = draft.pages.Length > 1;
            var nameRect = Rect("Page Name", editorPanel.transform, new Vector2(0, 180), new Vector2(750, 42));
            nameRect.gameObject.AddComponent<Image>().color = new Color32(48, 52, 49, 255);
            var nameText = Label("Text", nameRect, Vector2.zero, new Vector2(720, 40), draft.pages[page].name, 20);
            var input = nameRect.gameObject.AddComponent<InputField>(); input.textComponent = nameText; input.characterLimit = 24; input.text = draft.pages[page].name;
            input.onEndEdit.AddListener(value => draft.pages[page].name = string.IsNullOrWhiteSpace(value) ? "Field" : value.Trim());
            var p = draft.pages[page];
            for (var i = 0; i < p.slots.Length; i++)
            {
                var index = i;
                var pos = p.slots.Length == 8 ? new Vector2(-305 + i % 2 * 160, 110 - i / 2 * 66)
                    : new Vector2(-225, -15) + TopDown3DRadialGeometry.Point(120, i * 360f / p.slots.Length);
                var compass = p.slots.Length == 8 ? new[] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" }[i]
                    : new[] { "N", "E", "S", "W" }[i];
                var button = Button(compass + ": " + Name(p.slots[i]), editorPanel.transform, pos, () => selectSector(index), new Vector2(140, 52));
                button.GetComponent<Image>().color = index == sector ? new Color32(95, 75, 46, 255) : new Color32(48, 52, 49, 255);
            }
            var values = (TopDown3DRadialCommand[])Enum.GetValues(typeof(TopDown3DRadialCommand));
            for (var i = 0; i < values.Length; i++) { var command = values[i]; Button(command == TopDown3DRadialCommand.Empty ? "Clear slot" : Name(command), editorPanel.transform, new Vector2(220, 90 - i * 53), () => assign(command)); }
            Button(p.slots.Length == 4 ? "8 sectors" : "4 sectors", editorPanel.transform, new Vector2(-225, -205), resize);
            Button(draft.toggleOpen ? "Toggle + Confirm" : draft.explicitConfirm ? "Hold + Confirm" : "Hold + Stick Release", editorPanel.transform, new Vector2(220, -205), settings);
            Button("Scale " + draft.scale.ToString("0.00"), editorPanel.transform, new Vector2(0, -205), scale);
            Label("Message", editorPanel.transform, new Vector2(0, -258), new Vector2(850, 48), message, 18);
            Button("Cancel / Back", editorPanel.transform, new Vector2(-125, -310), cancel);
            Button("Apply layout", editorPanel.transform, new Vector2(125, -310), apply);
            SetVisible(true);
            var retained = editorPanel.GetComponentsInChildren<Button>().FirstOrDefault(b => b.name == selectedName);
            EventSystem.current?.SetSelectedGameObject(retained != null ? retained.gameObject : first.gameObject);
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f); rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
        }
        private static Text Label(string name, Transform parent, Vector2 position, Vector2 size, string text, int fontSize)
        {
            var label = Rect(name, parent, position, size).gameObject.AddComponent<Text>(); label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = text; label.supportRichText = false; label.fontSize = fontSize; label.color = Ink; label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false; return label;
        }
        private static Button Button(string text, Transform parent, Vector2 position, Action pressed, Vector2? size = null)
        {
            var rect = Rect(text, parent, position, size ?? new Vector2(195, 44)); rect.gameObject.AddComponent<Image>().color = new Color32(48, 52, 49, 255);
            var button = rect.gameObject.AddComponent<Button>(); button.onClick.AddListener(() => pressed());
            Label("Label", rect, Vector2.zero, rect.sizeDelta, text, 18); return button;
        }
    }
}
