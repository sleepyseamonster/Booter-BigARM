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
        private InputField guideSearch;
        private ScrollRect guideIndex,guideArticle,guideRelated;
        private Text guideCount,guideTitle,guideCategory,guideCopy;
        private CampMenuControl[] guideChapters;
        private CampMenuGuideTopic[] guideMatches=Array.Empty<CampMenuGuideTopic>();
        private string guideTopic="overview",almanacReturnPage="guide";
        private bool resourcesBuilt;
        private ScrollRect startResourcesScroll;
        private RectTransform startResourcesBody;
        internal string GuideTopic=>guideTopic;
        private RectTransform ResourceRoot(string key,string title,string introduction)
        {
            var root=new GameObject("Resource "+key,typeof(RectTransform)).GetComponent<RectTransform>();root.SetParent(roots["start"].transform.parent,false);root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.offsetMin=root.offsetMax=Vector2.zero;roots.Add(key,root.gameObject);
            var back=ProjectButton(root,"resource-back:"+key,key=="almanac"?"← Back to guide":"← Start menu");var backRect=(RectTransform)back.transform;backRect.anchorMin=backRect.anchorMax=backRect.pivot=new Vector2(0,1);backRect.anchoredPosition=Vector2.zero;backRect.sizeDelta=new Vector2(180,42);
            var heading=CatalogText(root,title,28);TopRegion(heading.rectTransform,0,44,200,0);
            var subtitle=CatalogText(root,introduction,16);TopRegion(subtitle.rectTransform,52,44);
            LinkMenuDirection(back.gameObject,data=>{if(data.moveDir is MoveDirection.Down or MoveDirection.Right)FocusResourcePage();data.Use();});
            root.gameObject.SetActive(false);return root;
        }
        private RectTransform ResourcePanel(Transform root,string name,float left,float right)
        {var panel=ProjectPanel(root,name,left,right);panel.offsetMin=new Vector2(0,0);panel.offsetMax=new Vector2(-12,-110);return panel;}
        private InputField ResourceSearch(Transform parent,string name,string placeholder)
        {
            var field=Instantiate(storageSearch,parent);field.name=name;field.onValueChanged=new InputField.OnChangeEvent();field.onEndEdit=new InputField.EndEditEvent();field.contentType=InputField.ContentType.Standard;field.characterLimit=120;field.shouldActivateOnSelect=false;field.navigation=new Navigation{mode=Navigation.Mode.None};field.SetTextWithoutNotify("");((Text)field.placeholder).text=placeholder;fontSizes[field.textComponent]=16;fontSizes[(Text)field.placeholder]=16;return field;
        }
        private void ConfigureResources()
        {
            if(resourcesBuilt||!roots.ContainsKey("start")||storageSearch==null)return;
            startResourcesBody=roots["start"].transform.Find("Resources") as RectTransform;
            if(startResourcesBody!=null)
            {
                startResourcesScroll=CreateMenuScroll(roots["start"].transform,"Start resource viewport");startResourcesScroll.viewport.offsetMin=Vector2.zero;startResourcesScroll.viewport.offsetMax=new Vector2(0,28);
                startResourcesBody.SetParent(startResourcesScroll.content,false);startResourcesBody.anchorMin=new Vector2(.037f,1);startResourcesBody.anchorMax=new Vector2(.963f,1);startResourcesBody.pivot=new Vector2(.5f,1);
            }
            var root=ResourceRoot("guide","Wasteland Reference Guide","A companion for the wasteland. Choose a chapter or search its contents.");
            var left=ResourcePanel(root,"Guide chapters",0,.24f);var middle=ResourcePanel(root,"Guide article",.25f,.77f);var right=ResourcePanel(root,"Related resources",.78f,1);
            guideSearch=ResourceSearch(left,"Guide search","Search chapters…");TopRegion((RectTransform)guideSearch.transform,12,44,12,12);guideSearch.onValueChanged.AddListener(_=>{RefreshGuide();ReflowResources();});
            guideCount=CatalogText(left,"",14);TopRegion(guideCount.rectTransform,64,26,14,12);
            guideIndex=CreateMenuScroll(left,"Guide index");guideIndex.viewport.offsetMin=new Vector2(12,12);guideIndex.viewport.offsetMax=new Vector2(-12,-98);
            guideArticle=CreateMenuScroll(middle,"Guide reading pane","guide-article");guideArticle.viewport.offsetMin=new Vector2(20,20);guideArticle.viewport.offsetMax=new Vector2(-20,-20);
            guideTitle=CatalogText(guideArticle.content,"",26);guideCategory=CatalogText(guideArticle.content,"",14);guideCategory.color=style.accent;guideCopy=CatalogText(guideArticle.content,"",18);
            guideRelated=CreateMenuScroll(right,"Guide related resources");guideRelated.viewport.offsetMin=new Vector2(12,12);guideRelated.viewport.offsetMax=new Vector2(-12,-12);
            var label=CatalogText(guideRelated.content,"RELATED RESOURCES",14);TopRegion(label.rectTransform,0,32);
            foreach(var link in new[]{("almanac","Item almanac"),("map","Schematic map"),("settings","Settings")})ProjectButton(guideRelated.content,"resource-open:"+link.Item1,link.Item2);
            guideChapters=menuCatalog.GuideTopics.Select((_,i)=>ProjectButton(guideIndex.content,"guide-chapter:"+i,"")).ToArray();
            LinkMenuDirection(guideSearch.gameObject,data=>{if(guideSearch.isFocused)return;if(data.moveDir==MoveDirection.Down)FocusResource(VisibleChapters().FirstOrDefault());else if(data.moveDir==MoveDirection.Up)FocusResource(controls["resource-back:guide"]);else if(data.moveDir==MoveDirection.Right)FocusResource(controls["guide-article"]);data.Use();});
            LinkMenuDirection(guideArticle.gameObject,data=>{if(data.moveDir==MoveDirection.Left){var chapter=VisibleChapters().FirstOrDefault(c=>c.IsSelected)??VisibleChapters().FirstOrDefault();if(chapter!=null)FocusResource(chapter);else events?.SetSelectedGameObject(guideSearch.gameObject);}else if(data.moveDir==MoveDirection.Right)FocusResource(controls["resource-open:almanac"]);else ScrollInspector(data.moveDir==MoveDirection.Up?1:-1,.18f);data.Use();});
            ConfigureAlmanac();ConfigureSchematicMap();resourcesBuilt=true;RefreshGuide();
        }
        private IEnumerable<CampMenuControl> VisibleChapters()=>guideChapters.Where(c=>c.gameObject.activeSelf);
        private void FocusResource(CampMenuControl control){if(control!=null&&control.gameObject.activeInHierarchy&&control.Button.IsInteractable())events?.SetSelectedGameObject(control.gameObject);}
        private void RefreshGuide()
        {
            if(guideSearch==null)return;guideMatches=CampMenuResources.FindTopics(menuCatalog,guideSearch.text);
            var selected=guideMatches.FirstOrDefault(t=>t.id==guideTopic)??guideMatches.FirstOrDefault();guideTopic=selected?.id;
            guideCount.text=$"CHAPTERS   {guideMatches.Length} / {guideChapters.Length}";
            for(int i=0;i<guideChapters.Length;i++){var button=guideChapters[i];button.gameObject.SetActive(i<guideMatches.Length);if(i>=guideMatches.Length)continue;var topic=guideMatches[i];button.SetContent(topic.title+"\n"+topic.category);button.SetSelected(topic.id==guideTopic);int index=i;LinkMenuDirection(button.gameObject,data=>{if(data.moveDir==MoveDirection.Right)FocusResource(controls["guide-article"]);else if(data.moveDir==MoveDirection.Up&&index==0)events?.SetSelectedGameObject(guideSearch.gameObject);else if(data.moveDir is MoveDirection.Up or MoveDirection.Down)FocusResource(guideChapters[Math.Clamp(index+(data.moveDir==MoveDirection.Down?1:-1),0,guideMatches.Length-1)]);data.Use();});}
            guideTitle.text=selected?.title??"No chapters match";guideCategory.text=selected?.category??"SEARCH";guideCopy.text=selected==null?"Try another word or clear your search.":string.Join("\n\n",selected.sections.Select(s=>"<b>"+s.heading+"</b>\n"+s.text));guideArticle.content.anchoredPosition=Vector2.zero;
        }
        private bool HandleResourceAction(string key)
        {
            if(key=="guide-article")return true;
            if(key=="back:resources"){Back();return true;}
            if(key is "guide" or "map" or "almanac"){OpenResource(key);return true;}
            if(key.StartsWith("resource-back:",StringComparison.Ordinal)){Back();return true;}
            if(key.StartsWith("resource-open:",StringComparison.Ordinal)){OpenResource(key.Substring(14));return true;}
            if(key.StartsWith("guide-chapter:",StringComparison.Ordinal)){int index=int.Parse(key.Substring(14));if(index<guideMatches.Length){guideTopic=guideMatches[index].id;RefreshGuide();ReflowResources();}return true;}
            return HandleAlmanacAction(key)||HandleMapAction(key);
        }
        private void OpenResource(string key)
        {if(key=="settings"){Command?.Invoke("settings");return;}if(key=="almanac")almanacReturnPage=page is "guide" or "map"?page:"guide";ShowPage(key);}
        private void FocusResourcePage()
        {
            if(events==null)return;
            if(page=="guide")events.SetSelectedGameObject(guideSearch.gameObject);
            else if(page=="almanac")FocusAlmanacSelection();
            else if(page=="map")FocusResource(mapMarkers[mapSelection]);
        }
        private void ReflowResources()
        {
            if(!resourcesBuilt)return;
            ApplyTypography();
            ReflowStartResources();
            float indexY=0;foreach(var button in VisibleChapters()){float height=Mathf.Max(large?100:82,ProjectTextHeight(button.title,guideIndex.viewport.rect.width-20)+12);TopRegion((RectTransform)button.transform,indexY,height);indexY+=height+8;}guideIndex.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,indexY);
            float width=guideArticle.viewport.rect.width,y=0;foreach(var text in new[]{guideCategory,guideTitle,guideCopy}){float height=ProjectTextHeight(text,width);TopRegion(text.rectTransform,y,height);y+=height+16;}guideArticle.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,y);
            float relatedY=44;var links=new[]{"almanac","map","settings"}.Select(id=>controls["resource-open:"+id]).ToArray();for(int i=0;i<links.Length;i++){var link=links[i];float height=Mathf.Max(62,ProjectTextHeight(link.title,guideRelated.viewport.rect.width-20)+14);TopRegion((RectTransform)link.transform,relatedY,height);relatedY+=height+12;int index=i;LinkMenuDirection(link.gameObject,data=>{if(data.moveDir==MoveDirection.Left)FocusResource(controls["guide-article"]);else if(data.moveDir==MoveDirection.Up&&index==0)FocusResource(controls["resource-back:guide"]);else if(data.moveDir is MoveDirection.Up or MoveDirection.Down)FocusResource(links[Math.Clamp(index+(data.moveDir==MoveDirection.Down?1:-1),0,links.Length-1)]);data.Use();});}guideRelated.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,relatedY);
            ReflowAlmanac();ReflowSchematicMap();
        }
        private void ReflowStartResources()
        {
            if(startResourcesBody==null||startResourcesScroll==null)return;
            float width=startResourcesScroll.viewport.rect.width*.926f,y=0;var captions=startResourcesBody.GetComponentsInChildren<Text>(true).Where(text=>text.transform.parent==startResourcesBody).ToArray();
            foreach(var text in captions){float height=ProjectTextHeight(text,width);TopRegion(text.rectTransform,y,height);y+=height+6;}y+=18;
            var cards=new[]{"guide","map","settings"}.Select(key=>controls[key]).ToArray();float cardWidth=width*.325f,cardHeight=0;
            foreach(var card in cards)
            {
                float title=ProjectTextHeight(card.title,cardWidth-52);TopRegion(card.title.rectTransform,24,title,26,26);var body=card.GetComponentsInChildren<Text>(true).FirstOrDefault(text=>text!=card.title);
                float copy=body==null?0:ProjectTextHeight(body,cardWidth-52);if(body!=null)TopRegion(body.rectTransform,40+title,copy,26,26);cardHeight=Mathf.Max(cardHeight,66+title+copy);
            }
            for(int i=0;i<cards.Length;i++){var rect=(RectTransform)cards[i].transform;rect.anchorMin=new Vector2(i*.337f,1);rect.anchorMax=new Vector2(i*.337f+.325f,1);rect.offsetMin=new Vector2(0,-y-cardHeight);rect.offsetMax=new Vector2(0,-y);}y+=cardHeight+28;
            controls["back:resources"].SetContent("← Return");var back=(RectTransform)controls["back:resources"].transform;var quit=(RectTransform)controls["quit"].transform;float actionHeight=Mathf.Max(48,Mathf.Max(ProjectTextHeight(controls["back:resources"].title,width*.46f-20),ProjectTextHeight(controls["quit"].title,width*.46f-20))+12);
            TopRegion(back,y,actionHeight,0,width*.54f);TopRegion(quit,y,actionHeight,width*.54f,0);y+=actionHeight+16;
            foreach(var key in new[]{"recover-pending","recover-backup"})if(controls.TryGetValue(key,out var recovery)&&recovery.gameObject.activeSelf){float height=Mathf.Max(52,ProjectTextHeight(recovery.title,width-20)+12);TopRegion((RectTransform)recovery.transform,y,height);y+=height+12;}
            startResourcesBody.offsetMin=new Vector2(0,-y);startResourcesBody.offsetMax=Vector2.zero;startResourcesScroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Mathf.Max(y,startResourcesScroll.viewport.rect.height));
        }
        internal void ScrollResource(Vector2 axis,float seconds)
        {
            if(quoting||pickerTarget!=null||IsEditingText)return;
            if(page=="map"&&mapScroll!=null&&events?.currentSelectedGameObject?.transform.IsChildOf(mapDetails.transform)!=true){mapScroll.horizontalNormalizedPosition=Mathf.Clamp01(mapScroll.horizontalNormalizedPosition+axis.x*seconds);mapScroll.verticalNormalizedPosition=Mathf.Clamp01(mapScroll.verticalNormalizedPosition+axis.y*seconds);}
            else ScrollInspector(axis.y,seconds);
        }
    }
}
