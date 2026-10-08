using System;
using System.IO;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BooterBigArm.Editor
{
    public static class TopDown3DMenuPrefabAuthoring
    {
        public const string PrefabPath = "Assets/_Project/Prefabs/UI/Resources/SystemMenu.prefab";
        private static readonly Color32 Ink = new(242, 234, 221, 255);
        private static readonly Color32 Back = new(27, 29, 28, 255);

        public static void CreateFromCli()
        { WritePrefab(false); }

        // Explicitly mutating, for a reviewed task-owned prefab update; preserves the asset GUID.
        public static void UpdateOwnedFromCli()
        { WritePrefab(true); }

        private static void WritePrefab(bool overwrite)
        {
            if (!overwrite && File.Exists(PrefabPath)) throw new InvalidOperationException("Menu prefab exists; refusing to overwrite.");
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath)); AssetDatabase.Refresh();
            var root = new GameObject("System Menu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(TopDown3DMenuView));
            try
            {
                root.SetActive(false);
                var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 300;
                var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
                var safe = Rect("Safe Area", root.transform); Stretch(safe);
                var dim = Rect("Dim", safe); Stretch(dim); dim.gameObject.AddComponent<Image>().color = new Color32(3, 4, 5, 170);
                PanelRoot(safe); PanelSettings(safe); PanelSetup(safe); PanelDialog(safe);
                var hints = Label("Hints", safe, "", 18); hints.rectTransform.anchorMin = hints.rectTransform.anchorMax = new Vector2(0.5f, 0);
                hints.rectTransform.anchoredPosition = new Vector2(0, 32); hints.rectTransform.sizeDelta = new Vector2(900, 44);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static RectTransform Panel(string name, Transform parent, Vector2 size)
        {
            var rect = Rect(name, parent); rect.sizeDelta = size;
            rect.gameObject.AddComponent<Image>().color = Back;
            var outline = rect.gameObject.AddComponent<Outline>(); outline.effectColor = new Color32(95, 86, 65, 255); outline.effectDistance = new Vector2(1, -1);
            return rect;
        }

        private static void PanelRoot(Transform parent)
        {
            var panel = Panel("Root", parent, new Vector2(480, 430));
            Vertical(panel, 12, new RectOffset(32, 32, 28, 28));
            Label("RootTitle", panel, "MENU", 28, 46);
            Button("Resume", panel, "Resume"); Button("RadialSetup", panel, "Radial Setup");
            Button("Settings", panel, "Settings"); Button("Exit", panel, "Exit Game");
        }

        private static void PanelSettings(Transform parent)
        {
            var panel = Panel("Settings", parent, new Vector2(620, 540));
            Vertical(panel, 14, new RectOffset(32, 32, 28, 28));
            Label("SettingsTitle", panel, "SETTINGS", 28, 46);
            Button("TextSize", panel, "Menu text: Normal"); Button("Motion", panel, "Menu motion: Normal");
            Height(Button("ViewDistance", panel, "Maximum view distance\nMaximum - 8,000 m"), 90);
            Label("SettingsNotice", panel, "Changes are saved with Apply.", 19, 72);
            Footer(panel, "SettingsApply", "SettingsBack");
        }

        private static void PanelDialog(Transform parent)
        {
            var panel = Panel("Dialog", parent, new Vector2(640, 360));
            Vertical(panel, 18, new RectOffset(32, 32, 32, 32));
            Label("DialogTitle", panel, "Confirm", 28, 58);
            Label("DialogMessage", panel, "", 22, 112);
            var footer = Rect("DialogFooter", panel); Horizontal(footer, 16); Height(footer, 58);
            Button("DialogCancel", footer, "Keep Playing"); Button("DialogAccept", footer, "Confirm");
        }

        private static void PanelSetup(Transform parent)
        {
            var panel = Panel("Setup", parent, new Vector2(940, 850));
            var title = Label("SetupTitle", panel, "RADIAL SETUP", 28); title.rectTransform.anchoredPosition = new Vector2(0, 374); title.rectTransform.sizeDelta = new Vector2(800, 52);
            var viewport = Rect("SetupViewport", panel); viewport.anchorMin = new Vector2(0, 0); viewport.anchorMax = new Vector2(1, 1);
            viewport.offsetMin = new Vector2(28, 130); viewport.offsetMax = new Vector2(-28, -96);
            viewport.gameObject.AddComponent<Image>().color = Back; viewport.gameObject.AddComponent<RectMask2D>();
            var content = Rect("SetupContent", viewport); content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1); content.sizeDelta = Vector2.zero;
            Vertical(content, 18, new RectOffset(8, 8, 8, 18));
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.viewport = viewport; scroll.content = content;
            scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
            var track = Rect("SetupScrollbar", panel); track.anchorMin = new Vector2(1, 0); track.anchorMax = Vector2.one;
            track.offsetMin = new Vector2(-24, 130); track.offsetMax = new Vector2(-10, -96);
            track.gameObject.AddComponent<Image>().color = new Color32(48, 52, 49, 255);
            var handle = Rect("ScrollHandle", track); Stretch(handle); var handleImage = handle.gameObject.AddComponent<Image>(); handleImage.color = new Color32(197, 173, 128, 255);
            var scrollbar = track.gameObject.AddComponent<Scrollbar>(); scrollbar.handleRect = handle; scrollbar.targetGraphic = handleImage;
            scrollbar.direction = Scrollbar.Direction.BottomToTop; scroll.verticalScrollbar = scrollbar;
            var scrollNavigation = scrollbar.navigation; scrollNavigation.mode = Navigation.Mode.None; scrollbar.navigation = scrollNavigation;
            var pages = Rect("PageControls", content); Horizontal(pages, 12); Height(pages, 72);
            Button("PreviousPage", pages, "Previous"); Label("PageNumber", pages, "Field\n1 / 1", 22, 72); Button("NextPage", pages, "Next");
            var columns = Rect("SetupColumns", content);
            var grid = columns.gameObject.AddComponent<GridLayoutGroup>(); grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2; grid.cellSize = new Vector2(400, 420); grid.spacing = new Vector2(24, 20);
            var compass = Rect("CompassPreview", columns);
            for (var i = 0; i < 8; i++)
            {
                var button = Button("Slot" + i, compass, "Direction");
                var rect = (RectTransform)button.transform; rect.sizeDelta = new Vector2(115, 68);
                UnityEngine.Object.DestroyImmediate(button.GetComponent<LayoutElement>());
                rect.anchoredPosition = TopDown3DRadialGeometry.Point(124, i * 45f);
                button.GetComponentInChildren<Text>().fontSize = 17;
            }
            var choices = Rect("Assignments", columns); Vertical(choices, 10, new RectOffset());
            Label("SelectedDirection", choices, "Selected: Inventory", 22, 64);
            for (var i = 0; i < 5; i++) Button("Assign" + i, choices, i == 0 ? "Clear slot" : TopDown3DRadialCanvas.Name((TopDown3DRadialCommand)i));
            Button("More", content, "More Options");
            var advanced = Rect("Advanced", content); Vertical(advanced, 12, new RectOffset());
            var nameRow = Rect("NameRow", advanced); Horizontal(nameRow, 12); Height(nameRow, 58);
            Label("NameLabel", nameRow, "Page name", 20, 58);
            var nameRect = Rect("PageName", nameRow); nameRect.gameObject.AddComponent<Image>().color = new Color32(48, 52, 49, 255); Height(nameRect, 58);
            var nameText = Label("NameText", nameRect, "Field", 22); Stretch(nameText.rectTransform); nameText.rectTransform.offsetMin = new Vector2(10, 5); nameText.rectTransform.offsetMax = new Vector2(-10, -5);
            var name = nameRect.gameObject.AddComponent<InputField>(); name.textComponent = nameText; name.characterLimit = 24;
            Button("AddPage", advanced, "Add page"); Button("DeletePage", advanced, "Delete page");
            Button("DefaultPage", advanced, "Make default page"); Button("SectorCount", advanced, "4 directions");
            Button("Style", advanced, "Hold + Stick Release"); Button("WheelScale", advanced, "Wheel scale: 1.00");
            Label("RenameHint", advanced, "Page names can be edited with a keyboard. All other setup controls work with a gamepad.", 18, 80);
            var notice = Label("SetupNotice", panel, "Choose a direction, then an action.", 18); notice.rectTransform.anchoredPosition = new Vector2(0, -326); notice.rectTransform.sizeDelta = new Vector2(880, 52);
            var footer = Rect("SetupFooter", panel); footer.anchoredPosition = new Vector2(0, -382); footer.sizeDelta = new Vector2(600, 58); Horizontal(footer, 18);
            Button("SetupApply", footer, "Apply"); Button("SetupBack", footer, "Back");
        }

        private static void Footer(Transform panel, string apply, string back)
        { var row = Rect("Footer", panel); Horizontal(row, 18); Height(row, 58); Button(apply, row, "Apply"); Button(back, row, "Back"); }
        private static RectTransform Rect(string name, Transform parent)
        { var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f); return rect; }
        private static void Stretch(RectTransform rect)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        private static void Height(Component component, float height)
        { var element = component.GetComponent<LayoutElement>() ?? component.gameObject.AddComponent<LayoutElement>(); element.minHeight = element.preferredHeight = height; }
        private static void Vertical(RectTransform rect, float gap, RectOffset padding)
        { var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>(); layout.spacing = gap; layout.padding = padding; layout.childControlHeight = layout.childControlWidth = true; layout.childForceExpandHeight = false; layout.childForceExpandWidth = true; }
        private static void Horizontal(RectTransform rect, float gap)
        { var layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>(); layout.spacing = gap; layout.childControlHeight = layout.childControlWidth = true; layout.childForceExpandHeight = false; layout.childForceExpandWidth = true; }
        private static Text Label(string name, Transform parent, string value, int size, float height = 0)
        { var rect = Rect(name, parent); var text = rect.gameObject.AddComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.text = value; text.fontSize = size; text.color = Ink; text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false; text.supportRichText = false; text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate; if (height > 0) Height(rect, height); return text; }
        private static Button Button(string name, Transform parent, string label)
        {
            var rect = Rect(name, parent); Height(rect, 58); rect.gameObject.AddComponent<Image>().color = new Color32(48, 52, 49, 255);
            var button = rect.gameObject.AddComponent<Button>(); var colors = button.colors;
            colors.highlightedColor = new Color(1.15f, 1.12f, 1.05f); colors.selectedColor = new Color32(232, 184, 109, 255);
            button.colors = colors;
            var outline = rect.gameObject.AddComponent<Outline>(); outline.effectColor = new Color32(232, 184, 109, 255); outline.effectDistance = new Vector2(2, -2); outline.enabled = false;
            rect.gameObject.AddComponent<TopDown3DMenuFocus>();
            var text = Label(name + "Label", rect, label, 28); Stretch(text.rectTransform); text.rectTransform.offsetMin = new Vector2(8, 4); text.rectTransform.offsetMax = new Vector2(-8, -4);
            return button;
        }
    }
}
