using System;
using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [CreateAssetMenu(
        menuName = "Booter & BigARM/Top Down 3D/Resource Catalog",
        fileName = "TopDown3DResourceCatalog")]
    public sealed class TopDown3DResourceCatalog : ScriptableObject
    {
        [SerializeField] private List<TopDown3DResourceDefinition> definitions =
            new List<TopDown3DResourceDefinition>();
        [NonSerialized] private Dictionary<string, TopDown3DResourceDefinition> lookup;

        public IReadOnlyList<TopDown3DResourceDefinition> Definitions => definitions;

        public bool TryGetDefinition(string resourceId, out TopDown3DResourceDefinition definition)
        {
            EnsureLookup();
            return lookup.TryGetValue(resourceId ?? string.Empty, out definition);
        }

        public TopDown3DResourceDefinition GetRequiredDefinition(string resourceId)
        {
            if (TryGetDefinition(resourceId, out var definition))
            {
                return definition;
            }

            throw new KeyNotFoundException($"Unknown TopDown3D resource ID '{resourceId}'.");
        }

        public bool TryValidate(TopDown3DNaturalObjectCatalog naturalCatalog, out string error)
        {
            if (definitions == null || definitions.Count == 0)
            {
                error = "Resource catalog must contain at least one definition.";
                return false;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            var assets = new HashSet<TopDown3DResourceDefinition>();
            for (var i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i];
                if (definition == null)
                {
                    error = $"Resource catalog entry {i} is null.";
                    return false;
                }

                if (!definition.TryValidate(naturalCatalog, out error))
                {
                    return false;
                }

                if (!ids.Add(definition.ResourceId) || !assets.Add(definition))
                {
                    error = $"Resource catalog contains duplicate definition '{definition.ResourceId}'.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        internal void Configure(IEnumerable<TopDown3DResourceDefinition> authoredDefinitions)
        {
            definitions = authoredDefinitions != null
                ? new List<TopDown3DResourceDefinition>(authoredDefinitions)
                : new List<TopDown3DResourceDefinition>();
            lookup = null;
        }

        private void OnEnable() => lookup = null;
        private void OnValidate() => lookup = null;

        private void EnsureLookup()
        {
            if (lookup != null)
            {
                return;
            }

            lookup = new Dictionary<string, TopDown3DResourceDefinition>(StringComparer.Ordinal);
            for (var i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i];
                if (definition == null || string.IsNullOrWhiteSpace(definition.ResourceId)
                    || lookup.ContainsKey(definition.ResourceId))
                {
                    throw new InvalidOperationException("Resource catalog contains null, empty, or duplicate IDs.");
                }

                lookup.Add(definition.ResourceId, definition);
            }
        }
    }
}
