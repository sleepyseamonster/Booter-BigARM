using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    /// <summary>
    /// Stores the deterministic terrain context for a dedicated landscape-authoring
    /// scene. Its editor companion builds a disposable terrain view from the same
    /// terrain generator used by the streamed world.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TopDown3DLandscapeAuthoringSandbox : MonoBehaviour
    {
        [SerializeField] private TopDown3DWorldSettings worldSettings;
        [SerializeField] private Material terrainMaterial;
        [SerializeField] private Material regularRockMaterial;
        [SerializeField] private Vector2Int centerChunk;
        [SerializeField, Range(0, 2)] private int terrainRadiusInChunks = 1;
        [SerializeField] private GameObject rockReference;
        [SerializeField, HideInInspector] private bool variationEnabled;
        [SerializeField, HideInInspector] private int variationSeed;
        public bool VariationEnabled => variationEnabled;
        public int VariationSeed => variationSeed;
        [SerializeField, Range(0f, 1f)] private float rockBurial = 0.035f;
        [SerializeField, Range(0f, 1f)] private float maximumRockBurial = 0.6f;
        [SerializeField, HideInInspector] private int burialRangeVersion;
        [SerializeField, Range(0f, 35f)] private float maximumRockTilt = 20f;
        // Legacy initializer is doubled once below, for both existing and new mixed setups.
        [SerializeField, Range(0f, 1f)] private float sandBuildup = 0.18f;
        [SerializeField, HideInInspector] private int sandBuildupVersion;
        [SerializeField, Range(0f, 1f)] private float groundClutter = 0.7f;
        [SerializeField, Range(0f, 1f)] private float landscapeSand = 0.55f;
        [SerializeField, Range(0f, 0.6f)] private float landscapeSandRelief = 0.32f;
        [SerializeField, Range(0f, 1f)] private float landscapeClutter = 0.72f;
        [Header("Rock / Sand Contact Edge")]
        [SerializeField, Range(0.05f, 1.5f)] private float contactSandWidth = 0.48f;
        [SerializeField, Range(0f, 1f)] private float contactSandOpacity = 0.72f;
        [SerializeField] private Color contactSandColor = new Color(0.58f, 0.27f, 0.12f, 1f);
        [SerializeField] private Color contactSandTrimColor = new Color(0.58f, 0.27f, 0.12f, 1f);
        [SerializeField, Range(0.01f, 1f)] private float contactSandFeather = 0.14f;
        [SerializeField, Range(0f, 0.5f)] private float contactSandBaseFeather = 0.08f;
        [SerializeField, Range(0f, 0.5f)] private float contactSandTopFeather = 0.06f;
        [SerializeField, Range(0f, 1f)] private float contactSandWaviness = 0.38f;
        [SerializeField, Range(0.2f, 8f)] private float contactSandNoiseScale = 2.2f;
        [SerializeField, Range(0f, 1f)] private float contactSandDirectionalBuildup = 0.22f;
        [SerializeField, Range(0.08f, 1.5f)] private float contactSandTrimWidth = 0.42f;
        [SerializeField, Range(0.01f, 0.3f)] private float contactSandTrimHeight = 0.085f;
        [SerializeField, Range(1f, 5f)] private float contactSandTrimCurve = 3f;
        [SerializeField, Range(0f, 0.04f)] private float pebbleDepth = 0.015f;
        [SerializeField, Range(0f, 1f)] private float blowingSand;
        [SerializeField, HideInInspector] private bool blowingSandOffDefaultApplied;

        public GameObject RockReference => rockReference;
        public float RockBurial => Mathf.Clamp01(rockBurial);
        public float MaximumRockBurial => Mathf.Clamp(
            burialRangeVersion == 0 ? Mathf.Max(0.6f, maximumRockBurial) : maximumRockBurial, RockBurial, 1f);
        public float MaximumRockTilt => Mathf.Clamp(maximumRockTilt, 0f, 35f);
        public float SandBuildup => Mathf.Clamp01(sandBuildupVersion == 0 ? sandBuildup * 2f : sandBuildup);
        public float GroundClutter => Mathf.Clamp01(groundClutter);
        public float LandscapeSand => Mathf.Clamp01(landscapeSand);
        public float LandscapeSandRelief => Mathf.Clamp(landscapeSandRelief, 0f, 0.6f);
        public float LandscapeClutter => Mathf.Clamp01(landscapeClutter);
        public float ContactSandWidth => Mathf.Clamp(contactSandWidth, 0.05f, 1.5f);
        public float ContactSandOpacity => Mathf.Clamp01(contactSandOpacity);
        public Color ContactSandColor => contactSandColor;
        public Color ContactSandTrimColor => contactSandTrimColor;
        public float ContactSandFeather => Mathf.Clamp(contactSandFeather, 0.01f, 1f);
        public float ContactSandBaseFeather => Mathf.Clamp(contactSandBaseFeather, 0f, 0.5f);
        public float ContactSandTopFeather => Mathf.Clamp(contactSandTopFeather, 0f, 0.5f);
        public float ContactSandWaviness => Mathf.Clamp01(contactSandWaviness);
        public float ContactSandNoiseScale => Mathf.Clamp(contactSandNoiseScale, 0.2f, 8f);
        public float ContactSandDirectionalBuildup => Mathf.Clamp01(contactSandDirectionalBuildup);
        public float ContactSandTrimWidth => Mathf.Clamp(contactSandTrimWidth, 0.08f, 1.5f);
        public float ContactSandTrimHeight => Mathf.Clamp(contactSandTrimHeight, 0.01f, 0.3f);
        public float ContactSandTrimCurve => Mathf.Clamp(contactSandTrimCurve, 1f, 5f);
        public float PebbleDepth => Mathf.Clamp(pebbleDepth, 0f, 0.04f);
        public float BlowingSand => blowingSandOffDefaultApplied ? Mathf.Clamp01(blowingSand) : 0f;

        public TopDown3DWorldSettings WorldSettings => worldSettings;
        public Material TerrainMaterial => terrainMaterial;
        public Material RegularRockMaterial => regularRockMaterial;
        public Vector2Int CenterChunk => centerChunk;
        public int TerrainRadiusInChunks => terrainRadiusInChunks;

        public void Configure(
            TopDown3DWorldSettings settings,
            Material groundMaterial,
            Material rockMaterial,
            Vector2Int initialCenterChunk)
        {
            worldSettings = settings;
            terrainMaterial = groundMaterial;
            regularRockMaterial = rockMaterial;
            centerChunk = initialCenterChunk;
            terrainRadiusInChunks = Mathf.Clamp(terrainRadiusInChunks, 0, 2);
        }

        public void ConfigureRockReference(GameObject reference)
        {
            rockReference = reference;
            UpgradeBurialRange();
            UpgradeSandBuildup();
            ApplyBlowingSandOffDefault();
        }

        private void OnValidate()
        {
            if (rockReference == null) return;
            UpgradeBurialRange();
            UpgradeSandBuildup();
            ApplyBlowingSandOffDefault();
        }

        private void ApplyBlowingSandOffDefault()
        {
            if (blowingSandOffDefaultApplied) return;
            // User deferred this effect. Turn existing previews off once, without
            // preventing an explicit future opt-in through the existing slider.
            blowingSand = 0f;
            blowingSandOffDefaultApplied = true;
        }

        private void UpgradeSandBuildup()
        {
            if (sandBuildupVersion != 0) return;
            sandBuildup = Mathf.Clamp01(sandBuildup * 2f);
            sandBuildupVersion = 1;
        }

        private void UpgradeBurialRange()
        {
            if (burialRangeVersion != 0) return;
            // Apply the requested deeper default to existing setups as well as new ones.
            // Preserve the shallow endpoint, and never repeat this after the user tunes it.
            maximumRockBurial = Mathf.Max(0.6f, maximumRockBurial);
            burialRangeVersion = 1;
        }
    }
}
