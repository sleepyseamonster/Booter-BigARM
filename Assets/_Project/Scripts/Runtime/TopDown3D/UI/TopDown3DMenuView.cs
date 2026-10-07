using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BooterBigArm.TopDown3D
{
    // The hierarchy is authored in SystemMenu.prefab. Runtime updates never reconstruct controls.
    public sealed class TopDown3DMenuView : MonoBehaviour
    {
        private readonly Dictionary<string, Button> buttons = new();
        private readonly Dictionary<string, Text> texts = new();
        private readonly Dictionary<string, GameObject> panels = new();
        private readonly Dictionary<Text, int> fontSizes = new();
        private CanvasGroup group;
        private RectTransform safe;
        private EventSystem eventSystem;
        private InputField pageName;
        private float fade;
        private bool reducedMotion, large;
        private string activePanel;
        private GridLayoutGroup columns;
        private int selectedSlot;
        private readonly List<Selectable> activeControls = new();
        public event Action<string> Command;
        public event Action<string> Rename;
        public bool IsEditingText => pageName != null && pageName.isFocused;
        public string ActivePanel => activePanel;

        public void Initialize(EventSystem system)
        {
            eventSystem = system;
            group = GetComponent<CanvasGroup>();
            safe = transform.Find("Safe Area") as RectTransform;
            columns = safe.Find("Setup/SetupViewport/SetupContent/SetupColumns").GetComponent<GridLayoutGroup>();
            foreach (var panel in new[] { "Root", "Settings", "Setup", "Dialog" })
                panels[panel] = safe.Find(panel).gameObject;
            foreach (var button in GetComponentsInChildren<Button>(true))
            {
                buttons.Add(button.name, button);
                var key = button.name;
                button.onClick.AddListener(() => Command?.Invoke(key));
            }
            foreach (var label in GetComponentsInChildren<Text>(true))
            { if (!texts.ContainsKey(label.name)) texts.Add(label.name, label); fontSizes[label] = label.fontSize; }
            pageName = GetComponentInChildren<InputField>(true);
            pageName.onEndEdit.AddListener(value => Rename?.Invoke(value));
            Show(null);
        }

        public void Show(string panel, string focus = null)
        {
            activePanel = panel;
            foreach (var entry in panels) if (entry.Value != null) entry.Value.SetActive(entry.Key == panel);
            if (group == null) return;
            group.alpha = panel == null ? 0 : (reducedMotion ? 1 : 0);
            group.blocksRaycasts = panel != null;
            group.interactable = panel != null;
            fade = panel == null ? 0 : 1;
            if (eventSystem != null) eventSystem.SetSelectedGameObject(null);
            if (focus != null && buttons.TryGetValue(focus, out var button))
                if (eventSystem != null && button != null) eventSystem.SetSelectedGameObject(button.gameObject);
            RefreshNavigation();
        }

        public void SetInputReady(bool ready) => group.interactable = ready && activePanel != null;
        public void SetText(string key, string value)
        {
            if (texts.TryGetValue(key, out var label) && label != null) label.text = value;
            else if (buttons.TryGetValue(key, out var button)) button.GetComponentInChildren<Text>().text = value;
        }
        public void SetButton(string key, bool enabled)
        { if (buttons.TryGetValue(key, out var button)) button.interactable = enabled; }
        public void SetActive(string key, bool enabled)
        { var target = Find(key); if (target != null) target.SetActive(enabled); }
        private GameObject Find(string name)
        {
            foreach (var child in GetComponentsInChildren<Transform>(true)) if (child.name == name) return child.gameObject;
            return null;
        }
        public void SetPageName(string name) => pageName.SetTextWithoutNotify(name);
        public void EndTextEdit() { pageName.DeactivateInputField(); eventSystem?.SetSelectedGameObject(buttons["SetupBack"].gameObject); }
        public string FocusKey => eventSystem != null && eventSystem.currentSelectedGameObject != null
            && eventSystem.currentSelectedGameObject.transform.IsChildOf(transform) ? eventSystem.currentSelectedGameObject.name : null;

        public void ApplySettings(TopDown3DMenuSettings settings)
        {
            large = settings.largeText; reducedMotion = settings.reducedMotion;
            foreach (var entry in fontSizes) if (entry.Key != null) entry.Key.fontSize = Mathf.RoundToInt(entry.Value * (large ? 1.25f : 1f));
            Reflow();
        }

        public void DrawSlots(TopDown3DRadialPage page, int selected)
        {
            selectedSlot = selected;
            var directions = page.slots.Length == 8 ? new[] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" } : new[] { "N", "E", "S", "W" };
            for (var i = 0; i < 8; i++)
            {
                var button = buttons["Slot" + i]; button.gameObject.SetActive(i < page.slots.Length);
                if (i >= page.slots.Length) continue;
                var rect = (RectTransform)button.transform;
                rect.anchoredPosition = TopDown3DRadialGeometry.Point(155, i * 360f / page.slots.Length);
                rect.sizeDelta = new Vector2(100, 94);
                var shortName = page.slots[i] switch { TopDown3DRadialCommand.PlayerInventory => "Kit",
                    TopDown3DRadialCommand.CallLegger => "Call", TopDown3DRadialCommand.DustCanister => "Dust",
                    TopDown3DRadialCommand.AutoPack => "Pack", _ => "—" };
                var label = button.GetComponentInChildren<Text>();
                label.text = directions[i] + "\n" + shortName; label.fontSize = large ? 28 : 22;
                button.GetComponent<Image>().color = i == selected ? new Color32(95, 75, 46, 255) : new Color32(48, 52, 49, 255);
            }
            RefreshNavigation();
        }

        public void RefreshNavigation()
        {
            activeControls.Clear();
            if (activePanel == null) return;
            if (panels[activePanel] == null) return;
            foreach (var control in panels[activePanel].GetComponentsInChildren<Selectable>())
                if (control.interactable && !(control is Scrollbar)) activeControls.Add(control);
            for (var i = 0; i < activeControls.Count; i++)
            {
                var control = activeControls[i];
                var navigation = new Navigation { mode = Navigation.Mode.Explicit,
                    selectOnUp = activeControls[(i + activeControls.Count - 1) % activeControls.Count],
                    selectOnDown = activeControls[(i + 1) % activeControls.Count] };
                navigation.selectOnLeft = navigation.selectOnUp; navigation.selectOnRight = navigation.selectOnDown;
                if (activePanel == "Setup" && control.name.StartsWith("Slot", StringComparison.Ordinal)) navigation.selectOnRight = buttons["Assign1"];
                if (activePanel == "Setup" && control.name.StartsWith("Assign", StringComparison.Ordinal)) navigation.selectOnLeft = buttons["Slot" + selectedSlot];
                control.navigation = navigation;
            }
        }

        private void Reflow()
        {
            if (safe == null) return;
            var area = Screen.safeArea;
            if (Screen.width > 0 && Screen.height > 0)
            { safe.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height); safe.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height); }
            ReflowForViewport(Screen.width, Screen.height);
        }

        public void ReflowForViewport(int widthPixels, int heightPixels)
        {
            if (columns == null) return;
            var stacked = large || (float)widthPixels / Mathf.Max(1, heightPixels) < 1.5f;
            var count = stacked ? 1 : 2;
            columns.constraintCount = count;
            var width = ((RectTransform)columns.transform).rect.width;
            columns.cellSize = new Vector2(Mathf.Max(350, (width - (count - 1) * columns.spacing.x) / count), large ? 440 : 420);
        }

        private void Update()
        {
            Reflow();
            if (fade > 0 && group.alpha < 1) group.alpha = Mathf.Min(1, group.alpha + Time.unscaledDeltaTime / 0.08f);
        }
    }
}
