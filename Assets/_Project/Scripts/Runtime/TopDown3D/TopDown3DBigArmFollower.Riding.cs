using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DBigArmFollower
    {
        private TopDown3DLeggerRidingController ridingController;
        internal bool HasRideLease => !ReferenceEquals(ridingController, null) && ridingController.State.OwnsMount;
        internal bool GroundReadyForRide => startupGroundingComplete && body != null;
        internal Rigidbody RideBody => body;
        internal bool BindRiding(TopDown3DLeggerRidingController controller)
        {
            if (controller == null || ridingController != null && ridingController != controller) return false;
            ridingController = controller; return true;
        }
        internal void UnbindRiding(TopDown3DLeggerRidingController controller)
        {
            if (ridingController != controller) return;
            ridingController = null; currentSpeed = 0;
            callRequested = callOverActive = callOverArrived = automaticCatchUp = false;
            targetTrail.Clear(); InvalidateRoute(); ResetStuckTracking();
        }
        internal bool TickRideLease()
        {
            if (ridingController != null && ridingController.HoldAfterRelease(Time.fixedDeltaTime)) return true;
            if (!HasRideLease) return false;
            if (body == null) { ridingController.NotifyActorUnavailable(); return true; }
            ridingController.TickRiding(Time.fixedDeltaTime); return true;
        }
        internal void CommitRidePose(Vector3 position, Quaternion rotation, float measuredSpeed)
        {
            body.MovePosition(position); body.MoveRotation(rotation); currentSpeed = measuredSpeed;
            companionState?.PublishPosition(position);
        }
        internal void ResetFollowAfterRide()
        {
            currentSpeed = 0; callRequested = callOverActive = callOverArrived = automaticCatchUp = false;
            targetTrail.Clear(); InvalidateRoute(); ResetStuckTracking(); State = FollowState.Idle;
        }
        internal void NotifyRideFollowerDisabled() { if (HasRideLease) ridingController.NotifyActorUnavailable(); }
    }
}
