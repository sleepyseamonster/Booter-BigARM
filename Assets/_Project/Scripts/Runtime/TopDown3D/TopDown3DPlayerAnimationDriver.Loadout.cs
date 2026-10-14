namespace BooterBigArm.TopDown3D
{
    public sealed partial class TopDown3DPlayerAnimationDriver
    {
        public void SynchronizeLoadoutCloak(bool equipped)
        {
            if (wornCloak != null && wornCloak.activeSelf != (equipped && !campLying)) wornCloak.SetActive(equipped && !campLying);
            if (sleepingBlanket != null && sleepingBlanket.activeSelf != (equipped && campLying)) sleepingBlanket.SetActive(equipped && campLying);
        }
    }
}
