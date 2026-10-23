using System;

namespace BooterBigArm.TopDown3D
{
    public enum CampRecoveryState { Sealed, Claimed, Returned, PaidTeamInTransit }
    [Serializable] public sealed class CampRecoveryBundle
    {
        public string id, ownerProfileId, lostRunId, claimId, finderProfileId, deliveryId;
        public string terrainId;
        public UnityEngine.Vector3 lossPosition;
        public int lostRunSeed;
        public CampRecoveryState state;
        public TopDown3DInventorySnapshot booter, legger;
        public int cargoFootprint;
        public double dueGameDay;
        public long quotedFee;
        public CampEquipmentStateRecord[] equipmentStates=Array.Empty<CampEquipmentStateRecord>();
    }
    [Serializable] public sealed class CampMailboxDelivery
    { public string id, bundleId, recipientProfileId; public bool collected; }

    /// <summary>Server-owned whole-bundle operations. The local save never grants multiplayer authority.</summary>
    public interface ICampAsyncRecoveryService
    {
        void PublishSealedLoss(string operationId,CampRecoveryBundle bundle);
        void ClaimWholeBundle(string operationId,string bundleId,string finderProfileId);
        void ReturnWholeBundle(string operationId,string claimId,string deliveryId);
    }
    public interface ICampRecoveryMailbox
    { void RequestDelivery(string operationId,string deliveryId,string ownerProfileId); }

    public static class CampOfflineRecovery
    {
        public const bool Enabled = false;
        public const int ProvisionalDelayDays = 3;
        public const long ProvisionalFee = 250;
        // This is a pure candidate operation for a later service slice. No background clock or automatic award.
        public static CampRecoveryBundle PrepareRequest(CampRecoveryBundle sealedBundle,double gameDay,long fee,int days)
        {
            if(sealedBundle==null || sealedBundle.state!=CampRecoveryState.Sealed || fee<=0 || days<2
                || !double.IsFinite(gameDay) || gameDay<0)throw new ArgumentException("A sealed loss, positive fee and several in-game days are required.");
            var copy=UnityEngine.JsonUtility.FromJson<CampRecoveryBundle>(UnityEngine.JsonUtility.ToJson(sealedBundle));
            copy.state=CampRecoveryState.PaidTeamInTransit;copy.quotedFee=fee;copy.dueGameDay=gameDay+days;return copy;
        }
        public static bool IsDue(CampRecoveryBundle bundle,double gameDay)=>bundle!=null && bundle.state==CampRecoveryState.PaidTeamInTransit && gameDay>=bundle.dueGameDay;
    }
}
