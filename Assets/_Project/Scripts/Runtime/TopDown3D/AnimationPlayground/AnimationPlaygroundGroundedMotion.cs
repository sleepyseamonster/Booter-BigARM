using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    public readonly struct AnimationPlaygroundGroundedMotion
    {
        public readonly long Tick, Epoch;
        public readonly double CompletedTime, Duration;
        public readonly Vector3 TangentDisplacement, SupportNormal;
        public readonly bool Available, Rebased;
        public double Distance => TangentDisplacement.magnitude;
        public float Speed => Available && Duration > 0 ? (float)(Distance / Duration) : 0;

        public AnimationPlaygroundGroundedMotion(long tick, long epoch, double time, double duration,
            Vector3 displacement, Vector3 normal, bool available, bool rebased)
        {
            Tick = tick; Epoch = epoch; CompletedTime = time; Duration = duration;
            TangentDisplacement = displacement; SupportNormal = normal;
            Available = available; Rebased = rebased;
        }
    }

    // Observes completed Rigidbody positions, never requested/commanded velocity.
    // Static supports only. A caller must mark moving/externally owned support unavailable.
    public sealed class AnimationPlaygroundGroundedMotionTracker
    {
        private bool hasPrevious, previousEligible;
        private Vector3 previousPosition, previousNormal;
        private double previousTime;
        private int previousSupport;
        private long tick, epoch;

        public void Reset()
        {
            hasPrevious = false; previousEligible = false; epoch++;
        }

        public AnimationPlaygroundGroundedMotion Observe(Vector3 completedPosition, double completedTime,
            bool eligible, int support, Vector3 normal, float maximumObservedSpeed = 12)
        {
            if (!Finite(completedPosition) || !double.IsFinite(completedTime) || !Finite(normal)
                || !float.IsFinite(maximumObservedSpeed) || maximumObservedSpeed <= 0)
                throw new ArgumentException("Completed motion observations must be finite.");
            // A duplicate publication cannot become a second simulation/contact tick.
            if (hasPrevious && completedTime == previousTime)
                return new AnimationPlaygroundGroundedMotion(tick, epoch, completedTime, 0,
                    Vector3.zero, previousNormal, false, false);
            tick++;
            eligible &= support != 0 && normal.sqrMagnitude > .5f;
            normal = normal.sqrMagnitude > .0001f ? normal.normalized : Vector3.up;
            var duration = hasPrevious ? completedTime - previousTime : 0;
            var delta = completedPosition - previousPosition;
            var rebase = !hasPrevious || duration <= 0 || !eligible || !previousEligible
                || support != previousSupport || Vector3.Dot(normal, previousNormal) < .5f
                || delta.magnitude > maximumObservedSpeed * duration + .05;
            if (rebase) epoch++;
            var tangent = rebase ? Vector3.zero : Vector3.ProjectOnPlane(delta, normal);
            previousPosition = completedPosition; previousTime = completedTime;
            previousEligible = eligible; previousSupport = support; previousNormal = normal; hasPrevious = true;
            return new AnimationPlaygroundGroundedMotion(tick, epoch, completedTime, Math.Max(0, duration),
                tangent, normal, eligible && !rebase, rebase);
        }

        private static bool Finite(Vector3 p) => float.IsFinite(p.x) && float.IsFinite(p.y) && float.IsFinite(p.z);
    }
}
