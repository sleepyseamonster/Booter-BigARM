using System;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DSleepTests
    {
        private GameObject actor;
        private TopDown3DSurvivalVitals vitals;
        private TopDown3DCampSettings camp;
        private TopDown3DSurvivalSettings survival;
        [SetUp] public void Setup()
        {
            survival = ScriptableObject.CreateInstance<TopDown3DSurvivalSettings>();
            camp = ScriptableObject.CreateInstance<TopDown3DCampSettings>();
            actor = new GameObject("Sleep rules fixture"); vitals = actor.AddComponent<TopDown3DSurvivalVitals>();
            vitals.Configure(survival, true); vitals.SetValue(TopDown3DSurvivalVital.Reserve, 20);
        }
        [TearDown] public void Cleanup()
        { UnityEngine.Object.DestroyImmediate(actor); UnityEngine.Object.DestroyImmediate(camp); UnityEngine.Object.DestroyImmediate(survival); }

        [Test] public void DefaultSleepQuoteMatchesActualRecoveryAndCosts()
        {
            var quote = TopDown3DSleepRules.Quote(vitals, camp, TopDown3DSurvivalActivity.Sleep, 150);
            Assert.That(quote.Seconds, Is.EqualTo(150).Within(.0001));
            Assert.That(quote.Water, Is.EqualTo(4.2).Within(.0001));
            vitals.SetActivity(TopDown3DSurvivalActivity.Sleep, camp); vitals.AdvanceActivity(quote.Seconds);
            Assert.That(vitals.Reserve, Is.EqualTo(80).Within(.001));
            Assert.That(vitals.Hunger, Is.EqualTo(97.75).Within(.001));
            Assert.That(vitals.Thirst, Is.EqualTo(95.8).Within(.001));
            Assert.That(vitals.Oxygen, Is.EqualTo(98.5).Within(.001));
            Assert.That(vitals.Activity, Is.EqualTo(TopDown3DSurvivalActivity.Sleep));
        }
        [Test] public void EquivalentElapsedTimeIsIndependentOfStepSize()
        {
            vitals.SetActivity(TopDown3DSurvivalActivity.Sleep, camp);
            vitals.AdvanceActivity(200); var once = vitals.CaptureSnapshot();
            vitals.ResetToFull(); vitals.SetValue(TopDown3DSurvivalVital.Reserve, 20);
            vitals.SetActivity(TopDown3DSurvivalActivity.Sleep, camp);
            for (var i = 0; i < 200; i++) vitals.AdvanceActivity(1);
            Assert.That(vitals.Reserve, Is.EqualTo(once.Reserve).Within(.002));
            Assert.That(vitals.Thirst, Is.EqualTo(once.Thirst).Within(.002));
        }
        [Test] public void TinyFrameIntervalsStillConsumeSuppliesAndPreserveRates()
        {
            vitals.ResetToFull();
            for (var i = 0; i < 100000; i++) vitals.Advance(.0001f);
            Assert.That(vitals.Hunger, Is.EqualTo(99.75).Within(.0001));
            Assert.That(vitals.Thirst, Is.EqualTo(99.6).Within(.0001));
            vitals.ResetToFull(); vitals.SetValue(TopDown3DSurvivalVital.Reserve, 20);
            vitals.SetActivity(TopDown3DSurvivalActivity.Sleep, camp);
            for (var i = 0; i < 100000; i++) vitals.AdvanceActivity(.0001f);
            Assert.That(vitals.Thirst, Is.EqualTo(99.72).Within(.0001));
            Assert.That(vitals.Reserve, Is.EqualTo(24).Within(.0001));
        }
        [TestCase(20)]
        [TestCase(80)]
        [TestCase(100)]
        public void LowSuppliesLimitSleepWithoutCrossingReservedWakeFloor(float reserve)
        {
            vitals.SetValue(TopDown3DSurvivalVital.Reserve, reserve);
            vitals.SetValue(TopDown3DSurvivalVital.Thirst, 11);
            var quote = TopDown3DSleepRules.Quote(vitals, camp, TopDown3DSurvivalActivity.Sleep);
            Assert.That(quote.Seconds, Is.LessThan(36)); Assert.That(quote.CanStart, Is.True);
            vitals.SetActivity(TopDown3DSurvivalActivity.Sleep, camp); vitals.AdvanceActivity(quote.Seconds);
            Assert.That(vitals.Thirst, Is.GreaterThanOrEqualTo(10));
            if (reserve < 80) Assert.That(vitals.Reserve, Is.LessThan(80));
            else Assert.That(vitals.Reserve, Is.EqualTo(reserve));
            Assert.That(vitals.Activity, Is.EqualTo(TopDown3DSurvivalActivity.Ordinary));
        }
        [Test] public void UnsafeActionRemainsUnavailableRegardlessOfReserve()
        {
            vitals.SetValue(TopDown3DSurvivalVital.Reserve, 80);
            Assert.That(TopDown3DSleepRules.Quote(vitals, camp, TopDown3DSurvivalActivity.Sleep).IsUseful, Is.False);
            Assert.That(TopDown3DSleepRules.Quote(vitals, camp, TopDown3DSurvivalActivity.Sleep).CanStart, Is.True);
            vitals.SetValue(TopDown3DSurvivalVital.Reserve, 20); vitals.SetValue(TopDown3DSurvivalVital.Oxygen, 9);
            Assert.That(TopDown3DSleepRules.Quote(vitals, camp, TopDown3DSurvivalActivity.Sleep).IsUseful, Is.False);
            Assert.That(TopDown3DSleepRules.Quote(vitals, camp, TopDown3DSurvivalActivity.Sleep).CanStart, Is.False);
        }
        [Test] public void RestContinuesAtCapUntilDurationCompletes()
        {
            vitals.SetValue(TopDown3DSurvivalVital.Reserve, 39);
            var atCompletion = 0f; vitals.ActivityCompleted += _ => atCompletion = vitals.Reserve;
            vitals.SetActivity(TopDown3DSurvivalActivity.Rest, camp); vitals.AdvanceActivity(20);
            Assert.That(atCompletion, Is.EqualTo(40).Within(.001));
            Assert.That(vitals.Reserve, Is.EqualTo(40).Within(.001));
            Assert.That(vitals.Activity, Is.EqualTo(TopDown3DSurvivalActivity.Ordinary));
        }
        [TestCase(TopDown3DSurvivalActivity.Sleep, 80)]
        [TestCase(TopDown3DSurvivalActivity.Sleep, 100)]
        [TestCase(TopDown3DSurvivalActivity.Rest, 40)]
        [TestCase(TopDown3DSurvivalActivity.Rest, 100)]
        public void FullReserveActivityStillConsumesSuppliesAndDoesNotLowerReserve(TopDown3DSurvivalActivity mode, float reserve)
        {
            vitals.SetValue(TopDown3DSurvivalVital.Reserve, reserve);
            var quote = TopDown3DSleepRules.Quote(vitals, camp, mode, 10);
            Assert.That(quote.CanStart, Is.True); Assert.That(quote.Gain, Is.Zero);
            vitals.SetActivity(mode, camp); vitals.AdvanceActivity(10);
            Assert.That(vitals.Activity, Is.EqualTo(mode));
            Assert.That(vitals.Reserve, Is.EqualTo(reserve));
            Assert.That(vitals.Thirst, Is.EqualTo(100 - quote.Water).Within(.001));
            Assert.That(vitals.Oxygen, Is.LessThan(100));
        }
        [Test] public void TimestampedSleepAndWakeSplitAnIntervalExactlyOnce()
        {
            vitals.ReconcileRuntimeClock(0, 0, false);
            vitals.QueueActivity(TopDown3DSurvivalActivity.Sleep, camp, 2);
            vitals.QueueActivity(TopDown3DSurvivalActivity.Ordinary, null, 6);
            vitals.ReconcileRuntimeClock(10, 10, false);
            Assert.That(vitals.Reserve, Is.EqualTo(21.54).Within(.001));
            Assert.That(vitals.Hunger, Is.EqualTo(99.79).Within(.001));
            vitals.ReconcileRuntimeClock(10, 0, false);
            Assert.That(vitals.Reserve, Is.EqualTo(21.54).Within(.001));
        }
        [Test] public void HealthLossIsSynchronousButRestoreDoesNotCountAsDamage()
        {
            var count = 0; vitals.HealthReduced += () => count++;
            vitals.SetValue(TopDown3DSurvivalVital.Health, 90); Assert.That(count, Is.EqualTo(1));
            var snapshot = vitals.CaptureSnapshot(); vitals.ResetToFull(); vitals.ApplySnapshot(snapshot);
            Assert.That(count, Is.EqualTo(1));
            Assert.Throws<ArgumentException>(() => vitals.SetValue(TopDown3DSurvivalVital.Oxygen, float.NaN));
        }
        [Test] public void EvenSmallRepresentableHealthLossEmitsWakeSignal()
        {
            var count = 0; vitals.HealthReduced += () => count++;
            vitals.SetValue(TopDown3DSurvivalVital.Health, 99.99995f);
            Assert.That(count, Is.EqualTo(1)); Assert.That(vitals.Health, Is.LessThan(100));
        }
        [Test] public void SpeedRequestRestoresBaselineAndDoesNotOverwriteExternalChange()
        {
            var original = Time.timeScale; var owner = new object(); var speed = actor.AddComponent<TopDown3DWorldSpeedController>();
            try
            {
                Time.timeScale = 1; Assert.That(speed.TryAcquire(owner, 4), Is.True);
                Assert.That(speed.TryAcquire(new object(), 4), Is.False);
                speed.Release(owner); speed.Release(owner); Assert.That(Time.timeScale, Is.EqualTo(1));
                Assert.That(speed.TryAcquire(owner, 4), Is.True); Time.timeScale = .5f; speed.Release(owner);
                Assert.That(Time.timeScale, Is.EqualTo(.5));
            }
            finally { Time.timeScale = original; }
        }
    }
}
