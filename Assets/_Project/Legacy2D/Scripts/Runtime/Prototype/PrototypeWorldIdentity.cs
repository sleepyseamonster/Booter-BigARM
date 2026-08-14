using System;
using UnityEngine;

namespace BooterBigArm.Runtime
{
    [Serializable]
    public sealed class PrototypeWorldIdentity
    {
        [SerializeField] private int seed;
        [SerializeField] private int generationVersion;
        [SerializeField] private int centerChunkX;
        [SerializeField] private int centerChunkY;

        public int Seed => seed;
        public int GenerationVersion => generationVersion;
        public int CenterChunkX => centerChunkX;
        public int CenterChunkY => centerChunkY;

        public Vector2Int CenterChunk => new Vector2Int(centerChunkX, centerChunkY);

        public static PrototypeWorldIdentity Create(int seed, int generationVersion, Vector2Int centerChunk)
        {
            return new PrototypeWorldIdentity
            {
                seed = seed,
                generationVersion = generationVersion,
                centerChunkX = centerChunk.x,
                centerChunkY = centerChunk.y
            };
        }
    }
}
