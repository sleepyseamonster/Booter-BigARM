using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Editor
{
    public static class AnimationPlaygroundCrouchFacingAuthoring
    {
        [Serializable] private sealed class Review
        {
            public string role,policy,path;
            public float leftPlantX,leftPlantZ,rightPlantX,rightPlantZ,maximumTorsoRadius,minimumSole,maximumBodyHeight;
            public bool finite;
        }
        [Serializable] private sealed class Report
        {public string state="candidate-facing-policies-not-physical-qualified";public Review[] clips;}
        [Serializable] private sealed class Recipe
        {
            public string role,clipPath,clipGuid,sourcePath,sourceSha256;
            public float sourcePhaseOffset,nominalSpeed,maximumTorsoRadius,maximumBodyHeight,minimumSole,maximumCorrection;
            public float[] rightContacts,leftContacts;
        }
        [Serializable] private sealed class Draft
        {public string state="authored-facing-contact-draft-not-physical-qualified";public float crouchRadius=.4f;public Recipe[] roles;}

        public static void CreateDraftFromCli()
        {
            if(!Application.isBatchMode || Directory.Exists(".git") || !File.Exists(AnimationPlaygroundLayoutAuthoring.CandidateMarker))
                throw new InvalidOperationException("Create the facing draft only in the marked validation copy.");
            var directory=Path.GetFullPath("MobilityQualification/FacingDraft");Directory.CreateDirectory(directory);
            try
            {
                var bindings=AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(AnimationPlaygroundPlayerAuthoring.BindingPath);
                var profile=bindings.crouch;
                if(profile==null || profile.contactsQualified)throw new InvalidDataException("Preserve a previously qualified profile.");
                var entries=new List<AnimationPlaygroundCrouchEntry>();var recipes=new List<Recipe>();
                foreach(AnimationPlaygroundCrouchRole role in Enum.GetValues(typeof(AnimationPlaygroundCrouchRole)))
                {
                    var index=role==AnimationPlaygroundCrouchRole.Enter ? 0 : role==AnimationPlaygroundCrouchRole.Exit ? 1
                        : role==AnimationPlaygroundCrouchRole.Idle ? 2 : (int)role+1;
                    var canonical=AnimationPlaygroundMobilityAuthoring.Paths[index];
                    var source="Assets/_Project/Art/Animations/Mixamo/Qualification/FacingInPlace/"+Path.GetFileNameWithoutExtension(canonical)+"OriginalBaked.fbx";
                    if(AnimationPlaygroundLayoutAuthoring.Hash(canonical)!=AnimationPlaygroundLayoutAuthoring.Hash(source))
                        throw new InvalidDataException("Facing comparison differs from its preserved source payload.");
                    var sourceClip=AnimationPlaygroundClipDerivatives.LoadClip(source);
                    var transition=role==AnimationPlaygroundCrouchRole.Enter || role==AnimationPlaygroundCrouchRole.Exit;
                    var derivativeName="Crouch"+role+(transition ? "OriginalFacingRange" : "OriginalFacingLoop");
                    var destination=AnimationPlaygroundClipDerivatives.Folder+"/"+derivativeName+".anim";
                    var clip=File.Exists(destination) ? AnimationPlaygroundClipDerivatives.LoadClip(destination)
                        : transition ? AnimationPlaygroundClipDerivatives.CreateRange(source,derivativeName,0,
                            role==AnimationPlaygroundCrouchRole.Enter ? 1.5f : sourceClip.length)
                        : AnimationPlaygroundClipDerivatives.CreateLoop(source,derivativeName,0,role==AnimationPlaygroundCrouchRole.Idle ? 1 : 2);
                    VerifyRecipe(sourceClip,clip,transition,role==AnimationPlaygroundCrouchRole.Enter ? 1.5f : sourceClip.length,role==AnimationPlaygroundCrouchRole.Idle ? 1 : 2);
                    var path=AssetDatabase.GetAssetPath(clip);
                    var measured=AnimationPlaygroundMotionSampler.Measure(AssetDatabase.GetAssetPath(bindings.targetModel),path,bindings.visualScale,bindings.visualOffset,60);
                    var correction=AnimationPlaygroundClipDerivatives.GroundCorrection(measured,-bindings.capsuleHeight*.5f);
                    var aligned=AnimationPlaygroundMotionSampler.Measure(AssetDatabase.GetAssetPath(bindings.targetModel),path,bindings.visualScale,bindings.visualOffset,60,false,correction);
                    File.WriteAllText(Path.Combine(directory,role+"-aligned.json"),JsonUtility.ToJson(aligned,true)+"\n");
                    var keys=new Keyframe[aligned.frames.Length];
                    for(var i=0;i<keys.Length;i++)
                    {
                        var height=aligned.frames[i].bodyMaximum.y+bindings.capsuleHeight*.5f+.02f;
                        if(height>profile.standingHeight+.005f || !transition && height>profile.crouchHeight)
                            throw new InvalidDataException("The new facing draft exceeds its reviewed height: "+role);
                        keys[i]=new Keyframe((float)i/(keys.Length-1),transition ? Mathf.Clamp(height,profile.crouchHeight,profile.standingHeight) : profile.crouchHeight);
                    }
                    var shape=new AnimationCurve(keys);
                    for(var i=0;i<keys.Length;i++){AnimationUtility.SetKeyLeftTangentMode(shape,i,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(shape,i,AnimationUtility.TangentMode.Linear);}
                    var entry=new AnimationPlaygroundCrouchEntry{role=role,clip=clip,looping=!transition,heightCorrection=correction,capsuleHeight=shape};
                    if(role>=AnimationPlaygroundCrouchRole.Forward && role<=AnimationPlaygroundCrouchRole.Right)
                    {
                        var direction=role==AnimationPlaygroundCrouchRole.Forward ? Vector3.forward : role==AnimationPlaygroundCrouchRole.Backward ? Vector3.back
                            : role==AnimationPlaygroundCrouchRole.Left ? Vector3.left : Vector3.right;
                        aligned.rightCandidates=FindSoleContactWindows(aligned.frames,false,direction,-bindings.capsuleHeight*.5f);
                        aligned.leftCandidates=FindSoleContactWindows(aligned.frames,true,direction,-bindings.capsuleHeight*.5f);
                        File.WriteAllText(Path.Combine(directory,role+"-contacts.json"),JsonUtility.ToJson(aligned,true)+"\n");
                        var right=CyclicContactPhases(aligned.rightCandidates,aligned.seconds,1/aligned.sampleRate);
                        var left=CyclicContactPhases(aligned.leftCandidates,aligned.seconds,1/aligned.sampleRate);
                        entry.sourcePhaseOffset=right[0];
                        entry.rightContacts=right.Select(value=>Mathf.Repeat(value-entry.sourcePhaseOffset,1)).OrderBy(value=>value).ToArray();
                        entry.leftContacts=left.Select(value=>Mathf.Repeat(value-entry.sourcePhaseOffset,1)).OrderBy(value=>value).ToArray();
                        if(!AnimationPlaygroundCrouchProfile.OrderedContacts(entry.rightContacts,entry.leftContacts))
                            throw new InvalidDataException("Directional contacts do not alternate for "+role);
                        var velocity=(PlantVelocity(aligned,true)+PlantVelocity(aligned,false))*.5f;
                        if(Vector3.Angle(-velocity,direction)>10)throw new InvalidDataException("Authored source does not align with the requested direction: "+role);
                        entry.nominalSpeed=Vector3.Dot(-velocity,direction);
                    }
                    entries.Add(entry);File.WriteAllText(Path.Combine(directory,role+"-aligned.json"),JsonUtility.ToJson(aligned,true)+"\n");
                    recipes.Add(new Recipe{role=role.ToString(),clipPath=path,clipGuid=AssetDatabase.AssetPathToGUID(path),sourcePath=canonical,
                        sourceSha256=AnimationPlaygroundLayoutAuthoring.Hash(canonical),sourcePhaseOffset=entry.sourcePhaseOffset,nominalSpeed=entry.nominalSpeed,
                        rightContacts=entry.rightContacts,leftContacts=entry.leftContacts,maximumTorsoRadius=aligned.frames.Max(frame=>frame.torsoMaximumRadius),
                        maximumBodyHeight=aligned.frames.Max(frame=>frame.bodyMaximum.y)+bindings.capsuleHeight*.5f,
                        minimumSole=aligned.frames.Min(frame=>Mathf.Min(frame.leftSole.y,frame.rightSole.y)),
                        maximumCorrection=correction.keys.Max(key=>key.value)});
                }
                profile.entries=entries.ToArray();profile.crouchRadius=.4f;profile.contactsQualified=false;
                profile.groundingQualified=false;profile.grounding=Array.Empty<AnimationPlaygroundCrouchGroundSample>();
                if(!profile.Validate(out var reason,false))throw new InvalidDataException(reason);
                EditorUtility.SetDirty(profile);AssetDatabase.SaveAssets();
                File.WriteAllText(Path.Combine(directory,"recipe.json"),JsonUtility.ToJson(new Draft{roles=recipes.ToArray()},true)+"\n");EditorApplication.Exit(0);
            }
            catch(Exception error){File.WriteAllText(Path.Combine(directory,"failure.txt"),error.ToString());Debug.LogException(error);EditorApplication.Exit(1);}
        }

        private static void VerifyRecipe(AnimationClip source,AnimationClip derivative,bool range,float end,int cycles)
        {
            var seconds=range ? end : source.length*cycles;
            if(!derivative.isHumanMotion || derivative.isLooping==range || Mathf.Abs(derivative.length-seconds)>.0001f)
                throw new InvalidDataException("An existing candidate derivative does not match the bounded recipe.");
            foreach(var binding in AnimationUtility.GetCurveBindings(source))
            {
                var original=AnimationUtility.GetEditorCurve(source,binding);var derived=AnimationUtility.GetEditorCurve(derivative,binding);
                if(original==null || derived==null)throw new InvalidDataException("Derivative curve bindings differ from the source.");
                foreach(var key in derived.keys)
                {
                    var time=range ? key.time : Mathf.Repeat(key.time/source.length,1)*source.length;
                    if(Mathf.Abs(key.value-original.Evaluate(time))>.0001f)throw new InvalidDataException("Preserve the existing candidate; its curve recipe differs.");
                }
            }
        }

        public static AnimationPlaygroundMotionSampler.ContactWindow[] FindSoleContactWindows(
            AnimationPlaygroundMotionSampler.PoseFrame[] frames,bool left,Vector3 direction,float feetPlane)
        {
            if(frames==null || frames.Length<3 || !float.IsFinite(feetPlane) || !float.IsFinite(direction.x) || !float.IsFinite(direction.y)
                || !float.IsFinite(direction.z) || Vector3.ProjectOnPlane(direction,Vector3.up).sqrMagnitude<.0001f)
                throw new ArgumentException("Sole contacts require calibrated poses and a travel axis.");
            direction=Vector3.ProjectOnPlane(direction,Vector3.up).normalized;
            var eligible=new bool[frames.Length];var speeds=new float[frames.Length];
            for(var i=1;i<frames.Length-1;i++)
            {
                var sole=left ? frames[i].leftSole : frames[i].rightSole;
                var before=left ? frames[i-1].leftFoot : frames[i-1].rightFoot;
                var after=left ? frames[i+1].leftFoot : frames[i+1].rightFoot;
                var seconds=frames[i+1].seconds-frames[i-1].seconds;
                if(!float.IsFinite(seconds) || seconds<=0)throw new ArgumentException("Pose times must increase.");
                var velocity=(after-before)/seconds;
                speeds[i]=Vector3.Dot(-velocity,direction);
                eligible[i]=sole.y>=feetPlane-.002f && sole.y<=feetPlane+.02f && speeds[i]>.15f;
            }
            var windows=new List<AnimationPlaygroundMotionSampler.ContactWindow>();
            for(var i=1;i<frames.Length-1;i++)
            {
                if(!eligible[i])continue;var start=i;var speed=0f;
                while(i<frames.Length-1 && eligible[i]){speed+=speeds[i];i++;}
                var end=i-1;
                if(frames[end].seconds-frames[start].seconds<.025f)continue;
                windows.Add(new AnimationPlaygroundMotionSampler.ContactWindow{start=frames[start].seconds,end=frames[end].seconds,
                    phase=(frames[start].seconds+frames[end].seconds)*.5f/frames[^1].seconds,meanPlanarSpeed=speed/(end-start+1)});
            }
            return windows.ToArray();
        }

        public static float[] CyclicContactPhases(AnimationPlaygroundMotionSampler.ContactWindow[] source,float duration,float sampleSeconds)
        {
            if(source==null || !float.IsFinite(duration) || duration<=0 || !float.IsFinite(sampleSeconds) || sampleSeconds<=0)
                throw new ArgumentException("Cyclic contacts require a finite measured timeline.");
            var windows=source.OrderBy(window=>window.start).ToList();
            var phases=windows.Select(window=>window.phase).ToList();
            if(windows.Count>2 && windows[0].start<=sampleSeconds*1.5f && windows[^1].end>=duration-sampleSeconds*1.5f)
            {
                var phase=Mathf.Repeat((windows[^1].start+windows[0].end+duration)*.5f/duration,1);
                phases.RemoveAt(phases.Count-1);phases.RemoveAt(0);phases.Add(phase);
            }
            if(phases.Count!=2 || phases.Any(phase=>!float.IsFinite(phase) || phase<0 || phase>=1))
                throw new InvalidDataException("Review the source contact windows; two cyclic plants per foot are required.");
            return phases.OrderBy(value=>value).ToArray();
        }

        public static void CompareFromCli()
        {
            if(!Application.isBatchMode || Directory.Exists(".git") || !File.Exists(AnimationPlaygroundLayoutAuthoring.CandidateMarker))
                throw new InvalidOperationException("Compare root policies only in the marked non-repository validation copy.");
            var directory=Path.GetFullPath("MobilityQualification/FacingInPlace");Directory.CreateDirectory(directory);
            var folder="Assets/_Project/Art/Animations/Mixamo/Qualification/FacingInPlace";Directory.CreateDirectory(folder);
            try
            {
                var bindings=AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(AnimationPlaygroundPlayerAuthoring.BindingPath);
                var rows=new List<Review>();
                for(var index=0;index<7;index++)
                {
                    var source=AnimationPlaygroundMobilityAuthoring.Paths[index];
                    foreach(var policy in new[]{"OriginalBaked","BodyBaked","OriginalExtracted"})
                    {
                        var destination=folder+"/"+Path.GetFileNameWithoutExtension(source)+policy+".fbx";
                        if(File.Exists(destination))throw new IOException("Preserve a previous comparison: "+destination);
                        File.Copy(source,destination);AssetDatabase.ImportAsset(destination,ImportAssetOptions.ForceSynchronousImport);
                        var importer=(ModelImporter)AssetImporter.GetAtPath(destination);
                        importer.animationType=ModelImporterAnimationType.Human;importer.avatarSetup=ModelImporterAvatarSetup.CopyFromOther;
                        importer.sourceAvatar=bindings.sourceAvatar;importer.importAnimation=true;importer.materialImportMode=ModelImporterMaterialImportMode.None;
                        importer.animationCompression=ModelImporterAnimationCompression.Off;
                        var original=((ModelImporter)AssetImporter.GetAtPath(source)).clipAnimations[0];
                        importer.clipAnimations=new[]{new ModelImporterClipAnimation{name=Path.GetFileNameWithoutExtension(destination),takeName=original.takeName,
                            firstFrame=original.firstFrame,lastFrame=original.lastFrame,lockRootHeightY=true,heightFromFeet=false,keepOriginalPositionY=true,
                            lockRootRotation=policy!="OriginalExtracted",keepOriginalOrientation=policy!="BodyBaked",lockRootPositionXZ=false,keepOriginalPositionXZ=false}};
                        importer.SaveAndReimport();
                        var measured=AnimationPlaygroundMotionSampler.Measure(AssetDatabase.GetAssetPath(bindings.targetModel),destination,bindings.visualScale,bindings.visualOffset,60);
                        File.WriteAllText(Path.Combine(directory,Path.GetFileNameWithoutExtension(destination)+".json"),JsonUtility.ToJson(measured,true)+"\n");
                        var left=PlantVelocity(measured,true);var right=PlantVelocity(measured,false);
                        rows.Add(new Review{role=Path.GetFileNameWithoutExtension(source),policy=policy,path=destination,
                            leftPlantX=left.x,leftPlantZ=left.z,rightPlantX=right.x,rightPlantZ=right.z,finite=measured.finite,
                            maximumTorsoRadius=measured.frames.Max(frame=>frame.torsoMaximumRadius),
                            minimumSole=measured.frames.Min(frame=>Mathf.Min(frame.leftSole.y,frame.rightSole.y)),
                            maximumBodyHeight=measured.frames.Max(frame=>frame.bodyMaximum.y)+bindings.capsuleHeight*.5f});
                        File.WriteAllText(Path.Combine(directory,"progress.json"),"{\"completed\":"+rows.Count+",\"total\":21}\n");
                    }
                }
                File.WriteAllText(Path.Combine(directory,"summary.json"),JsonUtility.ToJson(new Report{clips=rows.ToArray()},true)+"\n");
                EditorApplication.Exit(0);
            }
            catch(Exception error){File.WriteAllText(Path.Combine(directory,"failure.txt"),error.ToString());Debug.LogException(error);EditorApplication.Exit(1);}
        }

        public static Vector3 PlantVelocity(AnimationPlaygroundMotionSampler.MotionMeasurement measurement,bool left)
        {
            var windows=left ? measurement.leftCandidates : measurement.rightCandidates;
            var sum=Vector3.zero;var count=0;var frames=measurement.frames;
            for(var i=1;i<frames.Length-1;i++)
            {
                if(!windows.Any(window=>frames[i].seconds>=window.start && frames[i].seconds<=window.end))continue;
                var before=left ? frames[i-1].leftFoot : frames[i-1].rightFoot;
                var after=left ? frames[i+1].leftFoot : frames[i+1].rightFoot;
                sum+=Vector3.ProjectOnPlane((after-before)/(frames[i+1].seconds-frames[i-1].seconds),Vector3.up);count++;
            }
            return count==0 ? Vector3.zero : sum/count;
        }
    }
}
