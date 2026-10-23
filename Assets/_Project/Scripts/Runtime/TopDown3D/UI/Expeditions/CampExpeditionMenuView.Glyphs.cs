using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using GlyphShape=BooterBigArm.TopDown3D.CampMenuBindingGlyph.Shape;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class CampExpeditionMenuView
    {
        private void ApplyBindingPrompts()
        {
            if(promptInput==null||footer==null)return;
            bool gamepad=promptInput.PromptDevice==TopDown3DPromptDevice.Gamepad;
            string Name(string path,string fallback)=>promptInput.GetBindingDisplayName(path,fallback);
            void Tab(string path,string action,string fallback)
            {
                var root=transform.Find(path) as RectTransform;if(root==null)return;
                var image=root.GetComponent<Image>();if(image!=null)image.enabled=false;
                var text=BindingText(root,"Current binding");var control=action==null?null:promptInput.GetBindingPromptControl(action);
                string label=action==null?fallback:Name(action,fallback);
                float width=GlyphWidth(Shape(control),text,label);root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,width);
                DrawGlyph(root,"Binding glyph",text,control,label,0,width);
            }
            Tab("Safe Area/Primary/Prompt lb","UI/PreviousInventory","Previous");Tab("Safe Area/Primary/Prompt rb","UI/NextInventory","Next");
            foreach(var bar in new[]{"InventoryTabs","CraftingTabs","OutpostTabs"})
            {Tab("Safe Area/"+bar+"/Prompt lt",gamepad?"UI/PreviousExpeditionSubTab":null,"Click");Tab("Safe Area/"+bar+"/Prompt rt",gamepad?"UI/NextExpeditionSubTab":null,"Click");}
            foreach(var control in footer.GetComponentsInChildren<CampMenuControl>(true))
            {
                string action=control.key switch
                {
                    "page:start"=>"System/ToggleMenu","page:inventory"=>"System/ToggleInventory",
                    "hint:select"=>"UI/Submit","hint:back"=>"UI/Cancel","hint:west"=>"UI/QuickTransfer",
                    "hint:pan" when gamepad=>"UI/ExpeditionTreePan","hint:scroll" when gamepad=>"UI/ExpeditionInspectScroll",
                    "hint:navigate" when gamepad=>page is "tree" or "tracked"?"UI/ExpeditionTreeNavigate":"UI/Navigate",_=>null
                };
                var navLeft=gamepad&&control.key=="hint:navigate"&&page is not ("tree" or "tracked")?promptInput.GetBindingPromptControl("UI/Navigate","dpad"):null;
                var navRight=navLeft!=null?promptInput.GetBindingPromptControl("UI/Navigate","leftStick"):null;
                bool navigationPair=navLeft!=null&&navRight!=null;
                bool pair=navigationPair||control.key=="hint:tabs"||control.key=="hint:subtabs"&&gamepad;
                string fallback=control.key switch{"hint:subtabs"=>"Click","hint:navigate"=>"↑↓←→","hint:pan"=>page=="tracked"?"Click":"Drag","hint:scroll"=>page=="tree"?"Wheel / ±":"Wheel",_=>null};
                if(action==null&&!pair&&fallback==null)continue;
                foreach(Transform child in control.transform)
                    if(child.name.StartsWith("Prompt ",StringComparison.Ordinal)||child.name is "Inventory button glyph" or "Navigation glyph" or "Stick")child.gameObject.SetActive(false);
                if(!bindingPrompts.TryGetValue(control.key,out var text)){text=BindingText(control.transform,"Current binding");bindingPrompts.Add(control.key,text);}
                float width;
                if(pair)
                {
                    string left=control.key=="hint:tabs"?"UI/PreviousInventory":"UI/PreviousExpeditionSubTab",right=control.key=="hint:tabs"?"UI/NextInventory":"UI/NextExpeditionSubTab";
                    var a=BindingText(control.transform,"Binding label left");var b=BindingText(control.transform,"Binding label right");
                    var ac=navigationPair?navLeft:promptInput.GetBindingPromptControl(left);var bc=navigationPair?navRight:promptInput.GetBindingPromptControl(right);
                    string al=navigationPair?navLeft.shortDisplayName:Name(left,"Previous"),bl=navigationPair?navRight.shortDisplayName:Name(right,"Next");
                    float gap=navigationPair?14:4,aw=GlyphWidth(Shape(ac),a,al),bw=GlyphWidth(Shape(bc),b,bl);width=aw+gap+bw;
                    DrawGlyph(control.transform,"Binding glyph left",a,ac,al,0,aw);DrawGlyph(control.transform,"Binding glyph right",b,bc,bl,aw+gap,bw);
                    if(navigationPair){var separator=BindingText(control.transform,"Binding separator");separator.text="/";separator.rectTransform.anchorMin=separator.rectTransform.anchorMax=new Vector2(0,.5f);separator.rectTransform.pivot=new Vector2(0,.5f);separator.rectTransform.anchoredPosition=new Vector2(aw,3);separator.rectTransform.sizeDelta=new Vector2(gap,32);}
                    text.text=al+" / "+bl;text.enabled=false;
                }
                else
                {
                    var physical=action==null?null:promptInput.GetBindingPromptControl(action);string label=action==null?fallback:Name(action,fallback??"Select");
                    width=GlyphWidth(Shape(physical),text,label);DrawGlyph(control.transform,"Binding glyph",text,physical,label,0,width);
                }
                foreach(var name in new[]{"Binding glyph left","Binding glyph right","Binding label left","Binding label right"})SetChild(control.transform,name,pair);
                SetChild(control.transform,"Binding glyph",!pair);
                SetChild(control.transform,"Binding separator",navigationPair);
                control.title.rectTransform.offsetMin=new Vector2(width+8,3);
                var element=control.GetComponent<LayoutElement>();if(element!=null)element.preferredWidth=width+control.title.preferredWidth+16;
            }
        }

        private static GlyphShape Shape(InputControl control)
        {
            if(control?.device is not Gamepad)return GlyphShape.Key;
            return control.name switch
            {
                "leftShoulder"=>GlyphShape.LeftBumper,"rightShoulder"=>GlyphShape.RightBumper,
                "leftTrigger"=>GlyphShape.LeftTrigger,"rightTrigger"=>GlyphShape.RightTrigger,
                "buttonNorth"=>GlyphShape.North,"buttonSouth"=>GlyphShape.South,"buttonEast"=>GlyphShape.East,"buttonWest"=>GlyphShape.West,
                "start"=>GlyphShape.Menu,"select"=>GlyphShape.View,"leftStick" or "rightStick"=>GlyphShape.Stick,"dpad"=>GlyphShape.Dpad,_=>GlyphShape.Key
            };
        }
        private static float GlyphWidth(GlyphShape shape,Text text,string label)
        {text.text=label;return shape is GlyphShape.LeftBumper or GlyphShape.RightBumper?48:shape==GlyphShape.Key?Mathf.Max(32,text.preferredWidth+16):32;}
        private Text BindingText(Transform parent,string name)
        {
            var text=parent.Find(name)?.GetComponent<Text>();if(text!=null)return text;
            var root=new GameObject(name,typeof(RectTransform));root.layer=parent.gameObject.layer;root.transform.SetParent(parent,false);
            text=root.AddComponent<Text>();text.font=style.ResolveFont();text.fontSize=14;text.color=style.ink;text.alignment=TextAnchor.MiddleCenter;
            text.horizontalOverflow=HorizontalWrapMode.Overflow;text.verticalOverflow=VerticalWrapMode.Overflow;text.raycastTarget=false;return text;
        }
        private void DrawGlyph(Transform parent,string name,Text text,InputControl control,string label,float x,float width)
        {
            var graphic=parent.Find(name)?.GetComponent<CampMenuBindingGlyph>();
            if(graphic==null){var root=new GameObject(name,typeof(RectTransform));root.layer=parent.gameObject.layer;root.transform.SetParent(parent,false);graphic=root.AddComponent<CampMenuBindingGlyph>();graphic.transform.SetAsFirstSibling();}
            var rect=graphic.rectTransform;rect.anchorMin=rect.anchorMax=new Vector2(0,.5f);rect.pivot=new Vector2(0,.5f);rect.anchoredPosition=new Vector2(x,0);rect.sizeDelta=new Vector2(width,32);
            graphic.Configure(Shape(control),style.ink);text.text=label;text.enabled=graphic.ShowsLabel;text.rectTransform.anchorMin=text.rectTransform.anchorMax=new Vector2(0,.5f);text.rectTransform.pivot=new Vector2(0,.5f);
            // Segoe UI's visible glyphs sit below its line-box center; lift them into the outline.
            text.rectTransform.anchoredPosition=new Vector2(x,graphic.Kind is GlyphShape.LeftTrigger or GlyphShape.RightTrigger?5:3);text.rectTransform.sizeDelta=new Vector2(width,32);
        }
        private static void SetChild(Transform parent,string name,bool active){var child=parent.Find(name);if(child!=null)child.gameObject.SetActive(active);}
    }
}
