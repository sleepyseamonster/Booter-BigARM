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
        [NonSerialized] private List<TopDown3DItemDefinition> loadoutDefinitions;
        [NonSerialized] private TopDown3DCampLoopLoadoutProfile appliedProfile;

        public IReadOnlyList<TopDown3DItemDefinition> Definitions => loadoutDefinitions ?? definitions;
        public bool HasEquipmentDefinitions
        { get { foreach (var item in Definitions) if (item != null && item.IsEquipment) return true; return false; } }

        internal void ApplyLoadoutProfile(TopDown3DCampLoopLoadoutProfile profile)
        {
            if (loadoutDefinitions != null && appliedProfile == profile) return;
            var overrides = new Dictionary<string, TopDown3DItemDefinition>(StringComparer.Ordinal);
            foreach (var item in profile.definitions)
                if (item == null || !item.TryValidate(out _) || !overrides.TryAdd(item.ItemId, item))
                    throw new InvalidOperationException("Invalid Camp Loop loadout profile; inventory initialization stopped.");
            var resolved = new List<TopDown3DItemDefinition>();
            foreach (var item in definitions)
            {
                if (overrides.Remove(item.ItemId, out var replacement)) resolved.Add(replacement);
                else resolved.Add(item);
            }
            resolved.AddRange(overrides.Values);
            loadoutDefinitions = resolved;
            appliedProfile = profile;
            lookup = null;
        }

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
            if (Definitions == null || Definitions.Count == 0)
            {
                error = "Item catalog must contain at least one definition.";
                return false;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            var assets = new HashSet<TopDown3DItemDefinition>();
            for (var i = 0; i < Definitions.Count; i++)
            {
                var definition = Definitions[i];
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
            loadoutDefinitions = null;
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
                Definitions.Count,
                StringComparer.Ordinal);
            for (var i = 0; i < Definitions.Count; i++)
            {
                var definition = Definitions[i];
                lookup.Add(definition.ItemId, definition);
            }
        }
    }
}
