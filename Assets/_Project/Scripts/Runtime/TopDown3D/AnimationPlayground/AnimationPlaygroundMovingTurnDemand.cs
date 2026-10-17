using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    // Presentation demand only: completed travel determines the turn, while the
    // existing motor and gait clock retain movement and contact ownership.
    public sealed class AnimationPlaygroundMovingTurnDemand
    {
        public const float MinimumSpeed = .3f;
        public const float MinimumDistance = .0001f;
        public const float SmoothingSeconds = .1f;
        public const float MaximumCurvature = 120f; // Signed degrees per metre.
        public const float MaximumAngularSpeed = 360f; // Signed degrees per second.
        public const float MaximumObservedSpeed = 12f;
        public const double MaximumSampleDuration = .25;
        private const float MinimumNormalDot = .8660254f; // Thirty degrees.

        private bool hasObservation, hasDirection;
        private long lastTick, lastEpoch;
        private double lastTime;
        private Vector3 previousDirection, previousNormal;
        public float Curvature { get; private set; }
        public float PreviousCurvature { get; private set; }
        public float AngularSpeed { get; private set; }
        public bool Valid { get; private set; }

        public void Reset()
        {
            hasObservation = false;
            lastTick = lastEpoch = 0;
            lastTime = 0;
            ClearDemand();
        }

        public void Observe(AnimationPlaygroundGroundedMotion sample, bool eligible)
        {
            // A presentation owner can suppress an already observed tick. In
            // particular, entering LT must clear demand without a fresh sample.
            if (!eligible)
            {
                Record(sample);
                ClearDemand();
                return;
            }
            // The completed-motion tracker may republish an unavailable,
            // zero-duration record for this same tick. Never smooth it twice.
            if (hasObservation && sample.Tick == lastTick && sample.Epoch == lastEpoch) return;

            var hadObservation = hasObservation;
            var epochChanged = hadObservation && sample.Epoch != lastEpoch;
            var tickGap = hadObservation && sample.Tick != lastTick + 1;
            var timeReversed = hadObservation && sample.Epoch == lastEpoch
                && (sample.Tick < lastTick || double.IsFinite(lastTime) && sample.CompletedTime <= lastTime);
            var timeGap = hadObservation && sample.Epoch == lastEpoch && double.IsFinite(lastTime)
                && sample.CompletedTime - lastTime > MaximumSampleDuration;
            Record(sample);
            if (!sample.Available || sample.Rebased || timeReversed
                || !double.IsFinite(sample.CompletedTime) || !double.IsFinite(sample.Duration)
                || sample.Duration <= 0 || sample.Duration > MaximumSampleDuration
                || !Finite(sample.TangentDisplacement) || !Finite(sample.SupportNormal))
            {
                ClearDemand();
                return;
            }

            var normalLength = sample.SupportNormal.magnitude;
            if (!float.IsFinite(normalLength) || normalLength < .5f)
            {
                ClearDemand();
                return;
            }
            var normal = sample.SupportNormal / normalLength;
            var displacement = Vector3.ProjectOnPlane(sample.TangentDisplacement, normal);
            var distance = displacement.magnitude;
            var speed = (float)(distance / sample.Duration);
            if (!float.IsFinite(distance) || !float.IsFinite(speed) || distance < MinimumDistance
                || speed < MinimumSpeed || speed > MaximumObservedSpeed)
            {
                ClearDemand();
                return;
            }
            var direction = displacement / distance;
            if (!hasDirection || epochChanged || tickGap || timeGap)
            {
                ClearDemand();
                EstablishDirection(direction, normal);
                return;
            }
            if (Vector3.Dot(previousNormal, normal) < MinimumNormalDot)
            {
                // The next valid sample establishes a new baseline; a terrain
                // normal jump must not masquerade as a horizontal turn.
                ClearDemand();
                return;
            }

            var commonNormal = (previousNormal + normal).normalized;
            var from = Vector3.ProjectOnPlane(previousDirection, commonNormal);
            var to = Vector3.ProjectOnPlane(direction, commonNormal);
            if (from.sqrMagnitude < .000001f || to.sqrMagnitude < .000001f)
            {
                ClearDemand();
                return;
            }
            // Unity's positive SignedAngle about up is a right turn. Limit both
            // angular and spatial demand so a reversal cannot create a spike.
            var angle = Math.Atan2(Vector3.Dot(commonNormal, Vector3.Cross(from, to)), Vector3.Dot(from, to)) * (180 / Math.PI);
            var angular = Mathf.Clamp((float)(angle / sample.Duration),
                -MaximumAngularSpeed, MaximumAngularSpeed);
            var target = Mathf.Clamp(angular / speed, -MaximumCurvature, MaximumCurvature);
            angular = target * speed;
            var blend = (float)(1 - Math.Exp(-sample.Duration / SmoothingSeconds));
            PreviousCurvature = Curvature;
            Curvature = Mathf.Clamp(Mathf.Lerp(Curvature, target, blend), -MaximumCurvature, MaximumCurvature);
            AngularSpeed = Mathf.Clamp(Mathf.Lerp(AngularSpeed, angular, blend), -MaximumAngularSpeed, MaximumAngularSpeed);
            EstablishDirection(direction, normal);
        }

        public float RenderCurvature(float interpolation)
        {
            if (!Valid || !float.IsFinite(interpolation)) return 0;
            return Mathf.Lerp(PreviousCurvature, Curvature, Mathf.Clamp01(interpolation));
        }

        private void Record(AnimationPlaygroundGroundedMotion sample)
        {
            hasObservation = true;
            lastTick = sample.Tick;
            lastEpoch = sample.Epoch;
            lastTime = sample.CompletedTime;
        }

        private void EstablishDirection(Vector3 direction, Vector3 normal)
        {
            previousDirection = direction;
            previousNormal = normal;
            hasDirection = Valid = true;
        }

        private void ClearDemand()
        {
            Curvature = PreviousCurvature = AngularSpeed = 0;
            previousDirection = previousNormal = Vector3.zero;
            hasDirection = Valid = false;
        }

        private static bool Finite(Vector3 value) => float.IsFinite(value.x)
            && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
