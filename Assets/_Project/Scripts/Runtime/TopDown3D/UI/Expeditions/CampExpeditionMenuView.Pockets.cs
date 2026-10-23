using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class CampExpeditionMenuView
    {
        private sealed class GarmentPreview
        {
            public string target,identity;
            public CampMenuControl garment,previous,next;
            public RectTransform root;
            public ScrollRect scroll;
            public Text counter;
            public CampMenuControl[] pockets;
            public long count,page;
            public Navigation originalNavigation;
            public bool visible;
            public float reserve;
        }
        private readonly Dictionary<string,GarmentPreview> garmentPreviews=new(StringComparer.Ordinal);
        private readonly Dictionary<string,long> garmentPages=new(StringComparer.Ordinal);
        private bool refreshingGarments;
        private void ConfigureGarmentPreviews()
        {
            foreach(string target in new[]{"chest","legs"})
            {
                if(!controls.TryGetValue("equip:"+target,out var garment))continue;
                var root=new GameObject(target+" pocket preview",typeof(RectTransform)).GetComponent<RectTransform>();root.SetParent(garment.transform,false);
                var scroll=CreateMenuScroll(root,target+" pocket layout");var preview=new GarmentPreview{target=target,garment=garment,root=root,scroll=scroll,originalNavigation=garment.Button.navigation};
                preview.pockets=Enumerable.Range(0,CampMenuGarmentPockets.PageSize).Select(index=>ProjectButton(scroll.content,"pocket:empty:"+target+":"+index,"")).ToArray();
                preview.previous=ProjectButton(root,"pocket:previous:"+target,"‹");preview.next=ProjectButton(root,"pocket:next:"+target,"›");preview.counter=CatalogText(root,"",14);preview.counter.alignment=TextAnchor.MiddleCenter;preview.counter.supportRichText=false;
                garmentPreviews.Add(target,preview);root.gameObject.SetActive(false);LinkMenuDirection(garment.gameObject,data=>MoveGarmentFocus(preview,data));
                for(int i=0;i<preview.pockets.Length;i++){int index=i;LinkMenuDirection(preview.pockets[i].gameObject,data=>MovePocketFocus(preview,index,data));}
                LinkMenuDirection(preview.previous.gameObject,data=>MovePocketPager(preview,false,data));LinkMenuDirection(preview.next.gameObject,data=>MovePocketPager(preview,true,data));
            }
        }
        private void LateUpdate(){if(initialized&&garmentPreviews.Count>0)RefreshGarmentPreviews();}
        private bool EquipmentContext=>(page=="inventory"&&owner=="booter")||page=="crafting-equipment";
        internal long PocketPage(string target)=>garmentPreviews.GetValueOrDefault(target)?.page??0;
        internal void SetPocketPage(string target,long requested)
        {
            if(!garmentPreviews.TryGetValue(target,out var preview)||preview.count<=0||preview.identity==null)return;
            garmentPages[preview.identity]=CampMenuGarmentPockets.ClampPage(preview.count,requested);RefreshGarmentPreviews(true);
        }
        private void RefreshGarmentPreviews(bool force=false)
        {
            if(refreshingGarments||garmentPreviews.Count==0)return;refreshingGarments=true;bool changed=force;
            try
            {
                foreach(var preview in garmentPreviews.Values)
                {
                    var owned=presentation.stacks.FirstOrDefault(stack=>stack.owner=="booter"&&stack.equipmentAddress==preview.target);
                    var item=menuCatalog?.Find(owned?.itemId);long count=CampMenuGarmentPockets.Count(item,owned,preview.target);string identity=CampMenuGarmentPockets.Identity(owned);
                    var selected=events?.currentSelectedGameObject;bool within=selected!=null&&selected.transform.IsChildOf(preview.garment.transform);
                    bool pointer=promptInput?.PromptDevice!=TopDown3DPromptDevice.Gamepad&&preview.garment.HasPointerHover;
                    bool visible=EquipmentContext&&GetComponent<CanvasGroup>().interactable&&!quoting&&pickerTarget==null&&!SettingsPresentationOpen&&!StorageChoiceOpen&&count>0&&(within||pointer);
                    bool replaced=preview.identity!=identity;long current=identity==null?0:CampMenuGarmentPockets.ClampPage(count,garmentPages.GetValueOrDefault(identity));
                    if(preview.visible!=visible||replaced||preview.count!=count||preview.page!=current)changed=true;
                    if(!changed&&!force)continue;
                    preview.count=count;preview.identity=identity;preview.page=current;preview.visible=visible;preview.root.gameObject.SetActive(visible);preview.garment.Button.navigation=visible?new Navigation{mode=Navigation.Mode.None}:preview.originalNavigation;
                    var numbers=CampMenuGarmentPockets.Numbers(count,current);
                    for(int i=0;i<preview.pockets.Length;i++){var control=preview.pockets[i];control.gameObject.SetActive(i<numbers.Length);control.SetContent(i<numbers.Length?"Pocket "+numbers[i]+"\nEmpty":"");control.title.supportRichText=false;}
                    bool paging=CampMenuGarmentPockets.Pages(count)>1;preview.previous.gameObject.SetActive(paging);preview.next.gameObject.SetActive(paging);preview.counter.gameObject.SetActive(paging);
                    SetProjectEnabled(preview.previous.Button,current>0);SetProjectEnabled(preview.next.Button,current+1<CampMenuGarmentPockets.Pages(count));preview.counter.text=numbers.Length==0?"":numbers[0]+"–"+numbers[^1]+"\n/ "+count;
                    if(replaced&&within&&selected!=preview.garment.gameObject){events.SetSelectedGameObject(preview.garment.gameObject);InputContextChanged?.Invoke();}
                }
                if(changed){LayoutEquipmentScroll();LayoutEquipmentLabels();ReflowGarmentPreviews();}
            }
            finally {refreshingGarments=false;}
        }
        private float GarmentBodyMinimum()=>garmentPreviews.Values.Where(preview=>preview.visible).Select(preview=>2.1f*(Mathf.Max(preview.reserve,large?140:130)+Mathf.Max(30,preview.garment.title.preferredHeight)+120)).DefaultIfEmpty(0).Max();
        private float GarmentReserve(string controlKey)=>controlKey.StartsWith("equip:",StringComparison.Ordinal)&&garmentPreviews.TryGetValue(controlKey.Substring(6),out var preview)&&preview.visible?preview.reserve:0;
        private void ReflowGarmentPreviews()
        {
            foreach(var preview in garmentPreviews.Values.Where(preview=>preview.visible))
            {
                float width=((RectTransform)preview.garment.transform).rect.width-16;float pager=preview.counter.gameObject.activeSelf?Mathf.Max(30,ProjectTextHeight(preview.counter,Mathf.Max(20,width-76))):0;
                int count=preview.pockets.Count(control=>control.gameObject.activeSelf);float cellWidth=(width-6)/2;
                foreach(var control in preview.pockets){control.title.fontSize=Mathf.RoundToInt(14*(large?1.16f:1));fontSizes[control.title]=14;}
                float cellHeight=Mathf.Max(large?60:50,preview.pockets.Where(control=>control.gameObject.activeSelf).Select(control=>ProjectTextHeight(control.title,cellWidth-20)+8).DefaultIfEmpty(0).Max());
                float height=Mathf.Max(large?98:88,cellHeight)+(pager>0?pager+6:0);preview.reserve=height+12;
                var root=preview.root;root.anchorMin=Vector2.zero;root.anchorMax=new Vector2(1,0);root.pivot=new Vector2(.5f,0);root.offsetMin=new Vector2(8,8);root.offsetMax=new Vector2(-8,8+height);
                preview.scroll.viewport.offsetMin=new Vector2(0,pager>0?pager+6:0);preview.scroll.viewport.offsetMax=Vector2.zero;
                for(int i=0;i<count;i++){var rect=(RectTransform)preview.pockets[i].transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(i%2*(cellWidth+6),-(i/2)*(cellHeight+6));rect.sizeDelta=new Vector2(cellWidth,cellHeight);}
                preview.scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Mathf.Ceil(count/2f)*(cellHeight+6));
                if(pager>0){Bottom((RectTransform)preview.previous.transform,0,30);Bottom((RectTransform)preview.next.transform,width-30,30);Bottom(preview.counter.rectTransform,36,Mathf.Max(20,width-72));}
                void Bottom(RectTransform rect,float x,float size){rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.zero;rect.anchoredPosition=new Vector2(x,0);rect.sizeDelta=new Vector2(size,pager);}
            }
            // The reserved area is known only after text measurement (including very large authored counts).
            if(garmentPreviews.Values.Any(preview=>preview.visible)){LayoutEquipmentScroll();LayoutEquipmentLabels();}
        }
        private bool HandlePocketAction(string key)
        {
            if(!key.StartsWith("pocket:",StringComparison.Ordinal))return false;
            var parts=key.Split(':');if(parts.Length<3||!garmentPreviews.TryGetValue(parts[2],out var preview)||!preview.visible)return true;
            if(parts[1] is "previous" or "next"){SetPocketPage(preview.target,preview.page+(parts[1]=="next"?1:-1));events?.SetSelectedGameObject(preview.garment.gameObject);preview.scroll.verticalNormalizedPosition=1;InputContextChanged?.Invoke();}
            else if(parts[1]=="empty")InspectEmptyPocket(preview,int.Parse(parts[3]));return true;
        }
        private void InspectEmptyPocket(GarmentPreview preview,int index)
        {
            var numbers=CampMenuGarmentPockets.Numbers(preview.count,preview.page);if(index<0||index>=numbers.Length)return;selectedStack=null;Inspect(null);if(inspectorBody!=null)inspectorBody.text="Pocket "+numbers[index]+"\nEmpty · preview only.\nPocket contents are not available.";
        }
        private bool BrowsePocket(CampMenuControl control)
        {
            if(!control.key.StartsWith("pocket:empty:",StringComparison.Ordinal))return false;var parts=control.key.Split(':');if(garmentPreviews.TryGetValue(parts[2],out var preview)&&preview.visible)InspectEmptyPocket(preview,int.Parse(parts[3]));return true;
        }
        private void MoveGarmentFocus(GarmentPreview preview,AxisEventData data)
        {
            if(!preview.visible)return;
            if(data.moveDir==MoveDirection.Down)FocusResource(preview.pockets.FirstOrDefault(control=>control.gameObject.activeSelf));else FocusGarmentNeighbor(preview,data.moveDir);data.Use();
        }
        private void FocusGarmentNeighbor(GarmentPreview preview,MoveDirection direction)
        {
            var current=preview.garment.Button.navigation;preview.garment.Button.navigation=preview.originalNavigation;
            var next=direction switch{MoveDirection.Up=>preview.garment.Button.FindSelectableOnUp(),MoveDirection.Down=>preview.garment.Button.FindSelectableOnDown(),MoveDirection.Left=>preview.garment.Button.FindSelectableOnLeft(),_=>preview.garment.Button.FindSelectableOnRight()};preview.garment.Button.navigation=current;
            if(next!=null&&next.IsInteractable()&&next.gameObject.activeInHierarchy)events?.SetSelectedGameObject(next.gameObject);
        }
        private void MovePocketFocus(GarmentPreview preview,int index,AxisEventData data)
        {
            if(!preview.visible||quoting||pickerTarget!=null)return;int count=preview.pockets.Count(control=>control.gameObject.activeSelf),next=index;
            if(data.moveDir==MoveDirection.Up){if(index<2){FocusResource(preview.garment);data.Use();return;}next-=2;}
            else if(data.moveDir==MoveDirection.Down){if(index+2>=count){if(preview.next.gameObject.activeSelf)FocusResource(preview.next.Button.IsInteractable()?preview.next:preview.previous);else FocusGarmentNeighbor(preview,MoveDirection.Down);data.Use();return;}next+=2;}
            else if(data.moveDir==MoveDirection.Left){if(index%2==0){FocusResource(preview.garment);data.Use();return;}next--;}
            else if(data.moveDir==MoveDirection.Right){if(index%2==1||index+1>=count){FocusResource(preview.garment);data.Use();return;}next++;}
            FocusResource(preview.pockets[next]);data.Use();
        }
        private void MovePocketPager(GarmentPreview preview,bool forward,AxisEventData data)
        {if(data.moveDir==MoveDirection.Up)FocusResource(preview.pockets.Last(control=>control.gameObject.activeSelf));else if(data.moveDir==MoveDirection.Left)FocusResource(forward&&preview.previous.Button.IsInteractable()?preview.previous:preview.garment);else if(data.moveDir==MoveDirection.Right)FocusResource(!forward&&preview.next.Button.IsInteractable()?preview.next:preview.garment);else if(data.moveDir==MoveDirection.Down)FocusResource(preview.garment);data.Use();}
    }
}
