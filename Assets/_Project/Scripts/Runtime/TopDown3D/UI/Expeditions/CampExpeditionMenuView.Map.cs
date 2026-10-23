using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class CampExpeditionMenuView
    {
        private ScrollRect mapScroll,mapDetails;
        private RectTransform mapSheet;
        private CampMenuControl[] mapMarkers;
        private CampMenuControl mapZoomOut,mapZoomIn,mapWaypoint;
        private Text mapScale,mapDescription,mapWaypointStatus;
        private float mapZoom=1;
        private int mapSelection;
        private int? mapWaypointSelection;
        internal int? SchematicWaypoint=>mapWaypointSelection;
        private static readonly string[] MapLabels={"Booter","The Legger","Camp"};
        private static readonly Vector2[] MapPositions={new(.34f,.41f),new(.46f,.36f),new(.66f,.66f)};
        private static readonly string[] MapDescriptions={"Player marker. This position is illustrative.","Companion marker. This diagram does not establish actual range, routes or physical access.","Sample camp marker. Storage and workstation contents remain owned by their existing systems."};
        private void ConfigureSchematicMap()
        {
            var root=ResourceRoot("map","Know the way back.","A schematic map. These markers are illustrative, rather than live world positions.");
            var left=ResourcePanel(root,"Local area schematic",0,.71f);var right=ResourcePanel(root,"Map selected marker",.72f,1);
            var eyebrow=CatalogText(left,"LOCAL AREA / SCHEMATIC",14);TopRegion(eyebrow.rectTransform,16,32,16,230);
            mapZoomOut=ProjectButton(left,"map-zoom-out","−");mapZoomIn=ProjectButton(left,"map-zoom-in","+");mapScale=CatalogText(left,"100%",16);mapScale.alignment=TextAnchor.MiddleCenter;
            mapScroll=CreateMenuScroll(left,"Schematic viewport","map-canvas");mapScroll.viewport.offsetMin=new Vector2(16,16);mapScroll.viewport.offsetMax=new Vector2(-16,-66);mapScroll.horizontal=true;mapScroll.inertia=false;mapSheet=mapScroll.content;mapSheet.anchorMin=mapSheet.anchorMax=mapSheet.pivot=new Vector2(0,1);
            var drawing=new GameObject("Schematic contours",typeof(RectTransform),typeof(CampMenuMapGraphic)).GetComponent<CampMenuMapGraphic>();drawing.transform.SetParent(mapSheet,false);drawing.rectTransform.anchorMin=Vector2.zero;drawing.rectTransform.anchorMax=Vector2.one;drawing.rectTransform.offsetMin=drawing.rectTransform.offsetMax=Vector2.zero;drawing.color=new Color(style.ink.r,style.ink.g,style.ink.b,.22f);drawing.raycastTarget=false;
            var north=CatalogText(mapSheet,"N ↑",20);north.rectTransform.anchorMin=north.rectTransform.anchorMax=north.rectTransform.pivot=new Vector2(0,1);north.rectTransform.anchoredPosition=new Vector2(20,-18);north.rectTransform.sizeDelta=new Vector2(80,32);
            var note=CatalogText(mapSheet,"NO TERRAIN OR DISTANCE DATA",14);note.color=style.muted;note.rectTransform.anchorMin=note.rectTransform.anchorMax=note.rectTransform.pivot=Vector2.zero;note.rectTransform.anchoredPosition=new Vector2(20,18);note.rectTransform.sizeDelta=new Vector2(330,30);
            mapMarkers=new CampMenuControl[3];for(int i=0;i<3;i++){mapMarkers[i]=ProjectButton(mapSheet,"map-marker:"+i,MapLabels[i]);var rect=(RectTransform)mapMarkers[i].transform;rect.anchorMin=rect.anchorMax=MapPositions[i];rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=Vector2.zero;rect.sizeDelta=new Vector2(180,70);int index=i;LinkMenuDirection(rect.gameObject,data=>{if(data.moveDir==MoveDirection.Right)FocusResource(controls["map-details"]);else if(data.moveDir==MoveDirection.Left)FocusResource(mapZoomOut.Button.IsInteractable()?mapZoomOut:mapZoomIn);else if(data.moveDir==MoveDirection.Up)FocusResource(mapMarkers[index==2?0:2]);else if(data.moveDir==MoveDirection.Down)FocusResource(mapMarkers[index==1?0:1]);data.Use();});}
            mapDetails=CreateMenuScroll(right,"Map marker details","map-details");mapDetails.viewport.offsetMin=new Vector2(20,20);mapDetails.viewport.offsetMax=new Vector2(-20,-20);mapDescription=CatalogText(mapDetails.content,"",18);mapWaypoint=ProjectButton(mapDetails.content,"map-waypoint","Mark waypoint");mapWaypointStatus=CatalogText(mapDetails.content,"",16);
            LinkMenuDirection(mapDetails.gameObject,data=>{if(data.moveDir==MoveDirection.Left)FocusResource(mapMarkers[mapSelection]);else if(data.moveDir==MoveDirection.Down)FocusResource(mapWaypoint);else if(data.moveDir==MoveDirection.Up)ScrollInspector(1,.18f);data.Use();});LinkMenuDirection(mapWaypoint.gameObject,data=>{if(data.moveDir==MoveDirection.Left)FocusResource(mapMarkers[mapSelection]);else if(data.moveDir==MoveDirection.Up)FocusResource(controls["map-details"]);else if(data.moveDir==MoveDirection.Down)ScrollInspector(-1,.18f);data.Use();});
            foreach(var zoom in new[]{mapZoomOut,mapZoomIn})LinkMenuDirection(zoom.gameObject,data=>{if(data.moveDir==MoveDirection.Down)FocusResource(mapMarkers[mapSelection]);else if(data.moveDir==MoveDirection.Up)FocusResource(controls["resource-back:map"]);else if(data.moveDir==MoveDirection.Left)FocusResource(mapZoomOut);else if(data.moveDir==MoveDirection.Right)FocusResource(mapZoomIn);data.Use();});
            LinkMenuDirection(mapScroll.gameObject,data=>{FocusResource(mapMarkers[mapSelection]);data.Use();});RefreshSchematicMap();
        }
        private bool HandleMapAction(string key)
        {
            if(key is "map-zoom-in" or "map-zoom-out"){float previous=mapZoom;mapZoom=CampMenuResources.MapZoom(mapZoom,key=="map-zoom-in"?1:-1);if(previous!=mapZoom){var normalized=new Vector2(mapScroll.horizontalNormalizedPosition,mapScroll.verticalNormalizedPosition);ReflowSchematicMap();mapScroll.normalizedPosition=previous==1?new Vector2(.5f,.5f):normalized;}}
            else if(key.StartsWith("map-marker:",StringComparison.Ordinal)){int selected=int.Parse(key.Substring(11));if(selected!=mapSelection)mapDetails.content.anchoredPosition=Vector2.zero;mapSelection=selected;}
            else if(key=="map-waypoint")mapWaypointSelection=mapWaypointSelection==mapSelection?null:mapSelection;
            else return key is "map-canvas" or "map-details";
            RefreshSchematicMap();ReflowSchematicMap();EnsureUsableFocus();return true;
        }
        private void RefreshSchematicMap()
        {
            if(mapMarkers==null)return;for(int i=0;i<3;i++){mapMarkers[i].SetContent((i==0?"● ":i==1?"◇ ":"△ ")+MapLabels[i]+(mapWaypointSelection==i?"\nWAYPOINT":""));mapMarkers[i].SetSelected(i==mapSelection);}
            mapScale.text=Mathf.RoundToInt(mapZoom*100)+"%";SetProjectEnabled(mapZoomOut.Button,mapZoom>1);SetProjectEnabled(mapZoomIn.Button,mapZoom<2);
            mapDescription.text="SELECTED MARKER\n\n<b>"+MapLabels[mapSelection]+"</b>\n\n"+MapDescriptions[mapSelection];mapWaypoint.SetContent(mapWaypointSelection==mapSelection?"Clear waypoint":"Mark waypoint");mapWaypointStatus.text=mapWaypointSelection.HasValue?"Waypoint: "+MapLabels[mapWaypointSelection.Value]+"\n\nLocal visual marker only. It does not direct movement.":"Select a marker to place a local waypoint.";
        }
        private void ReflowSchematicMap()
        {
            if(mapScroll==null)return;float width=((RectTransform)mapScroll.transform.parent).rect.width;TopRegion((RectTransform)mapZoomOut.transform,12,42,width-220,176);TopRegion(mapScale.rectTransform,12,42,width-170,76);TopRegion((RectTransform)mapZoomIn.transform,12,42,width-60,16);mapSheet.sizeDelta=mapScroll.viewport.rect.size*mapZoom;
            foreach(var marker in mapMarkers){var rect=(RectTransform)marker.transform;rect.sizeDelta=new Vector2(Mathf.Clamp(marker.title.preferredWidth+24,80,200),Mathf.Max(62,marker.title.preferredHeight+16));rect.anchoredPosition=Vector2.zero;}
            // Keep the authored schematic anchors. Separate their labels only when a narrow pane would overlap them.
            var a=(RectTransform)mapMarkers[0].transform;var b=(RectTransform)mapMarkers[1].transform;float dx=(MapPositions[1].x-MapPositions[0].x)*mapSheet.rect.width,dy=(MapPositions[0].y-MapPositions[1].y)*mapSheet.rect.height;
            if(dx<(a.rect.width+b.rect.width)*.5f+8){float separate=Mathf.Max(0,(a.rect.height+b.rect.height)*.5f+10-dy)*.5f;a.anchoredPosition=new Vector2(0,separate);b.anchoredPosition=new Vector2(0,-separate);}
            float w=mapDetails.viewport.rect.width,y=0,h=ProjectTextHeight(mapDescription,w);TopRegion(mapDescription.rectTransform,0,h);y=h+20;h=Mathf.Max(54,ProjectTextHeight(mapWaypoint.title,w-20)+14);TopRegion((RectTransform)mapWaypoint.transform,y,h);y+=h+20;h=ProjectTextHeight(mapWaypointStatus,w);TopRegion(mapWaypointStatus.rectTransform,y,h);mapDetails.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,y+h);
        }
    }
}
