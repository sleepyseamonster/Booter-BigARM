using System.Collections.Generic;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEngine;

namespace BooterBigArm.Tests.Editor
{
    public sealed class TopDown3DLocalGridPathfinderTests
    {
        [Test]
        public void RoutesAroundBlockedGroundWithoutCuttingCorner()
        {
            var walkable = FilledGrid(7);
            var heights = new float[7, 7];
            walkable[3, 2] = false;
            walkable[3, 3] = false;
            walkable[3, 4] = false;
            var route = new List<Vector2Int>();

            Assert.That(TopDown3DLocalGridPathfinder.TryFindPath(walkable, heights,
                new Vector2Int(1, 3), new Vector2Int(5, 3), 0.5f, 0.5f, route), Is.True);
            Assert.That(route[route.Count - 1], Is.EqualTo(new Vector2Int(5, 3)));
            Assert.That(route.Exists(cell => cell.x == 3 && (cell.y <= 1 || cell.y >= 5)), Is.True);
        }

        [Test]
        public void RejectsUnloadedBarrierAndUnreachableSteepStep()
        {
            var walkable = FilledGrid(5);
            var heights = new float[5, 5];
            var route = new List<Vector2Int>();
            for (var z = 0; z < 5; z++)
                walkable[2, z] = false;

            Assert.That(TopDown3DLocalGridPathfinder.TryFindPath(walkable, heights,
                new Vector2Int(1, 2), new Vector2Int(4, 2), 0.5f, 0.5f, route), Is.False);
            Assert.That(route, Is.Empty);

            for (var z = 0; z < 5; z++)
            {
                walkable[2, z] = true;
                heights[2, z] = 2f;
                heights[3, z] = 2f;
                heights[4, z] = 2f;
            }
            Assert.That(TopDown3DLocalGridPathfinder.TryFindPath(walkable, heights,
                new Vector2Int(1, 2), new Vector2Int(4, 2), 0.5f, 0.5f, route), Is.False);
        }

        private static bool[,] FilledGrid(int size)
        {
            var grid = new bool[size, size];
            for (var x = 0; x < size; x++)
            for (var z = 0; z < size; z++)
                grid[x, z] = true;
            return grid;
        }
    }
}
