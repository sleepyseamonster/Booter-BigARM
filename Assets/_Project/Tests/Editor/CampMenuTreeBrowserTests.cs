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
    public sealed class CampMenuTreeBrowserTests
    {
        private GameObject root,eventRoot;
        private CampExpeditionMenuView view;
        private CampMenuCatalog catalog;
        private EventSystem events;
        [SetUp] public void Setup()
        {
            root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/CampExpedition/CampExpeditionMenu.prefab"));
            eventRoot=new GameObject("Tree library fixture",typeof(EventSystem));events=eventRoot.GetComponent<EventSystem>();view=root.GetComponent<CampExpeditionMenuView>();
            view.Initialize(events);view.Render(new CampMenuPresentation{canPlay=true});view.ShowPage("tree");catalog=CampMenuCatalog.Load();Canvas.ForceUpdateCanvases();view.RefreshLayout();Canvas.ForceUpdateCanvases();
        }
        [TearDown] public void Cleanup(){root.SetActive(false);Object.DestroyImmediate(root);Object.DestroyImmediate(eventRoot);}
        private CampMenuControl[] Controls=>root.GetComponentsInChildren<CampMenuControl>(true);
        private InputField Search=>root.GetComponentsInChildren<InputField>(true).Single(field=>field.name=="Tree search");
        [TestCase(false)][TestCase(true)] public void TreeHeadersRemainInsideTheirColumnInsteadOfOverlappingLibraryOrInspector(bool large)
        {
            view.SetLargeText(large);Canvas.ForceUpdateCanvases();view.RefreshLayout();Canvas.ForceUpdateCanvases();var viewport=(RectTransform)view.TreeCanvas.transform;
            foreach(var text in root.GetComponentsInChildren<Text>().Where(text=>text.name is "Tree heading" or "Tree input counts"))
            {var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(viewport,text.transform);Assert.That(bounds.min.x,Is.GreaterThanOrEqualTo(viewport.rect.xMin-1),text.name);Assert.That(bounds.max.x,Is.LessThanOrEqualTo(viewport.rect.xMax+1),text.name);Assert.That(text.preferredHeight,Is.LessThanOrEqualTo(text.rectTransform.rect.height+1),text.name);}
        }
        [Test] public void ExpandingAndReflowingTheLibraryPreservesVisibleControllerFocus()
        {
            var group=Controls.Single(c=>c.key=="tree-group:equipment");events.SetSelectedGameObject(group.gameObject);Assert.That(group.HasNavigationFocus,Is.True);
            group.Button.onClick.Invoke();view.RefreshLayout();Assert.That(events.currentSelectedGameObject,Is.SameAs(group.gameObject));Assert.That(group.HasNavigationFocus,Is.True);
        }
        [Test] public void LibraryAndPointerSelectedCardsCanMoveIntoTheCanvasThroughOneEventStream()
        {
            var group=Controls.Single(c=>c.key=="tree-group:equipment");events.SetSelectedGameObject(group.gameObject);
            group.GetComponent<CampMenuDirectionalLink>().OnMove(new AxisEventData(events){moveDir=MoveDirection.Right,moveVector=Vector2.right});Assert.That(events.currentSelectedGameObject,Is.SameAs(view.TreeCanvas.gameObject));
            var card=view.TreeCanvas.Nodes[0];events.SetSelectedGameObject(card.gameObject);card.GetComponent<CampMenuDirectionalLink>().OnMove(new AxisEventData(events){moveDir=MoveDirection.Up,moveVector=Vector2.up});
            Assert.That(events.currentSelectedGameObject,Is.SameAs(view.TreeCanvas.gameObject));
        }
        [Test] public void LibraryStartsGroupedAndSearchRevealsMatchingFinishedItemsWithoutChangingTheGraph()
        {
            Assert.That(Controls.Single(c=>c.key=="tree-group:equipment").title.text,Does.Contain("Equipment"));
            Assert.That(Controls.Where(c=>c.key.StartsWith("node:",StringComparison.Ordinal)&&c.gameObject.activeInHierarchy),Is.Empty);
            string selected=view.DisplayedTreeRoot;Search.text=catalog.Find("author.tool.arc_powered_rotary_percussion_mining_tool").name;
            var roots=Controls.Where(c=>c.key.StartsWith("node:",StringComparison.Ordinal)&&c.gameObject.activeInHierarchy).ToArray();
            int matches=catalog.Trees.Count(tree=>catalog.Find(tree.rootId).name.IndexOf(Search.text,StringComparison.OrdinalIgnoreCase)>=0);
            var activeKeys=Controls.Where(c=>c.gameObject.activeInHierarchy&&c.key.StartsWith("tree-group:",StringComparison.Ordinal)).Select(c=>c.key);
            Assert.That(roots,Is.Not.Empty,"Query '"+Search.text+"', matching data "+matches+", active rows "+string.Join(", ",activeKeys));
            Assert.That(roots.All(c=>catalog.Tree(c.key.Substring(5))!=null),Is.True);Assert.That(view.DisplayedTreeRoot,Is.EqualTo(selected));
            roots[0].Button.onClick.Invoke();Assert.That(view.DisplayedTreeRoot,Is.EqualTo(roots[0].key.Substring(5)));Assert.That(view.InspectedItem,Is.Null);
        }
        [Test] public void EmptySearchResultsPreserveTheSelectedTreeAndRepairFocusToSearch()
        {
            string selected=view.DisplayedTreeRoot;Search.text="no_such_crafting_tree_123";Assert.That(events.currentSelectedGameObject,Is.SameAs(Search.gameObject));view.Render(new CampMenuPresentation{canPlay=true});
            Assert.That(view.DisplayedTreeRoot,Is.EqualTo(selected));Assert.That(events.currentSelectedGameObject,Is.SameAs(Search.gameObject));
            Assert.That(Controls.Where(c=>c.key.StartsWith("node:",StringComparison.Ordinal)&&c.gameObject.activeInHierarchy),Is.Empty);
        }
        [Test] public void UnqualifiedDisplayTreeHasItsOwnCardsMetadataAndNeverCreatesRuntimeDefinitions()
        {
            const string id="author.tool.arc_powered_rotary_percussion_mining_tool";Assert.That(view.definition.Find(id),Is.Null);
            view.OpenDisplayTree(id);Assert.That(view.DisplayedTreeRoot,Is.EqualTo(id));
            CollectionAssert.AreEquivalent(catalog.Tree(id).nodes.Select(node=>"tree-node:"+node.id),view.TreeCanvas.Nodes.Select(c=>c.key));
            var finished=view.TreeCanvas.Nodes.Single(c=>c.key=="tree-node:"+id);finished.Button.onClick.Invoke();
            Assert.That(view.inspectorTitle.text,Is.EqualTo(catalog.Find(id).name));Assert.That(view.inspectorBody.text,Does.Contain(catalog.Find(id).purpose));Assert.That(finished.subtitle.text,Does.Contain("Finished item"));
            Assert.That(view.definition.Find(id),Is.Null);
        }
        [Test] public void RepeatedTreeChangesReuseABoundedCardPoolAndOneCameraHighlight()
        {
            var largest=catalog.Trees.OrderByDescending(tree=>tree.nodes.Length).First();var smallest=catalog.Trees.OrderBy(tree=>tree.nodes.Length).First();
            for(int i=0;i<4;i++){view.OpenDisplayTree(largest.rootId);view.TreeCanvas.Pan(Vector2.one*10);view.OpenDisplayTree(smallest.rootId);}
            Assert.That(view.TreeCanvas.transform.Find("Recipe canvas").GetComponentsInChildren<CampMenuControl>(true).Length,Is.EqualTo(largest.nodes.Length));
            Assert.That(root.GetComponentsInChildren<Transform>(true).Count(t=>t.name=="Crosshair target"),Is.EqualTo(1));
            Assert.That(view.TreeCanvas.Nodes.Length,Is.EqualTo(smallest.nodes.Length));
            Assert.That(view.TreeCanvas.Nodes.All(c=>c.gameObject.activeInHierarchy),Is.True);
            Assert.That(root.GetComponentsInChildren<CampTreeCanvasController>(true).Length,Is.EqualTo(1));
        }
        [Test] public void SelectionDimsDetachedNodesAndTrackingMarkerFollowsTheStableId()
        {
            var tree=catalog.Trees.First(t=>t.nodes.Length>catalog.Inputs(t.rootId,t.rootId).Count+1);view.OpenDisplayTree(tree.rootId);
            var finished=view.TreeCanvas.Nodes.Single(c=>c.key=="tree-node:"+tree.rootId);finished.Button.onClick.Invoke();
            var connected=catalog.Inputs(tree.rootId,tree.rootId);var detached=tree.nodes.First(node=>node.id!=tree.rootId&&!connected.Contains(node.id));
            Assert.That(view.TreeCanvas.Nodes.Single(c=>c.key=="tree-node:"+detached.id).GetComponent<CanvasGroup>().alpha,Is.EqualTo(.18f));
            var dimmed=view.TreeCanvas.Nodes.Single(c=>c.key=="tree-node:"+detached.id);dimmed.OnPointerEnter(new PointerEventData(events));Assert.That(dimmed.GetComponent<CanvasGroup>().alpha,Is.EqualTo(1));dimmed.OnPointerExit(new PointerEventData(events));Assert.That(dimmed.GetComponent<CanvasGroup>().alpha,Is.EqualTo(.18f));
            view.Render(new CampMenuPresentation{tracked=new[]{tree.rootId}});Assert.That(finished.transform.Find("Tracked marker").gameObject.activeSelf,Is.True);
            view.Render(new CampMenuPresentation());Assert.That(finished.transform.Find("Tracked marker").gameObject.activeSelf,Is.False);
        }
        [Test] public void DisplayArtworkLoadsAsANativeSpriteAndIsCached()
        {
            var item=catalog.Items.First(i=>!string.IsNullOrEmpty(i.artResource)&&view.definition.Find(i.id)==null);var first=catalog.Art(item.id);
            Assert.That(first,Is.Not.Null);Assert.That(catalog.Art(item.id),Is.SameAs(first));Assert.That(first.rect.width,Is.LessThanOrEqualTo(1024));Assert.That(first.rect.height,Is.LessThanOrEqualTo(1024));
        }
        [Test] public void EveryExportedTreeProducesNativeConnectionsWithinTheCanvasVertexLimit()
        {
            var graphic=view.TreeCanvas.transform.Find("Recipe canvas").GetComponent<CampMenuTreeGraphic>();
            var populate=typeof(CampMenuTreeGraphic).GetMethod("OnPopulateMesh",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.DeclaredOnly);
            int largest=0,count=0;
            foreach(var tree in catalog.Trees)
            {
                graphic.SetTree(tree);using var vertices=new VertexHelper();populate.Invoke(graphic,new object[]{vertices});
                Assert.That(vertices.currentVertCount,Is.LessThan(65000),tree.rootId);if(tree.nodes.Any(node=>node.inputs.Length>0))Assert.That(vertices.currentVertCount,Is.GreaterThan(0),tree.rootId);
                largest=Math.Max(largest,vertices.currentVertCount);count++;
            }
            TestContext.WriteLine("Rendered "+count+" tree connection meshes; maximum "+largest+" vertices.");
        }
        [TestCase("tool.micro_dust_harvester")]
        [TestCase("equipment.grappling_hook")]
        [TestCase("equipment.arc_battery_empty")]
        [TestCase("tool.popup_camp")]
        [TestCase("resource.airborne_dust")]
        [TestCase("generic")]
        public void BrowserVectorFallbacksProduceNativeCanvasGeometry(string id)
        {
            if(id=="generic")id=catalog.Items.First(item=>item.id.StartsWith("author.",StringComparison.Ordinal)&&string.IsNullOrEmpty(item.artResource)&&view.definition.Find(item.id)==null).id;
            view.Inspect(id);Canvas.ForceUpdateCanvases();var glyph=view.inspectorArt.GetComponentInChildren<CampMenuItemGlyphGraphic>();Assert.That(glyph,Is.Not.Null);Assert.That(glyph.canvasRenderer,Is.Not.Null);
            glyph.Rebuild(CanvasUpdate.PreRender);var mesh=glyph.canvasRenderer.GetMesh();Assert.That(mesh,Is.Not.Null);Assert.That(mesh.vertexCount,Is.GreaterThan(0));
        }
    }
}
