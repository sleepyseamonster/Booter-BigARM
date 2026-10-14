using System;
using System.IO;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class CampForwardLocomotionAuthoring
{
    public const string ProfilePath = "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/ForwardLocomotionCandidate.asset";
    public const string ClipsPath = "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/ForwardLocomotionClipsCandidate.asset";

    public static string CreateDraft() => Create(false);
    public static string CreateStageDraft() => Create(true);

    public static string CreateDistanceDraft()
    {
        var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode
            || Directory.Exists(Path.Combine(root, ".git"))
            || !File.Exists(Path.Combine(root, AnimationPlaygroundLayoutAuthoring.CandidateMarker)))
            throw new InvalidOperationException("Author only in the marked offline candidate.");
        var source = AssetDatabase.LoadAssetAtPath<AnimationPlaygroundForwardLocomotionProfile>(ProfilePath.Replace("Candidate", "StageAlignedCandidate"));
        var destination = ProfilePath.Replace("Candidate", "DistanceAlignedCandidate");
        if (File.Exists(destination)) throw new IOException("Preserve prior candidate.");
        var report = JObject.Parse(File.ReadAllText(Path.Combine(root, "LocomotionImplementation20261014/ForwardStagePoseV1/summary.json")));
        var points = new System.Collections.Generic.List<Vector2> { new(0, 0), new(1.4f, 0) };
        foreach (var row in (JArray)report["rows"])
        {
            var speed = (float)row["speed"];
            if (speed <= 1.4f || speed >= 2.7f) continue;
            var derivative = (float)row["patches"][2]["fittedCycleDistance"];
            points.Add(new Vector2(speed, derivative - (float)source.CycleDistance(speed)));
        }
        points.Add(new Vector2(2.7f, 0)); points.Add(new Vector2(3.8f, 0));
        var policy = Object.Instantiate(source);
        try
        {
            policy.secondRightDistanceAdjustment = points.ToArray();
            policy.name = "Measured mixed stance distance candidate - unqualified";
            if (!policy.Validate(policy.targetModel, policy.locomotion, out var error, false)) throw new InvalidDataException(error);
            AssetDatabase.CreateAsset(policy, destination); AssetDatabase.SaveAssetIfDirty(policy);
            return destination;
        }
        finally { if (!AssetDatabase.Contains(policy)) Object.DestroyImmediate(policy); }
    }

    private static string Create(bool stageAligned)
    {
        var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode
            || Directory.Exists(Path.Combine(root, ".git"))
            || !File.Exists(Path.Combine(root, AnimationPlaygroundLayoutAuthoring.CandidateMarker)))
            throw new InvalidOperationException("Author only in the marked offline candidate.");
        var profilePath = stageAligned ? ProfilePath.Replace("Candidate", "StageAlignedCandidate") : ProfilePath;
        var clipsPath = stageAligned ? ClipsPath.Replace("Candidate", "StageAlignedCandidate") : ClipsPath;
        if (File.Exists(profilePath) || File.Exists(clipsPath))
            throw new IOException("Preserve the existing candidate and GUIDs.");
        var recipe = JObject.Parse(File.ReadAllText(Path.Combine(root, stageAligned
            ? "LocomotionImplementation20261014/native-calibration-v6.json" : "LocomotionImplementation20261014/native-calibration-v5.json")));
        var bindings = AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(AnimationPlaygroundPlayerAuthoring.BindingPath);
        var baseline = AssetDatabase.LoadAssetAtPath<TopDown3DLocomotionClipProfile>("Assets/_Project/Settings/Player/TopDown3DLocomotionClipProfile.asset");
        var clips = Object.Instantiate(baseline); clips.name = "Measured native forward loops with retained legacy roles";
        var policy = ScriptableObject.CreateInstance<AnimationPlaygroundForwardLocomotionProfile>();
        policy.name = "Measured forward locomotion candidate - unqualified";
        policy.targetModel = bindings.targetModel; policy.footProjection = bindings.footProjection;
        policy.gaits = new AnimationPlaygroundForwardGait[3];
        var serialized = new SerializedObject(clips); var entries = serialized.FindProperty("clips");
        entries.GetArrayElementAtIndex(0).FindPropertyRelative("clip").objectReferenceValue =
            bindings.locomotion.GetRequiredEntry(TopDown3DLocomotionClipRole.Idle).Clip;
        var old = new SerializedObject(bindings.locomotion).FindProperty("clips").GetArrayElementAtIndex(0);
        entries.GetArrayElementAtIndex(0).FindPropertyRelative("visualHeightCorrection").animationCurveValue =
            old.FindPropertyRelative("visualHeightCorrection").animationCurveValue;
        try
        {
            var index = 0;
            foreach (var json in (JArray)recipe["gaits"])
            {
                var role = (TopDown3DLocomotionClipRole)(index + 1);
                var gait = new AnimationPlaygroundForwardGait
                {
                    role = role, sourceClip = baseline.GetRequiredEntry(role).Clip,
                    nominalSpeed = (float)json["nominalSpeed"], metresPerCycle = (float)json["metresPerCycle"],
                    nativeContacts = json["nativeContacts"].ToObject<float[]>(),
                    pureMinimumRate = index == 0 ? (stageAligned ? .8f : .85f) : index == 1 ? .65f : .7f,
                    pureMaximumRate = index == 0 ? (stageAligned ? 1.2f : 1.15f) : index == 1 ? 1.1f : 1.05f,
                    blendMinimumRate = index == 0 ? (stageAligned ? .8f : .85f) : index == 1 ? .5f : .65f,
                    blendMaximumRate = index == 0 ? (stageAligned ? 1.65f : 1.6f) : index == 1 ? 1.2f : 1.05f,
                };
                if (AssetDatabase.GetAssetPath(gait.sourceClip) != (string)json["source"])
                    throw new InvalidDataException("The source recipe disagrees with the original imported clip.");
                var map = (JArray)json["sourceTimeMap"]; gait.phaseMap = new AnimationPlaygroundNativePhase[map.Count];
                for (var k = 0; k < map.Count; k++)
                    gait.phaseMap[k] = new AnimationPlaygroundNativePhase((float)map[k][0], (float)map[k][1]);
                var corrections = (JArray)json["correction"]; var keys = new Keyframe[corrections.Count];
                for (var k = 0; k < keys.Length; k++) keys[k] = new Keyframe((float)corrections[k][0], (float)corrections[k][1]);
                gait.signedHeight = new AnimationCurve(keys);
                for (var k = 0; k < keys.Length; k++)
                {
                    AnimationUtility.SetKeyLeftTangentMode(gait.signedHeight, k, AnimationUtility.TangentMode.Linear);
                    AnimationUtility.SetKeyRightTangentMode(gait.signedHeight, k, AnimationUtility.TangentMode.Linear);
                }
                var entry = entries.GetArrayElementAtIndex(index + 1);
                entry.FindPropertyRelative("nominalForwardSpeed").floatValue = gait.nominalSpeed;
                foreach (var side in new[] { "leftContactPhases", "rightContactPhases" })
                {
                    var array = entry.FindPropertyRelative(side); array.arraySize = 2;
                    array.GetArrayElementAtIndex(0).floatValue = gait.nativeContacts[side[0] == 'l' ? 1 : 0];
                    array.GetArrayElementAtIndex(1).floatValue = gait.nativeContacts[side[0] == 'l' ? 3 : 2];
                }
                policy.gaits[index++] = gait;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            policy.locomotion = clips;
            if (!policy.Validate(bindings.targetModel, clips, out var error, false)) throw new InvalidDataException(error);
            AssetDatabase.CreateAsset(clips, clipsPath); AssetDatabase.CreateAsset(policy, profilePath);
            AssetDatabase.SaveAssetIfDirty(clips); AssetDatabase.SaveAssetIfDirty(policy);
            return profilePath;
        }
        finally
        {
            if (!AssetDatabase.Contains(policy)) Object.DestroyImmediate(policy);
            if (!AssetDatabase.Contains(clips)) Object.DestroyImmediate(clips);
        }
    }
}
