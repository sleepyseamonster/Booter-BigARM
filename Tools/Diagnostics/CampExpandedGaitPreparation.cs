using System;
using System.IO;
using System.Linq;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

// Builds a separate private review family. Original imports and the accepted
// three-gait policy are retained; this never saves or opens a scene.
public static class CampExpandedGaitPreparation
{
    public const string ProfilePath = "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/ExpandedForwardGaitReview.asset";
    private const string BasePath = "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/ForwardLocomotionDistanceAlignedCandidate.asset";
    public static string Prepare()
    {
        var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
            || Directory.Exists(Path.Combine(root, ".git")) || !File.Exists(Path.Combine(root, ".animation-playground-validation-project")))
            throw new InvalidOperationException("Use the stable marked background candidate.");
        if (File.Exists(ProfilePath)) throw new IOException("Preserve the existing expanded review.");
        var baseline = AssetDatabase.LoadAssetAtPath<AnimationPlaygroundForwardLocomotionProfile>(BasePath);
        var bindings = AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(AnimationPlaygroundPlayerAuthoring.BindingPath);
        if (baseline == null || !baseline.Validate(bindings.targetModel, baseline.locomotion, out _, false))
            throw new InvalidDataException("Restore the accepted baseline first.");
        var folder = "LocomotionImplementation20261014/FullSprintPreparation20261015";
        if (!File.Exists(Path.Combine(folder, "PlayerBindings.Before.asset"))) {
            File.Copy(AnimationPlaygroundPlayerAuthoring.BindingPath, Path.Combine(folder, "PlayerBindings.Before.asset"));
            File.Copy(AnimationPlaygroundPlayerAuthoring.BindingPath + ".meta", Path.Combine(folder, "PlayerBindings.Before.asset.meta"));
        }
        var profile = UnityEngine.Object.Instantiate(baseline);
        profile.name = "ExpandedForwardGaitReview"; profile.schema = 2;
        var native = JObject.Parse(File.ReadAllText("LocomotionImplementation20261014/native-calibration-v6.json"));
        var sprint = JObject.Parse(File.ReadAllText(Path.Combine(folder, "FullSprintStandardReview-final-calibration.json")));
        profile.gaits = new[] {
            Copy(baseline.gaits[0]), Copy(baseline.gaits[1]),
            FromReport(native["gaits"].First(row => (string)row["role"] == "Sprint"), 4f),
            Copy(baseline.gaits[2]), FromReport(sprint["gaits"][0], 6f) };
        if (!profile.Validate(bindings.targetModel, profile.locomotion, out var reason, false))
            throw new InvalidDataException(reason);
        AssetDatabase.CreateAsset(profile, ProfilePath);
        bindings.forwardReviewOverride = profile; bindings.forwardReviewOverrideRequired = true;
        EditorUtility.SetDirty(bindings); AssetDatabase.SaveAssetIfDirty(bindings);
        var output = Path.Combine(folder, "expanded-recipe.json");
        File.WriteAllText(output, Newtonsoft.Json.JsonConvert.SerializeObject(new {
            state = "unqualified-flat-deck-expanded-review", ProfilePath, schema = profile.schema,
            gamepad = "Stick amplitude: idle/walk/jog/run/fast run. Existing RB sprint binding: full sprint.",
            keyboard = "Existing keyboard speeds retained; keyboard stage selection deferred by author.",
            gaits = profile.gaits.Select((g, i) => new { stage = new[] { "Walk", "Jog", "Run", "FastRun", "Sprint" }[i],
                g.nominalSpeed, g.metresPerCycle, source = AssetDatabase.GetAssetPath(g.sourceClip), g.sourceClip.length }),
            maximumLift = profile.maximumLift, maximumLowering = profile.maximumLowering,
            originalThreeGaitProfileChanged = false, sceneSaved = false, rootMotionApplied = false
        }, Newtonsoft.Json.Formatting.Indented));
        return Path.GetFullPath(output);
    }
    private static AnimationPlaygroundForwardGait Copy(AnimationPlaygroundForwardGait gait) => new() {
        role = gait.role, sourceClip = gait.sourceClip, nominalSpeed = gait.nominalSpeed, metresPerCycle = gait.metresPerCycle,
        pureMinimumRate = gait.pureMinimumRate, pureMaximumRate = gait.pureMaximumRate,
        blendMinimumRate = .45f, blendMaximumRate = 1.65f,
        nativeContacts = (float[])gait.nativeContacts.Clone(), phaseMap = (AnimationPlaygroundNativePhase[])gait.phaseMap.Clone(),
        signedHeight = new AnimationCurve(gait.signedHeight.keys) };
    private static AnimationPlaygroundForwardGait FromReport(JToken row, float nominal)
    {
        var points = row["correction"].Select(p => new Keyframe((float)p[0], (float)p[1])).ToArray();
        var height = new AnimationCurve(points);
        for (var i = 0; i < height.length; i++) {
            AnimationUtility.SetKeyLeftTangentMode(height, i, AnimationUtility.TangentMode.Linear);
            AnimationUtility.SetKeyRightTangentMode(height, i, AnimationUtility.TangentMode.Linear);
        }
        return new AnimationPlaygroundForwardGait {
            role = TopDown3DLocomotionClipRole.Sprint,
            sourceClip = AnimationPlaygroundClipDerivatives.LoadClip((string)row["source"]), nominalSpeed = nominal,
            metresPerCycle = (float)row["metresPerCycle"], pureMinimumRate = .65f, pureMaximumRate = 1.35f,
            blendMinimumRate = .45f, blendMaximumRate = 1.65f,
            nativeContacts = row["nativeContacts"].Select(p => (float)p).ToArray(),
            phaseMap = row["sourceTimeMap"].Select(p => new AnimationPlaygroundNativePhase((float)p[0], (float)p[1])).ToArray(),
            signedHeight = height };
    }
}
