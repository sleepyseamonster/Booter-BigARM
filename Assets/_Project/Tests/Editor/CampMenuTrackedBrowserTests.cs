using System;
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
    public sealed class CampMenuTrackedBrowserTests
    {
        private GameObject root,eventRoot;
        private CampExpeditionMenuView view;
        private CampMenuCatalog catalog;
        private EventSystem events;
        private CampMenuControl[] Controls=>root.GetComponentsInChildren<CampMenuControl>(true);
        private CampMenuControl Control(string key)=>Controls.Single(c=>c.key==key);
        [SetUp] public void Setup()
        {
            root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/CampExpedition/CampExpeditionMenu.prefab"));
            eventRoot=new GameObject("Tracked fixture",typeof(EventSystem));events=eventRoot.GetComponent<EventSystem>();view=root.GetComponent<CampExpeditionMenuView>();view.Initialize(events);catalog=CampMenuCatalog.Load();
        }
        [TearDown] public void Cleanup(){root.SetActive(false);Object.DestroyImmediate(root);Object.DestroyImmediate(eventRoot);}
        private void Render(params string[] ids){view.Render(new CampMenuPresentation{tracked=ids});view.ShowPage("tracked");Canvas.ForceUpdateCanvases();view.RefreshLayout();Canvas.ForceUpdateCanvases();}
        private void Move(MoveDirection direction)
        {events.currentSelectedGameObject.GetComponent<CampMenuDirectionalLink>().OnMove(new AxisEventData(events){moveDir=direction});}

        [TestCase(false)][TestCase(true)] public void BookmarkCategoryAndRarityRemainVisibleWhenMetadataWraps(bool large)
        {
            view.SetLargeText(large);Render("author.bio.stone_bone");var card=Control("bookmark:0");Assert.That(card.subtitle.text,Does.Contain("\n"));Assert.That(card.subtitle.preferredHeight,Is.LessThanOrEqualTo(card.subtitle.rectTransform.rect.height+1));
        }

        [Test] public void ReverseUsesUseActualConnectedInputsAndStableNameOrdering()
        {
            var tracked=new[]{CampExpeditionDefinition.ToolZeroId,"author.bio.stone_bone"};Render(tracked);
            var expected=catalog.ResultsUsing(tracked);var actual=Controls.Where(c=>c.gameObject.activeInHierarchy&&c.key.StartsWith("tracked-filter:",StringComparison.Ordinal)&&c.key!="tracked-filter:all").Select(c=>c.key.Substring(15));
            CollectionAssert.AreEqual(expected,actual);Assert.That(Control("tracked-filter:all").subtitle.text,Is.EqualTo("2"));
            view.SelectTrackedFilter(CampExpeditionDefinition.ToolZeroId);CollectionAssert.AreEqual(new[]{"author.bio.stone_bone"},view.TrackedVisibleIds);
            Assert.That(Control("tracked-filter:"+CampExpeditionDefinition.ToolZeroId).subtitle.text,Is.EqualTo("1"));Assert.That(tracked,Has.Length.EqualTo(2));
        }
        [Test] public void RemovingSelectedBookmarksRepairsTheFilterSelectionAndInspector()
        {
            Render("author.bio.stone_bone",CampExpeditionDefinition.ToolZeroId);view.SelectTrackedFilter(CampExpeditionDefinition.ToolZeroId);
            view.Render(new CampMenuPresentation{tracked=new[]{CampExpeditionDefinition.ToolZeroId}});
            Assert.That(view.TrackedFilter,Is.Empty);Assert.That(view.TrackedSelected,Is.EqualTo(CampExpeditionDefinition.ToolZeroId));Assert.That(view.InspectedItem,Is.EqualTo(view.TrackedSelected));
            view.Render(new CampMenuPresentation());Assert.That(view.TrackedSelected,Is.Null);Assert.That(view.InspectedItem,Is.Null);Assert.That(Control("track").gameObject.activeSelf,Is.False);Assert.That(events.currentSelectedGameObject.activeInHierarchy,Is.True);
        }
        [Test] public void UnknownSavedIdsStayVisibleAndRemovableWithoutInventingADefinition()
        {
            const string missing="retired.item.saved.bookmark";Render(missing);view.RefreshLayout();
            Assert.That(view.TrackedVisibleIds,Is.EqualTo(new[]{missing}));Assert.That(view.inspectorBody.text,Does.Contain("definition unavailable"));Assert.That(Control("inspector-title").gameObject.activeInHierarchy,Is.True);
            string command=null;view.Command+=key=>command=key;Control("track").Button.onClick.Invoke();Assert.That(command,Is.EqualTo("track"));Assert.That(catalog.Find(missing),Is.Null);
        }
        [Test] public void FullCatalogHasABoundedCardPoolAndItsLastRowRemainsReachable()
        {
            Render(catalog.Items.Select(item=>item.id).ToArray());Assert.That(view.TrackedVisibleIds,Has.Length.EqualTo(catalog.Items.Count()));Assert.That(view.TrackedCardPoolCount,Is.LessThan(50));
            var grid=Control("tracked-grid").GetComponent<ScrollRect>();grid.content.anchoredPosition=new Vector2(0,grid.content.rect.height-grid.viewport.rect.height);grid.onValueChanged.Invoke(Vector2.zero);
            string key="bookmark:"+(catalog.Items.Count()-1);var last=Control(key);events.SetSelectedGameObject(last.gameObject);Move(MoveDirection.Down);
            Assert.That(view.TrackedSelected,Is.EqualTo(view.TrackedVisibleIds.Last()));Assert.That(view.TrackedCardPoolCount,Is.LessThan(50));
            var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(grid.viewport,events.currentSelectedGameObject.transform);
            if(bounds.size.y<=grid.viewport.rect.height)Assert.That(bounds.min.y,Is.GreaterThanOrEqualTo(grid.viewport.rect.yMin-1));
            else Assert.That(bounds.max.y,Is.EqualTo(grid.viewport.rect.yMax).Within(1),"A tile taller than the headless viewport must expose its top rather than disappear.");
            view.Render(new CampMenuPresentation{tracked=new[]{CampExpeditionDefinition.ToolZeroId}});Assert.That(grid.content.anchoredPosition.y,Is.InRange(0,Mathf.Max(0,grid.content.rect.height-grid.viewport.rect.height)+1));Assert.That(Control("bookmark:0").gameObject.activeInHierarchy,Is.True);
        }
        [Test] public void DpadAlwaysMovesTheGridWhileLeftAndRightStickUseIndependentLanes()
        {
            Render("author.bio.stone_bone",CampExpeditionDefinition.ToolZeroId);events.SetSelectedGameObject(Control("tracked-filter:all").gameObject);Assert.That(view.TrackedCanUntrack,Is.False);Assert.That(Control("hint:west").Button.interactable,Is.False);
            Move(MoveDirection.Right);Assert.That(view.TrackedSelected,Is.EqualTo(view.TrackedVisibleIds[1]));Assert.That(events.currentSelectedGameObject.name,Does.StartWith("bookmark:"));Assert.That(view.TrackedCanUntrack,Is.True);
            view.AdvanceTrackedInput(Vector2.down,Vector2.zero,.01f);Assert.That(view.TrackedFilter,Is.EqualTo(catalog.ResultsUsing(view.TrackedVisibleIds)[0]));
            var selected=view.TrackedSelected;view.AdvanceTrackedInput(Vector2.zero,Vector2.right,.01f);Assert.That(events.currentSelectedGameObject,Is.SameAs(Control("track").gameObject));Assert.That(view.TrackedSelected,Is.EqualTo(selected));
            Move(MoveDirection.Left);Assert.That(events.currentSelectedGameObject.name,Does.StartWith("bookmark:"));
        }
        [Test] public void HiddenAndQuotedMenusCannotConsumeTrackedLaneCommands()
        {
            Render("author.bio.stone_bone");view.ShowQuote("Waiting for save");var focused=events.currentSelectedGameObject;
            view.AdvanceTrackedInput(Vector2.down,Vector2.right,.1f);view.SelectTrackedFilter(CampExpeditionDefinition.ToolZeroId);view.FocusTrackedSelection();Assert.That(view.TrackedFilter,Is.Empty);Assert.That(events.currentSelectedGameObject,Is.SameAs(focused));
            view.HideQuote();view.SetVisible(false);view.Render(new CampMenuPresentation{tracked=new[]{CampExpeditionDefinition.ToolZeroId}});view.AdvanceTrackedInput(Vector2.down,Vector2.right,.1f);Assert.That(view.TrackedFilter,Is.Empty);Assert.That(events.currentSelectedGameObject,Is.Null);
        }
        [Test] public void ControlsClonedUnderASaveQuoteRestoreTheirRealInteractability()
        {
            Render("author.bio.stone_bone");view.ShowQuote("Waiting for save");view.Render(new CampMenuPresentation{tracked=catalog.Items.Select(item=>item.id).ToArray()});view.HideQuote();
            Assert.That(Controls.Where(c=>c.gameObject.activeInHierarchy&&c.key.StartsWith("tracked-filter:",StringComparison.Ordinal)).All(c=>c.Button.IsInteractable()),Is.True);
            Assert.That(Control("tracked-filter:all").Button.interactable,Is.True);
        }
        [Test] public void ReturningToThePageRestoresTheSelectedStableId()
        {
            Render("author.bio.stone_bone",CampExpeditionDefinition.ToolZeroId);Move(MoveDirection.Right);var selected=view.TrackedSelected;view.ShowPage("tree");view.ShowPage("tracked");Assert.That(view.TrackedSelected,Is.EqualTo(selected));Assert.That(events.currentSelectedGameObject.GetComponent<CampMenuControl>().key,Is.EqualTo("bookmark:"+Array.IndexOf(view.TrackedVisibleIds,selected)));
        }
        [Test] public void AllTrackedStaysFixedOutsideTheScrollingResultsAndResizingKeepsGridFocus()
        {
            Render(catalog.Items.Select(item=>item.id).ToArray());var all=Control("tracked-filter:all");Assert.That(all.GetComponentInParent<ScrollRect>(),Is.Null);
            var grid=Control("tracked-grid").GetComponent<ScrollRect>();grid.content.anchoredPosition=new Vector2(0,grid.content.rect.height-grid.viewport.rect.height);grid.onValueChanged.Invoke(Vector2.zero);events.SetSelectedGameObject(Control("bookmark:"+(view.TrackedVisibleIds.Length-1)).gameObject);var id=view.TrackedSelected;
            view.SetLargeText(true);Assert.That(view.TrackedSelected,Is.EqualTo(id));Assert.That(events.currentSelectedGameObject.GetComponent<CampMenuControl>().key,Is.EqualTo("bookmark:"+(view.TrackedVisibleIds.Length-1)));Assert.That(Control("hint:navigate").transform.Find("Navigation glyph").GetComponent<Text>().text,Is.EqualTo("+"));
        }
        [TestCase(2,8,3,MoveDirection.Right,2)]
        [TestCase(3,8,3,MoveDirection.Left,3)]
        [TestCase(5,8,3,MoveDirection.Down,7)]
        [TestCase(7,8,3,MoveDirection.Down,7)]
        [TestCase(1,8,3,MoveDirection.Up,1)]
        public void GridBoundariesMatchIncompleteBrowserRows(int index,int count,int columns,MoveDirection direction,int expected)
        {Assert.That(CampExpeditionMenuView.TrackedGridTarget(index,count,columns,direction),Is.EqualTo(expected));}
        [Test] public void RawStickRepeatUsesTheBrowserThresholdAndIndependentRepeatTimes()
        {
            var left=new CampTrackedStickRepeat();var right=new CampTrackedStickRepeat();Assert.That(left.Sample(.55f,0),Is.Zero);Assert.That(left.Sample(.56f,0),Is.EqualTo(1));Assert.That(left.Sample(.8f,.359),Is.Zero);Assert.That(left.Sample(.8f,.36),Is.EqualTo(1));Assert.That(left.Sample(.8f,.484),Is.Zero);Assert.That(left.Sample(.8f,.486),Is.EqualTo(1));Assert.That(right.Sample(-.8f,.4),Is.EqualTo(-1));Assert.That(left.Sample(-.8f,.5),Is.EqualTo(-1));Assert.That(left.Sample(float.NaN,.6),Is.Zero);Assert.That(left.Sample(-.8f,.6),Is.EqualTo(-1));
        }
    }
}
