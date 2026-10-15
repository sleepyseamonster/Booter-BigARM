using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CampCurrentLocomotionIntegrationAudit
{
    private static string Hash(string path)
    {
        using var input = File.OpenRead(path); using var digest = SHA256.Create();
        return BitConverter.ToString(digest.ComputeHash(input)).Replace("-", "").ToLowerInvariant();
    }
    public static string Run()
    {
        var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || Directory.Exists(Path.Combine(root, ".git"))
            || !File.Exists(Path.Combine(root, AnimationPlaygroundLayoutAuthoring.CandidateMarker)))
            throw new InvalidOperationException("Use the marked offline candidate.");
        var folder = Path.Combine(root, "CurrentSceneHydration20261014");
        var destination = Path.Combine(folder, "unity-verification.json");
        if (File.Exists(destination)) throw new IOException("Preserve previous integration evidence.");
        var plan = JObject.Parse(File.ReadAllText(Path.Combine(folder, "plan.json")));
        var repository = Path.GetFullPath((string)plan["repository"]);
        var errors = new List<string>(); var currentFiles = new List<object>(); var guidCount = 0;
        foreach (var file in (JArray)plan["files"])
        {
            var relative = (string)file["path"];
            var source = Path.GetFullPath(Path.Combine(repository, relative)); var candidate = Path.GetFullPath(Path.Combine(root, relative));
            if (!source.StartsWith(repository + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                || !candidate.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Dependency escaped its root.");
            if (!File.Exists(source) || !File.Exists(candidate)) { errors.Add("Missing dependency: " + relative); continue; }
            var expected = Hash(source); var actual = Hash(candidate);
            if (actual != expected) errors.Add("Current source differs: " + relative);
            currentFiles.Add(new { path = relative, sha256 = actual, currentRootExact = actual == expected,
                revisedAfterPlan = expected != (string)file["sha256"] });
            if (relative.EndsWith(".meta", StringComparison.Ordinal))
            {
                var match = Regex.Match(File.ReadAllText(candidate), @"(?m)^guid: ([a-f0-9]{32})");
                if (!match.Success) { errors.Add("Metadata GUID missing: " + relative); continue; }
                var wanted = relative[..^5]; var resolved = AssetDatabase.GUIDToAssetPath(match.Groups[1].Value);
                if (wanted != resolved) errors.Add("GUID resolves outside canonical path: " + relative + " -> " + resolved);
                guidCount++;
            }
        }
        var external = new List<object>();
        foreach (var value in (JArray)plan["unresolvedGuids"])
        {
            var guid = (string)value; var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.StartsWith("Packages/", StringComparison.Ordinal)) errors.Add("Unresolved external GUID: " + guid);
            external.Add(new { guid, path });
        }
        var scene = EditorSceneManager.OpenScene(TopDown3DAnimationPlaygroundSession.ScenePath, OpenSceneMode.Additive);
        try
        {
            Physics.SyncTransforms();
            var structural = AnimationPlaygroundSceneValidator.Validate(scene);
            errors.AddRange(structural.errors);
            var dependencies = AssetDatabase.GetDependencies(scene.path, true);
            foreach (var dependency in dependencies.Where(p => p.StartsWith("Assets/", StringComparison.Ordinal)))
            {
                var original = Path.Combine(repository, dependency); var local = Path.Combine(root, dependency);
                if (!File.Exists(original) || !File.Exists(local) || Hash(original) != Hash(local)) errors.Add("Imported scene dependency differs: " + dependency);
            }
            var session = TopDown3DAnimationPlaygroundSession.Find(scene);
            File.WriteAllText(destination, Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                state = "current-saved-scene-dependency-and-structure-verification", valid = errors.Count == 0,
                scenePath = scene.path, currentFiles, canonicalGuidPaths = guidCount, external,
                importedDependencies = dependencies, structural,
                forwardPolicyInstalledInBinding = session.Bindings.forwardLocomotion != null,
                sourceSceneSha256 = Hash(Path.Combine(repository, scene.path)), errors
            }, Newtonsoft.Json.Formatting.Indented));
            if (errors.Count > 0) throw new InvalidDataException("Integration evidence has errors: " + errors.Count);
            return destination;
        }
        finally { EditorSceneManager.CloseScene(scene, true); }
    }
}
