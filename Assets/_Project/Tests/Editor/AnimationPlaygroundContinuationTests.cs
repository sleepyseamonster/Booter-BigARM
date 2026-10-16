using System;
using System.IO;
using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using Object=UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class AnimationPlaygroundContinuationTests
    {
        private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        internal const string ProfilePath="Assets/_Project/Settings/AnimationPlayground/PrivateBindings/CampMotionContinuationReview.asset";
        internal static AnimationPlaygroundContinuationProfile Review()
        {
            var value=AssetDatabase.LoadAssetAtPath<AnimationPlaygroundContinuationProfile>(ProfilePath);
            Assert.That(value,Is.Not.Null,"Restore the private continuation recovery bundle.");return value;
        }
        [TestCase(0f)] [TestCase(.5f)] [TestCase(1.4f)] [TestCase(3f)] [TestCase(4f)] [TestCase(5f)] [TestCase(6f)] [TestCase(6.15f)]
        public void AirControlRetainsNeutralAndAlignedLaunchMomentumAndBoundsCountersteering(float speed)
        {
            var current=Vector3.forward*speed;
            var neutral=AnimationPlaygroundAirControl.Advance(current,Vector3.zero,speed,Mathf.Max(6,speed),3,.02f);
            Assert.That(neutral,Is.EqualTo(current));
            for(var i=0;i<100;i++)
            {
                var next=AnimationPlaygroundAirControl.Advance(current,Vector3.forward,speed,Mathf.Max(6,speed),3,.02f);
                Assert.That(next.magnitude,Is.LessThanOrEqualTo(Mathf.Max(1.4f,speed)+.00001f));
                Assert.That(Vector3.Distance(next,current),Is.LessThanOrEqualTo(.06001f));current=next;
            }
            if(speed>=1.4f)Assert.That(current.magnitude,Is.EqualTo(speed).Within(.00001f));
            var counter=AnimationPlaygroundAirControl.Advance(current,Vector3.back*.5f,speed,Mathf.Max(6,speed),3,.02f);
            Assert.That(Vector3.Dot(counter,Vector3.forward),Is.LessThan(Vector3.Dot(current,Vector3.forward)));
            Assert.That(counter.y,Is.Zero);
        }
        [Test] public void AdaptedTurnsRetainNativeDurationC1EndpointsAndExistingQualificationFlags()
        {
            var p=Review();Assert.That(p.Validate(p.targetModel,p.footProjection,out var reason),Is.True,reason);
            foreach(var source in new[]{p.left,p.right})
            {
                source.Sample(0,out var start,out _);Assert.That(start.magnitude,Is.LessThan(.00001f));
                var previous=Vector3.zero;
                for(var i=0;i<=1500;i++)
                {
                    source.Sample(i*.001f,out var position,out var rotation);
                    Assert.That(position.magnitude,Is.LessThan(.4f));
                    if(i>0)Assert.That(Vector3.Distance(previous,position)/.001f,Is.LessThan(1.401f));
                    Assert.That(float.IsFinite(rotation.w),Is.True);previous=position;
                }
                Assert.That(source.knots[0].velocity,Is.EqualTo(Vector2.zero));
                Assert.That(source.knots[^1].velocity,Is.EqualTo(Vector2.zero));
            }
            var old=AssetDatabase.LoadAssetAtPath<AnimationPlaygroundForwardLocomotionProfile>(
                "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/ExpandedForwardGaitReview.asset");
            Assert.That(old.qualified||old.terrainQualified,Is.False);
        }
        [TestCase(SkinWeights.OneBone)] [TestCase(SkinWeights.TwoBones)] [TestCase(SkinWeights.FourBones)]
        public void ExpandedFamilyFitsMildPlanesAcrossAllHeadingsWithOneOutput(SkinWeights quality)
        {
            using var f=new Fixture(quality);var mesh=new Mesh();
            try
            {
                foreach(var slope in new[]{0f,5f,10f})
                foreach(var heading in slope==0 ? new[]{0f} : new[]{0f,45,90,135,180,225,270,315})
                foreach(var speed in new[]{0f,.7f,1.4f,2.05f,3,4,5,6,6.15f})
                for(var frame=0;frame<32;frame++)
                {
                    f.Support(slope,heading);var before=f.Driver.LaboratoryTerrainEvaluations;
                    f.Forward.Frame(speed,frame/32d);
                    Assert.That(f.Driver.LaboratoryTerrainEvaluations,Is.EqualTo(before+1));
                    Assert.That(f.Graph.GetOutputCount(),Is.EqualTo(1));
                    Assert.That(f.Driver.GarmentFloorExceeded,Is.False,$"{slope}/{heading}/{speed}/{frame}");
                    if(frame%8!=0)continue;
                    var minimum=f.Minimum(mesh,out var height);
                    Assert.That(minimum,Is.GreaterThanOrEqualTo(-.001f),$"{slope}/{heading}/{speed}/{frame}");
                    Assert.That(height,Is.LessThanOrEqualTo(1.821f));
                }
            }
            finally{Object.DestroyImmediate(mesh);}
        }
        [TestCase(SkinWeights.OneBone)] [TestCase(SkinWeights.TwoBones)] [TestCase(SkinWeights.FourBones)]
        public void SlowLateralFamilyFitsTheSameBoundedSlopeEnvelope(SkinWeights quality)
        {
            using var f=new Fixture(quality);var mesh=new Mesh();
            try
            {
                foreach(var slope in new[]{0f,5f,10f})
                foreach(var heading in slope==0 ? new[]{0f} : new[]{0f,45,90,135,180,225,270,315})
                foreach(var side in new[]{AnimationPlaygroundStandingTravel.Left,AnimationPlaygroundStandingTravel.Right})
                for(var frame=0;frame<64;frame++)
                {
                    f.Support(slope,heading);f.Forward.Call("PrepareForwardGraphUpdate");
                    typeof(TopDown3DPlayerAnimationDriver).GetField("standingStrafeSide",Private).SetValue(f.Driver,side);
                    typeof(TopDown3DPlayerAnimationDriver).GetMethod("PresentStandingStrafeFrame",Private).Invoke(f.Driver,
                        new object[]{.5f,frame/64d,.15f});f.Forward.Call("LateUpdate");
                    Assert.That(f.Driver.GarmentFloorExceeded,Is.False);
                    if(frame%8!=0)continue;
                    Assert.That(f.Minimum(mesh,out var height),Is.GreaterThanOrEqualTo(-.001f),$"{slope}/{heading}/{side}/{frame}");
                    Assert.That(height,Is.LessThanOrEqualTo(1.821f));
                }
            }
            finally{Object.DestroyImmediate(mesh);}
        }
        [TestCase(SkinWeights.OneBone,false)] [TestCase(SkinWeights.TwoBones,false)] [TestCase(SkinWeights.FourBones,false)]
        [TestCase(SkinWeights.OneBone,true)] [TestCase(SkinWeights.TwoBones,true)] [TestCase(SkinWeights.FourBones,true)]
        public void RuntimeTurnOwnerUsesAdaptedCurveAndBoundedTangentStageForBothCompleteSources(SkinWeights quality,bool authoredOrigin)
        {
            using var f=new Fixture(quality);var resets=new AnimationPlaygroundResetRegistry();
            var coordinator=f.Forward.Actor.AddComponent<AnimationPlaygroundActionCoordinator>();
            var controller=f.Forward.Actor.AddComponent<AnimationPlaygroundTurnController>();var mesh=new Mesh();
            coordinator.Configure(f.Motor,f.Driver,resets,new[]{f.Motor.Position});
            controller.Configure(f.Motor,f.Driver,coordinator,Review(),resets,()=>false);
            try
            {
                foreach(var left in new[]{true,false})
                {
                    var origin=(authoredOrigin ? f.Forward.Station.transform.position : Vector3.zero)+Vector3.up*.91f;
                    f.Forward.Actor.transform.SetPositionAndRotation(origin,Quaternion.identity);
                    f.Forward.Player.position=origin;f.Forward.Player.rotation=Quaternion.identity;
                    f.Forward.Player.linearVelocity=Vector3.zero;f.Support(0,0);Physics.SyncTransforms();
                    var target=Quaternion.Euler(0,left ? -175 : 175,0)*Vector3.forward;
                    Assert.That(controller.TryBeginTurn(target),Is.True,controller.LastFailure);
                    Assert.That(coordinator.IsSequenceOwner(controller),Is.True);f.Driver.EndPoseBlend(controller);
                    var source=left ? Review().left : Review().right;
                    var anchor=Vector3.zero;var previous=Vector3.zero;var excursion=0f;var travel=0f;var maximumRequest=0f;
                    for(var frame=0;frame<=180;frame++)
                    {
                        var time=source.clip.length*frame/180;
                        source.Sample(time,out var point,out var yaw);
                        f.Forward.Actor.transform.SetPositionAndRotation(origin+point,yaw);
                        f.Forward.Player.position=f.Forward.Actor.transform.position;f.Forward.Player.rotation=yaw;
                        f.Driver.SetAnimationPresentationTime(coordinator,time);f.Forward.Call("PrepareForwardGraphUpdate");
                        f.Forward.Call("PresentAnimationAction");
                        var tangent=f.Forward.Get<AnimationPlaygroundTangentFootPose>(left ? "turnLeftPose" : "turnRightPose").Sample;
                        Assert.That(tangent.enabled&&tangent.available,Is.True,$"{left}/{frame}: enabled={tangent.enabled}, reason={tangent.unavailableReason}, paused={f.Motor.LaboratoryModalPaused}, blend={f.Driver.PoseBlendOwned}");
                        maximumRequest=Mathf.Max(maximumRequest,tangent.requestedCorrection);
                        Assert.That(f.Driver.LaboratoryTurnContactUnavailable,Is.False,$"{left}/{frame}");
                        var skinPoint=f.SolePoint(source.soleVertex,(int)quality);
                        if(frame==0)anchor=previous=skinPoint;
                        excursion=Mathf.Max(excursion,Vector3.ProjectOnPlane(skinPoint-anchor,Vector3.up).magnitude);
                        travel+=Vector3.ProjectOnPlane(skinPoint-previous,Vector3.up).magnitude;previous=skinPoint;
                        Assert.That(f.Graph.GetOutputCount(),Is.EqualTo(1));
                        if(frame%30==0)
                        {Assert.That(f.Minimum(mesh,out var height),Is.GreaterThanOrEqualTo(-.001f));Assert.That(height,Is.LessThan(1.821f));}
                    }
                    Assert.That(excursion,Is.LessThan(.001f));
                    // Retain the near-origin contact gate. At the authored
                    // world origin, record float-world cumulative travel as a
                    // separate qualification observation: its repeated ULP
                    // steps must not be relabelled as a passing 20mm plant.
                    if(!authoredOrigin)Assert.That(travel,Is.LessThan(.02f));
                    Assert.That(f.Forward.Policy.qualified||f.Forward.Policy.terrainQualified,Is.False);
                    var observations=Path.Combine("Temp","CampMotionContinuationTurnObservations");Directory.CreateDirectory(observations);
                    File.WriteAllText(Path.Combine(observations,$"{(int)quality}-{authoredOrigin}-{left}.json"),
                        JsonUtility.ToJson(new TurnObservation{clip=source.clip.name,influences=(int)quality,authoredOrigin=authoredOrigin,
                            samples=181,pointExcursion=excursion,cumulativeTravel=travel,maximumCorrection=maximumRequest,
                            contactAimMet=excursion<=.02f && travel<=.02f},true)+"\n");
                    controller.Cancel(null);Assert.That(coordinator.IsBusy,Is.False);Assert.That(f.Motor.IsActionConstrained,Is.False);
                    Assert.That(f.Driver.LaboratoryTurnContactUnavailable,Is.False);
                }
            }
            finally{controller.DisposeOwnedServices();coordinator.DisposeOwnedServices();Object.DestroyImmediate(mesh);}
        }
        [Test] public void TurnCannotOverrideBusyMovingSlopedOrLockedPlayersAndResetReleasesEveryLease()
        {
            using var f=new Fixture(SkinWeights.FourBones);var resets=new AnimationPlaygroundResetRegistry();
            var coordinator=f.Forward.Actor.AddComponent<AnimationPlaygroundActionCoordinator>();
            var controller=f.Forward.Actor.AddComponent<AnimationPlaygroundTurnController>();
            coordinator.Configure(f.Motor,f.Driver,resets,new[]{f.Motor.Position});
            controller.Configure(f.Motor,f.Driver,coordinator,Review(),resets,()=>false);
            try
            {
                f.Support(0,0);Physics.SyncTransforms();f.Forward.Player.linearVelocity=Vector3.forward;
                Assert.That(controller.TryBeginTurn(Vector3.back),Is.False);f.Forward.Player.linearVelocity=Vector3.zero;
                f.Support(5,0);Assert.That(controller.TryBeginTurn(Vector3.back),Is.False);
                f.Support(0,0);Physics.SyncTransforms();Assert.That(f.Motor.TryBeginActionConstraint(f),Is.True);
                Assert.That(controller.TryBeginTurn(Vector3.back),Is.False);f.Motor.EndActionConstraint(f);f.Support(0,0);
                typeof(TopDown3DPlayerMotor).GetField("standingStrafeLocked",Private).SetValue(f.Motor,true);
                Assert.That(controller.TryBeginTurn(Vector3.back),Is.False);
                typeof(TopDown3DPlayerMotor).GetField("standingStrafeLocked",Private).SetValue(f.Motor,false);
                Assert.That(controller.TryBeginTurn(Vector3.back),Is.True);
                typeof(TopDown3DPlayerMotor).GetProperty("AnimationSnapshot").SetValue(f.Motor,
                    new AnimationPlaygroundMotorSnapshot(AnimationPlaygroundMotionMode.Turn,AnimationPlaygroundPosture.Standing,
                        true,true,Vector3.zero,Vector3.zero,Vector3.forward,0));
                typeof(AnimationPlaygroundTurnController).GetMethod("ObserveTurn",Private).Invoke(controller,null);
                Assert.That(controller.Active,Is.False);Assert.That(coordinator.IsBusy,Is.False);
                Assert.That(f.Motor.IsActionConstrained,Is.False);Assert.That(controller.LastFailure,Does.Contain("blocked"));
                controller.Cancel(null);f.Support(0,0);Physics.SyncTransforms();
                Assert.That(controller.TryBeginTurn(Vector3.back),Is.True);coordinator.Cancel();
                typeof(AnimationPlaygroundTurnController).GetMethod("ObserveLocomotion",Private).Invoke(controller,null);
                Assert.That(controller.Active,Is.False);Assert.That(controller.LastFailure,Does.Contain("interrupted"));
                controller.InvalidateForReset(2);Assert.That(controller.Active,Is.False);Assert.That(f.Driver.PoseBlendOwned,Is.False);
                Assert.That(f.Graph.GetOutputCount(),Is.EqualTo(1));
            }
            finally{controller.DisposeOwnedServices();coordinator.DisposeOwnedServices();}
        }
        [TestCase(SkinWeights.OneBone)] [TestCase(SkinWeights.TwoBones)] [TestCase(SkinWeights.FourBones)]
        public void GroundedTakeoffAndLandingPosesShareMildSlopeOutputWithoutASecondRootWriter(SkinWeights quality)
        {
            using var f=new Fixture(quality);var mesh=new Mesh();
            var resets=new AnimationPlaygroundResetRegistry();var coordinator=f.Forward.Actor.AddComponent<AnimationPlaygroundActionCoordinator>();
            coordinator.Configure(f.Motor,f.Driver,resets,new[]{f.Motor.Position});
            var air=AssetDatabase.LoadAssetAtPath<AnimationPlaygroundAirborneProfile>(
                "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/AirborneProfile.asset");
            try
            {
                foreach(var clip in new[]{air.standing.takeoff,air.running.takeoff,air.unarmed.takeoff,
                    air.standing.landing,air.running.landing,air.unarmed.landing,air.hardLanding})
                {
                    var binding=new AnimationPlaygroundMotionBinding{sourceId="OfflineGroundedAirPose",clip=clip,
                        readiness=AnimationPlaygroundMotionReadiness.PhysicalQualified};
                    Assert.That(coordinator.TryBeginPhysical(binding,AnimationPlaygroundMotionMode.Recovery,null,
                        _=>new AnimationPlaygroundMotorIntent(Vector3.zero,Vector3.forward),null),Is.True);
                    Assert.That(f.Driver.SetAnimationFootProjection(coordinator,true),Is.True);
                    foreach(var slope in new[]{0f,5f,10f})
                    foreach(var heading in slope==0 ? new[]{0f} : new[]{0f,90,180,270})
                    for(var frame=0;frame<=32;frame++)
                    {
                        f.Support(slope,heading);Physics.SyncTransforms();f.Forward.Call("PrepareForwardGraphUpdate");
                        f.Driver.SetAnimationPresentationTime(coordinator,clip.length*frame/32);f.Forward.Call("PresentAnimationAction");
                        var stage=f.Forward.Get<AnimationPlaygroundTerrainPose>("forwardTerrainPose").Sample;
                        Assert.That(stage.incompatibleReach,Is.LessThanOrEqualTo(.001f));
                        Assert.That(f.Driver.GarmentFloorExceeded,Is.False);
                        if(frame%8==0)
                        {
                            Assert.That(f.Minimum(mesh,out var height),Is.GreaterThanOrEqualTo(-.001f),$"{clip.name}/{slope}/{heading}/{frame} terrain={f.Forward.Get<bool>("forwardTerrainApplied")} enabled={stage.enabled}");
                            Assert.That(height,Is.LessThanOrEqualTo(1.821f));
                        }
                        Assert.That(f.Graph.GetOutputCount(),Is.EqualTo(1));
                    }
                    coordinator.Cancel();
                }
            }
            finally{coordinator.DisposeOwnedServices();Object.DestroyImmediate(mesh);}
        }
        [Test] public void ContinuationSupportRejectsEdgesMovingSupportsAndUnmeasuredGradesAndResetClearsFallback()
        {
            using var f=new Fixture(SkinWeights.FourBones);
            f.Support(10,0);Physics.SyncTransforms();Assert.That(f.Driver.StandingStrafeReviewSupportAvailable(),Is.True);
            f.Support(10.1f,0);Physics.SyncTransforms();Assert.That(f.Driver.StandingStrafeReviewSupportAvailable(),Is.False);
            f.Support(5,0);Physics.SyncTransforms();Assert.That(f.Driver.StandingStrafeReviewSupportAvailable(),Is.True);
            f.Forward.Ground.GetComponent<BoxCollider>().size=new Vector3(.2f,.1f,.2f);
            Physics.SyncTransforms();Assert.That(f.Driver.StandingStrafeReviewSupportAvailable(),Is.False,"Missing footprint support must fall back.");
            f.Forward.Ground.GetComponent<BoxCollider>().size=new Vector3(20,.1f,20);Physics.SyncTransforms();
            typeof(TopDown3DPlayerAnimationDriver).GetMethod("RejectContinuationPose",Private).Invoke(f.Driver,new object[]{"Offline rejection"});
            Assert.That(f.Driver.StandingStrafeReviewSupportAvailable(),Is.False);Assert.That(f.Driver.LaboratoryContinuationFailure,Is.Not.Null);
            f.Driver.ResetLaboratoryContinuationReview();Assert.That(f.Driver.StandingStrafeReviewSupportAvailable(),Is.True);
            var jump=Array.Find(f.Forward.Session.Stations,s=>s.StationId=="AP04");
            f.Forward.Ground.transform.SetParent(jump.transform,true);
            Object.DestroyImmediate(f.Forward.Ground.GetComponent<TopDown3DGroundSurface>());
            f.Support(0,0);Physics.SyncTransforms();Assert.That(f.Driver.TryGetContinuationLandingPlane(out _),Is.True,
                "AP04's authored static deck remains valid for a selected bounded roll even without AP02's surface marker.");
            f.Forward.Ground.AddComponent<Rigidbody>().isKinematic=true;
            Assert.That(f.Driver.StandingStrafeReviewSupportAvailable(),Is.False);
        }
        [Test] public void RequiredContinuationCannotSilentlyLoseItsPrivateData()
        {
            var source=AssetDatabase.LoadAssetAtPath<TopDown3DAnimationPlaygroundBindings>(
                "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/PlayerBindings.asset");
            var bindings=Object.Instantiate(source);
            try
            {
                bindings.continuationReview=null;bindings.continuationReviewRequired=true;
                Assert.That(bindings.ValidatePlayer(out var reason),Is.False);Assert.That(reason,Does.Contain("continuation"));
            }
            finally{Object.DestroyImmediate(bindings);}
        }
        [Test] public void ReversalBrakingRequiresTheSameReviewSupportAsItsTurnOwner()
        {
            using var f=new Fixture(SkinWeights.FourBones);var coordinator=f.Forward.Actor.AddComponent<AnimationPlaygroundActionCoordinator>();
            coordinator.Configure(f.Motor,f.Driver,new AnimationPlaygroundResetRegistry(),new[]{f.Motor.Position});
            var routerObject=new GameObject("Offline reversal neutral gate");routerObject.SetActive(false);
            routerObject.transform.SetParent(f.Forward.Actor.transform,false);
            var router=routerObject.AddComponent<TopDown3DInputRouter>();
            var oldPalette=typeof(TopDown3DInputRouter).GetField("animationPalette",Private).GetValue(router);
            using var palette=new UnityEngine.InputSystem.InputAction("Offline neutral gate");
            try
            {
                typeof(TopDown3DPlayerMotor).GetField("input",Private).SetValue(f.Motor,router);
                f.Motor.FacePlanarDirection(Vector3.forward);f.Support(0,0);Physics.SyncTransforms();
                f.Motor.ConfigureLaboratoryTurnReview(coordinator,true,()=>false);
                var args=new object[]{Vector3.back};typeof(TopDown3DPlayerMotor).GetMethod("ApplyLaboratoryTurnBraking",Private).Invoke(f.Motor,args);
                Assert.That(f.Motor.LaboratoryTurnBraking,Is.False);Assert.That((Vector3)args[0],Is.EqualTo(Vector3.back));
                f.Motor.ConfigureLaboratoryTurnReview(coordinator,true,()=>true);args[0]=Vector3.back;
                typeof(TopDown3DInputRouter).GetField("animationPalette",Private).SetValue(router,palette);
                typeof(TopDown3DInputRouter).GetField("animationAwaitNeutral",Private).SetValue(router,true);
                typeof(TopDown3DPlayerMotor).GetMethod("ApplyLaboratoryTurnBraking",Private).Invoke(f.Motor,args);
                Assert.That(f.Motor.LaboratoryTurnBraking,Is.False,"Disarmed input must not capture a reversal.");
                typeof(TopDown3DInputRouter).GetField("animationAwaitNeutral",Private).SetValue(router,false);
                typeof(TopDown3DPlayerMotor).GetMethod("ApplyLaboratoryTurnBraking",Private).Invoke(f.Motor,args);
                Assert.That(f.Motor.LaboratoryTurnBraking,Is.True,$"grounded={f.Motor.IsGrounded}, plane={f.Motor.TryGetLaboratorySupportPlane(out _,true)}, yaw={f.Forward.Player.rotation.eulerAngles}, posture={f.Motor.AnimationPosture}, disarmed={router.LaboratoryStandingFacingDisarmed}");
                Assert.That((Vector3)args[0],Is.EqualTo(Vector3.zero));
            }
            finally{typeof(TopDown3DInputRouter).GetField("animationPalette",Private).SetValue(router,oldPalette);coordinator.DisposeOwnedServices();}
        }
        [Serializable] private sealed class TurnObservation
        {
            public string clip;public int influences,samples;public bool authoredOrigin,contactAimMet;
            public float pointExcursion,cumulativeTravel,maximumCorrection;
        }
        internal sealed class Fixture:IDisposable
        {
            public readonly AnimationPlaygroundForwardReviewTests.Fixture Forward;
            public TopDown3DPlayerAnimationDriver Driver=>Forward.Driver;
            public TopDown3DPlayerMotor Motor=>Forward.Motor;
            public PlayableGraph Graph=>Forward.Get<PlayableGraph>("playableGraph");
            public Plane Plane;
            public Fixture(SkinWeights quality)
            {
                Forward=new AnimationPlaygroundForwardReviewTests.Fixture(false,quality,
                    "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/ExpandedForwardGaitReview.asset");
                Forward.Actor.transform.SetPositionAndRotation(Vector3.up*.91f,Quaternion.identity);
                Forward.Player.position=Forward.Actor.transform.position;Forward.Player.rotation=Quaternion.identity;
                Driver.ConfigureLaboratoryForwardReview(Forward.Policy,Forward.Station);
                Driver.ConfigureLaboratoryStandingStrafeReview(AssetDatabase.LoadAssetAtPath<AnimationPlaygroundStandingStrafeProfile>(
                    "Assets/_Project/Settings/AnimationPlayground/PrivateBindings/StandingStrafeReview.asset"));
                Driver.ConfigureLaboratoryContinuation(Review());Forward.Call("Awake");Forward.Actor.SetActive(true);
                // This fixture supplies recorded support and poses, without an
                // initialized input service. Do not inherit the saved router's
                // disabled mode as a simulated user menu pause.
                typeof(TopDown3DPlayerMotor).GetField("input",Private).SetValue(Motor,null);
                Driver.ConfigureCamp(TopDown3DCampSettings.Load());Driver.SetCampPresentation(-1,1,false,true);
                Forward.Ground.GetComponent<BoxCollider>().size=new Vector3(20,.1f,20);Forward.Ground.AddComponent<TopDown3DGroundSurface>();
                Support(0,0);Forward.Frame(0,0);
            }
            public void Support(float slope,float heading)
            {
                var normal=Quaternion.Euler(0,heading,0)*Quaternion.AngleAxis(slope,Vector3.right)*Vector3.up;
                var point=Motor.Position+Vector3.down*.91f+Vector3.up*(-.2f*(1/normal.y-1));Plane=new Plane(normal,point);
                Forward.Ground.transform.SetPositionAndRotation(point-normal*.05f,Quaternion.FromToRotation(Vector3.up,normal));
                void Set(string name,object value)=>typeof(TopDown3DPlayerMotor).GetField(name,Private).SetValue(Motor,value);
                Set("hasCompletedLaboratoryGroundPlane",true);Set("completedLaboratoryGroundPlane",Plane);
                Set("observedGroundSupport",Forward.Ground.GetComponent<Collider>());Set("laboratoryGroundSupport",Forward.Ground.GetComponent<Collider>());
                Set("observedSupportMatrix",Forward.Ground.transform.localToWorldMatrix);
                typeof(TopDown3DPlayerMotor).GetProperty("IsGrounded").SetValue(Motor,true);
                typeof(TopDown3DPlayerMotor).GetProperty("LaboratoryGroundedMotion").SetValue(Motor,
                    new AnimationPlaygroundGroundedMotion(1,1,Time.fixedTimeAsDouble,.02,Vector3.zero,normal,true,false));
            }
            public float Minimum(Mesh mesh,out float height)
            {
                var minimum=float.PositiveInfinity;height=float.NegativeInfinity;
                var visual=Forward.Get<GameObject>("visualInstance");
                foreach(var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    skin.BakeMesh(mesh,true);
                    foreach(var vertex in mesh.vertices)
                    {
                        var p=skin.transform.TransformPoint(vertex);minimum=Mathf.Min(minimum,Plane.GetDistanceToPoint(p));
                        height=Mathf.Max(height,p.y-(Motor.Position.y-.91f));
                    }
                }
                return minimum;
            }
            public Vector3 SolePoint(int vertex,int influences)
            {
                var projection=Review().footProjection;var visual=Forward.Get<GameObject>("visualInstance").transform;
                var points=(AnimationPlaygroundSkinPoint[])projection.vertices[vertex].points.Clone();
                Array.Sort(points,(a,b)=>b.weight.CompareTo(a.weight));var result=Vector3.zero;var weight=0f;
                for(var i=0;i<Math.Min(influences,points.Length);i++)
                {
                    var p=points[i];var path=projection.bonePaths[p.bone];var bone=path.Length==0 ? visual : visual.Find(path);
                    result+=bone.TransformPoint(p.position)*p.weight;weight+=p.weight;
                }
                return result/weight;
            }
            public void Dispose()=>Forward.Dispose();
        }
    }
}
