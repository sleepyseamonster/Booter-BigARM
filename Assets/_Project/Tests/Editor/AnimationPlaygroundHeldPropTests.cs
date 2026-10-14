using System.Reflection;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BooterBigArm.Tests
{
    public sealed class AnimationPlaygroundHeldPropTests
    {
        private GameObject station, actor;
        private AnimationPlaygroundProp prop;
        private Rigidbody body;
        private BoxCollider collider, actorCollider;
        private Transform hand;
        private object owner;
        private AnimationPlaygroundResetRegistry registry;
        private AnimationPlaygroundEffectKey attach, release;
        [SetUp] public void SetUp()
        {
            station = new GameObject("Held prop authored station");
            var item = new GameObject("Scrap"); item.transform.SetParent(station.transform);
            body = item.AddComponent<Rigidbody>(); body.useGravity = true;
            collider = item.AddComponent<BoxCollider>(); collider.size = Vector3.one * .2f;
            var grip = new GameObject("Grip").transform; grip.SetParent(item.transform);
            grip.localPosition = new Vector3(.04f, .1f, -.03f); grip.localRotation = Quaternion.Euler(12, 28, 4);
            prop = item.AddComponent<AnimationPlaygroundProp>(); prop.Configure("AP05.HeldFixture", grip);
            registry = new AnimationPlaygroundResetRegistry(); prop.Bind(registry);
            actor = new GameObject("Unscaled actor");
            actorCollider = actor.AddComponent<BoxCollider>(); actorCollider.size = Vector3.one * 2;
            hand = new GameObject("Hand fixture").transform; hand.SetParent(actor.transform);
            hand.localPosition = new Vector3(.2f, .5f, 0); hand.localRotation = Quaternion.Euler(31, -14, 22);
            owner = new object();
            attach = new AnimationPlaygroundEffectKey(1, registry.Epoch, 0, 0);
            release = new AnimationPlaygroundEffectKey(2, registry.Epoch, 0, 0);
            Assert.That(prop.TryAcquire(owner), Is.True);
        }
        [TearDown] public void TearDown()
        { Object.DestroyImmediate(station); Object.DestroyImmediate(actor); Physics.SyncTransforms(); }
        private bool Attach(object lease = null) => prop.TryAttach(lease ?? owner, prop.Epoch, attach, hand, new Collider[] { actorCollider });
        private static void Tick(AnimationPlaygroundProp value, string method)
            => typeof(AnimationPlaygroundProp).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(value, null);

        [Test] public void AttachAlignsAuthoredGripAndFollowsHandWithoutParentingOrScalingPhysics()
        {
            Assert.That(Attach(), Is.True);
            Assert.That(prop.IsHeld, Is.True); Assert.That(body.isKinematic, Is.True);
            Assert.That(body.useGravity, Is.False); Assert.That(body.detectCollisions, Is.False);
            Assert.That(collider.enabled, Is.False); Assert.That(prop.transform.parent, Is.EqualTo(station.transform));
            Assert.That(Vector3.Distance(prop.Grip.position, hand.position), Is.LessThan(.00001f));
            Assert.That(Quaternion.Angle(prop.Grip.rotation, hand.rotation), Is.LessThan(.01f));
            actor.transform.localScale = Vector3.one * 1.7f;
            hand.localPosition += Vector3.right; hand.localRotation = Quaternion.Euler(60, 45, -10);
            Tick(prop, "LateUpdate");
            Assert.That(Vector3.Distance(prop.Grip.position, hand.position), Is.LessThan(.00001f));
            Assert.That(Quaternion.Angle(prop.Grip.rotation, hand.rotation), Is.LessThan(.01f));
            Assert.That(Vector3.Distance(prop.transform.lossyScale, Vector3.one), Is.LessThan(.00001f));
        }
        [Test] public void EffectReplaysCannotReattachOrThrowThePropTwice()
        {
            Assert.That(Attach(), Is.True);
            Assert.That(prop.TryReleaseHeld(owner, prop.Epoch, release, new Vector3(0, 2, 6), Vector3.up), Is.True);
            var position = prop.transform.position;
            Assert.That(Attach(), Is.True, "An acknowledged replay must not reattach.");
            Assert.That(prop.IsHeld, Is.False); Assert.That(prop.transform.position, Is.EqualTo(position));
            body.linearVelocity = Vector3.zero;
            Assert.That(prop.TryReleaseHeld(owner, prop.Epoch, release, Vector3.right * 100, Vector3.zero), Is.True);
            Assert.That(body.linearVelocity, Is.EqualTo(Vector3.zero));
            Assert.That(prop.HasLease, Is.False); Assert.That(body.isKinematic, Is.False);
            Assert.That(body.useGravity, Is.True); Assert.That(collider.enabled, Is.True);
            Assert.That(Attach(new object()), Is.False);
            Assert.That(prop.TryAttach(owner, prop.Epoch, release, hand, new Collider[] { actorCollider }), Is.False);
        }
        [Test] public void ReleaseGuardsSelfCollisionUntilSeparationThenRestoresPairs()
        {
            Assert.That(Attach(), Is.True);
            Assert.That(prop.TryReleaseHeld(owner, prop.Epoch, release, Vector3.zero, Vector3.zero), Is.True);
            Physics.SyncTransforms(); Tick(prop, "FixedUpdate");
            Assert.That(prop.HasReleaseCollisionGuard, Is.True); Assert.That(Physics.GetIgnoreCollision(collider, actorCollider), Is.True);
            prop.transform.position += Vector3.right * 5; Physics.SyncTransforms(); Tick(prop, "FixedUpdate");
            Assert.That(prop.HasReleaseCollisionGuard, Is.False); Assert.That(Physics.GetIgnoreCollision(collider, actorCollider), Is.False);
        }
        [Test] public void ReleasePreservesAPreexistingIgnorePair()
        {
            Physics.IgnoreCollision(collider, actorCollider, true);
            Assert.That(Attach(), Is.True);
            Assert.That(prop.TryReleaseHeld(owner, prop.Epoch, release, Vector3.zero, Vector3.zero), Is.True);
            prop.transform.position += Vector3.right * 5; Physics.SyncTransforms(); Tick(prop, "FixedUpdate");
            Assert.That(Physics.GetIgnoreCollision(collider, actorCollider), Is.True);
        }
        [Test] public void ResetDetachesRestoresAuthoredStateAndRejectsOldEffectEpochs()
        {
            var propEpoch = prop.Epoch;
            Assert.That(Attach(), Is.True); Assert.That(registry.TryReset(), Is.True, registry.LastFailure);
            Assert.That(prop.IsHeld, Is.False); Assert.That(prop.HasLease, Is.False);
            Assert.That(prop.transform.localPosition, Is.EqualTo(Vector3.zero)); Assert.That(collider.enabled, Is.True);
            Assert.That(body.isKinematic, Is.False); Assert.That(Physics.GetIgnoreCollision(collider, actorCollider), Is.False);
            Assert.That(prop.TryAcquire(owner), Is.True);
            Assert.That(prop.TryAttach(owner, propEpoch, attach, hand, new Collider[] { actorCollider }), Is.False);
            Assert.That(prop.TryAttach(owner, prop.Epoch, attach, hand, new Collider[] { actorCollider }), Is.False);
        }
        [TestCase(false)][TestCase(true)] public void LostAnchorOrOwnerSafelyDropsInsteadOfLeavingKinematicBody(bool destroyOwner)
        {
            var leaseObject = new GameObject("Lease owner");
            Assert.That(prop.Release(owner), Is.True); Assert.That(prop.TryAcquire(leaseObject), Is.True);
            Assert.That(Attach(leaseObject), Is.True);
            if (destroyOwner) Object.DestroyImmediate(leaseObject);
            else Object.DestroyImmediate(hand.gameObject);
            Tick(prop, "LateUpdate");
            Assert.That(prop.IsHeld, Is.False); Assert.That(prop.HasLease, Is.False); Assert.That(body.isKinematic, Is.False);
            Assert.That(collider.enabled, Is.True); Assert.That(body.useGravity, Is.True);
            if (leaseObject != null) Object.DestroyImmediate(leaseObject);
        }
        [TestCase(false)][TestCase(true)] public void CancelLeaseAndUnbindReleaseHeldBody(bool unbind)
        {
            Assert.That(Attach(), Is.True);
            if (unbind) prop.Unbind(); else Assert.That(prop.Release(owner), Is.True);
            Assert.That(prop.IsHeld, Is.False); Assert.That(prop.HasLease, Is.False);
            Assert.That(body.isKinematic, Is.False); Assert.That(body.useGravity, Is.True); Assert.That(collider.enabled, Is.True);
            Assert.That(Physics.GetIgnoreCollision(collider, actorCollider), Is.True);
        }
        [Test] public void ForeignOwnerAndInvalidGeometryHaveNoPhysicalSideEffects()
        {
            Assert.That(Attach(new object()), Is.False);
            Assert.That(prop.TryAttach(owner, prop.Epoch, attach, prop.Grip, new Collider[] { actorCollider }), Is.False);
            Assert.That(prop.TryAttach(owner, prop.Epoch, attach, hand, new Collider[] { null }), Is.False);
            station.transform.localScale = Vector3.one * 2;
            Assert.That(Attach(), Is.False);
            Assert.That(prop.IsHeld, Is.False); Assert.That(body.isKinematic, Is.False); Assert.That(collider.enabled, Is.True);
        }
        [Test] public void InvalidReleaseVelocityDoesNotConsumeLeaseOrEffect()
        {
            Assert.That(Attach(), Is.True);
            Assert.That(prop.TryReleaseHeld(owner, prop.Epoch, release, Vector3.one * float.NaN, Vector3.zero), Is.False);
            Assert.That(prop.IsHeld, Is.True); Assert.That(prop.IsLeaseOwner(owner), Is.True);
            Assert.That(prop.TryReleaseHeld(owner, prop.Epoch, release, Vector3.zero, Vector3.zero), Is.True);
        }
        [Test] public void CancelAllAndDisableRestorePhysicsWithoutRepositioningTheActor()
        {
            Assert.That(Attach(), Is.True);
            var actorPosition = actor.transform.position;
            registry.CancelAll();
            Assert.That(prop.IsHeld, Is.False); Assert.That(body.isKinematic, Is.False); Assert.That(collider.enabled, Is.True);
            Assert.That(prop.HasReleaseCollisionGuard, Is.True);
            Assert.That(actor.transform.position, Is.EqualTo(actorPosition));
            Assert.That(prop.TryAcquire(owner), Is.True);
            attach = new AnimationPlaygroundEffectKey(3, registry.Epoch, 0, 0);
            Assert.That(Attach(), Is.True); prop.enabled = false; Tick(prop, "OnDisable");
            Assert.That(prop.IsHeld, Is.False); Assert.That(body.isKinematic, Is.False); Assert.That(collider.enabled, Is.True);
            Assert.That(prop.HasLease, Is.False); Assert.That(prop.HasReleaseCollisionGuard, Is.True);
            Assert.That(prop.TryAcquire(owner), Is.False);
            prop.transform.position += Vector3.right * 5; Physics.SyncTransforms();
            var guard = prop.GetComponent<AnimationPlaygroundPropCollisionGuard>();
            typeof(AnimationPlaygroundPropCollisionGuard).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(guard, null);
            Assert.That(prop.HasReleaseCollisionGuard, Is.False); Assert.That(Physics.GetIgnoreCollision(collider, actorCollider), Is.False);
        }
        [Test] public void DeadLeaseOwnerAndInactiveHandCannotAcquirePhysicalOwnership()
        {
            var dead = new GameObject("Destroyed owner"); Object.DestroyImmediate(dead);
            Assert.That(prop.Release(owner), Is.True); Assert.That(prop.TryAcquire(dead), Is.False);
            Assert.That(prop.TryAcquire(owner), Is.True); hand.gameObject.SetActive(false);
            Assert.That(Attach(), Is.False); Assert.That(body.isKinematic, Is.False); Assert.That(collider.enabled, Is.True);
        }
        [Test] public void RebindKeepsAuthoredResetPoseAndInvalidatesEarlierPropCaptures()
        {
            var oldEpoch = prop.Epoch;
            Assert.That(Attach(), Is.True); prop.Unbind();
            prop.transform.position = Vector3.right * 10;
            var nextRegistry = new AnimationPlaygroundResetRegistry(); prop.Bind(nextRegistry);
            Assert.That(prop.TryAcquire(owner), Is.True);
            Assert.That(prop.TryAttach(owner, oldEpoch, attach, hand, new Collider[] { actorCollider }), Is.False);
            Assert.That(nextRegistry.TryReset(), Is.True);
            Assert.That(prop.transform.localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(prop.HasReleaseCollisionGuard, Is.False); Assert.That(body.isKinematic, Is.False);
        }
    }
}
