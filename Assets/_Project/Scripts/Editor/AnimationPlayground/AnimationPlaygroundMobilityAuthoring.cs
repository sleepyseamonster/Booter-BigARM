using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Editor
{
    public static class AnimationPlaygroundMobilityAuthoring
    {
        public static readonly string[] Paths = {
            "Assets/_Project/Art/Animations/Mixamo/Locomotion/Crouch/CrouchEnter.fbx",
            "Assets/_Project/Art/Animations/Mixamo/Locomotion/Crouch/CrouchExit.fbx",
            "Assets/_Project/Art/Animations/Mixamo/Locomotion/Crouch/CrouchIdle.fbx",
            "Assets/_Project/Art/Animations/Mixamo/Locomotion/Crouch/CrouchWalkForward.fbx",
            "Assets/_Project/Art/Animations/Mixamo/Locomotion/Crouch/CrouchWalkBackward.fbx",
            "Assets/_Project/Art/Animations/Mixamo/Locomotion/Crouch/CrouchWalkLeft.fbx",
            "Assets/_Project/Art/Animations/Mixamo/Locomotion/Crouch/CrouchWalkRight.fbx",
            "Assets/_Project/Art/Animations/Mixamo/Traversal/Slide/RunningSlide.fbx",
            "Assets/_Project/Art/Animations/Mixamo/Traversal/Jump/JumpStanding.fbx",
            "Assets/_Project/Art/Animations/Mixamo/Traversal/Jump/JumpRunning.fbx",
            "Assets/_Project/Art/Animations/Mixamo/Traversal/Jump/JumpUnarmed.fbx",
            "Assets/_Project/Art/Animations/Mixamo/Traversal/Fall/FallAirborneIdle.fbx",
            "Assets/_Project/Art/Animations/Mixamo/Traversal/Fall/FallLanding.fbx",
            "Assets/_Project/Art/Animations/Mixamo/Traversal/Fall/FallRollLanding.fbx" };
        [Serializable] private sealed class Phase { public float phase,bodyTop,leftSole,rightSole,hipsY; }
        [Serializable] private sealed class ClipSummary
        {
            public string clip;
            public bool finite;
            public float seconds,minimumSole,maximumBodyTop,minimumBodyTop,seamFootGap;
            public Phase[] phases;
        }
        [Serializable] private sealed class Summary { public string state="measured-not-physical-qualified"; public ClipSummary[] clips; }
        public const string CrouchProfilePath="Assets/_Project/Settings/AnimationPlayground/PrivateBindings/CrouchProfile.asset";
        public static void CreateCrouchDraftFromCli()
        {
            if(!Application.isBatchMode || Directory.Exists(".git") || !File.Exists(AnimationPlaygroundLayoutAuthoring.CandidateMarker))
                throw new InvalidOperationException("Qualify the crouch draft only in the marked validation copy.");
            var directory=Path.GetFullPath("MobilityQualification/CrouchDraft");Directory.CreateDirectory(directory);
            try
            {
                var bindings=AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(AnimationPlaygroundPlayerAuthoring.BindingPath);
                var profile=AssetDatabase.LoadAssetAtPath<AnimationPlaygroundCrouchProfile>(CrouchProfilePath);
                var repair=profile!=null;
                if(repair && (profile.contactsQualified || bindings.crouch!=profile))throw new InvalidOperationException("Only this unpublished unqualified candidate draft may be revised.");
                if(!repair)profile=ScriptableObject.CreateInstance<AnimationPlaygroundCrouchProfile>();
                var entries=new List<AnimationPlaygroundCrouchEntry>();var reports=new List<ClipSummary>();
                for(var role=0;role<7;role++)
                {
                    var kind=(AnimationPlaygroundCrouchRole)role;var transition=kind==AnimationPlaygroundCrouchRole.Enter || kind==AnimationPlaygroundCrouchRole.Exit;
                    var source=kind==AnimationPlaygroundCrouchRole.Enter
                        ? "Assets/_Project/Art/Animations/Mixamo/Qualification/RootPolicies/CrouchEnterBakedOriginal.fbx"
                        : kind==AnimationPlaygroundCrouchRole.Exit ? "Assets/_Project/Art/Animations/Mixamo/Qualification/RootPolicies/CrouchExitBakedOriginal.fbx"
                        : kind==AnimationPlaygroundCrouchRole.Idle ? Paths[2] : Paths[role+1];
                    var sourceClip=AnimationPlaygroundClipDerivatives.LoadClip(source);
                    AnimationClip clip;
                    if(repair && (transition || kind==AnimationPlaygroundCrouchRole.Idle))clip=profile.Get(kind).clip;
                    else clip=transition ? AnimationPlaygroundClipDerivatives.CreateRange(source,"Crouch"+kind+"QualifiedRange",0,kind==AnimationPlaygroundCrouchRole.Enter ? 1.5f : sourceClip.length)
                        : AnimationPlaygroundClipDerivatives.CreateLoop(source,"Crouch"+kind+(repair ? "MappedTwoCycles" : "TwoCycles"),0,kind==AnimationPlaygroundCrouchRole.Idle ? 1 : 2);
                    var clipPath=AssetDatabase.GetAssetPath(clip);var measured=AnimationPlaygroundMotionSampler.Measure(AssetDatabase.GetAssetPath(bindings.targetModel),clipPath,bindings.visualScale,bindings.visualOffset,60);
                    var correction=AnimationPlaygroundClipDerivatives.GroundCorrection(measured,-bindings.capsuleHeight*.5f);
                    var aligned=AnimationPlaygroundMotionSampler.Measure(AssetDatabase.GetAssetPath(bindings.targetModel),clipPath,bindings.visualScale,bindings.visualOffset,60,false,correction);
                    var shapeKeys=new Keyframe[aligned.frames.Length];
                    for(var i=0;i<shapeKeys.Length;i++)
                    {
                        var headHeight=aligned.frames[i].bodyMaximum.y+bindings.capsuleHeight*.5f+.02f;
                        if(headHeight>profile.standingHeight+.005f)throw new InvalidDataException("Crouch source exceeds the standing head envelope: "+kind);
                        var height=transition ? Mathf.Clamp(headHeight,profile.crouchHeight,profile.standingHeight) : profile.crouchHeight;
                        if(!transition && headHeight>profile.crouchHeight)throw new InvalidDataException("Raise/review the crouch shape for "+kind);
                        shapeKeys[i]=new Keyframe((float)i/(shapeKeys.Length-1),height);
                    }
                    var shape=new AnimationCurve(shapeKeys);
                    for(var i=0;i<shapeKeys.Length;i++){AnimationUtility.SetKeyLeftTangentMode(shape,i,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(shape,i,AnimationUtility.TangentMode.Linear);}
                    entries.Add(new AnimationPlaygroundCrouchEntry{role=kind,clip=clip,looping=!transition,nominalSpeed=transition || kind==AnimationPlaygroundCrouchRole.Idle ? 0 : 1.0f,
                        heightCorrection=correction,capsuleHeight=shape});
                    File.WriteAllText(Path.Combine(directory,kind+"-aligned.json"),JsonUtility.ToJson(aligned,true)+"\n");
                    reports.Add(new ClipSummary{clip=kind.ToString(),finite=aligned.finite,seconds=aligned.seconds,minimumSole=aligned.frames.Min(frame=>Mathf.Min(frame.leftSole.y,frame.rightSole.y)),
                        minimumBodyTop=aligned.frames.Min(frame=>frame.bodyMaximum.y),maximumBodyTop=aligned.frames.Max(frame=>frame.bodyMaximum.y),seamFootGap=aligned.seamFootGap});
                }
                profile.entries=entries.ToArray();profile.contactsQualified=false;
                if(!profile.Validate(out var reason,false))throw new InvalidDataException(reason);
                if(!repair)AssetDatabase.CreateAsset(profile,CrouchProfilePath);else EditorUtility.SetDirty(profile);
                bindings.crouch=profile;EditorUtility.SetDirty(bindings);AssetDatabase.SaveAssets();
                File.WriteAllText(Path.Combine(directory,"summary.json"),JsonUtility.ToJson(new Summary{clips=reports.ToArray()},true)+"\n");
                File.WriteAllText(Path.Combine(directory,"completed.json"),"{\"state\":\"seven-role-crouch-draft-not-physical-qualified\"}\n");EditorApplication.Exit(0);
            }
            catch(Exception error){File.WriteAllText(Path.Combine(directory,"failure.txt"),error.ToString());Debug.LogException(error);EditorApplication.Exit(1);}
        }
        public static void CompareRootPoliciesFromCli()
        {
            if(!Application.isBatchMode || Directory.Exists(".git") || !File.Exists(AnimationPlaygroundLayoutAuthoring.CandidateMarker))
                throw new InvalidOperationException("Compare policies only in the marked validation copy.");
            var directory=Path.GetFullPath("MobilityQualification/RootPolicies");Directory.CreateDirectory(directory);
            var folder="Assets/_Project/Art/Animations/Mixamo/Qualification/RootPolicies";Directory.CreateDirectory(folder);
            var reports=new List<ClipSummary>();
            try
            {
                var bindings=AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(AnimationPlaygroundPlayerAuthoring.BindingPath);
                foreach(var source in new[]{Paths[0],Paths[1],Paths[8],Paths[12],Paths[13]})
                    foreach(var feet in new[]{false,true})
                    {
                        var name=Path.GetFileNameWithoutExtension(source)+(feet ? "BakedFeet" : "BakedOriginal");
                        var destination=folder+"/"+name+".fbx";
                        if(!File.Exists(destination))File.Copy(source,destination);
                        AssetDatabase.ImportAsset(destination,ImportAssetOptions.ForceSynchronousImport);
                        var importer=(ModelImporter)AssetImporter.GetAtPath(destination);
                        importer.animationType=ModelImporterAnimationType.Human;importer.avatarSetup=ModelImporterAvatarSetup.CopyFromOther;
                        importer.sourceAvatar=bindings.sourceAvatar;importer.importAnimation=true;importer.materialImportMode=ModelImporterMaterialImportMode.None;
                        importer.animationCompression=ModelImporterAnimationCompression.Off;
                        var original=((ModelImporter)AssetImporter.GetAtPath(source)).clipAnimations[0];
                        importer.clipAnimations=new[]{new ModelImporterClipAnimation{name=name,takeName=original.takeName,firstFrame=original.firstFrame,lastFrame=original.lastFrame,
                            loopTime=false,lockRootHeightY=true,heightFromFeet=feet,keepOriginalPositionY=!feet,lockRootRotation=original.lockRootRotation,lockRootPositionXZ=original.lockRootPositionXZ}};
                        importer.SaveAndReimport();
                        var measured=AnimationPlaygroundMotionSampler.Measure(AssetDatabase.GetAssetPath(bindings.targetModel),destination,bindings.visualScale,bindings.visualOffset,60);
                        File.WriteAllText(Path.Combine(directory,name+".json"),JsonUtility.ToJson(measured,true)+"\n");
                        var phases=new List<Phase>();
                        for(var index=0;index<=8;index++)
                        {var frame=measured.frames[Mathf.RoundToInt((measured.frames.Length-1)*index/8f)];phases.Add(new Phase{phase=index/8f,bodyTop=frame.bodyMaximum.y,leftSole=frame.leftSole.y,rightSole=frame.rightSole.y,hipsY=frame.hips.y});}
                        reports.Add(new ClipSummary{clip=name,finite=measured.finite,seconds=measured.seconds,minimumSole=measured.frames.Min(frame=>Mathf.Min(frame.leftSole.y,frame.rightSole.y)),
                            maximumBodyTop=measured.frames.Max(frame=>frame.bodyMaximum.y),minimumBodyTop=measured.frames.Min(frame=>frame.bodyMaximum.y),seamFootGap=measured.seamFootGap,phases=phases.ToArray()});
                    }
                File.WriteAllText(Path.Combine(directory,"summary.json"),JsonUtility.ToJson(new Summary{clips=reports.ToArray()},true)+"\n");EditorApplication.Exit(0);
            }
            catch(Exception error){File.WriteAllText(Path.Combine(directory,"failure.txt"),error.ToString());Debug.LogException(error);EditorApplication.Exit(1);}
        }
        public static void MeasureCandidateFromCli()
        {
            if(!Application.isBatchMode || Directory.Exists(".git") || !File.Exists(AnimationPlaygroundLayoutAuthoring.CandidateMarker))
                throw new InvalidOperationException("Sample in the explicitly marked validation copy.");
            var directory=Path.GetFullPath("MobilityQualification");Directory.CreateDirectory(directory);
            var reports=new List<ClipSummary>();
            try
            {
                var bindings=AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(AnimationPlaygroundPlayerAuthoring.BindingPath);
                if(bindings==null || !bindings.ValidatePlayer(out _))throw new InvalidDataException("Restore the calibrated player binding.");
                foreach(var path in Paths)
                {
                    var sourceClip=AnimationPlaygroundClipDerivatives.LoadClip(path);
                    File.WriteAllText(Path.Combine(directory,"current.json"),"{\"path\":\""+path+"\",\"model\":\""+AssetDatabase.GetAssetPath(bindings.targetModel)+"\",\"human\":"+sourceClip.isHumanMotion.ToString().ToLowerInvariant()+",\"seconds\":"+sourceClip.length.ToString(System.Globalization.CultureInfo.InvariantCulture)+"}\n");
                    var measured=AnimationPlaygroundMotionSampler.Measure(AssetDatabase.GetAssetPath(bindings.targetModel),path,bindings.visualScale,bindings.visualOffset,60);
                    File.WriteAllText(Path.Combine(directory,Path.GetFileNameWithoutExtension(path)+".json"),JsonUtility.ToJson(measured,true)+"\n");
                    var phases=new List<Phase>();
                    for(var index=0;index<=8;index++)
                    {
                        var frame=measured.frames[Mathf.RoundToInt((measured.frames.Length-1)*index/8f)];
                        phases.Add(new Phase{phase=index/8f,bodyTop=frame.bodyMaximum.y,leftSole=frame.leftSole.y,rightSole=frame.rightSole.y,hipsY=frame.hips.y});
                    }
                    reports.Add(new ClipSummary{clip=Path.GetFileNameWithoutExtension(path),finite=measured.finite,seconds=measured.seconds,
                        minimumSole=measured.frames.Min(frame=>Mathf.Min(frame.leftSole.y,frame.rightSole.y)),maximumBodyTop=measured.frames.Max(frame=>frame.bodyMaximum.y),
                        minimumBodyTop=measured.frames.Min(frame=>frame.bodyMaximum.y),seamFootGap=measured.seamFootGap,phases=phases.ToArray()});
                    File.WriteAllText(Path.Combine(directory,"progress.json"),"{\"measured\":"+reports.Count+",\"total\":14}\n");
                }
                File.WriteAllText(Path.Combine(directory,"summary.json"),JsonUtility.ToJson(new Summary{clips=reports.ToArray()},true)+"\n");EditorApplication.Exit(0);
            }
            catch(Exception error){File.WriteAllText(Path.Combine(directory,"failure.txt"),error.ToString());Debug.LogException(error);EditorApplication.Exit(1);}
        }
    }
}
