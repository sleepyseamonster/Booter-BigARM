using System;

namespace BooterBigArm.TopDown3D
{
    public enum TopDown3DLoadBand { Comfortable, Loaded, Heavy, Rejected }

    public readonly struct TopDown3DBigArmLoadProfile
    {
        public TopDown3DBigArmLoadProfile(float totalMass, float leftMass, float rightMass, float recommendedMass, float hardMass)
        {
            TotalMass = totalMass; LeftMass = leftMass; RightMass = rightMass;
            RecommendedMass = recommendedMass; HardMass = hardMass;
            Utilization = recommendedMass > 0f ? totalMass / recommendedMass : 0f;
            Balance = totalMass <= 0.001f ? 1f : 1f - Math.Min(1f, Math.Abs(leftMass - rightMass) / totalMass);
            Band = totalMass > hardMass ? TopDown3DLoadBand.Rejected : totalMass > recommendedMass ? TopDown3DLoadBand.Heavy : totalMass > recommendedMass * 0.75f ? TopDown3DLoadBand.Loaded : TopDown3DLoadBand.Comfortable;
            SpeedMultiplier = Band == TopDown3DLoadBand.Heavy ? 0.82f : Band == TopDown3DLoadBand.Loaded ? 0.92f : 1f;
            AccelerationMultiplier = Band == TopDown3DLoadBand.Heavy ? 0.84f : Band == TopDown3DLoadBand.Loaded ? 0.94f : 1f;
        }
        public float TotalMass { get; }
        public float LeftMass { get; }
        public float RightMass { get; }
        public float RecommendedMass { get; }
        public float HardMass { get; }
        public float Utilization { get; }
        public float Balance { get; }
        public TopDown3DLoadBand Band { get; }
        public float SpeedMultiplier { get; }
        public float AccelerationMultiplier { get; }
    }
}
