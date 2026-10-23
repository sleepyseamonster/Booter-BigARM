using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [Flags]
    public enum WardrobeRegion { None = 0, Torso = 1, Arms = 2, Legs = 4, Hands = 8, Feet = 16, Head = 32 }
    public enum WardrobeLayer { Base, Outer, Armor }

    [Serializable]
    public sealed class WardrobePart
    {
        public Mesh mesh;
        public Material[] materials = Array.Empty<Material>();
        public string[] bonePaths = Array.Empty<string>();
        public string rootBonePath;
        public Bounds bounds;
    }

    /// <summary>Presentation only; it grants no inventory item, slot, protection, or attachment eligibility.</summary>
    public sealed class TopDown3DWardrobeDefinition : ScriptableObject
    {
        public int schemaVersion = 1;
        public string visualId;
        public TopDown3DWardrobeRig rig;
        public WardrobeLayer layer;
        public WardrobeRegion occupies;
        public WardrobeRegion hides;
        public Material[] bodyMaterials = Array.Empty<Material>();
        public WardrobePart[] parts = Array.Empty<WardrobePart>();
    }
}
