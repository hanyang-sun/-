using System;
using System.Collections.Generic;
using Brick2Life.Core;

namespace Brick2Life.Battle
{
    /// <summary>
    /// Four-way A* along physical-cell centerlines on the 20×20 battle grid, with soft obstacle costs.
    /// Multi-core levels prefer the cheapest route and, on ties,
    /// the core that comes first in row-major order. Adapted from the archived BrickDefense pathfinder.
    /// </summary>
    public static class RouteSearch
    {
        internal const int StepCost = 10;
        private const int AdjacentObstacleCost = 12;

        public static GridPoint[] Find(BattleCell[] cells, int width, int height, IReadOnlyList<string> coreIds, GridPoint start)
            => Find(cells, width, height, coreIds, start, out _);

        internal static GridPoint[] Find(BattleCell[] cells, int width, int height, IReadOnlyList<string> coreIds,
            GridPoint start, out int routeCost)
        {
            routeCost = int.MaxValue;
            if (cells == null) throw new ArgumentNullException(nameof(cells));
            if (coreIds == null) throw new ArgumentNullException(nameof(coreIds));
            if (start.X < 0 || start.Y < 0 || start.X >= width || start.Y >= height) return null;
            int count = width * height;
            var goals = new List<int>();
            var rank = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < coreIds.Count; i++) if (coreIds[i] != null) rank[coreIds[i]] = i;
            for (int index = 0; index < count; index++)
                if (cells[index].IsCoreCenter) goals.Add(index);
            if (goals.Count == 0) return null;

            var heuristic = new int[count];
            var cost = new int[count];
            var previous = new int[count];
            var closed = new bool[count];
            for (int index = 0; index < count; index++)
            {
                heuristic[index] = DistanceToNearest(cells[index].Position, cells, goals) * StepCost;
                cost[index] = int.MaxValue;
                previous[index] = -1;
            }

            int startIndex = start.Y * width + start.X;
            if (cells[startIndex].Blocked) return null;
            cost[startIndex] = 0;
            var open = new Heap();
            // The entry key is (f = cost + heuristic, cost, index); the first push must not swap cost and f.
            open.Push(heuristic[startIndex], 0, startIndex);
            int bestGoal = -1, bestCost = int.MaxValue;
            while (open.Count > 0)
            {
                open.Pop(out int score, out int currentCost, out int current);
                if (score > bestCost) break;
                if (closed[current] || currentCost != cost[current]) continue;
                closed[current] = true;
                if (cells[current].IsCoreCenter)
                {
                    if (bestGoal < 0 || currentCost < bestCost ||
                        (currentCost == bestCost && Rank(cells[current].SourceBrickId, rank) < Rank(cells[bestGoal].SourceBrickId, rank)))
                    {
                        bestGoal = current; bestCost = currentCost;
                    }
                    // Keep scanning equal-cost frontiers so every equally close core can be compared.
                    continue;
                }
                int x = current % width, y = current / width;
                if (y > 0) Visit(cells, width, height, current - width, current, cost, heuristic, previous, closed, open);
                if (x > 0) Visit(cells, width, height, current - 1, current, cost, heuristic, previous, closed, open);
                if (x < width - 1) Visit(cells, width, height, current + 1, current, cost, heuristic, previous, closed, open);
                if (y < height - 1) Visit(cells, width, height, current + width, current, cost, heuristic, previous, closed, open);
            }
            if (bestGoal < 0) return null;
            routeCost = bestCost;
            var route = new List<GridPoint>();
            for (int cursor = bestGoal; cursor >= 0; cursor = previous[cursor]) route.Add(cells[cursor].Position);
            route.Reverse();
            return route.ToArray();
        }

        private static int Rank(string id, Dictionary<string, int> rank) =>
            id != null && rank.TryGetValue(id, out int value) ? value : int.MaxValue;

        private static void Visit(BattleCell[] cells, int width, int height, int next, int current, int[] cost, int[] heuristic,
            int[] previous, bool[] closed, Heap open)
        {
            if (closed[next] || cells[next].Blocked) return;
            GridPoint from = cells[current].Position, to = cells[next].Position;
            // Odd/odd nodes are physical centers. Mixed parity nodes cross grid edges perpendicularly;
            // excluding even/even corners prevents paths from running along brick edges or cutting turns.
            if (to.X % 2 == 0 && to.Y % 2 == 0) return;
            bool legacyCornerStart = from.X % 2 == 0 && from.Y % 2 == 0;
            if (!legacyCornerStart && (from.Y == to.Y ? from.Y % 2 != 1 : from.X % 2 != 1)) return;
            int nextCost = cost[current] + StepCost + ClearancePenalty(cells, width, height, to);
            if (nextCost >= cost[next]) return;
            cost[next] = nextCost;
            previous[next] = current;
            open.Push(nextCost + heuristic[next], nextCost, next);
        }

        private static int ClearancePenalty(BattleCell[] cells, int width, int height, GridPoint point)
        {
            if (point.X % 2 != 1 || point.Y % 2 != 1) return 0;
            int penalty = 0, stride = BattleLevel.NavigationPerPhysical;
            if (point.X >= stride && cells[point.Y * width + point.X - stride].Blocked) penalty += AdjacentObstacleCost;
            if (point.X + stride < width && cells[point.Y * width + point.X + stride].Blocked) penalty += AdjacentObstacleCost;
            if (point.Y >= stride && cells[(point.Y - stride) * width + point.X].Blocked) penalty += AdjacentObstacleCost;
            if (point.Y + stride < height && cells[(point.Y + stride) * width + point.X].Blocked) penalty += AdjacentObstacleCost;
            return penalty;
        }

        private static int DistanceToNearest(GridPoint point, BattleCell[] cells, List<int> goals)
        {
            int nearest = int.MaxValue;
            foreach (int goal in goals)
            {
                GridPoint target = cells[goal].Position;
                int distance = Math.Abs(point.X - target.X) + Math.Abs(point.Y - target.Y);
                if (distance < nearest) nearest = distance;
            }
            return nearest;
        }

        // Small binary heap keyed by (score, cost, index); avoids a third-party dependency.
        private sealed class Heap
        {
            private readonly List<int[]> items = new List<int[]>();
            public int Count => items.Count;

            public void Push(int score, int cost, int index)
            {
                items.Add(new[] { score, cost, index });
                int child = items.Count - 1;
                while (child > 0)
                {
                    int parent = (child - 1) / 2;
                    if (Compare(items[child], items[parent]) >= 0) break;
                    (items[child], items[parent]) = (items[parent], items[child]);
                    child = parent;
                }
            }

            public void Pop(out int score, out int cost, out int index)
            {
                int[] top = items[0];
                score = top[0]; cost = top[1]; index = top[2];
                int last = items.Count - 1;
                items[0] = items[last];
                items.RemoveAt(last);
                int parent = 0;
                while (true)
                {
                    int left = parent * 2 + 1, right = left + 1, smallest = parent;
                    if (left < items.Count && Compare(items[left], items[smallest]) < 0) smallest = left;
                    if (right < items.Count && Compare(items[right], items[smallest]) < 0) smallest = right;
                    if (smallest == parent) break;
                    (items[parent], items[smallest]) = (items[smallest], items[parent]);
                    parent = smallest;
                }
            }

            private static int Compare(int[] a, int[] b)
            {
                if (a[0] != b[0]) return a[0].CompareTo(b[0]);
                if (a[1] != b[1]) return a[1].CompareTo(b[1]);
                return a[2].CompareTo(b[2]);
            }
        }
    }
}
