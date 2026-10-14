using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)]
    public sealed partial class AnimationPlaygroundProp : MonoBehaviour, IAnimationPlaygroundResetParticipant
    {
        [SerializeField] private string stableId;
        [SerializeField] private Transform grip, contact;
        private AnimationPlaygroundResetRegistry registry;
        private object lease;
        private Transform authoredParent;
        private Vector3 authoredPosition, authoredScale;
        private Quaternion authoredRotation;
        private Rigidbody body;
        private bool kinematic, gravity, collisions;
        private Collider[] colliders;
        private bool[] colliderEnabled;
        private bool authoredStateCaptured;
        public string ResetId => "CampLoop.Prop." + stableId;
        public string StableId => stableId;
        public uint Epoch { get; private set; }
        public Transform Grip => grip;
        public Transform Contact => contact;
        public bool HasLease => lease != null && (!(lease is UnityEngine.Object value) || value != null);
        public AnimationPlaygroundResetStage ResetStage => AnimationPlaygroundResetStage.HeldAndSpawnedProps;

        public void Configure(string id, Transform gripAnchor = null, Transform contactAnchor = null)
        {
            if (registry != null || string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Author a stable prop identity before binding.");
            stableId = id; grip = gripAnchor; contact = contactAnchor;
        }
        public void Bind(AnimationPlaygroundResetRegistry owner)
        {
            if (owner == null || string.IsNullOrWhiteSpace(stableId)) throw new ArgumentException("The prop requires an identity and reset registry.");
            if (registry == owner) return;
            if (registry != null) throw new InvalidOperationException("A prop cannot belong to two sessions.");
            owner.Register(this); registry = owner;
            if (authoredStateCaptured) return; // Rebinding must not promote a dropped pose to authored reset state.
            authoredParent = transform.parent; authoredPosition = transform.localPosition; authoredRotation = transform.localRotation; authoredScale = transform.localScale;
            body = GetComponent<Rigidbody>();
            if (body != null) { kinematic = body.isKinematic; gravity = body.useGravity; collisions = body.detectCollisions; }
            colliders = GetComponentsInChildren<Collider>(true); colliderEnabled = new bool[colliders.Length];
            for (var i = 0; i < colliders.Length; i++) colliderEnabled[i] = colliders[i].enabled;
            authoredStateCaptured = true;
        }
        public bool TryAcquire(object owner)
        {
            ReconcileHeldOwner();
            if (registry == null || owner == null || owner is UnityEngine.Object value && value == null
                || !isActiveAndEnabled || HasLease) return false;
            lease = owner; return true;
        }
        public bool Release(object owner)
        {
            if (owner == null || !ReferenceEquals(owner, lease)) return false;
            if (heldActorColliders != null) ReleaseHeldBody(Vector3.zero, Vector3.zero);
            lease = null; return true;
        }
        public void InvalidateForReset(uint epoch)
        {
            if (heldActorColliders != null) ReleaseHeldBody(Vector3.zero, Vector3.zero);
            committedPropEffects.Clear();
            lease = null; Epoch = unchecked(Epoch + 1);
        }
        public bool TryRestoreForReset(out string reason)
        {
            if (registry == null || authoredParent == null) { reason = "The authored prop parent is unavailable."; return false; }
            transform.SetParent(authoredParent, false); transform.SetLocalPositionAndRotation(authoredPosition, authoredRotation); transform.localScale = authoredScale;
            if (body != null)
            {
                body.isKinematic = false; body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
                body.position = transform.position; body.rotation = transform.rotation;
                body.useGravity = gravity; body.detectCollisions = collisions; body.isKinematic = kinematic;
            }
            for (var i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] == null) { reason = "An authored prop collider was removed."; return false; }
                colliders[i].enabled = colliderEnabled[i];
            }
            RestoreIgnoredPairs();
            reason = null; return true;
        }
        public void Unbind()
        {
            if (heldActorColliders != null) ReleaseHeldBody(Vector3.zero, Vector3.zero);
            committedPropEffects.Clear();
            lease = null; registry?.Unregister(this); registry = null;
            Epoch = unchecked(Epoch + 1);
        }
        private void OnDisable()
        {
            if (heldActorColliders != null) ReleaseHeldBody(Vector3.zero, Vector3.zero);
            lease = null; Epoch = unchecked(Epoch + 1); committedPropEffects.Clear();
        }
        private void OnDestroy() { Unbind(); RestoreIgnoredPairs(); }
    }
}
