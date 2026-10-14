using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform), typeof(Image), typeof(Button))]
    public sealed class TopDown3DInventorySlotView : MonoBehaviour, ISelectHandler
    {
        private static readonly Color32 EmptyColor = new(24, 28, 32, 245);
        private static readonly Color32 OccupiedColor = new(47, 43, 35, 250);
        private static readonly Color32 SourceColor = new(146, 111, 53, 255);

        private Image background;
        private Image icon;
        private Text quantity;
        private Button button;
        private Action<int> activated;
        private Action<int> selected;
        private TopDown3DEquipmentSlot equipmentRole;
        private bool itemOccupied;

        public int SlotIndex { get; private set; }
        public Button Button => button;

        public void Initialize(int index, Action<int> onActivated, Action<int> onSelected)
        {
            SlotIndex = index;
            activated = onActivated;
            selected = onSelected;
            EnsureVisualTree();
            button.onClick.RemoveListener(HandleActivated);
            button.onClick.AddListener(HandleActivated);
        }

        public void Refresh(TopDown3DInventorySlot slot, TopDown3DItemDefinition definition, bool sourceSelected)
        {
            EnsureVisualTree();
            var occupied = slot != null && !slot.IsEmpty && definition != null;
            background.color = sourceSelected ? SourceColor : occupied ? OccupiedColor : EmptyColor;
            icon.enabled = occupied;
            icon.sprite = occupied ? definition.Icon : null;
            quantity.text = occupied && slot.Quantity > 1 ? slot.Quantity.ToString() : string.Empty;
            button.interactable = true;
            itemOccupied = occupied; equipmentRole = occupied ? definition.EquipmentSlot : TopDown3DEquipmentSlot.None;
            ShowEquipmentGlyph(equipmentRole, occupied);
        }

        public void OnSelect(BaseEventData eventData)
        {
            selected?.Invoke(SlotIndex);
        }

        public void SetLoadoutCaption(string caption)
        {
            EnsureVisualTree();
            var child = transform.Find("Equipment Label") as RectTransform;
            if (child == null)
            {
                child = new GameObject("Equipment Label", typeof(RectTransform), typeof(Text)).GetComponent<RectTransform>();
                child.SetParent(transform, false);
            }
            child.gameObject.SetActive(!string.IsNullOrEmpty(caption));
            if (string.IsNullOrEmpty(caption))
            {
                ShowEquipmentGlyph(equipmentRole, itemOccupied);
                icon.rectTransform.anchorMin = Vector2.zero; icon.rectTransform.anchorMax = Vector2.one;
                icon.rectTransform.offsetMin = new Vector2(9, 9); icon.rectTransform.offsetMax = new Vector2(-9, -9);
                return;
            }
            child.anchorMin = Vector2.zero; child.anchorMax = Vector2.one;
            child.offsetMin = new Vector2(62, 5); child.offsetMax = new Vector2(-4, -5);
            var label = child.GetComponent<Text>(); label.text = caption;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.fontSize = 14;
            label.alignment = TextAnchor.MiddleLeft; label.color = Color.white; label.raycastTarget = false;
            icon.rectTransform.anchorMin = new Vector2(0, .5f); icon.rectTransform.anchorMax = new Vector2(0, .5f);
            icon.rectTransform.pivot = new Vector2(0, .5f); icon.rectTransform.anchoredPosition = new Vector2(8, 0);
            icon.rectTransform.sizeDelta = new Vector2(46, 46);
            ShowEquipmentGlyph((TopDown3DEquipmentSlot)(SlotIndex + 1), itemOccupied);
        }

        private void ShowEquipmentGlyph(TopDown3DEquipmentSlot role, bool occupied)
        {
            var child = transform.Find("Equipment Silhouette") as RectTransform;
            if (child == null && role == TopDown3DEquipmentSlot.None) return;
            if (child == null)
            {
                child = new GameObject("Equipment Silhouette", typeof(RectTransform), typeof(CanvasRenderer), typeof(TopDown3DEquipmentSilhouette)).GetComponent<RectTransform>();
                child.SetParent(transform, false);
            }
            child.gameObject.SetActive(role != TopDown3DEquipmentSlot.None);
            if (role == TopDown3DEquipmentSlot.None) { icon.enabled = occupied; return; }
            icon.enabled = false;
            child.anchorMin = child.anchorMax = new Vector2(.5f, .5f); child.pivot = new Vector2(.5f, .5f);
            child.anchoredPosition = transform.Find("Equipment Label")?.gameObject.activeSelf == true ? new Vector2(-72, 0) : Vector2.zero;
            child.sizeDelta = new Vector2(46, 46);
            child.GetComponent<TopDown3DEquipmentSilhouette>().Show(role, occupied);
        }

        private void HandleActivated()
        {
            activated?.Invoke(SlotIndex);
        }

        private void EnsureVisualTree()
        {
            // These references are transient: reconnect authored children after
            // reload, and repair destroyed components without repeating layout
            // and component lookups for every inventory refresh.
            if (background != null && button != null && icon != null && quantity != null) return;
            background = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            button = GetComponent<Button>() ?? gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.RemoveListener(HandleActivated);
            button.onClick.AddListener(HandleActivated);
            var colors = button.colors;
            colors.highlightedColor = new Color32(91, 78, 53, 255);
            colors.selectedColor = new Color32(105, 87, 56, 255);
            colors.pressedColor = new Color32(128, 96, 46, 255);
            colors.disabledColor = EmptyColor;
            button.colors = colors;

            var iconTransform = transform.Find("Icon") as RectTransform;
            if (iconTransform == null)
            {
                iconTransform = new GameObject("Icon", typeof(RectTransform), typeof(Image))
                    .GetComponent<RectTransform>();
                iconTransform.SetParent(transform, false);
            }

            iconTransform.anchorMin = Vector2.zero;
            iconTransform.anchorMax = Vector2.one;
            iconTransform.offsetMin = new Vector2(9f, 9f);
            iconTransform.offsetMax = new Vector2(-9f, -9f);
            icon = iconTransform.GetComponent<Image>() ?? iconTransform.gameObject.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            var quantityTransform = transform.Find("Quantity") as RectTransform;
            if (quantityTransform == null)
            {
                quantityTransform = new GameObject("Quantity", typeof(RectTransform), typeof(Text))
                    .GetComponent<RectTransform>();
                quantityTransform.SetParent(transform, false);
            }

            quantityTransform.anchorMin = new Vector2(0f, 0f);
            quantityTransform.anchorMax = new Vector2(1f, 0.45f);
            quantityTransform.offsetMin = new Vector2(4f, 2f);
            quantityTransform.offsetMax = new Vector2(-5f, 0f);
            quantity = quantityTransform.GetComponent<Text>() ?? quantityTransform.gameObject.AddComponent<Text>();
            quantity.font ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            quantity.alignment = TextAnchor.LowerRight;
            quantity.fontSize = 18;
            quantity.fontStyle = FontStyle.Bold;
            quantity.color = Color.white;
            quantity.raycastTarget = false;
        }
    }
}
