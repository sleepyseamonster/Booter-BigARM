using UnityEngine;
using UnityEngine.UI;

namespace BooterBigArm.TopDown3D
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CampMenuMapGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var bounds=rectTransform.rect;
            Vector2 Point(Vector2 p)=>new(bounds.xMin+p.x/1000*bounds.width,bounds.yMax-p.y/650*bounds.height);
            for(int i=0;i<11;i++)
            {
                float offset=i*30;Curve(new(-40,170+offset),new(190,90+offset),new(330,220+offset));Curve(new(330,220+offset),new(470,350+offset),new(650,270+offset));Curve(new(650,270+offset),new(830,190+offset),new(1040,80+offset));
            }
            void Curve(Vector2 a,Vector2 b,Vector2 c)
            {
                var previous=Point(a);for(int step=1;step<=36;step++){float t=step/36f;var next=Point((1-t)*(1-t)*a+2*(1-t)*t*b+t*t*c);var normal=new Vector2(-(next-previous).y,(next-previous).x).normalized*.65f;int start=mesh.currentVertCount;foreach(var point in new[]{previous-normal,previous+normal,next+normal,next-normal})mesh.AddVert(point,color,Vector2.zero);mesh.AddTriangle(start,start+1,start+2);mesh.AddTriangle(start,start+2,start+3);previous=next;}
            }
        }
    }
}
