using UnityEngine;
using UnityEngine.UI;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Native disclosure and tracking marks, independent of the menu font's glyph coverage.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CampMenuDisclosureGraphic : MaskableGraphic
    {
        private bool open,checkMark;
        internal void SetOpen(bool value){if(open==value)return;open=value;SetVerticesDirty();}
        internal void SetCheckMark(bool value){checkMark=value;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var center=rectTransform.rect.center;
            var a=center+(checkMark?new Vector2(-5,0):open?new Vector2(-4,2):new Vector2(-2,4));var b=center+(checkMark?new Vector2(-1,-4):open?new Vector2(0,-2):new Vector2(2,0));var c=center+(checkMark?new Vector2(6,5):open?new Vector2(4,2):new Vector2(-2,-4));
            Line(vh,a,b);Line(vh,b,c);
        }
        private void Line(VertexHelper vh,Vector2 a,Vector2 b)
        {var n=new Vector2(-(b-a).y,(b-a).x).normalized*.75f;int i=vh.currentVertCount;vh.AddVert(a-n,color,Vector2.zero);vh.AddVert(a+n,color,Vector2.zero);vh.AddVert(b+n,color,Vector2.zero);vh.AddVert(b-n,color,Vector2.zero);vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);}
    }
}
