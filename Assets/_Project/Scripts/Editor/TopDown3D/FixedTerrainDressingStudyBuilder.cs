using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Editor
{
    /// <summary>Creates a separate saved comparison scene from a read-only terrain patch.
    /// No production generator, runtime installer, terrain mutation or save service is installed.</summary>
    public static class FixedTerrainDressingStudyBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Reference/FixedTerrainDressingStudy.unity";
        public const string AssetFolder = "Assets/_Project/Art/Environment/Rocks/Studies/FixedTerrainDressing";
        private const string Production = "Assets/_Project/Scenes/Production/GreaterWasteland.unity";
        private const int Grid = 97;
        private const float Width = 24f;
        private const int Seed = 20261009;

        [Serializable] public sealed class Receipt
        {
            public string scene, sourceScene, sourceSceneSha256, terrainRevision;
            public Vector3 sourceCenter;
            public int seed, sampledVertices, panels, rockInstances;
            public float maximumReliefMeters;
            public string[] sampledTerrainPaths, sampledTerrainSha256;
            public string limitation = "Editor comparison only; no Player performance, saved-world integration, exact mesh contact, parallax or texture-synthesis proof.";
        }
        private sealed class Rock
        {
            public Mesh Mesh;
            public Material Material;
            public Vector3 Position, Scale;
            public Quaternion Rotation;
            public string Id;
            public Bounds Bounds;
        }

        [MenuItem("Booter & BigARM/Geology Studies/Build Fixed Terrain Dressing Comparison")]
        public static void BuildFromMenu() => Build();

        public static Receipt Build()
        {
            var source = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || source.path != Production || source.isDirty)
                throw new InvalidOperationException("Build from the clean saved Greater Wasteland scene in Edit Mode.");
            if (File.Exists(ScenePath) || Directory.Exists(AssetFolder))
                throw new InvalidOperationException("The study already exists. Preserve it; use a new versioned destination for another build.");
            var player = GreaterWastelandPlayerPlacement.FindPlayer(source)
                ?? throw new InvalidOperationException("Exactly one Booter is required to choose the source patch.");
            var terrains = source.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Terrain>(true)).ToArray();
            string hash = Hash(Production);
            var index = new FixedTerrainSurfaceIndex(terrains, hash);
            var center = player.transform.position;
            var heights = new float[Grid, Grid];
            var slopes = new float[Grid, Grid];
            var touched = new HashSet<Terrain>();
            Physics.SyncTransforms();
            for (int z = 0; z < Grid; z++) for (int x = 0; x < Grid; x++)
            {
                var local = Coordinate(x, z);
                if (!index.TrySample(new Vector2(center.x + local.x, center.z + local.y), out var sample))
                    throw new InvalidOperationException("The chosen study patch crosses a hole or missing/disabled terrain. No output was created.");
                heights[z, x] = sample.Position.y - center.y; slopes[z, x] = sample.SlopeDegrees;
                touched.Add(sample.Terrain);
            }
            var catalog = AssetDatabase.LoadAssetAtPath<TopDown3DAuthoredFormationCatalog>(
                "Assets/_Project/Art/Environment/Rocks/Generated/AuthoredFormationCatalog.asset");
            var templates = catalog == null ? null : catalog.Templates;
            if (templates == null || templates.Count != 2 || templates.Any(t => t == null || !t.HasApprovedStage))
                throw new InvalidOperationException("Two complete approved formation families are required.");
            var rockMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/TopDown3D/RockWorkbench_NeutralPBR.mat");
            var natural = AssetDatabase.LoadAssetAtPath<TopDown3DNaturalObjectCatalog>("Assets/_Project/Settings/World/TopDown3DNaturalObjectCatalog.asset");
            var sand = RequireTexture("Assets/_Project/Art/Environment/Ground/SandDirt/BrokenWorldSweptSandTransitionAlbedo.png");
            var shader = Shader.Find("BooterBigArm/Studies/Fixed Terrain Dressing");
            if (shader == null || rockMaterial == null || natural == null) throw new InvalidOperationException("Missing study shader/material/catalog.");
            var layouts = templates.Select(t => BuildRocks(t, natural, rockMaterial, heights)).ToArray();
            EnsureFolder(AssetFolder);
            var study = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var receipt = new Receipt { scene = ScenePath, sourceScene = Production, sourceSceneSha256 = hash,
                terrainRevision = hash, sourceCenter = center, seed = Seed, sampledVertices = Grid * Grid, panels = 6,
                maximumReliefMeters = .08f,
                sampledTerrainPaths = touched.Select(t => AssetDatabase.GetAssetPath(t.terrainData)).OrderBy(p => p, StringComparer.Ordinal).ToArray() };
            receipt.sampledTerrainSha256 = receipt.sampledTerrainPaths.Select(Hash).ToArray();
            try
            {
                for (int row = 0; row < layouts.Length; row++)
                {
                    var rocks = layouts[row];
                    var field = new FixedTerrainRockContactField(rocks.Select(r => new FixedTerrainRockContactField.Footprint(
                        new Vector2(r.Bounds.center.x, r.Bounds.center.z),
                        new Vector2(Mathf.Max(.05f, r.Bounds.extents.x), Mathf.Max(.05f, r.Bounds.extents.z)))), new Vector2(1, .35f), Seed);
                    for (int treatment = 0; treatment < 3; treatment++)
                    {
                        var root = new GameObject($"{templates[row].name} / {treatment}: " + new[] { "Geometry baseline", "Shared sand and gravel", "Shared contact plus relief" }[treatment]);
                        SceneManager.MoveGameObjectToScene(root, study);
                        root.transform.position = new Vector3(treatment * 28f, 0, row * 28f);
                        var material = new Material(shader) { name = $"StudyGround_{row}_{treatment}" };
                        material.SetTexture("_SandMap", sand);
                        material.SetTexture("_StoneMap", rockMaterial.GetTexture("_TopBaseMap"));
                        material.SetTexture("_StoneNormal", rockMaterial.GetTexture("_TopNormalMap"));
                        material.SetTexture("_StoneSurface", rockMaterial.GetTexture("_TopSurfaceMap"));
                        material.SetFloat("_Contact", treatment == 0 ? 0 : 1);
                        material.SetFloat("_MetersPerTile", 2f);
                        AssetDatabase.CreateAsset(material, $"{AssetFolder}/{material.name}.mat");
                        var mesh = BuildGround(heights, slopes, field, treatment == 2);
                        mesh.name = $"StudyGround_{row}_{treatment}";
                        AssetDatabase.CreateAsset(mesh, $"{AssetFolder}/{mesh.name}.asset");
                        var ground = new GameObject("Sampled terrain patch", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
                        ground.transform.SetParent(root.transform, false);
                        ground.GetComponent<MeshFilter>().sharedMesh = mesh;
                        ground.GetComponent<MeshCollider>().sharedMesh = mesh;
                        ground.GetComponent<MeshRenderer>().sharedMaterial = material;
                        foreach (var rock in rocks)
                        {
                            var go = new GameObject(rock.Id, typeof(MeshFilter), typeof(MeshRenderer));
                            go.transform.SetParent(root.transform, false);
                            go.transform.localPosition = rock.Position; go.transform.localRotation = rock.Rotation; go.transform.localScale = rock.Scale;
                            go.GetComponent<MeshFilter>().sharedMesh = rock.Mesh;
                            go.GetComponent<MeshRenderer>().sharedMaterial = rock.Material;
                            // Deliberately no broad-box obstacle collision: study rendering is not traversal proof.
                            receipt.rockInstances++;
                        }
                    }
                }
                var light = new GameObject("Study low sun", typeof(Light));
                SceneManager.MoveGameObjectToScene(light, study);
                var originalSun = source.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Light>()).FirstOrDefault(l => l.type == LightType.Directional);
                light.transform.rotation = originalSun != null ? originalSun.transform.rotation : Quaternion.Euler(22, -35, 0);
                var sun = light.GetComponent<Light>(); sun.type = LightType.Directional; sun.shadows = LightShadows.Soft;
                sun.color = originalSun != null ? originalSun.color : new Color(1f, .72f, .45f);
                sun.intensity = originalSun != null ? originalSun.intensity : 1.5f;
                var cameraObject = new GameObject("Study overview camera", typeof(Camera));
                SceneManager.MoveGameObjectToScene(cameraObject, study);
                cameraObject.transform.position = new Vector3(28, 68, -48);
                cameraObject.transform.LookAt(new Vector3(28, 0, 14));
                var camera = cameraObject.GetComponent<Camera>(); camera.fieldOfView = 48; camera.farClipPlane = 200;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.12f, .075f, .055f);
                EnsureFolder("Assets/_Project/Scenes/Reference");
                if (!EditorSceneManager.SaveScene(study, ScenePath)) throw new IOException("Could not save study scene.");
                string evidence = "Docs/Evidence/WorldCreator/FixedTerrainDressing";
                Directory.CreateDirectory(evidence);
                File.WriteAllText($"{evidence}/baseline-2026-10-09.json", JsonUtility.ToJson(receipt, true) + "\n");
                if (Hash(Production) != hash || source.isDirty) throw new InvalidOperationException("Production scene changed during study creation.");
                return receipt;
            }
            finally { SceneManager.SetActiveScene(source); EditorSceneManager.CloseScene(study, true); }
        }

        /// <summary>Offscreen Edit Mode capture; does not focus a window or enter Play Mode.</summary>
        public static void Capture()
        {
            if (!Application.isBatchMode)
                throw new InvalidOperationException("Capture only in an isolated background batchmode project; live Editor rendering stalled during this study.");
            var source = SceneManager.GetActiveScene();
            var study = EditorSceneManager.OpenPreviewScene(ScenePath);
            RenderTexture target = null; Texture2D pixels = null;
            var previousTarget = RenderTexture.active;
            try
            {
                var camera = study.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Camera>()).Single();
                camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(study);
                target = new RenderTexture(1280, 720, 24); target.Create(); camera.targetTexture = target;
                pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                string folder = "Docs/Evidence/WorldCreator/FixedTerrainDressing";
                Directory.CreateDirectory(folder);
                Action<string> render = name =>
                {
                    camera.Render(); RenderTexture.active = target;
                    pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
                    File.WriteAllBytes(folder + "/" + name + ".png", pixels.EncodeToPNG());
                };
                render("overview");
                for (int row = 0; row < 2; row++) for (int panel = 0; panel < 3; panel++)
                {
                    var center = new Vector3(panel * 28, 0, row * 28);
                    var rotation = Quaternion.Euler(55, 0, 0);
                    camera.transform.SetPositionAndRotation(center - rotation * Vector3.forward * 25, rotation);
                    render($"family-{row}-treatment-{panel}");
                }
                var errors = ShaderUtil.GetShaderMessages(Shader.Find("BooterBigArm/Studies/Fixed Terrain Dressing"))
                    .Where(m => m.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error).ToArray();
                if (errors.Length > 0) throw new InvalidOperationException("Study shader failed: " + string.Join("; ", errors.Select(e => e.message)));
            }
            finally
            {
                RenderTexture.active = previousTarget;
                if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
                if (pixels != null) UnityEngine.Object.DestroyImmediate(pixels);
                EditorSceneManager.ClosePreviewScene(study);
                if (source.IsValid() && source.isLoaded) SceneManager.SetActiveScene(source);
            }
        }

        private static List<Rock> BuildRocks(TopDown3DAuthoredFormationAsset template, TopDown3DNaturalObjectCatalog natural,
            Material material, float[,] heights)
        {
            var entries = template.ApprovedStageEntries;
            var bounds = new Bounds(); bool first = true;
            foreach (var entry in entries)
            {
                var b = TransformBounds(entry.Family.Lod0.bounds, entry.LocalPose);
                if (first) { bounds = b; first = false; } else bounds.Encapsulate(b);
            }
            float scale = Mathf.Min(1f, 17f / Mathf.Max(bounds.size.x, bounds.size.z));
            var shift = new Vector3(bounds.center.x, 0, bounds.center.z);
            var rocks = new List<Rock>();
            foreach (var entry in entries)
            {
                var pose = Matrix4x4.Scale(Vector3.one * scale) * Matrix4x4.Translate(-shift) * entry.LocalPose;
                var rock = new Rock { Mesh = entry.Family.Lod0, Material = template.Members[entry.SourceIndex].Material,
                    Position = pose.GetColumn(3), Rotation = pose.rotation, Scale = pose.lossyScale, Id = template.SourceGuid + "/" + entry.InstanceId };
                Fit(rock, heights, template.SurfaceTreatment.ShallowBurial);
                rocks.Add(rock);
            }
            // Identical seeded peripheral slabs/shards across all treatments; no shared UnityEngine.Random state.
            var random = new System.Random(Seed);
            for (int i = 0; i < 36; i++)
            {
                var family = natural.GetRequiredMeshFamily(i % 3 == 0 ? TopDown3DNaturalObjectShape.Slab : TopDown3DNaturalObjectShape.Shard, i % 3);
                float angle = (float)random.NextDouble() * Mathf.PI * 2;
                float radius = Mathf.Lerp(7.7f, 10f, (float)random.NextDouble());
                var rock = new Rock { Mesh = family.Lod0, Material = material,
                    Position = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius),
                    Rotation = Quaternion.Euler(0, (float)random.NextDouble() * 360, 0),
                    Scale = Vector3.one * Mathf.Lerp(.08f, .22f, (float)random.NextDouble()), Id = template.SourceGuid + "/study-fragment/" + i };
                Fit(rock, heights, i % 3 == 0 ? .35f : .15f); rocks.Add(rock);
            }
            return rocks;
        }
        private static void Fit(Rock rock, float[,] heights, float burial)
        {
            var pose = Matrix4x4.TRS(rock.Position, rock.Rotation, rock.Scale);
            var bounds = TransformBounds(rock.Mesh.bounds, pose);
            float lowestClearance = float.PositiveInfinity;
            foreach (var vertex in rock.Mesh.vertices)
            {
                var p = pose.MultiplyPoint3x4(vertex);
                if (Mathf.Abs(p.x) > Width / 2 || Mathf.Abs(p.z) > Width / 2)
                    throw new InvalidOperationException("Rock exceeds the bounded study patch.");
                lowestClearance = Mathf.Min(lowestClearance, p.y - GroundHeight(heights, p.x, p.z));
            }
            rock.Position.y -= lowestClearance + Mathf.Clamp01(burial) * bounds.size.y;
            rock.Bounds = TransformBounds(rock.Mesh.bounds, Matrix4x4.TRS(rock.Position, rock.Rotation, rock.Scale));
        }
        public static float GroundHeight(float[,] heights, float x, float z)
        {
            float gx = Mathf.Clamp((x / Width + .5f) * (Grid - 1), 0, Grid - 1);
            float gz = Mathf.Clamp((z / Width + .5f) * (Grid - 1), 0, Grid - 1);
            int ix = Mathf.Min((int)gx, Grid - 2), iz = Mathf.Min((int)gz, Grid - 2);
            return Mathf.Lerp(Mathf.Lerp(heights[iz, ix], heights[iz, ix + 1], gx - ix),
                Mathf.Lerp(heights[iz + 1, ix], heights[iz + 1, ix + 1], gx - ix), gz - iz);
        }
        private static Mesh BuildGround(float[,] heights, float[,] slopes, FixedTerrainRockContactField field, bool relief)
        {
            var vertices = new Vector3[Grid * Grid]; var uv = new Vector2[vertices.Length]; var colors = new Color[vertices.Length];
            var triangles = new int[(Grid - 1) * (Grid - 1) * 6]; int cursor = 0;
            for (int z = 0; z < Grid; z++) for (int x = 0; x < Grid; x++)
            {
                int i = z * Grid + x; var p = Coordinate(x, z); var contact = field.Evaluate(p, slopes[z, x]);
                vertices[i] = new Vector3(p.x, heights[z, x] + (relief ? contact.Relief : 0), p.y);
                uv[i] = p; colors[i] = new Color(contact.Sand, contact.Gravel, 0, 1);
                if (x == Grid - 1 || z == Grid - 1) continue;
                triangles[cursor++] = i; triangles[cursor++] = i + Grid; triangles[cursor++] = i + 1;
                triangles[cursor++] = i + 1; triangles[cursor++] = i + Grid; triangles[cursor++] = i + Grid + 1;
            }
            var mesh = new Mesh { vertices = vertices, uv = uv, colors = colors, triangles = triangles };
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds(); return mesh;
        }
        private static Bounds TransformBounds(Bounds source, Matrix4x4 pose)
        {
            var result = new Bounds(pose.MultiplyPoint3x4(source.center), Vector3.zero);
            for (int i = 0; i < 8; i++) result.Encapsulate(pose.MultiplyPoint3x4(source.center + Vector3.Scale(source.extents,
                new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1))));
            return result;
        }
        private static Vector2 Coordinate(int x, int z) => new Vector2((float)x / (Grid - 1) * Width - Width / 2, (float)z / (Grid - 1) * Width - Width / 2);
        private static Texture2D RequireTexture(string path) => AssetDatabase.LoadAssetAtPath<Texture2D>(path)
            ?? throw new InvalidOperationException("Missing texture: " + path);
        private static string Hash(string path)
        { using (var sha = SHA256.Create()) using (var file = File.OpenRead(path)) return BitConverter.ToString(sha.ComputeHash(file)).Replace("-", "").ToLowerInvariant(); }
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/'); EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
