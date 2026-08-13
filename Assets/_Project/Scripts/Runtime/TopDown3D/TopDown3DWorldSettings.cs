using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [CreateAssetMenu(menuName = "Booter & BigARM/Top Down 3D/World Settings", fileName = "TopDown3DWorldSettings")]
    public sealed class TopDown3DWorldSettings : ScriptableObject
    {
        [SerializeField] private int worldSeed = 24681357;
        [SerializeField, Min(4f)] private float chunkSize = 18f;
        [SerializeField, Range(2, 64)] private int quadsPerAxis = 12;
        [SerializeField, Range(1, 5)] private int streamingRadius = 2;
        [SerializeField] private float baseHeight;
        [SerializeField, Min(0f)] private float heightAmplitude = 2.1f;
        [SerializeField, Min(0.0001f)] private float noiseFrequency = 0.028f;
        [SerializeField, Range(1, 6)] private int noiseOctaves = 3;
        [SerializeField, Min(1f)] private float noiseLacunarity = 2f;
        [SerializeField, Range(0.05f, 0.95f)] private float noisePersistence = 0.5f;
        [SerializeField, Range(0, 12)] private int propsPerChunk = 4;
        [SerializeField, Min(0f)] private float clearSpawnRadius = 7f;

        public int WorldSeed => worldSeed;
        public float ChunkSize => chunkSize;
        public int QuadsPerAxis => quadsPerAxis;
        public int StreamingRadius => streamingRadius;
        public float BaseHeight => baseHeight;
        public float HeightAmplitude => heightAmplitude;
        public float NoiseFrequency => noiseFrequency;
        public int NoiseOctaves => noiseOctaves;
        public float NoiseLacunarity => noiseLacunarity;
        public float NoisePersistence => noisePersistence;
        public int PropsPerChunk => propsPerChunk;
        public float ClearSpawnRadius => clearSpawnRadius;

        private void OnValidate()
        {
            chunkSize = Mathf.Max(4f, chunkSize);
            quadsPerAxis = Mathf.Clamp(quadsPerAxis, 2, 64);
            streamingRadius = Mathf.Clamp(streamingRadius, 1, 5);
            noiseFrequency = Mathf.Max(0.0001f, noiseFrequency);
            noiseOctaves = Mathf.Clamp(noiseOctaves, 1, 6);
            noiseLacunarity = Mathf.Max(1f, noiseLacunarity);
            noisePersistence = Mathf.Clamp(noisePersistence, 0.05f, 0.95f);
            propsPerChunk = Mathf.Clamp(propsPerChunk, 0, 12);
            clearSpawnRadius = Mathf.Max(0f, clearSpawnRadius);
        }
    }
}
