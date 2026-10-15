using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using BooterBigArm.TopDown3D;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class CampMotionContinuationPreparation
{
    public const string ProfilePath="Assets/_Project/Settings/AnimationPlayground/PrivateBindings/CampMotionContinuationReview.asset";
    public static string SelectReview()
    {
        var root=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
        if(!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
            || Directory.Exists(Path.Combine(root,".git")) || !File.Exists(Path.Combine(root,".animation-playground-validation-project")))
            throw new InvalidOperationException("Select only in the marked candidate.");
        var bindings=AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(
            "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/PlayerBindings.asset");
        if(bindings.continuationReview!=null || bindings.continuationReviewRequired)throw new InvalidOperationException("Preserve the previous selection.");
        bindings.continuationReview=AssetDatabase.LoadAssetAtPath<AnimationPlaygroundContinuationProfile>(ProfilePath);
        bindings.continuationReviewRequired=true;
        if(!bindings.ValidatePlayer(out var reason))throw new InvalidDataException(reason);
        EditorUtility.SetDirty(bindings);AssetDatabase.SaveAssetIfDirty(bindings);
        return "Selected the candidate-only combined review; author binding untouched.";
    }
    public static string Inspect(string output)
    {
        var root=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
        if(!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
            || Directory.Exists(Path.Combine(root,".git")) || !File.Exists(Path.Combine(root,".animation-playground-validation-project"))
            || File.Exists(output))throw new InvalidOperationException("Use the compiled marked candidate and a fresh receipt.");
        var paths=AssetDatabase.GetDependencies(new[]{ProfilePath,
            "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/ExpandedForwardGaitReview.asset",
            "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/StandingStrafeReview.asset",
            "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/AirborneProfile.asset",
            AssetDatabase.GetAssetPath(TopDown3DCampSettings.Load())},true)
            .Where(p=>p.StartsWith("Assets/",StringComparison.Ordinal) && File.Exists(Path.Combine(root,p)))
            .SelectMany(p=>File.Exists(Path.Combine(root,p+".meta")) ? new[]{p,p+".meta"} : new[]{p}).Distinct().OrderBy(p=>p).ToArray();
        string Hash(string path){using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
        var profile=AssetDatabase.LoadAssetAtPath<AnimationPlaygroundContinuationProfile>(ProfilePath);
        if(!profile.Validate(profile.targetModel,profile.footProjection,out var reason))throw new InvalidDataException(reason);
        var assemblies=new[]{typeof(TopDown3DPlayerAnimationDriver).Assembly,
            AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetType("BooterBigArm.Tests.AnimationPlaygroundContinuationTests")!=null)};
        File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(new{
            utc=DateTime.UtcNow,profile.maximumSlope,profile.maximumAirSpeed,
            assemblies=assemblies.Select(a=>new{name=a.GetName().Name,module=a.ManifestModule.ModuleVersionId,sha256=Hash(a.Location)}).ToArray(),
            inputs=paths.Select(p=>new{path=p,sha256=Hash(Path.Combine(root,p))}).ToArray()
        },Newtonsoft.Json.Formatting.Indented)+"\n",new System.Text.UTF8Encoding(false));
        return output;
    }
    public static string Create()
    {
        var root=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
        if(!Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
            || Directory.Exists(Path.Combine(root,".git")) || !File.Exists(Path.Combine(root,".animation-playground-validation-project")))
            throw new InvalidOperationException("Use the marked compiled offline candidate.");
        if(File.Exists(Path.Combine(root,ProfilePath)))throw new IOException("Preserve the previous continuation profile.");
        var bindings=AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(
            "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/PlayerBindings.asset");
        var profile=ScriptableObject.CreateInstance<AnimationPlaygroundContinuationProfile>();
        profile.targetModel=bindings.targetModel;profile.footProjection=bindings.footProjection;
        profile.maximumSlope=10;
        string Hash(string path){using var sha=SHA256.Create();return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
        AnimationPlaygroundAdaptedTurn Source(string name)
        {
            var curve=JObject.Parse(File.ReadAllText(Path.Combine(root,"LocomotionImplementation20261014/TurnRootCurveV4/"+name+"-private-root.json")));
            var path=(string)curve["source"];
            var tapePath=Path.Combine(root,"LocomotionImplementation20261014/TurnNativeBasisV1/"+name+"-private-applied-poses.json");
            if(Hash(Path.Combine(root,path))!=(string)curve["sourceSha256"] || Hash(Path.Combine(root,path+".meta"))!=(string)curve["sourceMetaSha256"]
                || Hash(tapePath)!=(string)curve["nativeTapeSha256"] || !(bool)curve["metrics"]["boundedCurveEstablished"])
                throw new InvalidDataException("Turn recovery inputs no longer match their calibration.");
            var tape=JObject.Parse(File.ReadAllText(tapePath));var frames=(JArray)curve["frames"];var native=(JArray)tape["frames"];
            if(frames.Count!=native.Count)throw new InvalidDataException("Retain all source knots.");
            return new AnimationPlaygroundAdaptedTurn{
                clip=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Single(c=>c.name==name),
                soleVertex=(int)curve["vertex"],soleAnchor=new Vector2((float)curve["anchorXZ"][0],(float)curve["anchorXZ"][1]),
                knots=frames.Select((f,i)=>new AnimationPlaygroundAdaptedTurnKnot{
                    seconds=(float)f["seconds"],position=new Vector2((float)f["position"][0],(float)f["position"][1]),
                    velocity=new Vector2((float)f["velocity"][0],(float)f["velocity"][1]),
                    rotation=new Quaternion((float)native[i]["rootRotation"]["x"],(float)native[i]["rootRotation"]["y"],
                        (float)native[i]["rootRotation"]["z"],(float)native[i]["rootRotation"]["w"])}).ToArray()};
        }
        profile.left=Source("RapidTurningLeft");profile.right=Source("RapidTurningRight_Mirror");
        if(!profile.Validate(bindings.targetModel,bindings.footProjection,out var reason))throw new InvalidDataException(reason);
        AssetDatabase.CreateAsset(profile,ProfilePath);AssetDatabase.SaveAssetIfDirty(profile);
        return ProfilePath;
    }
}
