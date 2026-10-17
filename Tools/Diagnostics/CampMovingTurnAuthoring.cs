using System;
using System.IO;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json.Linq;

public static class CampMovingTurnAuthoring
{
    const string Private="Assets/_Project/Settings/AnimationPlayground/PrivateBindings/";
    public static string Prepare(string calibration)=>PrepareCore(calibration,false);
    public static string Refit(string calibration)=>PrepareCore(calibration,true);
    static string PrepareCore(string calibration,bool replace)
    {
        if(!Application.isBatchMode || Directory.Exists(".git") || !File.Exists(".animation-playground-validation-project")
            || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)throw new InvalidOperationException("Stable marked candidate required.");
        var path=Private+"MovingTurnReview.asset";if(File.Exists(path) && !replace)throw new IOException("Preserve the existing profile.");
        if(replace){var existing=AssetDatabase.LoadAssetAtPath<AnimationPlaygroundMovingTurnProfile>(path);if(existing!=null && existing.bands.Any(b=>b.leftPose!=null && b.leftPose.samples>0))throw new IOException("Preserve completed torso tapes.");}
        var policy=AssetDatabase.LoadAssetAtPath<AnimationPlaygroundForwardLocomotionProfile>(Private+"ExpandedForwardGaitReview.asset");
        var source=JArray.Parse(File.ReadAllText(calibration));var profile=ScriptableObject.CreateInstance<AnimationPlaygroundMovingTurnProfile>();
        profile.name="MovingTurnReview";profile.forwardProfile=policy;profile.bands=new AnimationPlaygroundMovingTurnBand[5];
        var names=new[]{"Walk","Jog","Run","FastRun","Sprint"};
        for(var i=0;i<5;i++)
        {
            AnimationPlaygroundForwardGait Gait(string side)
            {
                var row=source.Single(t=>(string)t["name"]=="MovingTurn"+names[i]+side);var original=policy.gaits[i];
                var keys=((JArray)row["correction"]).Select(k=>new Keyframe((float)k[0],(float)k[1])).ToArray();var curve=new AnimationCurve(keys);
                for(var k=0;k<keys.Length;k++){AnimationUtility.SetKeyLeftTangentMode(curve,k,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(curve,k,AnimationUtility.TangentMode.Linear);}
                return new AnimationPlaygroundForwardGait{role=original.role,nominalSpeed=original.nominalSpeed,metresPerCycle=original.metresPerCycle,
                    sourceClip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Project/Art/Animations/Mixamo/Derived/MovingTurn"+names[i]+side+".anim"),
                    pureMinimumRate=.65f,pureMaximumRate=1.35f,blendMinimumRate=.45f,blendMaximumRate=1.65f,
                    nativeContacts=row["nativeContacts"].ToObject<float[]>(),phaseMap=((JArray)row["sourceTimeMap"]).Select(k=>new AnimationPlaygroundNativePhase((float)k[0],(float)k[1])).ToArray(),signedHeight=curve};
            }
            var curvature=source.Where(t=>(string)t["band"]==names[i]).Average(t=>(float)t["sourceCurvatureDegreesPerMetre"]);
            profile.bands[i]=new AnimationPlaygroundMovingTurnBand{left=Gait("Left"),right=Gait("Right"),sourceCurvatureDegreesPerMetre=curvature,maximumWeight=i==0?.45f:.4f,
                title=i==0?"Walking Left Turn":i<3?"Running Right Turn":"Sprint Turn",
                provenance="Mixamo 2026-10-16; FBX Unity, without skin, 60fps, no reduction. Walking opposite/right and other opposite/left deliberately mirrored. Jog/run share neutral running source; fast run/sprint share sprint source. Travel/yaw extracted, four measured contacts, accepted distance clock unchanged."};
        }
        // Intermediate source map only: runtime validation follows upper-bone tape extraction.
        string reason=null;
        if(policy==null || !policy.Validate(policy.targetModel,policy.locomotion,out reason,false)){UnityEngine.Object.DestroyImmediate(profile);throw new InvalidDataException(reason);}
        if(replace){var target=AssetDatabase.LoadAssetAtPath<AnimationPlaygroundMovingTurnProfile>(path);if(target==null)throw new IOException("Refit requires the existing candidate profile.");EditorUtility.CopySerialized(profile,target);EditorUtility.SetDirty(target);AssetDatabase.SaveAssetIfDirty(target);UnityEngine.Object.DestroyImmediate(profile);}
        else {AssetDatabase.CreateAsset(profile,path);AssetDatabase.SaveAssetIfDirty(profile);}return path;
    }
}
