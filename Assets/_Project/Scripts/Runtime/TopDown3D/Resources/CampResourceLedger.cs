using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    [Serializable]
    public sealed class CampResourceLedger
    {
        public const int Count = 5;
        public static readonly string[] Keys = { "ironstone", "salts", "bristles", "cable", "beetle" };
        public static readonly string[] Items = { "resource.ironstone_ore", "lab.resource.dry_mineral_salts", "lab.resource.coarse_sensory_bristles", "lab.resource.salvaged_metal_cable", "lab.resource.chitin_fibers" };
        public static readonly int[] Quantities = { 2, 2, 3, 1, 3 };
        [SerializeField] private int version = 1;
        [SerializeField] private int consumed;
        [SerializeField] private float beetleHealth = 30;
        [SerializeField] private uint epoch;
        [SerializeField] private int revision;
        public float Health => beetleHealth;
        public bool Dead => beetleHealth <= 0;
        public uint Epoch => epoch;
        public int Revision => revision;
        public bool Valid => version == 1 && consumed >= 0 && consumed < 32 && float.IsFinite(beetleHealth)
            && beetleHealth >= 0 && beetleHealth <= 30 && ((consumed & 16) == 0 || Dead);
        public bool Used(int index) => index < 0 || index >= Count || (consumed & (1 << index)) != 0;
        public bool CanHarvest(int index) => Valid && !Used(index) && (index != 4 || Dead);
        public bool Consume(int index)
        {
            if (!CanHarvest(index)) return false;
            consumed |= 1 << index; revision++; return true;
        }
        public bool Damage(float amount)
        {
            if (!Valid || Dead || !float.IsFinite(amount) || amount <= 0) return false;
            beetleHealth = Mathf.Max(0, beetleHealth - amount); revision++; return true;
        }
        public void Invalidate() { epoch = unchecked(epoch + 1); }
        internal void RestoreTrial() { consumed = 0; beetleHealth = 30; revision++; Invalidate(); }
        public static string SourceId(int index) => "lab.resource-study.v1." + Keys[index] + ".001";
    }

    // One attack instance owns one finite hit window, independent of colliders/animation callbacks.
    public sealed class CampResourceStrike
    {
        private float elapsed;
        private bool emitted;
        public bool Active { get; private set; }
        public float Seconds => elapsed;
        public bool Begin() { if (Active) return false; elapsed = 0; emitted = false; Active = true; return true; }
        public bool Advance(float dt, bool allowed)
        {
            if (!Active) return false;
            if (!allowed || !float.IsFinite(dt) || dt < 0) { Cancel(); return false; }
            elapsed += dt;
            var hit = !emitted && elapsed >= .32f && elapsed <= .52f;
            if (elapsed >= .32f) emitted = true;
            if (elapsed >= .85f) Active = false;
            return hit;
        }
        public void Cancel() { Active = false; emitted = true; }
    }
}
