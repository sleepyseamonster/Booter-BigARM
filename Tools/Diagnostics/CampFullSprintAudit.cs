using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;

// Source audit only: no scene save, input, physics simulation or Play Mode.
public static class CampFullSprintAudit
{
    public const string DirectoryName = "LocomotionImplementation20261014/FullSprintAudit20261015";
    public static string Run()
    {
        var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
            || Directory.Exists(Path.Combine(root, ".git")) || !File.Exists(Path.Combine(root, ".animation-playground-validation-project")))
            throw new InvalidOperationException("Use the stable marked background candidate.");
        if (Directory.Exists(DirectoryName)) throw new IOException("Preserve earlier evidence.");
        Directory.CreateDirectory(DirectoryName);
        var bindings = AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(AnimationPlaygroundPlayerAuthoring.BindingPath);
        var model = AssetDatabase.GetAssetPath(bindings.targetModel);
        var paths = new[] {
            "Assets/_Project/Art/Animations/Mixamo/Derived/SprintArmDriveReview.anim",
            "Assets/_Project/Art/Characters/Animations/MoCapCentral/CoreMotion/Locomotion/MCU_am_Loco_Run_Fwd_NoRM.FBX",
            "Assets/_Project/Art/Animations/Mixamo/Locomotion/TwoCycleSprint.fbx",
            "Assets/_Project/Art/Animations/Mixamo/Reference/Locomotion/SprintForwardTravelAlternative.fbx",
            "Assets/_Project/Art/Characters/Prototype/UnityStandardHumanoid/Animations/m@SprintForwards.fbx",
            "Assets/_Project/Art/Characters/Prototype/UnityStandardHumanoid/Animations/m@RunForwards.fbx" };
        string Hash(string path) { using var sha = System.Security.Cryptography.SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant(); }
        var inputs = paths.SelectMany(p => new[] { p, p + ".meta" }).ToDictionary(p => p, Hash);
        var rows = new List<object>(); var quality = QualitySettings.skinWeights;
        try
        {
            QualitySettings.skinWeights = SkinWeights.FourBones;
            foreach (var path in paths)
            {
                var clip = AnimationPlaygroundClipDerivatives.LoadClip(path);
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                var takeInfo = importer == null ? null : importer.importedTakeInfos.Select(t => new { t.name, t.startTime, t.stopTime, t.bakeStartTime, t.bakeStopTime }).ToArray();
                var imports = importer == null ? null : (importer.clipAnimations.Length > 0 ? importer.clipAnimations : importer.defaultClipAnimations).Select(c => new {
                    c.name, c.firstFrame, c.lastFrame, c.loopTime, c.loopPose, c.lockRootPositionXZ, c.lockRootHeightY,
                    c.lockRootRotation, c.heightFromFeet, c.keepOriginalPositionY, c.keepOriginalPositionXZ, c.keepOriginalOrientation,
                    c.rotationOffset, c.heightOffset }).ToArray();
                var measured = AnimationPlaygroundMotionSampler.Measure(model, path, bindings.visualScale, bindings.visualOffset, 120);
                File.WriteAllText(Path.Combine(DirectoryName, Path.GetFileNameWithoutExtension(path) + ".json"), JsonUtility.ToJson(measured));
                var floor = -bindings.capsuleHeight * .5f;
                var minimum = measured.frames.Min(f => Mathf.Min(f.leftSole.y, f.rightSole.y));
                var footZ = measured.frames.SelectMany(f => new[] { f.leftFoot.z - f.hips.z, f.rightFoot.z - f.hips.z }).ToArray();
                var handZ = measured.frames.SelectMany(f => new[] { f.leftHand.z - f.hips.z, f.rightHand.z - f.hips.z }).ToArray();
                var leans = measured.frames.Select(f => Mathf.Atan2(f.head.z - f.hips.z, f.head.y - f.hips.y) * Mathf.Rad2Deg).ToArray();
                rows.Add(new { path, clip.name, clip.length, clip.frameRate, clip.isHumanMotion, clip.isLooping,
                    averageSpeed = new[] { clip.averageSpeed.x, clip.averageSpeed.y, clip.averageSpeed.z }, takeInfo, imports,
                    measured.finite, measured.seamFootGap, measured.seamHipsAngle, measured.rootDrift, measured.hipsPlanarDrift,
                    minimumSole = minimum, maximumRequiredLift = Mathf.Max(0, floor - minimum),
                    maximumBodyHeight = measured.frames.Max(f => f.bodyMaximum.y - floor),
                    footForwardBackSpan = footZ.Max() - footZ.Min(), handForwardBackSpan = handZ.Max() - handZ.Min(),
                    minimumLean = leans.Min(), maximumLean = leans.Max(), samples = measured.frames.Length });
            }
            foreach (var input in inputs) if (Hash(input.Key) != input.Value) throw new InvalidDataException("Source changed while auditing.");
            var output = Path.Combine(DirectoryName, "summary.json");
            File.WriteAllText(output, Newtonsoft.Json.JsonConvert.SerializeObject(new { state = "full-source-audit-not-installation", model,
                bindings.visualScale, bindings.bodyHeight, bindings.capsuleHeight, inputs, rows,
                scope = "Full imported ranges, target Humanoid poses at 120Hz; original source bytes and metadata preserved. No equipment/terrain acceptance." }, Newtonsoft.Json.Formatting.Indented));
            return Path.GetFullPath(output);
        }
        finally { QualitySettings.skinWeights = quality; }
    }
}
