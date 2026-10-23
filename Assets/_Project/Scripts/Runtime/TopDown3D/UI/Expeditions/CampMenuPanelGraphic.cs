using UnityEngine;
using UnityEngine.UI;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Native canvas frame with the browser's thin etched, clipped corners.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CampMenuPanelGraphic : MaskableGraphic
    {
        public Color border = new Color32(65, 69, 62, 255);
        public float corner = 0, thickness = 1;
        public bool underlineOnly;

        public void SetFrame(Color fill, Color edge, float width = 1)
        { color = fill; border = edge; thickness = width; SetVerticesDirty(); }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear(); var r = rectTransform.rect;
            var c = Mathf.Clamp(corner, 0, Mathf.Min(r.width, r.height) * .25f);
            var points = new[] { new Vector2(r.xMin + c, r.yMin), new Vector2(r.xMax - c, r.yMin),
                new Vector2(r.xMax, r.yMin + c), new Vector2(r.xMax, r.yMax - c),
                new Vector2(r.xMax - c, r.yMax), new Vector2(r.xMin + c, r.yMax),
                new Vector2(r.xMin, r.yMax - c), new Vector2(r.xMin, r.yMin + c) };
            float feather=CampMenuVectorMesh.PixelWidth(this);
            if(underlineOnly)
            {
                CampMenuVectorMesh.Shape(mesh,points,color,Color.clear,0,feather);
                CampMenuVectorMesh.Line(mesh,new Vector2(r.xMin,r.yMin+thickness*.5f),new Vector2(r.xMax,r.yMin+thickness*.5f),thickness,border,feather);return;
            }
            CampMenuVectorMesh.Shape(mesh,points,color,border,thickness,feather,true);
        }
    }
}
