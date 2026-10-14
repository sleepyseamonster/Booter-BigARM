using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace BooterBigArm.Editor
{
    public static class AnimationPlaygroundCrouchBoundaryAuthoring
    {
        [Serializable] public sealed class Boundary
        {
            public string name;
            public float feet,hips,head,hands;
        }
        [Serializable] private sealed class Report { public string state="measured-boundary-gaps"; public Boundary[] boundaries; }
        [Serializable] public sealed class BlendEnvelope
        {public string name;public float minimumSole=float.PositiveInfinity,maximumBodyHeight,maximumCapsuleGap=float.NegativeInfinity;}
        [Serializable] private sealed class BlendReport{public string state="runtime-blend-envelope-not-physical-qualified";public BlendEnvelope[] blends;}
        public static void MeasureRuntimeBlendsFromCli()
        {
            if(!Application.isBatchMode || Directory.Exists(".git") || !File.Exists(AnimationPlaygroundLayoutAuthoring.CandidateMarker))
                throw new InvalidOperationException("Measure the runtime blend only in the marked candidate.");
            var directory=Path.GetFullPath("MobilityQualification/RuntimeBoundaries");Directory.CreateDirectory(directory);
            var sourceScene=EditorSceneManager.OpenScene(TopDown3DAnimationPlaygroundSession.ScenePath,OpenSceneMode.Additive);
            var preview=EditorSceneManager.NewPreviewScene();GameObject actor=null;TopDown3DPlayerAnimationDriver driver=null;
            try
            {
                var session=TopDown3DAnimationPlaygroundSession.Find(sourceScene);var bindings=session.Bindings;var profile=bindings.crouch;
                actor=new GameObject("Runtime boundary fixture");actor.SetActive(false);SceneManager.MoveGameObjectToScene(actor,preview);
                var motor=actor.AddComponent<TopDown3DPlayerMotor>();driver=actor.AddComponent<TopDown3DPlayerAnimationDriver>();
                var shape=actor.GetComponent<CapsuleCollider>();shape.height=bindings.capsuleHeight;shape.radius=bindings.capsuleRadius;shape.center=Vector3.zero;
                EditorUtility.CopySerialized(session.Player.GetComponent<TopDown3DPlayerAnimationDriver>(),driver);
                driver.ConfigureLaboratory(bindings);Call(driver,"Awake");actor.SetActive(true);
                var graph=Get<PlayableGraph>(driver,"playableGraph");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var visual=Get<GameObject>(driver,"visualInstance");var animator=driver.HumanoidAnimator;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                using var probe=new AnimationPlaygroundMotionSampler.SoleProbe(visual,animator.GetBoneTransform(HumanBodyBones.LeftFoot),animator.GetBoneTransform(HumanBodyBones.RightFoot));
                var standing=bindings.locomotion.GetRequiredEntry(TopDown3DLocomotionClipRole.Idle);
                var idle=profile.Get(AnimationPlaygroundCrouchRole.Idle);var enter=profile.Get(AnimationPlaygroundCrouchRole.Enter);var exit=profile.Get(AnimationPlaygroundCrouchRole.Exit);
                var rows=new List<BlendEnvelope>();var owner=new object();motor.BindAnimationPostureOwner(owner);
                for(var phase=0;phase<4;phase++)
                    for(var boundary=0;boundary<4;boundary++)
                    {
                        var source=boundary==0 ? standing.Clip : boundary==1 ? enter.clip : boundary==2 ? idle.clip : exit.clip;
                        var sourceTime=boundary==0 ? source.length*phase/4f : boundary==1 ? source.length : boundary==2 ? source.length*phase/4f : source.length;
                        var target=boundary==0 ? enter.clip : boundary==1 ? idle.clip : boundary==2 ? exit.clip : standing.Clip;
                        var sourceCorrection=boundary==0 ? standing.HeightCorrection(phase/4f) : boundary==1 ? enter.Correction(1) : boundary==2 ? idle.Correction(phase/4f) : exit.Correction(1);
                        if(!driver.TryBeginAnimationPresentation(owner,source))throw new InvalidDataException("Source pose ownership failed.");
                        driver.SetAnimationPresentationTime(owner,sourceTime);driver.SetAnimationHeightCorrection(owner,sourceCorrection);Call(driver,"PresentAnimationAction");graph.Evaluate(0);
                        if(!driver.TryCapturePoseBlend(owner,.18f))throw new InvalidDataException("Source snapshot failed.");
                        driver.EndAnimationPresentation(owner);
                        if(!driver.TryBeginAnimationPresentation(owner,target))throw new InvalidDataException("Target ownership failed.");
                        var row=new BlendEnvelope{name="Boundary"+boundary+"Phase"+phase};
                        for(var i=0;i<=120;i++)
                        {
                            var elapsed=.18f*i/120;var targetTime=boundary==0 || boundary==2 ? elapsed : 0;
                            var correction=boundary==0 ? enter.Correction(targetTime/target.length) : boundary==1 ? idle.Correction(0)
                                : boundary==2 ? exit.Correction(targetTime/target.length) : standing.HeightCorrection(0);
                            if(boundary==3)motor.TryRestoreAnimationPosture(owner);
                            else motor.TrySetAnimationPosture(owner,AnimationPlaygroundPosture.Crouched,
                                boundary==0 ? enter.capsuleHeight.Evaluate(targetTime/target.length) : boundary==2 ? exit.capsuleHeight.Evaluate(targetTime/target.length) : profile.crouchHeight,
                                profile.crouchRadius);
                            driver.SetAnimationPresentationTime(owner,targetTime);driver.SetAnimationHeightCorrection(owner,correction);driver.SetPoseBlendElapsed(owner,elapsed);
                            Call(driver,"PresentAnimationAction");graph.Evaluate(0);probe.Measure(out var l,out var r,out _,out var maximum);
                            row.minimumSole=Mathf.Min(row.minimumSole,Mathf.Min(l.y,r.y));row.maximumBodyHeight=Mathf.Max(row.maximumBodyHeight,maximum.y+bindings.capsuleHeight*.5f);
                            row.maximumCapsuleGap=Mathf.Max(row.maximumCapsuleGap,probe.MeasureCapsuleGap(shape.height,actor.transform.TransformPoint(shape.center),shape.radius));
                        }
                        rows.Add(row);driver.EndPoseBlend(owner);driver.EndAnimationPresentation(owner);
                    }
                File.WriteAllText(Path.Combine(directory,"summary.json"),JsonUtility.ToJson(new BlendReport{blends=rows.ToArray()},true)+"\n");
            }
            catch(Exception error){File.WriteAllText(Path.Combine(directory,"failure.txt"),error.ToString());Debug.LogException(error);EditorApplication.Exit(1);return;}
            finally
            {if(driver!=null)Call(driver,"OnDestroy");if(actor!=null)Object.DestroyImmediate(actor);EditorSceneManager.ClosePreviewScene(preview);EditorSceneManager.CloseScene(sourceScene,true);}
            EditorApplication.Exit(0);
        }
        private static T Get<T>(object value,string name)=>(T)value.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(value);
        private static object Call(object value,string name)=>value.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(value,null);
        public static void MeasureFromCli()
        {
            if(!Application.isBatchMode || Directory.Exists(".git") || !File.Exists(AnimationPlaygroundLayoutAuthoring.CandidateMarker))
                throw new InvalidOperationException("Measure boundaries only in the marked validation copy.");
            var directory=Path.GetFullPath("MobilityQualification/Boundaries");Directory.CreateDirectory(directory);
            try
            {
                var bindings=AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(AnimationPlaygroundPlayerAuthoring.BindingPath);
                var model=AssetDatabase.GetAssetPath(bindings.targetModel);
                var standing=bindings.locomotion.GetRequiredEntry(TopDown3DLocomotionClipRole.Idle);
                var standingFrames=AnimationPlaygroundMotionSampler.Measure(model,AssetDatabase.GetAssetPath(standing.Clip),bindings.visualScale,bindings.visualOffset,60,
                    false,StandingCorrection(standing)).frames;
                var profile=bindings.crouch;
                var idle=Read(model,profile.Get(AnimationPlaygroundCrouchRole.Idle),bindings);
                var enter=Read(model,profile.Get(AnimationPlaygroundCrouchRole.Enter),bindings);
                var exit=Read(model,profile.Get(AnimationPlaygroundCrouchRole.Exit),bindings);
                var rows=new List<Boundary>();
                for(var i=0;i<4;i++)
                {
                    var currentIdle=idle[Mathf.RoundToInt((idle.Length-1)*i/4f)];
                    var currentStanding=standingFrames[Mathf.RoundToInt((standingFrames.Length-1)*i/4f)];
                    rows.Add(Gap("Standing"+i+"ToEnter",currentStanding,enter[0]));
                    rows.Add(Gap("EnterToIdle"+i,enter[^1],currentIdle));
                    rows.Add(Gap("Idle"+i+"ToExit",currentIdle,exit[0]));
                    rows.Add(Gap("ExitToStanding"+i,exit[^1],currentStanding));
                }
                File.WriteAllText(Path.Combine(directory,"summary.json"),JsonUtility.ToJson(new Report{boundaries=rows.ToArray()},true)+"\n");EditorApplication.Exit(0);
            }
            catch(Exception error){File.WriteAllText(Path.Combine(directory,"failure.txt"),error.ToString());Debug.LogException(error);EditorApplication.Exit(1);}
        }
        private static AnimationPlaygroundMotionSampler.PoseFrame[] Read(string model,AnimationPlaygroundCrouchEntry entry,TopDown3DAnimationPlaygroundBindings bindings)
            =>AnimationPlaygroundMotionSampler.Measure(model,AssetDatabase.GetAssetPath(entry.clip),bindings.visualScale,bindings.visualOffset,60,false,entry.heightCorrection).frames;
        private static AnimationCurve StandingCorrection(TopDown3DLocomotionClipEntry entry)
        {
            var keys=new Keyframe[129];for(var i=0;i<keys.Length;i++)keys[i]=new Keyframe(i/128f,entry.HeightCorrection(i/128f));
            return new AnimationCurve(keys);
        }
        private static Boundary Gap(string name,AnimationPlaygroundMotionSampler.PoseFrame a,AnimationPlaygroundMotionSampler.PoseFrame b)
            =>new(){name=name,feet=Mathf.Max(Vector3.Distance(a.leftFoot,b.leftFoot),Vector3.Distance(a.rightFoot,b.rightFoot)),
                hips=Vector3.Distance(a.hips,b.hips),head=Vector3.Distance(a.head,b.head),
                hands=Mathf.Max(Vector3.Distance(a.leftHand,b.leftHand),Vector3.Distance(a.rightHand,b.rightHand))};
    }
}
