using System.Linq;
using BooterBigArm.Editor;
using NUnit.Framework;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class GreaterWastelandTerrainOverviewTests
    {
        [Test]
        public void OutlineOmitsSharedEdgesAndKeepsConcaveBoundary()
        {
            var footprint = new[] { new Vector2Int(-2, -1), new Vector2Int(-1, -1), new Vector2Int(-2, 0) };
            var edges = GreaterWastelandTerrainOverview.FootprintEdges(footprint);
            Assert.That(edges.Count, Is.EqualTo(8));
            Assert.That(edges.Any(e => e.cell == footprint[0] && e.direction == Vector2Int.right), Is.False);
            Assert.That(edges.Any(e => e.cell == footprint[1] && e.direction == Vector2Int.up), Is.True);
            Assert.That(edges.Any(e => e.cell == footprint[2] && e.direction == Vector2Int.right), Is.True);
        }

        [Test]
        public void MissingTileRemainsAVisibleGapInFootprint()
        {
            var footprint = Enumerable.Range(0, 3).SelectMany(x => Enumerable.Range(0, 3)
                .Where(z => x != 1 || z != 1).Select(z => new Vector2Int(x, z))).ToArray();
            var edges = GreaterWastelandTerrainOverview.FootprintEdges(footprint);
            Assert.That(edges.Count, Is.EqualTo(16));
            Assert.That(edges.Count(e => e.cell + e.direction == Vector2Int.one), Is.EqualTo(4));
            Assert.That(GreaterWastelandTerrainOverview.FootprintEdges(footprint.Concat(footprint)).Count, Is.EqualTo(16));
        }
    }
}
