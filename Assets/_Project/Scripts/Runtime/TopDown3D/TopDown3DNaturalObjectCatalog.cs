using System;
using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public enum TopDown3DNaturalObjectLayer
    {
        GroundDetail,
        Scatter,
        Obstacle,
        FineGrayCluster
    }

    public enum TopDown3DNaturalObjectShape
    {
        Pebble,
        Shard,
        Slab,
        Boulder,
        Nodule
    }

    [Serializable]
    public sealed class TopDown3DNaturalObjectDefinition
    {
        [SerializeField] private string stableId = "natural-object";
        [SerializeField] private TopDown3DNaturalObjectLayer layer;
        [SerializeField] private TopDown3DNaturalObjectShape shape;
        [SerializeField, Min(0.01f)] private float weight = 1f;
        [SerializeField] private Vector2 uniformScaleRange = new Vector2(0.8f, 1.2f);
        [SerializeField] private Vector3 proportions = Vector3.one;
        [SerializeField, Range(0f, 1f)] private float sinkDepth = 0.08f;
        [SerializeField, Range(0f, 60f)] private float maximumTilt = 18f;
        [SerializeField, Min(0.01f)] private float footprintRadius = 0.4f;

        public string StableId => stableId;
        public TopDown3DNaturalObjectLayer Layer => layer;
        public TopDown3DNaturalObjectShape Shape => shape;
        public float Weight => Mathf.Max(0.01f, weight);
        public Vector2 UniformScaleRange => new Vector2(
            Mathf.Max(0.01f, Mathf.Min(uniformScaleRange.x, uniformScaleRange.y)),
            Mathf.Max(0.01f, Mathf.Max(uniformScaleRange.x, uniformScaleRange.y)));
        public Vector3 Proportions => new Vector3(
            Mathf.Max(0.01f, proportions.x),
            Mathf.Max(0.01f, proportions.y),
            Mathf.Max(0.01f, proportions.z));
        public float SinkDepth => Mathf.Clamp01(sinkDepth);
        public float MaximumTilt => Mathf.Clamp(maximumTilt, 0f, 60f);
        public float FootprintRadius => Mathf.Max(0.01f, footprintRadius);
    }

    [CreateAssetMenu(
        menuName = "Booter & BigARM/Top Down 3D/Natural Object Catalog",
        fileName = "TopDown3DNaturalObjectCatalog")]
    public sealed class TopDown3DNaturalObjectCatalog : ScriptableObject
    {
        [SerializeField] private List<TopDown3DNaturalObjectDefinition> definitions =
            new List<TopDown3DNaturalObjectDefinition>();

        public IReadOnlyList<TopDown3DNaturalObjectDefinition> Definitions => definitions;

        public bool HasLayer(TopDown3DNaturalObjectLayer layer)
        {
            for (var i = 0; i < definitions.Count; i++)
            {
                if (definitions[i] != null && definitions[i].Layer == layer)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
