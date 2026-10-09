using System;
using System.Collections.Generic;

namespace BrickDefense.Core
{
    public static class GridMapper
    {
        public static GridResult Map(ScanResult scan)
        {
            var issues = new List<Issue>();
            if (scan == null)
            {
                issues.Add(new Issue("SCAN_MISSING", "没有扫描结果。"));
                return Rejected(null, issues);
            }

            if (scan.Status != ResultStatus.Success)
            {
                issues.AddRange(scan.Issues);
                if (issues.Count == 0)
                    issues.Add(new Issue("SCAN_REJECTED", "扫描未通过。"));
                return Rejected(scan.ScanId, issues);
            }

            if (string.IsNullOrWhiteSpace(scan.ScanId) || scan.PhysicalCells == null ||
                scan.PhysicalCells.Length != BoardSize.PhysicalCellCount || scan.Bricks == null)
            {
                issues.Add(new Issue("INVALID_SCAN", "成功扫描必须包含扫描编号、100 个实体格和积木列表。"));
                return Rejected(scan.ScanId, issues);
            }

            var seen = new bool[BoardSize.PhysicalCellCount];
            var occupiedById = new Dictionary<string, PhysicalCellObservation>();
            foreach (PhysicalCellObservation cell in scan.PhysicalCells)
            {
                if (cell == null || !InsidePhysical(cell.C, cell.R))
                {
                    issues.Add(new Issue("INVALID_CELL", "实体格坐标无效。"));
                    continue;
                }

                int index = cell.R * BoardSize.Physical + cell.C;
                if (seen[index])
                    issues.Add(new Issue("DUPLICATE_CELL", $"实体格 ({cell.C},{cell.R}) 重复。"));
                seen[index] = true;

                if (cell.State == PhysicalCellState.Uncertain)
                    issues.Add(new Issue("UNCERTAIN_CELL", $"实体格 ({cell.C},{cell.R}) 不确定。"));
                else if (cell.State == PhysicalCellState.Empty && !string.IsNullOrEmpty(cell.BrickId))
                    issues.Add(new Issue("INVALID_CELL", $"空格 ({cell.C},{cell.R}) 有积木编号。"));
                else if (cell.State == PhysicalCellState.Occupied)
                {
                    if (string.IsNullOrWhiteSpace(cell.BrickId))
                        issues.Add(new Issue("INVALID_CELL", $"占格 ({cell.C},{cell.R}) 缺少积木编号。"));
                    else if (occupiedById.ContainsKey(cell.BrickId))
                        issues.Add(new Issue("DUPLICATE_BRICK_ID", $"积木编号 {cell.BrickId} 对应多个格位。"));
                    else
                        occupiedById.Add(cell.BrickId, cell);
                }
                else if (cell.State != PhysicalCellState.Empty)
                    issues.Add(new Issue("INVALID_CELL", $"实体格 ({cell.C},{cell.R}) 状态无效。"));
            }

            for (int i = 0; i < seen.Length; i++)
                if (!seen[i])
                    issues.Add(new Issue("MISSING_CELL",
                        $"实体格 ({i % BoardSize.Physical},{i / BoardSize.Physical}) 缺失。"));

            var brickIds = new HashSet<string>();
            foreach (BrickObservation brick in scan.Bricks)
            {
                if (brick == null || string.IsNullOrWhiteSpace(brick.BrickId) ||
                    string.IsNullOrWhiteSpace(brick.ColorId) || !InsidePhysical(brick.C, brick.R))
                {
                    issues.Add(new Issue("INVALID_BRICK", "积木记录缺少编号、颜色或有效格位。"));
                    continue;
                }

                if (!brickIds.Add(brick.BrickId))
                    issues.Add(new Issue("DUPLICATE_BRICK_ID", $"积木编号 {brick.BrickId} 重复。"));
                if (!occupiedById.TryGetValue(brick.BrickId, out PhysicalCellObservation cell) ||
                    cell.C != brick.C || cell.R != brick.R)
                    issues.Add(new Issue("BRICK_CELL_MISMATCH", $"积木 {brick.BrickId} 与实体格不一致。"));
            }

            if (brickIds.Count != occupiedById.Count)
                issues.Add(new Issue("BRICK_COUNT_MISMATCH", "占格数量与积木记录数量不一致。"));
            if (issues.Count > 0)
                return Rejected(scan.ScanId, issues);

            var placements = new GridPlacement[scan.Bricks.Length];
            var navCells = new GridCell[BoardSize.NavigationCellCount];
            for (int y = 0; y < BoardSize.Navigation; y++)
                for (int x = 0; x < BoardSize.Navigation; x++)
                    navCells[y * BoardSize.Navigation + x] = new GridCell(new GridPoint(x, y), null);

            for (int i = 0; i < scan.Bricks.Length; i++)
            {
                BrickObservation brick = scan.Bricks[i];
                placements[i] = new GridPlacement(brick);
                for (int dy = 0; dy < BoardSize.NavigationPerPhysical; dy++)
                    for (int dx = 0; dx < BoardSize.NavigationPerPhysical; dx++)
                    {
                        int x = BoardSize.NavigationPerPhysical * brick.C + dx;
                        int y = BoardSize.NavigationPerPhysical * brick.R + dy;
                        navCells[y * BoardSize.Navigation + x] =
                            new GridCell(new GridPoint(x, y), brick.BrickId);
                    }
            }

            return new GridResult(scan.ScanId, ResultStatus.Success, placements, navCells,
                Array.Empty<Issue>());
        }

        private static GridResult Rejected(string scanId, List<Issue> issues)
        {
            return new GridResult(scanId, ResultStatus.Rejected, Array.Empty<GridPlacement>(),
                Array.Empty<GridCell>(), issues.ToArray());
        }

        private static bool InsidePhysical(int c, int r)
        {
            return c >= 0 && c < BoardSize.Physical && r >= 0 && r < BoardSize.Physical;
        }
    }

    public static class LevelBuilder
    {
        public static LevelResult Build(GridResult grid, LevelRules rules, GridPoint spawn)
        {
            var issues = new List<Issue>();
            if (grid == null || grid.Status != ResultStatus.Success ||
                string.IsNullOrWhiteSpace(grid.SourceScanId) || grid.Placements == null ||
                grid.NavCells == null || grid.NavCells.Length != BoardSize.NavigationCellCount)
            {
                issues.Add(new Issue("INVALID_GRID", "游戏格映射未通过。"));
                return Rejected(spawn, Array.Empty<LevelEntity>(), Array.Empty<LevelCell>(),
                    Array.Empty<string>(), issues);
            }

            if (rules?.ColorRoleMap == null || rules.ColorRoleMap.Count != 3)
            {
                issues.Add(new Issue("INVALID_RULES", "必须配置三种颜色与核心、墙、塔的一一映射。"));
                return Rejected(spawn, Array.Empty<LevelEntity>(), Array.Empty<LevelCell>(),
                    Array.Empty<string>(), issues);
            }

            var configuredRoles = new HashSet<BrickRole>(rules.ColorRoleMap.Values);
            if (configuredRoles.Count != 3 || !configuredRoles.Contains(BrickRole.Core) ||
                !configuredRoles.Contains(BrickRole.Wall) || !configuredRoles.Contains(BrickRole.Tower))
            {
                issues.Add(new Issue("INVALID_RULES", "三种颜色必须对应不同角色。"));
                return Rejected(spawn, Array.Empty<LevelEntity>(), Array.Empty<LevelCell>(),
                    Array.Empty<string>(), issues);
            }

            var entities = new List<LevelEntity>();
            var rolesById = new Dictionary<string, BrickRole>();
            var seenBrickIds = new HashSet<string>();
            var expectedSources = new string[BoardSize.NavigationCellCount];
            foreach (GridPlacement placement in grid.Placements)
            {
                if (placement == null || string.IsNullOrWhiteSpace(placement.BrickId) ||
                    string.IsNullOrWhiteSpace(placement.ColorId) || placement.C < 0 ||
                    placement.C >= BoardSize.Physical || placement.R < 0 || placement.R >= BoardSize.Physical)
                {
                    issues.Add(new Issue("INVALID_GRID", "游戏格中的积木记录无效。"));
                    continue;
                }
                if (!seenBrickIds.Add(placement.BrickId))
                {
                    issues.Add(new Issue("INVALID_GRID", $"积木 {placement.BrickId} 重复。"));
                    continue;
                }
                for (int dy = 0; dy < BoardSize.NavigationPerPhysical; dy++)
                    for (int dx = 0; dx < BoardSize.NavigationPerPhysical; dx++)
                    {
                        int x = BoardSize.NavigationPerPhysical * placement.C + dx;
                        int y = BoardSize.NavigationPerPhysical * placement.R + dy;
                        int index = y * BoardSize.Navigation + x;
                        if (expectedSources[index] != null)
                            issues.Add(new Issue("INVALID_GRID", $"实体格 ({placement.C},{placement.R}) 被重复占用。"));
                        expectedSources[index] = placement.BrickId;
                    }
                if (!rules.ColorRoleMap.TryGetValue(placement.ColorId, out BrickRole role))
                {
                    issues.Add(new Issue("UNMAPPED_COLOR", $"积木 {placement.BrickId} 的颜色未映射。"));
                    continue;
                }
                rolesById.Add(placement.BrickId, role);
                entities.Add(new LevelEntity(placement, role));
            }

            var cells = new LevelCell[BoardSize.NavigationCellCount];
            for (int i = 0; i < cells.Length; i++)
            {
                GridCell source = grid.NavCells[i];
                if (source == null || source.Position.X != i % BoardSize.Navigation ||
                    source.Position.Y != i / BoardSize.Navigation)
                {
                    issues.Add(new Issue("INVALID_GRID", "游戏格位置不完整或顺序错误。"));
                    continue;
                }
                if (!string.Equals(source.SourceBrickId, expectedSources[i], StringComparison.Ordinal))
                    issues.Add(new Issue("INVALID_GRID",
                        $"游戏格 {source.Position} 的积木来源与实体格的 2×2 映射不一致。"));
                BrickRole? role = null;
                if (!string.IsNullOrEmpty(source.SourceBrickId))
                {
                    if (rolesById.TryGetValue(source.SourceBrickId, out BrickRole mappedRole))
                        role = mappedRole;
                }
                cells[i] = new LevelCell(source.Position, source.SourceBrickId, role);
            }

            var cores = new List<LevelEntity>();
            foreach (LevelEntity entity in entities)
                if (entity.Role == BrickRole.Core)
                    cores.Add(entity);
            cores.Sort((a, b) =>
            {
                int byRow = a.R.CompareTo(b.R);
                return byRow != 0 ? byRow : a.C.CompareTo(b.C);
            });
            var coreIds = new string[cores.Count];
            for (int i = 0; i < cores.Count; i++)
                coreIds[i] = cores[i].BrickId;

            if (cores.Count == 0)
                issues.Add(new Issue("NO_CORE", "关卡至少需要一块核心。"));
            if (!InsideNavigation(spawn))
                issues.Add(new Issue("SPAWN_OUT_OF_BOUNDS", "出生点不在 20×20 游戏格内。"));
            else if (cells[spawn.Y * BoardSize.Navigation + spawn.X] != null &&
                     !string.IsNullOrEmpty(cells[spawn.Y * BoardSize.Navigation + spawn.X].SourceBrickId))
                issues.Add(new Issue("SPAWN_OCCUPIED", "出生点必须是空白游戏格。"));

            if (issues.Count > 0)
                return Rejected(spawn, entities.ToArray(), cells, coreIds, issues);

            NavigationPath path = GridPathfinder.Find(cells, coreIds, spawn);
            if (path == null)
            {
                issues.Add(new Issue("NO_ROUTE", "出生点没有到任何核心的可达路线。"));
                return Rejected(spawn, entities.ToArray(), cells, coreIds, issues);
            }

            return new LevelResult(ResultStatus.Success, entities.ToArray(), cells, spawn,
                coreIds, path.TargetBrickId, path.Route, Array.Empty<Issue>());
        }

        private static LevelResult Rejected(GridPoint spawn, LevelEntity[] entities, LevelCell[] cells,
            string[] coreIds, List<Issue> issues)
        {
            return new LevelResult(ResultStatus.Rejected, entities, cells, spawn, coreIds, null,
                Array.Empty<GridPoint>(), issues.ToArray());
        }

        private static bool InsideNavigation(GridPoint point)
        {
            return point.X >= 0 && point.X < BoardSize.Navigation &&
                point.Y >= 0 && point.Y < BoardSize.Navigation;
        }

    }
}
