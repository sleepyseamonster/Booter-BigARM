using System;
using UnityEngine;

namespace BooterBigArm.Runtime
{
    [Serializable]
    public sealed class PrototypeHarvestYield
    {
        [SerializeField] private string itemId = string.Empty;
        [SerializeField, Min(1)] private int count = 1;

        public string ItemId => itemId;
        public int Count => Mathf.Max(1, count);

        public static PrototypeHarvestYield Create(string id, int amount)
        {
            return new PrototypeHarvestYield
            {
                itemId = id ?? string.Empty,
                count = Mathf.Max(1, amount)
            };
        }
    }
}
