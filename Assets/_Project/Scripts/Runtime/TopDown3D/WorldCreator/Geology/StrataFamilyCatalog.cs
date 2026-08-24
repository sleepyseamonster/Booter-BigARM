using System;
using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.TopDown3D.WorldCreator
{
    [CreateAssetMenu(
        menuName = "Booter & BigARM/World Creator/Strata Family Catalog",
        fileName = "StrataFamilyCatalog")]
    public sealed class StrataFamilyCatalog : ScriptableObject
    {
        [SerializeField] private bool nonCanonProofOnly;
        [SerializeField] private List<StrataFamilyDefinition> definitions =
            new List<StrataFamilyDefinition>();

        public bool NonCanonProofOnly => nonCanonProofOnly;
        public IReadOnlyList<StrataFamilyDefinition> Definitions => definitions;

        public bool TryGet(string stableId, out StrataFamilyDefinition definition)
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
                error = "Strata family catalog must contain at least one definition.";
                return false;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i];
                if (definition == null)
                {
                    error = $"Strata family catalog entry {i} is null.";
                    return false;
                }

                if (!definition.TryValidate(out error))
                {
                    return false;
                }

                if (!ids.Add(definition.StableId))
                {
                    error = $"Strata family catalog contains duplicate ID '{definition.StableId}'.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        public void ConfigureProofData(IEnumerable<StrataFamilyDefinition> authoredDefinitions)
        {
            nonCanonProofOnly = true;
            definitions = authoredDefinitions != null
                ? new List<StrataFamilyDefinition>(authoredDefinitions)
                : new List<StrataFamilyDefinition>();
        }
    }
}
