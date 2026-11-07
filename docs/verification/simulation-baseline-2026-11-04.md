# 仿真主线检查与验证边界

> 历史基线：此处对应 2026-11-04 原工程，不覆盖当前 C 的固定投影仪与两种识别模式。现行结果见 [当前验证摘要](current-simulation.md)。

日期：2026-11-04。代码基线 `ab7df00acc5a633d5d33d4ce2702a381b4458085`；本次仅整理文件跟踪规则、验证资料和文档，不变更运行时代码。

## 本次执行

使用 .NET 10 SDK，在从 Git 索引导出的独立源码目录执行：

```powershell
dotnet run --project Tests/Brick2Life.Checks.csproj
```

导出范围为该工程实际编译的 Core、Application、Vision、Battle 和已跟踪 Tests 文件，不复制原工作区的 `.scratch`、未跟踪输入或 `Archive`。此检查用于确认正式逻辑检查不依赖本次撤出的资料。

| 检查组 | 通过断言数 |
| --- | ---: |
| SimulationLayout | 325 |
| BoardContract | 24 |
| BoardSync | 20 |
| DatasetPlan | 21 |
| LevelSemantics | 22 |
| ProjectionPlan | 29 |
| LevelFeedback | 26 |
| Vision | 29 |
| Battle | 104 |
| LiveBattle | 188 |
| GeneratedRound | 215 |
| GeneratedBattleFallback | 73 |
| 合计 | 1076 |

结束标志为 `BRICK2LIFE_CHECKS_PASSED`，退出码 0。原始运行日志及整理校验结果在本机 `.scratch/repository-hygiene/`，不随仓库提交。

## 已有 Unity 记录与重跑入口

[2026-11-02 消隐同步记录](../research/synchronized-projection-blanking-github.md)记载：Unity 6000.3.24f1 中消隐检查本次 333 项、双窗口 179 项、自动布局刷新 108 项通过。消隐观察计数随帧率变化，不应作为固定数量指标。

可通过以下现有入口在隔离 Unity 工程复测，按实际安装位置设置 `-UnityPath`：

```powershell
pwsh -File Tests/RunSimulationDualWindowChecks.ps1 -CheckClass ProjectionBlankingSyncChecks
pwsh -File Tests/RunSimulationDualWindowChecks.ps1 -CheckClass SimulationDualWindowChecks
pwsh -File Tests/RunSimulationDualWindowChecks.ps1 -CheckClass LiveProjectionRefreshChecks
```

本次目录整理没有重跑 Unity、Windows Player 构建或人工观感检查。上面的历史结果不应标记为本次重新执行的结果。

## 未覆盖范围

- 1076 是控制台断言数，不是独立测试用例数量、覆盖率或识别准确率。
- 这些检查不证明真实相机在曝光、白平衡、遮挡及未知高度下的识别性能。
- 仿真投影消隐不等于真实投影仪和摄像机的曝光同步。
- 旧实拍与旧 Windows 验证记录属于归档工程，不表示当前主线已重新接入这些入口。
