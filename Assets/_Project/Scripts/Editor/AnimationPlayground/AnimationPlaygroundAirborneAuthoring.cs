using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Editor
{
    public static class AnimationPlaygroundAirborneAuthoring
    {
        [Serializable] private sealed class Policy
        {
            public string source,path;
            public bool bakeHeight;
            public float seconds,hipMinimum,hipMaximum,soleMinimum,soleMaximum,bodyMinimum,bodyMaximum;
            public string[] rootCurves;
        }
        [Serializable] private sealed class Report {public string state="airborne-policy-review";public Policy[] policies;}
        [Serializable] private sealed class Gauge {public string source;public float curveDelta,hipDelta,worldUnitsPerCurveUnit;}
        [Serializable] private sealed class Gauges {public Gauge[] gauges;}
        public const string ProfilePath="Assets/_Project/Settings/AnimationPlayground/PrivateBindings/AirborneProfile.asset";
        [Serializable] private sealed class Recipe
        {public string name,path,source;public float start,end,minimumSole,minimumHip,maximumHip,maximumBody,maximumAdjustment;}
        [Serializable] private sealed class Draft {public string state="airborne-draft-not-physical-qualified";public Recipe[] clips;}
        public static void ClearAirSoleDraftFromCli()
        {
            if(!Application.isBatchMode || Directory.Exists(".git") || !File.Exists(AnimationPlaygroundLayoutAuthoring.CandidateMarker))
                throw new InvalidOperationException("Revise only the unpublished candidate draft.");
            var directory=Path.GetFullPath("AirborneQualification/AirSoles");Directory.CreateDirectory(directory);var exitCode=0;
            try
            {
                var bindings=AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(AnimationPlaygroundPlayerAuthoring.BindingPath);
                var profile=bindings.airborne;if(profile==null || profile.qualified)throw new InvalidDataException("Preserve a qualified profile.");
                var model=AssetDatabase.GetAssetPath(bindings.targetModel);var rows=new List<Recipe>();
                foreach(var variant in new[]{profile.standing,profile.running,profile.unarmed})
                {
                    var clip=variant.rising;
                    var measured=AnimationPlaygroundMotionSampler.Measure(model,AssetDatabase.GetAssetPath(clip),bindings.visualScale,bindings.visualOffset,60);
                    var shift=Mathf.Max(0,-.91f-measured.frames.Min(x=>Mathf.Min(x.leftSole.y,x.rightSole.y))+.002f);
                    var binding=AnimationUtility.GetCurveBindings(clip).Single(x=>x.propertyName=="RootT.y");
                    var curve=AnimationUtility.GetEditorCurve(clip,binding);var keys=curve.keys;
                    for(var i=0;i<keys.Length;i++)keys[i].value+=shift/.9894466f;
                    AnimationUtility.SetEditorCurve(clip,binding,new AnimationCurve(keys));EditorUtility.SetDirty(clip);AssetDatabase.SaveAssetIfDirty(clip);
                    var corrected=AnimationPlaygroundMotionSampler.Measure(model,AssetDatabase.GetAssetPath(clip),bindings.visualScale,bindings.visualOffset,60);
                    File.WriteAllText(Path.Combine(directory,clip.name+".json"),JsonUtility.ToJson(corrected,true)+"\n");
                    rows.Add(new Recipe{name=clip.name,path=AssetDatabase.GetAssetPath(clip),minimumSole=corrected.frames.Min(x=>Mathf.Min(x.leftSole.y,x.rightSole.y)),
                        minimumHip=corrected.frames.Min(x=>x.hips.y),maximumHip=corrected.frames.Max(x=>x.hips.y),
                        maximumBody=corrected.frames.Max(x=>x.bodyMaximum.y),maximumAdjustment=shift});
                }
                File.WriteAllText(Path.Combine(directory,"summary.json"),JsonUtility.ToJson(new Draft{clips=rows.ToArray()},true)+"\n");
            }
            catch(Exception error){File.WriteAllText(Path.Combine(directory,"failure.txt"),error.ToString());Debug.LogException(error);exitCode=1;}
            EditorApplication.Exit(exitCode);
        }
        public static void CreateDraftFromCli()
        {
            if(!Application.isBatchMode || Directory.Exists(".git") || !File.Exists(AnimationPlaygroundLayoutAuthoring.CandidateMarker))
                throw new InvalidOperationException("Author only in the marked candidate.");
            var directory=Path.GetFullPath("AirborneQualification/Draft");Directory.CreateDirectory(directory);var exitCode=0;
            try
            {
                if(File.Exists(ProfilePath))throw new IOException("Preserve an existing airborne profile and GUID.");
                var bindings=AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(AnimationPlaygroundPlayerAuthoring.BindingPath);
                var gauge=JsonUtility.FromJson<Gauges>(File.ReadAllText("AirborneQualification/Gauge/summary.json"));
                var model=AssetDatabase.GetAssetPath(bindings.targetModel);var idle=bindings.locomotion.GetRequiredEntry(TopDown3DLocomotionClipRole.Idle);
                var standing=AnimationPlaygroundMotionSampler.Measure(model,AssetDatabase.GetAssetPath(idle.Clip),bindings.visualScale,bindings.visualOffset,60,false,AnimationCurve.Linear(0,idle.HeightCorrection(0),1,idle.HeightCorrection(0)));
                var reference=standing.frames[0].hips.y;var rows=new List<Recipe>();var profile=ScriptableObject.CreateInstance<AnimationPlaygroundAirborneProfile>();
                AnimationClip Create(string sourceName,string suffix,float start,float end,bool neutralHip=false,bool loop=false)
                {
                    var source="Assets/_Project/Art/Animations/Mixamo/Qualification/AirbornePolicies/"+sourceName+"OriginalHeightBaked.fbx";
                    var name=sourceName+suffix;var clip=AnimationPlaygroundClipDerivatives.CreateRange(source,name,start,end,loop);
                    var factor=gauge.gauges.Single(x=>x.source==source).worldUnitsPerCurveUnit;
                    if(!float.IsFinite(factor) || factor<.1f || factor>10)throw new InvalidDataException("The root-height gauge is invalid.");
                    var binding=AnimationUtility.GetCurveBindings(clip).Single(x=>x.propertyName=="RootT.y");
                    var adjustment=0f;
                    for(var pass=0;pass<3;pass++)
                    {
                        var measured=AnimationPlaygroundMotionSampler.Measure(model,AssetDatabase.GetAssetPath(clip),bindings.visualScale,bindings.visualOffset,60);
                        var curve=AnimationUtility.GetEditorCurve(clip,binding);var keys=new Keyframe[measured.frames.Length];
                        for(var i=0;i<keys.Length;i++)
                        {
                            var frame=measured.frames[i];
                            var delta=neutralHip ? reference-frame.hips.y : -.91f-Mathf.Min(frame.leftSole.y,frame.rightSole.y)+.002f;
                            if(!neutralHip && pass>0)delta=Mathf.Max(0,delta);
                            adjustment=Mathf.Max(adjustment,Mathf.Abs(delta));
                            keys[i]=new Keyframe(frame.seconds,curve.Evaluate(frame.seconds)+delta/factor);
                        }
                        var corrected=new AnimationCurve(keys);
                        for(var i=0;i<keys.Length;i++){AnimationUtility.SetKeyLeftTangentMode(corrected,i,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(corrected,i,AnimationUtility.TangentMode.Linear);}
                        AnimationUtility.SetEditorCurve(clip,binding,corrected);EditorUtility.SetDirty(clip);AssetDatabase.SaveAssetIfDirty(clip);
                    }
                    var result=AnimationPlaygroundMotionSampler.Measure(model,AssetDatabase.GetAssetPath(clip),bindings.visualScale,bindings.visualOffset,60);
                    if(!result.finite)throw new InvalidDataException("The derived poses are not finite.");
                    File.WriteAllText(Path.Combine(directory,name+".json"),JsonUtility.ToJson(result,true)+"\n");
                    rows.Add(new Recipe{name=name,path=AssetDatabase.GetAssetPath(clip),source=source,start=start,end=end,
                        minimumSole=result.frames.Min(x=>Mathf.Min(x.leftSole.y,x.rightSole.y)),minimumHip=result.frames.Min(x=>x.hips.y),
                        maximumHip=result.frames.Max(x=>x.hips.y),maximumBody=result.frames.Max(x=>x.bodyMaximum.y),maximumAdjustment=adjustment});
                    return clip;
                }
                profile.standing=new AnimationPlaygroundJumpVariant{sourceId="JumpStanding",
                    takeoff=Create("JumpStanding","TakeoffGrounded",0,.6333333f),
                    rising=Create("JumpStanding","RisingPose",.6333333f,.9666667f,true),
                    landing=Create("JumpStanding","LandingGrounded",1.2166667f,2.1666667f)};
                profile.running=new AnimationPlaygroundJumpVariant{sourceId="JumpRunning",
                    takeoff=Create("JumpRunning","TakeoffGrounded",0,.098f),
                    rising=Create("JumpRunning","RisingPose",.098f,.376f,true),
                    landing=Create("JumpRunning","LandingGrounded",.605f,.9f)};
                profile.unarmed=new AnimationPlaygroundJumpVariant{sourceId="JumpUnarmed",
                    takeoff=Create("JumpUnarmed","TakeoffGrounded",.2666667f,.8333333f),
                    rising=Create("JumpUnarmed","RisingPose",.8333333f,1.1666667f,true),
                    landing=Create("JumpUnarmed","LandingGrounded",1.4666667f,2.3333333f)};
                profile.falling=Create("FallAirborneIdle","PhysicsLoop",0,.7f,true,true);
                profile.approach=Create("FallLanding","ContactApproach",0,.2666667f);
                profile.hardLanding=Create("FallLanding","ContactRecovery",.2666667f,1.0666667f);
                if(!profile.Validate(out var reason,false))throw new InvalidDataException(reason);
                AssetDatabase.CreateAsset(profile,ProfilePath);bindings.airborne=profile;EditorUtility.SetDirty(bindings);AssetDatabase.SaveAssets();
                File.WriteAllText(Path.Combine(directory,"summary.json"),JsonUtility.ToJson(new Draft{clips=rows.ToArray()},true)+"\n");
            }
            catch(Exception error){File.WriteAllText(Path.Combine(directory,"failure.txt"),error.ToString());Debug.LogException(error);exitCode=1;}
            EditorApplication.Exit(exitCode);
        }
        public static void ProbeRootCurveFromCli()
        {
            if(!Application.isBatchMode || Directory.Exists(".git") || !File.Exists(AnimationPlaygroundLayoutAuthoring.CandidateMarker))
                throw new InvalidOperationException("Probe only in the marked candidate.");
            var directory=Path.GetFullPath("AirborneQualification/Gauge");Directory.CreateDirectory(directory);var exitCode=0;
            try
            {
                var bindings=AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(AnimationPlaygroundPlayerAuthoring.BindingPath);
                var rows=new List<Gauge>();
                foreach(var name in new[]{"JumpStanding","JumpRunning","JumpUnarmed","FallLanding","FallRollLanding","FallAirborneIdle"})
                {
                    var source="Assets/_Project/Art/Animations/Mixamo/Qualification/AirbornePolicies/"+name+"OriginalHeightBaked.fbx";
                    var original=AnimationPlaygroundClipDerivatives.LoadClip(source);
                    var probe=AnimationPlaygroundClipDerivatives.CreateRange(source,name+"RootGauge",0,original.length);
                    var binding=AnimationUtility.GetCurveBindings(probe).Single(x=>x.propertyName=="RootT.y");
                    var curve=AnimationUtility.GetEditorCurve(probe,binding);var keys=curve.keys;
                    for(var i=0;i<keys.Length;i++)keys[i].value+=.1f*Mathf.Sin(Mathf.PI*keys[i].time/probe.length);
                    AnimationUtility.SetEditorCurve(probe,binding,new AnimationCurve(keys));EditorUtility.SetDirty(probe);AssetDatabase.SaveAssetIfDirty(probe);
                    var baseline=AnimationPlaygroundMotionSampler.Measure(AssetDatabase.GetAssetPath(bindings.targetModel),source,bindings.visualScale,bindings.visualOffset,60);
                    var changed=AnimationPlaygroundMotionSampler.Measure(AssetDatabase.GetAssetPath(bindings.targetModel),AssetDatabase.GetAssetPath(probe),bindings.visualScale,bindings.visualOffset,60);
                    var index=baseline.frames.Length/2;var delta=changed.frames[index].hips.y-baseline.frames[index].hips.y;
                    rows.Add(new Gauge{source=source,curveDelta=.1f,hipDelta=delta,worldUnitsPerCurveUnit=delta/.1f});
                }
                File.WriteAllText(Path.Combine(directory,"summary.json"),JsonUtility.ToJson(new Gauges{gauges=rows.ToArray()},true)+"\n");
            }
            catch(Exception error){File.WriteAllText(Path.Combine(directory,"failure.txt"),error.ToString());Debug.LogException(error);exitCode=1;}
            EditorApplication.Exit(exitCode);
        }
        public static void ComparePoliciesFromCli()
        {
            if(!Application.isBatchMode || Directory.Exists(".git") || !File.Exists(AnimationPlaygroundLayoutAuthoring.CandidateMarker))
                throw new InvalidOperationException("Review policies only in the marked candidate.");
            var directory=Path.GetFullPath("AirborneQualification");Directory.CreateDirectory(directory);
            var folder="Assets/_Project/Art/Animations/Mixamo/Qualification/AirbornePolicies";Directory.CreateDirectory(folder);
            var rows=new List<Policy>();var exitCode=0;
            try
            {
                var bindings=AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(AnimationPlaygroundPlayerAuthoring.BindingPath);
                foreach(var index in new[]{8,9,10,11,12,13,7})
                    foreach(var bake in new[]{false,true})
                    {
                        var canonical=AnimationPlaygroundMobilityAuthoring.Paths[index];
                        var name=Path.GetFileNameWithoutExtension(canonical)+(bake ? "OriginalHeightBaked" : "PoseOnly");
                        var destination=folder+"/"+name+".fbx";
                        if(File.Exists(destination) || File.Exists(destination+".meta"))throw new IOException("Preserve existing policy artifacts: "+destination);
                        File.Copy(canonical,destination);AssetDatabase.ImportAsset(destination,ImportAssetOptions.ForceSynchronousImport);
                        var importer=(ModelImporter)AssetImporter.GetAtPath(destination);
                        var original=((ModelImporter)AssetImporter.GetAtPath(canonical)).clipAnimations[0];
                        importer.animationType=ModelImporterAnimationType.Human;importer.avatarSetup=ModelImporterAvatarSetup.CopyFromOther;
                        importer.sourceAvatar=bindings.sourceAvatar;importer.importAnimation=true;importer.animationCompression=ModelImporterAnimationCompression.Off;
                        importer.materialImportMode=ModelImporterMaterialImportMode.None;
                        importer.clipAnimations=new[]{new ModelImporterClipAnimation{
                            name=name,takeName=original.takeName,firstFrame=original.firstFrame,lastFrame=original.lastFrame,
                            loopTime=false,lockRootRotation=true,keepOriginalOrientation=true,
                            lockRootHeightY=bake,keepOriginalPositionY=true,heightFromFeet=false,
                            lockRootPositionXZ=false,keepOriginalPositionXZ=false}};
                        importer.SaveAndReimport();
                        var measured=AnimationPlaygroundMotionSampler.Measure(AssetDatabase.GetAssetPath(bindings.targetModel),destination,bindings.visualScale,bindings.visualOffset,60);
                        if(!measured.finite)throw new InvalidDataException("Non-finite airborne poses.");
                        File.WriteAllText(Path.Combine(directory,name+".json"),JsonUtility.ToJson(measured,true)+"\n");
                        var clip=AnimationPlaygroundClipDerivatives.LoadClip(destination);
                        rows.Add(new Policy{
                            source=canonical,path=destination,bakeHeight=bake,seconds=clip.length,
                            hipMinimum=measured.frames.Min(x=>x.hips.y),hipMaximum=measured.frames.Max(x=>x.hips.y),
                            soleMinimum=measured.frames.Min(x=>Mathf.Min(x.leftSole.y,x.rightSole.y)),
                            soleMaximum=measured.frames.Max(x=>Mathf.Min(x.leftSole.y,x.rightSole.y)),
                            bodyMinimum=measured.frames.Min(x=>x.bodyMinimum.y),bodyMaximum=measured.frames.Max(x=>x.bodyMaximum.y),
                            rootCurves=AnimationUtility.GetCurveBindings(clip).Where(x=>x.propertyName.StartsWith("Root",StringComparison.Ordinal)).Select(x=>x.propertyName).ToArray()});
                        File.WriteAllText(Path.Combine(directory,"progress.json"),JsonUtility.ToJson(new Report{policies=rows.ToArray()},true)+"\n");
                    }
                File.WriteAllText(Path.Combine(directory,"summary.json"),JsonUtility.ToJson(new Report{policies=rows.ToArray()},true)+"\n");
            }
            catch(Exception error){File.WriteAllText(Path.Combine(directory,"failure.txt"),error.ToString());Debug.LogException(error);exitCode=1;}
            EditorApplication.Exit(exitCode);
        }
    }
}
