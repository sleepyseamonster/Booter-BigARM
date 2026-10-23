using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class CampExpeditionMenuView
    {
        private ScrollRect equipmentScroll;
        private RectTransform carriedGrid,frameConnection;
        private Text carriedHeading;
        private string shownContext;
        private readonly Dictionary<string,Vector2> scrollMemory=new(StringComparer.Ordinal);
        private string FocusContext=>page=="inventory"?"inventory:"+owner:page;

        private void RememberBrowsingState()
        {
            if(shownContext==null)return;
            var selected=events?.currentSelectedGameObject?.GetComponent<CampMenuControl>();
            if(selected!=null&&!selected.tab&&!selected.prompt&&selected.transform.IsChildOf(transform))pageFocus[shownContext]=selected.key;
            if(equipmentScroll!=null&&equipmentScroll.gameObject.activeInHierarchy)scrollMemory[shownContext]=equipmentScroll.content.anchoredPosition;
            else if(cargoGrid!=null&&cargoGrid.gameObject.activeInHierarchy)scrollMemory[shownContext]=cargoGrid.GetComponent<ScrollRect>().content.anchoredPosition;
        }

        private void ConfigureEquipmentScroll()
        {
            if(inventoryGrid==null||inventoryAttachments==null||!controls.TryGetValue("cargo:0",out var template))return;
            var parent=inventoryGrid.parent;
            var viewport=new GameObject("Equipment viewport",typeof(RectTransform),typeof(Image),typeof(RectMask2D),typeof(ScrollRect));viewport.transform.SetParent(parent,false);
            var rect=(RectTransform)viewport.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=Vector2.zero;rect.offsetMax=new Vector2(0,-31);viewport.GetComponent<Image>().color=Color.clear;
            var list=new GameObject("Equipment content",typeof(RectTransform)).GetComponent<RectTransform>();list.SetParent(rect,false);list.anchorMin=new Vector2(0,1);list.anchorMax=Vector2.one;list.pivot=new Vector2(.5f,1);list.offsetMin=list.offsetMax=Vector2.zero;
            equipmentScroll=viewport.GetComponent<ScrollRect>();equipmentScroll.viewport=rect;equipmentScroll.content=list;equipmentScroll.horizontal=false;equipmentScroll.movementType=ScrollRect.MovementType.Clamped;equipmentScroll.scrollSensitivity=35;
            inventoryGrid.SetParent(list,false);inventoryAttachments.SetParent(list,false);
            carriedGrid=new GameObject("Carried gear",typeof(RectTransform)).GetComponent<RectTransform>();carriedGrid.SetParent(list,false);carriedGrid.anchorMin=new Vector2(0,1);carriedGrid.anchorMax=Vector2.one;carriedGrid.pivot=new Vector2(.5f,1);carriedGrid.offsetMin=carriedGrid.offsetMax=Vector2.zero;
            carriedHeading=EquipmentLabel(list,"CARRIED GEAR · 0 / 4");
            for(int i=0;i<CampExpeditionLayout.CarriedCount;i++)
            {
                var clone=Instantiate(template,carriedGrid);clone.name="Carried gear "+(i+1);clone.key="carried:"+i;clone.Wire();clone.Activated+=Activate;clone.Browsed+=Browse;controls.Add(clone.key,clone);RememberClonedTypography(template,clone);
                var card=(RectTransform)clone.transform;card.anchorMin=new Vector2(i*.25f,0);card.anchorMax=new Vector2((i+1)*.25f,1);card.offsetMin=Vector2.zero;card.offsetMax=new Vector2(i==3?0:-8,0);
                if(clone.cornerLabel!=null)clone.cornerLabel.text=(i+1).ToString("00");
            }
            frameConnection=new GameObject("Body frame attachment connection",typeof(RectTransform),typeof(Image)).GetComponent<RectTransform>();frameConnection.SetParent(list,false);frameConnection.anchorMin=frameConnection.anchorMax=new Vector2(.095f,1);frameConnection.pivot=new Vector2(.5f,1);frameConnection.GetComponent<Image>().raycastTarget=false;frameConnection.GetComponent<Image>().color=style.gold;
        }

        private Text EquipmentLabel(Transform parent,string value)
        {
            var node=new GameObject(value,typeof(RectTransform),typeof(Text));node.transform.SetParent(parent,false);var text=node.GetComponent<Text>();text.font=style.ResolveFont();text.fontSize=16;text.text=value;text.color=style.ink;text.raycastTarget=false;
            text.rectTransform.anchorMin=new Vector2(0,1);text.rectTransform.anchorMax=Vector2.one;text.rectTransform.pivot=new Vector2(.5f,1);return text;
        }

        private void LayoutEquipmentScroll()
        {
            if(equipmentScroll==null||content==null)return;
            float body=Mathf.Max(GarmentBodyMinimum(),Mathf.Min(495,Mathf.Max(270,content.rect.height-220)));
            float attachments=Mathf.Min(139,Mathf.Max(110,content.rect.height-body-100));
            attachments=Mathf.Max(attachments,controls.Values.Where(control=>control.key.StartsWith("equip:attachment-",StringComparison.Ordinal)).Select(control=>ProjectTextHeight(control.title,((RectTransform)control.transform).rect.width-16)+44).DefaultIfEmpty(110).Max());
            float carriedTop=body+56+attachments+52;
            inventoryGrid.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top,0,body);
            inventoryAttachments.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top,body+56,attachments);
            carriedHeading.rectTransform.offsetMin=new Vector2(0,-carriedTop+8);carriedHeading.rectTransform.offsetMax=new Vector2(0,-carriedTop+36);
            int columns=carriedGrid.rect.width<320?1:carriedGrid.rect.width<600?2:4;float rowHeight=large?155:135;float carriedHeight=Mathf.Ceil(CampExpeditionLayout.CarriedCount/(float)columns)*(rowHeight+8)-8;
            carriedGrid.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top,carriedTop,carriedHeight);
            for(int i=0;i<CampExpeditionLayout.CarriedCount;i++)
            {
                var control=controls["carried:"+i];var card=(RectTransform)control.transform;int column=i%columns;
                card.anchorMin=new Vector2(column/(float)columns,1);card.anchorMax=new Vector2((column+1)/(float)columns,1);card.offsetMin=new Vector2(0,0);card.offsetMax=new Vector2(column==columns-1?0:-8,0);card.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top,i/columns*(rowHeight+8),rowHeight);
                control.title.rectTransform.anchorMin=Vector2.zero;control.title.rectTransform.anchorMax=new Vector2(1,.5f);control.title.rectTransform.offsetMin=new Vector2(8,28);control.title.rectTransform.offsetMax=new Vector2(-8,0);
                control.art.rectTransform.anchorMin=new Vector2(0,.52f);control.art.rectTransform.anchorMax=new Vector2(1,.85f);
                if(control.cornerLabel!=null){var label=control.cornerLabel.rectTransform;label.anchorMin=new Vector2(0,1);label.anchorMax=Vector2.one;label.offsetMin=new Vector2(10,-24);label.offsetMax=new Vector2(-10,0);}
            }
            equipmentScroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,carriedTop+carriedHeight+8);
            frameConnection.anchoredPosition=new Vector2(0,-body-2);frameConnection.sizeDelta=new Vector2(2,14);
        }

        private void RefreshCarriedEquipment()
        {
            if(carriedGrid==null)return;
            var carried=presentation.stacks.Where(s=>s.owner=="booter"&&s.slotIndex>=CampExpeditionLayout.BodyCount+CampExpeditionLayout.MountCount).ToArray();
            carriedHeading.text=$"CARRIED GEAR · {carried.Length} / {CampExpeditionLayout.CarriedCount}";
            for(int i=0;i<CampExpeditionLayout.CarriedCount;i++)
            {
                var stack=carried.FirstOrDefault(s=>s.slotIndex==CampExpeditionLayout.BodyCount+CampExpeditionLayout.MountCount+i);var item=definition.Find(stack?.itemId);
                controls["carried:"+i].SetContent(item?.DisplayName??"Empty",item?.art,stack==null?"":"×"+stack.quantity);
            }
            frameConnection.gameObject.SetActive(presentation.attachmentMounts>0);
            var heading=inventoryAttachments.GetComponentsInChildren<Text>(true).FirstOrDefault(t=>t.name=="Attachment heading");
            if(heading!=null&&presentation.attachmentMounts==0)heading.text="ATTACHMENTS · BODY FRAME REQUIRED";
        }

        private void LayoutEquipmentLabels()
        {
            foreach(var control in controls.Values.Where(c=>c.key.StartsWith("equip:",StringComparison.Ordinal)))
            {
                var tile=(RectTransform)control.transform;bool compact=tile.rect.width<110;float height=tile.rect.height;if(height<=0)continue;
                if(control.cornerLabel!=null){if(compact)control.cornerLabel.fontSize=14;control.cornerLabel.text=control.key=="equip:frame"?(compact?"FRAME":"BODY FRAME"):control.key=="equip:tool"?(compact?"TOOL":"MULTI-TOOL"):control.cornerLabel.text;}
                if(compact)control.title.fontSize=large?18:16;
                float labelHeight=Mathf.Min(Mathf.Max(28,height-35),Mathf.Max(28,control.title.preferredHeight+6));
                float reserve=GarmentReserve(control.key);var label=control.title.rectTransform;label.anchorMin=Vector2.zero;label.anchorMax=new Vector2(1,0);label.offsetMin=new Vector2(8,4+reserve);label.offsetMax=new Vector2(-8,4+labelHeight+reserve);
                float headingHeight=25;if(control.cornerLabel!=null){headingHeight=ProjectTextHeight(control.cornerLabel,Mathf.Max(20,tile.rect.width-16),25);var heading=control.cornerLabel.rectTransform;heading.offsetMin=new Vector2(10,-headingHeight-5);heading.offsetMax=new Vector2(-6,-5);}
                if(control.art!=null){float top=Mathf.Max(.1f,(height-headingHeight-10)/height);control.art.rectTransform.anchorMin=new Vector2(0,Mathf.Min(top,Mathf.Clamp01((reserve+labelHeight+10)/height)));control.art.rectTransform.anchorMax=new Vector2(1,top);}
            }
        }

        private string InventoryDescription(string itemId,string purpose)
        {
            if(itemId==null)return purpose;
            var stack=selectedStack?.itemId==itemId?selectedStack:null;
            if(!string.IsNullOrEmpty(stack?.contentsSummary))purpose+="\n\n"+stack.contentsSummary;
            var item=menuCatalog?.Find(itemId);
            if(stack?.owner=="booter"&&(stack.equipmentAddress is "chest" or "legs")&&item?.pocketSlot==stack.equipmentAddress&&item.category is "Armor" or "Clothing"&&item.pockets>=0)
                purpose+=$"\n\nGarment preview: {item.pockets} authored pockets. Pocket storage is not available in this prototype.";
            return purpose;
        }

        private bool StorageMatches(CampMenuStackView stack,string query)
        {
            var item=definition.Find(stack.itemId);var display=menuCatalog?.Find(stack.itemId);
            var category=StorageCategory(stack);
            return (storageCategory.Length==0||category==storageCategory)&&(display?.name+" "+display?.purpose+" "+item?.DisplayName+" "+item?.purpose+" "+category+" "+display?.search+" "+stack.itemId).IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0;
        }
    }
}
