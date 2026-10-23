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
        private readonly List<CampMenuControl> treeCards=new();
        private readonly Dictionary<string,CampMenuControl> libraryControls=new(StringComparer.Ordinal);
        private readonly HashSet<string> expandedTreeGroups=new(StringComparer.Ordinal),collapsedSearchGroups=new(StringComparer.Ordinal);
        private readonly Dictionary<CampMenuControl,CampMenuDisclosureGraphic> treeMarkers=new();
        private RectTransform treeStage,treeList;
        private ScrollRect treeLibraryScroll;
        private InputField treeSearch;
        private Text treeHeading,treeCounts,treeEmpty;
        private string treeRoot;
        private CampMenuTree displayedTree;
        internal string DisplayedTreeRoot=>treeRoot;
        internal CampMenuTree DisplayedTree=>displayedTree;
        private sealed class LibraryRow {public string key,label;public int count,level;public bool root,containsSelection;}

        private void ConfigureCatalogTree(RectTransform stage)
        {
            if(menuCatalog==null||menuCatalog.Tree(definition.rootId)==null||storageSearch==null)return;
            treeStage=stage;treeCards.AddRange(controls.Values.Where(c=>c.key.StartsWith("tree-node:",StringComparison.Ordinal)));
            foreach(var card in treeCards)LinkMenuDirection(card.gameObject,MoveTreeCard);
            var browser=stage.parent.parent.Find("Item browser") as RectTransform;
            browser.anchorMax=new Vector2(.246f,1);
            var canvas=(RectTransform)stage.parent;canvas.anchorMin=new Vector2(.268f,0);canvas.anchorMax=new Vector2(.77f,1);canvas.offsetMin=Vector2.zero;
            var template=controls["node:"+definition.rootId];controls.Remove(template.key);
            foreach(Transform child in browser)if(child.name!="ITEMS")child.gameObject.SetActive(false);
            var header=browser.Find("ITEMS")?.GetComponentInChildren<Text>();if(header!=null)header.text="ITEMS  "+menuCatalog.Trees.Count();
            treeSearch=Instantiate(storageSearch,browser);treeSearch.name="Tree search";treeSearch.text="";((Text)treeSearch.placeholder).text="Find a crafting tree…";
            var search=(RectTransform)treeSearch.transform;search.anchorMin=new Vector2(0,1);search.anchorMax=Vector2.one;search.pivot=new Vector2(.5f,1);search.offsetMin=new Vector2(12,-90);search.offsetMax=new Vector2(-12,-34);
            var viewport=new GameObject("Tree library viewport",typeof(RectTransform),typeof(Image),typeof(RectMask2D),typeof(ScrollRect));viewport.transform.SetParent(browser,false);
            var rect=(RectTransform)viewport.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(10,10);rect.offsetMax=new Vector2(-10,-104);viewport.GetComponent<Image>().color=Color.clear;
            treeList=new GameObject("Finished items",typeof(RectTransform)).GetComponent<RectTransform>();treeList.SetParent(rect,false);treeList.anchorMin=new Vector2(0,1);treeList.anchorMax=Vector2.one;treeList.pivot=new Vector2(.5f,1);treeList.offsetMin=treeList.offsetMax=Vector2.zero;
            treeLibraryScroll=viewport.GetComponent<ScrollRect>();treeLibraryScroll.viewport=rect;treeLibraryScroll.content=treeList;treeLibraryScroll.horizontal=false;treeLibraryScroll.movementType=ScrollRect.MovementType.Clamped;treeLibraryScroll.scrollSensitivity=35;
            template.transform.SetParent(treeList,false);template.gameObject.SetActive(true);template.key="tree-group:equipment";controls.Add(template.key,template);libraryControls.Add(template.key,template);
            LinkMenuDirection(template.gameObject,MoveTreeLibrary);LinkMenuDirection(treeSearch.gameObject,data=>{if(!treeSearch.isFocused)MoveTreeLibrary(data);});
            treeSearch.onValueChanged.AddListener(_=>{treeLibraryScroll.StopMovement();treeList.anchoredPosition=Vector2.zero;RefreshTreeLibrary();});
            treeEmpty=CatalogText(rect,"No matching crafting trees.",16);treeEmpty.rectTransform.anchorMin=new Vector2(0,1);treeEmpty.rectTransform.anchorMax=Vector2.one;treeEmpty.rectTransform.offsetMin=new Vector2(8,-100);treeEmpty.rectTransform.offsetMax=new Vector2(-8,0);
            var pageRoot=stage.parent.parent;
            treeHeading=CatalogText(pageRoot,"",18);treeHeading.rectTransform.anchorMin=new Vector2(.268f,1);treeHeading.rectTransform.anchorMax=new Vector2(.66f,1);
            treeCounts=CatalogText(pageRoot,"",14);treeCounts.color=style.muted;treeCounts.alignment=TextAnchor.UpperRight;treeCounts.rectTransform.anchorMin=new Vector2(.67f,1);treeCounts.rectTransform.anchorMax=new Vector2(.77f,1);
            treeHeading.name="Tree heading";treeCounts.name="Tree input counts";foreach(var headerText in new[]{treeHeading,treeCounts}){headerText.rectTransform.offsetMin=new Vector2(0,-48);headerText.rectTransform.offsetMax=Vector2.zero;}
            OpenDisplayTree(definition.rootId);RefreshTreeLibrary();
        }
        private void ReflowCatalogTree()
        {
            if(treeHeading==null||treeCounts==null||treeStage==null)return;
            float width=((RectTransform)treeStage.parent.parent).rect.width,title=ProjectTextHeight(treeHeading,width*.392f),counts=ProjectTextHeight(treeCounts,width*.10f);
            float height=Mathf.Max(48,Mathf.Max(title,counts)+12);treeHeading.rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top,0,height-10);treeCounts.rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top,0,height-10);((RectTransform)treeStage.parent).offsetMax=new Vector2(0,-height);
        }

        private Text CatalogText(Transform parent,string value,int size)
        {
            var node=new GameObject(value.Length==0?"Catalog text":value,typeof(RectTransform),typeof(Text));node.transform.SetParent(parent,false);var text=node.GetComponent<Text>();text.font=style.ResolveFont();text.fontSize=size;text.color=style.ink;text.text=value;text.raycastTarget=false;fontSizes[text]=size;return text;
        }
        private string ItemName(string id)=>menuCatalog?.Find(id)?.name??definition.Find(id)?.DisplayName??id;
        private Sprite ItemArt(string id)=>definition.Find(id)?.art??menuCatalog?.Art(id)??style.FindGlyph(id switch{"equipment.cloak"=>"back","equipment.jacket"=>"chest","equipment.digging_tool"=>"tool",_=>null});
        private void SetItemArtwork(Image image,string id)
        {
            if(image==null)return;image.sprite=id==null?null:ItemArt(id);image.enabled=image.sprite!=null;
            var glyph=image.GetComponentInChildren<CampMenuItemGlyphGraphic>(true);
            if(image.sprite==null&&id!=null&&glyph==null)
            {var node=new GameObject("Item vector icon",typeof(RectTransform),typeof(CampMenuItemGlyphGraphic));node.transform.SetParent(image.transform,false);glyph=node.GetComponent<CampMenuItemGlyphGraphic>();glyph.raycastTarget=false;var rect=(RectTransform)node.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;}
            if(glyph!=null){glyph.gameObject.SetActive(image.sprite==null&&id!=null);glyph.SetItem(id,style.ink);}
        }
        private bool KnownItem(string id)=>menuCatalog?.Find(id)!=null||definition.Find(id)!=null;

        internal void OpenDisplayTree(string rootId,bool revealLibrary=false)
        {
            var tree=menuCatalog?.Tree(rootId);if(tree==null||treeStage==null)return;
            treeRoot=rootId;displayedTree=tree;selectedTree=null;selectedStack=null;
            if(revealLibrary){expandedTreeGroups.Add("tree-group:equipment");expandedTreeGroups.Add("tree-group:c:"+Uri.EscapeDataString(tree.category??""));expandedTreeGroups.Add("tree-group:f:"+Uri.EscapeDataString(tree.category??"")+":"+Uri.EscapeDataString(tree.family??""));}
            // The camera highlight can be parented to a card that is about to be pooled.
            TreeCanvas.PrepareNodeReplacement();
            for(int i=0;i<treeCards.Count;i++){var card=treeCards[i];controls.Remove(card.key);card.key="tree-pool:"+i;card.gameObject.SetActive(false);}
            while(treeCards.Count<tree.nodes.Length)
            {
                var clone=Instantiate(treeCards[0],treeStage);clone.Wire();clone.Activated+=Activate;clone.Browsed+=Browse;RememberClonedTypography(treeCards[0],clone);treeCards.Add(clone);
                LinkMenuDirection(clone.gameObject,MoveTreeCard);
                // A cloned tracking label belongs to this card, not the template's cache key.
                var marker=clone.transform.Find("Tracked marker")?.GetComponent<CampMenuDisclosureGraphic>();if(marker!=null)treeMarkers[clone]=marker;
            }
            for(int i=0;i<tree.nodes.Length;i++)
            {
                var node=tree.nodes[i];var card=treeCards[i];card.key="tree-node:"+node.id;card.name=card.key;card.Configure(style,card.key,style.surface);controls.Add(card.key,card);card.gameObject.SetActive(true);card.SetSelected(false);card.SetRelation(null);
                var rect=(RectTransform)card.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(node.x,-node.y);rect.sizeDelta=new Vector2(145,238);card.Button.navigation=new Navigation{mode=Navigation.Mode.None};
                var item=menuCatalog.Find(node.id);card.SetContent(item.name,ItemArt(node.id),(node.shared?"Shared input":node.id==tree.rootId?"Finished item":item.category)+"\n"+(CampMenuRarityGraphic.TryColor(item.rarity,out _)?item.rarity:"Unassigned"));
                SetItemArtwork(card.art,node.id);
                var rarity=card.GetComponentInChildren<CampMenuRarityGraphic>(true);if(rarity==null){var corner=new GameObject("Rarity corner",typeof(RectTransform),typeof(CampMenuRarityGraphic));corner.transform.SetParent(card.transform,false);rarity=corner.GetComponent<CampMenuRarityGraphic>();rarity.raycastTarget=false;}
                rarity.SetRarity(item.rarity);rarity.rectTransform.anchorMin=rarity.rectTransform.anchorMax=rarity.rectTransform.pivot=new Vector2(1,0);rarity.rectTransform.anchoredPosition=Vector2.zero;rarity.rectTransform.sizeDelta=new Vector2(18,18);
                card.title.rectTransform.anchorMin=new Vector2(0,.21f);card.title.rectTransform.anchorMax=new Vector2(1,.45f);card.title.rectTransform.offsetMin=new Vector2(8,0);card.title.rectTransform.offsetMax=new Vector2(-8,0);fontSizes[card.title]=14;card.title.fontSize=14;
                card.subtitle.rectTransform.anchorMin=Vector2.zero;card.subtitle.rectTransform.anchorMax=new Vector2(1,.2f);card.subtitle.rectTransform.offsetMin=new Vector2(8,4);card.subtitle.rectTransform.offsetMax=new Vector2(-8,0);card.subtitle.alignment=TextAnchor.UpperCenter;fontSizes[card.subtitle]=12;card.subtitle.fontSize=12;
                card.art.rectTransform.anchorMin=new Vector2(0,.46f);card.art.rectTransform.anchorMax=new Vector2(1,.94f);
                if(card.cornerLabel==null){card.cornerLabel=CatalogText(card.transform,"Tier",12);card.cornerLabel.name="Tier";}
                card.cornerLabel.text=item.tier;card.cornerLabel.rectTransform.anchorMin=card.cornerLabel.rectTransform.anchorMax=new Vector2(0,1);card.cornerLabel.rectTransform.pivot=new Vector2(0,1);card.cornerLabel.rectTransform.anchoredPosition=new Vector2(8,-4);card.cornerLabel.rectTransform.sizeDelta=new Vector2(65,24);
                if(!treeMarkers.TryGetValue(card,out var marker)){var icon=new GameObject("Tracked marker",typeof(RectTransform),typeof(CampMenuDisclosureGraphic));icon.transform.SetParent(card.transform,false);marker=icon.GetComponent<CampMenuDisclosureGraphic>();marker.raycastTarget=false;treeMarkers[card]=marker;}
                marker.SetCheckMark(true);marker.color=style.accent;marker.rectTransform.anchorMin=marker.rectTransform.anchorMax=marker.rectTransform.pivot=Vector2.one;marker.rectTransform.anchoredPosition=new Vector2(-6,-4);marker.rectTransform.sizeDelta=new Vector2(22,24);
                var group=card.GetComponent<CanvasGroup>();if(group==null)group=card.gameObject.AddComponent<CanvasGroup>();group.alpha=1;
            }
            treeStage.sizeDelta=new Vector2(tree.width,tree.height);treeStage.GetComponent<CampMenuTreeGraphic>().SetTree(tree);
            TreeCanvas.ReplaceNodes(treeCards.Take(tree.nodes.Length).ToArray());
            treeHeading.text=ItemName(rootId);var required=menuCatalog.Inputs(rootId,rootId);int detached=tree.nodes.Length-required.Count-1;
            treeCounts.text=$"{required.Count} unique inputs"+(detached>0?$" · {detached} detached items":"")+(tree.unavailableDefinitions+tree.omittedCycles>0?$"\n{tree.unavailableDefinitions} unavailable definitions · {tree.omittedCycles} circular connections omitted":"");
            Inspect(null);RefreshTreeRelations();RefreshTreeLibrary();ApplyTypography();
        }

        private void RefreshTreeRelations()
        {
            if(displayedTree==null)return;
            var inputs=menuCatalog.Inputs(treeRoot,selectedTree);var outputs=menuCatalog.Outputs(treeRoot,selectedTree);
            foreach(var card in TreeCanvas.Nodes)
            {
                var id=card.key.Substring(10);card.SetSelected(id==selectedTree);card.SetRelation(inputs.Contains(id)?CampMenuTreeGraphic.InputColor:outputs.Contains(id)?CampMenuTreeGraphic.OutputColor:(Color?)null,selectedTree==null||id==selectedTree||inputs.Contains(id)||outputs.Contains(id)?1:.18f);
                if(treeMarkers.TryGetValue(card,out var marker))marker.gameObject.SetActive(presentation.tracked.Contains(id));
            }
            treeStage.GetComponent<CampMenuTreeGraphic>().SetSelection(selectedTree,inputs,outputs);
        }

        private void ToggleTreeGroup(string key)
        {
            var set=string.IsNullOrEmpty(treeSearch?.text)?expandedTreeGroups:collapsedSearchGroups;if(!set.Add(key))set.Remove(key);RefreshTreeLibrary();
        }
        private bool TreeGroupOpen(string key)=>string.IsNullOrEmpty(treeSearch?.text)?expandedTreeGroups.Contains(key):!collapsedSearchGroups.Contains(key);
        private void RefreshTreeLibrary()
        {
            if(treeList==null)return;
            string query=treeSearch.text;var trees=menuCatalog.Trees.Where(tree=>(ItemName(tree.rootId)+" "+tree.family+" "+tree.category+" "+tree.tier).IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0).ToArray();
            var rows=new List<LibraryRow>();
            void Group(string key,string label,int count,int level,bool contains=false)=>rows.Add(new LibraryRow{key="tree-group:"+key,label=label,count=count,level=level,containsSelection=contains});
            if(trees.Length>0)
            {
                Group("equipment","Equipment",trees.Length,0);
                if(TreeGroupOpen("tree-group:equipment"))foreach(var category in trees.GroupBy(tree=>tree.category??menuCatalog.Find(tree.rootId).category))
                {
                    string categoryKey="tree-group:c:"+Uri.EscapeDataString(category.Key);Group(categoryKey.Substring(11),category.Key,category.Count(),1,category.Any(tree=>tree.rootId==treeRoot));
                    if(!TreeGroupOpen(categoryKey))continue;
                    foreach(var family in category.GroupBy(tree=>tree.family??menuCatalog.Find(tree.rootId).family))
                    {
                        string familyKey="tree-group:f:"+Uri.EscapeDataString(category.Key)+":"+Uri.EscapeDataString(family.Key??"");Group(familyKey.Substring(11),family.Key??"Other",family.Count(),2,family.Any(tree=>tree.rootId==treeRoot));
                        if(TreeGroupOpen(familyKey))foreach(var tree in family)rows.Add(new LibraryRow{key="node:"+tree.rootId,label=ItemName(tree.rootId),level=3,root=true});
                    }
                }
            }
            var visibleKeys=new HashSet<string>(rows.Select(row=>row.key),StringComparer.Ordinal);
            foreach(var pair in libraryControls)if(!visibleKeys.Contains(pair.Key))pair.Value.gameObject.SetActive(false);
            float top=0;
            foreach(var row in rows)
            {
                if(!libraryControls.TryGetValue(row.key,out var control))
                {var template=libraryControls.Values.First();control=Instantiate(template,treeList);control.key=row.key;control.name=row.key;control.Wire();control.Activated+=Activate;control.Browsed+=Browse;libraryControls.Add(row.key,control);controls.Add(row.key,control);RememberClonedTypography(template,control);LinkMenuDirection(control.gameObject,MoveTreeLibrary);}
                control.Configure(style,row.key,Color.clear);control.gameObject.SetActive(true);control.SetSelected(row.containsSelection||row.root&&row.key=="node:"+treeRoot);
                string label=row.label;control.SetContent(label);control.title.fontSize=large?19:16;fontSizes[control.title]=16;control.title.alignment=TextAnchor.MiddleLeft;
                if(control.subtitle==null)control.subtitle=CatalogText(control.transform,"",14);
                control.subtitle.text=row.root?menuCatalog.Find(row.key.Substring(5)).tier:row.count.ToString();control.subtitle.fontSize=large?16:14;fontSizes[control.subtitle]=14;
                float indent=row.level*8;float width=Mathf.Max(30,treeList.rect.width-indent-16-(row.root?0:16));
                float titleHeight=control.title.cachedTextGeneratorForLayout.GetPreferredHeight(label,control.title.GetGenerationSettings(new Vector2(row.root?width:Mathf.Max(20,width-36),0)))/control.title.pixelsPerUnit;
                float height=Mathf.Max(large?50:44,titleHeight+(row.root?26:12));var rect=(RectTransform)control.transform;rect.anchorMin=new Vector2(0,1);rect.anchorMax=Vector2.one;rect.pivot=new Vector2(.5f,1);rect.offsetMin=new Vector2(0,-top-height);rect.offsetMax=new Vector2(0,-top);
                var title=control.title.rectTransform;title.anchorMin=Vector2.zero;title.anchorMax=Vector2.one;title.offsetMin=new Vector2(indent+(row.root?8:24),row.root?22:4);title.offsetMax=new Vector2(row.root?-8:-40,-4);
                var disclosure=control.GetComponentInChildren<CampMenuDisclosureGraphic>(true);
                if(!row.root&&disclosure==null){var icon=new GameObject("Disclosure",typeof(RectTransform),typeof(CampMenuDisclosureGraphic));icon.transform.SetParent(control.transform,false);disclosure=icon.GetComponent<CampMenuDisclosureGraphic>();disclosure.raycastTarget=false;disclosure.color=style.ink;}
                if(disclosure!=null){disclosure.gameObject.SetActive(!row.root);disclosure.SetOpen(TreeGroupOpen(row.key));var icon=(RectTransform)disclosure.transform;icon.anchorMin=icon.anchorMax=new Vector2(0,.5f);icon.anchoredPosition=new Vector2(indent+12,0);icon.sizeDelta=new Vector2(14,14);}
                var note=control.subtitle.rectTransform;note.anchorMin=row.root?Vector2.zero:new Vector2(1,0);note.anchorMax=row.root?new Vector2(1,0):Vector2.one;note.offsetMin=row.root?new Vector2(indent+8,2):new Vector2(-36,4);note.offsetMax=row.root?new Vector2(-8,22):new Vector2(-4,-4);control.subtitle.alignment=row.root?TextAnchor.LowerLeft:TextAnchor.MiddleRight;
                top+=height+4;
            }
            treeList.sizeDelta=new Vector2(0,Mathf.Max(treeLibraryScroll.viewport.rect.height,top));treeEmpty.gameObject.SetActive(rows.Count==0);
            for(int i=0;i<rows.Count;i++){var control=libraryControls[rows[i].key];control.Button.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnUp=i==0?treeSearch:libraryControls[rows[i-1].key].Button,selectOnDown=i+1<rows.Count?libraryControls[rows[i+1].key].Button:null};}
            treeSearch.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnDown=rows.Count>0?libraryControls[rows[0].key].Button:null};
            var focused=events?.currentSelectedGameObject;if(page=="tree"&&focused!=null&&focused.transform.IsChildOf(treeList)&&!focused.activeInHierarchy)events.SetSelectedGameObject(treeSearch.gameObject);
        }
        private static void LinkMenuDirection(GameObject target,Action<AxisEventData> handler)
        {var link=target.GetComponent<CampMenuDirectionalLink>();if(link==null)link=target.AddComponent<CampMenuDirectionalLink>();link.Bind(handler);}
        private void MoveTreeLibrary(AxisEventData data)
        {if(data.moveDir==MoveDirection.Right&&TreeCanvas.CanInteract){TreeCanvas.ClaimFocus();data.Use();}}
        private void MoveTreeCard(AxisEventData data){TreeCanvas.OnMove(data);}
    }
}
