using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D.WorldCreator
{
    [Serializable]
    public sealed class StrataFamilyDefinition
    {
        [SerializeField] private string stableId;
        [SerializeField] private string displayName;
        [SerializeField, Range(0f, 1f)] private float hardness;
        [SerializeField, Range(0f, 1f)] private float folding;
        [SerializeField, Range(0f, 1f)] private float fracture;
        [SerializeField, Range(0f, 1f)] private float ironOxideTendency;
        [SerializeField, Range(0f, 1f)] private float paleDepositTendency;
        [SerializeField, Range(0f, 1f)] private float darkRockTendency;

        public StrataFamilyDefinition(
            string stableId,
            string displayName,
            float hardness,
            float folding,
            float fracture,
            float ironOxideTendency,
            float paleDepositTendency,
            float darkRockTendency)
        {
            this.stableId = stableId;
            this.displayName = displayName;
            this.hardness = WorldLandscapeParameters.RequireNormalized(hardness, nameof(hardness));
            this.folding = WorldLandscapeParameters.RequireNormalized(folding, nameof(folding));
            this.fracture = WorldLandscapeParameters.RequireNormalized(fracture, nameof(fracture));
            this.ironOxideTendency = WorldLandscapeParameters.RequireNormalized(
                ironOxideTendency,
                nameof(ironOxideTendency));
            this.paleDepositTendency = WorldLandscapeParameters.RequireNormalized(
                paleDepositTendency,
                nameof(paleDepositTendency));
            this.darkRockTendency = WorldLandscapeParameters.RequireNormalized(
                darkRockTendency,
                nameof(darkRockTendency));
        }

        public string StableId => stableId;
        public string DisplayName => displayName;
        public float Hardness => hardness;
        public float Folding => folding;
        public float Fracture => fracture;
        public float IronOxideTendency => ironOxideTendency;
        public float PaleDepositTendency => paleDepositTendency;
        public float DarkRockTendency => darkRockTendency;

        public bool TryValidate(out string error)
        {
            if (string.IsNullOrWhiteSpace(stableId))
            {
                error = "Strata family has an empty stable ID.";
                return false;
            }

            if (stableId.Length > 128 || !string.Equals(stableId, stableId.Trim(), StringComparison.Ordinal))
            {
                error = $"Strata family ID '{stableId}' is not canonical.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                error = $"Strata family '{stableId}' has an empty display name.";
                return false;
            }

            if (!IsNormalized(hardness)
                || !IsNormalized(folding)
                || !IsNormalized(fracture)
                || !IsNormalized(ironOxideTendency)
                || !IsNormalized(paleDepositTendency)
                || !IsNormalized(darkRockTendency))
            {
                error = $"Strata family '{stableId}' contains a non-finite or unbounded parameter.";
                return false;
            }

            error = null;
            return true;
        }

        private static bool IsNormalized(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f && value <= 1f;
        }
    }
}
