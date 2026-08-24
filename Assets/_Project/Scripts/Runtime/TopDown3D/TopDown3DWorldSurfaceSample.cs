using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public readonly struct TopDown3DWorldSurfaceSample
    {
        public TopDown3DWorldSurfaceSample(
            float height,
            Vector3 normal,
            int featureId,
            float sandWeight,
            float gravelWeight,
            float bedrockWeight,
            float depositWeight,
            float flow,
            float curvature,
            float talus,
            float lithology,
            float weathering,
            float traversalCorridor)
        {
            Height = height;
            Normal = normal;
            FeatureId = featureId;
            SandWeight = sandWeight;
            GravelWeight = gravelWeight;
            BedrockWeight = bedrockWeight;
            DepositWeight = depositWeight;
            Flow = flow;
            Curvature = curvature;
            Talus = talus;
            Lithology = lithology;
            Weathering = weathering;
            TraversalCorridor = traversalCorridor;
        }

        public float Height { get; }
        public Vector3 Normal { get; }
        public int FeatureId { get; }
        public float SandWeight { get; }
        public float GravelWeight { get; }
        public float BedrockWeight { get; }
        public float DepositWeight { get; }
        public float Flow { get; }
        public float Curvature { get; }
        public float Talus { get; }
        public float Lithology { get; }
        public float Weathering { get; }
        public float TraversalCorridor { get; }

        public Color ToVertexColor()
        {
            return new Color(SandWeight, GravelWeight, BedrockWeight, Weathering);
        }
    }
}
