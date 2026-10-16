using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using BooterBigArm.TopDown3D;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Asset-only installation; the author's open and unsaved scene is never saved,
// reloaded, restarted, instantiated into, or brought to the foreground.
public static class CampDirectionalInstallation
{
    public static string Inspect(string output)
    {
        var scene=SceneManager.GetActiveScene();
        if(Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
            || EditorUtility.scriptCompilationFailed || scene.path!=TopDown3DAnimationPlaygroundSession.ScenePath || File.Exists(output))
            throw new InvalidOperationException("Inspect the installed author in EditMode into a fresh receipt.");
        var bindings=AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(
            "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/PlayerBindings.asset");
        if(bindings==null)throw new InvalidDataException("Missing binding.");
        if(!bindings.ValidatePlayer(out var reason))throw new InvalidDataException(reason);
        var directional=bindings.standingStrafeReview;var native=bindings.continuationReview;
        if(directional==null || !directional.AllDirections || native==null || !native.NativeCampGround)
            throw new InvalidDataException("Expected the paired directional/native selection.");
        var result=new{state="read-back-installed-directional-native-review",utc=DateTime.UtcNow,scene=scene.path,scene.isDirty,
            playing=EditorApplication.isPlayingOrWillChangePlaymode,compiling=EditorApplication.isCompiling,
            compilationFailed=EditorUtility.scriptCompilationFailed,
            module=typeof(TopDown3DPlayerAnimationDriver).Assembly.ManifestModule.ModuleVersionId,
            directional=AssetDatabase.GetAssetPath(directional),directional.schema,directional.maximumSpeed,
            directional.maximumLateralSpeed,directional.maximumBackwardSpeed,
            gaits=directional.directionalGaits.Select(g=>new{g.nominalSpeed,source=AssetDatabase.GetAssetPath(g.sourceClip)}).ToArray(),
            native=AssetDatabase.GetAssetPath(native),nativeSchema=native.schema,native.maximumSlope,native.maximumAirSpeed,
            bodyVertices=native.nativeBodyProjection.vertices.Length,
            forward=AssetDatabase.GetAssetPath(bindings.forwardReviewOverride),
            forwardSpeeds=bindings.forwardReviewOverride.gaits.Select(g=>g.nominalSpeed).ToArray(),
            bindings.forwardReviewOverride.qualified,bindings.forwardReviewOverride.terrainQualified,
            scope="Read-only installed selection and compilation check. No scene save, reload, Play Mode or input."};
        File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented)+"\n",new System.Text.UTF8Encoding(false));
        return output;
    }

    public static string Install(string planPath)
    {
        var root=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
        var plan=JObject.Parse(File.ReadAllText(planPath));
        var source=(string)plan["candidate"];
        var scene=SceneManager.GetActiveScene();
        if(Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
            || !Directory.Exists(Path.Combine(root,".git")) || Path.GetFullPath((string)plan["author"])!=root
            || scene.path!=TopDown3DAnimationPlaygroundSession.ScenePath || scene.isDirty!=(bool)plan["sceneDirty"]
            || !File.Exists(Path.Combine(source,".animation-playground-validation-project")) || Directory.Exists(Path.Combine(source,".git")))
            throw new InvalidOperationException("Require the existing author EditMode scene, retained dirty state and marked candidate.");
        string Hash(string path){using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
        string Under(string basePath,string relative)
        {
            var path=Path.GetFullPath(Path.Combine(basePath,relative));
            if(!path.StartsWith(Path.GetFullPath(basePath)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Plan paths must remain beneath their declared checkout.");return path;
        }
        void Check(JToken values,string basePath)
        {foreach(var item in values)if(Hash(Under(basePath,(string)item["path"]))!=(string)item["sha256"])throw new InvalidDataException("Changed input: "+item["path"]);}
        Check(plan["runtime"],root);Check(plan["runtime"],source);Check(plan["protected"],root);
        Check(plan["privateFiles"],source);
        foreach(var item in plan["privateFiles"])
            if(File.Exists(Under(root,(string)item["path"]))
                && ((bool?)plan["reuseExactPreparedPrivateFiles"]!=true
                    || Hash(Under(root,(string)item["path"]))!=(string)item["sha256"]))
                throw new IOException("Preserve existing destination: "+item["path"]);
        var receipt=(string)plan["receipt"];
        if(File.Exists(receipt))throw new IOException("Preserve the previous installation receipt.");
        const string bindingPath="Assets/_Project/Settings/AnimationPlayground/PrivateBindings/PlayerBindings.asset";
        const string profilePath="Assets/_Project/Settings/AnimationPlayground/PrivateBindings/CampNativeMotionReview.asset";
        var bindings=AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(bindingPath);
        if(bindings==null || bindings.continuationReview==null || bindings.continuationReview.schema!=1
            || bindings.standingStrafeReview==null || bindings.standingStrafeReview.schema!=1
            || !bindings.ValidatePlayer(out var reason))throw new InvalidOperationException("Retain the previous selected slow-strafe/mild-ground binding.");
        var oldBytes=File.ReadAllBytes(Path.Combine(root,bindingPath));
        string RetainedBinding(byte[] bytes)
        {
            var yaml=System.Text.Encoding.UTF8.GetString(bytes).Replace("\r\n","\n");
            foreach(var field in new[]{"continuationReview","standingStrafeReview"})
                if(System.Text.RegularExpressions.Regex.Matches(yaml,@"(?m)^  "+field+@":").Count>1)
                    throw new InvalidDataException("Duplicate directional selection field.");
            return System.Text.RegularExpressions.Regex.Replace(yaml,@"(?m)^  (?:continuationReview|standingStrafeReview):[^\n]*(?:\n|$)","");
        }
        var retainedBefore=RetainedBinding(oldBytes);var sceneHash=Hash(Path.Combine(root,scene.path));
        var roots=scene.GetRootGameObjects();
        try
        {
            foreach(var item in plan["privateFiles"])
            {var path=(string)item["path"];Directory.CreateDirectory(Path.GetDirectoryName(Under(root,path)));if(!File.Exists(Under(root,path)))File.Copy(Under(source,path),Under(root,path),false);}
            foreach(var item in plan["privateFiles"].Where(p=>!((string)p["path"]).EndsWith(".meta",StringComparison.Ordinal)))
                AssetDatabase.ImportAsset((string)item["path"],ImportAssetOptions.ForceSynchronousImport);
            var profile=AssetDatabase.LoadAssetAtPath<AnimationPlaygroundContinuationProfile>(profilePath);
            if(profile==null || !profile.Validate(bindings.targetModel,bindings.footProjection,out reason))throw new InvalidDataException(reason??"Continuation import failed.");
            var directional=AssetDatabase.LoadAssetAtPath<AnimationPlaygroundStandingStrafeProfile>("Assets/_Project/Settings/AnimationPlayground/PrivateBindings/StandingDirectionalReview.asset");
            if(directional==null || !directional.AllDirections || !directional.Validate(bindings.targetModel,bindings.footProjection,out reason))throw new InvalidDataException(reason??"Directional import failed.");
            bindings.continuationReview=profile;bindings.standingStrafeReview=directional;
            if(!bindings.ValidatePlayer(out reason))throw new InvalidDataException(reason);
            EditorUtility.SetDirty(bindings);AssetDatabase.SaveAssetIfDirty(bindings);
            // Persisted YAML retains GUID/local-file references. Editor JSON
            // contains transient instance IDs that can change during imports.
            if(retainedBefore!=RetainedBinding(File.ReadAllBytes(Path.Combine(root,bindingPath))))
            {
                var diagnostic=receipt+".rejected-"+Guid.NewGuid().ToString("N");
                File.WriteAllBytes(diagnostic+".before.asset",oldBytes);
                File.Copy(Path.Combine(root,bindingPath),diagnostic+".after.asset",false);
                throw new InvalidDataException("An unrelated persisted player binding changed; retained comparison: "+diagnostic);
            }
            if(Hash(Path.Combine(root,scene.path))!=sceneHash || scene.isDirty!=(bool)plan["sceneDirty"]
                || !roots.SequenceEqual(scene.GetRootGameObjects()))
                throw new InvalidOperationException("The author scene changed during asset installation.");
            Check(plan["privateFiles"],root);
            foreach(var item in plan["protected"].Where(p=>(string)p["path"]!=bindingPath))
                if(Hash(Under(root,(string)item["path"]))!=(string)item["sha256"])throw new InvalidDataException("A protected source changed.");
            var result=new{state="installed-camp-directional-native-ground-review",utc=DateTime.UtcNow,
                scene=scene.path,scene.isDirty,sceneSha256=sceneHash,bindingSha256=Hash(Path.Combine(root,bindingPath)),
                profile=profilePath,profile.maximumSlope,profile.maximumAirSpeed,
                directional=AssetDatabase.GetAssetPath(directional),directional.maximumLateralSpeed,directional.maximumBackwardSpeed,privateFileCount=((JArray)plan["privateFiles"]).Count,
                forwardQualified=bindings.forwardReviewOverride.qualified,terrainQualified=bindings.forwardReviewOverride.terrainQualified,
                module=typeof(TopDown3DPlayerAnimationDriver).Assembly.ManifestModule.ModuleVersionId,
                bindingOnlyDirectionalSelectionFieldsChanged=true,
                scope="New private directional payload/metadata files and two existing profile selection fields only. No scene save/reload, Play Mode, input or foreground operation."};
            File.WriteAllText(receipt,Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented)+"\n",new System.Text.UTF8Encoding(false));
            return receipt;
        }
        catch
        {
            File.WriteAllBytes(Path.Combine(root,bindingPath),oldBytes);
            AssetDatabase.ImportAsset(bindingPath,ImportAssetOptions.ForceSynchronousImport);throw;
        }
    }
}
