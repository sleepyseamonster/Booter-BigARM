using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public enum TopDown3DGroundStampSurface
    {
        KeepExisting,
        RedDirt,
        TanSand,
        ShaleGravel,
        RockyShale
    }

    /// <summary>
    /// Editable, bounded ground instruction for the Landscape Authoring Sandbox.
    /// Its transform is a local authoring position, not a streamed world object.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TopDown3DLandscapeGroundStamp : MonoBehaviour
    {
        [SerializeField, Min(0.5f)] private float radius = 4f;
        [SerializeField, Range(0.02f, 1f)] private float edgeBlend = 0.35f;
        [SerializeField, Range(0f, 1f)] private float opacity = 1f;
        [SerializeField, Range(0f, 1.5f)] private float sandBankHeight = 0.3f;
        [SerializeField] private TopDown3DGroundStampSurface surface =
            TopDown3DGroundStampSurface.TanSand;

        public float Radius => Mathf.Max(0.5f, radius);
        public float SandBankHeight => Mathf.Clamp(sandBankHeight, 0f, 1.5f);
        public TopDown3DGroundStampSurface Surface => surface;

        public float SurfaceWeight(Vector2 point)
        {
            var center = new Vector2(transform.position.x, transform.position.z);
            var distance = Vector2.Distance(point, center);
            var innerRadius = Radius * (1f - Mathf.Clamp(edgeBlend, 0.02f, 1f));
            return Mathf.Clamp01(opacity) * (1f - Mathf.SmoothStep(
                0f, 1f, Mathf.InverseLerp(innerRadius, Radius, distance)));
        }

        public float HeightAt(Vector2 point)
        {
            var center = new Vector2(transform.position.x, transform.position.z);
            var distance = Vector2.Distance(point, center);
            var cap = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(distance / Radius));
            return SandBankHeight * cap * Mathf.Clamp01(opacity);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.95f, 0.75f, 0.38f, 0.85f);
            Gizmos.DrawWireSphere(transform.position, Radius);
        }
    }
}
