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
        private readonly List<CampMenuControl> trackedCards=new(),trackedResultPool=new();
        private readonly Dictionary<string,CampMenuControl> trackedResults=new(StringComparer.Ordinal);
        private readonly Dictionary<string,int> trackedMatchCounts=new(StringComparer.Ordinal);
        private readonly CampTrackedStickRepeat trackedLeftRepeat=new(),trackedRightRepeat=new();
        private ScrollRect trackedGridScroll,trackedResultsScroll;
        private RectTransform trackedPanel;
        private Text trackedHeading,trackedEmpty,trackedResultsHeading;
        private string trackedFilter="",trackedSelected;
        private string[] trackedSnapshot=Array.Empty<string>(),trackedAll=Array.Empty<string>(),trackedResultIds=Array.Empty<string>();
        private int trackedColumns=1,trackedWindowStart=-1,trackedWindowCount;
        private float trackedWidth=-1,trackedHeight=-1,trackedNameHeight=40,trackedNoteHeight=36,trackedCellHeight=220,trackedCellWidth=145;
        private bool trackedBuilt,rebindingTracked,trackedLarge;
        private double trackedClock;
        internal string TrackedFilter=>trackedFilter;
        internal string TrackedSelected=>trackedSelected;
        internal string[] TrackedVisibleIds=>visibleBookmarks;
        internal int TrackedCardPoolCount=>trackedCards.Count;
        private bool TrackedInteractive=>trackedBuilt&&page=="tracked"&&!quoting&&pickerTarget==null&&GetComponent<CanvasGroup>().interactable;
        internal bool TrackedCanUntrack
        {
            get{var key=events?.currentSelectedGameObject?.GetComponent<CampMenuControl>()?.key;return TrackedInteractive&&trackedSelected!=null&&(key?.StartsWith("bookmark:",StringComparison.Ordinal)==true||key is "track" or "tracked-grid" or "inspector-title" or "hint:west");}
        }

        private void ConfigureTrackedBrowser()
        {
            if(menuCatalog==null||!roots.TryGetValue("tracked",out var pageRoot))return;
            var results=pageRoot.transform.Find("Reverse uses");trackedPanel=pageRoot.transform.Find("Bookmarked items") as RectTransform;if(results==null||trackedPanel==null)return;
            trackedResultsHeading=results.Find("CRAFTING RESULTS")?.GetComponentInChildren<Text>();trackedHeading=trackedPanel.Find("TRACKED ITEMS")?.GetComponentInChildren<Text>();
            foreach(var key in new[]{"tracked-filter:all","tracked-filter:project"})if(controls.TryGetValue(key,out var control)){controls.Remove(key);control.key="tracked-result-pool:"+trackedResultPool.Count;control.gameObject.SetActive(false);trackedResultPool.Add(control);}
            foreach(var card in controls.Values.Where(c=>c.key.StartsWith("bookmark:",StringComparison.Ordinal)).ToArray())
            {controls.Remove(card.key);card.key="tracked-pool:"+trackedCards.Count;card.gameObject.SetActive(false);trackedCards.Add(card);}
            trackedResultsScroll=CreateMenuScroll(results,"Tracked result list");trackedResultsScroll.viewport.offsetMin=new Vector2(10,10);trackedResultsScroll.viewport.offsetMax=new Vector2(-10,-38);
            foreach(var control in trackedResultPool)control.transform.SetParent(trackedResultsScroll.content,false);
            trackedGridScroll=CreateMenuScroll(trackedPanel,"Tracked grid viewport","tracked-grid");trackedGridScroll.viewport.offsetMin=new Vector2(12,12);trackedGridScroll.viewport.offsetMax=new Vector2(-12,-48);
            foreach(var card in trackedCards)card.transform.SetParent(trackedGridScroll.content,false);
            LinkMenuDirection(trackedGridScroll.gameObject,MoveTrackedGrid);
            foreach(var key in new[]{"track","inspector-title"})if(controls.TryGetValue(key,out var control))LinkMenuDirection(control.gameObject,data=>{if(page=="tracked")MoveTrackedGrid(data);});
            trackedEmpty=CatalogText(trackedGridScroll.viewport,"No tracked items yet. Highlight an item in the crafting tree to track it.",16);TopRegion(trackedEmpty.rectTransform,12,150,10,10);
            trackedGridScroll.onValueChanged.AddListener(_=>RefreshTrackedWindow());trackedBuilt=true;
        }

        private void RefreshTracked()
        {
            if(!trackedBuilt)return;
            bool changed=!trackedSnapshot.SequenceEqual(presentation.tracked,StringComparer.Ordinal);
            if(changed)
            {
                trackedSnapshot=presentation.tracked.ToArray();trackedAll=trackedSnapshot.Where(id=>!string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.Ordinal).OrderBy(ItemName,StringComparer.OrdinalIgnoreCase).ThenBy(id=>id,StringComparer.Ordinal).ToArray();
                trackedResultIds=menuCatalog.ResultsUsing(trackedAll);var trackedSet=new HashSet<string>(trackedAll,StringComparer.Ordinal);trackedMatchCounts.Clear();
                foreach(var id in trackedResultIds)trackedMatchCounts[id]=menuCatalog.Inputs(id,id).Count(trackedSet.Contains);
                if(!trackedResultIds.Contains(trackedFilter))trackedFilter="";
                SetTrackedCollection(false);RebuildTrackedResults();
            }
            ReflowTracked(changed);
            if(page=="tracked")RefreshTrackedInspector();
        }
        private void SetTrackedCollection(bool resetScroll)
        {
            var allowed=trackedFilter.Length==0?null:menuCatalog.Inputs(trackedFilter,trackedFilter);
            visibleBookmarks=trackedAll.Where(id=>allowed==null||allowed.Contains(id)).ToArray();
            if(!visibleBookmarks.Contains(trackedSelected))trackedSelected=visibleBookmarks.FirstOrDefault();
            trackedWidth=-1;trackedWindowStart=-1;
            if(resetScroll){trackedGridScroll.StopMovement();trackedGridScroll.content.anchoredPosition=Vector2.zero;}
        }
        private void RebuildTrackedResults()
        {
            if(!trackedBuilt)return;
            var ids=new[]{""}.Concat(trackedResultIds).ToArray();
            while(trackedResultPool.Count<ids.Length)trackedResultPool.Add(CloneMenuControl(trackedResultPool[0],trackedResultsScroll.content,"tracked-result-pool:"+trackedResultPool.Count));
            foreach(var control in trackedResultPool){controls.Remove(control.key);control.key="tracked-result-pool:"+trackedResultPool.IndexOf(control);}
            trackedResults.Clear();float top=0;
            if(trackedResultsHeading!=null)trackedResultsHeading.text="CRAFTING RESULTS  "+trackedResultIds.Length;
            float heading=Mathf.Max(24,trackedResultsHeading?.preferredHeight+4??24);
            if(trackedResultsHeading!=null)((RectTransform)trackedResultsHeading.transform.parent).SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top,0,heading);
            for(int i=0;i<trackedResultPool.Count;i++)
            {
                var control=trackedResultPool[i];if(i>=ids.Length){control.gameObject.SetActive(false);continue;}
                string id=ids[i],key="tracked-filter:"+(id.Length==0?"all":id);control.key=control.name=key;controls.Add(key,control);trackedResults.Add(id,control);control.Configure(style,key,style.surface);control.gameObject.SetActive(true);control.Button.navigation=new Navigation{mode=Navigation.Mode.None};LinkMenuDirection(control.gameObject,MoveTrackedGrid);
                control.SetContent(id.Length==0?"All tracked items":ItemName(id));control.title.alignment=TextAnchor.MiddleLeft;control.title.fontSize=large?19:16;fontSizes[control.title]=16;
                if(control.subtitle==null)control.subtitle=CatalogText(control.transform,"",14);control.subtitle.text=(id.Length==0?trackedAll.Length:trackedMatchCounts[id]).ToString();control.subtitle.alignment=TextAnchor.MiddleRight;fontSizes[control.subtitle]=14;control.subtitle.fontSize=large?16:14;
                float width=Mathf.Max(40,trackedResultsScroll.viewport.rect.width-54);float labelHeight=control.title.cachedTextGeneratorForLayout.GetPreferredHeight(control.title.text,control.title.GetGenerationSettings(new Vector2(width,0)))/control.title.pixelsPerUnit;
                float height=Mathf.Max(large?60:52,labelHeight+14);
                if(i==0){control.transform.SetParent(trackedResultsScroll.transform.parent,false);TopRegion((RectTransform)control.transform,heading+14,height,10,10);trackedResultsScroll.viewport.offsetMax=new Vector2(-10,-heading-height-26);}
                else{control.transform.SetParent(trackedResultsScroll.content,false);TopRegion((RectTransform)control.transform,top,height);top+=height+6;}
                control.title.rectTransform.anchorMin=Vector2.zero;control.title.rectTransform.anchorMax=Vector2.one;control.title.rectTransform.offsetMin=new Vector2(8,6);control.title.rectTransform.offsetMax=new Vector2(-46,-6);
                control.subtitle.rectTransform.anchorMin=new Vector2(1,0);control.subtitle.rectTransform.anchorMax=Vector2.one;control.subtitle.rectTransform.offsetMin=new Vector2(-42,4);control.subtitle.rectTransform.offsetMax=new Vector2(-6,-4);control.SetSelected(id==trackedFilter);
            }
            trackedResultsScroll.content.sizeDelta=new Vector2(0,Mathf.Max(top,trackedResultsScroll.viewport.rect.height));
        }
        internal void SelectTrackedFilter(string id)
        {
            if(!TrackedInteractive)return;if(id.Length>0&&!trackedResultIds.Contains(id))id="";
            trackedFilter=id;SetTrackedCollection(true);ReflowTracked(true);foreach(var row in trackedResults)row.Value.SetSelected(row.Key==id);RefreshTrackedInspector();
            if(events!=null&&trackedResults.TryGetValue(id,out var control)){events.SetSelectedGameObject(control.gameObject);RevealFocusedControl(control);}
        }
        private void ReflowTracked(bool force=false)
        {
            if(!trackedBuilt)return;
            if(trackedHeading!=null){trackedHeading.text=(trackedFilter.Length==0?"TRACKED ITEMS":ItemName(trackedFilter))+"  "+visibleBookmarks.Length;float heading=Mathf.Max(24,trackedHeading.preferredHeight+4);((RectTransform)trackedHeading.transform.parent).SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top,0,heading);trackedGridScroll.viewport.offsetMax=new Vector2(-12,-heading-18);}
            float width=trackedGridScroll.viewport.rect.width,height=trackedGridScroll.viewport.rect.height;if(width<=0)return;
            bool reflow=force||Mathf.Abs(width-trackedWidth)>.1f||Mathf.Abs(height-trackedHeight)>.1f||trackedLarge!=large;
            bool gridFocused=events?.currentSelectedGameObject==trackedGridScroll.gameObject||trackedCards.Any(card=>card.gameObject==events?.currentSelectedGameObject);
            if(reflow)
            {
                trackedWidth=width;trackedHeight=height;trackedLarge=large;trackedColumns=Mathf.Max(1,Mathf.FloorToInt((width+12)/156));trackedCellWidth=(width-(trackedColumns-1)*12)/trackedColumns;
                var template=trackedCards[0];template.title.fontSize=large?19:16;template.subtitle.fontSize=large?16:14;trackedNameHeight=24;trackedNoteHeight=large?42:36;
                foreach(var id in visibleBookmarks)
                {
                    var label=TrackedTileName(id);trackedNameHeight=Mathf.Max(trackedNameHeight,template.title.cachedTextGeneratorForLayout.GetPreferredHeight(label,template.title.GetGenerationSettings(new Vector2(Mathf.Max(20,trackedCellWidth-16),0)))/template.title.pixelsPerUnit);
                    var item=menuCatalog.Find(id);string note=(item?.category??"Item definition unavailable")+"\n"+(CampMenuRarityGraphic.TryColor(item?.rarity,out _)?item.rarity:"Unassigned");trackedNoteHeight=Mathf.Max(trackedNoteHeight,template.subtitle.cachedTextGeneratorForLayout.GetPreferredHeight(note,template.subtitle.GetGenerationSettings(new Vector2(Mathf.Max(20,trackedCellWidth-34),0)))/template.subtitle.pixelsPerUnit+2);
                }
                trackedCellHeight=Mathf.Max(large?242:220,24+(large?104:96)+10+trackedNameHeight+8+trackedNoteHeight+14);
                trackedGridScroll.content.sizeDelta=new Vector2(0,Mathf.Max(trackedGridScroll.viewport.rect.height,Mathf.Ceil(visibleBookmarks.Length/(float)trackedColumns)*(trackedCellHeight+12)));
                float maximum=Mathf.Max(0,trackedGridScroll.content.rect.height-trackedGridScroll.viewport.rect.height);if(trackedGridScroll.content.anchoredPosition.y<0||trackedGridScroll.content.anchoredPosition.y>maximum){trackedGridScroll.StopMovement();trackedGridScroll.content.anchoredPosition=new Vector2(0,Mathf.Clamp(trackedGridScroll.content.anchoredPosition.y,0,maximum));}
                trackedWindowStart=-1;RebuildTrackedResults();
            }
            trackedEmpty.gameObject.SetActive(visibleBookmarks.Length==0);RefreshTrackedWindow();
            if(reflow&&gridFocused&&TrackedInteractive)FocusTrackedSelection();
        }
        private string TrackedTileName(string id){var name=ItemName(id);return name.Length>120?name.Substring(0,117)+"…":name;}
        private void RefreshTrackedWindow()
        {
            if(!trackedBuilt||rebindingTracked||trackedGridScroll.viewport.rect.height<=0)return;
            int firstRow=Mathf.Max(0,Mathf.FloorToInt(Mathf.Max(0,trackedGridScroll.content.anchoredPosition.y)/(trackedCellHeight+12))-1);
            int start=Mathf.Min(firstRow*trackedColumns,visibleBookmarks.Length),count=Mathf.Min(visibleBookmarks.Length-start,trackedColumns*(Mathf.CeilToInt(trackedGridScroll.viewport.rect.height/(trackedCellHeight+12))+2));
            if(start==trackedWindowStart&&count==trackedWindowCount)return;
            rebindingTracked=true;try
            {
                if(events!=null&&trackedCards.Any(card=>card.gameObject==events.currentSelectedGameObject))events.SetSelectedGameObject(TrackedInteractive?trackedGridScroll.gameObject:null);
                foreach(var card in trackedCards){controls.Remove(card.key);card.key="tracked-pool:"+trackedCards.IndexOf(card);card.gameObject.SetActive(false);}
                while(trackedCards.Count<count)trackedCards.Add(CloneMenuControl(trackedCards[0],trackedGridScroll.content,"tracked-pool:"+trackedCards.Count));
                for(int i=0;i<count;i++)
                {
                    int index=start+i;var id=visibleBookmarks[index];var card=trackedCards[i];card.key=card.name="bookmark:"+index;card.Configure(style,card.key,style.surface);controls.Add(card.key,card);card.gameObject.SetActive(true);card.Button.navigation=new Navigation{mode=Navigation.Mode.None};LinkMenuDirection(card.gameObject,MoveTrackedGrid);
                    var rect=(RectTransform)card.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(index%trackedColumns*(trackedCellWidth+12),-index/trackedColumns*(trackedCellHeight+12));rect.sizeDelta=new Vector2(trackedCellWidth,trackedCellHeight);
                    var item=menuCatalog.Find(id);card.SetContent(TrackedTileName(id),ItemArt(id),(item?.category??"Item definition unavailable")+"\n"+(CampMenuRarityGraphic.TryColor(item?.rarity,out _)?item.rarity:"Unassigned"));SetItemArtwork(card.art,id);AddRarityCorner(card,item?.rarity);
                    card.title.fontSize=large?19:16;fontSizes[card.title]=16;TopRegion(card.title.rectTransform,24+(large?104:96)+10,trackedNameHeight,8,8);
                    card.subtitle.fontSize=large?16:14;fontSizes[card.subtitle]=14;card.subtitle.alignment=TextAnchor.UpperCenter;TopRegion(card.subtitle.rectTransform,24+(large?104:96)+18+trackedNameHeight,trackedNoteHeight,8,26);
                    TopRegion(card.art.rectTransform,24,large?104:96,10,10);
                    if(card.cornerLabel==null)card.cornerLabel=CatalogText(card.transform,"",14);card.cornerLabel.text=item?.tier??"";fontSizes[card.cornerLabel]=14;card.cornerLabel.rectTransform.anchorMin=card.cornerLabel.rectTransform.anchorMax=card.cornerLabel.rectTransform.pivot=new Vector2(0,1);card.cornerLabel.rectTransform.anchoredPosition=new Vector2(8,-4);card.cornerLabel.rectTransform.sizeDelta=new Vector2(70,22);card.SetSelected(id==trackedSelected);
                }
                trackedWindowStart=start;trackedWindowCount=count;
            }
            finally{rebindingTracked=false;}
        }
        private void PreviewTracked(int index)
        {
            if(index<0||index>=visibleBookmarks.Length)return;trackedSelected=visibleBookmarks[index];foreach(var card in trackedCards)if(card.key.StartsWith("bookmark:",StringComparison.Ordinal))card.SetSelected(card.key=="bookmark:"+index);RefreshTrackedInspector();
        }
        private void RefreshTrackedInspector()
        {
            if(page!="tracked")return;selectedStack=null;Inspect(trackedSelected);
            if(trackedSelected!=null)
            {
                var item=menuCatalog.Find(trackedSelected);inspectorTitle.text=ItemName(trackedSelected);inspectorBody.text=item==null?"Item definition unavailable. This saved bookmark can still be removed.":(item.category+"\n\n"+(string.IsNullOrWhiteSpace(item.purpose)?"No description recorded.":item.purpose));SetItemArtwork(inspectorArt,trackedSelected);LayoutInspector(true);
            }
            else inspectorBody.text="No tracked items yet. Highlight an item in the crafting tree to track it.";
            if(controls.TryGetValue("track",out var action)){action.gameObject.SetActive(trackedSelected!=null);action.SetContent("Tracked · Remove");}
            LayoutInspector(trackedSelected!=null);
            RefreshTrackedUntrackAvailability();
        }
        private void RefreshTrackedUntrackAvailability()
        {if(page=="tracked"&&!quoting&&pickerTarget==null&&controls.TryGetValue("hint:west",out var west)){west.Button.interactable=TrackedCanUntrack;west.SetContent("Untrack");}}
        private void FocusTrackedPage()
        {
            if(events==null||!GetComponent<CanvasGroup>().interactable)return;
            if(pageFocus.TryGetValue("tracked",out var key)&&!key.StartsWith("bookmark:",StringComparison.Ordinal)&&controls.TryGetValue(key,out var saved)&&saved.gameObject.activeInHierarchy&&saved.Button.IsInteractable()){events.SetSelectedGameObject(saved.gameObject);return;}
            FocusTrackedSelection();
        }
        internal void FocusTrackedSelection()
        {
            if(!TrackedInteractive||events==null)return;
            int index=Array.IndexOf(visibleBookmarks,trackedSelected);if(index<0){events.SetSelectedGameObject(trackedResults.TryGetValue("",out var all)?all.gameObject:trackedGridScroll.gameObject);return;}
            float top=index/trackedColumns*(trackedCellHeight+12),bottom=top+trackedCellHeight,offset=trackedGridScroll.content.anchoredPosition.y;
            if(trackedCellHeight>trackedGridScroll.viewport.rect.height||top<offset)offset=top;else if(bottom>offset+trackedGridScroll.viewport.rect.height)offset=Mathf.Max(0,bottom-trackedGridScroll.viewport.rect.height);
            trackedGridScroll.StopMovement();trackedGridScroll.content.anchoredPosition=new Vector2(0,offset);RefreshTrackedWindow();
            events.SetSelectedGameObject(controls.TryGetValue("bookmark:"+index,out var card)?card.gameObject:trackedGridScroll.gameObject);
        }
        private void MoveTrackedGrid(AxisEventData data)
        {
            if(!TrackedInteractive||visibleBookmarks.Length==0)return;
            int index=Mathf.Max(0,Array.IndexOf(visibleBookmarks,trackedSelected)),next=TrackedGridTarget(index,visibleBookmarks.Length,trackedColumns,data.moveDir);trackedSelected=visibleBookmarks[next];FocusTrackedSelection();RefreshTrackedInspector();data.Use();
        }
        internal static int TrackedGridTarget(int index,int count,int columns,MoveDirection direction)
        {
            if(index<0||index>=count||count<1)return -1;columns=Mathf.Max(1,columns);
            return direction switch{MoveDirection.Left=>index%columns>0?index-1:index,MoveDirection.Right=>index%columns<columns-1&&index+1<count?index+1:index,MoveDirection.Up=>index>=columns?index-columns:index,MoveDirection.Down=>index+columns<count?index+columns:index<(count-1)/columns*columns?count-1:index,_=>index};
        }
        internal void ResetTrackedInput(){trackedLeftRepeat.Reset();trackedRightRepeat.Reset();trackedClock=0;}
        internal void AdvanceTrackedInput(Vector2 left,Vector2 right,float seconds)
        {
            if(page!="tracked"||quoting||pickerTarget!=null||!GetComponent<CanvasGroup>().interactable){ResetTrackedInput();return;}
            trackedClock+=Mathf.Clamp(seconds,0,.1f);int result=trackedLeftRepeat.Sample(left.y,trackedClock),inspector=trackedRightRepeat.Sample(right.x,trackedClock);
            if(result!=0){var ids=new[]{""}.Concat(trackedResultIds).ToArray();int at=Array.IndexOf(ids,trackedFilter),next=Mathf.Clamp(at-result,0,ids.Length-1);if(next!=at)SelectTrackedFilter(ids[next]);}
            if(inspector!=0&&trackedSelected!=null&&events!=null)
            {var title=inspectorTitleViewport.GetComponent<ScrollRect>();events.SetSelectedGameObject(inspector<0&&title.content.rect.height>title.viewport.rect.height+1?title.gameObject:controls["track"].gameObject);}
            var scroll=events?.currentSelectedGameObject==inspectorTitleViewport?.gameObject?inspectorTitleViewport?.GetComponent<ScrollRect>():inspectorBodyViewport?.GetComponent<ScrollRect>();if(scroll!=null)scroll.verticalNormalizedPosition=Mathf.Clamp01(scroll.verticalNormalizedPosition+right.y*seconds);
        }
    }
}
