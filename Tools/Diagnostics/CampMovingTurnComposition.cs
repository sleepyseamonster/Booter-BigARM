using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Object=UnityEngine.Object;

// Offline source clips provide phase-aligned torso/arms for upper-bone tape extraction.
// Their retargeted lower body is authoring context, not a runtime-qualified gait.
public static class CampMovingTurnComposition
{
    public static string BakeFinal(string calibration,string receipt)
    {
        if(!Application.isBatchMode || Directory.Exists(".git") || !File.Exists(".animation-playground-validation-project")
            || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || File.Exists(receipt))throw new InvalidOperationException("Fresh marked offline composition required.");
        var profile=AssetDatabase.LoadAssetAtPath<AnimationPlaygroundMovingTurnProfile>("Assets/_Project/Settings/AnimationPlayground/PrivateBindings/MovingTurnReview.asset");var policy=profile.forwardProfile;
        string reason=null;
        if(policy==null || !policy.Validate(policy.targetModel,policy.locomotion,out reason,false))throw new InvalidDataException(reason);
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("BooterBigArm.Tests.AnimationPlaygroundForwardReviewTests+Fixture")).First(t=>t!=null);
        using var fixture=(IDisposable)Activator.CreateInstance(type,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,new object[]{true,SkinWeights.FourBones,AssetDatabase.GetAssetPath(policy)},null);
        var driver=(TopDown3DPlayerAnimationDriver)type.GetField("Driver").GetValue(fixture);var graph=(PlayableGraph)type.GetField("Graph").GetValue(fixture);var output=(AnimationPlayableOutput)graph.GetOutput(0);var originalOutput=output.GetSourcePlayable();
        var animator=driver.HumanoidAnimator;using var handler=new HumanPoseHandler(animator.avatar,animator.transform);
        var names=HumanTrait.MuscleName;var muscle=names.Select((name,index)=>(name,index)).ToDictionary(x=>x.name,x=>x.index);
        var legBones=new[]{HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftFoot,HumanBodyBones.LeftToes,HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.RightFoot,HumanBodyBones.RightToes,HumanBodyBones.Hips};
        var upper=Enumerable.Range(0,names.Length).Select(i=>!legBones.Contains((HumanBodyBones)HumanTrait.BoneFromMuscle(i))).ToArray();
        var records=new List<object>();var sourceRows=Newtonsoft.Json.Linq.JArray.Parse(File.ReadAllText(calibration));var bandNames=new[]{"Walk","Jog","Run","FastRun","Sprint"};
        for(var band=0;band<5;band++)foreach(var right in new[]{false,true})
        {
            var accepted=policy.gaits[band];var rawName="MovingTurn"+bandNames[band]+(right?"Right":"Left");var rawPath=AnimationPlaygroundClipDerivatives.Folder+"/"+rawName+".anim";
            var row=sourceRows.Single(r=>(string)r["name"]==rawName);var raw=new AnimationPlaygroundForwardGait{sourceClip=AssetDatabase.LoadAssetAtPath<AnimationClip>(rawPath),phaseMap=((Newtonsoft.Json.Linq.JArray)row["sourceTimeMap"]).Select(k=>new AnimationPlaygroundNativePhase((float)k[0],(float)k[1])).ToArray()};var acceptedSettings=AnimationUtility.GetAnimationClipSettings(accepted.sourceClip);
            var name=raw.sourceClip.name+"UpperBodySource";var path=AnimationPlaygroundClipDerivatives.Folder+"/"+name+".anim";
            if(File.Exists(path) || File.Exists(path+".meta"))throw new IOException("Preserve existing compositions.");
            var clip=Object.Instantiate(accepted.sourceClip);clip.name=name;clip.hideFlags=HideFlags.None;
            var length=accepted.metresPerCycle/accepted.nominalSpeed;var count=Mathf.CeilToInt(length*240);var source=AnimationClipPlayable.Create(graph,raw.sourceClip);source.SetSpeed(0);source.SetApplyFootIK(false);source.SetApplyPlayableIK(false);output.SetSourcePlayable(source);
            var samples=new HumanPose[count+1];var turns=new float[count+1];
            for(var frame=0;frame<=count;frame++)
            {
                var cycle=frame==count?0:frame/(double)count;source.SetTime(raw.SourceSeconds(cycle));graph.Evaluate(0);
                var pose=new HumanPose();handler.GetHumanPose(ref pose);samples[frame]=pose;
                var up=pose.bodyRotation*Vector3.up;turns[frame]=Mathf.Atan2(up.x,up.y)*Mathf.Rad2Deg;
            }
            var bindings=AnimationUtility.GetCurveBindings(accepted.sourceClip);var heightKeys=new Keyframe[count+1];
            foreach(var binding in bindings)
            {
                var old=AnimationUtility.GetEditorCurve(accepted.sourceClip,binding);var keys=new Keyframe[count+1];var isUpper=muscle.TryGetValue(binding.propertyName,out var index)&&upper[index];
                for(var frame=0;frame<=count;frame++)
                {
                    var cycle=frame==count?0:frame/(double)count;var native=Mathf.Repeat(accepted.SourceSeconds(cycle)/accepted.sourceClip.length+acceptedSettings.cycleOffset,1);
                    var baseline=old.Evaluate(native*accepted.sourceClip.length);if(acceptedSettings.loopBlend)baseline-=(old.Evaluate(accepted.sourceClip.length)-old.Evaluate(0))*native;
                    var value=isUpper?samples[frame].muscles[index]:baseline;
                    // Root lean is carried into the torso, retaining the accepted
                    // lower-body/root pose. Values stay within Humanoid limits.
                    if(binding.propertyName=="Spine Left-Right" || binding.propertyName=="Chest Left-Right")
                    {
                        var share=binding.propertyName=="Spine Left-Right"?.65f:.35f;
                        var limit=Mathf.Max(1,HumanTrait.GetMuscleDefaultMax(index));
                        value=Mathf.Clamp(value+turns[frame]*share/limit,-1,1);
                    }
                    keys[frame]=new Keyframe(length*frame/count,value);
                }
                var curve=new AnimationCurve(keys);for(var k=0;k<keys.Length;k++){AnimationUtility.SetKeyLeftTangentMode(curve,k,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(curve,k,AnimationUtility.TangentMode.Linear);}AnimationUtility.SetEditorCurve(clip,binding,curve);
            }
            for(var frame=0;frame<=count;frame++)heightKeys[frame]=new Keyframe(frame/(float)count,accepted.Height(frame==count?0:frame/(double)count));
            var height=new AnimationCurve(heightKeys);for(var k=0;k<heightKeys.Length;k++){AnimationUtility.SetKeyLeftTangentMode(height,k,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(height,k,AnimationUtility.TangentMode.Linear);}
            var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.startTime=0;settings.stopTime=length;settings.loopTime=true;settings.loopBlend=false;settings.mirror=false;settings.cycleOffset=0;AnimationUtility.SetAnimationClipSettings(clip,settings);clip.frameRate=240;
            AssetDatabase.CreateAsset(clip,path);AssetDatabase.SaveAssetIfDirty(clip);
            var gait=new AnimationPlaygroundForwardGait{role=accepted.role,nominalSpeed=accepted.nominalSpeed,metresPerCycle=accepted.metresPerCycle,sourceClip=clip,
                pureMinimumRate=.65f,pureMaximumRate=1.35f,blendMinimumRate=.45f,blendMaximumRate=1.65f,nativeContacts=new[]{0f,.25f,.5f,.75f},
                phaseMap=new[]{new AnimationPlaygroundNativePhase(0,0),new(.25f,.25f),new(.5f,.5f),new(.75f,.75f),new(1,1)},signedHeight=height};
            if(right){profile.bands[band].right=gait;profile.bands[band].rightPose=null;}else{profile.bands[band].left=gait;profile.bands[band].leftPose=null;}
            records.Add(new{band,right,path,rawSource=rawPath,acceptedSource=AssetDatabase.GetAssetPath(accepted.sourceClip),length,samples=count+1,minimumRootLean=turns.Min(),maximumRootLean=turns.Max(),upperMuscles=upper.Count(u=>u),lowerBody="Accepted root and leg curves resampled at 240Hz as authoring context only; retargeted full-body playback is not qualified. Runtime selects upper-bone tapes only."});
            output.SetSourcePlayable(originalOutput);graph.DestroyPlayable(source);
        }
        // The unbound candidate remains incomplete until paired upper-bone tapes are extracted.
        profile.name="MovingTurnReview";
        EditorUtility.SetDirty(profile);AssetDatabase.SaveAssetIfDirty(profile);
        File.WriteAllText(receipt,Newtonsoft.Json.JsonConvert.SerializeObject(records,Newtonsoft.Json.Formatting.Indented));return receipt;
    }
}
