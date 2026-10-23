using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class CampExpeditionMenuView
    {
        private readonly Dictionary<Selectable,bool> beforePicker=new();
        private readonly Dictionary<ScrollRect,bool> beforePickerScroll=new(),beforeQuoteScroll=new();
        private readonly Dictionary<string,string> pickerMemory=new(StringComparer.Ordinal),confirmedSelections=new(StringComparer.Ordinal);
        private CampMenuStackView[] pickerRows=Array.Empty<CampMenuStackView>();
        private Text pickerPreview;
        private RectTransform pickerDetailsViewport;
        private RectTransform inspectorTitleViewport;
        private string lastOutpostPage="storage",startReturnPage="play";
        internal string OutpostPage=>lastOutpostPage;
        private void ConfigureInspectorTitle()
        {
            if(inspectorTitle==null)return;
            var title=inspectorTitle.rectTransform;var root=new GameObject("Inspector title viewport",typeof(RectTransform),typeof(RectMask2D),typeof(ScrollRect),typeof(CampMenuControl));root.transform.SetParent(title.parent,false);
            inspectorTitleViewport=(RectTransform)root.transform;inspectorTitleViewport.anchorMin=title.anchorMin;inspectorTitleViewport.anchorMax=title.anchorMax;inspectorTitleViewport.offsetMin=title.offsetMin;inspectorTitleViewport.offsetMax=title.offsetMax;
            title.SetParent(inspectorTitleViewport,false);title.anchorMin=new Vector2(0,1);title.anchorMax=Vector2.one;title.pivot=new Vector2(.5f,1);title.offsetMin=title.offsetMax=Vector2.zero;
            inspectorTitle.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            var scroll=root.GetComponent<ScrollRect>();scroll.viewport=inspectorTitleViewport;scroll.content=title;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=35;
            var control=root.GetComponent<CampMenuControl>();control.Configure(style,"inspector-title",Color.clear);control.Wire();control.Activated+=Activate;control.Browsed+=Browse;controls.Add(control.key,control);
        }
        private string SelectionContext=>page=="review"?"review":page=="storage"?"storage":owner;
        private void GatePicker()
        {
            GateScrollContext(transform.Find("Safe Area/Screen Host/EquipmentPicker"),beforePickerScroll);
            foreach(var selectable in GetComponentsInChildren<Selectable>(true))
            {
                if(!beforePicker.ContainsKey(selectable))beforePicker[selectable]=selectable.interactable;
                var key=selectable.GetComponent<CampMenuControl>()?.key;
                selectable.interactable=beforePicker[selectable]&&(key?.StartsWith("candidate:",StringComparison.Ordinal)==true||key is "picker-details" or "back" or "hint:back" or "hint:select");
            }
        }
        private void RestorePickerGate()
        {foreach(var row in beforePicker)if(row.Key!=null)row.Key.interactable=row.Value;beforePicker.Clear();RestoreScrollContext(beforePickerScroll);}
        private void GateScrollContext(Transform root,Dictionary<ScrollRect,bool> memory)
        {foreach(var scroll in GetComponentsInChildren<ScrollRect>(true)){if(!memory.ContainsKey(scroll))memory[scroll]=scroll.enabled;scroll.enabled=memory[scroll]&&root!=null&&scroll.transform.IsChildOf(root);}}
        private static void RestoreScrollContext(Dictionary<ScrollRect,bool> memory)
        {foreach(var row in memory)if(row.Key!=null)row.Key.enabled=row.Value;memory.Clear();}
        private void ConfigurePickerPreview()
        {
            var root=transform.Find("Safe Area/Screen Host/EquipmentPicker");if(root==null)return;
            if(pickerPreview==null)
            {
                var viewport=new GameObject("Candidate details viewport",typeof(RectTransform),typeof(RectMask2D),typeof(ScrollRect),typeof(CampMenuControl));viewport.transform.SetParent(root,false);
                pickerDetailsViewport=(RectTransform)viewport.transform;pickerDetailsViewport.anchorMin=Vector2.zero;pickerDetailsViewport.anchorMax=new Vector2(1,0);pickerDetailsViewport.pivot=new Vector2(.5f,0);
                var details=viewport.GetComponent<CampMenuControl>();details.Configure(style,"picker-details",Color.clear);details.Wire();controls.Add(details.key,details);
                var node=new GameObject("Candidate preview",typeof(RectTransform),typeof(Text),typeof(ContentSizeFitter));node.transform.SetParent(viewport.transform,false);
                var rect=(RectTransform)node.transform;rect.anchorMin=new Vector2(0,1);rect.anchorMax=Vector2.one;rect.pivot=new Vector2(.5f,1);rect.offsetMin=rect.offsetMax=Vector2.zero;
                pickerPreview=node.GetComponent<Text>();pickerPreview.font=style.ResolveFont();pickerPreview.fontSize=16;pickerPreview.color=style.ink;pickerPreview.raycastTarget=false;pickerPreview.verticalOverflow=VerticalWrapMode.Truncate;
                node.GetComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
                var scroll=viewport.GetComponent<ScrollRect>();scroll.viewport=pickerDetailsViewport;scroll.content=rect;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=35;
                fontSizes[pickerPreview]=16;
            }
            float previewHeight=Mathf.Clamp(((RectTransform)root).rect.height*.25f,48,150);
            pickerDetailsViewport.offsetMin=new Vector2(18,50);pickerDetailsViewport.offsetMax=new Vector2(-18,50+previewHeight);
            if(pickerViewport!=null)pickerViewport.offsetMin=new Vector2(15,previewHeight+65);
        }
        private void PreviewCandidate(CampMenuControl control)
        {
            if(pickerTarget==null||pickerPreview==null||!int.TryParse(control.key.Substring(10),out var index)||index>=pickerRows.Length)return;
            var candidate=pickerRows[index];pickerMemory[pickerTarget]=candidate.key;
            var item=definition.Find(candidate.itemId);pickerPreview.text=(item?.DisplayName??candidate.itemId)+"\n"+(item?.purpose??"Definition unavailable.")+(string.IsNullOrEmpty(candidate.contentsSummary)?"":"\n\n"+candidate.contentsSummary);
            pickerPreview.rectTransform.anchoredPosition=Vector2.zero;
            RevealFocusedControl(control);
        }
        private void RefreshConfirmedSelection()
        {
            confirmedSelections.TryGetValue(SelectionContext,out var selected);
            var rows=CargoRows();
            foreach(var pair in controls.Where(c=>c.Key.StartsWith("cargo:",StringComparison.Ordinal)))
            {int index=int.Parse(pair.Key.Substring(6));pair.Value.SetSelected(index<rows.Count&&SelectionIdentity(rows[index])==selected);}
        }
        private static string SelectionIdentity(CampMenuStackView item)=>string.IsNullOrEmpty(item.instanceId)?item.itemId+"@"+item.key:"instance:"+item.instanceId;
        private bool ReturnFromResource()
        {
            if(page=="start"){ShowPage(startReturnPage);return true;}
            if(page=="almanac"){ShowPage(almanacReturnPage);return true;}
            if(page is "guide" or "map" or "settings"){ShowPage("start");return true;}
            return false;
        }
    }
}
