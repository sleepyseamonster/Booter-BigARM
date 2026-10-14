using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public static class TopDown3DCampCompanionStatus
    {
        public static string Describe(TopDown3DCampLoopEconomySession session)
        {
            var state = session.Cargo.GetComponent<TopDown3DBigArmState>();
            var follower = session.Cargo.GetComponent<TopDown3DBigArmFollower>();
            if (state == null || follower == null || !follower.isActiveAndEnabled) return "Legger unavailable. Cargo remains with its owner.";
            return "Legger · " + state.CompanionId + "\n" + Vector3.Distance(session.Kit.transform.position, session.Cargo.transform.position).ToString("0.#")
                + " m away · " + (follower.HasRideLease ? "Mounted" : follower.State.ToString()) + "\nCargo mass: " + session.Cargo.State.CalculateMass().ToString("0.##")
                + "\n" + (session.CanAccessOwner(1) ? "Cargo within reach" : "Cargo inspection only") + "\nCall requests physical movement to Booter.";
        }
        public static bool CanCall(TopDown3DCampLoopEconomySession session)
        {
            var follower = session.Cargo.GetComponent<TopDown3DBigArmFollower>();
            var motor = session.Kit.GetComponent<TopDown3DPlayerMotor>();
            var vitals = session.Kit.GetComponent<TopDown3DSurvivalVitals>();
            var camp = session.Kit.GetComponent<TopDown3DCampController>();
            return follower != null && follower.isActiveAndEnabled && motor != null && motor.isActiveAndEnabled
                && motor.LaboratoryStandingActionsAllowed && !motor.IsMounted && !follower.HasRideLease && vitals != null && vitals.Health > 0
                && (camp == null || camp.Phase == TopDown3DCampPhase.Idle);
        }
    }
}
