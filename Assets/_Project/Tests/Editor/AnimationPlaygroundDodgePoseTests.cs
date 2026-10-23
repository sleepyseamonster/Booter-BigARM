using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class AnimationPlaygroundDodgePoseTests
    {
        [TestCase(SkinWeights.OneBone)] [TestCase(SkinWeights.TwoBones)] [TestCase(SkinWeights.FourBones)]
        public void PostLandingDirectionalDodgeEntryRetainsFootingOnCurvedModerateTerrain(SkinWeights quality)
        {
            var p=AnimationPlaygroundDodgeTests.Profile().Select(3);
            using var f=new AnimationPlaygroundNativeGroundTests.NativeFixture(quality);
            var actions=f.Inner.Forward.Actor.AddComponent<AnimationPlaygroundActionCoordinator>();
            actions.Configure(f.Inner.Motor,f.Inner.Driver,new AnimationPlaygroundResetRegistry(),new[]{f.Inner.Motor.Position});
            var owner=new object();var mesh=new Mesh();
            var binding=new AnimationPlaygroundMotionBinding{sourceId="Post-landing curved entry",clip=p.roll,
                readiness=AnimationPlaygroundMotionReadiness.PhysicalQualified};
            try
            {
                for(var phase=0;phase<16;phase++)
                {
                    f.Place(new Vector2(-1605.70f,-1658.58f),40);f.Inner.Lock(true);f.Inner.Frame(3,phase/16d,Vector2.left,1);
                    Assert.That(f.Inner.Driver.TryCapturePoseBlend(owner,p.entryBlend,true),Is.True);
                    Assert.That(actions.TryBeginPhysical(binding,AnimationPlaygroundMotionMode.Dodge,null,
                        _=>new AnimationPlaygroundMotorIntent(Vector3.zero,Vector3.forward),null,
                        policy:AnimationPlaygroundMotionPolicy.DodgeLocomotion),Is.True);
                    f.Inner.Driver.SetAnimationBodyProjection(actions,p.bodyProjection);
                    f.Inner.Driver.SetAnimationDodgeProjection(actions,Quaternion.Euler(0,310,0)*Vector3.forward);
                    f.Inner.Driver.SetAnimationGarmentFloor(actions,true,p.maximumGarmentProjection,p.garmentClearance);
                    for(var frame=0;frame<=12;frame++)
                    {
                        var time=p.entryBlend*frame/12;f.Inner.Forward.Call("PrepareForwardGraphUpdate");
                        f.Inner.Driver.SetAnimationPresentationTime(actions,time);f.Inner.Driver.SetPoseBlendElapsed(owner,time);
                        f.Inner.Forward.Call("PresentAnimationAction");f.Inner.Forward.Call("LateUpdate");
                        Assert.That(f.MeshMinimum(mesh),Is.GreaterThanOrEqualTo(-.002f),$"{quality}/{phase}/{frame}/{f.MinimumSkin}");
                        Assert.That(f.Inner.Driver.LaboratoryNativeBodyLift,Is.LessThanOrEqualTo(.18f));
                    }
                    f.Inner.Driver.EndPoseBlend(owner);actions.Cancel();
                }
            }
            finally{f.Inner.Driver.EndPoseBlend(owner);actions.DisposeOwnedServices();Object.DestroyImmediate(mesh);}
        }
        [TestCase(SkinWeights.OneBone)] [TestCase(SkinWeights.TwoBones)] [TestCase(SkinWeights.FourBones)]
        public void CompleteDodgeFitsEquippedPosesOnTheNativeCampTerrain(SkinWeights quality)
        {
            var p = AnimationPlaygroundDodgeTests.Profile(); using var f = new AnimationPlaygroundNativeGroundTests.NativeFixture(quality);
            f.Place(AnimationPlaygroundNativeGroundTests.Sites[0], 0);
            var actions = f.Inner.Forward.Actor.AddComponent<AnimationPlaygroundActionCoordinator>();
            actions.Configure(f.Inner.Motor, f.Inner.Driver, new AnimationPlaygroundResetRegistry(), new[] { f.Inner.Motor.Position });
            var binding = new AnimationPlaygroundMotionBinding { sourceId = "OfflineQualifiedDodge", clip = p.roll,
                readiness = AnimationPlaygroundMotionReadiness.PhysicalQualified };
            var mesh = new Mesh(); var minimum = float.PositiveInfinity; var count = 0;
            try
            {
                Assert.That(actions.TryBeginPhysical(binding, AnimationPlaygroundMotionMode.Dodge, null,
                    _ => new AnimationPlaygroundMotorIntent(Vector3.zero, Vector3.forward), null,
                    policy: AnimationPlaygroundMotionPolicy.DodgeLocomotion), Is.True);
                Assert.That(f.Inner.Driver.SetAnimationBodyProjection(actions, p.bodyProjection), Is.True);
                Assert.That(f.Inner.Driver.SetAnimationDodgeProjection(actions), Is.True);
                Assert.That(f.Inner.Driver.SetAnimationGarmentFloor(actions, true, p.maximumGarmentProjection, p.garmentClearance), Is.True);
                foreach (var site in AnimationPlaygroundNativeGroundTests.Sites)
                foreach (var yaw in new[] { 0f, 45, 90, 135, 180, 225, 270, 315 })
                for (var frame = 0; frame <= 32; frame++)
                {
                    f.Place(site, yaw); f.Inner.Forward.Call("PrepareForwardGraphUpdate");
                    f.Inner.Driver.SetAnimationPresentationTime(actions, p.roll.length * frame / 32);
                    f.Inner.Forward.Call("PresentAnimationAction"); f.Inner.Forward.Call("LateUpdate"); count++;
                    Assert.That(f.Inner.Driver.LaboratoryNativeGroundAdapted, Is.True, f.Inner.Driver.LaboratoryNativeGroundFailure);
                    Assert.That(f.Inner.Driver.GarmentFloorExceeded, Is.False, $"{quality}/{site}/{yaw}/{frame}");
                    Assert.That(f.Inner.Forward.Get<PlayableGraph>("playableGraph").GetOutputCount(), Is.EqualTo(1));
                    if (frame % 4 != 0) continue;
                    var gap = f.MeshMinimum(mesh); minimum = Mathf.Min(minimum, gap);
                    Assert.That(gap, Is.GreaterThanOrEqualTo(-.002f), $"{quality}/{site}/{yaw}/{frame}/{f.MinimumSkin}");
                }
                TestContext.Out.WriteLine($"Dodge native poses {count}; minimum sampled equipped mesh gap {minimum:R} m.");
            }
            finally { actions.DisposeOwnedServices(); Object.DestroyImmediate(mesh); }
        }

        [TestCase(SkinWeights.OneBone)] [TestCase(SkinWeights.TwoBones)] [TestCase(SkinWeights.FourBones)]
        public void EntryAndRunOutBlendsKeepTheSameEquippedOutputAndGroundEnvelope(SkinWeights quality)
        {
            var p = AnimationPlaygroundDodgeTests.Profile(); using var f = new AnimationPlaygroundNativeGroundTests.NativeFixture(quality);
            var actions = f.Inner.Forward.Actor.AddComponent<AnimationPlaygroundActionCoordinator>();
            actions.Configure(f.Inner.Motor, f.Inner.Driver, new AnimationPlaygroundResetRegistry(), new[] { f.Inner.Motor.Position });
            var owner = new object(); var mesh = new Mesh();
            var binding = new AnimationPlaygroundMotionBinding { sourceId = "OfflineDodgeBlend", clip = p.roll,
                readiness = AnimationPlaygroundMotionReadiness.PhysicalQualified };
            try
            {
                foreach (var site in new[] { AnimationPlaygroundNativeGroundTests.Sites[0], AnimationPlaygroundNativeGroundTests.Sites[3] })
                foreach (var speed in new[] { 0f, 1.4f, 3, 4, 4.9f, 6.15f })
                foreach (var phase in new[] { 0d, .25, .5, .75 })
                {
                    f.Place(site, 0); f.Inner.Lock(true); f.Inner.Frame(speed, phase, Vector2.up, 1);
                    Assert.That(f.Inner.Driver.TryCapturePoseBlend(owner, p.entryBlend), Is.True);
                    Assert.That(actions.TryBeginPhysical(binding, AnimationPlaygroundMotionMode.Dodge, null,
                        _ => new AnimationPlaygroundMotorIntent(Vector3.zero, Vector3.forward), null), Is.True);
                    f.Inner.Driver.SetAnimationBodyProjection(actions, p.bodyProjection);
                    f.Inner.Driver.SetAnimationDodgeProjection(actions);
                    f.Inner.Driver.SetAnimationGarmentFloor(actions, true, p.maximumGarmentProjection, p.garmentClearance);
                    for (var frame = 0; frame <= 12; frame++)
                    {
                        var time = p.entryBlend * frame / 12; f.Inner.Forward.Call("PrepareForwardGraphUpdate");
                        f.Inner.Driver.SetAnimationPresentationTime(actions, time); f.Inner.Driver.SetPoseBlendElapsed(owner, time);
                        f.Inner.Forward.Call("PresentAnimationAction"); f.Inner.Forward.Call("LateUpdate");
                        Assert.That(f.MeshMinimum(mesh), Is.GreaterThanOrEqualTo(-.002f), $"Entry {quality}/{site}/{speed}/{phase}/{frame}/{f.MinimumSkin}");
                    }
                    f.Inner.Driver.EndPoseBlend(owner); f.Inner.Driver.SetAnimationPresentationTime(actions, p.roll.length);
                    f.Inner.Forward.Call("PresentAnimationAction"); f.Inner.Forward.Call("LateUpdate");
                    Assert.That(f.Inner.Driver.TryCapturePoseBlend(owner, p.exitBlend), Is.True); actions.Cancel();
                    for (var frame = 0; frame <= 12; frame++)
                    {
                        var time = p.exitBlend * frame / 12; f.Inner.Driver.SetPoseBlendElapsed(owner, time);
                        f.Inner.Forward.Player.linearVelocity = Vector3.forward * speed;
                        f.Inner.Forward.Call("PrepareForwardGraphUpdate");
                        f.Inner.Forward.Call("PresentStandingPoseBlend"); f.Inner.Forward.Call("LateUpdate");
                        Assert.That(f.MeshMinimum(mesh), Is.GreaterThanOrEqualTo(-.002f), $"Run-out {quality}/{site}/{speed}/{phase}/{frame}/{f.MinimumSkin}");
                        Assert.That(f.Inner.Driver.GarmentFloorExceeded, Is.False);
                    }
                    f.Inner.Driver.EndPoseBlend(owner);
                    Assert.That(f.Inner.Forward.Get<PlayableGraph>("playableGraph").GetOutputCount(), Is.EqualTo(1));
                }
            }
            finally { f.Inner.Driver.EndPoseBlend(owner); actions.DisposeOwnedServices(); Object.DestroyImmediate(mesh); }
        }

        [TestCase(SkinWeights.OneBone)] [TestCase(SkinWeights.TwoBones)] [TestCase(SkinWeights.FourBones)]
        public void DirectionalWorldHeadingBlendsFitTheNativeCampWithoutChangingTheActorFacing(SkinWeights quality)
        {
            var p = AnimationPlaygroundDodgeTests.Profile(); using var f = new AnimationPlaygroundNativeGroundTests.NativeFixture(quality);
            var actions = f.Inner.Forward.Actor.AddComponent<AnimationPlaygroundActionCoordinator>();
            actions.Configure(f.Inner.Motor, f.Inner.Driver, new AnimationPlaygroundResetRegistry(), new[] { f.Inner.Motor.Position });
            var owner = new object(); var mesh = new Mesh();
            var binding = new AnimationPlaygroundMotionBinding { sourceId = "OfflineWorldHeadingDodge", clip = p.roll,
                readiness = AnimationPlaygroundMotionReadiness.PhysicalQualified };
            var at = typeof(TopDown3DPlayerAnimationDriver).GetMethod("PresentDodgeRunOutBlendAt", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            try
            {
                foreach (var site in AnimationPlaygroundNativeGroundTests.Sites)
                foreach (var speed in new[] { 4.9f, 6.15f })
                foreach (var phase in new[] { 0d, .5 })
                for (var heading = 0; heading < 360; heading += 45)
                {
                    f.Place(site, 0); f.Inner.Lock(true); f.Inner.Frame(speed, phase, Vector2.up, 1);
                    var direction = Quaternion.Euler(0, heading, 0) * Vector3.forward;
                    Assert.That(f.Inner.Driver.TryCapturePoseBlend(owner, p.entryBlend), Is.True);
                    Assert.That(actions.TryBeginPhysical(binding, AnimationPlaygroundMotionMode.Dodge, null,
                        _ => new AnimationPlaygroundMotorIntent(Vector3.zero, Vector3.forward), null), Is.True);
                    f.Inner.Driver.SetAnimationBodyProjection(actions, p.bodyProjection);
                    f.Inner.Driver.SetAnimationDodgeProjection(actions, direction);
                    f.Inner.Driver.SetAnimationGarmentFloor(actions, true, p.maximumGarmentProjection, p.garmentClearance);
                    for (var frame = 0; frame <= 8; frame++)
                    {
                        var time = p.entryBlend * frame / 8; f.Inner.Forward.Call("PrepareForwardGraphUpdate");
                        f.Inner.Driver.SetAnimationPresentationTime(actions, time); f.Inner.Driver.SetPoseBlendElapsed(owner, time);
                        f.Inner.Forward.Call("PresentAnimationAction"); f.Inner.Forward.Call("LateUpdate");
                        Assert.That(f.Inner.Driver.GarmentFloorExceeded, Is.False);
                        if (frame % 2 == 0) Assert.That(f.MeshMinimum(mesh), Is.GreaterThanOrEqualTo(-.002f), $"Entry {quality}/{site}/{speed}/{phase}/{heading}/{frame}/{f.MinimumSkin}");
                    }
                    f.Inner.Driver.EndPoseBlend(owner); f.Inner.Driver.SetAnimationPresentationTime(actions, p.roll.length);
                    f.Inner.Forward.Call("PresentAnimationAction"); f.Inner.Forward.Call("LateUpdate");
                    Assert.That(f.Inner.Driver.TryCapturePoseBlend(owner, p.exitBlend), Is.True); actions.Cancel();
                    f.Inner.Forward.Player.linearVelocity = direction * speed;
                    for (var frame = 0; frame <= 8; frame++)
                    {
                        var time = p.exitBlend * frame / 8; f.Inner.Driver.SetPoseBlendElapsed(owner, time);
                        f.Inner.Forward.Call("PrepareForwardGraphUpdate");
                        Assert.That((bool)at.Invoke(f.Inner.Driver, new object[] { .5f, p.exitBlend / 8 }), Is.True);
                        f.Inner.Forward.Call("LateUpdate");
                        Assert.That(f.Inner.Driver.GarmentFloorExceeded, Is.False);
                        if (frame % 2 == 0) Assert.That(f.MeshMinimum(mesh), Is.GreaterThanOrEqualTo(-.002f), $"Recovery {quality}/{site}/{speed}/{phase}/{heading}/{frame}/{f.MinimumSkin}");
                        Assert.That(Quaternion.Angle(f.Inner.Forward.Player.rotation, Quaternion.identity), Is.LessThan(.01f));
                    }
                    f.Inner.Driver.EndPoseBlend(owner);
                }
            }
            finally { f.Inner.Driver.EndPoseBlend(owner); actions.DisposeOwnedServices(); Object.DestroyImmediate(mesh); }
        }
    }
}
