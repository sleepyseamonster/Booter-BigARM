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
    public static class AnimationPlaygroundLowMotionFitting
    {
        public const string ProfilePath="Assets/_Project/Settings/AnimationPlayground/PrivateBindings/LowMotionFittedProfile.asset";
        public const string ProjectionPath="Assets/_Project/Settings/AnimationPlayground/PrivateBindings/BodyProjectionFitted.asset";
        [Serializable] private sealed class Envelope
        {public string name;public float minimumBody=float.PositiveInfinity,minimumGarment=float.PositiveInfinity,maximumBodyCorrection,maximumGarmentDisplacement,maximumPredictionError,maximumReducedError;public bool exceeded;}
        [Serializable] private sealed class Report
        {public string state="fitted-low-motion-review";public bool cacheExpanded;public int fullVertices,reducedVertices;public Envelope[] envelopes;}
        private static T Field<T>(object value,string name)=>(T)value.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(value);
        private static void Set(object value,string name,object field)=>value.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(value,field);
        private static void Call(object value,string name)=>value.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(value,null);
        public static void MeasureFromCli()=>Measure(false);
        public static void VerifyReducedFromCli()=>Measure(true);
        public static void EnableFittedFromCli()
        {
            if(!Application.isBatchMode || Directory.Exists(".git") || !File.Exists(AnimationPlaygroundLayoutAuthoring.CandidateMarker))
                throw new InvalidOperationException("Enable only the marked isolated candidate.");
            var directory=Path.GetFullPath("LowMotionQualification/FittedEnabled");Directory.CreateDirectory(directory);var exitCode=0;
            try
            {
                var report=JsonUtility.FromJson<Report>(File.ReadAllText("LowMotionQualification/FittedReduced/summary.json"));
                if(report.cacheExpanded || report.envelopes.Length!=108 || report.envelopes.Any(x=>x.exceeded || x.minimumBody<-.9103f || x.minimumGarment<-.9003f
                    || x.maximumBodyCorrection>.18f || x.maximumGarmentDisplacement>.6f || x.maximumPredictionError>.00001f || x.maximumReducedError>.0003f))
                    throw new InvalidDataException("The fitted source/cache envelope is incomplete.");
                var profile=AssetDatabase.LoadAssetAtPath<AnimationPlaygroundLowMotionProfile>(ProfilePath);
                var bindings=AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(AnimationPlaygroundPlayerAuthoring.BindingPath);
                if(profile==null || profile.qualified || !profile.garmentFloorQualified || !profile.Validate(out _,false))
                    throw new InvalidDataException("Preserve a qualified or mismatched fitted profile.");
                profile.qualified=true;bindings.lowMotion=profile;
                if(!bindings.ValidatePlayer(out var reason))throw new InvalidDataException(reason);
                EditorUtility.SetDirty(profile);EditorUtility.SetDirty(bindings);AssetDatabase.SaveAssets();
                File.WriteAllText(Path.Combine(directory,"completed.json"),"{\"state\":\"fitted-low-motion-enabled-in-candidate\"}\n");
            }
            catch(Exception error){File.WriteAllText(Path.Combine(directory,"failure.txt"),error.ToString());Debug.LogException(error);exitCode=1;}
            EditorApplication.Exit(exitCode);
        }
        private static void Measure(bool dense)
        {
            if(!Application.isBatchMode || Directory.Exists(".git") || !File.Exists(AnimationPlaygroundLayoutAuthoring.CandidateMarker))
                throw new InvalidOperationException("Fit only the marked isolated candidate.");
            var directory=Path.GetFullPath(dense ? "LowMotionQualification/FittedReduced" : "LowMotionQualification/Fitted");Directory.CreateDirectory(directory);
            var scene=EditorSceneManager.OpenScene(TopDown3DAnimationPlaygroundSession.ScenePath,OpenSceneMode.Additive);
            var preview=EditorSceneManager.NewPreviewScene();GameObject actor=null;TopDown3DPlayerAnimationDriver driver=null;
            AnimationPlaygroundFootProjection full=null;AnimationPlaygroundLowMotionProfile profile=null;var before=QualitySettings.skinWeights;var exitCode=0;
            Mesh baked=null;
            try
            {
                var session=TopDown3DAnimationPlaygroundSession.Find(scene);var bindings=session.Bindings;
                profile=AssetDatabase.LoadAssetAtPath<AnimationPlaygroundLowMotionProfile>(ProfilePath);
                if(profile==null || profile.qualified)throw new InvalidDataException("Create the unqualified equipped profile first.");
                actor=new GameObject("Fitted low-motion graph");actor.SetActive(false);SceneManager.MoveGameObjectToScene(actor,preview);
                var motor=actor.AddComponent<TopDown3DPlayerMotor>();driver=actor.AddComponent<TopDown3DPlayerAnimationDriver>();
                var shape=actor.GetComponent<CapsuleCollider>();shape.height=bindings.capsuleHeight;shape.radius=bindings.capsuleRadius;shape.center=Vector3.zero;
                EditorUtility.CopySerialized(session.Player.GetComponent<TopDown3DPlayerAnimationDriver>(),driver);
                driver.ConfigureLaboratory(bindings);Call(driver,"Awake");actor.SetActive(true);
                var graph=Field<PlayableGraph>(driver,"playableGraph");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var visual=Field<GameObject>(driver,"visualInstance");var animator=driver.HumanoidAnimator;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                var owner=new object();var idle=bindings.locomotion.GetRequiredEntry(TopDown3DLocomotionClipRole.Idle);
                driver.TryBeginAnimationPresentation(owner,idle.Clip);driver.SetAnimationPresentationTime(owner,0);
                driver.SetAnimationHeightCorrection(owner,idle.HeightCorrection(0));Call(driver,"PresentAnimationAction");graph.Evaluate(0);
                if(!driver.ConfigureCamp(TopDown3DCampSettings.Load()))throw new InvalidDataException("The equipment closure is missing.");
                driver.SetCampPresentation(-1,1,false,true);Call(driver,"LateUpdate");driver.EndAnimationPresentation(owner);Call(driver,"NormalizeCampSkinWeights");
                full=AnimationPlaygroundFootProjectionAuthoring.BuildFixture(bindings.targetModel,visual,true,true);var sampler=full.Bind(visual.transform);
                var activeProjection=dense ? profile.bodyProjection : full;var activeSampler=activeProjection.Bind(visual.transform);
                Set(driver,"garmentFixturePlane",new Plane(Vector3.up,new Vector3(0,-.91f,0)));
                using var cpu=new AnimationPlaygroundMotionSampler.SoleProbe(visual,animator.GetBoneTransform(HumanBodyBones.LeftFoot),animator.GetBoneTransform(HumanBodyBones.RightFoot),true);
                var cloth=visual.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(TopDown3DPlayerAnimationDriver.IsSoftGarment).ToArray();
                baked=new Mesh();var vertices=new List<Vector3>();var rows=new List<Envelope>();var selected=new HashSet<int>();
                void Sample(Envelope row)
                {
                    try{Call(driver,"PresentAnimationAction");graph.Evaluate(0);}
                    catch(Exception error){throw new InvalidDataException("Fitting failed at "+row.name+" pose "+Field<double>(driver,"animationPresentationTime"),error);}
                    cpu.Measure(out _,out _,out var minimum,out _);
                    var predicted=sampler.MinimumWorldY(out var index);selected.Add(index);
                    row.minimumBody=Mathf.Min(row.minimumBody,minimum.y);
                    row.maximumBodyCorrection=Mathf.Max(row.maximumBodyCorrection,driver.LastPoseBlendGroundCorrection);
                    row.maximumGarmentDisplacement=Mathf.Max(row.maximumGarmentDisplacement,driver.LastGarmentFloorDisplacement);
                    row.maximumPredictionError=Mathf.Max(row.maximumPredictionError,Mathf.Abs(predicted-minimum.y));row.exceeded|=driver.GarmentFloorExceeded;
                    row.maximumReducedError=Mathf.Max(row.maximumReducedError,Mathf.Abs(activeSampler.MinimumWorldY()-predicted));
                    foreach(var skin in cloth)
                    {
                        skin.BakeMesh(baked,true);baked.GetVertices(vertices);
                        foreach(var point in vertices)row.minimumGarment=Mathf.Min(row.minimumGarment,skin.transform.TransformPoint(point).y);
                    }
                }
                void Pose(AnimationClip clip,float time)
                {
                    if(!driver.TryBeginAnimationPresentation(owner,clip) || !driver.SetAnimationBodyProjection(owner,activeProjection)
                        || !driver.SetAnimationGarmentFloor(owner,true,profile.maximumGarmentProjection,profile.garmentClearance))throw new InvalidDataException("Fitted ownership failed.");
                    driver.SetAnimationPresentationTime(owner,time);
                }
                void Blend(string name,AnimationClip from,float time,AnimationClip to)
                {
                    Pose(from,time);Call(driver,"PresentAnimationAction");graph.Evaluate(0);
                    if(!driver.TryCapturePoseBlend(owner,.12f) || !driver.TryReplaceAnimationPresentation(owner,to)
                        || !driver.SetAnimationBodyProjection(owner,activeProjection) || !driver.SetAnimationGarmentFloor(owner,true))throw new InvalidDataException("Fitted blend ownership failed.");
                    var row=new Envelope{name=name};
                    var intervals=dense ? 120 : 60;
                    for(var i=0;i<=intervals;i++)
                    {
                        var elapsed=.12f*i/intervals;driver.SetAnimationPresentationTime(owner,Mathf.Min(to.length,elapsed));driver.SetPoseBlendElapsed(owner,elapsed);Sample(row);
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
                        var intervals=dense ? 256 : 128;
                        for(var i=0;i<=intervals;i++){driver.SetAnimationPresentationTime(owner,entry.clip.length*i/(float)intervals);Sample(row);}
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
                        for(var phase=0;phase<4;phase++)
                            Blend(quality+"."+prefix+".RecoverToCrouch"+phase,recover,recover.length*(phase+1)/4,
                                bindings.crouch.Get(AnimationPlaygroundCrouchRole.Idle).clip);
                    }
                    var phases=dense ? 8 : 4;
                    for(var phase=0;phase<phases;phase++)
                    {
                        var sprint=bindings.locomotion.GetRequiredEntry(TopDown3DLocomotionClipRole.Sprint).Clip;
                        Blend(quality+".SprintToSlide"+phase,sprint,sprint.length*phase/phases,profile.Get(AnimationPlaygroundLowMotionRole.SlideEnter).clip);
                        Blend(quality+".LandingToRoll"+phase,bindings.airborne.approach,bindings.airborne.approach.length*phase/phases,profile.Get(AnimationPlaygroundLowMotionRole.RollContact).clip);
                    }
                }
                Object.DestroyImmediate(baked);
                var reduced=profile.bodyProjection;reduced.model=full.model;reduced.bonePaths=full.bonePaths;
                var expand=dense && rows.Any(x=>x.maximumReducedError>.0003f);
                if(!dense || expand)reduced.vertices=selected.OrderBy(x=>x).Select(x=>full.vertices[x]).ToArray();profile.bodyProjection=reduced;
                if(!profile.Validate(out var reason,false))throw new InvalidDataException(reason);
                if(!dense || expand){EditorUtility.SetDirty(reduced);AssetDatabase.SaveAssetIfDirty(reduced);}profile=null;
                File.WriteAllText(Path.Combine(directory,"summary.json"),JsonUtility.ToJson(new Report{
                    cacheExpanded=expand,fullVertices=full.vertices.Length,reducedVertices=reduced.vertices.Length,envelopes=rows.ToArray()},true)+"\n");
            }
            catch(Exception error){File.WriteAllText(Path.Combine(directory,"failure.txt"),error.ToString());Debug.LogException(error);exitCode=1;}
            finally
            {
                QualitySettings.skinWeights=before;
                if(full!=null)Object.DestroyImmediate(full);
                if(baked!=null)Object.DestroyImmediate(baked);
                if(driver!=null)Call(driver,"OnDestroy");if(actor!=null)Object.DestroyImmediate(actor);
                EditorSceneManager.ClosePreviewScene(preview);EditorSceneManager.CloseScene(scene,true);
            }
            EditorApplication.Exit(exitCode);
        }
    }
}
