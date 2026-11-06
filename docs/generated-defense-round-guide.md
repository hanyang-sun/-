# 系统目标与实时建造玩法

更新：2026-11-07。适用 Unity 6000.3.24f1、默认 `Brick2LifeSimulation` 场景、编辑器双窗口。

## 当前规则与操作

1. 进入 Play，系统随机生成 **一个核心、1–4 个敌人出生点**。核心位于内侧，出生点位于棋盘边缘，互不重合；数量可以是一，也可以是多个。场景的 `minimumSpawns/maximumSpawns` 可调整范围。
2. 原始布局窗口显示蓝色核心标记、金色入口圈和编号。它们是固定系统目标，不能被玩家删除、拖动，也不能在对应格放置积木。展示窗口显示同一组目标。
3. 玩家摆放绿色城墙、红色防御塔；暂不设数量配额，仍受 10×10 棋盘、一格一块和目标保留格限制。清空只删除玩家积木。
4. 点击“开始”，自动识别开启时等待新 RGB 的稳定确认后开战。战斗、暂停和波次间隔中都能增删、移动墙塔；必须点击原始窗口取得编辑焦点，展示窗口保持只读。
5. B 确认新布局后重新计算入口路线和存活敌人从当前位置出发的余程。所有入口仍可通行时，新敌人立即使用新路线；被新积木压住或暂时无法连接新路线的旧敌人保留各自原路线，先脱离后自动重寻路。提示“道路已更新，…个敌人正脱离新放置积木”，不再因个别敌人被压住而显示全局堵路。脱离时不瞬移、不重置生命和本局进度。
6. 任一入口确实无法到达核心时，仍提示“沿用上次有效路线（可能穿过新放置的积木）”，**战斗继续**，保留完整旧入口路线，新墙不撤销，塔的增删仍生效。恢复通路并确认后重新规划，警告消失。局部脱离的旧敌人在进入其他导航格时尝试连接新路线，无需新的 RGB 布局版本，也不再等待每 0.25 秒的整图重试；全部脱离或被消灭后提示清除。手动暂停独立保留，修好路不会自动解除暂停。只有结构错误（例如识别结果占据系统保留格）进入等待修正；普通堵路不进入等待。
7. 一波结束后点击“下一波”；守住全部波次胜利，核心生命为 0 失败。“重试”保留核心、入口和玩家布局，重置战斗；“生成新关卡（清空积木）”重新生成目标并清空玩家积木，仅在未战斗且未等待开战或继续确认时可用。

准备阶段已经堵路时，首次回退使用本局生成时的无玩家障碍参考路线；之后保留最近一次全部可行的路线组。多个入口整组更新，敌人持续按原入口索引轮转，不用候选路线的部分成功结果替换入口编号。

正常路线沿 10×10 空格中心连接，在中心转弯，经过格边时垂直穿过，不沿积木边缘行进。保留 20×20 半格节点与移动速度单位；每半格基础寻路代价 10，进入大格中心时每个四邻接墙/塔增加代价 12，优先选择更宽的通道，允许为此适当绕行。该附加代价只影响选路，不把邻墙空格改成障碍，一格宽的通道仍可通行。投影道路与敌人共用同一结果。被新积木压住的旧敌人脱离、真正全局堵路回退时，仍可短暂穿过新积木。

## 识别边界与显示

系统目标是公开关卡配置，和玩家积木预测分开：B 仍只接收 RGB 与公开标定配置，不读 JSON 中的占用、颜色或 ID。`LevelState` 在应用层合入固定系统核心，但核心所在空格的 `Occupied=false`，不会伪造蓝色积木观测。无障碍参考关卡只用于初始导航，不提交给 `BoardSyncSession` 或识别评分。

原始目标提示位于 `Brick2LifeObjectives`（29）层，无 Collider；游戏对象位于 30 层，顶置相机同时排除这两层。31 层现在是投影图像内容源，由专用相机汇成纹理，再通过固定虚拟投影仪照到棋盘、积木表面，按颜色反射并计算遮挡；不再直接显示图案面片。默认连续识别直接采集带投影 RGB，采样不关闭投影；F8/顶部按钮切换到暂停识别后，按暂停才持续关投影并采集，继续/开战前完成新确认再恢复。排除 31 层无法移除表面反射光，不能用它代替消隐。搭建视图仍无投影；展示窗口模式不改变识别条件。详见 [固定投影仪仿真](physical-projector-guide.md)。人工目标接口仅供脚本或测试调用，F5/F6/F7 快捷键代码已移除。

识别采用自适应节奏：稳定布局每 0.8 秒检查一次；首次识别、开战需要新图像、RGB 出现可用的待确认布局时，每 0.12 秒补足三帧稳定确认，完成后立即恢复低频。快速期最多一秒，不因候选不断变化而无限延长；未知、拒绝和相机异常用低频恢复，新的可用候选可再次触发快速确认。调度只读取 RGB 确认状态，不读取编辑动作、GT 或评价分数。

周期性采样消隐及最短时间/帧数参数已删除。暂停模式的关闭时长由用户暂停和新确认等待决定，不是固定的短消隐。历史频闪数据只保留在 [研究记录](research/adaptive-projection-sampling-github.md)。

## 具体文件与调用链

| 文件 | 本轮职责 |
| --- | --- |
| `Assets/Scripts/Application/GeneratedRound.cs` | 固定目标、确定性随机生成、保留格、初始导航参考 |
| `Assets/Scripts/Application/LevelSemantics.cs`、`LevelFeedbackSession.cs` | 确认积木与系统目标合入关卡，检查保留格冲突 |
| `Assets/Scripts/Feedback/SimulationBattleController.cs` | 生成/重试、固定目标、确认后更新、最近有效导航 |
| `Assets/Scripts/Battle/BattleLevel.cs`、`BattleSimulation.cs` | 20×20 寻路，完整入口集合，路径回退与塔更新 |
| `Assets/Scripts/Simulation/SimulationBoardSession.cs`、`SimulationBrickEditor.cs` | 保护目标格，只提供墙塔，保持实时编辑 |
| `Assets/Scripts/Feedback/SimulationRoundMarkers.cs` | 原始窗口目标提示，资源复用与清理 |
| `Assets/Scripts/Application/ProjectionPlan.cs`、`Feedback/ProjectionSimulator.cs` | 桌面核心/入口投影，清除旧面片时立即停显 |
| `Assets/Editor/SimulationProjectionPreview.cs` | 独立展示目标，不重复镜像原始提示几何 |
| `Assets/Editor/SimulationFeedbackSetup.cs`、场景、`ProjectSettings/TagManager.asset` | 新默认规则与分层接线 |

```text
GeneratedRound → 固定目标 / 编辑保留格 / 原始提示
                       ↓ 已知关卡配置
TopCamera RGB → B 预测与连续帧稳定 → BoardSyncSession → BoardUpdate
  → SimulationLevelAdapter / LevelFeedbackSession → LevelState
  → BattleLevel.Build → BattleSimulation.TryUpdateLevel
       ├─ 全部入口可行：新敌人使用新路线；可连接旧敌人立即重寻路
       │    └─ 个别旧敌人无法连接：各自保留旧路 → 移动脱离 → 自动重寻路
       └─ 入口无路：保留完整旧入口路线，采用新墙塔，警告并继续
  → 投影计划 / 战斗显示 / 只读展示窗口
```

复现实验可调用 `controller.GenerateNewRound(seed: 2, spawnCount: 3)`；须在允许生成新关卡的状态调用。控制台记录种子、实际核心和入口坐标，面板显示种子、核心及入口数量。应同时记录 `GeneratedRound.GeneratorVersion`（当前 `generated-round-v1`）、棋盘定义、种子和入口数量，不把不同生成版本当作相同实验。

## 验证与限制

当前自动 RGB、暂停采集、光路与纯逻辑验证见 [验证摘要](verification/current-simulation.md)。早期系统目标、局部脱离、空格中心寻路及消隐的阶段结果见 [历史验收](history/generated-round-validation-2026-11-06.md)。

关卡难度平衡、数量经济、真实投影干扰成组实验、多层及实体设备仍需验证。当前没有用历史 Player 构建代替本地新版本构建。机制比较与授权记录见 [编码前研究](research/generated-defense-round-github.md)。
