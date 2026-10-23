using UnityEngine;
using UnityEngine.UI;

namespace BooterBigArm.TopDown3D
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CampTreeCrosshairGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var r=rectTransform.rect;var center=r.center;
            void Strip(bool horizontal)
            {
                float[] fractions={0,.2f,.8f,1};float[] alpha={0,.24f,.24f,0};
                for(int n=0;n<3;n++)
                {
                    var a=horizontal?new Vector2(Mathf.Lerp(r.xMin,r.xMax,fractions[n]),center.y):new Vector2(center.x,Mathf.Lerp(r.yMin,r.yMax,fractions[n]));
                    var b=horizontal?new Vector2(Mathf.Lerp(r.xMin,r.xMax,fractions[n+1]),center.y):new Vector2(center.x,Mathf.Lerp(r.yMin,r.yMax,fractions[n+1]));
                    var normal=horizontal?new Vector2(0,.5f):new Vector2(.5f,0);var ca=color;ca.a*=alpha[n];var cb=color;cb.a*=alpha[n+1];int i=mesh.currentVertCount;
                    mesh.AddVert(a-normal,ca,Vector2.zero);mesh.AddVert(a+normal,ca,Vector2.zero);mesh.AddVert(b+normal,cb,Vector2.zero);mesh.AddVert(b-normal,cb,Vector2.zero);mesh.AddTriangle(i,i+1,i+2);mesh.AddTriangle(i,i+2,i+3);
                }
            }
            Strip(true);Strip(false);int start=mesh.currentVertCount;var dot=new Color32(255,244,214,255);mesh.AddVert(center,dot,Vector2.zero);
            for(int n=0;n<24;n++){float a=n*Mathf.PI/12;mesh.AddVert(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*2,dot,Vector2.zero);}
            for(int n=0;n<24;n++)mesh.AddTriangle(start,start+n+1,start+(n+1)%24+1);
        }
    }
}
