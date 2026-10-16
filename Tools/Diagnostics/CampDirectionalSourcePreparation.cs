using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Object=UnityEngine.Object;

// Source imports and equipped retargeting only. Never runs gameplay or saves a scene.
public static class CampDirectionalSourcePreparation
{
    [Serializable] public sealed class Recipe { public string source,name; public bool mirror; }
    [Serializable] public sealed class Plan { public Recipe[] sources; public string folder,receipt; }
    [Serializable] public sealed class LoopRecipe { public string source,name; public float originPhase; }
    public static string PrepareLoops(string planPath)
    {
        Guard();var recipes=Newtonsoft.Json.JsonConvert.DeserializeObject<LoopRecipe[]>(File.ReadAllText(planPath));
        if(recipes==null || recipes.Length!=6)throw new InvalidDataException("Require the six measured directional loops.");
        foreach(var recipe in recipes)
        {
            if(!recipe.name.StartsWith("StandingDirectional",StringComparison.Ordinal))throw new InvalidDataException("Use a distinct directional derivative.");
            var clip=AnimationPlaygroundClipDerivatives.CreateLoop(recipe.source,recipe.name,recipe.originPhase,2);
            foreach(var binding in AnimationUtility.GetCurveBindings(clip))if(binding.propertyName=="RootT.x" || binding.propertyName=="RootT.z")
                AnimationUtility.SetEditorCurve(clip,binding,AnimationCurve.Constant(0,clip.length,0));
            EditorUtility.SetDirty(clip);AssetDatabase.SaveAssetIfDirty(clip);
        }
        return "Six complete two-cycle derivatives authored; horizontal travel remains motor-owned.";
    }
    static string Root => Path.GetFullPath(Path.Combine(Application.dataPath,".."));
    static void Guard()
    {
        if(!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
            || Directory.Exists(Path.Combine(Root,".git")) || !File.Exists(Path.Combine(Root,".animation-playground-validation-project")))
            throw new InvalidOperationException("Require the marked compiled offline candidate.");
    }
    static string Hash(string p) {using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(p))).Replace("-","").ToLowerInvariant();}
    public static string Import(string planPath)
    {
        Guard();var plan=Newtonsoft.Json.JsonConvert.DeserializeObject<Plan>(File.ReadAllText(planPath));
        if(plan?.sources==null || plan.sources.Length==0)throw new InvalidDataException("The import plan requires explicit sources.");
        if(!plan.folder.StartsWith("Assets/_Project/Art/Animations/Mixamo/Qualification/Directional",StringComparison.Ordinal)
            || Directory.Exists(Path.Combine(Root,plan.folder)) || File.Exists(plan.receipt))throw new IOException("Use fresh bounded destinations.");
        var bindings=AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>("Assets/_Project/Settings/AnimationPlayground/PrivateBindings/PlayerBindings.asset");
        Directory.CreateDirectory(Path.Combine(Root,plan.folder));AssetDatabase.Refresh();var records=new List<object>();
        foreach(var recipe in plan.sources)
        foreach(var policy in new[]{"OriginalBaked","BodyBaked","OriginalExtracted"})
        {
            if(recipe.name!=Path.GetFileName(recipe.name) || !recipe.name.All(c=>char.IsLetterOrDigit(c)))throw new InvalidDataException("Use a simple source name.");
            var source=Path.IsPathRooted(recipe.source) ? recipe.source : Path.Combine(Root,recipe.source);
            var before=Hash(source);var dest=plan.folder+"/"+recipe.name+policy+".fbx";
            File.Copy(source,Path.Combine(Root,dest));AssetDatabase.ImportAsset(dest,ImportAssetOptions.ForceSynchronousImport);
            var importer=(ModelImporter)AssetImporter.GetAtPath(dest);
            importer.animationType=ModelImporterAnimationType.Human;importer.avatarSetup=ModelImporterAvatarSetup.CopyFromOther;
            importer.sourceAvatar=bindings.sourceAvatar;importer.importAnimation=true;importer.materialImportMode=ModelImporterMaterialImportMode.None;
            importer.animationCompression=ModelImporterAnimationCompression.Off;importer.optimizeGameObjects=false;
            var defaults=importer.defaultClipAnimations;if(defaults.Length!=1)throw new InvalidDataException("Require one complete native take.");
            var clip=defaults[0];clip.name=recipe.name+policy;clip.mirror=recipe.mirror;clip.loop=false;clip.loopTime=false;clip.loopPose=false;clip.cycleOffset=0;
            clip.lockRootHeightY=true;clip.keepOriginalPositionY=true;clip.heightFromFeet=false;
            clip.lockRootRotation=policy!="OriginalExtracted";clip.keepOriginalOrientation=policy!="BodyBaked";
            clip.lockRootPositionXZ=false;clip.keepOriginalPositionXZ=false;importer.clipAnimations=new[]{clip};importer.SaveAndReimport();
            var imported=AssetDatabase.LoadAllAssetsAtPath(dest).OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__"));
            if(!imported.isHumanMotion || Hash(source)!=before)throw new InvalidDataException("Source pairing changed.");
            records.Add(new{source=recipe.source,sourceSha256=before,path=dest,sha256=Hash(Path.Combine(Root,dest)),metaSha256=Hash(Path.Combine(Root,dest+".meta")),policy,recipe.mirror,
                imported.length,imported.frameRate,clip.firstFrame,clip.lastFrame});
        }
        File.WriteAllText(plan.receipt,Newtonsoft.Json.JsonConvert.SerializeObject(records,Newtonsoft.Json.Formatting.Indented));return plan.receipt;
    }
    public static string Export(string folder,string destination)
    {
        Guard();if(Directory.Exists(destination) || folder!="StandingDirectionalReview" && !folder.StartsWith("Assets/_Project/Art/Animations/Mixamo/Qualification/Directional",StringComparison.Ordinal))throw new IOException("Fresh source tape destination required.");
        Directory.CreateDirectory(destination);
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("BooterBigArm.Tests.AnimationPlaygroundForwardReviewTests+Fixture")).First(t=>t!=null);
        using var f=(IDisposable)Activator.CreateInstance(type,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,
            new object[]{true,SkinWeights.FourBones,"Assets/_Project/Settings/AnimationPlayground/PrivateBindings/ExpandedForwardGaitReview.asset"},null);
        T Field<T>(string n)=>(T)type.GetField(n).GetValue(f);
        var driver=Field<TopDown3DPlayerAnimationDriver>("Driver");var graph=Field<PlayableGraph>("Graph");var output=(AnimationPlayableOutput)graph.GetOutput(0);var original=output.GetSourcePlayable();
        const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
        var visual=(GameObject)typeof(TopDown3DPlayerAnimationDriver).GetField("visualInstance",flags).GetValue(driver);
        var origin=(Vector3)typeof(TopDown3DPlayerAnimationDriver).GetField("visualLocalPosition",flags).GetValue(driver);
        var projection=Field<AnimationPlaygroundForwardLocomotionProfile>("Policy").footProjection;
        var bones=projection.bonePaths.Select(p=>p.Length==0 ? visual.transform : visual.transform.Find(p)).ToArray();
        Vector3 Point(AnimationPlaygroundFootVertex v) {var p=Vector3.zero;foreach(var b in v.points)p+=bones[b.bone].TransformPoint(b.position)*b.weight;return driver.transform.InverseTransformPoint(p);}
        float[] Coordinates(Vector3 p)=>new[]{p.x,p.y,p.z};
        var paths=folder=="StandingDirectionalReview" ? Directory.GetFiles(Path.Combine(Root,AnimationPlaygroundClipDerivatives.Folder),"StandingDirectional*.anim") : Directory.GetFiles(Path.Combine(Root,folder),"*.fbx");
        foreach(var path in paths.Select(p=>p.Substring(Root.Length+1).Replace('\\','/')).OrderBy(p=>p))
        {
            var source=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__"));var clip=Object.Instantiate(source);
            try
            {
                var rootCurves=AnimationUtility.GetCurveBindings(clip).Where(b=>b.propertyName.StartsWith("Root")).Select(b=>new{b.propertyName,first=AnimationUtility.GetEditorCurve(clip,b).Evaluate(0),last=AnimationUtility.GetEditorCurve(clip,b).Evaluate(clip.length)}).ToArray();
                foreach(var b in AnimationUtility.GetCurveBindings(clip))if(b.propertyName=="RootT.x" || b.propertyName=="RootT.z")AnimationUtility.SetEditorCurve(clip,b,AnimationCurve.Constant(0,clip.length,0));
                var playable=AnimationClipPlayable.Create(graph,clip);playable.SetApplyFootIK(false);playable.SetApplyPlayableIK(false);playable.SetSpeed(0);output.SetSourcePlayable(playable);
                var samples=new List<object>();var count=Mathf.CeilToInt(clip.length*120);
                for(var frame=0;frame<=count;frame++)
                {
                    visual.transform.localPosition=origin;playable.SetTime(frame*(double)clip.length/count);graph.Evaluate(0);
                    samples.Add(new{time=frame*(double)clip.length/count,points=projection.vertices.Select(v=>Coordinates(Point(v))).ToArray()});
                }
                var data=new{path,sha256=Hash(Path.Combine(Root,path)),metaSha256=Hash(Path.Combine(Root,path+".meta")),source.length,source.isLooping,source.isHumanMotion,rootCurves,
                    vertices=projection.vertices.Select(v=>v.points.Select(p=>new{bone=projection.bonePaths[p.bone],p.weight}).ToArray()).ToArray(),samples};
                File.WriteAllText(Path.Combine(destination,Path.GetFileNameWithoutExtension(path)+".json"),Newtonsoft.Json.JsonConvert.SerializeObject(data));output.SetSourcePlayable(original);graph.DestroyPlayable(playable);
            }
            finally{Object.DestroyImmediate(clip);}
        }
        return destination;
    }
}
