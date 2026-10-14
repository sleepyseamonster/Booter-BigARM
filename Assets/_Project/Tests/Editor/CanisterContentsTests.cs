using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class CanisterContentsTests
    {
        [Test] public void ExplicitVersionDistinguishesAbsentFromEmptyAcrossJsonRoundTrip()
        {
            var absent = JsonUtility.FromJson<TopDown3DCanisterContents>("{}");
            Assert.That(absent.IsPresent, Is.False);
            Assert.That(absent.IsValid(), Is.False);
            var empty = JsonUtility.FromJson<TopDown3DCanisterContents>(JsonUtility.ToJson(TopDown3DCanisterContents.Create()));
            Assert.That(empty.IsPresent && empty.IsValid(), Is.True);
        }
        [TestCase(-1, 0, false)] [TestCase(101, 0, false)] [TestCase(100, 3, false)]
        [TestCase(13, 3, true)] [TestCase(13, 10, false)]
        public void ValidationRejectsImpossibleFillAndRemainder(int amount, double seconds, bool expected)
        { Assert.That(TopDown3DCanisterContents.Create(amount, seconds).IsValid(), Is.EqualTo(expected)); }
    }
}
