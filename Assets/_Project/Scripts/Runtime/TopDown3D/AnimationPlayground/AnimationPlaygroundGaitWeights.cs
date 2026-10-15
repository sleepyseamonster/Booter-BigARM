using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    // Fixed channels avoid allocations in the pose and distance clocks. The
    // original four-channel API remains available for existing gait families.
    public struct AnimationPlaygroundGaitWeights
    {
        private Vector4 lower;
        private Vector2 upper;
        public AnimationPlaygroundGaitWeights(Vector4 legacy) { lower = legacy; upper = default; }
        public static AnimationPlaygroundGaitWeights Idle => new(new Vector4(1, 0, 0, 0));
        public Vector4 Legacy => lower;
        public float this[int index]
        {
            get => index < 0 || index > 5 ? throw new IndexOutOfRangeException()
                : index < 4 ? lower[index] : upper[index - 4];
            set
            {
                if (index < 0 || index > 5) throw new IndexOutOfRangeException();
                if (index < 4) lower[index] = value; else upper[index - 4] = value;
            }
        }
        public static AnimationPlaygroundGaitWeights Lerp(AnimationPlaygroundGaitWeights from,
            AnimationPlaygroundGaitWeights to, float amount)
            => new() { lower = Vector4.Lerp(from.lower, to.lower, amount), upper = Vector2.Lerp(from.upper, to.upper, amount) };
    }
}
