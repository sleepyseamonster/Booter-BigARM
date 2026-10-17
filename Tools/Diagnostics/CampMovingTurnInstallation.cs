using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;

// Exact sealed files only; never opens/saves a scene, enters Play Mode or drives input.
public static class CampMovingTurnInstallation
{
    public sealed class FileSeal {public string relative,sha256;}
    public sealed class Plan
    {
        public string root,candidate,backup,binding,bindingBefore,bindingAfter,bindingResult;
        public FileSeal[] code,privateAssets,protectedFiles;
        public Dictionary<string,string> before;
    }
    static string Hash(string path){using var h=SHA256.Create();return BitConverter.ToString(h.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
    static string PathIn(string root,string relative)
    {
        var full=Path.GetFullPath(Path.Combine(root,relative));if(!full.StartsWith(Path.GetFullPath(root)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new IOException("Path escaped sealed root.");return full;
    }
    static void CheckGuid(string source,string relative)
    {
        if(!relative.EndsWith(".meta",StringComparison.Ordinal))return;
        var match=System.Text.RegularExpressions.Regex.Match(File.ReadAllText(source),@"guid: ([0-9a-f]{32})");
        if(!match.Success)throw new InvalidDataException("New metadata lacks a GUID.");
        var existing=AssetDatabase.GUIDToAssetPath(match.Groups[1].Value);
        if(existing.Length>0 && existing!=relative.Substring(0,relative.Length-5))throw new InvalidDataException("New GUID already belongs to "+existing);
    }
    static Plan Read(string planPath)
    {
        var p=JsonConvert.DeserializeObject<Plan>(File.ReadAllText(planPath));
        var actual=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
        if(!string.Equals(actual,Path.GetFullPath(p.root),StringComparison.OrdinalIgnoreCase) || !Directory.Exists(Path.Combine(actual,".git"))
            || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating
            || SceneManager.sceneCount!=1 || SceneManager.GetActiveScene().path!=TopDown3DAnimationPlaygroundSession.ScenePath
            || !File.Exists(Path.Combine(p.candidate,".animation-playground-validation-project")) || Directory.Exists(Path.Combine(p.candidate,".git")))
            throw new InvalidOperationException("Stable author Edit Mode, current Camp Loop and marked source candidate required.");
        foreach(var f in p.protectedFiles)if(Hash(PathIn(actual,f.relative))!=f.sha256)throw new InvalidDataException("Protected input changed: "+f.relative);
        return p;
    }
    public static string Code(string planPath)
    {
        var p=Read(planPath);foreach(var f in p.code)
        {
            if(Hash(PathIn(p.candidate,f.relative))!=f.sha256)throw new InvalidDataException("Candidate code changed: "+f.relative);
            CheckGuid(PathIn(p.candidate,f.relative),f.relative);
            var dest=PathIn(p.root,f.relative);
            if(File.Exists(dest) && Hash(dest)==f.sha256)continue;
            if(p.before.TryGetValue(f.relative,out var expected))
            {if(!File.Exists(dest)||Hash(dest)!=expected)throw new InvalidDataException("Author code changed: "+f.relative);}
            else if(File.Exists(dest))throw new IOException("New code destination already exists: "+f.relative);
        }
        AssetDatabase.StartAssetEditing();
        try{foreach(var f in p.code){var dest=PathIn(p.root,f.relative);Directory.CreateDirectory(Path.GetDirectoryName(dest));File.Copy(PathIn(p.candidate,f.relative),dest,true);}}
        finally{AssetDatabase.StopAssetEditing();}
        foreach(var f in p.code.Where(f=>!f.relative.EndsWith(".meta",StringComparison.Ordinal)))
            AssetDatabase.ImportAsset(f.relative,ImportAssetOptions.ForceUpdate);
        UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();return "Sealed moving-turn code copied; wait for successful compilation before private binding installation.";
    }
    public static string Assets(string planPath)
    {
        var p=Read(planPath);foreach(var f in p.code)if(Hash(PathIn(p.root,f.relative))!=f.sha256)throw new InvalidDataException("Code differs from tested candidate.");
        foreach(var f in p.privateAssets)
        {
            if(Hash(PathIn(p.candidate,f.relative))!=f.sha256)throw new InvalidDataException("Private candidate changed.");
            CheckGuid(PathIn(p.candidate,f.relative),f.relative);
            var dest=PathIn(p.root,f.relative);if(File.Exists(dest)&&Hash(dest)!=f.sha256)throw new IOException("Private destination already exists.");
        }
        var binding=PathIn(p.root,p.binding);var previous=File.ReadAllBytes(binding);
        if(Hash(binding)==p.bindingAfter)return "Moving-turn assets already bound.";
        if(Hash(binding)!=p.bindingBefore || Hash(p.bindingResult)!=p.bindingAfter)throw new InvalidDataException("Private binding changed.");
        var loaded=AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(p.binding);
        if(loaded==null || EditorUtility.IsDirty(loaded))throw new InvalidOperationException("Private binding has unsaved author edits.");
        foreach(var f in p.privateAssets){var dest=PathIn(p.root,f.relative);Directory.CreateDirectory(Path.GetDirectoryName(dest));File.Copy(PathIn(p.candidate,f.relative),dest,true);}
        try
        {
            foreach(var f in p.privateAssets.Where(f=>!f.relative.EndsWith(".meta",StringComparison.Ordinal)))AssetDatabase.ImportAsset(f.relative,ImportAssetOptions.ForceSynchronousImport);
            var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("BooterBigArm.TopDown3D.AnimationPlaygroundMovingTurnProfile")).FirstOrDefault(t=>t!=null);
            if(type==null)throw new InvalidOperationException("Moving-turn code is not loaded.");
            var profile=AssetDatabase.LoadAssetAtPath("Assets/_Project/Settings/AnimationPlayground/PrivateBindings/MovingTurnReview.asset",type);
            var validation=new object[]{loaded.forwardReviewOverride,null};
            if(profile==null || !(bool)type.GetMethod("Validate").Invoke(profile,validation))throw new InvalidDataException("Imported moving turns invalid: "+validation[1]);
            File.Copy(p.bindingResult,binding,true);AssetDatabase.ImportAsset(p.binding,ImportAssetOptions.ForceSynchronousImport);
            loaded=AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(p.binding);
            string reason=null;
            if(!(bool)loaded.GetType().GetField("movingTurnsRequired").GetValue(loaded) || (UnityEngine.Object)loaded.GetType().GetField("movingTurns").GetValue(loaded)!=profile || !loaded.ValidatePlayer(out reason))throw new InvalidDataException("Moving-turn binding failed: "+reason);
            foreach(var f in p.privateAssets)if(Hash(PathIn(p.root,f.relative))!=f.sha256)throw new InvalidDataException("Private import changed sealed bytes.");
            if(Hash(binding)!=p.bindingAfter)throw new InvalidDataException("Binding serialization changed.");
            return "Five paired moving-turn bands installed in the existing main Camp Loop; accepted forward assets and scene untouched.";
        }
        catch
        {
            File.WriteAllBytes(binding,previous);AssetDatabase.ImportAsset(p.binding,ImportAssetOptions.ForceSynchronousImport);throw;
        }
    }
}
