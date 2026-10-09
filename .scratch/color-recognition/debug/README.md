# 颜色识别临时诊断

此目录是开发探针归档，不参与产品或正式检查。正式回归位于 `Tests/PhotoColorRegressionChecks.cs`。

`--color-probe` 含 `[DEBUG-color]` 日志；宽松阈值参数只用于旧版本单变量探针，不能用于正式验收。
原 `repro` 目录迁移至此以明确区分调试工具。宽松一致率参数曾命名 `--inner-grid`，实际只改变一致率，不改变采样位置。
