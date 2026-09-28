using System;
using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public enum TopDown3DAuthoredFormationVariationProfile
    {
        FixedCluster = 0,
        HandbuiltSpire = 1
    }

    /// <summary>Reusable authored composition data, not a world placement or an EditorOnly prefab.</summary>
    public sealed class TopDown3DAuthoredFormationAsset : ScriptableObject
    {
        [Serializable]
        public sealed class GroundTreatment
        {
            [SerializeField] private float shallowBurial = 0.11067194f;
            [SerializeField] private float deepBurial = 0.74308294f;
            [SerializeField] private float maximumGroundTilt = 35f;
            [SerializeField] private float bandHeight = 0.48f;
            [SerializeField] private float bandOpacity = 0.72f;
            [SerializeField] private Color bandColor = new Color(0.58f, 0.27f, 0.12f, 1f);
            [SerializeField] private float bandFeather = 0.14f;
            [SerializeField] private float bandWaviness = 0.38f;
            [SerializeField] private float bandNoiseScale = 2.2f;
            [SerializeField] private float bandDirectionalBuildup;
            [SerializeField] private float groundClutter = 1f;
            [SerializeField] private float pebbleDepth = 0.015f;

            public float ShallowBurial => Mathf.Clamp01(shallowBurial);
            public float DeepBurial => Mathf.Clamp(deepBurial, ShallowBurial, 1f);
            public float MaximumGroundTilt => Mathf.Clamp(maximumGroundTilt, 0f, 35f);
            public float BandHeight => bandHeight;
            public float BandOpacity => bandOpacity;
            public Color BandColor => bandColor;
            public float BandFeather => bandFeather;
            public float BandWaviness => bandWaviness;
            public float BandNoiseScale => bandNoiseScale;
            public float BandDirectionalBuildup => bandDirectionalBuildup;
            public float GroundClutter => groundClutter;
            public float PebbleDepth => pebbleDepth;

            internal void SetGroundFit(float shallow, float deep, float tilt)
            {
                shallowBurial = shallow;
                deepBurial = deep;
                maximumGroundTilt = tilt;
            }
        }

        [Serializable]
        public sealed class ApprovedStageEntry
        {
            [SerializeField] private int sourceIndex;
            [SerializeField] private string instanceId;
            [SerializeField] private Matrix4x4 localPose;
            [SerializeField] private TopDown3DNaturalMeshFamily family;

            public int SourceIndex => sourceIndex;
            public string InstanceId => instanceId;
            public Matrix4x4 LocalPose => localPose;
            public TopDown3DNaturalMeshFamily Family => family;

            internal ApprovedStageEntry(int sourceIndex, string instanceId, Matrix4x4 pose,
                TopDown3DNaturalMeshFamily bakedFamily)
            {
                this.sourceIndex = sourceIndex;
                this.instanceId = instanceId;
                localPose = pose;
                family = bakedFamily;
            }
        }

        [Serializable]
        public sealed class BakedGeneration
        {
            [SerializeField] private int seed;
            [SerializeField] private ApprovedStageEntry[] entries;
            public int Seed => seed;
            public IReadOnlyList<ApprovedStageEntry> Entries => Array.AsReadOnly(entries);

            internal BakedGeneration(int generationSeed, ApprovedStageEntry[] generationEntries)
            {
                seed = generationSeed;
                entries = (ApprovedStageEntry[])generationEntries.Clone();
            }
        }

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
        [SerializeField] private TopDown3DAuthoredFormationVariationProfile variationProfile;
        [SerializeField] private GroundTreatment groundTreatment = new GroundTreatment();
        [SerializeField] private bool approvedStageVariationEnabled;
        [SerializeField] private int approvedStageSeed;
        [SerializeField] private ApprovedStageEntry[] approvedStageEntries = Array.Empty<ApprovedStageEntry>();
        [SerializeField] private BakedGeneration[] proceduralGenerations = Array.Empty<BakedGeneration>();
        [SerializeField] private Member[] members = Array.Empty<Member>();
        public string SourceGuid => sourceGuid;
        public string SourceRevision => sourceRevision;
        public GroundTreatment SurfaceTreatment => groundTreatment;
        public bool ApprovedStageVariationEnabled => approvedStageVariationEnabled;
        public int ApprovedStageSeed => approvedStageSeed;
        public IReadOnlyList<ApprovedStageEntry> ApprovedStageEntries => Array.AsReadOnly(approvedStageEntries);
        public IReadOnlyList<BakedGeneration> ProceduralGenerations => Array.AsReadOnly(proceduralGenerations);
        public bool HasApprovedStage
        {
            get
            {
                if (approvedStageEntries == null || approvedStageEntries.Length == 0) return false;
                foreach (var entry in approvedStageEntries)
                    if (entry == null || entry.SourceIndex < 0 || entry.SourceIndex >= members.Length
                        || string.IsNullOrEmpty(entry.InstanceId) || entry.Family == null
                        || !entry.Family.IsComplete) return false;
                if (proceduralGenerations == null || proceduralGenerations.Length < 3) return false;
                foreach (var generation in proceduralGenerations)
                {
                    if (generation == null || generation.Entries.Count == 0) return false;
                    foreach (var entry in generation.Entries)
                        if (entry == null || entry.SourceIndex < 0 || entry.SourceIndex >= members.Length
                            || string.IsNullOrEmpty(entry.InstanceId) || entry.Family == null
                            || !entry.Family.IsComplete) return false;
                }
                return true;
            }
        }
        public TopDown3DAuthoredFormationVariationProfile VariationProfile =>
            variationProfile == TopDown3DAuthoredFormationVariationProfile.FixedCluster
                && string.Equals(name, "HandbuiltSpire", StringComparison.Ordinal)
                    ? TopDown3DAuthoredFormationVariationProfile.HandbuiltSpire
                    : variationProfile;
        public IReadOnlyList<Member> Members => Array.AsReadOnly(members);
        public bool HasBakedVariants
        {
            get
            {
                if (members.Length == 0) return false;
                foreach (var member in members)
                {
                    if (member == null || member.Mesh == null || member.Material == null) return false;
                    if (member.BakedVariants.Count != TopDown3DNaturalObjectCatalog.MeshVariantsPerShape) return false;
                    foreach (var variant in member.BakedVariants)
                        if (variant == null || !variant.IsComplete) return false;
                }
                return true;
            }
        }
        internal void Configure(string guid, string revision,
            TopDown3DAuthoredFormationVariationProfile profile, Member[] captured)
        {
            if (!string.Equals(sourceRevision, revision, StringComparison.Ordinal))
            {
                approvedStageEntries = Array.Empty<ApprovedStageEntry>();
                proceduralGenerations = Array.Empty<BakedGeneration>();
            }
            sourceGuid = guid;
            sourceRevision = revision;
            variationProfile = profile;
            members = (Member[])captured.Clone();
        }

        internal void SetApprovedStage(ApprovedStageEntry[] entries)
        {
            approvedStageEntries = (ApprovedStageEntry[])entries.Clone();
        }

        internal void SetApprovedStageConfiguration(bool variationEnabled, int seed)
        {
            approvedStageVariationEnabled = variationEnabled;
            approvedStageSeed = seed;
        }

        internal void SetProceduralGenerations(BakedGeneration[] generations)
        {
            proceduralGenerations = (BakedGeneration[])generations.Clone();
        }
    }
}
