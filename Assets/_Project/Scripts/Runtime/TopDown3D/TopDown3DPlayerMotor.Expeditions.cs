using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DPlayerMotor
    {
        internal void RestoreExpeditionPose(Vector3 position,Quaternion rotation)
        {
            Teleport(position);body.rotation=rotation;transform.rotation=rotation;facingDirection=rotation*Vector3.forward;
            // Restore support from collision, without waiting for a foreground physics tick
            // or snapping an airborne checkpoint onto the terrain.
            Physics.SyncTransforms();IsGrounded=TryGetGroundNormal(out var normal);
            if(IsGrounded){stableGroundNormal=normal;hasStableGroundNormal=true;}
        }
        // The settled town GUI freezes physics; the next field pose is separately qualified before publication.
        internal bool ReleaseExpeditionTownRider(TopDown3DLeggerRidingController owner)
        {
            if(CampExpeditionSession.Find(gameObject.scene)?.IsSettledTown!=true||!ReferenceEquals(riderOwner,owner))return false;
            riderOwner=null;EndActionConstraint(owner);capsule.enabled=riderCollider;body.isKinematic=riderKinematic;body.useGravity=riderGravity;
            body.interpolation=riderInterpolation;body.collisionDetectionMode=riderCollision;
            if(!body.isKinematic){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}
            animationShapeHits=preRideShapeHits;animationReturnHits=preRideReturnHits;preRideShapeHits=null;preRideReturnHits=null;IsGrounded=false;return true;
        }
    }
}
