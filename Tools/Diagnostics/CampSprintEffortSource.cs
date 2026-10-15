using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

// Private source preparation and pose measurement, never gameplay simulation.
public static class CampSprintEffortSource
{
    private const string Source = "Assets/_Project/Art/Animations/Mixamo/Reference/Locomotion/SprintForwardTravelAlternative.fbx";
    public const string Derived = AnimationPlaygroundClipDerivatives.Folder + "/SprintEffortTwoCycles.anim";
    private sealed class Region
    {
        public SkinnedMeshRenderer skin;
        public int[] indices;
    }

    public static string Prepare()
    {
        var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
            || Directory.Exists(Path.Combine(root, ".git"))
            || !File.Exists(Path.Combine(root, AnimationPlaygroundLayoutAuthoring.CandidateMarker)))
            throw new InvalidOperationException("Use the marked offline candidate in stable Edit Mode.");
        var directory = Path.Combine(root, "LocomotionImplementation20261014/SprintEffort20261015");
        if (Directory.Exists(directory) || File.Exists(Derived) || File.Exists(Derived + ".meta"))
            throw new IOException("Preserve previous source candidates and their GUIDs.");
        Directory.CreateDirectory(directory);
        string Hash(string path)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
        }
        var original = Hash(Source); var originalMeta = Hash(Source + ".meta");
        var clip = AnimationPlaygroundClipDerivatives.CreateLoop(Source, "SprintEffortTwoCycles", 0, 2);
        foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            if (binding.propertyName == "RootT.x" || binding.propertyName == "RootT.z")
                AnimationUtility.SetEditorCurve(clip, binding, AnimationCurve.Constant(0, clip.length, 0));
        EditorUtility.SetDirty(clip); AssetDatabase.SaveAssetIfDirty(clip);
        var bindings = AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(AnimationPlaygroundPlayerAuthoring.BindingPath);
        var preview = EditorSceneManager.NewPreviewScene();
        GameObject instance = null; PlayableGraph graph = default; Mesh baked = null;
        var qualityBefore = QualitySettings.skinWeights;
        try
        {
            QualitySettings.skinWeights = SkinWeights.FourBones;
            instance = PrefabUtility.InstantiatePrefab(bindings.targetModel, preview) as GameObject;
            instance.transform.SetPositionAndRotation(bindings.visualOffset, Quaternion.identity);
            instance.transform.localScale = Vector3.one * bindings.visualScale;
            var animator = instance.GetComponentInChildren<Animator>(); animator.avatar = bindings.targetAvatar;
            animator.applyRootMotion = false; animator.stabilizeFeet = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var left = animator.GetBoneTransform(HumanBodyBones.LeftFoot); var right = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            var regions = new List<Region>(); var ids = new List<object>();
            foreach (var skin in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                skin.quality = SkinQuality.Auto;
                using var counts = skin.sharedMesh.GetBonesPerVertex(); using var weights = skin.sharedMesh.GetAllBoneWeights();
                var indices = new List<int>(); var cursor = 0;
                for (var vertex = 0; vertex < counts.Length; vertex++)
                {
                    var lw = 0f; var rw = 0f;
                    for (var i = 0; i < counts[vertex]; i++)
                    {
                        var weight = weights[cursor++]; var bone = skin.bones[weight.boneIndex];
                        if (bone.IsChildOf(left)) lw += weight.weight;
                        if (bone.IsChildOf(right)) rw += weight.weight;
                    }
                    if (Mathf.Max(lw, rw) < .6f) continue;
                    indices.Add(vertex); ids.Add(new { renderer = AnimationUtility.CalculateTransformPath(skin.transform, instance.transform), vertex, side = lw >= rw ? 0 : 1 });
                }
                regions.Add(new Region { skin = skin, indices = indices.ToArray() });
            }
            if (ids.Count == 0) throw new InvalidDataException("No tracked sole vertices.");
            graph = PlayableGraph.Create("Offline sprint effort source"); graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var playable = AnimationClipPlayable.Create(graph, clip); playable.SetApplyFootIK(false); playable.SetApplyPlayableIK(false); playable.SetSpeed(0);
            AnimationPlayableOutput.Create(graph, "Measured sprint", animator).SetSourcePlayable(playable); graph.Play();
            var roles = new[] { HumanBodyBones.Hips, HumanBodyBones.Head, HumanBodyBones.Chest,
                HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand,
                HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand,
                HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot,
                HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot };
            var bones = roles.Select(animator.GetBoneTransform).ToArray();
            if (bones.Any(b => b == null)) throw new InvalidDataException("Missing required Humanoid bone.");
            var frames = new List<object>(); var points = new List<Vector3>(); baked = new Mesh();
            var intervals = Mathf.CeilToInt(clip.length * 120);
            for (var frame = 0; frame <= intervals; frame++)
            {
                var seconds = clip.length * frame / intervals;
                playable.SetTime(seconds); graph.Evaluate(0);
                var positions = new float[ids.Count][]; var cursor = 0;
                foreach (var region in regions)
                {
                    region.skin.BakeMesh(baked, true); baked.GetVertices(points);
                    foreach (var index in region.indices) positions[cursor++] = V(region.skin.transform.TransformPoint(points[index]));
                }
                frames.Add(new { seconds, vertices = positions, left = V(left.position), right = V(right.position),
                    hips = V(bones[0].position), head = V(bones[1].position), bones = bones.Select(b => V(b.position)).ToArray() });
            }
            var output = Path.Combine(directory, "SprintEffortTwoCycles.json");
            File.WriteAllText(output, Newtonsoft.Json.JsonConvert.SerializeObject(new {
                state = "unqualified-in-place-sprint-effort-source", path = Derived, clipName = clip.name, duration = clip.length,
                sampleRate = intervals / clip.length, floor = -bindings.capsuleHeight * .5f, scale = bindings.visualScale,
                footIK = false, skinInfluences = 4, globalSkinWeights = QualitySettings.skinWeights.ToString(),
                ids, boneRoles = roles.Select(r => r.ToString()).ToArray(), frames }));
            if (Hash(Source) != original || Hash(Source + ".meta") != originalMeta) throw new InvalidDataException("Original source changed.");
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            File.WriteAllText(Path.Combine(directory, "recipe.json"), Newtonsoft.Json.JsonConvert.SerializeObject(new {
                source = Source, sourceSha256 = original, sourceMetaSha256 = originalMeta, derivative = Derived,
                derivativeSha256 = Hash(Derived), derivativeMetaSha256 = Hash(Derived + ".meta"),
                repetitions = 2, originPhase = 0, horizontalRoot = "RootT.x/z zero; motor remains translation owner",
                settings.loopTime, settings.loopBlend, clip.length, clip.isHumanMotion,
                state = "source-prepared-not-bound-or-qualified" }, Newtonsoft.Json.Formatting.Indented));
            return output;
        }
        finally
        {
            if (graph.IsValid()) graph.Destroy(); if (baked != null) Object.DestroyImmediate(baked);
            if (instance != null) Object.DestroyImmediate(instance);
            QualitySettings.skinWeights = qualityBefore; EditorSceneManager.ClosePreviewScene(preview);
        }
    }
    private static float[] V(Vector3 point) => new[] { point.x, point.y, point.z };
}
