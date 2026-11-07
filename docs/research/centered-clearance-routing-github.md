# 空格中心与积木间距寻路

日期：2026-11-06。范围：用户希望敌人尽量不沿积木边缘移动，走空格。沿用上一轮已批准的局部脱离与全局堵路回退规则，只调整有效地图中的路径选择。

## 编码前的检索与比较

检索范围：GitHub 上已有的障碍距离代价、导航区域通行代价。关键词：`navigation2 inflation_layer costmap`、`recastnavigation Detour query filter area cost`。实际通过 GitHub 接口阅读以下 README、许可证和源文件，非只阅读摘要。

| 项目 | 阅读与版本（文件 SHA） | 比较与有限借鉴 |
| --- | --- | --- |
| [Nav2](https://github.com/ros-navigation/navigation2) | `README.md`、`LICENSE`、`nav2_costmap_2d/package.xml`、[`inflation_layer.cpp`](https://github.com/ros-navigation/navigation2/blob/main/nav2_costmap_2d/plugins/inflation_layer.cpp)，文件 SHA `b82501ffd05166821ea938dff29fdc3f78d5912e` | 仓库多许可证，已检查目标文件头为 Apache-2.0，包声明 BSD-3-Clause / Apache-2.0。采用“障碍附近增加代价，但可通行与偏好分别处理”的思路。ROS2/C++ 的距离场、线程和插件依赖过重，不引入。 |
| [Recast Navigation](https://github.com/recastnavigation/recastnavigation) | `README.md`、`License.txt`、[`DetourNavMeshQuery.cpp`](https://github.com/recastnavigation/recastnavigation/blob/main/Detour/Source/DetourNavMeshQuery.cpp)，文件 SHA `7edbe3b1dc849c40c4478e51cb3695405247c5d8` | zlib 许可证；`dtQueryFilter::getCost` 将移动距离和区域代价结合，搜索同时使用通行过滤与代价。采用机制，不引入 C++ 网格生成或本地插件。虽支持 Unity 等引擎，但本项目固定格盘无需重建 NavMesh。 |

两者都支持离线计算，但不能直接复用为现有纯 C# 接口。只作机制参考，不复制、改写外部代码，不新增第三方依赖或素材。继续使用本项目自己的四邻接 A*、堆、路径跟随和双窗口投影。

## 适配方案与验收

- 保留 20×20 导航坐标、每半格一个节点和现有速度单位。入口与核心仍位于 10×10 大格中心。
- 正常路线只沿相邻空格中心之间的线段移动：奇数/奇数节点为大格中心，混合奇偶节点只能垂直穿越格边，禁止经偶数/偶数的格角沿边拐弯。连续坐标重寻路可从既有非中心位置接回，不瞬移。
- 移动基础代价 10；进入大格中心时，每个四邻接墙/塔增加 12。只影响选路，不将相邻空格标为障碍，保留一格窄路；核心不计为墙。
- 曼哈顿启发式乘以基础代价，仍为可采纳且一致的下界。等代价核心按既有稳定顺序选择。
- 入口建图、活敌人重寻路、临时脱离后的连接统一采用此路径规则，投影消费同一路径，不只修改显示偏移。
- 回归：积木旁路线的中心与安全间距、宽路偏好、一格通道、真正无路、多个入口/核心、动态重寻路和无瞬移，以及独立物理格 Dijkstra 对照。
- 本轮只验证 Unity 仿真，不声称真实设备效果。

## 验证结果

- 修改前运行 `dotnet run --project Tests/Brick2Life.Checks.csproj --configuration Release --no-restore`，新增回归在单块积木案例失败：`Route follows brick edges instead of empty physical-cell centers.`；原 1105 项仍通过。
- 修改后同一命令全量 1178 项通过，新增 73 项包含物理格 Dijkstra 对照、一格窄路、真正封路、长墙绕行、路径线段与积木边缘至少半格距离。
- Unity `LiveProjectionRefreshChecks` 在 6000.3.24f1 隔离场景中 146 项通过：自动 RGB 更新墙塔、连续出怪的局部脱离、暂停与波次/胜利后更新、实际投影路径面片与中心路线坐标一致。没有修改原有投影外观。
- 主改动为 `RouteSearch.cs`；`BattleLevel.FindRoute` 的内部重载返回路径代价，让 `BattleSimulation.RemainingRoute` 在连续位置选择连接端点时沿用同一权重。用户此前的 0.25 秒 / 0.01 秒 / 一帧采样参数保持。
