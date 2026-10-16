using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class AnimationPlaygroundJumpTimingTests
    {
        [TestCase(.0199999921f,4)] [TestCase(.02f,4)] [TestCase(.01666667f,5)] [TestCase(.01f,8)]
        public void ImpulseQuantizationDoesNotAddAFloatRoundingTick(float step,int ticks)
        {
            var marker=AnimationPlaygroundJumpTiming.ImpulseMarkerSeconds(step);
            Assert.That(marker,Is.EqualTo(ticks*(double)step).Within(.00000001));
            Assert.That(marker,Is.LessThanOrEqualTo(.10001));
        }
        [TestCase(.3333333f,.25f)] [TestCase(.278f,.229f)] [TestCase(.3333333f,.3f)]
        public void ContinuousSourceClockIsMonotonicAndSharesItsApexDerivative(float rise,float fall)
        {
            var value=new AnimationPlaygroundJumpVariant{liftTime=.08f,apexTime=.08f+rise,contactTime=.08f+rise+fall};
            var previous=value.liftTime;
            for(var i=0;i<=1000;i++)
            {
                var current=AnimationPlaygroundJumpTiming.FlightSourceSeconds(value,i/1000f);
                Assert.That(current,Is.GreaterThanOrEqualTo(previous-.000001f));previous=current;
            }
            Assert.That(AnimationPlaygroundJumpTiming.FlightSourceSeconds(value,0),Is.EqualTo(value.liftTime).Within(.000001f));
            Assert.That(AnimationPlaygroundJumpTiming.FlightSourceSeconds(value,1),Is.EqualTo(value.contactTime).Within(.000001f));
            var before=(value.apexTime-AnimationPlaygroundJumpTiming.FlightSourceSeconds(value,.4999f))/.0001f;
            var after=(AnimationPlaygroundJumpTiming.FlightSourceSeconds(value,.5001f)-value.apexTime)/.0001f;
            Assert.That(before,Is.EqualTo(after).Within(.002f));
        }
        [TestCase(0,4.6f)] [TestCase(.65f,0)] [TestCase(.3f,3)] [TestCase(2,9)]
        public void ForecastUsesThePhysicalVerticalEquation(float gap,float speed)
        {
            var seconds=AnimationPlaygroundJumpTiming.TimeToContact(gap,speed,9.81f);
            Assert.That(seconds,Is.GreaterThanOrEqualTo(0));
            Assert.That(speed*seconds+4.905f*seconds*seconds,Is.EqualTo(gap).Within(.00001f));
        }
    }
}
