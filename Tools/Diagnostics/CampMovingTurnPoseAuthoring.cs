using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Object=UnityEngine.Object;

public static class CampMovingTurnPoseAuthoring
{
    public static string Bake(string receipt)
    {
        if(!Application.isBatchMode || Directory.Exists(".git") || !File.Exists(".animation-playground-validation-project")
            || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || File.Exists(receipt))throw new InvalidOperationException("Fresh marked offline torso-pose extraction required.");
        var profile=AssetDatabase.LoadAssetAtPath<AnimationPlaygroundMovingTurnProfile>("Assets/_Project/Settings/AnimationPlayground/PrivateBindings/MovingTurnReview.asset");var policy=profile.forwardProfile;
        if(profile.bands.Any(b=>b.leftPose!=null && b.leftPose.samples>0 || b.rightPose!=null && b.rightPose.samples>0))throw new IOException("Preserve existing torso-pose tapes.");
        var bindings=AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>("Assets/_Project/Settings/AnimationPlayground/PrivateBindings/PlayerBindings.asset");
        var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();var model=Object.Instantiate(policy.targetModel);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(model,scene);
        model.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);model.transform.localScale=Vector3.one*bindings.visualScale;
        var animator=model.GetComponent<Animator>() ?? model.AddComponent<Animator>();animator.avatar=bindings.targetAvatar;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        var graph=PlayableGraph.Create("Offline moving-turn torso tape");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);var output=AnimationPlayableOutput.Create(graph,"Offline torso",animator);graph.Play();
        try {
        profile.upperBodyBones=new[]{HumanBodyBones.Spine,HumanBodyBones.Chest,HumanBodyBones.UpperChest,HumanBodyBones.Neck,HumanBodyBones.Head,
            HumanBodyBones.LeftShoulder,HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,
            HumanBodyBones.RightShoulder,HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand}.Where(b=>animator.GetBoneTransform(b)!=null).ToArray();
        var bones=profile.upperBodyBones.Select(animator.GetBoneTransform).ToArray();var records=new List<object>();
        for(var band=0;band<5;band++)foreach(var right in new[]{false,true})
        {
            var gait=right?profile.bands[band].right:profile.bands[band].left;var count=Mathf.CeilToInt(gait.sourceClip.length*120);
            var tape=new AnimationPlaygroundMovingTurnBonePose{sourceClip=gait.sourceClip,samples=count+1,rotations=new Quaternion[(count+1)*bones.Length]};
            var playable=AnimationClipPlayable.Create(graph,gait.sourceClip);playable.SetSpeed(0);playable.SetApplyFootIK(false);playable.SetApplyPlayableIK(false);output.SetSourcePlayable(playable);
            for(var sample=0;sample<=count;sample++)
            {
                playable.SetTime(gait.SourceSeconds(sample==count?0:sample/(double)count));graph.Evaluate(0);
                for(var bone=0;bone<bones.Length;bone++)tape.rotations[sample*bones.Length+bone]=bones[bone].localRotation.normalized;
            }
            if(right)profile.bands[band].rightPose=tape;else profile.bands[band].leftPose=tape;
            profile.bands[band].maximumWeight=band==0?.45f:.5f;
            profile.bands[band].provenance+=" Final runtime uses allowed upper-bone local rotation tapes only; accepted root/hips/legs, gait clock, height and terrain output remain exact. Source root lean is carried into spine/chest. Action snapshots carry the displayed local rotation delta and fade it with the existing pose blend.";
            output.SetSourcePlayable(Playable.Null);graph.DestroyPlayable(playable);records.Add(new{band,right,source=AssetDatabase.GetAssetPath(gait.sourceClip),tape.samples,boneCount=bones.Length});
        }
        if(!profile.Validate(policy,out var reason))throw new InvalidDataException(reason);EditorUtility.SetDirty(profile);AssetDatabase.SaveAssetIfDirty(profile);
        profile.name="MovingTurnReview";EditorUtility.SetDirty(profile);AssetDatabase.SaveAssetIfDirty(profile);
        File.WriteAllText(receipt,Newtonsoft.Json.JsonConvert.SerializeObject(new{bones=profile.upperBodyBones.Select(b=>b.ToString()).ToArray(),records},Newtonsoft.Json.Formatting.Indented));return receipt;
        } finally {graph.Destroy();Object.DestroyImmediate(model);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
    }
}
