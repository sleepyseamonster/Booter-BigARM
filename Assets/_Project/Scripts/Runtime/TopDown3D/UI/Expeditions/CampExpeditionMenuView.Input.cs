using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class CampExpeditionMenuView
    {
        private RectTransform pickerList,pickerViewport;
        private TopDown3DInputRouter promptInput;
        private readonly Dictionary<string,Text> bindingPrompts=new(StringComparer.Ordinal);

        internal void FocusPlayAction()
        {
            if(page!="play"||quoting||pickerTarget!=null||events==null)return;
            if(controls.TryGetValue("play",out var play)&&play.gameObject.activeInHierarchy)
                events.SetSelectedGameObject(play.gameObject);
        }

        internal void SubmitPlayAction()
        {
            if(page!="play"||quoting||pickerTarget!=null)return;
            if(controls.TryGetValue("play",out var play))Activate(play);
        }

        internal void FocusReviewAction()
        {
            if(page!="review"||quoting||pickerTarget!=null||events==null)return;
            if(controls.TryGetValue("send-all",out var action)&&action.gameObject.activeInHierarchy)
                events.SetSelectedGameObject(action.gameObject);
        }
        internal void SubmitReviewAction()
        {
            if(page!="review"||quoting||pickerTarget!=null)return;
            if(controls.TryGetValue("send-all",out var action))Activate(action);
        }

        private void ConfigurePickerViewport()
        {
            var picker=transform.Find("Safe Area/Screen Host/EquipmentPicker");if(picker==null)return;
            pickerViewport=picker.Find("Candidates viewport") as RectTransform;
            pickerList=(pickerViewport!=null?pickerViewport.Find("Candidates"):picker.Find("Candidates")) as RectTransform;
            if(pickerList==null)return;
            if(pickerViewport==null)
            {
                pickerViewport=new GameObject("Candidates viewport",typeof(RectTransform),typeof(RectMask2D),typeof(ScrollRect),typeof(Image)).GetComponent<RectTransform>();pickerViewport.SetParent(picker,false);pickerViewport.GetComponent<Image>().color=Color.clear;
                pickerViewport.anchorMin=Vector2.zero;pickerViewport.anchorMax=Vector2.one;pickerViewport.offsetMin=new Vector2(15,50);pickerViewport.offsetMax=new Vector2(-15,-40);
                pickerList.SetParent(pickerViewport,false);
            }
            pickerList.anchorMin=new Vector2(0,1);pickerList.anchorMax=Vector2.one;pickerList.pivot=new Vector2(.5f,1);pickerList.anchoredPosition=Vector2.zero;
            var scroll=pickerViewport.GetComponent<ScrollRect>();scroll.viewport=pickerViewport;scroll.content=pickerList;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=35;
            MaskUtilities.Notify2DMaskStateChanged(pickerViewport.GetComponent<RectMask2D>());
        }

        private void EnsurePickerControls(int count)
        {
            if(!controls.TryGetValue("candidate:0",out var template))return;
            for(int i=0;i<count;i++)
            {
                string key="candidate:"+i;if(controls.ContainsKey(key))continue;
                var clone=Instantiate(template,template.transform.parent);clone.name=key;clone.key=key;clone.Wire();clone.Activated+=Activate;clone.Browsed+=Browse;controls.Add(key,clone);
                clone.Button.interactable=beforeQuote.TryGetValue(template.Button,out var enabled)?enabled:template.Button.interactable;
                RememberClonedTypography(template,clone);
            }
        }

        private void RememberClonedTypography(CampMenuControl template,CampMenuControl clone)
        {
            var source=template.GetComponentsInChildren<Text>(true);var copy=clone.GetComponentsInChildren<Text>(true);
            for(int i=0;i<copy.Length;i++)fontSizes[copy[i]]=fontSizes.TryGetValue(source[i],out var size)?size:source[i].fontSize;
        }

        private void ReflowPicker()
        {
            if(pickerList==null||pickerViewport==null)return;
            if(pickerTarget!=null)ConfigurePickerPreview();
            var grid=pickerList.GetComponent<GridLayoutGroup>();int columns=Mathf.Clamp(Mathf.FloorToInt((pickerViewport.rect.width+10)/138),1,3);
            grid.constraintCount=columns;grid.cellSize=new Vector2(Mathf.Max(80,(pickerViewport.rect.width-(columns-1)*10)/columns),Mathf.Max(80,Mathf.Min(large?200:180,pickerViewport.rect.height)));
            int count=controls.Values.Count(c=>c.key.StartsWith("candidate:",StringComparison.Ordinal)&&c.gameObject.activeSelf);
            pickerList.sizeDelta=new Vector2(0,Mathf.Max(pickerViewport.rect.height,Mathf.Ceil(count/(float)columns)*(grid.cellSize.y+10)));
            foreach(var control in controls.Values.Where(c=>c.key.StartsWith("candidate:",StringComparison.Ordinal)))
            {
                control.title.rectTransform.anchorMin=new Vector2(0,.14f);control.title.rectTransform.anchorMax=new Vector2(1,.43f);control.title.rectTransform.offsetMin=new Vector2(8,0);control.title.rectTransform.offsetMax=new Vector2(-8,0);
                control.art.rectTransform.anchorMin=new Vector2(0,.46f);control.art.rectTransform.anchorMax=new Vector2(1,.95f);
            }
            var candidates=controls.Values.Where(c=>c.key.StartsWith("candidate:",StringComparison.Ordinal)&&c.gameObject.activeSelf).OrderBy(c=>int.Parse(c.key.Substring(10))).ToArray();
            for(int i=0;i<candidates.Length;i++)
            {
                Selectable At(int index)=>index>=0&&index<candidates.Length?candidates[index].Button:null;
                candidates[i].Button.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnLeft=i%columns>0?At(i-1):null,
                    selectOnRight=i%columns<columns-1?At(i+1):null,selectOnUp=At(i-columns),
                    selectOnDown=i+columns<candidates.Length?At(i+columns):i<(candidates.Length-1)/columns*columns?At(candidates.Length-1):controls.GetValueOrDefault("picker-details")?.Button};
            }
            if(controls.TryGetValue("picker-details",out var details))details.Button.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnUp=candidates.LastOrDefault()?.Button,selectOnDown=controls.GetValueOrDefault("back")?.Button};
        }

        internal void BindInputRouter(TopDown3DInputRouter router)
        {
            if(promptInput!=null)promptInput.PromptDeviceChanged-=PromptDeviceChanged;
            promptInput=router;if(promptInput!=null)promptInput.PromptDeviceChanged+=PromptDeviceChanged;
            RefreshLayout();
        }
        private void PromptDeviceChanged(TopDown3DPromptDevice device){if(this!=null&&initialized)RefreshLayout();}
        private void OnDestroy(){if(promptInput!=null)promptInput.PromptDeviceChanged-=PromptDeviceChanged;}

    }
}
