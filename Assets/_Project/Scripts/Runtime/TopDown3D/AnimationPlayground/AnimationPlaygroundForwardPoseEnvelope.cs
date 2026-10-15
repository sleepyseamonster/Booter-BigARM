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
        private Vector4 fromWeights;
        private bool settling, resuming;
        public float Speed { get; private set; }
        public bool Settling => settling;
        public bool Resuming => resuming;
        public Vector4 Weights { get; private set; } = new(1, 0, 0, 0);

        public void Reset()
        {
            previousActual = from = elapsed = Speed = 0;
            settling = resuming = false;
            Weights = fromWeights = new Vector4(1, 0, 0, 0);
        }

        public float Sample(float actualSpeed, float deltaTime)
        {
            SampleWeights(actualSpeed > 0 ? new Vector4(0, 1, 0, 0) : new Vector4(1, 0, 0, 0), actualSpeed, deltaTime);
            return Speed;
        }

        public Vector4 SampleWeights(Vector4 target, float actualSpeed, float deltaTime)
        {
            if (!float.IsFinite(actualSpeed) || actualSpeed < 0 || !float.IsFinite(deltaTime) || deltaTime < 0)
                throw new ArgumentException("Forward pose samples require finite nonnegative speed and duration.");
            if (actualSpeed <= 0)
            {
                if (previousActual > 0) { from = Speed; fromWeights = Weights; elapsed = 0; settling = true; resuming = false; }
                if (settling)
                {
                    elapsed = Mathf.Min(SettleSeconds, elapsed + deltaTime);
                    var blend = Mathf.SmoothStep(0, 1, elapsed / SettleSeconds);
                    Speed = Mathf.Lerp(from, 0, blend);
                    Weights = Vector4.Lerp(fromWeights, new Vector4(1, 0, 0, 0), blend);
                    if (elapsed >= SettleSeconds) { Speed = 0; settling = false; }
                }
                else { Speed = 0; Weights = new Vector4(1, 0, 0, 0); }
            }
            else
            {
                if (settling) { from = Speed; fromWeights = Weights; elapsed = 0; settling = false; resuming = true; }
                if (resuming)
                {
                    elapsed = Mathf.Min(ResumeSeconds, elapsed + deltaTime);
                    var blend = Mathf.SmoothStep(0, 1, elapsed / ResumeSeconds);
                    Speed = Mathf.Lerp(from, actualSpeed, blend); Weights = Vector4.Lerp(fromWeights, target, blend);
                    if (elapsed >= ResumeSeconds) resuming = false;
                }
                else { Speed = actualSpeed; Weights = target; }
            }
            previousActual = actualSpeed;
            return Weights;
        }
    }
}
