using BooterBigArm.Editor.WorldCreator;
using NUnit.Framework;

namespace BooterBigArm.Tests.WorldCreator
{
    public sealed class WorldCreatorProductionPathTests
    {
        [Test]
        public void ProductionScene_UsesOnlyTheTopologyV2WorldCreatorTerrainPath()
        {
            var errors = WorldCreatorProductionPathValidator.CollectErrors();
            Assert.That(errors, Is.Empty, string.Join("\n", errors));
        }
    }
}
