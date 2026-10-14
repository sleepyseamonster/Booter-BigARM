using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [Serializable]
    public sealed class AnimationPlaygroundCatalogEntry
    {
        public string sourceId, title, category, stationId;
        public bool restrictedReference;
    }
    public sealed class AnimationPlaygroundCatalog : ScriptableObject
    {
        public int schemaVersion=1;
        public AnimationPlaygroundCatalogEntry[] entries=Array.Empty<AnimationPlaygroundCatalogEntry>();
    }
}
