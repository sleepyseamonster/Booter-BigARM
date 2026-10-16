using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using BooterBigArm.TopDown3D;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class CampDirectionalProfilePreparation
{
    public const string DirectionalPath="Assets/_Project/Settings/AnimationPlayground/PrivateBindings/StandingDirectionalReview.asset";
    public const string NativePath="Assets/_Project/Settings/AnimationPlayground/PrivateBindings/CampNativeMotionReview.asset";
    static void Guard()
    {
        var root=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
        if(!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || Directory.Exists(Path.Combine(root,".git"))
            || !File.Exists(Path.Combine(root,".animation-playground-validation-project")))throw new InvalidOperationException("Require the marked offline candidate.");
    }
    public static string Create(string proposal)
    {
        Guard();if(File.Exists(DirectionalPath) || File.Exists(NativePath))throw new IOException("Preserve existing private selections.");
        var b=AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>("Assets/_Project/Settings/AnimationPlayground/PrivateBindings/PlayerBindings.asset");
        var rows=(JArray)JObject.Parse(File.ReadAllText(proposal))["gaits"];
        var p=ScriptableObject.CreateInstance<AnimationPlaygroundStandingStrafeProfile>();p.schema=2;p.maximumSpeed=6;
        p.targetModel=b.targetModel;p.footProjection=b.footProjection;p.maximumLateralSpeed=5;p.maximumBackwardSpeed=4;
        p.directionalGaits=rows.Select(row=>{
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>((string)row["source"]);
            var keys=((JArray)row["correction"]).Select(k=>new Keyframe((float)k[0],(float)k[1])).ToArray();
            var height=new AnimationCurve(keys);
            for(var i=0;i<keys.Length;i++){AnimationUtility.SetKeyLeftTangentMode(height,i,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(height,i,AnimationUtility.TangentMode.Linear);}
            return new AnimationPlaygroundForwardGait{sourceClip=clip,nominalSpeed=(float)row["nominalSpeed"],metresPerCycle=(float)row["metresPerCycle"],
                nativeContacts=row["nativeContacts"].Select(t=>(float)t).ToArray(),signedHeight=height,
                phaseMap=row["sourceTimeMap"].Select(t=>new AnimationPlaygroundNativePhase((float)t[0],(float)t[1])).ToArray()};
        }).ToArray();
        if(!p.Validate(b.targetModel,b.footProjection,out var reason))throw new InvalidDataException(reason);
        AssetDatabase.CreateAsset(p,DirectionalPath);AssetDatabase.SaveAssetIfDirty(p);
        var native=UnityEngine.Object.Instantiate(b.continuationReview);native.name="CampNativeMotionReview";native.schema=2;native.maximumSlope=40;
        native.nativeBodyProjection=b.lowMotion.bodyProjection;
        if(!native.Validate(b.targetModel,b.footProjection,out reason))throw new InvalidDataException(reason);
        AssetDatabase.CreateAsset(native,NativePath);AssetDatabase.SaveAssetIfDirty(native);
        return "Created separate standing-directional and native-camp profiles. Accepted forward assets unchanged.";
    }
    public static string PairNativeBodyProjection()
    {
        Guard();var native=AssetDatabase.LoadAssetAtPath<AnimationPlaygroundContinuationProfile>(NativePath);
        var b=AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>("Assets/_Project/Settings/AnimationPlayground/PrivateBindings/PlayerBindings.asset");
        if(native==null || !native.NativeCampGround || native.nativeBodyProjection!=null)throw new InvalidOperationException("Pair only the new task-owned native candidate once.");
        native.nativeBodyProjection=b.lowMotion.bodyProjection;
        if(!native.Validate(b.targetModel,b.footProjection,out var reason))throw new InvalidDataException(reason);
        EditorUtility.SetDirty(native);AssetDatabase.SaveAssetIfDirty(native);return "Paired existing whole-body projection; native profile GUID retained.";
    }
    public static string FitBackwardWalk(string proposal)
    {
        Guard();var profile=AssetDatabase.LoadAssetAtPath<AnimationPlaygroundStandingStrafeProfile>(DirectionalPath);
        if(profile==null || !profile.AllDirections)throw new InvalidOperationException("Retain the new task-owned directional candidate.");
        var row=JObject.Parse(File.ReadAllText(proposal))["gaits"][4];var gait=profile.directionalGaits[4];
        if((string)row["source"]!=AssetDatabase.GetAssetPath(gait.sourceClip) || Math.Abs((float)row["metresPerCycle"]-gait.metresPerCycle)>.000001f)
            throw new InvalidDataException("Retain the selected native backward source and measured stride.");
        gait.phaseMap=row["sourceTimeMap"].Select(t=>new AnimationPlaygroundNativePhase((float)t[0],(float)t[1])).ToArray();
        if(!profile.Validate(profile.targetModel,profile.footProjection,out var reason))throw new InvalidDataException(reason);
        EditorUtility.SetDirty(profile);AssetDatabase.SaveAssetIfDirty(profile);return "Fitted the existing backward-walk stance map; source, stride, speed and GUID retained.";
    }
    public static string Inspect(string recipesPath,string output)
    {
        Guard();if(File.Exists(output))throw new IOException("Preserve the previous input receipt.");
        const string binding="Assets/_Project/Settings/AnimationPlayground/PrivateBindings/PlayerBindings.asset";
        var b=AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(binding);
        if(!b.ValidatePlayer(out var reason) || !b.standingStrafeReview.AllDirections || !b.continuationReview.NativeCampGround)throw new InvalidDataException(reason??"Select the paired directional/native family.");
        var recipes=JArray.Parse(File.ReadAllText(recipesPath));
        var roots=new[]{DirectionalPath,NativePath,AssetDatabase.GetAssetPath(b.forwardReviewOverride),AssetDatabase.GetAssetPath(b.airborne),
            AssetDatabase.GetAssetPath(b.lowMotion),AssetDatabase.GetAssetPath(TopDown3DCampSettings.Load())};
        var paths=AssetDatabase.GetDependencies(roots,true).Where(p=>p.StartsWith("Assets/",StringComparison.Ordinal)&&File.Exists(p))
            .Concat(recipes.Select(r=>(string)r["source"])).Concat(new[]{binding})
            .SelectMany(p=>File.Exists(p+".meta") ? new[]{p,p+".meta"} : new[]{p})
            .Concat(recipes.Select(r=>Path.GetDirectoryName((string)r["source"]).Replace('\\','/')+".meta"))
            .Distinct().OrderBy(p=>p).ToArray();
        string Hash(string p){using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(p))).Replace("-","").ToLowerInvariant();}
        var assemblies=new[]{typeof(TopDown3DPlayerAnimationDriver).Assembly,AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetType("BooterBigArm.Tests.AnimationPlaygroundDirectionalTests")!=null)};
        File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(new{utc=DateTime.UtcNow,selectedDirectional=AssetDatabase.GetAssetPath(b.standingStrafeReview),
            selectedNative=AssetDatabase.GetAssetPath(b.continuationReview),forwardSpeeds=b.forwardReviewOverride.gaits.Select(g=>g.nominalSpeed).ToArray(),
            b.forwardReviewOverride.qualified,b.forwardReviewOverride.terrainQualified,b.standingStrafeReview.maximumLateralSpeed,b.standingStrafeReview.maximumBackwardSpeed,
            soleVertices=b.footProjection.vertices.Length,bodyVertices=b.continuationReview.nativeBodyProjection.vertices.Length,
            assemblies=assemblies.Select(a=>new{name=a.GetName().Name,module=a.ManifestModule.ModuleVersionId,sha256=Hash(a.Location)}).ToArray(),
            inputs=paths.Select(p=>new{path=p,sha256=Hash(p)}).ToArray()},Newtonsoft.Json.Formatting.Indented));return output;
    }
    public static string Select()
    {
        Guard();var path="Assets/_Project/Settings/AnimationPlayground/PrivateBindings/PlayerBindings.asset";
        var b=AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(path);
        if(b.standingStrafeReview==null || b.standingStrafeReview.schema!=1 || b.continuationReview==null || b.continuationReview.schema!=1)
            throw new InvalidDataException("Select only from the retained prior review baseline.");
        b.standingStrafeReview=AssetDatabase.LoadAssetAtPath<AnimationPlaygroundStandingStrafeProfile>(DirectionalPath);
        b.continuationReview=AssetDatabase.LoadAssetAtPath<AnimationPlaygroundContinuationProfile>(NativePath);
        if(!b.ValidatePlayer(out var reason))throw new InvalidDataException(reason);
        EditorUtility.SetDirty(b);AssetDatabase.SaveAssetIfDirty(b);return "Selected the paired directional/native-ground family in the candidate only.";
    }
}
