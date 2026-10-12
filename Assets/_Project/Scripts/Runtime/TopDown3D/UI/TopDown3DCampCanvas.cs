using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace BooterBigArm.TopDown3D
{
    public sealed class TopDown3DCampCanvas : MonoBehaviour
    {
        private Text detail;
        private GameObject panel, choices;
        private CanvasGroup group;
        private Button sleep, rest, pack;
        private float summaryUntil;
        private bool textScaleApplied, largeText;
        public void ApplyTextScale(bool large)
        {
            if (detail == null) return;
            if (textScaleApplied && largeText == large) return;
            textScaleApplied = true; largeText = large;
            detail.fontSize = large ? 27 : 23;
            ((RectTransform)panel.transform).sizeDelta = new Vector2(large ? 800 : 740, large ? 270 : 225);
            detail.rectTransform.sizeDelta = new Vector2(large ? 750 : 690, large ? 170 : 135);
            ((RectTransform)choices.transform).anchoredPosition = new Vector2(0, large ? -96 : -70);
            foreach (var text in choices.GetComponentsInChildren<Text>()) text.fontSize = large ? 24 : 21;
        }
        public void Configure(Action doSleep, Action doRest, Action doPack)
        {
            var canvas = gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 115;
            var scaler = gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); gameObject.AddComponent<GraphicRaycaster>();
            scaler.matchWidthOrHeight = .5f;
            panel = Rect("Camp panel", transform, new Vector2(0, -290), new Vector2(740, 225)).gameObject;
            var backdrop = panel.AddComponent<Image>(); backdrop.color = new Color(.06f, .07f, .06f, .94f);
            group = panel.AddComponent<CanvasGroup>();
            var textRect = Rect("Camp details", panel.transform, new Vector2(0, 34), new Vector2(690, 135));
            detail = textRect.gameObject.AddComponent<Text>(); detail.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            detail.fontSize = 23; detail.alignment = TextAnchor.MiddleCenter; detail.color = new Color(.95f, .9f, .8f);
            choices = Rect("Choices", panel.transform, new Vector2(0, -70), new Vector2(690, 60)).gameObject;
            sleep = Button("Sleep", -235, doSleep);
            rest = Button("Rest", 0, doRest); pack = Button("Pack up", 235, doPack);
            panel.SetActive(false);
        }
        private Button Button(string label, float x, Action callback)
        {
            var rect = Rect(label, choices.transform, new Vector2(x, 0), new Vector2(220, 52));
            var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(.23f, .24f, .2f);
            var button = rect.gameObject.AddComponent<Button>(); button.onClick.AddListener(() => callback());
            var text = Rect("Label", rect, Vector2.zero, new Vector2(210, 48)).gameObject.AddComponent<Text>();
            text.font = detail.font; text.fontSize = 21; text.alignment = TextAnchor.MiddleCenter; text.color = detail.color; text.text = label;
            return button;
        }
        public void Show(string text, bool actions, bool ready = true)
        {
            summaryUntil = 0; panel.SetActive(true); detail.text = text; choices.SetActive(actions);
            group.interactable = actions && ready; group.blocksRaycasts = actions && ready;
        }
        public void Focus(EventSystem system)
        {
            if (system == null || !panel.activeInHierarchy || !choices.activeInHierarchy || !group.interactable) return;
            var selected = system.currentSelectedGameObject;
            if (selected == sleep.gameObject && sleep.IsInteractable()
                || selected == rest.gameObject && rest.IsInteractable()
                || selected == pack.gameObject && pack.IsInteractable()) return;
            var available = sleep.IsInteractable() ? sleep : rest.IsInteractable() ? rest : pack.IsInteractable() ? pack : null;
            if (available != null) { system.SetSelectedGameObject(available.gameObject); return; }
            if (selected != null && selected.transform.IsChildOf(choices.transform)) system.SetSelectedGameObject(null);
        }
        public void SetAvailability(bool canSleep, bool canRest, bool canPack)
        { sleep.interactable = canSleep; rest.interactable = canRest; pack.interactable = canPack; }
        public void Hide() { if (panel != null) panel.SetActive(false); }
        public void Summary(string text) { Show(text, false); summaryUntil = Time.unscaledTime + 4; }
        private void Update() { if (summaryUntil > 0 && Time.unscaledTime >= summaryUntil) { summaryUntil = 0; Hide(); } }
        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f); rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
        }
    }
}
