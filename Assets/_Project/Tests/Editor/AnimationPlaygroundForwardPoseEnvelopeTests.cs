using System;
using BooterBigArm.TopDown3D;
using NUnit.Framework;

namespace BooterBigArm.Tests
{
    public sealed class AnimationPlaygroundForwardPoseEnvelopeTests
    {
        [TestCase(30)] [TestCase(60)] [TestCase(144)]
        public void BlockedPoseSettlesWithinTheBoundAndStaysIdle(int rate)
        {
            var envelope = new AnimationPlaygroundForwardPoseEnvelope();
            Assert.That(envelope.Sample(3.8f, 1f / rate), Is.EqualTo(3.8f));
            var previous = 3.8f;
            for (var frame = 0; frame < rate; frame++)
            {
                var speed = envelope.Sample(0, 1f / rate);
                Assert.That(speed, Is.InRange(0, previous)); previous = speed;
                if (frame / (float)rate >= AnimationPlaygroundForwardPoseEnvelope.SettleSeconds) Assert.That(speed, Is.Zero);
            }
            Assert.That(envelope.Settling, Is.False);
        }

        [Test] public void StopFadesTheCurrentSourceDirectlyToIdleWithoutVisitingOtherGaits()
        {
            var envelope = new AnimationPlaygroundForwardPoseEnvelope();
            envelope.SampleWeights(new UnityEngine.Vector4(0,0,0,1),3.8f,.02f);
            var weights = envelope.SampleWeights(new UnityEngine.Vector4(1,0,0,0),0,.06f);
            Assert.That(weights.x,Is.GreaterThan(0)); Assert.That(weights.w,Is.GreaterThan(0));
            Assert.That(weights.y+weights.z,Is.Zero); Assert.That(weights.x+weights.w,Is.EqualTo(1).Within(.00001));
            var held=weights;
            weights=envelope.SampleWeights(new UnityEngine.Vector4(0,1,0,0),.2f,0);
            Assert.That(weights,Is.EqualTo(held));
        }

        [Test]
        public void ResumeInterruptsSettleContinuouslyAndReturnsToActualTravel()
        {
            var envelope = new AnimationPlaygroundForwardPoseEnvelope();
            envelope.Sample(3.8f, .02f); var held = envelope.Sample(0, .04f);
            Assert.That(envelope.Sample(.2f, 0), Is.EqualTo(held));
            Assert.That(envelope.Resuming, Is.True);
            Assert.That(envelope.Sample(.4f, AnimationPlaygroundForwardPoseEnvelope.ResumeSeconds), Is.EqualTo(.4f));
            Assert.That(envelope.Sample(.7f, .02f), Is.EqualTo(.7f));
            Assert.That(envelope.Resuming || envelope.Settling, Is.False);
        }

        [Test]
        public void ResetAndLongFrameDoNotLeaveAStaleMovingPose()
        {
            var envelope = new AnimationPlaygroundForwardPoseEnvelope();
            Assert.That(envelope.Sample(0, .02f), Is.Zero);
            envelope.Sample(2.7f, .02f);
            Assert.That(envelope.Sample(0, 1), Is.Zero);
            envelope.Sample(3.8f, .02f); envelope.Sample(0, .02f); envelope.Reset();
            Assert.That(envelope.Sample(0, 0), Is.Zero);
            Assert.That(envelope.Sample(.3f, 0), Is.EqualTo(.3f));
            Assert.Throws<ArgumentException>(() => envelope.Sample(float.NaN, .02f));
            Assert.Throws<ArgumentException>(() => envelope.Sample(1, -.01f));
        }
    }
}
