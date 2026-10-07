using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [CreateAssetMenu(
        menuName = "Booter & BigARM/Top Down 3D/Resource Definition",
        fileName = "TopDown3DResourceDefinition")]
    public sealed class TopDown3DResourceDefinition : ScriptableObject
    {
        [SerializeField] private string resourceId;
        [SerializeField] private string meshFamilyStableId;
        [SerializeField] private TopDown3DNaturalObjectShape meshShape = TopDown3DNaturalObjectShape.Outcrop;
        [SerializeField, Range(0, TopDown3DNaturalObjectCatalog.MeshVariantsPerShape - 1)] private int meshVariant;
        [SerializeField] private Material activeMaterial;
        [SerializeField] private Material depletedMaterial;
        [SerializeField] private TopDown3DItemDefinition yieldedItem;
        [SerializeField, Min(1)] private int yieldQuantity = 1;
        [SerializeField, Min(1)] private int maximumUses = 1;
        [SerializeField, Min(0.25f)] private float interactionRange = 2.2f;
        [SerializeField, Min(0.1f)] private float actionDuration = 1.1f;
        [SerializeField] private string prompt = "Gather Ironstone";
        [Header("Deterministic Placement")]
        [SerializeField, Range(0f, 4f)] private float targetPerChunk = 0.35f;
        [SerializeField, Min(2f)] private float globalCellSize = 24f;
        [SerializeField, Min(0f)] private float minimumSpacing = 8f;
        [SerializeField, Range(1f, 60f)] private float maximumSlope = 32f;
        [SerializeField, Range(0f, 1f)] private float minimumBedrockWeight = 0.18f;
        [SerializeField, Range(0f, 1f)] private float minimumDepositWeight = 0.08f;
        [SerializeField, Range(0f, 1f)] private float minimumLithology = 0.28f;
        [SerializeField, Range(0f, 1f)] private float maximumTraversalCorridor = 0.48f;
        [SerializeField] private Vector2 uniformScaleRange = new Vector2(0.9f, 1.25f);
        [SerializeField, Min(0.1f)] private float footprintRadius = 1.2f;
        [SerializeField, Range(0f, 1f)] private float sinkDepth = 0.08f;

        public string ResourceId => resourceId;
        public string MeshFamilyStableId => meshFamilyStableId;
        public TopDown3DNaturalObjectShape MeshShape => meshShape;
        public int MeshVariant => TopDown3DNaturalObjectCatalog.NormalizeMeshVariant(meshVariant);
        public Material ActiveMaterial => activeMaterial;
        public Material DepletedMaterial => depletedMaterial;
        public TopDown3DItemDefinition YieldedItem => yieldedItem;
        public int YieldQuantity => yieldQuantity;
        public int MaximumUses => maximumUses;
        public float InteractionRange => interactionRange;
        public float ActionDuration => actionDuration;
        public string Prompt => prompt;
        public float TargetPerChunk => targetPerChunk;
        public float GlobalCellSize => globalCellSize;
        public float MinimumSpacing => minimumSpacing;
        public float MaximumSlope => maximumSlope;
        public float MinimumBedrockWeight => minimumBedrockWeight;
        public float MinimumDepositWeight => minimumDepositWeight;
        public float MinimumLithology => minimumLithology;
        public float MaximumTraversalCorridor => maximumTraversalCorridor;
        public Vector2 UniformScaleRange => new Vector2(
            Mathf.Max(0.01f, Mathf.Min(uniformScaleRange.x, uniformScaleRange.y)),
            Mathf.Max(0.01f, Mathf.Max(uniformScaleRange.x, uniformScaleRange.y)));
        public float FootprintRadius => footprintRadius;
        public float SinkDepth => sinkDepth;

        public bool TryValidate(
            TopDown3DNaturalObjectCatalog naturalCatalog,
            out string error)
        {
            if (string.IsNullOrWhiteSpace(resourceId))
            {
                error = "Resource definition has an empty stable ID.";
                return false;
            }

            if (naturalCatalog == null
                || !naturalCatalog.TryGetMeshFamily(MeshShape, MeshVariant, out var family)
                || family == null
                || !family.IsComplete
                || !string.Equals(family.StableId, meshFamilyStableId, System.StringComparison.Ordinal))
            {
                error = $"Resource '{resourceId}' does not resolve its required baked mesh family '{meshFamilyStableId}'.";
                return false;
            }

            if (activeMaterial == null || depletedMaterial == null)
            {
                error = $"Resource '{resourceId}' requires active and depleted materials.";
                return false;
            }

            if (yieldedItem == null)
            {
                error = $"Resource '{resourceId}' requires a yielded item.";
                return false;
            }

            if (!yieldedItem.TryValidate(out error))
            {
                error = $"Resource '{resourceId}' has an invalid yielded item: {error}";
                return false;
            }

            if (yieldQuantity < 1 || maximumUses < 1 || interactionRange < 0.25f
                || actionDuration < 0.1f || string.IsNullOrWhiteSpace(prompt)
                || targetPerChunk <= 0f || globalCellSize < 2f || minimumSpacing < 0f
                || footprintRadius <= 0f)
            {
                error = $"Resource '{resourceId}' has invalid economy, interaction, or placement tuning.";
                return false;
            }

            error = null;
            return true;
        }

        internal void Configure(
            string stableResourceId,
            string requiredMeshFamilyId,
            TopDown3DNaturalObjectShape shape,
            int variant,
            Material authoredActiveMaterial,
            Material authoredDepletedMaterial,
            TopDown3DItemDefinition item,
            int itemYield,
            int uses,
            float range,
            float duration,
            string authoredPrompt,
            float density,
            float cellSize,
            float spacing)
        {
            resourceId = stableResourceId;
            meshFamilyStableId = requiredMeshFamilyId;
            meshShape = shape;
            meshVariant = variant;
            activeMaterial = authoredActiveMaterial;
            depletedMaterial = authoredDepletedMaterial;
            yieldedItem = item;
            yieldQuantity = itemYield;
            maximumUses = uses;
            interactionRange = range;
            actionDuration = duration;
            prompt = authoredPrompt;
            targetPerChunk = density;
            globalCellSize = cellSize;
            minimumSpacing = spacing;
        }

        internal void ConfigurePlacementConstraints(
            float slope,
            float bedrock,
            float deposit,
            float lithology,
            float traversalCorridor)
        {
            maximumSlope = slope;
            minimumBedrockWeight = bedrock;
            minimumDepositWeight = deposit;
            minimumLithology = lithology;
            maximumTraversalCorridor = traversalCorridor;
        }
    }
}
