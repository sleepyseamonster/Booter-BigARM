using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [CreateAssetMenu(menuName = "Booter & BigARM/Camp Settings")]
    public sealed class TopDown3DCampSettings : ScriptableObject
    {
        public const string ItemId = "tool.popup_camp";
        public GameObject deployedPrefab, packedPrefab, wornCloakPrefab, blanketPrefab;
        public AnimationClip sleepClip, restClip, setupClip, wakeClip;
        public Material previewMaterial;
        [Min(1)] public float sleepSpeed = 4;
        [Range(0, 1)] public float sleepTarget = .8f, restTarget = .4f, resourceFloor = .1f;
        [Min(0)] public float sleepRecovery = .4f, sleepFood = .015f, sleepWater = .028f, sleepOxygen = .01f;
        [Min(0)] public float restRecovery = .3f, restFood = .02f, restWater = .032f, restOxygen = .008f;
        [Min(.1f)] public float restDuration = 20, setupDuration = 2, poseDuration = .8f, wakeDuration = .45f;
        [Min(.1f)] public float placementReach = 3, interactionRange = 2.4f, approachTimeout = 4;
        [Range(0, 45)] public float maximumSlope = 14;
        [Range(.05f, .6f)] public float maximumHeightVariation = .32f;
        public Vector3 sleepOffset = new(0, 0, 1.2f);
        public Vector3 footprint = new(2.4f, .15f, 2.6f);
        public static TopDown3DCampSettings Load() => Resources.Load<TopDown3DCampSettings>("TopDown3DCampSettings");
        public bool HasAssets => deployedPrefab && packedPrefab && wornCloakPrefab && blanketPrefab
            && sleepClip && restClip && setupClip && wakeClip;
        public bool TryValidate(out string reason)
        {
            reason = "Camp meshes, clips, or footprint material are missing";
            if (!HasAssets || previewMaterial == null) return false;
            reason = "Camp tuning contains invalid values";
            foreach (var value in new[] { sleepSpeed, sleepTarget, restTarget, resourceFloor, sleepRecovery, sleepFood,
                sleepWater, sleepOxygen, restRecovery, restFood, restWater, restOxygen, restDuration, setupDuration,
                poseDuration, wakeDuration, placementReach, interactionRange, approachTimeout, maximumSlope, maximumHeightVariation,
                sleepOffset.x, sleepOffset.y, sleepOffset.z, footprint.x, footprint.y, footprint.z })
                if (!float.IsFinite(value)) return false;
            if (sleepSpeed < 1 || sleepSpeed > 4 || restTarget < 0 || sleepTarget > 1 || sleepTarget <= restTarget
                || resourceFloor < 0 || resourceFloor >= 1 || sleepRecovery <= 0 || restRecovery <= 0
                || sleepFood < 0 || sleepWater < 0 || sleepOxygen < 0 || restFood < 0 || restWater < 0 || restOxygen < 0
                || restDuration <= 0 || setupDuration <= 0 || poseDuration <= 0 || wakeDuration <= 0
                || placementReach <= 0 || interactionRange <= 0 || approachTimeout <= 0 || maximumSlope < 0 || maximumSlope > 45
                || maximumHeightVariation < .05f || maximumHeightVariation > .6f || footprint.x <= 0 || footprint.y <= 0 || footprint.z <= 0) return false;
            reason = null; return true;
        }
    }
}
