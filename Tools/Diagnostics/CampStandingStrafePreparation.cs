using System;
using System.IO;
using System.Linq;
using BooterBigArm.TopDown3D;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class CampStandingStrafePreparation
{
    public const string ProfilePath = "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/StandingStrafeReview.asset";
    public static string SelectReview()
    {
        var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
            || Directory.Exists(Path.Combine(root, ".git")) || !File.Exists(Path.Combine(root, ".animation-playground-validation-project")))
            throw new InvalidOperationException("Use the stable marked offline candidate.");
        var profile = AssetDatabase.LoadAssetAtPath<AnimationPlaygroundStandingStrafeProfile>(ProfilePath);
        var bindings = AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(
            "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/PlayerBindings.asset");
        if (bindings.standingStrafeReview != null || EditorUtility.IsDirty(bindings)
            || profile == null || !profile.Validate(bindings.targetModel, bindings.footProjection, out _))
            throw new InvalidOperationException("Preserve an existing binding; select only the complete candidate review.");
        bindings.standingStrafeReview = profile; bindings.standingStrafeReviewRequired = true;
        EditorUtility.SetDirty(bindings); AssetDatabase.SaveAssetIfDirty(bindings);
        return "Candidate private review selected; no scene saved.";
    }
    public static string Prepare()
    {
        var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
            || Directory.Exists(Path.Combine(root, ".git")) || !File.Exists(Path.Combine(root, ".animation-playground-validation-project")))
            throw new InvalidOperationException("Use the stable marked offline candidate.");
        if (File.Exists(Path.Combine(root, ProfilePath)) || File.Exists(Path.Combine(root, ProfilePath + ".meta")))
            throw new IOException("Preserve the previous private review.");
        var calibration = JObject.Parse(File.ReadAllText(Path.Combine(root, "LocomotionImplementation20261014/StrafeWalkLoops20261015/calibration.json")));
        var bindings = AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(
            "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/PlayerBindings.asset");
        var profile = ScriptableObject.CreateInstance<AnimationPlaygroundStandingStrafeProfile>();
        profile.targetModel = bindings.targetModel; profile.footProjection = bindings.footProjection;
        AnimationPlaygroundForwardGait Gait(string side)
        {
            var row = calibration["gaits"].Single(g => (string)g["role"] == side);
            var height = new AnimationCurve(row["correction"].Select(p => new Keyframe((float)p[0], (float)p[1])).ToArray());
            for (var i = 0; i < height.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(height, i, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(height, i, AnimationUtility.TangentMode.Linear);
            }
            return new AnimationPlaygroundForwardGait {
                sourceClip = AssetDatabase.LoadAssetAtPath<AnimationClip>((string)row["source"]),
                metresPerCycle = (float)row["metresPerCycle"], nominalSpeed = .5f,
                nativeContacts = row["nativeContacts"].Select(p => (float)p).ToArray(), signedHeight = height };
        }
        profile.left = Gait("Left"); profile.right = Gait("Right");
        if (!profile.Validate(profile.targetModel, profile.footProjection, out var reason)) throw new InvalidDataException(reason);
        AssetDatabase.CreateAsset(profile, ProfilePath); AssetDatabase.SaveAssetIfDirty(profile);
        return ProfilePath;
    }
}
