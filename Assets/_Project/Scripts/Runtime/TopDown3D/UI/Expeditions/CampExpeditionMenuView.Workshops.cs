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
        private const string WorkbenchId="author.station.workbench";
        private string workshopStation=WorkbenchId,workshopRecipe;
        private ScrollRect workshopDetails,workshopStations;
        private Text workshopRecipeTitle,workshopRequirements,workshopBlocker;
        private Image workshopArt;
        private readonly Dictionary<string,CampMenuItemGlyphGraphic> workshopMarks=new(StringComparer.Ordinal);
        private CampMenuControl workshopTrack,workshopTree,workshopCraft;
        private bool workshopsBuilt;
        internal string WorkshopRecipe=>workshopRecipe;
        private bool WorkshopInteractive=>page=="workstations"&&!quoting&&pickerTarget==null&&GetComponent<CanvasGroup>().interactable;
        private void ConfigureWorkshopPlans()
        {
            if(!roots.TryGetValue("workstations",out var pageRoot)||stationTitle==null||stationBody==null)return;
            var list=pageRoot.transform.Find("Station list");workshopStations=list?.GetComponent<ScrollRect>();
            if(workshopStations!=null)
            {
                var viewport=new GameObject("Station rows viewport",typeof(RectTransform),typeof(RectMask2D),typeof(Image)).GetComponent<RectTransform>();viewport.SetParent(list,false);viewport.GetComponent<Image>().color=Color.clear;viewport.anchorMin=Vector2.zero;viewport.anchorMax=Vector2.one;viewport.offsetMin=Vector2.zero;viewport.offsetMax=new Vector2(0,-38);
                workshopStations.content.SetParent(viewport,false);workshopStations.content.anchorMin=new Vector2(0,1);workshopStations.content.anchorMax=Vector2.one;workshopStations.content.pivot=new Vector2(.5f,1);workshopStations.content.offsetMin=workshopStations.content.offsetMax=Vector2.zero;workshopStations.viewport=viewport;workshopStations.movementType=ScrollRect.MovementType.Clamped;
            }
            var detail=(RectTransform)stationTitle.transform.parent;detail.anchorMin=new Vector2(.328f,0);detail.anchorMax=Vector2.one;detail.offsetMin=detail.offsetMax=Vector2.zero;
            workshopDetails=CreateMenuScroll(detail,"Workshop detail","workshop-details");workshopDetails.viewport.offsetMin=new Vector2(20,20);workshopDetails.viewport.offsetMax=new Vector2(-20,-20);
            stationTitle.transform.SetParent(workshopDetails.content,false);stationBody.transform.SetParent(workshopDetails.content,false);stationBody.fontSize=16;fontSizes[stationBody]=16;
            workshopArt=new GameObject("Workshop recipe art",typeof(RectTransform),typeof(Image)).GetComponent<Image>();workshopArt.transform.SetParent(workshopDetails.content,false);workshopArt.preserveAspect=true;workshopArt.raycastTarget=false;
            workshopRecipeTitle=CatalogText(workshopDetails.content,"",22);workshopRequirements=CatalogText(workshopDetails.content,"",16);workshopBlocker=CatalogText(workshopDetails.content,"",16);workshopBlocker.color=style.accent;
            workshopCraft=ProjectButton(workshopDetails.content,"workshop:craft","Crafting unavailable");workshopTrack=ProjectButton(workshopDetails.content,"workshop:track","Track project +");workshopTree=ProjectButton(workshopDetails.content,"workshop:tree","View crafting tree");workshopCraft.Button.interactable=false;
            foreach(var control in controls.Values.Where(control=>control.key.StartsWith("station:",StringComparison.Ordinal)))
            {
                control.Configure(style,control.key,Color.clear);control.title.supportRichText=false;
                control.subtitle=CatalogText(control.transform,"",14);control.subtitle.name="Station availability";control.subtitle.color=style.muted;control.subtitle.supportRichText=false;
                control.cornerLabel=CatalogText(control.transform,"›",18);control.cornerLabel.color=style.accent;control.cornerLabel.alignment=TextAnchor.MiddleCenter;
                var mark=new GameObject("Station mark",typeof(RectTransform),typeof(CampMenuItemGlyphGraphic)).GetComponent<CampMenuItemGlyphGraphic>();mark.transform.SetParent(control.transform,false);mark.raycastTarget=false;workshopMarks[control.key]=mark;
                LinkMenuDirection(control.gameObject,data=>{if(WorkshopInteractive&&data.moveDir==MoveDirection.Right){events?.SetSelectedGameObject(workshopTrack.gameObject.activeInHierarchy&&workshopTrack.Button.IsInteractable()?workshopTrack.gameObject:workshopDetails.gameObject);data.Use();}});
            }
            var header=list?.GetComponentsInChildren<Text>(true).FirstOrDefault(text=>text.GetComponentInParent<CampMenuControl>()==null);if(header!=null)header.text="STATIONS  "+definition.stations.Length;
            foreach(var control in new[]{workshopTrack,workshopTree})LinkMenuDirection(control.gameObject,data=>MoveWorkshopDetail(control.key,data));
            LinkMenuDirection(workshopDetails.gameObject,data=>MoveWorkshopDetail("workshop-details",data));
            workshopRecipe=definition.rootId;workshopsBuilt=true;RefreshWorkshopPlans();
        }
        internal void OpenWorkshopRecipe(string rootId)
        {
            if(menuCatalog?.Tree(rootId)==null)return;
            workshopRecipe=rootId;
            // Only the scoped T1 assembly has a selected work-surface assignment.
            // Other catalog trees remain plans without inventing facility/process requirements.
            workshopStation=rootId==definition.rootId?WorkbenchId:null;
            RefreshWorkshopPlans();ShowPage("workstations");
            if(events!=null&&WorkshopInteractive)events.SetSelectedGameObject(workshopDetails.gameObject);
        }
        private void RefreshWorkshopPlans()
        {
            if(!workshopsBuilt)return;
            var station=definition.stations.FirstOrDefault(station=>station.id==workshopStation);
            stationTitle.text=station?.name??"Recipe plan";stationBody.text=station?.purpose??"A workstation and process have not been assigned to this recipe plan.";
            foreach(var stationRow in definition.stations)
            {
                if(!controls.TryGetValue("station:"+stationRow.id,out var control))continue;bool selected=stationRow.id==workshopStation;control.SetSelected(selected);
                control.subtitle.text=stationRow.id==WorkbenchId?"Gathering plan · Crafting unavailable":stationRow.id.StartsWith("proposal.",StringComparison.Ordinal)?"Proposed facility":"Recipe not modelled";
                control.subtitle.color=selected?style.ink:style.muted;control.cornerLabel.color=selected?style.ink:style.accent;
                string mark=stationRow.id==WorkbenchId?"station-mark.workbench":stationRow.id=="author.station.dust_refinery"?"station-mark.dust":stationRow.id=="camp-menu.cooking"?"station-mark.cooking":"station-mark.generic";
                workshopMarks[control.key].SetItem(mark,selected?style.accent:style.muted);
            }
            bool recipe=workshopRecipe!=null&&menuCatalog?.Tree(workshopRecipe)!=null;
            stationBody.gameObject.SetActive(!recipe);
            workshopRecipeTitle.gameObject.SetActive(recipe);workshopRequirements.gameObject.SetActive(recipe);workshopArt.gameObject.SetActive(recipe||station!=null);workshopTrack.gameObject.SetActive(recipe);workshopTree.gameObject.SetActive(recipe);
            if(!recipe&&station!=null)SetItemArtwork(workshopArt,station.id);
            if(recipe)
            {
                var item=definition.Find(workshopRecipe);bool qualified=item!=null&&item.inputs.Length>0;workshopRecipeTitle.text=ItemName(workshopRecipe);SetItemArtwork(workshopArt,workshopRecipe);
                string Inputs(CampExpeditionIngredient input)
                {
                    int eligible=presentation.stacks.Where(stack=>stack.itemId==input.itemId&&(stack.owner=="booter"||stack.owner=="legger"&&presentation.leggerAccessible||stack.owner=="storage"&&presentation.townAccess)).Sum(stack=>stack.quantity);
                    return ItemName(input.itemId)+$"\n{eligible} eligible / {input.quantity} required";
                }
                workshopRequirements.text=qualified?"Direct requirements for one additional output\n\n"+string.Join("\n\n",item.inputs.Select(Inputs)):"Recipe quantities are unavailable. This plan cannot confirm material readiness.";
                workshopRequirements.text+="\n\nTarget output\n"+ItemName(workshopRecipe)+"\n\nOutput destination is unavailable while crafting is disabled.";
                bool tracked=presentation.planning.projects.Any(project=>project.rootId==workshopRecipe);workshopTrack.SetContent(tracked?"Project tracked":"Track project +");SetProjectEnabled(workshopTrack.Button,!tracked);SetProjectEnabled(workshopTree.Button,true);
            }
            workshopBlocker.text=!presentation.townAccess?"Station crafting requires returning to town.":recipe?"Crafting is unavailable. Gather and store materials for now.":"No qualified recipe is available at this station. Crafting is unavailable.";
            SetProjectEnabled(workshopCraft.Button,false);ReflowWorkshopPlans();
        }
        private bool HandleWorkshopPlanAction(string key)
        {
            if(key.StartsWith("station:",StringComparison.Ordinal))
            {if(WorkshopInteractive){workshopStation=key.Substring(8);workshopRecipe=workshopStation==WorkbenchId?definition.rootId:null;RefreshWorkshopPlans();}return true;}
            if(key is not ("workshop:track" or "workshop:tree" or "workshop:craft" or "workshop-details"))return false;
            if(!WorkshopInteractive||workshopRecipe==null)return true;
            if(key=="workshop:track")Command?.Invoke("project:add:"+workshopRecipe);
            else if(key=="workshop:tree"){if(treeSearch!=null)treeSearch.SetTextWithoutNotify("");OpenDisplayTree(workshopRecipe,true);ShowPage("tree");}
            return true;
        }
        private void ReflowWorkshopPlans()
        {
            if(!workshopsBuilt)return;float width=workshopDetails.viewport.rect.width-8;if(width<=0)return;float top=0;
            if(workshopStations!=null)
            {
                float listTop=0;
                foreach(var station in definition.stations)
                    if(controls.TryGetValue("station:"+station.id,out var row))
                    {
                        float textWidth=workshopStations.viewport.rect.width-92,rowTitle=ProjectTextHeight(row.title,textWidth),note=ProjectTextHeight(row.subtitle,textWidth,20),height=Mathf.Max(76,rowTitle+note+28);
                        TopRegion((RectTransform)row.transform,listTop,height);TopRegion(row.title.rectTransform,12,rowTitle,54,38);TopRegion(row.subtitle.rectTransform,18+rowTitle,note,54,38);TopRegion(row.cornerLabel.rectTransform,(height-28)*.5f,28,workshopStations.viewport.rect.width-30,10);
                        var mark=workshopMarks[row.key].rectTransform;mark.anchorMin=mark.anchorMax=mark.pivot=new Vector2(0,.5f);mark.anchoredPosition=new Vector2(12,0);mark.sizeDelta=new Vector2(30,30);listTop+=height+4;
                    }
                workshopStations.content.sizeDelta=new Vector2(0,Mathf.Max(listTop,workshopStations.viewport.rect.height));float listMaximum=Mathf.Max(0,listTop-workshopStations.viewport.rect.height);workshopStations.content.anchoredPosition=new Vector2(0,Mathf.Clamp(workshopStations.content.anchoredPosition.y,0,listMaximum));
            }
            bool recipe=workshopRecipeTitle.gameObject.activeSelf;
            if(workshopArt.gameObject.activeSelf){TopRegion(workshopArt.rectTransform,0,104);top=116;}
            float title=ProjectTextHeight(stationTitle,width,32);TopRegion(stationTitle.rectTransform,top,title,4,4);top+=title+12;
            if(stationBody.gameObject.activeSelf){float body=ProjectTextHeight(stationBody,width);TopRegion(stationBody.rectTransform,top,body,4,4);top+=body+20;}
            if(recipe)
            {float name=ProjectTextHeight(workshopRecipeTitle,width,30);TopRegion(workshopRecipeTitle.rectTransform,top,name,4,4);top+=name+14;float requirements=ProjectTextHeight(workshopRequirements,width);TopRegion(workshopRequirements.rectTransform,top,requirements,4,4);top+=requirements+20;}
            float blocker=ProjectTextHeight(workshopBlocker,width);TopRegion(workshopBlocker.rectTransform,top,blocker,4,4);top+=blocker+16;
            foreach(var action in new[]{workshopCraft,workshopTrack,workshopTree}.Where(action=>action.gameObject.activeSelf))
            {float height=Mathf.Max(48,ProjectTextHeight(action.title,width-20)+12);TopRegion((RectTransform)action.transform,top,height,4,4);top+=height+10;}
            workshopDetails.content.sizeDelta=new Vector2(0,Mathf.Max(top,workshopDetails.viewport.rect.height));float maximum=Mathf.Max(0,top-workshopDetails.viewport.rect.height);workshopDetails.content.anchoredPosition=new Vector2(0,Mathf.Clamp(workshopDetails.content.anchoredPosition.y,0,maximum));
        }
        private void MoveWorkshopDetail(string key,AxisEventData data)
        {
            if(!WorkshopInteractive||events==null)return;
            if(data.moveDir==MoveDirection.Left)
            {var station=controls.GetValueOrDefault("station:"+workshopStation)??controls.Values.FirstOrDefault(control=>control.key.StartsWith("station:",StringComparison.Ordinal));events.SetSelectedGameObject(station?.gameObject);}
            else if(key=="workshop-details")
            {if(data.moveDir==MoveDirection.Down&&workshopTrack.gameObject.activeInHierarchy&&workshopTrack.Button.IsInteractable())events.SetSelectedGameObject(workshopTrack.gameObject);else if(data.moveDir==MoveDirection.Right&&workshopTree.gameObject.activeInHierarchy)events.SetSelectedGameObject(workshopTree.gameObject);else if(data.moveDir is MoveDirection.Up or MoveDirection.Down)ScrollInspector(data.moveDir==MoveDirection.Up?1:-1,.15f);}
            else if(data.moveDir==MoveDirection.Up)events.SetSelectedGameObject(key=="workshop:tree"&&workshopTrack.Button.IsInteractable()?workshopTrack.gameObject:workshopDetails.gameObject);
            else if(data.moveDir==MoveDirection.Down&&key=="workshop:track")events.SetSelectedGameObject(workshopTree.gameObject);
            data.Use();
        }
    }
}
