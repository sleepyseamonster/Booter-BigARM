using UnityEngine;
using UnityEngine.UI;

namespace BooterBigArm.TopDown3D
{
    // Annular strips only: no center-fan triangles and no tapered angular cutouts.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TopDown3DRadialRingGraphic : MaskableGraphic
    {
        public int Sectors = 4;
        public int Selected = -1;
        public float Gap = 6f;
        public float InnerRadius = 68f;
        public Color Accent = new Color32(232, 184, 109, 255);

        public TopDown3DRadialRingGraphic() { useLegacyMeshGeneration = false; }

        public override Texture mainTexture => Texture2D.whiteTexture;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var outer = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * 0.5f;
            if (outer <= InnerRadius || Sectors < 1) return;
            for (var s = 0; s < Sectors; s++)
            {
                var c = s == Selected ? Color.Lerp(color, Accent, 0.16f) : color;
                AddStrip(vh, InnerRadius, outer, s, c);
                if (s == Selected) AddStrip(vh, outer - 3f, outer, s, Accent);
            }
        }

        private void AddStrip(VertexHelper vh, float inner, float outer, int sector, Color tint)
        {
            const int steps = 32;
            var width = 360f / Sectors;
            var start = sector * width - width * 0.5f;
            var end = start + width;
            var trimInner = inner > 0f ? Mathf.Asin(Mathf.Clamp(Gap * 0.5f / inner, 0f, 0.99f)) * Mathf.Rad2Deg : 0f;
            var trimOuter = Mathf.Asin(Mathf.Clamp(Gap * 0.5f / outer, 0f, 0.99f)) * Mathf.Rad2Deg;
            var quad = new UIVertex[4];
            for (var i = 0; i < steps; i++)
            {
                var a = (float)i / steps;
                var b = (float)(i + 1) / steps;
                var positions = new[] {
                    TopDown3DRadialGeometry.Point(inner, Mathf.Lerp(start + trimInner, end - trimInner, a)),
                    TopDown3DRadialGeometry.Point(outer, Mathf.Lerp(start + trimOuter, end - trimOuter, a)),
                    TopDown3DRadialGeometry.Point(outer, Mathf.Lerp(start + trimOuter, end - trimOuter, b)),
                    TopDown3DRadialGeometry.Point(inner, Mathf.Lerp(start + trimInner, end - trimInner, b)) };
                for (var j = 0; j < 4; j++) { quad[j] = UIVertex.simpleVert; quad[j].position = positions[j]; quad[j].color = tint; }
                vh.AddUIVertexQuad(quad);
            }
        }
    }
}
