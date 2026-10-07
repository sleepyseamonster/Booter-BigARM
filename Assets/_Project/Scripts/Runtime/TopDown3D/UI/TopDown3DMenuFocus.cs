using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BooterBigArm.TopDown3D
{
    public sealed class TopDown3DMenuFocus : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        public void OnSelect(BaseEventData data)
        {
            var outline = GetComponent<Outline>(); if (outline != null) outline.enabled = true;
            var scroll = GetComponentInParent<ScrollRect>();
            if (scroll == null) return;
            Canvas.ForceUpdateCanvases();
            var rect = (RectTransform)transform;
            var position = scroll.content.InverseTransformPoint(rect.position);
            var range = scroll.content.rect.height - scroll.viewport.rect.height;
            if (range > 0) scroll.verticalNormalizedPosition = Mathf.Clamp01(1 + (position.y + scroll.viewport.rect.height * 0.5f) / range);
        }
        public void OnDeselect(BaseEventData data) { var outline = GetComponent<Outline>(); if (outline != null) outline.enabled = false; }
    }
}
