# 架构早期验证记录（2026-10）

> 历史验收，不代表当前版本重跑结果。当前范围见 [最新验证摘要](../verification/current-simulation.md)。


2026-10-30 当前规则：纯 C# 1076 项、系统目标真实场景 167 项、原流程双窗口 179 项、控制器 131 项、实时编辑 71 项通过。目标提示与展示操作对干净 RGB 均为 0 像素变化。新增目标与回退机制、调用链及验证范围见 [玩法指南](../generated-defense-round-guide.md)；以下保留各阶段的历史验证记录，不代表本轮重跑了全部采集及 Player 构建。

2026-10-20 实际执行：

- `dotnet run --project Tests/Brick2Life.Checks.csproj`：369 项断言通过（原布局 325、合同 24、确认会话 20）。
- Unity 相机回归：684 项通过，验证工程为 `.scratch/camera-metadata-validation/run-20261020-150048-231/`。采集 fixture 不创建编辑界面，直接绑定棋盘会话。
- Unity 光照/编辑/采集回归：349 项通过、34 对样本，工程为 `.scratch/lighting-validation/run-20261020-151032-153/`。包含真实场景增删移动、占用拒绝、同帧几何更新、冻结采集、滑块和变化拒绝。
- Windows x64 Development Player 构建成功，输出 `Builds/Brick2Life/Brick2Life.exe`；`Builds/architecture-verification.json` 确认 Core/Application 无 Unity 引用、当前场景无缺失脚本。
- 归档的 178 个文件（含 74 个 .meta）逐项 SHA-256 一致；原场景除状态配置/接线外的记录和字体二进制保持不变。

光照首次恢复检查中，配置/GT 完全一致，9/262144 像素出现最多 1 级通道差异；相同配置的另一张图也有 8 像素单级波动。检查现要求配置完全一致，且画面最多 0.01% 像素、每通道最多差 1；最终回归实际差异为 0。首次并发相机检查因 Package Manager 连接取消退出，串行重跑通过。

构建和隔离场景检查不代表 B/C 已接通，也不代表真实相机精度、实体投影误差或 Android 真机验收。当前主工程已安全退出 Play，运行时的 10 块布局和画面另存于工作区 `.artifact-work/architecture-20261020/`，磁盘场景仍保留原布局。

机制比较与采用范围见 [编码前检索](../research/brick2life-core-architecture-github.md)。

随后加入有限自动测试集：纯 C# 检查增加至 390 项，Unity 光照／棋盘／保存回归增加至 359 项并保持 34 对光照样本。已实际生成并检查 100 对 RGB/GT，另验证 Windows 菜单启动方式和中途取消。运行路径、数据检查结果与使用方式见 [测试集生成指南](../task-a-dataset-generation-guide.md)。

2026-10-25 任务 C 实际执行：

- `dotnet run --project Tests/Brick2Life.Checks.csproj`：463 项断言通过（新增关卡语义 22、投影计划 25、反馈会话 26；原 390 项不变）。
- 编译基线：用 Unity 2022.3.62f3c1 的模块程序集与 netstandard 2.1 引用、以及内置 uGUI 源码编译出的 `UnityEngine.UI`，逐程序集编译 Core/Application/Simulation/Feedback/Editor，全部通过。这是语法与 API 基线，不替代 Unity 6000.3.24f1 的实际编译。
- 未执行（本机无 Unity 6.3）：在真实编辑器中运行场景、面片与格位对齐的实测、投影开/关的顶置相机图像对照、分层掩码的运行时复核、Windows Player 构建。执行方式与核对点见 [任务 C 反馈指南](../task-c-feedback-guide.md)。
