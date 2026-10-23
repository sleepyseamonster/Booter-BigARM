using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace BooterBigArm.TopDown3D
{
    /// <summary>One camera authority for the crafting viewport; replaces ScrollRect's transform ownership.</summary>
    public sealed class CampTreeCanvasController : MonoBehaviour,IPointerDownHandler,IBeginDragHandler,IDragHandler,IEndDragHandler,IScrollHandler,IMoveHandler,ISubmitHandler
    {
        private RectTransform viewport,stage,highlight;
        private CampMenuControl[] nodes;
        private CampMenuTreeGraphic wires;
        private CanvasGroup crosshair;
        private Vector2 offset,lastPointer;
        private Rect bounds;
        private float scale=1,idle;
        private bool fitted,dragging,userMoved;
        private bool reducedMotion;
        private Vector2 lastSize;
        private float reportedScale=-1;
        internal event Action<float> ScaleChanged;
        private Func<bool> allowed,keyboard;
        private Action<CampMenuControl> select;
        private Action<Vector2> navigate;
        private EventSystem events;
        internal bool OwnsFocus {get;private set;}
        internal float Scale=>scale;
        internal Vector2 Offset=>offset;
        internal float CrosshairAlpha=>crosshair==null?0:crosshair.alpha;
        internal CampMenuControl Target {get;private set;}
        internal bool CanInteract=>allowed?.Invoke()==true&&isActiveAndEnabled;
        internal CampMenuControl[] Nodes=>nodes;
        internal void PrepareNodeReplacement()
        {if(highlight!=null){highlight.SetParent(stage,false);highlight.gameObject.SetActive(false);}Target=null;ReleaseFocus();}
        internal void ReplaceNodes(CampMenuControl[] cards)
        {
            if(cards==null||cards.Length==0)throw new InvalidOperationException("A displayed crafting tree requires cards.");
            PrepareNodeReplacement();nodes=cards;
            CalculateBounds();fitted=userMoved=dragging=OwnsFocus=false;idle=0;if(crosshair!=null)crosshair.alpha=0;Fit(false);
        }
        private void CalculateBounds()
        {
            var rects=nodes.Select(c=>(RectTransform)c.transform).ToArray();
            float left=rects.Min(r=>r.anchoredPosition.x),top=rects.Min(r=>-r.anchoredPosition.y),right=rects.Max(r=>r.anchoredPosition.x+r.rect.width),bottom=rects.Max(r=>-r.anchoredPosition.y+r.rect.height);
            bounds=Rect.MinMaxRect(left-120,top-120,right+120,bottom+120);
        }

        internal void Configure(RectTransform content,CampMenuControl[] cards,Color accent,EventSystem system,Func<bool> gate,Func<bool> isKeyboard,Action<CampMenuControl> selected,Action<Vector2> move)
        {
            viewport=(RectTransform)transform;stage=content;nodes=cards;events=system;allowed=gate;keyboard=isKeyboard;select=selected;navigate=move;
            var scroll=GetComponent<ScrollRect>();if(scroll!=null){scroll.StopMovement();scroll.enabled=false;}
            stage.anchorMin=stage.anchorMax=stage.pivot=new Vector2(0,1);wires=stage.GetComponentInChildren<CampMenuTreeGraphic>();
            var rects=cards.Select(c=>(RectTransform)c.transform).ToArray();
            if(rects.Length==0)throw new InvalidOperationException("The crafting camera requires its authored recipe cards.");
            CalculateBounds();
            var root=new GameObject("Camera crosshair",typeof(RectTransform),typeof(CanvasGroup));root.layer=gameObject.layer;root.transform.SetParent(transform,false);
            var rect=(RectTransform)root.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var graphic=root.AddComponent<CampTreeCrosshairGraphic>();graphic.color=accent;graphic.raycastTarget=false;
            crosshair=root.GetComponent<CanvasGroup>();crosshair.alpha=0;crosshair.interactable=false;crosshair.blocksRaycasts=false;
            var marked=new GameObject("Crosshair target",typeof(RectTransform));marked.layer=gameObject.layer;highlight=(RectTransform)marked.transform;highlight.SetParent(stage,false);
            var frame=marked.AddComponent<CampMenuPanelGraphic>();frame.SetFrame(Color.clear,accent,2);frame.raycastTarget=false;marked.SetActive(false);
        }
        internal void RefreshCamera()
        {if(stage==null||!isActiveAndEnabled||viewport.rect.width<=0||viewport.rect.height<=0)return;if(!fitted||!userMoved&&lastSize!=viewport.rect.size)Fit(false);else Paint(false);lastSize=viewport.rect.size;}
        internal void Fit(bool activity=true)
        {
            if(activity&&!CanInteract||stage==null)return;var size=viewport.rect.size;scale=CampTreeCameraMath.Fit(size,stage.rect.size);offset=(size-stage.rect.size*scale)*.5f;fitted=true;Paint(activity);
        }
        internal void Zoom(float factor)
        {if(!CanInteract)return;float next=CampTreeCameraMath.Zoom(scale,factor);offset=CampTreeCameraMath.ZoomOffset(offset,scale,next,viewport.rect.size*.5f);scale=next;Paint(true);}
        internal void Pan(Vector2 screenDelta)
        {if(!CanInteract)return;offset+=screenDelta;Paint(true);}
        internal void MoveSticks(Vector2 pan,float zoom,float seconds)
        {
            if(!CanInteract||pan.sqrMagnitude<.000001f&&Mathf.Abs(zoom)<.000001f)return;
            float dt=Mathf.Clamp(seconds,0,.05f);offset+=new Vector2(-pan.x,pan.y)*850*dt;
            float next=CampTreeCameraMath.Zoom(scale,Mathf.Exp(zoom*1.8f*dt));offset=CampTreeCameraMath.ZoomOffset(offset,scale,next,viewport.rect.size*.5f);scale=next;Paint(true);
        }
        private void Paint(bool activity)
        {
            var before=stage.anchoredPosition;float previousScale=stage.localScale.x;
            offset=CampTreeCameraMath.Clamp(offset,scale,bounds,viewport.rect.size*.5f);stage.anchoredPosition=new Vector2(offset.x,-offset.y);stage.localScale=Vector3.one*scale;
            if(wires!=null){var c=wires.color;c.a=CampTreeCameraMath.Connections(scale);wires.color=c;}
            if(!Mathf.Approximately(reportedScale,scale)){reportedScale=scale;ScaleChanged?.Invoke(scale);}
            var point=(viewport.rect.size*.5f-offset)/scale;
            var target=nodes.FirstOrDefault(c=>{var r=(RectTransform)c.transform;return point.x>=r.anchoredPosition.x&&point.x<=r.anchoredPosition.x+r.rect.width&&point.y>=-r.anchoredPosition.y&&point.y<=-r.anchoredPosition.y+r.rect.height;});
            if(target!=Target)
            {
                Target=target;highlight.gameObject.SetActive(target!=null);
                if(target!=null){highlight.SetParent(target.transform,false);highlight.anchorMin=Vector2.zero;highlight.anchorMax=Vector2.one;highlight.offsetMin=highlight.offsetMax=Vector2.zero;}
            }
            if(activity){userMoved=true;OwnsFocus=true;if(events!=null)events.SetSelectedGameObject(gameObject);if((before-stage.anchoredPosition).sqrMagnitude>.0000001f||Mathf.Abs(previousScale-scale)>.0000001f)idle=2.5f;}
        }
        internal void AdvanceVisual(float seconds)
        {if(crosshair==null)return;idle=Mathf.Max(0,idle-seconds);crosshair.alpha=reducedMotion?(idle>0?1:0):Mathf.MoveTowards(crosshair.alpha,idle>0?1:0,seconds/(idle>0?.09f:.3f));}
        internal void SetReducedMotion(bool value){reducedMotion=value;if(value&&crosshair!=null)crosshair.alpha=idle>0?1:0;}
        private void LateUpdate(){RefreshCamera();if(!CanInteract){dragging=false;OwnsFocus=false;}AdvanceVisual(Time.unscaledDeltaTime);}
        private void OnDisable(){dragging=false;OwnsFocus=false;idle=0;if(crosshair!=null)crosshair.alpha=0;}
        internal void ReleaseFocus(){OwnsFocus=false;dragging=false;}
        internal void ClaimFocus(){if(!CanInteract)return;OwnsFocus=true;events?.SetSelectedGameObject(gameObject);}
        internal void SetEventSystem(EventSystem system){events=system;}
        public void OnPointerDown(PointerEventData data)
        {if(CanInteract&&(data.button==PointerEventData.InputButton.Left||data.button==PointerEventData.InputButton.Middle)&&(data.button==PointerEventData.InputButton.Middle||data.pointerPressRaycast.gameObject?.GetComponentInParent<CampMenuControl>()==null)){OwnsFocus=true;events?.SetSelectedGameObject(gameObject);}}
        public void OnBeginDrag(PointerEventData data)
        {if(!CanInteract||data.button!=PointerEventData.InputButton.Left&&data.button!=PointerEventData.InputButton.Middle)return;if(data.button==PointerEventData.InputButton.Left&&data.pointerPressRaycast.gameObject?.GetComponentInParent<CampMenuControl>()!=null)return;dragging=RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport,data.position,data.pressEventCamera,out lastPointer);}
        public void OnDrag(PointerEventData data)
        {if(!CanInteract){dragging=false;return;}if(!dragging)return;if(RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport,data.position,data.pressEventCamera,out var point)){var delta=point-lastPointer;lastPointer=point;Pan(new Vector2(delta.x,-delta.y));}}
        public void OnEndDrag(PointerEventData data){dragging=false;}
        public void OnScroll(PointerEventData data)
        {if(!CanInteract)return;float tick=events?.currentInputModule is InputSystemUIInputModule module?Mathf.Abs(module.scrollDeltaPerTick):1;if(tick<.00001f)return;Zoom(Mathf.Exp(Mathf.Clamp(data.scrollDelta.y/tick*100,-100,100)*.002f));data.Use();}
        public void OnMove(AxisEventData data)
        {if(!CanInteract)return;if(keyboard?.Invoke()==true)Pan(new Vector2(-data.moveVector.x,data.moveVector.y)*60);else{ReleaseFocus();navigate?.Invoke(data.moveVector);}data.Use();}
        public void OnSubmit(BaseEventData data){if(CanInteract&&Target!=null)select?.Invoke(Target);data.Use();}
    }
}
