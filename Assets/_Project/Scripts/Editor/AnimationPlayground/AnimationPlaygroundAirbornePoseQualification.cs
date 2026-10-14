using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace BooterBigArm.Editor
{
    public static class AnimationPlaygroundAirbornePoseQualification
    {
        [Serializable] private sealed class Envelope
        {public string name;public float minimumSole=float.PositiveInfinity,maximumHeight,maximumProjection;}
        [Serializable] private sealed class Report {public string state="airborne-rigged-pose-review";public Envelope[] envelopes;}
        private static T Field<T>(object value,string name)=>(T)value.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(value);
        private static void Call(object value,string name)=>value.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(value,null);
        public static void MeasureFromCli()
        {
            if(!Application.isBatchMode || Directory.Exists(".git") || !File.Exists(AnimationPlaygroundLayoutAuthoring.CandidateMarker))
                throw new InvalidOperationException("Sample the graph only in the marked candidate.");
            var directory=Path.GetFullPath("AirborneQualification/Rigged");Directory.CreateDirectory(directory);
            var scene=EditorSceneManager.OpenScene(TopDown3DAnimationPlaygroundSession.ScenePath,OpenSceneMode.Additive);
            var preview=EditorSceneManager.NewPreviewScene();GameObject actor=null;TopDown3DPlayerAnimationDriver driver=null;var exitCode=0;
            var originalQuality=QualitySettings.skinWeights;
            try
            {
                var session=TopDown3DAnimationPlaygroundSession.Find(scene);var bindings=session.Bindings;var profile=bindings.airborne;
                actor=new GameObject("Rigged airborne fixture");actor.SetActive(false);SceneManager.MoveGameObjectToScene(actor,preview);
                var motor=actor.AddComponent<TopDown3DPlayerMotor>();driver=actor.AddComponent<TopDown3DPlayerAnimationDriver>();
                var shape=actor.GetComponent<CapsuleCollider>();shape.height=bindings.capsuleHeight;shape.radius=bindings.capsuleRadius;shape.center=Vector3.zero;
                EditorUtility.CopySerialized(session.Player.GetComponent<TopDown3DPlayerAnimationDriver>(),driver);
                driver.ConfigureLaboratory(bindings);Call(driver,"Awake");actor.SetActive(true);
                var graph=Field<PlayableGraph>(driver,"playableGraph");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var visual=Field<GameObject>(driver,"visualInstance");var animator=driver.HumanoidAnimator;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                var owner=new object();var idle=bindings.locomotion.GetRequiredEntry(TopDown3DLocomotionClipRole.Idle);
                driver.TryBeginAnimationPresentation(owner,idle.Clip);driver.SetAnimationPresentationTime(owner,0);
                driver.SetAnimationHeightCorrection(owner,idle.HeightCorrection(0));Call(driver,"PresentAnimationAction");graph.Evaluate(0);
                if(!driver.ConfigureCamp(TopDown3DCampSettings.Load()))throw new InvalidDataException("Hydrate the camp equipment before sampling.");
                driver.SetCampPresentation(-1,1,false,true);Call(driver,"LateUpdate");driver.EndAnimationPresentation(owner);
                using var probe=new AnimationPlaygroundMotionSampler.SoleProbe(visual,animator.GetBoneTransform(HumanBodyBones.LeftFoot),animator.GetBoneTransform(HumanBodyBones.RightFoot));
                var rows=new List<Envelope>();
                void Measure(Envelope row)
                {
                    Call(driver,"PresentAnimationAction");graph.Evaluate(0);probe.Measure(out var l,out var r,out _,out var maximum);
                    row.minimumSole=Mathf.Min(row.minimumSole,Mathf.Min(l.y,r.y));row.maximumHeight=Mathf.Max(row.maximumHeight,maximum.y+bindings.capsuleHeight*.5f);
                    row.maximumProjection=Mathf.Max(row.maximumProjection,driver.LastPoseBlendGroundCorrection);
                }
                void Pose(AnimationClip clip,float time,bool grounded)
                {
                    if(!driver.TryBeginAnimationPresentation(owner,clip))throw new InvalidDataException("Pose ownership failed.");
                    driver.SetAnimationPresentationTime(owner,time);driver.SetAnimationFootProjection(owner,grounded);
                }
                void Blend(string name,AnimationClip from,float at,AnimationClip to,bool grounded,float timeRate=1,bool sourceGrounded=false,bool nested=false)
                {
                    Pose(from,at,sourceGrounded);Call(driver,"PresentAnimationAction");graph.Evaluate(0);
                    if(!driver.TryCapturePoseBlend(owner,.12f))throw new InvalidDataException("Snapshot ownership failed.");
                    if(!driver.TryReplaceAnimationPresentation(owner,to))throw new InvalidDataException("Stage replacement failed.");
                    driver.SetAnimationFootProjection(owner,grounded);var row=new Envelope{name=name};
                    if(nested)
                    {
                        driver.SetAnimationPresentationTime(owner,Mathf.Min(to.length,.08f));driver.SetPoseBlendElapsed(owner,.03);
                        Measure(row);
                        if(!driver.TryCapturePoseBlend(owner,.12f) || !driver.TryReplaceAnimationPresentation(owner,profile.hardLanding))
                            throw new InvalidDataException("Nested landing replacement failed.");
                        to=profile.hardLanding;driver.SetAnimationFootProjection(owner,true);
                    }
                    for(var i=0;i<=60;i++)
                    {
                        var elapsed=.12f*i/60;driver.SetAnimationPresentationTime(owner,Mathf.Min(to.length,elapsed*timeRate));
                        driver.SetPoseBlendElapsed(owner,elapsed);Measure(row);
                    }
                    rows.Add(row);driver.EndPoseBlend(owner);driver.EndAnimationPresentation(owner);
                }
                foreach(var quality in new[]{SkinWeights.OneBone,SkinWeights.TwoBones,SkinWeights.FourBones})
                {
                    QualitySettings.skinWeights=quality;
                    foreach(var variant in new[]{profile.standing,profile.running,profile.unarmed})
                    {
                        foreach(var clip in new[]{variant.takeoff,variant.rising,variant.landing})
                        {
                            var grounded=clip!=variant.rising;Pose(clip,0,grounded);var row=new Envelope{name=quality+"."+clip.name};
                            for(var i=0;i<=128;i++){driver.SetAnimationPresentationTime(owner,clip.length*i/128f);Measure(row);}
                            rows.Add(row);driver.EndAnimationPresentation(owner);
                        }
                        Blend(quality+"."+variant.sourceId+".IdleToTakeoff",idle.Clip,0,variant.takeoff,true,sourceGrounded:true);
                        Blend(quality+"."+variant.sourceId+".TakeoffToRising",variant.takeoff,variant.takeoff.length,variant.rising,false,9.81f/variant.upwardSpeed*variant.rising.length,true);
                        Blend(quality+"."+variant.sourceId+".RisingToFall",variant.rising,variant.rising.length,profile.falling,false);
                        Blend(quality+"."+variant.sourceId+".LandingToIdle",variant.landing,variant.landing.length,idle.Clip,true,sourceGrounded:true);
                    }
                    foreach(var role in new[]{TopDown3DLocomotionClipRole.Walk,TopDown3DLocomotionClipRole.Run,TopDown3DLocomotionClipRole.Sprint,
                        TopDown3DLocomotionClipRole.DirectionalLeft,TopDown3DLocomotionClipRole.DirectionalRight})
                        for(var phase=0;phase<4;phase++)
                        {
                            var clip=bindings.locomotion.GetRequiredEntry(role).Clip;
                            Blend(quality+"."+role+".ToRunningTakeoff"+phase,clip,clip.length*phase/4,profile.running.takeoff,true,sourceGrounded:true);
                        }
                    foreach(var clip in new[]{profile.falling,profile.approach,profile.hardLanding})
                    {
                        Pose(clip,0,clip!=profile.falling);var row=new Envelope{name=quality+"."+clip.name};
                        for(var i=0;i<=128;i++){driver.SetAnimationPresentationTime(owner,clip.length*i/128f);Measure(row);}
                        rows.Add(row);driver.EndAnimationPresentation(owner);
                    }
                    for(var phase=0;phase<4;phase++)
                    {
                        Blend(quality+".FallToApproach"+phase,profile.falling,profile.falling.length*phase/4,profile.approach,true);
                        Blend(quality+".FallToHardLanding"+phase,profile.falling,profile.falling.length*phase/4,profile.hardLanding,true);
                        Blend(quality+".NestedApproachToLanding"+phase,profile.falling,profile.falling.length*phase/4,profile.approach,true,nested:true);
                    }
                    Blend(quality+".ApproachToHardLanding",profile.approach,profile.approach.length,profile.hardLanding,true);
                }
                File.WriteAllText(Path.Combine(directory,"summary.json"),JsonUtility.ToJson(new Report{envelopes=rows.ToArray()},true)+"\n");
            }
            catch(Exception error){File.WriteAllText(Path.Combine(directory,"failure.txt"),error.ToString());Debug.LogException(error);exitCode=1;}
            finally
            {
                QualitySettings.skinWeights=originalQuality;
                if(driver!=null)Call(driver,"OnDestroy");if(actor!=null)Object.DestroyImmediate(actor);
                EditorSceneManager.ClosePreviewScene(preview);EditorSceneManager.CloseScene(scene,true);
            }
            EditorApplication.Exit(exitCode);
        }
    }
}
