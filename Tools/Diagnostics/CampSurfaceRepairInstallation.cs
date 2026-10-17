using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using BooterBigArm.TopDown3D;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Import only sealed task code into the existing author Editor. No scene save,
// scene reload, Play Mode, input, focus changes or private asset reselection.
public static class CampSurfaceRepairInstallation
{
    public sealed class Seal { public string relative, sha256; }
    public sealed class Plan
    {
        public string root, candidate, backup;
        public Seal[] code, protectedFiles;
        public Dictionary<string, string> before;
    }
    private static string Hash(string path)
    {
        using var hash = SHA256.Create();
        return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
    }
    private static string PathIn(string root, string relative)
    {
        var full = Path.GetFullPath(Path.Combine(root, relative));
        if (!full.StartsWith(Path.GetFullPath(root) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new IOException("Sealed path escaped its root.");
        return full;
    }
    public static string Import(string planPath)
    {
        var plan = JsonConvert.DeserializeObject<Plan>(File.ReadAllText(planPath));
        var actual = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (!string.Equals(actual, Path.GetFullPath(plan.root), StringComparison.OrdinalIgnoreCase)
            || !Directory.Exists(Path.Combine(actual, ".git"))
            || !File.Exists(Path.Combine(plan.candidate, ".animation-playground-validation-project"))
            || Directory.Exists(Path.Combine(plan.candidate, ".git"))
            || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating
            || EditorUtility.scriptCompilationFailed || SceneManager.sceneCount != 1
            || SceneManager.GetActiveScene().path != TopDown3DAnimationPlaygroundSession.ScenePath)
            throw new InvalidOperationException("Stable author Camp Loop Edit Mode and a marked source candidate are required.");
        var scene = SceneManager.GetActiveScene();
        var dirty = scene.isDirty;
        var roots = scene.GetRootGameObjects().Select(root => root.GetInstanceID()).ToArray();
        foreach (var seal in plan.protectedFiles)
            if (Hash(PathIn(actual, seal.relative)) != seal.sha256)
                throw new InvalidDataException("Protected input changed: " + seal.relative);
        foreach (var seal in plan.code)
        {
            var source = PathIn(plan.candidate, seal.relative);
            var target = PathIn(actual, seal.relative);
            if (Hash(source) != seal.sha256) throw new InvalidDataException("Candidate changed: " + seal.relative);
            if (seal.relative.EndsWith(".meta", StringComparison.Ordinal))
            {
                var match = System.Text.RegularExpressions.Regex.Match(File.ReadAllText(source), @"guid: ([0-9a-f]{32})");
                if (!match.Success) throw new InvalidDataException("Metadata GUID absent.");
                var existing = AssetDatabase.GUIDToAssetPath(match.Groups[1].Value);
                if (existing.Length > 0 && existing != seal.relative.Substring(0, seal.relative.Length - 5))
                    throw new InvalidDataException("GUID already belongs to " + existing);
            }
            if (File.Exists(target) && Hash(target) == seal.sha256) continue;
            if (plan.before.TryGetValue(seal.relative, out var before))
            {
                if (!File.Exists(target) || Hash(target) != before || Hash(PathIn(plan.backup, seal.relative)) != before)
                    throw new InvalidDataException("Author source or recovery bytes changed: " + seal.relative);
            }
            else if (File.Exists(target)) throw new IOException("New task destination already exists: " + seal.relative);
        }
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var seal in plan.code)
            {
                var target = PathIn(actual, seal.relative);
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Copy(PathIn(plan.candidate, seal.relative), target, true);
            }
        }
        finally { AssetDatabase.StopAssetEditing(); }
        // Import the asset itself, preserving its paired metadata verbatim.
        foreach (var seal in plan.code.Where(seal => !seal.relative.EndsWith(".meta", StringComparison.Ordinal)))
            AssetDatabase.ImportAsset(seal.relative, ImportAssetOptions.ForceUpdate);
        foreach (var seal in plan.code)
            if (Hash(PathIn(actual, seal.relative)) != seal.sha256) throw new InvalidDataException("Imported source differs from its seal.");
        if (scene.isDirty != dirty || !scene.GetRootGameObjects().Select(root => root.GetInstanceID()).SequenceEqual(roots))
            throw new InvalidOperationException("Author scene state changed during the code-only import.");
        UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
        return "Sealed platform/ramp/stair repair imported; wait for successful compilation before author testing.";
    }
}
