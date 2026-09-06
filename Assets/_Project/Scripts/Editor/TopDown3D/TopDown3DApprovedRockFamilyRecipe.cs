using System;
using System.Collections.Generic;
using BooterBigArm.TopDown3D;
using UnityEngine;

namespace BooterBigArm.Editor
{
    internal enum TopDown3DApprovedRockMassRole
    {
        Core,
        Support,
        Detail
    }

    [Serializable]
    internal sealed class TopDown3DApprovedRockRecipeVolume
    {
        [SerializeField] private string name;
        [SerializeField] private TopDown3DApprovedRockMassRole role;
        [SerializeField] private Vector3 localPosition;
        [SerializeField] private Quaternion localRotation = Quaternion.identity;
        [SerializeField] private Vector3 localScale = Vector3.one;
        [SerializeField] private TopDown3DRockSourceShape sourceShape;
        [SerializeField] private TopDown3DRockVolumeOperation operation;
        [SerializeField] private int shapeSeed;

        internal string Name => string.IsNullOrWhiteSpace(name) ? "Rock Volume" : name;
        internal TopDown3DApprovedRockMassRole Role => role;
        internal Vector3 LocalPosition => localPosition;
        internal Quaternion LocalRotation => localRotation;
        internal Vector3 LocalScale => localScale;
        internal TopDown3DRockSourceShape SourceShape => sourceShape;
        internal TopDown3DRockVolumeOperation Operation => operation;
        internal int ShapeSeed => shapeSeed;

        internal void Configure(
            string volumeName,
            TopDown3DApprovedRockMassRole volumeRole,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            TopDown3DRockSourceShape shape,
            TopDown3DRockVolumeOperation volumeOperation,
            int seed)
        {
            name = string.IsNullOrWhiteSpace(volumeName) ? "Rock Volume" : volumeName;
            role = volumeRole;
            localPosition = position;
            localRotation = rotation;
            localScale = new Vector3(
                Mathf.Max(0.001f, Mathf.Abs(scale.x)),
                Mathf.Max(0.001f, Mathf.Abs(scale.y)),
                Mathf.Max(0.001f, Mathf.Abs(scale.z)));
            sourceShape = shape;
            operation = volumeOperation;
            shapeSeed = seed;
        }
    }

    [Serializable]
    internal sealed class TopDown3DApprovedRockRecipeVariant
    {
        [SerializeField] private string stableId;
        [SerializeField, Min(0)] private int variant;
        [SerializeField] private int generationSeed;
        [SerializeField, Min(0.01f)] private float bodyWidth = 1f;
        [SerializeField, Min(0.01f)] private float bodyLength = 0.75f;
        [SerializeField] private List<TopDown3DApprovedRockRecipeVolume> volumes =
            new List<TopDown3DApprovedRockRecipeVolume>();

        internal string StableId => stableId;
        internal int Variant => variant;
        internal int GenerationSeed => generationSeed;
        internal float BodyWidth => Mathf.Max(0.01f, bodyWidth);
        internal float BodyLength => Mathf.Max(0.01f, bodyLength);
        internal IReadOnlyList<TopDown3DApprovedRockRecipeVolume> Volumes => volumes;

        internal void Configure(
            int variantIndex,
            int seed,
            float width,
            float length,
            IEnumerable<TopDown3DApprovedRockRecipeVolume> sourceVolumes)
        {
            variant = Mathf.Max(0, variantIndex);
            stableId = $"approved-boulder-{variant:00}";
            generationSeed = seed;
            bodyWidth = Mathf.Max(0.01f, width);
            bodyLength = Mathf.Max(0.01f, length);
            volumes = sourceVolumes != null
                ? new List<TopDown3DApprovedRockRecipeVolume>(sourceVolumes)
                : new List<TopDown3DApprovedRockRecipeVolume>();
        }
    }

    /// <summary>
    /// Editor-only capture of the accepted Rock Workbench language. The production baker reads
    /// these frozen source transforms; runtime world generation never loads or interprets recipes.
    /// </summary>
    public sealed class TopDown3DApprovedRockFamilyRecipe : ScriptableObject
    {
        [SerializeField, Min(1)] private int recipeVersion = 1;
        [SerializeField] private string stableId = "approved-boulder-family";
        [SerializeField] private Material sourceMaterial;
        [SerializeField] private TopDown3DRockSilhouetteProfile silhouetteProfile;
        [SerializeField] private TopDown3DRockSurfacePreset surfacePreset;
        [SerializeField, Min(0.001f)] private float previewVoxelSize = 0.05f;
        [SerializeField, Min(0f)] private float fusionSmoothness = 0.0657f;
        [SerializeField, Range(0f, 1f)] private float surfaceRelaxation = 0.45f;
        [SerializeField, Range(0f, 1f)] private float edgeDamage = 1f;
        [SerializeField, Min(0.01f)] private float geologyScale = 2.4f;
        [SerializeField, Range(0f, 1f)] private float surfaceVariation = 0.346f;
        [SerializeField, Range(0f, 1f)] private float crackAmount = 0.36f;
        [SerializeField, Range(0f, 1f)] private float sideGrit = 0.22f;
        [SerializeField, Range(0f, 1f)] private float undersideShale = 0.2f;
        [SerializeField, Range(0f, 1f)] private float sideShalePatches;
        [SerializeField, Range(0f, 1f)] private float topShalePatches;
        [SerializeField, Range(0f, 0.5f)] private float wornShine = 0.099f;
        [SerializeField, Range(0f, 1f)] private float colorVariation = 0.62f;
        [SerializeField] private Color environmentDustColor =
            new Color(0.32f, 0.3f, 0.28f, 1f);
        [SerializeField] private List<TopDown3DApprovedRockRecipeVariant> variants =
            new List<TopDown3DApprovedRockRecipeVariant>();

        internal int RecipeVersion => Mathf.Max(1, recipeVersion);
        internal string StableId => stableId;
        internal Material SourceMaterial => sourceMaterial;
        internal TopDown3DRockSilhouetteProfile SilhouetteProfile => silhouetteProfile;
        internal TopDown3DRockSurfacePreset SurfacePreset => surfacePreset;
        internal float PreviewVoxelSize => Mathf.Max(0.001f, previewVoxelSize);
        internal float FusionSmoothness => Mathf.Max(0f, fusionSmoothness);
        internal float SurfaceRelaxation => Mathf.Clamp01(surfaceRelaxation);
        internal float EdgeDamage => Mathf.Clamp01(edgeDamage);
        internal float GeologyScale => Mathf.Max(0.01f, geologyScale);
        internal float SurfaceVariation => Mathf.Clamp01(surfaceVariation);
        internal float CrackAmount => Mathf.Clamp01(crackAmount);
        internal float SideGrit => Mathf.Clamp01(sideGrit);
        internal float UndersideShale => Mathf.Clamp01(undersideShale);
        internal float SideShalePatches => Mathf.Clamp01(sideShalePatches);
        internal float TopShalePatches => Mathf.Clamp01(topShalePatches);
        internal float WornShine => Mathf.Clamp(wornShine, 0f, 0.5f);
        internal float ColorVariation => Mathf.Clamp01(colorVariation);
        internal Color EnvironmentDustColor => environmentDustColor;
        internal IReadOnlyList<TopDown3DApprovedRockRecipeVariant> Variants => variants;

        internal void Configure(
            TopDown3DRockWorkbenchAuthoring authoring,
            IEnumerable<TopDown3DApprovedRockRecipeVariant> recipeVariants)
        {
            if (authoring == null) throw new ArgumentNullException(nameof(authoring));

            recipeVersion = 1;
            stableId = "approved-boulder-family";
            sourceMaterial = authoring.RockMaterial;
            silhouetteProfile = authoring.GeneratedSilhouetteProfile;
            surfacePreset = authoring.SurfacePreset;
            previewVoxelSize = authoring.VoxelSize;
            fusionSmoothness = authoring.FusionSmoothness;
            surfaceRelaxation = authoring.SurfaceRelaxation;
            edgeDamage = authoring.GeneratedEdgeDamage;
            geologyScale = authoring.GeologyScale;
            surfaceVariation = authoring.SurfaceVariation;
            crackAmount = authoring.CrackAmount;
            sideGrit = authoring.SideGrit;
            undersideShale = authoring.UndersideShale;
            sideShalePatches = authoring.SideShalePatches;
            topShalePatches = authoring.TopShalePatches;
            wornShine = authoring.WornShine;
            colorVariation = authoring.ColorVariation;
            environmentDustColor = authoring.EnvironmentDustColor;
            variants = recipeVariants != null
                ? new List<TopDown3DApprovedRockRecipeVariant>(recipeVariants)
                : new List<TopDown3DApprovedRockRecipeVariant>();
        }
    }
}
