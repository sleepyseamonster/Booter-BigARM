using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

// Offline source measurements only. Raw retargeted tracks stay in the private candidate.
public static class CampStrafeSourceAudit
{
    public static string PrepareWalkLoops()
    {
        var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (!Application.isBatchMode || Directory.Exists(Path.Combine(root, ".git"))
            || !File.Exists(Path.Combine(root, ".animation-playground-validation-project")))
            throw new InvalidOperationException("Author derivatives only in the marked offline candidate.");
        foreach (var side in new[] { "Left", "Right" })
        {
            var clip = AnimationPlaygroundClipDerivatives.CreateLoop(
                "Assets/_Project/Art/Animations/Mixamo/Locomotion/Strafe/StrafeWalk" + side + ".fbx",
                "StandingStrafeWalk" + side + "Review", 0, 2);
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                if (binding.propertyName == "RootT.x" || binding.propertyName == "RootT.z")
                    AnimationUtility.SetEditorCurve(clip, binding, AnimationCurve.Constant(0, clip.length, 0));
            EditorUtility.SetDirty(clip); AssetDatabase.SaveAssetIfDirty(clip);
        }
        return "Two complete walk cycles retained; horizontal RootT removed. Sources unchanged.";
    }
    public static string Run() => RunSelected(true);
    public static string RunSources() => RunSelected(false);
    private static string RunSelected(bool walkLoops)
    {
        var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
            || Directory.Exists(Path.Combine(root, ".git"))
            || !File.Exists(Path.Combine(root, ".animation-playground-validation-project")))
            throw new InvalidOperationException("Use the compiled, marked offline candidate.");
        var folder = Path.Combine(root, "LocomotionImplementation20261014/" + (walkLoops ? "StrafeWalkLoops20261015" : "StrafeSources20261015"));
        if (Directory.Exists(folder)) throw new IOException("Preserve the existing source audit.");
        Directory.CreateDirectory(folder);
        var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("BooterBigArm.Tests.AnimationPlaygroundForwardReviewTests+Fixture")).First(t => t != null);
        using var fixture = (IDisposable)Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, new object[] { true, SkinWeights.FourBones, "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/ExpandedForwardGaitReview.asset" }, null);
        T Field<T>(string name) => (T)type.GetField(name).GetValue(fixture);
        var driver = Field<TopDown3DPlayerAnimationDriver>("Driver");
        var graph = Field<PlayableGraph>("Graph");
        var output = (AnimationPlayableOutput)graph.GetOutput(0);
        var original = output.GetSourcePlayable();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var visual = (GameObject)typeof(TopDown3DPlayerAnimationDriver).GetField("visualInstance", flags).GetValue(driver);
        var basePosition = (Vector3)typeof(TopDown3DPlayerAnimationDriver).GetField("visualLocalPosition", flags).GetValue(driver);
        var policy = Field<AnimationPlaygroundForwardLocomotionProfile>("Policy");
        var projection = policy.footProjection;
        var bones = projection.bonePaths.Select(p => p.Length == 0 ? visual.transform : visual.transform.Find(p)).ToArray();
        var animator = driver.HumanoidAnimator;
        var left = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        var right = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        Vector3 Point(AnimationPlaygroundFootVertex vertex)
        {
            var result = Vector3.zero;
            foreach (var influence in vertex.points) result += bones[influence.bone].TransformPoint(influence.position) * influence.weight;
            return driver.transform.InverseTransformPoint(result);
        }
        float[] Coordinates(Vector3 point) => new[] { point.x, point.y, point.z };
        var sources = walkLoops ? new[] {
            "Assets/_Project/Art/Animations/Mixamo/Derived/StandingStrafeWalkLeftReview.anim",
            "Assets/_Project/Art/Animations/Mixamo/Derived/StandingStrafeWalkRightReview.anim" } : new[] {
            "Assets/_Project/Art/Animations/Mixamo/Derived/DirectionalStrafeLeftTwoCycles.anim",
            "Assets/_Project/Art/Animations/Mixamo/Derived/DirectionalStrafeRightTwoCycles.anim",
            "Assets/_Project/Art/Animations/Mixamo/Locomotion/Strafe/StrafeWalkLeft.fbx",
            "Assets/_Project/Art/Animations/Mixamo/Locomotion/Strafe/StrafeWalkRight.fbx",
            "Assets/_Project/Art/Animations/Mixamo/Locomotion/Strafe/StrafeRunLeft.fbx",
            "Assets/_Project/Art/Animations/Mixamo/Locomotion/Strafe/StrafeRunRight.fbx" };
        var summary = new List<object>();
        foreach (var path in sources)
        {
            var source = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Single(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal));
            var clip = Object.Instantiate(source);
            try
            {
                foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                    if (binding.propertyName == "RootT.x" || binding.propertyName == "RootT.z")
                        AnimationUtility.SetEditorCurve(clip, binding, AnimationCurve.Constant(0, clip.length, 0));
                var playable = AnimationClipPlayable.Create(graph, clip);
                playable.SetApplyFootIK(false); playable.SetApplyPlayableIK(false); playable.SetSpeed(0);
                output.SetSourcePlayable(playable);
                var samples = new List<object>();
                var count = Mathf.CeilToInt(clip.length * 120);
                for (var frame = 0; frame <= count; frame++)
                {
                    visual.transform.localPosition = basePosition;
                    playable.SetTime(frame * (double)clip.length / count); graph.Evaluate(0);
                    samples.Add(new { time = frame * (double)clip.length / count,
                        left = Coordinates(driver.transform.InverseTransformPoint(left.position)), right = Coordinates(driver.transform.InverseTransformPoint(right.position)),
                        points = projection.vertices.Select(v => Coordinates(Point(v))).ToArray() });
                }
                var name = Path.GetFileNameWithoutExtension(path);
                string Hash(string p) { using var sha = System.Security.Cryptography.SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Path.Combine(root, p)))).Replace("-", "").ToLowerInvariant(); }
                var data = new { path, sha256 = Hash(path), metaSha256 = Hash(path + ".meta"), source.length,
                    source.isLooping, source.isHumanMotion, vertices = projection.vertices.Select(v => v.points.Select(p => new { bone = projection.bonePaths[p.bone], p.weight }).ToArray()).ToArray(), samples };
                File.WriteAllText(Path.Combine(folder, name + ".json"), Newtonsoft.Json.JsonConvert.SerializeObject(data));
                summary.Add(new { path, source.length, source.isLooping, samples = samples.Count, vertices = projection.vertices.Length });
                output.SetSourcePlayable(original); graph.DestroyPlayable(playable);
            }
            finally { Object.DestroyImmediate(clip); }
        }
        File.WriteAllText(Path.Combine(folder, "summary.json"), Newtonsoft.Json.JsonConvert.SerializeObject(summary, Newtonsoft.Json.Formatting.Indented));
        return Path.Combine(folder, "summary.json");
    }
}
