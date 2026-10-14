using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DPlayerMotor
    {
        private TopDown3DLeggerRidingController riderOwner;
        private bool riderKinematic, riderGravity, riderCollider;
        private RigidbodyInterpolation riderInterpolation;
        private CollisionDetectionMode riderCollision;
        private Collider[] preRideShapeHits;
        private RaycastHit[] preRideReturnHits;
        private object preRidePostureOwner;
        public bool IsMounted => !ReferenceEquals(riderOwner, null);
        internal bool OwnsRiderConstraint(TopDown3DLeggerRidingController owner)
            => !ReferenceEquals(owner, null) && ReferenceEquals(riderOwner, owner) && ReferenceEquals(actionConstraintOwner, owner);
        internal bool TryAcquireRider(TopDown3DLeggerRidingController owner)
        {
            if (owner == null || !ReferenceEquals(riderOwner, null) || !CanBeginCampAction
                || !TryBeginActionConstraint(owner)) return false;
            EnsureBody(); capsule ??= GetComponent<CapsuleCollider>();
            EnsureAnimationShape();
            preRidePostureOwner = animationPostureOwner;
            preRideShapeHits = animationShapeHits; preRideReturnHits = animationReturnHits;
            animationShapeHits = new Collider[32]; animationReturnHits = new RaycastHit[32];
            riderKinematic = body.isKinematic; riderGravity = body.useGravity; riderCollider = capsule.enabled;
            riderInterpolation = body.interpolation; riderCollision = body.collisionDetectionMode;
            body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.isKinematic = true; body.useGravity = false; capsule.enabled = false;
            riderOwner = owner; IsGrounded = false; return true;
        }
        internal bool YieldMountedMotor()
        {
            if (ReferenceEquals(riderOwner, null)) return false;
            SprintActive = false; PlanarAcceleration = Vector3.zero; IsGrounded = false;
            PublishPresentationSnapshot(Vector3.zero, Vector3.zero, true);
            return true;
        }
        internal void SetRiderPose(TopDown3DLeggerRidingController owner, Vector3 position, Quaternion rotation)
        {
            if (ReferenceEquals(owner, null) || !ReferenceEquals(riderOwner, owner) || !Finite(position)) return;
            body.position = position; body.rotation = rotation;
            transform.SetPositionAndRotation(position, rotation); facingDirection = rotation * Vector3.forward;
        }
        internal bool TryReleaseRider(TopDown3DLeggerRidingController owner, Vector3 position, Quaternion rotation)
        {
            if (ReferenceEquals(owner, null) || !ReferenceEquals(riderOwner, owner) || !Finite(position)
                || !owner.ReserveStandingValidation() || !StandingRiderBufferGuard(position) || !IsAnimationReturnClear(position)
                || !IsAnimationReturnSupported(position)) return false;
            SetRiderPose(owner, position, rotation);
            riderOwner = null; EndActionConstraint(owner);
            capsule.enabled = riderCollider; body.isKinematic = riderKinematic; body.useGravity = riderGravity;
            body.interpolation = riderInterpolation; body.collisionDetectionMode = riderCollision;
            if (!body.isKinematic) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
            animationShapeHits = preRideShapeHits; animationReturnHits = preRideReturnHits;
            preRideShapeHits = null; preRideReturnHits = null;
            IsGrounded = true; ResetPresentationSnapshot(); return true;
        }
        internal bool TryResetRider(TopDown3DLeggerRidingController owner, object resetOwner, uint epoch, Vector3 position)
        {
            if (!ReferenceEquals(riderOwner, owner) || ReferenceEquals(owner, null) || epoch != owner.State.Epoch || input == null
                || resetOwner == null || !input.LaboratoryResetBlocked || !ReferenceEquals(resetOwner, preRidePostureOwner)
                || animationPostureOwner != null && !ReferenceEquals(resetOwner, animationPostureOwner)) return false;
            bool restorePostureOwner = animationPostureOwner == null;
            if (!TryReleaseRider(owner, position, body.rotation)) return false;
            if (restorePostureOwner) BindAnimationPostureOwner(resetOwner);
            return true;
        }
        private bool StandingRiderBufferGuard(Vector3 position)
        {
            var scale = transform.lossyScale;
            float radius = Mathf.Max(.01f, animationOriginalRadius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z)) - .02f);
            float half = Mathf.Max(radius, animationOriginalHeight * Mathf.Abs(scale.y) * .5f - .02f);
            var middle = position + transform.rotation * Vector3.Scale(animationOriginalCenter, scale);
            if (Physics.OverlapCapsuleNonAlloc(middle + Vector3.up * (half - radius), middle - Vector3.up * (half - radius),
                radius, animationShapeHits, ~0, QueryTriggerInteraction.Ignore) == animationShapeHits.Length) return false;
            var foot = position + Vector3.up * (animationOriginalCenter.y - animationOriginalHeight * .5f);
            return Physics.RaycastNonAlloc(foot + Vector3.up * .1f, Vector3.down, animationReturnHits, .22f,
                groundMask, QueryTriggerInteraction.Ignore) < animationReturnHits.Length;
        }
        internal void NotifyRiderMotorDisabled() { if (!ReferenceEquals(riderOwner, null)) riderOwner.NotifyActorUnavailable(); }
    }
}
