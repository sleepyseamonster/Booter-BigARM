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
    public sealed class CampTreeCanvasTests
    {
        private GameObject root,eventRoot;
        private CampExpeditionMenuView view;
        private EventSystem events;
        private CampTreeCanvasController camera;
        [SetUp] public void Setup()
        {
            root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/CampExpedition/CampExpeditionMenu.prefab"));
            eventRoot=new GameObject("Tree camera fixture",typeof(EventSystem));events=eventRoot.GetComponent<EventSystem>();
            view=root.GetComponent<CampExpeditionMenuView>();view.Initialize(events);view.Render(new CampMenuPresentation{canPlay=true});view.ShowPage("tree");Canvas.ForceUpdateCanvases();view.RefreshLayout();Canvas.ForceUpdateCanvases();
            camera=view.TreeCanvas;camera.RefreshCamera();
        }
        [TearDown] public void Cleanup(){root.SetActive(false);Object.DestroyImmediate(root);Object.DestroyImmediate(eventRoot);}
        private RectTransform Viewport=>(RectTransform)camera.transform;
        private RectTransform Stage=>(RectTransform)camera.transform.Find("Recipe canvas");
        private Vector2 Center=>(Viewport.rect.size*.5f-camera.Offset)/camera.Scale;
        [Test] public void CameraFitsTheCompleteStageAndIsTheOnlyTransformOwner()
        {
            Assert.That(camera.GetComponent<ScrollRect>().enabled,Is.False);
            Assert.That(camera.Scale,Is.EqualTo(CampTreeCameraMath.Fit(Viewport.rect.size,Stage.rect.size)).Within(.0001f));
            Assert.That(camera.CrosshairAlpha,Is.Zero,"Automatic fitting must not show activity feedback.");
            Assert.That(camera.transform.Find("Camera crosshair").GetComponent<Graphic>().raycastTarget,Is.False);
        }
        [Test] public void ZoomPreservesTheWorldPointUnderTheFixedCenterAndClampsToBrowserRange()
        {
            var before=Center;camera.Zoom(1.15f);Assert.That((Center-before).magnitude,Is.LessThan(.001f));
            camera.Zoom(100);Assert.That(camera.Scale,Is.EqualTo(1));camera.Zoom(.0001f);Assert.That(camera.Scale,Is.EqualTo(.1f));
        }
        [Test] public void PanningCannotLoseTheAuthoredGraphBeyondItsPaddedBounds()
        {
            camera.Pan(new Vector2(100000,100000));var items=view.definition.items.Where(i=>i.visibleInTree).ToArray();
            Assert.That(Center.x,Is.EqualTo(items.Min(i=>i.treeX)-120).Within(.001f));Assert.That(Center.y,Is.EqualTo(items.Min(i=>i.treeY)-120).Within(.001f));
            camera.Pan(new Vector2(-200000,-200000));Assert.That(Center.x,Is.EqualTo(items.Max(i=>i.treeX+145)+120).Within(.001f));Assert.That(Center.y,Is.EqualTo(items.Max(i=>i.treeY+238)+120).Within(.001f));
        }
        [Test] public void StickMotionUsesBrowserDirectionSpeedExponentialZoomAndFrameCap()
        {
            var before=camera.Offset;float scale=camera.Scale;camera.MoveSticks(new Vector2(.5f,.5f),0,.5f);
            Assert.That((camera.Offset-before-new Vector2(-21.25f,21.25f)).magnitude,Is.LessThan(.001f));
            camera.MoveSticks(Vector2.zero,1,.05f);Assert.That(camera.Scale,Is.EqualTo(Mathf.Min(1,scale*Mathf.Exp(.09f))).Within(.0001f));
        }
        [Test] public void CrosshairFadesInOnActualMovementAndFadesOutAfterIdle()
        {
            camera.Pan(new Vector2(20,0));camera.AdvanceVisual(.09f);Assert.That(camera.CrosshairAlpha,Is.EqualTo(1));
            camera.AdvanceVisual(2.5f);camera.AdvanceVisual(.3f);Assert.That(camera.CrosshairAlpha,Is.Zero);
            camera.Zoom(.000001f);camera.AdvanceVisual(3);camera.Zoom(.5f);camera.AdvanceVisual(.1f);Assert.That(camera.CrosshairAlpha,Is.Zero,"At the minimum zoom, further zoom-out is not movement.");
        }
        [Test] public void ReducedMotionKeepsCrosshairFeedbackWithoutOpacityTweening()
        {camera.SetReducedMotion(true);camera.Pan(Vector2.right*20);camera.AdvanceVisual(.001f);Assert.That(camera.CrosshairAlpha,Is.EqualTo(1));camera.AdvanceVisual(3);Assert.That(camera.CrosshairAlpha,Is.Zero);}
        [Test] public void SouthSelectsTheNodeUnderTheCrosshairAndDoesNotRetargetTheWorld()
        {
            var node=view.definition.items.First(i=>i.visibleInTree);var point=new Vector2(node.treeX+72.5f,node.treeY+119);
            camera.Pan((Center-point)*camera.Scale);Assert.That(camera.Target.key,Is.EqualTo("tree-node:"+node.id));
            camera.OnSubmit(new BaseEventData(events));Assert.That(view.InspectedItem,Is.EqualTo(node.id));Assert.That(view.TreeTrackingTarget,Is.EqualTo(node.id));
            view.Render(new CampMenuPresentation{canPlay=true});Assert.That(events.currentSelectedGameObject,Is.SameAs(camera.gameObject),"Rendering bookmarks must preserve camera focus.");
        }
        [Test] public void ModalAndHiddenCanvasRejectPointerStickAndZoomCommands()
        {
            var offset=camera.Offset;float scale=camera.Scale;view.ShowQuote("Confirm transaction");camera.Pan(Vector2.one*40);camera.Zoom(1.15f);camera.MoveSticks(Vector2.one,1,.05f);
            Assert.That(camera.Offset,Is.EqualTo(offset));Assert.That(camera.Scale,Is.EqualTo(scale));view.HideQuote();view.SetVisible(false);camera.Pan(Vector2.one*40);Assert.That(camera.Offset,Is.EqualTo(offset));
        }
        [Test] public void ClickableSelectLegendSubmitsTheCanvasCrosshairTarget()
        {
            var node=view.definition.items.First(i=>i.visibleInTree);var point=new Vector2(node.treeX+72.5f,node.treeY+119);
            camera.Pan((Center-point)*camera.Scale);var legend=root.GetComponentsInChildren<CampMenuControl>(true).Single(c=>c.key=="hint:select");
            events.SetSelectedGameObject(legend.gameObject);legend.Button.onClick.Invoke();
            Assert.That(view.InspectedItem,Is.EqualTo(node.id));Assert.That(events.currentSelectedGameObject,Is.SameAs(camera.gameObject));
        }
        [Test] public void WheelZoomAndKeyboardArrowsUseTheBrowserCenterAndDirections()
        {
            var point=Center;float scale=camera.Scale;camera.OnScroll(new PointerEventData(events){scrollDelta=Vector2.up});
            Assert.That(camera.Scale,Is.EqualTo(Mathf.Min(1,scale*Mathf.Exp(.2f))).Within(.0001f));Assert.That((Center-point).magnitude,Is.LessThan(.001f));
            var offset=camera.Offset;camera.OnMove(new AxisEventData(events){moveVector=Vector2.up});Assert.That(camera.Offset.y,Is.EqualTo(offset.y+60).Within(.001f));
        }
        [TestCase(.1f,0)] [TestCase(.22f,0)] [TestCase(.61f,.5f)] [TestCase(-.61f,-.5f)] [TestCase(1,1)]
        public void CameraUsesOneBrowserDeadzoneRemap(float raw,float expected)=>Assert.That(CampTreeCameraMath.Axis(raw),Is.EqualTo(expected).Within(.0001f));
        [TestCase(.1f,0)] [TestCase(.25f,0)] [TestCase(.375f,.5f)] [TestCase(.5f,1)]
        public void ConnectionsFadeAtTheBrowserOverviewThreshold(float scale,float expected)=>Assert.That(CampTreeCameraMath.Connections(scale),Is.EqualTo(expected));
        [TestCase(PointerEventData.InputButton.Left,false,true)]
        [TestCase(PointerEventData.InputButton.Left,true,false)]
        [TestCase(PointerEventData.InputButton.Middle,true,true)]
        public void PointerDragBubblesFromCardsButLeftClickKeepsCardSelection(PointerEventData.InputButton button,bool onCard,bool moves)
        {
            var node=root.GetComponentsInChildren<CampMenuControl>().First(c=>c.key.StartsWith("tree-node:",StringComparison.Ordinal));
            var hit=onCard?node.gameObject:camera.gameObject;
            var target=ExecuteEvents.GetEventHandler<IDragHandler>(hit);Assert.That(target,Is.SameAs(camera.gameObject));
            var data=new PointerEventData(events){button=button,position=new Vector2(100,100),pointerPressRaycast=new RaycastResult{gameObject=hit}};
            var before=camera.Offset;ExecuteEvents.Execute(target,data,ExecuteEvents.beginDragHandler);data.position+=new Vector2(32,18);ExecuteEvents.Execute(target,data,ExecuteEvents.dragHandler);ExecuteEvents.Execute(target,data,ExecuteEvents.endDragHandler);
            Assert.That((camera.Offset-before-(moves?new Vector2(32,-18):Vector2.zero)).magnitude,Is.LessThan(.001f));
            Assert.That(ExecuteEvents.GetEventHandler<IScrollHandler>(hit),Is.SameAs(camera.gameObject));
        }
        [Test] public void ReinitializingTheViewUpdatesCameraFocusAndRightClickDoesNotClaimIt()
        {
            camera.OnPointerDown(new PointerEventData(events){button=PointerEventData.InputButton.Right});Assert.That(camera.OwnsFocus,Is.False);
            var replacement=new GameObject("Replacement tree focus",typeof(EventSystem));
            try{var next=replacement.GetComponent<EventSystem>();view.Initialize(next);camera.Pan(Vector2.zero);Assert.That(next.currentSelectedGameObject,Is.SameAs(camera.gameObject));}
            finally{Object.DestroyImmediate(replacement);}
        }
        [Test] public void InspectorTrackKeepsItsSelectedItemWhenTheCameraIsOverAnotherCard()
        {
            var nodes=view.definition.items.Where(i=>i.visibleInTree).Take(2).ToArray();
            camera.Pan((Center-new Vector2(nodes[0].treeX+72.5f,nodes[0].treeY+119))*camera.Scale);
            view.Inspect(nodes[1].id);string command=null;view.Command+=key=>command=key;
            root.GetComponentsInChildren<CampMenuControl>().Single(c=>c.key=="track").Button.onClick.Invoke();
            Assert.That(command,Is.EqualTo("track"));Assert.That(view.InspectedItem,Is.EqualTo(nodes[1].id));Assert.That(view.TreeTrackingTarget,Is.EqualTo(nodes[0].id));
        }
    }
}
