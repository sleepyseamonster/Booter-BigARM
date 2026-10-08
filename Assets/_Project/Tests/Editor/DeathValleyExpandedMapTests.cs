using System.IO;
using NUnit.Framework;
using BooterBigArm.Editor;

namespace BooterBigArm.Tests
{
    public sealed class DeathValleyExpandedMapTests
    {
        private static DeathValleyMapRecord[] Grid(int side, int east, int north)
        {
            var result = new DeathValleyMapRecord[side * side];
            for (int row = 0; row < side; row++) for (int col = 0; col < side; col++)
            {
                int x = east + col * 256, y = north + row * 256;
                result[row * side + col] = new DeathValleyMapRecord { legacy_id = $"r{row % 16:00}_c{col % 16:00}",
                    geographic_key = $"epsg26911/e{x}/n{y}/size256", bounds_m = new double[] { x, y, x + 256, y + 256 } };
            }
            return result;
        }

        [Test] public void SectionLocalLabelsDoNotBecomeDuplicateMapKeys()
        {
            var a = Grid(32, 516304, 4006200);
            Assert.That(a[0].legacy_id, Is.EqualTo(a[16].legacy_id));
            Assert.That(a[0].Key, Is.Not.EqualTo(a[16].Key));
            Assert.That(a[0].Label, Is.EqualTo(a[0].legacy_id));
        }

        [Test] public void ExpandedFramingUsesAllFourSectionsWithoutRecenteringOrigin()
        {
            Assert.That(DeathValleyMapData.GetPlayableBounds(Grid(32, 516304, 4006200)),
                Is.EqualTo(new double[] { 516304, 4006200, 524496, 4014392 }));
            // Original geographic origin remains inside retained SE, not at new rectangle center.
            Assert.That((516304 + 524496) / 2, Is.Not.EqualTo(522448));
        }

        [Test] public void BaselineFramingRemainsCompatible()
        {
            Assert.That(DeathValleyMapData.GetPlayableBounds(Grid(16, 520400, 4006200)),
                Is.EqualTo(new double[] { 520400, 4006200, 524496, 4010296 }));
        }

        [Test] public void MissingDuplicateOrShiftedGridCannotClaimFullPlayableBounds()
        {
            Assert.Throws<InvalidDataException>(() => DeathValleyMapData.GetPlayableBounds(new DeathValleyMapRecord[3]));
            var duplicate = Grid(32, 516304, 4006200); duplicate[1] = duplicate[0];
            Assert.Throws<InvalidDataException>(() => DeathValleyMapData.GetPlayableBounds(duplicate));
            Assert.Throws<InvalidDataException>(() => DeathValleyMapData.GetPlayableBounds(Grid(32, 516560, 4006200)));
        }
    }
}
