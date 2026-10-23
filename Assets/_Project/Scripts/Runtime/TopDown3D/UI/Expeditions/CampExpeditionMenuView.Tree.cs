using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class CampExpeditionMenuView
    {
        internal CampTreeCanvasController TreeCanvas {get;private set;}
        private Func<bool> treeInputGate;
        internal void SetTreeInputGate(Func<bool> gate){treeInputGate=gate;}
        internal void SetReducedMotion(bool value){TreeCanvas?.SetReducedMotion(value);}
        internal bool TreeCameraOwnsFocus=>page=="tree"&&TreeCanvas!=null&&TreeCanvas.OwnsFocus;
        internal string TreeTrackingTarget=>page=="tree"&&TreeCanvas?.Target!=null?TreeCanvas.Target.key.Substring(10):InspectedItem;
        internal void FocusTreeAction(){if(TreeCameraOwnsFocus&&!quoting)events?.SetSelectedGameObject(TreeCanvas.gameObject);}
        private void ConfigureTreeCanvas()
        {
            var root=transform.Find("Safe Area/Screen Host/Page tree/Tree viewport") as RectTransform;if(root==null)return;
            var stage=root.Find("Recipe canvas") as RectTransform;var cards=controls.Values.Where(c=>c.key.StartsWith("tree-node:",StringComparison.Ordinal)).ToArray();
            foreach(var card in cards){card.Button.navigation=new Navigation{mode=Navigation.Mode.None};card.title.fontSize=14;}
            if(!controls.ContainsKey("tree-fit")&&controls.TryGetValue("tree-zoom-in",out var template))
            {var fit=Instantiate(template,template.transform.parent);fit.key=fit.name="tree-fit";fit.SetContent("Fit");fit.title.fontSize=14;((RectTransform)fit.transform).anchoredPosition+=Vector2.right*60;((RectTransform)fit.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,64);fit.Wire();fit.Activated+=Activate;fit.Browsed+=Browse;controls.Add(fit.key,fit);}
            var output=new GameObject("Tree zoom percentage",typeof(RectTransform));output.layer=root.gameObject.layer;output.transform.SetParent(root.parent,false);
            var rect=(RectTransform)output.transform;rect.anchorMin=rect.anchorMax=new Vector2(.68f,0);rect.pivot=new Vector2(.5f,0);rect.anchoredPosition=new Vector2(0,54);rect.sizeDelta=new Vector2(160,24);
            var label=output.AddComponent<Text>();label.font=style.ResolveFont();label.fontSize=14;label.color=style.ink;label.alignment=TextAnchor.MiddleCenter;label.raycastTarget=false;
            TreeCanvas=root.gameObject.AddComponent<CampTreeCanvasController>();TreeCanvas.ScaleChanged+=scale=>label.text=Mathf.RoundToInt(scale*100)+"%";
            TreeCanvas.Configure(stage,cards,style.accent,events,()=>page=="tree"&&!quoting&&GetComponent<CanvasGroup>().interactable&&(treeInputGate?.Invoke()??true),
                ()=>promptInput==null||promptInput.PromptDevice==TopDown3DPromptDevice.KeyboardMouse,Activate,NavigateTreePanels);
            ConfigureCatalogTree(stage);
        }
        private void NavigateTreePanels(Vector2 direction)
        {
            if(events==null)return;var origin=((RectTransform)TreeCanvas.transform).TransformPoint(((RectTransform)TreeCanvas.transform).rect.center);CampMenuControl nearest=null;float best=float.PositiveInfinity;
            foreach(var control in controls.Values.Where(c=>!c.prompt&&!c.tab&&!c.key.StartsWith("tree-node:",StringComparison.Ordinal)&&c.gameObject.activeInHierarchy&&c.Button.IsInteractable()))
            {var rect=(RectTransform)control.transform;var delta=(Vector2)(rect.TransformPoint(rect.rect.center)-origin);float forward=Vector2.Dot(delta,direction);if(forward<=0)continue;float sideways=Mathf.Abs(delta.x*direction.y-delta.y*direction.x);float score=forward+sideways*3;if(score<best){nearest=control;best=score;}}
            if(nearest!=null)events.SetSelectedGameObject(nearest.gameObject);
            else if(direction.x<0&&treeSearch!=null&&treeSearch.IsInteractable())events.SetSelectedGameObject(treeSearch.gameObject);
        }
        internal void TreeShortcut(string action)
        {if(!TreeCameraOwnsFocus)return;if(action=="ExpeditionTreeFit")TreeCanvas.Fit();else TreeCanvas.Zoom(action=="ExpeditionTreeZoomIn"?1.15f:1/1.15f);}
    }
}
