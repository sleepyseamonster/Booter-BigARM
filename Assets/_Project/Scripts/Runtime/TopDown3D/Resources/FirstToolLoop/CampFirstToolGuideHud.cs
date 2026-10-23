using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Scenario-local read-only discovery and camp guidance; never owns quest or inventory state.</summary>
    [DisallowMultipleComponent]
    public sealed class CampFirstToolGuideHud : MonoBehaviour
    {
        private CampFirstToolScenario scenario;
        private TopDown3DCampLoopEconomySession session;
        private Camera viewCamera;
        private Canvas canvas;
        private RectTransform surface;
        private Text summary;
        private TopDown3DMenuController menu;
        private Text[] markers;
        private Vector3 destination;
        private string destinationName;
        private CampFirstToolSource nextSource;
        private float nextRefresh;
        private Rect lastSafeArea;
        private TopDown3DInventoryState[] observed;
        private readonly Vector3[] panelCorners = new Vector3[4];
        public string Summary => summary != null ? summary.text : "";
        public bool Visible => canvas != null && canvas.enabled;
        public int VisibleMarkerCount => markers == null ? 0 : markers.Count(m => m.gameObject.activeSelf);
        private void OnEnable() { if (canvas == null) Build(); }
        private void Build()
        {
            scenario = GetComponent<CampFirstToolScenario>();
            if (scenario == null) { enabled = false; return; }
            session = TopDown3DGameHudCanvas.FindInScene<TopDown3DCampLoopEconomySession>(gameObject.scene);
            viewCamera = TopDown3DGameHudCanvas.FindInScene<Camera>(gameObject.scene);
            menu = TopDown3DGameHudCanvas.FindInScene<TopDown3DMenuController>(gameObject.scene);
            var retained = transform.Find("First tool route guidance");
            if (retained != null) { retained.gameObject.SetActive(false); if (Application.isPlaying) Destroy(retained.gameObject); else DestroyImmediate(retained.gameObject); }
            var root = new GameObject("First tool route guidance", typeof(RectTransform)); root.transform.SetParent(transform, false);
            canvas = root.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 125;
            canvas.enabled = false;
            var scaler = root.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            var group = root.AddComponent<CanvasGroup>(); group.interactable = false; group.blocksRaycasts = false;
            surface = new GameObject("Safe area", typeof(RectTransform)).GetComponent<RectTransform>(); surface.SetParent(root.transform, false);
            var panelRect = new GameObject("First tool checklist", typeof(RectTransform)).GetComponent<RectTransform>(); panelRect.SetParent(surface, false);
            panelRect.sizeDelta = new Vector2(550, 245); panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = Vector2.one;
            panelRect.anchoredPosition = new Vector2(-24, -24);
            var panel = panelRect.gameObject.AddComponent<Image>(); panel.color = new Color(.055f, .045f, .035f, .94f); panel.raycastTarget = false;
            summary = Label("Checklist text", panelRect, new Vector2(520, 225), 22); summary.alignment = TextAnchor.UpperLeft;
            summary.rectTransform.anchorMin = summary.rectTransform.anchorMax = summary.rectTransform.pivot = new Vector2(0, 1);
            summary.rectTransform.anchoredPosition = new Vector2(15, -10);
            markers = new Text[scenario.Sources.Count + 1];
            for (var i = 0; i < markers.Length; i++) markers[i] = Label("Resource route marker " + i, surface, new Vector2(260, 50), 21);
            UpdateSafeArea();
        }
        private static Text Label(string name, Transform parent, Vector2 size, int fontSize)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false); rect.sizeDelta = size;
            var label = rect.gameObject.AddComponent<Text>(); label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize; label.color = new Color(1, .93f, .74f); label.raycastTarget = false;
            label.alignment = TextAnchor.MiddleCenter; label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate;
            var shadow = rect.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(0, 0, 0, .95f); shadow.effectDistance = new Vector2(2, -2);
            return label;
        }
        private void UpdateSafeArea()
        {
            lastSafeArea = Screen.safeArea;
            surface.anchorMin = new Vector2(lastSafeArea.xMin / Mathf.Max(1, Screen.width), lastSafeArea.yMin / Mathf.Max(1, Screen.height));
            surface.anchorMax = new Vector2(lastSafeArea.xMax / Mathf.Max(1, Screen.width), lastSafeArea.yMax / Mathf.Max(1, Screen.height));
            surface.offsetMin = surface.offsetMax = Vector2.zero;
        }
        private string Bearing(Vector3 target)
        {
            var delta = target - session.Kit.transform.position; var angle = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
            var names = new[] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
            return names[Mathf.RoundToInt(Mathf.Repeat(angle, 360) / 45) % 8] + " · " + Mathf.CeilToInt(new Vector2(delta.x, delta.z).magnitude) + " m";
        }
        private void RefreshSummary()
        {
            var progress = CampFirstToolProgress.For(scenario, session); destinationName = null; nextSource = null;
            if (!progress.Valid) { summary.text = "FIRST MULTI-TOOL\nGuidance unavailable. Current items are preserved."; return; }
            if (progress.Complete) { summary.text = "MULTI-TOOL COMPLETE\nTool equipped on Booter.\nInventory → Reset Inventory Trial to replay."; return; }
            if (progress.MissingRaw.Count > 0)
            {
                var source = scenario.Sources.Where(s => s.IsAvailable && progress.MissingRaw.ContainsKey(s.Reward.ItemId))
                    .OrderBy(s => (s.InteractionPoint - session.Kit.transform.position).sqrMagnitude).FirstOrDefault();
                summary.text = "FIRST MULTI-TOOL · GATHER\nNeed: " + string.Join(", ", progress.MissingRaw.Select(p => p.Value + " " + CampFirstToolProgress.ShortName(p.Key))) + "\n";
                if (source == null) { summary.text += "No matching stock remains.\nInventory → Reset Inventory Trial restores the loop."; return; }
                destination = source.InteractionPoint; destinationName = CampFirstToolProgress.ShortName(source.Reward.ItemId);
                nextSource = source;
                summary.text += "Next: " + destinationName + " · " + Bearing(destination) + "\n[" + session.Input.GetBindingDisplayName("Gameplay/Interact", "Interact")
                    + "] Gather at the marked work face.\nBring Legger; no attacks or tool required.";
                return;
            }
            var cargoMaterials = session.Cargo.State.Slots.Any(s => !s.IsEmpty && scenario.Recipes.Any(r => r.Ingredients.Any(i => i.itemId == s.ItemId)));
            destination = cargoMaterials ? session.Cache.InteractionPoint : session.Workbench.InteractionPoint;
            destinationName = cargoMaterials ? "CAMP CHEST" : "WORKBENCH";
            var next = progress.Next(session.Cache.State, session.Kit.State);
            summary.text = "FIRST MULTI-TOOL · " + (cargoMaterials ? "RETURN / TRANSFER" : "CRAFT") + "\n" + destinationName + " · " + Bearing(destination) + "\n"
                + (cargoMaterials ? "Return with Legger. At the chest:\nLEGGER → CAMP CHEST; transfer each stack."
                    : "Next: " + (next >= 0 ? CampFirstToolProgress.ShortName(scenario.Recipes[next].Output.ItemId) : "inspect materials") + "\nSelect READY steps; parts return to the chest.\nThe final tool equips on Booter.");
        }
        private void LateUpdate()
        {
            if (canvas == null) return;
            canvas.enabled = scenario.Ready && session != null && session.Ready && session.Input.Mode == TopDown3DInputMode.Gameplay;
            if (!canvas.enabled) return;
            if (observed == null)
            {
                observed = new[] { session.Kit.State, session.Cargo.State, session.Cache.State };
                foreach (var state in observed) state.Changed += Invalidate;
                Invalidate();
            }
            summary.fontSize = menu != null && menu.LargeText ? 26 : 22;
            if (lastSafeArea != Screen.safeArea) UpdateSafeArea();
            if (Time.unscaledTime >= nextRefresh) { nextRefresh = Time.unscaledTime + .25f; RefreshSummary(); }
            if (viewCamera == null) return;
            for (int i = 0; i < scenario.Sources.Count; i++)
            {
                var source = scenario.Sources[i]; var distance = Vector3.Distance(session.Kit.transform.position, source.InteractionPoint);
                var show = source != nextSource && source.isActiveAndEnabled && (source.IsAvailable && distance <= 40 || source.Harvested && distance <= 7);
                PlaceMarker(markers[i], source.InteractionPoint + Vector3.up * .8f,
                    CampFirstToolProgress.ShortName(source.Reward.ItemId) + (source.Harvested ? " · GATHERED" : " ×" + source.Reward.Quantity + " · " + Mathf.CeilToInt(distance) + " m"), show);
                markers[i].color = source.Harvested ? new Color(.65f, .65f, .6f) : new Color(1, .93f, .74f);
            }
            PlaceMarker(markers[^1], destination + Vector3.up, "NEXT · " + destinationName + "\n" + Bearing(destination), destinationName != null, true);
        }
        private void PlaceMarker(Text label, Vector3 point, string caption, bool show, bool edgeGuide = false)
        {
            var screen = viewCamera.WorldToScreenPoint(point);
            ((RectTransform)summary.transform.parent).GetWorldCorners(panelCorners);
            var bottom = RectTransformUtility.WorldToScreenPoint(null, panelCorners[0]);
            var top = RectTransformUtility.WorldToScreenPoint(null, panelCorners[2]);
            var panel = new Rect(bottom, top - bottom);
            var size = label.rectTransform.sizeDelta * canvas.scaleFactor;
            bool OverlapsPanel() => panel.Overlaps(new Rect(new Vector2(screen.x, screen.y) - size * .5f, size));
            if (edgeGuide)
            {
                var outside = screen.z <= 0 || !Screen.safeArea.Contains(new Vector2(screen.x, screen.y));
                if (screen.z < 0) { screen.x = Screen.width - screen.x; screen.y = Screen.height - screen.y; }
                var scale = canvas.scaleFactor;
                screen.x = Mathf.Clamp(screen.x, Screen.safeArea.xMin + 140 * scale, Screen.safeArea.xMax - 140 * scale);
                screen.y = Mathf.Clamp(screen.y, Screen.safeArea.yMin + 40 * scale, Screen.safeArea.yMax - 40 * scale);
                if (OverlapsPanel()) { screen.y = panel.yMin - 35 * scale; outside = true; }
                if (outside) caption = "NEXT (OUT OF VIEW)\n" + destinationName + " · " + Bearing(destination);
            }
            else
            {
                show &= screen.z > 0 && Screen.safeArea.Contains(new Vector2(screen.x, screen.y));
                show &= !OverlapsPanel();
            }
            label.gameObject.SetActive(show); if (!show) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(surface, screen, null, out var local);
            label.rectTransform.anchoredPosition = local; label.text = caption;
        }
        private void Invalidate() => nextRefresh = float.NegativeInfinity;
        private void OnDisable()
        {
            if (canvas != null) canvas.enabled = false;
            if (observed != null) foreach (var state in observed) state.Changed -= Invalidate;
            observed = null;
        }
    }
}
