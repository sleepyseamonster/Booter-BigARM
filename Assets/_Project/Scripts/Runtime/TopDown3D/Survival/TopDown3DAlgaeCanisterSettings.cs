using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public enum TopDown3DAlgaeUse { Drink, Eat, Treat }

    [CreateAssetMenu(menuName = "Booter & BigARM/Algae Canister Settings")]
    public sealed class TopDown3DAlgaeCanisterSettings : ScriptableObject
    {
        public float capacity = 100, initialFill = 100;
        public float drinkGain = 20, eatGain = 20, treatGain = 10;
        public float drinkCostPerPoint = .25f, eatCostPerPoint = .25f, treatCostPerPoint = 1;
        public float cooldownSeconds = .35f;
        [Header("Passive growth per simulation second")]
        public float replenishmentPerSecond = .02f;
        public float Gain(TopDown3DAlgaeUse use) => use == TopDown3DAlgaeUse.Drink ? drinkGain : use == TopDown3DAlgaeUse.Eat ? eatGain : treatGain;
        public float CostPerPoint(TopDown3DAlgaeUse use) => use == TopDown3DAlgaeUse.Drink ? drinkCostPerPoint : use == TopDown3DAlgaeUse.Eat ? eatCostPerPoint : treatCostPerPoint;
        public bool IsValid => Positive(capacity) && float.IsFinite(initialFill) && initialFill >= 0 && initialFill <= capacity
            && Positive(drinkGain) && Positive(eatGain) && Positive(treatGain)
            && Positive(drinkCostPerPoint) && Positive(eatCostPerPoint) && Positive(treatCostPerPoint)
            && float.IsFinite(cooldownSeconds) && cooldownSeconds >= 0
            && float.IsFinite(replenishmentPerSecond) && replenishmentPerSecond >= 0;
        private static bool Positive(float value) => float.IsFinite(value) && value > 0;
        public static TopDown3DAlgaeCanisterSettings Load() => Resources.Load<TopDown3DAlgaeCanisterSettings>("TopDown3DAlgaeCanisterSettings");
    }
}
