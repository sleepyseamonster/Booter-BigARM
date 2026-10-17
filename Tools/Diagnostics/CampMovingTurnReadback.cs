using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using BooterBigArm.TopDown3D;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Snapshot is read-only. Session preparation is restricted to the marked offline candidate.
public static class CampMovingTurnReadback
{
    const string BindingPath = "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/PlayerBindings.asset";
    static string Hash(string path)
    {
        using var hash = SHA256.Create();
        return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
    }
    static void Guard(string receipt)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating
            || EditorUtility.scriptCompilationFailed || File.Exists(receipt))
            throw new InvalidOperationException("Stable Edit Mode and a fresh receipt are required.");
    }
    public static string Snapshot(string receipt)
    {
        Guard(receipt);
        var binding = AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(BindingPath);
        if (binding == null || !binding.ValidatePlayer(out var reason))
            throw new InvalidDataException("Player bindings are invalid.");
        var field = binding.GetType().GetField("movingTurns");
        var turns = field?.GetValue(binding) as UnityEngine.Object;
        var forward = binding.forwardReviewOverride;
        var scenes = Enumerable.Range(0, SceneManager.sceneCount).Select(index =>
        {
            var scene = SceneManager.GetSceneAt(index);
            return new
            {
                scene.path, scene.isDirty, diskHash = File.Exists(scene.path) ? Hash(scene.path) : null,
                roots = scene.GetRootGameObjects().Select(root => new { root.name, id = root.GetInstanceID() }).ToArray(),
                terrain = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Terrain>(true))
                    .Select(terrain => new
                    {
                        path = AssetDatabase.GetAssetPath(terrain.terrainData),
                        sha256 = Hash(AssetDatabase.GetAssetPath(terrain.terrainData)),
                        position = new[] { terrain.transform.position.x, terrain.transform.position.y, terrain.transform.position.z },
                        scale = new[] { terrain.transform.lossyScale.x, terrain.transform.lossyScale.y, terrain.transform.lossyScale.z }
                    }).OrderBy(terrain => terrain.path).ToArray(),
                sessions = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<TopDown3DAnimationPlaygroundSession>(true))
                    .Select(session => new { session.IsLab, binding = AssetDatabase.GetAssetPath(session.Bindings), player = session.Player?.name }).ToArray()
            };
        }).ToArray();
        var result = new
        {
            playing = EditorApplication.isPlayingOrWillChangePlaymode, compiling = EditorApplication.isCompiling,
            compilationFailed = EditorUtility.scriptCompilationFailed,
            runtimeMvid = typeof(TopDown3DPlayerAnimationDriver).Assembly.ManifestModule.ModuleVersionId.ToString(),
            bindingPath = BindingPath, bindingHash = Hash(BindingPath), bindingDirty = EditorUtility.IsDirty(binding),
            movingTurns = turns == null ? null : AssetDatabase.GetAssetPath(turns),
            movingTurnsRequired = binding.GetType().GetField("movingTurnsRequired")?.GetValue(binding),
            forward = AssetDatabase.GetAssetPath(forward),
            gaits = forward.gaits.Select(gait => new { gait.role, gait.nominalSpeed, gait.metresPerCycle,
                source = AssetDatabase.GetAssetPath(gait.sourceClip), sha256 = Hash(AssetDatabase.GetAssetPath(gait.sourceClip)) }).ToArray(),
            scenes
        };
        File.WriteAllText(receipt, JsonConvert.SerializeObject(result, Formatting.Indented));
        return receipt;
    }
    public static string ValidateCandidateSession(string receipt)
    {
        Guard(receipt);
        if (!Application.isBatchMode || Directory.Exists(".git") || !File.Exists(".animation-playground-validation-project"))
            throw new InvalidOperationException("Only the marked offline candidate may prepare a held session.");
        var scene = EditorSceneManager.OpenScene(TopDown3DAnimationPlaygroundSession.ScenePath, OpenSceneMode.Additive);
        try
        {
            var session = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<TopDown3DAnimationPlaygroundSession>(true)).Single();
            if (!session.CheckDependencies()) throw new InvalidDataException(session.UnavailableReason);
            typeof(TopDown3DAnimationPlaygroundSession).GetMethod("PreparePlayer", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(session, null);
            var driver = session.Player.GetComponent<TopDown3DPlayerAnimationDriver>();
            var configured = (bool)driver.GetType().GetProperty("LaboratoryMovingTurnsConfigured").GetValue(driver);
            if (!configured || session.Readiness == AnimationPlaygroundReadiness.Failed)
                throw new InvalidDataException("Main Camp Loop startup did not select moving turns: " + session.UnavailableReason);
            File.WriteAllText(receipt, JsonConvert.SerializeObject(new { scope = "Held EditMode main Camp Loop preparation; no gameplay or physics simulation",
                scene = scene.path, movingTurnsConfigured = configured, forwardConfigured = driver.LaboratoryForwardReviewConfigured,
                binding = AssetDatabase.GetAssetPath(session.Bindings), sourceRequired = session.Bindings.GetType().GetField("movingTurnsRequired").GetValue(session.Bindings) }, Formatting.Indented));
            return receipt;
        }
        finally { EditorSceneManager.CloseScene(scene, true); }
    }
}
