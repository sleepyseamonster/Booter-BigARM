using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

// Private candidate imports only. Existing source assets/metadata are immutable.
public static class CampTurnNativeImport
{
    public static string Run()
    {
        var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
            || Directory.Exists(Path.Combine(root, ".git")) || !File.Exists(Path.Combine(root, ".animation-playground-validation-project")))
            throw new InvalidOperationException("Require the marked compiled EditMode candidate.");
        const string source = "Assets/_Project/Art/Characters/Prototype/UnityStandardHumanoid/Animations/m@RapidTurningLeft.fbx";
        const string folder = "Assets/_Project/Art/Characters/AnimationPlayground/Qualification/";
        var receipt = Path.Combine(root, "LocomotionImplementation20261014/TurnNativeImportsV1");
        if (Directory.Exists(receipt) || File.Exists(Path.Combine(root, folder + "RapidTurnOneShot.fbx"))
            || File.Exists(Path.Combine(root, folder + "RapidTurnNative.fbx")))
            throw new IOException("Preserve existing candidate imports/receipts.");
        string Hash(string path) { using var sha = SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Path.Combine(root, path)))).Replace("-", "").ToLowerInvariant(); }
        var sourceHash = Hash(source); var sourceMeta = Hash(source + ".meta");
        var rows = new System.Collections.Generic.List<object>();
        foreach (var native in new[] { false, true })
        {
            var target = folder + (native ? "RapidTurnNative.fbx" : "RapidTurnOneShot.fbx");
            if (!AssetDatabase.CopyAsset(source, target)) throw new IOException("Could not create private candidate copy.");
            var importer = AssetImporter.GetAtPath(target) as ModelImporter;
            var clips = importer.clipAnimations;
            if (clips.Length != 2) throw new InvalidDataException("Require the named left and mirrored right imports.");
            foreach (var clip in clips)
            {
                clip.loopTime = false; clip.loopPose = false; clip.loop = false; clip.cycleOffset = 0;
                if (native) { clip.lockRootPositionXZ = false; clip.keepOriginalPositionXZ = false; }
            }
            importer.clipAnimations = clips; importer.SaveAndReimport();
            var imported = AssetDatabase.LoadAllAssetsAtPath(target).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToArray();
            if (imported.Length != 2 || imported.Any(c => !c.isHumanMotion || c.isLooping))
                throw new InvalidDataException("Require two nonlooping Humanoid source candidates.");
            rows.Add(new { target, guid = AssetDatabase.AssetPathToGUID(target), sha256 = Hash(target), metaSha256 = Hash(target + ".meta"),
                settings = importer.clipAnimations.Select(c => new { c.name, c.firstFrame, c.lastFrame, c.loopTime, c.loopPose, c.cycleOffset,
                    c.mirror, c.lockRootRotation, c.lockRootHeightY, c.lockRootPositionXZ, c.keepOriginalPositionXZ }).ToArray(),
                clips = imported.Select(c => new { c.name, c.length, c.isLooping, c.isHumanMotion }).ToArray() });
        }
        if (Hash(source) != sourceHash || Hash(source + ".meta") != sourceMeta) throw new InvalidOperationException("Original import changed.");
        Directory.CreateDirectory(receipt);
        var output = Path.Combine(receipt, "summary.json");
        File.WriteAllText(output, Newtonsoft.Json.JsonConvert.SerializeObject(new { source, sourceHash, sourceMeta, rows,
            scope = "Private candidate-only source variants. Original FBX bytes retained. Nonlooping, zero cycle offset; native variant also extracts planar center motion. No player binding, controller, scene save or physics simulation."
        }, Newtonsoft.Json.Formatting.Indented) + "\n", new UTF8Encoding(false));
        return output;
    }
}
