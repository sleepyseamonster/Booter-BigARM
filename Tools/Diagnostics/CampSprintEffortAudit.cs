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

// Pure source motion on the calibrated target; no motor or gameplay simulation.
public static class CampSprintEffortAudit
{
    public static string Run()
    {
        var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
            || Directory.Exists(Path.Combine(root, ".git")) || !File.Exists(Path.Combine(root, ".animation-playground-validation-project")))
            throw new InvalidOperationException("Use the stable marked offline candidate.");
        const string folder = "LocomotionImplementation20261014/FullSprintEffort20261015";
        if (Directory.Exists(folder)) throw new IOException("Preserve earlier effort evidence.");
        Directory.CreateDirectory(folder);
        var policy = AssetDatabase.LoadAssetAtPath<AnimationPlaygroundForwardLocomotionProfile>(
            "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/ExpandedForwardGaitReview.asset");
        var bindings = AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(AnimationPlaygroundPlayerAuthoring.BindingPath);
        if (policy == null || !policy.Validate(bindings.targetModel, policy.locomotion, out _, false))
            throw new InvalidDataException("Restore the sealed expanded candidate.");
        string Hash(string path) { using var sha = System.Security.Cryptography.SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant(); }
        var rows = new List<object>();
        foreach (var index in new[] { 3, 4 })
        {
            var gait = policy.gaits[index]; var path = AssetDatabase.GetAssetPath(gait.sourceClip);
            var before = Hash(path); var metadata = Hash(path + ".meta");
            var scene = EditorSceneManager.NewPreviewScene(); GameObject actor = null; PlayableGraph graph = default;
            try
            {
                actor = PrefabUtility.InstantiatePrefab(bindings.targetModel, scene) as GameObject;
                actor.transform.SetPositionAndRotation(bindings.visualOffset, Quaternion.identity);
                actor.transform.localScale = Vector3.one * bindings.visualScale;
                var animator = actor.GetComponentInChildren<Animator>(); animator.avatar = bindings.targetAvatar;
                animator.applyRootMotion = false; animator.stabilizeFeet = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                Transform Bone(HumanBodyBones role) => animator.GetBoneTransform(role) ?? throw new InvalidDataException("Missing " + role);
                var hips = Bone(HumanBodyBones.Hips); var head = Bone(HumanBodyBones.Head);
                var upper = new[] { Bone(HumanBodyBones.LeftUpperLeg), Bone(HumanBodyBones.RightUpperLeg) };
                var knee = new[] { Bone(HumanBodyBones.LeftLowerLeg), Bone(HumanBodyBones.RightLowerLeg) };
                var foot = new[] { Bone(HumanBodyBones.LeftFoot), Bone(HumanBodyBones.RightFoot) };
                var hand = new[] { Bone(HumanBodyBones.LeftHand), Bone(HumanBodyBones.RightHand) };
                graph = PlayableGraph.Create("Offline full sprint effort comparison"); graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var playable = AnimationClipPlayable.Create(graph, gait.sourceClip);
                playable.SetApplyFootIK(false); playable.SetApplyPlayableIK(false); playable.SetSpeed(0);
                AnimationPlayableOutput.Create(graph, "Calibrated target", animator).SetSourcePlayable(playable); graph.Play();
                var intervals = Mathf.CeilToInt(gait.sourceClip.length * 120);
                var kneePeak = new float[2]; var thighPeak = new float[2]; var recovery = new float[2];
                var footMin = new[] { float.PositiveInfinity, float.PositiveInfinity }; var footMax = new[] { float.NegativeInfinity, float.NegativeInfinity };
                var handMin = new[] { float.PositiveInfinity, float.PositiveInfinity }; var handMax = new[] { float.NegativeInfinity, float.NegativeInfinity };
                var lean = 0f; var raw = new List<object>();
                for (var frame = 0; frame <= intervals; frame++)
                {
                    var time = gait.sourceClip.length * frame / intervals;
                    playable.SetTime(time); graph.Evaluate(frame == 0 ? 0 : gait.sourceClip.length / intervals);
                    var flex = new float[2]; var thigh = new float[2]; var height = new float[2];
                    for (var side = 0; side < 2; side++)
                    {
                        flex[side] = 180 - Vector3.Angle(upper[side].position - knee[side].position, foot[side].position - knee[side].position);
                        thigh[side] = Mathf.Atan2(knee[side].position.z - upper[side].position.z, upper[side].position.y - knee[side].position.y) * Mathf.Rad2Deg;
                        height[side] = foot[side].position.y - hips.position.y;
                        kneePeak[side] = Mathf.Max(kneePeak[side], flex[side]); thighPeak[side] = Mathf.Max(thighPeak[side], thigh[side]);
                        recovery[side] = Mathf.Max(recovery[side], height[side]);
                        footMin[side] = Mathf.Min(footMin[side], foot[side].position.y - hips.position.y);
                        footMax[side] = Mathf.Max(footMax[side], foot[side].position.y - hips.position.y);
                        handMin[side] = Mathf.Min(handMin[side], hand[side].position.z - hips.position.z);
                        handMax[side] = Mathf.Max(handMax[side], hand[side].position.z - hips.position.z);
                    }
                    lean = Mathf.Max(lean, Mathf.Atan2(head.position.z - hips.position.z, head.position.y - hips.position.y) * Mathf.Rad2Deg);
                    raw.Add(new { time, kneeFlexion = flex, thighForward = thigh, footRelativeHipHeight = height });
                }
                if (Hash(path) != before || Hash(path + ".meta") != metadata) throw new InvalidDataException("Source changed during the audit.");
                var name = index == 3 ? "FastRun" : "Sprint";
                File.WriteAllText(Path.Combine(folder, name + ".json"), Newtonsoft.Json.JsonConvert.SerializeObject(raw));
                rows.Add(new { stage = name, source = path, sha256 = before, metadataSha256 = metadata, gait.nominalSpeed,
                    gait.metresPerCycle, clipSeconds = gait.sourceClip.length, samples = intervals + 1,
                    stepsPerSecond = gait.nominalSpeed / gait.metresPerCycle * 4,
                    maximumKneeFlexionDegrees = kneePeak, maximumForwardThighDegrees = thighPeak,
                    footRecoveryRelativeHipMetres = Enumerable.Range(0, 2).Select(s => footMax[s] - footMin[s]).ToArray(),
                    handForwardBackMetres = Enumerable.Range(0, 2).Select(s => handMax[s] - handMin[s]).ToArray(), maximumHeadHipLeanDegrees = lean });
            }
            finally { if (graph.IsValid()) graph.Destroy(); if (actor != null) Object.DestroyImmediate(actor); EditorSceneManager.ClosePreviewScene(scene); }
        }
        var output = Path.Combine(folder, "summary.json");
        File.WriteAllText(output, Newtonsoft.Json.JsonConvert.SerializeObject(new { state = "native-effort-comparison-not-author-acceptance", rows,
            scope = "Calibrated target bone motion; complete pure sources, 120Hz. Scalar comparisons do not prove visual preference or full contact qualification." }, Newtonsoft.Json.Formatting.Indented));
        return Path.GetFullPath(output);
    }
}
