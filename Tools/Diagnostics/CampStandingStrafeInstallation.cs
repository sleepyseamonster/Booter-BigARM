using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using BooterBigArm.TopDown3D;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Imports only the sealed three private pairs and changes two binding fields.
// Never opens, saves or reloads a scene, changes Play Mode or focuses a window.
public static class CampStandingStrafeInstallation
{
    public static string Install(string planPath)
    {
        var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        var plan = JObject.Parse(File.ReadAllText(planPath));
        if (!Directory.Exists(Path.Combine(root, ".git")) || !string.Equals(root, (string)plan["author"], StringComparison.OrdinalIgnoreCase)
            || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || SceneManager.sceneCount != 1)
            throw new InvalidOperationException("Use the stable existing author Edit Mode session.");
        var scene = SceneManager.GetSceneAt(0);
        if (scene.path != TopDown3DAnimationPlaygroundSession.ScenePath || scene.isDirty)
            throw new InvalidOperationException("Preserve the author's scene; installation requires its clean existing Camp Loop session.");
        string Hash(string path) { using var sha = SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant(); }
        string Within(string basePath, string relative)
        {
            var result = Path.GetFullPath(Path.Combine(basePath, relative));
            if (!result.StartsWith(Path.GetFullPath(basePath) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Installation path escaped its root.");
            return result;
        }
        foreach (var entry in plan["sources"])
            if (Hash(Within(root, (string)entry["path"])) != (string)entry["sha256"])
                throw new InvalidDataException("Author task source changed since testing: " + entry["path"]);
        foreach (var entry in plan["protected"])
            if (Hash(Within(root, (string)entry["path"])) != (string)entry["sha256"])
                throw new InvalidDataException("Author protected input changed: " + entry["path"]);
        // These APIs distinguish the final compiled ownership/input/binding contract.
        if (typeof(TopDown3DAnimationPlaygroundBindings).GetField("standingStrafeReviewRequired") == null
            || typeof(TopDown3DInputRouter).GetProperty("LaboratoryStandingFacingHeld") == null
            || typeof(TopDown3DPlayerMotor).GetMethod("ConfigureLaboratoryStandingStrafe") == null)
            throw new InvalidOperationException("Wait for the paired runtime revision.");
        var bindingPath = "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/PlayerBindings.asset";
        var bindings = AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(bindingPath);
        if (bindings == null || EditorUtility.IsDirty(bindings) || bindings.standingStrafeReview != null || bindings.standingStrafeReviewRequired)
            throw new InvalidOperationException("Preserve an existing dirty or selected strafe binding.");
        var beforeJson = JObject.Parse(JsonUtility.ToJson(bindings));
        var beforeBytes = File.ReadAllBytes(Within(root, bindingPath));
        var sceneHash = Hash(Within(root, scene.path));
        foreach (var entry in plan["privatePairs"])
        {
            var relative = (string)entry["path"]; var source = Within((string)plan["candidate"], relative); var target = Within(root, relative);
            if (Hash(source) != (string)entry["sha256"] || File.Exists(target))
                throw new InvalidDataException("Preserve changed/private destination bytes: " + relative);
            if (relative.EndsWith(".meta", StringComparison.Ordinal))
            {
                var guid = System.Text.RegularExpressions.Regex.Match(File.ReadAllText(source), @"guid: (\w+)").Groups[1].Value;
                if (guid.Length != 32 || !string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid)))
                    throw new InvalidDataException("Private GUID is already owned: " + relative);
            }
        }
        try
        {
            foreach (var entry in plan["privatePairs"])
            {
                var relative = (string)entry["path"]; var target = Within(root, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(target)); File.Copy(Within((string)plan["candidate"], relative), target, false);
            }
            foreach (var entry in plan["privatePairs"].Where(e => !((string)e["path"]).EndsWith(".meta", StringComparison.Ordinal)))
                AssetDatabase.ImportAsset((string)entry["path"], ImportAssetOptions.ForceSynchronousImport);
            var profile = AssetDatabase.LoadAssetAtPath<AnimationPlaygroundStandingStrafeProfile>(
                "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/StandingStrafeReview.asset");
            if (profile == null || !profile.Validate(bindings.targetModel, bindings.footProjection, out _))
                throw new InvalidDataException("Imported standing strafe pair is invalid.");
            bindings.standingStrafeReview = profile; bindings.standingStrafeReviewRequired = true;
            var afterJson = JObject.Parse(JsonUtility.ToJson(bindings));
            foreach (var json in new[] { beforeJson, afterJson }) { json.Remove("standingStrafeReview"); json.Remove("standingStrafeReviewRequired"); }
            if (!JToken.DeepEquals(beforeJson, afterJson) || !bindings.ValidatePlayer(out var reason))
                throw new InvalidDataException("Unrelated binding changed or player validation failed.");
            EditorUtility.SetDirty(bindings); AssetDatabase.SaveAssetIfDirty(bindings);
            foreach (var entry in plan["protected"].Where(e => (string)e["path"] != bindingPath))
                if (Hash(Within(root, (string)entry["path"])) != (string)entry["sha256"])
                    throw new InvalidDataException("Protected input changed during installation: " + entry["path"]);
            foreach (var entry in plan["privatePairs"])
                if (Hash(Within(root, (string)entry["path"])) != (string)entry["sha256"])
                    throw new InvalidDataException("Private import serialized unexpected bytes.");
            if (scene.isDirty || Hash(Within(root, scene.path)) != sceneHash || EditorApplication.isCompiling)
                throw new InvalidOperationException("Author scene/compile state changed during installation.");
            var result = new { state = "installed-unqualified-ap02-slow-strafe-review", sceneSaved = false, sceneReloaded = false,
                playing = EditorApplication.isPlayingOrWillChangePlaymode, sceneDirty = scene.isDirty, profile.maximumSpeed,
                bindingSha256 = Hash(Within(root, bindingPath)), runtimeModule = typeof(TopDown3DPlayerMotor).Module.ModuleVersionId,
                forwardSpeeds = bindings.forwardReviewOverride.gaits.Select(g => g.nominalSpeed).ToArray() };
            var receipt = Path.Combine(Path.GetDirectoryName(planPath), "author-install.json");
            if (File.Exists(receipt)) throw new IOException("Preserve earlier installation receipt.");
            File.WriteAllText(receipt, Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
            return receipt;
        }
        catch
        {
            bindings.standingStrafeReview = null; bindings.standingStrafeReviewRequired = false;
            File.WriteAllBytes(Within(root, bindingPath), beforeBytes);
            AssetDatabase.ImportAsset(bindingPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            throw;
        }
    }
}
