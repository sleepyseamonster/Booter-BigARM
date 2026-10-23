using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Playables;
using UnityEngine.Animations;
using BooterBigArm.TopDown3D;

namespace BooterBigArm.Editor
{
    /// <summary>Candidate-only authoring. Never opens or saves the author's gameplay scenes.</summary>
    public static class WardrobePilotAuthoring
    {
        public const string ModelPath = "Assets/_Project/Art/Characters/AnimationPlayground/Qualification/MaleBaseMeshOriginalRig.fbx";
        public const string Output = "Assets/_Project/Art/Characters/Booter/Wardrobe/Pilot";
        public static void PoseFromCli()
        {
            Guard(); PrepareRendering();
            File.WriteAllText("Logs/Wardrobe/pose-trace.txt", "");
            var root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath));
            try
            {
                var rig = AssetDatabase.LoadAssetAtPath<TopDown3DWardrobeRig>(Output + "/BodyRig.asset");
                var body = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(s => s.sharedMesh == rig.bodyMesh);
                var neutral = AssetDatabase.LoadAssetAtPath<Material>(Output + "/DiagnosticNeutral.mat");
                var jacket = AssetDatabase.LoadAssetAtPath<TopDown3DWardrobeDefinition>(Output + "/Jacket.asset");
                var armor = AssetDatabase.LoadAssetAtPath<TopDown3DWardrobeDefinition>(Output + "/Armor.asset");
                var animator = root.GetComponentInChildren<Animator>(); animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var wardrobe = root.AddComponent<TopDown3DWardrobePresentation>(); wardrobe.Configure(root.transform, body, rig);
                if (!wardrobe.TryReplace(new[] { jacket, armor }, out var wardrobeError)) throw new InvalidOperationException(wardrobeError);
                foreach (var gait in new[] { "Walk", "Jog" })
                {
                    var path = "Assets/_Project/Art/Characters/Animations/MoCapCentral/CoreMotion/Locomotion/MCU_am_Loco_" + gait + "_Fwd_NoRM.FBX";
                    var clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview"));
                    var graph = PlayableGraph.Create("Offline wardrobe pose qualification");
                    try
                    {
                        var armSamples = new List<Quaternion>();
                        graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                        var output = AnimationPlayableOutput.Create(graph, "Single character pose", animator);
                        var playable = AnimationClipPlayable.Create(graph, clip); playable.SetApplyFootIK(false); playable.SetApplyPlayableIK(false);
                        output.SetSourcePlayable(playable); graph.Play();
                        for (var frame = 1; frame <= 3; frame++)
                        {
                            var sampleTime = clip.length * frame / 4;
                            Capture(root, body, rig, neutral, new[] { jacket, armor }, gait.ToLowerInvariant() + "-" + frame,
                                () => { playable.SetTime(sampleTime); graph.Evaluate(0);
                                    var arm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
                                    armSamples.Add(arm.localRotation);
                                    File.AppendAllText("Logs/Wardrobe/pose-trace.txt", gait + " " + sampleTime + " arm=" + arm.localRotation + " scale=" + arm.lossyScale + "\n"); });
                        }
                        if (!armSamples.Skip(1).Any(rotation => Quaternion.Angle(armSamples[0], rotation) > 1))
                            throw new InvalidOperationException("Offline sampling failed to produce distinct poses: " + gait);
                    }
                    finally { graph.Destroy(); }
                }
                File.WriteAllText("Logs/Wardrobe/pose-complete.txt", "Walk and jog sampled at 25%, 50%, 75% using Unity Playables. No Play Mode or gameplay smoke test.");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        private static void PrepareRendering()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/_Project/Settings/Rendering/URP/UniversalRP.asset");
            if (!pipeline) throw new InvalidOperationException("Copy the unchanged production URP assets into the candidate first.");
            GraphicsSettings.defaultRenderPipeline = pipeline; QualitySettings.renderPipeline = pipeline;
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.42f, .42f, .42f);
        }
        public static void CaptureFromCli()
        {
            Guard(); PrepareRendering();
            var root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath));
            try
            {
                var rig = AssetDatabase.LoadAssetAtPath<TopDown3DWardrobeRig>(Output + "/BodyRig.asset");
                var body = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(s => s.sharedMesh == rig.bodyMesh);
                var neutral = AssetDatabase.LoadAssetAtPath<Material>(Output + "/DiagnosticNeutral.mat");
                var skin = AssetDatabase.LoadAssetAtPath<TopDown3DWardrobeDefinition>(Output + "/TextureSkin.asset");
                var jacket = AssetDatabase.LoadAssetAtPath<TopDown3DWardrobeDefinition>(Output + "/Jacket.asset");
                var armor = AssetDatabase.LoadAssetAtPath<TopDown3DWardrobeDefinition>(Output + "/Armor.asset");
                var plate = armor.parts[0].mesh;
                ExportMeshes(root.transform.worldToLocalMatrix * body.transform.localToWorldMatrix, rig.bodyMesh, jacket.parts[0].mesh, plate);
                Capture(root, body, rig, neutral, Array.Empty<TopDown3DWardrobeDefinition>(), "baseline");
                Capture(root, body, rig, neutral, new[] { skin }, "texture");
                Capture(root, body, rig, neutral, new[] { jacket }, "jacket");
                Capture(root, body, rig, neutral, new[] { jacket, armor }, "armor");
                var original = body.bones.Select(b => b.localRotation).ToArray();
                var upperArm = Array.IndexOf(body.bones, root.GetComponentInChildren<Animator>().GetBoneTransform(HumanBodyBones.LeftUpperArm));
                if (upperArm < 0) throw new InvalidOperationException("Mapped upper arm is absent from the source mesh.");
                Capture(root, body, rig, neutral, new[] { jacket, armor }, "arm-stress",
                    () => body.bones[upperArm].localRotation = original[upperArm] * Quaternion.Euler(0, 0, 70));
                for (var i = 0; i < body.bones.Length; i++) body.bones[i].localRotation = original[i];
                File.WriteAllText("Logs/Wardrobe/capture-complete.txt", "Five diagnostic views completed without gameplay scenes or Play Mode.");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        public static void BuildFromCli()
        {
            Guard(); AuditFromCli(); PrepareRendering();
            if (AssetDatabase.IsValidFolder(Output)) throw new InvalidOperationException("Preserve existing pilot assets; use a new version.");
            Directory.CreateDirectory(Output); AssetDatabase.Refresh();
            var root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath));
            try
            {
                var body = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).OrderByDescending(s => s.sharedMesh.vertexCount).First();
                var mesh = body.sharedMesh; var vertices = mesh.vertices; var normals = mesh.normals;
                var points = vertices.Select(v => root.transform.InverseTransformPoint(body.transform.TransformPoint(v))).ToArray();
                var low = points.Min(v => v.y); var height = points.Max(v => v.y) - low;
                Func<int, float> y = i => (points[i].y - low) / height;
                var bones = body.bones;
                var weights = mesh.boneWeights;
                Func<int, bool> hand = i => bones[weights[i].boneIndex0].name.IndexOf("Hand", StringComparison.OrdinalIgnoreCase) >= 0
                    || bones[weights[i].boneIndex0].name.IndexOf("Finger", StringComparison.OrdinalIgnoreCase) >= 0;
                Func<int, bool> jacket = i => y(i) > .47f && y(i) < .825f && !hand(i);
                var rig = ScriptableObject.CreateInstance<TopDown3DWardrobeRig>();
                rig.bodyId = "study.male-base-original.v1"; rig.bodyMesh = mesh;
                rig.bonePaths = bones.Select(b => AnimationUtility.CalculateTransformPath(b, root.transform)).ToArray();
                var regions = Coverage(root, body);
                rig.triangleRegions = regions.ToArray(); Save(rig, "BodyRig.asset");
                var neutral = MakeMaterial("DiagnosticNeutral", new Color(.36f, .39f, .42f));
                var garment = MakeMaterial("DiagnosticGarment", new Color(.48f, .15f, .075f));
                var plate = MakeMaterial("DiagnosticPlate", new Color(.60f, .52f, .34f));
                var texture = new Texture2D(128, 128, TextureFormat.RGBA32, false) { name = "Diagnostic skin grid" };
                var pixels = new Color[128 * 128];
                for (var py = 0; py < 128; py++) for (var px = 0; px < 128; px++)
                    pixels[py * 128 + px] = px % 16 < 2 || py % 16 < 2 ? new Color(.8f, .5f, .15f) : new Color(.18f, .23f, .28f);
                texture.SetPixels(pixels); texture.Apply();
                var texturePath = Output + "/DiagnosticSkin.png"; File.WriteAllBytes(texturePath, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture); AssetDatabase.ImportAsset(texturePath);
                var skinMaterial = MakeMaterial("DiagnosticSkin", Color.white);
                skinMaterial.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
                EditorUtility.SetDirty(skinMaterial); AssetDatabase.SaveAssetIfDirty(skinMaterial);
                var skin = Definition(rig, "pilot.skin.grid", WardrobeLayer.Base, WardrobeRegion.None, WardrobeRegion.None);
                skin.bodyMaterials = Enumerable.Repeat(skinMaterial, mesh.subMeshCount).ToArray(); Save(skin, "TextureSkin.asset");
                var localPerModelUnit = root.transform.lossyScale.x / body.transform.lossyScale.x;
                var jacketMesh = Shell(mesh, jacket, height * .009f * localPerModelUnit, "FittedJacket", -1); Save(jacketMesh, "FittedJacket.asset");
                var chest = ChestIndex(root, body);
                var centerX = (points.Max(v => v.x) + points.Min(v => v.x)) * .5f;
                Func<int, bool> armor = i => y(i) > .60f && y(i) < .77f && Mathf.Abs(points[i].x - centerX) < height * .105f
                    && root.transform.InverseTransformDirection(body.transform.TransformDirection(normals[i])).z > .3f;
                var plateMesh = Shell(mesh, armor, height * .022f * localPerModelUnit, "RigidChestPlate", chest); Save(plateMesh, "RigidChestPlate.asset");
                var jacketDefinition = Definition(rig, "pilot.jacket.shell", WardrobeLayer.Outer, WardrobeRegion.Torso | WardrobeRegion.Arms, WardrobeRegion.Torso | WardrobeRegion.Arms);
                jacketDefinition.parts = new[] { Part(jacketMesh, rig, garment, rig.bonePaths[chest]) }; Save(jacketDefinition, "Jacket.asset");
                var armorDefinition = Definition(rig, "pilot.armor.chest", WardrobeLayer.Armor, WardrobeRegion.Torso, WardrobeRegion.None);
                armorDefinition.parts = new[] { Part(plateMesh, rig, plate, rig.bonePaths[chest]) }; Save(armorDefinition, "Armor.asset");
                File.WriteAllText("Logs/Wardrobe/pilot-metrics.json", JsonUtility.ToJson(new Metrics { sourceVertices = mesh.vertexCount,
                    sourceTriangles = mesh.triangles.Length / 3, jacketTriangles = jacketMesh.triangles.Length / 3,
                    armorTriangles = plateMesh.triangles.Length / 3, sourceModelHeight = height, animationSceneScale = .08989948f,
                    sourceBones = bones.Length, hiddenTriangles = regions.Count(v => v != 0) }, true));
                ExportMeshes(root.transform.worldToLocalMatrix * body.transform.localToWorldMatrix, mesh, jacketMesh, plateMesh);
                Capture(root, body, rig, neutral, new[] { skin }, "texture");
                Capture(root, body, rig, neutral, new[] { jacketDefinition }, "jacket");
                Capture(root, body, rig, neutral, new[] { jacketDefinition, armorDefinition }, "armor");
                AssetDatabase.SaveAssets();
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        private static Mesh Shell(Mesh source, Func<int, bool> select, float offset, string name, int rigidBone)
        {
            var original = source.vertices; var normals = source.normals; var uv = source.uv; var weights = source.boneWeights;
            var indices = source.triangles; var map = new Dictionary<int, int>(); var triangleList = new List<int>();
            var vertices = new List<Vector3>(); var uvs = new List<Vector2>(); var copiedWeights = new List<BoneWeight>();
            for (var t = 0; t < indices.Length; t += 3)
            {
                if (!select(indices[t]) || !select(indices[t + 1]) || !select(indices[t + 2])) continue;
                for (var j = 0; j < 3; j++)
                {
                    var i = indices[t + j];
                    if (!map.TryGetValue(i, out var next))
                    {
                        next = vertices.Count; map.Add(i, next); vertices.Add(original[i] + normals[i] * offset);
                        uvs.Add(uv.Length == original.Length ? uv[i] : Vector2.zero);
                        copiedWeights.Add(rigidBone < 0 ? weights[i] : new BoneWeight { boneIndex0 = rigidBone, weight0 = 1 });
                    }
                    triangleList.Add(next);
                }
            }
            if (triangleList.Count == 0) throw new InvalidOperationException("Empty fitted shell: " + name);
            var mesh = new Mesh { name = name }; mesh.SetVertices(vertices); mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangleList, 0); mesh.bindposes = source.bindposes; mesh.boneWeights = copiedWeights.ToArray();
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds(); return mesh;
        }
        private static int ChestIndex(GameObject root, SkinnedMeshRenderer body)
        {
            var animator = root.GetComponentInChildren<Animator>();
            if (!animator || !animator.isHuman) throw new InvalidOperationException("A qualified Humanoid is required.");
            var chest = animator.GetBoneTransform(HumanBodyBones.Chest);
            var index = Array.IndexOf(body.bones, chest);
            if (!chest || index < 0) throw new InvalidOperationException("No mapped chest bone for rigid plate.");
            return index;
        }
        private static List<int> Coverage(GameObject root, SkinnedMeshRenderer body)
        {
            var mesh = body.sharedMesh; var bones = body.bones; var weights = mesh.boneWeights;
            var points = mesh.vertices.Select(v => root.transform.InverseTransformPoint(body.transform.TransformPoint(v))).ToArray();
            var low = points.Min(p => p.y); var height = points.Max(p => p.y) - low;
            var center = (points.Min(p => p.x) + points.Max(p => p.x)) * .5f;
            var tags = new int[points.Length];
            for (var i = 0; i < points.Length; i++)
            {
                var y = (points[i].y - low) / height;
                var name = bones[weights[i].boneIndex0].name;
                if (y <= .5f || y >= .79f || name.IndexOf("Hand", StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("Finger", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                tags[i] = (int)(Mathf.Abs(points[i].x - center) < height * .105f ? WardrobeRegion.Torso : WardrobeRegion.Arms);
            }
            var result = new List<int>();
            for (var s = 0; s < mesh.subMeshCount; s++)
            {
                var t = mesh.GetTriangles(s);
                for (var i = 0; i < t.Length; i += 3)
                    result.Add(tags[t[i]] == tags[t[i + 1]] && tags[t[i]] == tags[t[i + 2]] ? tags[t[i]] : 0);
            }
            return result;
        }
        private static WardrobePart Part(Mesh mesh, TopDown3DWardrobeRig rig, Material material, string root)
        { var bounds = rig.bodyMesh.bounds; bounds.Expand(bounds.size.magnitude); return new WardrobePart {
            mesh = mesh, materials = new[] { material }, bonePaths = (string[])rig.bonePaths.Clone(), rootBonePath = root, bounds = bounds }; }
        private static TopDown3DWardrobeDefinition Definition(TopDown3DWardrobeRig rig, string id, WardrobeLayer layer, WardrobeRegion occupies, WardrobeRegion hides)
        { var d = ScriptableObject.CreateInstance<TopDown3DWardrobeDefinition>(); d.rig = rig; d.visualId = id; d.layer = layer; d.occupies = occupies; d.hides = hides; return d; }
        private static Material MakeMaterial(string name, Color color)
        { var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name }; m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", .18f); m.SetFloat("_Cull", 0); Save(m, name + ".mat"); return m; }
        private static void Save(UnityEngine.Object asset, string name) => AssetDatabase.CreateAsset(asset, Output + "/" + name);
        private static void Capture(GameObject root, SkinnedMeshRenderer body, TopDown3DWardrobeRig rig, Material neutral, TopDown3DWardrobeDefinition[] definitions, string label, Action samplePose = null)
        {
            var originalMaterials = body.sharedMaterials; body.sharedMaterials = Enumerable.Repeat(neutral, body.sharedMesh.subMeshCount).ToArray();
            var presenter = root.GetComponent<TopDown3DWardrobePresentation>(); var ownsPresenter = !presenter;
            if (ownsPresenter) { presenter = root.AddComponent<TopDown3DWardrobePresentation>(); presenter.Configure(root.transform, body, rig); }
            GameObject cameraObject = null, lightObject = null; RenderTexture target = null; Texture2D pixels = null;
            var bakedObjects = new List<GameObject>(); var bakedMeshes = new List<Mesh>(); var hidden = new List<SkinnedMeshRenderer>();
            var previousTarget = RenderTexture.active;
            try
            {
                if (ownsPresenter && !presenter.TryReplace(definitions, out var error)) throw new InvalidOperationException(error);
                samplePose?.Invoke();
                var bounds = body.bounds;
                // Offline sampling does not run the PlayerLoop's skinning update. Bake the current bone pose explicitly.
                foreach (var sourceSkin in root.GetComponentsInChildren<SkinnedMeshRenderer>().Where(s => s.enabled))
                {
                    var baked = WardrobePoseBake.Bake(sourceSkin); bakedMeshes.Add(baked);
                    var node = new GameObject("Baked inspection " + sourceSkin.name);
                    node.transform.SetPositionAndRotation(sourceSkin.transform.position, sourceSkin.transform.rotation);
                    node.transform.localScale = sourceSkin.transform.lossyScale;
                    node.AddComponent<MeshFilter>().sharedMesh = baked;
                    node.AddComponent<MeshRenderer>().sharedMaterials = sourceSkin.sharedMaterials;
                    sourceSkin.enabled = false; hidden.Add(sourceSkin); bakedObjects.Add(node);
                }
                cameraObject = new GameObject("Wardrobe inspection camera"); var camera = cameraObject.AddComponent<Camera>();
                camera.backgroundColor = new Color(.085f, .095f, .12f); camera.clearFlags = CameraClearFlags.SolidColor;
                camera.transform.position = bounds.center + new Vector3(.3f, .15f, 1.8f) * bounds.size.y;
                camera.transform.LookAt(bounds.center); camera.orthographic = true; camera.orthographicSize = bounds.size.y * .63f;
                camera.nearClipPlane = .01f; camera.farClipPlane = bounds.size.y * 10;
                lightObject = new GameObject("Wardrobe inspection light"); var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional; light.intensity = 1.3f; light.transform.rotation = Quaternion.Euler(25, 160, 0);
                target = new RenderTexture(768, 960, 24); camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                pixels = new Texture2D(768, 960, TextureFormat.RGB24, false); pixels.ReadPixels(new Rect(0, 0, 768, 960), 0, 0); pixels.Apply();
                File.WriteAllBytes("Logs/Wardrobe/" + label + ".png", pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previousTarget; UnityEngine.Object.DestroyImmediate(pixels); UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject); if (target) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
                foreach (var node in bakedObjects) UnityEngine.Object.DestroyImmediate(node);
                foreach (var mesh in bakedMeshes) UnityEngine.Object.DestroyImmediate(mesh);
                foreach (var renderer in hidden) if (renderer) renderer.enabled = true;
                if (ownsPresenter) UnityEngine.Object.DestroyImmediate(presenter); body.sharedMaterials = originalMaterials;
            }
        }
        private static void ExportMeshes(Matrix4x4 bodyToModel, params Mesh[] meshes)
        {
            File.WriteAllText("Logs/Wardrobe/unity-pilot-meshes.json", JsonUtility.ToJson(new Interchange {
                meshes = meshes.Select(m => new MeshRecord { name = m.name, vertices = m.vertices.Select(v => bodyToModel.MultiplyPoint3x4(v)).ToArray(), triangles = m.triangles,
                    bindposes = m.bindposes, weights = m.boneWeights }).ToArray() }, true));
        }
        [Serializable] private sealed class Interchange { public MeshRecord[] meshes; }
        [Serializable] private sealed class MeshRecord { public string name; public Vector3[] vertices; public int[] triangles; public Matrix4x4[] bindposes; public BoneWeight[] weights; }
        [Serializable] private sealed class Metrics { public int sourceVertices, sourceTriangles, jacketTriangles, armorTriangles, sourceBones, hiddenTriangles; public float sourceModelHeight, animationSceneScale; }
        public static void AuditFromCli()
        {
            Guard();
            Directory.CreateDirectory("Logs/Wardrobe");
            foreach (var path in new[] { ModelPath, "Assets/_Project/Art/Characters/Prototype/UnityStandardHumanoid/defaultmale_rig.fbx" })
            {
                var root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                try
                {
                    var report = new Audit { model = path, unity = Application.unityVersion,
                        skins = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(s => new SkinAudit {
                            name = s.name, vertices = s.sharedMesh.vertexCount, triangles = s.sharedMesh.triangles.Length / 3,
                            bounds = s.sharedMesh.bounds, transform = s.transform.localToWorldMatrix,
                            bones = s.bones.Select(b => AnimationUtility.CalculateTransformPath(b, root.transform)).ToArray(),
                            materials = s.sharedMaterials.Select(m => m ? m.name : "missing").ToArray()
                        }).ToArray() };
                    File.WriteAllText("Logs/Wardrobe/" + Path.GetFileNameWithoutExtension(path) + ".json", JsonUtility.ToJson(report, true));
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
        }
        private static void Guard()
        {
            var project = Path.GetDirectoryName(Application.dataPath);
            if (!Application.isBatchMode || !File.Exists(Path.Combine(project, ".wardrobe-validation")) || EditorApplication.isPlaying)
                throw new InvalidOperationException("Wardrobe authoring requires the marked background validation project in Edit Mode.");
        }
        [Serializable] private sealed class Audit { public string model, unity; public SkinAudit[] skins; }
        [Serializable] private sealed class SkinAudit
        { public string name; public int vertices, triangles; public Bounds bounds; public Matrix4x4 transform; public string[] bones, materials; }
    }
}
