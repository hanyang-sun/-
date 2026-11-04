# 颜色识别临时诊断

> 归档说明（2026-11-04）：本页记录当时的验证范围，不能直接作为当前主线验收。截图、扫描中间数据和一次性调试代码已停止跟踪，本地文件保留；完整清单与固定历史提交见[验证资料索引](../../../docs/verification/README.md)。旧 `BrickDefense.Checks.csproj` 命令需在对应旧工程中使用。

此目录是开发探针归档，不参与产品或正式检查。正式回归位于 `Tests/PhotoColorRegressionChecks.cs`。

`--color-probe` 含 `[DEBUG-color]` 日志；宽松阈值参数只用于旧版本单变量探针，不能用于正式验收。
原 `repro` 目录迁移至此以明确区分调试工具。宽松一致率参数曾命名 `--inner-grid`，实际只改变一致率，不改变采样位置。
