Status: ready-for-agent
Completion: complete
Type: task

# 地图校对与记录

按 ../spec.md 与 docs/research/layout-review-github.md 实施纯 C# 编辑模块、照片入口、中文界面和 JSON 保存，验证回归与实际 Unity 渲染。

## Comments

2026-10-12：用户已同意在统一校对环节核验识别正确率和网格位置恢复正确率，随后自由编辑游戏布局。

## Answer

已接入 LayoutEditSession、两阶段中文校对界面、原照片对照、三维草稿预览、稳定 ID 与撤销、可恢复检测和独立 review JSON。完整 .NET 回归及最终 Unity 实际按钮/选格/确认/发布、裁切草稿与宽窄屏检查通过。证据见 ../verification/README.md。
