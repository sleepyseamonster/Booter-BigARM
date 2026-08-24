using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D.WorldCreator
{
    [CreateAssetMenu(
        menuName = "Booter & BigARM/World Creator/Production Runtime Profile",
        fileName = "ProductionWorldCreatorProfile")]
    public sealed class WorldCreatorProductionProfile : ScriptableObject
    {
        public const string ResourcePath = "WorldCreator/ProductionWorldCreatorProfile";
        public const int CurrentTopologyVersion = 2;

        [Header("Temporary production-path proof authority")]
        [SerializeField] private bool nonCanonProofOnly = true;
        [SerializeField] private GeologicProvinceCatalog provinceCatalog;
        [SerializeField] private StrataFamilyCatalog strataCatalog;
        [SerializeField] private NonCanonProofInfluenceProfile influenceProfile;

        [Header("Independent world versions")]
        [SerializeField, Min(1)] private int topologyVersion = CurrentTopologyVersion;
        [SerializeField, Min(1)] private int coordinateVersion = 1;
        [SerializeField, Min(1)] private int landformVersion = 1;
        [SerializeField, Min(1)] private int materialVersion = 1;
        [SerializeField, Min(1)] private int decorationVersion = 1;
        [SerializeField, Min(1)] private int resourceVersion = 1;
        [SerializeField, Min(1)] private int siteVersion = 1;

        [Header("Representation budgets")]
        [SerializeField, Range(3, 65)] private int nearResolution = 25;
        [SerializeField, Range(3, 33)] private int midResolution = 9;
        [SerializeField, Range(3, 17)] private int farResolution = 5;
        [SerializeField, Range(9, 256)] private int maximumCanyonPlans = 96;
        [SerializeField, Range(1, 64)] private int maximumTerrainWindows = 24;
        [SerializeField, Range(16, 512)] private int maximumRepresentationEntries = 256;
        [SerializeField, Min(8)] private int maximumRepresentationMegabytes = 96;
        [SerializeField, Range(1, 16)] private int maximumRetainedBuffersPerResolution = 4;
        [SerializeField, Min(256f)] private float localFrameRadius = 8192f;
        [SerializeField, Min(128f)] private float rebaseThreshold = 1536f;

        public bool NonCanonProofOnly => nonCanonProofOnly;
        public GeologicProvinceCatalog ProvinceCatalog => provinceCatalog;
        public StrataFamilyCatalog StrataCatalog => strataCatalog;
        public NonCanonProofInfluenceProfile InfluenceProfile => influenceProfile;
        public int MaximumCanyonPlans => maximumCanyonPlans;
        public int MaximumTerrainWindows => maximumTerrainWindows;
        public int MaximumRepresentationEntries => maximumRepresentationEntries;
        public long MaximumRepresentationBytes => maximumRepresentationMegabytes * 1024L * 1024L;
        public int MaximumRetainedBuffersPerResolution => maximumRetainedBuffersPerResolution;
        public double LocalFrameRadius => localFrameRadius;
        public float RebaseThreshold => rebaseThreshold;

        public WorldVersionManifest CreateVersionManifest()
        {
            return new WorldVersionManifest(
                topologyVersion,
                coordinateVersion,
                landformVersion,
                materialVersion,
                decorationVersion,
                resourceVersion,
                siteVersion);
        }

        public WorldRepresentationBuildProfile CreateRepresentationProfile()
        {
            return new WorldRepresentationBuildProfile(nearResolution, midResolution, farResolution);
        }

        public bool TryValidate(out string error)
        {
            if (!nonCanonProofOnly)
            {
                error = "The temporary production runtime profile must remain explicitly non-canon.";
                return false;
            }

            if (topologyVersion != CurrentTopologyVersion)
            {
                error = $"The production runtime requires topology version {CurrentTopologyVersion}.";
                return false;
            }

            if (provinceCatalog == null || strataCatalog == null || influenceProfile == null)
            {
                error = "The production runtime profile is missing its non-canon proof geology references.";
                return false;
            }

            if (!provinceCatalog.NonCanonProofOnly
                || !strataCatalog.NonCanonProofOnly
                || !influenceProfile.NonCanonProofOnly)
            {
                error = "Every temporary production geology source must remain marked non-canon proof only.";
                return false;
            }

            if (!provinceCatalog.TryValidate(out error)
                || !strataCatalog.TryValidate(out error)
                || !influenceProfile.TryValidate(out error))
            {
                return false;
            }

            try
            {
                _ = CreateVersionManifest();
                _ = CreateRepresentationProfile();
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }

            if (maximumCanyonPlans < 9
                || maximumTerrainWindows < 1
                || maximumRepresentationEntries < 1
                || maximumRepresentationMegabytes < 8
                || maximumRetainedBuffersPerResolution < 1)
            {
                error = "The production runtime has an invalid cache or pool budget.";
                return false;
            }

            if (localFrameRadius <= rebaseThreshold * 2f)
            {
                error = "The local frame radius must exceed twice the rebase threshold.";
                return false;
            }

            error = null;
            return true;
        }

        public static WorldCreatorProductionProfile LoadRequired()
        {
            var profile = Resources.Load<WorldCreatorProductionProfile>(ResourcePath);
            if (profile == null)
            {
                throw new InvalidOperationException(
                    $"Missing World Creator production runtime profile at Resources/{ResourcePath}.");
            }

            if (!profile.TryValidate(out var error))
            {
                throw new InvalidOperationException(error);
            }

            return profile;
        }

        private void OnValidate()
        {
            topologyVersion = CurrentTopologyVersion;
            coordinateVersion = Mathf.Max(1, coordinateVersion);
            landformVersion = Mathf.Max(1, landformVersion);
            materialVersion = Mathf.Max(1, materialVersion);
            decorationVersion = Mathf.Max(1, decorationVersion);
            resourceVersion = Mathf.Max(1, resourceVersion);
            siteVersion = Mathf.Max(1, siteVersion);
            maximumCanyonPlans = Mathf.Max(9, maximumCanyonPlans);
            maximumTerrainWindows = Mathf.Max(1, maximumTerrainWindows);
            maximumRepresentationEntries = Mathf.Max(1, maximumRepresentationEntries);
            maximumRepresentationMegabytes = Mathf.Max(8, maximumRepresentationMegabytes);
            maximumRetainedBuffersPerResolution = Mathf.Max(1, maximumRetainedBuffersPerResolution);
            rebaseThreshold = Mathf.Max(128f, rebaseThreshold);
            localFrameRadius = Mathf.Max(rebaseThreshold * 2f + 1f, localFrameRadius);
        }
    }
}
