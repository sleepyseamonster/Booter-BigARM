using System;
using System.Collections.Generic;
using System.Diagnostics;
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

// Offline source selection. Stable mesh indices retain contact identity across frames.
// Never saves a scene, changes a source asset, or enters Play Mode.
public static class CampLocomotionSoleTracks
{
    private sealed class Region
    {
        public SkinnedMeshRenderer skin;
        public int[] indices;
        public int[] sides;
    }

    public static string Run() => RunCore("SoleTracksV2", null);
    public static string RunSprintReview() => RunCore("GaitPaceTuning20261015/SoleTracks", new[] {
        "Assets/_Project/Art/Animations/Mixamo/Derived/SprintArmDriveReview.anim" });
    public static string RunFullSprintReview() => RunCore("FullSprintPreparation20261015/SoleTracks", new[] {
        "Assets/_Project/Art/Animations/Mixamo/Derived/FullSprintMixamoReview.anim",
        "Assets/_Project/Art/Animations/Mixamo/Derived/FullSprintStandardReview.anim" });
    private static string RunCore(string destinationName, string[] selectedPaths)
    {
        var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode
            || Directory.Exists(Path.Combine(root, ".git"))
            || !File.Exists(Path.Combine(root, AnimationPlaygroundLayoutAuthoring.CandidateMarker)))
            throw new InvalidOperationException("Use the marked offline candidate.");
        var destination = Path.Combine(root, "LocomotionImplementation20261014", destinationName);
        if (Directory.Exists(destination)) throw new IOException("Preserve prior measurements.");
        Directory.CreateDirectory(destination);
        var bindings = AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(AnimationPlaygroundPlayerAuthoring.BindingPath);
        if (bindings == null || !bindings.ValidatePlayer(out var error))
            throw new InvalidDataException("Restore valid baseline bindings before sampling.");
        var paths = selectedPaths ?? new[]
        {
            "Assets/_Project/Art/Animations/Mixamo/Locomotion/StandardWalk.fbx",
            "Assets/_Project/Art/Animations/Mixamo/Locomotion/StandardRun.fbx",
            "Assets/_Project/Art/Animations/Mixamo/Locomotion/TwoCycleSprint.fbx",
            "Assets/_Project/Art/Animations/Mixamo/Reference/Locomotion/SprintForwardTravelAlternative.fbx",
            "Assets/_Project/Art/Characters/Animations/MoCapCentral/CoreMotion/Locomotion/MCU_am_Loco_Walk_Fwd_NoRM.FBX",
            "Assets/_Project/Art/Characters/Animations/MoCapCentral/CoreMotion/Locomotion/MCU_am_Loco_Jog_Fwd_NoRM.FBX",
            "Assets/_Project/Art/Characters/Animations/MoCapCentral/CoreMotion/Locomotion/MCU_am_Loco_Run_Fwd_NoRM.FBX",
            "Assets/_Project/Art/Animations/Mixamo/Derived/WalkTwoCycles.anim",
            "Assets/_Project/Art/Animations/Mixamo/Derived/RunTwoCycles.anim",
            "Assets/_Project/Art/Animations/Mixamo/Derived/SprintTwoCycles.anim"
        };
        var summaries = new List<object>();
        var qualityBefore = QualitySettings.skinWeights;
        try
        {
        QualitySettings.skinWeights = SkinWeights.FourBones;
        foreach (var path in paths)
        {
            var clip = AnimationPlaygroundClipDerivatives.LoadClip(path);
            var preview = EditorSceneManager.NewPreviewScene();
            GameObject instance = null;
            PlayableGraph graph = default;
            Mesh baked = null;
            try
            {
                instance = PrefabUtility.InstantiatePrefab(bindings.targetModel, preview) as GameObject;
                instance.transform.SetPositionAndRotation(bindings.visualOffset, Quaternion.identity);
                instance.transform.localScale = Vector3.one * bindings.visualScale;
                var animator = instance.GetComponentInChildren<Animator>();
                animator.avatar = bindings.targetAvatar;
                animator.applyRootMotion = false;
                animator.stabilizeFeet = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var left = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                var right = animator.GetBoneTransform(HumanBodyBones.RightFoot);
                var regions = new List<Region>();
                var ids = new List<object>();
                foreach (var skin in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    skin.quality = SkinQuality.Auto;
                    using var counts = skin.sharedMesh.GetBonesPerVertex();
                    using var weights = skin.sharedMesh.GetAllBoneWeights();
                    var indices = new List<int>(); var sides = new List<int>(); var cursor = 0;
                    for (var vertex = 0; vertex < counts.Length; vertex++)
                    {
                        var lw = 0f; var rw = 0f;
                        for (var i = 0; i < counts[vertex]; i++)
                        {
                            var w = weights[cursor++];
                            var bone = skin.bones[w.boneIndex];
                            if (bone.IsChildOf(left)) lw += w.weight;
                            if (bone.IsChildOf(right)) rw += w.weight;
                        }
                        if (Mathf.Max(lw, rw) < .6f) continue;
                        var side = lw >= rw ? 0 : 1;
                        indices.Add(vertex); sides.Add(side);
                        ids.Add(new { renderer = AnimationUtility.CalculateTransformPath(skin.transform, instance.transform), vertex, side });
                    }
                    regions.Add(new Region { skin = skin, indices = indices.ToArray(), sides = sides.ToArray() });
                }
                if (ids.Count == 0) throw new InvalidDataException("No stable weighted foot vertices.");
                graph = PlayableGraph.Create("Offline tracked sole source selection");
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var playable = AnimationClipPlayable.Create(graph, clip);
                playable.SetApplyFootIK(false); playable.SetApplyPlayableIK(false); playable.SetSpeed(0);
                AnimationPlayableOutput.Create(graph, "Native source pose", animator).SetSourcePlayable(playable);
                graph.Play();
                var intervals = Mathf.CeilToInt(clip.length * 120);
                var frames = new List<object>(intervals + 1);
                var points = new List<Vector3>();
                baked = new Mesh();
                var timer = Stopwatch.StartNew();
                for (var frame = 0; frame <= intervals; frame++)
                {
                    var seconds = clip.length * frame / intervals;
                    playable.SetTime(seconds); graph.Evaluate(frame == 0 ? 0 : clip.length / intervals);
                    var positions = new float[ids.Count][];
                    var cursor = 0;
                    foreach (var region in regions)
                    {
                        region.skin.BakeMesh(baked, true); baked.GetVertices(points);
                        foreach (var index in region.indices)
                            positions[cursor++] = V(region.skin.transform.TransformPoint(points[index]));
                    }
                    frames.Add(new { seconds, vertices = positions, left = V(left.position), right = V(right.position),
                        hips = V(animator.GetBoneTransform(HumanBodyBones.Hips).position),
                        head = V(animator.GetBoneTransform(HumanBodyBones.Head).position) });
                }
                timer.Stop();
                var name = Path.GetFileNameWithoutExtension(path);
                var data = new { state = "unqualified-native-source-tracks", path, clipName = clip.name,
                    duration = clip.length, sampleRate = intervals / clip.length, floor = -bindings.capsuleHeight * .5f,
                    scale = bindings.visualScale, footIK = false, skinInfluences = 4,
                    globalSkinWeights = QualitySettings.skinWeights.ToString(), rendererQuality = "Auto",
                    nativeRootMotionDisabled = true, measurementMilliseconds = timer.Elapsed.TotalMilliseconds,
                    ids, frames };
                File.WriteAllText(Path.Combine(destination, name + ".json"), Newtonsoft.Json.JsonConvert.SerializeObject(data));
                summaries.Add(new { name, path, frames = frames.Count, vertices = ids.Count,
                    duration = clip.length, measurementMilliseconds = timer.Elapsed.TotalMilliseconds });
            }
            finally
            {
                if (graph.IsValid()) graph.Destroy();
                if (baked != null) Object.DestroyImmediate(baked);
                if (instance != null) Object.DestroyImmediate(instance);
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }
        var summary = Path.Combine(destination, "summary.json");
        File.WriteAllText(summary, Newtonsoft.Json.JsonConvert.SerializeObject(summaries, Newtonsoft.Json.Formatting.Indented));
        return summary;
        }
        finally { QualitySettings.skinWeights = qualityBefore; }
    }

    private static float[] V(Vector3 p) => new[] { p.x, p.y, p.z };
}
