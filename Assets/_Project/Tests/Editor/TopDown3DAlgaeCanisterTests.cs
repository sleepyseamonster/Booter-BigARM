using System;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DAlgaeCanisterTests
    {
        private GameObject player;
        private TopDown3DSurvivalVitals vitals;
        private TopDown3DAlgaeCanisterState state;
        private TopDown3DAlgaeCanisterSettings algaeSettings;
        private TopDown3DSurvivalSettings survivalSettings;
        [SetUp] public void Setup()
        {
            player=new GameObject("Algae transaction test");
            survivalSettings=ScriptableObject.CreateInstance<TopDown3DSurvivalSettings>();
            algaeSettings=ScriptableObject.CreateInstance<TopDown3DAlgaeCanisterSettings>();
            vitals=player.AddComponent<TopDown3DSurvivalVitals>(); vitals.Configure(survivalSettings,true);
            state=player.AddComponent<TopDown3DAlgaeCanisterState>(); state.Configure(algaeSettings);
        }
        [TearDown] public void Cleanup(){UnityEngine.Object.DestroyImmediate(player);UnityEngine.Object.DestroyImmediate(algaeSettings);UnityEngine.Object.DestroyImmediate(survivalSettings);}
        [Test] public void PassiveGrowthRecoversFromEmptyAndClampsAtCapacity()
        {
            state.ApplySnapshot(new(){remaining=0,capacity=100});state.Advance(60);
            Assert.That(state.Remaining,Is.EqualTo(1.2).Within(1e-7));
            state.Advance(10000);Assert.That(state.Remaining,Is.EqualTo(100));
            var changes=0;state.Changed+=()=>changes++;state.Advance(60);Assert.That(changes,Is.Zero);
        }
        [Test] public void GrowthUsesElapsedSimulationTimeAndRejectsInvalidIntervals()
        {
            state.ApplySnapshot(new(){remaining=0,capacity=100});
            state.Advance(0);state.Advance(-1);state.Advance(double.NaN);state.Advance(double.PositiveInfinity);
            Assert.That(state.Remaining,Is.Zero);
            for(var i=0;i<3600;i++)state.Advance(1d/60);
            var ordinary=state.Remaining;state.ApplySnapshot(new(){remaining=0,capacity=100});state.Advance(240);
            Assert.That(state.Remaining,Is.EqualTo(ordinary*4).Within(1e-9));
            state.ApplySnapshot(new(){remaining=7,capacity=100});state.Configure(algaeSettings);
            Assert.That(state.Remaining,Is.EqualTo(7),"Restore and configuration cannot invent offline growth");
        }
        [Test] public void OrdinarySurvivalIsSustainableWithModestSurplus()
        {
            var demand=survivalSettings.HungerDepletionPerSecond*algaeSettings.eatCostPerPoint
                +survivalSettings.ThirstDepletionPerSecond*algaeSettings.drinkCostPerPoint;
            Assert.That(algaeSettings.replenishmentPerSecond/demand,Is.InRange(1.1f,1.3f));
            state.ApplySnapshot(new(){remaining=50,capacity=100});
            for(var i=0;i<60;i++)
            {
                vitals.Advance(60);state.Advance(60);
                Assert.That(state.TryConsume(vitals,TopDown3DAlgaeUse.Drink,out _,out _),Is.True);
                Assert.That(state.TryConsume(vitals,TopDown3DAlgaeUse.Eat,out _,out _),Is.True);
            }
            Assert.That(state.Remaining,Is.EqualTo(63.5).Within(.001));
            Assert.That(vitals.Thirst,Is.EqualTo(100));Assert.That(vitals.Hunger,Is.EqualTo(100));
        }
        [Test] public void GrowthDoesNotReenterTransactionsAndZeroRateCanDisableIt()
        {
            state.ApplySnapshot(new(){remaining=50,capacity=100});
            var changes=0;state.Changed+=()=>{changes++;state.Advance(60);};
            state.Advance(60);Assert.That(state.Remaining,Is.EqualTo(51.2).Within(1e-7));Assert.That(changes,Is.EqualTo(1));
            algaeSettings.replenishmentPerSecond=0;state.Advance(60);Assert.That(changes,Is.EqualTo(1));
            algaeSettings.replenishmentPerSecond=float.NaN;Assert.That(algaeSettings.IsValid,Is.False);
            algaeSettings.replenishmentPerSecond=-1;Assert.That(algaeSettings.IsValid,Is.False);
        }
        [TestCase(TopDown3DAlgaeUse.Drink,TopDown3DSurvivalVital.Thirst,20,5)]
        [TestCase(TopDown3DAlgaeUse.Eat,TopDown3DSurvivalVital.Hunger,20,5)]
        [TestCase(TopDown3DAlgaeUse.Treat,TopDown3DSurvivalVital.Health,10,10)]
        public void DoseChangesOnlyTargetAndChargesUsefulGain(TopDown3DAlgaeUse use,TopDown3DSurvivalVital vital,double gain,double cost)
        {
            vitals.SetValue(vital,50);
            Assert.That(state.TryConsume(vitals,use,out var actualGain,out var actualCost),Is.True);
            Assert.That(actualGain,Is.EqualTo(gain));Assert.That(actualCost,Is.EqualTo(cost));
            Assert.That(vitals.GetValue(vital),Is.EqualTo(50+gain));Assert.That(state.Remaining,Is.EqualTo(100-cost));
            Assert.That(vitals.Oxygen,Is.EqualTo(100));Assert.That(vitals.Reserve,Is.EqualTo(100));
            foreach(var other in new[]{TopDown3DSurvivalVital.Health,TopDown3DSurvivalVital.Hunger,TopDown3DSurvivalVital.Thirst})
                if(other!=vital)Assert.That(vitals.GetValue(other),Is.EqualTo(100));
        }
        [Test] public void PartialDosesDoNotWasteOrOverdraw()
        {
            vitals.SetValue(TopDown3DSurvivalVital.Thirst,96);
            Assert.That(state.TryConsume(vitals,TopDown3DAlgaeUse.Drink,out var gain,out var cost),Is.True);
            Assert.That(gain,Is.EqualTo(4));Assert.That(cost,Is.EqualTo(1));
            Assert.That(state.ApplySnapshot(new(){remaining=2,capacity=100}),Is.True);
            vitals.SetValue(TopDown3DSurvivalVital.Thirst,50);
            Assert.That(state.TryConsume(vitals,TopDown3DAlgaeUse.Drink,out gain,out cost),Is.True);
            Assert.That(gain,Is.EqualTo(8));Assert.That(cost,Is.EqualTo(2));Assert.That(state.Remaining,Is.Zero);
            Assert.That(state.TryConsume(vitals,TopDown3DAlgaeUse.Drink,out _,out _),Is.False);
        }
        [Test] public void FullDeadAndSubVisibleTargetsSpendNothing()
        {
            Assert.That(state.TryConsume(vitals,TopDown3DAlgaeUse.Drink,out _,out _),Is.False);
            vitals.SetValue(TopDown3DSurvivalVital.Health,0);
            Assert.That(state.TryConsume(vitals,TopDown3DAlgaeUse.Treat,out _,out _),Is.False);
            Assert.That(state.Remaining,Is.EqualTo(100));
            vitals.SetValue(TopDown3DSurvivalVital.Health,50);
            state.ApplySnapshot(new(){remaining=1e-10,capacity=100});
            Assert.That(state.TryConsume(vitals,TopDown3DAlgaeUse.Treat,out _,out _),Is.False);
            Assert.That(state.Remaining,Is.EqualTo(1e-10));
        }
        [Test] public void NotificationsSeeBothCommittedValuesAndRejectReentrantUse()
        {
            vitals.SetValue(TopDown3DSurvivalVital.Thirst,50);
            var notifications=0;
            vitals.Changed+=()=>{notifications++;Assert.That(state.Remaining,Is.EqualTo(95));Assert.That(vitals.Thirst,Is.EqualTo(70));
                Assert.That(state.TryConsume(vitals,TopDown3DAlgaeUse.Eat,out _,out _),Is.False);};
            state.Changed+=()=>{notifications++;Assert.That(vitals.Thirst,Is.EqualTo(70));};
            Assert.That(state.TryConsume(vitals,TopDown3DAlgaeUse.Drink,out _,out _),Is.True);Assert.That(notifications,Is.EqualTo(2));
        }
        [Test] public void ClockReconcilesOnceAndDosePreservesFractionalDepletion()
        {
            vitals.ReconcileRuntimeClock(10,.1f,false);
            var before=vitals.GetPreciseValue(TopDown3DSurvivalVital.Thirst);
            Assert.That(state.TryConsume(vitals,TopDown3DAlgaeUse.Drink,out var gain,out var cost),Is.True);
            Assert.That(gain,Is.EqualTo(100-before).Within(1e-10));Assert.That(cost,Is.EqualTo(gain*.25).Within(1e-10));
            vitals.ReconcileRuntimeClock(10,.1f,false);Assert.That(vitals.GetPreciseValue(TopDown3DSurvivalVital.Thirst),Is.EqualTo(100));
            vitals.SetValue(TopDown3DSurvivalVital.Thirst,50);
            vitals.Advance(.00001f);before=vitals.GetPreciseValue(TopDown3DSurvivalVital.Thirst);
            state.TryConsume(vitals,TopDown3DAlgaeUse.Drink,out gain,out _);
            Assert.That(vitals.GetPreciseValue(TopDown3DSurvivalVital.Thirst),Is.EqualTo(before+gain).Within(1e-12));
        }
        [Test] public void SnapshotsValidateAndReinstallationDoesNotRefill()
        {
            Assert.That(state.ApplySnapshot(new(){remaining=12.5,capacity=100}),Is.True);
            state.Configure(algaeSettings);state.enabled=false;state.enabled=true;
            Assert.That(state.Remaining,Is.EqualTo(12.5));
            Assert.That(state.ApplySnapshot(new(){remaining=double.NaN,capacity=100}),Is.False);
            Assert.That(state.ApplySnapshot(new(){version=2,remaining=50,capacity=100}),Is.False);
            Assert.That(state.ApplySnapshot(new(){remaining=-1,capacity=100}),Is.False);
            Assert.That(state.ApplySnapshot(new(){remaining=101,capacity=100}),Is.False);
            algaeSettings.capacity=10;algaeSettings.initialFill=10;state.Configure(algaeSettings);
            Assert.That(state.Remaining,Is.EqualTo(10));
            Assert.That(state.ApplySnapshot(new(){remaining=12.5,capacity=100}),Is.True);Assert.That(state.Remaining,Is.EqualTo(10));
        }
        [Test] public void InvalidSettingsRejectWithoutStateMutation()
        {
            algaeSettings.drinkCostPerPoint=0;
            Assert.Throws<ArgumentException>(()=>state.Configure(algaeSettings));
            Assert.That(state.TryConsume(vitals,TopDown3DAlgaeUse.Drink,out _,out _),Is.False);
            Assert.That(state.Remaining,Is.EqualTo(100));
        }
        [Test] public void RepeatedPartialDosesConserveContentsAtNonstandardCapacities()
        {
            var serialized=new UnityEditor.SerializedObject(survivalSettings);serialized.FindProperty("maximumThirst").floatValue=75;serialized.ApplyModifiedPropertiesWithoutUndo();
            vitals.Configure(survivalSettings,true); state.ApplySnapshot(new(){remaining=3.1,capacity=100});
            var start=state.Remaining;double gained=0;
            for(var i=0;i<50;i++)
            {
                vitals.SetValue(TopDown3DSurvivalVital.Thirst,74.7f);
                if(state.TryConsume(vitals,TopDown3DAlgaeUse.Drink,out var gain,out _))gained+=gain;
                Assert.That(vitals.Thirst,Is.LessThanOrEqualTo(75));Assert.That(state.Remaining,Is.GreaterThanOrEqualTo(0));
            }
            Assert.That(start-state.Remaining,Is.EqualTo(gained*.25).Within(1e-9));
        }
        [Test] public void UnrepresentableCostCannotCreateFreeRestoration()
        {
            algaeSettings.drinkCostPerPoint=float.Epsilon;state.Configure(algaeSettings);
            vitals.SetValue(TopDown3DSurvivalVital.Thirst,50);
            Assert.That(state.TryConsume(vitals,TopDown3DAlgaeUse.Drink,out _,out _),Is.False);
            Assert.That(state.Remaining,Is.EqualTo(100));Assert.That(vitals.Thirst,Is.EqualTo(50));
        }
    }
}
