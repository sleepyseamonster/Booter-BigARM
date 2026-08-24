using System;
using System.IO;
using BooterBigArm.TopDown3D.WorldCreator;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    public sealed class TopDown3DGameStateSaveService : MonoBehaviour
    {
        [SerializeField] private TopDown3DPlayerInventory booterInventory;
        [SerializeField] private TopDown3DBigArmCargo cargo;
        [SerializeField] private TopDown3DBigArmState bigArmState;
        [SerializeField] private Transform booterTransform;
        [SerializeField] private int worldSeed;
        [SerializeField] private string fileName = "booter-bigarm-save.json";
        public string SavePath => Path.Combine(Application.persistentDataPath, fileName);

        public void Configure(TopDown3DPlayerInventory player, Transform playerTransform, TopDown3DBigArmCargo bigArmCargo, TopDown3DBigArmState companionState, int seed)
        { booterInventory = player; booterTransform = playerTransform; cargo = bigArmCargo; bigArmState = companionState; worldSeed = seed; }

        public TopDown3DGameStateSnapshot CaptureSnapshot()
        {
            if (booterInventory == null || booterTransform == null) throw new InvalidOperationException("Save service is not configured.");
            return TopDown3DGameStateSnapshot.Create(
                worldSeed,
                WorldCreatorProductionProfile.CurrentTopologyVersion,
                booterTransform.position,
                booterInventory.CaptureSnapshot(),
                bigArmState != null ? bigArmState.CaptureSnapshot(cargo) : null);
        }

        public bool Save()
        {
            try
            {
                var temp = SavePath + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(CaptureSnapshot(), true));
                if (File.Exists(SavePath)) File.Replace(temp, SavePath, null); else File.Move(temp, SavePath);
                return true;
            }
            catch (Exception exception) { Debug.LogError($"Could not save BigARM state: {exception.Message}"); return false; }
        }

        public bool Load()
        {
            if (!File.Exists(SavePath) || booterInventory == null || booterTransform == null) return false;
            try
            {
                var snapshot = JsonUtility.FromJson<TopDown3DGameStateSnapshot>(File.ReadAllText(SavePath));
                if (snapshot == null
                    || !snapshot.IsCompatible(
                        worldSeed,
                        WorldCreatorProductionProfile.CurrentTopologyVersion)
                    || snapshot.BooterInventory == null
                    || !IsFinite(snapshot.BooterPosition))
                {
                    Debug.LogWarning("Rejected an incompatible pre-topology-v2 or wrong-world prototype save.", this);
                    return false;
                }
                if (!booterInventory.ApplySnapshot(snapshot.BooterInventory).Succeeded) return false;
                booterTransform.position = snapshot.BooterPosition;
                if (bigArmState != null && snapshot.BigArm != null && !bigArmState.ApplySnapshot(snapshot.BigArm)) return false;
                if (cargo != null && snapshot.BigArm != null && snapshot.BigArm.Cargo != null && !cargo.ApplySnapshot(snapshot.BigArm.Cargo).Succeeded) return false;
                return true;
            }
            catch (Exception exception) { Debug.LogError($"Could not load BigARM state: {exception.Message}"); return false; }
        }

        private static bool IsFinite(Vector3 value) => !(float.IsNaN(value.x) || float.IsNaN(value.y) || float.IsNaN(value.z) || float.IsInfinity(value.x) || float.IsInfinity(value.y) || float.IsInfinity(value.z));
    }
}
