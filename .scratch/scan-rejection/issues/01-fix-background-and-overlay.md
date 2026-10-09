Status: ready-for-agent
Completion: complete (2026-10-09)
Type: task

# 修复真实标定下的灰背景误判与照片面板透出

规格与因果证据见 [spec](../spec.md)，编码前检索见 [研究](../../../docs/research/scan-rejection-github.md)。

## 完成结果

完整原 JPG 用用户的实际标定识别到 10 块。保留真正裁切的拒绝，增加原图区域红框和报告记录。照片面板全窗口不透明。新回归、全部 .NET 检查、隔离 Unity 的场景/存储检查及真实 Game 视图截图均通过。

## Comments

用户回复“下载”后，在下载文件和应用已保存扫描中定位原 JPG 与裁切版。没有要求再次上传或更改用户的角色配置；没有提交 Git commit。
