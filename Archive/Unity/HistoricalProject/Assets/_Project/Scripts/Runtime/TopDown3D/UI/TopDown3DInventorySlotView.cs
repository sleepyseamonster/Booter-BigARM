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
        }

        public void OnSelect(BaseEventData eventData)
        {
            selected?.Invoke(SlotIndex);
        }

        private void HandleActivated()
        {
            activated?.Invoke(SlotIndex);
        }

        private void EnsureVisualTree()
        {
            background = GetComponent<Image>();
            button = GetComponent<Button>();
            button.targetGraphic = background;
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
            icon = iconTransform.GetComponent<Image>();
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
            quantity = quantityTransform.GetComponent<Text>();
            quantity.font ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            quantity.alignment = TextAnchor.LowerRight;
            quantity.fontSize = 18;
            quantity.fontStyle = FontStyle.Bold;
            quantity.color = Color.white;
            quantity.raycastTarget = false;
        }
    }
}
