using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public static class AnimationPlaygroundLowMotionPoseQualification
    {
        [Serializable] private sealed class Envelope
        {public string name;public float minimumBody=float.PositiveInfinity,maximumHeight,maximumCorrection,maximumRequiredCorrection,maximumBareCorrection,maximumPredictionError;}
        [Serializable] private sealed class Report
        {public string state="equipped-low-motion-draft-review";public bool requiresEquipmentFit;public int fullVertices,reducedVertices;public Envelope[] envelopes;}
        private static T Field<T>(object value,string name)=>(T)value.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(value);
        private static void Call(object value,string name)=>value.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(value,null);
        public static void MeasureFromCli()
        {
            if(!Application.isBatchMode || Directory.Exists(".git") || !File.Exists(AnimationPlaygroundLayoutAuthoring.CandidateMarker))
                throw new InvalidOperationException("Sample only the marked isolated candidate.");
            var directory=Path.GetFullPath("LowMotionQualification/Rigged");Directory.CreateDirectory(directory);
            var scene=EditorSceneManager.OpenScene(TopDown3DAnimationPlaygroundSession.ScenePath,OpenSceneMode.Additive);
            var preview=EditorSceneManager.NewPreviewScene();GameObject actor=null;TopDown3DPlayerAnimationDriver driver=null;
            AnimationPlaygroundFootProjection full=null,bare=null;var qualityBefore=QualitySettings.skinWeights;var exitCode=0;
            try
            {
                var bindings=TopDown3DAnimationPlaygroundSession.Find(scene).Bindings;
                var profile=AssetDatabase.LoadAssetAtPath<AnimationPlaygroundLowMotionProfile>(AnimationPlaygroundLowMotionAuthoring.ProfilePath);
                if(profile==null || profile.qualified)throw new InvalidDataException("Preserve a qualified low-motion profile.");
                actor=new GameObject("Equipped low-motion graph fixture");actor.SetActive(false);SceneManager.MoveGameObjectToScene(actor,preview);
                var motor=actor.AddComponent<TopDown3DPlayerMotor>();driver=actor.AddComponent<TopDown3DPlayerAnimationDriver>();
                var shape=actor.GetComponent<CapsuleCollider>();shape.height=bindings.capsuleHeight;shape.radius=bindings.capsuleRadius;shape.center=Vector3.zero;
                EditorUtility.CopySerialized(TopDown3DAnimationPlaygroundSession.Find(scene).Player.GetComponent<TopDown3DPlayerAnimationDriver>(),driver);
                driver.ConfigureLaboratory(bindings);Call(driver,"Awake");actor.SetActive(true);
                var graph=Field<PlayableGraph>(driver,"playableGraph");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var visual=Field<GameObject>(driver,"visualInstance");var animator=driver.HumanoidAnimator;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                var owner=new object();var idle=bindings.locomotion.GetRequiredEntry(TopDown3DLocomotionClipRole.Idle);
                driver.TryBeginAnimationPresentation(owner,idle.Clip);driver.SetAnimationPresentationTime(owner,0);
                driver.SetAnimationHeightCorrection(owner,idle.HeightCorrection(0));Call(driver,"PresentAnimationAction");graph.Evaluate(0);
                if(!driver.ConfigureCamp(TopDown3DCampSettings.Load()))throw new InvalidDataException("The camp equipment closure is missing.");
                driver.SetCampPresentation(-1,1,false,true);Call(driver,"LateUpdate");driver.EndAnimationPresentation(owner);
                Call(driver,"NormalizeCampSkinWeights");
                full=AnimationPlaygroundFootProjectionAuthoring.BuildFixture(bindings.targetModel,visual,true);
                var fullSampler=full.Bind(visual.transform);
                bare=AnimationPlaygroundFootProjectionAuthoring.Build(bindings.targetModel,true);var bareSampler=bare.Bind(visual.transform);
                // This is a raw diagnostic pass. Keep the runtime correction bound intact and measure excessive gaps explicitly.
                driver.GetType().GetField("footProjectionSampler",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(driver,null);
                using var cpu=new AnimationPlaygroundMotionSampler.SoleProbe(visual,animator.GetBoneTransform(HumanBodyBones.LeftFoot),animator.GetBoneTransform(HumanBodyBones.RightFoot));
                var rows=new List<Envelope>();var activeVertices=new HashSet<int>();
                void Sample(Envelope row)
                {
                    Call(driver,"PresentAnimationAction");graph.Evaluate(0);
                    cpu.Measure(out _,out _,out var minimum,out var maximum);
                    var predicted=fullSampler.MinimumWorldY(out var index);activeVertices.Add(index);
                    row.minimumBody=Mathf.Min(row.minimumBody,minimum.y);row.maximumHeight=Mathf.Max(row.maximumHeight,maximum.y+.91f);
                    row.maximumCorrection=Mathf.Max(row.maximumCorrection,driver.LastPoseBlendGroundCorrection);
                    row.maximumRequiredCorrection=Mathf.Max(row.maximumRequiredCorrection,Mathf.Max(0,-.91f-predicted));
                    row.maximumBareCorrection=Mathf.Max(row.maximumBareCorrection,Mathf.Max(0,-.91f-bareSampler.MinimumWorldY()));
                    row.maximumPredictionError=Mathf.Max(row.maximumPredictionError,Mathf.Abs(predicted-minimum.y));
                }
                void Pose(AnimationClip clip,float at)
                {
                    if(!driver.TryBeginAnimationPresentation(owner,clip))
                        throw new InvalidDataException("Low-motion pose ownership failed.");
                    driver.SetAnimationPresentationTime(owner,at);
                }
                void Blend(string name,AnimationClip from,float at,AnimationClip to)
                {
                    Pose(from,at);Call(driver,"PresentAnimationAction");graph.Evaluate(0);
                    if(!driver.TryCapturePoseBlend(owner,.12f) || !driver.TryReplaceAnimationPresentation(owner,to))
                        throw new InvalidDataException("Low-motion blend ownership failed.");
                    var row=new Envelope{name=name};
                    for(var i=0;i<=60;i++)
                    {
                        var elapsed=.12f*i/60;driver.SetAnimationPresentationTime(owner,Mathf.Min(to.length,elapsed));
                        driver.SetPoseBlendElapsed(owner,elapsed);Sample(row);
                    }
                    rows.Add(row);driver.EndPoseBlend(owner);driver.EndAnimationPresentation(owner);
                    File.WriteAllText(Path.Combine(directory,"progress.json"),JsonUtility.ToJson(new Report{envelopes=rows.ToArray()},true)+"\n");
                }
                foreach(var quality in new[]{SkinWeights.OneBone,SkinWeights.TwoBones,SkinWeights.FourBones})
                {
                    QualitySettings.skinWeights=quality;
                    foreach(var entry in profile.entries)
                    {
                        Pose(entry.clip,0);var row=new Envelope{name=quality+"."+entry.role};
                        for(var i=0;i<=128;i++){driver.SetAnimationPresentationTime(owner,entry.clip.length*i/128f);Sample(row);}
                        rows.Add(row);driver.EndAnimationPresentation(owner);
                    }
                    foreach(var prefix in new[]{"Slide","Roll"})
                    {
                        var first=profile.Get(prefix=="Slide" ? AnimationPlaygroundLowMotionRole.SlideEnter : AnimationPlaygroundLowMotionRole.RollContact).clip;
                        var active=profile.Get(prefix=="Slide" ? AnimationPlaygroundLowMotionRole.SlideActive : AnimationPlaygroundLowMotionRole.RollActive).clip;
                        var recover=profile.Get(prefix=="Slide" ? AnimationPlaygroundLowMotionRole.SlideRecover : AnimationPlaygroundLowMotionRole.RollRecover).clip;
                        Blend(quality+"."+prefix+".EntryToActive",first,first.length,active);
                        Blend(quality+"."+prefix+".ActiveToRecover",active,active.length,recover);
                        Blend(quality+"."+prefix+".RecoverToIdle",recover,recover.length,idle.Clip);
                        for(var phase=0;phase<4;phase++)Blend(quality+"."+prefix+".Cancel"+phase,active,active.length*phase/4,recover);
                    }
                    for(var phase=0;phase<4;phase++)
                    {
                        var sprint=bindings.locomotion.GetRequiredEntry(TopDown3DLocomotionClipRole.Sprint).Clip;
                        Blend(quality+".SprintToSlide"+phase,sprint,sprint.length*phase/4,profile.Get(AnimationPlaygroundLowMotionRole.SlideEnter).clip);
                        Blend(quality+".LandingToRoll"+phase,bindings.airborne.approach,bindings.airborne.approach.length*phase/4,
                            profile.Get(AnimationPlaygroundLowMotionRole.RollContact).clip);
                    }
                }
                // Reduction is a draft recipe. The reduced cache must pass the same graph envelopes before physical input is enabled.
                var reduced=activeVertices.OrderBy(x=>x).Select(x=>full.vertices[x]).ToArray();
                var target=profile.bodyProjection;target.model=full.model;target.bonePaths=full.bonePaths;target.vertices=reduced;
                EditorUtility.SetDirty(target);AssetDatabase.SaveAssetIfDirty(target);
                File.WriteAllText(Path.Combine(directory,"summary.json"),JsonUtility.ToJson(new Report{
                    fullVertices=full.vertices.Length,reducedVertices=reduced.Length,
                    requiresEquipmentFit=rows.Any(x=>x.maximumRequiredCorrection>.18f),envelopes=rows.ToArray()},true)+"\n");
            }
            catch(Exception error){File.WriteAllText(Path.Combine(directory,"failure.txt"),error.ToString());Debug.LogException(error);exitCode=1;}
            finally
            {
                QualitySettings.skinWeights=qualityBefore;
                if(full!=null)Object.DestroyImmediate(full);if(bare!=null)Object.DestroyImmediate(bare);
                if(driver!=null)Call(driver,"OnDestroy");if(actor!=null)Object.DestroyImmediate(actor);
                EditorSceneManager.ClosePreviewScene(preview);EditorSceneManager.CloseScene(scene,true);
            }
            EditorApplication.Exit(exitCode);
        }
    }
}
