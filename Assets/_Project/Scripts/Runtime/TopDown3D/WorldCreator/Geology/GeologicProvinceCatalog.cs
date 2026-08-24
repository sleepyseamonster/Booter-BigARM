using System;
using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.TopDown3D.WorldCreator
{
    [CreateAssetMenu(
        menuName = "Booter & BigARM/World Creator/Geologic Province Catalog",
        fileName = "GeologicProvinceCatalog")]
    public sealed class GeologicProvinceCatalog : ScriptableObject
    {
        [SerializeField] private bool nonCanonProofOnly;
        [SerializeField] private List<GeologicProvinceDefinition> definitions =
            new List<GeologicProvinceDefinition>();

        public bool NonCanonProofOnly => nonCanonProofOnly;
        public IReadOnlyList<GeologicProvinceDefinition> Definitions => definitions;

        public bool TryGet(string stableId, out GeologicProvinceDefinition definition)
        {
            for (var i = 0; i < definitions.Count; i++)
            {
                var candidate = definitions[i];
                if (candidate != null && string.Equals(candidate.StableId, stableId, StringComparison.Ordinal))
                {
                    definition = candidate;
                    return true;
                }
            }

            definition = null;
            return false;
        }

        public bool TryValidate(out string error)
        {
            if (definitions == null || definitions.Count == 0)
            {
                error = "Geologic province catalog must contain at least one definition.";
                return false;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i];
                if (definition == null)
                {
                    error = $"Geologic province catalog entry {i} is null.";
                    return false;
                }

                if (!definition.TryValidate(out error))
                {
                    return false;
                }

                if (!ids.Add(definition.StableId))
                {
                    error = $"Geologic province catalog contains duplicate ID '{definition.StableId}'.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        public void ConfigureProofData(IEnumerable<GeologicProvinceDefinition> authoredDefinitions)
        {
            nonCanonProofOnly = true;
            definitions = authoredDefinitions != null
                ? new List<GeologicProvinceDefinition>(authoredDefinitions)
                : new List<GeologicProvinceDefinition>();
        }
    }
}
