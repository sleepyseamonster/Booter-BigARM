using System;
using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class AnimationPlaygroundProp
    {
        private enum PropEffect { Attach, Release }
        private readonly struct CommittedPropEffect
        {
            public readonly object Owner;
            public readonly uint PropEpoch;
            public readonly PropEffect Kind;
            public CommittedPropEffect(object owner, uint epoch, PropEffect kind)
            { Owner = owner; PropEpoch = epoch; Kind = kind; }
        }
        private readonly struct IgnoredPair
        {
            public readonly Collider Prop, Actor;
            public readonly bool PreviouslyIgnored;
            public IgnoredPair(Collider prop, Collider actor)
            { Prop = prop; Actor = actor; PreviouslyIgnored = Physics.GetIgnoreCollision(prop, actor); }
        }
        private readonly Dictionary<AnimationPlaygroundEffectKey, CommittedPropEffect> committedPropEffects = new();
        private readonly List<IgnoredPair> heldCollisionPairs = new();
        private AnimationPlaygroundPropCollisionGuard releaseGuard;
        private Transform heldAnchor;
        private Collider[] heldActorColliders;
        public bool IsHeld => heldAnchor != null;
        public bool HasReleaseCollisionGuard => releaseGuard != null && releaseGuard.HasPairs;
        public bool IsLeaseOwner(object owner) => owner != null && ReferenceEquals(owner, lease) && HasLease;

        // The coordinator commits these at the owned effect boundary. A replay of an
        // already committed key succeeds without moving, reattaching or throwing twice.
        public bool TryAttach(object owner, uint propEpoch, AnimationPlaygroundEffectKey key,
            Transform handAnchor, Collider[] actorColliders)
        {
            if (!ValidEffectEpoch(propEpoch, key)) return false;
            if (committedPropEffects.TryGetValue(key, out var previous))
                return SameEffect(previous, owner, propEpoch, PropEffect.Attach);
            if (!isActiveAndEnabled || !IsLeaseOwner(owner) || IsHeld || body == null || body.isKinematic || colliders.Length == 0
                || grip == null || !grip.IsChildOf(transform) || handAnchor == null || !handAnchor.gameObject.activeInHierarchy || handAnchor.IsChildOf(transform)
                || handAnchor.gameObject.scene != gameObject.scene || actorColliders == null || actorColliders.Length == 0
                || !TopDown3DAnimationPlaygroundSession.HasUnitPhysicsHierarchy(transform)) return false;
            foreach (var item in actorColliders)
                if (item == null || item.transform.IsChildOf(transform) || item.gameObject.scene != gameObject.scene) return false;
            foreach (var item in colliders) if (item == null) return false;
            RestoreIgnoredPairs();
            heldActorColliders = (Collider[])actorColliders.Clone();
            foreach (var item in colliders) foreach (var actor in heldActorColliders)
                heldCollisionPairs.Add(new IgnoredPair(item, actor));
            body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
            body.isKinematic = true; body.useGravity = false; body.detectCollisions = false;
            foreach (var item in colliders) if (item != null) item.enabled = false;
            heldAnchor = handAnchor;
            FollowHeldAnchor();
            committedPropEffects.Add(key, new CommittedPropEffect(owner, propEpoch, PropEffect.Attach));
            return true;
        }

        public bool TryReleaseHeld(object owner, uint propEpoch, AnimationPlaygroundEffectKey key,
            Vector3 velocity, Vector3 angularVelocity)
        {
            if (!ValidEffectEpoch(propEpoch, key)) return false;
            if (committedPropEffects.TryGetValue(key, out var previous))
                return SameEffect(previous, owner, propEpoch, PropEffect.Release);
            if (!IsLeaseOwner(owner) || !IsHeld || !Finite(velocity) || !Finite(angularVelocity)) return false;
            FollowHeldAnchor();
            ReleaseHeldBody(velocity, angularVelocity);
            lease = null;
            committedPropEffects.Add(key, new CommittedPropEffect(owner, propEpoch, PropEffect.Release));
            return true;
        }

        private bool ValidEffectEpoch(uint propEpoch, AnimationPlaygroundEffectKey key)
            => registry != null && propEpoch == Epoch && key.Epoch == registry.Epoch;
        private static bool SameEffect(CommittedPropEffect effect, object owner, uint epoch, PropEffect kind)
            => owner != null && ReferenceEquals(effect.Owner, owner) && effect.PropEpoch == epoch && effect.Kind == kind;
        private static bool Finite(Vector3 value)
            => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

        // Runs after the existing animation driver has evaluated its one pose graph.
        // The prop stays under its station parent; a scaled skeleton never scales physics.
        private void LateUpdate()
        {
            ReconcileHeldOwner();
            if (IsHeld) FollowHeldAnchor();
        }
        private void FixedUpdate()
        {
            ReconcileHeldOwner();
            if (IsHeld) FollowHeldAnchor();
            RetireSeparatedPairs();
        }
        private void FollowHeldAnchor()
        {
            if (heldAnchor == null || grip == null || body == null) return;
            var relativeRotation = Quaternion.Inverse(transform.rotation) * grip.rotation;
            transform.rotation = heldAnchor.rotation * Quaternion.Inverse(relativeRotation);
            transform.position += heldAnchor.position - grip.position;
            body.position = transform.position; body.rotation = transform.rotation;
        }
        private void ReconcileHeldOwner()
        {
            if (heldActorColliders == null) return;
            if (HasLease && heldAnchor != null && grip != null && body != null && heldAnchor.gameObject.activeInHierarchy) return;
            ReleaseHeldBody(Vector3.zero, Vector3.zero); lease = null;
        }
        private void ReleaseHeldBody(Vector3 velocity, Vector3 angularVelocity)
        {
            RestoreIgnoredPairs();
            var releasePairs = heldCollisionPairs.ToArray();
            EndHeldState();
            if (body == null) return;
            body.position = transform.position; body.rotation = transform.rotation;
            body.isKinematic = kinematic; body.useGravity = gravity; body.detectCollisions = collisions;
            if (!body.isKinematic) { body.linearVelocity = velocity; body.angularVelocity = angularVelocity; }
            for (var i = 0; i < colliders.Length; i++) if (colliders[i] != null) colliders[i].enabled = colliderEnabled[i];
            if (releaseGuard == null) releaseGuard = GetComponent<AnimationPlaygroundPropCollisionGuard>()
                ?? gameObject.AddComponent<AnimationPlaygroundPropCollisionGuard>();
            releaseGuard.enabled = true;
            foreach (var pair in releasePairs)
                if (pair.Prop != null && pair.Actor != null) releaseGuard.Add(pair.Prop, pair.Actor, pair.PreviouslyIgnored);
        }
        private void EndHeldState() { heldAnchor = null; heldActorColliders = null; heldCollisionPairs.Clear(); }
        private void RetireSeparatedPairs()
            => releaseGuard?.RetireSeparatedPairs();
        private void RestoreIgnoredPairs()
            => releaseGuard?.RestoreAll();
    }
}
