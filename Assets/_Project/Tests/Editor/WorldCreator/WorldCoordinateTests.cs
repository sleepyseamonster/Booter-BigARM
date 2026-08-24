using BooterBigArm.TopDown3D.WorldCreator;
using NUnit.Framework;

namespace BooterBigArm.Tests.WorldCreator
{
    public sealed class WorldCoordinateTests
    {
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
