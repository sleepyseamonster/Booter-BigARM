using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Native vector versions of the browser menu's SVG prompts. No input ownership.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CampMenuBindingGlyph : MaskableGraphic
    {
        public enum Shape { Key,LeftBumper,RightBumper,LeftTrigger,RightTrigger,North,South,East,West,Menu,View,Stick,Dpad }
        public Shape Kind {get;private set;}
        public bool ShowsLabel=>Kind is Shape.Key or Shape.LeftBumper or Shape.RightBumper or Shape.LeftTrigger or Shape.RightTrigger or Shape.Stick;
        public void Configure(Shape shape,Color ink){Kind=shape;color=ink;raycastTarget=false;SetVerticesDirty();}

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var points=new List<Vector2>();float feather=CampMenuVectorMesh.PixelWidth(this);
            bool bumper=Kind is Shape.LeftBumper or Shape.RightBumper;
            bool trigger=Kind is Shape.LeftTrigger or Shape.RightTrigger;
            Vector2 source=bumper?new Vector2(64,32):trigger?new Vector2(40,40):new Vector2(32,32);
            var rect=rectTransform.rect;
            if(Kind==Shape.Key)source=new Vector2(Mathf.Max(32,rect.width),32);
            float scale=Mathf.Min(rect.width/source.x,rect.height/source.y);
            Vector2 origin=rect.center-source*scale*.5f;
            bool mirror=Kind is Shape.RightBumper or Shape.RightTrigger;
            Vector2 Map(Vector2 p)=>origin+new Vector2(mirror?source.x-p.x:p.x,source.y-p.y)*scale;
            void Line(Vector2 a,Vector2 b,float width,Color ink)
            {
                CampMenuVectorMesh.Line(mesh,Map(a),Map(b),width*scale,ink,feather);
            }
            void Polygon(List<Vector2> path,float fill,float stroke=1.8f)
            {
                var mapped=new List<Vector2>(path.Count);foreach(var point in path)mapped.Add(Map(point));
                var ink=color;ink.a*=fill;
                CampMenuVectorMesh.Shape(mesh,mapped,ink,color,stroke*scale,feather);
            }
            void Circle(Vector2 center,float radius,bool pressed)
            {var path=new List<Vector2>();for(int i=0;i<32;i++){float a=i*Mathf.PI/16;path.Add(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius);}Polygon(path,pressed?1:0,1.5f);}
            void Curve(Vector2 a,Vector2 b,Vector2 c,Vector2 d)
            {for(int i=1;i<=12;i++){float t=i/12f,u=1-t;points.Add(u*u*u*a+3*u*u*t*b+3*u*t*t*c+t*t*t*d);}}
            void Quad(Vector2 a,Vector2 b,Vector2 c)
            {for(int i=1;i<=10;i++){float t=i/10f,u=1-t;points.Add(u*u*a+2*u*t*b+t*t*c);}}

            if(bumper)
            {
                points.Add(new Vector2(6,26));points.Add(new Vector2(6,16));Curve(new Vector2(6,16),new Vector2(6,11),new Vector2(10,7),new Vector2(15,6));
                points.Add(new Vector2(48,3));Curve(new Vector2(48,3),new Vector2(54,3),new Vector2(58,8),new Vector2(58,14));points.Add(new Vector2(58,26));Polygon(points,.06f);return;
            }
            if(trigger)
            {
                points.Add(new Vector2(10,4));points.Add(new Vector2(30,4));Quad(new Vector2(30,4),new Vector2(35,4),new Vector2(35,10));points.Add(new Vector2(35,23));
                Quad(new Vector2(35,23),new Vector2(35,27),new Vector2(31,29));points.Add(new Vector2(21,36));Quad(new Vector2(21,36),new Vector2(17,38),new Vector2(14,33));
                points.Add(new Vector2(5,13));Quad(new Vector2(5,13),new Vector2(2,4),new Vector2(10,4));Polygon(points,.06f);return;
            }
            if(Kind is Shape.North or Shape.South or Shape.East or Shape.West)
            {Circle(new Vector2(16,6),4,Kind==Shape.North);Circle(new Vector2(26,16),4,Kind==Shape.East);Circle(new Vector2(6,16),4,Kind==Shape.West);Circle(new Vector2(16,26),4,Kind==Shape.South);return;}
            if(Kind==Shape.Menu){for(int y=8;y<=24;y+=8)Line(new Vector2(5,y),new Vector2(27,y),2,color);return;}
            if(Kind==Shape.View)
            {foreach(int p in new[]{5,12})Polygon(new List<Vector2>{new(p,p),new(p+15,p),new(p+15,p+15),new(p,p+15)},0,1.5f);return;}
            if(Kind==Shape.Stick){Circle(new Vector2(16,16),13,false);return;}
            if(Kind==Shape.Dpad)
            {Polygon(new List<Vector2>{new(12,3),new(20,3),new(20,12),new(29,12),new(29,20),new(20,20),new(20,29),new(12,29),new(12,20),new(3,20),new(3,12),new(12,12)},1,0);return;}
            // Keyboard/mouse alternatives retain their actual labels inside rounded keycaps.
            float w=source.x;
            points.Add(new Vector2(6,3));points.Add(new Vector2(w-6,3));Quad(new Vector2(w-6,3),new Vector2(w-2,3),new Vector2(w-2,7));points.Add(new Vector2(w-2,25));
            Quad(new Vector2(w-2,25),new Vector2(w-2,29),new Vector2(w-6,29));points.Add(new Vector2(6,29));Quad(new Vector2(6,29),new Vector2(2,29),new Vector2(2,25));
            points.Add(new Vector2(2,7));Quad(new Vector2(2,7),new Vector2(2,3),new Vector2(6,3));Polygon(points,0,1.5f);
        }
    }
}
