using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

// Source screen, not a pivot/contact qualification or runtime installation.
public static class CampTurnSourceScreen
{
    public static string Run(string name = "TurnSourceScreenV7", string rapidSource = "Assets/_Project/Art/Characters/Prototype/UnityStandardHumanoid/Animations/m@RapidTurningLeft.fbx")
    {
        var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
            || Directory.Exists(Path.Combine(root, ".git")) || !File.Exists(Path.Combine(root, ".animation-playground-validation-project")))
            throw new InvalidOperationException("Use the marked, compiled, non-playing candidate.");
        if (!Regex.IsMatch(name, @"^[A-Za-z0-9]+$")) throw new ArgumentException("Use a simple immutable run name.");
        if (!rapidSource.StartsWith("Assets/_Project/Art/Characters/", StringComparison.Ordinal) || rapidSource.Contains(".."))
            throw new ArgumentException("Use an imported project-owned character source.");
        var output = Path.Combine(root, "LocomotionImplementation20261014", name);
        if (Directory.Exists(output)) throw new IOException("Preserve existing measurements.");
        const string bindingPath = "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/PlayerBindings.asset";
        var bindings = AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(bindingPath);
        var model = AssetDatabase.GetAssetPath(bindings.targetModel);
        var paths = new[] {
            "Assets/_Project/Art/Characters/Animations/MoCapCentral/CoreMotion/Locomotion/MCU_am_Loco_Jog_Fwd_Pivot_NoRM.FBX",
            "Assets/_Project/Art/Characters/Animations/MoCapCentral/CoreMotion/Locomotion/MCU_am_Loco_Jog_Fwd_Pivot_NoRM_Mirror.FBX",
            rapidSource
        };
        var sourcePaths = paths.SelectMany(p => new[] { p, p + ".meta" }).Concat(new[] { model, model + ".meta", bindingPath,
            "Assets/_Project/Scripts/Editor/AnimationPlayground/AnimationPlaygroundMotionSampler.cs" }).ToArray();
        string Hash(string path) { using var sha = SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Path.Combine(root, path)))).Replace("-", "").ToLowerInvariant(); }
        var sources = sourcePaths.ToDictionary(p => p, Hash);
        var quality = QualitySettings.skinWeights; var rows = new List<object>();
        Directory.CreateDirectory(output);
        try
        {
            QualitySettings.skinWeights = SkinWeights.FourBones;
            foreach (var path in paths)
            {
                var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToArray();
                if (clips.Length == 0) { rows.Add(new { path, available = false, reason = "No imported clip" }); continue; }
                foreach (var clip in clips)
                {
                if (!clip.isHumanMotion) { rows.Add(new { path, available = false, clip = clip.name, reason = "No imported Humanoid clip" }); continue; }
                var sample = MeasureNamed(bindings, path, clip, false);
                var applied = MeasureNamed(bindings, path, clip, true);
                if (!sample.finite) throw new InvalidDataException("Nonfinite sampled turn.");
                var first = sample.frames[0];
                var basis = Vector3.ProjectOnPlane(first.hipsRotation * Vector3.forward, Vector3.up).sqrMagnitude > .25f ? Vector3.forward : Vector3.right;
                var prior = Heading(first.hipsRotation * basis); var total = 0f; var low = 0f; var high = 0f;
                var motion = 0f; var minimumSole = float.PositiveInfinity; var maxHeight = float.NegativeInfinity;
                foreach (var frame in sample.frames)
                {
                    var current = Heading(frame.hipsRotation * basis); total += Mathf.DeltaAngle(prior, current); prior = current;
                    low = Mathf.Min(low, total); high = Mathf.Max(high, total);
                    motion = Mathf.Max(motion, Vector3.Distance(frame.leftFoot, first.leftFoot), Vector3.Distance(frame.rightFoot, first.rightFoot));
                    minimumSole = Mathf.Min(minimumSole, frame.leftSole.y, frame.rightSole.y); maxHeight = Mathf.Max(maxHeight, frame.bodyMaximum.y);
                }
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                var rotationProperties = AnimationUtility.GetCurveBindings(clip).Where(b => b.propertyName.Contains("RootQ") || b.propertyName.Contains("MotionQ"))
                    .Select(b => b.propertyName).Distinct().ToArray();
                if (motion < .01f) throw new InvalidDataException("Inactive or frozen source sampler.");
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(clip, out string clipGuid, out long localId);
                File.WriteAllText(Path.Combine(output, clip.name + "-private-poses.json"),
                    JsonUtility.ToJson(sample), new UTF8Encoding(false));
                File.WriteAllText(Path.Combine(output, clip.name + "-private-applied-poses.json"),
                    JsonUtility.ToJson(applied), new UTF8Encoding(false));
                rows.Add(new { path, available = true, clip = clip.name, clipGuid, localId, sample.seconds, samples = sample.frames.Length,
                    totalHipsYaw = total, minimumHipsYaw = low, maximumHipsYaw = high,
                    sample.rootRotationDrift, sample.rootDrift, maximumFootDisplacement = motion,
                    appliedRootRotation = applied.rootRotationDrift, appliedRootDistance = applied.rootDrift,
                    appliedSignedYaw = AppliedYaw(applied.frames),
                    minimumSole, maximumHeight = maxHeight, settings.loopBlendOrientation, settings.keepOriginalOrientation,
                    settings.orientationOffsetY, rotationProperties, rotationCurves = new[] { RotationCurve(clip, "RootQ"), RotationCurve(clip, "MotionQ") },
                    leftContactProposals = sample.leftCandidates.Length, rightContactProposals = sample.rightCandidates.Length });
                }
            }
            foreach (var path in sourcePaths) if (Hash(path) != sources[path]) throw new InvalidOperationException("Source changed during screen.");
            if (EditorApplication.isCompiling) throw new InvalidOperationException("Assembly changed during screen.");
            var destination = Path.Combine(output, "summary.json");
            File.WriteAllText(destination, Newtonsoft.Json.JsonConvert.SerializeObject(new {
                state = "existing-turn-source-screen-not-qualified", sources,
                moduleId = typeof(AnimationPlaygroundMotionSampler).Module.ModuleVersionId, rows,
                scope = "Named original clips on the accepted target at saved scale/offset. Four-influence body. Separate manual-graph root-disabled and root-applied samples; root curves are raw importer data, whose mirror sign is not a runtime contract. Hip heading is only a visual proxy. No action/controller or physics simulation. Raw soles and proposed windows do not qualify contact or equipment."
            }, Newtonsoft.Json.Formatting.Indented) + "\n", new UTF8Encoding(false));
            return destination;
        }
        finally { QualitySettings.skinWeights = quality; }
    }

    private static float Heading(Vector3 vector) => Mathf.Atan2(vector.x, vector.z) * Mathf.Rad2Deg;

    private static float AppliedYaw(AnimationPlaygroundMotionSampler.PoseFrame[] frames)
    {
        var previous = Heading(frames[0].rootRotation * Vector3.forward); var total = 0f;
        foreach (var frame in frames)
        {
            var current = Heading(frame.rootRotation * Vector3.forward);
            total += Mathf.DeltaAngle(previous, current); previous = current;
        }
        return total;
    }

    private static object RotationCurve(AnimationClip clip, string prefix)
    {
        var bindings = AnimationUtility.GetCurveBindings(clip);
        var components = new[] { "x", "y", "z", "w" }.Select(s => bindings.Where(b => b.propertyName == prefix + "." + s).ToArray()).ToArray();
        if (components.Any(c => c.Length != 1)) return new { prefix, available = false };
        var curves = components.Select(c => AnimationUtility.GetEditorCurve(clip, c[0])).ToArray();
        var previous = 0f; var total = 0f; var minimum = 0f; var maximum = 0f;
        var intervals = Mathf.CeilToInt(clip.length * 120);
        for (var i = 0; i <= intervals; i++)
        {
            var time = clip.length * i / intervals;
            var rotation = new Quaternion(curves[0].Evaluate(time), curves[1].Evaluate(time), curves[2].Evaluate(time), curves[3].Evaluate(time)).normalized;
            var heading = Heading(rotation * Vector3.forward);
            if (i > 0) total += Mathf.DeltaAngle(previous, heading);
            previous = heading; minimum = Mathf.Min(minimum, total); maximum = Mathf.Max(maximum, total);
        }
        return new { prefix, available = true, totalYaw = total, minimumYaw = minimum, maximumYaw = maximum, samples = intervals + 1 };
    }

    private static AnimationPlaygroundMotionSampler.MotionMeasurement MeasureNamed(TopDown3DAnimationPlaygroundBindings bindings,
        string path, AnimationClip clip, bool applyRoot)
    {
        var scene = EditorSceneManager.NewPreviewScene(); GameObject instance = null; PlayableGraph graph = default;
        try
        {
            instance = (GameObject)PrefabUtility.InstantiatePrefab(bindings.targetModel, scene);
            instance.transform.SetPositionAndRotation(bindings.visualOffset, Quaternion.identity);
            instance.transform.localScale = Vector3.one * bindings.visualScale; instance.SetActive(true);
            var animator = instance.GetComponentInChildren<Animator>(); animator.enabled = true;
            if (!animator.isActiveAndEnabled || !animator.isHuman || !animator.avatar.isValid) throw new InvalidDataException("Inactive or invalid target rig.");
            animator.applyRootMotion = applyRoot; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate; animator.stabilizeFeet = false;
            foreach (var skin in instance.GetComponentsInChildren<SkinnedMeshRenderer>()) skin.quality = SkinQuality.Auto;
            var roles = new[] { HumanBodyBones.Hips, HumanBodyBones.Head, HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot,
                HumanBodyBones.LeftToes, HumanBodyBones.RightToes, HumanBodyBones.LeftHand, HumanBodyBones.RightHand };
            var bones = roles.Select(animator.GetBoneTransform).ToArray();
            if (bones.Any(b => b == null)) throw new InvalidDataException("Missing source-screen target bone.");
            using var sole = new AnimationPlaygroundMotionSampler.SoleProbe(instance, bones[2], bones[3]);
            graph = PlayableGraph.Create("Named offline turn source"); graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var playable = AnimationClipPlayable.Create(graph, clip); playable.SetApplyFootIK(false); playable.SetApplyPlayableIK(false); playable.SetSpeed(0);
            AnimationPlayableOutput.Create(graph, "Named source", animator).SetSourcePlayable(playable); graph.Play();
            var intervals = Mathf.CeilToInt(clip.length * 120);
            var result = new AnimationPlaygroundMotionSampler.MotionMeasurement { modelPath = AssetDatabase.GetAssetPath(bindings.targetModel),
                clipPath = path, clipName = clip.name, visualScale = bindings.visualScale, seconds = clip.length,
                sampleRate = intervals / clip.length, finite = true, footIK = false, frames = new AnimationPlaygroundMotionSampler.PoseFrame[intervals + 1] };
            for (var i = 0; i <= intervals; i++)
            {
                var seconds = clip.length * i / intervals;
                if (!applyRoot) instance.transform.SetPositionAndRotation(bindings.visualOffset, Quaternion.identity);
                playable.SetTime(seconds); graph.Evaluate(i == 0 ? 0 : clip.length / intervals);
                var frame = new AnimationPlaygroundMotionSampler.PoseFrame { seconds = seconds, root = instance.transform.position, rootRotation = instance.transform.rotation,
                    hips = bones[0].position, head = bones[1].position, leftFoot = bones[2].position, rightFoot = bones[3].position,
                    leftToes = bones[4].position, rightToes = bones[5].position, leftHand = bones[6].position, rightHand = bones[7].position,
                    hipsRotation = bones[0].rotation, leftFootRotation = bones[2].rotation, rightFootRotation = bones[3].rotation };
                sole.Measure(out frame.leftSole, out frame.rightSole, out frame.bodyMinimum, out frame.bodyMaximum);
                foreach (var bone in bones)
                    if (!float.IsFinite(bone.position.sqrMagnitude) || !float.IsFinite(bone.rotation.x + bone.rotation.y + bone.rotation.z + bone.rotation.w))
                        throw new InvalidDataException("Nonfinite named source pose.");
                result.frames[i] = frame;
            }
            result.rootDrift = Vector3.Distance(result.frames[0].root, result.frames[intervals].root);
            result.rootRotationDrift = Quaternion.Angle(result.frames[0].rootRotation, result.frames[intervals].rootRotation);
            result.leftCandidates = AnimationPlaygroundMotionSampler.FindContactWindows(result.frames, true);
            result.rightCandidates = AnimationPlaygroundMotionSampler.FindContactWindows(result.frames, false);
            return result;
        }
        finally
        {
            if (graph.IsValid()) graph.Destroy();
            if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}
