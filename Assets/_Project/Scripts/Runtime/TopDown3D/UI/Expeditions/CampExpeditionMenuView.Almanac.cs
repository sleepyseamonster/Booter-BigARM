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
        private sealed class AlmanacCard {public CampMenuControl control;public Text address,category;public int slot=-1;}
        private InputField almanacSearch;
        private ScrollRect almanacCategories,almanacGrid,almanacDetail;
        private CampMenuControl almanacStatus,almanacSort,almanacPrevious,almanacNext;
        private Text almanacProgress,almanacResults,almanacPagination,almanacFacts,almanacEmpty;
        private Image almanacArt;
        private readonly List<AlmanacCard> almanacCards=new();
        private CampMenuControl[] almanacCategoryButtons,almanacFolds;
        private Text[] almanacFoldBodies;
        private readonly List<CampMenuControl> almanacUses=new();
        private string[] almanacUseRoots=Array.Empty<string>();
        private string almanacCategory="",almanacStatusFilter="all";
        private bool almanacCatalogOrder;
        private int almanacPage,almanacSlot=-1;
        private CampAlmanacIndex almanacIndex;
        private readonly bool[] almanacExpanded=new bool[3];
        internal int AlmanacSelectedSlot=>almanacSlot;
        private void ConfigureAlmanac()
        {
            var root=ResourceRoot("almanac","Item almanac","Every item has a place. Discovery fills in the details.");
            var left=ResourcePanel(root,"Almanac categories",0,.195f);var middle=ResourcePanel(root,"Almanac collection",.205f,.695f);var right=ResourcePanel(root,"Almanac record",.705f,1);
            almanacProgress=CatalogText(left,"",18);TopRegion(almanacProgress.rectTransform,14,76,14,14);
            almanacCategories=CreateMenuScroll(left,"Almanac category rail");almanacCategories.viewport.offsetMin=new Vector2(12,12);almanacCategories.viewport.offsetMax=new Vector2(-12,-100);
            var categories=CampMenuResources.Almanac(menuCatalog,Array.Empty<string>()).categories;
            almanacCategoryButtons=new[]{""}.Concat(categories.Select(c=>c.name)).Select((category,index)=>ProjectButton(almanacCategories.content,"almanac-category:"+index,category)).ToArray();
            almanacSearch=ResourceSearch(middle,"Almanac search","Search discovered items…");TopRegion((RectTransform)almanacSearch.transform,12,44,12,12);almanacSearch.onValueChanged.AddListener(_=>{almanacPage=0;almanacGrid.content.anchoredPosition=Vector2.zero;RefreshAlmanac();ReflowResources();});
            almanacStatus=ProjectButton(middle,"almanac-status","All entries");almanacSort=ProjectButton(middle,"almanac-sort","Discovered first");almanacResults=CatalogText(middle,"",14);
            almanacGrid=CreateMenuScroll(middle,"Almanac entries");almanacGrid.viewport.offsetMin=new Vector2(12,64);almanacGrid.viewport.offsetMax=new Vector2(-12,-144);
            almanacEmpty=CatalogText(almanacGrid.content,"No entries match. Try another category, discovery filter or search.",18);
            almanacPrevious=ProjectButton(middle,"almanac-previous","← Previous");almanacNext=ProjectButton(middle,"almanac-next","Next →");almanacPagination=CatalogText(middle,"",14);almanacPagination.alignment=TextAnchor.MiddleCenter;
            almanacDetail=CreateMenuScroll(right,"Almanac detail","almanac-detail");almanacDetail.viewport.offsetMin=new Vector2(18,18);almanacDetail.viewport.offsetMax=new Vector2(-18,-18);
            almanacArt=new GameObject("Record artwork",typeof(RectTransform),typeof(Image)).GetComponent<Image>();almanacArt.transform.SetParent(almanacDetail.content,false);almanacArt.preserveAspect=true;almanacArt.raycastTarget=false;
            almanacFacts=CatalogText(almanacDetail.content,"",18);
            almanacFolds=new[]{"Acquisition","Crafting & processing","Limitations"}.Select((label,index)=>ProjectButton(almanacDetail.content,"almanac-fold:"+index,label)).ToArray();
            almanacFoldBodies=Enumerable.Range(0,3).Select(_=>CatalogText(almanacDetail.content,"",16)).ToArray();
            LinkMenuDirection(almanacSearch.gameObject,data=>{if(almanacSearch.isFocused)return;if(data.moveDir==MoveDirection.Down)FocusResource(almanacStatus);else if(data.moveDir==MoveDirection.Up)FocusResource(controls["resource-back:almanac"]);else if(data.moveDir==MoveDirection.Left)FocusResource(almanacCategoryButtons[0]);else if(data.moveDir==MoveDirection.Right)FocusResource(controls["almanac-detail"]);data.Use();});
            LinkMenuDirection(almanacStatus.gameObject,data=>MoveAlmanacTools(data,false));LinkMenuDirection(almanacSort.gameObject,data=>MoveAlmanacTools(data,true));
            LinkMenuDirection(almanacDetail.gameObject,data=>{if(data.moveDir==MoveDirection.Left)FocusAlmanacSelection();else if(data.moveDir==MoveDirection.Down&&DetailActions().FirstOrDefault() is CampMenuControl action)FocusResource(action);else if(data.moveDir is MoveDirection.Up or MoveDirection.Down)ScrollInspector(data.moveDir==MoveDirection.Up?1:-1,.18f);data.Use();});
            LinkMenuDirection(almanacPrevious.gameObject,data=>MoveAlmanacPager(data,false));LinkMenuDirection(almanacNext.gameObject,data=>MoveAlmanacPager(data,true));
            RefreshAlmanac();
        }
        private void MoveAlmanacTools(AxisEventData data,bool sort)
        {
            if(data.moveDir==MoveDirection.Up)events?.SetSelectedGameObject(almanacSearch.gameObject);
            else if(data.moveDir==MoveDirection.Down)FocusAlmanacSelection();
            else if(data.moveDir==MoveDirection.Left)FocusResource(sort?almanacStatus:almanacCategoryButtons[0]);
            else if(data.moveDir==MoveDirection.Right)FocusResource(sort?controls["almanac-detail"]:almanacSort);data.Use();
        }
        private void MoveAlmanacPager(AxisEventData data,bool next)
        {if(data.moveDir==MoveDirection.Up)FocusAlmanacSelection();else if(data.moveDir==MoveDirection.Left)FocusResource(next?almanacPrevious:almanacCategoryButtons[0]);else if(data.moveDir==MoveDirection.Right)FocusResource(next?controls["almanac-detail"]:almanacNext);data.Use();}
        private void FocusAlmanacSelection()
        {FocusResource(almanacCards.FirstOrDefault(card=>card.slot==almanacSlot&&card.control.gameObject.activeSelf)?.control??almanacStatus);}
        private AlmanacCard CreateAlmanacCard(int index)
        {
            var control=ProjectButton(almanacGrid.content,"almanac-entry:"+index,"");var card=new AlmanacCard{control=control,address=CatalogText(control.transform,"",14),category=CatalogText(control.transform,"",14)};
            control.art=new GameObject("Item artwork",typeof(RectTransform),typeof(Image)).GetComponent<Image>();control.art.transform.SetParent(control.transform,false);control.art.preserveAspect=true;control.art.raycastTarget=false;return card;
        }
        private void RefreshAlmanac(int locateSlot=-1)
        {
            if(almanacSearch==null)return;
            almanacIndex=CampMenuResources.Almanac(menuCatalog,presentation.discovered,almanacCategory,almanacStatusFilter,almanacSearch.text,almanacCatalogOrder,almanacPage,36,locateSlot);almanacPage=almanacIndex.page;
            var selected=almanacIndex.entries.FirstOrDefault(entry=>entry.slot==almanacSlot)??almanacIndex.entries.FirstOrDefault();int previous=almanacSlot;almanacSlot=selected?.slot??-1;
            if(previous!=almanacSlot){Array.Clear(almanacExpanded,0,3);almanacDetail.content.anchoredPosition=Vector2.zero;}
            almanacProgress.text=$"{almanacIndex.known} / {almanacIndex.total}\nITEMS DISCOVERED";
            almanacResults.text=$"{almanacIndex.matches} {(almanacIndex.matches==1?"entry":"entries")} · "+(almanacSearch.text.Length>0?"Only discovered records can match search.":"Undiscovered records stay concealed.");
            almanacStatus.SetContent(almanacStatusFilter=="all"?"All entries":almanacStatusFilter=="discovered"?"Discovered":"Undiscovered");almanacSort.SetContent(almanacCatalogOrder?"Category order":"Discovered first");
            for(int i=0;i<almanacCategoryButtons.Length;i++){var category=i==0?null:almanacIndex.categories[i-1];almanacCategoryButtons[i].SetContent((category?.name??"All items")+"\n"+(category?.known??almanacIndex.known)+" / "+(category?.total??almanacIndex.total));almanacCategoryButtons[i].SetSelected((category?.name??"")==almanacCategory);int index=i;LinkMenuDirection(almanacCategoryButtons[i].gameObject,data=>{if(data.moveDir==MoveDirection.Right)FocusAlmanacSelection();else if(data.moveDir==MoveDirection.Up&&index==0)FocusResource(controls["resource-back:almanac"]);else if(data.moveDir is MoveDirection.Up or MoveDirection.Down)FocusResource(almanacCategoryButtons[Math.Clamp(index+(data.moveDir==MoveDirection.Down?1:-1),0,almanacCategoryButtons.Length-1)]);data.Use();});}
            while(almanacCards.Count<almanacIndex.entries.Length)almanacCards.Add(CreateAlmanacCard(almanacCards.Count));
            for(int i=0;i<almanacCards.Count;i++)
            {
                var card=almanacCards[i];var entry=almanacIndex.entries.ElementAtOrDefault(i);card.slot=entry?.slot??-1;card.control.gameObject.SetActive(entry!=null);
                // Clear pooled private content as well as hiding it. Unknown controls use only public addresses.
                card.control.SetContent(entry==null?"":entry.Known?entry.item.name:"???");card.address.text=entry==null?"":entry.ordinal.ToString("000")+(entry.Known?"  ◆":"  ○");card.category.text=entry?.category??"";SetItemArtwork(card.control.art,entry?.item?.id);card.control.SetSelected(entry?.slot==almanacSlot);
            }
            almanacEmpty.gameObject.SetActive(almanacIndex.entries.Length==0);SetProjectEnabled(almanacPrevious.Button,almanacPage>0);SetProjectEnabled(almanacNext.Button,almanacPage<almanacIndex.pages-1);
            almanacPagination.text=$"{(almanacIndex.matches==0?0:almanacIndex.start+1)}–{almanacIndex.start+almanacIndex.entries.Length} of {almanacIndex.matches}\nPage {almanacPage+1} / {almanacIndex.pages}";
            RefreshAlmanacDetail(selected);
        }
        private void RefreshAlmanacDetail(CampAlmanacEntry entry)
        {
            var item=entry?.item;SetItemArtwork(almanacArt,item?.id);almanacArt.gameObject.SetActive(item!=null);
            almanacFacts.text=entry==null?"No entries match this view.":!entry.Known?$"UNDISCOVERED · RECORD {entry.ordinal:000}\n\n?\n\n???\n{entry.category}\n\nThis entry has not been discovered.\n\nFind or craft this item to record its name, appearance and details here.":$"DISCOVERED · RECORD {entry.ordinal:000}\n\n<b>{item.name}</b>\n{item.purpose}\n\n"+string.Join("\n",new[]{("Category",entry.category),("Type",item.type),("Rarity",item.rarity),("Value tier",item.tier),("Unit mass",item.mass),("Stack limit",item.stackLimit)}.Where(fact=>!string.IsNullOrEmpty(fact.Item2)).Select(fact=>fact.Item1+": "+fact.Item2));
            if(item!=null)
            {
                var locations=presentation.stacks.Where(stack=>stack.itemId==item.id).GroupBy(stack=>stack.owner).ToArray();almanacFacts.text+="\n\nCurrently owned: "+locations.Sum(group=>group.Sum(stack=>stack.quantity));
                almanacFacts.text+=locations.Length==0?"\nPreviously discovered. None currently owned.":"\n"+string.Join("\n",locations.Select(group=>(group.Key=="booter"?"Booter":group.Key=="legger"?"The Legger":"Town storage")+" · "+group.Sum(stack=>stack.quantity)));
            }
            var values=new[]{item?.acquisition,item?.processing,item?.limitations};var labels=new[]{"Acquisition","Crafting & processing","Limitations"};
            for(int i=0;i<3;i++){bool present=!string.IsNullOrEmpty(values[i]);almanacFolds[i].gameObject.SetActive(present);almanacFolds[i].SetContent((almanacExpanded[i]?"− ":"+ ")+labels[i]);almanacFoldBodies[i].text=values[i]??"";almanacFoldBodies[i].gameObject.SetActive(present&&almanacExpanded[i]);}
            var discovered=new HashSet<string>(presentation.discovered??Array.Empty<string>(),StringComparer.Ordinal);
            almanacUseRoots=item==null?Array.Empty<string>():menuCatalog.ResultsUsing(new[]{item.id}).Concat(menuCatalog.Tree(item.id)!=null?new[]{item.id}:Array.Empty<string>()).Distinct(StringComparer.Ordinal).Where(discovered.Contains).ToArray();
            while(almanacUses.Count<almanacUseRoots.Length)almanacUses.Add(ProjectButton(almanacDetail.content,"almanac-use:"+almanacUses.Count,""));
            for(int i=0;i<almanacUses.Count;i++){almanacUses[i].gameObject.SetActive(i<almanacUseRoots.Length);almanacUses[i].SetContent(i>=almanacUseRoots.Length?"":(almanacUseRoots[i]==item?.id?"Make: ":"Used in: ")+ItemName(almanacUseRoots[i]));}
        }
        private bool HandleAlmanacAction(string key)
        {
            if(key.StartsWith("almanac-category:",StringComparison.Ordinal)){int i=int.Parse(key.Substring(17));almanacCategory=i==0?"":almanacIndex.categories[i-1].name;almanacPage=0;}
            else if(key=="almanac-status"){almanacStatusFilter=almanacStatusFilter=="all"?"discovered":almanacStatusFilter=="discovered"?"undiscovered":"all";almanacPage=0;}
            else if(key=="almanac-sort"){almanacCatalogOrder=!almanacCatalogOrder;almanacPage=0;}
            else if(key is "almanac-previous" or "almanac-next"){almanacPage+=key=="almanac-next"?1:-1;almanacGrid.content.anchoredPosition=Vector2.zero;}
            else if(key.StartsWith("almanac-entry:",StringComparison.Ordinal)){SelectAlmanacCard(int.Parse(key.Substring(14)));return true;}
            else if(key.StartsWith("almanac-fold:",StringComparison.Ordinal)){int i=int.Parse(key.Substring(13));almanacExpanded[i]=!almanacExpanded[i];}
            else if(key.StartsWith("almanac-use:",StringComparison.Ordinal)){int i=int.Parse(key.Substring(12));if(i<almanacUseRoots.Length)OpenWorkshopRecipe(almanacUseRoots[i]);return true;}
            else return key=="almanac-detail";
            if(key.StartsWith("almanac-category:",StringComparison.Ordinal)||key is "almanac-status" or "almanac-sort" or "almanac-previous" or "almanac-next")almanacGrid.content.anchoredPosition=Vector2.zero;
            RefreshAlmanac();ReflowResources();EnsureUsableFocus();return true;
        }
        private void SelectAlmanacCard(int index)
        {
            var entry=almanacIndex.entries.ElementAtOrDefault(index);if(entry==null||entry.slot==almanacSlot)return;almanacSlot=entry.slot;Array.Clear(almanacExpanded,0,3);almanacDetail.content.anchoredPosition=Vector2.zero;
            foreach(var card in almanacCards)card.control.SetSelected(card.slot==almanacSlot);RefreshAlmanacDetail(entry);ReflowResources();
        }
        private IEnumerable<CampMenuControl> DetailActions()=>almanacFolds.Concat(almanacUses).Where(c=>c.gameObject.activeSelf);
        private void ReflowAlmanac()
        {
            if(almanacGrid==null)return;
            float progressHeight=ProjectTextHeight(almanacProgress,((RectTransform)almanacCategories.transform.parent).rect.width-28,52);TopRegion(almanacProgress.rectTransform,14,progressHeight,14,14);almanacCategories.viewport.offsetMax=new Vector2(-12,-28-progressHeight);
            float categoryY=0;foreach(var button in almanacCategoryButtons){float h=Mathf.Max(72,ProjectTextHeight(button.title,almanacCategories.viewport.rect.width-20)+12);TopRegion((RectTransform)button.transform,categoryY,h);categoryY+=h+8;}almanacCategories.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,categoryY);
            var middle=(RectTransform)almanacGrid.transform.parent;float w=middle.rect.width;
            TopRegion((RectTransform)almanacStatus.transform,66,large?50:42,12,w/2+6);TopRegion((RectTransform)almanacSort.transform,66,large?50:42,w/2+6,12);
            float resultTop=large?124:116,resultHeight=ProjectTextHeight(almanacResults,w-24,28);TopRegion(almanacResults.rectTransform,resultTop,resultHeight,12,12);almanacGrid.viewport.offsetMax=new Vector2(-12,-resultTop-resultHeight-10);
            Bottom((RectTransform)almanacPrevious.transform,12,132);Bottom((RectTransform)almanacNext.transform,w-144,132);Bottom(almanacPagination.rectTransform,150,Mathf.Max(10,w-300));
            int columns=Mathf.Max(1,Mathf.FloorToInt((almanacGrid.viewport.rect.width+10)/(large?190:165)));float cardWidth=(almanacGrid.viewport.rect.width-(columns-1)*10)/columns;float y=0;
            var rowHeights=new float[(almanacIndex.entries.Length+columns-1)/columns];for(int i=0;i<almanacIndex.entries.Length;i++){var card=almanacCards[i];float required=(large?142:130)+ProjectTextHeight(card.control.title,cardWidth-20,large?72:62)+14+ProjectTextHeight(card.category,cardWidth-20,26)+8;rowHeights[i/columns]=Mathf.Max(rowHeights[i/columns],required);}
            for(int i=0;i<almanacIndex.entries.Length;i++)
            {
                var card=almanacCards[i];float height=rowHeights[i/columns];if(i>0&&i%columns==0)y+=rowHeights[i/columns-1]+10;var rect=(RectTransform)card.control.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(i%columns*(cardWidth+10),-y);rect.sizeDelta=new Vector2(cardWidth,height);
                float categoryHeight=ProjectTextHeight(card.category,cardWidth-20,26);TopRegion(card.address.rectTransform,8,20,10,10);TopRegion(card.control.art.rectTransform,32,large?104:94,18,18);TopRegion(card.control.title.rectTransform,large?142:130,height-(large?142:130)-categoryHeight-22,10,10);TopRegion(card.category.rectTransform,height-categoryHeight-8,categoryHeight,10,10);
                int index=i;LinkMenuDirection(card.control.gameObject,data=>{int target=index;if(data.moveDir==MoveDirection.Left){if(index%columns==0){FocusResource(almanacCategoryButtons.First(c=>c.IsSelected));data.Use();return;}target--;}else if(data.moveDir==MoveDirection.Right){if(index%columns==columns-1||index==almanacIndex.entries.Length-1){FocusResource(controls["almanac-detail"]);data.Use();return;}target++;}else if(data.moveDir==MoveDirection.Up){if(index<columns){FocusResource(almanacStatus);data.Use();return;}target-=columns;}else if(data.moveDir==MoveDirection.Down){if(index+columns>=almanacIndex.entries.Length){FocusResource(almanacNext.Button.IsInteractable()?almanacNext:almanacPrevious);data.Use();return;}target+=columns;}FocusResource(almanacCards[target].control);data.Use();});
            }
            almanacGrid.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,rowHeights.Sum()+rowHeights.Length*10);TopRegion(almanacEmpty.rectTransform,16,130,12,12);
            float detailY=0,detailW=almanacDetail.viewport.rect.width;if(almanacArt.gameObject.activeSelf){TopRegion(almanacArt.rectTransform,0,170,15,15);detailY=186;}float factsH=ProjectTextHeight(almanacFacts,detailW);TopRegion(almanacFacts.rectTransform,detailY,factsH);detailY+=factsH+18;
            for(int i=0;i<3;i++){if(!almanacFolds[i].gameObject.activeSelf)continue;float h=Mathf.Max(50,ProjectTextHeight(almanacFolds[i].title,detailW-20)+12);TopRegion((RectTransform)almanacFolds[i].transform,detailY,h);detailY+=h+8;if(almanacFoldBodies[i].gameObject.activeSelf){h=ProjectTextHeight(almanacFoldBodies[i],detailW);TopRegion(almanacFoldBodies[i].rectTransform,detailY,h);detailY+=h+14;}}
            foreach(var use in almanacUses.Where(c=>c.gameObject.activeSelf)){float h=Mathf.Max(52,ProjectTextHeight(use.title,detailW-20)+12);TopRegion((RectTransform)use.transform,detailY,h);detailY+=h+10;}almanacDetail.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,detailY);
            var actions=DetailActions().ToArray();for(int i=0;i<actions.Length;i++){int index=i;LinkMenuDirection(actions[i].gameObject,data=>{if(data.moveDir==MoveDirection.Left)FocusAlmanacSelection();else if(data.moveDir==MoveDirection.Up)FocusResource(index==0?controls["almanac-detail"]:actions[index-1]);else if(data.moveDir==MoveDirection.Down&&index+1<actions.Length)FocusResource(actions[index+1]);data.Use();});}
            void Bottom(RectTransform rect,float x,float width){rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,0);rect.anchoredPosition=new Vector2(x,12);rect.sizeDelta=new Vector2(width,44);}
        }
    }
}
