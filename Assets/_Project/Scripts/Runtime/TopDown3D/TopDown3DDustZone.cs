using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    public sealed class TopDown3DDustZone : MonoBehaviour
    {
        private static readonly HashSet<TopDown3DDustZone> ActiveInstances =
            new HashSet<TopDown3DDustZone>();

        [SerializeField, Min(0f)] private float innerRadius = 14f;
        [SerializeField, Min(0.01f)] private float blendDistance = 12f;
        [SerializeField, Range(0.25f, 3f)] private float dustIntensity = 1.7f;
        [SerializeField] private Color dustTint = TopDown3DDustAtmosphere.DefaultBrightRustDust;

        public static IEnumerable<TopDown3DDustZone> ActiveZones => ActiveInstances;

        public float InnerRadius => innerRadius;
        public float BlendDistance => blendDistance;
        public float DustIntensity => dustIntensity;
        public Color DustTint => dustTint;

        private void OnEnable()
        {
            ActiveInstances.Add(this);
        }

        private void OnDisable()
        {
            ActiveInstances.Remove(this);
        }

        private void OnValidate()
        {
            innerRadius = Mathf.Max(0f, innerRadius);
            blendDistance = Mathf.Max(0.01f, blendDistance);
            dustIntensity = Mathf.Clamp(dustIntensity, 0.25f, 3f);
        }

        public void Configure(float fullStrengthRadius, float edgeBlendDistance, float intensity, Color tint)
        {
            innerRadius = Mathf.Max(0f, fullStrengthRadius);
            blendDistance = Mathf.Max(0.01f, edgeBlendDistance);
            dustIntensity = Mathf.Clamp(intensity, 0.25f, 3f);
            dustTint = tint;
        }

        public float SampleWeight(Vector3 worldPosition)
        {
            return EvaluateWeight(
                Vector3.Distance(worldPosition, transform.position),
                innerRadius,
                blendDistance);
        }

        public static float EvaluateWeight(float distanceFromCenter, float fullStrengthRadius, float edgeBlendDistance)
        {
            var safeInnerRadius = Mathf.Max(0f, fullStrengthRadius);
            var safeBlendDistance = Mathf.Max(0.01f, edgeBlendDistance);
            var normalized = Mathf.InverseLerp(
                safeInnerRadius + safeBlendDistance,
                safeInnerRadius,
                Mathf.Max(0f, distanceFromCenter));
            return normalized * normalized * (3f - (2f * normalized));
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(dustTint.r, dustTint.g, dustTint.b, 0.9f);
            Gizmos.DrawWireSphere(transform.position, innerRadius);
            Gizmos.color = new Color(dustTint.r, dustTint.g, dustTint.b, 0.35f);
            Gizmos.DrawWireSphere(transform.position, innerRadius + blendDistance);
        }
    }
}
