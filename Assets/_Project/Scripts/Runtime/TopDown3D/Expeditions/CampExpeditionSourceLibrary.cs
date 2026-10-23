using System;
using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Presentation only. Never participates in the economy or population fingerprint.</summary>
    [CreateAssetMenu(menuName = "Booter & BigARM/Expedition Source Presentation")]
    public sealed class CampExpeditionSourceLibrary : ScriptableObject
    {
        [Serializable] public sealed class Entry
        {
            public string itemId;
            public CampExpeditionSourceView prefab;
        }
        public Entry[] entries = Array.Empty<Entry>();
        public CampExpeditionSourceView Find(string itemId)
        {
            foreach (var entry in entries) if (entry != null && entry.itemId == itemId) return entry.prefab;
            throw new InvalidOperationException("Missing expedition source presentation: " + itemId);
        }
        public void Validate(CampExpeditionDefinition definition)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.itemId) || !ids.Add(entry.itemId)
                    || entry.prefab == null || !entry.prefab.Valid)
                    throw new InvalidOperationException("Invalid expedition source presentation binding.");
            }
            foreach (var item in definition.items)
                if (item.visibleInTree && item.inputs.Length == 0) Find(item.id);
            foreach (var part in definition.Find(definition.rootId).inputs) Find(part.itemId);
        }
        public static CampExpeditionSourceLibrary Load(CampExpeditionDefinition definition)
        {
            var value = Resources.Load<CampExpeditionSourceLibrary>("CampExpeditionSourceLibrary");
            if (value == null) throw new InvalidOperationException("Expedition source presentation is not installed; checkpoint preserved.");
            value.Validate(definition);
            return value;
        }
    }
}
