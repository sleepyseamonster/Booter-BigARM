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
    public static class AnimationPlaygroundCrouchPoseQualification
    {
        [Serializable] public sealed class Envelope
        {
            public string role;
            public Vector2 direction;
            public float movingWeight,minimumSole,maximumBodyHeight,maximumCapsuleGap,maximumTorsoRadius;
            public float maximumContactGap;
            public bool finite=true;
        }
        [Serializable] private sealed class Report
        {public string state="actual-runtime-pose-envelope-not-physical-qualified";public Envelope[] poses;}
        private static T Field<T>(object value,string name)=>(T)value.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(value);
        private static object Invoke(object value,string name)=>value.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(value,null);

        public static void MeasureFromCli()=>Run(false,false);
        public static void CalibrateGroundingFromCli()=>Run(true,false);
        public static void MeasureMidpointsFromCli()=>Run(false,true);
        public static void FinalizeShapeFromCli()
        {
            if(!Application.isBatchMode || Directory.Exists(".git") || !File.Exists(AnimationPlaygroundLayoutAuthoring.CandidateMarker))
                throw new InvalidOperationException("Finalize the unpublished shape only in the marked candidate.");
            var profile=AssetDatabase.LoadAssetAtPath<AnimationPlaygroundCrouchProfile>(AnimationPlaygroundMobilityAuthoring.CrouchProfilePath);
            if(profile.contactsQualified || profile.groundingQualified || profile.grounding.Length!=AnimationPlaygroundCrouchProfile.GroundSampleCount)
                throw new InvalidDataException("Preserve a qualified profile or finish the measured grounding draft first.");
            profile.crouchHeight=1.57f;
            foreach(var entry in profile.entries)
            {
                var keys=entry.capsuleHeight.keys;
                for(var i=0;i<keys.Length;i++)keys[i].value=Mathf.Max(keys[i].value,profile.crouchHeight);
                entry.capsuleHeight=new AnimationCurve(keys);
                for(var i=0;i<keys.Length;i++){AnimationUtility.SetKeyLeftTangentMode(entry.capsuleHeight,i,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(entry.capsuleHeight,i,AnimationUtility.TangentMode.Linear);}
            }
            if(!profile.Validate(out var reason,false))throw new InvalidDataException(reason);
            EditorUtility.SetDirty(profile);AssetDatabase.SaveAssets();EditorApplication.Exit(0);
        }
        public static AnimationPlaygroundCrouchGroundSample[] ZeroGrounding()
        {
            var rows=new AnimationPlaygroundCrouchGroundSample[AnimationPlaygroundCrouchProfile.GroundSampleCount];
            for(var i=0;i<rows.Length;i++)rows[i]=new AnimationPlaygroundCrouchGroundSample
                {directionIndex=i==0 ? -1 : (i-1)/AnimationPlaygroundCrouchProfile.GroundWeightCount,
                    weightIndex=i==0 ? 0 : (i-1)%AnimationPlaygroundCrouchProfile.GroundWeightCount+1,correction=AnimationCurve.Linear(0,0,1,0)};
            return rows;
        }
        private static void Run(bool calibrate,bool midpoints)
        {
            if(!Application.isBatchMode || Directory.Exists(".git") || !File.Exists(AnimationPlaygroundLayoutAuthoring.CandidateMarker))
                throw new InvalidOperationException("Sample actual runtime poses only in the marked candidate.");
            var directory=Path.GetFullPath(calibrate ? "MobilityQualification/GroundingFine" : midpoints ? "MobilityQualification/GroundingFineMidpoints" : "MobilityQualification/RuntimePoseGrounded");Directory.CreateDirectory(directory);
            var sourceScene=EditorSceneManager.OpenScene(TopDown3DAnimationPlaygroundSession.ScenePath,OpenSceneMode.Additive);
            var previewScene=EditorSceneManager.NewPreviewScene();GameObject actor=null;
            AnimationPlaygroundCrouchProfile profile=null;TopDown3DPlayerAnimationDriver driver=null;
            try
            {
                var session=TopDown3DAnimationPlaygroundSession.Find(sourceScene);var bindings=session.Bindings;
                profile=Object.Instantiate(bindings.crouch);profile.contactsQualified=true;profile.groundingQualified=true; // Unsaved measurement override.
                if(calibrate || profile.grounding==null || profile.grounding.Length==0)profile.grounding=ZeroGrounding();
                if(!profile.Validate(out var error))throw new InvalidDataException(error);
                actor=new GameObject("Crouch pose qualification");actor.SetActive(false);SceneManager.MoveGameObjectToScene(actor,previewScene);
                var motor=actor.AddComponent<TopDown3DPlayerMotor>();driver=actor.AddComponent<TopDown3DPlayerAnimationDriver>();
                var shape=actor.GetComponent<CapsuleCollider>();shape.height=bindings.capsuleHeight;shape.radius=bindings.capsuleRadius;shape.center=Vector3.zero;
                EditorUtility.CopySerialized(session.Player.GetComponent<TopDown3DPlayerAnimationDriver>(),driver);
                driver.ConfigureLaboratory(bindings);Invoke(driver,"Awake");
                actor.SetActive(true);
                var graph=Field<PlayableGraph>(driver,"playableGraph");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var visual=Field<GameObject>(driver,"visualInstance");var animator=driver.HumanoidAnimator;
                animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                if(!animator.isActiveAndEnabled)throw new InvalidDataException("The fixture Animator must be active before evaluating its pose.");
                var left=animator.GetBoneTransform(HumanBodyBones.LeftFoot);var right=animator.GetBoneTransform(HumanBodyBones.RightFoot);
                using var probe=new AnimationPlaygroundMotionSampler.SoleProbe(visual,left,right);
                var owner=new object();motor.BindAnimationPostureOwner(owner);
                if(!motor.TrySetAnimationPosture(owner,AnimationPlaygroundPosture.Crouched,profile.crouchHeight,profile.crouchRadius))
                    throw new InvalidDataException("The candidate fixture cannot adopt the calibrated posture.");
                if(!driver.TryBeginCrouchPresentation(owner,profile))throw new InvalidDataException("The actual pose branch could not initialize.");
                if(calibrate)
                {
                    var calibrated=ZeroGrounding();
                    for(var index=1;index<calibrated.Length;index++)
                    {
                        var sample=calibrated[index];var direction=AnimationPlaygroundCrouchProfile.GroundDirection(sample.directionIndex);
                        var weight=sample.weightIndex/(float)AnimationPlaygroundCrouchProfile.GroundWeightCount;var keys=new Keyframe[129];
                        for(var i=0;i<keys.Length;i++)
                        {
                            var phase=i/128f;var extra=0f;
                            for(var idlePhase=0;idlePhase<4;idlePhase++)
                            {
                                driver.SetCrouchPose(owner,new AnimationPlaygroundCrouchPose(direction,weight,phase,
                                    profile.Get(AnimationPlaygroundCrouchRole.Idle).clip.length*idlePhase/4f,true));
                                Invoke(driver,"PresentCrouchPose");graph.Evaluate(0);probe.MeasureSoles(out var l,out var r);
                                extra=Mathf.Max(extra,-bindings.capsuleHeight*.5f-Mathf.Min(l.y,r.y));
                            }
                            extra+=.005f*weight;
                            if(!float.IsFinite(extra) || extra>.14f)throw new InvalidDataException("Blend grounding exceeds the reviewed correction ceiling.");
                            keys[i]=new Keyframe(phase,extra);
                        }
                        keys[^1].value=keys[0].value;sample.correction=new AnimationCurve(keys);
                        for(var i=0;i<keys.Length;i++){AnimationUtility.SetKeyLeftTangentMode(sample.correction,i,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(sample.correction,i,AnimationUtility.TangentMode.Linear);}
                        File.WriteAllText(Path.Combine(directory,"calibration-progress.json"),"{\"completed\":"+index+",\"total\":"+(calibrated.Length-1)+"}\n");
                    }
                    profile.grounding=calibrated;
                }
                var rows=new List<Envelope>();
                for(var angle=0;angle<(midpoints ? AnimationPlaygroundCrouchProfile.GroundDirectionCount : 8);angle++)
                    foreach(var weight in midpoints ? new[]{.0625f,.1875f,.3125f,.4375f,.5625f,.6875f,.8125f,.9375f} : new[]{.5f,1f})
                    {
                        var direction=new Vector2(Mathf.Sin(angle*Mathf.PI/4),Mathf.Cos(angle*Mathf.PI/4));
                        if(midpoints)
                        {
                            var a=AnimationPlaygroundCrouchProfile.GroundDirection(angle);
                            var b=AnimationPlaygroundCrouchProfile.GroundDirection((angle+1)%AnimationPlaygroundCrouchProfile.GroundDirectionCount);
                            a/=Mathf.Abs(a.x)+Mathf.Abs(a.y);b/=Mathf.Abs(b.x)+Mathf.Abs(b.y);direction=(a+b).normalized;
                        }
                        var row=NewEnvelope("Direction"+angle+"Weight"+weight,direction,weight);
                        for(var i=0;i<=150;i++)
                        {
                            var phase=i/150f;driver.SetCrouchPose(owner,new AnimationPlaygroundCrouchPose(direction,weight,phase,phase*2.5f,true));
                            if(!(bool)Invoke(driver,"PresentCrouchPose"))throw new InvalidDataException("The actual pose lost ownership.");
                            graph.Evaluate(0);ReadEnvelope(row,probe,shape,actor.transform);
                        }
                        for(var marker=0;marker<4;marker++)
                        {
                            var phase=marker/4f;driver.SetCrouchPose(owner,new AnimationPlaygroundCrouchPose(direction,weight,phase,phase*2.5f,true));
                            Invoke(driver,"PresentCrouchPose");graph.Evaluate(0);probe.MeasureSoles(out var l,out var r);
                            row.maximumContactGap=Mathf.Max(row.maximumContactGap,(marker%2==0 ? r.y : l.y)+bindings.capsuleHeight*.5f);
                        }
                        rows.Add(row);
                        File.WriteAllText(Path.Combine(directory,"progress.json"),"{\"completed\":"+rows.Count+",\"total\":"+(midpoints ? 259 : 19)+"}\n");
                    }
                var idle=NewEnvelope("Idle",Vector2.zero,0);
                for(var i=0;i<=120;i++)
                {
                    driver.SetCrouchPose(owner,new AnimationPlaygroundCrouchPose(Vector2.zero,0,0,profile.Get(AnimationPlaygroundCrouchRole.Idle).clip.length*i/120f,true));
                    Invoke(driver,"PresentCrouchPose");graph.Evaluate(0);ReadEnvelope(idle,probe,shape,actor.transform);
                }
                rows.Add(idle);driver.EndCrouchPresentation(owner);
                if(idle.maximumBodyHeight>profile.crouchHeight)throw new InvalidDataException("The fixture did not evaluate the expected crouch idle envelope.");
                foreach(var role in new[]{AnimationPlaygroundCrouchRole.Enter,AnimationPlaygroundCrouchRole.Exit})
                {
                    var entry=profile.Get(role);var row=NewEnvelope(role.ToString(),Vector2.zero,0);
                    if(!driver.TryBeginAnimationPresentation(owner,entry.clip))throw new InvalidDataException("The transition pose branch could not initialize.");
                    for(var i=0;i<=150;i++)
                    {
                        var phase=i/150f;
                        if(!motor.TrySetAnimationPosture(owner,AnimationPlaygroundPosture.Crouched,entry.capsuleHeight.Evaluate(phase),profile.crouchRadius))
                            throw new InvalidDataException("The transition fixture shape was obstructed.");
                        driver.SetAnimationPresentationTime(owner,phase*entry.clip.length);driver.SetAnimationHeightCorrection(owner,entry.Correction(phase));
                        Invoke(driver,"PresentAnimationAction");graph.Evaluate(0);ReadEnvelope(row,probe,shape,actor.transform);
                    }
                    rows.Add(row);driver.EndAnimationPresentation(owner);
                }
                File.WriteAllText(Path.Combine(directory,"summary.json"),JsonUtility.ToJson(new Report{poses=rows.ToArray()},true)+"\n");
                if(calibrate)
                {
                    bindings.crouch.grounding=profile.grounding;bindings.crouch.groundingQualified=false;
                    EditorUtility.SetDirty(bindings.crouch);AssetDatabase.SaveAssets();
                    File.WriteAllText(Path.Combine(directory,"completed.json"),"{\"state\":\"measured-grounding-draft-not-physical-qualified\"}\n");
                }
            }
            catch(Exception error){File.WriteAllText(Path.Combine(directory,"failure.txt"),error.ToString());Debug.LogException(error);EditorApplication.Exit(1);return;}
            finally
            {
                if(driver!=null)Invoke(driver,"OnDestroy");
                if(actor!=null)Object.DestroyImmediate(actor);if(profile!=null)Object.DestroyImmediate(profile);
                EditorSceneManager.ClosePreviewScene(previewScene);EditorSceneManager.CloseScene(sourceScene,true);
            }
            EditorApplication.Exit(0);
        }
        private static Envelope NewEnvelope(string name,Vector2 direction,float weight)=>new(){role=name,direction=direction,movingWeight=weight,
            minimumSole=float.PositiveInfinity,maximumBodyHeight=float.NegativeInfinity,maximumCapsuleGap=float.NegativeInfinity};
        private static void ReadEnvelope(Envelope row,AnimationPlaygroundMotionSampler.SoleProbe probe,CapsuleCollider shape,Transform actor)
        {
            probe.Measure(out var left,out var right,out var minimum,out var maximum);
            var gap=probe.MeasureCapsuleGap(shape.height,actor.TransformPoint(shape.center),shape.radius);
            row.minimumSole=Mathf.Min(row.minimumSole,Mathf.Min(left.y,right.y));
            row.maximumBodyHeight=Mathf.Max(row.maximumBodyHeight,maximum.y+.91f);
            row.maximumCapsuleGap=Mathf.Max(row.maximumCapsuleGap,gap);row.maximumTorsoRadius=Mathf.Max(row.maximumTorsoRadius,probe.TorsoMaximumRadius);
            row.finite&=float.IsFinite(left.y)&&float.IsFinite(right.y)&&float.IsFinite(minimum.y)&&float.IsFinite(maximum.y)&&float.IsFinite(gap);
        }
    }
}
