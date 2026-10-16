using System;
using System.Linq;
using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Playables;
using Object=UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class AnimationPlaygroundDirectionalTests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        internal const string DirectionalPath="Assets/_Project/Settings/AnimationPlayground/PrivateBindings/StandingDirectionalReview.asset";
        internal const string NativePath="Assets/_Project/Settings/AnimationPlayground/PrivateBindings/CampNativeMotionReview.asset";
        internal static AnimationPlaygroundStandingStrafeProfile Profile()
        {
            var p=AssetDatabase.LoadAssetAtPath<AnimationPlaygroundStandingStrafeProfile>(DirectionalPath);
            Assert.That(p,Is.Not.Null,"Restore the private directional sources and profile.");return p;
        }
        [Test] public void ContinuousAngularWeightsCoverTheFullCircleWithoutChangingAnalogIntent()
        {
            var previous=AnimationPlaygroundDirectionalMath.Weights(Vector2.up);
            for(var angle=0f;angle<=360;angle+=.25f)
            {
                var d=new Vector2(Mathf.Sin(angle*Mathf.Deg2Rad),Mathf.Cos(angle*Mathf.Deg2Rad));
                var w=AnimationPlaygroundDirectionalMath.Weights(d*.37f);
                Assert.That(w.x+w.y+w.z+w.w,Is.EqualTo(1).Within(.000001));
                for(var i=0;i<4;i++)Assert.That(w[i],Is.InRange(0,1));
                Assert.That((w-previous).magnitude,Is.LessThan(.004f));previous=w;
                Assert.That(AnimationPlaygroundDirectionalMath.Classify(d),Is.Not.EqualTo(AnimationPlaygroundStandingTravel.Unsupported));
                foreach(var yaw in new[]{0f,37,179,270})
                {
                    var r=Quaternion.Euler(0,yaw,0);var world=r*new Vector3(d.x,0,d.y)*.37f;
                    Assert.That((AnimationPlaygroundDirectionalMath.Local(world,r*Vector3.forward)-d*.37f).magnitude,Is.LessThan(.00001f));
                }
            }
            Assert.That(AnimationPlaygroundDirectionalMath.Classify(Vector2.zero),Is.EqualTo(AnimationPlaygroundStandingTravel.Idle));
            Assert.Throws<ArgumentException>(()=>AnimationPlaygroundDirectionalMath.Weights(new Vector2(float.NaN,0)));
        }
        [Test] public void DirectionalProfileKeepsNativeWholeLoopsAndTheAcceptedForwardBlendWeights()
        {
            var p=Profile();Assert.That(p.Validate(p.targetModel,p.footProjection,out var reason),Is.True,reason);
            var f=AssetDatabase.LoadAssetAtPath<AnimationPlaygroundForwardLocomotionProfile>("Assets/_Project/Settings/AnimationPlayground/PrivateBindings/ExpandedForwardGaitReview.asset");
            var w=new float[12];
            foreach(var speed in new[]{0f,.7f,1.4f,2.05f,2.7f,3,4,5,6})
            {
                AnimationPlaygroundDirectionalMath.FillPoseWeights(p,f,speed,Vector2.up,w);var accepted=f.PoseWeights(speed);
                for(var i=0;i<12;i++)Assert.That(w[i],Is.EqualTo(i<6 ? accepted[i] : 0).Within(.000001));
            foreach(var angle in new[]{0f,10,22,23,45,90,135,180,225,270,315})
                {
                    var d=new Vector2(Mathf.Sin(angle*Mathf.Deg2Rad),Mathf.Cos(angle*Mathf.Deg2Rad));
                    AnimationPlaygroundDirectionalMath.FillPoseWeights(p,f,speed,d,w);
                    Assert.That(w.Sum(),Is.EqualTo(1).Within(.000001));Assert.That(w.All(v=>float.IsFinite(v)&&v>=0),Is.True);
                }
            }
            Assert.That(f.qualified||f.terrainQualified,Is.False);
            foreach(var g in p.directionalGaits)
            {
                Assert.That(g.sourceClip.isLooping&&g.sourceClip.isHumanMotion,Is.True);
                foreach(var binding in AnimationUtility.GetCurveBindings(g.sourceClip))
                    if(binding.propertyName=="RootT.x" || binding.propertyName=="RootT.z")
                        Assert.That(AnimationUtility.GetEditorCurve(g.sourceClip,binding).keys.All(k=>k.value==0),Is.True);
            }
            var clone=Object.Instantiate(p);
            try {clone.maximumBackwardSpeed=6;Assert.That(clone.Validate(p.targetModel,p.footProjection,out _),Is.False);}
            finally{Object.DestroyImmediate(clone);}
        }
        [Test] public void FacingLeaseCapturesWhileMovingAndPreservesEveryDirectionThroughOwnerBoundaries()
        {
            using var f=new AnimationPlaygroundStandingStrafeTests.InputFixture();var motor=f.Actor.AddComponent<TopDown3DPlayerMotor>();
            typeof(TopDown3DPlayerMotor).GetField("input",Private).SetValue(motor,f.Router);
            var owner=new object();motor.BindAnimationPostureOwner(owner);motor.ConfigureLaboratoryStandingStrafe(owner,Profile(),()=>true);
            typeof(TopDown3DPlayerMotor).GetProperty("IsGrounded").SetValue(motor,true);
            var body=f.Actor.GetComponent<Rigidbody>();body.rotation=Quaternion.Euler(0,37,0);body.linearVelocity=Vector3.forward*5;
            Vector3 Apply(Vector3 d){var a=new object[]{d};typeof(TopDown3DPlayerMotor).GetMethod("ApplyStandingStrafeIntent",Private).Invoke(motor,a);return (Vector3)a[0];}
            Apply(Vector3.zero);f.PadState(new GamepadState{leftTrigger=1});
            foreach(var angle in new[]{0f,10,22,23,45,90,135,180,225,270,315})
            {
                var d=Quaternion.Euler(0,angle+37,0)*Vector3.forward*.37f+Vector3.up*.08f;
                Assert.That(Apply(d),Is.EqualTo(d));Assert.That(motor.LaboratoryStandingFacingLocked,Is.True);
                Assert.That(motor.LaboratoryStandingDirectionUnsupported,Is.False);
                var expected=AnimationPlaygroundDirectionalMath.SpeedLimit(Profile(),new Vector2(Mathf.Sin(angle*Mathf.Deg2Rad),Mathf.Cos(angle*Mathf.Deg2Rad)));
                var limited=(float)typeof(TopDown3DPlayerMotor).GetMethod("StandingStrafeRequestedSpeed",Private).Invoke(motor,new object[]{6f});
                Assert.That(limited,Is.EqualTo(expected).Within(.00001f));
                Assert.That(Vector3.Angle((Vector3)typeof(TopDown3DPlayerMotor).GetField("standingStrafeFacing",Private).GetValue(motor),body.rotation*Vector3.forward),Is.LessThan(.001f));
            }
            f.PadState(new GamepadState());Assert.That(Apply(Vector3.back),Is.EqualTo(Vector3.back));Assert.That(motor.LaboratoryStandingFacingLocked,Is.False);
            f.PadState(new GamepadState{leftTrigger=1});Apply(Vector3.left);
            Assert.That(motor.TryBeginActionConstraint(owner),Is.True);Assert.That(motor.LaboratoryStandingFacingLocked,Is.False);
            motor.EndActionConstraint(owner);Apply(Vector3.left);Assert.That(motor.LaboratoryStandingFacingLocked,Is.False);
            f.PadState(new GamepadState());Apply(Vector3.zero);f.PadState(new GamepadState{leftTrigger=1});Apply(Vector3.back);
            Assert.That(motor.LaboratoryStandingFacingLocked,Is.True);motor.Teleport(Vector3.zero);Assert.That(motor.LaboratoryStandingFacingLocked,Is.False);
            Assert.That(motor.ReleaseAnimationPostureOwner(owner),Is.True);
        }
        [TestCase(45f)] [TestCase(90f)] [TestCase(135f)] [TestCase(180f)] [TestCase(225f)] [TestCase(270f)] [TestCase(315f)]
        public void DirectionalClockUsesCompletedDistanceAndIgnoresDuplicateBlockedAndRebasedSamples(float heading)
        {
            using var f=new Fixture(SkinWeights.FourBones);f.Flat();f.Lock(true);
            var direction=Quaternion.Euler(0,heading,0)*Vector3.forward;
            void Advance(long tick,bool available=true,bool rebase=false)
            {
                var motion=new AnimationPlaygroundGroundedMotion(tick,17,Time.fixedTimeAsDouble,.02,direction*.06f,Vector3.up,available,rebase);
                typeof(TopDown3DPlayerMotor).GetProperty("LaboratoryGroundedMotion").SetValue(f.Motor,motion);
                typeof(TopDown3DPlayerAnimationDriver).GetMethod("AdvanceForwardMotion",Private).Invoke(f.Driver,new object[]{motion});
            }
            Advance(1,true,true);for(var i=2;i<=101;i++)Advance(i);
            var distance=(double)typeof(TopDown3DPlayerAnimationDriver).GetMethod("DirectionalCycleDistance",Private).Invoke(f.Driver,new object[]{3f});
            Assert.That(f.Driver.LaboratoryForwardCycles,Is.EqualTo(6/distance).Within(.00001));
            var before=f.Driver.LaboratoryForwardCycles;Advance(101);Advance(102,false);Advance(103,true,true);
            Assert.That(f.Driver.LaboratoryForwardCycles,Is.EqualTo(before));
            Assert.That(f.Forward.Get<PlayableGraph>("playableGraph").GetOutputCount(),Is.EqualTo(1));
        }
        [TestCase(SkinWeights.OneBone)] [TestCase(SkinWeights.TwoBones)] [TestCase(SkinWeights.FourBones)]
        public void FullDirectionalFamilyFitsFlatEquippedPosesAndKeepsOneOutput(SkinWeights quality)
        {
            using var f=new Fixture(quality);f.Flat();f.Lock(true);var mesh=new Mesh();
            try
            {
                foreach(var angle in new[]{0f,45,90,135,180,225,270,315})
                foreach(var requested in new[]{.7f,1.1f,1.4f,1.8f,3,4,5,6})
                {
                    var local=new Vector2(Mathf.Sin(angle*Mathf.Deg2Rad),Mathf.Cos(angle*Mathf.Deg2Rad));
                    var speed=Mathf.Min(requested,AnimationPlaygroundDirectionalMath.SpeedLimit(Profile(),local));
                    for(var frame=0;frame<32;frame++)
                    {
                        f.Frame(speed,frame/32d,local,frame==0 ? 1f : .02f);
                        Assert.That(f.Driver.LaboratoryStandingStrafeActive,Is.True);
                        if(frame%8!=0)continue;
                        Assert.That(f.Minimum(mesh,out var height),Is.GreaterThanOrEqualTo(-.001f),$"{angle}/{speed}/{frame}");
                        Assert.That(height,Is.LessThanOrEqualTo(1.821f));
                        Assert.That(f.Forward.Get<PlayableGraph>("playableGraph").GetOutputCount(),Is.EqualTo(1));
                    }
                }
            }
            finally{Object.DestroyImmediate(mesh);}
        }
        internal sealed class Fixture:IDisposable
        {
            public readonly AnimationPlaygroundForwardReviewTests.Fixture Forward;
            public TopDown3DPlayerAnimationDriver Driver=>Forward.Driver;
            public TopDown3DPlayerMotor Motor=>Forward.Motor;
            public Fixture(SkinWeights quality)
            {
                Forward=new AnimationPlaygroundForwardReviewTests.Fixture(false,quality,"Assets/_Project/Settings/AnimationPlayground/PrivateBindings/ExpandedForwardGaitReview.asset");
                Driver.ConfigureLaboratoryForwardReview(Forward.Policy,Forward.Station);Driver.ConfigureLaboratoryStandingStrafeReview(Profile());
                Driver.ConfigureLaboratoryContinuation(AssetDatabase.LoadAssetAtPath<AnimationPlaygroundContinuationProfile>(NativePath));
                Forward.Call("Awake");Forward.Actor.SetActive(true);typeof(TopDown3DPlayerMotor).GetField("input",Private).SetValue(Motor,null);
                Driver.ConfigureCamp(TopDown3DCampSettings.Load());Driver.SetCampPresentation(-1,1,false,true);
                Forward.Actor.transform.SetPositionAndRotation(Vector3.up*.91f,Quaternion.identity);Forward.Player.position=Forward.Actor.transform.position;
                Forward.Player.rotation=Quaternion.identity;Forward.Ground.AddComponent<TopDown3DGroundSurface>();Flat();
            }
            public void Flat()
            {
                Forward.Ground.SetActive(true);Forward.Ground.transform.SetPositionAndRotation(Vector3.down*.05f,Quaternion.identity);
                Forward.Ground.GetComponent<BoxCollider>().size=new Vector3(20,.1f,20);
                SetSupport(new Plane(Vector3.up,Vector3.zero),Forward.Ground.GetComponent<Collider>());Physics.SyncTransforms();
            }
            public void SetSupport(Plane plane,Collider support)
            {
                void Set(string n,object v)=>typeof(TopDown3DPlayerMotor).GetField(n,Private).SetValue(Motor,v);
                Set("hasCompletedLaboratoryGroundPlane",true);Set("completedLaboratoryGroundPlane",plane);Set("observedGroundSupport",support);Set("laboratoryGroundSupport",support);
                Set("observedSupportMatrix",support.transform.localToWorldMatrix);Set("facingDirection",Forward.Player.rotation*Vector3.forward);
                typeof(TopDown3DPlayerMotor).GetProperty("IsGrounded").SetValue(Motor,true);
                typeof(TopDown3DPlayerMotor).GetProperty("LaboratoryGroundedMotion").SetValue(Motor,new AnimationPlaygroundGroundedMotion(1,1,Time.fixedTimeAsDouble,.02,Vector3.zero,plane.normal,true,false));
            }
            public void Lock(bool value)=>typeof(TopDown3DPlayerMotor).GetField("standingStrafeLocked",Private).SetValue(Motor,value);
            public void Frame(float speed,double cycles,Vector2 direction,float dt=.02f)
            {
                Forward.Call("PrepareForwardGraphUpdate");typeof(TopDown3DPlayerAnimationDriver).GetField("directionalLocal",Private).SetValue(Driver,direction);
                try {typeof(TopDown3DPlayerAnimationDriver).GetMethod("PresentDirectionalFrame",Private).Invoke(Driver,new object[]{speed,cycles,dt});Forward.Call("LateUpdate");}
                catch(TargetInvocationException error){throw new InvalidOperationException($"Directional pose {speed}/{cycles}/{direction}: {error.InnerException?.Message}",error);}
            }
            public float Minimum(Mesh mesh,out float height)
            {
                var minimum=float.PositiveInfinity;height=float.NegativeInfinity;
                foreach(var skin in Forward.Get<GameObject>("visualInstance").GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    skin.BakeMesh(mesh,true);foreach(var v in mesh.vertices){var p=skin.transform.TransformPoint(v);minimum=Mathf.Min(minimum,p.y);height=Mathf.Max(height,p.y);}
                }
                return minimum;
            }
            public void Dispose()=>Forward.Dispose();
        }
    }
}
