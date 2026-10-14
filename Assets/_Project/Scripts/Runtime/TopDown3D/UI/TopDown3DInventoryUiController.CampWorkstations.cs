namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DInventoryUiController
    {
        private TopDown3DCampWorkstationUiController campWorkstations;
        public void ConfigureCampWorkstations(TopDown3DCampWorkstationUiController value) => campWorkstations = value;
        private bool CloseCampWorkstationOnToggle()
        {
            if (campWorkstations == null || !campWorkstations.IsOpen) return false;
            campWorkstations.Close(); return true;
        }
    }
}
