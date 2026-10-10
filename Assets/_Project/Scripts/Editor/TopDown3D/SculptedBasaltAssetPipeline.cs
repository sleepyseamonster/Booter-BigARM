using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Editor
{
    /// <summary>Bounded import/handoff for the author's saved sculpt; never places content in a scene.</summary>
    public static class SculptedBasaltAssetPipeline
    {
        public const string Folder = "Assets/_Project/Art/Environment/Rocks/SculptedBasalt01";
        public const string MaterialPath = "Assets/_Project/Materials/TopDown3D/Rocks/SculptedBasalt01.mat";
        public const string PrefabPath = "Assets/_Project/Prefabs/TopDown3D/Environment/SculptedBasalt01.prefab";

        public static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null || AssetDatabase.LoadAssetAtPath<Material>(MaterialPath) != null
                || Directory.GetFiles(Folder, "*.asset").Length > 0)
                throw new InvalidOperationException("Preserve existing handoff before rebuilding.");
            AssetDatabase.ImportAsset(Folder + "/SculptedBasalt01.fbx", ImportAssetOptions.ForceSynchronousImport);
            var importer = (ModelImporter)AssetImporter.GetAtPath(Folder + "/SculptedBasalt01.fbx");
            importer.globalScale = 1; importer.useFileScale = true; importer.bakeAxisConversion = true;
            importer.importAnimation = false; importer.animationType = ModelImporterAnimationType.None;
            importer.importCameras = false; importer.importLights = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importNormals = ModelImporterNormals.Import; importer.importTangents = ModelImporterTangents.Import;
            importer.meshCompression = ModelImporterMeshCompression.Off; importer.isReadable = false;
            importer.addCollider = false; importer.SaveAndReimport();
            Texture2D texture(string suffix, bool srgb, bool normal = false)
            {
                var path = Folder + "/SculptedBasalt01_" + suffix + ".png";
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var ti = (TextureImporter)AssetImporter.GetAtPath(path);
                ti.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                ti.sRGBTexture = srgb; ti.convertToNormalmap = false; ti.mipmapEnabled = true;
                ti.maxTextureSize = 1024; ti.wrapMode = TextureWrapMode.Clamp; ti.anisoLevel = 4;
                ti.alphaSource = TextureImporterAlphaSource.FromInput; ti.alphaIsTransparency = false;
                ti.textureCompression = TextureImporterCompression.CompressedHQ; ti.SaveAndReimport();
                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            EnsureFolder(Path.GetDirectoryName(MaterialPath).Replace('\\', '/'));
            EnsureFolder(Path.GetDirectoryName(PrefabPath).Replace('\\', '/'));
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "SculptedBasalt01", enableInstancing = true };
            material.SetTexture("_BaseMap", texture("BaseColor", true)); material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BumpMap", texture("Normal", false, true)); material.SetFloat("_BumpScale", 1);
            material.SetTexture("_MetallicGlossMap", texture("MetallicSmoothness", false));
            material.SetFloat("_Metallic", 0); material.SetFloat("_Smoothness", 1); material.SetFloat("_SmoothnessTextureChannel", 0);
            material.SetTexture("_OcclusionMap", texture("Occlusion", false)); material.SetFloat("_OcclusionStrength", .6f);
            // Roughness remains a Blender/source inspection map; runtime consumes the packed smoothness map.
            texture("Roughness", false);
            material.EnableKeyword("_NORMALMAP"); material.EnableKeyword("_METALLICSPECGLOSSMAP"); material.EnableKeyword("_OCCLUSIONMAP");
            AssetDatabase.CreateAsset(material, MaterialPath);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/SculptedBasalt01.fbx");
            var sourceMeshes = model.GetComponentsInChildren<MeshFilter>(true);
            var root = new GameObject("SculptedBasalt01");
            try
            {
                var lods = new LOD[3]; float[] thresholds = { .35f, .15f, .015f };
                for (int i = 0; i < 3; i++)
                {
                    var source = sourceMeshes.Single(m => m.name == "SculptedBasalt01_LOD" + i);
                    var child = new GameObject("LOD" + i, typeof(MeshFilter), typeof(MeshRenderer)); child.transform.SetParent(root.transform, false);
                    child.GetComponent<MeshFilter>().sharedMesh = BakeTransform(source);
                    var renderer = child.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material;
                    lods[i] = new LOD(thresholds[i], new Renderer[] { renderer });
                }
                var collision = new GameObject("Collision", typeof(MeshCollider)); collision.transform.SetParent(root.transform, false);
                var collider = collision.GetComponent<MeshCollider>();
                collider.sharedMesh = BakeTransform(sourceMeshes.Single(m => m.name == "SculptedBasalt01_Collision")); collider.convex = true;
                var group = root.AddComponent<LODGroup>(); group.SetLODs(lods); group.RecalculateBounds();
                group.fadeMode = LODFadeMode.None;
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssetIfDirty(material);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
            Validate();
        }

        public static void Validate()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) throw new InvalidOperationException("Missing basalt prefab.");
            var group = prefab.GetComponent<LODGroup>(); var lods = group.GetLODs();
            if (lods.Length != 3) throw new InvalidOperationException("Expected three rock LODs.");
            var counts = new int[3]; Bounds bounds = default;
            int[] expected = { 982, 490, 244 };
            for (int i = 0; i < 3; i++)
            {
                var renderer = lods[i].renderers.Single(); var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                counts[i] = checked((int)mesh.GetIndexCount(0) / 3);
                if (counts[i] != expected[i] || mesh.uv.Length != mesh.vertexCount || mesh.normals.Length != mesh.vertexCount || mesh.tangents.Length != mesh.vertexCount)
                    throw new InvalidOperationException("Mesh topology, UVs, normals or tangents do not match delivery.");
                if (renderer.sharedMaterial != AssetDatabase.LoadAssetAtPath<Material>(MaterialPath) || renderer.transform.localScale != Vector3.one || renderer.transform.localPosition != Vector3.zero)
                    throw new InvalidOperationException("Unexpected mesh transform/material.");
                if (i == 0) bounds = mesh.bounds;
            }
            if (Mathf.Abs(bounds.size.x - 3.163379f) > .05f || Mathf.Abs(bounds.size.y - 4.959615f) > .05f || Mathf.Abs(bounds.size.z - 3.2489f) > .05f || Mathf.Abs(bounds.min.y) > .03f)
                throw new InvalidOperationException("Unity metre scale, axes or ground pivot mismatch: " + bounds);
            var collider = prefab.GetComponentInChildren<MeshCollider>();
            if (!collider.convex || collider.sharedMesh.GetIndexCount(0) / 3 > 255 || collider.GetComponent<Renderer>() != null)
                throw new InvalidOperationException("Collision contract mismatch.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material.shader.name != "Universal Render Pipeline/Lit" || !material.enableInstancing || !material.IsKeywordEnabled("_NORMALMAP")
                || !material.IsKeywordEnabled("_METALLICSPECGLOSSMAP") || material.GetFloat("_Metallic") != 0)
                throw new InvalidOperationException("Basalt material contract mismatch.");
            foreach (var suffix in new[] { "BaseColor", "Normal", "MetallicSmoothness", "Occlusion" })
            {
                var ti = (TextureImporter)AssetImporter.GetAtPath(Folder + "/SculptedBasalt01_" + suffix + ".png");
                if (ti.sRGBTexture != (suffix == "BaseColor") || ti.maxTextureSize != 1024 || !ti.mipmapEnabled
                    || (suffix == "Normal" && ti.textureType != TextureImporterType.NormalMap))
                    throw new InvalidOperationException("Texture import mismatch: " + suffix);
            }
            var report = JsonUtility.ToJson(new Report { unity = Application.unityVersion, lodTriangles = counts,
                boundsSize = bounds.size, boundsMin = bounds.min, collisionTriangles = (int)collider.sharedMesh.GetIndexCount(0) / 3 }, true);
            Directory.CreateDirectory("Logs/RockSculpt"); File.WriteAllText("Logs/RockSculpt/unity-import.json", report);
            Debug.Log("Sculpted basalt import validation passed: " + report);
        }
        public static void Capture()
        {
            Validate();
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                UnityEditor.SceneManagement.NewSceneMode.Single);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.3079263f, .33095473f, .3954263f);
            RenderSettings.ambientEquatorColor = new Color(.44889024f, .2672181f, .2066469f);
            RenderSettings.ambientGroundColor = new Color(.16225578f, .09435347f, .07776821f);
            var rock = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), scene);
            rock.GetComponent<LODGroup>().ForceLOD(0);
            var key = new GameObject("Review twilight sun", typeof(Light));
            var light = key.GetComponent<Light>(); light.type = LightType.Directional;
            light.color = new Color(1, .7253322f, .46321848f); light.intensity = 1.1011368f;
            key.transform.rotation = Quaternion.Euler(30, -35, 0);
            var cameraObject = new GameObject("Review camera", typeof(Camera)); var camera = cameraObject.GetComponent<Camera>();
            cameraObject.transform.position = new Vector3(8, 7, -11); cameraObject.transform.LookAt(new Vector3(0, 2.4f, 0));
            camera.orthographic = true; camera.orthographicSize = 3.6f; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.12f, .09f, .07f);
            var target = new RenderTexture(900, 900, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var image = new Texture2D(900, 900, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 900, 900), 0, 0); image.Apply();
                File.WriteAllBytes("Logs/RockSculpt/unity-review.png", image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null; RenderTexture.active = null; target.Release();
                UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(image);
            }
            // In-memory review only: no scene is saved and no Play Mode is entered.
        }
        [Serializable] private sealed class Report
        { public string unity; public int[] lodTriangles; public Vector3 boundsSize; public Vector3 boundsMin; public int collisionTriangles; }
        private static Mesh BakeTransform(MeshFilter source)
        {
            // FBX may retain axis conversion in the node hierarchy. Bake the complete imported
            // transform into a task-owned runtime mesh so prefab children have unit transforms.
            var mesh = UnityEngine.Object.Instantiate(source.sharedMesh); mesh.name = source.name;
            var matrix = source.transform.localToWorldMatrix;
            mesh.vertices = mesh.vertices.Select(matrix.MultiplyPoint3x4).ToArray();
            var normalMatrix = matrix.inverse.transpose;
            mesh.normals = mesh.normals.Select(n => normalMatrix.MultiplyVector(n).normalized).ToArray();
            mesh.tangents = mesh.tangents.Select(t =>
            {
                var direction = matrix.MultiplyVector(new Vector3(t.x, t.y, t.z)).normalized;
                return new Vector4(direction.x, direction.y, direction.z, t.w * Mathf.Sign(matrix.determinant));
            }).ToArray();
            mesh.RecalculateBounds(); AssetDatabase.CreateAsset(mesh, Folder + "/" + source.name + ".asset");
            return mesh;
        }
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/')); AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path));
        }
    }
}
