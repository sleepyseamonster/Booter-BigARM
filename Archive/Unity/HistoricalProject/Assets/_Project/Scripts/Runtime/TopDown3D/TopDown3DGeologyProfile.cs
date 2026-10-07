using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [Serializable]
    public sealed class TopDown3DGeologyProfile
    {
        [SerializeField, Min(72f)] private float regionSize = 288f;
        [SerializeField, Min(0f)] private float basinRelief = 14f;
        [SerializeField, Min(0f)] private float ridgeRelief = 9f;
        [SerializeField, Min(0f)] private float mesaRelief = 12f;
        [SerializeField, Min(0f)] private float drainageDepth = 5.5f;
        [SerializeField, Min(0f)] private float terraceStepHeight = 2.4f;
        [SerializeField, Range(0f, 1f)] private float terraceStrength = 0.38f;
        [SerializeField, Min(0f)] private float localRelief = 1.35f;
        [SerializeField, Range(0.01f, 0.3f)] private float drainageWidth = 0.105f;
        [SerializeField, Range(0f, 1f)] private float corridorWidth = 0.48f;
        [SerializeField, Range(0f, 1f)] private float talusSlopeStart = 0.28f;
        [SerializeField, Range(0f, 1f)] private float talusSlopeEnd = 0.72f;

        public float RegionSize => Mathf.Max(72f, regionSize);
        public float BasinRelief => Mathf.Max(0f, basinRelief);
        public float RidgeRelief => Mathf.Max(0f, ridgeRelief);
        public float MesaRelief => Mathf.Max(0f, mesaRelief);
        public float DrainageDepth => Mathf.Max(0f, drainageDepth);
        public float TerraceStepHeight => Mathf.Max(0f, terraceStepHeight);
        public float TerraceStrength => Mathf.Clamp01(terraceStrength);
        public float LocalRelief => Mathf.Max(0f, localRelief);
        public float DrainageWidth => Mathf.Clamp(drainageWidth, 0.01f, 0.3f);
        public float CorridorWidth => Mathf.Clamp01(corridorWidth);
        public float TalusSlopeStart => Mathf.Clamp01(talusSlopeStart);
        public float TalusSlopeEnd => Mathf.Max(TalusSlopeStart + 0.01f, Mathf.Clamp01(talusSlopeEnd));
    }
}
