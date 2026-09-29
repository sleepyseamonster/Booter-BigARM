using BooterBigArm.TopDown3D.WorldCreator;
using NUnit.Framework;

namespace BooterBigArm.Tests.WorldCreator
{
    public sealed class WorldCoordinateTests
    {
        [Test]
        public void TechnicalCartesianAddressParsesExactValuesAndRejectsExtraFields()
        {
            var model = new NonCanonTechnicalCoordinateModel();
            var positions = new[]
            {
                new AbsoluteWorldPosition(0d, 0d, 0d),
                new AbsoluteWorldPosition(-123.125d, 42.5d, 9876.25d),
                new AbsoluteWorldPosition(9_007_199_254_740_991d, -731.25d, -8_765_432_109_876d)
            };
            foreach (var position in positions)
            {
                Assert.That(model.TryResolve(model.Encode(position), out var restored), Is.True);
                Assert.That(restored, Is.EqualTo(position));
            }

            foreach (var invalid in new[] { "1|2", "1|2|3|4", "x|2|3", "|2|3", "1||3" })
            {
                var address = new WorldCoordinateAddress(model.ModelId, model.ModelVersion, invalid);
                Assert.That(model.TryResolve(address, out _), Is.False, invalid);
            }
        }

        [Test]
        public void OpaqueAddress_RoundTripsFarBeyondUnityFloatPrecision()
        {
            var model = new NonCanonCoordinateModel();
            var absolute = new AbsoluteWorldPosition(
                9_000_000_000_000.25d,
                -123.5d,
                -9_000_000_000_000.75d);

            var address = model.Encode(absolute);

            Assert.That(model.TryResolve(address, out var resolved), Is.True);
            Assert.That(resolved, Is.EqualTo(absolute));
            Assert.That(address.CanonicalValue, Is.Not.Empty);
        }

        [Test]
        public void LocalOriginRebase_ChangesOnlyTemporaryLocalPosition()
        {
            var model = new NonCanonCoordinateModel();
            var world = WorldCreatorTestFactory.CreateWorld();
            var seedNamespace = new WorldSeedNamespace(WorldVersionDomain.Landform, "landform.canyon-system");
            var absolute = new AbsoluteWorldPosition(9_000_000_000_128.25d, 44.5d, -8_999_999_999_872.75d);
            var address = model.Encode(absolute);
            var firstOriginPosition = new AbsoluteWorldPosition(9_000_000_000_000d, 40d, -9_000_000_000_000d);
            var secondOriginPosition = new AbsoluteWorldPosition(9_000_000_000_100d, 42d, -8_999_999_999_900d);
            var firstFrame = new LocalOriginFrame(model.Encode(firstOriginPosition), firstOriginPosition, 4096d);
            var secondFrame = new LocalOriginFrame(model.Encode(secondOriginPosition), secondOriginPosition, 4096d);
            var identityBefore = WorldFeatureId.Create(world, seedNamespace, address, "spine:0");

            var firstLocal = firstFrame.ToLocal(absolute);
            var secondLocal = secondFrame.ToLocal(absolute);
            var identityAfter = WorldFeatureId.Create(world, seedNamespace, address, "spine:0");

            Assert.That(firstLocal, Is.Not.EqualTo(secondLocal));
            Assert.That(firstFrame.ToAbsolute(firstLocal), Is.EqualTo(absolute));
            Assert.That(secondFrame.ToAbsolute(secondLocal), Is.EqualTo(absolute));
            Assert.That(identityAfter, Is.EqualTo(identityBefore));
            Assert.That(model.Encode(firstFrame.ToAbsolute(firstLocal)), Is.EqualTo(address));
            Assert.That(model.Encode(secondFrame.ToAbsolute(secondLocal)), Is.EqualTo(address));
        }

        [Test]
        public void LocalOriginFrame_RejectsPositionsOutsideDeclaredRange()
        {
            var model = new NonCanonCoordinateModel();
            var origin = new AbsoluteWorldPosition(1000d, 0d, -1000d);
            var frame = new LocalOriginFrame(model.Encode(origin), origin, 512d);

            Assert.That(frame.TryToLocal(new AbsoluteWorldPosition(1512d, 0d, -1000d), out _), Is.True);
            Assert.That(frame.TryToLocal(new AbsoluteWorldPosition(1512.01d, 0d, -1000d), out _), Is.False);
            Assert.That(
                () => frame.ToAbsolute(new LocalWorldPosition(512.01f, 0f, 0f)),
                Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        [Test]
        public void CoordinateModel_RejectsAnotherModelsAddress()
        {
            var first = new NonCanonCoordinateModel("test.non-canon.first");
            var second = new NonCanonCoordinateModel("test.non-canon.second");
            var address = first.Encode(new AbsoluteWorldPosition(1d, 2d, 3d));

            Assert.That(second.TryResolve(address, out _), Is.False);
            Assert.That(address.IsCompatibleWith(first), Is.True);
            Assert.That(address.IsCompatibleWith(second), Is.False);
        }
    }
}
