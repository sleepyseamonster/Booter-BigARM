using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    // A bounded, deterministic search over already-probed ground. The caller owns world
    // sampling and must treat missing chunks and occupied cells as impassable.
    public static class TopDown3DLocalGridPathfinder
    {
        public static bool TryFindPath(
            bool[,] traversable,
            float[,] height,
            Vector2Int start,
            Vector2Int goal,
            float maxStepUp,
            float maxStepDown,
            List<Vector2Int> path)
        {
            path.Clear();
            var width = traversable.GetLength(0);
            var depth = traversable.GetLength(1);
            if (width == 0 || depth == 0 || height.GetLength(0) != width
                || height.GetLength(1) != depth
                || start.x < 0 || start.y < 0 || start.x >= width || start.y >= depth
                || goal.x < 0 || goal.y < 0 || goal.x >= width || goal.y >= depth
                || !traversable[start.x, start.y])
            {
                return false;
            }

            var cost = new float[width, depth];
            var closed = new bool[width, depth];
            var parent = new Vector2Int[width, depth];
            for (var x = 0; x < width; x++)
            for (var y = 0; y < depth; y++)
                cost[x, y] = float.PositiveInfinity;
            cost[start.x, start.y] = 0f;

            for (var expanded = 0; expanded < width * depth; expanded++)
            {
                var best = float.PositiveInfinity;
                var current = new Vector2Int(-1, -1);
                for (var x = 0; x < width; x++)
                for (var y = 0; y < depth; y++)
                {
                    if (closed[x, y] || cost[x, y] >= best)
                        continue;
                    best = cost[x, y];
                    current = new Vector2Int(x, y);
                }

                if (current.x < 0)
                    break;
                closed[current.x, current.y] = true;
                for (var dx = -1; dx <= 1; dx++)
                for (var dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0)
                        continue;
                    var next = current + new Vector2Int(dx, dy);
                    if (!CanCross(current, next, traversable, height, maxStepUp, maxStepDown))
                        continue;
                    // A diagonal cannot squeeze between two blocked orthogonal cells.
                    if (dx != 0 && dy != 0
                        && (!CanCross(current, current + new Vector2Int(dx, 0), traversable, height, maxStepUp, maxStepDown)
                            || !CanCross(current, current + new Vector2Int(0, dy), traversable, height, maxStepUp, maxStepDown)))
                        continue;
                    var newCost = cost[current.x, current.y] + (dx != 0 && dy != 0 ? 1.414214f : 1f)
                        + Mathf.Abs(height[next.x, next.y] - height[current.x, current.y]) * 0.3f;
                    if (newCost >= cost[next.x, next.y])
                        continue;
                    cost[next.x, next.y] = newCost;
                    parent[next.x, next.y] = current;
                }
            }

            // The destination can be beyond the local window or unwalkable. Reach the
            // closest valid cell only if it makes genuine progress toward the target.
            var endpoint = start;
            var remaining = Vector2Int.Distance(start, goal);
            for (var x = 0; x < width; x++)
            for (var y = 0; y < depth; y++)
            {
                if (float.IsInfinity(cost[x, y]))
                    continue;
                var candidate = new Vector2Int(x, y);
                var distance = Vector2Int.Distance(candidate, goal);
                if (distance >= remaining - 0.001f)
                    continue;
                remaining = distance;
                endpoint = candidate;
            }

            if (endpoint == start)
                return false;
            for (var node = endpoint; node != start; node = parent[node.x, node.y])
                path.Add(node);
            path.Reverse();
            return true;
        }

        private static bool CanCross(Vector2Int from, Vector2Int to, bool[,] traversable,
            float[,] height, float maxStepUp, float maxStepDown)
        {
            if (to.x < 0 || to.y < 0 || to.x >= traversable.GetLength(0)
                || to.y >= traversable.GetLength(1) || !traversable[to.x, to.y])
                return false;
            var rise = height[to.x, to.y] - height[from.x, from.y];
            return rise <= maxStepUp && rise >= -maxStepDown;
        }
    }
}
