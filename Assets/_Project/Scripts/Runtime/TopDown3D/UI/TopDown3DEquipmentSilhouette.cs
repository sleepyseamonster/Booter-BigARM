using UnityEngine;
using UnityEngine.UI;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Original code-drawn equipment glyphs; empty slots retain their intended silhouette.</summary>
    [ExecuteAlways, RequireComponent(typeof(CanvasRenderer))]
    public sealed class TopDown3DEquipmentSilhouette : MaskableGraphic
    {
        private TopDown3DEquipmentSlot slot;
        public void Show(TopDown3DEquipmentSlot value, bool occupied)
        { slot = value; color = occupied ? new Color32(224, 210, 174, 255) : new Color32(112, 101, 83, 255); raycastTarget = false; SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            void Line(float x1, float y1, float x2, float y2)
            {
                var a = new Vector2(x1, y1) * 20; var b = new Vector2(x2, y2) * 20;
                var normal = (b - a).normalized; normal = new Vector2(-normal.y, normal.x) * 1.3f;
                var start = mesh.currentVertCount;
                mesh.AddVert(a - normal, color, Vector2.zero); mesh.AddVert(a + normal, color, Vector2.zero);
                mesh.AddVert(b + normal, color, Vector2.zero); mesh.AddVert(b - normal, color, Vector2.zero);
                mesh.AddTriangle(start, start + 1, start + 2); mesh.AddTriangle(start, start + 2, start + 3);
            }
            void Outline(params float[] points)
            { for (var i = 0; i + 3 < points.Length; i += 2) Line(points[i], points[i + 1], points[i + 2], points[i + 3]); }
            switch (slot)
            {
                case TopDown3DEquipmentSlot.DustCanister:
                case TopDown3DEquipmentSlot.AlgaeCanister:
                    Outline(-.5f, -.8f, .5f, -.8f, .5f, .5f, .3f, .7f, -.3f, .7f, -.5f, .5f, -.5f, -.8f);
                    Outline(-.25f, .7f, -.25f, .9f, .25f, .9f, .25f, .7f);
                    if (slot == TopDown3DEquipmentSlot.DustCanister)
                    { Line(-.25f, -.5f, -.12f, -.5f); Line(.12f, -.2f, .25f, -.2f); Line(-.22f, .1f, -.09f, .1f); }
                    else Outline(-.3f, -.45f, .3f, .35f, .35f, -.15f, -.1f, -.4f, -.3f, -.45f);
                    break;
                case TopDown3DEquipmentSlot.GrapplingHook:
                    Outline(0, -.9f, 0, .4f, -.5f, .85f, -.8f, .45f, -.4f, .45f);
                    Outline(0, .4f, .5f, .85f, .8f, .45f, .4f, .45f);
                    break;
                case TopDown3DEquipmentSlot.DiggingTool:
                    Line(-.65f, -.8f, .35f, .65f); Outline(-.25f, .5f, .3f, .9f, .9f, .55f, .35f, .3f, -.25f, .5f);
                    Outline(-.65f, -.8f, -.9f, -.65f, -.75f, -.35f);
                    break;
                case TopDown3DEquipmentSlot.Cloak:
                    Outline(-.2f, .4f, -.35f, .65f, 0, .95f, .35f, .65f, .2f, .4f);
                    Outline(-.2f, .4f, -.8f, -.8f, .8f, -.8f, .2f, .4f, -.2f, .4f);
                    Line(0, .35f, 0, -.6f);
                    break;
                case TopDown3DEquipmentSlot.Jacket:
                    Outline(-.25f, .7f, -.65f, .5f, -.9f, -.05f, -.6f, -.2f, -.4f, .1f, -.4f, -.8f, .4f, -.8f, .4f, .1f, .6f, -.2f, .9f, -.05f, .65f, .5f, .25f, .7f);
                    Outline(-.25f, .7f, 0, .4f, .25f, .7f); Line(0, .4f, 0, -.8f);
                    break;
            }
        }
    }
}
