using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public readonly struct AnimationPlaygroundGaitContacts
    {
        public readonly long Epoch, FirstOrdinal, Count;
        public AnimationPlaygroundGaitContacts(long epoch, long first, long count)
        { Epoch = epoch; FirstOrdinal = first; Count = count; }
        public static TopDown3DFootSide Side(long ordinal) => (ordinal & 1) == 0
            ? TopDown3DFootSide.Right : TopDown3DFootSide.Left;
        // Cosmetic consumers may drop stale effects, never change the simulation ledger.
        public long FirstRecentOrdinal(int capacity) => FirstOrdinal + Math.Max(0, Count - Math.Max(0, capacity));
    }

    // Inert until a qualified laboratory family supplies measured metres/cycle.
    // Simulation advances once per completed tick; rendering only interpolates.
    public sealed class AnimationPlaygroundGaitClock
    {
        private long lastTick = -1, motionEpoch = -1, contactEpoch;
        private double previousCycles;
        public double Cycles { get; private set; }
        public bool Travelling { get; private set; }
        public long ContactCount { get; private set; }
        public float EnterSpeed { get; }
        public float ExitSpeed { get; }

        public AnimationPlaygroundGaitClock(float enterSpeed = .18f, float exitSpeed = .1f)
        {
            if (!float.IsFinite(enterSpeed) || !float.IsFinite(exitSpeed) || exitSpeed < 0 || enterSpeed <= exitSpeed)
                throw new ArgumentException("Travel hysteresis must be finite and ordered.");
            EnterSpeed = enterSpeed; ExitSpeed = exitSpeed;
        }

        public void Reset()
        {
            lastTick = -1; motionEpoch = -1; contactEpoch++;
            previousCycles = Cycles; Travelling = false;
        }
        public void RebaseContact(TopDown3DFootSide side)
        {
            var phase=side==TopDown3DFootSide.Left ? .25 : 0;
            var next=Math.Floor(Cycles)+phase;if(next<Cycles)next++;
            Cycles=previousCycles=next;lastTick=-1;motionEpoch=-1;contactEpoch++;Travelling=false;
        }

        public AnimationPlaygroundGaitContacts Advance(AnimationPlaygroundGroundedMotion sample, double metresPerCycle)
            => Advance(sample, metresPerCycle, null);

        public AnimationPlaygroundGaitContacts Advance(AnimationPlaygroundGroundedMotion sample,
            AnimationPlaygroundForwardLocomotionProfile profile) => Advance(sample, profile.CycleDistance(sample.Speed), profile);

        private AnimationPlaygroundGaitContacts Advance(AnimationPlaygroundGroundedMotion sample, double metresPerCycle,
            AnimationPlaygroundForwardLocomotionProfile profile)
        {
            if (!double.IsFinite(metresPerCycle) || metresPerCycle <= 0)
                throw new ArgumentException("A qualified positive stride distance is required.");
            if (sample.Tick == lastTick) return default;
            previousCycles = Cycles;
            var discontinuity = sample.Tick < lastTick || sample.Epoch != motionEpoch || sample.Rebased;
            lastTick = sample.Tick; motionEpoch = sample.Epoch;
            if (discontinuity || !sample.Available)
            {
                contactEpoch++; Travelling = false;
                return new AnimationPlaygroundGaitContacts(contactEpoch, 0, 0);
            }
            Travelling = sample.Speed >= (Travelling ? ExitSpeed : EnterSpeed);
            if (!Travelling) return new AnimationPlaygroundGaitContacts(contactEpoch, 0, 0);
            var before = (long)Math.Floor(Cycles * 4);
            Cycles = profile == null ? Cycles + sample.Distance / metresPerCycle
                : profile.AdvanceCycles(sample.Speed, Cycles, sample.Distance);
            var after = (long)Math.Floor(Cycles * 4);
            var count = after - before;
            ContactCount += count;
            return new AnimationPlaygroundGaitContacts(contactEpoch, before + 1, count);
        }

        public double RenderCycles(float fixedInterpolation) =>
            previousCycles + (Cycles - previousCycles) * Mathf.Clamp01(fixedInterpolation);

        public double RenderCycles(float fixedInterpolation, float acceptedSpeed, AnimationPlaygroundForwardLocomotionProfile profile)
        {
            var from = profile.DistanceAtCycles(acceptedSpeed, previousCycles);
            var distance = profile.DistanceAtCycles(acceptedSpeed, Cycles) - from;
            return profile.AdvanceCycles(acceptedSpeed, previousCycles, distance * Mathf.Clamp01(fixedInterpolation));
        }

        public static double BlendedCycleDistance(float speed, Vector3 nominalSpeeds, Vector3 metresPerCycle)
        {
            if (!float.IsFinite(speed) || speed < 0 || !Positive(nominalSpeeds) || !Positive(metresPerCycle)
                || nominalSpeeds.x >= nominalSpeeds.y || nominalSpeeds.y >= nominalSpeeds.z)
                throw new ArgumentException("Measured distances and nominal speeds must be positive and ordered.");
            TopDown3DAnimationMath.CalculateGaitWeights(speed, nominalSpeeds.x, nominalSpeeds.y, nominalSpeeds.z,
                out var walk, out var run, out var sprint);
            return walk * (double)metresPerCycle.x + run * (double)metresPerCycle.y + sprint * (double)metresPerCycle.z;
        }

        private static bool Positive(Vector3 v) => float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z)
            && v.x > 0 && v.y > 0 && v.z > 0;
    }
}
