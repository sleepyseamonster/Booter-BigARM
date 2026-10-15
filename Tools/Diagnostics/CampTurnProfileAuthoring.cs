using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;

// Candidate-only materialization of signed source data. Never qualifies or binds it.
public static class CampTurnProfileAuthoring
{
    public static string Run()
    {
        var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
            || Directory.Exists(Path.Combine(root, ".git")) || !File.Exists(Path.Combine(root, ".animation-playground-validation-project")))
            throw new InvalidOperationException("Use the marked, compiled, non-playing candidate.");
        const string destination = "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/ForwardTurnCandidate.asset";
        var receipt = Path.Combine(root, "LocomotionImplementation20261014/TurnProfileAuthoringV1");
        if (File.Exists(Path.Combine(root, destination)) || File.Exists(Path.Combine(root, destination + ".meta")))
            throw new IOException("Preserve the existing private turn candidate.");
        if (Directory.Exists(receipt)) throw new IOException("Preserve prior authoring receipt.");
        const string basis = "LocomotionImplementation20261014/TurnSourceContactBasisV1/";
        const string contacts = "LocomotionImplementation20261014/TurnContactV1/summary.json";
        const string source = "Assets/_Project/Art/Characters/Prototype/UnityStandardHumanoid/Animations/m@RapidTurningLeft.fbx";
        var bindings = AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>("Assets/_Project/Settings/AnimationPlayground/PrivateBindings/PlayerBindings.asset");
        var profile = ScriptableObject.CreateInstance<AnimationPlaygroundTurnProfile>();
        string Hash(string path) { using var sha = SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Path.Combine(root, path)))).Replace("-", "").ToLowerInvariant(); }
        try
        {
            profile.targetModel = bindings.targetModel; profile.footProjection = bindings.footProjection;
            profile.visualScale = bindings.visualScale; profile.visualOffset = bindings.visualOffset;
            profile.sourceReceiptSha256 = Hash(basis + "summary.json"); profile.contactReceiptSha256 = Hash(contacts);
            AnimationPlaygroundTurnSource Entry(string name, AnimationPlaygroundTurnDirection direction)
            {
                var clip = AssetDatabase.LoadAllAssetsAtPath(source).OfType<AnimationClip>().Single(c => c.name == name);
                var tape = JsonUtility.FromJson<AnimationPlaygroundMotionSampler.MotionMeasurement>(File.ReadAllText(Path.Combine(root, basis + name + "-private-applied-poses.json")));
                if (tape.clipName != name || tape.clipPath != source || tape.modelPath != AssetDatabase.GetAssetPath(bindings.targetModel)
                    || Math.Abs(tape.seconds - clip.length) > .00001f || Math.Abs(tape.visualScale - bindings.visualScale) > .000001f
                    || Vector3.Distance(tape.frames[0].root, bindings.visualOffset) > .00001f)
                    throw new InvalidDataException("Named source tape or target calibration mismatch.");
                var origin = tape.frames[0].root; var previous = Heading(tape.frames[0].rootRotation); var total = 0f;
                var entry = new AnimationPlaygroundTurnSource { clip = clip, sampledClipName = clip.name, direction = direction, duration = clip.length,
                    samples = new AnimationPlaygroundTurnSample[tape.frames.Length] };
                for (var i = 0; i < tape.frames.Length; i++)
                {
                    var frame = tape.frames[i]; var yaw = Heading(frame.rootRotation);
                    if (Vector3.Angle(frame.rootRotation * Vector3.up, Vector3.up) > .001f)
                        throw new InvalidDataException("A yaw-only contract cannot discard source root tilt.");
                    if (i > 0) total += Mathf.DeltaAngle(previous, yaw);
                    previous = yaw;
                    entry.samples[i] = new AnimationPlaygroundTurnSample(frame.seconds, total);
                    entry.maximumPlanarRootExcursion = Mathf.Max(entry.maximumPlanarRootExcursion, Vector3.ProjectOnPlane(frame.root - origin, Vector3.up).magnitude);
                    entry.maximumVerticalRootExcursion = Mathf.Max(entry.maximumVerticalRootExcursion, Mathf.Abs(frame.root.y - origin.y));
                }
                if (!entry.Validate(direction, out var error)) throw new InvalidDataException(error);
                return entry;
            }
            profile.left = Entry("RapidTurningLeft", AnimationPlaygroundTurnDirection.Left);
            profile.right = Entry("RapidTurningRight_Mirror", AnimationPlaygroundTurnDirection.Right);
            if (!profile.Validate(bindings.targetModel, out var reason, false)) throw new InvalidDataException(reason);
            if (profile.qualified || profile.Validate(bindings.targetModel, out _)) throw new InvalidOperationException("The source candidate must remain unqualified.");
            AssetDatabase.CreateAsset(profile, destination); AssetDatabase.SaveAssetIfDirty(profile);
            Directory.CreateDirectory(receipt);
            File.WriteAllText(Path.Combine(receipt, "summary.json"), Newtonsoft.Json.JsonConvert.SerializeObject(new {
                state = "unqualified-source-contract-only-not-bound", asset = destination, sha256 = Hash(destination), metaSha256 = Hash(destination + ".meta"),
                profile.sourceReceiptSha256, profile.contactReceiptSha256, leftSamples = profile.left.samples.Length, rightSamples = profile.right.samples.Length,
                leftTapeSha256 = Hash(basis + "RapidTurningLeft-private-applied-poses.json"),
                rightTapeSha256 = Hash(basis + "RapidTurningRight_Mirror-private-applied-poses.json"),
                leftClip = profile.left.clip.name, rightClip = profile.right.clip.name, qualified = profile.qualified,
                moduleId = typeof(AnimationPlaygroundTurnProfile).Module.ModuleVersionId,
                scope = "Private candidate asset only; no scene, binding, motor or animation graph changed."
            }, Newtonsoft.Json.Formatting.Indented) + "\n", new UTF8Encoding(false));
            return destination;
        }
        finally { if (!AssetDatabase.Contains(profile)) UnityEngine.Object.DestroyImmediate(profile); }
    }

    private static float Heading(Quaternion rotation)
    { var direction = rotation * Vector3.forward; return Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg; }

    public static string UpgradeClipIdentity()
    {
        var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
            || Directory.Exists(Path.Combine(root, ".git")) || !File.Exists(Path.Combine(root, ".animation-playground-validation-project")))
            throw new InvalidOperationException("Use the marked, compiled, non-playing candidate.");
        const string path = "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/ForwardTurnCandidate.asset";
        var previous = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Path.Combine(root, "LocomotionImplementation20261014/TurnProfileAuthoringV1/summary.json")));
        string Hash(string relative) { using var sha = SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Path.Combine(root, relative)))).Replace("-", "").ToLowerInvariant(); }
        if (Hash(path) != (string)previous["sha256"] || Hash(path + ".meta") != (string)previous["metaSha256"])
            throw new InvalidDataException("Only upgrade the exact unbound task-owned V1 source asset.");
        var directory = Path.Combine(root, "LocomotionImplementation20261014/TurnProfileIdentityUpgradeV1");
        if (Directory.Exists(directory)) throw new IOException("Preserve the previous identity upgrade.");
        var profile = AssetDatabase.LoadAssetAtPath<AnimationPlaygroundTurnProfile>(path);
        if (profile == null || profile.qualified || profile.left.clip.name != "RapidTurningLeft" || profile.right.clip.name != "RapidTurningRight_Mirror"
            || AssetDatabase.GetDependencies(TopDown3DAnimationPlaygroundSession.ScenePath, true).Contains(path))
            throw new InvalidOperationException("Require the untouched unbound source candidate.");
        Directory.CreateDirectory(directory);
        File.Copy(Path.Combine(root, path), Path.Combine(directory, "before.asset"));
        File.Copy(Path.Combine(root, path + ".meta"), Path.Combine(directory, "before.asset.meta"));
        profile.left.sampledClipName = profile.left.clip.name; profile.right.sampledClipName = profile.right.clip.name;
        if (!profile.Validate(profile.targetModel, out var error, false)) throw new InvalidDataException(error);
        EditorUtility.SetDirty(profile); AssetDatabase.SaveAssetIfDirty(profile);
        if (Hash(path + ".meta") != (string)previous["metaSha256"]) throw new InvalidDataException("Preserve the source asset GUID and metadata.");
        File.WriteAllText(Path.Combine(directory, "summary.json"), Newtonsoft.Json.JsonConvert.SerializeObject(new {
            state = "unqualified-source-contract-identity-upgrade-not-bound", asset = path, beforeSha256 = (string)previous["sha256"],
            sha256 = Hash(path), metaSha256 = Hash(path + ".meta"), profile.sourceReceiptSha256, profile.contactReceiptSha256,
            qualified = profile.qualified, moduleId = typeof(AnimationPlaygroundTurnProfile).Module.ModuleVersionId
        }, Newtonsoft.Json.Formatting.Indented) + "\n", new UTF8Encoding(false));
        return path;
    }
}
