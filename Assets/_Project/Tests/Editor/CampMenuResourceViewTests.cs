using System;
using System.Collections.Generic;
using System.Linq;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class CampMenuResourceViewTests
    {
        private GameObject root,eventRoot;
        private CampExpeditionMenuView view;
        private EventSystem events;
        private readonly List<string> commands=new();
        private CampMenuControl Control(string key)=>root.GetComponentsInChildren<CampMenuControl>(true).Single(c=>c.key==key);
        private InputField Search(string name)=>root.GetComponentsInChildren<InputField>(true).Single(f=>f.name==name);
        private void Click(string key)=>Control(key).Button.onClick.Invoke();
        private string[] Mutations=>commands.Where(command=>!command.StartsWith("inspect:",StringComparison.Ordinal)).ToArray();
        private void Move(string key,MoveDirection direction)=>Control(key).GetComponent<CampMenuDirectionalLink>().OnMove(new AxisEventData(events){moveDir=direction});
        private void Layout(){Canvas.ForceUpdateCanvases();view.RefreshLayout();Canvas.ForceUpdateCanvases();}
        [SetUp] public void Setup()
        {
            root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/CampExpedition/CampExpeditionMenu.prefab"));eventRoot=new GameObject("Resource fixture",typeof(EventSystem));events=eventRoot.GetComponent<EventSystem>();view=root.GetComponent<CampExpeditionMenuView>();view.Initialize(events);view.Command+=commands.Add;view.Render(new CampMenuPresentation{canPlay=true});
        }
        [TearDown] public void Cleanup(){root.SetActive(false);Object.DestroyImmediate(root);Object.DestroyImmediate(eventRoot);commands.Clear();}
        [Test] public void GuideAlmanacAndMapReturnThroughStartToTheirOriginalPage()
        {
            view.ShowPage("storage");view.ShowPage("start");Assert.That(events.currentSelectedGameObject,Is.SameAs(Control("guide").gameObject));Click("guide");Assert.That(view.Page,Is.EqualTo("guide"));Click("resource-open:almanac");Assert.That(view.Page,Is.EqualTo("almanac"));view.Back();Assert.That(view.Page,Is.EqualTo("guide"));view.Back();Assert.That(view.Page,Is.EqualTo("start"));Click("map");view.Back();Click("back:resources");Assert.That(view.Page,Is.EqualTo("storage"));Assert.That(Mutations,Is.Empty);
        }
        [TestCase(false)][TestCase(true)] public void StartResourceCardsFitTheirCopyAndRecoveryActionsCanBeRevealed(bool large)
        {
            view.Render(new CampMenuPresentation{canPlay=false,recoveryAvailable=true});view.ShowPage("start");view.SetLargeText(large);Layout();
            foreach(var key in new[]{"guide","map","settings","back:resources","quit"})foreach(var text in Control(key).GetComponentsInChildren<Text>())Assert.That(text.preferredHeight,Is.LessThanOrEqualTo(text.rectTransform.rect.height+1),key+": "+text.text);
            var recovery=Control("recover-backup");events.SetSelectedGameObject(recovery.gameObject);var scroll=recovery.GetComponentInParent<ScrollRect>();Assert.That(scroll,Is.Not.Null);var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport,recovery.transform);Assert.That(bounds.min.y,Is.GreaterThanOrEqualTo(scroll.viewport.rect.yMin-1));Assert.That(bounds.max.y,Is.LessThanOrEqualTo(scroll.viewport.rect.yMax+1));Assert.That(Mutations,Is.Empty);
        }
        [TestCase("play")][TestCase("inventory")][TestCase("storage")][TestCase("tree")][TestCase("guide")][TestCase("map")]
        public void AStoppedProfileKeepsRecoveryAndQuitReachableInsteadOfOpeningInertDestinations(string destination)
        {
            view.ShowPage("inventory");view.Render(new CampMenuPresentation{recoveryAvailable=true});Assert.That(view.Page,Is.EqualTo("start"));view.ShowPage(destination);Assert.That(view.Page,Is.EqualTo("start"));
            Assert.That(Control("guide").Button.IsInteractable(),Is.False);Assert.That(Control("primary:crafting").Button.IsInteractable(),Is.False);Assert.That(Control("recover-pending").Button.IsInteractable(),Is.True);Assert.That(Control("quit").Button.IsInteractable(),Is.True);Assert.That(events.currentSelectedGameObject,Is.SameAs(Control("recover-pending").gameObject));Assert.That(Mutations,Is.Empty);
            view.Render(new CampMenuPresentation{canPlay=true});view.ShowPage("inventory");Assert.That(view.Page,Is.EqualTo("inventory"));Assert.That(Control("guide").Button.IsInteractable(),Is.True);
        }
        [Test] public void GuideSearchRepairsSelectionAndProvidesAnExplicitNoResultsState()
        {
            Click("guide");var search=Search("Guide search");Assert.That(search.shouldActivateOnSelect,Is.False);search.text="Phagia algae";Assert.That(view.GuideTopic,Is.EqualTo("survival"));Click("guide-chapter:0");Assert.That(Control("guide-article").GetComponentsInChildren<Text>().Any(t=>t.text.Contains("basis of survival")),Is.True);search.text="impossible_123";Assert.That(view.GuideTopic,Is.Null);Assert.That(Control("guide-article").GetComponentsInChildren<Text>().Any(t=>t.text=="No chapters match"),Is.True);Move("guide-article",MoveDirection.Left);Assert.That(events.currentSelectedGameObject,Is.SameAs(search.gameObject));search.text="";Assert.That(view.GuideTopic,Is.EqualTo("overview"));
        }
        [Test] public void UnknownRecordNamesIdsAndArtworkDoNotEnterPooledAlmanacCards()
        {
            string id=CampExpeditionDefinition.ToolZeroId;view.Render(new CampMenuPresentation{discovered=new[]{id}});Click("guide");Click("resource-open:almanac");Assert.That(Control("almanac-entry:0").title.text,Is.EqualTo(CampMenuCatalog.Load().Find(id).name));
            view.Render(new CampMenuPresentation());Layout();var entries=root.GetComponentsInChildren<CampMenuControl>(true).Where(c=>c.key.StartsWith("almanac-entry:",StringComparison.Ordinal)).ToArray();Assert.That(entries.Length,Is.EqualTo(36));Assert.That(entries.All(c=>c.title.text=="???"&&c.art.sprite==null&&!c.art.enabled),Is.True);Assert.That(entries.All(c=>!c.name.Contains(id)),Is.True);Assert.That(Control("almanac-detail").GetComponentsInChildren<Text>(true).All(t=>!t.text.Contains(CampMenuCatalog.Load().Find(id).name)),Is.True);
            Search("Almanac search").text=CampMenuCatalog.Load().Find(id).name;Assert.That(entries.All(c=>!c.gameObject.activeSelf&&c.title.text==""),Is.True);Assert.That(Control("almanac-detail").GetComponentsInChildren<Text>().Any(t=>t.text.Contains("No entries match")),Is.True);
        }
        [Test] public void DiscoveryFactsAndOwnedLocationsRefreshWithoutGrantingDiscoveries()
        {
            string id=CampExpeditionDefinition.ToolZeroId;var discoveries=new[]{id};view.Render(new CampMenuPresentation{discovered=discoveries,stacks=new[]{new CampMenuStackView{itemId=id,owner="legger",quantity=2},new CampMenuStackView{itemId=id,owner="storage",quantity=3}}});Click("guide");Click("resource-open:almanac");Search("Almanac search").text=CampMenuCatalog.Load().Find(id).name;Layout();string text=string.Join("\n",Control("almanac-detail").GetComponentsInChildren<Text>().Select(t=>t.text));Assert.That(text,Does.Contain("Currently owned: 5"));Assert.That(text,Does.Contain("The Legger · 2"));Assert.That(text,Does.Contain("Town storage · 3"));Assert.That(text,Does.Contain("Type: Multi-tool"));Assert.That(text,Does.Not.Contain("Unit mass:"),"A missing authored mass must not be invented from runtime tuning.");Assert.That(discoveries,Is.EqualTo(new[]{id}));Assert.That(Mutations,Is.Empty);
        }
        [Test] public void CategoryGridDetailAndPagerHaveControllerRoutesAndIndependentScroll()
        {
            Click("guide");Click("resource-open:almanac");Layout();events.SetSelectedGameObject(Control("almanac-category:0").gameObject);Move("almanac-category:0",MoveDirection.Right);Assert.That(events.currentSelectedGameObject,Is.SameAs(Control("almanac-entry:0").gameObject));int selected=view.AlmanacSelectedSlot;Move("almanac-entry:0",MoveDirection.Down);Assert.That(view.AlmanacSelectedSlot,Is.Not.EqualTo(selected));Move("almanac-detail",MoveDirection.Left);Assert.That(events.currentSelectedGameObject.GetComponent<CampMenuControl>().key,Does.StartWith("almanac-entry:"));Click("almanac-next");Assert.That(Control("almanac-previous").Button.IsInteractable(),Is.True);Click("almanac-status");Assert.That(Control("almanac-next").Button.IsInteractable(),Is.False);Assert.That(events.currentSelectedGameObject.activeInHierarchy,Is.True);
        }
        [Test] public void MapZoomPanWaypointAndBackArePresentationOnly()
        {
            view.ShowPage("start");Click("map");Layout();Assert.That(Control("map-zoom-out").Button.IsInteractable(),Is.False);for(int i=0;i<4;i++)Click("map-zoom-in");Layout();Assert.That(Control("map-zoom-in").Button.IsInteractable(),Is.False);Click("map-marker:1");Click("map-waypoint");Assert.That(view.SchematicWaypoint,Is.EqualTo(1));var scroll=Control("map-canvas").GetComponent<ScrollRect>();events.SetSelectedGameObject(Control("map-marker:1").gameObject);scroll.normalizedPosition=new Vector2(.5f,.5f);view.ScrollResource(new Vector2(.5f,.5f),.3f);Assert.That(scroll.horizontalNormalizedPosition,Is.GreaterThan(.5f));Assert.That(scroll.verticalNormalizedPosition,Is.GreaterThan(.5f));Click("map-waypoint");Assert.That(view.SchematicWaypoint,Is.Null);view.Back();Click("map");Assert.That(view.SchematicWaypoint,Is.Null);Assert.That(Mutations,Is.Empty);
        }
        [Test] public void AlmanacFoldsUseDiscoveredFactsAndRecipeLinksConcealUnknownOutputs()
        {
            string id=CampExpeditionDefinition.ToolZeroId;view.Render(new CampMenuPresentation{discovered=new[]{id}});Click("guide");Click("resource-open:almanac");Search("Almanac search").text=CampMenuCatalog.Load().Find(id).name;Layout();var detail=Control("almanac-detail").GetComponent<ScrollRect>();float previous=detail.content.rect.height;Click("almanac-fold:0");Assert.That(detail.content.rect.height,Is.GreaterThan(previous));var links=root.GetComponentsInChildren<CampMenuControl>().Where(c=>c.key.StartsWith("almanac-use:",StringComparison.Ordinal)).ToArray();Assert.That(links.Length,Is.EqualTo(1));Assert.That(links[0].title.text,Does.Contain(CampMenuCatalog.Load().Find(id).name));Click(links[0].key);Assert.That(view.Page,Is.EqualTo("workstations"));Assert.That(view.WorkshopRecipe,Is.EqualTo(id));Assert.That(Mutations,Is.Empty);
        }
        [Test] public void OpeningAnAlmanacDescriptionPreservesTheCollectionScroll()
        {
            view.Render(new CampMenuPresentation{discovered=new[]{CampExpeditionDefinition.ToolZeroId}});Click("guide");Click("resource-open:almanac");Layout();var grid=Control("almanac-entry:0").GetComponentInParent<ScrollRect>();grid.verticalNormalizedPosition=.5f;var offset=grid.content.anchoredPosition;Click("almanac-fold:0");Assert.That(grid.content.anchoredPosition,Is.EqualTo(offset));
        }
        [Test] public void RepeatedAlmanacPagingKeepsTheControlPoolBoundedAndLongNamesFitTheirCards()
        {
            var item=CampMenuCatalog.Load().Items.OrderByDescending(i=>i.name.Length).First();view.Render(new CampMenuPresentation{discovered=new[]{item.id}});Click("guide");Click("resource-open:almanac");for(int i=0;i<4;i++){Click("almanac-next");Click("almanac-previous");}Search("Almanac search").text=item.name;view.SetLargeText(true);Layout();var card=Control("almanac-entry:0");Assert.That(card.title.preferredHeight,Is.LessThanOrEqualTo(card.title.rectTransform.rect.height+1));Assert.That(root.GetComponentsInChildren<CampMenuControl>(true).Count(c=>c.key.StartsWith("almanac-entry:",StringComparison.Ordinal)),Is.EqualTo(36));
        }
        [Test] public void ResourceMutationControlsRemainContainedBySaveQuotes()
        {
            Click("map");view.ShowQuote("Save waiting");Click("map-waypoint");Assert.That(view.SchematicWaypoint,Is.Null);Click("resource-back:map");Assert.That(view.Page,Is.EqualTo("map"));view.Render(new CampMenuPresentation());Assert.That(Control("map-waypoint").Button.IsInteractable(),Is.False);view.HideQuote();Assert.That(Control("map-waypoint").Button.IsInteractable(),Is.True);
        }
        [Test] public void NarrowLargeTextKeepsTheDiscoveryCaptionAndMapMarkerLabelsComplete()
        {
            Click("guide");Click("resource-open:almanac");view.SetLargeText(true);Layout();var progress=root.GetComponentsInChildren<Text>().Single(t=>t.text.Contains("ITEMS DISCOVERED"));progress.rectTransform.parent.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,200);Layout();Assert.That(progress.preferredHeight,Is.LessThanOrEqualTo(progress.rectTransform.rect.height+1));
            view.ShowPage("map");var scroll=Control("map-canvas").GetComponent<ScrollRect>();scroll.viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,540);Layout();var first=RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.content,Control("map-marker:0").transform);var second=RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.content,Control("map-marker:1").transform);Assert.That(first.Intersects(second),Is.False,"Responsive labels must not overlap while schematic anchors remain unchanged.");
        }
        [Test] public void DetailAndGuideReadingPanesSupportRightStickWithoutMovingOtherPanes()
        {
            Click("guide");Layout();var article=Control("guide-article").GetComponent<ScrollRect>();article.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,3000);events.SetSelectedGameObject(article.gameObject);Click("guide-article");Assert.That(Mutations,Is.Empty);article.verticalNormalizedPosition=1;view.ScrollResource(new Vector2(0,-1),.2f);Assert.That(article.verticalNormalizedPosition,Is.LessThan(1));Move("guide-article",MoveDirection.Right);Assert.That(events.currentSelectedGameObject,Is.SameAs(Control("resource-open:almanac").gameObject));
        }
    }
}
