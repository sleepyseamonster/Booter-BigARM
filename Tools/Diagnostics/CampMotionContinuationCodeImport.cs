using System;
using System.IO;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CampMotionContinuationCodeImport
{
    public static string Import(string planPath)
    {
        var plan=JObject.Parse(File.ReadAllText(planPath));var root=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
        var scene=SceneManager.GetActiveScene();
        if(Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
            || !Directory.Exists(Path.Combine(root,".git")) || Path.GetFullPath((string)plan["author"])!=root
            || scene.path!="Assets/_Project/Scenes/Reference/CampLoopTesting.unity" || scene.isDirty!=(bool)plan["sceneDirty"])
            throw new InvalidOperationException("Require the existing author scene and unchanged dirty state.");
        foreach(var item in plan["runtime"])
        {
            var relative=(string)item["path"];var path=Path.GetFullPath(Path.Combine(root,relative));
            if(!path.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Require task paths inside the checkout.");
            using var sha=SHA256.Create();
            var hash=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();
            if(hash!=(string)item["sha256"])throw new InvalidDataException("Task source changed: "+relative);
        }
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach(var item in plan["runtime"])
            {var path=(string)item["path"];if(path.EndsWith(".cs",StringComparison.Ordinal))AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);}
        }
        finally{AssetDatabase.StopAssetEditing();}
        UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
        return "Requested only the sealed task script imports and compilation; no scene save/reload or foreground operation.";
    }
}
