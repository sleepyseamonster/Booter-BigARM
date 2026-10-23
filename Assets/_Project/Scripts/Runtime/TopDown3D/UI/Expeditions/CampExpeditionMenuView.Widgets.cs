using UnityEngine;
using UnityEngine.UI;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class CampExpeditionMenuView
    {
        private ScrollRect CreateMenuScroll(Transform parent,string name,string focusKey=null)
        {
            var root=new GameObject(name,typeof(RectTransform),typeof(RectMask2D),typeof(ScrollRect));root.transform.SetParent(parent,false);
            if(focusKey==null)root.AddComponent<Image>().color=Color.clear;
            else
            {var control=root.AddComponent<CampMenuControl>();control.Configure(style,focusKey,Color.clear);control.Wire();control.Activated+=Activate;control.Browsed+=Browse;control.Button.navigation=new Navigation{mode=Navigation.Mode.None};controls.Add(focusKey,control);}
            var viewport=(RectTransform)root.transform;viewport.anchorMin=Vector2.zero;viewport.anchorMax=Vector2.one;viewport.offsetMin=viewport.offsetMax=Vector2.zero;
            var content=new GameObject(name+" content",typeof(RectTransform)).GetComponent<RectTransform>();content.SetParent(viewport,false);content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;content.pivot=new Vector2(.5f,1);content.offsetMin=content.offsetMax=Vector2.zero;
            var scroll=root.GetComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=35;return scroll;
        }
        private static void TopRegion(RectTransform rect,float top,float height,float left=0,float right=0)
        {rect.anchorMin=new Vector2(0,1);rect.anchorMax=Vector2.one;rect.pivot=new Vector2(.5f,1);rect.offsetMin=new Vector2(left,-top-height);rect.offsetMax=new Vector2(-right,-top);}
        private CampMenuControl CloneMenuControl(CampMenuControl source,Transform parent,string key)
        {
            var control=Instantiate(source,parent);control.key=control.name=key;control.Wire();control.Activated+=Activate;control.Browsed+=Browse;RememberClonedTypography(source,control);
            control.Button.interactable=beforeStorageChoice.TryGetValue(source.Button,out var enabled)?enabled:beforePicker.TryGetValue(source.Button,out enabled)?enabled:beforeQuote.TryGetValue(source.Button,out enabled)?enabled:source.Button.interactable;return control;
        }
        private void AddRarityCorner(CampMenuControl card,string rarity)
        {
            var mark=card.GetComponentInChildren<CampMenuRarityGraphic>(true);
            if(mark==null){var root=new GameObject("Rarity corner",typeof(RectTransform),typeof(CampMenuRarityGraphic));root.transform.SetParent(card.transform,false);mark=root.GetComponent<CampMenuRarityGraphic>();mark.raycastTarget=false;}
            mark.SetRarity(rarity);mark.rectTransform.anchorMin=mark.rectTransform.anchorMax=mark.rectTransform.pivot=new Vector2(1,0);mark.rectTransform.anchoredPosition=Vector2.zero;mark.rectTransform.sizeDelta=new Vector2(18,18);
        }
    }
}
