namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DPlayerActionController
    {
        private TopDown3DLeggerRidingController RideOwner => input != null && input.RidingController != null
            && input.RidingController.Player == motor ? input.RidingController : null;
        private bool RideReserved => RideOwner != null && RideOwner.State.OwnsMount;
        private bool MountedCargoAllowed
        {
            get
            {
                var ride = RideOwner; var activity = Activity;
                return ride != null && ride.CargoEligible && motor != null && motor.OwnsRiderConstraint(ride)
                    && !activity.LegacyWork && !activity.Placement && !activity.CampActive && !activity.StationActive;
            }
        }
        private TopDown3DGatherActionResult TryBeginMount(TopDown3DInteractionTarget selection)
            => selection.IsValid && interaction != null && interaction.CanReachTarget(selection.Target)
                && selection.Target is TopDown3DLeggerMountTarget mount && mount.TryMount()
                ? TopDown3DGatherActionResult.Completed : TopDown3DGatherActionResult.Rejected;
    }
}
