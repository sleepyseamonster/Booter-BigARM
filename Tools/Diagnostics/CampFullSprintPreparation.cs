using System;
using System.IO;
using BooterBigArm.Editor;
using UnityEditor;
using UnityEngine;

public static class CampFullSprintPreparation
{
    public static string Prepare()
    {
        var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
            || Directory.Exists(Path.Combine(root, ".git")) || !File.Exists(Path.Combine(root, ".animation-playground-validation-project")))
            throw new InvalidOperationException("Use the stable marked background candidate.");
        var directory = "LocomotionImplementation20261014/FullSprintPreparation20261015";
        if (Directory.Exists(directory)) throw new IOException("Preserve earlier source preparation.");
        Directory.CreateDirectory(directory);
        var rows = new System.Collections.Generic.List<object>();
        foreach (var item in new[] {
            (source: "Assets/_Project/Art/Animations/Mixamo/Locomotion/TwoCycleSprint.fbx", name: "FullSprintMixamoReview", origin: .4375f, repetitions: 1),
            (source: "Assets/_Project/Art/Characters/Prototype/UnityStandardHumanoid/Animations/m@SprintForwards.fbx", name: "FullSprintStandardReview", origin: .5f, repetitions: 2) })
        {
            string Hash(string path) { using var sha = System.Security.Cryptography.SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant(); }
            var before = Hash(item.source); var meta = Hash(item.source + ".meta");
            var source = AnimationPlaygroundClipDerivatives.LoadClip(item.source);
            var clip = AnimationPlaygroundClipDerivatives.CreateLoop(item.source, item.name, item.origin, item.repetitions);
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                if (binding.propertyName == "RootT.x" || binding.propertyName == "RootT.z")
                    AnimationUtility.SetEditorCurve(clip, binding, AnimationCurve.Constant(0, clip.length, 0));
            EditorUtility.SetDirty(clip); AssetDatabase.SaveAssetIfDirty(clip);
            if (Hash(item.source) != before || Hash(item.source + ".meta") != meta || Mathf.Abs(clip.length - source.length * item.repetitions) > .00001f)
                throw new InvalidDataException("Original source or complete cycle changed.");
            var path = AssetDatabase.GetAssetPath(clip);
            rows.Add(new { item.source, sourceSha256 = before, sourceMetaSha256 = meta, sourceSeconds = source.length,
                item.origin, item.repetitions, path, clip.length, clip.isHumanMotion, clip.isLooping,
                sha256 = Hash(path), metaSha256 = Hash(path + ".meta"),
                adaptation = "Complete source pose cycle retained and phase shifted; repeat short source twice; horizontal root translation zeroed for existing motor ownership." });
        }
        var output = Path.Combine(directory, "recipe.json");
        File.WriteAllText(output, Newtonsoft.Json.JsonConvert.SerializeObject(new { state = "unbound-complete-sprint-candidates", rows }, Newtonsoft.Json.Formatting.Indented));
        return Path.GetFullPath(output);
    }
}
