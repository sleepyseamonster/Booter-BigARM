using System;
using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [CreateAssetMenu(
        menuName = "Booter & BigARM/Top Down 3D/Item Catalog",
        fileName = "TopDown3DItemCatalog")]
    public sealed class TopDown3DItemCatalog : ScriptableObject
    {
        [SerializeField] private List<TopDown3DItemDefinition> definitions =
            new List<TopDown3DItemDefinition>();

        [NonSerialized] private Dictionary<string, TopDown3DItemDefinition> lookup;

        public IReadOnlyList<TopDown3DItemDefinition> Definitions => definitions;

        public bool TryGetDefinition(string itemId, out TopDown3DItemDefinition definition)
        {
            EnsureLookup();
            return lookup.TryGetValue(itemId ?? string.Empty, out definition);
        }

        public TopDown3DItemDefinition GetRequiredDefinition(string itemId)
        {
            if (TryGetDefinition(itemId, out var definition))
            {
                return definition;
            }

            throw new KeyNotFoundException($"Unknown TopDown3D item ID '{itemId}'.");
        }

        public bool TryValidate(out string error)
        {
            if (definitions == null || definitions.Count == 0)
            {
                error = "Item catalog must contain at least one definition.";
                return false;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            var assets = new HashSet<TopDown3DItemDefinition>();
            for (var i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i];
                if (definition == null)
                {
                    error = $"Item catalog entry {i} is null.";
                    return false;
                }

                if (!definition.TryValidate(out error))
                {
                    return false;
                }

                if (!ids.Add(definition.ItemId))
                {
                    error = $"Item catalog contains duplicate ID '{definition.ItemId}'.";
                    return false;
                }

                if (!assets.Add(definition))
                {
                    error = $"Item catalog references '{definition.ItemId}' more than once.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        internal void Configure(IEnumerable<TopDown3DItemDefinition> authoredDefinitions)
        {
            definitions = authoredDefinitions != null
                ? new List<TopDown3DItemDefinition>(authoredDefinitions)
                : new List<TopDown3DItemDefinition>();
            lookup = null;
        }

        private void OnEnable()
        {
            lookup = null;
        }

        private void OnValidate()
        {
            lookup = null;
        }

        private void EnsureLookup()
        {
            if (lookup != null)
            {
                return;
            }

            if (!TryValidate(out var error))
            {
                throw new InvalidOperationException(error);
            }

            lookup = new Dictionary<string, TopDown3DItemDefinition>(
                definitions.Count,
                StringComparer.Ordinal);
            for (var i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i];
                lookup.Add(definition.ItemId, definition);
            }
        }
    }
}
