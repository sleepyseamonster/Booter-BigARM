using System;
using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Reusable authored composition data, not a world placement or an EditorOnly prefab.</summary>
    public sealed class TopDown3DAuthoredFormationAsset : ScriptableObject
    {
        [Serializable]
        public sealed class Member
        {
            [SerializeField] private string sourceId;
            [SerializeField] private Mesh mesh;
            [SerializeField] private Material material;
            [SerializeField] private Matrix4x4 localPose;
            [SerializeField] private TopDown3DNaturalMeshFamily[] bakedVariants = Array.Empty<TopDown3DNaturalMeshFamily>();
            public string SourceId => sourceId;
            public Mesh Mesh => mesh;
            public Material Material => material;
            public Matrix4x4 LocalPose => localPose;
            public IReadOnlyList<TopDown3DNaturalMeshFamily> BakedVariants => Array.AsReadOnly(bakedVariants);
            internal void SetBakedVariants(TopDown3DNaturalMeshFamily[] variants)
                => bakedVariants = (TopDown3DNaturalMeshFamily[])variants.Clone();
            internal Member(string id, Mesh geometry, Material surface, Matrix4x4 pose)
            {
                sourceId = id;
                mesh = geometry;
                material = surface;
                localPose = pose;
            }
        }

        [SerializeField] private string sourceGuid;
        [SerializeField] private string sourceRevision;
        [SerializeField] private Member[] members = Array.Empty<Member>();
        public string SourceGuid => sourceGuid;
        public string SourceRevision => sourceRevision;
        public IReadOnlyList<Member> Members => Array.AsReadOnly(members);
        public bool HasBakedVariants
        {
            get
            {
                if (members.Length == 0) return false;
                foreach (var member in members)
                {
                    if (member.BakedVariants.Count != TopDown3DNaturalObjectCatalog.MeshVariantsPerShape) return false;
                    foreach (var variant in member.BakedVariants)
                        if (variant == null || !variant.IsComplete) return false;
                }
                return true;
            }
        }
        internal void Configure(string guid, string revision, Member[] captured)
        {
            sourceGuid = guid;
            sourceRevision = revision;
            members = (Member[])captured.Clone();
        }
    }
}
