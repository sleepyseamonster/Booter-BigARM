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
        [SerializeField, Range(1, 4)] private int immediateLoadRadius = 2;
        [SerializeField, Range(1, 8)] private int chunksBuiltPerFrame = 2;
        [SerializeField, Range(0, 3)] private int unloadPadding = 1;
        [SerializeField] private float baseHeight;
        [SerializeField, Min(0f)] private float heightAmplitude = 2.1f;
        [SerializeField, Min(0.0001f)] private float noiseFrequency = 0.028f;
        [SerializeField, Range(1, 6)] private int noiseOctaves = 3;
        [SerializeField, Min(1f)] private float noiseLacunarity = 2f;
        [SerializeField, Range(0.05f, 0.95f)] private float noisePersistence = 0.5f;
        [Header("Craggy Escarpments")]
        [SerializeField] private bool generateEscarpments = true;
        [SerializeField, Min(1)] private int escarpmentGenerationVersion = 1;
        [SerializeField, Min(24f)] private float escarpmentRegionSize = 54f;
        [SerializeField, Range(0f, 1f)] private float escarpmentRegionChance = 0.48f;
        [SerializeField, Min(2f)] private float escarpmentMinimumRadius = 7.5f;
        [SerializeField, Min(3f)] private float escarpmentMaximumRadius = 13.5f;
        [SerializeField, Range(0.25f, 0.75f)] private float escarpmentMinimumHeight = 0.48f;
        [SerializeField, Range(0.3f, 0.8f)] private float escarpmentMaximumHeight = 0.66f;
        [SerializeField, Range(0.35f, 1.5f)] private float escarpmentEdgeWidth = 0.92f;
        [SerializeField, Range(0f, 0.3f)] private float cragReliefAmplitude = 0.12f;
        [SerializeField, Min(0.01f)] private float cragReliefFrequency = 0.12f;
        [SerializeField, Range(16, 64)] private int escarpmentFaceSegments = 40;
        [SerializeField, Range(1, 6)] private int escarpmentColliderSegmentsPerRun = 3;
        [SerializeField, Range(0, 12)] private int propsPerChunk = 4;
        [Header("Natural Objects")]
        [SerializeField] private TopDown3DNaturalObjectCatalog naturalObjectCatalog;
        [SerializeField, Min(1)] private int naturalObjectGenerationVersion = 2;
        [SerializeField, Min(1)] private int physicalRockGenerationVersion = 1;
        [SerializeField, Range(0, 64)] private int scatterObjectsPerChunk = 22;
        [SerializeField, Range(0, 160)] private int groundDetailsPerChunk = 72;
        [SerializeField, Min(0.0001f)] private float clutterClusterFrequency = 0.035f;
        [SerializeField, Range(0f, 1f)] private float clutterClusterStrength = 0.7f;
        [SerializeField, Min(0.0001f)] private float rockAbundanceFrequency = 0.018f;
        [SerializeField, Range(0f, 1f)] private float rockAbundanceStrength = 0.95f;
        [FormerlySerializedAs("obstacleFormationChance")]
        [SerializeField, Range(0f, 1f)] private float largeToLargeChance = 0.32f;
        [SerializeField, Range(0f, 1f)] private float largeContinuationDecay = 0.5f;
        [FormerlySerializedAs("obstacleFormationMaximumMembers")]
        [SerializeField, Range(1, 8)] private int physicalFormationMaximumMembers = 5;
        [SerializeField, Range(0, 8)] private int physicalFormationMaximumDepth = 4;
        [SerializeField, Range(0f, 0.2f)] private float formationContactInset = 0.05f;
        [SerializeField, Range(0f, 0.5f)] private float massiveRocksPerChunk = 0.14f;
        [SerializeField, Min(0f)] private float massiveRockSpacing = 5.5f;
        [SerializeField, Range(1f, 60f)] private float maximumMassiveRockSlope = 32f;
        [SerializeField, Range(0f, 1f)] private float toweringToMassiveChance = 0.65f;
        [SerializeField, Range(0f, 1f)] private float massiveToLargeChance = 0.7f;
        [SerializeField, Range(0f, 0.25f)] private float landmarksPerChunk = 0.03f;
        [SerializeField, Min(0f)] private float landmarkSpacing = 8f;
        [SerializeField, Range(1f, 60f)] private float maximumLandmarkSlope = 28f;
        [SerializeField, Min(0f)] private float scatterSpacing = 0.18f;
        [SerializeField, Min(0f)] private float groundDetailSpacing = 0.04f;
        [SerializeField] private Material fineGrayClutterMaterial;
        [SerializeField, Range(0, 240)] private int fineGrayClutterPerChunk = 156;
        [SerializeField, Min(0.0001f)] private float fineGrayClusterFrequency = 0.065f;
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
        public int ImmediateLoadRadius => immediateLoadRadius;
        public int ChunksBuiltPerFrame => chunksBuiltPerFrame;
        public int UnloadPadding => unloadPadding;
        public float BaseHeight => baseHeight;
        public float HeightAmplitude => heightAmplitude;
        public float NoiseFrequency => noiseFrequency;
        public int NoiseOctaves => noiseOctaves;
        public float NoiseLacunarity => noiseLacunarity;
        public float NoisePersistence => noisePersistence;
        public bool GenerateEscarpments => generateEscarpments;
        public int EscarpmentGenerationVersion => escarpmentGenerationVersion;
        public float EscarpmentRegionSize => escarpmentRegionSize;
        public float EscarpmentRegionChance => escarpmentRegionChance;
        public float EscarpmentMinimumRadius => escarpmentMinimumRadius;
        public float EscarpmentMaximumRadius => escarpmentMaximumRadius;
        public float EscarpmentMinimumHeight => escarpmentMinimumHeight;
        public float EscarpmentMaximumHeight => escarpmentMaximumHeight;
        public float EscarpmentEdgeWidth => escarpmentEdgeWidth;
        public float CragReliefAmplitude => cragReliefAmplitude;
        public float CragReliefFrequency => cragReliefFrequency;
        public int EscarpmentFaceSegments => escarpmentFaceSegments;
        public int EscarpmentColliderSegmentsPerRun => escarpmentColliderSegmentsPerRun;
        public int PropsPerChunk => propsPerChunk;
        public TopDown3DNaturalObjectCatalog NaturalObjectCatalog => naturalObjectCatalog;
        public int NaturalObjectGenerationVersion => naturalObjectGenerationVersion;
        public int PhysicalRockGenerationVersion => physicalRockGenerationVersion;
        public int ScatterObjectsPerChunk => scatterObjectsPerChunk;
        public int GroundDetailsPerChunk => groundDetailsPerChunk;
        public float ClutterClusterFrequency => clutterClusterFrequency;
        public float ClutterClusterStrength => clutterClusterStrength;
        public float RockAbundanceFrequency => rockAbundanceFrequency;
        public float RockAbundanceStrength => rockAbundanceStrength;
        public float LargeToLargeChance => largeToLargeChance;
        public float LargeContinuationDecay => largeContinuationDecay;
        public int PhysicalFormationMaximumMembers => physicalFormationMaximumMembers;
        public int PhysicalFormationMaximumDepth => physicalFormationMaximumDepth;
        public float FormationContactInset => formationContactInset;
        public float MassiveRocksPerChunk => massiveRocksPerChunk;
        public float MassiveRockSpacing => massiveRockSpacing;
        public float MaximumMassiveRockSlope => maximumMassiveRockSlope;
        public float ToweringToMassiveChance => toweringToMassiveChance;
        public float MassiveToLargeChance => massiveToLargeChance;
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

        private void OnValidate()
        {
            chunkSize = Mathf.Max(4f, chunkSize);
            quadsPerAxis = Mathf.Clamp(quadsPerAxis, 2, 64);
            streamingRadius = Mathf.Clamp(streamingRadius, 1, 7);
            immediateLoadRadius = Mathf.Clamp(immediateLoadRadius, 1, streamingRadius);
            chunksBuiltPerFrame = Mathf.Clamp(chunksBuiltPerFrame, 1, 8);
            unloadPadding = Mathf.Clamp(unloadPadding, 0, 3);
            noiseFrequency = Mathf.Max(0.0001f, noiseFrequency);
            noiseOctaves = Mathf.Clamp(noiseOctaves, 1, 6);
            noiseLacunarity = Mathf.Max(1f, noiseLacunarity);
            noisePersistence = Mathf.Clamp(noisePersistence, 0.05f, 0.95f);
            escarpmentGenerationVersion = Mathf.Max(1, escarpmentGenerationVersion);
            escarpmentRegionSize = Mathf.Max(24f, escarpmentRegionSize);
            escarpmentRegionChance = Mathf.Clamp01(escarpmentRegionChance);
            escarpmentMinimumRadius = Mathf.Max(2f, escarpmentMinimumRadius);
            escarpmentMaximumRadius = Mathf.Max(escarpmentMinimumRadius, escarpmentMaximumRadius);
            escarpmentMinimumHeight = Mathf.Clamp(escarpmentMinimumHeight, 0.25f, 0.75f);
            escarpmentMaximumHeight = Mathf.Clamp(
                escarpmentMaximumHeight,
                escarpmentMinimumHeight,
                0.8f);
            escarpmentEdgeWidth = Mathf.Clamp(escarpmentEdgeWidth, 0.35f, 1.5f);
            cragReliefAmplitude = Mathf.Clamp(cragReliefAmplitude, 0f, 0.3f);
            cragReliefFrequency = Mathf.Max(0.01f, cragReliefFrequency);
            escarpmentFaceSegments = Mathf.Clamp(escarpmentFaceSegments, 16, 64);
            escarpmentColliderSegmentsPerRun = Mathf.Clamp(escarpmentColliderSegmentsPerRun, 1, 6);
            propsPerChunk = Mathf.Clamp(propsPerChunk, 0, 12);
            naturalObjectGenerationVersion = Mathf.Max(1, naturalObjectGenerationVersion);
            physicalRockGenerationVersion = Mathf.Max(1, physicalRockGenerationVersion);
            scatterObjectsPerChunk = Mathf.Clamp(scatterObjectsPerChunk, 0, 64);
            groundDetailsPerChunk = Mathf.Clamp(groundDetailsPerChunk, 0, 160);
            clutterClusterFrequency = Mathf.Max(0.0001f, clutterClusterFrequency);
            clutterClusterStrength = Mathf.Clamp01(clutterClusterStrength);
            rockAbundanceFrequency = Mathf.Max(0.0001f, rockAbundanceFrequency);
            rockAbundanceStrength = Mathf.Clamp01(rockAbundanceStrength);
            largeToLargeChance = Mathf.Clamp01(largeToLargeChance);
            largeContinuationDecay = Mathf.Clamp01(largeContinuationDecay);
            physicalFormationMaximumMembers = Mathf.Clamp(physicalFormationMaximumMembers, 1, 8);
            physicalFormationMaximumDepth = Mathf.Clamp(physicalFormationMaximumDepth, 0, 8);
            formationContactInset = Mathf.Clamp(formationContactInset, 0f, 0.2f);
            massiveRocksPerChunk = Mathf.Clamp(massiveRocksPerChunk, 0f, 0.5f);
            massiveRockSpacing = Mathf.Max(0f, massiveRockSpacing);
            maximumMassiveRockSlope = Mathf.Clamp(maximumMassiveRockSlope, 1f, 60f);
            toweringToMassiveChance = Mathf.Clamp01(toweringToMassiveChance);
            massiveToLargeChance = Mathf.Clamp01(massiveToLargeChance);
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
