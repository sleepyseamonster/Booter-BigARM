using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Exact private handoff after the matching public code has compiled. It never
// saves, reloads or replaces the author's dirty scene, or starts Play Mode.
public static class CampExpandedGaitInstallation
{
    private const string PolicyPath = "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/ExpandedForwardGaitReview.asset";
    private const string SprintPath = "Assets/_Project/Art/Animations/Mixamo/Derived/FullSprintStandardReview.anim";
    private static string Hash(string path) {
        using var sha = System.Security.Cryptography.SHA256.Create();
        return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
    }
    public static string Install(string archive)
    {
        if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
            || !Directory.Exists(".git") || SceneManager.sceneCount != 1)
            throw new InvalidOperationException("Use stable author Edit Mode with the existing Camp Loop scene.");
        var scene = SceneManager.GetSceneAt(0); var session = TopDown3DAnimationPlaygroundSession.Find(scene);
        if (!scene.isLoaded || scene.path != TopDown3DAnimationPlaygroundSession.ScenePath || session == null)
            throw new InvalidOperationException("Preserve the already open Camp Loop scene.");
        var sceneHash = Hash(scene.path); var dirty = scene.isDirty;
        var recipe = JObject.Parse(File.ReadAllText(Path.Combine(archive, "manifest.json")));
        var before = (JArray)recipe["privateBefore"]; var after = (JArray)recipe["privateAfter"];
        var expectedBefore = new HashSet<string>(new[] { AnimationPlaygroundPlayerAuthoring.BindingPath, AnimationPlaygroundPlayerAuthoring.BindingPath + ".meta" });
        var expectedAfter = new HashSet<string>(new[] { AnimationPlaygroundPlayerAuthoring.BindingPath, PolicyPath, SprintPath }.SelectMany(p => new[] { p, p + ".meta" }));
        if (!expectedBefore.SetEquals(before.Select(p => (string)p["path"])) || before.Count != 2
            || !expectedAfter.SetEquals(after.Select(p => (string)p["path"])) || after.Count != 6)
            throw new InvalidDataException("Unexpected private gait handoff.");
        foreach (var item in before)
            if (Hash((string)item["path"]) != (string)item["sha256"]
                || Hash(Path.Combine(archive, "Before", (string)item["path"])) != (string)item["sha256"])
                throw new InvalidDataException("The author's previous bindings changed.");
        foreach (var item in after)
            if (Hash(Path.Combine(archive, "After", (string)item["path"])) != (string)item["sha256"])
                throw new InvalidDataException("The private recovery payload changed.");
        foreach (var item in (JArray)recipe["codeAfter"])
            if (Hash((string)item["path"]) != (string)item["sha256"])
                throw new InvalidDataException("Install and compile the tested public code first.");
        foreach (var item in (JArray)recipe["preservedSources"])
            if (Hash((string)item["path"]) != (string)item["sha256"])
                throw new InvalidDataException("An accepted or original source changed.");
        foreach (var path in new[] { PolicyPath, SprintPath }.SelectMany(p => new[] { p, p + ".meta" }))
            if (File.Exists(path)) throw new IOException("Preserve existing source/GUID: " + path);
        try
        {
            foreach (var item in after) File.Copy(Path.Combine(archive, "After", (string)item["path"]), (string)item["path"], true);
            foreach (var path in new[] { SprintPath, PolicyPath, AnimationPlaygroundPlayerAuthoring.BindingPath })
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var policy = session.ExperimentalForwardReview;
            if (policy == null || !policy.ExpandedReview || !policy.Validate(session.Bindings.targetModel, policy.locomotion, out _, false)
                || !session.Bindings.ValidatePlayer(out _) || policy.qualified || policy.terrainQualified
                || policy.gaits[3].nominalSpeed != 5 || policy.gaits[4].nominalSpeed != 6
                || scene.isDirty != dirty || Hash(scene.path) != sceneHash)
                throw new InvalidDataException("Expanded review or unchanged author scene verification failed.");
            foreach (var item in after) if (Hash((string)item["path"]) != (string)item["sha256"]) throw new InvalidDataException("Imported bytes differ.");
            var output = Path.GetFullPath("Logs/expanded-gait-author-installed.json");
            File.WriteAllText(output, Newtonsoft.Json.JsonConvert.SerializeObject(new {
                state = "expanded-gamepad-review-installed-awaiting-author-feedback", profile = AssetDatabase.GetAssetPath(policy),
                stages = new[] { "Idle", "Walk", "Jog", "Run", "FastRun", "Sprint" },
                speeds = policy.gaits.Select(g => g.nominalSpeed).ToArray(), gamepad = "Stick to fast run; existing RB to sprint",
                sceneHash, sceneDirty = dirty, sceneSaved = false, playModeEntered = false,
                policy.qualified, policy.terrainQualified, payloads = after
            }, Newtonsoft.Json.Formatting.Indented));
            return output;
        }
        catch
        {
            foreach (var item in before) File.Copy(Path.Combine(archive, "Before", (string)item["path"]), (string)item["path"], true);
            AssetDatabase.ImportAsset(AnimationPlaygroundPlayerAuthoring.BindingPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            // New sources are retained unbound, including their GUIDs, for recovery.
            throw;
        }
    }
}
