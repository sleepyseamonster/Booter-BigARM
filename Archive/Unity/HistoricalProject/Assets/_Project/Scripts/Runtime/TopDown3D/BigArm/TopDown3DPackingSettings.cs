using System;
using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public enum TopDown3DMountZone { Core, Side, Upper }
    public enum TopDown3DMountSide { Left, Right }

    [Serializable]
    public sealed class TopDown3DMountDefinition
    {
        [SerializeField] private string stableId;
        [SerializeField] private TopDown3DMountZone zone;
        [SerializeField] private TopDown3DMountSide side;
        [SerializeField, Min(0f)] private float height;
        [SerializeField, Min(0.1f)] private float maxStackMass = 20f;
        public string StableId => stableId;
        public TopDown3DMountZone Zone => zone;
        public TopDown3DMountSide Side => side;
        public float Height => height;
        public float MaxStackMass => maxStackMass;
        internal void Configure(string id, TopDown3DMountZone mountZone, TopDown3DMountSide mountSide, float mountHeight)
        { stableId = id; zone = mountZone; side = mountSide; height = mountHeight; maxStackMass = 20f; }
    }

    [CreateAssetMenu(menuName = "Booter & BigARM/Top Down 3D/Packing Settings", fileName = "TopDown3DPackingSettings")]
    public sealed class TopDown3DPackingSettings : ScriptableObject
    {
        public const int MountCount = 12;
        [SerializeField] private List<TopDown3DMountDefinition> mounts = new List<TopDown3DMountDefinition>();
        [SerializeField, Min(1f)] private float recommendedMass = 80f;
        [SerializeField, Min(1f)] private float comfortableFraction = 0.75f;
        [SerializeField, Min(1f)] private float hardFraction = 1.15f;
        public IReadOnlyList<TopDown3DMountDefinition> Mounts => mounts;
        public float RecommendedMass => recommendedMass;
        public float ComfortableMass => recommendedMass * comfortableFraction;
        public float HardMass => recommendedMass * hardFraction;

        public bool TryValidate(out string error)
        {
            if (mounts == null || mounts.Count != MountCount) { error = $"Packing settings require exactly {MountCount} mounts."; return false; }
            var ids = new HashSet<string>();
            for (var i = 0; i < mounts.Count; i++)
            {
                if (mounts[i] == null || string.IsNullOrWhiteSpace(mounts[i].StableId) || !ids.Add(mounts[i].StableId))
                { error = $"Mount {i} has an invalid or duplicate stable ID."; return false; }
            }
            if (recommendedMass <= 0f || comfortableFraction <= 0f || hardFraction < 1f) { error = "Packing mass bands are invalid."; return false; }
            error = null; return true;
        }

        internal void ConfigureDefaults()
        {
            mounts.Clear();
            Add("core.l1", TopDown3DMountZone.Core, TopDown3DMountSide.Left, 0f);
            Add("core.l2", TopDown3DMountZone.Core, TopDown3DMountSide.Left, 0.1f);
            Add("core.l3", TopDown3DMountZone.Core, TopDown3DMountSide.Left, 0.2f);
            Add("core.r1", TopDown3DMountZone.Core, TopDown3DMountSide.Right, 0f);
            Add("core.r2", TopDown3DMountZone.Core, TopDown3DMountSide.Right, 0.1f);
            Add("core.r3", TopDown3DMountZone.Core, TopDown3DMountSide.Right, 0.2f);
            Add("side.l1", TopDown3DMountZone.Side, TopDown3DMountSide.Left, 0.8f);
            Add("side.l2", TopDown3DMountZone.Side, TopDown3DMountSide.Left, 0.9f);
            Add("side.r1", TopDown3DMountZone.Side, TopDown3DMountSide.Right, 0.8f);
            Add("side.r2", TopDown3DMountZone.Side, TopDown3DMountSide.Right, 0.9f);
            Add("upper.l1", TopDown3DMountZone.Upper, TopDown3DMountSide.Left, 1.6f);
            Add("upper.r1", TopDown3DMountZone.Upper, TopDown3DMountSide.Right, 1.6f);
            recommendedMass = 80f; comfortableFraction = 0.75f; hardFraction = 1.15f;
        }
        private void Add(string id, TopDown3DMountZone zone, TopDown3DMountSide side, float height)
        { var mount = new TopDown3DMountDefinition(); mount.Configure(id, zone, side, height); mounts.Add(mount); }
    }
}
