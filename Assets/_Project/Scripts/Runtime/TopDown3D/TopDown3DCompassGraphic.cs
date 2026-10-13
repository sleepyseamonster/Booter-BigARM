using UnityEngine;
using UnityEngine.UI;

namespace BooterBigArm.TopDown3D
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TopDown3DCompassGraphic : MaskableGraphic
    {
        private float heading, bearing;
        private bool hasHeading, hasDestination, arrived;
        private static readonly Color32 Rim = new(214, 203, 169, 255);
        private static readonly Color32 Dark = new(24, 29, 31, 255);
        private static readonly Color32 Sun = new(242, 225, 167, 255);
        private static readonly Color32 Arrow = new(241, 186, 62, 255);
        private static readonly Color32 Track = new(161, 151, 122, 185);

        public float Heading => heading;
        public float CampBearing => bearing;
        public bool HasDestination => hasDestination;
        public bool Arrived => arrived;

        public void SetState(float facing, bool validHeading, float campBearing, bool validDestination, bool atCamp)
        {
            if (Mathf.Approximately(heading, facing) && hasHeading == validHeading
                && Mathf.Approximately(bearing, campBearing) && hasDestination == validDestination && arrived == atCamp) return;
            heading = facing; hasHeading = validHeading; bearing = campBearing; hasDestination = validDestination; arrived = atCamp;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var reference = TopDown3DActionDpadHud.ReferenceSize;
            var scale = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) / (TopDown3DCompassLayout.OuterRadius(reference) * 2);
            var orbit = TopDown3DCompassLayout.RingRadius(reference) * scale;
            var center = rectTransform.rect.center;
            Ring(mesh, center, orbit, 1.5f * scale, Track);
            Quad(mesh, center + Vector2.up * 126 * scale, new Vector2(2, 6) * scale, Rim);
            if (!hasHeading) return;
            var north = center + TopDown3DCompassNavigation.OnRing(-heading, orbit);
            var south = center + TopDown3DCompassNavigation.OnRing(180 - heading, orbit);
            Circle(mesh, north, 10 * scale, Rim); Circle(mesh, north, 8.5f * scale, Dark);
            // Rays distinguish the sun from both the dark sphere and the triangular destination marker.
            for (var i = 0; i < 4; i++)
            {
                var ray = TopDown3DCompassNavigation.OnRing(i * 90, 12 * scale);
                Quad(mesh, south + ray, (i % 2 == 0 ? new Vector2(2, 4) : new Vector2(4, 2)) * scale, Dark);
                Quad(mesh, south + ray, (i % 2 == 0 ? new Vector2(1, 3) : new Vector2(3, 1)) * scale, Sun);
            }
            Circle(mesh, south, 10 * scale, Dark); Circle(mesh, south, 8.5f * scale, Sun);
            if (!hasDestination) return;
            if (arrived)
            {
                Circle(mesh, center + Vector2.up * 95 * scale, 5 * scale, Dark);
                Ring(mesh, center + Vector2.up * 95 * scale, 4 * scale, 1.5f * scale, Arrow);
                return;
            }
            var direction = TopDown3DCompassNavigation.OnRing(bearing, 1);
            var perpendicular = new Vector2(direction.y, -direction.x);
            Triangle(mesh, center + direction * 100 * scale, center + direction * 90 * scale - perpendicular * 7 * scale,
                center + direction * 90 * scale + perpendicular * 7 * scale, Dark);
            Triangle(mesh, center + direction * 98 * scale, center + direction * 92 * scale - perpendicular * 4.5f * scale,
                center + direction * 92 * scale + perpendicular * 4.5f * scale, Arrow);
        }

        private static void Circle(VertexHelper mesh, Vector2 center, float radius, Color32 tint)
        {
            for (var i = 0; i < 32; i++) Triangle(mesh, center, center + TopDown3DCompassNavigation.OnRing(i * 11.25f, radius),
                center + TopDown3DCompassNavigation.OnRing((i + 1) * 11.25f, radius), tint);
        }
        private static void Ring(VertexHelper mesh, Vector2 center, float radius, float thickness, Color32 tint)
        {
            for (var i = 0; i < 96; i++)
            {
                var a = i * 3.75f; var b = (i + 1) * 3.75f;
                var outerA = center + TopDown3DCompassNavigation.OnRing(a, radius + thickness * .5f);
                var outerB = center + TopDown3DCompassNavigation.OnRing(b, radius + thickness * .5f);
                var innerA = center + TopDown3DCompassNavigation.OnRing(a, radius - thickness * .5f);
                var innerB = center + TopDown3DCompassNavigation.OnRing(b, radius - thickness * .5f);
                Triangle(mesh, outerA, outerB, innerA, tint); Triangle(mesh, innerA, outerB, innerB, tint);
            }
        }
        private static void Quad(VertexHelper mesh, Vector2 center, Vector2 size, Color32 tint)
        {
            var half = size * .5f;
            Triangle(mesh, center - half, center + new Vector2(-half.x, half.y), center + half, tint);
            Triangle(mesh, center - half, center + half, center + new Vector2(half.x, -half.y), tint);
        }
        private static void Triangle(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Color32 tint)
        {
            var start = mesh.currentVertCount;
            mesh.AddVert(a, tint, Vector2.zero); mesh.AddVert(b, tint, Vector2.zero); mesh.AddVert(c, tint, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
        }
    }
}
