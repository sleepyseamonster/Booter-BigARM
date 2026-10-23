namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DInputRouter
    {
        internal TopDown3DLeggerRidingController RidingController { get; private set; }
        private bool ridingInteractAwaitRelease;
        private uint ridingPressEpoch;
        internal bool BindRidingInput(TopDown3DLeggerRidingController owner)
        {
            if (owner == null || RidingController != null && RidingController != owner) return false;
            if (RidingController == owner) return true;
            RidingController = owner; InteractionPressStarted += CaptureRidingPress; return true;
        }
        internal void FenceRidingInteraction()
        { ridingInteractAwaitRelease |= interactAction?.IsPressed() == true || openCargoAction?.IsPressed() == true; }
        private void CaptureRidingPress() => ridingPressEpoch = RidingController?.State.Epoch ?? 0;
        internal void UnbindRidingInput(TopDown3DLeggerRidingController owner)
        {
            if (RidingController != owner) return;
            InteractionPressStarted -= CaptureRidingPress; RidingController = null; ridingInteractAwaitRelease = false;
        }
        private bool ResolveRidingCommands(bool modalRequested)
        {
            // Departure cancellation owns C only during this scene's active countdown.
            // Otherwise pickup/cancel keeps its existing item and riding meaning.
            if(this!=null&&(pickupRequested||itemCancelRequested)&&!modalRequested&&CanCaptureGameplayCommand)
            {
                var departure=CampExpeditionSession.Find(gameObject.scene)?.CurrentBeacon;
                if(departure?.IsDeparting==true)
                {pickupRequested=itemCancelRequested=false;departure.Cancel();return true;}
            }
            if (RidingController == null || !RidingController.State.OwnsMount) return false;
            if (modalRequested || !CanCaptureGameplayCommand)
            {
                interactRequested = false; return true;
            }
            if ((animationRequests & 2) != 0) { interactRequested = false; return false; }
            if (ridingInteractAwaitRelease)
            {
                interactRequested = cargoRequested = false;
                if (interactAction?.IsPressed() != true && openCargoAction?.IsPressed() != true) ridingInteractAwaitRelease = false;
                return true;
            }
            bool consumed = false;
            if (interactRequested)
            {
                interactRequested = false;
                if (ridingPressEpoch == RidingController.State.Epoch) RidingController.RequestDismount();
                consumed = true;
            }
            // Palette remains a system action. Ground lab motions cannot run from a seat.
            int groundMotions = animationRequests & ~2;
            animationRequests &= 2;
            if (groundMotions != 0)
            {
                RidingController.ReportUnavailableAction(); consumed = true;
            }
            if (pickupRequested || itemCancelRequested)
            {
                pickupRequested = itemCancelRequested = false;
                RidingController.ReportUnavailableAction(); consumed = true;
            }
            return consumed;
        }
    }
}
