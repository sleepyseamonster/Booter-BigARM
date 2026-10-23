using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BooterBigArm.TopDown3D
{
    [RequireComponent(typeof(Button), typeof(CampMenuPanelGraphic))]
    public sealed class CampMenuControl : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public string key;
        public Text title, subtitle, cornerLabel;
        public Image art;
        public bool tab;
        public bool goldAction;
        public bool prompt;
        [SerializeField] private CampExpeditionMenuStyle style;
        [SerializeField] private Color baseFill;
        private bool selected, focused, hovered, wired;
        private Color? relationEdge;
        private float relationOpacity=1;
        public event Action<CampMenuControl> Activated, Browsed;
        public Button Button => GetComponent<Button>();
        public bool IsSelected => selected;
        public bool HasNavigationFocus=>focused;
        public bool HasPointerHover=>hovered;

        public void Configure(CampExpeditionMenuStyle value, string address, Color fill)
        { style = value; key = address; baseFill = fill; Refresh(); }
        private void OnEnable() { Wire(); Refresh(); }
        private void OnDisable(){focused=hovered=false;}
        public void Wire()
        {
            if (wired) return;
            Button.transition = Selectable.Transition.None;
            Button.onClick.AddListener(() => Activated?.Invoke(this)); wired = true;
        }
        public void SetSelected(bool value) { selected = value; Refresh(); }
        internal void RefreshAvailability(){Refresh();}
        internal void SetRelation(Color? value,float opacity=1){relationEdge=value;relationOpacity=opacity;Refresh();}
        public void SetContent(string label, Sprite icon = null, string note = null)
        {
            if (title != null) title.text = label ?? "";
            if (art != null) { art.sprite = icon; art.enabled = icon != null; }
            if (subtitle != null) subtitle.text = note ?? "";
            Refresh();
        }
        public void OnSelect(BaseEventData data) { focused = true; Refresh(); Browsed?.Invoke(this); }
        public void OnDeselect(BaseEventData data) { focused = false; Refresh(); }
        public void OnPointerEnter(PointerEventData data) { hovered = true; Refresh(); Browsed?.Invoke(this); }
        public void OnPointerExit(PointerEventData data) { hovered = false; Refresh(); }
        private void Refresh()
        {
            if (style == null) return;
            var group=GetComponent<CanvasGroup>();if(group!=null)group.alpha=hovered?1:relationOpacity;
            var fill = selected ? Color.Lerp(style.surface, style.accent, .22f) : baseFill;
            var edge = focused || hovered ? style.ink : selected ? style.accent : relationEdge??style.line;
            if(tab){fill=Color.clear;if(!focused&&!hovered&&!selected)edge=Color.clear;}
            if(goldAction){fill=style.gold;edge=focused||hovered?style.ink:style.gold;}
            if(prompt){fill=Color.clear;edge=focused||hovered?style.ink:Color.clear;}
            GetComponent<CampMenuPanelGraphic>().SetFrame(fill, edge, focused || selected ? 2 : 1);
            GetComponent<CampMenuPanelGraphic>().underlineOnly=tab;
            if (title != null) title.color = !Button.interactable ? style.muted : tab&&selected?style.accent:style.ink;
            if(title!=null&&goldAction&&Button.interactable)title.color=style.background;
            if(title!=null&&prompt)title.color=style.ink;
        }
    }
}
