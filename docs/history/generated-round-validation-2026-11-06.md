# 系统目标与寻路阶段验收（2026-10 至 2026-11-06）

> 历史记录：包括已经退役的周期性消隐和初期投影结果。当前复测入口与结论见 [验证摘要](../verification/current-simulation.md)。


2026-11-06 固定投影仪：实际图像检查 **26 项**、自动 RGB/战斗/道路 **146 项**、双窗口消隐与恢复 **175 项**、纯 C# **1195 项**通过。覆盖表面颜色反射、高度与位置偏移、投影方向遮挡、黑场/残余光及不直接显示源面片；详见 [固定投影仪仿真](../physical-projector-guide.md)。

2026-11-06 低频消隐优化：纯 C# 全量 **1195 项**通过，含新增自适应节奏 **17 项**；Unity 采样对照 **29 项**、双窗口消隐与异常恢复 **208 项**、实时布局/道路显示 **146 项**通过。默认稳定期 0.8 秒、确认期 0.12 秒，三帧确认与单次最短消隐不变。

2026-11-06 空格中心寻路：纯 C# 全量 **1178 项**通过，新增中心路线与间距检查 **73 项**，包含一格通道、宽路优先、积木旁半格间距及 40 张固定种子地图与独立 10×10 Dijkstra 对照；Unity 实际 RGB、动态更新及投影道路位置检查 **146 项**通过。

2026-11-06 局部脱离修复：纯 C# 全量 **1105 项**通过，其中新增局部重寻路 **19 项**；Unity 实际 RGB、稳定确认、投影窗口和面板检查 **130 项**通过。覆盖持续出怪时新敌人立即绕开新墙、旧敌人连续移动并自动恢复、局部提示清除、暂停、移走新墙和真正全局堵路回退。下列为早期版本的历史验证记录。

2026-10-30 本机结果：

- 纯 C# **1076 项**通过，含新增目标生成 215 项、堵路回退 73 项；覆盖确定性、1/2/多个入口、目标互异、初始可达、部分入口堵路、敌人脚下障碍、塔更新、暂停与恢复。
- Unity 新规则真实场景 **167 项**通过，使用实际 RGB 预测与稳定确认，覆盖单/多入口、60 块玩家积木、保留格保护、战斗堵路继续、修复重规划、无新布局版本的障碍脱离恢复、准备期堵路开战、重试路线恢复、核心受伤桌面反馈和双窗口只读。
- Unity 原流程双窗口 **179 项**、控制器 **131 项**、实时编辑 **71 项**通过。旧流程测试明确关闭 `generateObjectives`，保留手动核心与布局等待行为的兼容检查。
- 新规则样例干净 RGB 整盘精确识别率 **100%**；目标提示开/关、预览操作前/后对源 RGB 的变化均 **0 像素**。这些是当前仿真样例的回归结果，不代表带投影或真实设备的总体准确率。

```powershell
dotnet run --project Tests/Brick2Life.Checks.csproj --no-restore
pwsh -File Tests/RunSimulationDualWindowChecks.ps1 -CheckClass GeneratedDefenseRoundChecks
pwsh -File Tests/RunSimulationDualWindowChecks.ps1
pwsh -File Tests/RunBattleControllerChecks.ps1
pwsh -File Tests/RunBattleControllerChecks.ps1 -CheckClass BattleLiveEditingChecks
```

本机新规则结果：`.scratch/dual-window/run-20261029-172644-289/verification-GeneratedDefenseRoundChecks.json`；同目录 `generated-round.png`、`blocked-fallback.png` 可查看效果。

复用成熟部分：现有固定格位识别、确认会话、四方向 A*、固定步长战斗与双窗口。新增目标生成和旧路回退已通过上述仿真验证。关卡难度平衡、数量经济、真实投影干扰成组实验、多层及实体设备仍需后续验证；本轮没有重新执行 Windows Player 构建。机制比较与授权记录见 [编码前研究](../research/generated-defense-round-github.md)。
