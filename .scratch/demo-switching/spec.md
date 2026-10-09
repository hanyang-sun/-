# 示例切换显示回归

日期：2026-10-09。用户症状：两个示例只显示地图。

## 最小失败路径

实际 Unity 场景进入 Play → 应用使用 `physical-color-*` 的照片关卡 → 调用普通示例按钮的 `LoadExample(false)`。
旧版结果：底板 400 格保留；实体和绘制积木均为 0；错误 `UNMAPPED_COLOR`、`NO_CORE`。
初始示例和照片阶段均可绘制 9 块，排除相机、材料和示例原始布局损坏。

检查入口：`BrickDefense.Editor.DemoSceneChecks.VerifyExamples`，仅在独立 batch 项目中运行。
修复前日志 `.scratch/demo-switching-before.log`，退出 1。

## 修复

示例入口先恢复 `ExampleScans.CreateDemoRules()`，再映射示例并生成关卡，避免保留上一张照片的颜色规则。
前置 GitHub 比较记录：`docs/research/demo-switching-github.md`。

## 验收结果

- Unity Play 检查退出 0：初始示例、照片、照片后两个示例、重复切换。
- 普通示例：9 块实体、9 个可绘制对象、16 步路线。
- 包围示例：5 块实体、5 个可绘制对象、预期 `NO_ROUTE`；保留布局以展示不可达原因。
- 相机渲染人工核对通过；图片位于 `verification/`。
- 架构说明已写入 `docs/architecture.md`，没有进行额外架构重写。
