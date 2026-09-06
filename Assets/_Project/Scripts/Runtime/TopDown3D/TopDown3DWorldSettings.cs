using UnityEngine;
using UnityEngine.Serialization;

namespace BooterBigArm.TopDown3D
{
    [CreateAssetMenu(menuName = "Booter & BigARM/Top Down 3D/World Settings", fileName = "TopDown3DWorldSettings")]
    public sealed class TopDown3DWorldSettings : ScriptableObject
    {
        [SerializeField] private int worldSeed = 24681357;
        [SerializeField, Min(4f)] private float chunkSize = 18f;
        [SerializeField, Range(2, 64)] private int quadsPerAxis = 24;
        [SerializeField, Range(1, 7)] private int streamingRadius = 7;
        [SerializeField, Range(1, 7)] private int decorationStreamingRadius = 3;
        [SerializeField, Range(1, 4)] private int immediateLoadRadius = 2;
        [SerializeField, Range(1, 8)] private int chunksBuiltPerFrame = 2;
        [SerializeField, Range(0, 3)] private int unloadPadding = 1;
        [Header("Geological World")]
        [SerializeField, Min(1)] private int terrainGenerationVersion = 3;
        [SerializeField] private TopDown3DGeologyProfile geologyProfile = new TopDown3DGeologyProfile();
        [SerializeField] private float baseHeight;
        [SerializeField, Range(0f, 4f)] private float propsPerChunk = 1.7f;
        [Header("Natural Objects")]
        [SerializeField] private TopDown3DNaturalObjectCatalog naturalObjectCatalog;
        [SerializeField, Min(1)] private int naturalObjectGenerationVersion = 3;
        [SerializeField, Min(1)] private int physicalRockGenerationVersion = 7;
        [Header("Physical Rock Placement")]
        [SerializeField, Range(0f, 0.3f)]
        [Tooltip(
            "How far complete rock formations sit into the terrain. "
            + "Each formation varies automatically while all of its rocks stay together.")]
        private float physicalRockAdditionalBurialFraction = 0.12f;
        [Header("Interactive Resources")]
        [SerializeField] private TopDown3DResourceCatalog resourceCatalog;
        [SerializeField, Min(1)] private int resourceGenerationVersion = 1;
        [SerializeField, Range(0, 64)] private int scatterObjectsPerChunk = 30;
        [SerializeField, Range(0, 160)] private int groundDetailsPerChunk = 72;
        [SerializeField, Min(0.0001f)] private float clutterClusterFrequency = 0.08f;
        [SerializeField, Range(0f, 1f)] private float clutterClusterStrength = 0.9f;
        [SerializeField, Min(0.0001f)] private float rockAbundanceFrequency = 0.035f;
        [SerializeField, Range(0f, 1f)] private float rockAbundanceStrength = 1f;
        [SerializeField, Range(0f, 8f)] private float smallRocksPerChunk = 2.4f;
        [SerializeField, Min(0f)] private float smallRockSpacing = 0.2f;
        [SerializeField, Range(1f, 60f)] private float maximumSmallRockSlope = 44f;
        [SerializeField, Range(0f, 6f)] private float mediumRocksPerChunk = 1.9f;
        [SerializeField, Min(0f)] private float mediumRockSpacing = 0.4f;
        [SerializeField, Range(1f, 60f)] private float maximumMediumRockSlope = 42f;
        [SerializeField, Range(0f, 2f)] private float extraLargeRocksPerChunk = 0.86f;
        [SerializeField, Min(0f)] private float extraLargeRockSpacing = 3f;
        [SerializeField, Range(1f, 60f)] private float maximumExtraLargeRockSlope = 35f;
        [FormerlySerializedAs("obstacleFormationChance")]
        [FormerlySerializedAs("largeToLargeChance")]
        [SerializeField, Range(0f, 1f)] private float largeToMediumChance = 0.84f;
        [FormerlySerializedAs("largeContinuationDecay")]
        [SerializeField, Range(0f, 1f)] private float additionalChildChanceMultiplier = 0.7f;
        [SerializeField, Range(1, 3)] private int formationMaximumChildrenPerParent = 3;
        [FormerlySerializedAs("obstacleFormationMaximumMembers")]
        [SerializeField, Range(1, 16)] private int physicalFormationMaximumMembers = 12;
        [SerializeField, Range(0, 8)] private int physicalFormationMaximumDepth = 5;
        [SerializeField, Range(0.15f, 0.85f)] private float formationMinimumParentDistanceRatio = 0.32f;
        [SerializeField, Range(0.55f, 1f)] private float formationMaximumParentDistanceRatio = 0.96f;
        [SerializeField, Range(0f, 0.5f)] private float massiveRocksPerChunk = 0.42f;
        [SerializeField, Min(0f)] private float massiveRockSpacing = 5.5f;
        [SerializeField, Range(1f, 60f)] private float maximumMassiveRockSlope = 32f;
        [SerializeField, Range(0f, 1f)] private float toweringToMassiveChance = 0.92f;
        [FormerlySerializedAs("massiveToLargeChance")]
        [SerializeField, Range(0f, 1f)] private float massiveToExtraLargeChance = 0.88f;
        [SerializeField, Range(0f, 1f)] private float extraLargeToLargeChance = 0.86f;
        [SerializeField, Range(0f, 1f)] private float mediumToSmallChance = 0.78f;
        [SerializeField, Range(0f, 0.25f)] private float landmarksPerChunk = 0.19f;
        [SerializeField, Min(0f)] private float landmarkSpacing = 8f;
        [SerializeField, Range(1f, 60f)] private float maximumLandmarkSlope = 28f;
        [SerializeField, Min(0f)] private float scatterSpacing = 0.18f;
        [SerializeField, Min(0f)] private float groundDetailSpacing = 0.04f;
        [SerializeField] private Material fineGrayClutterMaterial;
        [SerializeField, Range(0, 240)] private int fineGrayClutterPerChunk = 156;
        [SerializeField, Min(0.0001f)] private float fineGrayClusterFrequency = 0.12f;
        [SerializeField, Range(0f, 1f)] private float fineGrayClusterStrength = 1f;
        [SerializeField, Min(0f)] private float fineGrayClutterSpacing = 0.006f;
        [Header("Rock Surface Clusters")]
        [SerializeField] private Material darkRockMaterial;
        [SerializeField] private Material tealRockMaterial;
        [SerializeField, Min(0.0001f)] private float rockSurfaceClusterFrequency = 0.04f;
        [SerializeField, Range(0.05f, 0.45f)] private float darkRockSurfaceThreshold = 0.4f;
        [SerializeField, Range(0.55f, 0.95f)] private float tealRockSurfaceThreshold = 0.74f;
        [SerializeField, Range(1f, 60f)] private float maximumClutterSlope = 44f;
        [Header("Deposited Dust")]
        [SerializeField] private bool generateDepositedDust = true;
        [SerializeField] private Material depositedDustMaterial;
        [SerializeField, Min(1)] private int dustDepositionGenerationVersion = 1;
        [SerializeField, Range(8, 40)] private int dustOverlayQuadsPerAxis = 40;
        [SerializeField, Range(0f, 360f)] private float prevailingWindDegrees = 32f;
        [SerializeField, Min(0.0001f)] private float dustPocketFrequency = 0.012f;
        [SerializeField, Min(0.0001f)] private float dustWindrowFrequency = 0.09f;
        [SerializeField, Range(0.3f, 0.85f)] private float dustCoverageThreshold = 0.58f;
        [SerializeField, Range(0.01f, 0.5f)] private float dustMaximumBaseHeight = 0.16f;
        [SerializeField, Range(1f, 16f)] private float dustWakeLength = 2.8f;
        [SerializeField, Range(0.5f, 3f)] private float dustWakeWidthMultiplier = 1.15f;
        [SerializeField, Range(0.05f, 0.8f)] private float dustMaximumWakeHeight = 0.28f;
        [SerializeField, Range(1f, 60f)] private float maximumDustDepositionSlope = 26f;
        [SerializeField, Range(0.002f, 0.08f)] private float dustSurfaceOffset = 0.018f;
        [SerializeField, Min(0f)] private float clearSpawnRadius = 7f;
        [SerializeField, Min(0.25f)] private float safeSpawnSearchRadius = 8f;
        [SerializeField, Min(0.25f)] private float safeSpawnSearchStep = 1.5f;
        [SerializeField, Range(1f, 60f)] private float maximumSafeSpawnSlope = 38f;
        [SerializeField, Range(1f, 60f)] private float maximumPropSlope = 38f;
        [SerializeField, Min(0f)] private float propSpacing = 0.6f;
        [SerializeField, Range(1, 24)] private int propPlacementAttempts = 10;

        public int WorldSeed => worldSeed;
        public float ChunkSize => chunkSize;
        public int QuadsPerAxis => quadsPerAxis;
        public int StreamingRadius => streamingRadius;
        public int DecorationStreamingRadius => decorationStreamingRadius;
        public int ImmediateLoadRadius => immediateLoadRadius;
        public int ChunksBuiltPerFrame => chunksBuiltPerFrame;
        public int UnloadPadding => unloadPadding;
        public int TerrainGenerationVersion => terrainGenerationVersion;
        public TopDown3DGeologyProfile GeologyProfile => geologyProfile;
        public float BaseHeight => baseHeight;
        public float PropsPerChunk => propsPerChunk;
        public TopDown3DNaturalObjectCatalog NaturalObjectCatalog => naturalObjectCatalog;
        public int NaturalObjectGenerationVersion => naturalObjectGenerationVersion;
        public int PhysicalRockGenerationVersion => physicalRockGenerationVersion;
        public float PhysicalRockAdditionalBurialFraction => physicalRockAdditionalBurialFraction;
        public TopDown3DResourceCatalog ResourceCatalog => resourceCatalog;
        public int ResourceGenerationVersion => resourceGenerationVersion;
        public int ScatterObjectsPerChunk => scatterObjectsPerChunk;
        public int GroundDetailsPerChunk => groundDetailsPerChunk;
        public float ClutterClusterFrequency => clutterClusterFrequency;
        public float ClutterClusterStrength => clutterClusterStrength;
        public float RockAbundanceFrequency => rockAbundanceFrequency;
        public float RockAbundanceStrength => rockAbundanceStrength;
        public float SmallRocksPerChunk => smallRocksPerChunk;
        public float SmallRockSpacing => smallRockSpacing;
        public float MaximumSmallRockSlope => maximumSmallRockSlope;
        public float MediumRocksPerChunk => mediumRocksPerChunk;
        public float MediumRockSpacing => mediumRockSpacing;
        public float MaximumMediumRockSlope => maximumMediumRockSlope;
        public float ExtraLargeRocksPerChunk => extraLargeRocksPerChunk;
        public float ExtraLargeRockSpacing => extraLargeRockSpacing;
        public float MaximumExtraLargeRockSlope => maximumExtraLargeRockSlope;
        public float LargeToMediumChance => largeToMediumChance;
        public float AdditionalChildChanceMultiplier => additionalChildChanceMultiplier;
        public int FormationMaximumChildrenPerParent => formationMaximumChildrenPerParent;
        public int PhysicalFormationMaximumMembers => physicalFormationMaximumMembers;
        public int PhysicalFormationMaximumDepth => physicalFormationMaximumDepth;
        public float FormationMinimumParentDistanceRatio => formationMinimumParentDistanceRatio;
        public float FormationMaximumParentDistanceRatio => formationMaximumParentDistanceRatio;
        public float MassiveRocksPerChunk => massiveRocksPerChunk;
        public float MassiveRockSpacing => massiveRockSpacing;
        public float MaximumMassiveRockSlope => maximumMassiveRockSlope;
        public float ToweringToMassiveChance => toweringToMassiveChance;
        public float MassiveToExtraLargeChance => massiveToExtraLargeChance;
        public float ExtraLargeToLargeChance => extraLargeToLargeChance;
        public float MediumToSmallChance => mediumToSmallChance;
        public float LandmarksPerChunk => landmarksPerChunk;
        public float LandmarkSpacing => landmarkSpacing;
        public float MaximumLandmarkSlope => maximumLandmarkSlope;
        public float ScatterSpacing => scatterSpacing;
        public float GroundDetailSpacing => groundDetailSpacing;
        public Material FineGrayClutterMaterial => fineGrayClutterMaterial;
        public int FineGrayClutterPerChunk => fineGrayClutterPerChunk;
        public float FineGrayClusterFrequency => fineGrayClusterFrequency;
        public float FineGrayClusterStrength => fineGrayClusterStrength;
        public float FineGrayClutterSpacing => fineGrayClutterSpacing;
        public Material DarkRockMaterial => darkRockMaterial;
        public Material TealRockMaterial => tealRockMaterial;
        public float RockSurfaceClusterFrequency => rockSurfaceClusterFrequency;
        public float DarkRockSurfaceThreshold => darkRockSurfaceThreshold;
        public float TealRockSurfaceThreshold => tealRockSurfaceThreshold;
        public float MaximumClutterSlope => maximumClutterSlope;
        public bool GenerateDepositedDust => generateDepositedDust;
        public Material DepositedDustMaterial => depositedDustMaterial;
        public int DustDepositionGenerationVersion => dustDepositionGenerationVersion;
        public int DustOverlayQuadsPerAxis => dustOverlayQuadsPerAxis;
        public float PrevailingWindDegrees => prevailingWindDegrees;
        public float DustPocketFrequency => dustPocketFrequency;
        public float DustWindrowFrequency => dustWindrowFrequency;
        public float DustCoverageThreshold => dustCoverageThreshold;
        public float DustMaximumBaseHeight => dustMaximumBaseHeight;
        public float DustWakeLength => dustWakeLength;
        public float DustWakeWidthMultiplier => dustWakeWidthMultiplier;
        public float DustMaximumWakeHeight => dustMaximumWakeHeight;
        public float MaximumDustDepositionSlope => maximumDustDepositionSlope;
        public float DustSurfaceOffset => dustSurfaceOffset;
        public float ClearSpawnRadius => clearSpawnRadius;
        public float SafeSpawnSearchRadius => safeSpawnSearchRadius;
        public float SafeSpawnSearchStep => safeSpawnSearchStep;
        public float MaximumSafeSpawnSlope => maximumSafeSpawnSlope;
        public float MaximumPropSlope => maximumPropSlope;
        public float PropSpacing => propSpacing;
        public int PropPlacementAttempts => propPlacementAttempts;

        public void ConfigureNaturalObjectAssets(
            TopDown3DNaturalObjectCatalog catalog,
            Material grayClutterMaterial,
            Material darkSurfaceMaterial,
            Material tealSurfaceMaterial)
        {
            naturalObjectCatalog = catalog;
            fineGrayClutterMaterial = grayClutterMaterial;
            darkRockMaterial = darkSurfaceMaterial;
            tealRockMaterial = tealSurfaceMaterial;
        }

        public void ConfigureResourceAssets(
            TopDown3DResourceCatalog catalog,
            int generationVersion = 1)
        {
            resourceCatalog = catalog;
            resourceGenerationVersion = Mathf.Max(1, generationVersion);
        }

        private void OnValidate()
        {
            chunkSize = Mathf.Max(4f, chunkSize);
            quadsPerAxis = Mathf.Clamp(quadsPerAxis, 2, 64);
            streamingRadius = Mathf.Clamp(streamingRadius, 1, 7);
            decorationStreamingRadius = Mathf.Clamp(decorationStreamingRadius, 1, streamingRadius);
            immediateLoadRadius = Mathf.Clamp(immediateLoadRadius, 1, streamingRadius);
            chunksBuiltPerFrame = Mathf.Clamp(chunksBuiltPerFrame, 1, 8);
            unloadPadding = Mathf.Clamp(unloadPadding, 0, 3);
            terrainGenerationVersion = Mathf.Max(1, terrainGenerationVersion);
            geologyProfile ??= new TopDown3DGeologyProfile();
            propsPerChunk = Mathf.Clamp(propsPerChunk, 0f, 4f);
            naturalObjectGenerationVersion = Mathf.Max(1, naturalObjectGenerationVersion);
            physicalRockGenerationVersion = Mathf.Max(1, physicalRockGenerationVersion);
            physicalRockAdditionalBurialFraction = Mathf.Clamp(
                physicalRockAdditionalBurialFraction,
                0f,
                0.3f);
            resourceGenerationVersion = Mathf.Max(1, resourceGenerationVersion);
            scatterObjectsPerChunk = Mathf.Clamp(scatterObjectsPerChunk, 0, 64);
            groundDetailsPerChunk = Mathf.Clamp(groundDetailsPerChunk, 0, 160);
            clutterClusterFrequency = Mathf.Max(0.0001f, clutterClusterFrequency);
            clutterClusterStrength = Mathf.Clamp01(clutterClusterStrength);
            rockAbundanceFrequency = Mathf.Max(0.0001f, rockAbundanceFrequency);
            rockAbundanceStrength = Mathf.Clamp01(rockAbundanceStrength);
            smallRocksPerChunk = Mathf.Clamp(smallRocksPerChunk, 0f, 8f);
            smallRockSpacing = Mathf.Max(0f, smallRockSpacing);
            maximumSmallRockSlope = Mathf.Clamp(maximumSmallRockSlope, 1f, 60f);
            mediumRocksPerChunk = Mathf.Clamp(mediumRocksPerChunk, 0f, 6f);
            mediumRockSpacing = Mathf.Max(0f, mediumRockSpacing);
            maximumMediumRockSlope = Mathf.Clamp(maximumMediumRockSlope, 1f, 60f);
            extraLargeRocksPerChunk = Mathf.Clamp(extraLargeRocksPerChunk, 0f, 2f);
            extraLargeRockSpacing = Mathf.Max(0f, extraLargeRockSpacing);
            maximumExtraLargeRockSlope = Mathf.Clamp(maximumExtraLargeRockSlope, 1f, 60f);
            largeToMediumChance = Mathf.Clamp01(largeToMediumChance);
            additionalChildChanceMultiplier = Mathf.Clamp01(additionalChildChanceMultiplier);
            formationMaximumChildrenPerParent = Mathf.Clamp(formationMaximumChildrenPerParent, 1, 3);
            physicalFormationMaximumMembers = Mathf.Clamp(physicalFormationMaximumMembers, 1, 16);
            physicalFormationMaximumDepth = Mathf.Clamp(physicalFormationMaximumDepth, 0, 8);
            formationMinimumParentDistanceRatio = Mathf.Clamp(
                formationMinimumParentDistanceRatio,
                0.15f,
                0.85f);
            formationMaximumParentDistanceRatio = Mathf.Clamp(
                formationMaximumParentDistanceRatio,
                Mathf.Max(0.55f, formationMinimumParentDistanceRatio),
                1f);
            massiveRocksPerChunk = Mathf.Clamp(massiveRocksPerChunk, 0f, 0.5f);
            massiveRockSpacing = Mathf.Max(0f, massiveRockSpacing);
            maximumMassiveRockSlope = Mathf.Clamp(maximumMassiveRockSlope, 1f, 60f);
            toweringToMassiveChance = Mathf.Clamp01(toweringToMassiveChance);
            massiveToExtraLargeChance = Mathf.Clamp01(massiveToExtraLargeChance);
            extraLargeToLargeChance = Mathf.Clamp01(extraLargeToLargeChance);
            mediumToSmallChance = Mathf.Clamp01(mediumToSmallChance);
            landmarksPerChunk = Mathf.Clamp(landmarksPerChunk, 0f, 0.25f);
            landmarkSpacing = Mathf.Max(0f, landmarkSpacing);
            maximumLandmarkSlope = Mathf.Clamp(maximumLandmarkSlope, 1f, 60f);
            scatterSpacing = Mathf.Max(0f, scatterSpacing);
            groundDetailSpacing = Mathf.Max(0f, groundDetailSpacing);
            fineGrayClutterPerChunk = Mathf.Clamp(fineGrayClutterPerChunk, 0, 240);
            fineGrayClusterFrequency = Mathf.Max(0.0001f, fineGrayClusterFrequency);
            fineGrayClusterStrength = Mathf.Clamp01(fineGrayClusterStrength);
            fineGrayClutterSpacing = Mathf.Max(0f, fineGrayClutterSpacing);
            rockSurfaceClusterFrequency = Mathf.Max(0.0001f, rockSurfaceClusterFrequency);
            darkRockSurfaceThreshold = Mathf.Clamp(darkRockSurfaceThreshold, 0.05f, 0.45f);
            tealRockSurfaceThreshold = Mathf.Clamp(tealRockSurfaceThreshold, 0.55f, 0.95f);
            maximumClutterSlope = Mathf.Clamp(maximumClutterSlope, 1f, 60f);
            dustDepositionGenerationVersion = Mathf.Max(1, dustDepositionGenerationVersion);
            dustOverlayQuadsPerAxis = Mathf.Clamp(dustOverlayQuadsPerAxis, 8, 40);
            prevailingWindDegrees = Mathf.Repeat(prevailingWindDegrees, 360f);
            dustPocketFrequency = Mathf.Max(0.0001f, dustPocketFrequency);
            dustWindrowFrequency = Mathf.Max(0.0001f, dustWindrowFrequency);
            dustCoverageThreshold = Mathf.Clamp(dustCoverageThreshold, 0.3f, 0.85f);
            dustMaximumBaseHeight = Mathf.Clamp(dustMaximumBaseHeight, 0.01f, 0.5f);
            dustWakeLength = Mathf.Clamp(dustWakeLength, 1f, 16f);
            dustWakeWidthMultiplier = Mathf.Clamp(dustWakeWidthMultiplier, 0.5f, 3f);
            dustMaximumWakeHeight = Mathf.Clamp(dustMaximumWakeHeight, 0.05f, 0.8f);
            maximumDustDepositionSlope = Mathf.Clamp(maximumDustDepositionSlope, 1f, 60f);
            dustSurfaceOffset = Mathf.Clamp(dustSurfaceOffset, 0.002f, 0.08f);
            clearSpawnRadius = Mathf.Max(0f, clearSpawnRadius);
            safeSpawnSearchRadius = Mathf.Max(0.25f, safeSpawnSearchRadius);
            safeSpawnSearchStep = Mathf.Max(0.25f, safeSpawnSearchStep);
            maximumSafeSpawnSlope = Mathf.Clamp(maximumSafeSpawnSlope, 1f, 60f);
            maximumPropSlope = Mathf.Clamp(maximumPropSlope, 1f, 60f);
            propSpacing = Mathf.Max(0f, propSpacing);
            propPlacementAttempts = Mathf.Clamp(propPlacementAttempts, 1, 24);
        }
    }
}
