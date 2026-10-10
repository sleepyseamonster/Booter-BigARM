using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Authored ground dressing. Editor-baked surface samples persist in the scene;
    /// runtime reconstructs presentation only, never raycasts or changes terrain/collision.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class GroundTextureSticker : MonoBehaviour
    {
        [SerializeField, HideInInspector] private string authoredId;
        [SerializeField] private Texture2D texture;
        [SerializeField] private Texture2D normalMap;
        [SerializeField] private Color tint = Color.white;
        [SerializeField] private Vector2 size = new Vector2(8, 8);
        [SerializeField, Range(0, 1)] private float opacity = .85f;
        [SerializeField, Range(.01f, .49f)] private float edgeSoftness = .2f;
        [SerializeField] private bool roundShape = true;
        [SerializeField] private bool tileTexture = true;
        [SerializeField, Min(.1f)] private float metersPerRepeat = 2f;
        [SerializeField, Range(0, 2)] private float normalStrength = 1f;
        [SerializeField, Range(0, 1)] private float smoothness = .08f;
        [SerializeField, Range(-100, 100)] private int overlapOrder;
        [SerializeField, Range(.1f, 2)] private float sampleSpacing = .5f;
        [SerializeField, HideInInspector] private Vector3[] bakedVertices = Array.Empty<Vector3>();
        [SerializeField, HideInInspector] private Vector3[] bakedNormals = Array.Empty<Vector3>();
        [SerializeField, HideInInspector] private Vector2[] bakedUvs = Array.Empty<Vector2>();
        [SerializeField, HideInInspector] private int[] bakedTriangles = Array.Empty<int>();
        [SerializeField, HideInInspector] private Matrix4x4 bakedPose;
        [NonSerialized] private Mesh ownedMesh;
        [NonSerialized] private MaterialPropertyBlock properties;
        [NonSerialized] private bool refresh = true;
        public string AuthoredId => authoredId;
        public Vector2 Size => new Vector2(Mathf.Clamp(size.x, .25f, 64), Mathf.Clamp(size.y, .25f, 64));
        public float SampleSpacing => Mathf.Clamp(sampleSpacing, .1f, 2);
        public Texture2D Texture => texture;
        public bool HasGeometry => bakedTriangles != null && bakedTriangles.Length > 0;
        public bool NeedsConform => bakedPose != transform.localToWorldMatrix;
        public int TriangleCount => bakedTriangles == null ? 0 : bakedTriangles.Length / 3;

        public void Configure(string id, Texture2D albedo, Vector2 dimensions)
        { authoredId = id; texture = albedo; size = dimensions; refresh = true; }
        public void SetAuthoredId(string id) => authoredId = id;
        public void SetBakedGeometry(Vector3[] vertices, Vector3[] normals, Vector2[] uvs, int[] triangles)
        {
            if (vertices == null || normals == null || uvs == null || triangles == null
                || vertices.Length != normals.Length || vertices.Length != uvs.Length || triangles.Length % 3 != 0)
                throw new ArgumentException("Invalid sticker geometry.");
            foreach (int i in triangles) if (i < 0 || i >= vertices.Length) throw new ArgumentException("Invalid sticker index.");
            bakedVertices = (Vector3[])vertices.Clone(); bakedNormals = (Vector3[])normals.Clone();
            bakedUvs = (Vector2[])uvs.Clone(); bakedTriangles = (int[])triangles.Clone();
            bakedPose = transform.localToWorldMatrix;
            RebuildVisual();
        }
        private void OnEnable() => RebuildVisual();
        private void OnValidate() => refresh = true;
        private void Update() { if (refresh) RebuildVisual(); }
        public void RebuildVisual()
        {
            refresh = false;
            if (ownedMesh == null) ownedMesh = new Mesh { name = "Authored ground sticker mesh", hideFlags = HideFlags.HideAndDontSave };
            ownedMesh.Clear();
            if (bakedVertices != null && bakedNormals != null && bakedUvs != null && bakedTriangles != null
                && bakedVertices.Length == bakedNormals.Length && bakedVertices.Length == bakedUvs.Length)
            {
                ownedMesh.vertices = bakedVertices; ownedMesh.normals = bakedNormals;
                ownedMesh.uv = bakedUvs; ownedMesh.triangles = bakedTriangles;
                if (HasGeometry) { ownedMesh.RecalculateTangents(); ownedMesh.RecalculateBounds(); }
            }
            GetComponent<MeshFilter>().sharedMesh = ownedMesh;
            var renderer = GetComponent<MeshRenderer>();
            renderer.enabled = enabled && HasGeometry && texture != null;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            renderer.sortingOrder = Mathf.Clamp(overlapOrder, -100, 100);
            properties ??= new MaterialPropertyBlock(); properties.Clear();
            properties.SetTexture("_BaseMap", texture != null ? texture : Texture2D.whiteTexture);
            // With no normal map, strength zero suppresses the neutral fallback's packed-normal interpretation.
            properties.SetTexture("_NormalMap", normalMap != null ? normalMap : Texture2D.grayTexture);
            properties.SetFloat("_NormalStrength", normalMap != null ? Mathf.Clamp(normalStrength, 0, 2) : 0);
            properties.SetColor("_Tint", tint);
            properties.SetFloat("_Opacity", Mathf.Clamp01(opacity)); properties.SetFloat("_Feather", Mathf.Clamp(edgeSoftness, .01f, .49f));
            properties.SetFloat("_Round", roundShape ? 1 : 0);
            properties.SetVector("_Repeats", tileTexture
                ? new Vector4(Size.x / Mathf.Max(.1f, metersPerRepeat), Size.y / Mathf.Max(.1f, metersPerRepeat), 0, 0)
                : new Vector4(1, 1, 0, 0));
            properties.SetFloat("_Smoothness", Mathf.Clamp01(smoothness));
            renderer.SetPropertyBlock(properties);
        }
        private void OnDisable()
        {
            var renderer = GetComponent<MeshRenderer>(); if (renderer != null) renderer.enabled = false;
            if (ownedMesh != null)
            {
                var filter = GetComponent<MeshFilter>(); if (filter != null && filter.sharedMesh == ownedMesh) filter.sharedMesh = null;
                if (Application.isPlaying) Destroy(ownedMesh); else DestroyImmediate(ownedMesh);
                ownedMesh = null;
            }
        }
        private void OnDestroy() => OnDisable();
    }
}
