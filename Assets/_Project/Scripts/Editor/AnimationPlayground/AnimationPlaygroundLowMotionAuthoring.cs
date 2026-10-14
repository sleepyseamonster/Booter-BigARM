using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Editor
{
    public static class AnimationPlaygroundLowMotionAuthoring
    {
        public const string ProfilePath="Assets/_Project/Settings/AnimationPlayground/PrivateBindings/LowMotionProfile.asset";
        public const string ProjectionPath="Assets/_Project/Settings/AnimationPlayground/PrivateBindings/BodyProjection.asset";
        [Serializable] private sealed class Recipe
        {public string role,path,source;public float start,end,minimumBody,maximumHeight,maximumAdjustment;public bool horizontal;}
        [Serializable] private sealed class Report
        {public string state="low-motion-draft-not-physical-qualified";public int projectionVertices,projectionBones;public Recipe[] clips;}
        private static AnimationCurve Linear(Keyframe[] keys)
        {
            var result=new AnimationCurve(keys);
            for(var i=0;i<keys.Length;i++)
            {AnimationUtility.SetKeyLeftTangentMode(result,i,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(result,i,AnimationUtility.TangentMode.Linear);}
            return result;
        }
        public static void CreateDraftFromCli()
        {
            if(!Application.isBatchMode || Directory.Exists(".git") || !File.Exists(AnimationPlaygroundLayoutAuthoring.CandidateMarker))
                throw new InvalidOperationException("Author low-motion drafts only in the marked candidate.");
            var directory=Path.GetFullPath("LowMotionQualification/Draft");Directory.CreateDirectory(directory);var exitCode=0;
            try
            {
                if(File.Exists(ProfilePath) || File.Exists(ProfilePath+".meta") || File.Exists(ProjectionPath) || File.Exists(ProjectionPath+".meta"))
                    throw new IOException("Preserve prior low-motion assets and their GUIDs.");
                var bindings=AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(AnimationPlaygroundPlayerAuthoring.BindingPath);
                var model=AssetDatabase.GetAssetPath(bindings.targetModel);
                var profile=ScriptableObject.CreateInstance<AnimationPlaygroundLowMotionProfile>();profile.targetModel=bindings.targetModel;
                var entries=new List<AnimationPlaygroundLowMotionEntry>();var rows=new List<Recipe>();
                void Create(AnimationPlaygroundLowMotionRole role,string name,float start,float end,bool horizontal)
                {
                    var source="Assets/_Project/Art/Animations/Mixamo/Qualification/AirbornePolicies/"+name+"OriginalHeightBaked.fbx";
                    var clip=AnimationPlaygroundClipDerivatives.CreateRange(source,role+"BodyGrounded",start,end);
                    var root=AnimationUtility.GetCurveBindings(clip).Single(x=>x.propertyName=="RootT.y");var adjustment=0f;
                    for(var pass=0;pass<3;pass++)
                    {
                        var measured=AnimationPlaygroundMotionSampler.Measure(model,AssetDatabase.GetAssetPath(clip),bindings.visualScale,bindings.visualOffset,90);
                        var curve=AnimationUtility.GetEditorCurve(clip,root);var keys=new Keyframe[measured.frames.Length];
                        for(var i=0;i<keys.Length;i++)
                        {
                            var frame=measured.frames[i];var delta=-.908f-frame.bodyMinimum.y;
                            if(pass>0)delta=Mathf.Max(0,delta);
                            adjustment=Mathf.Max(adjustment,Mathf.Abs(delta));
                            keys[i]=new Keyframe(frame.seconds,curve.Evaluate(frame.seconds)+delta/.9894466f);
                        }
                        AnimationUtility.SetEditorCurve(clip,root,Linear(keys));EditorUtility.SetDirty(clip);AssetDatabase.SaveAssetIfDirty(clip);
                    }
                    var result=AnimationPlaygroundMotionSampler.Measure(model,AssetDatabase.GetAssetPath(clip),bindings.visualScale,bindings.visualOffset,90);
                    if(!result.finite)throw new InvalidDataException("The derived body poses are not finite.");
                    var shape=new Keyframe[result.frames.Length];
                    for(var i=0;i<shape.Length;i++)
                        shape[i]=new Keyframe(result.frames[i].seconds/clip.length,
                            Mathf.Clamp(result.frames[i].bodyMaximum.y+.91f+.035f,profile.verticalRadius*2,profile.standingHeight));
                    if(role==AnimationPlaygroundLowMotionRole.SlideEnter || role==AnimationPlaygroundLowMotionRole.RollContact)shape[0].value=profile.standingHeight;
                    if(role==AnimationPlaygroundLowMotionRole.SlideRecover || role==AnimationPlaygroundLowMotionRole.RollRecover)shape[^1].value=profile.standingHeight;
                    entries.Add(new AnimationPlaygroundLowMotionEntry{role=role,clip=clip,horizontal=horizontal,capsuleHeight=Linear(shape)});
                    rows.Add(new Recipe{role=role.ToString(),path=AssetDatabase.GetAssetPath(clip),source=source,start=start,end=end,horizontal=horizontal,
                        minimumBody=result.frames.Min(x=>x.bodyMinimum.y),maximumHeight=result.frames.Max(x=>x.bodyMaximum.y)+.91f,maximumAdjustment=adjustment});
                    File.WriteAllText(Path.Combine(directory,role+".json"),JsonUtility.ToJson(result,true)+"\n");
                }
                Create(AnimationPlaygroundLowMotionRole.SlideEnter,"RunningSlide",0,.3666667f,false);
                Create(AnimationPlaygroundLowMotionRole.SlideActive,"RunningSlide",.3666667f,.75f,true);
                Create(AnimationPlaygroundLowMotionRole.SlideRecover,"RunningSlide",.75f,1.5333333f,false);
                Create(AnimationPlaygroundLowMotionRole.RollContact,"FallRollLanding",.4666667f,.7f,false);
                Create(AnimationPlaygroundLowMotionRole.RollActive,"FallRollLanding",.7f,1.3f,true);
                Create(AnimationPlaygroundLowMotionRole.RollRecover,"FallRollLanding",1.3f,2.1333333f,false);
                profile.entries=entries.ToArray();profile.bodyProjection=AnimationPlaygroundFootProjectionAuthoring.Build(bindings.targetModel,true);
                if(!profile.Validate(out var reason,false))throw new InvalidDataException(reason);
                AssetDatabase.CreateAsset(profile.bodyProjection,ProjectionPath);AssetDatabase.CreateAsset(profile,ProfilePath);AssetDatabase.SaveAssets();
                File.WriteAllText(Path.Combine(directory,"summary.json"),JsonUtility.ToJson(new Report{
                    projectionVertices=profile.bodyProjection.vertices.Length,projectionBones=profile.bodyProjection.bonePaths.Length,clips=rows.ToArray()},true)+"\n");
            }
            catch(Exception error){File.WriteAllText(Path.Combine(directory,"failure.txt"),error.ToString());Debug.LogException(error);exitCode=1;}
            EditorApplication.Exit(exitCode);
        }
    }
}
