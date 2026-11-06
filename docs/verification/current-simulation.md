# 当前仿真验证摘要

整理：2026-11-07。适用 Brick2Life-C 当前本地工作树，不代表这些未提交改动已经进入远端。下面是已存在的 2026-11-06 本地记录；本次文档清理未重跑产品测试。

## 最后一次本地结果

| 检查 | 记录 | 范围 |
| --- | --- | --- |
| 纯 C# | 1216 项断言通过 | Core、Application、Vision、Battle；不是独立样本量或覆盖率 |
| `LiveProjectionRefreshChecks` | 最近实跑 1381 项、1224 个连续观察帧，无失败 | 连续 RGB 自动确认、实时改图、波次与胜败刷新、局部脱离；追加导出自动识别输入 |
| `PauseRecognitionChecks` | 103 项通过 | 准备/战斗暂停采集、新帧确认后恢复、切换、取消、异常/超时释放 |
| `PhysicalProjectorChecks` | 75 项通过，45 个光斑位置，最大误差约 0.01348 格 | 成像、反射、遮挡、不同显式高度和投影仪位姿的校正 |

Unity 本地证据目录：`.scratch/dual-window/run-20261106-two-modes/`。摘要来自其中对应的 `verification-*.json`；实际 RGB 示例为 `continuous-recognition-input.png`。大图、日志及隔离工程不随源码分发。连续检查含逐观察帧断言，其总数会随机器帧率和运行时序变化，不作为固定验收阈值。

## 当前复测入口

在仓库根目录运行，Unity 检查需要目标版本编辑器及图形环境：

```powershell
pwsh -File Tests/RunLogicChecks.ps1
pwsh -File Tests/RunSimulationDualWindowChecks.ps1 -CheckClass LiveProjectionRefreshChecks
pwsh -File Tests/RunSimulationDualWindowChecks.ps1 -CheckClass PauseRecognitionChecks
pwsh -File Tests/RunSimulationDualWindowChecks.ps1 -CheckClass PhysicalProjectorChecks
```

逻辑 CI 执行同一纯 C# 脚本，不覆盖 Unity 渲染、交互或 Player 构建；远端运行状态以 Actions 为准。Windows 构建入口为 `Tests/BuildWindows.ps1`，早期成功记录不表示当前本地版本已重新构建。

## 结论边界

已证明上述指定仿真场景与恢复路径可运行。未完成真实相机/投影仪验收、硬件曝光同步、多层识别、儿童使用研究及成组投影干扰准确率实验。测试中显式放置的高度物体只验证光路，不是 B 预测高度的证据。100 格或逐帧断言不能当作独立实验样本。

早期基线见 [2026-11-04 记录](simulation-baseline-2026-11-04.md)，更早记录见 [历史归档](../history/README.md)。
