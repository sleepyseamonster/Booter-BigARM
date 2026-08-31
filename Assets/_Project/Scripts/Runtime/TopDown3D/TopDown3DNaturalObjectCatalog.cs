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
        FineGrayCluster,
        Landmark
    }

    public enum TopDown3DNaturalObjectShape
    {
        Pebble,
        Shard,
        Slab,
        Boulder,
        Nodule,
        Outcrop,
        Cliff,
        Talus,
        HeroSpire
    }

    public enum TopDown3DRockSizeTier
    {
        None = 0,
        // Existing values remain fixed so serialized catalog entries keep their tier.
        Large = 1,
        Massive = 2,
        Towering = 3,
        Small = 4,
        Medium = 5,
        ExtraLarge = 6
    }

    public enum TopDown3DRockSurface
    {
        Regular,
        Dark,
        Teal
    }

    [Serializable]
    public sealed class TopDown3DNaturalObjectDefinition
    {
        [SerializeField] private string stableId = "natural-object";
        [SerializeField] private TopDown3DNaturalObjectLayer layer;
        [SerializeField] private TopDown3DRockSizeTier rockSizeTier;
        [SerializeField] private TopDown3DNaturalObjectShape shape;
        [SerializeField, Min(0.01f)] private float weight = 1f;
        [SerializeField] private Vector2 uniformScaleRange = new Vector2(0.8f, 1.2f);
        [SerializeField] private Vector3 proportions = Vector3.one;
        [SerializeField, Range(0f, 1f)] private float sinkDepth = 0.08f;
        [SerializeField, Range(0f, 60f)] private float maximumTilt = 18f;
        [SerializeField, Min(0.01f)] private float footprintRadius = 0.4f;

        public string StableId => stableId;
        public TopDown3DNaturalObjectLayer Layer => layer;
        public TopDown3DRockSizeTier RockSizeTier => rockSizeTier;
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

    [Serializable]
    public sealed class TopDown3DNaturalMeshFamily
    {
        [SerializeField] private string stableId;
        [SerializeField] private TopDown3DNaturalObjectShape shape;
        [SerializeField, Min(0)] private int variant;
        [SerializeField] private Mesh lod0;
        [SerializeField] private Mesh lod1;
        [SerializeField] private Mesh lod2;
        [SerializeField] private Vector3 colliderCenter;
        [SerializeField] private Vector3 colliderSize = Vector3.one;
        [SerializeField, Range(0.01f, 1f)] private float lod0ScreenHeight = 0.24f;
        [SerializeField, Range(0.005f, 1f)] private float lod1ScreenHeight = 0.09f;
        [SerializeField, Range(0.001f, 1f)] private float lod2ScreenHeight = 0.018f;

        public string StableId => stableId;
        public TopDown3DNaturalObjectShape Shape => shape;
        public int Variant => TopDown3DNaturalObjectCatalog.NormalizeMeshVariant(variant);
        public Mesh Lod0 => lod0;
        public Mesh Lod1 => lod1;
        public Mesh Lod2 => lod2;
        public Vector3 ColliderCenter => colliderCenter;
        public Vector3 ColliderSize => new Vector3(
            Mathf.Max(0.01f, colliderSize.x),
            Mathf.Max(0.01f, colliderSize.y),
            Mathf.Max(0.01f, colliderSize.z));
        public float Lod0ScreenHeight => Mathf.Clamp(lod0ScreenHeight, 0.01f, 1f);
        public float Lod1ScreenHeight => Mathf.Clamp(lod1ScreenHeight, 0.005f, Lod0ScreenHeight - 0.001f);
        public float Lod2ScreenHeight => Mathf.Clamp(lod2ScreenHeight, 0.001f, Lod1ScreenHeight - 0.001f);

        public bool IsComplete => !string.IsNullOrWhiteSpace(stableId)
            && lod0 != null
            && lod1 != null
            && lod2 != null
            && colliderSize.x > 0f
            && colliderSize.y > 0f
            && colliderSize.z > 0f
            && Lod0ScreenHeight > Lod1ScreenHeight
            && Lod1ScreenHeight > Lod2ScreenHeight;

        public Mesh GetLod(int index)
        {
            switch (index)
            {
                case 0:
                    return lod0;
                case 1:
                    return lod1;
                case 2:
                    return lod2;
                default:
                    throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        internal void Configure(
            string id,
            TopDown3DNaturalObjectShape meshShape,
            int meshVariant,
            Mesh lod0Mesh,
            Mesh lod1Mesh,
            Mesh lod2Mesh,
            Bounds colliderBounds,
            float lod0Transition,
            float lod1Transition,
            float lod2Transition)
        {
            stableId = id;
            shape = meshShape;
            variant = TopDown3DNaturalObjectCatalog.NormalizeMeshVariant(meshVariant);
            lod0 = lod0Mesh;
            lod1 = lod1Mesh;
            lod2 = lod2Mesh;
            colliderCenter = colliderBounds.center;
            colliderSize = colliderBounds.size;
            lod0ScreenHeight = lod0Transition;
            lod1ScreenHeight = lod1Transition;
            lod2ScreenHeight = lod2Transition;
        }
    }

    [CreateAssetMenu(
        menuName = "Booter & BigARM/Top Down 3D/Natural Object Catalog",
        fileName = "TopDown3DNaturalObjectCatalog")]
    public sealed class TopDown3DNaturalObjectCatalog : ScriptableObject
    {
        public const int MeshVariantsPerShape = 3;

        [SerializeField] private List<TopDown3DNaturalObjectDefinition> definitions =
            new List<TopDown3DNaturalObjectDefinition>();
        [SerializeField] private List<TopDown3DNaturalMeshFamily> meshFamilies =
            new List<TopDown3DNaturalMeshFamily>();
        [NonSerialized] private Dictionary<int, TopDown3DNaturalMeshFamily> meshFamilyLookup;
        [NonSerialized] private Dictionary<int, TopDown3DNaturalMeshData> lod0DataLookup;

        public IReadOnlyList<TopDown3DNaturalObjectDefinition> Definitions => definitions;
        public IReadOnlyList<TopDown3DNaturalMeshFamily> MeshFamilies => meshFamilies;

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

        public bool TryGetMeshFamily(
            TopDown3DNaturalObjectShape shape,
            int variant,
            out TopDown3DNaturalMeshFamily family)
        {
            var normalizedVariant = NormalizeMeshVariant(variant);
            EnsureMeshFamilyLookup();
            return meshFamilyLookup.TryGetValue(GetMeshFamilyKey(shape, normalizedVariant), out family);
        }

        public TopDown3DNaturalMeshFamily GetRequiredMeshFamily(
            TopDown3DNaturalObjectShape shape,
            int variant)
        {
            if (TryGetMeshFamily(shape, variant, out var family) && family.IsComplete)
            {
                return family;
            }

            throw new InvalidOperationException(
                $"The production natural-object catalog is missing complete baked mesh family {shape}:{NormalizeMeshVariant(variant)}.");
        }

        internal TopDown3DNaturalMeshData GetRequiredLod0Data(
            TopDown3DNaturalObjectShape shape,
            int variant)
        {
            var normalizedVariant = NormalizeMeshVariant(variant);
            var key = GetMeshFamilyKey(shape, normalizedVariant);
            if (lod0DataLookup != null && lod0DataLookup.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var family = GetRequiredMeshFamily(shape, normalizedVariant);
            var mesh = family.Lod0;
            if (!mesh.isReadable)
            {
                throw new InvalidOperationException(
                    $"Baked mesh family {family.StableId} LOD0 must remain CPU-readable for deterministic placement geometry.");
            }

            var data = new TopDown3DNaturalMeshData(
                mesh,
                mesh.vertices,
                mesh.normals,
                mesh.triangles);
            if (data.Vertices.Length == 0
                || data.Normals.Length != data.Vertices.Length
                || data.Triangles.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Baked mesh family {family.StableId} LOD0 has incomplete geometry data.");
            }

            if (lod0DataLookup == null)
            {
                lod0DataLookup = new Dictionary<int, TopDown3DNaturalMeshData>();
            }

            lod0DataLookup.Add(key, data);
            return data;
        }

        internal void ReplaceBakedMeshFamilies(IEnumerable<TopDown3DNaturalMeshFamily> replacements)
        {
            meshFamilies = replacements != null
                ? new List<TopDown3DNaturalMeshFamily>(replacements)
                : new List<TopDown3DNaturalMeshFamily>();
            meshFamilyLookup = null;
            lod0DataLookup = null;
        }

        internal static int NormalizeMeshVariant(int variant)
        {
            var positive = variant == int.MinValue ? int.MaxValue : Mathf.Abs(variant);
            return positive % MeshVariantsPerShape;
        }

        private void OnEnable()
        {
            meshFamilyLookup = null;
            lod0DataLookup = null;
        }

        private void OnValidate()
        {
            meshFamilyLookup = null;
            lod0DataLookup = null;
        }

        private void EnsureMeshFamilyLookup()
        {
            if (meshFamilyLookup != null)
            {
                return;
            }

            meshFamilyLookup = new Dictionary<int, TopDown3DNaturalMeshFamily>(meshFamilies.Count);
            for (var i = 0; i < meshFamilies.Count; i++)
            {
                var family = meshFamilies[i];
                if (family == null)
                {
                    continue;
                }

                var key = GetMeshFamilyKey(family.Shape, family.Variant);
                if (!meshFamilyLookup.ContainsKey(key))
                {
                    meshFamilyLookup.Add(key, family);
                }
            }
        }

        private static int GetMeshFamilyKey(TopDown3DNaturalObjectShape shape, int variant)
        {
            return (int)shape * MeshVariantsPerShape + variant;
        }
    }

    internal sealed class TopDown3DNaturalMeshData
    {
        internal TopDown3DNaturalMeshData(
            Mesh mesh,
            Vector3[] vertices,
            Vector3[] normals,
            int[] triangles)
        {
            Mesh = mesh;
            Vertices = vertices;
            Normals = normals;
            Triangles = triangles;
        }

        internal Mesh Mesh { get; }
        internal Vector3[] Vertices { get; }
        internal Vector3[] Normals { get; }
        internal int[] Triangles { get; }
    }
}
