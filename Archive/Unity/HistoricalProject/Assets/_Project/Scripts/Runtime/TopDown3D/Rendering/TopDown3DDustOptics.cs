using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public static class TopDown3DDustOptics
    {
        public const float SqrtNaturalLogOfTwo = 0.8325546f;
        private const float FourPi = 12.5663706f;

        public static float ConvertLegacyDensityToExtinction(float legacyDensity)
        {
            return Mathf.Max(0f, legacyDensity) * SqrtNaturalLogOfTwo;
        }

        public static float EvaluateTransmittance(float extinction, float distance)
        {
            return Mathf.Exp(-Mathf.Max(0f, extinction) * Mathf.Max(0f, distance));
        }

        public static float EvaluateHalfVisibilityDistance(float extinction)
        {
            return Mathf.Log(2f) / Mathf.Max(0.0001f, extinction);
        }

        public static float EvaluateNormalizedHenyeyGreenstein(float cosine, float anisotropy)
        {
            var g = Mathf.Clamp(anisotropy, -0.9f, 0.9f);
            var gSquared = g * g;
            var denominator = Mathf.Pow(
                Mathf.Max(0.0001f, 1f + gSquared - (2f * g * Mathf.Clamp(cosine, -1f, 1f))),
                1.5f);
            return ((1f - gSquared) / (FourPi * denominator)) * FourPi;
        }

        public static float EvaluateVisibilitySafePhase(
            float cosine,
            float anisotropy,
            float maximumForwardPhase)
        {
            var rawPhase = EvaluateNormalizedHenyeyGreenstein(cosine, anisotropy);
            if (rawPhase <= 1f)
            {
                return rawPhase;
            }

            var forwardRange = Mathf.Max(0f, maximumForwardPhase - 1f);
            if (forwardRange <= 0f)
            {
                return 1f;
            }

            var forwardExcess = rawPhase - 1f;
            return 1f + ((forwardExcess * forwardRange) / (forwardExcess + forwardRange));
        }

        public static Vector2 SnapDensityMapMinimum(Vector3 center, float extent, int resolution)
        {
            var safeExtent = Mathf.Max(1f, extent);
            var safeResolution = Mathf.Max(1, resolution);
            var cellSize = safeExtent / safeResolution;
            return new Vector2(
                Mathf.Floor((center.x - (safeExtent * 0.5f)) / cellSize) * cellSize,
                Mathf.Floor((center.z - (safeExtent * 0.5f)) / cellSize) * cellSize);
        }
    }
}
