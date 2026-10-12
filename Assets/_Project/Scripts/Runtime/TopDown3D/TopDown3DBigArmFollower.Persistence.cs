using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DBigArmFollower
    {
        // Restore this companion's own checkpoint position, never a position beside Booter.
        internal void RestoreSavedWorldPosition(Vector3 position, Quaternion rotation)
        {
            body = GetComponent<Rigidbody>();
            if (body != null) { body.position = position; body.rotation = rotation; }
            transform.SetPositionAndRotation(position, rotation);
            currentSpeed = 0; startupGroundingComplete = true;
            callRequested = callOverActive = callOverArrived = automaticCatchUp = false;
            targetTrail.Clear(); InvalidateRoute(); ResetStuckTracking(); State = FollowState.Idle;
            GetComponent<TopDown3DBigArmState>()?.PublishPosition(position);
        }
    }
}
