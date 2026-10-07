using UnityEngine;
using UnityEngine.UI;

namespace BooterBigArm.TopDown3D
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class TopDown3DRadialCanvas : MonoBehaviour
    {
        private Canvas canvas;
        private RectTransform safe;
        private GameObject wheelPanel, detailBacking;
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
            wheelPanel.SetActive(true);
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
    }
}
