using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Native equivalents of the browser's non-bitmap item icons (96-unit artwork space).</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CampMenuItemGlyphGraphic : MaskableGraphic
    {
        private string itemId;
        private VertexHelper mesh;
        private Vector2 origin;
        private float scale;
        private readonly List<Vector2> path=new();
        internal void SetItem(string id,Color ink){if(itemId==id&&color==ink)return;itemId=id;color=ink;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(string.IsNullOrEmpty(itemId))return;mesh=vh;var rect=rectTransform.rect;scale=Mathf.Min(rect.width,rect.height)/96;origin=rect.center-new Vector2(48,48)*scale;
            switch(itemId)
            {
                case "station-mark.workbench":
                    Polyline(19,76,70,25,77,32,26,83,19,76);Polyline(51,23,61,13,83,35,73,45,51,23);Polyline(17,20,29,13,43,27,36,34,79,77,72,84,29,41,22,48,8,34,15,22);break;
                case "station-mark.dust":
                    Polyline(42,11,61,11,47,41,70,41,37,85,46,54,25,54,42,11);break;
                case "station-mark.cooking":
                    Circle(48,48,33);Circle(48,48,24);break;
                case "station-mark.generic":
                    Polygon(false,48,17,79,48,48,79,17,48);break;
                case "tool.micro_dust_harvester":
                    RoundedRect(31,21,34,58,8);Polyline(38,21,38,14,58,14,58,21);Polyline(31,34,65,34);Polyline(31,67,65,67);Polygon(false,43,40,53,40,53,61,43,61);Polyline(26,37,26,61);Polyline(70,37,70,61);break;
                case "equipment.arc_battery_empty":
                    Polygon(true,29,25,38,14,58,14,67,25,67,80,29,80);Polyline(29,32,67,32);Polyline(38,14,38,25,58,25,58,14);Polyline(38,42,38,70);Polyline(48,42,48,70);Polyline(58,42,58,70);Polyline(29,75,67,75);break;
                case "tool.popup_camp":
                    Polygon(true,10,75,41,21,55,21,86,75);Polyline(41,21,61,75);Polyline(21,75,48,39,61,75);Polyline(8,80,88,80);Polyline(53,19,58,12);break;
                case "resource.airborne_dust":
                    BeginPath(16,64);Quadratic(28,51,38,64);Quadratic(48,77,60,64);Quadratic(72,51,80,64);Stroke();Polyline(20,74,78,74);Polyline(29,36,30,36);Polyline(50,22,51,22);Polyline(66,39,67,39);Polyline(39,48,40,48);Polyline(58,51,59,51);Circle(28,35,3);Circle(50,22,2);Circle(67,39,3);break;
                case "equipment.grappling_hook":
                    BeginPath(44,53);path.Add(new Vector2(44,24));Quadratic(44,12,56,12);Quadratic(68,12,66,24);Quadratic(66,31,59,34);Stroke();Polyline(44,34,30,47);Polyline(44,34,58,47);Polyline(28,40,30,47,38,46);Polyline(60,40,58,47,50,46);
                    BeginPath(44,53);Quadratic(27,54,27,67);Quadratic(27,80,48,82);Quadratic(71,82,71,67);Quadratic(70,58,60,58);Quadratic(51,58,51,65);Quadratic(51,71,59,71);Stroke();break;
                default:
                    Polygon(true,24,31,53,17,78,36,71,75,37,83,17,59);Polyline(24,31,47,51,78,36);Polyline(47,51,37,83);Polyline(47,51,71,75);break;
            }
        }
        private Vector2 Point(Vector2 p)=>origin+new Vector2(p.x,96-p.y)*scale;
        private void BeginPath(float x,float y){path.Clear();path.Add(new Vector2(x,y));}
        private void Quadratic(float cx,float cy,float x,float y)
        {var start=path[path.Count-1];var control=new Vector2(cx,cy);var end=new Vector2(x,y);for(int i=1;i<=8;i++){float t=i/8f,u=1-t;path.Add(u*u*start+2*u*t*control+t*t*end);}}
        private void Stroke(){for(int i=1;i<path.Count;i++)Line(path[i-1],path[i]);}
        private void Polyline(params float[] values){for(int i=2;i<values.Length;i+=2)Line(new Vector2(values[i-2],values[i-1]),new Vector2(values[i],values[i+1]));}
        private void Polygon(bool fill,params float[] values)
        {
            if(fill){var tint=color;tint.a*=.14f;int start=mesh.currentVertCount;for(int i=0;i<values.Length;i+=2)mesh.AddVert(Point(new Vector2(values[i],values[i+1])),tint,Vector2.zero);for(int i=1;i<values.Length/2-1;i++)mesh.AddTriangle(start,start+i,start+i+1);}
            Polyline(values);Line(new Vector2(values[values.Length-2],values[values.Length-1]),new Vector2(values[0],values[1]));
        }
        private void Circle(float x,float y,float radius)
        {var previous=new Vector2(x+radius,y);for(int i=1;i<=24;i++){float angle=i*Mathf.PI/12;var next=new Vector2(x+Mathf.Cos(angle)*radius,y+Mathf.Sin(angle)*radius);Line(previous,next);previous=next;}}
        private void RoundedRect(float x,float y,float width,float height,float radius)
        {
            BeginPath(x+radius,y);path.Add(new Vector2(x+width-radius,y));Quadratic(x+width,y,x+width,y+radius);path.Add(new Vector2(x+width,y+height-radius));Quadratic(x+width,y+height,x+width-radius,y+height);path.Add(new Vector2(x+radius,y+height));Quadratic(x,y+height,x,y+height-radius);path.Add(new Vector2(x,y+radius));Quadratic(x,y,x+radius,y);Stroke();
        }
        private void Line(Vector2 from,Vector2 to)
        {
            var a=Point(from);var b=Point(to);var n=new Vector2(-(b-a).y,(b-a).x).normalized*(1.05f*scale);int i=mesh.currentVertCount;
            mesh.AddVert(a-n,color,Vector2.zero);mesh.AddVert(a+n,color,Vector2.zero);mesh.AddVert(b+n,color,Vector2.zero);mesh.AddVert(b-n,color,Vector2.zero);mesh.AddTriangle(i,i+1,i+2);mesh.AddTriangle(i,i+2,i+3);
        }
    }
}
