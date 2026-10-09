# 架构重构记录

本目录记录 2026-10-09 已完成的五模块重构。

- `spec.md`：范围、验收和完成结果。
- `unity-play.log`：隔离 Unity Play 回归日志。
- `unity-check/Artifacts/`：相机渲染截图及扫描读写检查产物；验证项目、缓存和产物被 Git 忽略，可重新生成。
- `debug/`：一次性文本迁移脚本的归档。

## 迁移脚本

`debug/prepare_structure.py` 和 `debug/refactor_photo_panel.py` 仅用于记录本次迁移过程。
它们依赖重构前的目录结构和源文件位置，不可重复执行，也不是构建或开发入口。
后续修改直接编辑 `Assets/` 中的模块源码。

## 可重复的检查

在项目根目录运行 `dotnet run --project Tests/BrickDefense.Checks.csproj`。
Unity 批量检查入口为 `-executeMethod BrickDefense.Editor.DemoSceneChecks.VerifyExamples`，会进入 Play 并检查公开流程和实际渲染，完成后自行退出。
它仅允许批量运行，需使用单独的验证项目，并保留图形渲染以检查相机截图，避免与正在打开的工作项目争用。
