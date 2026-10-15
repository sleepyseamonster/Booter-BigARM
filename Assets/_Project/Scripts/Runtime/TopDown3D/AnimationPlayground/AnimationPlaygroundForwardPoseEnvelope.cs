using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    // Presentation only. This never advances the gait clock or creates a contact.
    // Hold the accepted phase while blending a suddenly blocked pose into idle.
    public sealed class AnimationPlaygroundForwardPoseEnvelope
    {
        public const float SettleSeconds = .18f, ResumeSeconds = .16f;
        private float previousActual, from, elapsed;
        private AnimationPlaygroundGaitWeights fromWeights;
        private bool settling, resuming;
        public float Speed { get; private set; }
        public bool Settling => settling;
        public bool Resuming => resuming;
        public AnimationPlaygroundGaitWeights GaitWeights { get; private set; } = AnimationPlaygroundGaitWeights.Idle;
        public Vector4 Weights => GaitWeights.Legacy;

        public void Reset()
        {
            previousActual = from = elapsed = Speed = 0;
            settling = resuming = false;
            GaitWeights = fromWeights = AnimationPlaygroundGaitWeights.Idle;
        }

        public float Sample(float actualSpeed, float deltaTime)
        {
            SampleWeights(actualSpeed > 0 ? new Vector4(0, 1, 0, 0) : new Vector4(1, 0, 0, 0), actualSpeed, deltaTime);
            return Speed;
        }

        public Vector4 SampleWeights(Vector4 target, float actualSpeed, float deltaTime)
            => SampleGaitWeights(new AnimationPlaygroundGaitWeights(target), actualSpeed, deltaTime).Legacy;

        public AnimationPlaygroundGaitWeights SampleGaitWeights(AnimationPlaygroundGaitWeights target, float actualSpeed, float deltaTime)
        {
            if (!float.IsFinite(actualSpeed) || actualSpeed < 0 || !float.IsFinite(deltaTime) || deltaTime < 0)
                throw new ArgumentException("Forward pose samples require finite nonnegative speed and duration.");
            if (actualSpeed <= 0)
            {
                if (previousActual > 0) { from = Speed; fromWeights = GaitWeights; elapsed = 0; settling = true; resuming = false; }
                if (settling)
                {
                    elapsed = Mathf.Min(SettleSeconds, elapsed + deltaTime);
                    var blend = Mathf.SmoothStep(0, 1, elapsed / SettleSeconds);
                    Speed = Mathf.Lerp(from, 0, blend);
                    GaitWeights = AnimationPlaygroundGaitWeights.Lerp(fromWeights, AnimationPlaygroundGaitWeights.Idle, blend);
                    if (elapsed >= SettleSeconds) { Speed = 0; settling = false; }
                }
                else { Speed = 0; GaitWeights = AnimationPlaygroundGaitWeights.Idle; }
            }
            else
            {
                if (settling) { from = Speed; fromWeights = GaitWeights; elapsed = 0; settling = false; resuming = true; }
                if (resuming)
                {
                    elapsed = Mathf.Min(ResumeSeconds, elapsed + deltaTime);
                    var blend = Mathf.SmoothStep(0, 1, elapsed / ResumeSeconds);
                    Speed = Mathf.Lerp(from, actualSpeed, blend); GaitWeights = AnimationPlaygroundGaitWeights.Lerp(fromWeights, target, blend);
                    if (elapsed >= ResumeSeconds) resuming = false;
                }
                else { Speed = actualSpeed; GaitWeights = target; }
            }
            previousActual = actualSpeed;
            return GaitWeights;
        }
    }
}
