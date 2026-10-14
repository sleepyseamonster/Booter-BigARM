namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DMenuController
    {
        private TopDown3DCampWorkstationUiController campWorkstations;
        private bool returnCampWorkstation;
        public void ConfigureCampWorkstations(TopDown3DCampWorkstationUiController value) => campWorkstations = value;
        private void SuspendCampWorkstation()
        {
            returnCampWorkstation = campWorkstations != null && campWorkstations.IsOpen;
            if (returnCampWorkstation) campWorkstations.SuspendForMenu();
        }
        private bool ResumeCampWorkstation()
        {
            var resume = returnCampWorkstation; returnCampWorkstation = false;
            return resume && campWorkstations != null && campWorkstations.ResumeFromMenu();
        }
        private void EndCampWorkstation()
        { returnCampWorkstation = false; if (campWorkstations != null) campWorkstations.Close(false); }
    }
}
