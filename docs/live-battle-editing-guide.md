# 战斗中实时编辑积木

更新：2026-11-07。适用 Unity 6000.3.24f1、默认系统目标玩法、10×10 单层与编辑器双窗口。

## 操作与确认

1. Play 自动生成一个核心和 1–4 个入口；在原始窗口编辑墙塔，展示窗口只读，编辑需要原始窗口焦点。
2. 开始前等待 B 的新 RGB 确认。连续模式在准备、战斗、暂停及波次间隔自动识别带投影 RGB，投影保持开启；稳定/候选间隔约 0.8/0.12 秒，要求三次新采集的完整布局一致。
3. 暂停识别模式只在按暂停后关投影并采集。暂停期间可继续编辑；继续/开战前等待最新确认，再恢复投影与战斗。F8/顶部按钮切换模式，不是直接开关展示。
4. 未确认时沿用上一份地图；单帧矩阵变化不代表新布局已提交。过曝或未知可能中断确认，不通过真值直接更新游戏。
5. 重试保留本局目标与玩家积木、重置战斗；生成新关卡才重新生成目标并清空玩家积木。两者都不用于手动刷新投影。

## 路径与状态保留

新确认保留同一个战斗实例、敌人 ID/位置/血量、波次、核心生命、统计、计时及用户暂停。已有塔不会因无关改图刷新攻击冷却。

- 全部入口仍有路：入口立即采用新路线，可连接的存活敌人立即重寻路。新积木压住个别旧敌人时，它们各自沿旧路脱离，在进入其他导航格后尝试连接新路线；不冻结所有入口，不等待新 RGB 版本才重试。
- 任一入口确实无路：采用新墙塔，保留完整上次有效入口路线及旧敌人余程，提示可能穿过新积木，继续战斗。首次使用本局无障碍参考路线；恢复通路并确认后解除全局回退。
- 结构错误：保留感知结果，由应用等待修复。手动暂停不会因为路线修好而自动解除。

正常寻路经过物理空格中心，格边垂直穿过，不走格角。靠近墙/塔的中心节点有软代价，优先宽路；一格宽的空通道仍可行。敌人与投影道路使用同一路线，不在绘制时另行偏移。

## 调用链与显示诊断

```text
原始窗口编辑 → TopCamera RGB
  → ProjectedLightRecognizer（连续）/ FixedGridRecognizer（暂停）
  → LayoutChangeTracker → BoardSyncSession → BoardUpdate
  → SimulationLevelAdapter / LevelFeedbackSession → 新 LevelState
  → SimulationBattleController.SyncConfirmedLayout
  → BattleLevel.Build / BattleSimulation.TryUpdateLevel
  → 路线、塔与投影内容 → 固定投影仪 → 表面光 / 展示窗口
```

`FeedbackChanged` 直达控制器，确认回调内刷新地图及投影几何。胜败后仍刷新显示，但保留本局结果。窗口底部“确认 / 投影 / 画面”版本用于判断确认与展示是否一致。

展示积木来自已确认布局，确认前可暂时落后源场景真实积木。识别未知和显示版本不同需要分别诊断，不能只看单帧准确率。

## 验证

```powershell
pwsh -File Tests/RunLogicChecks.ps1
pwsh -File Tests/RunSimulationDualWindowChecks.ps1 -CheckClass LiveProjectionRefreshChecks
pwsh -File Tests/RunSimulationDualWindowChecks.ps1 -CheckClass PauseRecognitionChecks
```

最后一次结果见 [当前验证摘要](verification/current-simulation.md)。旧 0.15 秒采样、全局原子路线回退和周期性采样消隐的记录不再作为当前规则。
