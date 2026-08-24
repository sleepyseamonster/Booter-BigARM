using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D.WorldCreator
{
    [Serializable]
    public sealed class GeologicProvinceDefinition
    {
        [SerializeField] private string stableId;
        [SerializeField] private string displayName;
        [SerializeField] private WorldLandscapeParameters landscape;

        public GeologicProvinceDefinition(
            string stableId,
            string displayName,
            WorldLandscapeParameters landscape)
        {
            this.stableId = stableId;
            this.displayName = displayName;
            this.landscape = landscape;
        }

        public string StableId => stableId;
        public string DisplayName => displayName;
        public WorldLandscapeParameters Landscape => landscape;

        public bool TryValidate(out string error)
        {
            if (string.IsNullOrWhiteSpace(stableId))
            {
                error = "Geologic province has an empty stable ID.";
                return false;
            }

            if (stableId.Length > 128 || !string.Equals(stableId, stableId.Trim(), StringComparison.Ordinal))
            {
                error = $"Geologic province ID '{stableId}' is not canonical.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                error = $"Geologic province '{stableId}' has an empty display name.";
                return false;
            }

            if (!landscape.TryValidate(out error))
            {
                error = $"Geologic province '{stableId}' is invalid: {error}";
                return false;
            }

            error = null;
            return true;
        }
    }
}
