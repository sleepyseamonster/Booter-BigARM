using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [CreateAssetMenu(menuName = "Booter & BigARM/Top Down 3D/World Settings", fileName = "TopDown3DWorldSettings")]
    public sealed class TopDown3DWorldSettings : ScriptableObject
    {
        [SerializeField] private int worldSeed = 24681357;
        [SerializeField, Min(4f)] private float chunkSize = 18f;
        [SerializeField, Range(2, 64)] private int quadsPerAxis = 12;
        [SerializeField, Range(1, 5)] private int streamingRadius = 4;
        [SerializeField, Range(1, 4)] private int immediateLoadRadius = 2;
        [SerializeField, Range(1, 8)] private int chunksBuiltPerFrame = 2;
        [SerializeField, Range(0, 3)] private int unloadPadding = 1;
        [SerializeField] private float baseHeight;
        [SerializeField, Min(0f)] private float heightAmplitude = 2.1f;
        [SerializeField, Min(0.0001f)] private float noiseFrequency = 0.028f;
        [SerializeField, Range(1, 6)] private int noiseOctaves = 3;
        [SerializeField, Min(1f)] private float noiseLacunarity = 2f;
        [SerializeField, Range(0.05f, 0.95f)] private float noisePersistence = 0.5f;
        [SerializeField, Range(0, 12)] private int propsPerChunk = 4;
        [Header("Natural Objects")]
        [SerializeField] private TopDown3DNaturalObjectCatalog naturalObjectCatalog;
        [SerializeField, Min(1)] private int naturalObjectGenerationVersion = 1;
        [SerializeField, Range(0, 64)] private int scatterObjectsPerChunk = 22;
        [SerializeField, Range(0, 160)] private int groundDetailsPerChunk = 72;
        [SerializeField, Min(0.0001f)] private float clutterClusterFrequency = 0.035f;
        [SerializeField, Range(0f, 1f)] private float clutterClusterStrength = 0.7f;
        [SerializeField, Min(0f)] private float scatterSpacing = 0.18f;
        [SerializeField, Min(0f)] private float groundDetailSpacing = 0.04f;
        [SerializeField] private Material fineGrayClutterMaterial;
        [SerializeField, Range(0, 240)] private int fineGrayClutterPerChunk = 118;
        [SerializeField, Min(0.0001f)] private float fineGrayClusterFrequency = 0.085f;
        [SerializeField, Range(0f, 1f)] private float fineGrayClusterStrength = 0.95f;
        [SerializeField, Min(0f)] private float fineGrayClutterSpacing = 0.012f;
        [SerializeField, Range(1f, 60f)] private float maximumClutterSlope = 44f;
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
        public int PropsPerChunk => propsPerChunk;
        public TopDown3DNaturalObjectCatalog NaturalObjectCatalog => naturalObjectCatalog;
        public int NaturalObjectGenerationVersion => naturalObjectGenerationVersion;
        public int ScatterObjectsPerChunk => scatterObjectsPerChunk;
        public int GroundDetailsPerChunk => groundDetailsPerChunk;
        public float ClutterClusterFrequency => clutterClusterFrequency;
        public float ClutterClusterStrength => clutterClusterStrength;
        public float ScatterSpacing => scatterSpacing;
        public float GroundDetailSpacing => groundDetailSpacing;
        public Material FineGrayClutterMaterial => fineGrayClutterMaterial;
        public int FineGrayClutterPerChunk => fineGrayClutterPerChunk;
        public float FineGrayClusterFrequency => fineGrayClusterFrequency;
        public float FineGrayClusterStrength => fineGrayClusterStrength;
        public float FineGrayClutterSpacing => fineGrayClutterSpacing;
        public float MaximumClutterSlope => maximumClutterSlope;
        public float ClearSpawnRadius => clearSpawnRadius;
        public float SafeSpawnSearchRadius => safeSpawnSearchRadius;
        public float SafeSpawnSearchStep => safeSpawnSearchStep;
        public float MaximumSafeSpawnSlope => maximumSafeSpawnSlope;
        public float MaximumPropSlope => maximumPropSlope;
        public float PropSpacing => propSpacing;
        public int PropPlacementAttempts => propPlacementAttempts;

        public void ConfigureNaturalObjectAssets(
            TopDown3DNaturalObjectCatalog catalog,
            Material grayClutterMaterial)
        {
            naturalObjectCatalog = catalog;
            fineGrayClutterMaterial = grayClutterMaterial;
        }

        private void OnValidate()
        {
            chunkSize = Mathf.Max(4f, chunkSize);
            quadsPerAxis = Mathf.Clamp(quadsPerAxis, 2, 64);
            streamingRadius = Mathf.Clamp(streamingRadius, 1, 5);
            immediateLoadRadius = Mathf.Clamp(immediateLoadRadius, 1, streamingRadius);
            chunksBuiltPerFrame = Mathf.Clamp(chunksBuiltPerFrame, 1, 8);
            unloadPadding = Mathf.Clamp(unloadPadding, 0, 3);
            noiseFrequency = Mathf.Max(0.0001f, noiseFrequency);
            noiseOctaves = Mathf.Clamp(noiseOctaves, 1, 6);
            noiseLacunarity = Mathf.Max(1f, noiseLacunarity);
            noisePersistence = Mathf.Clamp(noisePersistence, 0.05f, 0.95f);
            propsPerChunk = Mathf.Clamp(propsPerChunk, 0, 12);
            naturalObjectGenerationVersion = Mathf.Max(1, naturalObjectGenerationVersion);
            scatterObjectsPerChunk = Mathf.Clamp(scatterObjectsPerChunk, 0, 64);
            groundDetailsPerChunk = Mathf.Clamp(groundDetailsPerChunk, 0, 160);
            clutterClusterFrequency = Mathf.Max(0.0001f, clutterClusterFrequency);
            clutterClusterStrength = Mathf.Clamp01(clutterClusterStrength);
            scatterSpacing = Mathf.Max(0f, scatterSpacing);
            groundDetailSpacing = Mathf.Max(0f, groundDetailSpacing);
            fineGrayClutterPerChunk = Mathf.Clamp(fineGrayClutterPerChunk, 0, 240);
            fineGrayClusterFrequency = Mathf.Max(0.0001f, fineGrayClusterFrequency);
            fineGrayClusterStrength = Mathf.Clamp01(fineGrayClusterStrength);
            fineGrayClutterSpacing = Mathf.Max(0f, fineGrayClutterSpacing);
            maximumClutterSlope = Mathf.Clamp(maximumClutterSlope, 1f, 60f);
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
