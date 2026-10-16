using System;
using System.Linq;
using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using Object=UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class AnimationPlaygroundNativeGroundTests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        // Spawn, typical grade, steepest sampled grade, largest local residual,
        // and largest normal change in the 320 m native-camp audit.
        internal static readonly Vector2[] Sites={new(-1848,-1498),new(-1848,-1546),new(-1840,-1394),new(-1976,-1370),new(-1880,-1506)};
        [TestCase(SkinWeights.OneBone)] [TestCase(SkinWeights.TwoBones)] [TestCase(SkinWeights.FourBones)]
        public void AcceptedFamilyAndDirectionalRunningRemainSelectedOnActualMainCampTerrain(SkinWeights quality)
        {
            using var f=new NativeFixture(quality);var minimum=float.PositiveInfinity;var bodyMinimum=float.PositiveInfinity;var adapted=0;var poses=0;
            var worstSole="";var worstBody="";
            var mesh=new Mesh();
            try {
            foreach(var site in Sites)
            foreach(var facing in new[]{0f,90,180,270})
            foreach(var heading in new[]{0f,45,90,135,180,225,270,315})
            foreach(var requested in new[]{1.1f,1.4f,3,5,6,6.15f})
            {
                f.Place(site,facing);var local=new Vector2(Mathf.Sin(heading*Mathf.Deg2Rad),Mathf.Cos(heading*Mathf.Deg2Rad));
                var speed=Mathf.Min(requested,AnimationPlaygroundDirectionalMath.SpeedLimit(AnimationPlaygroundDirectionalTests.Profile(),local));
                f.Inner.Lock(true);
                Assert.That(f.Inner.Driver.StandingStrafeReviewSupportAvailable(),Is.True,$"Main camp selection {site}/{heading}/{speed}");
                for(var phase=0;phase<8;phase++)
                {
                    f.Inner.Frame(speed,phase/8d,local,1);poses++;
                    if(f.Inner.Driver.LaboratoryNativeGroundAdapted)adapted++;
                    Assert.That(f.Inner.Driver.LaboratoryStandingStrafeActive,Is.True);
                    Assert.That(f.Inner.Driver.LaboratoryNativeGroundAdapted,Is.True,$"{site}/facing {facing}/{heading}/{speed}/{phase}: {f.Inner.Driver.LaboratoryNativeGroundFailure}");
                    Assert.That(f.Inner.Forward.Get<PlayableGraph>("playableGraph").GetOutputCount(),Is.EqualTo(1));
                    Assert.That(f.Inner.Driver.GarmentFloorExceeded,Is.False);
                    if(phase%2==0){var value=f.SoleMinimum();if(value<minimum){minimum=value;worstSole=$"{site}/{facing}/{heading}/{speed}/{phase}";}}
                    if(phase==0 && heading%90==0 && (requested==3 || requested==6 || requested==6.15f))
                    {var value=f.MeshMinimum(mesh);if(value<bodyMinimum){bodyMinimum=value;worstBody=$"{site}/{facing}/{heading}/{speed}/{phase}/{f.MinimumSkin}/{f.MinimumPoint}";}}
                }
            }
            TestContext.Out.WriteLine($"Actual native-ground poses {adapted}/{poses}; minimum sole/collision gap {minimum:R}m; equipped mesh gap {bodyMinimum:R}m.");
            Assert.That(minimum,Is.GreaterThanOrEqualTo(-.002f),worstSole);
            Assert.That(bodyMinimum,Is.GreaterThanOrEqualTo(-.002f),worstBody);
            } finally {Object.DestroyImmediate(mesh);}
        }
        [Test] public void ALocalFootFailureDoesNotRejectTheWholeNativeTerrainTileOrTheForwardFamily()
        {
            using var f=new NativeFixture(SkinWeights.FourBones);f.Place(Sites[0],0);f.Inner.Lock(true);
            var support=f.Inner.Motor.LaboratorySupportCollider;
            typeof(TopDown3DPlayerAnimationDriver).GetField("continuationRejectedSupport",Private).SetValue(f.Inner.Driver,support);
            typeof(TopDown3DPlayerAnimationDriver).GetField("continuationRejectedMatrix",Private).SetValue(f.Inner.Driver,support.transform.localToWorldMatrix);
            Assert.That(f.Inner.Driver.StandingStrafeReviewSupportAvailable(),Is.True);
            f.Inner.Frame(5,.2,Vector2.right,1);Assert.That(f.Inner.Driver.LaboratoryStandingStrafeActive,Is.True);
            f.Inner.Driver.ResetLaboratoryContinuationReview();Assert.That(f.Inner.Driver.LaboratoryNativeGroundFailure,Is.Null);
        }
        [TestCase(SkinWeights.OneBone)] [TestCase(SkinWeights.TwoBones)] [TestCase(SkinWeights.FourBones)]
        public void GroundedJumpAndLandingSourcesUseNativeFootSupportWithTheSameOutput(SkinWeights quality)
        {
            using var f=new NativeFixture(quality);var driver=f.Inner.Driver;var motor=f.Inner.Motor;var mesh=new Mesh();
            var coordinator=f.Inner.Forward.Actor.AddComponent<AnimationPlaygroundActionCoordinator>();
            coordinator.Configure(motor,driver,new AnimationPlaygroundResetRegistry(),new[]{motor.Position});
            var air=AssetDatabase.LoadAssetAtPath<AnimationPlaygroundAirborneProfile>("Assets/_Project/Settings/AnimationPlayground/PrivateBindings/AirborneProfile.asset");
            try
            {
                foreach(var clip in new[]{air.standing.takeoff,air.running.takeoff,air.unarmed.takeoff,air.standing.landing,air.running.landing,air.unarmed.landing,air.hardLanding})
                {
                    var binding=new AnimationPlaygroundMotionBinding{sourceId="OfflineNativeGroundedAir",clip=clip,readiness=AnimationPlaygroundMotionReadiness.PhysicalQualified};
                    Assert.That(coordinator.TryBeginPhysical(binding,AnimationPlaygroundMotionMode.Recovery,null,_=>new AnimationPlaygroundMotorIntent(Vector3.zero,Vector3.forward),null),Is.True);
                    Assert.That(driver.SetAnimationFootProjection(coordinator,true),Is.True);
                    foreach(var site in new[]{Sites[0],Sites[3]})foreach(var yaw in new[]{0f,90,180,270})
                    for(var frame=0;frame<=32;frame+=8)
                    {
                        f.Place(site,yaw);
                        // Actions obtain support through their current contact
                        // probe; no completed locomotion sample is fabricated.
                        typeof(TopDown3DPlayerMotor).GetField("hasCompletedLaboratoryGroundPlane",Private).SetValue(motor,false);
                        f.Inner.Forward.Call("PrepareForwardGraphUpdate");driver.SetAnimationPresentationTime(coordinator,clip.length*frame/32);
                        f.Inner.Forward.Call("PresentAnimationAction");f.Inner.Forward.Call("LateUpdate");
                        Assert.That(driver.LaboratoryNativeGroundAdapted,Is.True,$"{clip.name}/{site}/{yaw}/{frame}: {driver.LaboratoryNativeGroundFailure}");
                        Assert.That(f.MeshMinimum(mesh),Is.GreaterThanOrEqualTo(-.002f),$"{clip.name}/{site}/{yaw}/{frame}/{f.MinimumSkin}");
                        Assert.That(driver.GarmentFloorExceeded,Is.False);
                        Assert.That(f.Inner.Forward.Get<PlayableGraph>("playableGraph").GetOutputCount(),Is.EqualTo(1));
                    }
                    coordinator.Cancel();
                }
            }
            finally{coordinator.DisposeOwnedServices();Object.DestroyImmediate(mesh);}
        }
        internal sealed class NativeFixture:IDisposable
        {
            public readonly AnimationPlaygroundDirectionalTests.Fixture Inner;
            readonly (TerrainCollider collider,Bounds bounds)[] terrains;
            readonly Transform[] bones;
            readonly AnimationPlaygroundSkinPoint[][] vertices;
            public string MinimumSkin;public Vector3 MinimumPoint;
            public NativeFixture(SkinWeights quality)
            {
                Inner=new AnimationPlaygroundDirectionalTests.Fixture(quality);Inner.Forward.Ground.SetActive(false);
                terrains=Inner.Forward.Scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<TerrainCollider>(true))
                    .Select(t=>(t,t.bounds)).ToArray();Assert.That(terrains.Length,Is.EqualTo(9));
                var data=AnimationPlaygroundDirectionalTests.Profile().footProjection;
                var visual=Inner.Forward.Get<GameObject>("visualInstance").transform;
                bones=data.bonePaths.Select(p=>p.Length==0 ? visual : visual.Find(p)).ToArray();
                vertices=data.vertices.Select(v=>v.points.OrderByDescending(p=>p.weight).Take((int)quality).ToArray()).ToArray();
            }
            public bool Hit(Vector3 point,out RaycastHit hit)
            {
                foreach(var terrain in terrains)
                    if(point.x>=terrain.bounds.min.x && point.x<=terrain.bounds.max.x && point.z>=terrain.bounds.min.z && point.z<=terrain.bounds.max.z
                        && terrain.collider.Raycast(new Ray(new Vector3(point.x,2000,point.z),Vector3.down),out hit,2500))return true;
                hit=default;return false;
            }
            public void Place(Vector2 point,float yaw)
            {
                Assert.That(Hit(new Vector3(point.x,0,point.y),out var hit),Is.True);
                var origin=hit.point+Vector3.up*(.91f+.2f*(1/hit.normal.y-1));
                Inner.Forward.Actor.transform.SetPositionAndRotation(origin,Quaternion.Euler(0,yaw,0));
                Inner.Forward.Player.position=origin;Inner.Forward.Player.rotation=Quaternion.Euler(0,yaw,0);
                Inner.SetSupport(new Plane(hit.normal,hit.point),hit.collider);Physics.SyncTransforms();
            }
            public float SoleMinimum()
            {
                var minimum=float.PositiveInfinity;
                foreach(var vertex in vertices)
                {
                    var point=Vector3.zero;var weight=0f;
                    foreach(var influence in vertex){point+=bones[influence.bone].TransformPoint(influence.position)*influence.weight;weight+=influence.weight;}
                    point/=weight;Assert.That(Hit(point,out var hit),Is.True);minimum=Mathf.Min(minimum,point.y-hit.point.y);
                }
                return minimum;
            }
            public float MeshMinimum(Mesh mesh)
            {
                var minimum=float.PositiveInfinity;var flat=Inner.Motor.Position.y-.91f;
                foreach(var skin in Inner.Forward.Get<GameObject>("visualInstance").GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    skin.BakeMesh(mesh,true);
                    foreach(var vertex in mesh.vertices)
                    {
                        var point=skin.transform.TransformPoint(vertex);if(point.y-flat>.6f)continue;
                        Assert.That(Hit(point,out var hit),Is.True);
                        if(point.y-hit.point.y<minimum){minimum=point.y-hit.point.y;MinimumSkin=skin.name;MinimumPoint=point;}
                    }
                }
                return minimum;
            }
            public void Dispose()=>Inner.Dispose();
        }
    }
}
