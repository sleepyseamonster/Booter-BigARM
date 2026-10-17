using System;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;

namespace BooterBigArm.Tests
{
    // Pure completed-motion observations: no actor, input, scene or physics step.
    public sealed class AnimationPlaygroundMovingTurnDemandTests
    {
        private const double Duration = .02;
        private static AnimationPlaygroundGroundedMotion Sample(long tick, float heading, float speed = 3,
            long epoch = 1, bool available = true, bool rebased = false, Quaternion? basis = null)
        {
            var frame = basis ?? Quaternion.identity;
            return new AnimationPlaygroundGroundedMotion(tick, epoch, tick * Duration, Duration,
                frame * (Quaternion.AngleAxis(heading, Vector3.up) * Vector3.forward * (float)(speed * Duration)),
                frame * Vector3.up, available, rebased);
        }

        private static AnimationPlaygroundMovingTurnDemand Turning(float sign = 1)
        {
            var tracker = new AnimationPlaygroundMovingTurnDemand();
            for (var i = 1; i <= 60; i++) tracker.Observe(Sample(i, sign * (i - 1) * 1.2f), true);
            return tracker;
        }

        private static void Zero(AnimationPlaygroundMovingTurnDemand tracker, bool valid = false)
        {
            Assert.That(tracker.Curvature, Is.Zero);
            Assert.That(tracker.PreviousCurvature, Is.Zero);
            Assert.That(tracker.AngularSpeed, Is.Zero);
            Assert.That(tracker.Valid, Is.EqualTo(valid));
            Assert.That(tracker.RenderCurvature(.5f), Is.Zero);
        }

        [TestCase(1.4f)] [TestCase(3f)] [TestCase(4f)] [TestCase(4.9f)] [TestCase(6.15f)]
        public void StraightTravelKeepsEveryAcceptedGaitAtZero(float speed)
        {
            var tracker = new AnimationPlaygroundMovingTurnDemand();
            for (var i = 1; i <= 60; i++) tracker.Observe(Sample(i, 35, speed), true);
            Zero(tracker, true);
        }

        [TestCase(-1f, 1.4f)] [TestCase(1f, 1.4f)]
        [TestCase(-1f, 3f)] [TestCase(1f, 3f)]
        [TestCase(-1f, 4f)] [TestCase(1f, 4f)]
        [TestCase(-1f, 4.9f)] [TestCase(1f, 4.9f)]
        [TestCase(-1f, 6.15f)] [TestCase(1f, 6.15f)]
        public void ConstantTravelCurvesMeasureSignedDegreesPerMetre(float sign, float speed)
        {
            var tracker = new AnimationPlaygroundMovingTurnDemand();
            const float curvature = 20;
            for (var i = 1; i <= 80; i++)
                tracker.Observe(Sample(i, sign * (i - 1) * curvature * speed * (float)Duration, speed), true);
            Assert.That(tracker.Valid, Is.True);
            Assert.That(tracker.Curvature, Is.EqualTo(sign * curvature).Within(.002f));
            Assert.That(tracker.AngularSpeed, Is.EqualTo(sign * curvature * speed).Within(.01f));
        }

        [Test] public void FirstFreshDirectionEstablishesAZeroDemandBaseline()
        {
            var tracker = new AnimationPlaygroundMovingTurnDemand();
            tracker.Observe(Sample(1, 150), true);
            Zero(tracker, true);
            tracker.Observe(Sample(2, 151.2f), true);
            Assert.That(tracker.Curvature, Is.GreaterThan(0).And.LessThan(20));
        }

        [Test] public void SCurveChangesSignAndStraightTravelReleasesTheDemand()
        {
            var tracker = Turning();
            var heading = 59 * 1.2f;
            for (var i = 61; i <= 100; i++) tracker.Observe(Sample(i, heading - (i - 60) * 1.2f), true);
            Assert.That(tracker.Curvature, Is.LessThan(-19.9f));
            heading -= 40 * 1.2f;
            for (var i = 101; i <= 160; i++) tracker.Observe(Sample(i, heading), true);
            Assert.That(Mathf.Abs(tracker.Curvature), Is.LessThan(.001f));
            Assert.That(Mathf.Abs(tracker.AngularSpeed), Is.LessThan(.002f));
        }

        [Test] public void DuplicateTickCannotSmoothTwiceOrEraseACompletedObservation()
        {
            var tracker = Turning();
            var curvature = tracker.Curvature;
            var previous = tracker.PreviousCurvature;
            var angular = tracker.AngularSpeed;
            tracker.Observe(Sample(60, -90), true);
            tracker.Observe(new AnimationPlaygroundGroundedMotion(60, 1, 60 * Duration, 0,
                Vector3.zero, Vector3.up, false, false), true);
            Assert.That(tracker.Curvature, Is.EqualTo(curvature));
            Assert.That(tracker.PreviousCurvature, Is.EqualTo(previous));
            Assert.That(tracker.AngularSpeed, Is.EqualTo(angular));
            Assert.That(tracker.Valid, Is.True);
        }

        [Test] public void EligibilityLossClearsEvenAnAlreadyObservedTick()
        {
            var tracker = Turning();
            tracker.Observe(Sample(60, 90), false);
            Zero(tracker);
            tracker.Observe(Sample(61, -90), true);
            Zero(tracker, true);
            tracker.Observe(Sample(62, -91.2f), true);
            Assert.That(tracker.Curvature, Is.LessThan(0));
        }

        [Test] public void ChangedEpochOrMissingTickStartsAZeroDemandBaseline()
        {
            var tracker = Turning();
            tracker.Observe(Sample(61, -90, epoch: 2), true);
            Zero(tracker, true);
            tracker.Observe(Sample(62, -91.2f, epoch: 2), true);
            Assert.That(tracker.Curvature, Is.LessThan(0));
            tracker.Observe(Sample(65, 90, epoch: 2), true);
            Zero(tracker, true);
        }

        [Test] public void RebasedObservationDropsTheOldDirectionBeforeFreshTravel()
        {
            var tracker = Turning();
            tracker.Observe(Sample(61, -90, rebased: true), true);
            Zero(tracker);
            tracker.Observe(Sample(62, 90), true);
            Zero(tracker, true);
            tracker.Observe(Sample(63, 88.8f), true);
            Assert.That(tracker.Curvature, Is.LessThan(0));
        }

        [TestCase("unavailable")] [TestCase("blocked")] [TestCase("tiny")]
        [TestCase("slow")] [TestCase("teleport")] [TestCase("zero duration")]
        [TestCase("negative duration")] [TestCase("stale duration")] [TestCase("NaN duration")]
        [TestCase("NaN time")] [TestCase("infinite time")] [TestCase("NaN displacement")] [TestCase("zero normal")]
        [TestCase("NaN normal")]
        public void InvalidTravelClearsDemandAndDoesNotResumeAStaleTurn(string failure)
        {
            var tracker = Turning();
            var sample = Sample(61, -90);
            var displacement = sample.TangentDisplacement;
            var normal = sample.SupportNormal;
            var duration = sample.Duration;
            var time = sample.CompletedTime;
            switch (failure)
            {
                case "blocked": displacement = Vector3.zero; break;
                case "tiny": displacement = Vector3.forward * .000001f; break;
                case "slow": displacement = Vector3.forward * (float)(.29 * Duration); break;
                case "teleport": displacement = Vector3.forward * (float)(13 * Duration); break;
                case "zero duration": duration = 0; break;
                case "negative duration": duration = -Duration; break;
                case "stale duration": duration = .5; break;
                case "NaN duration": duration = double.NaN; break;
                case "NaN time": time = double.NaN; break;
                case "infinite time": time = double.PositiveInfinity; break;
                case "NaN displacement": displacement = new Vector3(float.NaN, 0, 0); break;
                case "zero normal": normal = Vector3.zero; break;
                case "NaN normal": normal = new Vector3(0, float.NaN, 0); break;
            }
            tracker.Observe(new AnimationPlaygroundGroundedMotion(61, 1, time, duration,
                displacement, normal, failure != "unavailable", false), true);
            Zero(tracker);
            tracker.Observe(Sample(62, -90), true);
            Zero(tracker, true);
        }

        [Test] public void ReversedTickOrTimeCannotBecomeAnOrdinaryArc()
        {
            var tracker = Turning();
            tracker.Observe(Sample(59, -90), true);
            Zero(tracker);
            tracker.Observe(Sample(60, -90), true);
            Zero(tracker, true);
            tracker.Observe(new AnimationPlaygroundGroundedMotion(61, 1, 59 * Duration, Duration,
                Vector3.left * .06f, Vector3.up, true, false), true);
            Zero(tracker);
        }

        [Test] public void ALongObservationGapCannotReuseTheEarlierDirection()
        {
            var tracker = Turning();
            tracker.Observe(new AnimationPlaygroundGroundedMotion(61, 1, 10, Duration,
                Vector3.left * .06f, Vector3.up, true, false), true);
            Zero(tracker, true);
            tracker.Observe(new AnimationPlaygroundGroundedMotion(62, 1, 10 + Duration, Duration,
                Quaternion.AngleAxis(-1.2f, Vector3.up) * Vector3.left * .06f, Vector3.up, true, false), true);
            Assert.That(tracker.Curvature, Is.LessThan(0));
        }

        [Test] public void ACommonSlopedPlanePreservesTheSignedCurveMeasurement()
        {
            var flat = new AnimationPlaygroundMovingTurnDemand();
            var sloped = new AnimationPlaygroundMovingTurnDemand();
            var basis = Quaternion.Euler(24, 41, 13);
            for (var i = 1; i <= 60; i++)
            {
                flat.Observe(Sample(i, (i - 1) * 1.2f), true);
                sloped.Observe(Sample(i, (i - 1) * 1.2f, basis: basis), true);
            }
            Assert.That(sloped.Valid, Is.True);
            Assert.That(sloped.Curvature, Is.EqualTo(flat.Curvature).Within(.001f));
            Assert.That(sloped.AngularSpeed, Is.EqualTo(flat.AngularSpeed).Within(.002f));
        }

        [Test] public void ChangingGradeWithoutHorizontalTurningDoesNotCreateAnArc()
        {
            var tracker = new AnimationPlaygroundMovingTurnDemand();
            for (var i = 1; i <= 60; i++)
                tracker.Observe(Sample(i, 0, basis: Quaternion.AngleAxis(i % 2 == 0 ? 10 : -10, Vector3.right)), true);
            Zero(tracker, true);
        }

        [Test] public void DiscontinuousSupportNormalDropsDemandAndRequiresAFreshDirection()
        {
            var tracker = Turning();
            var slope = Quaternion.AngleAxis(50, Vector3.right);
            tracker.Observe(Sample(61, -90, basis: slope), true);
            Zero(tracker);
            tracker.Observe(Sample(62, 90, basis: slope), true);
            Zero(tracker, true);
            tracker.Observe(Sample(63, 91.2f, basis: slope), true);
            Assert.That(tracker.Curvature, Is.GreaterThan(0));
        }

        [TestCase(.31f)] [TestCase(6.15f)]
        public void RepeatedExtremeReversalsRemainFiniteAndBounded(float speed)
        {
            var tracker = new AnimationPlaygroundMovingTurnDemand();
            for (var i = 1; i <= 80; i++)
            {
                tracker.Observe(Sample(i, i % 2 == 0 ? 180 : 0, speed), true);
                Assert.That(float.IsFinite(tracker.Curvature + tracker.AngularSpeed), Is.True);
                Assert.That(Mathf.Abs(tracker.Curvature), Is.LessThanOrEqualTo(AnimationPlaygroundMovingTurnDemand.MaximumCurvature));
                Assert.That(Mathf.Abs(tracker.AngularSpeed), Is.LessThanOrEqualTo(AnimationPlaygroundMovingTurnDemand.MaximumAngularSpeed));
            }
        }

        [Test] public void RenderingOnlyInterpolatesTheCompletedDemandWithinBounds()
        {
            var tracker = new AnimationPlaygroundMovingTurnDemand();
            tracker.Observe(Sample(1, 0), true);
            tracker.Observe(Sample(2, 1.2f), true);
            var previous = tracker.PreviousCurvature;
            var current = tracker.Curvature;
            Assert.That(tracker.RenderCurvature(-10), Is.EqualTo(previous));
            Assert.That(tracker.RenderCurvature(0), Is.EqualTo(previous));
            Assert.That(tracker.RenderCurvature(.5f), Is.EqualTo((previous + current) * .5f).Within(.000001));
            Assert.That(tracker.RenderCurvature(1), Is.EqualTo(current));
            Assert.That(tracker.RenderCurvature(10), Is.EqualTo(current));
            Assert.That(tracker.RenderCurvature(float.NaN), Is.Zero);
            Assert.That(tracker.RenderCurvature(float.PositiveInfinity), Is.Zero);
            Assert.That(tracker.RenderCurvature(float.NegativeInfinity), Is.Zero);
            Assert.That(tracker.Curvature, Is.EqualTo(current));
            Assert.That(tracker.PreviousCurvature, Is.EqualTo(previous));
        }

        [Test] public void ResetAllowsTheSameTickToEstablishANewBaseline()
        {
            var tracker = Turning();
            tracker.Reset();
            Zero(tracker);
            tracker.Observe(Sample(60, -90), true);
            Zero(tracker, true);
        }
    }
}
