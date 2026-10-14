using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class AnimationPlaygroundTargetTests
    {
        private GameObject actor, station;
        private TopDown3DPlayerMotor motor;
        private TopDown3DInteractionController selector;
        private TopDown3DAnimationPlaygroundTarget target;
        [SetUp] public void Setup()
        {
            actor=new GameObject("Station actor"); actor.SetActive(false);actor.transform.position=new Vector3(20000,20000,20000);
            motor=actor.AddComponent<TopDown3DPlayerMotor>(); motor.FacePlanarDirection(Vector3.forward);
            selector=actor.AddComponent<TopDown3DInteractionController>();selector.Configure(motor);
            station=new GameObject("Station target");station.transform.position=actor.transform.position+Vector3.forward*2f;
            var owned=new GameObject("Compound collider");owned.transform.SetParent(station.transform,false);
            var collider=owned.AddComponent<BoxCollider>();collider.center=new Vector3(0,.4f,0);
            target=station.AddComponent<TopDown3DAnimationPlaygroundTarget>();
            target.Configure("AP05.button",AnimationPlaygroundActionKind.Button,"Press test button",null,3,new Collider[]{collider});
            Physics.SyncTransforms();
        }
        [TearDown] public void Cleanup(){Object.DestroyImmediate(station);Object.DestroyImmediate(actor);Physics.SyncTransforms();}
        [Test] public void CompoundNonResourceTargetUsesTheSameSelector()
        {
            selector.RefreshTarget();
            Assert.That(selector.SelectedTarget,Is.SameAs(target));
            Assert.That(selector.CurrentTarget,Is.Null,"Station must not be a fake resource deposit.");
            Assert.That(selector.CanReachTarget(target),Is.True);
            var blocked=new GameObject("Unrelated blocker");
            try
            {
                blocked.transform.position=actor.transform.position+new Vector3(0,.5f,1);
                blocked.AddComponent<BoxCollider>();Physics.SyncTransforms();selector.RefreshTarget();
                Assert.That(selector.SelectedTarget,Is.Null);
            } finally {Object.DestroyImmediate(blocked);}
        }
        [Test] public void ResetEpochAndDisableInvalidateCapturedPressWithoutRetargeting()
        {
            var captured=new TopDown3DInteractionTarget(target);Assert.That(captured.IsValid,Is.True);
            target.Invalidate();Assert.That(captured.IsValid,Is.False);
            var fresh=new TopDown3DInteractionTarget(target);target.SetAvailable(false);Assert.That(fresh.IsValid,Is.False);
            target.SetAvailable(true);Assert.That(fresh.IsValid,Is.False,"Re-enable cannot revive a stale press.");
            var latest=new TopDown3DInteractionTarget(target);Object.DestroyImmediate(station);Assert.That(latest.IsValid,Is.False);
        }
        [Test] public void StationDispatchNeverStartsGatheringAndSharesActivityGates()
        {
            var actions=actor.AddComponent<TopDown3DPlayerActionController>();
            actions.Configure(null,selector,null,motor,null);
            var receiver=new Receiver();actions.ConfigureStationActions(receiver);selector.RefreshTarget();
            Assert.That(actions.TryBeginCurrentTarget(),Is.EqualTo(TopDown3DGatherActionResult.Completed));
            Assert.That(receiver.Request.Target,Is.SameAs(target));Assert.That(actions.IsGathering,Is.False);
            Assert.That(actions.IsBusy,Is.True);Assert.That(actions.CanOpenCargo,Is.False);
            Assert.That(actions.TryBeginCurrentTarget(),Is.EqualTo(TopDown3DGatherActionResult.Rejected));
            Assert.That(receiver.Starts,Is.EqualTo(1));
        }
        [TestCase(true,false,false,false,false)]
        [TestCase(false,true,false,false,false)]
        [TestCase(false,false,true,false,false)]
        [TestCase(false,false,false,true,false)]
        [TestCase(false,false,false,false,true)]
        public void EveryExistingAndNewOwnerProjectsBusy(bool legacy,bool placement,bool motion,bool camp,bool stationBusy)
        {Assert.That(new TopDown3DActivitySnapshot(legacy,placement,motion,camp,stationBusy).IsBusy,Is.True);}
        private sealed class Receiver : ITopDown3DStationActionReceiver
        {
            public bool IsBusy {get;private set;}
            public TopDown3DInteractionTarget Request;public int Starts;
            public bool TryBegin(TopDown3DInteractionTarget request){Request=request;IsBusy=true;Starts++;return true;}
        }
    }
}
