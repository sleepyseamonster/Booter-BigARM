using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CampForwardReviewInstallation
{
    private const string PolicyPath = "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/ForwardLocomotionDistanceAlignedCandidate.asset";
    private const string PlanPath = "Docs/Evidence/Gameplay/LocomotionImplementation20261014/ForwardReview/scene-patch.json";
    private sealed class Patch
    {
        public string scenePath, beforeSha256, afterSha256, beforeRecord, afterRecord, intermediateSha256, intermediateRecord;
    }
    private static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    private static string Hash(byte[] bytes)
    { using var sha = SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
    private static void Guard()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            throw new InvalidOperationException("Finish compilation and remain in Edit Mode.");
        if (typeof(TopDown3DAnimationPlaygroundSession).GetField("experimentalForwardReviewRequired",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)==null
            || typeof(TopDown3DPlayerAnimationDriver).GetField("forwardReviewConfigured",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)==null
            || typeof(TopDown3DAnimationPlaygroundSession).GetMethod("ResetSessionForForwardReview")==null)
            throw new InvalidOperationException("Refresh the paired runtime assembly before installation.");
    }
    private static bool Required(TopDown3DAnimationPlaygroundSession session) => session != null && (bool)session.GetType()
        .GetField("experimentalForwardReviewRequired",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(session);
    private static AnimationPlaygroundForwardLocomotionProfile Validate(TopDown3DAnimationPlaygroundSession session)
    {
        var policy = AssetDatabase.LoadAssetAtPath<AnimationPlaygroundForwardLocomotionProfile>(PolicyPath);
        if (session == null || !session.Bindings.ValidatePlayer(out _) || policy == null || policy.qualified || policy.terrainQualified
            || !policy.Validate(session.Bindings.targetModel, policy.locomotion, out _, false)
            || policy.footProjection != session.Bindings.footProjection || session.Stations.Count(s => s != null && s.StationId == "AP02") != 1)
            throw new InvalidOperationException("Restore the paired baseline, unqualified policy and authored AP02 station.");
        return policy;
    }
    private static System.Collections.Generic.Dictionary<string,string> Records(string text)
    {
        var matches = Regex.Matches(text, @"(?m)^--- !u!\d+ &[^\n]+\n");
        return matches.Cast<Match>().Select((m, i) => new { header=m.Value.TrimEnd(),
            body=text.Substring(m.Index, (i+1 < matches.Count ? matches[i+1].Index : text.Length)-m.Index) })
            .ToDictionary(x=>x.header,x=>x.body);
    }
    public static string InstallCandidate()
    {
        Guard();
        if (!Application.isBatchMode || Directory.Exists(Path.Combine(Root, ".git"))
            || !File.Exists(Path.Combine(Root, ".animation-playground-validation-project")))
            throw new InvalidOperationException("Candidate installation requires the marked isolated copy.");
        var directory = Path.Combine(Root, "LocomotionImplementation20261014/ForwardReviewInstallationV3");
        if (Directory.Exists(directory)) throw new IOException("Preserve earlier installation evidence.");
        var path = TopDown3DAnimationPlaygroundSession.ScenePath;
        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        try
        {
            if (scene.isDirty) throw new InvalidOperationException("The saved candidate must open clean.");
            var session = TopDown3DAnimationPlaygroundSession.Find(scene); var policy = Validate(session);
            var current = File.ReadAllBytes(Path.Combine(Root,path));
            var before = current; string intermediateSha = null, intermediateRecord = null;
            var previous = Path.Combine(Root,"LocomotionImplementation20261014/ForwardReviewInstallationV2");
            if (Directory.Exists(previous))
            {
                if (Hash(current)!=Hash(File.ReadAllBytes(Path.Combine(previous,"after.unity"))))
                    throw new InvalidOperationException("The intermediate candidate changed; preserve it.");
                before=File.ReadAllBytes(Path.Combine(previous,"before.unity")); intermediateSha=Hash(current);
            }
            var beforeText = Encoding.UTF8.GetString(before);
            var a = Records(beforeText.Replace("\r\n","\n"));
            var scriptGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(session)));
            var key = a.Keys.Single(k=>a[k].Contains("guid: " + scriptGuid + ","));
            var record = a[key];
            if (record.Contains("  experimentalForwardReview:")) throw new InvalidOperationException("Review is already authored; preserve its checkpoint.");
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(policy,out string guid,out long id)) throw new InvalidOperationException("The policy needs its original asset identity.");
            var line = "  experimentalForwardReview: {fileID: " + id + ", guid: " + guid + ", type: 2}\n  experimentalForwardReviewRequired: 1\n";
            if (Regex.Matches(record,@"(?m)^  palettePrefab:.*\n").Count!=1) throw new InvalidOperationException("The session layout changed.");
            var revised = Regex.Replace(record,@"(?m)^  palettePrefab:.*\n",m=>m.Value+line);
            var eol = beforeText.Contains("\r\n") ? "\r\n" : "\n";
            var oldRecord = record.Replace("\n",eol); var newRecord = revised.Replace("\n",eol);
            if(intermediateSha!=null)intermediateRecord=Records(Encoding.UTF8.GetString(current).Replace("\r\n","\n"))[key].Replace("\n",eol);
            var patched = beforeText.Replace(oldRecord,newRecord);
            Directory.CreateDirectory(directory); File.WriteAllBytes(Path.Combine(directory,"before.unity"),before);
            if(intermediateSha!=null)File.WriteAllBytes(Path.Combine(directory,"intermediate.unity"),current);
            EditorSceneManager.CloseScene(scene,true);
            File.WriteAllText(Path.Combine(Root,path),patched,new UTF8Encoding(false));
            File.WriteAllBytes(Path.Combine(directory,"after.unity"),File.ReadAllBytes(Path.Combine(Root,path)));
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
            scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            if (TopDown3DAnimationPlaygroundSession.Find(scene)?.ExperimentalForwardReview!=policy || !Required(TopDown3DAnimationPlaygroundSession.Find(scene)) || scene.isDirty)
                throw new InvalidOperationException("Verify the saved review reference after reload.");
            var plan = new Patch { scenePath=path, beforeSha256=Hash(before), afterSha256=Hash(new UTF8Encoding(false).GetBytes(patched)),
                beforeRecord=oldRecord, afterRecord=newRecord, intermediateSha256=intermediateSha, intermediateRecord=intermediateRecord };
            var output = Path.Combine(directory,"scene-patch.json");
            File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(plan,Newtonsoft.Json.Formatting.Indented),new UTF8Encoding(false));
            return output;
        }
        finally { if(scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene,true); }
    }

    // Applies the already measured single-record patch. It never saves a whole
    // author scene or opens a different scene to establish a clean state.
    public static string InstallAuthor()
    {
        Guard();
        if (!Directory.Exists(Path.Combine(Root,".git"))) throw new InvalidOperationException("Use the canonical author checkout.");
        var plan = Newtonsoft.Json.JsonConvert.DeserializeObject<Patch>(File.ReadAllText(Path.Combine(Root,PlanPath)));
        if (plan.scenePath != TopDown3DAnimationPlaygroundSession.ScenePath) throw new InvalidOperationException("Unexpected scene patch.");
        if (SceneManager.sceneCount != 1 || SceneManager.GetActiveScene().path != plan.scenePath
            || SceneManager.GetActiveScene().isDirty || PrefabStageUtility.GetCurrentPrefabStage()!=null)
            throw new InvalidOperationException("The clean Camp Loop scene must already be open, with no prefab stage or other scenes.");
        var scene = SceneManager.GetActiveScene(); var policy = Validate(TopDown3DAnimationPlaygroundSession.Find(scene));
        var absolute = Path.Combine(Root,plan.scenePath); var bytes = File.ReadAllBytes(absolute);
        if (Hash(bytes)==plan.afterSha256)
        {
            // A domain reload may carry an older in-memory serialization of a
            // newly added field. The clean saved scene is authoritative here.
            if (TopDown3DAnimationPlaygroundSession.Find(scene).ExperimentalForwardReview != policy || !Required(TopDown3DAnimationPlaygroundSession.Find(scene)))
                scene=EditorSceneManager.OpenScene(plan.scenePath,OpenSceneMode.Single);
            if (TopDown3DAnimationPlaygroundSession.Find(scene).ExperimentalForwardReview != policy)
                throw new InvalidOperationException("Reload the already patched scene before claiming installation.");
            if (!Required(TopDown3DAnimationPlaygroundSession.Find(scene)) || scene.isDirty) throw new InvalidOperationException("The saved review readiness flag did not reload.");
            return "already installed";
        }
        var actualBeforeSha=Hash(bytes);
        if (actualBeforeSha!=plan.beforeSha256 && actualBeforeSha!=plan.intermediateSha256) throw new InvalidOperationException("The author scene changed since candidate qualification.");
        var expectedRecord=actualBeforeSha==plan.intermediateSha256 ? plan.intermediateRecord : plan.beforeRecord;
        var text = Encoding.UTF8.GetString(bytes);
        if (text.IndexOf(expectedRecord,StringComparison.Ordinal)<0 || text.IndexOf(expectedRecord,StringComparison.Ordinal)
            != text.LastIndexOf(expectedRecord,StringComparison.Ordinal)) throw new InvalidOperationException("The session record is not unique.");
        var after = new UTF8Encoding(false).GetBytes(text.Replace(expectedRecord,plan.afterRecord));
        if (Hash(after)!=plan.afterSha256) throw new InvalidOperationException("Patch output does not match its seal.");
        var directory = Path.Combine(Root,"Logs/ForwardReviewAuthorInstallation20261014V2");
        if (Directory.Exists(directory)) throw new IOException("Preserve the earlier author receipt.");
        Directory.CreateDirectory(directory); File.WriteAllBytes(Path.Combine(directory,"before.unity"),bytes);
        var temporary = Path.Combine(directory,"candidate.unity"); File.WriteAllBytes(temporary,after);
        if (Hash(File.ReadAllBytes(absolute))!=actualBeforeSha) throw new InvalidOperationException("Scene changed before write.");
        File.Replace(temporary,absolute,Path.Combine(directory,"replaced.unity"));
        AssetDatabase.ImportAsset(plan.scenePath,ImportAssetOptions.ForceUpdate);
        scene=EditorSceneManager.OpenScene(plan.scenePath,OpenSceneMode.Single);
        if (TopDown3DAnimationPlaygroundSession.Find(scene)?.ExperimentalForwardReview!=policy || !Required(TopDown3DAnimationPlaygroundSession.Find(scene)) || scene.isDirty
            || Hash(File.ReadAllBytes(absolute))!=plan.afterSha256) throw new InvalidOperationException("Verify the scene reload before claiming installation.");
        File.WriteAllText(Path.Combine(directory,"receipt.json"),Newtonsoft.Json.JsonConvert.SerializeObject(new {
            state="experimental-flat-deck-review-installed-awaiting-author-playtest",plan.beforeSha256,actualBeforeSha,plan.afterSha256,
            changedRecords=1,forwardQualified=policy.qualified,terrainQualified=policy.terrainQualified,scene.isDirty },Newtonsoft.Json.Formatting.Indented));
        return Path.Combine(directory,"receipt.json");
    }
}
