using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Editor
{
    public static class AnimationPlaygroundPropContactAuthoring
    {
        private const string DirectoryPath = "HeldPropQualification/SourceContactDraft";
        private static readonly string[] Sources = {
            "Pickup/PickupOneHand", "Pickup/PickupAndExamine",
            "Throw/ThrowStandard", "Throw/ThrowAlternate", "Throw/ThrowPickupAndThrow",
            "Button/ButtonPress", "Push/PushStart", "Push/PushCycle", "Push/PushStop"
        };
        [Serializable] private sealed class ContactCandidate
        {
            public string path, sha256, metaSha256;
            public int samples;
            public float duration, lowestLeftHandSeconds, lowestRightHandSeconds;
            public Vector3 lowestLeftHand, lowestRightHand;
            public float fastestLeftHandSeconds, fastestRightHandSeconds, fastestLeftHandSpeed, fastestRightHandSpeed;
            public float minimumSole, maximumBodyHeight, rootDrift, hipsPlanarDrift;
        }
        [Serializable] private sealed class Report
        {
            public string state = "contact-candidates-only-not-physical-qualified";
            public string modelPath, modelSha256, modelMetaSha256;
            public float feetPlane = -.91f, visualScale;
            public Vector3 visualOffset;
            public ContactCandidate[] candidates;
        }
        private static string Hash(string path)
        {
            using var stream = File.OpenRead(path); using var hash = SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
        // This read-only probe does not change an importer, clip, private binding or scene.
        // Hand extrema/speeds are evidence for choosing a contact range, not a release gate.
        public static void MeasureSourcesFromCli()
        {
            if (!Application.isBatchMode || Directory.Exists(".git") || !File.Exists(AnimationPlaygroundLayoutAuthoring.CandidateMarker))
                throw new InvalidOperationException("Measure prop contacts only in the marked isolated candidate.");
            if (Directory.Exists(DirectoryPath)) throw new IOException("Preserve the earlier contact draft evidence.");
            Directory.CreateDirectory(DirectoryPath); var exitCode = 0;
            try
            {
                var bindings = AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(AnimationPlaygroundPlayerAuthoring.BindingPath);
                if (bindings == null || !bindings.ValidatePlayer(out _)) throw new InvalidDataException("Restore the paired player calibration first.");
                var modelPath = AssetDatabase.GetAssetPath(bindings.targetModel);
                var report = new Report { modelPath = modelPath, modelSha256 = Hash(modelPath), modelMetaSha256 = Hash(modelPath + ".meta"),
                    visualScale = bindings.visualScale, visualOffset = bindings.visualOffset };
                var rows = new List<ContactCandidate>();
                foreach (var source in Sources)
                {
                    var path = "Assets/_Project/Art/Animations/Mixamo/Interactions/" + source + ".fbx";
                    var sourceHash = Hash(path); var metaHash = Hash(path + ".meta");
                    var measured = AnimationPlaygroundMotionSampler.Measure(modelPath, path, bindings.visualScale, bindings.visualOffset, 90);
                    if (!measured.finite || measured.frames.Length < 3) throw new InvalidDataException("A complete finite hand-contact timeline is required.");
                    var left = measured.frames.OrderBy(x => x.leftHand.y).First();
                    var right = measured.frames.OrderBy(x => x.rightHand.y).First();
                    var row = new ContactCandidate { path = path, sha256 = sourceHash, metaSha256 = metaHash, samples = measured.frames.Length,
                        duration = measured.seconds, lowestLeftHandSeconds = left.seconds, lowestRightHandSeconds = right.seconds,
                        lowestLeftHand = left.leftHand, lowestRightHand = right.rightHand, rootDrift = measured.rootDrift, hipsPlanarDrift = measured.hipsPlanarDrift,
                        minimumSole = measured.frames.Min(x => Mathf.Min(x.leftSole.y, x.rightSole.y)),
                        maximumBodyHeight = measured.frames.Max(x => x.bodyMaximum.y) - report.feetPlane };
                    for (var i = 1; i < measured.frames.Length - 1; i++)
                    {
                        var previous = measured.frames[i - 1]; var next = measured.frames[i + 1];
                        var delta = next.seconds - previous.seconds;
                        var leftSpeed = (next.leftHand - previous.leftHand).magnitude / delta;
                        var rightSpeed = (next.rightHand - previous.rightHand).magnitude / delta;
                        if (leftSpeed > row.fastestLeftHandSpeed) { row.fastestLeftHandSpeed = leftSpeed; row.fastestLeftHandSeconds = measured.frames[i].seconds; }
                        if (rightSpeed > row.fastestRightHandSpeed) { row.fastestRightHandSpeed = rightSpeed; row.fastestRightHandSeconds = measured.frames[i].seconds; }
                    }
                    if (Hash(path) != sourceHash || Hash(path + ".meta") != metaHash) throw new InvalidDataException("The read-only source probe changed its input.");
                    rows.Add(row);
                    File.WriteAllText(Path.Combine(DirectoryPath, Path.GetFileNameWithoutExtension(path) + ".json"), JsonUtility.ToJson(measured, true) + "\n");
                }
                if (Hash(modelPath) != report.modelSha256 || Hash(modelPath + ".meta") != report.modelMetaSha256)
                    throw new InvalidDataException("The target source/import identity changed.");
                report.candidates = rows.ToArray();
                File.WriteAllText(Path.Combine(DirectoryPath, "summary.json"), JsonUtility.ToJson(report, true) + "\n");
            }
            catch (Exception error) { File.WriteAllText(Path.Combine(DirectoryPath, "failure.txt"), error.ToString()); Debug.LogException(error); exitCode = 1; }
            EditorApplication.Exit(exitCode);
        }
    }
}
