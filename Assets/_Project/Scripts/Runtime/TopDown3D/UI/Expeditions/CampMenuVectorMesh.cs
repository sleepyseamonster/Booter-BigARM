using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Joined canvas contours with one screen pixel of alpha coverage at their edges.</summary>
    internal static class CampMenuVectorMesh
    {
        internal static float PixelWidth(Graphic graphic)
        {
            var canvas=graphic.canvas;
            if(canvas==null)return 1;
            var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
            var transform=graphic.rectTransform;
            var origin=RectTransformUtility.WorldToScreenPoint(camera,transform.TransformPoint(Vector3.zero));
            float x=(RectTransformUtility.WorldToScreenPoint(camera,transform.TransformPoint(Vector3.right))-origin).magnitude;
            float y=(RectTransformUtility.WorldToScreenPoint(camera,transform.TransformPoint(Vector3.up))-origin).magnitude;
            return 1/Mathf.Max(.001f,Mathf.Min(x,y));
        }

        internal static void Line(VertexHelper mesh,Vector2 a,Vector2 b,float width,Color ink,float feather)
        {
            if(width<=0||(b-a).sqrMagnitude<.000001f)return;
            var normal=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;
            Shape(mesh,new[]{a-normal,b-normal,b+normal,a+normal},ink,Color.clear,0,feather);
        }

        // The menu's convex outlines and star-shaped D-pad support a centre fan.
        // Fill, stroke and feather share contours, avoiding overlapping translucent line segments.
        internal static void Shape(VertexHelper mesh,IList<Vector2> source,Color fill,Color stroke,float width,float feather,bool insetStroke=false)
        {
            if(fill.a<=0&&(stroke.a<=0||width<=0))return;
            var points=new List<Vector2>(source.Count);
            foreach(var point in source)
                if(points.Count==0||(point-points[points.Count-1]).sqrMagnitude>.000001f)points.Add(point);
            if(points.Count>1&&(points[0]-points[points.Count-1]).sqrMagnitude<.000001f)points.RemoveAt(points.Count-1);
            int count=points.Count;if(count<3)return;
            float area=0;Vector2 center=Vector2.zero;
            for(int i=0;i<count;i++){var a=points[i];var b=points[(i+1)%count];area+=a.x*b.y-b.x*a.y;center+=a;}
            if(Mathf.Abs(area)<.000001f)return;
            center/=count;float winding=Mathf.Sign(area);
            var offsets=new Vector2[count];
            Vector2 Normal(Vector2 edge)=>new Vector2(edge.y,-edge.x).normalized*winding;
            for(int i=0;i<count;i++)
            {
                var previous=Normal(points[i]-points[(i+count-1)%count]);
                var next=Normal(points[(i+1)%count]-points[i]);var sum=previous+next;
                float denominator=Vector2.Dot(sum,next);
                offsets[i]=Vector2.ClampMagnitude(denominator>.0001f?sum/denominator:next,4);
            }
            int Contour(float offset,Color ink)
            {
                int start=mesh.currentVertCount;
                for(int i=0;i<count;i++)mesh.AddVert(points[i]+offsets[i]*offset,ink,Vector2.zero);
                return start;
            }
            void Ring(int inner,int outer)
            {
                for(int i=0;i<count;i++)
                {int next=(i+1)%count;mesh.AddTriangle(inner+i,outer+i,outer+next);mesh.AddTriangle(inner+i,outer+next,inner+next);}
            }
            void Fill(int contour)
            {
                if(fill.a<=0)return;
                int middle=mesh.currentVertCount;mesh.AddVert(center,fill,Vector2.zero);
                for(int i=0;i<count;i++)mesh.AddTriangle(middle,contour+i,contour+(i+1)%count);
            }
            feather=Mathf.Max(.001f,feather);
            if(width<=0||stroke.a<=0)
            {
                int inside=Contour(-feather*.5f,fill);Fill(inside);
                var transparent=fill;transparent.a=0;
                Ring(inside,Contour(feather*.5f,transparent));return;
            }
            float origin=insetStroke?-width*.5f:0;
            float edge=(width+feather)*.5f,core=Mathf.Max(0,(width-feather)*.5f);
            // Subpixel strokes retain their area instead of becoming an opaque one-pixel line.
            stroke.a*=Mathf.Min(1,2*width/(width+feather));
            int inner=Contour(origin-edge,fill);Fill(inner);
            int innerStroke=Contour(origin-core,stroke);Ring(inner,innerStroke);
            int outerStroke=Contour(origin+core,stroke);Ring(innerStroke,outerStroke);
            var clear=stroke;clear.a=0;Ring(outerStroke,Contour(origin+edge,clear));
        }
    }
}
