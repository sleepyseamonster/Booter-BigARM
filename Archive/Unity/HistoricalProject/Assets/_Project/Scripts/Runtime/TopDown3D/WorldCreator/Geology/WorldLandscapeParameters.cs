using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D.WorldCreator
{
    [Serializable]
    public struct WorldLandscapeParameters : IEquatable<WorldLandscapeParameters>
    {
        [SerializeField, Range(-1f, 1f)] private float baseElevationBias;
        [SerializeField, Range(0f, 1f)] private float relief;
        [SerializeField, Range(0f, 1f)] private float structuralDirection;
        [SerializeField, Range(0f, 1f)] private float structuralAnisotropy;
        [SerializeField, Range(0f, 1f)] private float canyonDensity;
        [SerializeField, Range(0f, 1f)] private float canyonBranching;
        [SerializeField, Range(0f, 1f)] private float canyonDepth;
        [SerializeField, Range(0f, 1f)] private float canyonWidth;
        [SerializeField, Range(0f, 1f)] private float weathering;
        [SerializeField, Range(0f, 1f)] private float sediment;
        [SerializeField, Range(0f, 1f)] private float prevailingWindDirection;
        [SerializeField, Range(0f, 1f)] private float windStrength;
        [SerializeField, Range(0f, 1f)] private float landmarkCadence;
        [SerializeField, Range(0f, 1f)] private float visualDensity;

        public WorldLandscapeParameters(
            float baseElevationBias,
            float relief,
            float structuralDirection,
            float structuralAnisotropy,
            float canyonDensity,
            float canyonBranching,
            float canyonDepth,
            float canyonWidth,
            float weathering,
            float sediment,
            float prevailingWindDirection,
            float windStrength,
            float landmarkCadence,
            float visualDensity)
        {
            this.baseElevationBias = RequireSignedNormalized(baseElevationBias, nameof(baseElevationBias));
            this.relief = RequireNormalized(relief, nameof(relief));
            this.structuralDirection = RequireNormalized(structuralDirection, nameof(structuralDirection));
            this.structuralAnisotropy = RequireNormalized(structuralAnisotropy, nameof(structuralAnisotropy));
            this.canyonDensity = RequireNormalized(canyonDensity, nameof(canyonDensity));
            this.canyonBranching = RequireNormalized(canyonBranching, nameof(canyonBranching));
            this.canyonDepth = RequireNormalized(canyonDepth, nameof(canyonDepth));
            this.canyonWidth = RequireNormalized(canyonWidth, nameof(canyonWidth));
            this.weathering = RequireNormalized(weathering, nameof(weathering));
            this.sediment = RequireNormalized(sediment, nameof(sediment));
            this.prevailingWindDirection = RequireNormalized(
                prevailingWindDirection,
                nameof(prevailingWindDirection));
            this.windStrength = RequireNormalized(windStrength, nameof(windStrength));
            this.landmarkCadence = RequireNormalized(landmarkCadence, nameof(landmarkCadence));
            this.visualDensity = RequireNormalized(visualDensity, nameof(visualDensity));
        }

        public float BaseElevationBias => baseElevationBias;
        public float Relief => relief;
        public float StructuralDirection => structuralDirection;
        public float StructuralAnisotropy => structuralAnisotropy;
        public float CanyonDensity => canyonDensity;
        public float CanyonBranching => canyonBranching;
        public float CanyonDepth => canyonDepth;
        public float CanyonWidth => canyonWidth;
        public float Weathering => weathering;
        public float Sediment => sediment;
        public float PrevailingWindDirection => prevailingWindDirection;
        public float WindStrength => windStrength;
        public float LandmarkCadence => landmarkCadence;
        public float VisualDensity => visualDensity;

        public bool TryValidate(out string error)
        {
            if (!IsSignedNormalized(baseElevationBias))
            {
                error = "Base elevation bias must be finite and inside [-1, 1].";
                return false;
            }

            if (!IsNormalized(relief)
                || !IsNormalized(structuralDirection)
                || !IsNormalized(structuralAnisotropy)
                || !IsNormalized(canyonDensity)
                || !IsNormalized(canyonBranching)
                || !IsNormalized(canyonDepth)
                || !IsNormalized(canyonWidth)
                || !IsNormalized(weathering)
                || !IsNormalized(sediment)
                || !IsNormalized(prevailingWindDirection)
                || !IsNormalized(windStrength)
                || !IsNormalized(landmarkCadence)
                || !IsNormalized(visualDensity))
            {
                error = "Landscape parameters must be finite and normalized.";
                return false;
            }

            error = null;
            return true;
        }

        public bool Equals(WorldLandscapeParameters other)
        {
            return baseElevationBias.Equals(other.baseElevationBias)
                && relief.Equals(other.relief)
                && structuralDirection.Equals(other.structuralDirection)
                && structuralAnisotropy.Equals(other.structuralAnisotropy)
                && canyonDensity.Equals(other.canyonDensity)
                && canyonBranching.Equals(other.canyonBranching)
                && canyonDepth.Equals(other.canyonDepth)
                && canyonWidth.Equals(other.canyonWidth)
                && weathering.Equals(other.weathering)
                && sediment.Equals(other.sediment)
                && prevailingWindDirection.Equals(other.prevailingWindDirection)
                && windStrength.Equals(other.windStrength)
                && landmarkCadence.Equals(other.landmarkCadence)
                && visualDensity.Equals(other.visualDensity);
        }

        public override bool Equals(object obj)
        {
            return obj is WorldLandscapeParameters other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = baseElevationBias.GetHashCode();
                hash = hash * 397 ^ relief.GetHashCode();
                hash = hash * 397 ^ structuralDirection.GetHashCode();
                hash = hash * 397 ^ canyonDensity.GetHashCode();
                hash = hash * 397 ^ canyonDepth.GetHashCode();
                hash = hash * 397 ^ visualDensity.GetHashCode();
                return hash;
            }
        }

        internal static float RequireNormalized(float value, string parameterName)
        {
            if (!IsNormalized(value))
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "The value must be finite and inside [0, 1].");
            }

            return value == 0f ? 0f : value;
        }

        internal static float RequireSignedNormalized(float value, string parameterName)
        {
            if (!IsSignedNormalized(value))
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "The value must be finite and inside [-1, 1].");
            }

            return value == 0f ? 0f : value;
        }

        private static bool IsNormalized(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f && value <= 1f;
        }

        private static bool IsSignedNormalized(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value >= -1f && value <= 1f;
        }
    }
}
