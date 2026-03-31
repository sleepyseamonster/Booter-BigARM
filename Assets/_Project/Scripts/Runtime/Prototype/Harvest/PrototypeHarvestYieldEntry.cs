using System;
using UnityEngine;

namespace BooterBigArm.Runtime
{
    [Serializable]
    public sealed class PrototypeHarvestYieldEntry
    {
        [SerializeField] private string itemId = "scrap";
        [SerializeField, Min(0)] private int minAmount = 1;
        [SerializeField, Min(0)] private int maxAmount = 3;
        [SerializeField, Range(0f, 1f)] private float chance = 1f;
        [SerializeField, Min(1)] private int rolls = 1;

        public string ItemId => itemId;
        public int MinAmount => minAmount;
        public int MaxAmount => maxAmount;
        public float Chance => chance;
        public int Rolls => rolls;

        public static PrototypeHarvestYieldEntry Create(string id, int min, int max, float dropChance, int rollCount)
        {
            return new PrototypeHarvestYieldEntry
            {
                itemId = id ?? string.Empty,
                minAmount = Mathf.Max(0, min),
                maxAmount = Mathf.Max(0, max),
                chance = Mathf.Clamp01(dropChance),
                rolls = Mathf.Max(1, rollCount)
            };
        }

        public int RollAmount(System.Random rng)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return 0;
            }

            if (chance < 1f)
            {
                var sample = rng != null ? rng.NextDouble() : UnityEngine.Random.value;
                if (sample > chance)
                {
                    return 0;
                }
            }

            var min = Mathf.Max(0, minAmount);
            var max = Mathf.Max(min, maxAmount);
            if (max <= min)
            {
                return min;
            }

            if (rng != null)
            {
                return rng.Next(min, max + 1);
            }

            return UnityEngine.Random.Range(min, max + 1);
        }
    }
}
