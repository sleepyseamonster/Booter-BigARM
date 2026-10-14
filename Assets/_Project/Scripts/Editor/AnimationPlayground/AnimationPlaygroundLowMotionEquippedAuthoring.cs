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
    public static class AnimationPlaygroundLowMotionEquippedAuthoring
    {
        [Serializable] private sealed class Recipe
        {public string role,source,path;public float minimum,maximumHeight,maximumShift;}
        [Serializable] private sealed class Report {public Recipe[] clips;public float lowRadius;}
        private static T Field<T>(object value,string name)=>(T)value.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(value);
        private static void Call(object value,string name)=>value.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(value,null);
        private static AnimationCurve Linear(Keyframe[] keys)
        {
            var curve=new AnimationCurve(keys);
            for(var i=0;i<keys.Length;i++){AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.Linear);}
            return curve;
        }
        public static void CreateFromCli()
        {
            if(!Application.isBatchMode || Directory.Exists(".git") || !File.Exists(AnimationPlaygroundLayoutAuthoring.CandidateMarker))
                throw new InvalidOperationException("Author equipped poses only in the marked candidate.");
            var directory=Path.GetFullPath("LowMotionQualification/Equipped");Directory.CreateDirectory(directory);
            var scene=EditorSceneManager.OpenScene(TopDown3DAnimationPlaygroundSession.ScenePath,OpenSceneMode.Additive);
            var preview=EditorSceneManager.NewPreviewScene();GameObject actor=null;TopDown3DPlayerAnimationDriver driver=null;
            AnimationPlaygroundLowMotionProfile profile=null;AnimationPlaygroundFootProjection projection=null;var before=QualitySettings.skinWeights;var exitCode=0;
            try
            {
                if(File.Exists(AnimationPlaygroundLowMotionFitting.ProfilePath) || File.Exists(AnimationPlaygroundLowMotionFitting.ProjectionPath))
                    throw new IOException("Preserve prior fitted assets.");
                var session=TopDown3DAnimationPlaygroundSession.Find(scene);var bindings=session.Bindings;
                profile=Object.Instantiate(bindings.lowMotion);profile.name="LowMotionFittedProfile";profile.qualified=false;profile.garmentFloorQualified=true;
                actor=new GameObject("Equipped derivative fixture");actor.SetActive(false);SceneManager.MoveGameObjectToScene(actor,preview);
                var motor=actor.AddComponent<TopDown3DPlayerMotor>();driver=actor.AddComponent<TopDown3DPlayerAnimationDriver>();
                var shape=actor.GetComponent<CapsuleCollider>();shape.height=bindings.capsuleHeight;shape.radius=bindings.capsuleRadius;shape.center=Vector3.zero;
                EditorUtility.CopySerialized(session.Player.GetComponent<TopDown3DPlayerAnimationDriver>(),driver);
                driver.ConfigureLaboratory(bindings);Call(driver,"Awake");actor.SetActive(true);
                var graph=Field<PlayableGraph>(driver,"playableGraph");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var visual=Field<GameObject>(driver,"visualInstance");var animator=driver.HumanoidAnimator;var owner=new object();
                var idle=bindings.locomotion.GetRequiredEntry(TopDown3DLocomotionClipRole.Idle);
                driver.TryBeginAnimationPresentation(owner,idle.Clip);driver.SetAnimationPresentationTime(owner,0);
                driver.SetAnimationHeightCorrection(owner,idle.HeightCorrection(0));Call(driver,"PresentAnimationAction");graph.Evaluate(0);
                if(!driver.ConfigureCamp(TopDown3DCampSettings.Load()))throw new InvalidDataException("Equipment is missing.");
                driver.SetCampPresentation(-1,1,false,true);Call(driver,"LateUpdate");driver.EndAnimationPresentation(owner);Call(driver,"NormalizeCampSkinWeights");
                projection=AnimationPlaygroundFootProjectionAuthoring.BuildFixture(bindings.targetModel,visual,true,true);
                using var probe=new AnimationPlaygroundMotionSampler.SoleProbe(visual,animator.GetBoneTransform(HumanBodyBones.LeftFoot),animator.GetBoneTransform(HumanBodyBones.RightFoot),true);
                var rows=new List<Recipe>();var lowMaximum=0f;
                foreach(var entry in profile.entries)
                {
                    var source=AssetDatabase.GetAssetPath(entry.clip);
                    var clip=AnimationPlaygroundClipDerivatives.CreateRange(source,entry.role+"EquippedGrounded",0,entry.clip.length);
                    var root=AnimationUtility.GetCurveBindings(clip).Single(x=>x.propertyName=="RootT.y");var intervals=Mathf.CeilToInt(clip.length*120);
                    var maximumShift=0f;
                    for(var pass=0;pass<3;pass++)
                    {
                        var correction=new float[intervals+1];
                        foreach(var quality in new[]{SkinWeights.OneBone,SkinWeights.TwoBones,SkinWeights.FourBones})
                        {
                            QualitySettings.skinWeights=quality;driver.TryBeginAnimationPresentation(owner,clip);
                            for(var i=0;i<=intervals;i++)
                            {
                                driver.SetAnimationPresentationTime(owner,clip.length*i/intervals);Call(driver,"PresentAnimationAction");graph.Evaluate(0);
                                probe.Measure(out _,out _,out var minimum,out _);correction[i]=Mathf.Max(correction[i],-.908f-minimum.y);
                            }
                            driver.EndAnimationPresentation(owner);
                        }
                        var curve=AnimationUtility.GetEditorCurve(clip,root);var keys=new Keyframe[intervals+1];
                        for(var i=0;i<=intervals;i++)
                        {
                            var time=clip.length*i/intervals;var shift=Mathf.Max(0,correction[i]);maximumShift=Mathf.Max(maximumShift,shift);
                            keys[i]=new Keyframe(time,curve.Evaluate(time)+shift/.9894466f);
                        }
                        AnimationUtility.SetEditorCurve(clip,root,Linear(keys));EditorUtility.SetDirty(clip);AssetDatabase.SaveAssetIfDirty(clip);
                    }
                    var height=new float[intervals+1];var minimumBody=float.PositiveInfinity;
                    foreach(var quality in new[]{SkinWeights.OneBone,SkinWeights.TwoBones,SkinWeights.FourBones})
                    {
                        QualitySettings.skinWeights=quality;driver.TryBeginAnimationPresentation(owner,clip);
                        for(var i=0;i<=intervals;i++)
                        {
                            driver.SetAnimationPresentationTime(owner,clip.length*i/intervals);Call(driver,"PresentAnimationAction");graph.Evaluate(0);
                            probe.Measure(out _,out _,out var minimum,out var maximum);minimumBody=Mathf.Min(minimumBody,minimum.y);
                            height[i]=Mathf.Max(height[i],maximum.y+.91f);
                        }
                        driver.EndAnimationPresentation(owner);
                    }
                    var shapeKeys=new Keyframe[height.Length];
                    for(var i=0;i<height.Length;i++)shapeKeys[i]=new Keyframe(i/(float)intervals,Mathf.Clamp(height[i]+.035f,profile.verticalRadius*2,profile.standingHeight));
                    if(entry.role==AnimationPlaygroundLowMotionRole.SlideEnter || entry.role==AnimationPlaygroundLowMotionRole.RollContact)shapeKeys[0].value=profile.standingHeight;
                    if(entry.role==AnimationPlaygroundLowMotionRole.SlideRecover || entry.role==AnimationPlaygroundLowMotionRole.RollRecover)shapeKeys[^1].value=profile.standingHeight;
                    entry.clip=clip;entry.capsuleHeight=Linear(shapeKeys);if(entry.horizontal)lowMaximum=Mathf.Max(lowMaximum,height.Max());
                    rows.Add(new Recipe{role=entry.role.ToString(),source=source,path=AssetDatabase.GetAssetPath(clip),minimum=minimumBody,
                        maximumHeight=height.Max(),maximumShift=maximumShift});
                    File.WriteAllText(Path.Combine(directory,"progress.json"),JsonUtility.ToJson(new Report{clips=rows.ToArray()},true)+"\n");
                }
                profile.lowRadius=Mathf.Max(profile.lowRadius,(lowMaximum+.04f)*.5f);profile.bodyProjection=projection;
                if(!profile.Validate(out var reason,false))throw new InvalidDataException(reason);
                AssetDatabase.CreateAsset(projection,AnimationPlaygroundLowMotionFitting.ProjectionPath);projection=null;
                AssetDatabase.CreateAsset(profile,AnimationPlaygroundLowMotionFitting.ProfilePath);var radius=profile.lowRadius;profile=null;AssetDatabase.SaveAssets();
                File.WriteAllText(Path.Combine(directory,"summary.json"),JsonUtility.ToJson(new Report{clips=rows.ToArray(),lowRadius=radius},true)+"\n");
            }
            catch(Exception error){File.WriteAllText(Path.Combine(directory,"failure.txt"),error.ToString());Debug.LogException(error);exitCode=1;}
            finally
            {
                QualitySettings.skinWeights=before;if(profile!=null)Object.DestroyImmediate(profile);if(projection!=null)Object.DestroyImmediate(projection);
                if(driver!=null)Call(driver,"OnDestroy");if(actor!=null)Object.DestroyImmediate(actor);
                EditorSceneManager.ClosePreviewScene(preview);EditorSceneManager.CloseScene(scene,true);
            }
            EditorApplication.Exit(exitCode);
        }
    }
}
